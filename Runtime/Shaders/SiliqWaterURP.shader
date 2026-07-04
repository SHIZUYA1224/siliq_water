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
        _Scroll1 ("レイヤー1 スクロール (XY)", Vector) = (0.02, 0.013, 0, 0)
        _Scroll2 ("レイヤー2 スクロール (XY)", Vector) = (-0.017, 0.021, 0, 0)

        _Smoothness ("スムースネス", Range(0, 1)) = 0.92
        _FresnelPower ("フレネルの鋭さ", Range(0.5, 8)) = 4
        _ReflStrength ("反射の強さ (リフレクションプローブ)", Range(0, 1)) = 0.8

        [Toggle(_USE_FLOWMAP)] _UseFlowMap ("フローマップを使う", Float) = 0
        [NoScaleOffset] _FlowMap ("フローマップ (RG)", 2D) = "grey" {}
        _FlowSpeed ("フロー速度", Range(0, 2)) = 0.5
        _FlowIntensity ("フロー強度", Range(0, 1)) = 0.3

        [Toggle(_SHORE_EFFECTS)] _UseShore ("岸辺エフェクトを使う (深度テクスチャ必須)", Float) = 0
        _ShoreDepth ("岸辺の色変化の深さ", Range(0.05, 10)) = 1.5
        _ShoreFoamWidth ("岸辺フォームの幅", Range(0.01, 5)) = 0.6
        [NoScaleOffset] _FoamMap ("フォームマスク (R)", 2D) = "white" {}
        _FoamTiling ("フォームのタイリング", Float) = 2
        _FoamColor ("フォームの色", Color) = (1, 1, 1, 1)
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

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half _Opacity;
                half _NormalStrength;
                float _Tiling1;
                float _Tiling2;
                float4 _Scroll1;
                float4 _Scroll2;
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
            CBUFFER_END

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

            half3 SampleWaterNormal(float2 uv)
            {
                float time = _Time.y;

                #if defined(_USE_FLOWMAP)
                // フローマップ駆動: 2 位相のサンプルをクロスフェードして連続的な流れを作る
                half2 flow = (SAMPLE_TEXTURE2D(_FlowMap, sampler_FlowMap, uv).rg * 2.0h - 1.0h) * _FlowIntensity;
                half phase0 = frac(time * _FlowSpeed);
                half phase1 = frac(time * _FlowSpeed + 0.5h);
                half blend = abs((0.5h - phase0) / 0.5h);

                half3 nA0 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv * _Tiling1 - flow * phase0));
                half3 nA1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv * _Tiling1 - flow * phase1 + 0.37h));
                half3 n1 = lerp(nA0, nA1, blend);

                half3 nB0 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv * _Tiling2 - flow * phase0 * 1.3h));
                half3 nB1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv * _Tiling2 - flow * phase1 * 1.3h + 0.71h));
                half3 n2 = lerp(nB0, nB1, blend);
                #else
                // 標準: 2 レイヤーの UV スクロール
                half3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv * _Tiling1 + _Scroll1.xy * time));
                half3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv * _Tiling2 + _Scroll2.xy * time));
                #endif

                half3 blended = normalize(half3(n1.xy + n2.xy, n1.z * n2.z));
                blended.xy *= _NormalStrength;
                return normalize(blended);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 normalTS = SampleWaterNormal(input.uv);

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
                half3 baseCol = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthLerp);

                // 反射: リフレクションプローブ + フレネル
                half3 reflectVector = reflect(-viewDir, normalWS);
                half perceptualRoughness = 1.0h - _Smoothness;
                half3 reflection = GlossyEnvironmentReflection(reflectVector, perceptualRoughness, 1.0h) * _ReflStrength;

                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDir)), _FresnelPower);

                // スペキュラハイライト
                half3 halfDir = normalize(mainLight.direction + viewDir);
                half specPow = exp2(10.0h * _Smoothness + 1.0h);
                half3 spec = pow(saturate(dot(normalWS, halfDir)), specPow) * mainLight.color;

                half3 color = lerp(baseCol, reflection, fresnel) + spec;
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
