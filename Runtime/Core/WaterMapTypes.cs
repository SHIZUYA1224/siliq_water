using System;
using UnityEngine;

namespace Siliq.Water
{
    /// <summary>
    /// 波レイヤーの種類。
    /// </summary>
    public enum WaveLayerType
    {
        PerlinWaves,      // fBm パーリンノイズ(穏やかな揺らぎ)
        RidgedWaves,      // リッジノイズ(尖ったうねり)
        VoronoiCells,     // ボロノイ・セル(丸い盛り上がり)
        VoronoiCaustics,  // ボロノイ・エッジ(コースティクス風の網目)
        DirectionalWaves, // 指向性のある波(ゲルストナー風 / 多波スペクトル)
        RainRipples,      // 雨の波紋(広がるリング)
    }

    /// <summary>
    /// レイヤー合成モード。
    /// </summary>
    public enum WaveBlendMode
    {
        Add,
        Multiply,
        Max,
        Min,
    }

    /// <summary>
    /// 書き出し・生成できるマップの種類。
    /// </summary>
    public enum WaterMapType
    {
        Normal,    // タンジェント空間ノーマルマップ
        Height,    // ハイトマップ (グレースケール)
        Foam,      // フォームマスク (波頭・グレースケール)
        Roughness, // ラフネス (傾斜・フォームから合成)
        Flow,      // フローマップ (RG = 流れベクトル)
        Dudv,      // DUDV / ディストーションマップ (RG = 屈折オフセット)
        Caustics,  // コースティクス近似 (グレースケール)
    }

    /// <summary>
    /// 1 枚の波レイヤー。全パラメータはシームレスタイリングと
    /// t ∈ [0,1) の完全ループを壊さないよう整数周期で設計されている。
    /// </summary>
    [Serializable]
    public class WaveLayer
    {
        public bool enabled = true;
        public string name = "波レイヤー";
        public WaveLayerType type = WaveLayerType.PerlinWaves;
        public WaveBlendMode blend = WaveBlendMode.Add;

        [Range(0f, 2f)] public float amplitude = 1f;

        // 共通: テクスチャ 1 枚あたりの繰り返し数(整数にすることでシームレスを保証)
        [Range(1, 64)] public int scale = 8;

        // ノイズ系
        [Range(1, 8)] public int octaves = 4;
        [Range(0.1f, 0.9f)] public float persistence = 0.5f;

        // 形状の鋭さ(波の頂点の尖り / ボロノイの減衰 / 波紋のリング幅)
        [Range(0.25f, 8f)] public float sharpness = 1f;

        // 指向性波・流れ
        [Range(0f, 360f)] public float directionDeg = 0f;
        [Range(0f, 90f)] public float spreadDeg = 30f;
        [Range(1, 64)] public int waveCount = 6;

        // ノイズを流れ方向 (X 軸) に引き伸ばす(川など)。整数でタイリング維持
        [Range(1, 8)] public int stretch = 1;

        // 雨の波紋
        [Range(1, 128)] public int dropCount = 24;

        // ボロノイのゆらぎ量
        [Range(0f, 1f)] public float jitter = 0.9f;

        // ドメインワープ: 模様を有機的に歪ませる (0 = なし)
        [Range(0f, 1f)] public float warpAmount = 0f;
        [Range(1, 16)] public int warpScale = 4;

        // マスク: 低周波ノイズでレイヤーの効きにムラを作る (0 = なし)
        [Range(0f, 1f)] public float maskAmount = 0f;
        [Range(1, 16)] public int maskScale = 3;

        // アニメーション速度(整数にすることで完全ループを保証)
        [Range(0, 8)] public int speed = 1;

        public int seed = 0;
        public bool invert = false;

        public WaveLayer Clone()
        {
            return (WaveLayer)MemberwiseClone();
        }
    }

    /// <summary>
    /// 生成設定一式。JsonUtility でシリアライズ可能。
    /// </summary>
    [Serializable]
    public class WaterMapSettings
    {
        public int resolution = 1024;
        [Range(0.05f, 5f)] public float strength = 1f;
        public bool autoNormalize = true;
        public bool flipY = false; // DirectX 系ツールに合わせたい場合のみ true
        public int globalSeed = 1234;

        // --- 品質 ---
        [Range(1, 2)] public int supersample = 1;             // 2 でスーパーサンプリング (エッジのジャギー低減)
        public bool exportExr = false;                        // 16bit EXR で書き出し (バンディング防止)

        // --- フォームマスク ---
        [Range(0f, 1f)] public float foamThreshold = 0.7f;    // この高さ以上の波頭にフォーム
        [Range(0.01f, 0.5f)] public float foamSoftness = 0.12f;
        [Range(0f, 2f)] public float foamSlopeBoost = 0.5f;   // 急斜面 (砕け波) への追加フォーム
        [Range(0, 4)] public int foamBlur = 1;                // フォームのぼかし回数 (3x3 ボックス)

        // --- ラフネス ---
        [Range(0f, 1f)] public float baseRoughness = 0.06f;
        [Range(0f, 2f)] public float slopeRoughness = 0.6f;   // 傾斜によるラフネス増加
        [Range(0f, 1f)] public float foamRoughness = 0.7f;    // フォーム部分のラフネス
        public bool exportSmoothness = false;                  // ON でラフネスを反転して書き出し

        // --- フローマップ ---
        [Range(0f, 1f)] public float flowSwirl = 0.5f;        // 0 = レイヤー方向のみ, 1 = 渦のみ
        [Range(0f, 1f)] public float flowStrength = 0.6f;

        // --- DUDV ---
        [Range(0f, 2f)] public float dudvStrength = 1f;

        // --- コースティクス ---
        [Range(0.05f, 8f)] public float causticsIntensity = 1f; // 曲率 → 明るさの変換強度
        [Range(0.5f, 8f)] public float causticsSharpness = 2.5f;

        // --- アニメーション書き出し ---
        [Range(2, 64)] public int frameCount = 16;

        // --- Quest / モバイル向けインポート設定を自動適用 (エディタ書き出し時) ---
        public bool applyMobileImportSettings = true;

        public WaveLayer[] layers = new WaveLayer[0];

        public WaterMapSettings Clone()
        {
            var c = (WaterMapSettings)MemberwiseClone();
            c.layers = new WaveLayer[layers.Length];
            for (int i = 0; i < layers.Length; i++) c.layers[i] = layers[i].Clone();
            return c;
        }
    }
}
