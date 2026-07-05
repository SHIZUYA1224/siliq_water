using UnityEditor;
using UnityEngine;

namespace Siliq.Water.Editor
{
    /// <summary>
    /// 焼き済み水マテリアルを選択オブジェクトへワンクリック適用するメニュー。
    /// Hierarchy でオブジェクトを右クリック → Siliq Water → 好きな水を選ぶだけ。
    /// </summary>
    public static class WaterPackQuickApply
    {
        // PrebakedPack/Materials/*.mat.meta の固定 GUID
        const string CalmGuid = "a171aabb01c34e01a1b2c3d4e5f60201";
        const string RippleGuid = "a171aabb01c34e01a1b2c3d4e5f60202";
        const string StreamGuid = "a171aabb01c34e01a1b2c3d4e5f60203";
        const string PoolGuid = "a171aabb01c34e01a1b2c3d4e5f60204";
        const string CyberGuid = "a171aabb01c34e01a1b2c3d4e5f60205";

        const string MenuRoot = "GameObject/Siliq Water/水マテリアルを適用/";

        [MenuItem(MenuRoot + "静かな水面 (Calm)", false, 10)]
        static void ApplyCalm() => Apply(CalmGuid, "Calm");

        [MenuItem(MenuRoot + "波紋 (Ripple)", false, 11)]
        static void ApplyRipple() => Apply(RippleGuid, "Ripple");

        [MenuItem(MenuRoot + "流れ (Stream)", false, 12)]
        static void ApplyStream() => Apply(StreamGuid, "Stream");

        [MenuItem(MenuRoot + "プール (Pool)", false, 13)]
        static void ApplyPool() => Apply(PoolGuid, "Pool");

        [MenuItem(MenuRoot + "サイバー (Cyber)", false, 14)]
        static void ApplyCyber() => Apply(CyberGuid, "Cyber");

        [MenuItem(MenuRoot + "静かな水面 (Calm)", true)]
        [MenuItem(MenuRoot + "波紋 (Ripple)", true)]
        [MenuItem(MenuRoot + "流れ (Stream)", true)]
        [MenuItem(MenuRoot + "プール (Pool)", true)]
        [MenuItem(MenuRoot + "サイバー (Cyber)", true)]
        static bool ValidateSelection()
        {
            foreach (var go in Selection.gameObjects)
            {
                if (go.GetComponent<Renderer>() != null) return true;
            }
            return false;
        }

        static void Apply(string guid, string label)
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
