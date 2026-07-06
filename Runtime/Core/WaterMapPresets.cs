namespace Siliq.Water
{
    /// <summary>
    /// 代表的な水表現のプリセット集。ここから選んで微調整するのが基本ワークフロー。
    /// </summary>
    public static class WaterMapPresets
    {
        public static readonly string[] Names =
        {
            "穏やかな湖",
            "海のうねり",
            "外洋 (多波スペクトル)",
            "川の流れ",
            "雨の水面",
            "さざ波",
            "トゥーン / スタイライズ",
            "溶岩 / 粘性流体",
            "プール (光の網目)",
            "サイバー (人工水面)",
            "透明な浅瀬 (煌めき)",
            "リブガラス (フルート)",
            "液体金属 (クローム)",
            "マットガラス (すりガラス)",
            "結露ガラス (水滴)",
            "万華鏡 (放射クリスタル ※非タイリング)",
        };

        public static WaterMapSettings Create(int index)
        {
            switch (index)
            {
                case 0: return CalmLake();
                case 1: return OceanSwell();
                case 2: return OpenOcean();
                case 3: return RiverStream();
                case 4: return RainSurface();
                case 5: return GentleRipples();
                case 6: return StylizedToon();
                case 7: return Lava();
                case 8: return Pool();
                case 9: return Cyber();
                case 10: return ClearShallows();
                case 11: return FlutedGlass();
                case 12: return LiquidMetal();
                case 13: return FrostedGlass();
                case 14: return Condensation();
                case 15: return Kaleidoscope();
                default: return CalmLake();
            }
        }

        static WaterMapSettings Base(float strength)
        {
            return new WaterMapSettings
            {
                resolution = 1024,
                strength = strength,
                autoNormalize = true,
                flipY = false,
                globalSeed = 1234,
                frameCount = 16,
                applyMobileImportSettings = true,
            };
        }

        static WaterMapSettings CalmLake()
        {
            var s = Base(0.6f);
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "大きな揺らぎ", type = WaveLayerType.PerlinWaves, blend = WaveBlendMode.Add,
                    amplitude = 1f, scale = 4, octaves = 4, persistence = 0.5f, sharpness = 1f, speed = 1,
                },
                new WaveLayer
                {
                    name = "細かいさざ波", type = WaveLayerType.PerlinWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.25f, scale = 16, octaves = 3, persistence = 0.5f, sharpness = 1f, speed = 2, seed = 7,
                },
            };
            return s;
        }

        static WaterMapSettings OceanSwell()
        {
            var s = Base(1.4f);
            s.foamThreshold = 0.72f;
            s.foamSlopeBoost = 0.8f;
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "うねり", type = WaveLayerType.DirectionalWaves, blend = WaveBlendMode.Add,
                    amplitude = 1f, scale = 4, sharpness = 2.5f, directionDeg = 15f, spreadDeg = 35f, waveCount = 8, speed = 1,
                },
                new WaveLayer
                {
                    name = "波の乱れ", type = WaveLayerType.PerlinWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.5f, scale = 8, octaves = 5, persistence = 0.55f, speed = 1, seed = 3, warpAmount = 0.3f, warpScale = 4,
                },
                new WaveLayer
                {
                    name = "表面の網目", type = WaveLayerType.VoronoiCaustics, blend = WaveBlendMode.Add,
                    amplitude = 0.2f, scale = 20, sharpness = 1.5f, jitter = 0.9f, speed = 1, seed = 11,
                },
            };
            return s;
        }

        static WaterMapSettings OpenOcean()
        {
            var s = Base(1.6f);
            s.foamThreshold = 0.68f;
            s.foamSlopeBoost = 1f;
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "スペクトル波", type = WaveLayerType.DirectionalWaves, blend = WaveBlendMode.Add,
                    amplitude = 1f, scale = 6, sharpness = 3f, directionDeg = 0f, spreadDeg = 60f, waveCount = 32, speed = 1,
                },
                new WaveLayer
                {
                    name = "うねりの歪み", type = WaveLayerType.PerlinWaves, blend = WaveBlendMode.Multiply,
                    amplitude = 0.4f, scale = 3, octaves = 3, speed = 1, seed = 21, maskAmount = 0.5f, maskScale = 2,
                },
                new WaveLayer
                {
                    name = "細波", type = WaveLayerType.RidgedWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.25f, scale = 24, octaves = 3, sharpness = 1.5f, speed = 2, seed = 13,
                },
            };
            return s;
        }

        static WaterMapSettings RiverStream()
        {
            var s = Base(1f);
            s.flowSwirl = 0.35f;
            s.flowStrength = 0.8f;
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "流れの筋", type = WaveLayerType.PerlinWaves, blend = WaveBlendMode.Add,
                    amplitude = 1f, scale = 16, octaves = 4, persistence = 0.5f, stretch = 4, speed = 3, seed = 2,
                },
                new WaveLayer
                {
                    name = "流れの波", type = WaveLayerType.DirectionalWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.5f, scale = 8, sharpness = 1.8f, directionDeg = 0f, spreadDeg = 12f, waveCount = 5, speed = 2, seed = 5,
                },
            };
            return s;
        }

        static WaterMapSettings RainSurface()
        {
            var s = Base(1.2f);
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "雨の波紋", type = WaveLayerType.RainRipples, blend = WaveBlendMode.Add,
                    amplitude = 1f, scale = 5, octaves = 3, sharpness = 2f, dropCount = 40, speed = 1,
                },
                new WaveLayer
                {
                    name = "下地の揺らぎ", type = WaveLayerType.PerlinWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.3f, scale = 6, octaves = 4, speed = 1, seed = 9,
                },
            };
            return s;
        }

        static WaterMapSettings GentleRipples()
        {
            var s = Base(0.8f);
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "さざ波 A", type = WaveLayerType.DirectionalWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.7f, scale = 12, sharpness = 1.2f, directionDeg = 30f, spreadDeg = 50f, waveCount = 10, speed = 2,
                },
                new WaveLayer
                {
                    name = "さざ波 B", type = WaveLayerType.PerlinWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.5f, scale = 20, octaves = 3, persistence = 0.45f, speed = 2, seed = 4,
                },
            };
            return s;
        }

        static WaterMapSettings StylizedToon()
        {
            var s = Base(1.6f);
            s.causticsSharpness = 4f;
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "網目模様", type = WaveLayerType.VoronoiCaustics, blend = WaveBlendMode.Add,
                    amplitude = 1f, scale = 8, sharpness = 3f, jitter = 0.85f, speed = 1,
                },
                new WaveLayer
                {
                    name = "丸い盛り上がり", type = WaveLayerType.VoronoiCells, blend = WaveBlendMode.Add,
                    amplitude = 0.35f, scale = 8, sharpness = 2f, jitter = 0.85f, speed = 1, seed = 6,
                },
            };
            return s;
        }

        static WaterMapSettings Lava()
        {
            var s = Base(2.2f);
            s.baseRoughness = 0.5f;
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "粘性の流れ", type = WaveLayerType.RidgedWaves, blend = WaveBlendMode.Add,
                    amplitude = 1f, scale = 5, octaves = 5, persistence = 0.55f, sharpness = 1.5f, stretch = 2,
                    speed = 1, warpAmount = 0.8f, warpScale = 3,
                },
                new WaveLayer
                {
                    name = "プレート割れ目", type = WaveLayerType.VoronoiCaustics, blend = WaveBlendMode.Add,
                    amplitude = 0.6f, scale = 5, sharpness = 4f, jitter = 0.7f, speed = 0, seed = 8, invert = true,
                },
            };
            return s;
        }

        /// <summary>PrebakedPack の Water_Normal_Pool_01 と同一レシピ。</summary>
        static WaterMapSettings Pool()
        {
            var s = Base(0.95f);
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "光の網目", type = WaveLayerType.VoronoiCaustics, blend = WaveBlendMode.Add,
                    amplitude = 0.8f, scale = 9, sharpness = 1.6f, jitter = 0.95f, speed = 1,
                    warpAmount = 0.6f, warpScale = 5,
                },
                new WaveLayer
                {
                    name = "下地の揺らぎ", type = WaveLayerType.PerlinWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.55f, scale = 6, octaves = 4, speed = 1, seed = 4,
                },
            };
            return s;
        }

        /// <summary>PrebakedPack の Water_Normal_Cyber_01 と同一レシピ。</summary>
        static WaterMapSettings Cyber()
        {
            var s = Base(1.4f);
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "格子波 (横)", type = WaveLayerType.DirectionalWaves, blend = WaveBlendMode.Add,
                    amplitude = 1f, scale = 12, sharpness = 3.5f, directionDeg = 0f, spreadDeg = 0f, waveCount = 1, speed = 1, seed = 1,
                },
                new WaveLayer
                {
                    name = "格子波 (縦)", type = WaveLayerType.DirectionalWaves, blend = WaveBlendMode.Add,
                    amplitude = 1f, scale = 12, sharpness = 3.5f, directionDeg = 90f, spreadDeg = 0f, waveCount = 1, speed = 1, seed = 2,
                },
                new WaveLayer
                {
                    name = "規則ドット", type = WaveLayerType.VoronoiCells, blend = WaveBlendMode.Add,
                    amplitude = 0.4f, scale = 24, sharpness = 5f, jitter = 0.15f, speed = 1, seed = 3,
                },
            };
            return s;
        }

        /// <summary>PrebakedPack の Water_Normal_Shallows_01 と同一レシピ。透き通った浅瀬の煌めき。</summary>
        static WaterMapSettings ClearShallows()
        {
            var s = Base(1.1f);
            s.causticsSharpness = 3f;
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "煌めきの網目", type = WaveLayerType.VoronoiCaustics, blend = WaveBlendMode.Add,
                    amplitude = 1f, scale = 14, sharpness = 1.3f, jitter = 0.95f, speed = 1,
                    warpAmount = 0.4f, warpScale = 5,
                },
                new WaveLayer
                {
                    name = "細かい煌めき", type = WaveLayerType.VoronoiCaustics, blend = WaveBlendMode.Add,
                    amplitude = 0.4f, scale = 28, sharpness = 1.2f, jitter = 0.95f, speed = 2, seed = 5,
                },
                new WaveLayer
                {
                    name = "うねり", type = WaveLayerType.PerlinWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.3f, scale = 5, octaves = 3, speed = 1, seed = 9,
                },
            };
            return s;
        }

        /// <summary>PrebakedPack の Water_Normal_FlutedGlass_01 と同一レシピ。液体で歪んだ縦リブガラス。</summary>
        static WaterMapSettings FlutedGlass()
        {
            var s = Base(2.4f);
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "縦リブ", type = WaveLayerType.FlutedRibs, blend = WaveBlendMode.Add,
                    amplitude = 1f, scale = 12, sharpness = 1.4f, speed = 0,
                    warpAmount = 0.35f, warpScale = 3,
                },
                new WaveLayer
                {
                    name = "表面のゆらぎ", type = WaveLayerType.PerlinWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.12f, scale = 6, octaves = 3, speed = 1, seed = 3,
                },
            };
            return s;
        }

        /// <summary>PrebakedPack の Water_Normal_LiquidMetal_01 と同一レシピ。クロームの液だまり。</summary>
        static WaterMapSettings LiquidMetal()
        {
            var s = Base(2.4f);
            s.baseRoughness = 0.02f;
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "大きな液だまり", type = WaveLayerType.MetaBlobs, blend = WaveBlendMode.Add,
                    amplitude = 1f, scale = 4, jitter = 0.9f, sharpness = 1.2f, speed = 1,
                },
                new WaveLayer
                {
                    name = "小さな液滴", type = WaveLayerType.MetaBlobs, blend = WaveBlendMode.Add,
                    amplitude = 0.45f, scale = 8, jitter = 0.7f, sharpness = 1.5f, speed = 1, seed = 7,
                },
                new WaveLayer
                {
                    name = "うねり", type = WaveLayerType.PerlinWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.15f, scale = 3, octaves = 2, speed = 1, seed = 11,
                },
            };
            return s;
        }

        /// <summary>PrebakedPack の Water_Normal_FrostedGlass_01 と同一レシピ。すりガラスの微細グレイン。</summary>
        static WaterMapSettings FrostedGlass()
        {
            var s = Base(0.4f);
            s.baseRoughness = 0.6f;
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "微細グレイン", type = WaveLayerType.PerlinWaves, blend = WaveBlendMode.Add,
                    amplitude = 1f, scale = 48, octaves = 3, persistence = 0.6f, speed = 0,
                },
                new WaveLayer
                {
                    name = "むら", type = WaveLayerType.PerlinWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.25f, scale = 12, octaves = 2, speed = 0, seed = 4,
                },
            };
            return s;
        }

        /// <summary>PrebakedPack の Water_Normal_Condensation_01 と同一レシピ。窓の結露(水滴)。</summary>
        static WaterMapSettings Condensation()
        {
            var s = Base(1.5f);
            s.baseRoughness = 0.3f;
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "大きな水滴", type = WaveLayerType.MetaBlobs, blend = WaveBlendMode.Add,
                    amplitude = 1f, scale = 14, jitter = 0.28f, sharpness = 6f, speed = 0,
                    maskAmount = 0.45f, maskScale = 3,
                },
                new WaveLayer
                {
                    name = "小さな水滴", type = WaveLayerType.MetaBlobs, blend = WaveBlendMode.Add,
                    amplitude = 0.55f, scale = 30, jitter = 0.22f, sharpness = 6f, speed = 0, seed = 7,
                    maskAmount = 0.35f, maskScale = 4,
                },
                new WaveLayer
                {
                    name = "垂れる筋", type = WaveLayerType.RidgedWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.22f, scale = 20, octaves = 2, stretch = 6, directionDeg = 270f,
                    sharpness = 2f, speed = 1, seed = 3, maskAmount = 0.7f, maskScale = 4,
                },
                new WaveLayer
                {
                    name = "曇りグレイン", type = WaveLayerType.PerlinWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.08f, scale = 56, octaves = 2, speed = 0, seed = 9,
                },
            };
            return s;
        }

        /// <summary>
        /// PrebakedPack の Water_Normal_Kaleidoscope_01 と同一レシピ。放射状のカットガラス。
        /// ※ このプリセットは中心対称のためシームレスにタイリングしない (装飾パネル向け)。
        /// </summary>
        static WaterMapSettings Kaleidoscope()
        {
            var s = Base(2f);
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "粗ファセット", type = WaveLayerType.Kaleidoscope, blend = WaveBlendMode.Add,
                    amplitude = 1f, scale = 6, octaves = 4, waveCount = 8, sharpness = 3f, speed = 1,
                },
                new WaveLayer
                {
                    name = "細ファセット", type = WaveLayerType.Kaleidoscope, blend = WaveBlendMode.Add,
                    amplitude = 0.4f, scale = 12, octaves = 8, waveCount = 8, sharpness = 2f, speed = 1, seed = 5,
                },
            };
            return s;
        }

        /// <summary>新規レイヤーのデフォルト。</summary>
        public static WaveLayer NewLayer()
        {
            return new WaveLayer { name = "新しいレイヤー" };
        }
    }
}
