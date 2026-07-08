using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Siliq.Water.Editor
{
    /// <summary>
    /// 水面マップ生成スタジオ。
    /// メニュー: Tools > Siliq Water > 水面マップスタジオ
    /// </summary>
    public class WaterMapStudioWindow : EditorWindow
    {
        enum GoalPreset
        {
            ClearSea,
            ClearPool,
            IndoorBluePool,
            FlagshipCrystal,
            BloodSea,
            LiquidMetal,
        }

        const string PrefsKey = "Siliq.Water.Settings.v2";
        const int IndoorBluePoolPresetIndex = 10;
        const int FlagshipCrystalPresetIndex = 11;
        const int PreviewResolution = 256;
        const float PreviewAnimationFps = 12f;
        const float PreviewRegenerateMinInterval = 0.12f;
        const float WideLayoutThreshold = 820f;
        const float LeftPaneMinWidth = 360f;
        const float LeftPaneMaxWidth = 520f;
        const float PaneGap = 10f;

        static readonly WaterMapType[] MapTypes =
        {
            WaterMapType.Normal, WaterMapType.Height, WaterMapType.Foam, WaterMapType.Roughness,
            WaterMapType.Flow, WaterMapType.Dudv, WaterMapType.Caustics,
        };
        static readonly string[] MapTabLabels = { "ノーマル", "ハイト", "フォーム", "ラフネス", "フロー", "DUDV", "コースティクス" };
        static readonly string[] MapSuffixes = { "Normal", "Height", "Foam", "Roughness", "Flow", "DUDV", "Caustics" };

        static readonly int[] ResolutionOptions = { 256, 512, 1024, 2048, 4096 };
        static readonly string[] ResolutionLabels = { "256", "512", "1024", "2048", "4096" };

        static readonly string[] MaterialShaderLabels =
        {
            "作成しない",
            "Standard (ビルトイン)",
            "Universal Render Pipeline/Lit (URP)",
            "Siliq/Water Mobile (Quest / iOS)",
            "Siliq/Water URP (Sample導入時)",
        };

        [SerializeField] WaterMapSettings settings;
        [SerializeField] int previewMapIndex;
        [SerializeField] bool[] exportMapFlags = { true, false, false, false, false, false, false };
        [SerializeField] int materialShaderIndex;

        WaterMapProfile profileAsset;
        Texture2D previewTexture;
        bool previewDirty = true;
        bool previewTiled;
        float previewTime;
        bool animatePreview;
        double lastAnimTime;
        double nextPreviewRegenerateTime;

        int presetIndex;
        Vector2 scroll;
        readonly List<bool> layerFoldouts = new List<bool>();
        bool mapSettingsFoldout;

        [MenuItem("Tools/Siliq Water/水面マップスタジオ")]
        public static void Open()
        {
            var window = GetWindow<WaterMapStudioWindow>("水面マップスタジオ");
            window.minSize = new Vector2(720f, 560f);
        }

        void OnEnable()
        {
            LoadSettings();
            if (exportMapFlags == null || exportMapFlags.Length != MapTypes.Length)
            {
                exportMapFlags = new bool[MapTypes.Length];
                exportMapFlags[0] = true;
            }
            previewDirty = true;
            string selectedShaderName = ShaderNameForMaterialIndex(materialShaderIndex);
            if (materialShaderIndex > 0 &&
                (string.IsNullOrEmpty(selectedShaderName) || !IsUsableShader(Shader.Find(selectedShaderName))))
            {
                materialShaderIndex = BestFallbackMaterialShaderIndex();
            }
            EditorApplication.update += OnEditorUpdate;
        }

        void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            SaveSettings();
            DestroyPreview();
        }

        void OnEditorUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            bool shouldRepaint = false;

            if (animatePreview)
            {
                double interval = 1.0 / PreviewAnimationFps;
                if (lastAnimTime <= 0 || now - lastAnimTime >= interval)
                {
                    float dt = lastAnimTime > 0 ? Mathf.Min((float)(now - lastAnimTime), (float)interval * 2f) : (float)interval;
                    lastAnimTime = now;
                    previewTime = (previewTime + dt * 0.15f) % 1f;
                    previewDirty = true;
                    shouldRepaint = true;
                }
            }
            else
            {
                lastAnimTime = 0;
                if (previewDirty && now >= nextPreviewRegenerateTime)
                {
                    shouldRepaint = true;
                }
            }

            if (shouldRepaint)
            {
                Repaint();
            }
        }

        // ---------------------------------------------------------------
        // 設定の保存 / 読み込み
        // ---------------------------------------------------------------

        void LoadSettings()
        {
            string json = EditorPrefs.GetString(PrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    settings = JsonUtility.FromJson<WaterMapSettings>(json);
                }
                catch
                {
                    settings = null;
                }
            }
            if (settings == null || settings.layers == null || settings.layers.Length == 0)
            {
                settings = WaterMapPresets.Create(0);
            }
        }

        void SaveSettings()
        {
            if (settings != null)
            {
                EditorPrefs.SetString(PrefsKey, JsonUtility.ToJson(settings));
            }
        }

        // ---------------------------------------------------------------
        // GUI
        // ---------------------------------------------------------------

        void OnGUI()
        {
            if (settings == null)
            {
                settings = WaterMapPresets.Create(0);
            }

            if (position.width >= WideLayoutThreshold)
            {
                DrawWideLayout();
            }
            else
            {
                DrawCompactLayout();
            }

            if (previewDirty &&
                Event.current.type == EventType.Repaint &&
                EditorApplication.timeSinceStartup >= nextPreviewRegenerateTime)
            {
                RegeneratePreview();
            }
        }

        void DrawWideLayout()
        {
            float leftWidth = Mathf.Clamp(position.width * 0.38f, LeftPaneMinWidth, LeftPaneMaxWidth);
            float previewPaneWidth = Mathf.Max(280f, position.width - leftWidth - PaneGap - 18f);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(leftWidth)))
                {
                    DrawControlScroll();
                }

                GUILayout.Space(PaneGap);

                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                {
                    DrawPreviewSection(previewPaneWidth, fixedPane: true);
                }
            }
        }

        void DrawCompactLayout()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawControlContents();
            GUILayout.Space(8f);
            DrawPreviewSection(EditorGUIUtility.currentViewWidth - 24f, fixedPane: false);
            EditorGUILayout.EndScrollView();
        }

        void DrawControlScroll()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            DrawControlContents();
            EditorGUILayout.EndScrollView();
        }

        void DrawControlContents()
        {
            EditorGUI.BeginChangeCheck();

            DrawPresetAndProfileSection();
            GUILayout.Space(8f);
            DrawGlobalSection();
            GUILayout.Space(8f);
            DrawMapSettingsSection();
            GUILayout.Space(8f);
            DrawLayersSection();
            GUILayout.Space(8f);
            DrawExportSection();

            if (EditorGUI.EndChangeCheck())
            {
                previewDirty = true;
                SaveSettings();
            }
        }

        // ---------------------------------------------------------------
        // プリセット & プロファイル
        // ---------------------------------------------------------------

        void DrawPresetAndProfileSection()
        {
            EditorGUILayout.LabelField("1. プリセット", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("目的から始める", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("綺麗な海", "透明感のある海向け。海の揺らぎ、透明 Siliq マテリアル、高品質書き出しをまとめて設定します。")))
                {
                    ApplyGoalPreset(GoalPreset.ClearSea);
                }
                if (GUILayout.Button(new GUIContent("透明プール", "浅い水と控えめな光網向け。弱い凹凸、透明度高め、プール用出力に設定します。")))
                {
                    ApplyGoalPreset(GoalPreset.ClearPool);
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("室内ブループール", "青い室内プール、窓反射、柔らかい白ハイライト向け。広い反射の揺らぎと透明 Siliq マテリアルに設定します。")))
                {
                    ApplyGoalPreset(GoalPreset.IndoorBluePool);
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("フラッグシップ透明水", "製品デモ向け。多層反射、細波、淡いコースティクス、4096px 書き出しの最高品質設定にします。")))
                {
                    ApplyGoalPreset(GoalPreset.FlagshipCrystal);
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("血の海", "重い赤い液体向け。粘度感のある流れ、濃い透明度、ラフネス/フロー出力に設定します。")))
                {
                    ApplyGoalPreset(GoalPreset.BloodSea);
                }
                if (GUILayout.Button(new GUIContent("液体金属", "水銀や SF 金属液向け。不透明、高反射、細い流線系の normal に設定します。")))
                {
                    ApplyGoalPreset(GoalPreset.LiquidMetal);
                }
            }

            GUILayout.Space(4f);
            EditorGUILayout.LabelField("生成レシピを直接選ぶ", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                presetIndex = EditorGUILayout.Popup(presetIndex, WaterMapPresets.Names);
                if (GUILayout.Button("適用", GUILayout.Width(60f)))
                {
                    settings = WaterMapPresets.Create(presetIndex);
                    layerFoldouts.Clear();
                    previewDirty = true;
                    SaveSettings();
                    GUI.FocusControl(null);
                }
            }

            EditorGUILayout.LabelField("プロファイル", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                profileAsset = (WaterMapProfile)EditorGUILayout.ObjectField(profileAsset, typeof(WaterMapProfile), false);
                using (new EditorGUI.DisabledScope(profileAsset == null))
                {
                    if (GUILayout.Button("読み込み", GUILayout.Width(70f)))
                    {
                        settings = profileAsset.settings.Clone();
                        layerFoldouts.Clear();
                        previewDirty = true;
                        SaveSettings();
                        GUI.FocusControl(null);
                    }
                    if (GUILayout.Button("上書き保存", GUILayout.Width(80f)))
                    {
                        Undo.RecordObject(profileAsset, "水面マッププロファイル更新");
                        profileAsset.settings = settings.Clone();
                        EditorUtility.SetDirty(profileAsset);
                        AssetDatabase.SaveAssets();
                    }
                }
                if (GUILayout.Button("新規保存", GUILayout.Width(70f)))
                {
                    SaveProfileAsNew();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("設定を JSON コピー"))
                {
                    EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(settings, true);
                    ShowNotification(new GUIContent("クリップボードへコピーしました"));
                }
                if (GUILayout.Button("JSON を貼り付け"))
                {
                    try
                    {
                        var loaded = JsonUtility.FromJson<WaterMapSettings>(EditorGUIUtility.systemCopyBuffer);
                        if (loaded != null && loaded.layers != null)
                        {
                            settings = loaded;
                            layerFoldouts.Clear();
                            previewDirty = true;
                            SaveSettings();
                        }
                    }
                    catch
                    {
                        ShowNotification(new GUIContent("JSON の読み込みに失敗しました"));
                    }
                }
            }
        }

        void ApplyGoalPreset(GoalPreset goal)
        {
            switch (goal)
            {
                case GoalPreset.ClearSea:
                    presetIndex = 1;
                    settings = WaterMapPresets.Create(presetIndex);
                    settings.resolution = 2048;
                    settings.strength = 0.82f;
                    settings.supersample = 2;
                    settings.exportExr = false;
                    settings.frameCount = 32;
                    settings.createTransparentMaterial = true;
                    settings.materialOpacity = 0.46f;
                    materialShaderIndex = BestSiliqMaterialShaderIndex();
                    previewMapIndex = 0;
                    SetExportMaps(WaterMapType.Normal, WaterMapType.Height, WaterMapType.Roughness, WaterMapType.Flow, WaterMapType.Dudv, WaterMapType.Caustics);
                    break;

                case GoalPreset.ClearPool:
                    presetIndex = 8;
                    settings = WaterMapPresets.Create(presetIndex);
                    settings.resolution = 2048;
                    settings.strength = 0.42f;
                    settings.supersample = 2;
                    settings.exportExr = false;
                    settings.frameCount = 24;
                    settings.createTransparentMaterial = true;
                    settings.materialOpacity = 0.34f;
                    materialShaderIndex = BestSiliqMaterialShaderIndex();
                    previewMapIndex = 0;
                    SetExportMaps(WaterMapType.Normal, WaterMapType.Height, WaterMapType.Roughness, WaterMapType.Caustics);
                    break;

                case GoalPreset.IndoorBluePool:
                    presetIndex = IndoorBluePoolPresetIndex;
                    settings = WaterMapPresets.Create(presetIndex);
                    settings.resolution = 2048;
                    settings.strength = 0.36f;
                    settings.supersample = 2;
                    settings.exportExr = false;
                    settings.frameCount = 24;
                    settings.createTransparentMaterial = true;
                    settings.materialOpacity = 0.38f;
                    materialShaderIndex = BestSiliqMaterialShaderIndex();
                    previewMapIndex = 0;
                    SetExportMaps(WaterMapType.Normal, WaterMapType.Height, WaterMapType.Roughness, WaterMapType.Dudv, WaterMapType.Caustics);
                    break;

                case GoalPreset.FlagshipCrystal:
                    presetIndex = FlagshipCrystalPresetIndex;
                    settings = WaterMapPresets.Create(presetIndex);
                    settings.resolution = 4096;
                    settings.strength = 0.72f;
                    settings.supersample = 2;
                    settings.exportExr = false;
                    settings.frameCount = 48;
                    settings.createTransparentMaterial = true;
                    settings.materialOpacity = 0.52f;
                    materialShaderIndex = BestSiliqMaterialShaderIndex();
                    previewMapIndex = 0;
                    SetExportMaps(WaterMapType.Normal, WaterMapType.Height, WaterMapType.Roughness, WaterMapType.Flow, WaterMapType.Dudv, WaterMapType.Caustics);
                    break;

                case GoalPreset.BloodSea:
                    presetIndex = 7;
                    settings = WaterMapPresets.Create(presetIndex);
                    settings.resolution = 2048;
                    settings.strength = 0.78f;
                    settings.supersample = 2;
                    settings.exportExr = false;
                    settings.frameCount = 32;
                    settings.createTransparentMaterial = true;
                    settings.materialOpacity = 0.68f;
                    materialShaderIndex = BestSiliqMaterialShaderIndex();
                    previewMapIndex = 0;
                    SetExportMaps(WaterMapType.Normal, WaterMapType.Height, WaterMapType.Roughness, WaterMapType.Flow, WaterMapType.Dudv);
                    break;

                case GoalPreset.LiquidMetal:
                    presetIndex = 9;
                    settings = WaterMapPresets.Create(presetIndex);
                    settings.resolution = 2048;
                    settings.strength = 0.62f;
                    settings.supersample = 2;
                    settings.exportExr = false;
                    settings.frameCount = 32;
                    settings.createTransparentMaterial = false;
                    settings.materialOpacity = 1f;
                    materialShaderIndex = BestSiliqMaterialShaderIndex();
                    previewMapIndex = 0;
                    SetExportMaps(WaterMapType.Normal, WaterMapType.Height, WaterMapType.Roughness, WaterMapType.Flow);
                    break;
            }

            mapSettingsFoldout = false;
            layerFoldouts.Clear();
            previewDirty = true;
            SaveSettings();
            GUI.FocusControl(null);
        }

        int BestSiliqMaterialShaderIndex()
        {
            return WaterShaderUtility.BestSiliqMaterialShaderIndex();
        }

        static bool IsUniversalPipelineActive()
        {
            return WaterShaderUtility.IsUniversalPipelineActive();
        }

        void SetExportMaps(params WaterMapType[] selectedTypes)
        {
            if (exportMapFlags == null || exportMapFlags.Length != MapTypes.Length)
            {
                exportMapFlags = new bool[MapTypes.Length];
            }

            for (int i = 0; i < exportMapFlags.Length; i++)
            {
                exportMapFlags[i] = false;
            }

            foreach (WaterMapType type in selectedTypes)
            {
                int index = Array.IndexOf(MapTypes, type);
                if (index >= 0) exportMapFlags[index] = true;
            }
        }

        void SaveProfileAsNew()
        {
            string path = EditorUtility.SaveFilePanelInProject("プロファイルを保存", "WaterMapProfile", "asset", "保存先を選択してください");
            if (string.IsNullOrEmpty(path)) return;

            var profile = ScriptableObject.CreateInstance<WaterMapProfile>();
            profile.settings = settings.Clone();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            profileAsset = profile;
            EditorGUIUtility.PingObject(profile);
        }

        // ---------------------------------------------------------------
        // 全体設定 & マップ別設定
        // ---------------------------------------------------------------

        void DrawGlobalSection()
        {
            EditorGUILayout.LabelField("2. 全体設定", EditorStyles.boldLabel);

            int resIndex = Mathf.Max(0, Array.IndexOf(ResolutionOptions, settings.resolution));
            resIndex = EditorGUILayout.Popup("書き出し解像度", resIndex, ResolutionLabels);
            settings.resolution = ResolutionOptions[resIndex];

            settings.strength = EditorGUILayout.Slider(new GUIContent("凹凸の強さ", "ノーマルの傾きの強さ。大きいほど波が立って見えます。"), settings.strength, 0.05f, 5f);
            settings.autoNormalize = EditorGUILayout.Toggle(new GUIContent("高さを自動正規化", "高さの範囲を [-1,1] に広げてコントラストを最大化します。"), settings.autoNormalize);
            settings.flipY = EditorGUILayout.Toggle(new GUIContent("Y (緑) を反転", "Unity 標準は OFF。DirectX 系ツールに合わせる場合のみ ON。"), settings.flipY);

            using (new EditorGUILayout.HorizontalScope())
            {
                settings.globalSeed = EditorGUILayout.IntField(new GUIContent("シード", "全体の乱数シード。変えると別バリエーションになります。"), settings.globalSeed);
                if (GUILayout.Button("🎲", GUILayout.Width(30f)))
                {
                    settings.globalSeed = UnityEngine.Random.Range(0, 999999);
                    previewDirty = true;
                }
            }

            settings.applyMobileImportSettings = EditorGUILayout.Toggle(
                new GUIContent("モバイル向けインポート設定を自動適用", "書き出し時に Repeat / Android・iOS 共に ASTC 6x6 (最大 1024px) を自動設定します (VRChat Quest / iOS 向け)。"),
                settings.applyMobileImportSettings);

            EditorGUILayout.LabelField("品質", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("軽量", "1024px / 通常生成。試作やモバイル確認向け。")))
                {
                    ApplyQualityShortcut(1024, 1, false);
                }
                if (GUILayout.Button(new GUIContent("高品質", "2048px / 2x スーパーサンプリング。通常の本番向け。")))
                {
                    ApplyQualityShortcut(2048, 2, false);
                }
                if (GUILayout.Button(new GUIContent("最高品質 EXR", "4096px / 2x スーパーサンプリング / 16bit EXR。重いが階調が最も綺麗。")))
                {
                    ApplyQualityShortcut(4096, 2, true);
                }
            }
            bool ss = EditorGUILayout.Toggle(new GUIContent("スーパーサンプリング (2×)", "2 倍解像度で生成してから縮小し、鋭いエッジのジャギーを抑えます。生成時間は約 4 倍。生成解像度が 4096 を超える場合は自動的に無効になります。"), settings.supersample > 1);
            settings.supersample = ss ? 2 : 1;
            settings.exportExr = EditorGUILayout.Toggle(new GUIContent("16bit EXR で書き出し", "PNG (8bit) の代わりに EXR (16bit float) で書き出します。穏やかな水面のバンディング (縞) を根絶できます。"), settings.exportExr);
        }

        void ApplyQualityShortcut(int resolution, int supersample, bool exportExr)
        {
            settings.resolution = resolution;
            settings.supersample = supersample;
            settings.exportExr = exportExr;
            previewDirty = true;
        }

        void DrawMapSettingsSection()
        {
            mapSettingsFoldout = EditorGUILayout.Foldout(mapSettingsFoldout, "マップ別設定 (フォーム / ラフネス / フロー / DUDV / コースティクス)", true);
            if (!mapSettingsFoldout) return;

            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("フォームマスク", EditorStyles.miniBoldLabel);
            settings.foamThreshold = EditorGUILayout.Slider(new GUIContent("波頭しきい値", "この高さより上にフォームが出ます。"), settings.foamThreshold, 0f, 1f);
            settings.foamSoftness = EditorGUILayout.Slider(new GUIContent("境界のやわらかさ"), settings.foamSoftness, 0.01f, 0.5f);
            settings.foamSlopeBoost = EditorGUILayout.Slider(new GUIContent("砕け波ブースト", "急斜面にもフォームを追加します。"), settings.foamSlopeBoost, 0f, 2f);
            settings.foamBlur = EditorGUILayout.IntSlider(new GUIContent("ぼかし回数", "フォームをやわらかくします (3×3 ボックスブラー)。"), settings.foamBlur, 0, 4);

            EditorGUILayout.LabelField("ラフネス", EditorStyles.miniBoldLabel);
            settings.baseRoughness = EditorGUILayout.Slider(new GUIContent("ベースラフネス"), settings.baseRoughness, 0f, 1f);
            settings.slopeRoughness = EditorGUILayout.Slider(new GUIContent("傾斜の影響"), settings.slopeRoughness, 0f, 2f);
            settings.foamRoughness = EditorGUILayout.Slider(new GUIContent("フォームの影響"), settings.foamRoughness, 0f, 1f);
            settings.exportSmoothness = EditorGUILayout.Toggle(new GUIContent("スムースネスとして書き出し", "ON で反転して書き出します (Standard/URP の Smoothness 用)。"), settings.exportSmoothness);

            EditorGUILayout.LabelField("フローマップ", EditorStyles.miniBoldLabel);
            settings.flowSwirl = EditorGUILayout.Slider(new GUIContent("渦の割合", "0 = レイヤーの進行方向のみ、1 = 高さ場の渦のみ。"), settings.flowSwirl, 0f, 1f);
            settings.flowStrength = EditorGUILayout.Slider(new GUIContent("フロー強度"), settings.flowStrength, 0f, 1f);

            EditorGUILayout.LabelField("DUDV / コースティクス", EditorStyles.miniBoldLabel);
            settings.dudvStrength = EditorGUILayout.Slider(new GUIContent("DUDV 強度"), settings.dudvStrength, 0f, 2f);
            settings.causticsIntensity = EditorGUILayout.Slider(new GUIContent("コースティクスの強さ", "曲率から明るさへの変換強度。模様が真っ白 / 真っ黒になる場合はここを調整。"), settings.causticsIntensity, 0.05f, 8f);
            settings.causticsSharpness = EditorGUILayout.Slider(new GUIContent("コースティクスの鋭さ"), settings.causticsSharpness, 0.5f, 8f);

            EditorGUI.indentLevel--;
        }

        // ---------------------------------------------------------------
        // レイヤー
        // ---------------------------------------------------------------

        void DrawLayersSection()
        {
            EditorGUILayout.LabelField("3. 波レイヤー", EditorStyles.boldLabel);

            var layers = new List<WaveLayer>(settings.layers);
            while (layerFoldouts.Count < layers.Count) layerFoldouts.Add(true);

            int removeIndex = -1;
            int moveUpIndex = -1;
            int moveDownIndex = -1;

            for (int i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        layer.enabled = EditorGUILayout.Toggle(layer.enabled, GUILayout.Width(18f));
                        layerFoldouts[i] = EditorGUILayout.Foldout(layerFoldouts[i], $"{i + 1}. {layer.name}  ({TypeLabel(layer.type)})", true);

                        using (new EditorGUI.DisabledScope(i == 0))
                        {
                            if (GUILayout.Button("▲", GUILayout.Width(24f))) moveUpIndex = i;
                        }
                        using (new EditorGUI.DisabledScope(i == layers.Count - 1))
                        {
                            if (GUILayout.Button("▼", GUILayout.Width(24f))) moveDownIndex = i;
                        }
                        if (GUILayout.Button("複製", GUILayout.Width(40f)))
                        {
                            layers.Insert(i + 1, layer.Clone());
                            layerFoldouts.Insert(i + 1, true);
                            settings.layers = layers.ToArray();
                            previewDirty = true;
                            GUIUtility.ExitGUI();
                        }
                        if (GUILayout.Button("×", GUILayout.Width(24f))) removeIndex = i;
                    }

                    if (layerFoldouts[i])
                    {
                        DrawLayerFields(layer, i);
                    }
                }
            }

            if (removeIndex >= 0)
            {
                layers.RemoveAt(removeIndex);
                layerFoldouts.RemoveAt(removeIndex);
                previewDirty = true;
            }
            if (moveUpIndex > 0)
            {
                (layers[moveUpIndex - 1], layers[moveUpIndex]) = (layers[moveUpIndex], layers[moveUpIndex - 1]);
                previewDirty = true;
            }
            if (moveDownIndex >= 0 && moveDownIndex < layers.Count - 1)
            {
                (layers[moveDownIndex + 1], layers[moveDownIndex]) = (layers[moveDownIndex], layers[moveDownIndex + 1]);
                previewDirty = true;
            }

            if (GUILayout.Button("＋ レイヤーを追加"))
            {
                layers.Add(WaterMapPresets.NewLayer());
                layerFoldouts.Add(true);
                previewDirty = true;
            }

            settings.layers = layers.ToArray();
        }

        static string TypeLabel(WaveLayerType type)
        {
            switch (type)
            {
                case WaveLayerType.PerlinWaves: return "揺らぎノイズ";
                case WaveLayerType.RidgedWaves: return "尖ったうねり";
                case WaveLayerType.VoronoiCells: return "ボロノイ(泡)";
                case WaveLayerType.VoronoiCaustics: return "ボロノイ(網目)";
                case WaveLayerType.DirectionalWaves: return "指向性の波";
                case WaveLayerType.RainRipples: return "雨の波紋";
                default: return type.ToString();
            }
        }

        void DrawLayerFields(WaveLayer layer, int index)
        {
            EditorGUI.indentLevel++;

            layer.name = EditorGUILayout.TextField("名前", layer.name);
            layer.type = (WaveLayerType)EditorGUILayout.Popup("種類", (int)layer.type, new[]
            {
                "揺らぎノイズ (パーリン fBm)",
                "尖ったうねり (リッジノイズ)",
                "ボロノイ・泡 (丸い盛り上がり)",
                "ボロノイ・網目 (コースティクス風)",
                "指向性の波 (ゲルストナー風 / スペクトル)",
                "雨の波紋 (広がるリング)",
            });

            if (index > 0)
            {
                layer.blend = (WaveBlendMode)EditorGUILayout.Popup("合成モード", (int)layer.blend, new[] { "加算", "乗算 (変調)", "最大値", "最小値" });
            }

            layer.amplitude = EditorGUILayout.Slider(new GUIContent("振幅", "このレイヤーの強さ"), layer.amplitude, 0f, 2f);
            layer.scale = EditorGUILayout.IntSlider(new GUIContent("スケール", "テクスチャ 1 枚あたりの繰り返し数。大きいほど細かい模様。"), layer.scale, 1, 64);

            switch (layer.type)
            {
                case WaveLayerType.PerlinWaves:
                case WaveLayerType.RidgedWaves:
                    layer.octaves = EditorGUILayout.IntSlider(new GUIContent("オクターブ", "重ねるノイズの階層数。多いほどディテール増。"), layer.octaves, 1, 8);
                    layer.persistence = EditorGUILayout.Slider(new GUIContent("減衰率", "高周波の寄与率"), layer.persistence, 0.1f, 0.9f);
                    layer.sharpness = EditorGUILayout.Slider(new GUIContent("尖り", "1 でそのまま。大きいほど山が尖ります。"), layer.sharpness, 0.25f, 8f);
                    layer.stretch = EditorGUILayout.IntSlider(new GUIContent("流れ方向へ引き伸ばし", "X 方向に模様を引き伸ばします(川向け)。"), layer.stretch, 1, 8);
                    layer.directionDeg = EditorGUILayout.Slider(new GUIContent("スクロール方向 (度)", "アニメ時の流れる向き"), layer.directionDeg, 0f, 360f);
                    break;

                case WaveLayerType.VoronoiCells:
                case WaveLayerType.VoronoiCaustics:
                    layer.sharpness = EditorGUILayout.Slider(new GUIContent("コントラスト", "模様のメリハリ"), layer.sharpness, 0.25f, 8f);
                    layer.jitter = EditorGUILayout.Slider(new GUIContent("ゆらぎ", "セル配置の不規則さ。アニメ時は揺れの大きさ。"), layer.jitter, 0f, 1f);
                    break;

                case WaveLayerType.DirectionalWaves:
                    layer.waveCount = EditorGUILayout.IntSlider(new GUIContent("波の本数", "重ねる正弦波の数。32 以上で外洋のスペクトル波のような複雑さになります。"), layer.waveCount, 1, 64);
                    layer.directionDeg = EditorGUILayout.Slider(new GUIContent("進行方向 (度)"), layer.directionDeg, 0f, 360f);
                    layer.spreadDeg = EditorGUILayout.Slider(new GUIContent("方向のばらつき (度)", "0 で完全に揃った波、大きいほど乱れます。"), layer.spreadDeg, 0f, 90f);
                    layer.sharpness = EditorGUILayout.Slider(new GUIContent("波頭の尖り", "1 で正弦波。大きいほど山が尖り谷が広がります。"), layer.sharpness, 0.25f, 8f);
                    break;

                case WaveLayerType.RainRipples:
                    layer.dropCount = EditorGUILayout.IntSlider(new GUIContent("滴の数"), layer.dropCount, 1, 128);
                    layer.octaves = EditorGUILayout.IntSlider(new GUIContent("リングの本数", "1 つの波紋に含まれる輪の数"), layer.octaves, 1, 8);
                    layer.sharpness = EditorGUILayout.Slider(new GUIContent("リングの鋭さ"), layer.sharpness, 0.25f, 8f);
                    break;
            }

            layer.warpAmount = EditorGUILayout.Slider(new GUIContent("ドメインワープ", "模様を有機的に歪ませます。0 で無効。"), layer.warpAmount, 0f, 1f);
            if (layer.warpAmount > 0f)
            {
                layer.warpScale = EditorGUILayout.IntSlider(new GUIContent("ワープの粗さ"), layer.warpScale, 1, 16);
            }
            layer.maskAmount = EditorGUILayout.Slider(new GUIContent("マスクむら", "低周波ノイズでレイヤーの効きにムラを作ります。0 で無効。"), layer.maskAmount, 0f, 1f);
            if (layer.maskAmount > 0f)
            {
                layer.maskScale = EditorGUILayout.IntSlider(new GUIContent("マスクの粗さ"), layer.maskScale, 1, 16);
            }

            layer.speed = EditorGUILayout.IntSlider(new GUIContent("アニメ速度", "連番書き出し時の速度。整数のため必ずループします。0 で静止。"), layer.speed, 0, 8);
            layer.seed = EditorGUILayout.IntField(new GUIContent("シード"), layer.seed);
            layer.invert = EditorGUILayout.Toggle(new GUIContent("凹凸を反転"), layer.invert);

            EditorGUI.indentLevel--;
        }

        // ---------------------------------------------------------------
        // プレビュー
        // ---------------------------------------------------------------

        void DrawPreviewSection(float paneWidth, bool fixedPane)
        {
            using (new EditorGUILayout.VerticalScope(fixedPane ? EditorStyles.helpBox : GUIStyle.none, GUILayout.ExpandHeight(fixedPane)))
            {
            EditorGUILayout.LabelField(fixedPane ? "固定プレビュー" : "プレビュー", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            int columns = paneWidth < 420f ? 2 : 3;
            previewMapIndex = GUILayout.SelectionGrid(previewMapIndex, MapTabLabels, columns);
            if (EditorGUI.EndChangeCheck())
            {
                previewDirty = true;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                previewTiled = GUILayout.Toggle(previewTiled, "2×2 タイリング表示", "Button");
                bool newAnimate = GUILayout.Toggle(animatePreview, "アニメ再生", "Button");
                if (newAnimate != animatePreview)
                {
                    animatePreview = newAnimate;
                    lastAnimTime = 0;
                }
            }

            EditorGUI.BeginChangeCheck();
            previewTime = EditorGUILayout.Slider(new GUIContent("時間 (ループ位置)", "アニメーションの位相。書き出しにも反映されます。"), previewTime, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                previewDirty = true;
            }

            float availableHeight = fixedPane ? Mathf.Max(220f, position.height - 122f) : 320f;
            float size = Mathf.Min(Mathf.Max(220f, paneWidth - 28f), availableHeight);
            Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
            float contentWidth = fixedPane ? paneWidth : EditorGUIUtility.currentViewWidth;
            rect.x += Mathf.Max(0f, (contentWidth - size) * 0.5f - 8f);

            if (previewTexture != null)
            {
                if (previewTiled)
                {
                    GUI.DrawTextureWithTexCoords(rect, previewTexture, new Rect(0f, 0f, 2f, 2f));
                }
                else
                {
                    GUI.DrawTexture(rect, previewTexture, ScaleMode.ScaleToFit);
                }
            }
            else
            {
                EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
            }
            }
        }

        void RegeneratePreview()
        {
            previewDirty = false;
            double now = EditorApplication.timeSinceStartup;
            nextPreviewRegenerateTime = now + (animatePreview ? 1.0 / PreviewAnimationFps : PreviewRegenerateMinInterval);
            DestroyPreview();

            var mapType = MapTypes[Mathf.Clamp(previewMapIndex, 0, MapTypes.Length - 1)];
            // プレビューはアニメ再生の応答性を優先して SSAA なしで生成
            var previewSettings = settings.Clone();
            previewSettings.supersample = animatePreview ? 1 : settings.supersample;
            Color32[] pixels = WaterMapCore.GeneratePixels(previewSettings, mapType, PreviewResolution, previewTime);
            previewTexture = new Texture2D(PreviewResolution, PreviewResolution, TextureFormat.RGBA32, false, true)
            {
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.HideAndDontSave,
            };
            previewTexture.SetPixels32(pixels);
            previewTexture.Apply(false, false);
            Repaint();
        }

        void DestroyPreview()
        {
            if (previewTexture != null)
            {
                DestroyImmediate(previewTexture);
                previewTexture = null;
            }
        }

        // ---------------------------------------------------------------
        // 書き出し
        // ---------------------------------------------------------------

        void DrawExportSection()
        {
            EditorGUILayout.LabelField("4. 書き出し", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("書き出すマップ", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < 4; i++)
                {
                    exportMapFlags[i] = GUILayout.Toggle(exportMapFlags[i], MapTabLabels[i], "Button");
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 4; i < MapTypes.Length; i++)
                {
                    exportMapFlags[i] = GUILayout.Toggle(exportMapFlags[i], MapTabLabels[i], "Button");
                }
            }

            materialShaderIndex = EditorGUILayout.Popup(new GUIContent("マテリアルを自動作成", "書き出したマップを割り当てたマテリアルを一緒に作成します。"), materialShaderIndex, MaterialShaderLabels);
            if (materialShaderIndex > 0)
            {
                settings.createTransparentMaterial = EditorGUILayout.Toggle(
                    new GUIContent("透明マテリアルとして作成", "PC / URP / iOS 向け。Standard / URP Lit / Siliq Mobile は Transparent Blend に、Siliq URP は _Opacity に反映します。Quest 向け不透明運用では OFF 推奨です。"),
                    settings.createTransparentMaterial);
                if (settings.createTransparentMaterial)
                {
                    settings.materialOpacity = EditorGUILayout.Slider(
                        new GUIContent("不透明度", "1 に近いほど濃く不透明、低いほど透けます。薄すぎる場合は 0.5 以上に上げてください。"),
                        settings.materialOpacity, 0.05f, 1f);
                }
            }

            using (new EditorGUI.DisabledScope(!AnyExportSelected()))
            {
                if (GUILayout.Button($"選択したマップを一括書き出し ({FileExtension.ToUpperInvariant()})", GUILayout.Height(32f)))
                {
                    ExportSelectedMaps();
                }
            }

            GUILayout.Space(4f);
            settings.frameCount = EditorGUILayout.IntSlider(new GUIContent("アニメのフレーム数", "連番 / アトラス書き出しのフレーム数。完全ループします。"), settings.frameCount, 2, 64);

            using (new EditorGUI.DisabledScope(!AnyExportSelected()))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("ループアニメを連番書き出し"))
                {
                    ExportSequence();
                }
                if (GUILayout.Button("ループアニメをアトラス書き出し"))
                {
                    ExportAtlas();
                }
            }

            EditorGUILayout.HelpBox(
                "VRChat モバイル (Quest) で使う場合:\n" +
                "・アバター → VRChat/Mobile/Standard Lite の Normal Map スロットにセット\n" +
                "・ワールド → 同梱の \"Siliq/Water Mobile (Quest)\" などのカスタムシェーダー可\n" +
                "最新 Unity (URP) では Sample 導入後の \"Siliq/Water URP\" やお手持ちの水シェーダーで、\n" +
                "フォーム / フロー / ラフネスマップも活用できます。ランタイム生成 API は README 参照。",
                MessageType.None);
        }

        bool AnyExportSelected()
        {
            foreach (bool f in exportMapFlags)
            {
                if (f) return true;
            }
            return false;
        }

        string FileExtension => settings.exportExr ? "exr" : "png";

        void ExportSelectedMaps()
        {
            string path = EditorUtility.SaveFilePanelInProject("一括書き出し", "Water", FileExtension, "ベース名を指定してください (マップ別のサフィックスが付きます)");
            if (string.IsNullOrEmpty(path)) return;

            string dir = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string baseName = Path.GetFileNameWithoutExtension(path);
            int size = settings.resolution;
            int ss = WaterMapCore.EffectiveSupersample(settings, size);
            int genSize = size * ss;
            var exported = new List<(string path, WaterMapType type)>();

            try
            {
                EditorUtility.DisplayProgressBar("水面マップ生成", "ハイトフィールド生成中...", 0.1f);
                float[] heights = WaterMapCore.GenerateHeightField(settings, genSize, previewTime);

                for (int i = 0; i < MapTypes.Length; i++)
                {
                    if (!exportMapFlags[i]) continue;
                    EditorUtility.DisplayProgressBar("水面マップ生成", $"{MapTabLabels[i]} を書き出し中...", 0.2f + 0.7f * i / MapTypes.Length);

                    Color[] colors = WaterMapCore.ColorsFromHeights(heights, settings, MapTypes[i], genSize);
                    colors = WaterMapCore.Downsample(colors, genSize, ss, MapTypes[i] == WaterMapType.Normal);
                    string mapPath = $"{dir}/{baseName}_{MapSuffixes[i]}.{FileExtension}";
                    WriteImage(mapPath, colors, size, refresh: false);
                    exported.Add((mapPath, MapTypes[i]));
                }

                AssetDatabase.Refresh();
                ApplyImportSettingsBulk(exported, size);

                if (materialShaderIndex > 0 && exported.Count > 0)
                {
                    CreateMaterial($"{dir}/{baseName}.mat", exported);
                }

                if (exported.Count > 0)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(exported[0].path);
                    if (asset != null) EditorGUIUtility.PingObject(asset);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        void ExportSequence()
        {
            string path = EditorUtility.SaveFilePanelInProject("連番アニメを書き出し", "Water_anim", FileExtension, "ベース名を指定してください (マップ別サフィックスと連番が付きます)");
            if (string.IsNullOrEmpty(path)) return;

            string dir = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string baseName = Path.GetFileNameWithoutExtension(path);
            int frames = settings.frameCount;
            int size = settings.resolution;
            int ss = WaterMapCore.EffectiveSupersample(settings, size);
            int genSize = size * ss;
            var exported = new List<(string path, WaterMapType type)>();

            try
            {
                try
                {
                    AssetDatabase.StartAssetEditing();
                    for (int f = 0; f < frames; f++)
                    {
                        EditorUtility.DisplayProgressBar("連番アニメ書き出し", $"フレーム {f + 1} / {frames}", (float)f / frames);
                        float t = (float)f / frames;
                        float[] heights = WaterMapCore.GenerateHeightField(settings, genSize, t);

                        for (int i = 0; i < MapTypes.Length; i++)
                        {
                            if (!exportMapFlags[i]) continue;
                            Color[] colors = WaterMapCore.ColorsFromHeights(heights, settings, MapTypes[i], genSize);
                            colors = WaterMapCore.Downsample(colors, genSize, ss, MapTypes[i] == WaterMapType.Normal);
                            string framePath = $"{dir}/{baseName}_{MapSuffixes[i]}_{f:D3}.{FileExtension}";
                            WriteImage(framePath, colors, size, refresh: false);
                            exported.Add((framePath, MapTypes[i]));
                        }
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
                AssetDatabase.Refresh();
                ApplyImportSettingsBulk(exported, size);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        void ExportAtlas()
        {
            int frames = settings.frameCount;
            int cols = Mathf.CeilToInt(Mathf.Sqrt(frames));
            int rows = Mathf.CeilToInt((float)frames / cols);
            int cellSize = settings.resolution;
            int atlasW = cols * cellSize;
            int atlasH = rows * cellSize;

            int sizeLimit = settings.exportExr ? 4096 : 8192; // EXR はメモリ使用量が大きいため控えめに
            if (Mathf.Max(atlasW, atlasH) > sizeLimit)
            {
                EditorUtility.DisplayDialog("アトラスが大きすぎます",
                    $"アトラスサイズが {atlasW}×{atlasH} になり上限 ({sizeLimit}) を超えます。\n解像度またはフレーム数を下げてください。", "OK");
                return;
            }

            int selectedMapCount = CountSelectedExportMaps();
            long atlasPixels = (long)atlasW * atlasH;
            long estimatedBytes = atlasPixels * 16L * selectedMapCount;
            const long maxAtlasWorkingBytes = 768L * 1024L * 1024L;
            if (estimatedBytes > maxAtlasWorkingBytes)
            {
                EditorUtility.DisplayDialog("アトラスのメモリ使用量が大きすぎます",
                    $"選択中のマップ数では作業メモリが約 {estimatedBytes / (1024L * 1024L)} MB 必要です。\n解像度・フレーム数・書き出しマップ数を下げてください。", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject("アトラスを書き出し", $"Water_atlas_{cols}x{rows}", FileExtension, "ベース名を指定してください");
            if (string.IsNullOrEmpty(path)) return;

            string dir = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string baseName = Path.GetFileNameWithoutExtension(path);
            int ss = WaterMapCore.EffectiveSupersample(settings, cellSize);
            int genSize = cellSize * ss;

            try
            {
                // フレームごとにハイトフィールドを生成し、選択された全マップのアトラスへ書き込む
                var atlases = new Dictionary<int, Color[]>();
                for (int i = 0; i < MapTypes.Length; i++)
                {
                    if (exportMapFlags[i]) atlases[i] = new Color[atlasW * atlasH];
                }

                for (int f = 0; f < frames; f++)
                {
                    EditorUtility.DisplayProgressBar("アトラス書き出し", $"フレーム {f + 1} / {frames}", (float)f / frames);
                    float t = (float)f / frames;
                    float[] heights = WaterMapCore.GenerateHeightField(settings, genSize, t);

                    int col = f % cols;
                    int row = f / cols;
                    int dstX = col * cellSize;
                    int dstY = (rows - 1 - row) * cellSize; // テクスチャ座標は下から上

                    foreach (var kv in atlases)
                    {
                        Color[] colors = WaterMapCore.ColorsFromHeights(heights, settings, MapTypes[kv.Key], genSize);
                        colors = WaterMapCore.Downsample(colors, genSize, ss, MapTypes[kv.Key] == WaterMapType.Normal);
                        for (int y = 0; y < cellSize; y++)
                        {
                            Array.Copy(colors, y * cellSize, kv.Value, (dstY + y) * atlasW + dstX, cellSize);
                        }
                    }
                }

                EditorUtility.DisplayProgressBar("アトラス書き出し", "画像書き出し中...", 0.95f);
                string firstPath = null;
                var exported = new List<(string path, WaterMapType type)>();
                foreach (var kv in atlases)
                {
                    string mapPath = $"{dir}/{baseName}_{MapSuffixes[kv.Key]}.{FileExtension}";
                    WriteImage(mapPath, kv.Value, atlasW, atlasH, refresh: false);
                    exported.Add((mapPath, MapTypes[kv.Key]));
                    firstPath = firstPath ?? mapPath;
                }

                AssetDatabase.Refresh();
                ApplyImportSettingsBulk(exported, Mathf.Max(atlasW, atlasH));

                if (firstPath != null)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(firstPath);
                    if (asset != null) EditorGUIUtility.PingObject(asset);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        void WriteImage(string path, Color[] colors, int size, bool refresh = true)
        {
            WriteImage(path, colors, size, size, refresh);
        }

        void WriteImage(string path, Color[] colors, int w, int h, bool refresh = true)
        {
            if (settings.exportExr)
            {
                var tex = new Texture2D(w, h, TextureFormat.RGBAHalf, false, true);
                tex.SetPixels(colors);
                tex.Apply(false, false);
                File.WriteAllBytes(path, tex.EncodeToEXR(Texture2D.EXRFlags.CompressZIP));
                DestroyImmediate(tex);
            }
            else
            {
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
                tex.SetPixels32(WaterMapCore.Quantize(colors));
                tex.Apply(false, false);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                DestroyImmediate(tex);
            }

            if (refresh)
            {
                AssetDatabase.ImportAsset(path);
            }
        }

        int CountSelectedExportMaps()
        {
            int count = 0;
            for (int i = 0; i < exportMapFlags.Length; i++)
            {
                if (exportMapFlags[i]) count++;
            }
            return count;
        }

        void ApplyImportSettingsBulk(List<(string path, WaterMapType type)> exported, int maxTextureSize)
        {
            if (exported == null || exported.Count == 0) return;

            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var (p, type) in exported)
                {
                    ApplyImportSettings(p, type, maxTextureSize);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        void ApplyImportSettings(string path, WaterMapType mapType, int maxTextureSize)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            if (mapType == WaterMapType.Normal)
            {
                importer.textureType = TextureImporterType.NormalMap;
            }
            else
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = false; // データ系マップは linear
            }
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            int importMaxSize = Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.Max(maxTextureSize, 256)), 256, 8192);
            importer.maxTextureSize = importMaxSize;

            if (settings.applyMobileImportSettings)
            {
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = "Android",
                    overridden = true,
                    maxTextureSize = Mathf.Min(importMaxSize, 1024), // Quest では 1024 以下を推奨
                    format = TextureImporterFormat.ASTC_6x6,
                });
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = "iPhone",
                    overridden = true,
                    maxTextureSize = Mathf.Min(importMaxSize, 1024), // iOS も ASTC 6x6 (Metal 対応)
                    format = TextureImporterFormat.ASTC_6x6,
                });
            }
            else
            {
                importer.ClearPlatformTextureSettings("Android");
                importer.ClearPlatformTextureSettings("iPhone");
            }

            importer.SaveAndReimport();
        }

        // ---------------------------------------------------------------
        // マテリアル自動作成
        // ---------------------------------------------------------------

        void CreateMaterial(string path, List<(string path, WaterMapType type)> maps)
        {
            int resolvedShaderIndex = materialShaderIndex;
            string requestedShaderName = ShaderNameForMaterialIndex(resolvedShaderIndex);
            if (string.IsNullOrEmpty(requestedShaderName)) return;

            Shader shader = WaterShaderUtility.FindUsableShaderForCurrentPipeline(requestedShaderName);
            if (shader == null)
            {
                int fallbackIndex = WaterShaderUtility.ResolveSafeMaterialShaderIndex(resolvedShaderIndex);
                if (fallbackIndex == WaterShaderUtility.NoMaterialIndex)
                {
                    Debug.LogWarning($"[Siliq Water] シェーダー '{requestedShaderName}' は現在の Render Pipeline では使用できないためマテリアル作成をスキップしました。");
                    return;
                }

                string fallbackShaderName = ShaderNameForMaterialIndex(fallbackIndex);
                Shader fallbackShader = WaterShaderUtility.FindUsableShaderForCurrentPipeline(fallbackShaderName);
                if (fallbackShader == null)
                {
                    Debug.LogWarning($"[Siliq Water] fallback shader '{fallbackShaderName}' も現在の Render Pipeline では使用できないためマテリアル作成をスキップしました。");
                    return;
                }

                Debug.LogWarning($"[Siliq Water] シェーダー '{requestedShaderName}' は現在の Render Pipeline では使用できないため、ピンク表示を避けるため '{fallbackShaderName}' でマテリアルを作成しました。");
                resolvedShaderIndex = fallbackIndex;
                shader = fallbackShader;
            }

            var mat = new Material(shader);

            Texture2D Find(WaterMapType t)
            {
                foreach (var (p, type) in maps)
                {
                    if (type == t) return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                }
                return null;
            }

            var normal = Find(WaterMapType.Normal);
            var height = Find(WaterMapType.Height);
            var foam = Find(WaterMapType.Foam);
            var flow = Find(WaterMapType.Flow);
            var caustics = Find(WaterMapType.Caustics);
            bool indoorBluePool = presetIndex == IndoorBluePoolPresetIndex;
            bool flagshipCrystal = presetIndex == FlagshipCrystalPresetIndex;
            Color generatedBaseColor = new Color(0.1f, 0.35f, 0.45f, settings.createTransparentMaterial ? settings.materialOpacity : 1f);
            if (indoorBluePool)
            {
                generatedBaseColor = new Color(0.32f, 0.86f, 1f, settings.createTransparentMaterial ? settings.materialOpacity : 1f);
            }
            else if (flagshipCrystal)
            {
                generatedBaseColor = new Color(0.24f, 0.92f, 1f, settings.createTransparentMaterial ? settings.materialOpacity : 1f);
            }

            switch (resolvedShaderIndex)
            {
                case 1: // Standard
                    if (normal != null)
                    {
                        mat.SetTexture("_BumpMap", normal);
                        mat.EnableKeyword("_NORMALMAP");
                    }
                    if (height != null)
                    {
                        mat.SetTexture("_ParallaxMap", height);
                        mat.EnableKeyword("_PARALLAXMAP");
                        mat.SetFloat("_Parallax", 0.02f);
                    }
                    mat.SetColor("_Color", generatedBaseColor);
                    mat.SetFloat("_Glossiness", (indoorBluePool || flagshipCrystal) ? 0.98f : 0.9f);
                    if (settings.createTransparentMaterial)
                    {
                        SetupStandardTransparent(mat, settings.materialOpacity);
                    }
                    break;

                case 2: // URP Lit
                    if (normal != null)
                    {
                        mat.SetTexture("_BumpMap", normal);
                        mat.EnableKeyword("_NORMALMAP");
                    }
                    if (height != null && mat.HasProperty("_ParallaxMap"))
                    {
                        mat.SetTexture("_ParallaxMap", height);
                        mat.EnableKeyword("_PARALLAXMAP");
                    }
                    mat.SetColor("_BaseColor", generatedBaseColor);
                    mat.SetFloat("_Smoothness", (indoorBluePool || flagshipCrystal) ? 0.98f : 0.9f);
                    if (settings.createTransparentMaterial)
                    {
                        SetupUrpLitTransparent(mat, settings.materialOpacity);
                    }
                    break;

                case 3: // Siliq Mobile
                case 4: // Siliq URP
                    if (normal != null)
                    {
                        mat.SetTexture("_NormalMap", normal);
                    }
                    SetupSiliqCaustics(mat, caustics, 0.34f, 1.8f, 0.00016f, new Color(0.78f, 1f, 1f, 1f));
                    if (height != null && mat.HasProperty("_HeightMap"))
                    {
                        mat.SetTexture("_HeightMap", height);
                        if (mat.HasProperty("_HeightMapInfluence")) mat.SetFloat("_HeightMapInfluence", 0.45f);
                        if (mat.HasProperty("_DisplacementStrength")) mat.SetFloat("_DisplacementStrength", settings.createTransparentMaterial ? 0.04f : 0.025f);
                        if (mat.HasProperty("_DisplacementScale")) mat.SetFloat("_DisplacementScale", 0.75f);
                        if (mat.HasProperty("_DisplacementSpeed")) mat.SetFloat("_DisplacementSpeed", 0.0008f);
                    }
                    else
                    {
                        if (mat.HasProperty("_HeightMapInfluence")) mat.SetFloat("_HeightMapInfluence", 0f);
                        if (mat.HasProperty("_DisplacementStrength")) mat.SetFloat("_DisplacementStrength", settings.createTransparentMaterial ? 0.035f : 0.018f);
                        if (mat.HasProperty("_DisplacementScale")) mat.SetFloat("_DisplacementScale", 0.75f);
                        if (mat.HasProperty("_DisplacementSpeed")) mat.SetFloat("_DisplacementSpeed", 0.0008f);
                    }
                    SetupMacroVariation(mat, 0.42f, 0.10f, 0.36f, 0.18f);
                    SetupDarkSceneResponse(mat);
                    if (resolvedShaderIndex == 4)
                    {
                        if (flow != null)
                        {
                            mat.SetTexture("_FlowMap", flow);
                            mat.EnableKeyword("_USE_FLOWMAP");
                            mat.SetFloat("_UseFlowMap", 1f);
                        }
                        if (foam != null)
                        {
                            mat.SetTexture("_FoamMap", foam);
                            mat.EnableKeyword("_SHORE_EFFECTS");
                            mat.SetFloat("_UseShore", 1f);
                        }
                        if (settings.createTransparentMaterial)
                        {
                            mat.SetFloat("_Opacity", settings.materialOpacity);
                        }
                    }
                    else if (settings.createTransparentMaterial)
                    {
                        if (!indoorBluePool && !flagshipCrystal)
                        {
                            mat.SetColor("_ShallowColor", new Color(0.62f, 0.96f, 1f, 1f));
                            mat.SetColor("_DeepColor", new Color(0.005f, 0.09f, 0.16f, 1f));
                            mat.SetColor("_HorizonColor", new Color(0.82f, 0.94f, 1f, 1f));
                            if (mat.HasProperty("_TransmissionColor")) mat.SetColor("_TransmissionColor", new Color(0.35f, 0.9f, 1f, 1f));
                            if (mat.HasProperty("_GlimmerColor")) mat.SetColor("_GlimmerColor", new Color(0.92f, 0.99f, 1f, 1f));
                        }
                        SetupSiliqMobileTransparent(mat, settings.materialOpacity);
                    }
                    if (indoorBluePool)
                    {
                        SetupSiliqIndoorBluePool(mat, settings.materialOpacity, height != null);
                    }
                    else if (flagshipCrystal)
                    {
                        SetupSiliqFlagshipCrystal(mat, settings.materialOpacity, height != null);
                    }
                    break;
            }

            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
        }

        static string ShaderNameForMaterialIndex(int index)
        {
            return WaterShaderUtility.ShaderNameForMaterialIndex(index);
        }

        static bool IsUsableShader(Shader shader)
        {
            return WaterShaderUtility.IsUsableShaderForCurrentPipeline(shader);
        }

        static int BestFallbackMaterialShaderIndex()
        {
            return WaterShaderUtility.BestFallbackMaterialShaderIndex();
        }

        static void SetupStandardTransparent(Material mat, float opacity)
        {
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
        }

        static void SetupUrpLitTransparent(Material mat, float opacity)
        {
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

        static void SetupSiliqMobileTransparent(Material mat, float opacity)
        {
            SetupDarkSceneResponse(mat);
            if (mat.HasProperty("_Opacity")) mat.SetFloat("_Opacity", Mathf.Clamp01(opacity));
            if (mat.HasProperty("_AlphaFresnel")) mat.SetFloat("_AlphaFresnel", 0.74f);
            if (mat.HasProperty("_AlphaPower")) mat.SetFloat("_AlphaPower", 2.05f);
            if (mat.HasProperty("_EdgeReflection")) mat.SetFloat("_EdgeReflection", 0.70f);
            if (mat.HasProperty("_TransmissionStrength")) mat.SetFloat("_TransmissionStrength", 0.70f);
            if (mat.HasProperty("_GlimmerIntensity")) mat.SetFloat("_GlimmerIntensity", 0.28f);
            if (mat.HasProperty("_GlimmerSharpness")) mat.SetFloat("_GlimmerSharpness", 14f);
            if (mat.HasProperty("_GlintIntensity")) mat.SetFloat("_GlintIntensity", 0.74f);
            if (mat.HasProperty("_GlintPower")) mat.SetFloat("_GlintPower", 220f);
            if (mat.HasProperty("_FresnelPower")) mat.SetFloat("_FresnelPower", 2.35f);
            if (mat.HasProperty("_ReflStrength")) mat.SetFloat("_ReflStrength", 1f);
            if (mat.HasProperty("_SpecIntensity")) mat.SetFloat("_SpecIntensity", 1.35f);
            if (mat.HasProperty("_SpecPower")) mat.SetFloat("_SpecPower", 220f);
            SetupSiliqCaustics(mat, null, 0.30f, 1.8f, 0.00016f, new Color(0.78f, 1f, 1f, 1f));
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);

            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        static void SetupSiliqIndoorBluePool(Material mat, float opacity, bool hasHeightMap)
        {
            SetupDarkSceneResponse(mat);

            if (mat.HasProperty("_ShallowColor")) mat.SetColor("_ShallowColor", new Color(0.48f, 0.92f, 1f, 1f));
            if (mat.HasProperty("_DeepColor")) mat.SetColor("_DeepColor", new Color(0.02f, 0.22f, 0.52f, 1f));
            if (mat.HasProperty("_HorizonColor")) mat.SetColor("_HorizonColor", new Color(0.92f, 0.99f, 1f, 1f));
            if (mat.HasProperty("_TransmissionColor")) mat.SetColor("_TransmissionColor", new Color(0.56f, 0.98f, 1f, 1f));
            if (mat.HasProperty("_GlimmerColor")) mat.SetColor("_GlimmerColor", new Color(1f, 1f, 0.96f, 1f));
            if (mat.HasProperty("_Opacity")) mat.SetFloat("_Opacity", Mathf.Clamp01(opacity));
            if (mat.HasProperty("_NormalStrength")) mat.SetFloat("_NormalStrength", 0.46f);
            if (mat.HasProperty("_Tiling1")) mat.SetFloat("_Tiling1", 0.78f);
            if (mat.HasProperty("_Tiling2")) mat.SetFloat("_Tiling2", 1.55f);
            if (mat.HasProperty("_AlphaFresnel")) mat.SetFloat("_AlphaFresnel", 0.78f);
            if (mat.HasProperty("_AlphaPower")) mat.SetFloat("_AlphaPower", 1.85f);
            if (mat.HasProperty("_EdgeReflection")) mat.SetFloat("_EdgeReflection", 0.82f);
            if (mat.HasProperty("_TransmissionStrength")) mat.SetFloat("_TransmissionStrength", 0.82f);
            if (mat.HasProperty("_GlimmerIntensity")) mat.SetFloat("_GlimmerIntensity", 0.24f);
            if (mat.HasProperty("_GlimmerSharpness")) mat.SetFloat("_GlimmerSharpness", 16f);
            if (mat.HasProperty("_GlintIntensity")) mat.SetFloat("_GlintIntensity", 0.68f);
            if (mat.HasProperty("_GlintPower")) mat.SetFloat("_GlintPower", 260f);
            if (mat.HasProperty("_FresnelPower")) mat.SetFloat("_FresnelPower", 2.05f);
            if (mat.HasProperty("_ReflStrength")) mat.SetFloat("_ReflStrength", 1f);
            if (mat.HasProperty("_SpecIntensity")) mat.SetFloat("_SpecIntensity", 1.50f);
            if (mat.HasProperty("_SpecPower")) mat.SetFloat("_SpecPower", 300f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.98f);
            if (mat.HasProperty("_DisplacementStrength")) mat.SetFloat("_DisplacementStrength", 0.025f);
            if (mat.HasProperty("_DisplacementScale")) mat.SetFloat("_DisplacementScale", 0.42f);
            if (mat.HasProperty("_DisplacementSpeed")) mat.SetFloat("_DisplacementSpeed", 0.0008f);
            if (mat.HasProperty("_HeightMapInfluence")) mat.SetFloat("_HeightMapInfluence", hasHeightMap ? 0.25f : 0f);
            SetupSiliqCaustics(mat, null, 0.58f, 1.7f, 0.00016f, new Color(0.82f, 0.98f, 1f, 1f));
            SetupMacroVariation(mat, 0.30f, 0.07f, 0.18f, 0.10f);
        }

        static void SetupSiliqFlagshipCrystal(Material mat, float opacity, bool hasHeightMap)
        {
            SetupDarkSceneResponse(mat);

            if (mat.HasProperty("_ShallowColor")) mat.SetColor("_ShallowColor", new Color(0.24f, 0.92f, 1f, 1f));
            if (mat.HasProperty("_DeepColor")) mat.SetColor("_DeepColor", new Color(0.003f, 0.07f, 0.22f, 1f));
            if (mat.HasProperty("_HorizonColor")) mat.SetColor("_HorizonColor", new Color(0.90f, 0.99f, 1f, 1f));
            if (mat.HasProperty("_TransmissionColor")) mat.SetColor("_TransmissionColor", new Color(0.34f, 1f, 0.95f, 1f));
            if (mat.HasProperty("_GlimmerColor")) mat.SetColor("_GlimmerColor", new Color(1f, 1f, 0.94f, 1f));
            if (mat.HasProperty("_Opacity")) mat.SetFloat("_Opacity", Mathf.Clamp01(opacity));
            if (mat.HasProperty("_NormalStrength")) mat.SetFloat("_NormalStrength", 0.58f);
            if (mat.HasProperty("_Tiling1")) mat.SetFloat("_Tiling1", 1.80f);
            if (mat.HasProperty("_Tiling2")) mat.SetFloat("_Tiling2", 4.60f);
            if (mat.HasProperty("_AlphaFresnel")) mat.SetFloat("_AlphaFresnel", 0.72f);
            if (mat.HasProperty("_AlphaPower")) mat.SetFloat("_AlphaPower", 1.85f);
            if (mat.HasProperty("_EdgeReflection")) mat.SetFloat("_EdgeReflection", 0.88f);
            if (mat.HasProperty("_TransmissionStrength")) mat.SetFloat("_TransmissionStrength", 0.84f);
            if (mat.HasProperty("_GlimmerIntensity")) mat.SetFloat("_GlimmerIntensity", 0.32f);
            if (mat.HasProperty("_GlimmerSharpness")) mat.SetFloat("_GlimmerSharpness", 18f);
            if (mat.HasProperty("_GlintIntensity")) mat.SetFloat("_GlintIntensity", 0.95f);
            if (mat.HasProperty("_GlintPower")) mat.SetFloat("_GlintPower", 320f);
            if (mat.HasProperty("_FresnelPower")) mat.SetFloat("_FresnelPower", 1.90f);
            if (mat.HasProperty("_ReflStrength")) mat.SetFloat("_ReflStrength", 1f);
            if (mat.HasProperty("_SpecIntensity")) mat.SetFloat("_SpecIntensity", 1.75f);
            if (mat.HasProperty("_SpecPower")) mat.SetFloat("_SpecPower", 340f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.99f);
            if (mat.HasProperty("_DisplacementStrength")) mat.SetFloat("_DisplacementStrength", 0.012f);
            if (mat.HasProperty("_DisplacementScale")) mat.SetFloat("_DisplacementScale", 0.85f);
            if (mat.HasProperty("_DisplacementSpeed")) mat.SetFloat("_DisplacementSpeed", 0.0008f);
            if (mat.HasProperty("_HeightMapInfluence")) mat.SetFloat("_HeightMapInfluence", 0f);
            SetupSiliqCaustics(mat, null, 0.64f, 2.1f, 0.00016f, new Color(0.76f, 1f, 0.98f, 1f));
            SetupMacroVariation(mat, 0.22f, 0.075f, 0.18f, 0.10f);
        }

        static void SetupSiliqCaustics(Material mat, Texture2D caustics, float strength, float scale, float speed, Color tint)
        {
            if (mat == null) return;

            if (mat.HasProperty("_CausticsMap"))
            {
                Texture texture = caustics;
                if (texture == null)
                {
                    texture = mat.GetTexture("_CausticsMap");
                }
                if (texture == null)
                {
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.siliq.water-normalmap/PrebakedPack/Textures/Water_Caustics_Crystal_01.png");
                }
                if (texture != null) mat.SetTexture("_CausticsMap", texture);
            }
            if (mat.HasProperty("_CausticsStrength")) mat.SetFloat("_CausticsStrength", strength);
            if (mat.HasProperty("_CausticsScale")) mat.SetFloat("_CausticsScale", scale);
            if (mat.HasProperty("_CausticsSpeed")) mat.SetFloat("_CausticsSpeed", speed);
            if (mat.HasProperty("_CausticsFocus")) mat.SetFloat("_CausticsFocus", Mathf.Lerp(1.15f, 2.0f, Mathf.Clamp01(strength)));
            if (mat.HasProperty("_CausticsPrismStrength")) mat.SetFloat("_CausticsPrismStrength", Mathf.Lerp(0.04f, 0.18f, Mathf.Clamp01(strength)));
            if (mat.HasProperty("_CausticsTint")) mat.SetColor("_CausticsTint", tint);
        }

        static void SetupMacroVariation(Material mat, float variation, float scale, float directionBreakup, float colorVariation)
        {
            if (mat.HasProperty("_MacroVariation")) mat.SetFloat("_MacroVariation", variation);
            if (mat.HasProperty("_MacroScale")) mat.SetFloat("_MacroScale", scale);
            if (mat.HasProperty("_MacroDirectionBreakup")) mat.SetFloat("_MacroDirectionBreakup", directionBreakup);
            if (mat.HasProperty("_MacroColorVariation")) mat.SetFloat("_MacroColorVariation", colorVariation);
        }

        static void SetupDarkSceneResponse(Material mat)
        {
            if (mat == null) return;

            if (mat.HasProperty("_MinLighting")) mat.SetFloat("_MinLighting", 0.10f);
            if (mat.HasProperty("_DarkReflectionDamping")) mat.SetFloat("_DarkReflectionDamping", 0.72f);
            if (mat.HasProperty("_DarkDetailDamping")) mat.SetFloat("_DarkDetailDamping", 0.70f);
        }
    }
}
