Shader "Cinderreach/VFX/AbilityGroundZone"
{
    Properties
    {
        [HDR] _Color ("Zone Fill Color", Color) = (0.35, 0.30, 0.15, 1)
        [HDR] _RimColor ("Telegraph Rim Color", Color) = (0.95, 0.69, 0.20, 1)
        _Intensity ("HDR Intensity", Range(0, 8)) = 1.5
        _MainTex ("Radial Mask", 2D) = "white" {}
        _NoiseTex ("Swirl Noise", 2D) = "white" {}
        _ScrollSpeed ("Noise Scroll Speed (XY)", Vector) = (0.18, -0.14, 0, 0)
        _PulseSpeed ("Pulse Speed", Range(0, 12)) = 3.0
        _RimWidth ("Outer Rim Width", Range(0.01, 0.35)) = 0.09
        _FillAlpha ("Fill Opacity", Range(0, 1)) = 0.55
        _Progress ("Zone Reveal / Alpha", Range(0, 1)) = 1.0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
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

        Pass
        {
            Name "AbilityGroundZoneVfx"
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                half4  color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                half4  _Color;
                half4  _RimColor;
                float  _Intensity;
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                float4 _ScrollSpeed;
                float  _PulseSpeed;
                float  _RimWidth;
                float  _FillAlpha;
                float  _Progress;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float dist = length(IN.uv - 0.5) * 2.0;
                float circle = 1.0 - smoothstep(0.94, 1.0, dist);
                float rimStart = max(0.05, 1.0 - _RimWidth);
                float rim = smoothstep(rimStart - 0.04, rimStart, dist) * circle;

                float2 nUv = IN.uv * _NoiseTex_ST.xy + _NoiseTex_ST.zw + _Time.y * _ScrollSpeed.xy;
                half noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, nUv).r;
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).r;

                float pulse = 0.85 + 0.15 * sin(_Time.y * _PulseSpeed);
                float fill = saturate(mask * lerp(0.45, 1.15, noise) * _FillAlpha) * circle;
                float reveal = saturate(_Progress);

                half3 rgb = (_Color.rgb * fill + _RimColor.rgb * rim * 1.35) * pulse * _Intensity * IN.color.rgb;
                half alpha = saturate((fill + rim * 0.9) * reveal * _Color.a * IN.color.a);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
