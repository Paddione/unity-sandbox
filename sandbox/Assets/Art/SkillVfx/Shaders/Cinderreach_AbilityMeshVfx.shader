Shader "Cinderreach/VFX/AbilityMesh"
{
    Properties
    {
        [HDR] _Color ("Base Color", Color) = (1, 0.7, 0.25, 1)
        [HDR] _EdgeColor ("Dissolve / Fresnel Edge Color", Color) = (1, 0.92, 0.6, 1)
        _Intensity ("HDR Intensity", Range(0, 12)) = 1.8
        _MainTex ("Main Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _ScrollSpeed ("Scroll Speed (Main XY, Noise ZW)", Vector) = (0.3, 0, -0.4, 0.2)
        [Toggle] _UsePolar ("Use Polar UVs", Float) = 0
        _Dissolve ("Dissolve Amount", Range(0, 1)) = 0
        _EdgeWidth ("Dissolve Edge Width", Range(0.01, 0.4)) = 0.12
        _FresnelPower ("Fresnel Power", Range(0.1, 8)) = 2.5
        _FresnelIntensity ("Fresnel Intensity", Range(0, 4)) = 0.5
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
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
            Name "AbilityMeshVfx"
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
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                half4  color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                half4  _Color;
                half4  _EdgeColor;
                float  _Intensity;
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                float4 _ScrollSpeed;
                float  _UsePolar;
                float  _Dissolve;
                float  _EdgeWidth;
                float  _FresnelPower;
                float  _FresnelIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normInputs.normalWS;
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 baseUv = IN.uv;
                if (_UsePolar > 0.5)
                {
                    float2 c = IN.uv - 0.5;
                    float rad = length(c) * 2.0;
                    float ang = atan2(c.y, c.x) * 0.15915494 + 0.5;
                    baseUv = float2(ang, rad);
                }

                float2 uvMain = baseUv * _MainTex_ST.xy + _MainTex_ST.zw + _Time.y * _ScrollSpeed.xy;
                float2 uvNoise = baseUv * _NoiseTex_ST.xy + _NoiseTex_ST.zw + _Time.y * _ScrollSpeed.zw;

                half4 mainSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvMain);
                half noiseSample = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uvNoise).r;

                float val = mainSample.r * lerp(0.45, 1.25, noiseSample);
                float ew = max(_EdgeWidth, 0.005);
                float cut = _Dissolve * 1.30;
                float alphaClip = smoothstep(cut, cut + ew, val);
                float edgeBand = saturate(smoothstep(cut - ew, cut, val) - alphaClip);

                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float ndotv = saturate(abs(dot(normalize(IN.normalWS), viewDir)));
                float fresnel = pow(1.0 - ndotv, max(_FresnelPower, 0.1)) * _FresnelIntensity;

                half3 rgb = IN.color.rgb * _Color.rgb * mainSample.rgb;
                rgb += _EdgeColor.rgb * (edgeBand * 1.4 + fresnel);
                rgb *= _Intensity;

                half alpha = saturate((alphaClip * mainSample.a + edgeBand * 0.45) * IN.color.a * _Color.a);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
