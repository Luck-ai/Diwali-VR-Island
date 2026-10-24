Shader "DiwaliFestival/Festival Water"
{
    Properties
    {
        [Normal] _WaveNormal ("Wave Normal", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 1)) = 0.45
        _ReflectionStrength ("Reflection Strength", Range(0, 2)) = 1.0
        _DeepColor ("Deep Color", Color) = (0.018, 0.085, 0.14, 1)
        _ShallowColor ("Shallow Color", Color) = (0.06, 0.25, 0.30, 1)
        _WaveScale ("Wave Scale", Float) = 1.4
        _WaveSpeed ("Wave Speed", Float) = 0.45
        _Smoothness ("Smoothness", Range(0, 1)) = 0.8
        _Opacity ("Opacity", Range(0, 1)) = 0.84
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-100" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Water"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_WaveNormal);
            SAMPLER(sampler_WaveNormal);

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _ShallowColor;
                float _WaveScale;
                float _WaveSpeed;
                half _Smoothness;
                half _Opacity;
                half _NormalStrength;
                half _ReflectionStrength;
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
                float2 uv = input.positionWS.xz * (_WaveScale * 0.055);
                float t = _Time.y * _WaveSpeed;
                half3 waveA = UnpackNormal(SAMPLE_TEXTURE2D(_WaveNormal, sampler_WaveNormal,
                    uv + float2(t * 0.018, t * 0.011)));
                half3 waveB = UnpackNormal(SAMPLE_TEXTURE2D(_WaveNormal, sampler_WaveNormal,
                    float2(-uv.y, uv.x) * 2.31 + float2(-t * 0.013, t * 0.017)));
                half2 slope = (waveA.xy + waveB.xy * 0.55h) * _NormalStrength;
                half3 normalWS = normalize(half3(slope.x, 1.0h, slope.y));
                half3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half ndv = saturate(dot(normalWS, viewWS));
                half fresnel = 0.025h + 0.975h * pow(1.0h - ndv, 5.0h);
                half3 reflectionWS = reflect(-viewWS, normalWS);
                half roughness = max(0.06h, 1.0h - _Smoothness);
                half3 sky = GlossyEnvironmentReflection(reflectionWS, roughness, 1.0h);
                half3 body = lerp(_DeepColor.rgb, _ShallowColor.rgb, 0.12h + 0.15h * (1.0h - ndv));
                half3 color = lerp(body, sky * _ReflectionStrength, fresnel);
                Light mainLight = GetMainLight();
                half3 halfDirection = SafeNormalize(mainLight.direction + viewWS);
                half highlight = pow(saturate(dot(normalWS, halfDirection)), lerp(100.0h, 420.0h, _Smoothness));
                color += mainLight.color * highlight * 0.3h;
                float viewDistance = distance(GetCameraPositionWS(), input.positionWS);
                half waterFog = saturate((viewDistance - 70.0) / 430.0) * 0.18h;
                color = lerp(color, unity_FogColor.rgb, waterFog);
                return half4(color, saturate(_Opacity + fresnel * (1.0h - _Opacity)));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
