Shader "Diwali/Diya Dust Coating" {
 Properties {
  _BaseColor("Dust colour", Color) = (.48,.41,.29,.86)
  _Clean("Cleaned amount", Range(0,1)) = 0
 }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Offset -1, -1
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
   CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;
    float _Clean;
   CBUFFER_END
   Varyings vert(Attributes v) { Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;return o; }
   float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
   float noise(float2 p) {
    float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
   }
   half4 frag(Varyings i):SV_Target {
    float patch=noise(i.uv*15);
    float grain=noise(i.uv*100);
    float sweep=saturate(i.uv.x*.72+patch*.28);
    float remaining=smoothstep(_Clean-.06,_Clean+.06,sweep)*(1-smoothstep(.96,1,_Clean));
    float alpha=_BaseColor.a*smoothstep(.12,.35,patch)*lerp(.58,1,grain)*remaining;
    return half4(_BaseColor.rgb*lerp(.75,1.15,grain),alpha);
   }
   ENDHLSL
  }
 }
}
