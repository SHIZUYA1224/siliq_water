// Universal Render Pipeline (URP) 向け水面シェーダー。SRP Batcher 対応。
// 本ツールで生成したノーマル / フロー / フォームマップをフル活用できます:
// - ノーマルマップ 2 レイヤースクロール、またはフローマップ駆動の歪み
// - リフレクションプローブによる映り込み + フレネル
// - カメラ深度テクスチャによる岸辺の色変化・フォームライン (オプション)
Shader "Siliq/Water URP"
{
    Properties
    {
        _ShallowColor ("浅い水の色", Color) = (0.16, 0.55, 0.60, 1)
        _DeepColor ("深い水の色", Color) = (0.02, 0.15, 0.25, 1)
        _Opacity ("不透明度", Range(0, 1)) = 0.9

        [NoScaleOffset] _NormalMap ("水面ノーマルマップ", 2D) = "bump" {}
        _NormalStrength ("ノーマル強度", Range(0, 2)) = 1
        _Tiling1 ("レイヤー1 タイリング", Float) = 1
        _Tiling2 ("レイヤー2 タイリング", Float) = 2.7
        _Scroll1 ("レイヤー1 スクロール (XY)", Vector) = (0.01, 0.004, 0, 0)
        _Scroll2 ("レイヤー2 スクロール (XY)", Vector) = (-0.003, 0.007, 0, 0)
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
        _BottomLightStrength ("水底光の透け", Range(0, 2)) = 1
        _CausticsTint ("水底の光の色", Color) = (0.75, 1, 1, 1)

        _Smoothness ("スムースネス", Range(0, 1)) = 0.92
        _FresnelPower ("フレネルの鋭さ", Range(0.5, 8)) = 4
        _ReflStrength ("反射の強さ (リフレクションプローブ)", Range(0, 1)) = 0.8

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
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200

        Pass
        {
            Name "ForwardUnlitWater"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _USE_FLOWMAP
            #pragma shader_feature_local _SHORE_EFFECTS
            #pragma shader_feature_local _USE_RIPPLES
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #if defined(_SHORE_EFFECTS)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #endif

            TEXTURE2D(_NormalMap);      SAMPLER(sampler_NormalMap);
            TEXTURE2D(_FlowMap);        SAMPLER(sampler_FlowMap);
            TEXTURE2D(_FoamMap);        SAMPLER(sampler_FoamMap);
            TEXTURE2D(_CausticsMap);    SAMPLER(sampler_CausticsMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half _Opacity;
                half _NormalStrength;
                float _Tiling1;
                float _Tiling2;
                float4 _Scroll1;
                float4 _Scroll2;
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
                half _BottomLightStrength;
                half4 _CausticsTint;
                half _Smoothness;
                half _FresnelPower;
                half _ReflStrength;
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
            CBUFFER_END

            float SiliqMacroNoise(float2 p)
            {
                float time = _Time.y;
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

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.positionNDC = posInputs.positionNDC;
                output.normalWS = normInputs.normalWS;
                output.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            half3 SampleWaterNormal(float2 uv, float3 positionWS, out half macroMask)
            {
                float time = _Time.y;
                float macroScale = max(_MacroScale, 0.0001h);
                half macroA = SiliqMacroNoise(positionWS.xz * macroScale);
                half macroB = SiliqMacroNoise(positionWS.xz * macroScale * 1.71 + float2(13.1, 7.7));
                macroMask = saturate(0.5h + macroA * 0.5h);
                float2 centeredUv = uv - 0.5;
                float2 macroWarp = float2(macroA, macroB) * (_MacroVariation * 0.075h);
                half layer2Angle = macroB * _MacroDirectionBreakup * 0.9h;
                float tiling1 = _Tiling1 * (1.0 + macroA * _MacroVariation * 0.18);
                float tiling2 = _Tiling2 * (1.0 + macroB * _MacroVariation * 0.22);
                float2 uv1 = centeredUv * tiling1 + 0.5 + macroWarp;
                float2 uv2 = SiliqRotate2D(centeredUv, layer2Angle) * tiling2 + 0.5 - macroWarp * 1.35;

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
                // 標準: 2 レイヤーの UV スクロール
                half3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv1 + _Scroll1.xy * time));
                half3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv2 + _Scroll2.xy * time));
                #endif

                half3 blended = normalize(half3(n1.xy + n2.xy, n1.z * n2.z));
                half macroStrength = lerp(1.0h - _MacroVariation * 0.45h, 1.0h + _MacroVariation * 0.55h, macroMask);
                blended.xy *= _NormalStrength * macroStrength;
                return normalize(blended);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half macroMask = 0.5h;
                half3 normalTS = SampleWaterNormal(input.uv, input.positionWS, macroMask);

                half rippleLight = 0;
                #if defined(_USE_RIPPLES)
                half3 ripple = SiliqComputeRipple(input.positionWS.xz);
                normalTS.xy += ripple.xy;
                rippleLight = ripple.z;
                normalTS = normalize(normalTS);
                #endif

                half3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                half3x3 tbn = half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);
                half3 normalWS = normalize(mul(normalTS, tbn));

                half3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));

                // --- 深度ベースの岸辺エフェクト ---
                half shoreFade = 1.0h;   // 1 = 深い
                half foamLine = 0.0h;
                #if defined(_SHORE_EFFECTS)
                float2 screenUV = input.positionNDC.xy / input.positionNDC.w;
                float sceneEyeDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float fragEyeDepth = input.positionNDC.w;
                float depthDiff = max(0.0, sceneEyeDepth - fragEyeDepth);
                shoreFade = saturate(depthDiff / _ShoreDepth);
                foamLine = saturate(1.0 - depthDiff / _ShoreFoamWidth);
                #endif

                // ベースカラー: 岸辺 (または法線の起伏) で浅い色 ⇔ 深い色
                Light mainLight = GetMainLight();
                half ndl = saturate(dot(normalWS, mainLight.direction)) * 0.5h + 0.5h;
                half depthLerp = shoreFade;
                #if !defined(_SHORE_EFFECTS)
                depthLerp = 1.0h - ndl * 0.5h;
                #endif
                depthLerp = saturate(depthLerp + (0.5h - macroMask) * _MacroColorVariation);
                half3 baseCol = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthLerp);

                // 反射: リフレクションプローブ + フレネル
                half3 reflectVector = reflect(-viewDir, normalWS);
                half perceptualRoughness = 1.0h - _Smoothness;
                half3 reflection = GlossyEnvironmentReflection(reflectVector, perceptualRoughness, 1.0h) * _ReflStrength;
                reflection *= lerp(0.78h, 1.22h, macroMask);

                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDir)), _FresnelPower);

                // スペキュラハイライト
                half3 halfDir = normalize(mainLight.direction + viewDir);
                half specPow = exp2(10.0h * _Smoothness + 1.0h);
                half3 spec = pow(saturate(dot(normalWS, halfDir)), specPow) * mainLight.color;
                spec *= lerp(0.72h, 1.28h, macroMask);

                half3 color = lerp(baseCol, reflection, fresnel) + spec;
                float causticsScale = max((float)_CausticsScale, 0.001);
                float2 causticsUvA = input.positionWS.xz * causticsScale * 0.12 + normalWS.xz * 0.10 + _Time.y * _CausticsSpeed * float2(0.33, 0.21);
                float2 causticsUvB = SiliqRotate2D(input.positionWS.xz, 1.17) * causticsScale * 0.09 - normalWS.xz * 0.08 - _Time.y * _CausticsSpeed * float2(0.19, 0.29);
                half causticsA = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, causticsUvA).r;
                half causticsB = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, causticsUvB).r;
                half causticsPrismR = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, causticsUvA + normalWS.xz * 0.013 + float2(0.004, -0.002)).r;
                half causticsPrismB = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, causticsUvB - normalWS.xz * 0.011 + float2(-0.003, 0.005)).r;
                half bottomLight = saturate(_BottomLightStrength * 0.5h);
                half causticsRaw = saturate(causticsA * 0.62h + causticsB * 0.50h);
                half causticsFocus = pow(causticsRaw, max(_CausticsFocus, 0.5h));
                half causticsLine = saturate((causticsFocus - 0.10h) * _CausticsStrength * lerp(0.82h, 1.55h, bottomLight));
                half causticsScatter = smoothstep(0.10h, 0.82h, causticsRaw) * _CausticsStrength * _CausticsScatterStrength;
                causticsScatter *= lerp(0.45h, 1.35h, bottomLight) * saturate(0.35h + shoreFade * 0.4h + (1.0h - fresnel) * 0.35h);
                half3 causticsColor = _CausticsTint.rgb;
                half3 prismColor = half3(causticsPrismR, causticsRaw, causticsPrismB) * _CausticsTint.rgb;
                causticsColor = lerp(causticsColor, prismColor, saturate(_CausticsPrismStrength) * saturate(0.35h + bottomLight * 0.35h));
                color += causticsColor * causticsLine * saturate(0.35h + shoreFade * 0.65h);
                color += lerp(_ShallowColor.rgb, causticsColor, 0.48h) * causticsScatter;
                color += lerp(_ShallowColor.rgb, half3(1.0h, 1.0h, 1.0h), 0.72h) * rippleLight * 0.35h;
                half alpha = _Opacity;

                #if defined(_SHORE_EFFECTS)
                // 岸辺フォーム: フォームマスクを流しつつフォームラインで乗せる
                half foamMask = SAMPLE_TEXTURE2D(_FoamMap, sampler_FoamMap, input.uv * _FoamTiling + _Scroll1.xy * _Time.y).r;
                half foam = saturate(foamLine * (0.6h + foamMask));
                color = lerp(color, _FoamColor.rgb, foam * _FoamColor.a);
                // 水際は透明に近づけてなじませる
                alpha = lerp(saturate(_Opacity * 0.35h + foam), _Opacity, shoreFade);
                #endif

                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
