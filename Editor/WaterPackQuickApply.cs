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

        struct LookPreset
        {
            public string assetName;
            public string displayName;
            public string sourceGuid;
            public string sourceLabel;
            public MotionPreset motion;
            public bool transparent;
            public float opacity;
            public Color shallow;
            public Color deep;
            public Color horizon;
            public Color transmission;
            public Color glimmer;
            public float normalStrength;
            public float tiling1;
            public float tiling2;
            public float alphaFresnel;
            public float alphaPower;
            public float edgeReflection;
            public float transmissionStrength;
            public float glimmerIntensity;
            public float glimmerSharpness;
            public float glintIntensity;
            public float glintPower;
            public float specPower;
            public float specIntensity;
            public float fresnelPower;
            public float reflStrength;
            public float smoothness;
            public float macroVariation;
            public float macroScale;
            public float macroDirectionBreakup;
            public float macroColorVariation;
        }

        // 各水の雰囲気に合わせた動き。Standard のノーマルだけでも見えるよう少し強めにしている。
        static readonly MotionPreset CalmMotion = new MotionPreset(35f, 0.30f, 1.35f, 1.35f);
        static readonly MotionPreset RippleMotion = new MotionPreset(0f, 0f, 0.55f, 1.15f);
        static readonly MotionPreset StreamMotion = new MotionPreset(0f, 0.55f, 1.45f, 1.8f);
        static readonly MotionPreset PoolMotion = new MotionPreset(50f, 0.24f, 0.55f, 1.08f);
        static readonly MotionPreset CyberMotion = new MotionPreset(18f, 0.34f, 0.78f, 1.24f);

        static readonly LookPreset ClearSeaLook = new LookPreset
        {
            assetName = "ClearSea",
            displayName = "美しい海",
            sourceGuid = CalmGuid,
            sourceLabel = "Calm",
            motion = new MotionPreset(22f, 0.30f, 0.92f, 1.45f),
            transparent = true,
            opacity = 0.52f,
            shallow = new Color(0.22f, 0.82f, 0.94f, 1f),
            deep = new Color(0.005f, 0.12f, 0.34f, 1f),
            horizon = new Color(0.72f, 0.92f, 1f, 1f),
            transmission = new Color(0.36f, 0.94f, 1f, 1f),
            glimmer = new Color(0.92f, 0.99f, 1f, 1f),
            normalStrength = 0.92f,
            tiling1 = 1.35f,
            tiling2 = 3.1f,
            alphaFresnel = 0.68f,
            alphaPower = 2.35f,
            edgeReflection = 0.58f,
            transmissionStrength = 0.58f,
            glimmerIntensity = 0.20f,
            glimmerSharpness = 13f,
            glintIntensity = 0.55f,
            glintPower = 220f,
            specPower = 220f,
            specIntensity = 1.05f,
            fresnelPower = 2.8f,
            reflStrength = 0.92f,
            smoothness = 0.96f,
            macroVariation = 0.46f,
            macroScale = 0.10f,
            macroDirectionBreakup = 0.40f,
            macroColorVariation = 0.18f,
        };

        static readonly LookPreset ClearPoolLook = new LookPreset
        {
            assetName = "ClearPool",
            displayName = "透明プール",
            sourceGuid = PoolGuid,
            sourceLabel = "Pool",
            motion = new MotionPreset(50f, 0.18f, 0.38f, 1.0f),
            transparent = true,
            opacity = 0.42f,
            shallow = new Color(0.70f, 0.98f, 1f, 1f),
            deep = new Color(0.08f, 0.42f, 0.62f, 1f),
            horizon = new Color(0.86f, 0.98f, 1f, 1f),
            transmission = new Color(0.54f, 0.98f, 1f, 1f),
            glimmer = new Color(0.98f, 1f, 1f, 1f),
            normalStrength = 0.40f,
            tiling1 = 0.95f,
            tiling2 = 2.15f,
            alphaFresnel = 0.62f,
            alphaPower = 2.0f,
            edgeReflection = 0.45f,
            transmissionStrength = 0.64f,
            glimmerIntensity = 0.16f,
            glimmerSharpness = 18f,
            glintIntensity = 0.36f,
            glintPower = 260f,
            specPower = 260f,
            specIntensity = 0.82f,
            fresnelPower = 3.0f,
            reflStrength = 0.72f,
            smoothness = 0.94f,
            macroVariation = 0.24f,
            macroScale = 0.08f,
            macroDirectionBreakup = 0.22f,
            macroColorVariation = 0.10f,
        };

        static readonly LookPreset BloodSeaLook = new LookPreset
        {
            assetName = "BloodSea",
            displayName = "血の海",
            sourceGuid = StreamGuid,
            sourceLabel = "Stream",
            motion = new MotionPreset(6f, 0.22f, 0.82f, 1.18f),
            transparent = true,
            opacity = 0.68f,
            shallow = new Color(0.48f, 0.02f, 0.025f, 1f),
            deep = new Color(0.055f, 0.0f, 0.006f, 1f),
            horizon = new Color(0.36f, 0.04f, 0.035f, 1f),
            transmission = new Color(0.72f, 0.06f, 0.035f, 1f),
            glimmer = new Color(1f, 0.28f, 0.16f, 1f),
            normalStrength = 0.78f,
            tiling1 = 1.15f,
            tiling2 = 2.35f,
            alphaFresnel = 0.18f,
            alphaPower = 2.9f,
            edgeReflection = 0.30f,
            transmissionStrength = 0.18f,
            glimmerIntensity = 0.08f,
            glimmerSharpness = 9f,
            glintIntensity = 0.18f,
            glintPower = 130f,
            specPower = 120f,
            specIntensity = 0.52f,
            fresnelPower = 4.2f,
            reflStrength = 0.35f,
            smoothness = 0.86f,
            macroVariation = 0.52f,
            macroScale = 0.09f,
            macroDirectionBreakup = 0.42f,
            macroColorVariation = 0.28f,
        };

        static readonly LookPreset LiquidMetalLook = new LookPreset
        {
            assetName = "LiquidMetal",
            displayName = "液体金属",
            sourceGuid = CyberGuid,
            sourceLabel = "Cyber",
            motion = new MotionPreset(18f, 0.30f, 0.72f, 1.38f),
            transparent = false,
            opacity = 1f,
            shallow = new Color(0.86f, 0.88f, 0.90f, 1f),
            deep = new Color(0.16f, 0.17f, 0.18f, 1f),
            horizon = new Color(0.95f, 0.97f, 1f, 1f),
            transmission = new Color(0.70f, 0.76f, 0.82f, 1f),
            glimmer = new Color(1f, 1f, 1f, 1f),
            normalStrength = 0.72f,
            tiling1 = 1.1f,
            tiling2 = 2.45f,
            alphaFresnel = 0f,
            alphaPower = 3.4f,
            edgeReflection = 0.95f,
            transmissionStrength = 0f,
            glimmerIntensity = 0.12f,
            glimmerSharpness = 22f,
            glintIntensity = 1.2f,
            glintPower = 360f,
            specPower = 380f,
            specIntensity = 1.65f,
            fresnelPower = 1.6f,
            reflStrength = 1f,
            smoothness = 0.99f,
            macroVariation = 0.40f,
            macroScale = 0.12f,
            macroDirectionBreakup = 0.24f,
            macroColorVariation = 0.08f,
        };

        // PrebakedPack/Materials/*.mat.meta の固定 GUID
        const string CalmGuid = "a171aabb01c34e01a1b2c3d4e5f60201";
        const string RippleGuid = "a171aabb01c34e01a1b2c3d4e5f60202";
        const string StreamGuid = "a171aabb01c34e01a1b2c3d4e5f60203";
        const string PoolGuid = "a171aabb01c34e01a1b2c3d4e5f60204";
        const string CyberGuid = "a171aabb01c34e01a1b2c3d4e5f60205";

        const string MenuRoot = "GameObject/Siliq Water/水マテリアルを適用/";
        const string TransparentMenuRoot = "GameObject/Siliq Water/透明な水マテリアルを適用 (PC)/";
        const string MobileTransparentMenuRoot = "GameObject/Siliq Water/透明な水マテリアルを適用 (iOS/Mobile)/";
        const string LookMenuRoot = "GameObject/Siliq Water/用途別マテリアルを適用/";
        const float PcSiliqTransparentOpacity = 0.52f;
        const float StandardTransparentFallbackOpacity = 0.68f;
        const float MobileTransparentOpacity = 0.38f;

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

        [MenuItem(LookMenuRoot + "美しい海 (Clear Sea)", false, 40)]
        static void ApplyClearSeaLook() => ApplyLook(ClearSeaLook);

        [MenuItem(LookMenuRoot + "透明プール (Clear Pool)", false, 41)]
        static void ApplyClearPoolLook() => ApplyLook(ClearPoolLook);

        [MenuItem(LookMenuRoot + "血の海 (Blood Sea)", false, 42)]
        static void ApplyBloodSeaLook() => ApplyLook(BloodSeaLook);

        [MenuItem(LookMenuRoot + "液体金属 (Liquid Metal)", false, 43)]
        static void ApplyLiquidMetalLook() => ApplyLook(LiquidMetalLook);

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
        [MenuItem(LookMenuRoot + "美しい海 (Clear Sea)", true)]
        [MenuItem(LookMenuRoot + "透明プール (Clear Pool)", true)]
        [MenuItem(LookMenuRoot + "血の海 (Blood Sea)", true)]
        [MenuItem(LookMenuRoot + "液体金属 (Liquid Metal)", true)]
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

            string texturePropertyName = mat != null && mat.HasProperty("_NormalMap") ? "_NormalMap" : "_BumpMap";
            ApplyMaterialToSelection(mat, motion, texturePropertyName);
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

        static void ApplyLook(LookPreset preset)
        {
            var source = LoadPrebakedMaterial(preset.sourceGuid, $"M_Water_{preset.sourceLabel}");
            if (source == null)
            {
                EditorUtility.DisplayDialog("Siliq Water",
                    $"M_Water_{preset.sourceLabel} が見つかりませんでした。\nPrebakedPack フォルダがプロジェクトに含まれているか確認してください。", "OK");
                return;
            }

            var mat = GetOrCreateLookMaterial(preset, source);
            if (mat == null)
            {
                EditorUtility.DisplayDialog("Siliq Water",
                    "Siliq 水シェーダーが見つかりませんでした。パッケージが正しく読み込まれているか確認してください。", "OK");
                return;
            }

            ApplyMaterialToSelection(mat, preset.motion, "_NormalMap");
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
                animator.SyncLookFromMaterial();
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
            if (!IsUsableShader(shader)) return null;

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
                SetupSiliqMobileTransparent(mat, mobileTransparent ? MobileTransparentOpacity : PcSiliqTransparentOpacity);
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
            Shader waterShader = IsUniversalPipelineActive()
                ? Shader.Find("Siliq/Water URP")
                : Shader.Find("Siliq/Water Mobile (Quest)");
            if (IsUsableShader(waterShader))
            {
                if (mat == null)
                {
                    mat = new Material(waterShader);
                    AssetDatabase.CreateAsset(mat, path);
                }
                else
                {
                    mat.shader = waterShader;
                }

                mat.name = $"M_Water_{label}_Transparent";
                ApplySiliqNormal(mat, source);
                ApplySiliqWaterPalette(mat, source);
                SetupMacroVariation(mat, 0.42f, 0.10f, 0.36f, 0.18f);
                SetupSiliqMobileTransparent(mat, PcSiliqTransparentOpacity);
                EditorUtility.SetDirty(mat);
                AssetDatabase.SaveAssets();
                return mat;
            }

            Shader urpLit = IsUniversalPipelineActive() ? Shader.Find("Universal Render Pipeline/Lit") : null;
            if (IsUsableShader(urpLit))
            {
                if (mat == null)
                {
                    mat = new Material(urpLit);
                    AssetDatabase.CreateAsset(mat, path);
                }
                else
                {
                    mat.shader = urpLit;
                }

                mat.name = $"M_Water_{label}_Transparent";
                ApplyLitNormalAndColor(mat, source, StandardTransparentFallbackOpacity);
                SetupUrpLitTransparent(mat, StandardTransparentFallbackOpacity);
                EditorUtility.SetDirty(mat);
                AssetDatabase.SaveAssets();
                return mat;
            }

            if (mat == null)
            {
                mat = new Material(source);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (source != null)
            {
                EditorUtility.CopySerialized(source, mat);
            }

            mat.name = $"M_Water_{label}_Transparent";
            SetupStandardTransparent(mat, StandardTransparentFallbackOpacity);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        static Material GetOrCreateMobileTransparentMaterial(Material source, string label)
        {
            Shader shader = Shader.Find("Siliq/Water Mobile (Quest)");
            if (!IsUsableShader(shader)) return null;

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

        static Material GetOrCreateLookMaterial(LookPreset preset, Material source)
        {
            Shader shader = FindBestSiliqLookShader();
            if (!IsUsableShader(shader)) return null;

            const string root = "Assets/SiliqWater";
            const string folder = root + "/GeneratedMaterials";
            EnsureFolder("Assets", "SiliqWater");
            EnsureFolder(root, "GeneratedMaterials");

            string path = $"{folder}/M_Water_Look_{preset.assetName}.mat";
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

            mat.name = $"M_Water_Look_{preset.assetName}";
            SetupSiliqLook(mat, preset, source);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        static Shader FindBestSiliqLookShader()
        {
            if (IsUniversalPipelineActive())
            {
                Shader urp = Shader.Find("Siliq/Water URP");
                if (IsUsableShader(urp)) return urp;
            }

            Shader mobile = Shader.Find("Siliq/Water Mobile (Quest)");
            if (IsUsableShader(mobile)) return mobile;
            return null;
        }

        static bool IsUsableShader(Shader shader)
        {
            return shader != null && shader.isSupported;
        }

        static bool IsUniversalPipelineActive()
        {
            var pipeline = GraphicsSettings.renderPipelineAsset;
            if (pipeline == null) return false;

            string typeName = pipeline.GetType().Name;
            return typeName.Contains("Universal") || typeName.Contains("URP");
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

        static void ApplyLitNormalAndColor(Material mat, Material source, float opacity)
        {
            if (mat == null) return;

            if (source != null && source.HasProperty("_BumpMap") && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
                mat.EnableKeyword("_NORMALMAP");
            }
            if (source != null && source.HasProperty("_BumpScale") && mat.HasProperty("_BumpScale"))
            {
                mat.SetFloat("_BumpScale", Mathf.Max(1f, source.GetFloat("_BumpScale")));
            }

            Color color = new Color(0.08f, 0.34f, 0.45f, Mathf.Clamp01(opacity));
            if (source != null && source.HasProperty("_Color"))
            {
                Color sourceColor = source.GetColor("_Color");
                color = new Color(sourceColor.r, sourceColor.g, sourceColor.b, Mathf.Clamp01(opacity));
            }

            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.92f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.92f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        }

        static void SetupUrpLitTransparent(Material mat, float opacity)
        {
            if (mat == null) return;

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = Mathf.Clamp01(opacity);
                mat.SetColor("_BaseColor", c);
            }

            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
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

        static void SetupSiliqBlend(Material mat, bool transparent, float opacity)
        {
            if (mat == null) return;

            if (mat.HasProperty("_Opacity")) mat.SetFloat("_Opacity", Mathf.Clamp01(opacity));

            if (!mat.HasProperty("_SrcBlend") || !mat.HasProperty("_DstBlend") || !mat.HasProperty("_ZWrite"))
            {
                return;
            }

            if (transparent)
            {
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                mat.SetFloat("_SrcBlend", (float)BlendMode.One);
                mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
                mat.SetFloat("_ZWrite", 1f);
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.renderQueue = (int)RenderQueue.Geometry;
            }
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

        static void SetupSiliqLook(Material mat, LookPreset preset, Material source)
        {
            if (mat == null) return;

            ApplySiliqNormal(mat, source);
            SetupSiliqBlend(mat, preset.transparent, preset.opacity);

            if (mat.HasProperty("_ShallowColor")) mat.SetColor("_ShallowColor", preset.shallow);
            if (mat.HasProperty("_DeepColor")) mat.SetColor("_DeepColor", preset.deep);
            if (mat.HasProperty("_HorizonColor")) mat.SetColor("_HorizonColor", preset.horizon);
            if (mat.HasProperty("_TransmissionColor")) mat.SetColor("_TransmissionColor", preset.transmission);
            if (mat.HasProperty("_GlimmerColor")) mat.SetColor("_GlimmerColor", preset.glimmer);

            if (mat.HasProperty("_Opacity")) mat.SetFloat("_Opacity", Mathf.Clamp01(preset.opacity));
            if (mat.HasProperty("_NormalStrength")) mat.SetFloat("_NormalStrength", preset.normalStrength);
            if (mat.HasProperty("_Tiling1")) mat.SetFloat("_Tiling1", preset.tiling1);
            if (mat.HasProperty("_Tiling2")) mat.SetFloat("_Tiling2", preset.tiling2);
            if (mat.HasProperty("_AlphaFresnel")) mat.SetFloat("_AlphaFresnel", preset.alphaFresnel);
            if (mat.HasProperty("_AlphaPower")) mat.SetFloat("_AlphaPower", preset.alphaPower);
            if (mat.HasProperty("_EdgeReflection")) mat.SetFloat("_EdgeReflection", preset.edgeReflection);
            if (mat.HasProperty("_TransmissionStrength")) mat.SetFloat("_TransmissionStrength", preset.transmissionStrength);
            if (mat.HasProperty("_GlimmerIntensity")) mat.SetFloat("_GlimmerIntensity", preset.glimmerIntensity);
            if (mat.HasProperty("_GlimmerSharpness")) mat.SetFloat("_GlimmerSharpness", preset.glimmerSharpness);
            if (mat.HasProperty("_GlintIntensity")) mat.SetFloat("_GlintIntensity", preset.glintIntensity);
            if (mat.HasProperty("_GlintPower")) mat.SetFloat("_GlintPower", preset.glintPower);
            if (mat.HasProperty("_SpecPower")) mat.SetFloat("_SpecPower", preset.specPower);
            if (mat.HasProperty("_SpecIntensity")) mat.SetFloat("_SpecIntensity", preset.specIntensity);
            if (mat.HasProperty("_FresnelPower")) mat.SetFloat("_FresnelPower", preset.fresnelPower);
            if (mat.HasProperty("_ReflStrength")) mat.SetFloat("_ReflStrength", preset.reflStrength);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", preset.smoothness);

            if (mat.HasProperty("_Scroll1"))
            {
                Vector2 primary = MotionVector(preset.motion.dir, preset.motion.speed);
                mat.SetVector("_Scroll1", new Vector4(primary.x, primary.y, 0f, 0f));
            }
            if (mat.HasProperty("_Scroll2"))
            {
                Vector2 primary = MotionVector(preset.motion.dir + 92f, preset.motion.speed * 0.73f);
                mat.SetVector("_Scroll2", new Vector4(primary.x, primary.y, 0f, 0f));
            }

            SetupMacroVariation(mat, preset.macroVariation, preset.macroScale, preset.macroDirectionBreakup, preset.macroColorVariation);

            if (mat.HasProperty("_UseFlowMap")) mat.SetFloat("_UseFlowMap", 0f);
            if (mat.HasProperty("_UseShore")) mat.SetFloat("_UseShore", 0f);
            mat.DisableKeyword("_USE_FLOWMAP");
            mat.DisableKeyword("_SHORE_EFFECTS");
            mat.DisableKeyword("_USE_RIPPLES");
        }

        static Vector2 MotionVector(float directionDegrees, float speed)
        {
            float rad = directionDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed;
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
