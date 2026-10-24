using System.Collections;
using UnityEngine;

namespace DiwaliGameplay {
public sealed class FireworksTargetBoard : MonoBehaviour {
 public Transform board,stand;
 public bool LastHit {get;private set;}
 Vector3 home;float minimumY,maximumY; float missUntil;
 Renderer[] renderers; MaterialPropertyBlock flash;
 TextMesh label; ParticleSystem sparks;
 public void Configure(Transform boardTransform,Transform standTransform){
  flash=new MaterialPropertyBlock();board=boardTransform;stand=standTransform;home=board.position;var standBounds=GameObjectUtility.BoundsOf(stand);var boardBounds=GameObjectUtility.BoundsOf(board);minimumY=standBounds.min.y+boardBounds.extents.y+.08f;maximumY=Mathf.Max(minimumY+.15f,standBounds.max.y-boardBounds.extents.y+.02f);renderers=board.GetComponentsInChildren<Renderer>();
  var b=GameObjectUtility.BoundsOf(board);
  var go=new GameObject("Rocket target rings");go.transform.SetParent(board,true);go.transform.position=b.center+Vector3.back*(b.extents.z+.012f);
  for(int j=0;j<3;j++){var ring=new GameObject("Target ring "+j).AddComponent<LineRenderer>();ring.transform.SetParent(go.transform,false);ring.loop=true;ring.useWorldSpace=false;ring.positionCount=48;ring.widthMultiplier=.012f;ring.sharedMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));ring.sharedMaterial.color=j==1?new Color(.65f,.12f,.06f):new Color(1,.76f,.25f);float radius=.08f+j*.09f;for(int k=0;k<48;k++){float a=k*Mathf.PI*2/48;ring.SetPosition(k,new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0));}}
  var text=new GameObject("Target feedback");text.transform.SetParent(go.transform,false);text.transform.localPosition=new Vector3(0,.35f,-.015f);label=text.AddComponent<TextMesh>();label.anchor=TextAnchor.MiddleCenter;label.fontSize=48;label.characterSize=.009f;label.color=new Color(1,.86f,.49f);label.text="ROCKET TARGET";
  var fx=new GameObject("Target hit sparks");fx.transform.SetParent(go.transform,false);sparks=fx.AddComponent<ParticleSystem>();sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=sparks.main;main.playOnAwake=false;main.loop=false;main.duration=.5f;main.startLifetime=.65f;main.startSpeed=1.6f;main.startSize=.045f;main.startColor=new Color(1,.65f,.12f);main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=.3f;var emission=sparks.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,45)});var shape=sparks.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.06f;var renderer=sparks.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));FestivalParticleStyle.Apply(sparks);
 }
 public Bounds TargetBounds{get{var b=renderers[0].bounds;for(int i=1;i<renderers.Length;i++)b.Encapsulate(renderers[i].bounds);return b;}}
 public void SetHeight(float value){board.position=new Vector3(home.x,Mathf.Lerp(minimumY,maximumY,Mathf.Clamp01(value)),home.z);LastHit=false;missUntil=0;if(renderers!=null)foreach(var renderer in renderers)if(renderer)renderer.SetPropertyBlock(null);if(sparks)sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);if(label){label.text="ROCKET TARGET";label.color=new Color(1,.86f,.49f);}}

 public bool TryAim(Vector3 start,Vector3 direction,out Vector3 hit){var b=TargetBounds;float z=b.center.z-b.extents.z-.015f;float t=Mathf.Abs(direction.z)>.001f?(z-start.z)/direction.z:-1;hit=start+direction*Mathf.Max(0,t);return t>0&&Mathf.Abs(hit.x-b.center.x)<b.extents.x+.12f&&Mathf.Abs(hit.y-b.center.y)<b.extents.y+.12f;}
 public IEnumerator Hit(Vector3 point){LastHit=true;label.text="HIT!  TO THE SKY";sparks.transform.position=point;sparks.Play();for(float t=0;t<.45f;t+=Time.deltaTime){if(flash==null)flash=new MaterialPropertyBlock();flash.SetColor("_EmissionColor",new Color(1,.48f,.06f)*(1-t/.45f));foreach(var r in renderers)if(r)r.SetPropertyBlock(flash);yield return null;}foreach(var r in renderers)if(r)r.SetPropertyBlock(null);}
 public void ShowAim(bool aligned){if(!label||LastHit||Time.time<missUntil)return;label.text=aligned?"AIM READY - PRESS PLUNGER":"ADJUST TILT / TARGET HEIGHT";label.color=aligned?new Color(.3f,1,.55f):new Color(1,.86f,.49f);}
 public void ShowMiss(){missUntil=Time.time+2;if(label){label.text="MISS - ADJUST TILT / HEIGHT";label.color=new Color(1,.65f,.3f);}}
}
}
