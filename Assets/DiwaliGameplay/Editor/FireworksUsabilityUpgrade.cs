#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DiwaliGameplay {
public static class FireworksUsabilityUpgrade {
 [MenuItem("Diwali Island/Gameplay/Upgrade current Fireworks staging tray")]
 public static void Apply(){
  if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode before upgrading staging.");
  var game=UnityEngine.Object.FindFirstObjectByType<FireworksGame>();var venue=GameObjectUtility.FindExact("Fireworks - Native PBR");if(!game||!venue||!game.rocket||!game.rocketSocket)throw new InvalidOperationException("Existing Fireworks gameplay is required.");
  var source=GameObjectUtility.FindUnder(venue,"platform");if(!source)throw new InvalidOperationException("Existing fountain platform mesh is missing.");
  const string name="Rocket staging tray — reused platform";var tray=GameObjectUtility.FindExact(name);
  if(!tray){var copy=UnityEngine.Object.Instantiate(source.gameObject,game.transform);copy.name=name;Undo.RegisterCreatedObjectUndo(copy,"Reuse platform as rocket staging tray");tray=copy.transform;tray.SetPositionAndRotation(source.position,source.rotation);tray.localScale=source.lossyScale*.8f;}
  Undo.RecordObject(tray,"Position rocket staging tray");Undo.RecordObject(game.rocket.transform,"Stage rocket away from launcher");Undo.RecordObject(game,"Set rocket staging home");
  var b=GameObjectUtility.BoundsOf(tray);var target=game.rocketSocket.position-venue.right*1.15f;target.y=venue.position.y+.035f;
  var floor=GameObjectUtility.FindUnder(venue,"Circle");if(floor){var floorBounds=GameObjectUtility.BoundsOf(floor);target.x=Mathf.Clamp(target.x,floorBounds.min.x+b.extents.x+.15f,floorBounds.max.x-b.extents.x-.15f);target.z=Mathf.Clamp(target.z,floorBounds.min.z+b.extents.z+.15f,floorBounds.max.z-b.extents.z-.15f);}
  tray.position+=new Vector3(target.x-b.center.x,target.y-b.min.y,target.z-b.center.z);b=GameObjectUtility.BoundsOf(tray);var half=GameObjectUtility.BoundsOf(game.rocket.transform).extents.y;game.rocketStagingPosition=new Vector3(b.center.x,b.max.y+half+.015f,b.center.z);game.rocket.transform.position=game.rocketStagingPosition;
  if(Vector2.Distance(new Vector2(game.rocketStagingPosition.x,game.rocketStagingPosition.z),new Vector2(game.rocketSocket.position.x,game.rocketSocket.position.z))<.75f)throw new InvalidOperationException("Tray is too near the launcher; inspect venue clearance.");
  EditorUtility.SetDirty(tray);EditorUtility.SetDirty(game.rocket.transform);EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(game.gameObject.scene);Debug.Log("Fireworks staging upgraded in place. Rocket rests on reused platform tray, separate from launcher. Save scene, then start a fresh Play session to capture its reset home.");
 }
}
}
#endif
