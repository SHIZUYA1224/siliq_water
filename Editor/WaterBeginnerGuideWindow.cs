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
        internal const string PlaceCrystalLagoonHeroCompleteActionLabel = "最高品質 Hero 完成セットを配置";
        internal const string PlaceSunlitPoolCompleteActionLabel = "透明プール完成セットを配置";
        internal const string CreateCrystalLagoonHeroActionLabel = "最高品質 Hero 水面を作成";
        internal const string CreateCrystalLagoonActionLabel = "クリスタルラグーン水面を作成";
        internal const string CreateFlagshipActionLabel = "フラッグシップ水面を作成";
        internal const string RepairSelectionActionLabel = "選択中の水面を診断して自動修復";
        internal const string CrystalLagoonShowcaseScenePath = "PrebakedPack/SampleScene/SC_CrystalLagoon_Showcase.unity";
        internal const string CrystalLagoonCompletePrefabPath = "PrebakedPack/Prefabs/PF_Siliq_CrystalLagoon_Complete.prefab";
        internal const string CrystalLagoonHeroCompletePrefabPath = "PrebakedPack/Prefabs/PF_Siliq_CrystalLagoon_Hero_Complete.prefab";
        internal const string SunlitPoolCompletePrefabPath = "PrebakedPack/Prefabs/PF_Siliq_SunlitPool_Complete.prefab";
        internal const string CrystalLagoonCompletePreviewPath = "PrebakedPack/Preview/preview_crystal_lagoon_complete.png";
        internal const string CrystalLagoonHeroCompletePreviewPath = "PrebakedPack/Preview/preview_crystal_lagoon_hero_complete.png";
        internal const string CrystalLagoonHeroMaterialPath = "PrebakedPack/ReadyMaterials/M_Siliq_CrystalLagoon_Hero_Ready.mat";

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
            if (GUILayout.Button(PlaceCrystalLagoonHeroCompleteActionLabel, GUILayout.Height(38)))
            {
                WaterBeginnerSetup.PlaceCrystalLagoonHeroCompletePrefab();
            }

            if (GUILayout.Button(PlaceSunlitPoolCompleteActionLabel, GUILayout.Height(34)))
            {
                WaterBeginnerSetup.PlaceSunlitPoolCompletePrefab();
            }

            if (GUILayout.Button(CreateCrystalLagoonHeroActionLabel, GUILayout.Height(34)))
            {
                WaterBeginnerSetup.CreateCrystalLagoonHeroWater();
            }

            if (GUILayout.Button(CreateCrystalLagoonActionLabel, GUILayout.Height(34)))
            {
                WaterBeginnerSetup.CreateCrystalLagoonWater();
            }

            if (GUILayout.Button(CreateFlagshipActionLabel, GUILayout.Height(30)))
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
            Bullet("美しさを最優先する場合は Hero 完成セット、プール用途なら透明プール完成セットを配置します。水面、明るい床、水底用 caustics overlay が別メッシュで入っています。");
            Bullet("水底やプール床の模様は水面ではなく、床や水底用の別メッシュに caustics overlay material を貼って作ります。");
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
            Bullet("水底の光を使う場合は、Quest/iOS でも水面 material に混ぜず、床や水底側の別メッシュを薄く重ねます。");
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
                if (GUILayout.Button("最高品質 Material を選択"))
                {
                    PingPackageAsset(CrystalLagoonHeroMaterialPath);
                }
                if (GUILayout.Button("完成プレビューを選択"))
                {
                    PingPackageAsset(CrystalLagoonCompletePreviewPath);
                }
                if (GUILayout.Button("Hero プレビューを選択"))
                {
                    PingPackageAsset(CrystalLagoonHeroCompletePreviewPath);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Crystal Lagoon シーンを選択"))
                {
                    PingPackageAsset(CrystalLagoonShowcaseScenePath);
                }
                if (GUILayout.Button("Hero Prefab を選択"))
                {
                    PingPackageAsset(CrystalLagoonHeroCompletePrefabPath);
                }
                if (GUILayout.Button("透明プール Prefab を選択"))
                {
                    PingPackageAsset(SunlitPoolCompletePrefabPath);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("通常 Prefab を選択"))
                {
                    PingPackageAsset(CrystalLagoonCompletePrefabPath);
                }
                if (GUILayout.Button("PrebakedPack を選択"))
                {
                    PingPackageAsset("PrebakedPack");
                }
                if (GUILayout.Button("ReadyMaterials を選択"))
                {
                    PingPackageAsset("PrebakedPack/ReadyMaterials");
                }
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
