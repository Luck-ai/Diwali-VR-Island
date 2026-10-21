Shader "DiwaliIsland/FestivalWater"
{
    Properties
    {
        _DeepColor ("Deep water", Color) = (0.035, 0.13, 0.20, 1)
        _RippleColor ("Ripple highlights", Color) = (0.08, 0.26, 0.32, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _RippleColor;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float phase = input.positionWS.z * 1.5 + sin(input.positionWS.x * 0.28) * 1.1 + _Time.y * 0.35;
                float ripple = pow(saturate(sin(phase) * 0.5 + 0.5), 22) * 0.28;
                return half4(lerp(_DeepColor.rgb, _RippleColor.rgb, ripple), 1);
            }
            ENDHLSL
        }
    }
}
