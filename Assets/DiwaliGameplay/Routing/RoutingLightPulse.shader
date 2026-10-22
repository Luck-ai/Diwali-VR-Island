Shader "Diwali/Routing Light Pulse" {
 Properties { _BaseColor("Pulse colour",Color)=(2,1.3,0.3,1) }
 SubShader {Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+100"}
 Pass {Blend SrcAlpha One ZWrite Off ZTest Always Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A {float4 positionOS:POSITION;half4 color:COLOR;};struct V {float4 positionCS:SV_POSITION;half4 color:COLOR;};half4 _BaseColor;
 V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.color=a.color;return o;}
 half4 frag(V v):SV_Target{half4 c=v.color*_BaseColor;c.a=saturate(c.a);return c;}
 ENDHLSL }
 }
}
