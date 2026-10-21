using UnityEngine;
using UnityEngine.Rendering;
namespace DiwaliGameplay {
public sealed class DiyaFlameVisual : MonoBehaviour {
 public Light source;
 ParticleSystem flame;static Material flameMaterial;
 void Start(){
 var go=new GameObject("Soft rising flame");go.transform.SetParent(transform,false);flame=go.AddComponent<ParticleSystem>();flame.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
 var main=flame.main;main.loop=true;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.startLifetime=.45f;main.startSpeed=.14f;main.startSize=new ParticleSystem.MinMaxCurve(.06f,.10f);main.startColor=new Color(1,.72f,.23f,.85f);main.maxParticles=24;
 var emission=flame.emission;emission.rateOverTime=32;var shape=flame.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=4;shape.radius=.018f;shape.rotation=new Vector3(-90,0,0);
 var size=flame.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,0));
 var color=flame.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(1,1,.65f),0),new GradientColorKey(new Color(1,.35f,.04f),1)},new[]{new GradientAlphaKey(.9f,0),new GradientAlphaKey(.7f,.4f),new GradientAlphaKey(0,1)});color.color=gradient;
 var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.sharedMaterial=Material();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
 }
 static Material Material(){if(flameMaterial)return flameMaterial;var tex=new Texture2D(32,32,TextureFormat.RGBA32,false);tex.name="Diya soft flame alpha";tex.wrapMode=TextureWrapMode.Clamp;for(int y=0;y<32;y++)for(int x=0;x<32;x++){float u=(x-15.5f)/15.5f,v=(y-15.5f)/15.5f;float a=Mathf.Pow(Mathf.Clamp01(1-u*u-v*v),2);tex.SetPixel(x,y,new Color(1,1,1,a));}tex.Apply();flameMaterial=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));flameMaterial.name="Diya flame particles";flameMaterial.SetTexture("_BaseMap",tex);flameMaterial.SetColor("_BaseColor",Color.white);flameMaterial.SetFloat("_Surface",1);flameMaterial.SetFloat("_Blend",0);flameMaterial.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);flameMaterial.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);flameMaterial.SetFloat("_ZWrite",0);flameMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");flameMaterial.renderQueue=3000;return flameMaterial;}
 void LateUpdate(){if(!source||!flame)return;if(source.enabled&&!flame.isPlaying)flame.Play();else if(!source.enabled&&flame.isPlaying)flame.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
}
}
