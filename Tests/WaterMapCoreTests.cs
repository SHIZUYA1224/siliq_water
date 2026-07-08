using System.IO;
using System.Reflection;
using NUnit.Framework;
using Siliq.Water.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

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
        public void WaterSurfaceAnimator_DefaultsUseSlowProductMotion()
        {
            GameObject go = null;
            try
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                var animator = go.AddComponent<WaterSurfaceAnimator>();

                Assert.LessOrEqual(animator.speed, 0.0008f,
                    "初期 speed は見た瞬間に流れすぎない低速値にする");
                Assert.LessOrEqual(animator.displacementSpeed, 0.0005f,
                    "初期 height animation も速すぎない値にする");
                Assert.LessOrEqual(animator.causticsSpeed, 0.00008f,
                    "初期 caustics animation もプールで流れすぎない値にする");
            }
            finally
            {
                if (go != null) Object.DestroyImmediate(go);
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
                animator.speed = 0.3f;
                animator.strength = 2f;
                animator.tiling = 2f;
                animator.opacity = 0.62f;
                animator.shallowColor = new Color(0.12f, 0.34f, 0.56f, 1f);
                animator.ApplyImmediate(0.5f);

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block, 0);
                Vector4 st = block.GetVector("_BumpMap_ST");
                Vector4 mainSt = block.GetVector("_MainTex_ST");

                Assert.AreEqual(2f, st.x, 1e-5f, "tiling が _BumpMap_ST.x に反映されていない");
                Assert.AreEqual(2f, st.y, 1e-5f, "tiling が _BumpMap_ST.y に反映されていない");
                Assert.Greater(st.z, 0.00002f, "speed / direction による X offset が反映されていない");
                Assert.LessOrEqual(st.z, 0.00004f, "Speed 0.3 でも水面として速すぎない内部減速が必要");
                Assert.AreEqual(2f, mainSt.x, 1e-5f, "Standard の normal UV 用 _MainTex_ST.x に tiling が反映されていない");
                Assert.AreEqual(2f, mainSt.y, 1e-5f, "Standard の normal UV 用 _MainTex_ST.y に tiling が反映されていない");
                Assert.Greater(mainSt.z, 0.00002f, "Standard の normal UV 用 _MainTex_ST に offset が反映されていない");
                Assert.LessOrEqual(mainSt.z, 0.00004f, "Standard の normal UV も Speed 0.3 で速すぎてはいけない");
                Assert.AreEqual(2f, block.GetFloat("_BumpScale"), 1e-5f, "strength が _BumpScale に反映されていない");
                Color c = block.GetColor("_Color");
                Assert.AreEqual(0.12f, c.r, 1e-5f, "shallowColor が Standard の _Color.r に反映されていない");
                Assert.AreEqual(0.34f, c.g, 1e-5f, "shallowColor が Standard の _Color.g に反映されていない");
                Assert.AreEqual(0.56f, c.b, 1e-5f, "shallowColor が Standard の _Color.b に反映されていない");
                Assert.AreEqual(0.62f, c.a, 1e-5f, "opacity が Standard の _Color alpha に反映されていない");
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
                animator.shallowColor = new Color(0.2f, 0.8f, 1f, 1f);
                animator.deepColor = new Color(0.01f, 0.1f, 0.24f, 1f);
                animator.reflectionColor = new Color(0.9f, 1f, 1f, 1f);
                animator.transmissionColor = new Color(0.4f, 0.95f, 1f, 1f);
                animator.sparkleColor = new Color(1f, 0.95f, 0.85f, 1f);
                animator.clarity = 0.67f;
                animator.edgeReflection = 0.73f;
                animator.refractionStrength = 0.37f;
                animator.reflectionStrength = 0.64f;
                animator.reflectionPatternStrength = 0.29f;
                animator.reflectionPatternScale = 1.23f;
                animator.transmissionStrength = 0.58f;
                animator.sparkle = 0.31f;
                animator.highlightStrength = 1.42f;
                animator.displacementStrength = 0.07f;
                animator.displacementScale = 0.66f;
                animator.displacementSpeed = 0.44f;
                animator.heightMapInfluence = 0.25f;
                animator.causticsStrength = 0.42f;
                animator.causticsScale = 2.3f;
                animator.causticsSpeed = 0.05f;
                animator.causticsFocus = 2.1f;
                animator.causticsPrismStrength = 0.17f;
                animator.causticsScatterStrength = 0.63f;
                animator.bottomLightStrength = 1.33f;
                animator.causticsTint = new Color(0.8f, 1f, 0.95f, 1f);
                animator.ApplyImmediate(0f);

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block, 0);

                Assert.AreEqual(0.48f, block.GetFloat("_Opacity"), 1e-5f);
                AssertColor(animator.shallowColor, block.GetColor("_ShallowColor"), "_ShallowColor");
                AssertColor(animator.deepColor, block.GetColor("_DeepColor"), "_DeepColor");
                AssertColor(animator.reflectionColor, block.GetColor("_HorizonColor"), "_HorizonColor");
                AssertColor(animator.transmissionColor, block.GetColor("_TransmissionColor"), "_TransmissionColor");
                AssertColor(animator.sparkleColor, block.GetColor("_GlimmerColor"), "_GlimmerColor");
                Assert.AreEqual(0.67f, block.GetFloat("_Clarity"), 1e-5f);
                Assert.AreEqual(0.73f, block.GetFloat("_EdgeReflection"), 1e-5f);
                Assert.AreEqual(0.37f, block.GetFloat("_RefractionStrength"), 1e-5f);
                Assert.AreEqual(0.64f, block.GetFloat("_ReflStrength"), 1e-5f);
                Assert.AreEqual(0.29f, block.GetFloat("_ReflectionPatternStrength"), 1e-5f);
                Assert.AreEqual(1.23f, block.GetFloat("_ReflectionPatternScale"), 1e-5f);
                Assert.AreEqual(0.58f, block.GetFloat("_TransmissionStrength"), 1e-5f);
                Assert.AreEqual(0.31f, block.GetFloat("_GlimmerIntensity"), 1e-5f);
                Assert.AreEqual(0.62f, block.GetFloat("_GlintIntensity"), 1e-5f);
                Assert.AreEqual(1.42f, block.GetFloat("_SpecIntensity"), 1e-5f);
                Assert.AreEqual(0.07f, block.GetFloat("_DisplacementStrength"), 1e-5f);
                Assert.AreEqual(0.66f, block.GetFloat("_DisplacementScale"), 1e-5f);
                Assert.AreEqual(0.44f, block.GetFloat("_DisplacementSpeed"), 1e-5f);
                Assert.AreEqual(0.25f, block.GetFloat("_HeightMapInfluence"), 1e-5f);
                Assert.AreEqual(0.42f, block.GetFloat("_CausticsStrength"), 1e-5f);
                Assert.AreEqual(2.3f, block.GetFloat("_CausticsScale"), 1e-5f);
                Assert.AreEqual(0.05f, block.GetFloat("_CausticsSpeed"), 1e-5f);
                Assert.AreEqual(2.1f, block.GetFloat("_CausticsFocus"), 1e-5f);
                Assert.AreEqual(0.17f, block.GetFloat("_CausticsPrismStrength"), 1e-5f);
                Assert.AreEqual(0.63f, block.GetFloat("_CausticsScatterStrength"), 1e-5f);
                Assert.AreEqual(1.33f, block.GetFloat("_BottomLightStrength"), 1e-5f);
                AssertColor(animator.causticsTint, block.GetColor("_CausticsTint"), "_CausticsTint");
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
        public void WaterRippleShader_UsesSmoothHighPrecisionRippleAnimation()
        {
            string[] shaderPaths =
            {
                "Packages/com.siliq.water-normalmap/Runtime/Shaders/SiliqWaterMobile.shader",
                "Packages/com.siliq.water-normalmap/Samples~/URP/SiliqWaterURP.shader",
            };

            foreach (string path in shaderPaths)
            {
                Assert.IsTrue(File.Exists(path), $"{path} が見つからない");
                string text = File.ReadAllText(path);

                StringAssert.Contains("#define SILIQ_MAX_RIPPLES 16", text,
                    $"{path}: 波紋スロットが少ないと寿命中の波紋を上書きしてピクつく");
                StringAssert.Contains("smoothstep(0.0, fadeIn, age)", text,
                    $"{path}: 波紋の出現は急な線形フェードではなく smoothstep にする");
                Assert.IsFalse(text.Contains("age / 0.08"),
                    $"{path}: 0.08 秒フェードは短すぎて波紋が点滅して見える");
                Assert.IsFalse(text.Contains("half SiliqMacroNoise"),
                    $"{path}: 時間変化する macro noise は half だとモバイルで量子化しやすい");
                Assert.IsFalse(text.Contains("half3 SiliqComputeRipple"),
                    $"{path}: 波紋計算は half だとモバイルでピクつきやすい");
            }
        }

        [Test]
        public void WaterShaders_SupportSoftCausticsScatterAndRefraction()
        {
            string[] shaderPaths =
            {
                "Packages/com.siliq.water-normalmap/Runtime/Shaders/SiliqWaterMobile.shader",
                "Packages/com.siliq.water-normalmap/Samples~/URP/SiliqWaterURP.shader",
            };

            foreach (string path in shaderPaths)
            {
                Assert.IsTrue(File.Exists(path), $"{path} が見つからない");
                string text = File.ReadAllText(path);

                StringAssert.Contains("_CausticsScatterStrength", text,
                    $"{path}: 水底光の柔らかい広がりを調整できる必要がある");
                StringAssert.Contains("causticsScatter", text,
                    $"{path}: 細い焦点線だけでなく柔らかい水底光を合成する必要がある");
                StringAssert.Contains("_CausticsPrismStrength", text,
                    $"{path}: 水底光の薄い色分散を維持する必要がある");
                StringAssert.Contains("_RefractionStrength", text,
                    $"{path}: 透明水越しの軽量な揺らぎを調整できる必要がある");
                StringAssert.Contains("refractOffset", text,
                    $"{path}: 水底光や透明色を法線で歪ませる疑似屈折が必要");
            }
        }

        [Test]
        public void WaterRippleDefaults_AreSubtleEnoughToAvoidPopping()
        {
            GameObject go = null;
            try
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                var emitter = go.AddComponent<WaterRippleEmitter>();
                Assert.LessOrEqual(emitter.ripplesPerSecond, 1.0f,
                    "自動波紋は発生頻度が高すぎると雨粒の点滅に見える");
                Assert.LessOrEqual(emitter.rippleAmplitude, 0.55f,
                    "波紋 amplitude の初期値が強いとピクピクしたノイズに見える");
                Assert.GreaterOrEqual(emitter.rippleWidth, 0.40f,
                    "リング幅が細すぎると aliasing と点滅が出やすい");
                Assert.GreaterOrEqual(emitter.rippleLifetime, 3.5f,
                    "波紋寿命が短いと出現/消滅が目立つ");
            }
            finally
            {
                if (go != null) Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ReadyMaterials_AreBundledForDragAndDropUse()
        {
            string[] transparentReadyMaterials =
            {
                "M_Siliq_ClearSea_Ready",
                "M_Siliq_ClearPool_Ready",
                "M_Siliq_IndoorBluePool_Ready",
                "M_Siliq_FlagshipCrystal_Ready",
                "M_Siliq_CrystalLagoon_Ready",
                "M_Siliq_BloodSea_Ready",
            };

            foreach (string name in transparentReadyMaterials)
            {
                var mat = LoadReadyMaterial(name);
                Assert.AreEqual("Siliq/Water Mobile (Quest)", mat.shader.name,
                    $"{name} は最初から使える Siliq 水マテリアルである必要がある");
                Assert.IsNotNull(mat.GetTexture("_NormalMap"),
                    $"{name} はドラッグ&ドロップだけで凹凸が出るよう normal map を持つ必要がある");
                Assert.IsNotNull(mat.GetTexture("_CausticsMap"),
                    $"{name} は水底の光表現用 caustics map を持つ必要がある");
                Assert.IsTrue(mat.HasProperty("_CausticsFocus"),
                    $"{name} は水底光の焦点調整を持つ必要がある");
                Assert.IsTrue(mat.HasProperty("_CausticsPrismStrength"),
                    $"{name} は水底光の色分散調整を持つ必要がある");
                Assert.IsTrue(mat.HasProperty("_CausticsScatterStrength"),
                    $"{name} は透明水越しの柔らかい水底光調整を持つ必要がある");
                Assert.IsTrue(mat.HasProperty("_RefractionStrength"),
                    $"{name} は透明水越しの揺らぎ調整を持つ必要がある");
                Assert.Greater(mat.GetFloat("_CausticsStrength"), 0.04f,
                    $"{name} は水底の光が完全に死んだ初期値ではいけない");
                Assert.GreaterOrEqual(mat.GetFloat("_CausticsFocus"), 1.1f,
                    $"{name} は水底光をぼやけた単色模様に戻さない");
                Assert.GreaterOrEqual(mat.GetFloat("_CausticsScatterStrength"), 0.04f,
                    $"{name} は細い線だけでなく柔らかい水底光の広がりを持つ必要がある");
                Assert.GreaterOrEqual(mat.GetFloat("_RefractionStrength"), 0.08f,
                    $"{name} は水底や反射が平板に見えない程度の軽量屈折を持つ必要がある");
                Assert.Greater(mat.GetVector("_Scroll1").sqrMagnitude, 0.0000000001f,
                    $"{name} は WaterSurfaceAnimator なしでも shader 側で波が動く scroll を持つ必要がある");
                Assert.LessOrEqual(mat.GetVector("_Scroll1").magnitude, 0.00022f,
                    $"{name} の shader scroll が速すぎる");
                Assert.LessOrEqual(mat.GetVector("_Scroll2").magnitude, 0.00016f,
                    $"{name} の shader scroll 2 が速すぎる");
                Assert.LessOrEqual(mat.GetFloat("_DisplacementSpeed"), 0.00045f,
                    $"{name} の高さアニメーションが速すぎる");
                Assert.LessOrEqual(mat.GetFloat("_CausticsSpeed"), 0.00010f,
                    $"{name} の水底光アニメーションが速すぎる");
                Assert.AreEqual((float)BlendMode.SrcAlpha, mat.GetFloat("_SrcBlend"), 1e-5f,
                    $"{name} は透明水としてすぐ使える blend 設定が必要");
                Assert.AreEqual((float)BlendMode.OneMinusSrcAlpha, mat.GetFloat("_DstBlend"), 1e-5f);
                Assert.AreEqual(0f, mat.GetFloat("_ZWrite"), 1e-5f);
                Assert.AreEqual((int)RenderQueue.Transparent, mat.renderQueue);
                Assert.LessOrEqual(mat.GetFloat("_HeightMapInfluence"), 0.05f,
                    $"{name} は初期状態で height map を強く使って板模様にしない");
            }

            var metal = LoadReadyMaterial("M_Siliq_LiquidMetal_Ready");
            Assert.AreEqual("Siliq/Water Mobile (Quest)", metal.shader.name);
            Assert.IsNotNull(metal.GetTexture("_NormalMap"));
            Assert.Greater(metal.GetVector("_Scroll1").sqrMagnitude, 0.0000000001f);
            Assert.LessOrEqual(metal.GetVector("_Scroll1").magnitude, 0.00022f);
            Assert.LessOrEqual(metal.GetVector("_Scroll2").magnitude, 0.00016f);
            Assert.LessOrEqual(metal.GetFloat("_DisplacementSpeed"), 0.00045f);
            Assert.AreEqual((float)BlendMode.One, metal.GetFloat("_SrcBlend"), 1e-5f);
            Assert.AreEqual((float)BlendMode.Zero, metal.GetFloat("_DstBlend"), 1e-5f);
            Assert.AreEqual(1f, metal.GetFloat("_ZWrite"), 1e-5f);
            Assert.AreEqual((int)RenderQueue.Geometry, metal.renderQueue);

            var flagship = LoadReadyMaterial("M_Siliq_FlagshipCrystal_Ready");
            Assert.IsNotNull(flagship.GetTexture("_HeightMap"),
                "Flagship ready material は専用 height map を持つが、初期 influence は 0 にする");
            Assert.LessOrEqual(flagship.GetFloat("_DisplacementStrength"), 0.02f);
            Assert.GreaterOrEqual(flagship.GetFloat("_Clarity"), 0.80f,
                "美しさ特化の Flagship ready material は透明な抜け感を高めに持つ必要がある");
            Assert.GreaterOrEqual(flagship.GetFloat("_ReflectionPatternStrength"), 0.30f,
                "美しさ特化の Flagship ready material は空や窓が映る反射パターンを持つ必要がある");
            Assert.GreaterOrEqual(flagship.GetFloat("_CausticsStrength"), 0.60f,
                "美しさ特化の Flagship ready material は水底光をはっきり持つ必要がある");
            Assert.GreaterOrEqual(flagship.GetFloat("_BottomLightStrength"), 1.40f,
                "美しさ特化の Flagship ready material は水底光が水越しに見える必要がある");
            Assert.GreaterOrEqual(flagship.GetFloat("_CausticsScatterStrength"), 0.45f,
                "美しさ特化の Flagship ready material は水底光の柔らかい広がりを持つ必要がある");
            Assert.GreaterOrEqual(flagship.GetFloat("_RefractionStrength"), 0.30f,
                "美しさ特化の Flagship ready material は水越しの揺らぎを持つ必要がある");

            var lagoon = LoadReadyMaterial("M_Siliq_CrystalLagoon_Ready");
            Assert.LessOrEqual(lagoon.GetFloat("_NormalStrength"), 0.45f,
                "Crystal Lagoon は透明感優先なので凹凸を強くしすぎない");
            Assert.LessOrEqual(lagoon.GetFloat("_DisplacementStrength"), 0.01f,
                "Crystal Lagoon は板模様を避けるため実高さを控えめにする");
            Assert.GreaterOrEqual(lagoon.GetFloat("_TransmissionStrength"), 0.90f,
                "Crystal Lagoon は透き通った見た目を最優先にする");
            Assert.GreaterOrEqual(lagoon.GetFloat("_Clarity"), 0.90f,
                "Crystal Lagoon は濁りを抑えた透明な抜け感を最優先にする");
            Assert.GreaterOrEqual(lagoon.GetFloat("_ReflectionPatternStrength"), 0.42f,
                "Crystal Lagoon は透明水でも反射が薄すぎない初期値にする");
            Assert.GreaterOrEqual(lagoon.GetFloat("_CausticsStrength"), 0.70f,
                "Crystal Lagoon は水底光を強めに持つ必要がある");
            Assert.GreaterOrEqual(lagoon.GetFloat("_CausticsFocus"), 2.0f,
                "Crystal Lagoon は水底光を細い焦点線として見せる");
            Assert.GreaterOrEqual(lagoon.GetFloat("_CausticsPrismStrength"), 0.18f,
                "Crystal Lagoon は水底光に薄い色分散を持たせる");
            Assert.GreaterOrEqual(lagoon.GetFloat("_CausticsScatterStrength"), 0.55f,
                "Crystal Lagoon は水底光を細い線だけでなく柔らかく広げる");
            Assert.GreaterOrEqual(lagoon.GetFloat("_BottomLightStrength"), 1.65f,
                "Crystal Lagoon は水底光が水越しに強く見える必要がある");
            Assert.GreaterOrEqual(lagoon.GetFloat("_RefractionStrength"), 0.30f,
                "Crystal Lagoon は透明水越しの揺らぎで床と水底光をなじませる必要がある");

            var hero = LoadReadyMaterial("M_Siliq_CrystalLagoon_Hero_Ready");
            Assert.AreEqual("Siliq/Water Mobile (Quest)", hero.shader.name,
                "Hero ready material はドラッグ&ドロップで使える Siliq Mobile 水シェーダーにする");
            Assert.AreEqual("Packages/com.siliq.water-normalmap/PrebakedPack/Textures/Water_Normal_CrystalLagoon_Hero_01.png", AssetDatabase.GetAssetPath(hero.GetTexture("_NormalMap")),
                "Hero ready material は Hero 専用 normal map を使う");
            Assert.AreEqual("Packages/com.siliq.water-normalmap/PrebakedPack/Textures/Water_Height_CrystalLagoon_Hero_01.png", AssetDatabase.GetAssetPath(hero.GetTexture("_HeightMap")),
                "Hero ready material は Hero 専用 height map を使う");
            Assert.AreNotEqual(AssetDatabase.GetAssetPath(lagoon.GetTexture("_NormalMap")), AssetDatabase.GetAssetPath(hero.GetTexture("_NormalMap")),
                "Hero ready material は通常 Lagoon normal の流用に戻してはいけない");
            Assert.AreNotEqual(AssetDatabase.GetAssetPath(lagoon.GetTexture("_HeightMap")), AssetDatabase.GetAssetPath(hero.GetTexture("_HeightMap")),
                "Hero ready material は通常 Lagoon height の流用に戻してはいけない");
            Assert.AreEqual("Packages/com.siliq.water-normalmap/PrebakedPack/Textures/Water_Caustics_CrystalLagoon_Hero_01.png", AssetDatabase.GetAssetPath(hero.GetTexture("_CausticsMap")),
                "Hero ready material は Hero 専用 caustics map を使う");
            Assert.AreNotEqual(AssetDatabase.GetAssetPath(lagoon.GetTexture("_CausticsMap")), AssetDatabase.GetAssetPath(hero.GetTexture("_CausticsMap")),
                "Hero ready material は通常 Lagoon より強い水底光 texture を別に持つ");
            Assert.LessOrEqual(hero.GetFloat("_Opacity"), 0.36f,
                "Hero は透明な抜け感を最優先にするため正面 opacity を低めにする");
            Assert.GreaterOrEqual(hero.GetFloat("_Clarity"), 0.97f,
                "Hero は通常 Lagoon より高い透明な抜け感を持つ必要がある");
            Assert.GreaterOrEqual(hero.GetFloat("_TransmissionStrength"), 0.97f,
                "Hero は透過光を強めて水の厚みを出す");
            Assert.GreaterOrEqual(hero.GetFloat("_RefractionStrength"), 0.40f,
                "Hero は水越しに床と水底光が揺らいで見える屈折感を持つ必要がある");
            Assert.GreaterOrEqual(hero.GetFloat("_ReflectionPatternStrength"), 0.55f,
                "Hero は空や窓の反射帯が見える強さにする");
            Assert.GreaterOrEqual(hero.GetFloat("_CausticsStrength"), 0.80f,
                "Hero は水底光をはっきり見せる");
            Assert.GreaterOrEqual(hero.GetFloat("_CausticsFocus"), 2.3f,
                "Hero は水底光を高品質な焦点線として締める");
            Assert.GreaterOrEqual(hero.GetFloat("_CausticsPrismStrength"), 0.22f,
                "Hero は水底光に薄いプリズム色を持たせる");
            Assert.GreaterOrEqual(hero.GetFloat("_CausticsScatterStrength"), 0.70f,
                "Hero は透明水越しに柔らかい水底光の膜を持つ必要がある");
            Assert.GreaterOrEqual(hero.GetFloat("_BottomLightStrength"), 1.85f,
                "Hero は水底光が水越しに強く透ける必要がある");
            Assert.LessOrEqual(hero.GetFloat("_NormalStrength"), 0.40f,
                "Hero は凹凸を抑え、変な模様ではなく透明感を優先する");
            Assert.LessOrEqual(hero.GetFloat("_DisplacementStrength"), 0.006f,
                "Hero は高さで板状の模様を出さない");
            Assert.LessOrEqual(hero.GetVector("_Scroll1").magnitude, 0.00012f,
                "Hero の shader scroll が速すぎてはいけない");
            Assert.LessOrEqual(hero.GetFloat("_DisplacementSpeed"), 0.00035f,
                "Hero の高さアニメーションが速すぎてはいけない");
            Assert.LessOrEqual(hero.GetFloat("_CausticsSpeed"), 0.00008f,
                "Hero の水底光アニメーションが速すぎてはいけない");
            Assert.AreEqual((float)BlendMode.SrcAlpha, hero.GetFloat("_SrcBlend"), 1e-5f,
                "Hero は透明水としてすぐ使える blend 設定にする");
            Assert.AreEqual((float)BlendMode.OneMinusSrcAlpha, hero.GetFloat("_DstBlend"), 1e-5f);
            Assert.AreEqual(0f, hero.GetFloat("_ZWrite"), 1e-5f);
            Assert.AreEqual((int)RenderQueue.Transparent, hero.renderQueue);
        }

        static Material LoadReadyMaterial(string name)
        {
            string path = $"Packages/com.siliq.water-normalmap/PrebakedPack/ReadyMaterials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Assert.IsNotNull(mat, $"{path} が同梱されていない");
            return mat;
        }

        [Test]
        public void CrystalLagoonPreview_IsBundledForBeautyFirstSelection()
        {
            const string path = "Packages/com.siliq.water-normalmap/PrebakedPack/Preview/preview_crystal_lagoon.png";
            var preview = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.IsNotNull(preview, "Crystal Lagoon の美しさ確認用 preview が同梱されていない");
            Assert.GreaterOrEqual(preview.width, 1024, "Crystal Lagoon preview は水底光が見える解像度が必要");
            Assert.GreaterOrEqual(preview.height, 512, "Crystal Lagoon preview は水面の透明感が見える解像度が必要");

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(importer, "Crystal Lagoon preview の import 設定が読めない");
            Assert.IsTrue(importer.sRGBTexture, "Preview 画像は見た目確認用なので sRGB で読み込む");
        }

        [Test]
        public void CrystalLagoonCompletePreview_ShowsPrefabBeautyDirection()
        {
            const string path = "Packages/com.siliq.water-normalmap/PrebakedPack/Preview/preview_crystal_lagoon_complete.png";
            var preview = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.IsNotNull(preview, "Crystal Lagoon 完成 Prefab の方向性が分かる preview が同梱されていない");
            Assert.GreaterOrEqual(preview.width, 1024, "完成 preview は Unity 上でも水面、床、反射、水底光が読める横長解像度が必要");
            Assert.GreaterOrEqual(preview.height, 512, "完成 preview は Unity 上でも完成形を確認できる高さが必要");

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(importer, "完成 preview の import 設定が読めない");
            Assert.IsTrue(importer.sRGBTexture, "完成 preview は見た目確認用なので sRGB で読み込む");
            Assert.GreaterOrEqual(importer.maxTextureSize, 2048, "完成 preview は 1280px 以上で潰さず表示できる import 上限が必要");

            var readable = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                Assert.IsTrue(readable.LoadImage(File.ReadAllBytes(path)), "完成 preview を PNG として読めない");
                Assert.GreaterOrEqual(readable.width, 1280, "完成 preview の元 PNG は十分な横解像度が必要");
                Assert.GreaterOrEqual(readable.height, 720, "完成 preview の元 PNG は十分な縦解像度が必要");
                var pixels = readable.GetPixels32();
                int blueWaterPixels = 0;
                int brightCausticPixels = 0;
                int darkerReflectionPixels = 0;
                float luminanceSum = 0f;
                byte minLum = byte.MaxValue;
                byte maxLum = 0;

                foreach (var p in pixels)
                {
                    byte lum = (byte)((p.r + p.g + p.b) / 3);
                    luminanceSum += lum;
                    if (lum < minLum) minLum = lum;
                    if (lum > maxLum) maxLum = lum;
                    if (p.b > p.r + 18 && p.g > p.r + 8) blueWaterPixels++;
                    if (lum > 225) brightCausticPixels++;
                    if (lum < 115) darkerReflectionPixels++;
                }

                float averageLuminance = luminanceSum / pixels.Length;
                Assert.Greater(blueWaterPixels, pixels.Length * 0.65f,
                    "完成 preview は透明な青系の水面として読める色比率が必要");
                Assert.Greater(brightCausticPixels, pixels.Length * 0.03f,
                    "完成 preview は水底光や反射の明るい筋を含む必要がある");
                Assert.Greater(darkerReflectionPixels, pixels.Length * 0.00005f,
                    "完成 preview は反射の濃淡がなく単調な水色だけに戻ってはいけない");
                Assert.Greater(averageLuminance, 150f, "完成 preview は暗く沈みすぎてはいけない");
                Assert.Less(averageLuminance, 220f, "完成 preview は白飛びした単色に近づけない");
                Assert.Greater(maxLum - minLum, 120, "完成 preview は床、光、反射の明暗差が必要");
            }
            finally
            {
                Object.DestroyImmediate(readable);
            }
        }

        [Test]
        public void CrystalLagoonHeroCompletePreview_ShowsHeroPrefabBeautyDirection()
        {
            const string path = "Packages/com.siliq.water-normalmap/PrebakedPack/Preview/preview_crystal_lagoon_hero_complete.png";
            var preview = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.IsNotNull(preview, "Hero 完成 Prefab の方向性が分かる preview が同梱されていない");
            Assert.GreaterOrEqual(preview.width, 1024, "Hero preview は Unity 上でも完成形を確認できる横長表示サイズが必要");
            Assert.GreaterOrEqual(preview.height, 512, "Hero preview は Unity 上でも完成形を確認できる高さが必要");

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(importer, "Hero preview の import 設定が読めない");
            Assert.IsTrue(importer.sRGBTexture, "Hero preview は見た目確認用なので sRGB で読み込む");
            Assert.GreaterOrEqual(importer.maxTextureSize, 2048, "Hero preview は 1280px で潰さず表示できる import 上限が必要");

            var readable = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                Assert.IsTrue(readable.LoadImage(File.ReadAllBytes(path)), "Hero preview を PNG として読めない");
                Assert.AreEqual(1280, readable.width, "Hero preview の元 PNG は十分な横解像度が必要");
                Assert.AreEqual(720, readable.height, "Hero preview の元 PNG は十分な縦解像度が必要");
                var pixels = readable.GetPixels32();
                int blueWaterPixels = 0;
                int brightCausticPixels = 0;
                int whiteReflectionPixels = 0;
                int darkerBluePixels = 0;
                float luminanceSum = 0f;
                byte minLum = byte.MaxValue;
                byte maxLum = 0;

                foreach (var p in pixels)
                {
                    byte lum = (byte)((p.r + p.g + p.b) / 3);
                    luminanceSum += lum;
                    if (lum < minLum) minLum = lum;
                    if (lum > maxLum) maxLum = lum;
                    if (p.b > p.r + 35 && p.g > p.r + 25 && p.b > 150) blueWaterPixels++;
                    if (p.r > 185 && p.g > 215 && p.b > 205) brightCausticPixels++;
                    if (p.r > 220 && p.g > 235 && p.b > 235) whiteReflectionPixels++;
                    if (p.b > 100 && p.r < 100 && p.g < 190) darkerBluePixels++;
                }

                float averageLuminance = luminanceSum / pixels.Length;
                Assert.Greater(blueWaterPixels, pixels.Length * 0.55f,
                    "Hero preview は透明な青系の水面として読める色比率が必要");
                Assert.Greater(brightCausticPixels, pixels.Length * 0.20f,
                    "Hero preview は水底光と明るい反射を十分に含む必要がある");
                Assert.Greater(whiteReflectionPixels, pixels.Length * 0.02f,
                    "Hero preview は太い白帯ではなく、細い反射ハイライトが確認できる必要がある");
                Assert.Greater(darkerBluePixels, pixels.Length * 0.004f,
                    "Hero preview は濃淡がなく単調な水色だけに戻ってはいけない");
                Assert.Greater(averageLuminance, 180f, "Hero preview は暗く沈みすぎてはいけない");
                Assert.Less(averageLuminance, 240f, "Hero preview は白飛びした単色に近づけない");
                Assert.Greater(maxLum - minLum, 90, "Hero preview は床、光、反射の明暗差が必要");
            }
            finally
            {
                Object.DestroyImmediate(readable);
            }
        }

        [Test]
        public void CrystalCausticsTexture_UsesSoftNonPolygonalLight()
        {
            const string path = "Packages/com.siliq.water-normalmap/PrebakedPack/Textures/Water_Caustics_Crystal_01.png";
            var caustics = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.IsNotNull(caustics, "水底光 texture が同梱されていない");
            Assert.GreaterOrEqual(caustics.width, 1024, "水底光 texture は低解像度に戻さない");
            Assert.GreaterOrEqual(caustics.height, 1024, "水底光 texture は低解像度に戻さない");

            var readable = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                Assert.IsTrue(readable.LoadImage(File.ReadAllBytes(path)), "水底光 texture を PNG として読めない");
                var pixels = readable.GetPixels32();
                int nearBlack = 0;
                int nearWhite = 0;
                int min = 255;
                int max = 0;
                long sum = 0;
                foreach (var pixel in pixels)
                {
                    int v = pixel.r;
                    if (v <= 2) nearBlack++;
                    if (v >= 250) nearWhite++;
                    if (v < min) min = v;
                    if (v > max) max = v;
                    sum += v;
                }

                float total = pixels.Length;
                Assert.AreEqual(0, nearBlack, "水底光 texture に真っ黒な大面積セルを戻してはならない");
                Assert.AreEqual(0, nearWhite, "水底光 texture に飽和した白い Voronoi 線を戻してはならない");
                Assert.GreaterOrEqual(min, 8, "水底光は黒い多角形セルではなく、淡い光として扱う");
                Assert.LessOrEqual(max, 220, "水底光は白飛びした線ではなく、shader 側で強度調整できる余地を残す");
                Assert.Greater(sum / total, 24f, "水底光 texture が暗すぎる");
            }
            finally
            {
                Object.DestroyImmediate(readable);
            }
        }

        [Test]
        public void CrystalLagoonCausticsTexture_IsHighResolutionAndVisible()
        {
            const string path = "Packages/com.siliq.water-normalmap/PrebakedPack/Textures/Water_Caustics_CrystalLagoon_01.png";
            var caustics = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.IsNotNull(caustics, "Crystal Lagoon 専用の水底光 texture が同梱されていない");
            Assert.GreaterOrEqual(caustics.width, 2048, "Crystal Lagoon の水底光は美しさ優先で 2048px 以上にする");
            Assert.GreaterOrEqual(caustics.height, 2048, "Crystal Lagoon の水底光は美しさ優先で 2048px 以上にする");

            var readable = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                Assert.IsTrue(readable.LoadImage(File.ReadAllBytes(path)), "Crystal Lagoon 水底光 texture を PNG として読めない");
                var pixels = readable.GetPixels32();
                int min = 255;
                int max = 0;
                long sum = 0;
                foreach (var pixel in pixels)
                {
                    int v = pixel.r;
                    if (v < min) min = v;
                    if (v > max) max = v;
                    sum += v;
                }

                float average = sum / (float)pixels.Length;
                Assert.GreaterOrEqual(min, 8, "Crystal Lagoon 水底光に黒つぶれを戻してはならない");
                Assert.GreaterOrEqual(max, 110, "Crystal Lagoon 水底光は床に見えるだけの明るい筋が必要");
                Assert.LessOrEqual(max, 220, "Crystal Lagoon 水底光は白飛びした線にしない");
                Assert.GreaterOrEqual(average, 32f, "Crystal Lagoon 水底光が暗すぎる");
                Assert.LessOrEqual(average, 48f, "Crystal Lagoon 水底光が全面発光のように強すぎる");
            }
            finally
            {
                Object.DestroyImmediate(readable);
            }
        }

        [Test]
        public void CrystalLagoonHeroCausticsTexture_IsHighResolutionSoftAndVisible()
        {
            const string path = "Packages/com.siliq.water-normalmap/PrebakedPack/Textures/Water_Caustics_CrystalLagoon_Hero_01.png";
            var caustics = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.IsNotNull(caustics, "Crystal Lagoon Hero 専用の水底光 texture が同梱されていない");
            Assert.GreaterOrEqual(caustics.width, 2048, "Hero 水底光は美しさ優先で 2048px 以上にする");
            Assert.GreaterOrEqual(caustics.height, 2048, "Hero 水底光は美しさ優先で 2048px 以上にする");

            var readable = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                Assert.IsTrue(readable.LoadImage(File.ReadAllBytes(path)), "Hero 水底光 texture を PNG として読めない");
                var pixels = readable.GetPixels32();
                int min = 255;
                int max = 0;
                int brightPixels = 0;
                long sum = 0;
                foreach (var pixel in pixels)
                {
                    int v = pixel.r;
                    if (v < min) min = v;
                    if (v > max) max = v;
                    if (v >= 150) brightPixels++;
                    sum += v;
                }

                float average = sum / (float)pixels.Length;
                Assert.GreaterOrEqual(min, 8, "Hero 水底光に黒つぶれを戻してはならない");
                Assert.GreaterOrEqual(max, 180, "Hero 水底光には床に映える明るい焦点線が必要");
                Assert.LessOrEqual(max, 230, "Hero 水底光は texture 側で白飛びさせない");
                Assert.GreaterOrEqual(average, 40f, "Hero 水底光が暗すぎる");
                Assert.LessOrEqual(average, 80f, "Hero 水底光が全面発光のように強すぎる");
                Assert.Greater(brightPixels, pixels.Length * 0.01f,
                    "Hero 水底光には商品品質で見える明るい光筋が必要");
                Assert.Less(brightPixels, pixels.Length * 0.20f,
                    "Hero 水底光の明るい領域が広すぎると床が白い板になる");

                int[] orientationBins = new int[18];
                int orientationSamples = 0;
                const int stride = 8;
                for (int y = stride; y < readable.height - stride; y += stride)
                {
                    for (int x = stride; x < readable.width - stride; x += stride)
                    {
                        int index = y * readable.width + x;
                        int center = pixels[index].r;
                        if (center < 70) continue;

                        int gx = pixels[index + stride].r - pixels[index - stride].r;
                        int gy = pixels[index + stride * readable.width].r - pixels[index - stride * readable.width].r;
                        float magnitude = Mathf.Sqrt(gx * gx + gy * gy);
                        if (magnitude < 8f) continue;

                        float angle = Mathf.Atan2(gy, gx);
                        if (angle < 0f) angle += Mathf.PI;
                        int bin = Mathf.Clamp(Mathf.FloorToInt(angle / Mathf.PI * orientationBins.Length), 0, orientationBins.Length - 1);
                        orientationBins[bin]++;
                        orientationSamples++;
                    }
                }

                int dominantBin = 0;
                for (int i = 0; i < orientationBins.Length; i++)
                {
                    dominantBin = Mathf.Max(dominantBin, orientationBins[i]);
                }

                Assert.Greater(orientationSamples, 1000, "Hero 水底光は方向性を評価できるだけの曲線ディテールが必要");
                Assert.Less(dominantBin / (float)orientationSamples, 0.16f,
                    "Hero 水底光が一方向の長い線や格子に戻っている");
            }
            finally
            {
                Object.DestroyImmediate(readable);
            }
        }

        [Test]
        public void CrystalLagoonCausticsOverlay_IsBundledForPoolFloors()
        {
            const string shaderPath = "Packages/com.siliq.water-normalmap/Runtime/Shaders/SiliqCausticsOverlay.shader";
            const string materialPath = "Packages/com.siliq.water-normalmap/PrebakedPack/ReadyMaterials/M_Siliq_CrystalLagoon_CausticsOverlay.mat";
            const string heroMaterialPath = "Packages/com.siliq.water-normalmap/PrebakedPack/ReadyMaterials/M_Siliq_CrystalLagoon_Hero_CausticsOverlay.mat";
            const string causticsPath = "Packages/com.siliq.water-normalmap/PrebakedPack/Textures/Water_Caustics_CrystalLagoon_01.png";
            const string heroCausticsPath = "Packages/com.siliq.water-normalmap/PrebakedPack/Textures/Water_Caustics_CrystalLagoon_Hero_01.png";

            Assert.IsTrue(File.Exists(shaderPath), "床用 caustics overlay shader が同梱されていない");
            string shaderText = File.ReadAllText(shaderPath);
            StringAssert.Contains("Shader \"Siliq/Caustics Overlay Mobile\"", shaderText);
            StringAssert.Contains("Blend One One", shaderText,
                "床用 caustics overlay は床を暗くせず加算で光だけを重ねる");
            StringAssert.Contains("_SoftScatter", shaderText,
                "床用 caustics overlay は細い線だけでなく柔らかい水底光の広がりを持つ必要がある");
            StringAssert.Contains("_PrismStrength", shaderText,
                "床用 caustics overlay は単色の白線ではなく薄いプリズム色を持つ必要がある");
            StringAssert.Contains("_Focus", shaderText,
                "床用 caustics overlay は水底光の焦点を調整できる必要がある");
            Assert.IsFalse(shaderText.Contains("com.unity.render-pipelines.universal"),
                "床用 caustics overlay は URP package 未導入でも壊れない Built-in 互換にする");

            var shader = Shader.Find("Siliq/Caustics Overlay Mobile");
            Assert.IsNotNull(shader, "Siliq/Caustics Overlay Mobile shader が見つからない");

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            var heroMaterial = AssetDatabase.LoadAssetAtPath<Material>(heroMaterialPath);
            Assert.IsNotNull(material, "Crystal Lagoon 床用 caustics overlay material が同梱されていない");
            Assert.IsNotNull(heroMaterial, "Hero 床用 caustics overlay material が同梱されていない");
            Assert.AreEqual("Siliq/Caustics Overlay Mobile", material.shader.name);
            Assert.AreEqual("Siliq/Caustics Overlay Mobile", heroMaterial.shader.name);
            Assert.AreEqual(causticsPath, AssetDatabase.GetAssetPath(material.GetTexture("_CausticsMap")),
                "床用 caustics overlay が Crystal Lagoon 専用 caustics を参照していない");
            Assert.AreEqual(heroCausticsPath, AssetDatabase.GetAssetPath(heroMaterial.GetTexture("_CausticsMap")),
                "Hero 床用 caustics overlay が Hero 専用 caustics を参照していない");
            Assert.GreaterOrEqual(material.GetFloat("_Intensity"), 0.4f,
                "床用 caustics overlay が弱すぎると水底光として見えない");
            Assert.LessOrEqual(material.GetFloat("_Intensity"), 0.75f,
                "床用 caustics overlay が強すぎると床が発光しすぎる");
            Assert.GreaterOrEqual(heroMaterial.GetFloat("_Intensity"), 0.60f,
                "Hero 床用 caustics overlay は通常版より強い水底光を持つ必要がある");
            Assert.LessOrEqual(heroMaterial.GetFloat("_Intensity"), 0.80f,
                "Hero 床用 caustics overlay が強すぎると床が発光しすぎる");
            Assert.GreaterOrEqual(material.GetFloat("_Tiling"), 1.0f);
            Assert.LessOrEqual(material.GetFloat("_Tiling"), 2.0f);
            Assert.GreaterOrEqual(material.GetFloat("_Focus"), 1.5f,
                "床用 caustics overlay はぼやけた光だけでなく焦点線を持つ必要がある");
            Assert.GreaterOrEqual(material.GetFloat("_SoftScatter"), 0.30f,
                "床用 caustics overlay は柔らかい散光を持つ必要がある");
            Assert.GreaterOrEqual(material.GetFloat("_PrismStrength"), 0.08f,
                "床用 caustics overlay は薄い色分散を持つ必要がある");
            Assert.GreaterOrEqual(heroMaterial.GetFloat("_Focus"), 2.0f,
                "Hero 床用 caustics overlay は通常版より締まった焦点線を持つ必要がある");
            Assert.GreaterOrEqual(heroMaterial.GetFloat("_SoftScatter"), 0.45f,
                "Hero 床用 caustics overlay は透明水越しの柔らかい散光を強めに持つ必要がある");
            Assert.GreaterOrEqual(heroMaterial.GetFloat("_PrismStrength"), 0.14f,
                "Hero 床用 caustics overlay は薄いプリズム色を強めに持つ必要がある");
            Assert.AreEqual((int)RenderQueue.Transparent, material.renderQueue,
                "床用 caustics overlay は透明キューで床の上に重ねる");
            Assert.AreEqual((int)RenderQueue.Transparent, heroMaterial.renderQueue,
                "Hero 床用 caustics overlay は透明キューで床の上に重ねる");
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
                Assert.IsTrue(mat.HasProperty("_Clarity"));
                Assert.IsTrue(mat.HasProperty("_ReflectionPatternStrength"));
                Assert.IsTrue(mat.HasProperty("_ReflectionPatternScale"));
                Assert.IsTrue(mat.HasProperty("_HeightMap"));
                Assert.IsTrue(mat.HasProperty("_DisplacementStrength"));
                Assert.IsTrue(mat.HasProperty("_DisplacementScale"));
                Assert.IsTrue(mat.HasProperty("_DisplacementSpeed"));
                Assert.IsTrue(mat.HasProperty("_HeightMapInfluence"));
                Assert.IsTrue(mat.HasProperty("_CausticsMap"));
                Assert.IsTrue(mat.HasProperty("_CausticsStrength"));
                Assert.IsTrue(mat.HasProperty("_CausticsScale"));
                Assert.IsTrue(mat.HasProperty("_CausticsSpeed"));
                Assert.IsTrue(mat.HasProperty("_BottomLightStrength"));
                Assert.IsTrue(mat.HasProperty("_CausticsTint"));
            }
            finally
            {
                if (mat != null) Object.DestroyImmediate(mat);
            }
        }

        [Test]
        public void ShaderUtility_UniversalPipelineWithoutUrpPackage_DoesNotChooseBuiltInShaders()
        {
            var originalPipeline = GraphicsSettings.renderPipelineAsset;
            var fakePipeline = ScriptableObject.CreateInstance<UniversalFakePipelineAsset>();
            try
            {
                GraphicsSettings.renderPipelineAsset = fakePipeline;

                Assert.IsTrue(WaterShaderUtility.IsUniversalPipelineActive());
                Assert.AreNotEqual(WaterShaderUtility.SiliqMobileIndex, WaterShaderUtility.BestSiliqMaterialShaderIndex());
                Assert.AreNotEqual(WaterShaderUtility.StandardIndex, WaterShaderUtility.BestFallbackMaterialShaderIndex());
            }
            finally
            {
                GraphicsSettings.renderPipelineAsset = originalPipeline;
                Object.DestroyImmediate(fakePipeline);
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

        [Test]
        public void IndoorBluePoolPreset_UsesBroadGentleReflectionRecipe()
        {
            var settings = WaterMapPresets.Create(10);
            Assert.LessOrEqual(settings.strength, 0.45f, "室内ブループールは凹凸ではなく広い反射の揺らぎを主役にする");
            Assert.LessOrEqual(settings.baseRoughness, 0.04f, "室内ブループールは窓反射が乗るようラフネスを低めにする");

            bool foundBroadWave = false;
            bool foundSubtleCaustics = false;
            foreach (var layer in settings.layers)
            {
                if (layer.type == WaveLayerType.DirectionalWaves &&
                    layer.scale <= 5 &&
                    layer.amplitude >= 0.45f &&
                    layer.sharpness <= 1.2f)
                {
                    foundBroadWave = true;
                }

                if (layer.type != WaveLayerType.VoronoiCaustics) continue;
                foundSubtleCaustics = true;
                Assert.LessOrEqual(layer.amplitude, 0.08f, "室内ブループールの光ムラを太いプール網目に戻してはならない");
                Assert.LessOrEqual(layer.sharpness, 2.2f, "室内ブループールの光ムラは柔らかくする");
            }

            Assert.IsTrue(foundBroadWave, "室内ブループールには窓反射を崩す広い方向波が必要");
            Assert.IsTrue(foundSubtleCaustics, "室内ブループールには淡い室内光のムラが必要");
        }

        [Test]
        public void FlagshipCrystalPreset_UsesLayeredPremiumWaterRecipe()
        {
            var settings = WaterMapPresets.Create(11);
            Assert.That(settings.layers.Length, Is.GreaterThanOrEqualTo(5), "フラッグシップ透明水は単調な一枚ノイズにしない");
            Assert.LessOrEqual(settings.strength, 0.65f, "フラッグシップ透明水は高品質でも法線を荒く盛りすぎない");
            Assert.LessOrEqual(settings.baseRoughness, 0.03f, "フラッグシップ透明水は鏡面反射が主役なのでラフネスを低く保つ");
            Assert.GreaterOrEqual(settings.causticsIntensity, 1.0f, "フラッグシップ透明水には淡いコースティクスの存在感が必要");

            bool foundBroadSmoothWave = false;
            bool foundFineRipples = false;
            bool foundFineLightBreakup = false;
            foreach (var layer in settings.layers)
            {
                Assert.AreNotEqual(WaveLayerType.VoronoiCaustics, layer.type,
                    "フラッグシップ normal / height にセル境界を入れると氷やタイル状に見える");
                Assert.AreNotEqual(WaveLayerType.VoronoiCells, layer.type,
                    "フラッグシップ normal / height にセル面を入れると水面ではなく板状に見える");
                Assert.LessOrEqual(layer.amplitude, 0.32f,
                    "フラッグシップ透明水では単一レイヤーを強くしすぎず、複数の弱い波で作る");

                if (layer.type == WaveLayerType.DirectionalWaves && layer.scale <= 8)
                {
                    Assert.LessOrEqual(layer.amplitude, 0.20f,
                        "低周波の強い DirectionalWaves は大きな多角形面や氷割れに見える");
                    Assert.LessOrEqual(layer.sharpness, 0.80f,
                        "低周波 DirectionalWaves を尖らせると水ではなく板模様になる");
                }

                if (layer.type == WaveLayerType.PerlinWaves &&
                    layer.scale >= 4 &&
                    layer.scale <= 6 &&
                    layer.amplitude >= 0.12f &&
                    layer.amplitude <= 0.22f &&
                    layer.sharpness <= 0.75f)
                {
                    foundBroadSmoothWave = true;
                }

                if ((layer.type == WaveLayerType.RidgedWaves || layer.type == WaveLayerType.DirectionalWaves) &&
                    layer.scale >= 30 &&
                    layer.amplitude <= 0.18f)
                {
                    foundFineRipples = true;
                }

                if (layer.type == WaveLayerType.DirectionalWaves &&
                    layer.scale >= 30 &&
                    layer.amplitude <= 0.12f &&
                    layer.waveCount >= 20)
                {
                    foundFineLightBreakup = true;
                }
            }

            Assert.IsTrue(foundBroadSmoothWave, "フラッグシップ透明水の広い透明ムラは控えめな Perlin 系で作る");
            Assert.IsTrue(foundFineRipples, "フラッグシップ透明水には反射を細かく割る細波が必要");
            Assert.IsTrue(foundFineLightBreakup, "フラッグシップ透明水にはセル境界ではない薄い光のゆらぎが必要");
        }

        [Test]
        public void FlagshipCrystalPrebakedAssets_AreDedicatedHighResolutionAssets()
        {
            const string normalGuid = "a171aabb01c34e01a1b2c3d4e5f60106";
            const string calmNormalGuid = "a171aabb01c34e01a1b2c3d4e5f60101";
            const string heightGuid = "a171aabb01c34e01a1b2c3d4e5f60306";
            const string materialGuid = "a171aabb01c34e01a1b2c3d4e5f60206";

            string normalPath = AssetDatabase.GUIDToAssetPath(normalGuid);
            string calmNormalPath = AssetDatabase.GUIDToAssetPath(calmNormalGuid);
            string heightPath = AssetDatabase.GUIDToAssetPath(heightGuid);
            string materialPath = AssetDatabase.GUIDToAssetPath(materialGuid);

            Assert.IsNotEmpty(normalPath, "フラッグシップ専用 normal map が package に含まれていない");
            Assert.IsNotEmpty(heightPath, "フラッグシップ専用 height map が package に含まれていない");
            Assert.IsNotEmpty(materialPath, "フラッグシップ専用 material が package に含まれていない");
            Assert.AreNotEqual(calmNormalPath, normalPath, "フラッグシップが Calm normal の流用に戻っている");

            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            var height = AssetDatabase.LoadAssetAtPath<Texture2D>(heightPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.IsNotNull(normal, "フラッグシップ normal map を読み込めない");
            Assert.IsNotNull(height, "フラッグシップ height map を読み込めない");
            Assert.IsNotNull(material, "フラッグシップ material を読み込めない");
            Assert.GreaterOrEqual(normal.width, 2048, "製品デモ向け normal map は 2048px 以上にする");
            Assert.GreaterOrEqual(normal.height, 2048, "製品デモ向け normal map は 2048px 以上にする");
            Assert.GreaterOrEqual(height.width, 2048, "実高さ用 height map は 2048px 以上にする");
            Assert.GreaterOrEqual(height.height, 2048, "実高さ用 height map は 2048px 以上にする");

            var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            Assert.IsNotNull(normalImporter, "normal map の importer が TextureImporter ではない");
            Assert.AreEqual(TextureImporterType.NormalMap, normalImporter.textureType, "normal map が NormalMap import になっていない");
            Assert.AreEqual(normalPath, AssetDatabase.GetAssetPath(material.GetTexture("_BumpMap")),
                "フラッグシップ material が専用 normal map を参照していない");
            Assert.IsNull(material.GetTexture("_ParallaxMap"),
                "ドラッグ&ドロップ用 Standard material では height map を Parallax に接続しない");
            Assert.IsFalse(material.IsKeywordEnabled("_PARALLAXMAP"),
                "ドラッグ&ドロップ用 Standard material では Parallax を切り、変な板模様を出さない");
            Assert.LessOrEqual(material.GetFloat("_Parallax"), 0.001f,
                "ドラッグ&ドロップ用 Standard material では height map を視差として強制使用しない");
            Assert.LessOrEqual(material.GetFloat("_BumpScale"), 0.70f,
                "ドラッグ&ドロップ用 Standard material でも normal を強く盛りすぎない");
        }

        [Test]
        public void FlagshipQuickApply_UsesDedicatedNormalAndHeightMaps()
        {
            const string normalGuid = "a171aabb01c34e01a1b2c3d4e5f60106";
            const string heightGuid = "a171aabb01c34e01a1b2c3d4e5f60306";
            string normalPath = AssetDatabase.GUIDToAssetPath(normalGuid);
            string heightPath = AssetDatabase.GUIDToAssetPath(heightGuid);

            GameObject go = null;
            try
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Selection.activeGameObject = go;

                bool executed = EditorApplication.ExecuteMenuItem(
                    "GameObject/Siliq Water/用途別マテリアルを適用/フラッグシップ透明水 (Flagship Crystal)");
                Assert.IsTrue(executed, "フラッグシップ透明水の Quick Apply menu を実行できない");

                var renderer = go.GetComponent<Renderer>();
                var mat = renderer != null ? renderer.sharedMaterial : null;
                var animator = go.GetComponent<WaterSurfaceAnimator>();
                Assert.IsNotNull(mat, "Quick Apply で material が設定されていない");
                Assert.IsNotNull(animator, "Quick Apply で WaterSurfaceAnimator が追加されていない");

                string normalProperty = mat.HasProperty("_NormalMap") ? "_NormalMap" : "_BumpMap";
                string heightProperty = mat.HasProperty("_HeightMap") ? "_HeightMap" : "_ParallaxMap";
                Assert.AreEqual(normalPath, AssetDatabase.GetAssetPath(mat.GetTexture(normalProperty)),
                    "Quick Apply が専用 flagship normal ではなく別 normal を割り当てている");
                Assert.AreEqual(heightPath, AssetDatabase.GetAssetPath(mat.GetTexture(heightProperty)),
                    "Quick Apply が専用 flagship height を割り当てていない");

                if (mat.HasProperty("_HeightMapInfluence"))
                {
                    Assert.LessOrEqual(mat.GetFloat("_HeightMapInfluence"), 0.05f,
                        "フラッグシップ透明水は初期状態で height map を強く使うと多角形模様に見える");
                }
                if (mat.HasProperty("_DisplacementStrength"))
                {
                    Assert.LessOrEqual(mat.GetFloat("_DisplacementStrength"), 0.02f,
                        "フラッグシップ透明水の初期高さは控えめにして、粗いメッシュで破綻させない");
                }
                if (mat.HasProperty("_NormalStrength"))
                {
                    Assert.LessOrEqual(mat.GetFloat("_NormalStrength"), 0.65f,
                        "フラッグシップ透明水の normal を盛りすぎると変な模様に見える");
                }
                Assert.LessOrEqual(animator.displacementStrength, 0.02f,
                    "Animator 側も低い高さで開始し、粗いメッシュで多角形化させない");
                Assert.LessOrEqual(animator.heightMapInfluence, 0.05f,
                    "Animator 側も height map を初期状態で強く使わない");
                Assert.LessOrEqual(animator.strength, 0.65f,
                    "Animator 側で normal strength を上書きして変な模様へ戻さない");
            }
            finally
            {
                Selection.activeGameObject = null;
                if (go != null) Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CrystalLagoonPrebakedAssets_AreDedicatedHighResolutionAssets()
        {
            const string normalGuid = "a171aabb01c34e01a1b2c3d4e5f60107";
            const string heightGuid = "a171aabb01c34e01a1b2c3d4e5f60307";
            const string causticsGuid = "a171aabb01c34e01a1b2c3d4e5f60407";
            const string heroNormalGuid = "a171aabb01c34e01a1b2c3d4e5f60108";
            const string heroHeightGuid = "a171aabb01c34e01a1b2c3d4e5f60308";
            const string heroCausticsGuid = "a171aabb01c34e01a1b2c3d4e5f60408";
            const string sharedCausticsGuid = "00e6b1e9a23c24df69fe9558209de596";
            const string flagshipNormalGuid = "a171aabb01c34e01a1b2c3d4e5f60106";
            const string flagshipHeightGuid = "a171aabb01c34e01a1b2c3d4e5f60306";

            string normalPath = AssetDatabase.GUIDToAssetPath(normalGuid);
            string heightPath = AssetDatabase.GUIDToAssetPath(heightGuid);
            string causticsPath = AssetDatabase.GUIDToAssetPath(causticsGuid);
            string heroNormalPath = AssetDatabase.GUIDToAssetPath(heroNormalGuid);
            string heroHeightPath = AssetDatabase.GUIDToAssetPath(heroHeightGuid);
            string heroCausticsPath = AssetDatabase.GUIDToAssetPath(heroCausticsGuid);
            string sharedCausticsPath = AssetDatabase.GUIDToAssetPath(sharedCausticsGuid);
            string flagshipNormalPath = AssetDatabase.GUIDToAssetPath(flagshipNormalGuid);
            string flagshipHeightPath = AssetDatabase.GUIDToAssetPath(flagshipHeightGuid);

            Assert.IsNotEmpty(normalPath, "Crystal Lagoon 専用 normal map が package に含まれていない");
            Assert.IsNotEmpty(heightPath, "Crystal Lagoon 専用 height map が package に含まれていない");
            Assert.IsNotEmpty(causticsPath, "Crystal Lagoon 専用 caustics map が package に含まれていない");
            Assert.IsNotEmpty(heroNormalPath, "Crystal Lagoon Hero 専用 normal map が package に含まれていない");
            Assert.IsNotEmpty(heroHeightPath, "Crystal Lagoon Hero 専用 height map が package に含まれていない");
            Assert.IsNotEmpty(heroCausticsPath, "Crystal Lagoon Hero 専用 caustics map が package に含まれていない");
            Assert.AreNotEqual(flagshipNormalPath, normalPath, "Crystal Lagoon normal が Flagship normal の流用に戻っている");
            Assert.AreNotEqual(flagshipHeightPath, heightPath, "Crystal Lagoon height が Flagship height の流用に戻っている");
            Assert.AreNotEqual(normalPath, heroNormalPath, "Hero normal が通常 Crystal Lagoon normal の流用に戻っている");
            Assert.AreNotEqual(heightPath, heroHeightPath, "Hero height が通常 Crystal Lagoon height の流用に戻っている");
            Assert.AreNotEqual(sharedCausticsPath, causticsPath, "Crystal Lagoon caustics が共通 caustics の流用に戻っている");
            Assert.AreNotEqual(causticsPath, heroCausticsPath, "Hero caustics が通常 Crystal Lagoon caustics の流用に戻っている");
            Assert.AreNotEqual(sharedCausticsPath, heroCausticsPath, "Hero caustics が共通 caustics の流用に戻っている");

            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            var height = AssetDatabase.LoadAssetAtPath<Texture2D>(heightPath);
            var caustics = AssetDatabase.LoadAssetAtPath<Texture2D>(causticsPath);
            var heroNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(heroNormalPath);
            var heroHeight = AssetDatabase.LoadAssetAtPath<Texture2D>(heroHeightPath);
            var heroCaustics = AssetDatabase.LoadAssetAtPath<Texture2D>(heroCausticsPath);
            Assert.IsNotNull(normal, "Crystal Lagoon normal map を読み込めない");
            Assert.IsNotNull(height, "Crystal Lagoon height map を読み込めない");
            Assert.IsNotNull(caustics, "Crystal Lagoon caustics map を読み込めない");
            Assert.IsNotNull(heroNormal, "Crystal Lagoon Hero normal map を読み込めない");
            Assert.IsNotNull(heroHeight, "Crystal Lagoon Hero height map を読み込めない");
            Assert.IsNotNull(heroCaustics, "Crystal Lagoon Hero caustics map を読み込めない");
            Assert.GreaterOrEqual(normal.width, 2048, "美しさ特化 normal map は 2048px 以上にする");
            Assert.GreaterOrEqual(normal.height, 2048, "美しさ特化 normal map は 2048px 以上にする");
            Assert.GreaterOrEqual(height.width, 2048, "美しさ特化 height map は 2048px 以上にする");
            Assert.GreaterOrEqual(height.height, 2048, "美しさ特化 height map は 2048px 以上にする");
            Assert.GreaterOrEqual(caustics.width, 2048, "美しさ特化 caustics map は 2048px 以上にする");
            Assert.GreaterOrEqual(caustics.height, 2048, "美しさ特化 caustics map は 2048px 以上にする");
            Assert.GreaterOrEqual(heroNormal.width, 2048, "Hero normal map は 2048px 以上にする");
            Assert.GreaterOrEqual(heroNormal.height, 2048, "Hero normal map は 2048px 以上にする");
            Assert.GreaterOrEqual(heroHeight.width, 2048, "Hero height map は 2048px 以上にする");
            Assert.GreaterOrEqual(heroHeight.height, 2048, "Hero height map は 2048px 以上にする");
            Assert.GreaterOrEqual(heroCaustics.width, 2048, "Hero caustics map は 2048px 以上にする");
            Assert.GreaterOrEqual(heroCaustics.height, 2048, "Hero caustics map は 2048px 以上にする");

            var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            Assert.IsNotNull(normalImporter, "Crystal Lagoon normal importer が TextureImporter ではない");
            Assert.AreEqual(TextureImporterType.NormalMap, normalImporter.textureType,
                "Crystal Lagoon normal map が NormalMap import になっていない");
            var heroNormalImporter = AssetImporter.GetAtPath(heroNormalPath) as TextureImporter;
            Assert.IsNotNull(heroNormalImporter, "Hero normal importer が TextureImporter ではない");
            Assert.AreEqual(TextureImporterType.NormalMap, heroNormalImporter.textureType,
                "Hero normal map が NormalMap import になっていない");
            Assert.IsFalse(heroNormalImporter.sRGBTexture, "Hero normal map は sRGB ではなく linear import にする");

            var ready = LoadReadyMaterial("M_Siliq_CrystalLagoon_Ready");
            Assert.AreEqual(normalPath, AssetDatabase.GetAssetPath(ready.GetTexture("_NormalMap")),
                "Crystal Lagoon ready material が専用 normal map を参照していない");
            Assert.AreEqual(heightPath, AssetDatabase.GetAssetPath(ready.GetTexture("_HeightMap")),
                "Crystal Lagoon ready material が専用 height map を参照していない");
            Assert.AreEqual(causticsPath, AssetDatabase.GetAssetPath(ready.GetTexture("_CausticsMap")),
                "Crystal Lagoon ready material が専用 caustics map を参照していない");

            var hero = LoadReadyMaterial("M_Siliq_CrystalLagoon_Hero_Ready");
            Assert.AreEqual(heroNormalPath, AssetDatabase.GetAssetPath(hero.GetTexture("_NormalMap")),
                "Crystal Lagoon Hero ready material が Hero 専用 normal map を参照していない");
            Assert.AreEqual(heroHeightPath, AssetDatabase.GetAssetPath(hero.GetTexture("_HeightMap")),
                "Crystal Lagoon Hero ready material が Hero 専用 height map を参照していない");
            Assert.AreEqual(heroCausticsPath, AssetDatabase.GetAssetPath(hero.GetTexture("_CausticsMap")),
                "Crystal Lagoon Hero ready material が Hero 専用 caustics map を参照していない");
        }

        [Test]
        public void CrystalLagoonQuickApply_UsesDedicatedBeautyTextures()
        {
            const string normalGuid = "a171aabb01c34e01a1b2c3d4e5f60107";
            const string heightGuid = "a171aabb01c34e01a1b2c3d4e5f60307";
            const string causticsGuid = "a171aabb01c34e01a1b2c3d4e5f60407";
            string normalPath = AssetDatabase.GUIDToAssetPath(normalGuid);
            string heightPath = AssetDatabase.GUIDToAssetPath(heightGuid);
            string causticsPath = AssetDatabase.GUIDToAssetPath(causticsGuid);

            GameObject go = null;
            try
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Selection.activeGameObject = go;

                bool executed = EditorApplication.ExecuteMenuItem(
                    "GameObject/Siliq Water/用途別マテリアルを適用/クリスタルラグーン (Crystal Lagoon)");
                Assert.IsTrue(executed, "Crystal Lagoon の Quick Apply menu を実行できない");

                var renderer = go.GetComponent<Renderer>();
                var mat = renderer != null ? renderer.sharedMaterial : null;
                Assert.IsNotNull(mat, "Crystal Lagoon Quick Apply で material が設定されていない");

                Assert.AreEqual(normalPath, AssetDatabase.GetAssetPath(mat.GetTexture("_NormalMap")),
                    "Crystal Lagoon Quick Apply が専用 normal map を割り当てていない");
                Assert.AreEqual(heightPath, AssetDatabase.GetAssetPath(mat.GetTexture("_HeightMap")),
                    "Crystal Lagoon Quick Apply が専用 height map を割り当てていない");
                Assert.AreEqual(causticsPath, AssetDatabase.GetAssetPath(mat.GetTexture("_CausticsMap")),
                    "Crystal Lagoon Quick Apply が専用 caustics map を割り当てていない");
                Assert.GreaterOrEqual(mat.GetFloat("_TransmissionStrength"), 0.90f,
                    "Crystal Lagoon Quick Apply は透き通った見た目を優先する");
                Assert.GreaterOrEqual(mat.GetFloat("_Clarity"), 0.90f,
                    "Crystal Lagoon Quick Apply は透明な抜け感を最優先する");
                Assert.GreaterOrEqual(mat.GetFloat("_ReflectionPatternStrength"), 0.42f,
                    "Crystal Lagoon Quick Apply は反射パターンを持つ必要がある");
                Assert.GreaterOrEqual(mat.GetFloat("_CausticsStrength"), 0.70f,
                    "Crystal Lagoon Quick Apply は水底光を強めに持つ必要がある");
                Assert.GreaterOrEqual(mat.GetFloat("_CausticsScatterStrength"), 0.55f,
                    "Crystal Lagoon Quick Apply は水底光の柔らかい広がりを持つ必要がある");
                Assert.GreaterOrEqual(mat.GetFloat("_BottomLightStrength"), 1.65f,
                    "Crystal Lagoon Quick Apply は水底光が水越しに見える必要がある");
            }
            finally
            {
                Selection.activeGameObject = null;
                if (go != null) Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CrystalLagoonHeroQuickApply_UsesDedicatedHeroBeautyTextures()
        {
            const string normalGuid = "a171aabb01c34e01a1b2c3d4e5f60108";
            const string heightGuid = "a171aabb01c34e01a1b2c3d4e5f60308";
            const string causticsGuid = "a171aabb01c34e01a1b2c3d4e5f60408";
            string normalPath = AssetDatabase.GUIDToAssetPath(normalGuid);
            string heightPath = AssetDatabase.GUIDToAssetPath(heightGuid);
            string causticsPath = AssetDatabase.GUIDToAssetPath(causticsGuid);

            GameObject go = null;
            try
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Selection.activeGameObject = go;

                bool executed = EditorApplication.ExecuteMenuItem(
                    "GameObject/Siliq Water/用途別マテリアルを適用/クリスタルラグーン Hero (Crystal Lagoon Hero)");
                Assert.IsTrue(executed, "Crystal Lagoon Hero の Quick Apply menu を実行できない");

                var renderer = go.GetComponent<Renderer>();
                var animator = go.GetComponent<WaterSurfaceAnimator>();
                var mat = renderer != null ? renderer.sharedMaterial : null;
                Assert.IsNotNull(mat, "Crystal Lagoon Hero Quick Apply で material が設定されていない");
                Assert.IsNotNull(animator, "Crystal Lagoon Hero Quick Apply で WaterSurfaceAnimator が追加されていない");

                Assert.AreEqual(normalPath, AssetDatabase.GetAssetPath(mat.GetTexture("_NormalMap")),
                    "Hero Quick Apply が専用 normal map を割り当てていない");
                Assert.AreEqual(heightPath, AssetDatabase.GetAssetPath(mat.GetTexture("_HeightMap")),
                    "Hero Quick Apply が専用 height map を割り当てていない");
                Assert.AreEqual(causticsPath, AssetDatabase.GetAssetPath(mat.GetTexture("_CausticsMap")),
                    "Hero Quick Apply が専用 caustics map を割り当てていない");
                Assert.LessOrEqual(mat.GetFloat("_Opacity"), 0.36f,
                    "Hero Quick Apply は透明な抜け感を最優先する");
                Assert.GreaterOrEqual(mat.GetFloat("_Clarity"), 0.97f,
                    "Hero Quick Apply は透明な抜け感を最大寄りにする");
                Assert.GreaterOrEqual(mat.GetFloat("_ReflectionPatternStrength"), 0.55f,
                    "Hero Quick Apply は反射帯を強めに持つ必要がある");
                Assert.GreaterOrEqual(mat.GetFloat("_CausticsStrength"), 0.80f,
                    "Hero Quick Apply は水底光を強めに持つ必要がある");
                Assert.GreaterOrEqual(mat.GetFloat("_CausticsFocus"), 2.3f,
                    "Hero Quick Apply は水底光を締めた焦点線にする");
                Assert.GreaterOrEqual(mat.GetFloat("_CausticsScatterStrength"), 0.70f,
                    "Hero Quick Apply は水底光の柔らかい広がりを強めに持つ必要がある");
                Assert.GreaterOrEqual(mat.GetFloat("_BottomLightStrength"), 1.85f,
                    "Hero Quick Apply は水底光が水越しに見える必要がある");
                Assert.LessOrEqual(animator.speed, 0.001f,
                    "Hero Quick Apply は静かな初期速度で始める");
            }
            finally
            {
                Selection.activeGameObject = null;
                if (go != null) Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void MaterialGuide_DocumentsCrystalLagoonDedicatedTextures()
        {
            const string guidePath = "Packages/com.siliq.water-normalmap/Docs/MaterialLookPresetGuide.md";
            Assert.IsTrue(File.Exists(guidePath), "用途別マテリアルガイドが同梱されていない");
            string guide = File.ReadAllText(guidePath);

            StringAssert.Contains("Water_Normal_CrystalLagoon_01.png", guide,
                "ガイドには Crystal Lagoon 専用 normal map を明記する");
            StringAssert.Contains("Water_Height_CrystalLagoon_01.png", guide,
                "ガイドには Crystal Lagoon 専用 height map を明記する");
            StringAssert.Contains("Water_Caustics_CrystalLagoon_01.png", guide,
                "ガイドには Crystal Lagoon 専用 caustics map を明記する");
            StringAssert.Contains("Water_Normal_CrystalLagoon_Hero_01.png", guide,
                "ガイドには Crystal Lagoon Hero 専用 normal map を明記する");
            StringAssert.Contains("Water_Height_CrystalLagoon_Hero_01.png", guide,
                "ガイドには Crystal Lagoon Hero 専用 height map を明記する");
            StringAssert.Contains("Water_Caustics_CrystalLagoon_Hero_01.png", guide,
                "ガイドには Crystal Lagoon Hero 専用 caustics map を明記する");
            StringAssert.Contains("M_Siliq_CrystalLagoon_Hero_Ready", guide,
                "最高品質確認用 Hero material の説明がない");
            StringAssert.Contains("クリスタルラグーンでは専用 2048px normal map、height map、`Water_Caustics_CrystalLagoon_01.png` を割り当てる", guide,
                "Crystal Lagoon が Flagship / 共通 caustics の流用ではないことをガイドで説明する");
            StringAssert.Contains("Hero では `Water_Normal_CrystalLagoon_Hero_01.png`、`Water_Height_CrystalLagoon_Hero_01.png`、`Water_Caustics_CrystalLagoon_Hero_01.png` を使う", guide,
                "Hero が通常 Lagoon texture の流用ではないことをガイドで説明する");
            StringAssert.Contains("Caustics Scatter", guide,
                "ガイドには Hero の柔らかい水底光パラメータを明記する");
            StringAssert.Contains("透明な海・プール・フラッグシップ水では `Water_Caustics_Crystal_01.png`", guide,
                "共通水底光と Crystal Lagoon 専用水底光の対象を分けて説明する");
        }

        [Test]
        public void BeginnerSetup_RepairsOldFlagshipHeightSettings()
        {
            GameObject go = null;
            Material mat = null;
            try
            {
                Shader shader = Shader.Find("Siliq/Water Mobile (Quest)");
                Assert.IsNotNull(shader, "Siliq/Water Mobile (Quest) シェーダーが見つからない");

                go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                mat = new Material(shader) { name = "M_Water_Look_FlagshipCrystal_Old" };
                if (mat.HasProperty("_HeightMapInfluence")) mat.SetFloat("_HeightMapInfluence", 0.35f);
                if (mat.HasProperty("_DisplacementStrength")) mat.SetFloat("_DisplacementStrength", 0.045f);
                go.GetComponent<Renderer>().sharedMaterial = mat;

                var report = new System.Collections.Generic.List<string>();
                bool changed = WaterBeginnerSetup.RepairWaterObject(go, report);
                var repaired = go.GetComponent<Renderer>().sharedMaterial;

                Assert.IsTrue(changed, "古い強い flagship height 設定を診断修復できていない");
                Assert.IsNotNull(repaired, "修復後 material がない");
                if (repaired.HasProperty("_HeightMapInfluence"))
                {
                    Assert.LessOrEqual(repaired.GetFloat("_HeightMapInfluence"), 0.05f);
                }
                if (repaired.HasProperty("_DisplacementStrength"))
                {
                    Assert.LessOrEqual(repaired.GetFloat("_DisplacementStrength"), 0.02f);
                }
                if (repaired.HasProperty("_TransmissionStrength"))
                {
                    Assert.GreaterOrEqual(repaired.GetFloat("_TransmissionStrength"), 0.90f,
                        "診断修復は美しさ優先の Crystal Lagoon へ寄せる");
                }
            }
            finally
            {
                if (go != null) Object.DestroyImmediate(go);
                if (mat != null) Object.DestroyImmediate(mat);
            }
        }

        [Test]
        public void BeginnerSetup_BuildsDenseWaterGridMesh()
        {
            Mesh mesh = null;
            try
            {
                mesh = WaterBeginnerSetup.BuildWaterGridMesh(16, 8f);
                Assert.AreEqual((16 + 1) * (16 + 1), mesh.vertexCount, "水面の実高さに必要な分割が作られていない");
                Assert.AreEqual(16 * 16 * 6, mesh.triangles.Length, "水面グリッドの三角形数が不正");
                Assert.AreEqual(mesh.vertexCount, mesh.uv.Length, "UV が全頂点に入っていない");
                Assert.AreEqual(mesh.vertexCount, mesh.tangents.Length, "ノーマルマップ用 tangent が全頂点に入っていない");
                Assert.Greater(mesh.bounds.size.x, 7.9f);
                Assert.Greater(mesh.bounds.size.z, 7.9f);
            }
            finally
            {
                if (mesh != null) Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void BeginnerSetup_RepairsEmptyObjectIntoUsableWater()
        {
            GameObject go = null;
            try
            {
                go = new GameObject("Beginner Water Test");
                var report = new System.Collections.Generic.List<string>();

                bool changed = WaterBeginnerSetup.RepairWaterObject(go, report);

                Assert.IsTrue(changed, "空の GameObject を水面へ修復できていない");
                Assert.IsNotNull(go.GetComponent<MeshFilter>(), "MeshFilter が追加されていない");
                Assert.IsNotNull(go.GetComponent<Renderer>(), "Renderer が追加されていない");
                Assert.IsNotNull(go.GetComponent<WaterSurfaceAnimator>(), "WaterSurfaceAnimator が追加されていない");
                Assert.Greater(go.GetComponent<MeshFilter>().sharedMesh.vertexCount, 64, "実高さ用の分割メッシュに交換されていない");
                Assert.IsNotEmpty(report, "初心者向けの診断結果が出ていない");
            }
            finally
            {
                if (go != null) Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BeginnerSetup_RepairsDefaultPlaneMaterialIntoWater()
        {
            GameObject go = null;
            try
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                var report = new System.Collections.Generic.List<string>();

                bool changed = WaterBeginnerSetup.RepairWaterObject(go, report);
                var renderer = go.GetComponent<Renderer>();
                var mat = renderer != null ? renderer.sharedMaterial : null;

                Assert.IsTrue(changed, "Unity 標準 Plane の見た目を水へ修復できていない");
                Assert.IsNotNull(mat, "水マテリアルが適用されていない");
                Assert.IsNotNull(mat.shader, "水マテリアルの shader がない");
                Assert.AreNotEqual("Hidden/InternalErrorShader", mat.shader.name, "ピンク shader が適用されている");
                Assert.IsTrue(mat.shader.name.StartsWith("Siliq/Water") || mat.HasProperty("_BumpMap"),
                    "水向けのマテリアルへ差し替わっていない");
                if (mat.HasProperty("_TransmissionStrength"))
                {
                    Assert.GreaterOrEqual(mat.GetFloat("_TransmissionStrength"), 0.90f,
                        "初心者向け修復は Crystal Lagoon の透明感を優先する");
                }
                Assert.IsNotNull(go.GetComponent<WaterSurfaceAnimator>(), "Animator が追加されていない");
            }
            finally
            {
                if (go != null) Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BeginnerGuide_ProvidesMenuEntryAndActions()
        {
            var method = typeof(WaterBeginnerGuideWindow).GetMethod(
                "Open",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "はじめてガイドを開く entry point が見つからない");

            bool hasMenu = false;
            foreach (var attribute in method.GetCustomAttributes(typeof(MenuItem), false))
            {
                var menuItem = attribute as MenuItem;
                if (menuItem != null && menuItem.menuItem == WaterBeginnerGuideWindow.MenuPath)
                {
                    hasMenu = true;
                    break;
                }
            }

            Assert.IsTrue(hasMenu, "Tools > Siliq Water > はじめてガイド menu が登録されていない");
            Assert.AreEqual("最高品質 Hero 水面を作成", WaterBeginnerGuideWindow.CreateCrystalLagoonHeroActionLabel);
            Assert.AreEqual("クリスタルラグーン水面を作成", WaterBeginnerGuideWindow.CreateCrystalLagoonActionLabel);
            Assert.AreEqual("フラッグシップ水面を作成", WaterBeginnerGuideWindow.CreateFlagshipActionLabel);
            Assert.AreEqual("選択中の水面を診断して自動修復", WaterBeginnerGuideWindow.RepairSelectionActionLabel);
            Assert.AreEqual("PrebakedPack/SampleScene/SC_CrystalLagoon_Showcase.unity",
                WaterBeginnerGuideWindow.CrystalLagoonShowcaseScenePath);
            Assert.AreEqual("PrebakedPack/Prefabs/PF_Siliq_CrystalLagoon_Complete.prefab",
                WaterBeginnerGuideWindow.CrystalLagoonCompletePrefabPath);
            Assert.AreEqual("PrebakedPack/Prefabs/PF_Siliq_CrystalLagoon_Hero_Complete.prefab",
                WaterBeginnerGuideWindow.CrystalLagoonHeroCompletePrefabPath);
            Assert.AreEqual("PrebakedPack/Preview/preview_crystal_lagoon_complete.png",
                WaterBeginnerGuideWindow.CrystalLagoonCompletePreviewPath);
            Assert.AreEqual("PrebakedPack/Preview/preview_crystal_lagoon_hero_complete.png",
                WaterBeginnerGuideWindow.CrystalLagoonHeroCompletePreviewPath);
            Assert.AreEqual("PrebakedPack/ReadyMaterials/M_Siliq_CrystalLagoon_Hero_Ready.mat",
                WaterBeginnerGuideWindow.CrystalLagoonHeroMaterialPath);
        }

        [Test]
        public void BeginnerSetup_ProvidesHeroCreateMenu()
        {
            var method = typeof(WaterBeginnerSetup).GetMethod(
                "CreateCrystalLagoonHeroWater",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "最高品質 Hero のかんたん作成 entry point が見つからない");

            bool hasToolsMenu = false;
            bool hasGameObjectMenu = false;
            foreach (var attribute in method.GetCustomAttributes(typeof(MenuItem), false))
            {
                var menuItem = attribute as MenuItem;
                if (menuItem == null) continue;
                if (menuItem.menuItem == "Tools/Siliq Water/かんたん作成/最高品質 Hero 水面を作成")
                {
                    hasToolsMenu = true;
                }
                if (menuItem.menuItem == "GameObject/Siliq Water/かんたん作成/最高品質 Hero 水面を作成")
                {
                    hasGameObjectMenu = true;
                }
            }

            Assert.IsTrue(hasToolsMenu, "Tools の Hero かんたん作成 menu が登録されていない");
            Assert.IsTrue(hasGameObjectMenu, "GameObject の Hero かんたん作成 menu が登録されていない");
        }

        [Test]
        public void BeginnerSetup_ProvidesCrystalLagoonCreateMenu()
        {
            var method = typeof(WaterBeginnerSetup).GetMethod(
                "CreateCrystalLagoonWater",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "Crystal Lagoon のかんたん作成 entry point が見つからない");

            bool hasToolsMenu = false;
            bool hasGameObjectMenu = false;
            foreach (var attribute in method.GetCustomAttributes(typeof(MenuItem), false))
            {
                var menuItem = attribute as MenuItem;
                if (menuItem == null) continue;
                if (menuItem.menuItem == "Tools/Siliq Water/かんたん作成/クリスタルラグーン水面を作成")
                {
                    hasToolsMenu = true;
                }
                if (menuItem.menuItem == "GameObject/Siliq Water/かんたん作成/クリスタルラグーン水面を作成")
                {
                    hasGameObjectMenu = true;
                }
            }

            Assert.IsTrue(hasToolsMenu, "Tools の Crystal Lagoon かんたん作成 menu が登録されていない");
            Assert.IsTrue(hasGameObjectMenu, "GameObject の Crystal Lagoon かんたん作成 menu が登録されていない");
        }

        [Test]
        public void CrystalLagoonShowcaseScene_IsBundledForProductPreview()
        {
            const string sceneGuid = "b24c89703d3b34043b6907a31617c178";
            const string gridGuid = "c372220d922244035b21bdd3e4ff000a";
            const string materialGuid = "f2c657c8bc4a4c7080e6a1327818890d";
            const string overlayMaterialGuid = "f2c657c8bc4a4c7080e6a1327818897c";

            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
            string gridPath = AssetDatabase.GUIDToAssetPath(gridGuid);
            string materialPath = AssetDatabase.GUIDToAssetPath(materialGuid);
            string overlayMaterialPath = AssetDatabase.GUIDToAssetPath(overlayMaterialGuid);
            Assert.AreEqual("Packages/com.siliq.water-normalmap/PrebakedPack/SampleScene/SC_CrystalLagoon_Showcase.unity", scenePath);
            Assert.AreEqual("Packages/com.siliq.water-normalmap/PrebakedPack/SampleScene/CrystalLagoon_Showcase_Grid.asset", gridPath);
            Assert.AreEqual("Packages/com.siliq.water-normalmap/PrebakedPack/ReadyMaterials/M_Siliq_CrystalLagoon_Ready.mat", materialPath);
            Assert.AreEqual("Packages/com.siliq.water-normalmap/PrebakedPack/ReadyMaterials/M_Siliq_CrystalLagoon_CausticsOverlay.mat", overlayMaterialPath);

            var grid = AssetDatabase.LoadAssetAtPath<Mesh>(gridPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            var overlayMaterial = AssetDatabase.LoadAssetAtPath<Material>(overlayMaterialPath);
            Assert.IsNotNull(grid, "Crystal Lagoon showcase 用の分割メッシュが読み込めない");
            Assert.IsNotNull(material, "Crystal Lagoon ready material が読み込めない");
            Assert.IsNotNull(overlayMaterial, "Crystal Lagoon 床用 caustics overlay material が読み込めない");
            Assert.GreaterOrEqual(grid.vertexCount, 9000, "showcase は高さと反射が見える分割メッシュにする");

            string scene = File.ReadAllText(scenePath);
            StringAssert.Contains("Crystal Lagoon Water - transparent beauty preset", scene);
            StringAssert.Contains(materialGuid, scene, "showcase scene は Crystal Lagoon ready material を直接参照する");
            StringAssert.Contains("Crystal Lagoon Floor Caustics Overlay", scene,
                "showcase scene は開いた時点で水底光 overlay を確認できる必要がある");
            StringAssert.Contains(overlayMaterialGuid, scene,
                "showcase scene は Crystal Lagoon 床用 caustics overlay material を直接参照する");
            StringAssert.Contains(gridGuid, scene, "showcase scene は分割済み grid mesh を参照する");
            StringAssert.Contains("Pale pool floor for transparency check", scene, "showcase scene には透明度確認用の明るい床が必要");
            StringAssert.Contains("Crystal Lagoon Preview Camera", scene, "showcase scene には確認用 camera が必要");
        }

        [Test]
        public void CrystalLagoonCompletePrefab_IsBundledForBeginnerDragAndDrop()
        {
            const string prefabGuid = "97c15c18ecf804f7695b02375df4ffb4";
            const string gridGuid = "c372220d922244035b21bdd3e4ff000a";
            const string waterMaterialGuid = "f2c657c8bc4a4c7080e6a1327818890d";
            const string overlayMaterialGuid = "f2c657c8bc4a4c7080e6a1327818897c";
            const string floorMaterialGuid = "42443abe313b246949f1b9f8504234ce";
            const string floorShaderGuid = "a171aabb01c34e01a1b2c3d4e5f60607";

            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
            string floorMaterialPath = AssetDatabase.GUIDToAssetPath(floorMaterialGuid);
            string floorShaderPath = AssetDatabase.GUIDToAssetPath(floorShaderGuid);
            Assert.AreEqual("Packages/com.siliq.water-normalmap/PrebakedPack/Prefabs/PF_Siliq_CrystalLagoon_Complete.prefab", prefabPath);
            Assert.AreEqual("Packages/com.siliq.water-normalmap/PrebakedPack/ReadyMaterials/M_Siliq_PalePoolFloor.mat", floorMaterialPath);
            Assert.AreEqual("Packages/com.siliq.water-normalmap/Runtime/Shaders/SiliqPalePoolFloor.shader", floorShaderPath);

            string floorShaderText = File.ReadAllText(floorShaderPath);
            StringAssert.Contains("Shader \"Siliq/Pale Pool Floor Mobile\"", floorShaderText);
            StringAssert.Contains("_TileScale", floorShaderText,
                "完成 Prefab の床は単色板ではなく薄いタイル感を持つ必要がある");
            Assert.IsFalse(floorShaderText.Contains("com.unity.render-pipelines.universal"),
                "床 shader は URP package 未導入でも壊れない Built-in 互換にする");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, "Crystal Lagoon 完成 Prefab が読み込めない");
            Assert.AreEqual("PF_Siliq_CrystalLagoon_Complete", prefab.name);

            var water = prefab.transform.Find("Crystal Lagoon Water - transparent beauty preset");
            var floor = prefab.transform.Find("Pale Pool Floor - transparency and caustics receiver");
            var overlay = prefab.transform.Find("Crystal Lagoon Floor Caustics Overlay");
            var light = prefab.transform.Find("Crystal Lagoon Soft Preview Light");
            Assert.IsNotNull(water, "完成 Prefab に Crystal Lagoon 水面がない");
            Assert.IsNotNull(floor, "完成 Prefab に透明度確認用の明るい床がない");
            Assert.IsNotNull(overlay, "完成 Prefab に床用 caustics overlay がない");
            Assert.IsNotNull(light, "完成 Prefab に確認用ライトがない");

            var waterFilter = water.GetComponent<MeshFilter>();
            var waterRenderer = water.GetComponent<MeshRenderer>();
            var floorRenderer = floor.GetComponent<MeshRenderer>();
            var overlayRenderer = overlay.GetComponent<MeshRenderer>();
            var animator = water.GetComponent<WaterSurfaceAnimator>();
            Assert.IsNotNull(waterFilter, "完成 Prefab の水面に MeshFilter がない");
            Assert.IsNotNull(waterRenderer, "完成 Prefab の水面に MeshRenderer がない");
            Assert.IsNotNull(floorRenderer, "完成 Prefab の床に MeshRenderer がない");
            Assert.IsNotNull(overlayRenderer, "完成 Prefab の caustics overlay に MeshRenderer がない");
            Assert.IsNotNull(animator, "完成 Prefab の水面に初心者調整用 WaterSurfaceAnimator がない");

            Assert.AreEqual(gridGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(waterFilter.sharedMesh)),
                "完成 Prefab は分割済み Crystal Lagoon grid mesh を使う必要がある");
            Assert.AreEqual(waterMaterialGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(waterRenderer.sharedMaterial)),
                "完成 Prefab の水面は Crystal Lagoon ready material を直接参照する必要がある");
            Assert.AreEqual(floorMaterialGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(floorRenderer.sharedMaterial)),
                "完成 Prefab の床は明るい確認用 material を直接参照する必要がある");
            Assert.AreEqual(overlayMaterialGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(overlayRenderer.sharedMaterial)),
                "完成 Prefab の overlay は Crystal Lagoon 床用 caustics material を直接参照する必要がある");
            Assert.AreEqual("Siliq/Pale Pool Floor Mobile", floorRenderer.sharedMaterial.shader.name,
                "完成 Prefab の床は水底光が映える専用 shader を使う必要がある");
            Assert.GreaterOrEqual(floorRenderer.sharedMaterial.GetFloat("_TileScale"), 4f,
                "完成 Prefab の床は近距離でも単色板に見えないタイル密度が必要");
            Assert.LessOrEqual(floorRenderer.sharedMaterial.GetFloat("_TileScale"), 12f,
                "完成 Prefab の床タイルが細かすぎるとノイズに見える");
            Assert.GreaterOrEqual(floorRenderer.sharedMaterial.GetFloat("_CausticsReceive"), 0.10f,
                "完成 Prefab の床は水底光を受ける明るさを持つ必要がある");

            Assert.IsNull(floor.GetComponent<Collider>(), "完成 Prefab の床は置いた瞬間に不要な物理 collider を増やさない");
            Assert.IsNull(overlay.GetComponent<Collider>(), "完成 Prefab の caustics overlay は不要な collider を持たない");
            Assert.LessOrEqual(animator.speed, 0.00014f, "完成 Prefab は静かな Crystal Lagoon 速度から始める");
            Assert.LessOrEqual(animator.displacementStrength, 0.006f, "完成 Prefab は板状の高さ模様を避ける");
            Assert.GreaterOrEqual(animator.clarity, 0.90f, "完成 Prefab は透明な抜け感を高くしておく");
            Assert.GreaterOrEqual(animator.refractionStrength, 0.30f, "完成 Prefab は水越しの揺らぎを確認できる必要がある");
            Assert.GreaterOrEqual(animator.reflectionPatternStrength, 0.42f, "完成 Prefab は反射パターンを強めに確認できる必要がある");
            Assert.GreaterOrEqual(animator.causticsStrength, 0.70f, "完成 Prefab は水底光を強めに確認できる必要がある");
            Assert.GreaterOrEqual(animator.causticsFocus, 2.0f, "完成 Prefab は水底光を細い焦点線として確認できる必要がある");
            Assert.GreaterOrEqual(animator.causticsPrismStrength, 0.18f, "完成 Prefab は水底光の薄い色分散を確認できる必要がある");
            Assert.GreaterOrEqual(animator.causticsScatterStrength, 0.55f, "完成 Prefab は水底光の柔らかい広がりを確認できる必要がある");
            Assert.GreaterOrEqual(animator.bottomLightStrength, 1.65f, "完成 Prefab は水底光が水越しに見える必要がある");
        }

        [Test]
        public void CrystalLagoonHeroCompletePrefab_IsBundledForBeautyFirstDragAndDrop()
        {
            const string prefabGuid = "a171aabb01c34e01a1b2c3d4e5f60708";
            const string gridGuid = "c372220d922244035b21bdd3e4ff000a";
            const string heroWaterMaterialGuid = "a171aabb01c34e01a1b2c3d4e5f60807";
            const string heroOverlayMaterialGuid = "f2c657c8bc4a4c7080e6a1327818898c";
            const string floorMaterialGuid = "42443abe313b246949f1b9f8504234ce";

            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
            Assert.AreEqual("Packages/com.siliq.water-normalmap/PrebakedPack/Prefabs/PF_Siliq_CrystalLagoon_Hero_Complete.prefab", prefabPath);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, "Crystal Lagoon Hero 完成 Prefab が読み込めない");
            Assert.AreEqual("PF_Siliq_CrystalLagoon_Hero_Complete", prefab.name);

            var water = prefab.transform.Find("Crystal Lagoon Hero Water - premium transparent beauty");
            var floor = prefab.transform.Find("Pale Pool Floor - transparency and caustics receiver");
            var overlay = prefab.transform.Find("Crystal Lagoon Hero Floor Caustics Overlay");
            var light = prefab.transform.Find("Crystal Lagoon Hero Soft Preview Light");
            Assert.IsNotNull(water, "Hero 完成 Prefab に水面がない");
            Assert.IsNotNull(floor, "Hero 完成 Prefab に明るい床がない");
            Assert.IsNotNull(overlay, "Hero 完成 Prefab に Hero caustics overlay がない");
            Assert.IsNotNull(light, "Hero 完成 Prefab に確認用ライトがない");

            var waterFilter = water.GetComponent<MeshFilter>();
            var waterRenderer = water.GetComponent<MeshRenderer>();
            var floorRenderer = floor.GetComponent<MeshRenderer>();
            var overlayRenderer = overlay.GetComponent<MeshRenderer>();
            var animator = water.GetComponent<WaterSurfaceAnimator>();
            Assert.IsNotNull(waterFilter, "Hero 完成 Prefab の水面に MeshFilter がない");
            Assert.IsNotNull(waterRenderer, "Hero 完成 Prefab の水面に MeshRenderer がない");
            Assert.IsNotNull(floorRenderer, "Hero 完成 Prefab の床に MeshRenderer がない");
            Assert.IsNotNull(overlayRenderer, "Hero 完成 Prefab の caustics overlay に MeshRenderer がない");
            Assert.IsNotNull(animator, "Hero 完成 Prefab の水面に WaterSurfaceAnimator がない");

            Assert.AreEqual(gridGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(waterFilter.sharedMesh)),
                "Hero 完成 Prefab は分割済み Crystal Lagoon grid mesh を使う必要がある");
            Assert.AreEqual(heroWaterMaterialGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(waterRenderer.sharedMaterial)),
                "Hero 完成 Prefab の水面は Hero ready material を直接参照する必要がある");
            Assert.AreEqual(heroOverlayMaterialGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(overlayRenderer.sharedMaterial)),
                "Hero 完成 Prefab の overlay は Hero caustics overlay material を直接参照する必要がある");
            Assert.AreEqual(floorMaterialGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(floorRenderer.sharedMaterial)),
                "Hero 完成 Prefab の床は明るい確認用 material を直接参照する必要がある");

            Assert.AreEqual("Siliq/Water Mobile (Quest)", waterRenderer.sharedMaterial.shader.name);
            Assert.AreEqual("Siliq/Caustics Overlay Mobile", overlayRenderer.sharedMaterial.shader.name);
            Assert.GreaterOrEqual(overlayRenderer.sharedMaterial.GetFloat("_Intensity"), 0.60f,
                "Hero 完成 Prefab は床側にも強めの水底光を持つ必要がある");

            Assert.IsNull(floor.GetComponent<Collider>(), "Hero 完成 Prefab の床は置いた瞬間に不要な物理 collider を増やさない");
            Assert.IsNull(overlay.GetComponent<Collider>(), "Hero 完成 Prefab の caustics overlay は不要な collider を持たない");
            Assert.LessOrEqual(animator.speed, 0.00010f, "Hero 完成 Prefab は静かな速度から始める");
            Assert.LessOrEqual(animator.displacementStrength, 0.004f, "Hero 完成 Prefab は板状の高さ模様を避ける");
            Assert.LessOrEqual(animator.opacity, 0.36f, "Hero 完成 Prefab は透明感を最優先にする");
            Assert.GreaterOrEqual(animator.clarity, 0.97f, "Hero 完成 Prefab は透明な抜け感を最大寄りにしておく");
            Assert.GreaterOrEqual(animator.refractionStrength, 0.40f, "Hero 完成 Prefab は水越しの揺らぎを強めに確認できる必要がある");
            Assert.GreaterOrEqual(animator.reflectionPatternStrength, 0.55f, "Hero 完成 Prefab は反射帯を確認できる必要がある");
            Assert.GreaterOrEqual(animator.transmissionStrength, 0.97f, "Hero 完成 Prefab は透過光を強めにする");
            Assert.GreaterOrEqual(animator.causticsStrength, 0.80f, "Hero 完成 Prefab は水底光を強めに確認できる必要がある");
            Assert.GreaterOrEqual(animator.causticsFocus, 2.3f, "Hero 完成 Prefab は水底光を細く締めて見せる必要がある");
            Assert.GreaterOrEqual(animator.causticsPrismStrength, 0.22f, "Hero 完成 Prefab は水底光の薄いプリズム色を確認できる必要がある");
            Assert.GreaterOrEqual(animator.causticsScatterStrength, 0.70f, "Hero 完成 Prefab は柔らかい水底光の膜を確認できる必要がある");
            Assert.GreaterOrEqual(animator.bottomLightStrength, 1.85f, "Hero 完成 Prefab は水底光が水越しに強く見える必要がある");
        }

        [Test]
        public void PackageRoot_IncludesPackageJsonMeta()
        {
            const string packageJsonGuid = "0f4d17e00f5a4d638fbfdc4cb7483e2f";
            string packageJsonPath = AssetDatabase.GUIDToAssetPath(packageJsonGuid);
            Assert.IsNotEmpty(packageJsonPath, "package.json.meta の GUID が解決できない");

            string metaPath = packageJsonPath + ".meta";
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(packageJsonPath);
            if (packageInfo != null && !string.IsNullOrEmpty(packageInfo.resolvedPath))
            {
                metaPath = Path.Combine(packageInfo.resolvedPath, "package.json.meta");
            }

            Assert.IsTrue(File.Exists(metaPath), "`package.json has no meta file` 警告を防ぐ package.json.meta がない");
            StringAssert.Contains("TextScriptImporter", File.ReadAllText(metaPath));
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

        static void AssertColor(Color expected, Color actual, string label)
        {
            Assert.AreEqual(expected.r, actual.r, 1e-5f, $"{label}.r");
            Assert.AreEqual(expected.g, actual.g, 1e-5f, $"{label}.g");
            Assert.AreEqual(expected.b, actual.b, 1e-5f, $"{label}.b");
            Assert.AreEqual(expected.a, actual.a, 1e-5f, $"{label}.a");
        }

        sealed class UniversalFakePipelineAsset : RenderPipelineAsset
        {
            protected override RenderPipeline CreatePipeline()
            {
                return null;
            }
        }
    }
}
