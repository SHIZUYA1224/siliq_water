// Lightweight procedural pale pool floor for transparent water previews.
// Built-in compatible and URP-package independent.
Shader "Siliq/Pale Pool Floor Mobile"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.78, 0.96, 0.98, 1)
        _DeepTint ("Soft Depth Tint", Color) = (0.48, 0.86, 0.92, 1)
        _LineColor ("Tile Line Color", Color) = (0.62, 0.82, 0.86, 1)
        _TileScale ("Tile Scale", Range(1, 32)) = 7
        _LineWidth ("Tile Line Width", Range(0.002, 0.08)) = 0.018
        _LineSoftness ("Line Softness", Range(0.001, 0.08)) = 0.018
        _DepthVignette ("Depth Vignette", Range(0, 1)) = 0.22
        _CausticsReceive ("Caustics Receive Boost", Range(0, 1)) = 0.18
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 80

        Pass
        {
            ZWrite On
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            fixed4 _BaseColor;
            fixed4 _DeepTint;
            fixed4 _LineColor;
            half _TileScale;
            half _LineWidth;
            half _LineSoftness;
            half _DepthVignette;
            half _CausticsReceive;

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
                float2 centered = i.uv - 0.5;
                half radial = saturate(length(centered) * 1.65h);
                fixed3 color = lerp(_BaseColor.rgb, _DeepTint.rgb, radial * _DepthVignette);

                float2 tile = abs(frac(i.uv * _TileScale) - 0.5);
                half tileLine = 1.0h - smoothstep(_LineWidth, _LineWidth + _LineSoftness, min(tile.x, tile.y));
                color = lerp(color, _LineColor.rgb, tileLine * 0.28h);

                half centerGlow = saturate(1.0h - radial);
                color += _BaseColor.rgb * centerGlow * _CausticsReceive;

                return fixed4(saturate(color), 1.0h);
            }
            ENDCG
        }
    }

    FallBack Off
}
