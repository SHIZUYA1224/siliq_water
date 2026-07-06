using NUnit.Framework;
using UnityEngine;

namespace Siliq.Water.Tests
{
    /// <summary>
    /// 生成コアの構造的保証 (シームレスタイリング / 完全ループ / 値域 / 決定性) を検証する。
    /// Unity Test Runner (EditMode) で実行。
    /// </summary>
    public class WaterMapCoreTests
    {
        const float SeamTolerance = 1e-3f;   // 継ぎ目・ループの許容誤差 (値域 ±1 に対して)
        const int SampleCount = 64;

        static int PresetCount => WaterMapPresets.Names.Length;

        // ---------------------------------------------------------------
        // シームレスタイリング
        // ---------------------------------------------------------------

        [Test]
        public void AllPresets_TileSeamlessly_Horizontal()
        {
            for (int p = 0; p < PresetCount; p++)
            {
                var s = WaterMapPresets.Create(p);
                for (int i = 0; i < SampleCount; i++)
                {
                    float v = (float)i / SampleCount;
                    float edge = SumLayers(s, 1f, v, 0f);
                    float zero = SumLayers(s, 0f, v, 0f);
                    Assert.AreEqual(zero, edge, SeamTolerance,
                        $"プリセット '{WaterMapPresets.Names[p]}' の U 方向境界が不連続 (v={v})");
                }
            }
        }

        [Test]
        public void AllPresets_TileSeamlessly_Vertical()
        {
            for (int p = 0; p < PresetCount; p++)
            {
                var s = WaterMapPresets.Create(p);
                for (int i = 0; i < SampleCount; i++)
                {
                    float u = (float)i / SampleCount;
                    float edge = SumLayers(s, u, 1f, 0f);
                    float zero = SumLayers(s, u, 0f, 0f);
                    Assert.AreEqual(zero, edge, SeamTolerance,
                        $"プリセット '{WaterMapPresets.Names[p]}' の V 方向境界が不連続 (u={u})");
                }
            }
        }

        // ---------------------------------------------------------------
        // アニメーションの完全ループ
        // ---------------------------------------------------------------

        [Test]
        public void AllPresets_LoopPerfectly()
        {
            for (int p = 0; p < PresetCount; p++)
            {
                var s = WaterMapPresets.Create(p);
                var rng = new System.Random(1);
                for (int i = 0; i < SampleCount; i++)
                {
                    float u = (float)rng.NextDouble();
                    float v = (float)rng.NextDouble();
                    float t0 = SumLayers(s, u, v, 0f);
                    float t1 = SumLayers(s, u, v, 1f);
                    Assert.AreEqual(t0, t1, SeamTolerance,
                        $"プリセット '{WaterMapPresets.Names[p]}' が t=0 と t=1 で一致しない (u={u}, v={v})");
                }
            }
        }

        // ---------------------------------------------------------------
        // ハイトフィールド
        // ---------------------------------------------------------------

        [Test]
        public void HeightField_AutoNormalize_UsesFullRange()
        {
            var s = WaterMapPresets.Create(0);
            s.autoNormalize = true;
            float[] h = WaterMapCore.GenerateHeightField(s, 64, 0f);

            float min = float.MaxValue, max = float.MinValue;
            foreach (float v in h)
            {
                if (v < min) min = v;
                if (v > max) max = v;
            }
            Assert.AreEqual(-1f, min, 1e-4f);
            Assert.AreEqual(1f, max, 1e-4f);
        }

        [Test]
        public void HeightField_IsDeterministic()
        {
            var s = WaterMapPresets.Create(1);
            float[] a = WaterMapCore.GenerateHeightField(s, 64, 0.25f);
            float[] b = WaterMapCore.GenerateHeightField(s, 64, 0.25f);
            CollectionAssert.AreEqual(a, b, "同一設定・同一時刻で結果が変わってはならない");
        }

        // ---------------------------------------------------------------
        // マップ変換
        // ---------------------------------------------------------------

        [Test]
        public void AllMapTypes_ProduceCorrectPixelCount()
        {
            var s = WaterMapPresets.Create(1);
            const int size = 64;
            foreach (WaterMapType mapType in System.Enum.GetValues(typeof(WaterMapType)))
            {
                Color[] colors = WaterMapCore.GenerateColors(s, mapType, size, 0f);
                Assert.AreEqual(size * size, colors.Length, $"{mapType} のピクセル数が不正");
            }
        }

        [Test]
        public void NormalMap_PixelsAreUnitVectors()
        {
            var s = WaterMapPresets.Create(1);
            const int size = 64;
            Color[] colors = WaterMapCore.GenerateColors(s, WaterMapType.Normal, size, 0f);

            foreach (Color c in colors)
            {
                var n = new Vector3(c.r * 2f - 1f, c.g * 2f - 1f, c.b * 2f - 1f);
                Assert.AreEqual(1f, n.magnitude, 0.01f, "ノーマルが単位ベクトルでない");
                Assert.Greater(n.z, 0f, "タンジェント空間ノーマルの Z は正であるべき");
            }
        }

        [Test]
        public void GrayscaleMaps_ValuesWithinRange()
        {
            var s = WaterMapPresets.Create(1);
            const int size = 64;
            foreach (var mapType in new[] { WaterMapType.Height, WaterMapType.Foam, WaterMapType.Roughness, WaterMapType.Caustics })
            {
                Color[] colors = WaterMapCore.GenerateColors(s, mapType, size, 0f);
                foreach (Color c in colors)
                {
                    Assert.That(c.r, Is.InRange(0f, 1f), $"{mapType} の値域が [0,1] を外れている");
                    Assert.AreEqual(c.r, c.g, 1e-5f, $"{mapType} はグレースケールであるべき");
                }
            }
        }

        [Test]
        public void Supersampling_ReturnsRequestedSize()
        {
            var s = WaterMapPresets.Create(0);
            s.supersample = 2;
            const int size = 64;
            Color[] colors = WaterMapCore.GenerateColors(s, WaterMapType.Normal, size, 0f);
            Assert.AreEqual(size * size, colors.Length);
        }

        [Test]
        public void EffectiveSupersample_DisabledForHugeResolutions()
        {
            var s = new WaterMapSettings { supersample = 2 };
            Assert.AreEqual(2, WaterMapCore.EffectiveSupersample(s, 2048));
            Assert.AreEqual(1, WaterMapCore.EffectiveSupersample(s, 4096));
        }

        [Test]
        public void Quantize_RoundTripsExtremes()
        {
            var colors = new[] { new Color(0f, 0.5f, 1f, 1f) };
            Color32[] q = WaterMapCore.Quantize(colors);
            Assert.AreEqual(0, q[0].r);
            Assert.AreEqual(128, q[0].g);
            Assert.AreEqual(255, q[0].b);
        }

        // ---------------------------------------------------------------
        // ベイク API
        // ---------------------------------------------------------------

        [Test]
        public void BakeTexture_ProducesRepeatWrappedTexture()
        {
            var s = WaterMapPresets.Create(0);
            Texture2D tex = null;
            try
            {
                tex = WaterMapCore.BakeTexture(s, WaterMapType.Normal, 64);
                Assert.AreEqual(64, tex.width);
                Assert.AreEqual(64, tex.height);
                Assert.AreEqual(TextureWrapMode.Repeat, tex.wrapMode);
                Assert.AreEqual(TextureFormat.RGBA32, tex.format);
            }
            finally
            {
                if (tex != null) Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void BakeTexture_HighPrecision_UsesHalfFormat()
        {
            var s = WaterMapPresets.Create(0);
            Texture2D tex = null;
            try
            {
                tex = WaterMapCore.BakeTexture(s, WaterMapType.Normal, 64, 0f, highPrecision: true);
                Assert.AreEqual(TextureFormat.RGBAHalf, tex.format);
            }
            finally
            {
                if (tex != null) Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void WaterSurfaceAnimator_AppliesMotionAndLookThroughPropertyBlock()
        {
            GameObject go = null;
            Material mat = null;
            try
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                mat = new Material(Shader.Find("Standard"));
                var renderer = go.GetComponent<Renderer>();
                renderer.sharedMaterial = mat;

                var animator = go.AddComponent<WaterSurfaceAnimator>();
                animator.texturePropertyName = "_BumpMap";
                animator.directionDegrees = 0f;
                animator.speed = 0.6f;
                animator.strength = 2f;
                animator.tiling = 2f;
                animator.opacity = 0.62f;
                animator.ApplyImmediate(0.5f);

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block, 0);
                Vector4 st = block.GetVector("_BumpMap_ST");
                Vector4 mainSt = block.GetVector("_MainTex_ST");

                Assert.AreEqual(2f, st.x, 1e-5f, "tiling が _BumpMap_ST.x に反映されていない");
                Assert.AreEqual(2f, st.y, 1e-5f, "tiling が _BumpMap_ST.y に反映されていない");
                Assert.Greater(st.z, 0.2f, "speed / direction による X offset が反映されていない");
                Assert.AreEqual(2f, mainSt.x, 1e-5f, "Standard の normal UV 用 _MainTex_ST.x に tiling が反映されていない");
                Assert.AreEqual(2f, mainSt.y, 1e-5f, "Standard の normal UV 用 _MainTex_ST.y に tiling が反映されていない");
                Assert.Greater(mainSt.z, 0.2f, "Standard の normal UV 用 _MainTex_ST に offset が反映されていない");
                Assert.AreEqual(2f, block.GetFloat("_BumpScale"), 1e-5f, "strength が _BumpScale に反映されていない");
                Assert.AreEqual(0.62f, block.GetColor("_Color").a, 1e-5f, "opacity が Standard の _Color alpha に反映されていない");
                Vector2 sharedOffset = mat.GetTextureOffset("_BumpMap");
                Assert.AreEqual(0f, sharedOffset.x, 1e-5f, "共有マテリアルを直接変更してはならない");
                Assert.AreEqual(0f, sharedOffset.y, 1e-5f, "共有マテリアルを直接変更してはならない");
            }
            finally
            {
                if (go != null) Object.DestroyImmediate(go);
                if (mat != null) Object.DestroyImmediate(mat);
            }
        }

        [Test]
        public void WaterSurfaceAnimator_AppliesSiliqTransparencyControls()
        {
            GameObject go = null;
            Material mat = null;
            try
            {
                Shader shader = Shader.Find("Siliq/Water Mobile (Quest)");
                Assert.IsNotNull(shader, "Siliq/Water Mobile (Quest) シェーダーが見つからない");

                go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                mat = new Material(shader);
                var renderer = go.GetComponent<Renderer>();
                renderer.sharedMaterial = mat;

                var animator = go.AddComponent<WaterSurfaceAnimator>();
                animator.texturePropertyName = "_NormalMap";
                animator.opacity = 0.48f;
                animator.edgeReflection = 0.73f;
                animator.reflectionStrength = 0.64f;
                animator.sparkle = 0.31f;
                animator.ApplyImmediate(0f);

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block, 0);

                Assert.AreEqual(0.48f, block.GetFloat("_Opacity"), 1e-5f);
                Assert.AreEqual(0.73f, block.GetFloat("_EdgeReflection"), 1e-5f);
                Assert.AreEqual(0.64f, block.GetFloat("_ReflStrength"), 1e-5f);
                Assert.AreEqual(0.31f, block.GetFloat("_GlimmerIntensity"), 1e-5f);
                Assert.AreEqual(0.62f, block.GetFloat("_GlintIntensity"), 1e-5f);
                Assert.AreEqual(1f, mat.GetFloat("_Opacity"), 1e-5f, "共有マテリアルの _Opacity を直接変更してはならない");
            }
            finally
            {
                if (go != null) Object.DestroyImmediate(go);
                if (mat != null) Object.DestroyImmediate(mat);
            }
        }

        [Test]
        public void WaterRippleEmitter_ConfiguresRendererForExpandingRipples()
        {
            GameObject go = null;
            Material mat = null;
            try
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Shader shader = Shader.Find("Siliq/Water Mobile (Quest)") ?? Shader.Find("Standard");
                mat = new Material(shader);
                var renderer = go.GetComponent<Renderer>();
                renderer.sharedMaterial = mat;

                var emitter = go.AddComponent<WaterRippleEmitter>();
                emitter.targetRenderer = renderer;
                emitter.rippleChannel = 3;
                emitter.rippleSpeed = 3.2f;
                emitter.rippleWidth = 0.22f;
                emitter.rippleLifetime = 2.4f;
                emitter.rippleAmplitude = 1.4f;
                emitter.ApplyImmediate();

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);

                Assert.AreEqual(3f, block.GetFloat("_RippleChannel"), 1e-5f);
                Assert.AreEqual(3.2f, block.GetFloat("_RippleSpeed"), 1e-5f);
                Assert.AreEqual(0.22f, block.GetFloat("_RippleWidth"), 1e-5f);
                Assert.AreEqual(2.4f, block.GetFloat("_RippleLifetime"), 1e-5f);
                Assert.AreEqual(1.4f, block.GetFloat("_RippleAmplitude"), 1e-5f);
                Assert.IsTrue(mat.IsKeywordEnabled("_USE_RIPPLES"), "波紋 keyword が有効化されていない");
            }
            finally
            {
                if (go != null) Object.DestroyImmediate(go);
                if (mat != null) Object.DestroyImmediate(mat);
            }
        }

        [Test]
        public void SiliqMobileShader_ExposesMacroVariationControls()
        {
            Material mat = null;
            try
            {
                Shader shader = Shader.Find("Siliq/Water Mobile (Quest)");
                Assert.IsNotNull(shader, "Siliq/Water Mobile (Quest) シェーダーが見つからない");
                mat = new Material(shader);

                Assert.IsTrue(mat.HasProperty("_MacroVariation"));
                Assert.IsTrue(mat.HasProperty("_MacroScale"));
                Assert.IsTrue(mat.HasProperty("_MacroDirectionBreakup"));
                Assert.IsTrue(mat.HasProperty("_MacroColorVariation"));
                Assert.IsTrue(mat.HasProperty("_MinLighting"));
                Assert.IsTrue(mat.HasProperty("_DarkReflectionDamping"));
                Assert.IsTrue(mat.HasProperty("_DarkDetailDamping"));
            }
            finally
            {
                if (mat != null) Object.DestroyImmediate(mat);
            }
        }

        [Test]
        public void PoolPreset_UsesSubtleNormalRecipe()
        {
            var settings = WaterMapPresets.Create(8);
            Assert.LessOrEqual(settings.strength, 0.45f, "Pool は強い凹凸ではなく浅い光網として扱う");

            bool foundCaustics = false;
            foreach (var layer in settings.layers)
            {
                if (layer.type != WaveLayerType.VoronoiCaustics) continue;
                foundCaustics = true;
                Assert.LessOrEqual(layer.amplitude, 0.18f, "Pool の光網を法線で太く盛りすぎている");
                Assert.GreaterOrEqual(layer.scale, 18, "Pool の光網が粗すぎると太いリボン状に見える");
            }
            Assert.IsTrue(foundCaustics, "Pool には控えめな光網レイヤーが必要");
        }

        [Test]
        public void CyberPreset_UsesThinFlowRecipe()
        {
            var settings = WaterMapPresets.Create(9);
            Assert.LessOrEqual(settings.strength, 0.8f, "Cyber は太い床模様ではなく薄い SF 水面として扱う");

            bool foundDirectionalFlow = false;
            foreach (var layer in settings.layers)
            {
                Assert.AreNotEqual(WaveLayerType.VoronoiCaustics, layer.type,
                    "Cyber にセル境界の網目を戻すと床タイル状に見える");
                Assert.AreNotEqual(WaveLayerType.VoronoiCells, layer.type,
                    "Cyber に太い規則ドットやブロック状セルを戻してはならない");

                if (layer.type != WaveLayerType.DirectionalWaves) continue;
                foundDirectionalFlow = true;
                Assert.Greater(layer.waveCount, 1, "Cyber は単独グリッド波ではなく複数の細い流れで作る");
                Assert.Greater(layer.spreadDeg, 0f, "Cyber の方向が完全固定だと床格子に見える");
                Assert.LessOrEqual(layer.sharpness, 1.5f, "Cyber の線を太く尖らせすぎている");
            }

            Assert.IsTrue(foundDirectionalFlow, "Cyber には細いデータ流の方向波レイヤーが必要");
        }

        // ---------------------------------------------------------------
        // ヘルパー
        // ---------------------------------------------------------------

        static float SumLayers(WaterMapSettings s, float u, float v, float t)
        {
            float sum = 0f;
            foreach (var layer in s.layers)
            {
                if (!layer.enabled) continue;
                sum += WaterMapCore.EvaluateLayer(layer, u, v, t, s.globalSeed) * layer.amplitude;
            }
            return sum;
        }
    }
}
