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
            "サイバー (細いデータ流)",
            "室内ブループール (窓反射)",
            "フラッグシップ透明水 (多層反射)",
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
                case 10: return IndoorBluePool();
                case 11: return FlagshipCrystalWater();
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

        /// <summary>浅いプール向け。光の網目は法線では浅く扱い、強くしても太いリボン状になりにくくする。</summary>
        static WaterMapSettings Pool()
        {
            var s = Base(0.42f);
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "薄い光の網目", type = WaveLayerType.VoronoiCaustics, blend = WaveBlendMode.Add,
                    amplitude = 0.12f, scale = 22, sharpness = 3.2f, jitter = 0.94f, speed = 1,
                    warpAmount = 0.25f, warpScale = 4, maskAmount = 0.45f, maskScale = 3,
                },
                new WaveLayer
                {
                    name = "浅い水面の揺らぎ", type = WaveLayerType.PerlinWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.55f, scale = 10, octaves = 4, persistence = 0.45f, speed = 1, seed = 4,
                    warpAmount = 0.18f, warpScale = 3,
                },
                new WaveLayer
                {
                    name = "細い表面波", type = WaveLayerType.DirectionalWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.26f, scale = 18, sharpness = 1.15f, directionDeg = 35f, spreadDeg = 65f,
                    waveCount = 12, speed = 1, seed = 15,
                },
            };
            return s;
        }

        /// <summary>SF 水面向け。太い格子やセルではなく、細いデータ流と走査光として使う。</summary>
        static WaterMapSettings Cyber()
        {
            var s = Base(0.62f);
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "細いデータ流", type = WaveLayerType.DirectionalWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.48f, scale = 20, sharpness = 1.0f, directionDeg = 12f, spreadDeg = 20f,
                    waveCount = 18, speed = 2, seed = 1, maskAmount = 0.36f, maskScale = 3,
                },
                new WaveLayer
                {
                    name = "斜めスキャン光", type = WaveLayerType.DirectionalWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.22f, scale = 31, sharpness = 0.95f, directionDeg = 68f, spreadDeg = 18f,
                    waveCount = 11, speed = 1, seed = 2, maskAmount = 0.45f, maskScale = 4,
                },
                new WaveLayer
                {
                    name = "微細ホログラム揺らぎ", type = WaveLayerType.RidgedWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.22f, scale = 42, octaves = 4, persistence = 0.35f, sharpness = 1.25f,
                    directionDeg = 12f, stretch = 5, speed = 2, seed = 3, warpAmount = 0.18f, warpScale = 5,
                    maskAmount = 0.28f, maskScale = 5,
                },
                new WaveLayer
                {
                    name = "透明な下地ムラ", type = WaveLayerType.PerlinWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.18f, scale = 8, octaves = 4, persistence = 0.45f, sharpness = 1.0f,
                    directionDeg = 35f, speed = 1, seed = 4, warpAmount = 0.16f, warpScale = 3,
                },
            };
            return s;
        }

        /// <summary>明るい室内プール向け。大きな窓反射が揺らぐよう、細かい法線より広い面のうねりを優先する。</summary>
        static WaterMapSettings IndoorBluePool()
        {
            var s = Base(0.36f);
            s.baseRoughness = 0.025f;
            s.slopeRoughness = 0.35f;
            s.foamThreshold = 0.92f;
            s.foamSlopeBoost = 0.10f;
            s.flowSwirl = 0.28f;
            s.flowStrength = 0.32f;
            s.dudvStrength = 0.55f;
            s.causticsIntensity = 0.90f;
            s.causticsSharpness = 1.80f;
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "窓反射を崩す大きな揺らぎ", type = WaveLayerType.DirectionalWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.58f, scale = 3, sharpness = 0.85f, directionDeg = 18f, spreadDeg = 42f,
                    waveCount = 7, speed = 1, seed = 10,
                },
                new WaveLayer
                {
                    name = "青い水面の低周波ムラ", type = WaveLayerType.PerlinWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.42f, scale = 5, octaves = 4, persistence = 0.48f, sharpness = 0.90f,
                    speed = 1, seed = 21, warpAmount = 0.22f, warpScale = 3, maskAmount = 0.28f, maskScale = 2,
                },
                new WaveLayer
                {
                    name = "反射を割る細い表面波", type = WaveLayerType.DirectionalWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.20f, scale = 14, sharpness = 0.90f, directionDeg = 72f, spreadDeg = 70f,
                    waveCount = 16, speed = 1, seed = 13, maskAmount = 0.32f, maskScale = 4,
                },
                new WaveLayer
                {
                    name = "淡い室内光のムラ", type = WaveLayerType.VoronoiCaustics, blend = WaveBlendMode.Add,
                    amplitude = 0.06f, scale = 16, sharpness = 1.80f, jitter = 0.96f, speed = 1,
                    seed = 31, warpAmount = 0.22f, warpScale = 4, maskAmount = 0.50f, maskScale = 3,
                },
            };
            return s;
        }

        /// <summary>製品デモの主役にする透明水。セル境界や大きな塊を使わず、細い反射筋と穏やかなうねりを重ねる。</summary>
        static WaterMapSettings FlagshipCrystalWater()
        {
            var s = Base(0.46f);
            s.baseRoughness = 0.018f;
            s.slopeRoughness = 0.32f;
            s.foamThreshold = 0.92f;
            s.foamSlopeBoost = 0.12f;
            s.flowSwirl = 0.22f;
            s.flowStrength = 0.30f;
            s.dudvStrength = 0.66f;
            s.causticsIntensity = 1.05f;
            s.causticsSharpness = 1.70f;
            s.layers = new[]
            {
                new WaveLayer
                {
                    name = "長い反射の流れ", type = WaveLayerType.DirectionalWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.26f, scale = 12, sharpness = 0.58f, directionDeg = 18f, spreadDeg = 24f,
                    waveCount = 14, speed = 1, seed = 41, maskAmount = 0.16f, maskScale = 3,
                },
                new WaveLayer
                {
                    name = "滑らかな透明ムラ", type = WaveLayerType.PerlinWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.18f, scale = 5, octaves = 4, persistence = 0.42f, sharpness = 0.70f,
                    speed = 1, seed = 42, warpAmount = 0.12f, warpScale = 3, maskAmount = 0.12f, maskScale = 2,
                },
                new WaveLayer
                {
                    name = "透明水の細いリップル", type = WaveLayerType.DirectionalWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.14f, scale = 34, sharpness = 0.56f, directionDeg = 42f, spreadDeg = 48f,
                    waveCount = 22, speed = 2, seed = 43, warpAmount = 0.10f, warpScale = 5,
                    maskAmount = 0.24f, maskScale = 5,
                },
                new WaveLayer
                {
                    name = "交差する薄い反射筋", type = WaveLayerType.DirectionalWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.10f, scale = 48, sharpness = 0.52f, directionDeg = -18f, spreadDeg = 64f,
                    waveCount = 24, speed = 1, seed = 44, warpAmount = 0.08f, warpScale = 6,
                    maskAmount = 0.28f, maskScale = 4,
                },
                new WaveLayer
                {
                    name = "微細な光のゆらぎ", type = WaveLayerType.RidgedWaves, blend = WaveBlendMode.Add,
                    amplitude = 0.06f, scale = 58, octaves = 3, persistence = 0.30f, sharpness = 0.64f,
                    directionDeg = 34f, stretch = 2, speed = 2, seed = 45, warpAmount = 0.08f, warpScale = 8,
                    maskAmount = 0.34f, maskScale = 5,
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
