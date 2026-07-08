// Lightweight additive caustics overlay for pool floors and shallow seabeds.
// Put this material on a thin plane just above the floor to add visible bottom light.
Shader "Siliq/Caustics Overlay Mobile"
{
    Properties
    {
        [NoScaleOffset] _CausticsMap ("Caustics Map", 2D) = "black" {}
        _Tint ("Light Tint", Color) = (0.72, 1, 0.92, 1)
        _Intensity ("Intensity", Range(0, 2)) = 0.55
        _Tiling ("Tiling", Range(0.1, 12)) = 1.4
        _Focus ("Focus", Range(0.5, 4)) = 1.6
        _SoftScatter ("Soft Scatter", Range(0, 1)) = 0.32
        _PrismStrength ("Prism Tint", Range(0, 1)) = 0.10
        _Scroll1 ("Layer 1 Scroll", Vector) = (0.000030, 0.000012, 0, 0)
        _Scroll2 ("Layer 2 Scroll", Vector) = (-0.000009, 0.000018, 0, 0)
        _FloorFade ("Floor Fade", Range(0, 1)) = 0.82
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 80

        Pass
        {
            Blend One One
            ZWrite Off
            Cull Off
            ColorMask RGB

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _CausticsMap;
            fixed4 _Tint;
            half _Intensity;
            half _Tiling;
            half _Focus;
            half _SoftScatter;
            half _PrismStrength;
            float4 _Scroll1;
            float4 _Scroll2;
            half _FloorFade;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y;
                float2 centered = i.uv - 0.5;
                float2 uv1 = centered * _Tiling + 0.5 + _Scroll1.xy * t;
                float2 uv2 = float2(centered.y, -centered.x) * (_Tiling * 0.72) + 0.5 + float2(0.31, 0.17) + _Scroll2.xy * t;

                half c1 = tex2D(_CausticsMap, uv1).r;
                half c2 = tex2D(_CausticsMap, uv2).r;
                half prismR = tex2D(_CausticsMap, uv1 + float2(0.004, -0.002)).r;
                half prismB = tex2D(_CausticsMap, uv2 + float2(-0.003, 0.005)).r;
                half raw = saturate(c1 * 0.78h + c2 * 0.42h);
                half focused = pow(raw, max(_Focus, 0.5h));
                half scatter = smoothstep(0.10h, 0.82h, raw) * _SoftScatter;
                half light = saturate((focused * 1.18h + scatter - 0.12h) * _Intensity);
                light *= _FloorFade;
                half3 prism = half3(prismR, raw, prismB) * _Tint.rgb;
                half3 tint = lerp(_Tint.rgb, prism, saturate(_PrismStrength));

                return fixed4(tint * light, light);
            }
            ENDCG
        }
    }

    FallBack Off
}
