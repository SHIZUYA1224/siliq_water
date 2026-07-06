using UnityEditor;
using UnityEngine;

namespace Siliq.Water.Editor
{
    /// <summary>
    /// 焼き済み水マテリアルを選択オブジェクトへワンクリック適用するメニュー。
    /// Hierarchy でオブジェクトを右クリック → Siliq Water → 好きな水を選ぶだけ。
    /// 適用と同時に UV スクロール用コンポーネントも付与し、静止画にならないようにする。
    /// </summary>
    public static class WaterPackQuickApply
    {
        // 各水の雰囲気に合わせた動き (方向degと速さ)
        static readonly (float dir, float speed) CalmMotion = (35f, 0.15f);
        static readonly (float dir, float speed) RippleMotion = (60f, 0.08f);
        static readonly (float dir, float speed) StreamMotion = (0f, 0.5f);
        static readonly (float dir, float speed) PoolMotion = (50f, 0.12f);
        static readonly (float dir, float speed) CyberMotion = (45f, 0.3f);

        // 素材系 (ガラス・金属) はほぼ動かないので速度低め
        static readonly (float dir, float speed) StillMotion = (30f, 0.03f);
        static readonly (float dir, float speed) FlowSlow = (30f, 0.06f);

        // PrebakedPack/Materials/*.mat.meta の固定 GUID
        const string CalmGuid = "a171aabb01c34e01a1b2c3d4e5f60201";
        const string RippleGuid = "a171aabb01c34e01a1b2c3d4e5f60202";
        const string StreamGuid = "a171aabb01c34e01a1b2c3d4e5f60203";
        const string PoolGuid = "a171aabb01c34e01a1b2c3d4e5f60204";
        const string CyberGuid = "a171aabb01c34e01a1b2c3d4e5f60205";
        const string ShallowsGuid = "a171aabb01c34e01a1b2c3d4e5f60206";
        const string FlutedGuid = "a171aabb01c34e01a1b2c3d4e5f60207";
        const string LiquidMetalGuid = "a171aabb01c34e01a1b2c3d4e5f60208";
        const string FrostedGuid = "a171aabb01c34e01a1b2c3d4e5f60209";
        const string CondensationGuid = "a171aabb01c34e01a1b2c3d4e5f60210";
        const string KaleidoscopeGuid = "a171aabb01c34e01a1b2c3d4e5f60211";

        const string MenuRoot = "GameObject/Siliq Water/水マテリアルを適用/";

        [MenuItem(MenuRoot + "静かな水面 (Calm)", false, 10)]
        static void ApplyCalm() => Apply(CalmGuid, "Calm", CalmMotion);

        [MenuItem(MenuRoot + "波紋 (Ripple)", false, 11)]
        static void ApplyRipple() => Apply(RippleGuid, "Ripple", RippleMotion);

        [MenuItem(MenuRoot + "流れ (Stream)", false, 12)]
        static void ApplyStream() => Apply(StreamGuid, "Stream", StreamMotion);

        [MenuItem(MenuRoot + "プール (Pool)", false, 13)]
        static void ApplyPool() => Apply(PoolGuid, "Pool", PoolMotion);

        [MenuItem(MenuRoot + "サイバー (Cyber)", false, 14)]
        static void ApplyCyber() => Apply(CyberGuid, "Cyber", CyberMotion);

        // --- 素材系 (透明・ガラス・金属) ---
        [MenuItem(MenuRoot + "透明な浅瀬 (Shallows)", false, 30)]
        static void ApplyShallows() => Apply(ShallowsGuid, "Shallows", FlowSlow);

        [MenuItem(MenuRoot + "リブガラス (FlutedGlass)", false, 31)]
        static void ApplyFluted() => Apply(FlutedGuid, "FlutedGlass", StillMotion);

        [MenuItem(MenuRoot + "液体金属 (LiquidMetal)", false, 32)]
        static void ApplyLiquidMetal() => Apply(LiquidMetalGuid, "LiquidMetal", FlowSlow);

        [MenuItem(MenuRoot + "マットガラス (FrostedGlass)", false, 33)]
        static void ApplyFrosted() => Apply(FrostedGuid, "FrostedGlass", StillMotion);

        [MenuItem(MenuRoot + "結露ガラス (Condensation)", false, 34)]
        static void ApplyCondensation() => Apply(CondensationGuid, "Condensation", StillMotion);

        [MenuItem(MenuRoot + "万華鏡 (Kaleidoscope)", false, 35)]
        static void ApplyKaleidoscope() => Apply(KaleidoscopeGuid, "Kaleidoscope", StillMotion);

        [MenuItem(MenuRoot + "静かな水面 (Calm)", true)]
        [MenuItem(MenuRoot + "波紋 (Ripple)", true)]
        [MenuItem(MenuRoot + "流れ (Stream)", true)]
        [MenuItem(MenuRoot + "プール (Pool)", true)]
        [MenuItem(MenuRoot + "サイバー (Cyber)", true)]
        [MenuItem(MenuRoot + "透明な浅瀬 (Shallows)", true)]
        [MenuItem(MenuRoot + "リブガラス (FlutedGlass)", true)]
        [MenuItem(MenuRoot + "液体金属 (LiquidMetal)", true)]
        [MenuItem(MenuRoot + "マットガラス (FrostedGlass)", true)]
        [MenuItem(MenuRoot + "結露ガラス (Condensation)", true)]
        [MenuItem(MenuRoot + "万華鏡 (Kaleidoscope)", true)]
        static bool ValidateSelection()
        {
            foreach (var go in Selection.gameObjects)
            {
                if (go.GetComponent<Renderer>() != null) return true;
            }
            return false;
        }

        static void Apply(string guid, string label, (float dir, float speed) motion)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                EditorUtility.DisplayDialog("Siliq Water",
                    $"M_Water_{label} が見つかりませんでした。\nPrebakedPack フォルダがプロジェクトに含まれているか確認してください。", "OK");
                return;
            }

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
                animator.texturePropertyName = "_BumpMap";
                animator.directionDegrees = motion.dir;
                animator.speed = motion.speed;

                applied++;
            }

            if (applied == 0)
            {
                EditorUtility.DisplayDialog("Siliq Water",
                    "Renderer を持つオブジェクトを選択してから実行してください。", "OK");
            }
        }
    }
}
