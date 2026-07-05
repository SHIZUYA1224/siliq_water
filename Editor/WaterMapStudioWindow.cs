using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Siliq.Water.Editor
{
    /// <summary>
    /// 水面マップ生成スタジオ。
    /// メニュー: Tools > Siliq Water > 水面マップスタジオ
    /// </summary>
    public class WaterMapStudioWindow : EditorWindow
    {
        const string PrefsKey = "Siliq.Water.Settings.v2";
        const int PreviewResolution = 256;

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
            "Siliq/Water Mobile (Quest)",
            "Siliq/Water URP",
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

        int presetIndex;
        Vector2 scroll;
        readonly List<bool> layerFoldouts = new List<bool>();
        bool mapSettingsFoldout;

        [MenuItem("Tools/Siliq Water/水面マップスタジオ")]
        public static void Open()
        {
            var window = GetWindow<WaterMapStudioWindow>("水面マップスタジオ");
            window.minSize = new Vector2(440f, 660f);
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
            if (!animatePreview) return;
            double now = EditorApplication.timeSinceStartup;
            if (lastAnimTime <= 0) lastAnimTime = now;
            float dt = (float)(now - lastAnimTime);
            lastAnimTime = now;
            previewTime = (previewTime + dt * 0.15f) % 1f;
            previewDirty = true;
            Repaint();
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
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUI.BeginChangeCheck();

            DrawPresetAndProfileSection();
            GUILayout.Space(6f);
            DrawGlobalSection();
            GUILayout.Space(6f);
            DrawMapSettingsSection();
            GUILayout.Space(6f);
            DrawLayersSection();

            if (EditorGUI.EndChangeCheck())
            {
                previewDirty = true;
                SaveSettings();
            }

            GUILayout.Space(8f);
            DrawPreviewSection();
            GUILayout.Space(8f);
            DrawExportSection();

            EditorGUILayout.EndScrollView();

            if (previewDirty && Event.current.type == EventType.Repaint)
            {
                RegeneratePreview();
            }
        }

        // ---------------------------------------------------------------
        // プリセット & プロファイル
        // ---------------------------------------------------------------

        void DrawPresetAndProfileSection()
        {
            EditorGUILayout.LabelField("プリセット", EditorStyles.boldLabel);
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

            EditorGUILayout.LabelField("プロファイル (アセット保存 / 共有)", EditorStyles.boldLabel);
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
            EditorGUILayout.LabelField("全体設定", EditorStyles.boldLabel);

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
            bool ss = EditorGUILayout.Toggle(new GUIContent("スーパーサンプリング (2×)", "2 倍解像度で生成してから縮小し、鋭いエッジのジャギーを抑えます。生成時間は約 4 倍。生成解像度が 4096 を超える場合は自動的に無効になります。"), settings.supersample > 1);
            settings.supersample = ss ? 2 : 1;
            settings.exportExr = EditorGUILayout.Toggle(new GUIContent("16bit EXR で書き出し", "PNG (8bit) の代わりに EXR (16bit float) で書き出します。穏やかな水面のバンディング (縞) を根絶できます。"), settings.exportExr);
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
            EditorGUILayout.LabelField("波レイヤー", EditorStyles.boldLabel);

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

        void DrawPreviewSection()
        {
            EditorGUILayout.LabelField("プレビュー", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            previewMapIndex = GUILayout.SelectionGrid(previewMapIndex, MapTabLabels, 4);
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

            float size = Mathf.Min(EditorGUIUtility.currentViewWidth - 40f, 320f);
            Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
            rect.x = (EditorGUIUtility.currentViewWidth - size) * 0.5f;

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

        void RegeneratePreview()
        {
            previewDirty = false;
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
            EditorGUILayout.LabelField("書き出し", EditorStyles.boldLabel);

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
                "最新 Unity (URP) では同梱の \"Siliq/Water URP\" やお手持ちの水シェーダーで、\n" +
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
                    WriteImage(mapPath, colors, size);
                    ApplyImportSettings(mapPath, MapTypes[i]);
                    exported.Add((mapPath, MapTypes[i]));
                }

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
                foreach (var (p, type) in exported)
                {
                    ApplyImportSettings(p, type);
                }
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
                foreach (var kv in atlases)
                {
                    string mapPath = $"{dir}/{baseName}_{MapSuffixes[kv.Key]}.{FileExtension}";
                    WriteImage(mapPath, kv.Value, atlasW, atlasH);
                    ApplyImportSettings(mapPath, MapTypes[kv.Key]);
                    firstPath = firstPath ?? mapPath;
                }

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
                var tex = new Texture2D(w, h, TextureFormat.RGBAFloat, false, true);
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

        void ApplyImportSettings(string path, WaterMapType mapType)
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
            importer.maxTextureSize = Mathf.Max(settings.resolution, 256);

            if (settings.applyMobileImportSettings)
            {
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = "Android",
                    overridden = true,
                    maxTextureSize = Mathf.Min(settings.resolution, 1024), // Quest では 1024 以下を推奨
                    format = TextureImporterFormat.ASTC_6x6,
                });
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = "iPhone",
                    overridden = true,
                    maxTextureSize = Mathf.Min(settings.resolution, 1024), // iOS も ASTC 6x6 (Metal 対応)
                    format = TextureImporterFormat.ASTC_6x6,
                });
            }

            importer.SaveAndReimport();
        }

        // ---------------------------------------------------------------
        // マテリアル自動作成
        // ---------------------------------------------------------------

        void CreateMaterial(string path, List<(string path, WaterMapType type)> maps)
        {
            string shaderName;
            switch (materialShaderIndex)
            {
                case 1: shaderName = "Standard"; break;
                case 2: shaderName = "Universal Render Pipeline/Lit"; break;
                case 3: shaderName = "Siliq/Water Mobile (Quest)"; break;
                case 4: shaderName = "Siliq/Water URP"; break;
                default: return;
            }

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[Siliq Water] シェーダー '{shaderName}' が見つからないためマテリアル作成をスキップしました。");
                return;
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

            switch (materialShaderIndex)
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
                    }
                    mat.SetColor("_Color", new Color(0.1f, 0.35f, 0.45f, 1f));
                    mat.SetFloat("_Glossiness", 0.9f);
                    break;

                case 2: // URP Lit
                    if (normal != null)
                    {
                        mat.SetTexture("_BumpMap", normal);
                        mat.EnableKeyword("_NORMALMAP");
                    }
                    mat.SetColor("_BaseColor", new Color(0.1f, 0.35f, 0.45f, 1f));
                    mat.SetFloat("_Smoothness", 0.9f);
                    break;

                case 3: // Siliq Mobile
                case 4: // Siliq URP
                    if (normal != null)
                    {
                        mat.SetTexture("_NormalMap", normal);
                    }
                    break;
            }

            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
        }
    }
}
