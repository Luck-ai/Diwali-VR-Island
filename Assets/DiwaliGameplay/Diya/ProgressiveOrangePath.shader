Shader "Diwali/Progressive Orange Path" {
 Properties { _BaseColor("Unlit orange",Color)=(0.3,0.065,0.004,1) _GlowColor("Visited orange",Color)=(3,0.8,0.03,1) }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A { float4 positionOS:POSITION; };struct V { float4 positionCS:SV_POSITION;float3 world:TEXCOORD0; };
 float4 _BaseColor,_GlowColor,_Visited[32];int _VisitedCount;
 V vert(A a){V o;o.world=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);return o;}
 half4 frag(V v):SV_Target { float glow=0;for(int i=0;i<_VisitedCount;i++){float d=distance(v.world.xz,_Visited[i].xz);glow=max(glow,1-smoothstep(0.45,0.95,d));}return half4(lerp(_BaseColor.rgb,_GlowColor.rgb,glow),1); }
 ENDHLSL }
 }
}
