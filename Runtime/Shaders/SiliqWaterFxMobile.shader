// Lightweight transparent/additive water FX overlay for VRChat mobile and iOS.
// Uses only UnityCG; it avoids camera-copy passes, depth sampling, URP includes, and compute shader paths.
Shader "Siliq/Water FX Mobile (iOS VRChat)"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("FX Mask", 2D) = "black" {}
        _Tint ("Tint", Color) = (0.78, 1, 1, 1)
        _Intensity ("Intensity", Range(0, 3)) = 1
        _Alpha ("Alpha", Range(0, 1)) = 0.75
        _Tiling ("Tiling", Range(0.1, 12)) = 1
        _MaskPower ("Mask Focus", Range(0.25, 4)) = 1
        _PulseSpeed ("Pulse Speed", Range(0, 6)) = 0
        _PulseAmount ("Pulse Amount", Range(0, 0.8)) = 0
        _RadialSpeed ("Radial Expansion Speed", Range(0, 2)) = 0
        _RadialAmount ("Radial Expansion Amount", Range(0, 2)) = 0
        _Scroll1 ("Layer 1 Scroll", Vector) = (0, 0, 0, 0)
        _Scroll2 ("Layer 2 Scroll", Vector) = (0, 0, 0, 0)

        [HideInInspector] _ManualTime ("Manual Preview Time", Float) = 0
        [HideInInspector] _SrcBlend ("Source Blend", Float) = 5
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 10
        [HideInInspector] _ZWrite ("ZWrite", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        LOD 60

        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Off
            Lighting Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Tint;
            half _Intensity;
            half _Alpha;
            half _Tiling;
            half _MaskPower;
            half _PulseSpeed;
            half _PulseAmount;
            half _RadialSpeed;
            half _RadialAmount;
            float _ManualTime;
            float4 _Scroll1;
            float4 _Scroll2;

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

            half SampleMask(float2 uv)
            {
                fixed3 rgb = tex2D(_MainTex, uv).rgb;
                return max(rgb.r, max(rgb.g, rgb.b));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y + _ManualTime;
                float2 centered = i.uv - 0.5;
                half radialEnabled = step(0.0001h, _RadialSpeed + _RadialAmount);
                half radialPhase = frac(t * _RadialSpeed);
                half radialScale = 1.0h + radialPhase * _RadialAmount * radialEnabled;
                half radialFade = lerp(1.0h, saturate(1.0h - radialPhase), radialEnabled);
                float2 radialCentered = centered / max(radialScale, 0.01h);
                float2 uv1 = radialCentered * _Tiling + 0.5 + _Scroll1.xy * t;
                float2 uv2 = float2(radialCentered.y, -radialCentered.x) * (_Tiling * 1.17) + 0.5 + float2(0.23, 0.37) + _Scroll2.xy * t;

                half mask1 = SampleMask(uv1);
                half mask2 = SampleMask(uv2);
                float secondaryWeight = step(0.0000001, abs(_Scroll2.x) + abs(_Scroll2.y));
                half secondary = mask2 * 0.45h * secondaryWeight;
                half mask = saturate(max(mask1, secondary));
                mask = pow(mask, max(_MaskPower, 0.25h));

                half pulse = 1.0h + sin(t * _PulseSpeed) * _PulseAmount;
                half alpha = saturate(mask * _Alpha * pulse * radialFade) * _Tint.a;
                half3 color = _Tint.rgb * (mask * _Intensity * pulse * radialFade);
                return fixed4(color, alpha);
            }
            ENDCG
        }
    }

    FallBack Off
}
