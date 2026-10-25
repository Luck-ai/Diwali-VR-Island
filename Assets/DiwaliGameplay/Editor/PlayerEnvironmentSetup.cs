using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace DiwaliGameplay {
public static class PlayerEnvironmentSetup {
 [MenuItem("Diwali/Gameplay/Use controller selection")]
 public static void UseControllerSelection(){
  if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Apply player settings outside Play mode.");
  foreach(var gaze in Object.FindObjectsByType<XRGazeInteractor>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
   Undo.RecordObject(gaze,"Require deliberate controller selection");
   gaze.hoverToSelect=false;
   gaze.enableUIInteraction=false;
   PrefabUtility.RecordPrefabInstancePropertyModifications(gaze);
   EditorUtility.SetDirty(gaze);
  }
 }
 public static int AddEnvironmentColliders(){
  int added=0;
  foreach(var rootName in new[]{"Diwali Festival Ground","Christmas Island Integration"}){
   var root=GameObjectUtility.FindExact(rootName);if(!root)continue;
   foreach(var mf in root.GetComponentsInChildren<MeshFilter>(true)){
    if(!mf.sharedMesh||mf.sharedMesh.name=="Earth-beyond-the-walls-surface"||!mf.GetComponent<MeshRenderer>()||mf.GetComponent<Collider>())continue;
    if(mf.GetComponentInParent<XRGrabInteractable>()||mf.GetComponentInParent<Rigidbody>()||mf.GetComponentInParent<Canvas>())continue;
    var name=mf.name.ToLowerInvariant();if(name.Contains("flame")||name.Contains("rangoli")||name.Contains("silk"))continue;
    var collider=Undo.AddComponent<MeshCollider>(mf.gameObject);collider.sharedMesh=mf.sharedMesh;collider.convex=false;collider.isTrigger=false;added++;
   }
  }
  foreach(var cc in Object.FindObjectsByType<CharacterController>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
   Undo.RecordObject(cc,"Enable player body collision");cc.radius=.22f;cc.skinWidth=.025f;cc.stepOffset=.25f;cc.slopeLimit=50;cc.minMoveDistance=.001f;cc.detectCollisions=true;cc.excludeLayers=0;EditorUtility.SetDirty(cc);
  }
  Physics.SyncTransforms();return added;
 }
}
}
