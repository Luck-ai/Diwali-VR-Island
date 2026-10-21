#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DiwaliGameplay {
public static class SweetFireworksLayoutUpdate {
 [MenuItem("Diwali Island/Gameplay/Apply latest Sweet and Fireworks layout")]
 public static void ApplyLatestSweetFireworksLayout(){
  if(Application.isPlaying)throw new InvalidOperationException("Apply layout in Edit Mode.");
  var sweet=UnityEngine.Object.FindFirstObjectByType<SweetGame>();var fire=UnityEngine.Object.FindFirstObjectByType<FireworksGame>();if(!sweet||!fire)throw new InvalidOperationException("Install both games before applying layout.");
  var venue=GameObjectUtility.FindExact("SweetShop - Approved Warm Palette")??GameObjectUtility.FindExact("SweetShop - Native PBR");
  var counter=GameObjectUtility.BoundsOf(VenueInstallHelpers.Part(venue,"Cube.012"));float y=counter.max.y+GameObjectUtility.BoundsOf(sweet.basket.transform).extents.y+.015f;
  Place(sweet.basket.transform,venue,new Vector3(.8f,0,-2.3f),y);Place(sweet.rest,venue,new Vector3(0,0,-1.7f),y);Place(sweet.checkout,venue,new Vector3(1.6f,0,-1.8f),y);
  sweet.panelPosition=venue.TransformPoint(new Vector3(.4f,0,-1.4f));sweet.panelPosition=new Vector3(sweet.panelPosition.x,venue.position.y+1.65f,sweet.panelPosition.z);sweet.panelRotation=FaceAisle(venue,sweet.panelPosition);
  var fv=GameObjectUtility.FindExact("Fireworks - Native PBR");var rocketPosition=fire.rocketSocket.position;var fountainPosition=fire.fountainSocket.position;var spinnerPosition=fire.spinnerSocket.position;
  fire.rocketPlatform.rotation=fv.rotation;fire.rocketHome=fire.rocketPlatform.localRotation;fire.spinnerPivot.rotation=fv.rotation;fire.spinnerHome=fire.spinnerPivot.localRotation;
  fire.rocketSocket.SetPositionAndRotation(rocketPosition,fire.rocket.transform.rotation);fire.fountainSocket.SetPositionAndRotation(fountainPosition,fire.fountain.transform.rotation);fire.spinnerSocket.SetPositionAndRotation(spinnerPosition,fire.spinner.transform.rotation);
  fire.rocket.transform.position=rocketPosition;fire.fountain.transform.position=fountainPosition;fire.spinner.transform.position=spinnerPosition;
  fire.panelRotation=FaceAisle(fv,fire.panelPosition);
  const string path="Assets/DiwaliGameplay/Fireworks/FireworkSparks.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(!material){var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit")??Shader.Find("Particles/Standard Unlit");if(!shader)throw new InvalidOperationException("Particle shader missing");material=new Material(shader){name="Firework Sparks"};material.SetColor("_BaseColor",Color.white);AssetDatabase.CreateAsset(material,path);}
  foreach(var effect in new[]{fire.rocketEffect,fire.fountainEffect,fire.spinnerEffect}){effect.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;EditorUtility.SetDirty(effect.GetComponent<ParticleSystemRenderer>());}
  foreach(var t in new[]{sweet.basket.transform,sweet.rest,sweet.checkout,fire.rocket.transform,fire.fountain.transform,fire.spinner.transform,fire.rocketPlatform,fire.spinnerPivot,fire.rocketSocket,fire.fountainSocket,fire.spinnerSocket})EditorUtility.SetDirty(t);
  EditorUtility.SetDirty(sweet);EditorUtility.SetDirty(fire);EditorSceneManager.MarkSceneDirty(sweet.gameObject.scene);Debug.Log("Updated Sweet and Fireworks layout, source rotations and persistent particles. Save scene to retain changes.");
 }
 static void Place(Transform target,Transform venue,Vector3 local,float y){var p=venue.TransformPoint(local);p.y=y;target.position=p;}
 static Quaternion FaceAisle(Transform venue,Vector3 position){var toward=venue.TransformPoint(Vector3.zero)-position;toward.y=0;return Quaternion.LookRotation(-toward.normalized,Vector3.up);}
}
}
#endif
