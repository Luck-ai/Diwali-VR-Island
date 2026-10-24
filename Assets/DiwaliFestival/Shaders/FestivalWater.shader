Shader "DiwaliFestival/Festival Water"
{
    Properties
    {
        _DeepColor ("Deep Color", Color) = (0.018, 0.085, 0.14, 1)
        _ShallowColor ("Shallow Color", Color) = (0.06, 0.25, 0.30, 1)
        _WaveScale ("Wave Scale", Float) = 1.4
        _WaveSpeed ("Wave Speed", Float) = 0.45
        _Smoothness ("Smoothness", Range(0, 1)) = 0.8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Water"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _ShallowColor;
                float _WaveScale;
                float _WaveSpeed;
                half _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half fogFactor : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.fogFactor = ComputeFogFactor(position.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 p = input.positionWS.xz * _WaveScale;
                float t = _Time.y * _WaveSpeed;
                float a = dot(p, float2(0.8, 0.6)) + t;
                float b = dot(p, float2(-0.45, 0.89)) * 1.7 - t * 1.2;
                float c = dot(p, float2(0.96, -0.28)) * 3.1 + t * 0.7;
                float2 slope = float2(0.8, 0.6) * cos(a) * 0.075
                    + float2(-0.45, 0.89) * cos(b) * 0.045
                    + float2(0.96, -0.28) * cos(c) * 0.018;
                half3 normalWS = normalize(float3(-slope.x, 1.0, -slope.y));
                half3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half fresnel = pow(1.0h - saturate(dot(normalWS, viewWS)), 4.0h);
                half variation = (sin(a) + sin(b) * 0.4h) * 0.12h + 0.3h;
                half3 color = lerp(_DeepColor.rgb, _ShallowColor.rgb, saturate(variation + fresnel * 0.65h));
                Light mainLight = GetMainLight();
                half3 halfDirection = SafeNormalize(mainLight.direction + viewWS);
                half highlight = pow(saturate(dot(normalWS, halfDirection)), lerp(32.0h, 180.0h, _Smoothness));
                color += mainLight.color * highlight * 0.45h;
                color += half3(0.11h, 0.15h, 0.22h) * fresnel * 0.35h;
                color = MixFog(color, ComputeFogFactor(TransformWorldToHClip(input.positionWS).z));
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
