Shader "Diwali/Routing Light Pulse" {
 Properties {
  _BaseColor("Pulse colour",Color)=(2,1.3,0.3,1)
  _Head("Travelling pulse",Float)=0
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+100" "RenderType"="Transparent"}
  Pass {
   Blend SrcAlpha One
   ZWrite Off
   ZTest LEqual
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A {float4 positionOS:POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V {float4 positionCS:SV_POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
   float _Head;
   CBUFFER_END
   V vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.color=a.color;o.uv=a.uv;return o;}
   half4 frag(V v):SV_Target{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
    float across=abs(v.uv.y-.5)*2;
    float core=exp(-across*across*120);
    float halo=exp(-across*across*7)*.45;
    float behind=_Head-v.uv.x;
    float reveal=smoothstep(-.015,.015,behind);
    float head=exp(-pow(behind/.075,2));
    float tail=exp(-max(behind,0)*8)*reveal;
    float energy=reveal*.32+tail*.9+head*1.25;
    float ends=smoothstep(0,.025,v.uv.x)*(1-smoothstep(.975,1,v.uv.x));
    half3 colour=lerp(_BaseColor.rgb,half3(4,3.6,2.5),core*.75);
    return half4(colour*v.color.rgb,saturate((core+halo)*energy*ends*_BaseColor.a*v.color.a));
   }
   ENDHLSL
  }
 }
}
