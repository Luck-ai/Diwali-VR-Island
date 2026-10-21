Shader "DiwaliIsland/DuskSky"
{
    Properties
    {
        _ZenithColor ("Upper sky", Color) = (0.12, 0.16, 0.29, 1)
        _HorizonColor ("Warm horizon", Color) = (0.67, 0.43, 0.42, 1)
        _LowerColor ("Below horizon", Color) = (0.18, 0.29, 0.34, 1)
        _SunColor ("Setting sun", Color) = (1, 0.74, 0.38, 1)
        _SunDirection ("Sun direction", Vector) = (-0.8, 0.12, -0.4, 0)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 position : SV_POSITION; float3 direction : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            float4 _ZenithColor, _HorizonColor, _LowerColor, _SunColor, _SunDirection;
            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.position = UnityObjectToClipPos(input.vertex);
                output.direction = input.vertex.xyz;
                return output;
            }
            half4 frag(v2f input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 direction = normalize(input.direction);
                float height = direction.y;
                float3 color = height >= 0
                    ? lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(saturate(height), 0.6))
                    : lerp(_HorizonColor.rgb, _LowerColor.rgb, saturate(-height * 3));
                float sun = smoothstep(0.9985, 0.9997, dot(direction, normalize(_SunDirection.xyz)));
                return half4(color + _SunColor.rgb * sun, 1);
            }
            ENDHLSL
        }
    }
}
