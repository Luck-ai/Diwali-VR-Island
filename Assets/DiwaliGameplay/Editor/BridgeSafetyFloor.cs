#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace DiwaliGameplay {
public static class BridgeSafetyFloor {
 [MenuItem("Diwali/Final Pass/Add bridge safety floor")]
 public static void Apply(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
  var bridge=GameObjectUtility.FindExact("Diwali Christmas Walkway");
  if(!bridge)throw new InvalidOperationException("Festival bridge is missing.");
  var decks=bridge.GetComponentsInChildren<BoxCollider>().Where(c=>c.name.StartsWith("Walkable Deck")||c.name.StartsWith("Turn Landing")).ToArray();
  if(decks.Length==0)throw new InvalidOperationException("Bridge deck colliders are missing.");
  var bounds=decks[0].bounds;foreach(var deck in decks.Skip(1))bounds.Encapsulate(deck.bounds);
  if(bounds.size.y>.6f)throw new InvalidOperationException("A sloping bridge needs safety floors fitted per segment.");
  var floor=GameObjectUtility.FindUnder(bridge,"Bridge safety floor - wide walkable margin");
  if(!floor){var go=new GameObject("Bridge safety floor - wide walkable margin",typeof(BoxCollider));Undo.RegisterCreatedObjectUndo(go,"Add bridge safety floor");floor=go.transform;floor.SetParent(bridge,true);}
  Undo.RecordObject(floor,"Fit bridge safety floor");floor.SetPositionAndRotation(new Vector3(bounds.center.x,bounds.max.y-.14f,bounds.center.z),Quaternion.identity);floor.localScale=Vector3.one;
  var collider=floor.GetComponent<BoxCollider>();Undo.RecordObject(collider,"Widen bridge safety collision");collider.size=new Vector3(bounds.size.x+10f,.2f,bounds.size.z+4f);collider.center=Vector3.zero;collider.isTrigger=false;
  var source=decks.Select(d=>d.GetComponent<TeleportationArea>()).FirstOrDefault(a=>a);
  var area=floor.GetComponent<TeleportationArea>();if(!area)area=Undo.AddComponent<TeleportationArea>(floor.gameObject);
  Undo.RecordObject(area,"Allow teleporting onto bridge safety floor");area.colliders.Clear();area.colliders.Add(collider);area.matchOrientation=MatchOrientation.WorldSpaceUp;
  if(source){area.interactionLayers=source.interactionLayers;area.teleportationProvider=source.teleportationProvider;}
  EditorUtility.SetDirty(area);Physics.SyncTransforms();
  var checks=Enumerable.Range(0,5).SelectMany(i=>new[]{-4.5f,0,4.5f}.Select(offset=>{var p=new Vector3(bounds.center.x+offset,bounds.max.y+.5f,Mathf.Lerp(bounds.min.z,bounds.max.z,i/4f));bool supported=Physics.RaycastAll(p,Vector3.down,1f,~0,QueryTriggerInteraction.Ignore).Any(h=>h.collider==collider);return new{x=p.x,z=p.z,supported};})).ToArray();
  var scene=floor.gameObject.scene;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  var path=Path.GetFullPath("../Game_Plans/Finish_Today_2026-10-24/Bridge_Safety_Floor.json");Directory.CreateDirectory(Path.GetDirectoryName(path));
  File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(new{width=collider.bounds.size.x,length=collider.bounds.size.z,top=collider.bounds.max.y,visibleRenderer=floor.GetComponent<Renderer>()!=null,teleportEnabled=area.enabled,checks},Newtonsoft.Json.Formatting.Indented));
  if(checks.Any(c=>!c.supported))throw new InvalidOperationException("A bridge safety-floor sample lacks collision support.");
 }
}
}
#endif
