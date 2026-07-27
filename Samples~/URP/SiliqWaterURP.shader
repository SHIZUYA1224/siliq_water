// Universal Render Pipeline (URP) 向け水面シェーダー。SRP Batcher 対応。
// 本ツールで生成したノーマル / フロー / フォーム / コースティクスマップをフル活用します:
// - ノーマル 3 レイヤー (大 / 中 / 微細) + 距離によるディテール LOD でチラつきを抑制
// - 大きなうねり (スウェル) を頂点変位と法線傾斜として合成
// - _CameraOpaqueTexture による本物のスクリーンスペース屈折
// - Beer-Lambert 吸収による水深カラー + 波頭の透過光 (SSS)
// - GGX スペキュラ + 影 + 追加ライト + リフレクションプローブ
// - 水底へ投影されるコースティクスと深度ベースの岸辺フォーム
Shader "Siliq/Water URP"
{
    Properties
    {
        _ShallowColor ("浅い水の色", Color) = (0.16, 0.55, 0.60, 1)
        _DeepColor ("深い水の色", Color) = (0.02, 0.15, 0.25, 1)
        _ScatterColor ("水中の散乱色", Color) = (0.10, 0.42, 0.48, 1)
        _Opacity ("不透明度", Range(0, 1)) = 0.9
        _RefractionStrength ("水越しの揺らぎ", Range(0, 1)) = 0.18

        [Toggle(_SCREEN_REFRACTION)] _UseRefraction ("本物の屈折を使う (Opaque Texture 必須)", Float) = 0
        _AbsorptionDepth ("水の透明距離 (m)", Range(0.05, 40)) = 4
        _EdgeSoftness ("水際のなじませ幅 (m)", Range(0.01, 4)) = 0.35

        [NoScaleOffset] _NormalMap ("水面ノーマルマップ", 2D) = "bump" {}
        _NormalStrength ("ノーマル強度", Range(0, 2)) = 1
        _Tiling1 ("レイヤー1 タイリング", Float) = 1
        _Tiling2 ("レイヤー2 タイリング", Float) = 2.7
        _Scroll1 ("レイヤー1 スクロール (XY)", Vector) = (0.01, 0.004, 0, 0)
        _Scroll2 ("レイヤー2 スクロール (XY)", Vector) = (-0.003, 0.007, 0, 0)
        _DetailStrength ("近くの細かい波", Range(0, 1)) = 0.25
        _DetailTiling ("細かい波の細かさ", Range(2, 24)) = 7.3
        _DetailDistance ("細かい波が消える距離", Range(1, 200)) = 24
        _SpecularAA ("遠景のちらつき防止", Range(0, 1)) = 0.9

        _SwellNormalStrength ("大きなうねりの傾き", Range(0, 1)) = 0.28
        _SwellHeight ("大きなうねりの高さ (頂点変位)", Range(0, 2)) = 0
        _SwellScale ("大きなうねりの波長", Range(0.01, 2)) = 0.09
        _SwellSpeed ("大きなうねりの速さ", Range(0, 2)) = 0.35

        _MacroVariation ("大きなムラ", Range(0, 1)) = 0.35
        _MacroScale ("大きなムラのスケール", Range(0.01, 1)) = 0.12
        _MacroDirectionBreakup ("方向の崩し", Range(0, 1)) = 0.28
        _MacroColorVariation ("色と光のムラ", Range(0, 1)) = 0.16

        [NoScaleOffset] _CausticsMap ("水底の光マップ", 2D) = "black" {}
        _CausticsStrength ("水底の光の強さ", Range(0, 1)) = 0
        _CausticsScale ("水底の光の細かさ", Range(0.2, 8)) = 1.8
        _CausticsSpeed ("水底の光の速度", Range(0, 0.25)) = 0.00016
        _CausticsFocus ("水底光の焦点", Range(0.5, 4)) = 1.4
        _CausticsPrismStrength ("水底光の色分散", Range(0, 1)) = 0.12
        _CausticsScatterStrength ("水底光の柔らかい広がり", Range(0, 1)) = 0.25
        _CausticsDepthFade ("水底光が届く深さ (m)", Range(0.1, 30)) = 6
        _BottomVisibility ("水底の見え方", Range(0, 2)) = 1
        _BottomLightStrength ("水底光の透け", Range(0, 2)) = 1
        _CausticsTint ("水底の光の色", Color) = (0.75, 1, 1, 1)

        _Smoothness ("スムースネス", Range(0, 1)) = 0.92
        _FresnelPower ("フレネルの鋭さ", Range(0.5, 8)) = 5
        _ReflStrength ("反射の強さ (リフレクションプローブ)", Range(0, 1)) = 0.8
        _SunSheen ("太陽の広がる光沢", Range(0, 1)) = 0
        _SssStrength ("波の透過光 (SSS)", Range(0, 2)) = 0.55

        [Toggle(_USE_FLOWMAP)] _UseFlowMap ("フローマップを使う", Float) = 0
        [NoScaleOffset] _FlowMap ("フローマップ (RG)", 2D) = "grey" {}
        _FlowSpeed ("フロー速度", Range(0, 2)) = 0.18
        _FlowIntensity ("フロー強度", Range(0, 1)) = 0.3

        [Toggle(_SHORE_EFFECTS)] _UseShore ("岸辺エフェクトを使う (深度テクスチャ必須)", Float) = 0
        _ShoreDepth ("岸辺の色変化の深さ", Range(0.05, 10)) = 1.5
        _ShoreFoamWidth ("岸辺フォームの幅", Range(0.01, 5)) = 0.6
        [NoScaleOffset] _FoamMap ("フォームマスク (R)", 2D) = "white" {}
        _FoamTiling ("フォームのタイリング", Float) = 2
        _FoamColor ("フォームの色", Color) = (1, 1, 1, 1)

        [Toggle(_USE_RIPPLES)] _UseRipples ("触れた時の波紋を有効化", Float) = 0
        _RippleSpeed ("波紋の広がる速さ", Range(0.1, 10)) = 1.85
        _RippleWidth ("波紋の幅", Range(0.05, 2)) = 0.46
        _RippleLifetime ("波紋の持続時間 (秒)", Range(0.5, 10)) = 4.2
        _RippleAmplitude ("波紋の強さ", Range(0, 3)) = 0.45
        _RippleChannel ("波紋チャンネル", Float) = 0

        [HideInInspector] _ManualTime ("Manual Preview Time", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 300

        Pass
        {
            Name "ForwardLitWater"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma shader_feature_local _USE_FLOWMAP
            #pragma shader_feature_local _SHORE_EFFECTS
            #pragma shader_feature_local _USE_RIPPLES
            #pragma shader_feature_local _SCREEN_REFRACTION

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            // 深度は岸辺・屈折・水底コースティクスのいずれでも使う
            #if defined(_SHORE_EFFECTS) || defined(_SCREEN_REFRACTION)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #define SILIQ_HAS_SCENE_DEPTH 1
            #endif
            #if defined(_SCREEN_REFRACTION)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #endif

            TEXTURE2D(_NormalMap);      SAMPLER(sampler_NormalMap);
            TEXTURE2D(_FlowMap);        SAMPLER(sampler_FlowMap);
            TEXTURE2D(_FoamMap);        SAMPLER(sampler_FoamMap);
            TEXTURE2D(_CausticsMap);    SAMPLER(sampler_CausticsMap);

            // SRP Batcher 互換のため、_TexelSize を含むマテリアル定数はすべて
            // UnityPerMaterial の中に置く。
            CBUFFER_START(UnityPerMaterial)
                float4 _NormalMap_TexelSize;
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _ScatterColor;
                half _Opacity;
                half _RefractionStrength;
                float _UseRefraction;
                half _AbsorptionDepth;
                half _EdgeSoftness;
                half _NormalStrength;
                float _Tiling1;
                float _Tiling2;
                float4 _Scroll1;
                float4 _Scroll2;
                half _DetailStrength;
                float _DetailTiling;
                float _DetailDistance;
                half _SpecularAA;
                half _SwellNormalStrength;
                half _SwellHeight;
                float _SwellScale;
                half _SwellSpeed;
                half _MacroVariation;
                half _MacroScale;
                half _MacroDirectionBreakup;
                half _MacroColorVariation;
                half _CausticsStrength;
                half _CausticsScale;
                half _CausticsSpeed;
                half _CausticsFocus;
                half _CausticsPrismStrength;
                half _CausticsScatterStrength;
                half _CausticsDepthFade;
                half _BottomVisibility;
                half _BottomLightStrength;
                half4 _CausticsTint;
                half _Smoothness;
                half _FresnelPower;
                half _ReflStrength;
                half _SunSheen;
                half _SssStrength;
                float _UseFlowMap;
                half _FlowSpeed;
                half _FlowIntensity;
                float _UseShore;
                half _ShoreDepth;
                half _ShoreFoamWidth;
                float _FoamTiling;
                half4 _FoamColor;
                float _RippleSpeed;
                float _RippleWidth;
                float _RippleLifetime;
                float _RippleAmplitude;
                float _RippleChannel;
                float _UseRipples;
                float _ManualTime;
            CBUFFER_END

            float SiliqAnimationTime()
            {
                return _Time.y + _ManualTime;
            }

            float SiliqMacroNoise(float2 p)
            {
                float time = SiliqAnimationTime();
                float a = sin(dot(p, float2(1.27, 2.31)) + time * 0.07);
                float b = sin(dot(p, float2(-2.14, 1.43)) - time * 0.05);
                float c = sin(dot(p, float2(0.63, -1.19)) + time * 0.03);
                return a * 0.5 + b * 0.32 + c * 0.18;
            }

            float2 SiliqRotate2D(float2 v, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                return float2(v.x * c - v.y * s, v.x * s + v.y * c);
            }

            // 大きなうねり。高さと解析勾配を同時に返す (xyz = 高さ, dH/dx, dH/dz)。
            // テクスチャのタイリングとは独立なので、遠景まで繰り返しが見えない波形になる。
            float3 SiliqSwell(float2 worldXZ)
            {
                float t = SiliqAnimationTime() * _SwellSpeed;
                float2 p = worldXZ * max(_SwellScale, 0.001);

                float2 k1 = float2(1.00, 0.31);
                float2 k2 = float2(-0.47, 0.88);
                float2 k3 = float2(0.66, -0.72);
                float a1 = 0.52, a2 = 0.31, a3 = 0.17;

                float p1 = dot(p, k1) + t * 1.00;
                float p2 = dot(p, k2) - t * 0.73;
                float p3 = dot(p, k3) + t * 1.31;

                float h = sin(p1) * a1 + sin(p2) * a2 + sin(p3) * a3;
                float2 grad = cos(p1) * a1 * k1 + cos(p2) * a2 * k2 + cos(p3) * a3 * k3;
                grad *= max(_SwellScale, 0.001);
                return float3(h, grad);
            }

            // 1 ピクセルが跨ぐテクセル数から「サンプリング不足」の度合いを求める。
            // 遠景でノーマルとハイライトを丸めるための指標 (Toksvig 近似)。
            half SiliqUnderSampling(float2 uv)
            {
                float2 footprint = fwidth(uv) * _NormalMap_TexelSize.zw;
                float span = max(footprint.x, footprint.y);
                // 2 テクセル/ピクセルまでは無視し、そこから緩やかに立ち上げる。
                // 近〜中距離の見た目に触らず、本当にサンプリングが破綻する距離だけを対象にする。
                return saturate((log2(max(span, 1.0)) - 1.0) * 0.30);
            }

            #ifdef _USE_RIPPLES
            // アバターが触れた/水に入った場所からスクリプト (WaterRippleSource / Udon版) が
            // 都度書き込むグローバル配列。xy=ワールドXZ座標, z=発生時刻, w=波紋チャンネル。
            // 複数の水面マテリアルで共有されるため UnityPerMaterial の外で定義する (SRP Batcher 互換)。
            #define SILIQ_MAX_RIPPLES 16
            float4 _SiliqRipplePoints[SILIQ_MAX_RIPPLES];

            float3 SiliqComputeRipple(float2 worldXZ)
            {
                float2 total = 0;
                float light = 0;
                UNITY_UNROLL
                for (int i = 0; i < SILIQ_MAX_RIPPLES; i++)
                {
                    float2 center = _SiliqRipplePoints[i].xy;
                    float startTime = _SiliqRipplePoints[i].z;
                    float channel = _SiliqRipplePoints[i].w;
                    float age = _Time.y - startTime;
                    if (abs(channel - _RippleChannel) > 0.5) continue;
                    if (startTime <= 0 || age <= 0 || age >= _RippleLifetime) continue;

                    float d = distance(worldXZ, center);
                    float radius = age * _RippleSpeed;
                    float width = max(_RippleWidth, 1e-3);
                    float ringPhase = (d - radius) / width;
                    float envelope = exp(-ringPhase * ringPhase);
                    float fadeIn = min(0.35, _RippleLifetime * 0.18);
                    float fadeOut = min(0.75, _RippleLifetime * 0.30);
                    float fade = smoothstep(0.0, fadeIn, age) *
                                 (1.0 - smoothstep(max(0.0, _RippleLifetime - fadeOut), _RippleLifetime, age));
                    float2 dir = d > 1e-4 ? (worldXZ - center) / d : float2(0, 0);
                    float phase = ringPhase * TWO_PI;
                    float wave = sin(phase);
                    float crest = (0.5 + 0.5 * cos(phase)) * envelope * fade;
                    total += dir * (wave * envelope * fade * _RippleAmplitude);
                    light += crest * _RippleAmplitude;
                }
                return half3(total, saturate(light));
            }
            #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3; // w = sign
                float4 positionNDC : TEXCOORD4;
                half fogFactor : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                if (_SwellHeight > 0.0001h)
                {
                    // 大きなうねりで実際に頂点を持ち上げる (メッシュの分割数に依存)
                    positionWS.y += SiliqSwell(positionWS.xz).x * _SwellHeight;
                }

                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                // URP 内部と同じ手順でスクリーン座標を作る (バージョン間のヘルパー差異を避ける)
                float4 ndc = output.positionCS * 0.5;
                output.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
                output.positionNDC.zw = output.positionCS.zw;
                output.normalWS = normInputs.normalWS;
                output.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            // 3 レイヤーのノーマル合成。距離が遠いほど微細レイヤーを落とし、
            // サンプリング不足の量だけ法線を平坦化してチラつきを断つ。
            half3 SampleWaterNormal(float2 uv, float3 positionWS, float viewDist,
                                    out half macroMask, out half aliasing)
            {
                float time = SiliqAnimationTime();
                float macroScale = max(_MacroScale, 0.0001h);
                half macroA = SiliqMacroNoise(positionWS.xz * macroScale);
                half macroB = SiliqMacroNoise(positionWS.xz * macroScale * 1.71 + float2(13.1, 7.7));
                macroMask = saturate(0.5h + macroA * 0.5h);
                float2 centeredUv = uv - 0.5;
                float2 macroWarp = float2(macroA, macroB) * (_MacroVariation * 0.075h);
                half layer2Angle = 0.61h + macroB * _MacroDirectionBreakup * 0.9h;
                float tiling1 = _Tiling1 * (1.0 + macroA * _MacroVariation * 0.18);
                float tiling2 = _Tiling2 * (1.0 + macroB * _MacroVariation * 0.22);
                float2 uv1 = centeredUv * tiling1 + 0.5 + macroWarp;
                float2 uv2 = SiliqRotate2D(centeredUv, layer2Angle) * tiling2 + 0.5 - macroWarp * 1.35;

                aliasing = SiliqUnderSampling(uv1 + _Scroll1.xy * time) * _SpecularAA;
                half sharpness = 1.0h - aliasing;

                #if defined(_USE_FLOWMAP)
                // フローマップ駆動: 2 位相のサンプルをクロスフェードして連続的な流れを作る
                half2 flow = (SAMPLE_TEXTURE2D(_FlowMap, sampler_FlowMap, uv + macroWarp).rg * 2.0h - 1.0h) * _FlowIntensity;
                half phase0 = frac(time * _FlowSpeed);
                half phase1 = frac(time * _FlowSpeed + 0.5h);
                half blend = abs((0.5h - phase0) / 0.5h);

                half3 nA0 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv1 - flow * phase0));
                half3 nA1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv1 - flow * phase1 + 0.37h));
                half3 n1 = lerp(nA0, nA1, blend);

                half3 nB0 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv2 - flow * phase0 * 1.3h));
                half3 nB1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv2 - flow * phase1 * 1.3h + 0.71h));
                half3 n2 = lerp(nB0, nB1, blend);
                #else
                // 標準: 2 レイヤーの UV スクロール (互いに回転させて格子状の相関を消す)
                half3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv1 + _Scroll1.xy * time));
                half3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv2 + _Scroll2.xy * time));
                #endif

                // 近距離だけに乗る微細波。分岐条件はマテリアル定数のみにして、
                // テクスチャ暗黙微分が未定義になる varying flow control を避ける。
                half3 n3 = half3(0, 0, 1);
                half detailWeight = 0;
                UNITY_BRANCH
                if (_DetailStrength > 0.002h)
                {
                    half detailFade = saturate(1.0h - viewDist / max(_DetailDistance, 1.0));
                    detailWeight = _DetailStrength * detailFade * detailFade * sharpness;
                    float2 uv3 = SiliqRotate2D(centeredUv, 2.31h - macroA * _MacroDirectionBreakup * 0.6h) *
                                 (tiling1 * max(_DetailTiling, 2.0)) + 0.5 +
                                 (_Scroll2.xy * 1.9 - _Scroll1.xy * 1.3) * time;
                    n3 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv3));
                }

                // 3 枚目を足した分だけ正規化し、_NormalStrength の意味を 2 レイヤー時代から変えない
                half layerNorm = 2.0h / (2.0h + detailWeight);
                half3 blended = half3((n1.xy + n2.xy + n3.xy * detailWeight) * layerNorm, n1.z * n2.z);
                half macroStrength = lerp(1.0h - _MacroVariation * 0.45h, 1.0h + _MacroVariation * 0.55h, macroMask);
                // 遠景の対策は roughness 側で行う。法線を潰すと近〜中距離まで平板になる。
                blended.xy *= _NormalStrength * macroStrength * lerp(1.0h, 0.78h, aliasing);
                return normalize(blended);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 cameraPos = GetCameraPositionWS();
                float viewDist = distance(cameraPos, input.positionWS);

                half macroMask = 0.5h;
                half aliasing = 0.0h;
                half3 normalTS = SampleWaterNormal(input.uv, input.positionWS, viewDist, macroMask, aliasing);
                half sharpness = 1.0h - aliasing;

                half rippleLight = 0;
                #if defined(_USE_RIPPLES)
                half3 ripple = SiliqComputeRipple(input.positionWS.xz);
                normalTS.xy += ripple.xy * sharpness;
                rippleLight = ripple.z * sharpness;
                normalTS = normalize(normalTS);
                #endif

                half3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                half3x3 tbn = half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);
                half3 normalWS = normalize(mul(normalTS, tbn));

                // 大きなうねりの傾き。テクスチャに依存しないので遠景でも繰り返して見えない。
                // 水平な水面を前提にした world 空間の傾きなので、垂直に近い面では効かせない。
                float3 swell = SiliqSwell(input.positionWS.xz);
                half swellFit = saturate(abs(input.normalWS.y));
                normalWS = normalize(normalWS + half3(-swell.y, 0, -swell.z) * (_SwellNormalStrength * sharpness * swellFit));

                half3 viewDir = normalize(cameraPos - input.positionWS);
                half NdotV = saturate(dot(normalWS, viewDir));
                half grazing = 1.0h - NdotV;
                float2 screenUV = input.positionNDC.xy / max(input.positionNDC.w, 1e-5);
                half refraction = saturate(_RefractionStrength);

                // --- 水中の厚み ---
                half shoreFade = 1.0h;    // 1 = 深い
                half foamLine = 0.0h;
                // 深度テクスチャがない時は視線角から厚みを近似する。
                // 真上から覗けば光路は短く (浅い色)、斜めから見れば長い (深い色)。
                float waterDepth = _AbsorptionDepth * lerp(2.2h, 0.55h, NdotV);
                float3 floorWS = input.positionWS;
                #if defined(SILIQ_HAS_SCENE_DEPTH)
                float sceneEyeDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float fragEyeDepth = input.positionNDC.w;
                waterDepth = max(0.0, sceneEyeDepth - fragEyeDepth);
                // 視線に沿った実距離へ直し、水底のワールド座標を復元する。
                // view 行列の 3 行目が -forward なので、符号を反転してカメラ前方を得る。
                float3 viewRayWS = -viewDir;
                float3 viewForwardWS = -UNITY_MATRIX_V[2].xyz;
                float forwardCos = max(dot(viewRayWS, viewForwardWS), 1e-4);
                floorWS = input.positionWS + viewRayWS * (waterDepth / forwardCos);
                #endif
                #if defined(_SHORE_EFFECTS)
                shoreFade = saturate(waterDepth / _ShoreDepth);
                foamLine = saturate(1.0 - waterDepth / _ShoreFoamWidth);
                #endif

                // --- ライティング ---
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half shadow = mainLight.shadowAttenuation;
                half3 lightColor = mainLight.color * shadow;
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 ambient = SampleSH(normalWS);

                // --- 反射 ---
                half perceptualRoughness = saturate(1.0h - _Smoothness);
                // サンプリング不足の分だけ荒くする = スペキュラアンチエイリアス
                perceptualRoughness = saturate(perceptualRoughness + aliasing * 0.35h);
                half3 reflectVector = reflect(-viewDir, normalWS);
                half3 reflection = GlossyEnvironmentReflection(reflectVector, perceptualRoughness, 1.0h) * _ReflStrength;
                reflection *= lerp(0.82h, 1.18h, macroMask);

                // 水の垂直入射反射率は約 2%。_FresnelPower で形だけ調整できるようにする。
                half fresnel = saturate(0.02h + 0.98h * pow(max(grazing, 1e-4h), _FresnelPower));
                fresnel *= lerp(0.35h, 1.0h, _ReflStrength);

                // --- 水中の色 (Beer-Lambert 吸収) ---
                // _DeepColor は「_AbsorptionDepth だけ進んだ時に残る透過率」として扱う
                half3 extinction = -log(max(_DeepColor.rgb, 0.002h)) / max(_AbsorptionDepth, 0.05h);
                half3 transmittance = exp(-extinction * waterDepth);
                half3 scatter = _ScatterColor.rgb * (ambient + lightColor * (0.35h + 0.65h * NdotL));

                half3 underwater;
                #if defined(_SCREEN_REFRACTION)
                // 法線でスクリーン UV をずらして背景を歪ませる。水際では歪みを弱め、
                // 手前にある物体を吸い込んだ場合は歪みなしのサンプルへ戻す。
                half depthMask = saturate(waterDepth / max(_EdgeSoftness, 0.01h));
                float2 refractUV = screenUV + normalWS.xz * refraction * 0.12h * depthMask;
                float refractedSceneDepth = LinearEyeDepth(SampleSceneDepth(refractUV), _ZBufferParams);
                // 歪めた先が水面より手前なら、それは水の上の物体なのでずらさない
                refractUV = refractedSceneDepth < input.positionNDC.w ? screenUV : refractUV;
                underwater = SampleSceneColor(refractUV);
                #else
                // 屈折テクスチャなしの近似: 浅い色を法線で軽く揺らす
                half2 refractOffset = normalWS.xz * refraction;
                half waterLens = saturate(0.5h + SiliqMacroNoise(input.positionWS.xz * 0.19h + refractOffset * 3.1h) * 0.5h);
                underwater = _ShallowColor.rgb * lerp(0.92h, 1.12h, waterLens * refraction) * (ambient + lightColor * NdotL);
                #endif

                half2 refractOffsetWS = normalWS.xz * refraction;

                // --- 水底のコースティクス ---
                half3 causticsColor = _CausticsTint.rgb;
                half causticsLine = 0;
                half causticsScatter = 0;
                half bottomVisibility = min(_BottomVisibility, 2.0h) * saturate(0.34h + shoreFade * 0.28h + NdotV * 0.38h);
                half bottomLight = saturate(_BottomLightStrength * 0.5h) * saturate(0.68h + bottomVisibility * 0.32h);
                UNITY_BRANCH
                if (_CausticsStrength > 0.0001h)
                {
                    float time = SiliqAnimationTime();
                    float causticsScale = max((float)_CausticsScale, 0.001);
                    // 水面ではなく復元した水底の座標へ投影するので、視点が動いても模様が張り付く
                    float2 floorXZ = floorWS.xz;
                    float2 causticsUvA = floorXZ * causticsScale * 0.12 + normalWS.xz * (0.10 + refraction * 0.24) + time * _CausticsSpeed * float2(0.33, 0.21);
                    float2 causticsUvB = SiliqRotate2D(floorXZ + refractOffsetWS * 0.42, 1.17) * causticsScale * 0.09 - normalWS.xz * (0.08 + refraction * 0.18) - time * _CausticsSpeed * float2(0.19, 0.29);
                    half causticsA = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, causticsUvA).r;
                    half causticsB = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, causticsUvB).r;
                    half causticsPrismR = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, causticsUvA + normalWS.xz * 0.013 + float2(0.004, -0.002)).r;
                    half causticsPrismB = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, causticsUvB - normalWS.xz * 0.011 + float2(-0.003, 0.005)).r;
                    half causticsRaw = saturate(causticsA * 0.62h + causticsB * 0.50h);
                    half causticsFocus = pow(causticsRaw, max(_CausticsFocus, 0.5h));
                    // 深いところほど光が届かない
                    half depthReach = exp(-waterDepth / max(_CausticsDepthFade, 0.1h));
                    causticsLine = saturate((causticsFocus - 0.10h) * _CausticsStrength * lerp(0.82h, 1.55h, bottomLight)) * depthReach;
                    causticsScatter = smoothstep(0.10h, 0.82h, causticsRaw) * _CausticsStrength * _CausticsScatterStrength;
                    causticsScatter *= lerp(0.45h, 1.35h, bottomLight) * depthReach;
                    causticsScatter *= saturate(0.35h + shoreFade * 0.4h + NdotV * 0.35h + bottomVisibility * 0.26h);
                    half3 prismColor = half3(causticsPrismR, causticsRaw, causticsPrismB) * _CausticsTint.rgb;
                    causticsColor = lerp(causticsColor, prismColor, saturate(_CausticsPrismStrength) * saturate(0.35h + bottomLight * 0.35h));
                    // コースティクスは水底で発生するので、吸収を受ける前の水中カラーに足す
                    underwater += causticsColor * causticsLine * (ambient + lightColor) * saturate(0.35h + shoreFade * 0.65h);
                }

                // 水を通った分だけ減衰させ、散乱で戻ってくる光を足す
                half3 waterBody = underwater * transmittance + scatter * (1.0h - transmittance);
                waterBody += lerp(_ShallowColor.rgb, causticsColor, 0.48h) * causticsScatter;
                waterBody += _ShallowColor.rgb * bottomVisibility * saturate(_BottomLightStrength * 0.10h) * (1.0h - transmittance);
                // 大きなムラで水色をわずかに濃淡させ、一様な板に見えないようにする
                waterBody *= lerp(1.0h - _MacroColorVariation * 0.3h, 1.0h + _MacroColorVariation * 0.3h, macroMask);

                // --- 波頭の透過光 (SSS) ---
                // 太陽を背にした波の腹が明るく透ける。傾きが大きいほど厚みがあるとみなす。
                half slope = saturate(length(normalTS.xy) * 2.2h);
                half3 sssDir = normalize(mainLight.direction + normalWS * 0.65h);
                half sss = pow(saturate(dot(viewDir, -sssDir)), 4.0h) * _SssStrength * slope * sharpness;
                waterBody += _ScatterColor.rgb * lightColor * sss;

                // --- スペキュラ (GGX) ---
                half roughness = max(perceptualRoughness * perceptualRoughness, 0.008h);
                half a2 = roughness * roughness;
                half3 halfDir = SafeNormalize(mainLight.direction + viewDir);
                half NdotH = saturate(dot(normalWS, halfDir));
                half dTerm = (NdotH * NdotH * (a2 - 1.0h) + 1.0h);
                half ggx = a2 / max(PI * dTerm * dTerm, 1e-5h);
                half3 spec = min(ggx, 200.0h) * NdotL * lightColor * lerp(0.72h, 1.28h, macroMask);
                // 広いローブを少量足すと、点ではなく水面に伸びる光の道に見える
                half sheen = pow(NdotH, lerp(24.0h, 220.0h, _Smoothness)) * _SunSheen * 0.5h;
                spec += sheen * NdotL * lightColor * sharpness;

                // --- 追加ライト (室内プールの照明など) ---
                #if defined(_ADDITIONAL_LIGHTS)
                int additionalLightCount = GetAdditionalLightsCount();
                for (int li = 0; li < additionalLightCount; ++li)
                {
                    Light addLight = GetAdditionalLight(li, input.positionWS);
                    half3 addAtten = addLight.color * (addLight.distanceAttenuation * addLight.shadowAttenuation);
                    half addNdotL = saturate(dot(normalWS, addLight.direction));
                    half3 addHalf = SafeNormalize(addLight.direction + viewDir);
                    half addNdotH = saturate(dot(normalWS, addHalf));
                    half addD = (addNdotH * addNdotH * (a2 - 1.0h) + 1.0h);
                    half addGgx = a2 / max(PI * addD * addD, 1e-5h);
                    spec += min(addGgx, 200.0h) * addNdotL * addAtten;
                    waterBody += _ScatterColor.rgb * addAtten * addNdotL * 0.15h;
                }
                #endif

                // 反射率と同じフレネルでハイライトを重み付け。真上から見た時に光源が焼き付かない。
                spec *= saturate(0.18h + fresnel);

                // --- 合成 ---
                half3 color = lerp(waterBody, reflection, fresnel) + spec;
                color += lerp(_ShallowColor.rgb, half3(1.0h, 1.0h, 1.0h), 0.72h) * rippleLight * 0.35h * (ambient + lightColor);

                half alpha = _Opacity;
                #if defined(_SCREEN_REFRACTION)
                // 背景は自前で合成済みなので、二重ブレンドを避けて不透明に描く
                alpha = 1.0h;
                #endif
                #if defined(SILIQ_HAS_SCENE_DEPTH)
                // 水際を柔らかく消してポリゴンの切り口を隠す
                alpha *= saturate(waterDepth / max(_EdgeSoftness, 0.01h));
                #endif

                #if defined(_SHORE_EFFECTS)
                // 岸辺フォーム: マスクを 2 方向へ流し、水面法線で歪ませて薄い泡の縁を作る
                float time = SiliqAnimationTime();
                float2 foamUv = input.uv * _FoamTiling + normalWS.xz * 0.05h;
                half foamA = SAMPLE_TEXTURE2D(_FoamMap, sampler_FoamMap, foamUv + _Scroll1.xy * time).r;
                half foamB = SAMPLE_TEXTURE2D(_FoamMap, sampler_FoamMap, foamUv * 1.7h - _Scroll2.xy * time * 1.4h).r;
                half foamMask = saturate(foamA * 0.65h + foamB * 0.55h);
                // しきい値で溶かすと、のっぺりした帯ではなく泡の縁が生まれる
                half foam = saturate(smoothstep(0.55h - foamLine * 0.55h, 0.95h - foamLine * 0.55h, foamMask) * foamLine * 1.4h);
                foam = max(foam, saturate(foamLine * foamLine * 0.55h));
                color = lerp(color, _FoamColor.rgb * (ambient + lightColor), foam * _FoamColor.a);
                alpha = saturate(max(alpha, foam * _FoamColor.a));
                #endif

                color = MixFog(color, input.fogFactor);
                return half4(color, saturate(alpha));
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
