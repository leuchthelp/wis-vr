Shader "AlignmentColorBlind"
{
    Properties
    {
        _PriorLeftTex("Prior Left", 2D) = "black" {}
        _PriorRightTex("Prior Right", 2D) = "black" {}
        _LeftTex("Left Texture", 2D) = "black" {}
        _RightTex("Right Texture", 2D) = "black" {}
        _R("R", Vector) = (1, 0, -0, 1)  
        _G("G", Vector) = (0, 1, 0, 1)
        _B("B", Vector) = (-0, -0, 1, 1)
        _Severity ("Severity", Range(0.0, 1.0)) = 1.0
        _LeftUvOffset("Left UV Offset", Vector) = (0,0,0,0)
        _RightUvOffset("Right UV Offset", Vector) = (0,0,0,0)
        _PreviewEye("Preview Eye (0 Left, 1 Right)", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "RenderType"="Opaque" }

        Pass
        {
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            TEXTURE2D(_PriorLeftTex);
            SAMPLER(sampler_PriorLeftTex);
            TEXTURE2D(_PriorRightTex);
            SAMPLER(sampler_PriorRightTex);
            TEXTURE2D(_LeftTex);
            SAMPLER(sampler_LeftTex);
            TEXTURE2D(_RightTex);
            SAMPLER(sampler_RightTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _R;
            float4 _G;
            float4 _B;
            float _Severity;
            float _PreviewEye;
            float2 _LeftUvOffset;
            float2 _RightUvOffset;
            CBUFFER_END

            float3 _LeftCameraPos;
            float3 _RightCameraPos;
            float4x4 _LeftCameraRotationMatrix;
            float4x4 _RightCameraRotationMatrix;

            float2 _LeftFocalLength;
            float2 _RightFocalLength;
            float2 _LeftPrincipalPoint;
            float2 _RightPrincipalPoint;
            float2 _LeftSensorResolution;
            float2 _RightSensorResolution;
            float2 _LeftCurrentResolution;
            float2 _RightCurrentResolution;


            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            float4 ProjectToViewport(
                float3 worldPos,
                float3 cameraPos,
                float4x4 inverseCameraRotation,
                float2 focalLength,
                float2 principalPoint,
                float2 sensorResolution,
                float2 currentResolution)
            {
                float3 localPos = mul(inverseCameraRotation, float4(worldPos - cameraPos, 1.0)).xyz;
                if (localPos.z <= 0.0001)
                {
                    return float4(0.0, 0.0, 0.0, 0.0);
                }

                float2 sensorPoint = float2(
                    (localPos.x / localPos.z) * focalLength.x + principalPoint.x,
                    (localPos.y / localPos.z) * focalLength.y + principalPoint.y);

                float2 scaleFactor = currentResolution / sensorResolution;
                scaleFactor /= max(scaleFactor.x, scaleFactor.y);

                float2 cropMin = sensorResolution * (1.0 - scaleFactor) * 0.5;
                float2 cropSize = sensorResolution * scaleFactor;
                float2 uv = (sensorPoint - cropMin) / cropSize;

                return float4(uv, localPos.z, 1.0);
            }

            float4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                int eyeIndex = 0;
                #if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED) || defined(UNITY_SINGLE_PASS_STEREO)
                    eyeIndex = unity_StereoEyeIndex;
                #else
                    eyeIndex = (int)round(saturate(_PreviewEye));
                #endif

                float4 projected = eyeIndex == 0
                    ? ProjectToViewport(
                        IN.worldPos,
                        _LeftCameraPos,
                        _LeftCameraRotationMatrix,
                        _LeftFocalLength,
                        _LeftPrincipalPoint,
                        _LeftSensorResolution,
                        _LeftCurrentResolution)
                    : ProjectToViewport(
                        IN.worldPos,
                        _RightCameraPos,
                        _RightCameraRotationMatrix,
                        _RightFocalLength,
                        _RightPrincipalPoint,
                        _RightSensorResolution,
                        _RightCurrentResolution);

                if (projected.w < 0.5)
                {
                    discard;
                }

                float2 uv = projected.xy;
                float2 uvOffset = eyeIndex == 0 ? _LeftUvOffset : _RightUvOffset;
                uv += uvOffset;

                float4 c;
                float4 intermediate;
                if (any(uv < 0.0) || any(uv > 1.0)) 
                {
                    c = eyeIndex == 0
                        ? SAMPLE_TEXTURE2D(_PriorLeftTex, sampler_PriorLeftTex, IN.worldPos.xy)
                        : SAMPLE_TEXTURE2D(_PriorRightTex, sampler_PriorRightTex, IN.worldPos.xy);
                    
                    intermediate = c;
                } else 
                {
                    c = eyeIndex == 0
                        ? SAMPLE_TEXTURE2D(_LeftTex, sampler_LeftTex, uv)
                        : SAMPLE_TEXTURE2D(_RightTex, sampler_RightTex, uv);
                    
                    float4 cb = float4
                    (
                        c.r * _R[0] + c.g * _R[1] + c.b * _R[2],
                        c.r * _G[0] + c.g * _G[1] + c.b * _G[2],
                        c.r * _B[0] + c.g * _B[1] + c.b * _B[2],
                        1
                    );
                    intermediate = lerp(c, cb, _Severity);
                }
                return intermediate;
            }
            ENDHLSL
        }
    }
}
