using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Siliq.Water.Editor
{
    /// <summary>
    /// 焼き済み水マテリアルを選択オブジェクトへワンクリック適用するメニュー。
    /// Hierarchy でオブジェクトを右クリック → Siliq Water → 好きな水を選ぶだけ。
    /// 適用と同時に動き用コンポーネントも付与し、静止画にならないようにする。
    /// Ripple は UV スクロールではなく、発生点から外へ広がる波紋 emitter を使う。
    /// </summary>
    public static class WaterPackQuickApply
    {
        internal enum ReadyLook
        {
            ClearSea,
            ClearPool,
            IndoorBluePool,
            WaterTable,
            FlagshipCrystal,
            CrystalLagoon,
            CrystalLagoonHero,
            BloodSea,
            LiquidMetal,
        }

        internal enum TargetPlatform
        {
            PC,
            Quest,
            Ios,
        }

        struct MotionPreset
        {
            public float dir;
            public float speed;
            public float strength;
            public float tiling;

            public MotionPreset(float dir, float speed, float strength, float tiling)
            {
                this.dir = dir;
                this.speed = speed;
                this.strength = strength;
                this.tiling = tiling;
            }
        }

        struct LookPreset
        {
            public string assetName;
            public string displayName;
            public string sourceGuid;
            public string sourceLabel;
            public string normalGuid;
            public string heightGuid;
            public MotionPreset motion;
            public bool transparent;
            public float opacity;
            public Color shallow;
            public Color deep;
            public Color horizon;
            public Color transmission;
            public Color glimmer;
            public float normalStrength;
            public float tiling1;
            public float tiling2;
            public float alphaFresnel;
            public float alphaPower;
            public float clarity;
            public float edgeReflection;
            public float refractionStrength;
            public float transmissionStrength;
            public float glimmerIntensity;
            public float glimmerSharpness;
            public float glintIntensity;
            public float glintPower;
            public float specPower;
            public float specIntensity;
            public float fresnelPower;
            public float reflStrength;
            public float reflectionPatternStrength;
            public float reflectionPatternScale;
            public float displacementStrength;
            public float displacementScale;
            public float displacementSpeed;
            public float heightMapInfluence;
            public float smoothness;
            public float minLighting;
            public float darkReflectionDamping;
            public float darkDetailDamping;
            public float macroVariation;
            public float macroScale;
            public float macroDirectionBreakup;
            public float macroColorVariation;
            public string causticsGuid;
            public float causticsStrength;
            public float causticsScale;
            public float causticsSpeed;
            public float causticsFocus;
            public float causticsPrismStrength;
            public float causticsScatterStrength;
            public float bottomVisibility;
            public float bottomLightStrength;
            public float bottomGlowStrength;
            public float depthTintStrength;
            public Color causticsTint;
        }

        // 各水の雰囲気に合わせた動き。初期値はゆっくり動く水に見える速度に抑える。
        const float MaterialScrollFromUiSpeed = 0.06f;

        static readonly MotionPreset CalmMotion = new MotionPreset(35f, 0.18f, 1.35f, 1.35f);
        static readonly MotionPreset RippleMotion = new MotionPreset(0f, 0f, 0.55f, 1.15f);
        static readonly MotionPreset StreamMotion = new MotionPreset(0f, 0.24f, 1.45f, 1.8f);
        static readonly MotionPreset PoolMotion = new MotionPreset(50f, 0.16f, 0.55f, 1.08f);
        static readonly MotionPreset CyberMotion = new MotionPreset(18f, 0.18f, 0.78f, 1.24f);

        static readonly LookPreset ClearSeaLook = new LookPreset
        {
            assetName = "ClearSea",
            displayName = "美しい海",
            sourceGuid = CalmGuid,
            sourceLabel = "Calm",
            motion = new MotionPreset(22f, 0.20f, 0.92f, 1.45f),
            transparent = true,
            opacity = 0.46f,
            shallow = new Color(0.30f, 0.88f, 1f, 1f),
            deep = new Color(0.004f, 0.10f, 0.28f, 1f),
            horizon = new Color(0.86f, 0.98f, 1f, 1f),
            transmission = new Color(0.40f, 0.98f, 1f, 1f),
            glimmer = new Color(0.97f, 1f, 1f, 1f),
            normalStrength = 0.92f,
            tiling1 = 1.35f,
            tiling2 = 3.1f,
            alphaFresnel = 0.74f,
            alphaPower = 2.20f,
            clarity = 0.64f,
            edgeReflection = 0.72f,
            refractionStrength = 0.28f,
            transmissionStrength = 0.72f,
            glimmerIntensity = 0.26f,
            glimmerSharpness = 13f,
            glintIntensity = 0.78f,
            glintPower = 220f,
            specPower = 220f,
            specIntensity = 1.35f,
            fresnelPower = 2.45f,
            reflStrength = 1f,
            reflectionPatternStrength = 0.18f,
            reflectionPatternScale = 1.05f,
            displacementStrength = 0.055f,
            displacementScale = 0.55f,
            displacementSpeed = 0.080f,
            heightMapInfluence = 0f,
            smoothness = 0.96f,
            macroVariation = 0.46f,
            macroScale = 0.10f,
            macroDirectionBreakup = 0.40f,
            macroColorVariation = 0.18f,
            causticsGuid = SunlitPoolCausticsGuid,
            causticsStrength = 0f,
            causticsScale = 1.9f,
            causticsSpeed = 0f,
            causticsFocus = 1.35f,
            causticsPrismStrength = 0f,
            causticsScatterStrength = 0f,
            bottomVisibility = 0f,
            bottomLightStrength = 0f,
            bottomGlowStrength = 0f,
            depthTintStrength = 0.38f,
            causticsTint = new Color(0.68f, 1f, 1f, 1f),
        };

        static readonly LookPreset ClearPoolLook = new LookPreset
        {
            assetName = "ClearPool",
            displayName = "透明プール",
            sourceGuid = PoolGuid,
            sourceLabel = "Pool",
            motion = new MotionPreset(50f, 0.16f, 0.38f, 1.0f),
            transparent = true,
            opacity = 0.34f,
            shallow = new Color(0.74f, 0.99f, 1f, 1f),
            deep = new Color(0.08f, 0.42f, 0.62f, 1f),
            horizon = new Color(0.92f, 1f, 1f, 1f),
            transmission = new Color(0.62f, 1f, 1f, 1f),
            glimmer = new Color(0.98f, 1f, 1f, 1f),
            normalStrength = 0.40f,
            tiling1 = 0.95f,
            tiling2 = 2.15f,
            alphaFresnel = 0.70f,
            alphaPower = 2.0f,
            clarity = 0.76f,
            edgeReflection = 0.60f,
            refractionStrength = 0.34f,
            transmissionStrength = 0.76f,
            glimmerIntensity = 0.20f,
            glimmerSharpness = 18f,
            glintIntensity = 0.52f,
            glintPower = 260f,
            specPower = 260f,
            specIntensity = 1.05f,
            fresnelPower = 2.65f,
            reflStrength = 0.92f,
            reflectionPatternStrength = 0.28f,
            reflectionPatternScale = 0.85f,
            displacementStrength = 0.018f,
            displacementScale = 1.10f,
            displacementSpeed = 0.055f,
            heightMapInfluence = 0f,
            smoothness = 0.94f,
            macroVariation = 0.24f,
            macroScale = 0.08f,
            macroDirectionBreakup = 0.22f,
            macroColorVariation = 0.10f,
            causticsGuid = SunlitPoolCausticsGuid,
            causticsStrength = 0f,
            causticsScale = 2.4f,
            causticsSpeed = 0f,
            causticsFocus = 1.55f,
            causticsPrismStrength = 0f,
            causticsScatterStrength = 0f,
            bottomVisibility = 0f,
            bottomLightStrength = 0f,
            bottomGlowStrength = 0f,
            depthTintStrength = 0.24f,
            causticsTint = new Color(0.84f, 1f, 1f, 1f),
        };

        static readonly LookPreset IndoorBluePoolLook = new LookPreset
        {
            assetName = "IndoorBluePool",
            displayName = "室内ブループール",
            sourceGuid = CalmGuid,
            sourceLabel = "Calm",
            motion = new MotionPreset(18f, 0.15f, 0.46f, 0.82f),
            transparent = true,
            opacity = 0.38f,
            shallow = new Color(0.48f, 0.92f, 1f, 1f),
            deep = new Color(0.02f, 0.22f, 0.52f, 1f),
            horizon = new Color(0.92f, 0.99f, 1f, 1f),
            transmission = new Color(0.56f, 0.98f, 1f, 1f),
            glimmer = new Color(1f, 1f, 0.96f, 1f),
            normalStrength = 0.46f,
            tiling1 = 0.78f,
            tiling2 = 1.55f,
            alphaFresnel = 0.78f,
            alphaPower = 1.85f,
            clarity = 0.72f,
            edgeReflection = 0.82f,
            refractionStrength = 0.38f,
            transmissionStrength = 0.82f,
            glimmerIntensity = 0.24f,
            glimmerSharpness = 16f,
            glintIntensity = 0.68f,
            glintPower = 260f,
            specPower = 300f,
            specIntensity = 1.50f,
            fresnelPower = 2.05f,
            reflStrength = 1f,
            reflectionPatternStrength = 0.42f,
            reflectionPatternScale = 0.78f,
            displacementStrength = 0.025f,
            displacementScale = 0.42f,
            displacementSpeed = 0.050f,
            heightMapInfluence = 0f,
            smoothness = 0.98f,
            macroVariation = 0.30f,
            macroScale = 0.07f,
            macroDirectionBreakup = 0.18f,
            macroColorVariation = 0.10f,
            causticsGuid = CrystalCausticsGuid,
            causticsStrength = 0f,
            causticsScale = 1.7f,
            causticsSpeed = 0f,
            causticsFocus = 1.45f,
            causticsPrismStrength = 0f,
            causticsScatterStrength = 0f,
            bottomVisibility = 0f,
            bottomLightStrength = 0f,
            bottomGlowStrength = 0f,
            depthTintStrength = 0.26f,
            causticsTint = new Color(0.82f, 0.98f, 1f, 1f),
        };

        static readonly LookPreset FlagshipCrystalLook = new LookPreset
        {
            assetName = "FlagshipCrystal",
            displayName = "フラッグシップ透明水",
            sourceGuid = FlagshipCrystalGuid,
            sourceLabel = "FlagshipCrystal",
            heightGuid = FlagshipCrystalHeightGuid,
            motion = new MotionPreset(26f, 0.18f, 0.58f, 1.18f),
            transparent = true,
            opacity = 0.52f,
            shallow = new Color(0.24f, 0.92f, 1f, 1f),
            deep = new Color(0.003f, 0.07f, 0.22f, 1f),
            horizon = new Color(0.90f, 0.99f, 1f, 1f),
            transmission = new Color(0.34f, 1f, 0.95f, 1f),
            glimmer = new Color(1f, 1f, 0.94f, 1f),
            normalStrength = 0.58f,
            tiling1 = 1.80f,
            tiling2 = 4.60f,
            alphaFresnel = 0.72f,
            alphaPower = 1.85f,
            clarity = 0.82f,
            edgeReflection = 0.88f,
            refractionStrength = 0.32f,
            transmissionStrength = 0.84f,
            glimmerIntensity = 0.32f,
            glimmerSharpness = 18f,
            glintIntensity = 0.95f,
            glintPower = 320f,
            specPower = 340f,
            specIntensity = 1.75f,
            fresnelPower = 1.90f,
            reflStrength = 1f,
            reflectionPatternStrength = 0.34f,
            reflectionPatternScale = 1.15f,
            displacementStrength = 0.012f,
            displacementScale = 0.85f,
            displacementSpeed = 0.060f,
            heightMapInfluence = 0f,
            smoothness = 0.99f,
            macroVariation = 0.22f,
            macroScale = 0.075f,
            macroDirectionBreakup = 0.18f,
            macroColorVariation = 0.10f,
            causticsGuid = CrystalCausticsGuid,
            causticsStrength = 0f,
            causticsScale = 2.1f,
            causticsSpeed = 0f,
            causticsFocus = 1.75f,
            causticsPrismStrength = 0f,
            causticsScatterStrength = 0f,
            bottomVisibility = 0f,
            bottomLightStrength = 0f,
            bottomGlowStrength = 0f,
            depthTintStrength = 0.32f,
            causticsTint = new Color(0.76f, 1f, 0.98f, 1f),
        };

        static readonly LookPreset WaterTableLook = new LookPreset
        {
            assetName = "WaterTable",
            displayName = "ウォーターテーブル",
            sourceGuid = WaterTableReadyGuid,
            sourceLabel = "WaterTable",
            normalGuid = WaterTableNormalGuid,
            heightGuid = WaterTableHeightGuid,
            motion = new MotionPreset(19f, 0.08f, 0.42f, 1.0f),
            transparent = true,
            opacity = 0.50f,
            shallow = new Color(0.015f, 0.22f, 0.72f, 1f),
            deep = new Color(0f, 0.015f, 0.075f, 1f),
            horizon = new Color(0.12f, 0.55f, 1f, 1f),
            transmission = new Color(0.04f, 0.42f, 1f, 1f),
            glimmer = new Color(0.66f, 0.92f, 1f, 1f),
            normalStrength = 0.42f,
            tiling1 = 1.0f,
            tiling2 = 1.08f,
            alphaFresnel = 0.92f,
            alphaPower = 1.42f,
            clarity = 0.62f,
            edgeReflection = 0.98f,
            refractionStrength = 0.24f,
            transmissionStrength = 0.55f,
            glimmerIntensity = 0.22f,
            glimmerSharpness = 24f,
            glintIntensity = 1.45f,
            glintPower = 420f,
            specPower = 460f,
            specIntensity = 2f,
            fresnelPower = 1.28f,
            reflStrength = 1f,
            reflectionPatternStrength = 0.68f,
            reflectionPatternScale = 0.55f,
            displacementStrength = 0.006f,
            displacementScale = 0.72f,
            displacementSpeed = 0.025f,
            heightMapInfluence = 0.18f,
            smoothness = 0.995f,
            minLighting = 0.08f,
            darkReflectionDamping = 0.36f,
            darkDetailDamping = 0.52f,
            macroVariation = 0.12f,
            macroScale = 0.045f,
            macroDirectionBreakup = 0.10f,
            macroColorVariation = 0.08f,
            causticsStrength = 0f,
            causticsScale = 1.4f,
            causticsSpeed = 0f,
            causticsFocus = 1.6f,
            causticsPrismStrength = 0f,
            causticsScatterStrength = 0f,
            bottomVisibility = 0f,
            bottomLightStrength = 0f,
            bottomGlowStrength = 0f,
            depthTintStrength = 0.44f,
            causticsTint = new Color(0.18f, 0.64f, 1f, 1f),
        };

        static readonly LookPreset CrystalLagoonLook = new LookPreset
        {
            assetName = "CrystalLagoon",
            displayName = "クリスタルラグーン",
            sourceGuid = FlagshipCrystalGuid,
            sourceLabel = "FlagshipCrystal",
            normalGuid = CrystalLagoonNormalGuid,
            heightGuid = CrystalLagoonHeightGuid,
            motion = new MotionPreset(20f, 0.16f, 0.42f, 0.92f),
            transparent = true,
            opacity = 0.40f,
            shallow = new Color(0.56f, 1f, 0.98f, 1f),
            deep = new Color(0.004f, 0.13f, 0.30f, 1f),
            horizon = new Color(0.96f, 1f, 1f, 1f),
            transmission = new Color(0.62f, 1f, 0.96f, 1f),
            glimmer = new Color(1f, 1f, 0.96f, 1f),
            normalStrength = 0.42f,
            tiling1 = 1.15f,
            tiling2 = 3.40f,
            alphaFresnel = 0.82f,
            alphaPower = 1.70f,
            clarity = 0.92f,
            edgeReflection = 0.92f,
            refractionStrength = 0.36f,
            transmissionStrength = 0.92f,
            glimmerIntensity = 0.38f,
            glimmerSharpness = 20f,
            glintIntensity = 1.05f,
            glintPower = 360f,
            specPower = 380f,
            specIntensity = 1.85f,
            fresnelPower = 1.75f,
            reflStrength = 1f,
            reflectionPatternStrength = 0.46f,
            reflectionPatternScale = 0.92f,
            displacementStrength = 0.006f,
            displacementScale = 0.95f,
            displacementSpeed = 0.045f,
            heightMapInfluence = 0f,
            smoothness = 0.995f,
            macroVariation = 0.18f,
            macroScale = 0.055f,
            macroDirectionBreakup = 0.14f,
            macroColorVariation = 0.08f,
            causticsGuid = CrystalLagoonCausticsGuid,
            causticsStrength = 0f,
            causticsScale = 2.25f,
            causticsSpeed = 0f,
            causticsFocus = 2.0f,
            causticsPrismStrength = 0f,
            causticsScatterStrength = 0f,
            bottomVisibility = 0f,
            bottomLightStrength = 0f,
            bottomGlowStrength = 0f,
            depthTintStrength = 0.30f,
            causticsTint = new Color(0.84f, 1f, 0.96f, 1f),
        };

        static readonly LookPreset CrystalLagoonHeroLook = new LookPreset
        {
            assetName = "CrystalLagoonHero",
            displayName = "クリスタルラグーン Hero",
            sourceGuid = FlagshipCrystalGuid,
            sourceLabel = "FlagshipCrystal",
            normalGuid = CrystalLagoonHeroNormalGuid,
            heightGuid = CrystalLagoonHeroHeightGuid,
            motion = new MotionPreset(20f, 0.14f, 0.36f, 0.82f),
            transparent = true,
            opacity = 0.34f,
            shallow = new Color(0.68f, 1f, 0.99f, 1f),
            deep = new Color(0.002f, 0.10f, 0.24f, 1f),
            horizon = new Color(0.98f, 1f, 1f, 1f),
            transmission = new Color(0.72f, 1f, 0.97f, 1f),
            glimmer = new Color(1f, 1f, 0.98f, 1f),
            normalStrength = 0.36f,
            tiling1 = 1.0f,
            tiling2 = 3.0f,
            alphaFresnel = 0.88f,
            alphaPower = 1.55f,
            clarity = 0.98f,
            edgeReflection = 0.96f,
            refractionStrength = 0.42f,
            transmissionStrength = 0.98f,
            glimmerIntensity = 0.44f,
            glimmerSharpness = 22f,
            glintIntensity = 1.2f,
            glintPower = 400f,
            specPower = 420f,
            specIntensity = 2f,
            fresnelPower = 1.55f,
            reflStrength = 1f,
            reflectionPatternStrength = 0.58f,
            reflectionPatternScale = 0.82f,
            displacementStrength = 0.004f,
            displacementScale = 0.90f,
            displacementSpeed = 0.035f,
            heightMapInfluence = 0f,
            smoothness = 0.995f,
            macroVariation = 0.16f,
            macroScale = 0.05f,
            macroDirectionBreakup = 0.16f,
            macroColorVariation = 0.07f,
            causticsGuid = CrystalLagoonHeroCausticsGuid,
            causticsStrength = 0f,
            causticsScale = 2.1f,
            causticsSpeed = 0f,
            causticsFocus = 2.4f,
            causticsPrismStrength = 0f,
            causticsScatterStrength = 0f,
            bottomVisibility = 0f,
            bottomLightStrength = 0f,
            bottomGlowStrength = 0f,
            depthTintStrength = 0.34f,
            causticsTint = new Color(0.88f, 1f, 0.97f, 1f),
        };

        static readonly LookPreset BloodSeaLook = new LookPreset
        {
            assetName = "BloodSea",
            displayName = "血の海",
            sourceGuid = StreamGuid,
            sourceLabel = "Stream",
            motion = new MotionPreset(6f, 0.14f, 0.82f, 1.18f),
            transparent = true,
            opacity = 0.68f,
            shallow = new Color(0.48f, 0.02f, 0.025f, 1f),
            deep = new Color(0.055f, 0.0f, 0.006f, 1f),
            horizon = new Color(0.36f, 0.04f, 0.035f, 1f),
            transmission = new Color(0.72f, 0.06f, 0.035f, 1f),
            glimmer = new Color(1f, 0.28f, 0.16f, 1f),
            normalStrength = 0.78f,
            tiling1 = 1.15f,
            tiling2 = 2.35f,
            alphaFresnel = 0.18f,
            alphaPower = 2.9f,
            clarity = 0.10f,
            edgeReflection = 0.30f,
            refractionStrength = 0.12f,
            transmissionStrength = 0.18f,
            glimmerIntensity = 0.08f,
            glimmerSharpness = 9f,
            glintIntensity = 0.18f,
            glintPower = 130f,
            specPower = 120f,
            specIntensity = 0.52f,
            fresnelPower = 4.2f,
            reflStrength = 0.35f,
            reflectionPatternStrength = 0.04f,
            reflectionPatternScale = 1.2f,
            displacementStrength = 0.035f,
            displacementScale = 0.65f,
            displacementSpeed = 0.060f,
            heightMapInfluence = 0f,
            smoothness = 0.86f,
            macroVariation = 0.52f,
            macroScale = 0.09f,
            macroDirectionBreakup = 0.42f,
            macroColorVariation = 0.28f,
            causticsGuid = CrystalCausticsGuid,
            causticsStrength = 0f,
            causticsScale = 1.4f,
            causticsSpeed = 0f,
            causticsFocus = 1.1f,
            causticsPrismStrength = 0f,
            causticsScatterStrength = 0f,
            bottomVisibility = 0f,
            bottomLightStrength = 0f,
            bottomGlowStrength = 0f,
            depthTintStrength = 0.55f,
            causticsTint = new Color(1f, 0.22f, 0.16f, 1f),
        };

        static readonly LookPreset LiquidMetalLook = new LookPreset
        {
            assetName = "LiquidMetal",
            displayName = "液体金属",
            sourceGuid = CyberGuid,
            sourceLabel = "Cyber",
            motion = new MotionPreset(18f, 0.18f, 0.72f, 1.38f),
            transparent = false,
            opacity = 1f,
            shallow = new Color(0.86f, 0.88f, 0.90f, 1f),
            deep = new Color(0.16f, 0.17f, 0.18f, 1f),
            horizon = new Color(0.95f, 0.97f, 1f, 1f),
            transmission = new Color(0.70f, 0.76f, 0.82f, 1f),
            glimmer = new Color(1f, 1f, 1f, 1f),
            normalStrength = 0.72f,
            tiling1 = 1.1f,
            tiling2 = 2.45f,
            alphaFresnel = 0f,
            alphaPower = 3.4f,
            clarity = 0f,
            edgeReflection = 0.95f,
            refractionStrength = 0.10f,
            transmissionStrength = 0f,
            glimmerIntensity = 0.12f,
            glimmerSharpness = 22f,
            glintIntensity = 1.2f,
            glintPower = 360f,
            specPower = 380f,
            specIntensity = 1.65f,
            fresnelPower = 1.6f,
            reflStrength = 1f,
            reflectionPatternStrength = 0.22f,
            reflectionPatternScale = 1.4f,
            displacementStrength = 0.025f,
            displacementScale = 0.85f,
            displacementSpeed = 0.040f,
            heightMapInfluence = 0f,
            smoothness = 0.99f,
            macroVariation = 0.40f,
            macroScale = 0.12f,
            macroDirectionBreakup = 0.24f,
            macroColorVariation = 0.08f,
            causticsStrength = 0f,
            causticsScale = 1.8f,
            causticsSpeed = 0f,
            causticsFocus = 1.0f,
            causticsPrismStrength = 0f,
            causticsScatterStrength = 0f,
            bottomVisibility = 0f,
            bottomLightStrength = 0f,
            bottomGlowStrength = 0f,
            depthTintStrength = 0.12f,
            causticsTint = Color.white,
        };

        // PrebakedPack/Materials/*.mat.meta の固定 GUID (旧互換用)
        const string CalmGuid = "a171aabb01c34e01a1b2c3d4e5f60201";
        const string RippleGuid = "a171aabb01c34e01a1b2c3d4e5f60202";
        const string StreamGuid = "a171aabb01c34e01a1b2c3d4e5f60203";
        const string PoolGuid = "a171aabb01c34e01a1b2c3d4e5f60204";
        const string CyberGuid = "a171aabb01c34e01a1b2c3d4e5f60205";
        const string FlagshipCrystalGuid = "a171aabb01c34e01a1b2c3d4e5f60206";
        const string FlagshipCrystalHeightGuid = "a171aabb01c34e01a1b2c3d4e5f60306";
        const string CrystalLagoonNormalGuid = "a171aabb01c34e01a1b2c3d4e5f60107";
        const string CrystalLagoonHeightGuid = "a171aabb01c34e01a1b2c3d4e5f60307";
        const string CrystalLagoonCausticsGuid = "a171aabb01c34e01a1b2c3d4e5f60407";
        const string CrystalLagoonHeroNormalGuid = "a171aabb01c34e01a1b2c3d4e5f60108";
        const string CrystalLagoonHeroHeightGuid = "a171aabb01c34e01a1b2c3d4e5f60308";
        const string CrystalLagoonHeroCausticsGuid = "a171aabb01c34e01a1b2c3d4e5f60408";
        const string SunlitPoolCausticsGuid = "a171aabb01c34e01a1b2c3d4e5f60409";
        const string WaterTableReadyGuid = "a171aabb01c34e01a1b2c3d4e5f60809";
        const string WaterTableNormalGuid = "a171aabb01c34e01a1b2c3d4e5f60109";
        const string WaterTableHeightGuid = "a171aabb01c34e01a1b2c3d4e5f60309";
        const string CrystalCausticsGuid = "00e6b1e9a23c24df69fe9558209de596";

        // PrebakedPack/ReadyMaterials/*.mat.meta の固定 GUID。
        // 通常の製品導線は、この手調整済み Material を直接割り当てる。
        const string ClearSeaReadyGuid = "2b8547535812946fd8151123f0afeda5";
        const string ClearPoolReadyGuid = "0d11aad9d06714851b748d978793f0ad";
        const string IndoorBluePoolReadyGuid = "ad71efef756f9480dabc5a3c73c8a927";
        const string FlagshipCrystalReadyGuid = "1216010767daa46eeaf0b35c759962e3";
        const string CrystalLagoonReadyGuid = "f2c657c8bc4a4c7080e6a1327818890d";
        const string CrystalLagoonHeroReadyGuid = "a171aabb01c34e01a1b2c3d4e5f60807";
        const string BloodSeaReadyGuid = "480c4411cd37646b19d1d097d9cb688c";
        const string LiquidMetalReadyGuid = "c14c01736dc8a4ddc97ceefa809d21da";

        internal const string ReadyMenuRoot = "GameObject/Siliq Water/完成水面を適用/";
        const float PcSiliqTransparentOpacity = 0.46f;
        const float StandardTransparentFallbackOpacity = 0.68f;
        const float MobileTransparentOpacity = 0.34f;

        [MenuItem(ReadyMenuRoot + "最高品質 クリスタルラグーン Hero", false, 10)]
        static void ApplyCrystalLagoonHeroLook() => ApplyReadyLook(ReadyLook.CrystalLagoonHero, TargetForActiveBuild());

        [MenuItem(ReadyMenuRoot + "クリスタルラグーン", false, 11)]
        static void ApplyCrystalLagoonLook() => ApplyReadyLook(ReadyLook.CrystalLagoon, TargetForActiveBuild());

        [MenuItem(ReadyMenuRoot + "透明プール", false, 12)]
        static void ApplyClearPoolLook() => ApplyReadyLook(ReadyLook.ClearPool, TargetForActiveBuild());

        [MenuItem(ReadyMenuRoot + "室内ブループール", false, 13)]
        static void ApplyIndoorBluePoolLook() => ApplyReadyLook(ReadyLook.IndoorBluePool, TargetForActiveBuild());

        [MenuItem(ReadyMenuRoot + "美しい海", false, 14)]
        static void ApplyClearSeaLook() => ApplyReadyLook(ReadyLook.ClearSea, TargetForActiveBuild());

        [MenuItem(ReadyMenuRoot + "ウォーターテーブル", false, 15)]
        static void ApplyWaterTableLook() => ApplyReadyLook(ReadyLook.WaterTable, TargetForActiveBuild());

        [MenuItem(ReadyMenuRoot + "フラッグシップ透明水", false, 16)]
        static void ApplyFlagshipCrystalLook() => ApplyReadyLook(ReadyLook.FlagshipCrystal, TargetForActiveBuild());

        [MenuItem(ReadyMenuRoot + "特殊液体/血の海", false, 30)]
        static void ApplyBloodSeaLook() => ApplyReadyLook(ReadyLook.BloodSea, TargetForActiveBuild());

        [MenuItem(ReadyMenuRoot + "特殊液体/液体金属", false, 31)]
        static void ApplyLiquidMetalLook() => ApplyReadyLook(ReadyLook.LiquidMetal, TargetForActiveBuild());

        [MenuItem(ReadyMenuRoot + "最高品質 クリスタルラグーン Hero", true)]
        [MenuItem(ReadyMenuRoot + "クリスタルラグーン", true)]
        [MenuItem(ReadyMenuRoot + "透明プール", true)]
        [MenuItem(ReadyMenuRoot + "室内ブループール", true)]
        [MenuItem(ReadyMenuRoot + "美しい海", true)]
        [MenuItem(ReadyMenuRoot + "ウォーターテーブル", true)]
        [MenuItem(ReadyMenuRoot + "フラッグシップ透明水", true)]
        [MenuItem(ReadyMenuRoot + "特殊液体/血の海", true)]
        [MenuItem(ReadyMenuRoot + "特殊液体/液体金属", true)]
        static bool ValidateSelection()
        {
            return HasRendererSelection();
        }

        internal static bool HasRendererSelection()
        {
            foreach (var go in Selection.gameObjects)
            {
                if (go.GetComponent<Renderer>() != null) return true;
            }
            return false;
        }

        internal static string DisplayNameForLook(ReadyLook look)
        {
            return GetLookPreset(look).displayName;
        }

        internal static string ReadyMaterialNameForLook(ReadyLook look)
        {
            switch (look)
            {
                case ReadyLook.ClearSea: return "M_Siliq_ClearSea_Ready";
                case ReadyLook.ClearPool: return "M_Siliq_ClearPool_Ready";
                case ReadyLook.IndoorBluePool: return "M_Siliq_IndoorBluePool_Ready";
                case ReadyLook.WaterTable: return "M_Siliq_WaterTable_Ready";
                case ReadyLook.FlagshipCrystal: return "M_Siliq_FlagshipCrystal_Ready";
                case ReadyLook.CrystalLagoon: return "M_Siliq_CrystalLagoon_Ready";
                case ReadyLook.CrystalLagoonHero: return "M_Siliq_CrystalLagoon_Hero_Ready";
                case ReadyLook.BloodSea: return "M_Siliq_BloodSea_Ready";
                case ReadyLook.LiquidMetal: return "M_Siliq_LiquidMetal_Ready";
                default: return "M_Siliq_CrystalLagoon_Hero_Ready";
            }
        }

        internal static Material ReadyMaterialForLook(ReadyLook look)
        {
            string guid;
            switch (look)
            {
                case ReadyLook.ClearSea: guid = ClearSeaReadyGuid; break;
                case ReadyLook.ClearPool: guid = ClearPoolReadyGuid; break;
                case ReadyLook.IndoorBluePool: guid = IndoorBluePoolReadyGuid; break;
                case ReadyLook.WaterTable: guid = WaterTableReadyGuid; break;
                case ReadyLook.FlagshipCrystal: guid = FlagshipCrystalReadyGuid; break;
                case ReadyLook.CrystalLagoon: guid = CrystalLagoonReadyGuid; break;
                case ReadyLook.CrystalLagoonHero: guid = CrystalLagoonHeroReadyGuid; break;
                case ReadyLook.BloodSea: guid = BloodSeaReadyGuid; break;
                case ReadyLook.LiquidMetal: guid = LiquidMetalReadyGuid; break;
                default: guid = CrystalLagoonHeroReadyGuid; break;
            }

            return LoadPrebakedMaterial(guid, ReadyMaterialNameForLook(look));
        }

        internal static TargetPlatform TargetForActiveBuild()
        {
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.Android:
                    return TargetPlatform.Quest;
                case BuildTarget.iOS:
                    return TargetPlatform.Ios;
                default:
                    return TargetPlatform.PC;
            }
        }

        internal static void ApplyReadyLookToSelection(ReadyLook look, TargetPlatform target)
        {
            ApplyReadyLook(look, target);
        }

        static LookPreset GetLookPreset(ReadyLook look)
        {
            switch (look)
            {
                case ReadyLook.ClearSea: return ClearSeaLook;
                case ReadyLook.ClearPool: return ClearPoolLook;
                case ReadyLook.IndoorBluePool: return IndoorBluePoolLook;
                case ReadyLook.WaterTable: return WaterTableLook;
                case ReadyLook.FlagshipCrystal: return FlagshipCrystalLook;
                case ReadyLook.CrystalLagoon: return CrystalLagoonLook;
                case ReadyLook.CrystalLagoonHero: return CrystalLagoonHeroLook;
                case ReadyLook.BloodSea: return BloodSeaLook;
                case ReadyLook.LiquidMetal: return LiquidMetalLook;
                default: return CrystalLagoonHeroLook;
            }
        }

        static void Apply(string guid, string label, MotionPreset motion, bool transparent = false)
        {
            var mat = LoadPrebakedMaterial(guid, $"M_Water_{label}");
            if (mat == null)
            {
                EditorUtility.DisplayDialog("Siliq Water",
                    $"M_Water_{label} が見つかりませんでした。\nPrebakedPack フォルダがプロジェクトに含まれているか確認してください。", "OK");
                return;
            }
            if (transparent || !WaterShaderUtility.IsMaterialCompatibleWithCurrentPipeline(mat))
            {
                mat = GetOrCreatePipelineMaterial(mat, label, transparent, transparent ? PcSiliqTransparentOpacity : 1f);
                if (mat == null)
                {
                    EditorUtility.DisplayDialog("Siliq Water",
                        "現在の Render Pipeline で安全に使える水マテリアルを作成できませんでした。URP の場合は Universal Render Pipeline package と Pipeline Asset を確認してください。", "OK");
                    return;
                }
            }

            string texturePropertyName = mat != null && mat.HasProperty("_NormalMap") ? "_NormalMap" : "_BumpMap";
            ApplyMaterialToSelection(mat, motion, texturePropertyName);
        }

        static void ApplyRipplePreset(bool transparent, bool mobileTransparent)
        {
            var source = LoadPrebakedMaterial(RippleGuid, "M_Water_Ripple");
            if (source == null)
            {
                EditorUtility.DisplayDialog("Siliq Water",
                    "M_Water_Ripple が見つかりませんでした。\nPrebakedPack フォルダがプロジェクトに含まれているか確認してください。", "OK");
                return;
            }

            var baseNormalSource = LoadPrebakedMaterial(CalmGuid, "M_Water_Calm") ?? source;
            var mat = GetOrCreateExpandingRippleMaterial(source, baseNormalSource, transparent, mobileTransparent);
            if (mat == null)
            {
                EditorUtility.DisplayDialog("Siliq Water",
                    "現在の Render Pipeline で安全に使える水マテリアルを作成できませんでした。", "OK");
                return;
            }

            string texturePropertyName = mat.HasProperty("_NormalMap") ? "_NormalMap" : "_BumpMap";
            ApplyMaterialToSelection(mat, RippleMotion, texturePropertyName, mat.HasProperty("_UseRipples"));
        }

        static void ApplyMobileTransparent(string guid, string label, MotionPreset motion)
        {
            var source = LoadPrebakedMaterial(guid, $"M_Water_{label}");
            if (source == null)
            {
                EditorUtility.DisplayDialog("Siliq Water",
                    $"M_Water_{label} が見つかりませんでした。\nPrebakedPack フォルダがプロジェクトに含まれているか確認してください。", "OK");
                return;
            }

            var mat = GetOrCreatePipelineMaterial(source, label, true, MobileTransparentOpacity, "_iOS_Transparent");
            if (mat == null)
            {
                EditorUtility.DisplayDialog("Siliq Water",
                    "現在の Render Pipeline で安全に使える透明水マテリアルを作成できませんでした。", "OK");
                return;
            }

            string texturePropertyName = mat.HasProperty("_NormalMap") ? "_NormalMap" : "_BumpMap";
            ApplyMaterialToSelection(mat, motion, texturePropertyName);
        }

        static void ApplyReadyLook(ReadyLook look, TargetPlatform target)
        {
            LookPreset preset = GetLookPreset(look);
            Material mat = ReadyMaterialForLook(look);
            if (mat == null)
            {
                EditorUtility.DisplayDialog("Siliq Water",
                    $"{ReadyMaterialNameForLook(look)} が見つかりませんでした。\nPackage の PrebakedPack/ReadyMaterials を確認してください。", "OK");
                return;
            }

            if (!WaterShaderUtility.IsMaterialCompatibleWithCurrentPipeline(mat))
            {
                string pipelineHelp = WaterShaderUtility.IsUniversalPipelineActive()
                    ? "この完成Materialは Built-in / VRChat 用です。URPでは Package Manager の Samples から URP Shader をImportしてください。品質の異なる自動変換Materialは作成しません。"
                    : "現在のRender Pipelineでは同梱の完成Materialを使用できません。Built-in / VRChat環境、または対応するURP Sampleを使用してください。";
                EditorUtility.DisplayDialog("Siliq Water",
                    pipelineHelp, "OK");
                return;
            }

            string texturePropertyName = mat.HasProperty("_NormalMap") ? "_NormalMap" : "_BumpMap";
            ApplyMaterialToSelection(mat, preset.motion, texturePropertyName, false, target);
        }

        static void ApplyMaterialToSelection(
            Material mat,
            MotionPreset motion,
            string texturePropertyName,
            bool expandingRipples = false,
            TargetPlatform target = TargetPlatform.PC)
        {
            int applied = 0;
            foreach (var go in Selection.gameObjects)
            {
                var renderer = go.GetComponent<Renderer>();
                if (renderer == null) continue;
                Undo.RecordObject(renderer, "水マテリアルを適用");
                renderer.sharedMaterial = mat;

                var animator = go.GetComponent<WaterSurfaceAnimator>();
                if (animator == null)
                {
                    animator = Undo.AddComponent<WaterSurfaceAnimator>(go);
                }
                else
                {
                    Undo.RecordObject(animator, "水マテリアルを適用");
                }
                animator.texturePropertyName = texturePropertyName;
                animator.SyncLookFromMaterial();
                animator.directionDegrees = motion.dir;
                animator.speed = motion.speed;
                animator.strength = motion.strength;
                animator.tiling = motion.tiling;
                TuneAnimatorForTarget(animator, mat, target);
                animator.ApplyImmediate(0f);
                EditorUtility.SetDirty(animator);

                ConfigureRippleEmitter(go, renderer, expandingRipples);
                applied++;
            }

            if (applied == 0)
            {
                EditorUtility.DisplayDialog("Siliq Water",
                    "Renderer を持つオブジェクトを選択してから実行してください。", "OK");
            }
        }

        static void TuneAnimatorForTarget(WaterSurfaceAnimator animator, Material mat, TargetPlatform target)
        {
            if (animator == null || target == TargetPlatform.PC) return;

            bool opaqueLiquid = mat != null && mat.HasProperty("_Opacity") && mat.GetFloat("_Opacity") >= 0.99f;
            bool quest = target == TargetPlatform.Quest;

            animator.displacementStrength = Mathf.Min(animator.displacementStrength, quest ? 0.004f : 0f);
            animator.heightMapInfluence = 0f;
            animator.reflectionStrength = Mathf.Min(animator.reflectionStrength, quest ? 0.74f : 0.68f);
            animator.reflectionPatternStrength = Mathf.Min(animator.reflectionPatternStrength, quest ? 0.22f : 0.18f);
            animator.sparkle = Mathf.Min(animator.sparkle, quest ? 0.20f : 0.18f);
            animator.highlightStrength = Mathf.Min(animator.highlightStrength, quest ? 1.35f : 1.20f);
            animator.strength = Mathf.Min(animator.strength, quest ? 0.50f : 0.42f);

            if (!opaqueLiquid)
            {
                animator.opacity = quest
                    ? Mathf.Clamp(animator.opacity, 0.42f, 0.58f)
                    : Mathf.Min(animator.opacity, 0.38f);
            }
        }

        static Material LoadPrebakedMaterial(string guid, string materialName)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            string[] guids = AssetDatabase.FindAssets($"{materialName} t:Material");
            foreach (string foundGuid in guids)
            {
                string foundPath = AssetDatabase.GUIDToAssetPath(foundGuid);
                if (foundPath.EndsWith($"/PrebakedPack/Materials/{materialName}.mat") ||
                    foundPath.EndsWith($"/{materialName}.mat"))
                {
                    mat = AssetDatabase.LoadAssetAtPath<Material>(foundPath);
                    if (mat != null) return mat;
                }
            }
            return null;
        }

        static Texture LoadTexture(string guid, string assetName)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var texture = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Texture>(path);
            if (texture != null) return texture;

            if (string.IsNullOrEmpty(assetName)) return null;
            string[] guids = AssetDatabase.FindAssets($"{assetName} t:Texture2D");
            foreach (string foundGuid in guids)
            {
                string foundPath = AssetDatabase.GUIDToAssetPath(foundGuid);
                if (foundPath.EndsWith($"/PrebakedPack/Textures/{assetName}.png") ||
                    foundPath.EndsWith($"/{assetName}.png"))
                {
                    texture = AssetDatabase.LoadAssetAtPath<Texture>(foundPath);
                    if (texture != null) return texture;
                }
            }
            return null;
        }

        static Material GetOrCreateExpandingRippleMaterial(Material colorSource, Material normalSource, bool transparent, bool mobileTransparent)
        {
            int shaderIndex = WaterShaderUtility.BestSiliqMaterialShaderIndex();
            if (shaderIndex == WaterShaderUtility.NoMaterialIndex) return null;

            string shaderName = WaterShaderUtility.ShaderNameForMaterialIndex(shaderIndex);
            Shader shader = WaterShaderUtility.FindUsableShaderForCurrentPipeline(shaderName);
            if (shader == null) return null;

            const string root = "Assets/SiliqWater";
            const string folder = root + "/GeneratedMaterials";
            EnsureFolder("Assets", "SiliqWater");
            EnsureFolder(root, "GeneratedMaterials");

            string suffix = transparent ? (mobileTransparent ? "_iOS_Transparent" : "_Transparent") : "_Expanding";
            string path = $"{folder}/M_Water_Ripple{suffix}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            mat.name = $"M_Water_Ripple{suffix}";
            float opacity = transparent ? (mobileTransparent ? MobileTransparentOpacity : PcSiliqTransparentOpacity) : 1f;
            SetupGeneratedWaterMaterial(mat, shaderIndex, colorSource, normalSource, transparent, opacity, 0.48f, 0.11f, 0.42f, 0.20f);
            if (mat.HasProperty("_UseRipples"))
            {
                SetupExpandingRippleMaterial(mat);
            }
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        static Material GetOrCreatePipelineMaterial(Material source, string label, bool transparent, float opacity, string suffixOverride = null)
        {
            const string root = "Assets/SiliqWater";
            const string folder = root + "/GeneratedMaterials";
            EnsureFolder("Assets", "SiliqWater");
            EnsureFolder(root, "GeneratedMaterials");

            int shaderIndex = WaterShaderUtility.BestSiliqMaterialShaderIndex();
            if (shaderIndex == WaterShaderUtility.NoMaterialIndex)
            {
                shaderIndex = WaterShaderUtility.BestFallbackMaterialShaderIndex();
            }
            if (shaderIndex == WaterShaderUtility.NoMaterialIndex) return null;

            string shaderName = WaterShaderUtility.ShaderNameForMaterialIndex(shaderIndex);
            Shader shader = WaterShaderUtility.FindUsableShaderForCurrentPipeline(shaderName);
            if (shader == null) return null;

            string suffix = suffixOverride ?? (transparent ? "_Transparent" : "_Compatible");
            string path = $"{folder}/M_Water_{label}{suffix}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            mat.name = $"M_Water_{label}{suffix}";
            SetupGeneratedWaterMaterial(mat, shaderIndex, source, source, transparent, opacity, 0.42f, 0.10f, 0.36f, 0.18f);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        static Material GetOrCreateLookMaterial(LookPreset preset, Material source, TargetPlatform target = TargetPlatform.PC)
        {
            int shaderIndex = WaterShaderUtility.BestSiliqMaterialShaderIndex();
            if (shaderIndex == WaterShaderUtility.NoMaterialIndex)
            {
                shaderIndex = WaterShaderUtility.BestFallbackMaterialShaderIndex();
            }
            if (shaderIndex == WaterShaderUtility.NoMaterialIndex) return null;

            string shaderName = WaterShaderUtility.ShaderNameForMaterialIndex(shaderIndex);
            Shader shader = WaterShaderUtility.FindUsableShaderForCurrentPipeline(shaderName);
            if (shader == null) return null;

            const string root = "Assets/SiliqWater";
            const string folder = root + "/GeneratedMaterials";
            EnsureFolder("Assets", "SiliqWater");
            EnsureFolder(root, "GeneratedMaterials");

            string suffix = target == TargetPlatform.PC ? string.Empty : "_" + target;
            string path = $"{folder}/M_Water_Look_{preset.assetName}{suffix}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            mat.name = $"M_Water_Look_{preset.assetName}{suffix}";
            if (shaderIndex == WaterShaderUtility.SiliqMobileIndex || shaderIndex == WaterShaderUtility.SiliqUrpIndex)
            {
                SetupSiliqLook(mat, preset, source);
            }
            else
            {
                SetupFallbackLook(mat, preset, source, shaderIndex);
            }
            TuneLookForTarget(mat, preset, target);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        static void TuneLookForTarget(Material mat, LookPreset preset, TargetPlatform target)
        {
            if (mat == null || target == TargetPlatform.PC) return;
            bool liquidMetal = preset.assetName == "LiquidMetal";

            if (mat.HasProperty("_DisplacementStrength"))
            {
                float limit = target == TargetPlatform.Quest ? 0.004f : 0f;
                mat.SetFloat("_DisplacementStrength", Mathf.Min(mat.GetFloat("_DisplacementStrength"), limit));
            }
            if (mat.HasProperty("_HeightMapInfluence")) mat.SetFloat("_HeightMapInfluence", 0f);
            if (mat.HasProperty("_ReflStrength")) mat.SetFloat("_ReflStrength", Mathf.Min(mat.GetFloat("_ReflStrength"), target == TargetPlatform.Quest ? 0.74f : 0.68f));
            if (mat.HasProperty("_ReflectionPatternStrength")) mat.SetFloat("_ReflectionPatternStrength", Mathf.Min(mat.GetFloat("_ReflectionPatternStrength"), target == TargetPlatform.Quest ? 0.22f : 0.18f));
            if (mat.HasProperty("_GlimmerIntensity")) mat.SetFloat("_GlimmerIntensity", Mathf.Min(mat.GetFloat("_GlimmerIntensity"), target == TargetPlatform.Quest ? 0.20f : 0.18f));
            if (mat.HasProperty("_GlintIntensity")) mat.SetFloat("_GlintIntensity", Mathf.Min(mat.GetFloat("_GlintIntensity"), target == TargetPlatform.Quest ? 0.55f : 0.48f));
            if (mat.HasProperty("_NormalStrength")) mat.SetFloat("_NormalStrength", Mathf.Min(mat.GetFloat("_NormalStrength"), target == TargetPlatform.Quest ? 0.50f : 0.42f));
            if (mat.HasProperty("_CausticsStrength")) mat.SetFloat("_CausticsStrength", 0f);
            if (mat.HasProperty("_CausticsSpeed")) mat.SetFloat("_CausticsSpeed", Mathf.Min(mat.GetFloat("_CausticsSpeed"), target == TargetPlatform.Quest ? 0.0000045f : 0.0000035f));

            if (!liquidMetal && mat.HasProperty("_Opacity"))
            {
                float opacity = target == TargetPlatform.Quest
                    ? Mathf.Clamp(mat.GetFloat("_Opacity"), 0.42f, 0.58f)
                    : Mathf.Min(mat.GetFloat("_Opacity"), 0.38f);
                mat.SetFloat("_Opacity", opacity);
            }
        }

        static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        static void SetupStandardTransparent(Material mat, float opacity)
        {
            if (mat == null) return;

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
            mat.EnableKeyword("_NORMALMAP");
        }

        static void SetupStandardOpaque(Material mat)
        {
            if (mat == null) return;

            if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.One);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 1f);
            if (mat.HasProperty("_Color"))
            {
                Color c = mat.GetColor("_Color");
                c.a = 1f;
                mat.SetColor("_Color", c);
            }

            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = (int)RenderQueue.Geometry;
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.EnableKeyword("_NORMALMAP");
        }

        static void ApplyLitNormalAndColor(Material mat, Material source, float opacity)
        {
            if (mat == null) return;

            if (source != null && source.HasProperty("_BumpMap") && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
                mat.EnableKeyword("_NORMALMAP");
            }
            else if (source != null && source.HasProperty("_NormalMap") && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", source.GetTexture("_NormalMap"));
                mat.EnableKeyword("_NORMALMAP");
            }
            if (source != null && source.HasProperty("_BumpScale") && mat.HasProperty("_BumpScale"))
            {
                mat.SetFloat("_BumpScale", Mathf.Max(1f, source.GetFloat("_BumpScale")));
            }
            else if (source != null && source.HasProperty("_NormalStrength") && mat.HasProperty("_BumpScale"))
            {
                mat.SetFloat("_BumpScale", Mathf.Max(1f, source.GetFloat("_NormalStrength")));
            }

            Color color = new Color(0.08f, 0.34f, 0.45f, Mathf.Clamp01(opacity));
            if (source != null && source.HasProperty("_Color"))
            {
                Color sourceColor = source.GetColor("_Color");
                color = new Color(sourceColor.r, sourceColor.g, sourceColor.b, Mathf.Clamp01(opacity));
            }
            else if (source != null && source.HasProperty("_ShallowColor"))
            {
                Color sourceColor = source.GetColor("_ShallowColor");
                color = new Color(sourceColor.r, sourceColor.g, sourceColor.b, Mathf.Clamp01(opacity));
            }

            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.92f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.92f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        }

        static void SetupGeneratedWaterMaterial(
            Material mat,
            int shaderIndex,
            Material colorSource,
            Material normalSource,
            bool transparent,
            float opacity,
            float macroVariation,
            float macroScale,
            float macroDirectionBreakup,
            float macroColorVariation)
        {
            if (mat == null) return;

            if (shaderIndex == WaterShaderUtility.SiliqMobileIndex || shaderIndex == WaterShaderUtility.SiliqUrpIndex)
            {
                ApplySiliqNormal(mat, normalSource);
                ApplySiliqWaterPalette(mat, colorSource);
                SetupMacroVariation(mat, macroVariation, macroScale, macroDirectionBreakup, macroColorVariation);
                if (transparent)
                {
                    SetupSiliqMobileTransparent(mat, opacity);
                }
                else
                {
                    SetupSiliqOpaque(mat);
                }
                return;
            }

            ApplyLitNormalAndColor(mat, normalSource ?? colorSource, opacity);
            if (shaderIndex == WaterShaderUtility.UrpLitIndex)
            {
                if (transparent) SetupUrpLitTransparent(mat, opacity);
                else SetupUrpLitOpaque(mat);
                return;
            }

            if (transparent) SetupStandardTransparent(mat, opacity);
            else SetupStandardOpaque(mat);
        }

        static void SetupFallbackLook(Material mat, LookPreset preset, Material source, int shaderIndex)
        {
            if (mat == null) return;

            float opacity = preset.transparent ? preset.opacity : 1f;
            ApplyLitNormalAndColor(mat, source, opacity);

            Color color = Color.Lerp(preset.deep, preset.shallow, 0.55f);
            color.a = opacity;
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", preset.smoothness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", preset.smoothness);
            if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", preset.assetName == "LiquidMetal" ? 1f : 0f);
            }

            if (shaderIndex == WaterShaderUtility.UrpLitIndex)
            {
                if (preset.transparent) SetupUrpLitTransparent(mat, opacity);
                else SetupUrpLitOpaque(mat);
            }
            else
            {
                if (preset.transparent) SetupStandardTransparent(mat, opacity);
                else SetupStandardOpaque(mat);
            }
        }

        static void SetupUrpLitTransparent(Material mat, float opacity)
        {
            if (mat == null) return;

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

        static void SetupUrpLitOpaque(Material mat)
        {
            if (mat == null) return;

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.One);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 1f);
            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = 1f;
                mat.SetColor("_BaseColor", c);
            }

            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = (int)RenderQueue.Geometry;
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_NORMALMAP");
        }

        static void SetupSiliqOpaque(Material mat)
        {
            if (mat == null) return;

            SetupDarkSceneResponse(mat);
            if (mat.HasProperty("_Opacity")) mat.SetFloat("_Opacity", 1f);
            if (mat.HasProperty("_Clarity")) mat.SetFloat("_Clarity", 0f);
            if (mat.HasProperty("_AlphaFresnel")) mat.SetFloat("_AlphaFresnel", 0f);
            if (mat.HasProperty("_EdgeReflection")) mat.SetFloat("_EdgeReflection", 0f);
            if (mat.HasProperty("_RefractionStrength")) mat.SetFloat("_RefractionStrength", 0f);
            if (mat.HasProperty("_ReflectionPatternStrength")) mat.SetFloat("_ReflectionPatternStrength", 0f);
            if (mat.HasProperty("_CausticsMap")) mat.SetTexture("_CausticsMap", null);
            if (mat.HasProperty("_CausticsStrength")) mat.SetFloat("_CausticsStrength", 0f);
            if (mat.HasProperty("_CausticsSpeed")) mat.SetFloat("_CausticsSpeed", 0f);
            if (mat.HasProperty("_CausticsPrismStrength")) mat.SetFloat("_CausticsPrismStrength", 0f);
            if (mat.HasProperty("_CausticsScatterStrength")) mat.SetFloat("_CausticsScatterStrength", 0f);
            if (mat.HasProperty("_BottomVisibility")) mat.SetFloat("_BottomVisibility", 0f);
            if (mat.HasProperty("_BottomLightStrength")) mat.SetFloat("_BottomLightStrength", 0f);
            if (mat.HasProperty("_BottomGlowStrength")) mat.SetFloat("_BottomGlowStrength", 0f);
            if (mat.HasProperty("_DepthTintStrength")) mat.SetFloat("_DepthTintStrength", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.One);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 1f);

            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = (int)RenderQueue.Geometry;
        }

        static void SetupSiliqMobileTransparent(Material mat, float opacity)
        {
            if (mat == null) return;

            SetupDarkSceneResponse(mat);
            if (mat.HasProperty("_Opacity")) mat.SetFloat("_Opacity", Mathf.Clamp01(opacity));
            if (mat.HasProperty("_Clarity")) mat.SetFloat("_Clarity", 0.70f);
            if (mat.HasProperty("_AlphaFresnel")) mat.SetFloat("_AlphaFresnel", 0.74f);
            if (mat.HasProperty("_AlphaPower")) mat.SetFloat("_AlphaPower", 2.05f);
            if (mat.HasProperty("_EdgeReflection")) mat.SetFloat("_EdgeReflection", 0.70f);
            if (mat.HasProperty("_RefractionStrength")) mat.SetFloat("_RefractionStrength", 0.28f);
            if (mat.HasProperty("_BottomGlowStrength")) mat.SetFloat("_BottomGlowStrength", 0f);
            if (mat.HasProperty("_DepthTintStrength")) mat.SetFloat("_DepthTintStrength", 0.28f);
            if (mat.HasProperty("_TransmissionStrength")) mat.SetFloat("_TransmissionStrength", 0.70f);
            if (mat.HasProperty("_GlimmerIntensity")) mat.SetFloat("_GlimmerIntensity", 0.28f);
            if (mat.HasProperty("_GlimmerSharpness")) mat.SetFloat("_GlimmerSharpness", 14f);
            if (mat.HasProperty("_GlintIntensity")) mat.SetFloat("_GlintIntensity", 0.74f);
            if (mat.HasProperty("_GlintPower")) mat.SetFloat("_GlintPower", 220f);
            if (mat.HasProperty("_FresnelPower")) mat.SetFloat("_FresnelPower", 2.35f);
            if (mat.HasProperty("_ReflStrength")) mat.SetFloat("_ReflStrength", 1f);
            if (mat.HasProperty("_ReflectionPatternStrength")) mat.SetFloat("_ReflectionPatternStrength", 0.24f);
            if (mat.HasProperty("_ReflectionPatternScale")) mat.SetFloat("_ReflectionPatternScale", 1.0f);
            if (mat.HasProperty("_SpecIntensity")) mat.SetFloat("_SpecIntensity", 1.35f);
            if (mat.HasProperty("_SpecPower")) mat.SetFloat("_SpecPower", 220f);
            if (mat.HasProperty("_DisplacementStrength")) mat.SetFloat("_DisplacementStrength", 0.04f);
            if (mat.HasProperty("_DisplacementScale")) mat.SetFloat("_DisplacementScale", 0.70f);
            if (mat.HasProperty("_DisplacementSpeed")) mat.SetFloat("_DisplacementSpeed", 0.060f);
            if (mat.HasProperty("_HeightMapInfluence")) mat.SetFloat("_HeightMapInfluence", 0f);
            if (mat.HasProperty("_CausticsMap")) mat.SetTexture("_CausticsMap", null);
            if (mat.HasProperty("_CausticsStrength")) mat.SetFloat("_CausticsStrength", 0f);
            if (mat.HasProperty("_CausticsScale")) mat.SetFloat("_CausticsScale", 1.8f);
            if (mat.HasProperty("_CausticsSpeed")) mat.SetFloat("_CausticsSpeed", 0f);
            if (mat.HasProperty("_CausticsFocus")) mat.SetFloat("_CausticsFocus", 1.35f);
            if (mat.HasProperty("_CausticsPrismStrength")) mat.SetFloat("_CausticsPrismStrength", 0f);
            if (mat.HasProperty("_CausticsScatterStrength")) mat.SetFloat("_CausticsScatterStrength", 0f);
            if (mat.HasProperty("_BottomVisibility")) mat.SetFloat("_BottomVisibility", 0f);
            if (mat.HasProperty("_BottomLightStrength")) mat.SetFloat("_BottomLightStrength", 0f);
            if (mat.HasProperty("_BottomGlowStrength")) mat.SetFloat("_BottomGlowStrength", 0f);
            if (mat.HasProperty("_DepthTintStrength")) mat.SetFloat("_DepthTintStrength", 0.28f);
            if (mat.HasProperty("_CausticsTint")) mat.SetColor("_CausticsTint", new Color(0.78f, 1f, 1f, 1f));
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);

            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        static void SetupSiliqBlend(Material mat, bool transparent, float opacity)
        {
            if (mat == null) return;

            if (mat.HasProperty("_Opacity")) mat.SetFloat("_Opacity", Mathf.Clamp01(opacity));

            if (!mat.HasProperty("_SrcBlend") || !mat.HasProperty("_DstBlend") || !mat.HasProperty("_ZWrite"))
            {
                return;
            }

            if (transparent)
            {
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                mat.SetFloat("_SrcBlend", (float)BlendMode.One);
                mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
                mat.SetFloat("_ZWrite", 1f);
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.renderQueue = (int)RenderQueue.Geometry;
            }
        }

        static void SetupExpandingRippleMaterial(Material mat)
        {
            if (mat == null) return;

            if (mat.HasProperty("_UseRipples")) mat.SetFloat("_UseRipples", 1f);
            if (mat.HasProperty("_RippleSpeed")) mat.SetFloat("_RippleSpeed", 1.85f);
            if (mat.HasProperty("_RippleWidth")) mat.SetFloat("_RippleWidth", 0.46f);
            if (mat.HasProperty("_RippleLifetime")) mat.SetFloat("_RippleLifetime", 4.2f);
            if (mat.HasProperty("_RippleAmplitude")) mat.SetFloat("_RippleAmplitude", 0.45f);
            if (mat.HasProperty("_RippleChannel")) mat.SetFloat("_RippleChannel", 0f);
            if (mat.HasProperty("_Scroll1")) mat.SetVector("_Scroll1", Vector4.zero);
            if (mat.HasProperty("_Scroll2")) mat.SetVector("_Scroll2", Vector4.zero);
            if (mat.HasProperty("_NormalStrength")) mat.SetFloat("_NormalStrength", 0.55f);
            if (mat.HasProperty("_Tiling1")) mat.SetFloat("_Tiling1", 1.15f);
            if (mat.HasProperty("_Tiling2")) mat.SetFloat("_Tiling2", 2.1f);
            if (mat.HasProperty("_DisplacementStrength")) mat.SetFloat("_DisplacementStrength", 0.022f);
            if (mat.HasProperty("_DisplacementScale")) mat.SetFloat("_DisplacementScale", 0.90f);
            if (mat.HasProperty("_DisplacementSpeed")) mat.SetFloat("_DisplacementSpeed", 0.045f);
            if (mat.HasProperty("_HeightMapInfluence")) mat.SetFloat("_HeightMapInfluence", 0f);
            mat.EnableKeyword("_USE_RIPPLES");
        }

        static void SetupMacroVariation(Material mat, float variation, float scale, float directionBreakup, float colorVariation)
        {
            if (mat == null) return;

            if (mat.HasProperty("_MacroVariation")) mat.SetFloat("_MacroVariation", variation);
            if (mat.HasProperty("_MacroScale")) mat.SetFloat("_MacroScale", scale);
            if (mat.HasProperty("_MacroDirectionBreakup")) mat.SetFloat("_MacroDirectionBreakup", directionBreakup);
            if (mat.HasProperty("_MacroColorVariation")) mat.SetFloat("_MacroColorVariation", colorVariation);
        }

        static void SetupDarkSceneResponse(Material mat)
        {
            SetupDarkSceneResponse(mat, default);
        }

        static void SetupDarkSceneResponse(Material mat, LookPreset preset)
        {
            if (mat == null) return;

            float minLighting = preset.minLighting > 0f ? preset.minLighting : 0.10f;
            float darkReflectionDamping = preset.darkReflectionDamping > 0f ? preset.darkReflectionDamping : 0.72f;
            float darkDetailDamping = preset.darkDetailDamping > 0f ? preset.darkDetailDamping : 0.70f;
            if (mat.HasProperty("_MinLighting")) mat.SetFloat("_MinLighting", minLighting);
            if (mat.HasProperty("_DarkReflectionDamping")) mat.SetFloat("_DarkReflectionDamping", darkReflectionDamping);
            if (mat.HasProperty("_DarkDetailDamping")) mat.SetFloat("_DarkDetailDamping", darkDetailDamping);
        }

        static void ApplySiliqNormal(Material mat, Material source, LookPreset preset)
        {
            if (mat == null) return;

            if (!string.IsNullOrEmpty(preset.normalGuid) && mat.HasProperty("_NormalMap"))
            {
                Texture normal = LoadTexture(preset.normalGuid, $"Water_Normal_{preset.assetName}_01");
                if (normal != null)
                {
                    mat.SetTexture("_NormalMap", normal);
                    return;
                }
            }

            if (source == null) return;

            if (source.HasProperty("_BumpMap") && mat.HasProperty("_NormalMap"))
            {
                mat.SetTexture("_NormalMap", source.GetTexture("_BumpMap"));
            }
            if (source.HasProperty("_BumpScale") && mat.HasProperty("_NormalStrength"))
            {
                mat.SetFloat("_NormalStrength", Mathf.Max(1f, source.GetFloat("_BumpScale")));
            }
        }

        static void ApplySiliqNormal(Material mat, Material source)
        {
            ApplySiliqNormal(mat, source, default);
        }

        static void ApplySiliqHeight(Material mat, Material source, string heightGuid, string sourceLabel, float influence)
        {
            if (mat == null || !mat.HasProperty("_HeightMap")) return;

            Texture height = null;
            if (!string.IsNullOrEmpty(heightGuid))
            {
                height = LoadTexture(heightGuid, $"Water_Height_{sourceLabel}_01");
            }
            if (height == null && source != null && source.HasProperty("_ParallaxMap"))
            {
                height = source.GetTexture("_ParallaxMap");
            }

            if (height != null)
            {
                mat.SetTexture("_HeightMap", height);
            }
            if (mat.HasProperty("_HeightMapInfluence"))
            {
                mat.SetFloat("_HeightMapInfluence", influence);
            }
        }

        static void ApplySiliqCaustics(Material mat, LookPreset preset)
        {
            if (mat == null || !mat.HasProperty("_CausticsMap")) return;
            if (string.IsNullOrEmpty(preset.causticsGuid) || preset.causticsStrength <= 0f)
            {
                mat.SetTexture("_CausticsMap", null);
                return;
            }

            Texture caustics = LoadTexture(preset.causticsGuid, "Water_Caustics_Crystal_01");
            if (caustics != null)
            {
                mat.SetTexture("_CausticsMap", caustics);
            }
        }

        static void ApplySiliqWaterPalette(Material mat, Material source)
        {
            if (mat == null) return;

            Color sourceColor = new Color(0.1f, 0.35f, 0.45f, 1f);
            if (source != null && source.HasProperty("_Color"))
            {
                sourceColor = source.GetColor("_Color");
            }

            Color shallow = Color.Lerp(sourceColor, new Color(0.62f, 0.96f, 1f, 1f), 0.45f);
            Color deep = Color.Lerp(sourceColor, new Color(0.005f, 0.09f, 0.16f, 1f), 0.58f);
            if (mat.HasProperty("_ShallowColor")) mat.SetColor("_ShallowColor", shallow);
            if (mat.HasProperty("_DeepColor")) mat.SetColor("_DeepColor", deep);
            if (mat.HasProperty("_HorizonColor")) mat.SetColor("_HorizonColor", new Color(0.82f, 0.94f, 1f, 1f));
            if (mat.HasProperty("_TransmissionColor")) mat.SetColor("_TransmissionColor", new Color(0.35f, 0.9f, 1f, 1f));
            if (mat.HasProperty("_GlimmerColor")) mat.SetColor("_GlimmerColor", new Color(0.92f, 0.99f, 1f, 1f));
        }

        static void SetupSiliqLook(Material mat, LookPreset preset, Material source)
        {
            if (mat == null) return;

            ApplySiliqNormal(mat, source, preset);
            ApplySiliqHeight(mat, source, preset.heightGuid, preset.sourceLabel, preset.heightMapInfluence);
            ApplySiliqCaustics(mat, preset);
            SetupSiliqBlend(mat, preset.transparent, preset.opacity);

            if (mat.HasProperty("_ShallowColor")) mat.SetColor("_ShallowColor", preset.shallow);
            if (mat.HasProperty("_DeepColor")) mat.SetColor("_DeepColor", preset.deep);
            if (mat.HasProperty("_HorizonColor")) mat.SetColor("_HorizonColor", preset.horizon);
            if (mat.HasProperty("_TransmissionColor")) mat.SetColor("_TransmissionColor", preset.transmission);
            if (mat.HasProperty("_GlimmerColor")) mat.SetColor("_GlimmerColor", preset.glimmer);

            if (mat.HasProperty("_Opacity")) mat.SetFloat("_Opacity", Mathf.Clamp01(preset.opacity));
            if (mat.HasProperty("_DetailStrength")) mat.SetFloat("_DetailStrength", 0f);
            if (mat.HasProperty("_SunSheen")) mat.SetFloat("_SunSheen", 0f);
            if (mat.HasProperty("_NormalStrength")) mat.SetFloat("_NormalStrength", preset.normalStrength);
            if (mat.HasProperty("_Tiling1")) mat.SetFloat("_Tiling1", preset.tiling1);
            if (mat.HasProperty("_Tiling2")) mat.SetFloat("_Tiling2", preset.tiling2);
            if (mat.HasProperty("_AlphaFresnel")) mat.SetFloat("_AlphaFresnel", preset.alphaFresnel);
            if (mat.HasProperty("_AlphaPower")) mat.SetFloat("_AlphaPower", preset.alphaPower);
            if (mat.HasProperty("_Clarity")) mat.SetFloat("_Clarity", preset.clarity);
            if (mat.HasProperty("_EdgeReflection")) mat.SetFloat("_EdgeReflection", preset.edgeReflection);
            if (mat.HasProperty("_RefractionStrength")) mat.SetFloat("_RefractionStrength", preset.refractionStrength);
            if (mat.HasProperty("_TransmissionStrength")) mat.SetFloat("_TransmissionStrength", preset.transmissionStrength);
            if (mat.HasProperty("_GlimmerIntensity")) mat.SetFloat("_GlimmerIntensity", preset.glimmerIntensity);
            if (mat.HasProperty("_GlimmerSharpness")) mat.SetFloat("_GlimmerSharpness", preset.glimmerSharpness);
            if (mat.HasProperty("_GlintIntensity")) mat.SetFloat("_GlintIntensity", preset.glintIntensity);
            if (mat.HasProperty("_GlintPower")) mat.SetFloat("_GlintPower", preset.glintPower);
            if (mat.HasProperty("_SpecPower")) mat.SetFloat("_SpecPower", preset.specPower);
            if (mat.HasProperty("_SpecIntensity")) mat.SetFloat("_SpecIntensity", preset.specIntensity);
            if (mat.HasProperty("_FresnelPower")) mat.SetFloat("_FresnelPower", preset.fresnelPower);
            if (mat.HasProperty("_ReflStrength")) mat.SetFloat("_ReflStrength", preset.reflStrength);
            if (mat.HasProperty("_ReflectionPatternStrength")) mat.SetFloat("_ReflectionPatternStrength", preset.reflectionPatternStrength);
            if (mat.HasProperty("_ReflectionPatternScale")) mat.SetFloat("_ReflectionPatternScale", preset.reflectionPatternScale);
            if (mat.HasProperty("_DisplacementStrength")) mat.SetFloat("_DisplacementStrength", preset.displacementStrength);
            if (mat.HasProperty("_DisplacementScale")) mat.SetFloat("_DisplacementScale", preset.displacementScale);
            if (mat.HasProperty("_DisplacementSpeed")) mat.SetFloat("_DisplacementSpeed", preset.displacementSpeed);
            if (mat.HasProperty("_HeightMapInfluence")) mat.SetFloat("_HeightMapInfluence", preset.heightMapInfluence);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", preset.smoothness);
            if (mat.HasProperty("_CausticsStrength")) mat.SetFloat("_CausticsStrength", preset.causticsStrength);
            if (mat.HasProperty("_CausticsScale")) mat.SetFloat("_CausticsScale", preset.causticsScale);
            if (mat.HasProperty("_CausticsSpeed")) mat.SetFloat("_CausticsSpeed", preset.causticsSpeed);
            if (mat.HasProperty("_CausticsFocus")) mat.SetFloat("_CausticsFocus", Mathf.Max(0.5f, preset.causticsFocus));
            if (mat.HasProperty("_CausticsPrismStrength")) mat.SetFloat("_CausticsPrismStrength", Mathf.Clamp01(preset.causticsPrismStrength));
            if (mat.HasProperty("_CausticsScatterStrength")) mat.SetFloat("_CausticsScatterStrength", Mathf.Clamp01(preset.causticsScatterStrength));
            if (mat.HasProperty("_BottomVisibility")) mat.SetFloat("_BottomVisibility", Mathf.Clamp(preset.bottomVisibility, 0f, 2f));
            if (mat.HasProperty("_BottomLightStrength")) mat.SetFloat("_BottomLightStrength", preset.bottomLightStrength);
            if (mat.HasProperty("_BottomGlowStrength")) mat.SetFloat("_BottomGlowStrength", preset.bottomGlowStrength);
            if (mat.HasProperty("_DepthTintStrength")) mat.SetFloat("_DepthTintStrength", preset.depthTintStrength);
            if (mat.HasProperty("_CausticsTint")) mat.SetColor("_CausticsTint", preset.causticsTint);

            if (mat.HasProperty("_Scroll1"))
            {
                Vector2 primary = MotionVector(preset.motion.dir, preset.motion.speed);
                mat.SetVector("_Scroll1", new Vector4(primary.x, primary.y, 0f, 0f));
            }
            if (mat.HasProperty("_Scroll2"))
            {
                Vector2 primary = MotionVector(preset.motion.dir + 92f, preset.motion.speed * 0.73f);
                mat.SetVector("_Scroll2", new Vector4(primary.x, primary.y, 0f, 0f));
            }

            SetupMacroVariation(mat, preset.macroVariation, preset.macroScale, preset.macroDirectionBreakup, preset.macroColorVariation);
            SetupDarkSceneResponse(mat, preset);

            if (mat.HasProperty("_UseFlowMap")) mat.SetFloat("_UseFlowMap", 0f);
            if (mat.HasProperty("_UseShore")) mat.SetFloat("_UseShore", 0f);
            mat.DisableKeyword("_USE_FLOWMAP");
            mat.DisableKeyword("_SHORE_EFFECTS");
            mat.DisableKeyword("_USE_RIPPLES");
        }

        static Vector2 MotionVector(float directionDegrees, float speed)
        {
            float rad = directionDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed * MaterialScrollFromUiSpeed;
        }

        static void ConfigureRippleEmitter(GameObject go, Renderer renderer, bool enabled)
        {
            var emitter = go.GetComponent<WaterRippleEmitter>();
            if (!enabled)
            {
                if (emitter != null)
                {
                    Undo.RecordObject(emitter, "水マテリアルを適用");
                    emitter.enabled = false;
                    EditorUtility.SetDirty(emitter);
                }
                return;
            }

            if (emitter == null)
            {
                emitter = Undo.AddComponent<WaterRippleEmitter>(go);
            }
            else
            {
                Undo.RecordObject(emitter, "水マテリアルを適用");
            }

            emitter.enabled = true;
            emitter.targetRenderer = renderer;
            emitter.waterSurfaceY = renderer.bounds.center.y;
            emitter.rippleChannel = 0;
            emitter.ripplesPerSecond = 0.9f;
            emitter.burstCount = 1;
            emitter.rippleSpeed = 1.85f;
            emitter.rippleWidth = 0.46f;
            emitter.rippleLifetime = 4.2f;
            emitter.rippleAmplitude = 0.45f;
            emitter.applyShaderSettings = true;
            emitter.ApplyImmediate();
            EditorUtility.SetDirty(emitter);
        }
    }
}
