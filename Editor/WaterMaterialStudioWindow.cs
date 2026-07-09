using UnityEditor;
using UnityEngine;

namespace Siliq.Water.Editor
{
    /// <summary>
    /// Product-facing ready material browser. It never creates textures or materials.
    /// </summary>
    public sealed class WaterMaterialStudioWindow : EditorWindow
    {
        internal const string MenuPath = "Tools/Siliq Water/完成マテリアル";

        static readonly string[] TargetLabels = { "PC", "Quest", "iOS" };
        static readonly WaterPackQuickApply.ReadyLook[] Looks =
        {
            WaterPackQuickApply.ReadyLook.CrystalLagoonHero,
            WaterPackQuickApply.ReadyLook.CrystalLagoon,
            WaterPackQuickApply.ReadyLook.ClearPool,
            WaterPackQuickApply.ReadyLook.IndoorBluePool,
            WaterPackQuickApply.ReadyLook.ClearSea,
            WaterPackQuickApply.ReadyLook.WaterTable,
            WaterPackQuickApply.ReadyLook.FlagshipCrystal,
            WaterPackQuickApply.ReadyLook.BloodSea,
            WaterPackQuickApply.ReadyLook.LiquidMetal,
        };

        int targetIndex;
        int lookIndex;
        Vector2 scroll;

        void OnEnable()
        {
            switch (WaterPackQuickApply.TargetForActiveBuild())
            {
                case WaterPackQuickApply.TargetPlatform.Quest:
                    targetIndex = 1;
                    break;
                case WaterPackQuickApply.TargetPlatform.Ios:
                    targetIndex = 2;
                    break;
                default:
                    targetIndex = 0;
                    break;
            }
        }

        [MenuItem(MenuPath, false, -90)]
        public static void Open()
        {
            var window = GetWindow<WaterMaterialStudioWindow>("Siliq Water");
            window.minSize = new Vector2(440f, 390f);
            window.Show();
        }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawHeader();
            DrawTarget();
            DrawLook();
            DrawReadyMaterial();
            DrawApply();
            EditorGUILayout.EndScrollView();
        }

        void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("完成マテリアル", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "用途を選び、同梱済みの完成Materialをそのまま水面へ設定します。TextureやMaterialは自動生成しません。",
                EditorStyles.wordWrappedLabel);
            GUILayout.Space(8);
        }

        void DrawTarget()
        {
            EditorGUILayout.LabelField("対象", EditorStyles.boldLabel);
            targetIndex = GUILayout.Toolbar(targetIndex, TargetLabels, GUILayout.Height(30));
            EditorGUILayout.HelpBox(TargetDescription(TargetPlatform), MessageType.None);
            GUILayout.Space(8);
        }

        void DrawLook()
        {
            EditorGUILayout.LabelField("見た目", EditorStyles.boldLabel);
            string[] labels = new string[Looks.Length];
            for (int i = 0; i < Looks.Length; i++)
            {
                labels[i] = WaterPackQuickApply.DisplayNameForLook(Looks[i]);
            }

            lookIndex = EditorGUILayout.Popup(lookIndex, labels);
            EditorGUILayout.HelpBox(LookDescription(Looks[lookIndex]), MessageType.None);
            GUILayout.Space(8);
        }

        void DrawReadyMaterial()
        {
            Material material = SelectedReadyMaterial;
            EditorGUILayout.LabelField("使用するMaterial", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(material, typeof(Material), false);
            }

            using (new EditorGUI.DisabledScope(material == null))
            {
                if (GUILayout.Button("Projectで表示", GUILayout.Height(26)))
                {
                    Selection.activeObject = material;
                    EditorGUIUtility.PingObject(material);
                }
            }

            GUILayout.Space(8);
        }

        void DrawApply()
        {
            Material material = SelectedReadyMaterial;
            bool compatible = material != null && WaterShaderUtility.IsMaterialCompatibleWithCurrentPipeline(material);
            bool canApply = WaterPackQuickApply.HasRendererSelection() && compatible;
            using (new EditorGUI.DisabledScope(!canApply))
            {
                if (GUILayout.Button("選択中の水面へ設定", GUILayout.Height(42)))
                {
                    ApplySelected();
                }
            }

            if (material == null)
            {
                EditorGUILayout.HelpBox("同梱の完成Materialが見つかりません。Packageを再導入してください。", MessageType.Error);
            }
            else if (!compatible)
            {
                EditorGUILayout.HelpBox(
                    "現在のRender Pipelineではこの完成Materialを使用できません。URPの場合はPackage ManagerからURP Shader SampleをImportしてください。",
                    MessageType.Error);
            }
            else if (!WaterPackQuickApply.HasRendererSelection())
            {
                EditorGUILayout.HelpBox("Renderer を持つ水面オブジェクトを選択してください。", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Package内の完成Materialを直接割り当てます。PC / Quest / iOS差分はMaterialを複製せず、対象Rendererの設定として反映します。",
                    MessageType.Info);
            }
        }

        void ApplySelected()
        {
            WaterPackQuickApply.ApplyReadyLookToSelection(Looks[lookIndex], TargetPlatform);
        }

        WaterPackQuickApply.TargetPlatform TargetPlatform
        {
            get
            {
                if (targetIndex == 1) return WaterPackQuickApply.TargetPlatform.Quest;
                if (targetIndex == 2) return WaterPackQuickApply.TargetPlatform.Ios;
                return WaterPackQuickApply.TargetPlatform.PC;
            }
        }

        Material SelectedReadyMaterial => WaterPackQuickApply.ReadyMaterialForLook(Looks[lookIndex]);

        static string TargetDescription(WaterPackQuickApply.TargetPlatform target)
        {
            switch (target)
            {
                case WaterPackQuickApply.TargetPlatform.Quest:
                    return "Quest向け: 完成Materialは共通の軽量1パスを使い、高さ・反射・細部を対象水面だけ抑えます。";
                case WaterPackQuickApply.TargetPlatform.Ios:
                    return "iOS向け: 透明感を残し、実高さと強い反射を対象水面だけ抑えます。";
                default:
                    return "PC向け: 完成Material本来の反射、透明感、ゆるい実高さを使います。";
            }
        }

        static string LookDescription(WaterPackQuickApply.ReadyLook look)
        {
            switch (look)
            {
                case WaterPackQuickApply.ReadyLook.WaterTable:
                    return "ガラス水盤・水テーブル向け。中央リング波、黒青い浅い水、強いエッジ反射を使います。";
                case WaterPackQuickApply.ReadyLook.CrystalLagoonHero:
                    return "最初に見るべき最高品質寄りの透明水。迷ったらこれ。";
                case WaterPackQuickApply.ReadyLook.CrystalLagoon:
                    return "浅い水、室内プール、リゾート水面向けの透明感。";
                case WaterPackQuickApply.ReadyLook.ClearSea:
                    return "綺麗な海向け。海らしい青緑、反射、奥行きを持たせます。";
                case WaterPackQuickApply.ReadyLook.ClearPool:
                    return "透明プール向け。凹凸を抑え、抜け感を優先します。水底光は別メッシュで足します。";
                case WaterPackQuickApply.ReadyLook.IndoorBluePool:
                    return "室内プール向け。窓反射と明るい青い水面を強めます。";
                case WaterPackQuickApply.ReadyLook.FlagshipCrystal:
                    return "製品デモ向け。PC確認用の高反射・高透明プリセット。";
                case WaterPackQuickApply.ReadyLook.BloodSea:
                    return "赤い液体、血の海向け。透明水とは違う濁りと重さ。";
                case WaterPackQuickApply.ReadyLook.LiquidMetal:
                    return "液体金属向け。不透明で反射を強く使う特殊液体。";
                default:
                    return string.Empty;
            }
        }
    }
}
