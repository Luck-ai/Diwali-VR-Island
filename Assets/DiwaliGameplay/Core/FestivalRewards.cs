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
 public bool SweetsShared=>sweetGift&&sweetGift.activeSelf;
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
  case "SweetGame":ShareSweetGift((SweetGame)game);game.status+=" Your sweet gift is on display in the central plaza.";break;
  case "FireworksGame":LaunchPlazaFinale(game);break;
 }}
 void Update(){if(spin&&rangoli)rangoli.Rotate(Vector3.up,8*Time.deltaTime,Space.World);}
 void ShareSweetGift(SweetGame game){
  sweetGift=new GameObject("Festival sweet gift - checkout reward");sweetGift.transform.position=new Vector3(4,0,20.75f);
  var table=GameObject.CreatePrimitive(PrimitiveType.Cube);table.name="Sweet sharing plinth";table.transform.SetParent(sweetGift.transform,false);table.transform.localPosition=new Vector3(0,.4f,0);table.transform.localScale=new Vector3(1.1f,.8f,.8f);var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=new Color(.45f,.12f,.08f);mat.SetFloat("_Smoothness",.2f);table.GetComponent<Renderer>().sharedMaterial=mat;
  var basket=CopyVisual(game.basket.transform,sweetGift.transform);var b=GameObjectUtility.BoundsOf(basket);float scale=.85f/Mathf.Max(b.size.x,b.size.z);basket.localScale*=scale;b=GameObjectUtility.BoundsOf(basket);basket.position+=new Vector3(4,.8f,20.75f)-new Vector3(b.center.x,b.min.y,b.center.z);
  int n=0;foreach(var item in game.sweets){if(!item)continue;var clone=CopyVisual(item.transform,sweetGift.transform);var cb=GameObjectUtility.BoundsOf(clone);clone.localScale*=.18f/Mathf.Max(cb.size.x,cb.size.z);cb=GameObjectUtility.BoundsOf(clone);clone.position+=new Vector3(3.82f+(n%2)*.34f,1.0f,20.6f+(n/2)*.3f)-new Vector3(cb.center.x,cb.min.y,cb.center.z);if(++n==4)break;}
  var sign=new GameObject("Sweet gift plaque").AddComponent<TextMesh>();sign.transform.SetParent(sweetGift.transform,false);sign.transform.localPosition=new Vector3(0,.6f,-.415f);sign.text="SWEETS TO SHARE\nGift prepared at the Sweet Shop";sign.anchor=TextAnchor.MiddleCenter;sign.fontSize=44;sign.characterSize=.009f;sign.color=new Color(1,.82f,.42f);
 }
 static Transform CopyVisual(Transform source,Transform parent){var root=new GameObject(source.name+" display copy").transform;root.SetParent(parent,true);foreach(var mr in source.GetComponentsInChildren<MeshRenderer>()){var mf=mr.GetComponent<MeshFilter>();if(!mf)continue;var go=new GameObject(mr.name);go.transform.SetParent(root,true);go.transform.SetPositionAndRotation(mr.transform.position,mr.transform.rotation);go.transform.localScale=mr.transform.lossyScale;go.AddComponent<MeshFilter>().sharedMesh=mf.sharedMesh;go.AddComponent<MeshRenderer>().sharedMaterials=mr.sharedMaterials;}return root;}
 void LaunchPlazaFinale(VenueGame owner){
  var f=owner as FireworksGame;if(!f||!f.rocketEffect)return;
  for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;var launch=new Vector3(Mathf.Cos(a)*5,.25f,20.75f+Mathf.Sin(a)*5);var burst=Instantiate(f.rocketEffect,launch+Vector3.up*8,Quaternion.identity,transform);burst.name="Finale fireworks above central decoration "+(i+1);burst.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=burst.main;main.loop=true;main.duration=3.5f+i*.4f;main.startDelay=i*.6f;main.startLifetime=2;main.startSpeed=3.8f;main.startSize=.07f;var trails=burst.trails;trails.enabled=true;trails.lifetime=.2f;FestivalParticleStyle.Apply(burst);burst.Play();if(!centralFireworks)centralFireworks=burst;
   var trail=new GameObject("Finale rising launch trail "+(i+1));trail.transform.SetParent(transform,true);trail.transform.position=launch;trail.transform.rotation=Quaternion.Euler(-90,0,0);var ps=trail.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var m=ps.main;m.playOnAwake=false;m.loop=true;m.duration=3.5f+i*.4f;m.startDelay=i*.6f;m.startLifetime=1.2f;m.startSpeed=6.7f;m.startSize=.055f;m.gravityModifier=.12f;var em=ps.emission;em.rateOverTime=0;em.SetBursts(new[]{new ParticleSystem.Burst(.1f,7)});var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=1;shape.radius=.03f;FestivalParticleStyle.Apply(ps);ps.Play();}
 }
}
}
