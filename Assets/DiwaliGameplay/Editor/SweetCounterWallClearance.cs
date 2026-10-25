#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DiwaliGameplay {
public static class SweetCounterWallClearance {
 [MenuItem("Diwali/Gameplay/Clear Sweet counter from wall")]
 public static void Apply(){
  if(Application.isPlaying)throw new InvalidOperationException("Apply counter clearance outside Play Mode.");
  var game=UnityEngine.Object.FindFirstObjectByType<SweetGame>();
  var venue=GameObjectUtility.FindExact("SweetShop - Approved Warm Palette");
  const string stamp="Sweet counter wall clearance";
  if(!game||!venue)throw new InvalidOperationException("Sweet shop gameplay missing.");
  if(venue.Find(stamp))return;
  var group=GameObjectUtility.FindUnder(venue,"Accessible checkout group");
  var parts=new[]{group,game.rest,game.checkout,game.cardReader,game.card.transform,game.collection}.Where(t=>t).Distinct().ToArray();
  var delta=new Vector3(.5f,0,1.8f);
  foreach(var part in parts.Where(t=>!parts.Any(p=>p!=t&&t.IsChildOf(p)))){
   Undo.RecordObject(part,"Clear Sweet counter and retain its basket/card targets");part.position+=delta;EditorUtility.SetDirty(part);
  }
  game.panelPosition=game.rest.position-Vector3.right*1.2f+Vector3.forward*.6f;game.panelPosition.y=game.worktopY+.5f;
  var marker=new GameObject(stamp);Undo.RegisterCreatedObjectUndo(marker,"Record counter clearance");marker.transform.SetParent(venue,false);
  EditorUtility.SetDirty(game);Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
 }
}
}
#endif
