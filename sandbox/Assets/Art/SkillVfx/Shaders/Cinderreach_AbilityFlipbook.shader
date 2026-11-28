Shader "Cinderreach/VFX/AbilityFlipbook"
{
    Properties
    {
        [HDR] _Color ("Tint Color", Color) = (1, 1, 1, 1)
        _Intensity ("HDR Intensity", Range(0, 12)) = 1.6
        _MainTex ("Flipbook Atlas (RGBA)", 2D) = "white" {}
        _MotionTex ("Motion Vector Map (RG, optional)", 2D) = "grey" {}
        _MotionStrength ("Motion Vector Blend Strength", Range(0, 0.15)) = 0
        _Rows ("Rows", Float) = 4
        _Cols ("Columns", Float) = 4
        _FPS ("Playback FPS", Float) = 16
        _Frame ("Manual Frame Index", Float) = 0
        [Toggle] _UseManualFrame ("Use Manual Frame", Float) = 0
        _Softness ("Alpha Contrast", Range(0.2, 3)) = 1.0
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
            Name "AbilityFlipbookVfx"
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
            TEXTURE2D(_MotionTex);
            SAMPLER(sampler_MotionTex);

            CBUFFER_START(UnityPerMaterial)
                half4  _Color;
                float  _Intensity;
                float4 _MainTex_ST;
                float4 _MotionTex_ST;
                float  _MotionStrength;
                float  _Rows;
                float  _Cols;
                float  _FPS;
                float  _Frame;
                float  _UseManualFrame;
                float  _Softness;
            CBUFFER_END

            float2 FlipbookCellUV(float2 uv, float frameIdx, float cols, float rows)
            {
                float c = fmod(frameIdx, cols);
                float rTop = floor(frameIdx / cols);
                float rBottom = (rows - 1.0) - rTop;
                return (uv + float2(c, rBottom)) / float2(cols, rows);
            }

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
                float cols = max(1.0, floor(_Cols + 0.5));
                float rows = max(1.0, floor(_Rows + 0.5));
                float total = cols * rows;

                float rawFrame = _UseManualFrame > 0.5
                    ? max(0.0, _Frame)
                    : frac(_Time.y * (_FPS / total)) * total;

                float f0 = floor(fmod(rawFrame, total));
                float f1 = fmod(f0 + 1.0, total);
                float blend = frac(rawFrame);

                float2 uvA = FlipbookCellUV(IN.uv, f0, cols, rows);
                float2 uvB = FlipbookCellUV(IN.uv, f1, cols, rows);

                if (_MotionStrength > 0.0001)
                {
                    float2 mvA = (SAMPLE_TEXTURE2D(_MotionTex, sampler_MotionTex, uvA).rg * 2.0 - 1.0) * _MotionStrength;
                    float2 mvB = (SAMPLE_TEXTURE2D(_MotionTex, sampler_MotionTex, uvB).rg * 2.0 - 1.0) * _MotionStrength;
                    uvA -= mvA * blend;
                    uvB += mvB * (1.0 - blend);
                }

                half4 sA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvA);
                half4 sB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvB);
                half4 tex = lerp(sA, sB, blend);

                half3 rgb = tex.rgb * IN.color.rgb * _Color.rgb * _Intensity;
                half alpha = saturate(pow(max(tex.a, 0.0001), _Softness) * IN.color.a * _Color.a);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
