using UnityEditor;
using UnityEngine;

namespace Siliq.Water.Editor
{
    /// <summary>
    /// Lightweight product-facing studio: choose target platform, choose a polished ready look, apply.
    /// The procedural generator remains under the Advanced menu.
    /// </summary>
    public sealed class WaterMaterialStudioWindow : EditorWindow
    {
        internal const string MenuPath = "Tools/Siliq Water/水面マップスタジオ";

        static readonly string[] TargetLabels = { "PC", "Quest", "iOS" };
        static readonly WaterPackQuickApply.ReadyLook[] Looks =
        {
            WaterPackQuickApply.ReadyLook.CrystalLagoonHero,
            WaterPackQuickApply.ReadyLook.CrystalLagoon,
            WaterPackQuickApply.ReadyLook.ClearSea,
            WaterPackQuickApply.ReadyLook.ClearPool,
            WaterPackQuickApply.ReadyLook.IndoorBluePool,
            WaterPackQuickApply.ReadyLook.FlagshipCrystal,
            WaterPackQuickApply.ReadyLook.BloodSea,
            WaterPackQuickApply.ReadyLook.LiquidMetal,
        };

        int targetIndex;
        int lookIndex;
        bool addSurfaceFx;
        bool addUnderwaterFx;
        Vector2 scroll;

        [MenuItem(MenuPath, false, -90)]
        public static void Open()
        {
            var window = GetWindow<WaterMaterialStudioWindow>("Siliq Water");
            window.minSize = new Vector2(420f, 420f);
            window.Show();
        }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawHeader();
            DrawTarget();
            DrawLook();
            DrawOptions();
            DrawApply();
            DrawAdvanced();
            EditorGUILayout.EndScrollView();
        }

        void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("水面マップスタジオ", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "選択中の水面に、完成Materialを貼って反映します。重い自由生成は通常使いません。",
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

        void DrawOptions()
        {
            EditorGUILayout.LabelField("追加オプション", EditorStyles.boldLabel);
            addSurfaceFx = EditorGUILayout.ToggleLeft("水面FXを追加する（水しぶき、泡、光、広がる雨波紋）", addSurfaceFx);
            addUnderwaterFx = EditorGUILayout.ToggleLeft("水中・水槽FX空間を追加する（霞、粒子、光筋、泡柱）", addUnderwaterFx);
            GUILayout.Space(8);
        }

        void DrawApply()
        {
            bool canApply = WaterPackQuickApply.HasRendererSelection();
            using (new EditorGUI.DisabledScope(!canApply))
            {
                if (GUILayout.Button("選択中の水面へ反映", GUILayout.Height(42)))
                {
                    ApplySelected();
                }
            }

            if (!canApply)
            {
                EditorGUILayout.HelpBox("Renderer を持つ水面オブジェクトを選択してください。", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("共有Materialは直接汚さず、必要なMaterialを生成してAnimatorも設定します。", MessageType.Info);
            }
        }

        void DrawAdvanced()
        {
            GUILayout.Space(12);
            EditorGUILayout.LabelField("上級者向け", EditorStyles.boldLabel);
            if (GUILayout.Button("手続き生成スタジオを開く", GUILayout.Height(28)))
            {
                WaterMapStudioWindow.Open();
            }
        }

        void ApplySelected()
        {
            var originalSelection = Selection.objects;
            WaterPackQuickApply.ApplyReadyLookToSelection(Looks[lookIndex], TargetPlatform);
            if (addSurfaceFx)
            {
                Selection.objects = originalSelection;
                WaterBeginnerSetup.PlaceMobileWaterFxSet();
            }
            if (addUnderwaterFx)
            {
                Selection.objects = originalSelection;
                WaterBeginnerSetup.PlaceUnderwaterFxVolumeSet();
            }
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

        static string TargetDescription(WaterPackQuickApply.TargetPlatform target)
        {
            switch (target)
            {
                case WaterPackQuickApply.TargetPlatform.Quest:
                    return "Quest向け: 高さ、反射パターン、細かい光を抑えた軽量設定。ワールド用の最初の基準です。";
                case WaterPackQuickApply.TargetPlatform.Ios:
                    return "iOS向け: 透明感を残しつつ、実高さと強い反射を抑えた軽量設定。";
                default:
                    return "PC向け: 反射、水底光、透明感を優先した見た目重視設定。";
            }
        }

        static string LookDescription(WaterPackQuickApply.ReadyLook look)
        {
            switch (look)
            {
                case WaterPackQuickApply.ReadyLook.CrystalLagoonHero:
                    return "最初に見るべき最高品質寄りの透明水。迷ったらこれ。";
                case WaterPackQuickApply.ReadyLook.CrystalLagoon:
                    return "浅い水、室内プール、リゾート水面向けの透明感。";
                case WaterPackQuickApply.ReadyLook.ClearSea:
                    return "綺麗な海向け。海らしい青緑、反射、奥行きを持たせます。";
                case WaterPackQuickApply.ReadyLook.ClearPool:
                    return "透明プール向け。凹凸を抑え、水底光と抜け感を優先します。";
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
