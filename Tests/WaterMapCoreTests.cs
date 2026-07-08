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

                Assert.LessOrEqual(animator.speed, 0.04f,
                    "初期 speed は見た瞬間に流れすぎない低速値にする");
                Assert.LessOrEqual(animator.displacementSpeed, 0.04f,
                    "初期 height animation も速すぎない値にする");
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
                animator.speed = 0.6f;
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
                Assert.Greater(st.z, 0.1f, "speed / direction による X offset が反映されていない");
                Assert.AreEqual(2f, mainSt.x, 1e-5f, "Standard の normal UV 用 _MainTex_ST.x に tiling が反映されていない");
                Assert.AreEqual(2f, mainSt.y, 1e-5f, "Standard の normal UV 用 _MainTex_ST.y に tiling が反映されていない");
                Assert.Greater(mainSt.z, 0.1f, "Standard の normal UV 用 _MainTex_ST に offset が反映されていない");
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
                animator.edgeReflection = 0.73f;
                animator.reflectionStrength = 0.64f;
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
                Assert.AreEqual(0.73f, block.GetFloat("_EdgeReflection"), 1e-5f);
                Assert.AreEqual(0.64f, block.GetFloat("_ReflStrength"), 1e-5f);
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
                Assert.Greater(mat.GetFloat("_CausticsStrength"), 0.04f,
                    $"{name} は水底の光が完全に死んだ初期値ではいけない");
                Assert.Greater(mat.GetVector("_Scroll1").sqrMagnitude, 0.0001f,
                    $"{name} は WaterSurfaceAnimator なしでも shader 側で波が動く scroll を持つ必要がある");
                Assert.LessOrEqual(mat.GetVector("_Scroll1").magnitude, 0.045f,
                    $"{name} の shader scroll が速すぎる");
                Assert.LessOrEqual(mat.GetVector("_Scroll2").magnitude, 0.045f,
                    $"{name} の shader scroll 2 が速すぎる");
                Assert.LessOrEqual(mat.GetFloat("_DisplacementSpeed"), 0.05f,
                    $"{name} の高さアニメーションが速すぎる");
                Assert.LessOrEqual(mat.GetFloat("_CausticsSpeed"), 0.01f,
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
            Assert.Greater(metal.GetVector("_Scroll1").sqrMagnitude, 0.0001f);
            Assert.LessOrEqual(metal.GetVector("_Scroll1").magnitude, 0.045f);
            Assert.LessOrEqual(metal.GetVector("_Scroll2").magnitude, 0.045f);
            Assert.LessOrEqual(metal.GetFloat("_DisplacementSpeed"), 0.05f);
            Assert.AreEqual((float)BlendMode.One, metal.GetFloat("_SrcBlend"), 1e-5f);
            Assert.AreEqual((float)BlendMode.Zero, metal.GetFloat("_DstBlend"), 1e-5f);
            Assert.AreEqual(1f, metal.GetFloat("_ZWrite"), 1e-5f);
            Assert.AreEqual((int)RenderQueue.Geometry, metal.renderQueue);

            var flagship = LoadReadyMaterial("M_Siliq_FlagshipCrystal_Ready");
            Assert.IsNotNull(flagship.GetTexture("_HeightMap"),
                "Flagship ready material は専用 height map を持つが、初期 influence は 0 にする");
            Assert.LessOrEqual(flagship.GetFloat("_DisplacementStrength"), 0.02f);
            Assert.GreaterOrEqual(flagship.GetFloat("_CausticsStrength"), 0.60f,
                "美しさ特化の Flagship ready material は水底光をはっきり持つ必要がある");

            var lagoon = LoadReadyMaterial("M_Siliq_CrystalLagoon_Ready");
            Assert.LessOrEqual(lagoon.GetFloat("_NormalStrength"), 0.45f,
                "Crystal Lagoon は透明感優先なので凹凸を強くしすぎない");
            Assert.LessOrEqual(lagoon.GetFloat("_DisplacementStrength"), 0.01f,
                "Crystal Lagoon は板模様を避けるため実高さを控えめにする");
            Assert.GreaterOrEqual(lagoon.GetFloat("_TransmissionStrength"), 0.90f,
                "Crystal Lagoon は透き通った見た目を最優先にする");
            Assert.GreaterOrEqual(lagoon.GetFloat("_CausticsStrength"), 0.70f,
                "Crystal Lagoon は水底光を強めに持つ必要がある");
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
                Assert.IsTrue(mat.HasProperty("_HeightMap"));
                Assert.IsTrue(mat.HasProperty("_DisplacementStrength"));
                Assert.IsTrue(mat.HasProperty("_DisplacementScale"));
                Assert.IsTrue(mat.HasProperty("_DisplacementSpeed"));
                Assert.IsTrue(mat.HasProperty("_HeightMapInfluence"));
                Assert.IsTrue(mat.HasProperty("_CausticsMap"));
                Assert.IsTrue(mat.HasProperty("_CausticsStrength"));
                Assert.IsTrue(mat.HasProperty("_CausticsScale"));
                Assert.IsTrue(mat.HasProperty("_CausticsSpeed"));
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
            const string flagshipNormalGuid = "a171aabb01c34e01a1b2c3d4e5f60106";
            const string flagshipHeightGuid = "a171aabb01c34e01a1b2c3d4e5f60306";

            string normalPath = AssetDatabase.GUIDToAssetPath(normalGuid);
            string heightPath = AssetDatabase.GUIDToAssetPath(heightGuid);
            string flagshipNormalPath = AssetDatabase.GUIDToAssetPath(flagshipNormalGuid);
            string flagshipHeightPath = AssetDatabase.GUIDToAssetPath(flagshipHeightGuid);

            Assert.IsNotEmpty(normalPath, "Crystal Lagoon 専用 normal map が package に含まれていない");
            Assert.IsNotEmpty(heightPath, "Crystal Lagoon 専用 height map が package に含まれていない");
            Assert.AreNotEqual(flagshipNormalPath, normalPath, "Crystal Lagoon normal が Flagship normal の流用に戻っている");
            Assert.AreNotEqual(flagshipHeightPath, heightPath, "Crystal Lagoon height が Flagship height の流用に戻っている");

            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            var height = AssetDatabase.LoadAssetAtPath<Texture2D>(heightPath);
            Assert.IsNotNull(normal, "Crystal Lagoon normal map を読み込めない");
            Assert.IsNotNull(height, "Crystal Lagoon height map を読み込めない");
            Assert.GreaterOrEqual(normal.width, 2048, "美しさ特化 normal map は 2048px 以上にする");
            Assert.GreaterOrEqual(normal.height, 2048, "美しさ特化 normal map は 2048px 以上にする");
            Assert.GreaterOrEqual(height.width, 2048, "美しさ特化 height map は 2048px 以上にする");
            Assert.GreaterOrEqual(height.height, 2048, "美しさ特化 height map は 2048px 以上にする");

            var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            Assert.IsNotNull(normalImporter, "Crystal Lagoon normal importer が TextureImporter ではない");
            Assert.AreEqual(TextureImporterType.NormalMap, normalImporter.textureType,
                "Crystal Lagoon normal map が NormalMap import になっていない");

            var ready = LoadReadyMaterial("M_Siliq_CrystalLagoon_Ready");
            Assert.AreEqual(normalPath, AssetDatabase.GetAssetPath(ready.GetTexture("_NormalMap")),
                "Crystal Lagoon ready material が専用 normal map を参照していない");
            Assert.AreEqual(heightPath, AssetDatabase.GetAssetPath(ready.GetTexture("_HeightMap")),
                "Crystal Lagoon ready material が専用 height map を参照していない");
        }

        [Test]
        public void CrystalLagoonQuickApply_UsesDedicatedBeautyTextures()
        {
            const string normalGuid = "a171aabb01c34e01a1b2c3d4e5f60107";
            const string heightGuid = "a171aabb01c34e01a1b2c3d4e5f60307";
            string normalPath = AssetDatabase.GUIDToAssetPath(normalGuid);
            string heightPath = AssetDatabase.GUIDToAssetPath(heightGuid);

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
                Assert.GreaterOrEqual(mat.GetFloat("_TransmissionStrength"), 0.90f,
                    "Crystal Lagoon Quick Apply は透き通った見た目を優先する");
                Assert.GreaterOrEqual(mat.GetFloat("_CausticsStrength"), 0.70f,
                    "Crystal Lagoon Quick Apply は水底光を強めに持つ必要がある");
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
            StringAssert.Contains("クリスタルラグーンでは専用 2048px normal map と height map を割り当てる", guide,
                "Crystal Lagoon が Flagship texture の流用ではないことをガイドで説明する");
            StringAssert.Contains("透明な海・プール・フラッグシップ水・クリスタルラグーン", guide,
                "水底光の対象に Crystal Lagoon が含まれていることを説明する");
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
            Assert.AreEqual("クリスタルラグーン水面を作成", WaterBeginnerGuideWindow.CreateCrystalLagoonActionLabel);
            Assert.AreEqual("フラッグシップ水面を作成", WaterBeginnerGuideWindow.CreateFlagshipActionLabel);
            Assert.AreEqual("選択中の水面を診断して自動修復", WaterBeginnerGuideWindow.RepairSelectionActionLabel);
            Assert.AreEqual("PrebakedPack/SampleScene/SC_CrystalLagoon_Showcase.unity",
                WaterBeginnerGuideWindow.CrystalLagoonShowcaseScenePath);
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

            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
            string gridPath = AssetDatabase.GUIDToAssetPath(gridGuid);
            string materialPath = AssetDatabase.GUIDToAssetPath(materialGuid);
            Assert.AreEqual("Packages/com.siliq.water-normalmap/PrebakedPack/SampleScene/SC_CrystalLagoon_Showcase.unity", scenePath);
            Assert.AreEqual("Packages/com.siliq.water-normalmap/PrebakedPack/SampleScene/CrystalLagoon_Showcase_Grid.asset", gridPath);
            Assert.AreEqual("Packages/com.siliq.water-normalmap/PrebakedPack/ReadyMaterials/M_Siliq_CrystalLagoon_Ready.mat", materialPath);

            var grid = AssetDatabase.LoadAssetAtPath<Mesh>(gridPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.IsNotNull(grid, "Crystal Lagoon showcase 用の分割メッシュが読み込めない");
            Assert.IsNotNull(material, "Crystal Lagoon ready material が読み込めない");
            Assert.GreaterOrEqual(grid.vertexCount, 9000, "showcase は高さと反射が見える分割メッシュにする");

            string scene = File.ReadAllText(scenePath);
            StringAssert.Contains("Crystal Lagoon Water - transparent beauty preset", scene);
            StringAssert.Contains(materialGuid, scene, "showcase scene は Crystal Lagoon ready material を直接参照する");
            StringAssert.Contains(gridGuid, scene, "showcase scene は分割済み grid mesh を参照する");
            StringAssert.Contains("Pale pool floor for transparency check", scene, "showcase scene には透明度確認用の明るい床が必要");
            StringAssert.Contains("Crystal Lagoon Preview Camera", scene, "showcase scene には確認用 camera が必要");
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
