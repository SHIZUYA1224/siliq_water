// Quest (VRChat モバイル) ワールド向けの軽量水面シェーダー。
// 1 パス / 不透明 / GrabPass なし。1 枚のノーマルマップをスケール違いで
// 2 回スクロールサンプリングして波の動きを作ります。
// ※ VRChat のモバイルアバターではカスタムシェーダーが使えないため、
//    アバターでは VRChat/Mobile/Standard Lite にノーマルマップを設定してください。
Shader "Siliq/Water Mobile (Quest)"
{
    Properties
    {
        _ShallowColor ("浅い水の色", Color) = (0.16, 0.55, 0.60, 1)
        _DeepColor ("深い水の色", Color) = (0.02, 0.15, 0.25, 1)
        _HorizonColor ("反射 (空) の色", Color) = (0.65, 0.80, 0.90, 1)
        [NoScaleOffset] _NormalMap ("水面ノーマルマップ", 2D) = "bump" {}
        _NormalStrength ("ノーマル強度", Range(0, 2)) = 1
        _Tiling1 ("レイヤー1 タイリング", Float) = 1
        _Tiling2 ("レイヤー2 タイリング", Float) = 2.7
        _Scroll1 ("レイヤー1 スクロール (XY)", Vector) = (0.02, 0.013, 0, 0)
        _Scroll2 ("レイヤー2 スクロール (XY)", Vector) = (-0.017, 0.021, 0, 0)
        _SpecPower ("ハイライトの鋭さ", Range(8, 512)) = 160
        _SpecIntensity ("ハイライトの強さ", Range(0, 2)) = 0.8
        _FresnelPower ("フレネルの鋭さ", Range(0.5, 8)) = 4
        [Toggle(USE_REFLECTION_CUBE)] _UseCube ("キューブマップ反射を使う", Float) = 0
        [NoScaleOffset] _ReflCube ("反射キューブマップ", Cube) = "" {}
        _ReflStrength ("反射の強さ", Range(0, 1)) = 0.6

        [Toggle(_USE_RIPPLES)] _UseRipples ("触れた時の波紋を有効化", Float) = 0
        _RippleSpeed ("波紋の広がる速さ", Range(0.1, 10)) = 2.5
        _RippleWidth ("波紋の幅", Range(0.05, 2)) = 0.35
        _RippleLifetime ("波紋の持続時間 (秒)", Range(0.5, 10)) = 3
        _RippleAmplitude ("波紋の強さ", Range(0, 3)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 150

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local USE_REFLECTION_CUBE
            #pragma shader_feature_local _USE_RIPPLES
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _NormalMap;
            half4 _ShallowColor;
            half4 _DeepColor;
            half4 _HorizonColor;
            half _NormalStrength;
            float _Tiling1;
            float _Tiling2;
            float4 _Scroll1;
            float4 _Scroll2;
            half _SpecPower;
            half _SpecIntensity;
            half _FresnelPower;
            half _ReflStrength;
            #ifdef USE_REFLECTION_CUBE
            samplerCUBE _ReflCube;
            #endif

            #ifdef _USE_RIPPLES
            half _RippleSpeed;
            half _RippleWidth;
            half _RippleLifetime;
            half _RippleAmplitude;

            // アバターが触れた/水に入った場所からスクリプト (WaterRippleSource / Udon版) が
            // 都度書き込むグローバル配列。xy=ワールドXZ座標, z=発生時刻, w=未使用。
            // 複数の水面マテリアルで共有されるためマテリアル固有バッファの外で定義する。
            #define SILIQ_MAX_RIPPLES 8
            float4 _SiliqRipplePoints[SILIQ_MAX_RIPPLES];

            half2 SiliqComputeRippleOffset(float2 worldXZ)
            {
                half2 total = 0;
                UNITY_UNROLL
                for (int i = 0; i < SILIQ_MAX_RIPPLES; i++)
                {
                    float2 center = _SiliqRipplePoints[i].xy;
                    float startTime = _SiliqRipplePoints[i].z;
                    float age = _Time.y - startTime;
                    if (startTime <= 0 || age <= 0 || age >= _RippleLifetime) continue;

                    float d = distance(worldXZ, center);
                    float radius = age * _RippleSpeed;
                    float width = max(_RippleWidth, 1e-3);
                    float ringPhase = (d - radius) / width;
                    float envelope = exp(-ringPhase * ringPhase) * saturate(1.0 - age / _RippleLifetime);
                    half2 dir = d > 1e-4 ? (worldXZ - center) / d : half2(0, 0);
                    float wave = sin((d - radius) * (UNITY_TWO_PI / width));
                    total += dir * (wave * envelope * _RippleAmplitude);
                }
                return total;
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
                float2 uv1 = i.uv * _Tiling1 + _Scroll1.xy * _Time.y;
                float2 uv2 = i.uv * _Tiling2 + _Scroll2.xy * _Time.y;

                half3 n1 = UnpackNormal(tex2D(_NormalMap, uv1));
                half3 n2 = UnpackNormal(tex2D(_NormalMap, uv2));
                half3 tn = normalize(half3(n1.xy + n2.xy, n1.z * n2.z));
                tn.xy *= _NormalStrength;

                #ifdef _USE_RIPPLES
                tn.xy += SiliqComputeRippleOffset(i.worldPos.xz);
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

                half halfLambert = saturate(dot(worldN, lightDir)) * 0.5h + 0.5h;
                half3 baseCol = lerp(_DeepColor.rgb, _ShallowColor.rgb, halfLambert);

                half3 reflCol = _HorizonColor.rgb;
                #ifdef USE_REFLECTION_CUBE
                half3 reflDir = reflect(-viewDir, worldN);
                reflCol = lerp(reflCol, texCUBE(_ReflCube, reflDir).rgb, _ReflStrength);
                #endif

                half fresnel = pow(1.0h - saturate(dot(worldN, viewDir)), _FresnelPower);
                half spec = pow(saturate(dot(worldN, halfDir)), _SpecPower) * _SpecIntensity;

                half4 col;
                col.rgb = lerp(baseCol, reflCol, fresnel) + spec * _LightColor0.rgb;
                col.a = 1.0h;

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }

    Fallback "Mobile/VertexLit"
}
