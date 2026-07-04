using System.Collections.Generic;

namespace Siliq.WaterNormalMap
{
    /// <summary>
    /// 代表的な水表現のプリセット集。ここから選んで微調整するのが基本ワークフロー。
    /// </summary>
    public static class WaterNormalMapPresets
    {
        public static readonly string[] Names =
        {
            "穏やかな湖",
            "海のうねり",
            "川の流れ",
            "雨の水面",
            "さざ波",
            "トゥーン / スタイライズ",
        };

        public static WaterNormalMapSettings Create(int index)
        {
            switch (index)
            {
                case 0: return CalmLake();
                case 1: return OceanSwell();
                case 2: return RiverStream();
                case 3: return RainSurface();
                case 4: return GentleRipples();
                case 5: return StylizedToon();
                default: return CalmLake();
            }
        }

        static WaterNormalMapSettings Base(float strength)
        {
            return new WaterNormalMapSettings
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

        static WaterNormalMapSettings CalmLake()
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

        static WaterNormalMapSettings OceanSwell()
        {
            var s = Base(1.4f);
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
                    amplitude = 0.5f, scale = 8, octaves = 5, persistence = 0.55f, speed = 1, seed = 3,
                },
                new WaveLayer
                {
                    name = "表面の網目", type = WaveLayerType.VoronoiCaustics, blend = WaveBlendMode.Add,
                    amplitude = 0.2f, scale = 20, sharpness = 1.5f, jitter = 0.9f, speed = 1, seed = 11,
                },
            };
            return s;
        }

        static WaterNormalMapSettings RiverStream()
        {
            var s = Base(1f);
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

        static WaterNormalMapSettings RainSurface()
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

        static WaterNormalMapSettings GentleRipples()
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

        static WaterNormalMapSettings StylizedToon()
        {
            var s = Base(1.6f);
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

        /// <summary>新規レイヤーのデフォルト。</summary>
        public static WaveLayer NewLayer()
        {
            return new WaveLayer { name = "新しいレイヤー" };
        }
    }
}
