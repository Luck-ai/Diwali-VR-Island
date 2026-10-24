Shader "DiwaliFestival/Festival Twilight Sky"
{
    Properties
    {
        _ZenithColor ("Indigo zenith", Color) = (0.045, 0.065, 0.16, 1)
        _HorizonColor ("Dusk horizon", Color) = (0.29, 0.22, 0.30, 1)
        _LowerColor ("Below horizon", Color) = (0.12, 0.14, 0.21, 1)
        _StarColor ("Soft starlight", Color) = (0.62, 0.70, 0.88, 1)
        _StarStrength ("Star strength", Range(0, 1)) = 0.55
        _StarDensity ("Star probability per cell", Range(0, 0.01)) = 0.002
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor, _HorizonColor, _LowerColor, _StarColor;
                float _StarStrength, _StarDensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            float3 CellRandom(float2 cell)
            {
                float3 p = frac(float3(cell.xyx) * float3(.1031, .1030, .0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.xxy + p.yxx) * p.zyx);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 direction = normalize(input.direction);
                float height = direction.y;
                float gradient = sqrt(saturate(height * 1.8));
                half3 color = height >= 0
                    ? lerp(_HorizonColor.rgb, _ZenithColor.rgb, gradient)
                    : lerp(_HorizonColor.rgb, _LowerColor.rgb, saturate(-height * 4));

                // Cube-face coordinates keep star size even near the zenith without spherical trig.
                float3 a = abs(direction);
                float2 uv;
                float face;
                if (a.x >= a.y && a.x >= a.z) { uv = direction.zy / a.x; face = direction.x > 0 ? 0 : 1; }
                else if (a.y >= a.z) { uv = direction.xz / a.y; face = direction.y > 0 ? 2 : 3; }
                else { uv = direction.xy / a.z; face = direction.z > 0 ? 4 : 5; }
                float2 grid = (uv + 1) * 60;
                float2 cell = floor(grid);
                float3 random = CellRandom(cell + face * float2(157, 263));
                float2 center = .2 + .6 * random.yz;
                float distanceToStar = length(frac(grid) - center);
                float radius = lerp(.065, .105, random.z);
                float edge = max(fwidth(distanceToStar), .025);
                float star = (1 - smoothstep(radius - edge, radius + edge, distanceToStar))
                    * step(1 - _StarDensity, random.x) * smoothstep(.10, .30, height);
                color += _StarColor.rgb * (star * _StarStrength * lerp(.6, 1, random.y));
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
