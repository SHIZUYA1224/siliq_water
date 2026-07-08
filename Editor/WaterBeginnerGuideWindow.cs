using UnityEditor;
using UnityEngine;

namespace Siliq.Water.Editor
{
    /// <summary>
    /// Unity / VRChat 初心者が最初に開くガイド。作成、修復、確認先を 1 画面に集約する。
    /// </summary>
    public sealed class WaterBeginnerGuideWindow : EditorWindow
    {
        internal const string MenuPath = "Tools/Siliq Water/はじめてガイド";
        internal const string CreateFlagshipActionLabel = "フラッグシップ水面を作成";
        internal const string RepairSelectionActionLabel = "選択中の水面を診断して自動修復";

        Vector2 scroll;

        [MenuItem(MenuPath, false, -100)]
        public static void Open()
        {
            var window = GetWindow<WaterBeginnerGuideWindow>("Siliq Water");
            window.minSize = new Vector2(420f, 520f);
            window.Show();
        }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawHeader();
            DrawQuickActions();
            DrawBeginnerChecks();
            DrawVrChatNotes();
            DrawReferenceButtons();
            EditorGUILayout.EndScrollView();
        }

        static void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Siliq Water はじめてガイド", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "迷ったら上から順に押してください。水面作成、ピンク修復、動かない/高さが出ない問題をこの画面から処理できます。",
                EditorStyles.wordWrappedLabel);
            GUILayout.Space(8);
        }

        static void DrawQuickActions()
        {
            EditorGUILayout.LabelField("まずやること", EditorStyles.boldLabel);
            if (GUILayout.Button(CreateFlagshipActionLabel, GUILayout.Height(34)))
            {
                WaterBeginnerSetup.CreateFlagshipWater();
            }

            using (new EditorGUI.DisabledScope(Selection.gameObjects == null || Selection.gameObjects.Length == 0))
            {
                if (GUILayout.Button(RepairSelectionActionLabel, GUILayout.Height(30)))
                {
                    WaterBeginnerSetup.DiagnoseAndRepairSelection();
                }
            }

            EditorGUILayout.HelpBox(
                "既存 Plane / Quad は選択してから診断してください。頂点数が少ない場合は、実高さが見える分割メッシュへ交換します。",
                MessageType.Info);
            GUILayout.Space(8);
        }

        static void DrawBeginnerChecks()
        {
            EditorGUILayout.LabelField("水が安っぽく見える時の確認", EditorStyles.boldLabel);
            Bullet("すぐ使う場合は PrebakedPack/ReadyMaterials の M_Siliq_*_Ready をドラッグします。material だけで波が動きます。");
            Bullet("normal だけでは透明感は出ません。ReadyMaterials か用途別マテリアルで色、透明度、反射、ハイライトも設定します。");
            Bullet("高さはメッシュの頂点変位です。1 枚 Quad では見えないため、分割メッシュを使います。");
            Bullet("ピンク material は shader 不一致です。診断修復で現在の Render Pipeline に合う material へ差し替えます。");
            Bullet("編集中に重い場合は WaterSurfaceAnimator の Animate In Edit Mode を OFF、または Preview FPS を下げます。");
            GUILayout.Space(8);
        }

        static void DrawVrChatNotes()
        {
            EditorGUILayout.LabelField("VRChat / Quest / iOS", EditorStyles.boldLabel);
            Bullet("Quest アバターはカスタムシェーダー不可です。生成 normal PNG を VRChat/Mobile/Standard Lite の Normal Map に入れます。");
            Bullet("ワールドでは Siliq/Water Mobile (Quest) を優先します。透明・反射を強くしすぎると重くなります。");
            Bullet("PC 専用の見た目を作る場合は用途別プリセット、Quest/iOS では normal 解像度と透明描画を控えめにします。");
            GUILayout.Space(8);
        }

        static void DrawReferenceButtons()
        {
            EditorGUILayout.LabelField("確認する", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("README を開く"))
                {
                    OpenPackageAsset("README.md");
                }
                if (GUILayout.Button("用途別ガイドを開く"))
                {
                    OpenPackageAsset("Docs/MaterialLookPresetGuide.md");
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("サンプルシーンを選択"))
                {
                    PingPackageAsset("PrebakedPack/SampleScene/SC_WaterNormalMap_Preview.unity");
                }
                if (GUILayout.Button("PrebakedPack を選択"))
                {
                    PingPackageAsset("PrebakedPack");
                }
            }

            if (GUILayout.Button("ReadyMaterials を選択"))
            {
                PingPackageAsset("PrebakedPack/ReadyMaterials");
            }
        }

        static void Bullet(string text)
        {
            EditorGUILayout.LabelField("・" + text, EditorStyles.wordWrappedLabel);
        }

        static void OpenPackageAsset(string relativePath)
        {
            var asset = LoadPackageAsset(relativePath);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
                return;
            }

            EditorUtility.DisplayDialog("Siliq Water", relativePath + " が見つかりません。Package が正しく読み込まれているか確認してください。", "OK");
        }

        static void PingPackageAsset(string relativePath)
        {
            var asset = LoadPackageAsset(relativePath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
                return;
            }

            EditorUtility.DisplayDialog("Siliq Water", relativePath + " が見つかりません。Package が正しく読み込まれているか確認してください。", "OK");
        }

        static Object LoadPackageAsset(string relativePath)
        {
            string packagePath = "Packages/com.siliq.water-normalmap/" + relativePath;
            var asset = AssetDatabase.LoadAssetAtPath<Object>(packagePath);
            if (asset != null) return asset;

            string assetPath = "Assets/SiliqWater/" + relativePath;
            return AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        }
    }
}
