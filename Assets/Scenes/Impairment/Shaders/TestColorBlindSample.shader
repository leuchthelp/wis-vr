Shader "TestColorBlindSample" {
    Properties {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _R("R", Vector) = (1, 0, -0, 1)  
        _G("G", Vector) = (0, 1, 0, 1)
        _B("B", Vector) = (-0, -0, 1, 1)
        _Severity ("Severity", Range(0.0, 1.0)) = 1.0
    }

    SubShader {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS   : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionHCS  : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _R;
            float4 _G;
            float4 _B;
            float _Severity;
            CBUFFER_END


            Varyings vert(Attributes IN) {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                ZERO_INITIALIZE(Varyings, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                // Set the color
                float4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                float4 cb = float4
                (
                    c.r * _R[0] + c.g * _R[1] + c.b * _R[2],
                    c.r * _G[0] + c.g * _G[1] + c.b * _G[2],
                    c.r * _B[0] + c.g * _B[1] + c.b * _B[2],
                    1
                );

                float4 intermediate = lerp(c, cb, _Severity);
                return intermediate;
            }
            ENDHLSL
        }
    }
}
