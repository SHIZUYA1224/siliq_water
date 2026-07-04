using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Siliq.WaterNormalMap
{
    /// <summary>
    /// 波レイヤーの種類。
    /// </summary>
    public enum WaveLayerType
    {
        PerlinWaves,      // fBm パーリンノイズ(穏やかな揺らぎ)
        RidgedWaves,      // リッジノイズ(尖ったうねり)
        VoronoiCells,     // ボロノイ・セル(丸い盛り上がり)
        VoronoiCaustics,  // ボロノイ・エッジ(コースティクス風の網目)
        DirectionalWaves, // 指向性のある波(ゲルストナー風)
        RainRipples,      // 雨の波紋(広がるリング)
    }

    /// <summary>
    /// レイヤー合成モード。
    /// </summary>
    public enum WaveBlendMode
    {
        Add,
        Multiply,
        Max,
        Min,
    }

    [Serializable]
    public class WaveLayer
    {
        public bool enabled = true;
        public string name = "波レイヤー";
        public WaveLayerType type = WaveLayerType.PerlinWaves;
        public WaveBlendMode blend = WaveBlendMode.Add;

        [Range(0f, 2f)] public float amplitude = 1f;

        // 共通: テクスチャ 1 枚あたりの繰り返し数(整数にすることでシームレスを保証)
        [Range(1, 64)] public int scale = 8;

        // ノイズ系
        [Range(1, 8)] public int octaves = 4;
        [Range(0.1f, 0.9f)] public float persistence = 0.5f;

        // 形状の鋭さ(波の頂点の尖り / ボロノイの減衰 / 波紋のリング幅)
        [Range(0.25f, 8f)] public float sharpness = 1f;

        // 指向性波・流れ
        [Range(0f, 360f)] public float directionDeg = 0f;
        [Range(0f, 90f)] public float spreadDeg = 30f;
        [Range(1, 24)] public int waveCount = 6;

        // ノイズを流れ方向 (X 軸) に引き伸ばす(川など)。整数でタイリング維持
        [Range(1, 8)] public int stretch = 1;

        // 雨の波紋
        [Range(1, 128)] public int dropCount = 24;

        // ボロノイのゆらぎ量
        [Range(0f, 1f)] public float jitter = 0.9f;

        // アニメーション速度(整数にすることで完全ループを保証)
        [Range(0, 8)] public int speed = 1;

        public int seed = 0;
        public bool invert = false;

        public WaveLayer Clone()
        {
            return (WaveLayer)MemberwiseClone();
        }
    }

    [Serializable]
    public class WaterNormalMapSettings
    {
        public int resolution = 1024;
        [Range(0.05f, 5f)] public float strength = 1f;
        public bool autoNormalize = true;
        public bool flipY = false; // DirectX 系ツールに合わせたい場合のみ true
        public int globalSeed = 1234;

        // アニメーション書き出し
        [Range(2, 64)] public int frameCount = 16;

        // Quest / モバイル向けインポート設定を自動適用
        public bool applyMobileImportSettings = true;

        public WaveLayer[] layers = new WaveLayer[0];
    }

    /// <summary>
    /// ハイトフィールド → ノーマルマップ生成のコア。全ノイズはトーラス上で定義され、
    /// 生成結果は必ずシームレスにタイリングし、t ∈ [0,1) で完全ループする。
    /// </summary>
    public static class WaterNormalMapCore
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

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int ix = cx + dx;
                    int iy = cy + dy;
                    int wx = Mod(ix, cellsX);
                    int wy = Mod(iy, cellsY);

                    float ph = Hash01(wx, wy, seed + 71) * TwoPi;
                    float ang = t * speed * TwoPi;
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
                    if (ridged)
                    {
                        // [-1,1] へ整えつつ尖りを制御
                        float n01 = Mathf.Clamp01(h * 0.5f + 0.5f);
                        h = Mathf.Pow(n01, layer.sharpness) * 2f - 1f;
                    }
                    else if (Mathf.Abs(layer.sharpness - 1f) > 0.01f)
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

            if (layer.invert) h = -h;
            return h;
        }

        // ---------------------------------------------------------------
        // ハイトフィールド生成
        // ---------------------------------------------------------------

        public static float[] GenerateHeightField(WaterNormalMapSettings settings, int size, float t)
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
        // ノーマルマップ / ハイトマップへの変換
        // ---------------------------------------------------------------

        public static Color32[] HeightsToNormalPixels(float[] heights, int size, float strength, bool flipY)
        {
            var pixels = new Color32[size * size];
            // 解像度に依存しない見た目になるよう勾配をスケール
            float k = strength * size * 0.02f;

            Parallel.For(0, size, y =>
            {
                int ym = (y - 1 + size) % size;
                int yp = (y + 1) % size;
                int row = y * size;
                int rowM = ym * size;
                int rowP = yp * size;

                for (int x = 0; x < size; x++)
                {
                    int xm = (x - 1 + size) % size;
                    int xp = (x + 1) % size;

                    float dx = (heights[row + xp] - heights[row + xm]) * 0.5f * k;
                    float dy = (heights[rowP + x] - heights[rowM + x]) * 0.5f * k;
                    if (flipY) dy = -dy;

                    float nx = -dx;
                    float ny = -dy;
                    float nz = 1f;
                    float invLen = 1f / Mathf.Sqrt(nx * nx + ny * ny + 1f);
                    nx *= invLen;
                    ny *= invLen;
                    nz *= invLen;

                    pixels[row + x] = new Color32(
                        (byte)Mathf.RoundToInt((nx * 0.5f + 0.5f) * 255f),
                        (byte)Mathf.RoundToInt((ny * 0.5f + 0.5f) * 255f),
                        (byte)Mathf.RoundToInt((nz * 0.5f + 0.5f) * 255f),
                        255);
                }
            });

            return pixels;
        }

        public static Color32[] HeightsToGrayscalePixels(float[] heights, int size)
        {
            var pixels = new Color32[size * size];
            Parallel.For(0, size, y =>
            {
                int row = y * size;
                for (int x = 0; x < size; x++)
                {
                    byte g = (byte)Mathf.RoundToInt(Mathf.Clamp01(heights[row + x] * 0.5f + 0.5f) * 255f);
                    pixels[row + x] = new Color32(g, g, g, 255);
                }
            });
            return pixels;
        }

        /// <summary>設定からノーマルマップの Texture2D を生成(呼び出し側で破棄すること)。</summary>
        public static Texture2D GenerateNormalMapTexture(WaterNormalMapSettings settings, int size, float t)
        {
            float[] heights = GenerateHeightField(settings, size, t);
            Color32[] pixels = HeightsToNormalPixels(heights, size, settings.strength, settings.flipY);

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }
    }
}
