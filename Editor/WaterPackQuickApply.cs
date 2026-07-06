using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Siliq.Water.Editor
{
    /// <summary>
    /// 焼き済み水マテリアルを選択オブジェクトへワンクリック適用するメニュー。
    /// Hierarchy でオブジェクトを右クリック → Siliq Water → 好きな水を選ぶだけ。
    /// 適用と同時に動き用コンポーネントも付与し、静止画にならないようにする。
    /// Ripple は UV スクロールではなく、発生点から外へ広がる波紋 emitter を使う。
    /// </summary>
    public static class WaterPackQuickApply
    {
        struct MotionPreset
        {
            public float dir;
            public float speed;
            public float strength;
            public float tiling;

            public MotionPreset(float dir, float speed, float strength, float tiling)
            {
                this.dir = dir;
                this.speed = speed;
                this.strength = strength;
                this.tiling = tiling;
            }
        }

        // 各水の雰囲気に合わせた動き。Standard のノーマルだけでも見えるよう少し強めにしている。
        static readonly MotionPreset CalmMotion = new MotionPreset(35f, 0.35f, 1.35f, 1.35f);
        static readonly MotionPreset RippleMotion = new MotionPreset(0f, 0f, 0.55f, 1.15f);
        static readonly MotionPreset StreamMotion = new MotionPreset(0f, 1.0f, 1.45f, 1.8f);
        static readonly MotionPreset PoolMotion = new MotionPreset(50f, 0.24f, 0.55f, 1.08f);
        static readonly MotionPreset CyberMotion = new MotionPreset(18f, 0.52f, 0.78f, 1.24f);

        // PrebakedPack/Materials/*.mat.meta の固定 GUID
        const string CalmGuid = "a171aabb01c34e01a1b2c3d4e5f60201";
        const string RippleGuid = "a171aabb01c34e01a1b2c3d4e5f60202";
        const string StreamGuid = "a171aabb01c34e01a1b2c3d4e5f60203";
        const string PoolGuid = "a171aabb01c34e01a1b2c3d4e5f60204";
        const string CyberGuid = "a171aabb01c34e01a1b2c3d4e5f60205";

        const string MenuRoot = "GameObject/Siliq Water/水マテリアルを適用/";
        const string TransparentMenuRoot = "GameObject/Siliq Water/透明な水マテリアルを適用 (PC)/";
        const string MobileTransparentMenuRoot = "GameObject/Siliq Water/透明な水マテリアルを適用 (iOS/Mobile)/";
        const float TransparentOpacity = 0.5f;
        const float MobileTransparentOpacity = 0.30f;

        [MenuItem(MenuRoot + "静かな水面 (Calm)", false, 10)]
        static void ApplyCalm() => Apply(CalmGuid, "Calm", CalmMotion);

        [MenuItem(MenuRoot + "波紋 (Ripple)", false, 11)]
        static void ApplyRipple() => ApplyRipplePreset(false, false);

        [MenuItem(MenuRoot + "流れ (Stream)", false, 12)]
        static void ApplyStream() => Apply(StreamGuid, "Stream", StreamMotion);

        [MenuItem(MenuRoot + "プール (Pool)", false, 13)]
        static void ApplyPool() => Apply(PoolGuid, "Pool", PoolMotion);

        [MenuItem(MenuRoot + "サイバー (Cyber)", false, 14)]
        static void ApplyCyber() => Apply(CyberGuid, "Cyber", CyberMotion);

        [MenuItem(TransparentMenuRoot + "静かな水面 (Calm)", false, 20)]
        static void ApplyTransparentCalm() => Apply(CalmGuid, "Calm", CalmMotion, true);

        [MenuItem(TransparentMenuRoot + "波紋 (Ripple)", false, 21)]
        static void ApplyTransparentRipple() => ApplyRipplePreset(true, false);

        [MenuItem(TransparentMenuRoot + "流れ (Stream)", false, 22)]
        static void ApplyTransparentStream() => Apply(StreamGuid, "Stream", StreamMotion, true);

        [MenuItem(TransparentMenuRoot + "プール (Pool)", false, 23)]
        static void ApplyTransparentPool() => Apply(PoolGuid, "Pool", PoolMotion, true);

        [MenuItem(TransparentMenuRoot + "サイバー (Cyber)", false, 24)]
        static void ApplyTransparentCyber() => Apply(CyberGuid, "Cyber", CyberMotion, true);

        [MenuItem(MobileTransparentMenuRoot + "静かな水面 (Calm)", false, 30)]
        static void ApplyMobileTransparentCalm() => ApplyMobileTransparent(CalmGuid, "Calm", CalmMotion);

        [MenuItem(MobileTransparentMenuRoot + "波紋 (Ripple)", false, 31)]
        static void ApplyMobileTransparentRipple() => ApplyRipplePreset(true, true);

        [MenuItem(MobileTransparentMenuRoot + "流れ (Stream)", false, 32)]
        static void ApplyMobileTransparentStream() => ApplyMobileTransparent(StreamGuid, "Stream", StreamMotion);

        [MenuItem(MobileTransparentMenuRoot + "プール (Pool)", false, 33)]
        static void ApplyMobileTransparentPool() => ApplyMobileTransparent(PoolGuid, "Pool", PoolMotion);

        [MenuItem(MobileTransparentMenuRoot + "サイバー (Cyber)", false, 34)]
        static void ApplyMobileTransparentCyber() => ApplyMobileTransparent(CyberGuid, "Cyber", CyberMotion);

        [MenuItem(MenuRoot + "静かな水面 (Calm)", true)]
        [MenuItem(MenuRoot + "波紋 (Ripple)", true)]
        [MenuItem(MenuRoot + "流れ (Stream)", true)]
        [MenuItem(MenuRoot + "プール (Pool)", true)]
        [MenuItem(MenuRoot + "サイバー (Cyber)", true)]
        [MenuItem(TransparentMenuRoot + "静かな水面 (Calm)", true)]
        [MenuItem(TransparentMenuRoot + "波紋 (Ripple)", true)]
        [MenuItem(TransparentMenuRoot + "流れ (Stream)", true)]
        [MenuItem(TransparentMenuRoot + "プール (Pool)", true)]
        [MenuItem(TransparentMenuRoot + "サイバー (Cyber)", true)]
        [MenuItem(MobileTransparentMenuRoot + "静かな水面 (Calm)", true)]
        [MenuItem(MobileTransparentMenuRoot + "波紋 (Ripple)", true)]
        [MenuItem(MobileTransparentMenuRoot + "流れ (Stream)", true)]
        [MenuItem(MobileTransparentMenuRoot + "プール (Pool)", true)]
        [MenuItem(MobileTransparentMenuRoot + "サイバー (Cyber)", true)]
        static bool ValidateSelection()
        {
            foreach (var go in Selection.gameObjects)
            {
                if (go.GetComponent<Renderer>() != null) return true;
            }
            return false;
        }

        static void Apply(string guid, string label, MotionPreset motion, bool transparent = false)
        {
            var mat = LoadPrebakedMaterial(guid, $"M_Water_{label}");
            if (mat == null)
            {
                EditorUtility.DisplayDialog("Siliq Water",
                    $"M_Water_{label} が見つかりませんでした。\nPrebakedPack フォルダがプロジェクトに含まれているか確認してください。", "OK");
                return;
            }
            if (transparent)
            {
                mat = GetOrCreateTransparentMaterial(mat, label);
            }

            ApplyMaterialToSelection(mat, motion, "_BumpMap");
        }

        static void ApplyRipplePreset(bool transparent, bool mobileTransparent)
        {
            var source = LoadPrebakedMaterial(RippleGuid, "M_Water_Ripple");
            if (source == null)
            {
                EditorUtility.DisplayDialog("Siliq Water",
                    "M_Water_Ripple が見つかりませんでした。\nPrebakedPack フォルダがプロジェクトに含まれているか確認してください。", "OK");
                return;
            }

            var baseNormalSource = LoadPrebakedMaterial(CalmGuid, "M_Water_Calm") ?? source;
            var mat = GetOrCreateExpandingRippleMaterial(source, baseNormalSource, transparent, mobileTransparent);
            if (mat == null)
            {
                EditorUtility.DisplayDialog("Siliq Water",
                    "Siliq/Water Mobile (Quest) シェーダーが見つかりませんでした。パッケージが正しく読み込まれているか確認してください。", "OK");
                return;
            }

            ApplyMaterialToSelection(mat, RippleMotion, "_NormalMap", true);
        }

        static void ApplyMobileTransparent(string guid, string label, MotionPreset motion)
        {
            var source = LoadPrebakedMaterial(guid, $"M_Water_{label}");
            if (source == null)
            {
                EditorUtility.DisplayDialog("Siliq Water",
                    $"M_Water_{label} が見つかりませんでした。\nPrebakedPack フォルダがプロジェクトに含まれているか確認してください。", "OK");
                return;
            }

            var mat = GetOrCreateMobileTransparentMaterial(source, label);
            if (mat == null)
            {
                EditorUtility.DisplayDialog("Siliq Water",
                    "Siliq/Water Mobile (Quest) シェーダーが見つかりませんでした。パッケージが正しく読み込まれているか確認してください。", "OK");
                return;
            }

            ApplyMaterialToSelection(mat, motion, "_NormalMap");
        }

        static void ApplyMaterialToSelection(Material mat, MotionPreset motion, string texturePropertyName, bool expandingRipples = false)
        {
            int applied = 0;
            foreach (var go in Selection.gameObjects)
            {
                var renderer = go.GetComponent<Renderer>();
                if (renderer == null) continue;
                Undo.RecordObject(renderer, "水マテリアルを適用");
                renderer.sharedMaterial = mat;

                var animator = go.GetComponent<WaterSurfaceAnimator>();
                if (animator == null)
                {
                    animator = Undo.AddComponent<WaterSurfaceAnimator>(go);
                }
                else
                {
                    Undo.RecordObject(animator, "水マテリアルを適用");
                }
                animator.texturePropertyName = texturePropertyName;
                animator.directionDegrees = motion.dir;
                animator.speed = motion.speed;
                animator.strength = motion.strength;
                animator.tiling = motion.tiling;
                animator.ApplyImmediate(0f);
                EditorUtility.SetDirty(animator);

                ConfigureRippleEmitter(go, renderer, expandingRipples);
                applied++;
            }

            if (applied == 0)
            {
                EditorUtility.DisplayDialog("Siliq Water",
                    "Renderer を持つオブジェクトを選択してから実行してください。", "OK");
            }
        }

        static Material LoadPrebakedMaterial(string guid, string materialName)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            string[] guids = AssetDatabase.FindAssets($"{materialName} t:Material");
            foreach (string foundGuid in guids)
            {
                string foundPath = AssetDatabase.GUIDToAssetPath(foundGuid);
                if (foundPath.EndsWith($"/PrebakedPack/Materials/{materialName}.mat") ||
                    foundPath.EndsWith($"/{materialName}.mat"))
                {
                    mat = AssetDatabase.LoadAssetAtPath<Material>(foundPath);
                    if (mat != null) return mat;
                }
            }
            return null;
        }

        static Material GetOrCreateExpandingRippleMaterial(Material colorSource, Material normalSource, bool transparent, bool mobileTransparent)
        {
            Shader shader = Shader.Find("Siliq/Water Mobile (Quest)");
            if (shader == null) return null;

            const string root = "Assets/SiliqWater";
            const string folder = root + "/GeneratedMaterials";
            EnsureFolder("Assets", "SiliqWater");
            EnsureFolder(root, "GeneratedMaterials");

            string suffix = transparent ? (mobileTransparent ? "_iOS_Transparent" : "_Transparent") : "_Expanding";
            string path = $"{folder}/M_Water_Ripple{suffix}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            mat.name = $"M_Water_Ripple{suffix}";
            ApplySiliqNormal(mat, normalSource);
            ApplySiliqWaterPalette(mat, colorSource);
            SetupMacroVariation(mat, 0.48f, 0.11f, 0.42f, 0.20f);

            if (transparent)
            {
                SetupSiliqMobileTransparent(mat, mobileTransparent ? MobileTransparentOpacity : TransparentOpacity);
            }
            else
            {
                SetupSiliqOpaque(mat);
            }

            SetupExpandingRippleMaterial(mat);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        static Material GetOrCreateTransparentMaterial(Material source, string label)
        {
            const string root = "Assets/SiliqWater";
            const string folder = root + "/GeneratedMaterials";
            EnsureFolder("Assets", "SiliqWater");
            EnsureFolder(root, "GeneratedMaterials");

            string path = $"{folder}/M_Water_{label}_Transparent.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(source);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                EditorUtility.CopySerialized(source, mat);
            }

            mat.name = $"M_Water_{label}_Transparent";
            SetupStandardTransparent(mat, TransparentOpacity);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        static Material GetOrCreateMobileTransparentMaterial(Material source, string label)
        {
            Shader shader = Shader.Find("Siliq/Water Mobile (Quest)");
            if (shader == null) return null;

            const string root = "Assets/SiliqWater";
            const string folder = root + "/GeneratedMaterials";
            EnsureFolder("Assets", "SiliqWater");
            EnsureFolder(root, "GeneratedMaterials");

            string path = $"{folder}/M_Water_{label}_iOS_Transparent.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            mat.name = $"M_Water_{label}_iOS_Transparent";
            ApplySiliqNormal(mat, source);
            ApplySiliqWaterPalette(mat, source);
            SetupMacroVariation(mat, 0.42f, 0.10f, 0.36f, 0.18f);

            SetupSiliqMobileTransparent(mat, MobileTransparentOpacity);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        static void SetupStandardTransparent(Material mat, float opacity)
        {
            if (mat == null) return;

            if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 3f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);

            if (mat.HasProperty("_Color"))
            {
                Color c = mat.GetColor("_Color");
                c.a = Mathf.Clamp01(opacity);
                mat.SetColor("_Color", c);
            }

            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.EnableKeyword("_NORMALMAP");
        }

        static void SetupSiliqOpaque(Material mat)
        {
            if (mat == null) return;

            if (mat.HasProperty("_Opacity")) mat.SetFloat("_Opacity", 1f);
            if (mat.HasProperty("_AlphaFresnel")) mat.SetFloat("_AlphaFresnel", 0f);
            if (mat.HasProperty("_EdgeReflection")) mat.SetFloat("_EdgeReflection", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.One);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 1f);

            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = (int)RenderQueue.Geometry;
        }

        static void SetupSiliqMobileTransparent(Material mat, float opacity)
        {
            if (mat == null) return;

            if (mat.HasProperty("_Opacity")) mat.SetFloat("_Opacity", Mathf.Clamp01(opacity));
            if (mat.HasProperty("_AlphaFresnel")) mat.SetFloat("_AlphaFresnel", 0.62f);
            if (mat.HasProperty("_AlphaPower")) mat.SetFloat("_AlphaPower", 2.15f);
            if (mat.HasProperty("_EdgeReflection")) mat.SetFloat("_EdgeReflection", 0.55f);
            if (mat.HasProperty("_TransmissionStrength")) mat.SetFloat("_TransmissionStrength", 0.52f);
            if (mat.HasProperty("_GlimmerIntensity")) mat.SetFloat("_GlimmerIntensity", 0.22f);
            if (mat.HasProperty("_GlimmerSharpness")) mat.SetFloat("_GlimmerSharpness", 14f);
            if (mat.HasProperty("_GlintIntensity")) mat.SetFloat("_GlintIntensity", 0.45f);
            if (mat.HasProperty("_GlintPower")) mat.SetFloat("_GlintPower", 220f);
            if (mat.HasProperty("_FresnelPower")) mat.SetFloat("_FresnelPower", 2.65f);
            if (mat.HasProperty("_ReflStrength")) mat.SetFloat("_ReflStrength", 0.9f);
            if (mat.HasProperty("_SpecIntensity")) mat.SetFloat("_SpecIntensity", 1.15f);
            if (mat.HasProperty("_SpecPower")) mat.SetFloat("_SpecPower", 220f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);

            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        static void SetupExpandingRippleMaterial(Material mat)
        {
            if (mat == null) return;

            if (mat.HasProperty("_UseRipples")) mat.SetFloat("_UseRipples", 1f);
            if (mat.HasProperty("_RippleSpeed")) mat.SetFloat("_RippleSpeed", 2.8f);
            if (mat.HasProperty("_RippleWidth")) mat.SetFloat("_RippleWidth", 0.28f);
            if (mat.HasProperty("_RippleLifetime")) mat.SetFloat("_RippleLifetime", 2.6f);
            if (mat.HasProperty("_RippleAmplitude")) mat.SetFloat("_RippleAmplitude", 1.15f);
            if (mat.HasProperty("_RippleChannel")) mat.SetFloat("_RippleChannel", 0f);
            if (mat.HasProperty("_Scroll1")) mat.SetVector("_Scroll1", Vector4.zero);
            if (mat.HasProperty("_Scroll2")) mat.SetVector("_Scroll2", Vector4.zero);
            if (mat.HasProperty("_NormalStrength")) mat.SetFloat("_NormalStrength", 0.55f);
            if (mat.HasProperty("_Tiling1")) mat.SetFloat("_Tiling1", 1.15f);
            if (mat.HasProperty("_Tiling2")) mat.SetFloat("_Tiling2", 2.1f);
            mat.EnableKeyword("_USE_RIPPLES");
        }

        static void SetupMacroVariation(Material mat, float variation, float scale, float directionBreakup, float colorVariation)
        {
            if (mat == null) return;

            if (mat.HasProperty("_MacroVariation")) mat.SetFloat("_MacroVariation", variation);
            if (mat.HasProperty("_MacroScale")) mat.SetFloat("_MacroScale", scale);
            if (mat.HasProperty("_MacroDirectionBreakup")) mat.SetFloat("_MacroDirectionBreakup", directionBreakup);
            if (mat.HasProperty("_MacroColorVariation")) mat.SetFloat("_MacroColorVariation", colorVariation);
        }

        static void ApplySiliqNormal(Material mat, Material source)
        {
            if (mat == null || source == null) return;

            if (source.HasProperty("_BumpMap") && mat.HasProperty("_NormalMap"))
            {
                mat.SetTexture("_NormalMap", source.GetTexture("_BumpMap"));
            }
            if (source.HasProperty("_BumpScale") && mat.HasProperty("_NormalStrength"))
            {
                mat.SetFloat("_NormalStrength", Mathf.Max(1f, source.GetFloat("_BumpScale")));
            }
        }

        static void ApplySiliqWaterPalette(Material mat, Material source)
        {
            if (mat == null) return;

            Color sourceColor = new Color(0.1f, 0.35f, 0.45f, 1f);
            if (source != null && source.HasProperty("_Color"))
            {
                sourceColor = source.GetColor("_Color");
            }

            Color shallow = Color.Lerp(sourceColor, new Color(0.62f, 0.96f, 1f, 1f), 0.45f);
            Color deep = Color.Lerp(sourceColor, new Color(0.005f, 0.09f, 0.16f, 1f), 0.58f);
            if (mat.HasProperty("_ShallowColor")) mat.SetColor("_ShallowColor", shallow);
            if (mat.HasProperty("_DeepColor")) mat.SetColor("_DeepColor", deep);
            if (mat.HasProperty("_HorizonColor")) mat.SetColor("_HorizonColor", new Color(0.82f, 0.94f, 1f, 1f));
            if (mat.HasProperty("_TransmissionColor")) mat.SetColor("_TransmissionColor", new Color(0.35f, 0.9f, 1f, 1f));
            if (mat.HasProperty("_GlimmerColor")) mat.SetColor("_GlimmerColor", new Color(0.92f, 0.99f, 1f, 1f));
        }

        static void ConfigureRippleEmitter(GameObject go, Renderer renderer, bool enabled)
        {
            var emitter = go.GetComponent<WaterRippleEmitter>();
            if (!enabled)
            {
                if (emitter != null)
                {
                    Undo.RecordObject(emitter, "水マテリアルを適用");
                    emitter.enabled = false;
                    EditorUtility.SetDirty(emitter);
                }
                return;
            }

            if (emitter == null)
            {
                emitter = Undo.AddComponent<WaterRippleEmitter>(go);
            }
            else
            {
                Undo.RecordObject(emitter, "水マテリアルを適用");
            }

            emitter.enabled = true;
            emitter.targetRenderer = renderer;
            emitter.waterSurfaceY = renderer.bounds.center.y;
            emitter.rippleChannel = 0;
            emitter.ripplesPerSecond = 1.8f;
            emitter.burstCount = 1;
            emitter.rippleSpeed = 2.8f;
            emitter.rippleWidth = 0.28f;
            emitter.rippleLifetime = 2.6f;
            emitter.rippleAmplitude = 1.15f;
            emitter.applyShaderSettings = true;
            emitter.ApplyImmediate();
            EditorUtility.SetDirty(emitter);
        }
    }
}
