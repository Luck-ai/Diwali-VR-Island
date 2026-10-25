#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace DiwaliGameplay {
public static class FireworksInstaller {
 static ParticleSystem Effect(Transform parent,string name,Vector3 position,Color color,bool fountain=false){var go=new GameObject(name);go.transform.SetParent(parent,true);go.transform.position=position;go.transform.rotation=Quaternion.Euler(-90,0,0);var p=go.AddComponent<ParticleSystem>();var main=p.main;main.playOnAwake=false;main.duration=fountain?2:1.5f;main.loop=fountain;main.startLifetime=fountain?.7f:1.6f;main.startSpeed=fountain?2.5f:3;main.startSize=.055f;main.startColor=color;main.gravityModifier=.2f;main.maxParticles=120;var e=p.emission;e.rateOverTime=fountain?45:0;if(!fountain)e.SetBursts(new[]{new ParticleSystem.Burst(0,65)});var shape=p.shape;shape.shapeType=fountain?ParticleSystemShapeType.Cone:ParticleSystemShapeType.Sphere;shape.angle=20;shape.radius=.08f;var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit")??Shader.Find("Particles/Standard Unlit");if(shader){const string path="Assets/DiwaliGameplay/Fireworks/FireworkSparks.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(shader);m.name="Firework Sparks";m.SetColor("_BaseColor",Color.white);AssetDatabase.CreateAsset(m,path);}p.GetComponent<ParticleSystemRenderer>().sharedMaterial=m;}p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);return p;}
 public static VenueGame Install(Transform gameplayRoot){
  var venue=GameObjectUtility.FindExact("Fireworks - Native PBR");if(!venue)throw new InvalidOperationException("Fireworks - Native PBR missing");FireworksAccessibleLayout.Apply(venue);var root=new GameObject("Fireworks light show gameplay").transform;root.SetParent(gameplayRoot,false);var g=root.gameObject.AddComponent<FireworksGame>();
  Func<string,Transform> part=n=>VenueInstallHelpers.Part(venue,n);
  g.rocketPlatform=VenueInstallHelpers.Pivot(part("rocket stand rotating"),"Rocket turntable pivot",root);g.rocketPlatform.rotation=venue.rotation;g.rocketHome=g.rocketPlatform.localRotation;
  g.lift=VenueInstallHelpers.Pivot(part("fountain lift "),"Fountain lift pivot",root);g.liftHome=g.lift.localPosition;
  g.rocket=VenueInstallHelpers.Grab(part("rocker"),"rocket",root);g.fountain=VenueInstallHelpers.Grab(part("fountain firework"),"fountain",root);g.spinner=VenueInstallHelpers.Grab(part("spinner firework"),"spinner",root);
  g.rocketSocket=VenueInstallHelpers.Point(g.rocketPlatform,"Rocket socket",g.rocket.transform.position);g.fountainSocket=VenueInstallHelpers.Point(g.lift,"Fountain socket",g.fountain.transform.position);g.spinnerPivot=VenueInstallHelpers.Point(root,"Spinner rotating hub",g.spinner.transform.position);g.spinnerSocket=VenueInstallHelpers.Point(g.spinnerPivot,"Spinner socket",g.spinner.transform.position);
  g.rocketSocket.rotation=g.rocket.transform.rotation;g.fountainSocket.rotation=g.fountain.transform.rotation;g.spinnerPivot.rotation=venue.rotation;g.spinnerHome=g.spinnerPivot.localRotation;g.spinnerSocket.rotation=g.spinner.transform.rotation;
  FireworksUsabilityUpgrade.PlaceRocketOnGround(g,venue);
  g.plunger=VenueInstallHelpers.Grab(part("plunger"),"plunger",root);g.plungerHome=g.plunger.transform.localPosition;g.plunger.grab.trackPosition=false;g.plunger.grab.trackRotation=false;
  g.rocketEffect=Effect(root,"Rocket sky burst",g.rocketSocket.position+Vector3.up*6,new Color(1,.6f,.12f));g.fountainEffect=Effect(g.lift,"Fountain sparks",g.fountainSocket.position+Vector3.up*.3f,new Color(1,.8f,.3f),true);g.spinnerEffect=Effect(g.spinnerPivot,"Spinner sparks",g.spinnerSocket.position,new Color(1,.3f,.6f));
  var board=GameObjectUtility.BoundsOf(part("board"));g.panelPosition=board.center+venue.TransformVector(new Vector3(0,.65f,-.65f));var toward=venue.TransformPoint(Vector3.zero)-g.panelPosition;toward.y=0;g.panelRotation=Quaternion.LookRotation(-toward.normalized,Vector3.up);return g;
 }
}
}
#endif
