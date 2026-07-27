using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Siliq.Water
{
    /// <summary>
    /// ハイトフィールド生成と各種マップ変換のコア。
    /// 全ノイズはトーラス上で定義され、生成結果は必ずシームレスにタイリングし、
    /// t ∈ [0,1) で完全ループする。UnityEditor 非依存のためランタイム生成にも使える。
    ///
    /// パイプラインは float 精度 (Color[]) で統一され、8bit PNG 用には最後に量子化する。
    /// 16bit EXR 書き出しやスーパーサンプリングはこの float パイプラインの上に成り立つ。
    /// </summary>
    public static class WaterMapCore
    {
        const float TwoPi = Mathf.PI * 2f;

        // ---------------------------------------------------------------
        // ハッシュ / 乱数
        // ---------------------------------------------------------------

        static uint HashUInt(uint x)
        {
            x ^= x >> 16;
            x *= 0x7feb352du;
            x ^= x >> 15;
            x *= 0x846ca68bu;
            x ^= x >> 16;
            return x;
        }

        static uint Hash(int x, int y, int seed)
        {
            uint h = (uint)seed;
            h = HashUInt(h ^ (uint)(x * 374761393));
            h = HashUInt(h ^ (uint)(y * 668265263));
            return h;
        }

        static float Hash01(int x, int y, int seed)
        {
            return Hash(x, y, seed) * (1f / 4294967295f);
        }

        // ---------------------------------------------------------------
        // タイリング可能パーリンノイズ
        // ---------------------------------------------------------------

        static int Mod(int a, int m)
        {
            int r = a % m;
            return r < 0 ? r + m : r;
        }

        static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

        static Vector2 Gradient(int x, int y, int seed)
        {
            float a = Hash01(x, y, seed) * TwoPi;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        /// <summary>周期 (perX, perY) でラップするグラディエントノイズ。戻り値はおよそ [-1, 1]。</summary>
        public static float PerlinTileable(float x, float y, int perX, int perY, int seed)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float tx = x - x0;
            float ty = y - y0;

            int gx0 = Mod(x0, perX);
            int gx1 = Mod(x0 + 1, perX);
            int gy0 = Mod(y0, perY);
            int gy1 = Mod(y0 + 1, perY);

            Vector2 g00 = Gradient(gx0, gy0, seed);
            Vector2 g10 = Gradient(gx1, gy0, seed);
            Vector2 g01 = Gradient(gx0, gy1, seed);
            Vector2 g11 = Gradient(gx1, gy1, seed);

            float d00 = g00.x * tx + g00.y * ty;
            float d10 = g10.x * (tx - 1f) + g10.y * ty;
            float d01 = g01.x * tx + g01.y * (ty - 1f);
            float d11 = g11.x * (tx - 1f) + g11.y * (ty - 1f);

            float u = Fade(tx);
            float v = Fade(ty);

            float a = Mathf.Lerp(d00, d10, u);
            float b = Mathf.Lerp(d01, d11, u);
            return Mathf.Lerp(a, b, v) * 1.4142f;
        }

        /// <summary>周期を保ったままオクターブを重ねる fBm。ラクナリティは 2 固定(タイリング維持のため)。</summary>
        public static float FbmTileable(float u, float v, int perX, int perY, int octaves, float persistence, int seed, bool ridged)
        {
            float sum = 0f;
            float amp = 1f;
            float ampSum = 0f;
            int px = perX;
            int py = perY;

            for (int i = 0; i < octaves; i++)
            {
                float n = PerlinTileable(u * px, v * py, px, py, seed + i * 131);
                if (ridged)
                {
                    n = 1f - Mathf.Abs(n) * 2f; // 尾根状に変形
                }
                sum += n * amp;
                ampSum += amp;
                amp *= persistence;
                px *= 2;
                py *= 2;
            }
            return sum / Mathf.Max(ampSum, 1e-5f);
        }

        // ---------------------------------------------------------------
        // タイリング可能ボロノイ
        // ---------------------------------------------------------------

        /// <summary>F1 / F2 距離を返す。座標はセルグリッド空間。t でセル中心がループ運動する。</summary>
        public static void VoronoiTileable(float x, float y, int cellsX, int cellsY, int seed, float jitter, float t, int speed, out float f1, out float f2)
        {
            int cx = Mathf.FloorToInt(x);
            int cy = Mathf.FloorToInt(y);
            f1 = float.MaxValue;
            f2 = float.MaxValue;
            float ang = t * speed * TwoPi;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int ix = cx + dx;
                    int iy = cy + dy;
                    int wx = Mod(ix, cellsX);
                    int wy = Mod(iy, cellsY);

                    float ph = Hash01(wx, wy, seed + 71) * TwoPi;
                    float ox = 0.5f + jitter * 0.45f * Mathf.Sin(ang + ph);
                    float oy = 0.5f + jitter * 0.45f * Mathf.Cos(ang + ph * 1.7f + Hash01(wx, wy, seed + 913) * TwoPi);

                    float fx = ix + ox;
                    float fy = iy + oy;

                    float ddx = x - fx;
                    float ddy = y - fy;
                    float d = Mathf.Sqrt(ddx * ddx + ddy * ddy);

                    if (d < f1)
                    {
                        f2 = f1;
                        f1 = d;
                    }
                    else if (d < f2)
                    {
                        f2 = d;
                    }
                }
            }
        }

        // ---------------------------------------------------------------
        // レイヤー評価
        // ---------------------------------------------------------------

        /// <summary>1 レイヤーの高さを評価。u, v ∈ [0,1)、t ∈ [0,1)。戻り値はおよそ [-1, 1]。</summary>
        public static float EvaluateLayer(WaveLayer layer, float u, float v, float t, int globalSeed)
        {
            int seed = layer.seed * 7919 + globalSeed;

            // ドメインワープ: 周期関数によるオフセットなのでタイリングは保たれる
            if (layer.warpAmount > 0f)
            {
                int wp = Mathf.Max(1, layer.warpScale);
                float wu = PerlinTileable(u * wp, v * wp, wp, wp, seed + 7771) * layer.warpAmount * 0.25f;
                float wv = PerlinTileable(u * wp + 31.7f, v * wp + 17.3f, wp, wp, seed + 8887) * layer.warpAmount * 0.25f;
                u += wu;
                v += wv;
            }

            float h;

            switch (layer.type)
            {
                case WaveLayerType.PerlinWaves:
                case WaveLayerType.RidgedWaves:
                {
                    // 流れ方向へ整数速度でスクロール → t=1 で必ず元に戻る
                    float dirRad = layer.directionDeg * Mathf.Deg2Rad;
                    int sx = Mathf.RoundToInt(Mathf.Cos(dirRad) * layer.speed);
                    int sy = Mathf.RoundToInt(Mathf.Sin(dirRad) * layer.speed);
                    if (layer.speed > 0 && sx == 0 && sy == 0) sx = layer.speed;

                    float uu = u + t * sx;
                    float vv = v + t * sy;

                    // 流れ (X) 方向へ引き伸ばす: X の周期を下げると模様が横に伸びる
                    int perX = Mathf.Max(1, layer.scale / Mathf.Max(1, layer.stretch));
                    int perY = Mathf.Max(1, layer.scale);

                    bool ridged = layer.type == WaveLayerType.RidgedWaves;
                    h = FbmTileable(uu, vv, perX, perY, layer.octaves, layer.persistence, seed, ridged);
                    if (ridged || Mathf.Abs(layer.sharpness - 1f) > 0.01f)
                    {
                        float n01 = Mathf.Clamp01(h * 0.5f + 0.5f);
                        h = Mathf.Pow(n01, layer.sharpness) * 2f - 1f;
                    }
                    break;
                }

                case WaveLayerType.VoronoiCells:
                {
                    int cells = Mathf.Max(1, layer.scale);
                    VoronoiTileable(u * cells, v * cells, cells, cells, seed, layer.jitter, t, layer.speed, out float f1, out _);
                    float d01 = Mathf.Clamp01(f1);
                    // セル中心が盛り上がる丸い水泡状
                    h = (1f - Mathf.Pow(d01, layer.sharpness)) * 2f - 1f;
                    break;
                }

                case WaveLayerType.VoronoiCaustics:
                {
                    int cells = Mathf.Max(1, layer.scale);
                    VoronoiTileable(u * cells, v * cells, cells, cells, seed, layer.jitter, t, layer.speed, out float f1, out float f2);
                    float e = Mathf.Clamp01(f2 - f1);
                    // セル境界が谷になる網目模様(コースティクス風)
                    h = Mathf.Pow(e, 1f / Mathf.Max(0.25f, layer.sharpness)) * 2f - 1f;
                    break;
                }

                case WaveLayerType.DirectionalWaves:
                {
                    float sum = 0f;
                    float ampSum = 0f;
                    for (int i = 0; i < layer.waveCount; i++)
                    {
                        float spread = (Hash01(i, 17, seed) * 2f - 1f) * layer.spreadDeg;
                        float ang = (layer.directionDeg + spread) * Mathf.Deg2Rad;

                        // 波数ベクトルを整数に丸めてタイリングを保証
                        float freq = layer.scale * (0.6f + Hash01(i, 29, seed) * 0.8f);
                        int kx = Mathf.RoundToInt(Mathf.Cos(ang) * freq);
                        int ky = Mathf.RoundToInt(Mathf.Sin(ang) * freq);
                        if (kx == 0 && ky == 0) kx = Mathf.Max(1, layer.scale);

                        int spd = Mathf.Max(1, layer.speed) * (1 + (int)(Hash01(i, 43, seed) * 2f));
                        float phase = Hash01(i, 57, seed) * TwoPi + t * spd * TwoPi;

                        float s = Mathf.Sin(TwoPi * (kx * u + ky * v) + phase);
                        // ゲルストナー風に山を尖らせ谷を広げる
                        float shaped = 2f * Mathf.Pow((s + 1f) * 0.5f, layer.sharpness) - 1f;

                        float amp = 1f / (1f + i * 0.35f);
                        sum += shaped * amp;
                        ampSum += amp;
                    }
                    h = sum / Mathf.Max(ampSum, 1e-5f);
                    break;
                }

                case WaveLayerType.RainRipples:
                {
                    float maxR = 0.7f / Mathf.Max(1, layer.scale);
                    float ringWavelength = maxR / Mathf.Max(1, layer.octaves);
                    float width = maxR * 0.35f / Mathf.Sqrt(layer.sharpness);
                    int spd = Mathf.Max(1, layer.speed);

                    float sum = 0f;
                    for (int i = 0; i < layer.dropCount; i++)
                    {
                        float px = Hash01(i, 101, seed);
                        float py = Hash01(i, 211, seed);
                        float tOff = Hash01(i, 307, seed);

                        // 各滴は t*speed 周期で発生 → 拡大 → 減衰をループ
                        float p = (t * spd + tOff) % 1f;
                        float radius = p * maxR;

                        // トーラス距離
                        float dx = Mathf.Abs(u - px); dx = Mathf.Min(dx, 1f - dx);
                        float dy = Mathf.Abs(v - py); dy = Mathf.Min(dy, 1f - dy);
                        float d = Mathf.Sqrt(dx * dx + dy * dy);

                        float q = d - radius;
                        float envelope = Mathf.Exp(-(q * q) / (2f * width * width));
                        float ring = Mathf.Cos(q / ringWavelength * TwoPi);
                        float fade = (1f - p) * Mathf.Clamp01(p * 10f); // 発生直後と消滅時をなめらかに

                        sum += ring * envelope * fade;
                    }
                    h = Mathf.Clamp(sum, -1.5f, 1.5f) / 1.5f;
                    break;
                }

                default:
                    h = 0f;
                    break;
            }

            // マスク: 低周波ノイズで効きにムラを作る (時間非依存なのでループ・タイリング維持)
            if (layer.maskAmount > 0f)
            {
                int mp = Mathf.Max(1, layer.maskScale);
                float m01 = Mathf.Clamp01(PerlinTileable(u * mp, v * mp, mp, mp, seed + 5551) * 0.5f + 0.5f);
                h *= Mathf.Lerp(1f, m01, layer.maskAmount);
            }

            if (layer.invert) h = -h;
            return h;
        }

        // ---------------------------------------------------------------
        // ハイトフィールド生成
        // ---------------------------------------------------------------

        public static float[] GenerateHeightField(WaterMapSettings settings, int size, float t)
        {
            var heights = new float[size * size];
            var layers = settings.layers;
            float inv = 1f / size;

            Parallel.For(0, size, y =>
            {
                float v = y * inv;
                int row = y * size;
                for (int x = 0; x < size; x++)
                {
                    float u = x * inv;
                    float h = 0f;
                    bool first = true;

                    for (int li = 0; li < layers.Length; li++)
                    {
                        var layer = layers[li];
                        if (!layer.enabled || layer.amplitude <= 0f) continue;

                        float lv = EvaluateLayer(layer, u, v, t, settings.globalSeed) * layer.amplitude;

                        if (first)
                        {
                            h = lv;
                            first = false;
                            continue;
                        }

                        switch (layer.blend)
                        {
                            case WaveBlendMode.Add:
                                h += lv;
                                break;
                            case WaveBlendMode.Multiply:
                                // レイヤー値 [-a, a] を [1-a, 1+a] の係数として乗算(変調)
                                h *= 1f + lv;
                                break;
                            case WaveBlendMode.Max:
                                h = Mathf.Max(h, lv);
                                break;
                            case WaveBlendMode.Min:
                                h = Mathf.Min(h, lv);
                                break;
                        }
                    }

                    heights[row + x] = h;
                }
            });

            if (settings.autoNormalize)
            {
                NormalizeHeights(heights);
            }

            return heights;
        }

        static void NormalizeHeights(float[] heights)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = 0; i < heights.Length; i++)
            {
                float h = heights[i];
                if (h < min) min = h;
                if (h > max) max = h;
            }
            float range = max - min;
            if (range < 1e-6f) return;
            float scale = 2f / range;
            for (int i = 0; i < heights.Length; i++)
            {
                heights[i] = (heights[i] - min) * scale - 1f;
            }
        }

        // ---------------------------------------------------------------
        // 勾配 / ラプラシアン (ラップあり)
        // ---------------------------------------------------------------

        static void GradientAt(float[] h, int size, int x, int y, out float dx, out float dy)
        {
            int xm = (x - 1 + size) % size;
            int xp = (x + 1) % size;
            int ym = (y - 1 + size) % size;
            int yp = (y + 1) % size;
            dx = (h[y * size + xp] - h[y * size + xm]) * 0.5f;
            dy = (h[yp * size + x] - h[ym * size + x]) * 0.5f;
        }

        /// <summary>
        /// Scharr 3x3 勾配。Sobel と同じスケール (中央差分相当) だが、
        /// 重み (3, 10, 3) は回転対称性が最適化されているため、
        /// 斜め方向の波で軸沿いのギザギザが出にくく、法線がなめらかになる。
        /// </summary>
        static void ScharrGradientAt(float[] h, int size, int x, int y, out float dx, out float dy)
        {
            int xm = (x - 1 + size) % size;
            int xp = (x + 1) % size;
            int ym = (y - 1 + size) % size;
            int yp = (y + 1) % size;

            float h00 = h[ym * size + xm];
            float h10 = h[ym * size + x];
            float h20 = h[ym * size + xp];
            float h01 = h[y * size + xm];
            float h21 = h[y * size + xp];
            float h02 = h[yp * size + xm];
            float h12 = h[yp * size + x];
            float h22 = h[yp * size + xp];

            const float inv = 1f / 32f; // 正の重み合計 16 → 中央差分と同じ 0.5 スケール
            dx = (h20 * 3f + h21 * 10f + h22 * 3f - h00 * 3f - h01 * 10f - h02 * 3f) * inv;
            dy = (h02 * 3f + h12 * 10f + h22 * 3f - h00 * 3f - h10 * 10f - h20 * 3f) * inv;
        }

        static float LaplacianAt(float[] h, int size, int x, int y)
        {
            int xm = (x - 1 + size) % size;
            int xp = (x + 1) % size;
            int ym = (y - 1 + size) % size;
            int yp = (y + 1) % size;
            return h[y * size + xp] + h[y * size + xm] + h[yp * size + x] + h[ym * size + x] - 4f * h[y * size + x];
        }

        // ---------------------------------------------------------------
        // 各マップへの変換 (float 精度)
        // ---------------------------------------------------------------

        public static Color[] HeightsToNormalColors(float[] heights, int size, float strength, bool flipY)
        {
            var pixels = new Color[size * size];
            // 解像度に依存しない見た目になるよう勾配をスケール
            float k = strength * size * 0.02f;

            Parallel.For(0, size, y =>
            {
                int row = y * size;
                for (int x = 0; x < size; x++)
                {
                    ScharrGradientAt(heights, size, x, y, out float dx, out float dy);
                    dx *= k;
                    dy *= k;
                    if (flipY) dy = -dy;

                    float nx = -dx;
                    float ny = -dy;
                    float invLen = 1f / Mathf.Sqrt(nx * nx + ny * ny + 1f);
                    float nz = invLen;
                    nx *= invLen;
                    ny *= invLen;

                    pixels[row + x] = new Color(nx * 0.5f + 0.5f, ny * 0.5f + 0.5f, nz * 0.5f + 0.5f, 1f);
                }
            });

            return pixels;
        }

        public static Color[] HeightsToGrayscaleColors(float[] heights, int size)
        {
            var pixels = new Color[size * size];
            Parallel.For(0, size, y =>
            {
                int row = y * size;
                for (int x = 0; x < size; x++)
                {
                    float g = Mathf.Clamp01(heights[row + x] * 0.5f + 0.5f);
                    pixels[row + x] = new Color(g, g, g, 1f);
                }
            });
            return pixels;
        }

        /// <summary>フォームマスク: 波頭 (高さしきい値) + 急斜面 (砕け波) を白く。foamBlur でぼかし。</summary>
        public static Color[] HeightsToFoamColors(float[] heights, int size, WaterMapSettings s)
        {
            var foam = new float[size * size];
            float slopeK = size * 0.02f * s.strength;

            Parallel.For(0, size, y =>
            {
                int row = y * size;
                for (int x = 0; x < size; x++)
                {
                    float h01 = Mathf.Clamp01(heights[row + x] * 0.5f + 0.5f);
                    float crest = Mathf.Clamp01((h01 - s.foamThreshold) / Mathf.Max(1e-4f, s.foamSoftness));
                    crest = crest * crest * (3f - 2f * crest); // smoothstep

                    GradientAt(heights, size, x, y, out float dx, out float dy);
                    float slope = Mathf.Sqrt(dx * dx + dy * dy) * slopeK;
                    float breaking = Mathf.Clamp01(slope * s.foamSlopeBoost);

                    foam[row + x] = Mathf.Clamp01(Mathf.Max(crest, breaking));
                }
            });

            BlurWrapped(foam, size, s.foamBlur);

            return GrayscaleToColors(foam, size);
        }

        /// <summary>ラフネス: ベース + 傾斜 + フォーム部分。exportSmoothness なら反転。</summary>
        public static Color[] HeightsToRoughnessColors(float[] heights, int size, WaterMapSettings s)
        {
            var pixels = new Color[size * size];
            float slopeK = size * 0.02f * s.strength;

            Parallel.For(0, size, y =>
            {
                int row = y * size;
                for (int x = 0; x < size; x++)
                {
                    float h01 = Mathf.Clamp01(heights[row + x] * 0.5f + 0.5f);
                    float crest = Mathf.Clamp01((h01 - s.foamThreshold) / Mathf.Max(1e-4f, s.foamSoftness));

                    GradientAt(heights, size, x, y, out float dx, out float dy);
                    float slope = Mathf.Sqrt(dx * dx + dy * dy) * slopeK;

                    float r = s.baseRoughness + slope * s.slopeRoughness + crest * s.foamRoughness;
                    r = Mathf.Clamp01(r);
                    if (s.exportSmoothness) r = 1f - r;

                    pixels[row + x] = new Color(r, r, r, 1f);
                }
            });
            return pixels;
        }

        /// <summary>DUDV / ディストーション: RG = 屈折オフセット (ノーマル XY と同等)。</summary>
        public static Color[] HeightsToDudvColors(float[] heights, int size, WaterMapSettings s)
        {
            var pixels = new Color[size * size];
            float k = s.strength * s.dudvStrength * size * 0.02f;

            Parallel.For(0, size, y =>
            {
                int row = y * size;
                for (int x = 0; x < size; x++)
                {
                    GradientAt(heights, size, x, y, out float dx, out float dy);
                    float ox = Mathf.Clamp(-dx * k, -1f, 1f);
                    float oy = Mathf.Clamp(-dy * k, -1f, 1f);

                    pixels[row + x] = new Color(ox * 0.5f + 0.5f, oy * 0.5f + 0.5f, 0.5f, 1f);
                }
            });
            return pixels;
        }

        /// <summary>
        /// フローマップ: RG = 流れベクトル。レイヤーの進行方向と
        /// ハイトフィールドの回転成分 (等高線に沿う渦) を flowSwirl で合成。
        /// </summary>
        public static Color[] HeightsToFlowColors(float[] heights, int size, WaterMapSettings s)
        {
            // レイヤー方向の加重平均 (スクロール・進行方向を持つレイヤーのみ)
            Vector2 baseDir = Vector2.zero;
            foreach (var layer in s.layers)
            {
                if (!layer.enabled || layer.amplitude <= 0f) continue;
                bool directional = layer.type == WaveLayerType.DirectionalWaves ||
                                   ((layer.type == WaveLayerType.PerlinWaves || layer.type == WaveLayerType.RidgedWaves) && layer.speed > 0);
                if (!directional) continue;
                float rad = layer.directionDeg * Mathf.Deg2Rad;
                baseDir += new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * layer.amplitude;
            }
            if (baseDir.sqrMagnitude > 1e-6f) baseDir.Normalize();
            else baseDir = Vector2.right;

            var pixels = new Color[size * size];
            float curlK = size * 0.02f;

            Parallel.For(0, size, y =>
            {
                int row = y * size;
                for (int x = 0; x < size; x++)
                {
                    GradientAt(heights, size, x, y, out float dx, out float dy);
                    // 勾配の直交ベクトル = 等高線に沿った渦成分
                    var curl = new Vector2(dy, -dx) * curlK;
                    if (curl.sqrMagnitude > 1f) curl.Normalize();

                    Vector2 flow = Vector2.Lerp(baseDir, curl, s.flowSwirl) * s.flowStrength;
                    flow.x = Mathf.Clamp(flow.x, -1f, 1f);
                    flow.y = Mathf.Clamp(flow.y, -1f, 1f);

                    pixels[row + x] = new Color(flow.x * 0.5f + 0.5f, flow.y * 0.5f + 0.5f, 0f, 1f);
                }
            });
            return pixels;
        }

        /// <summary>
        /// コースティクス近似: 光が収束する場所 (負のラプラシアン = 凸レンズ状) を細い焦点線にし、
        /// その周囲に低周波の散光を重ねる。決定論的なマッピングなのでアニメーションでもちらつかない。
        /// </summary>
        public static Color[] HeightsToCausticsColors(float[] heights, int size, WaterMapSettings s)
        {
            var pixels = new Color[size * size];
            var rawFocus = new float[size * size];
            var softFocus = new float[size * size];
            var wideScatter = new float[size * size];
            // -lap * size^2 は解像度に依存しない曲率。0.5e-4 は代表プリセットの
            // 曲率レンジ (数千〜数万) を [0,1] 付近へ写す較正値。
            float k = 0.5e-4f * s.causticsIntensity * size * (float)size;

            Parallel.For(0, size, y =>
            {
                int row = y * size;
                for (int x = 0; x < size; x++)
                {
                    float focus = -LaplacianAt(heights, size, x, y) * k;
                    float converged = Mathf.Clamp01(0.5f + focus);
                    rawFocus[row + x] = Mathf.Pow(converged, Mathf.Max(0.5f, s.causticsSharpness));
                }
            });

            Array.Copy(rawFocus, softFocus, rawFocus.Length);
            Array.Copy(rawFocus, wideScatter, rawFocus.Length);
            // 焦点線のすぐ外側のにじみと、広く柔らかい散光を別半径で作る。
            // 半径は解像度に比例させ、512px でも 2048px でも同じ見た目になるようにする。
            int blurUnit = Mathf.Max(1, Mathf.RoundToInt(size / 512f));
            BlurWrapped(softFocus, size, blurUnit);
            BlurWrapped(wideScatter, size, blurUnit * 4);

            Parallel.For(0, size, y =>
            {
                int row = y * size;
                float v = y / (float)size;
                for (int x = 0; x < size; x++)
                {
                    int i = row + x;
                    float u = x / (float)size;

                    float tight = Mathf.Pow(Mathf.Clamp01((rawFocus[i] - 0.76f) / 0.20f), 1.35f);
                    float glow = Mathf.SmoothStep(0.56f, 0.90f, softFocus[i]);
                    float veil = Mathf.SmoothStep(0.50f, 0.86f, wideScatter[i]);
                    float macro = Mathf.Clamp01(0.5f + 0.5f * PerlinTileable(u * 3f, v * 3f, 3, 3, s.globalSeed + 9917));
                    float micro = Mathf.Clamp01(0.5f + 0.5f * PerlinTileable(u * 11f + 7.13f, v * 11f + 3.71f, 11, 11, s.globalSeed + 9929));

                    float light = 0.04f;
                    light += tight * 0.58f;
                    light += glow * 0.11f;
                    light += veil * 0.030f;
                    light *= Mathf.Lerp(0.82f, 1.14f, macro);
                    light += micro * 0.010f;
                    light = Mathf.Clamp(light, 0.035f, 0.92f);

                    pixels[i] = new Color(light, light, light, 1f);
                }
            });
            return pixels;
        }

        static Color[] GrayscaleToColors(float[] values, int size)
        {
            var pixels = new Color[size * size];
            Parallel.For(0, size, y =>
            {
                int row = y * size;
                for (int x = 0; x < size; x++)
                {
                    float g = Mathf.Clamp01(values[row + x]);
                    pixels[row + x] = new Color(g, g, g, 1f);
                }
            });
            return pixels;
        }

        /// <summary>
        /// ラップありの分離型ガウシアンブラー (グレースケール値用)。
        /// 3x3 ボックスブラーの反復は十字状のにじみを残すが、二項係数の
        /// 分離カーネルなら等方的にぼけるため、水底光の広がりが自然になる。
        /// </summary>
        static void BlurWrapped(float[] values, int size, int radius)
        {
            radius = Mathf.Min(radius, size / 2);
            if (radius < 1) return;
            float[] kernel = BinomialKernel(radius);
            var tmp = new float[values.Length];

            // 横方向
            Parallel.For(0, size, y =>
            {
                int row = y * size;
                for (int x = 0; x < size; x++)
                {
                    float sum = 0f;
                    for (int k = -radius; k <= radius; k++)
                    {
                        int sx = (x + k) % size;
                        if (sx < 0) sx += size;
                        sum += values[row + sx] * kernel[k + radius];
                    }
                    tmp[row + x] = sum;
                }
            });

            // 縦方向
            Parallel.For(0, size, y =>
            {
                int row = y * size;
                for (int x = 0; x < size; x++)
                {
                    float sum = 0f;
                    for (int k = -radius; k <= radius; k++)
                    {
                        int sy = (y + k) % size;
                        if (sy < 0) sy += size;
                        sum += tmp[sy * size + x] * kernel[k + radius];
                    }
                    values[row + x] = sum;
                }
            });
        }

        /// <summary>正規化済みの二項係数カーネル (ガウシアン近似)。</summary>
        static float[] BinomialKernel(int radius)
        {
            int n = radius * 2;
            var kernel = new float[n + 1];
            double c = 1.0;
            double total = 0.0;
            for (int i = 0; i <= n; i++)
            {
                kernel[i] = (float)c;
                total += c;
                c = c * (n - i) / (i + 1);
            }
            float inv = (float)(1.0 / total);
            for (int i = 0; i <= n; i++) kernel[i] *= inv;
            return kernel;
        }

        // ---------------------------------------------------------------
        // パイプライン: ハイトフィールド → マップ / スーパーサンプリング / 量子化
        // ---------------------------------------------------------------

        /// <summary>生成済みハイトフィールドから指定マップ種の float ピクセルへ変換。</summary>
        public static Color[] ColorsFromHeights(float[] heights, WaterMapSettings settings, WaterMapType mapType, int size)
        {
            switch (mapType)
            {
                case WaterMapType.Normal: return HeightsToNormalColors(heights, size, settings.strength, settings.flipY);
                case WaterMapType.Height: return HeightsToGrayscaleColors(heights, size);
                case WaterMapType.Foam: return HeightsToFoamColors(heights, size, settings);
                case WaterMapType.Roughness: return HeightsToRoughnessColors(heights, size, settings);
                case WaterMapType.Flow: return HeightsToFlowColors(heights, size, settings);
                case WaterMapType.Dudv: return HeightsToDudvColors(heights, size, settings);
                case WaterMapType.Caustics: return HeightsToCausticsColors(heights, size, settings);
                default: throw new ArgumentOutOfRangeException(nameof(mapType));
            }
        }

        /// <summary>
        /// メモリ使用量が過大にならない範囲で実際に適用するスーパーサンプリング係数を返す。
        /// (生成解像度が 4096 を超える場合は無効化)
        /// </summary>
        public static int EffectiveSupersample(WaterMapSettings settings, int size)
        {
            int ss = Mathf.Clamp(settings.supersample, 1, 4);
            // 収まらない場合は 1 に落とすのではなく、収まる最大の係数まで下げる
            while (ss > 1 && (long)size * ss > 4096) ss--;
            return ss;
        }

        /// <summary>
        /// ss×ss ブロック平均でダウンサンプリング。ノーマルマップはベクトルを平均後に再正規化する。
        /// </summary>
        public static Color[] Downsample(Color[] src, int srcSize, int factor, bool isNormalMap)
        {
            if (factor <= 1) return src;
            int dstSize = srcSize / factor;
            var dst = new Color[dstSize * dstSize];
            float inv = 1f / (factor * factor);

            Parallel.For(0, dstSize, y =>
            {
                for (int x = 0; x < dstSize; x++)
                {
                    float r = 0f, g = 0f, b = 0f;
                    for (int sy = 0; sy < factor; sy++)
                    {
                        int srcRow = (y * factor + sy) * srcSize + x * factor;
                        for (int sx = 0; sx < factor; sx++)
                        {
                            Color c = src[srcRow + sx];
                            r += c.r; g += c.g; b += c.b;
                        }
                    }
                    r *= inv; g *= inv; b *= inv;

                    if (isNormalMap)
                    {
                        // [0,1] → [-1,1] へ戻して平均ベクトルを正規化
                        float nx = r * 2f - 1f;
                        float ny = g * 2f - 1f;
                        float nz = Mathf.Max(b * 2f - 1f, 1e-4f);
                        float invLen = 1f / Mathf.Sqrt(nx * nx + ny * ny + nz * nz);
                        r = nx * invLen * 0.5f + 0.5f;
                        g = ny * invLen * 0.5f + 0.5f;
                        b = nz * invLen * 0.5f + 0.5f;
                    }

                    dst[y * dstSize + x] = new Color(r, g, b, 1f);
                }
            });
            return dst;
        }

        /// <summary>float ピクセルを 8bit へ量子化 (PNG / RGBA32 テクスチャ用)。</summary>
        public static Color32[] Quantize(Color[] src)
        {
            var dst = new Color32[src.Length];
            Parallel.For(0, src.Length, i =>
            {
                Color c = src[i];
                dst[i] = new Color32(
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f),
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f),
                    (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f),
                    255);
            });
            return dst;
        }

        // 4x4 Bayer 行列。4 の倍数の解像度ならタイリングを壊さず、
        // 高周波の規則パターンなので mipmap でほぼ消える。
        static readonly int[] BayerMatrix4x4 =
        {
             0,  8,  2, 10,
            12,  4, 14,  6,
             3, 11,  1,  9,
            15,  7, 13,  5,
        };

        /// <summary>
        /// 順序ディザ付きの 8bit 量子化。穏やかな水面のハイトマップや
        /// 水底光のような緩やかなグラデーションは、素の丸めだと 1/255 刻みの
        /// 縞 (バンディング) が出る。±0.5LSB のディザでそれを均す。
        /// 0 と 255 はそのまま保たれる。
        /// </summary>
        public static Color32[] Quantize(Color[] src, int width, bool dither)
        {
            if (!dither || width <= 0) return Quantize(src);

            var dst = new Color32[src.Length];
            int height = src.Length / width;
            Parallel.For(0, height, y =>
            {
                int row = y * width;
                int bayerRow = (y & 3) * 4;
                for (int x = 0; x < width; x++)
                {
                    // [-0.46875, +0.46875] の範囲に収まるので端の値は丸め先が変わらない
                    float offset = (BayerMatrix4x4[bayerRow + (x & 3)] + 0.5f) / 16f - 0.5f;
                    Color c = src[row + x];
                    dst[row + x] = new Color32(
                        DitherToByte(c.r, offset),
                        DitherToByte(c.g, offset),
                        DitherToByte(c.b, offset),
                        255);
                }
            });
            return dst;
        }

        static byte DitherToByte(float value, float offset)
        {
            int v = Mathf.RoundToInt(Mathf.Clamp01(value) * 255f + offset);
            return (byte)Mathf.Clamp(v, 0, 255);
        }

        /// <summary>
        /// ディザを掛けてよいマップ種か。ノーマルマップは 8bit の 1LSB が
        /// そのまま法線長の誤差になるため、ディザは掛けずに 16bit / EXR で扱う。
        /// </summary>
        public static bool ShouldDither(WaterMapSettings settings, WaterMapType mapType)
        {
            return settings.dither8Bit && mapType != WaterMapType.Normal;
        }

        /// <summary>
        /// 指定マップの float ピクセルを生成。settings.supersample に応じて
        /// 高解像度生成 → ブロック平均のスーパーサンプリングを行う。
        /// </summary>
        public static Color[] GenerateColors(WaterMapSettings settings, WaterMapType mapType, int size, float t)
        {
            int ss = EffectiveSupersample(settings, size);
            int genSize = size * ss;
            float[] heights = GenerateHeightField(settings, genSize, t);
            Color[] colors = ColorsFromHeights(heights, settings, mapType, genSize);
            return Downsample(colors, genSize, ss, mapType == WaterMapType.Normal);
        }

        /// <summary>指定マップ種の 8bit ピクセルを生成 (互換 API)。</summary>
        public static Color32[] GeneratePixels(WaterMapSettings settings, WaterMapType mapType, int size, float t)
        {
            return Quantize(GenerateColors(settings, mapType, size, t), size, ShouldDither(settings, mapType));
        }

        // ---------------------------------------------------------------
        // ランタイム / エディタ共通のテクスチャベイク API
        // ---------------------------------------------------------------

        /// <summary>
        /// 指定マップの Texture2D を生成する (ランタイム可)。呼び出し側で破棄すること。
        /// highPrecision = true で RGBAHalf (16bit float) テクスチャを生成し、
        /// 穏やかな水面のバンディングを防げる。
        /// </summary>
        public static Texture2D BakeTexture(WaterMapSettings settings, WaterMapType mapType, int size, float t = 0f, bool highPrecision = false)
        {
            Color[] colors = GenerateColors(settings, mapType, size, t);
            return CreateTexture(colors, mapType, size, highPrecision, ShouldDither(settings, mapType));
        }

        /// <summary>生成済み float ピクセルから Texture2D を作る。Texture2D 作成はメインスレッドで呼ぶこと。</summary>
        public static Texture2D CreateTexture(Color[] colors, WaterMapType mapType, int size, bool highPrecision = false, bool dither = false)
        {
            var format = highPrecision ? TextureFormat.RGBAHalf : TextureFormat.RGBA32;
            var tex = new Texture2D(size, size, format, true, true)
            {
                wrapMode = TextureWrapMode.Repeat,
                name = $"SiliqWater_{mapType}",
            };

            if (highPrecision)
            {
                tex.SetPixels(colors);
            }
            else
            {
                tex.SetPixels32(Quantize(colors, size, dither));
            }
            tex.Apply(true, false);
            return tex;
        }
    }
}
