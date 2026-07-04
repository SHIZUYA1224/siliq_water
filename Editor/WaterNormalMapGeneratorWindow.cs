using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Siliq.WaterNormalMap
{
    /// <summary>
    /// 水面ノーマルマップ生成ウィンドウ。
    /// メニュー: Tools > Siliq Water > 水面ノーマルマップ生成
    /// </summary>
    public class WaterNormalMapGeneratorWindow : EditorWindow
    {
        const string PrefsKey = "Siliq.WaterNormalMap.Settings";
        const int PreviewResolution = 256;

        [SerializeField] WaterNormalMapSettings settings;

        Texture2D previewTexture;
        bool previewDirty = true;
        bool previewTiled;
        float previewTime;
        bool animatePreview;
        double lastAnimTime;

        int presetIndex;
        Vector2 scroll;
        readonly List<bool> layerFoldouts = new List<bool>();

        static readonly int[] ResolutionOptions = { 256, 512, 1024, 2048, 4096 };
        static readonly string[] ResolutionLabels = { "256", "512", "1024", "2048", "4096" };

        [MenuItem("Tools/Siliq Water/水面ノーマルマップ生成")]
        public static void Open()
        {
            var window = GetWindow<WaterNormalMapGeneratorWindow>("水面ノーマルマップ");
            window.minSize = new Vector2(420f, 640f);
        }

        void OnEnable()
        {
            LoadSettings();
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
                    settings = JsonUtility.FromJson<WaterNormalMapSettings>(json);
                }
                catch
                {
                    settings = null;
                }
            }
            if (settings == null || settings.layers == null || settings.layers.Length == 0)
            {
                settings = WaterNormalMapPresets.Create(0);
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

            DrawPresetSection();
            GUILayout.Space(6f);
            DrawGlobalSection();
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

        void DrawPresetSection()
        {
            EditorGUILayout.LabelField("プリセット", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                presetIndex = EditorGUILayout.Popup(presetIndex, WaterNormalMapPresets.Names);
                if (GUILayout.Button("適用", GUILayout.Width(60f)))
                {
                    Undo.RecordObject(this, "プリセット適用");
                    settings = WaterNormalMapPresets.Create(presetIndex);
                    layerFoldouts.Clear();
                    previewDirty = true;
                    SaveSettings();
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.HelpBox("プリセットを起点に、下のレイヤーを追加・調整して好みの水面を作れます。生成されるマップは必ずシームレスにタイリングします。", MessageType.Info);
        }

        void DrawGlobalSection()
        {
            EditorGUILayout.LabelField("全体設定", EditorStyles.boldLabel);

            int resIndex = Mathf.Max(0, Array.IndexOf(ResolutionOptions, settings.resolution));
            resIndex = EditorGUILayout.Popup("書き出し解像度", resIndex, ResolutionLabels);
            settings.resolution = ResolutionOptions[resIndex];

            settings.strength = EditorGUILayout.Slider(new GUIContent("凹凸の強さ", "ノーマルの傾きの強さ。大きいほど波が立って見えます。"), settings.strength, 0.05f, 5f);
            settings.autoNormalize = EditorGUILayout.Toggle(new GUIContent("高さを自動正規化", "高さの範囲を [-1,1] に広げてコントラストを最大化します。"), settings.autoNormalize);
            settings.flipY = EditorGUILayout.Toggle(new GUIContent("Y (緑) を反転", "Unity 標準は OFF。DirectX 系ツールに合わせる場合のみ ON。"), settings.flipY);
            settings.globalSeed = EditorGUILayout.IntField(new GUIContent("シード", "全体の乱数シード。変えると別バリエーションになります。"), settings.globalSeed);
            settings.applyMobileImportSettings = EditorGUILayout.Toggle(
                new GUIContent("Quest 向けインポート設定を自動適用", "書き出し時に ノーマルマップ / Repeat / Android=ASTC 6x6 を自動設定します。"),
                settings.applyMobileImportSettings);
        }

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
                layers.Add(WaterNormalMapPresets.NewLayer());
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
                "指向性の波 (ゲルストナー風)",
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
                    layer.waveCount = EditorGUILayout.IntSlider(new GUIContent("波の本数", "重ねる正弦波の数"), layer.waveCount, 1, 24);
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
            previewTexture = WaterNormalMapCore.GenerateNormalMapTexture(settings, PreviewResolution, previewTime);
            previewTexture.hideFlags = HideFlags.HideAndDontSave;
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

            if (GUILayout.Button("ノーマルマップを書き出し (PNG)", GUILayout.Height(32f)))
            {
                ExportSingle(isNormalMap: true);
            }
            if (GUILayout.Button("ハイトマップを書き出し (PNG)"))
            {
                ExportSingle(isNormalMap: false);
            }

            GUILayout.Space(4f);
            settings.frameCount = EditorGUILayout.IntSlider(new GUIContent("アニメのフレーム数", "連番 / アトラス書き出しのフレーム数。完全ループします。"), settings.frameCount, 2, 64);

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
                "Quest (VRChat モバイル) で使う場合:\n" +
                "・アバター → VRChat/Mobile/Standard Lite の Normal Map スロットにセット\n" +
                "・ワールド → 任意のカスタムシェーダー可。同梱の \"Siliq/Water Mobile\" も利用できます\n" +
                "・アニメはノーマルマップ 1 枚を UV スクロールするのが最軽量です",
                MessageType.None);
        }

        void ExportSingle(bool isNormalMap)
        {
            string defaultName = isNormalMap ? "WaterNormal" : "WaterHeight";
            string path = EditorUtility.SaveFilePanelInProject(
                isNormalMap ? "ノーマルマップを書き出し" : "ハイトマップを書き出し",
                defaultName, "png", "保存先を選択してください");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                EditorUtility.DisplayProgressBar("水面マップ生成", "生成中...", 0.3f);

                int size = settings.resolution;
                float[] heights = WaterNormalMapCore.GenerateHeightField(settings, size, previewTime);
                Color32[] pixels = isNormalMap
                    ? WaterNormalMapCore.HeightsToNormalPixels(heights, size, settings.strength, settings.flipY)
                    : WaterNormalMapCore.HeightsToGrayscalePixels(heights, size);

                EditorUtility.DisplayProgressBar("水面マップ生成", "PNG 書き出し中...", 0.7f);
                WritePng(path, pixels, size);
                ApplyImportSettings(path, isNormalMap);

                EditorUtility.DisplayProgressBar("水面マップ生成", "完了", 1f);
                var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (asset != null) EditorGUIUtility.PingObject(asset);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        void ExportSequence()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "連番アニメを書き出し", "WaterNormal_anim", "png",
                "ベース名を指定してください (連番が付きます)");
            if (string.IsNullOrEmpty(path)) return;

            string dir = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string baseName = Path.GetFileNameWithoutExtension(path);
            int frames = settings.frameCount;
            int size = settings.resolution;

            try
            {
                AssetDatabase.StartAssetEditing();
                var exported = new List<string>();
                for (int i = 0; i < frames; i++)
                {
                    EditorUtility.DisplayProgressBar("連番アニメ書き出し", $"フレーム {i + 1} / {frames}", (float)i / frames);
                    float t = (float)i / frames;
                    float[] heights = WaterNormalMapCore.GenerateHeightField(settings, size, t);
                    Color32[] pixels = WaterNormalMapCore.HeightsToNormalPixels(heights, size, settings.strength, settings.flipY);
                    string framePath = $"{dir}/{baseName}_{i:D3}.png";
                    WritePng(framePath, pixels, size, refresh: false);
                    exported.Add(framePath);
                }
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
                foreach (string p in exported)
                {
                    ApplyImportSettings(p, isNormalMap: true);
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

            if (Mathf.Max(atlasW, atlasH) > 8192)
            {
                EditorUtility.DisplayDialog("アトラスが大きすぎます",
                    $"アトラスサイズが {atlasW}×{atlasH} になり 8192 を超えます。\n解像度またはフレーム数を下げてください。", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "アトラスを書き出し", $"WaterNormal_atlas_{cols}x{rows}", "png", "保存先を選択してください");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var atlas = new Color32[atlasW * atlasH];
                for (int i = 0; i < frames; i++)
                {
                    EditorUtility.DisplayProgressBar("アトラス書き出し", $"フレーム {i + 1} / {frames}", (float)i / frames);
                    float t = (float)i / frames;
                    float[] heights = WaterNormalMapCore.GenerateHeightField(settings, cellSize, t);
                    Color32[] pixels = WaterNormalMapCore.HeightsToNormalPixels(heights, cellSize, settings.strength, settings.flipY);

                    // 左上から行優先で配置 (テクスチャ座標は下から上なので行を反転)
                    int col = i % cols;
                    int row = i / cols;
                    int dstX = col * cellSize;
                    int dstY = (rows - 1 - row) * cellSize;
                    for (int y = 0; y < cellSize; y++)
                    {
                        Array.Copy(pixels, y * cellSize, atlas, (dstY + y) * atlasW + dstX, cellSize);
                    }
                }

                EditorUtility.DisplayProgressBar("アトラス書き出し", "PNG 書き出し中...", 0.95f);
                var tex = new Texture2D(atlasW, atlasH, TextureFormat.RGBA32, false, true);
                tex.SetPixels32(atlas);
                tex.Apply(false, false);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                ApplyImportSettings(path, isNormalMap: true);

                var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (asset != null) EditorGUIUtility.PingObject(asset);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static void WritePng(string path, Color32[] pixels, int size, bool refresh = true)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            DestroyImmediate(tex);
            if (refresh)
            {
                AssetDatabase.ImportAsset(path);
            }
        }

        void ApplyImportSettings(string path, bool isNormalMap)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            if (isNormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
            }
            else
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = false;
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
            }

            importer.SaveAndReimport();
        }
    }
}
