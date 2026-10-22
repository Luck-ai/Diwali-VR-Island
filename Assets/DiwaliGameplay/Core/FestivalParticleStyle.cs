using UnityEngine;
using UnityEngine.Rendering;
namespace DiwaliGameplay {
public static class FestivalParticleStyle {
 static Material material;
 public static void Apply(ParticleSystem system){
  if(!material){var texture=new Texture2D(32,32,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;texture.name="Soft golden spark";for(int y=0;y<32;y++)for(int x=0;x<32;x++){float u=(x-15.5f)/15.5f,v=(y-15.5f)/15.5f;float a=Mathf.Exp(-(u*u+v*v)*7)*Mathf.Clamp01((1-u*u-v*v)*3);texture.SetPixel(x,y,new Color(1,1,1,a));}texture.Apply();material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",Color.white);material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.One);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;}
  var renderer=system.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.trailMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
  var color=system.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(1,.38f,.07f),1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.7f,.6f),new GradientAlphaKey(0,1)});color.color=gradient;
 }
}
}
