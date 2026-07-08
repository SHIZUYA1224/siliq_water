// Quest (VRChat モバイル) ワールド向けの軽量水面シェーダー。
// 1 パス / GrabPass なし。通常は不透明、透明マテリアル作成時だけ alpha blend へ
// 切り替えます。1 枚のノーマルマップをスケール違いで 2 回スクロールサンプリングして波の動きを作ります。
// ※ VRChat のモバイルアバターではカスタムシェーダーが使えないため、
//    アバターでは VRChat/Mobile/Standard Lite にノーマルマップを設定してください。
Shader "Siliq/Water Mobile (Quest)"
{
    Properties
    {
        _ShallowColor ("浅い水の色", Color) = (0.16, 0.55, 0.60, 1)
        _DeepColor ("深い水の色", Color) = (0.02, 0.15, 0.25, 1)
        _HorizonColor ("反射 (空) の色", Color) = (0.65, 0.80, 0.90, 1)
        _MinLighting ("暗所の最低明るさ", Range(0, 0.5)) = 0.10
        _DarkReflectionDamping ("暗所の反射抑制", Range(0, 1)) = 0.72
        _DarkDetailDamping ("暗所のきらめき抑制", Range(0, 1)) = 0.70
        _Opacity ("正面の不透明度", Range(0, 1)) = 1
        _AlphaFresnel ("斜め視線の不透明度加算", Range(0, 1)) = 0
        _AlphaPower ("透明フレネルの鋭さ", Range(0.5, 8)) = 3
        _Clarity ("透明な抜け感", Range(0, 1)) = 0.45
        _EdgeReflection ("斜め視線の反射強調", Range(0, 1)) = 0
        _TransmissionColor ("透過光の色", Color) = (0.28, 0.75, 0.95, 1)
        _TransmissionStrength ("透過光の強さ", Range(0, 1)) = 0
        _GlimmerColor ("細い光の色", Color) = (0.85, 0.97, 1, 1)
        _GlimmerIntensity ("細い光の揺らぎ", Range(0, 1)) = 0
        _GlimmerSharpness ("細い光の鋭さ", Range(2, 32)) = 12
        _GlintIntensity ("きらめきの強さ", Range(0, 2)) = 0
        _GlintPower ("きらめきの鋭さ", Range(16, 512)) = 180
        [NoScaleOffset] _NormalMap ("水面ノーマルマップ", 2D) = "bump" {}
        [NoScaleOffset] _HeightMap ("水面ハイトマップ", 2D) = "gray" {}
        _DisplacementStrength ("実際の高さ", Range(0, 0.5)) = 0
        _DisplacementScale ("高さの波長", Range(0.05, 4)) = 0.75
        _DisplacementSpeed ("高さの速度", Range(0, 1)) = 0.001
        _HeightMapInfluence ("ハイトマップの影響", Range(0, 1)) = 0
        _NormalStrength ("ノーマル強度", Range(0, 2)) = 1
        _Tiling1 ("レイヤー1 タイリング", Float) = 1
        _Tiling2 ("レイヤー2 タイリング", Float) = 2.7
        _Scroll1 ("レイヤー1 スクロール (XY)", Vector) = (0.00055, 0.00022, 0, 0)
        _Scroll2 ("レイヤー2 スクロール (XY)", Vector) = (-0.00017, 0.00038, 0, 0)
        _MacroVariation ("大きなムラ", Range(0, 1)) = 0.35
        _MacroScale ("大きなムラのスケール", Range(0.01, 1)) = 0.12
        _MacroDirectionBreakup ("方向の崩し", Range(0, 1)) = 0.28
        _MacroColorVariation ("色と光のムラ", Range(0, 1)) = 0.16
        [NoScaleOffset] _CausticsMap ("水底の光マップ", 2D) = "black" {}
        _CausticsStrength ("水底の光の強さ", Range(0, 1)) = 0
        _CausticsScale ("水底の光の細かさ", Range(0.2, 8)) = 1.8
        _CausticsSpeed ("水底の光の速度", Range(0, 0.25)) = 0.00025
        _BottomLightStrength ("水底光の透け", Range(0, 2)) = 1
        _CausticsTint ("水底の光の色", Color) = (0.75, 1, 1, 1)
        _SpecPower ("ハイライトの鋭さ", Range(8, 512)) = 160
        _SpecIntensity ("ハイライトの強さ", Range(0, 2)) = 0.8
        _FresnelPower ("フレネルの鋭さ", Range(0.5, 8)) = 4
        [Toggle(USE_REFLECTION_CUBE)] _UseCube ("キューブマップ反射を使う", Float) = 0
        [NoScaleOffset] _ReflCube ("反射キューブマップ", Cube) = "" {}
        _ReflStrength ("反射の強さ", Range(0, 1)) = 0.6
        _ReflectionPatternStrength ("反射パターンの強さ", Range(0, 1)) = 0
        _ReflectionPatternScale ("反射パターンの大きさ", Range(0.1, 8)) = 1.2

        [Toggle(_USE_RIPPLES)] _UseRipples ("触れた時の波紋を有効化", Float) = 0
        _RippleSpeed ("波紋の広がる速さ", Range(0.1, 10)) = 1.85
        _RippleWidth ("波紋の幅", Range(0.05, 2)) = 0.46
        _RippleLifetime ("波紋の持続時間 (秒)", Range(0.5, 10)) = 4.2
        _RippleAmplitude ("波紋の強さ", Range(0, 3)) = 0.45
        _RippleChannel ("波紋チャンネル", Float) = 0

        [HideInInspector] _SrcBlend ("Source Blend", Float) = 1
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 0
        [HideInInspector] _ZWrite ("ZWrite", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 150

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma shader_feature_local USE_REFLECTION_CUBE
            #pragma shader_feature_local _USE_RIPPLES
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _NormalMap;
            sampler2D _HeightMap;
            sampler2D _CausticsMap;
            half4 _ShallowColor;
            half4 _DeepColor;
            half4 _HorizonColor;
            half _MinLighting;
            half _DarkReflectionDamping;
            half _DarkDetailDamping;
            half _Opacity;
            half _AlphaFresnel;
            half _AlphaPower;
            half _Clarity;
            half _EdgeReflection;
            half4 _TransmissionColor;
            half _TransmissionStrength;
            half4 _GlimmerColor;
            half _GlimmerIntensity;
            half _GlimmerSharpness;
            half _GlintIntensity;
            half _GlintPower;
            half _DisplacementStrength;
            half _DisplacementScale;
            half _DisplacementSpeed;
            half _HeightMapInfluence;
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
            half _BottomLightStrength;
            half4 _CausticsTint;
            half _SpecPower;
            half _SpecIntensity;
            half _FresnelPower;
            half _ReflStrength;
            half _ReflectionPatternStrength;
            half _ReflectionPatternScale;
            #ifdef USE_REFLECTION_CUBE
            samplerCUBE _ReflCube;
            #endif

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

            float SiliqVertexHeight(float3 localPos, float2 uv)
            {
                float t = _Time.y * _DisplacementSpeed;
                float scale = max((float)_DisplacementScale, 0.001);
                float2 p = localPos.xz * scale;
                float waveA = sin(dot(p, float2(1.37, 0.41)) + t * 1.70);
                float waveB = sin(dot(p, float2(-0.52, 1.19)) - t * 1.13);
                float waveC = sin(dot(p, float2(0.31, 0.73)) + t * 0.61);
                float procedural = waveA * 0.52 + waveB * 0.33 + waveC * 0.15;
                float heightMap = tex2Dlod(_HeightMap, float4(uv * scale + t * 0.035, 0, 0)).r * 2.0 - 1.0;
                return lerp(procedural, heightMap, _HeightMapInfluence) * _DisplacementStrength;
            }

            #ifdef _USE_RIPPLES
            float _RippleSpeed;
            float _RippleWidth;
            float _RippleLifetime;
            float _RippleAmplitude;
            float _RippleChannel;

            // アバターが触れた/水に入った場所からスクリプト (WaterRippleSource / Udon版) が
            // 都度書き込むグローバル配列。xy=ワールドXZ座標, z=発生時刻, w=波紋チャンネル。
            // 複数の水面マテリアルで共有されるためマテリアル固有バッファの外で定義する。
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
                    float phase = ringPhase * UNITY_TWO_PI;
                    float wave = sin(phase);
                    float crest = (0.5 + 0.5 * cos(phase)) * envelope * fade;
                    total += dir * (wave * envelope * fade * _RippleAmplitude);
                    light += crest * _RippleAmplitude;
                }
                return half3(total, saturate(light));
            }
            #endif

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 tspace0 : TEXCOORD1;
                float3 tspace1 : TEXCOORD2;
                float3 tspace2 : TEXCOORD3;
                float3 worldPos : TEXCOORD4;
                UNITY_FOG_COORDS(5)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                if (_DisplacementStrength > 0.0001h)
                {
                    v.vertex.xyz += v.normal * SiliqVertexHeight(v.vertex.xyz, v.uv);
                }

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                float3 wNormal = UnityObjectToWorldNormal(v.normal);
                float3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);
                float tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                float3 wBitangent = cross(wNormal, wTangent) * tangentSign;

                o.tspace0 = float3(wTangent.x, wBitangent.x, wNormal.x);
                o.tspace1 = float3(wTangent.y, wBitangent.y, wNormal.y);
                o.tspace2 = float3(wTangent.z, wBitangent.z, wNormal.z);

                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float macroScale = max(_MacroScale, 0.0001h);
                half macroA = SiliqMacroNoise(i.worldPos.xz * macroScale);
                half macroB = SiliqMacroNoise(i.worldPos.xz * macroScale * 1.71 + float2(13.1, 7.7));
                half macro01 = saturate(0.5h + macroA * 0.5h);
                float2 centeredUv = i.uv - 0.5;
                float2 macroWarp = float2(macroA, macroB) * (_MacroVariation * 0.075h);
                half layer2Angle = macroB * _MacroDirectionBreakup * 0.9h;
                float tiling1 = _Tiling1 * (1.0 + macroA * _MacroVariation * 0.18);
                float tiling2 = _Tiling2 * (1.0 + macroB * _MacroVariation * 0.22);

                float2 uv1 = centeredUv * tiling1 + 0.5 + macroWarp + _Scroll1.xy * _Time.y;
                float2 uv2 = SiliqRotate2D(centeredUv, layer2Angle) * tiling2 + 0.5 - macroWarp * 1.35 + _Scroll2.xy * _Time.y;

                half3 n1 = UnpackNormal(tex2D(_NormalMap, uv1));
                half3 n2 = UnpackNormal(tex2D(_NormalMap, uv2));
                half3 tn = normalize(half3(n1.xy + n2.xy, n1.z * n2.z));
                half macroStrength = lerp(1.0h - _MacroVariation * 0.45h, 1.0h + _MacroVariation * 0.55h, macro01);
                tn.xy *= _NormalStrength * macroStrength;

                half rippleLight = 0;
                #ifdef _USE_RIPPLES
                half3 ripple = SiliqComputeRipple(i.worldPos.xz);
                tn.xy += ripple.xy;
                rippleLight = ripple.z;
                #endif

                tn = normalize(tn);

                half3 worldN;
                worldN.x = dot(i.tspace0, tn);
                worldN.y = dot(i.tspace1, tn);
                worldN.z = dot(i.tspace2, tn);
                worldN = normalize(worldN);

                half3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                half3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                half3 halfDir = normalize(lightDir + viewDir);

                half3 lumaWeights = half3(0.2126h, 0.7152h, 0.0722h);
                half ndl = saturate(dot(worldN, lightDir));
                half3 ambientSH = max(ShadeSH9(half4(worldN, 1.0h)), half3(0, 0, 0));
                half ambientLum = saturate(dot(ambientSH, lumaWeights));
                half mainLightLum = saturate(dot(_LightColor0.rgb, lumaWeights));
                half directLum = ndl * mainLightLum;
                half surfaceLight = saturate(ambientLum + directLum);
                half baseVisibility = saturate(_MinLighting + surfaceLight * (1.0h - _MinLighting));
                half reflectionVisibility = lerp(1.0h - _DarkReflectionDamping, 1.0h, ambientLum);
                half detailVisibility = lerp(1.0h - _DarkDetailDamping, 1.0h, surfaceLight);
                half colorLift = (macro01 - 0.5h) * _MacroColorVariation;
                half colorMix = saturate(0.12h + directLum * 0.68h + ambientLum * 0.32h + colorLift);
                half viewFacing = saturate(dot(worldN, viewDir));
                half3 baseCol = lerp(_DeepColor.rgb, _ShallowColor.rgb, colorMix) * baseVisibility;
                half clarity = saturate(_Clarity);
                half3 clearWaterCol = lerp(_ShallowColor.rgb, _TransmissionColor.rgb, 0.68h) * baseVisibility;
                baseCol = lerp(baseCol, clearWaterCol, clarity * viewFacing * 0.62h);

                half3 reflCol = _HorizonColor.rgb;
                #ifdef USE_REFLECTION_CUBE
                half3 reflDir = reflect(-viewDir, worldN);
                reflCol = lerp(reflCol, texCUBE(_ReflCube, reflDir).rgb, _ReflStrength);
                #endif
                half reflectionAmount = lerp(0.08h, 1.45h, _ReflStrength);
                reflCol *= reflectionVisibility * reflectionAmount;

                half fresnel = saturate(pow(1.0h - viewFacing, _FresnelPower) * lerp(0.72h, 1.18h, _ReflStrength));
                half alphaFresnel = pow(1.0h - viewFacing, _AlphaPower);
                float2 reflectionP = SiliqRotate2D(i.worldPos.xz + worldN.xz * 0.35, -0.38) * max((float)_ReflectionPatternScale, 0.1);
                half reflectionBand = pow(saturate(sin(reflectionP.x * 3.14159) * 0.5h + 0.5h), 18.0h);
                half reflectionBreakup = saturate(0.55h + 0.45h * sin(reflectionP.y * 2.3h + macroA * 2.2h));
                half reflectionPattern = reflectionBand * reflectionBreakup * _ReflectionPatternStrength;
                reflectionPattern *= reflectionVisibility * detailVisibility * saturate(0.25h + fresnel + _EdgeReflection * 0.35h);
                half spec = pow(saturate(dot(worldN, halfDir)), _SpecPower) * _SpecIntensity;
                spec *= lerp(0.72h, 1.28h, macro01) * detailVisibility * mainLightLum;
                half glint = pow(saturate(dot(reflect(-lightDir, worldN), viewDir)), _GlintPower) * _GlintIntensity;
                glint *= detailVisibility * mainLightLum;

                half interference = saturate(1.0h - abs(n1.x * 0.72h + n1.y * 0.31h - n2.x * 0.46h + n2.y * 0.58h));
                half glimmer = pow(interference, _GlimmerSharpness) * _GlimmerIntensity * saturate(0.35h + surfaceLight);
                glimmer *= lerp(0.65h, 1.35h, macro01) * detailVisibility;
                float causticsScale = max((float)_CausticsScale, 0.001);
                float2 causticsUvA = i.worldPos.xz * causticsScale * 0.12 + worldN.xz * 0.10 + _Time.y * _CausticsSpeed * float2(0.33, 0.21);
                float2 causticsUvB = SiliqRotate2D(i.worldPos.xz, 1.17) * causticsScale * 0.09 - worldN.xz * 0.08 - _Time.y * _CausticsSpeed * float2(0.19, 0.29);
                half causticsA = tex2D(_CausticsMap, causticsUvA).r;
                half causticsB = tex2D(_CausticsMap, causticsUvB).r;
                half bottomLight = saturate(_BottomLightStrength * 0.5h);
                half caustics = saturate((causticsA * 0.68h + causticsB * 0.45h - 0.22h) * _CausticsStrength * lerp(0.82h, 1.55h, bottomLight));
                caustics *= viewFacing * detailVisibility * saturate(0.32h + _TransmissionStrength + bottomLight * 0.28h);
                half3 transmission = _TransmissionColor.rgb * _TransmissionStrength * viewFacing * saturate(0.2h + surfaceLight) * baseVisibility;
                transmission *= lerp(1.0h, 1.38h, clarity);

                half4 col;
                col.rgb = lerp(baseCol + transmission, reflCol, fresnel) + (spec + glint) * _LightColor0.rgb;
                col.rgb += _HorizonColor.rgb * reflectionPattern;
                col.rgb += _CausticsTint.rgb * caustics;
                col.rgb += _GlimmerColor.rgb * (glimmer + rippleLight * 0.35h * detailVisibility);
                col.rgb = lerp(col.rgb, reflCol + (spec + glint) * _LightColor0.rgb, alphaFresnel * _EdgeReflection);
                col.a = saturate(_Opacity - clarity * viewFacing * 0.12h + alphaFresnel * _AlphaFresnel + glimmer * 0.08h + rippleLight * 0.05h);

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }

    Fallback "Mobile/VertexLit"
}
