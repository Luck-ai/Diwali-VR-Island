Shader "Diwali/Progressive Orange Path" {
 Properties {
  _BaseMap("Authored floor atlas", 2D) = "white" {}
  _BaseColor("Authored color", Color) = (1,1,1,1)
  [Normal] _BumpMap("Authored normal", 2D) = "bump" {}
  _BumpScale("Normal strength", Float) = 1
  _MetallicGlossMap("Metallic / smoothness", 2D) = "white" {}
  _Metallic("Metallic", Range(0,1)) = 0
  _Smoothness("Smoothness", Range(0,1)) = 0.48
  _EmissionMap("Authored path emission", 2D) = "white" {}
  [HDR] _EmissionColor("Visited emission multiplier", Color) = (1,1,1,1)
 }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
  Pass {
   Name "ForwardLit"
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma target 3.0
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile_fog
   #pragma shader_feature_local _NORMALMAP
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
   TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor, _EmissionColor;
    half _BumpScale, _Metallic, _Smoothness;
   CBUFFER_END
   float4 _Visited[32];
   int _VisitedCount;
   struct A {
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
   };
   struct V {
    float4 positionCS : SV_POSITION;
    float3 world : TEXCOORD0;
    float2 uv : TEXCOORD1;
    half3 normalWS : TEXCOORD2;
    half4 tangentWS : TEXCOORD3;
    half fog : TEXCOORD4;
    float4 shadowCoord : TEXCOORD5;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
   };
   V vert(A a) {
    V o = (V)0;
    UNITY_SETUP_INSTANCE_ID(a);
    UNITY_TRANSFER_INSTANCE_ID(a, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    VertexPositionInputs pos = GetVertexPositionInputs(a.positionOS.xyz);
    VertexNormalInputs normal = GetVertexNormalInputs(a.normalOS, a.tangentOS);
    o.world = pos.positionWS;
    o.positionCS = pos.positionCS;
    o.normalWS = normal.normalWS;
    o.tangentWS = half4(normal.tangentWS, a.tangentOS.w * GetOddNegativeScale());
    o.uv = TRANSFORM_TEX(a.uv, _BaseMap);
    o.fog = ComputeFogFactor(pos.positionCS.z);
    o.shadowCoord = GetShadowCoord(pos);
    return o;
   }
   half4 frag(V v) : SV_Target {
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
    float glow = 0;
    for (int i = 0; i < min(_VisitedCount, 32); i++) {
     float d = distance(v.world.xz, _Visited[i].xz);
     glow = max(glow, 1 - smoothstep(0.45, 0.95, d));
    }
    half3 normalTS = SampleNormal(v.uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    half3 bitangent = v.tangentWS.w * cross(v.normalWS, v.tangentWS.xyz);
    InputData input = (InputData)0;
    input.positionWS = v.world;
    input.normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, half3x3(v.tangentWS.xyz, bitangent, v.normalWS)));
    input.viewDirectionWS = GetWorldSpaceNormalizeViewDir(v.world);
    input.shadowCoord = v.shadowCoord;
    input.bakedGI = SampleSH(input.normalWS);
    input.vertexLighting = VertexLighting(v.world, input.normalWS);
    input.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(v.positionCS);
    input.shadowMask = half4(1,1,1,1);
    SurfaceData surface = (SurfaceData)0;
    surface.albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, v.uv).rgb * _BaseColor.rgb;
    half4 metal = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, v.uv);
    surface.metallic = metal.r * _Metallic;
    surface.smoothness = metal.a * _Smoothness;
    surface.normalTS = normalTS;
    surface.occlusion = 1;
    surface.alpha = 1;
    // Only walked sections emit; their authored orange albedo remains visible at rest.
    surface.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, v.uv).rgb * _EmissionColor.rgb * glow;
    half4 color = UniversalFragmentPBR(input, surface);
    color.rgb = MixFog(color.rgb, v.fog);
    return color;
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
