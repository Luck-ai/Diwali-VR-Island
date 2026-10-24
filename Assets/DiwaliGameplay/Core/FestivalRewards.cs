using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace DiwaliGameplay {
public sealed class FestivalRewards : MonoBehaviour {
 static FestivalRewards instance;
 readonly HashSet<string> earned=new HashSet<string>();
 readonly List<Renderer> flames=new List<Renderer>(),silks=new List<Renderer>();
 readonly List<Light> lamps=new List<Light>();
 Transform rangoli;bool spin;GameObject centralControl,sweetGift;ParticleSystem centralFireworks;
 public int FlameCount=>flames.Count;
 public int LampCount=>lamps.Count;
 public bool CentralDisplayEnabled=>centralControl&&centralControl.activeSelf;
 public bool CentralDisplayRunning=>centralFireworks&&centralFireworks.isPlaying;
 public bool RangoliSpinning=>spin;
 public bool SweetsShared=>earned.Contains("SweetGame");
 public bool SweetCelebrationActive=>sweetGift&&sweetGift.activeSelf;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){instance=null;var root=GameObjectUtility.FindExact("Diwali Festival Ground");if(root){var go=new GameObject("Connected festival rewards");go.transform.SetParent(root,true);instance=go.AddComponent<FestivalRewards>();instance.Initialise(root);}}
 void Initialise(Transform root){
  foreach(var r in root.GetComponentsInChildren<Renderer>(true)){
   if(r.name=="DiyaFlame"){flames.Add(r);r.enabled=false;}
   if(r.name=="Lamp silk - combined detail"){silks.Add(r);SetLanternPower(r,false);}
  }
  foreach(var light in root.GetComponentsInChildren<Light>(true))if(light.name=="Warm lantern pool"){lamps.Add(light);light.intensity=.05f;}
  var floor=GameObjectUtility.FindExact("Flower rangoli - walkable inlaid carpet");
  if(floor){rangoli=new GameObject("Central rangoli celebration pivot").transform;rangoli.SetParent(root,true);rangoli.position=floor.position;var decorationRoot=floor.parent;var layers=new List<Transform>();foreach(Transform t in decorationRoot)if(t==floor||t.name.EndsWith("- combined detail"))layers.Add(t);foreach(var layer in layers)layer.SetParent(rangoli,true);}

 }
 static void SetLanternPower(Renderer renderer,bool on){foreach(var material in renderer.materials){if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",on?new Color(1.65f,1.12f,.42f):new Color(.12f,.085f,.035f));if(material.HasProperty("_EmissionColor")){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",on?new Color(.6f,.32f,.1f):Color.black);}}}
 static void SetEmission(Renderer r,Color color){foreach(var m in r.materials){if(!m.HasProperty("_EmissionColor"))continue;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color);}}
 public static void Award(VenueGame game){if(!instance)instance=Object.FindFirstObjectByType<FestivalRewards>();if(instance)instance.Apply(game);}
 void Apply(VenueGame game){if(!earned.Add(game.GetType().Name))return;switch(game.GetType().Name){
  case "DiyaGame":foreach(var r in flames){r.enabled=true;var b=r.bounds;var go=new GameObject("Festival diya lit flame");go.transform.SetParent(r.transform.parent,true);go.transform.position=new Vector3(b.center.x,b.max.y,b.center.z);go.AddComponent<DiyaFlameVisual>().decorative=true;}var main=GameObjectUtility.FindExact("Central lamp warm light");if(main&&main.GetComponent<Light>())main.GetComponent<Light>().intensity=1.1f;game.status+=" All festival diyas are now lit.";break;
  case "RoutingGame":foreach(var r in silks)SetLanternPower(r,true);foreach(var l in lamps){l.intensity=.85f;l.range=5;l.shadows=LightShadows.None;}game.status+=" The island lanterns are now lit.";break;
  case "RangoliGame":spin=true;if(rangoli)foreach(var r in rangoli.GetComponentsInChildren<Renderer>())SetEmission(r,new Color(.13f,.055f,.015f));game.status+=" The central rangoli now glows and spins.";break;
  case "SweetGame":ShareSweetGift((SweetGame)game);game.status+=" A warm sharing celebration lights your gift at checkout.";break;
  case "FireworksGame":LaunchPlazaFinale(game);break;
 }}
 void Update(){if(spin&&rangoli)rangoli.Rotate(Vector3.up,8*Time.deltaTime,Space.World);}
 void ShareSweetGift(SweetGame game){
  if(!game.basket)return;
  var bounds=GameObjectUtility.BoundsOf(game.basket.transform);
  sweetGift=new GameObject("Sweet gift checkout sharing celebration");
  sweetGift.transform.SetParent(game.basket.transform,true);
  sweetGift.transform.position=bounds.center+Vector3.up*bounds.extents.y*.4f;
  var scale=game.basket.transform.lossyScale;sweetGift.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
  var light=sweetGift.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.65f,.25f);light.intensity=0;light.range=1.8f;light.shadows=LightShadows.None;
  var sparks=sweetGift.AddComponent<ParticleSystem>();sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var main=sparks.main;main.playOnAwake=false;main.loop=false;main.duration=2.5f;main.startLifetime=new ParticleSystem.MinMaxCurve(.7f,1.3f);main.startSpeed=new ParticleSystem.MinMaxCurve(.08f,.2f);main.startSize=new ParticleSystem.MinMaxCurve(.015f,.03f);main.startColor=new Color(1,.78f,.35f,.8f);main.maxParticles=32;main.scalingMode=ParticleSystemScalingMode.Shape;main.simulationSpace=ParticleSystemSimulationSpace.World;
  var emission=sparks.emission;emission.rateOverTime=12;
  var shape=sparks.shape;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=Mathf.Clamp(Mathf.Max(bounds.extents.x,bounds.extents.z)*.8f,.15f,.45f);shape.radiusThickness=.15f;shape.rotation=new Vector3(-90,0,0);
  var size=sparks.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,0));
  FestivalParticleStyle.Apply(sparks);sparks.Play();StartCoroutine(FadeSharingCelebration(sweetGift,light));
 }
 IEnumerator FadeSharingCelebration(GameObject effect,Light light){
  float elapsed=0;while(effect&&elapsed<4){elapsed+=Time.deltaTime;float fade=Mathf.Clamp01(elapsed/.4f)*Mathf.Clamp01((4-elapsed)/1.4f);light.intensity=fade*(.9f+.25f*Mathf.Sin(elapsed*4));yield return null;}
  if(effect)Destroy(effect);
 }
 void LaunchPlazaFinale(VenueGame owner){
  var f=owner as FireworksGame;if(!f||!f.rocketEffect)return;
  for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;var launch=new Vector3(Mathf.Cos(a)*5,.25f,20.75f+Mathf.Sin(a)*5);var burst=Instantiate(f.rocketEffect,launch+Vector3.up*8,Quaternion.identity,transform);burst.name="Finale fireworks above central decoration "+(i+1);burst.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=burst.main;main.loop=true;main.duration=3.5f+i*.4f;main.startDelay=i*.6f;main.startLifetime=2;main.startSpeed=3.8f;main.startSize=.07f;var trails=burst.trails;trails.enabled=true;trails.lifetime=.2f;FestivalParticleStyle.Apply(burst);burst.Play();if(!centralFireworks)centralFireworks=burst;
   var trail=new GameObject("Finale rising launch trail "+(i+1));trail.transform.SetParent(transform,true);trail.transform.position=launch;trail.transform.rotation=Quaternion.Euler(-90,0,0);var ps=trail.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var m=ps.main;m.playOnAwake=false;m.loop=true;m.duration=3.5f+i*.4f;m.startDelay=i*.6f;m.startLifetime=1.2f;m.startSpeed=6.7f;m.startSize=.055f;m.gravityModifier=.12f;var em=ps.emission;em.rateOverTime=0;em.SetBursts(new[]{new ParticleSystem.Burst(.1f,7)});var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=1;shape.radius=.03f;FestivalParticleStyle.Apply(ps);ps.Play();}
 }
}
}
