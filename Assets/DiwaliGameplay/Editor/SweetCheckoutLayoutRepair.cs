#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace DiwaliGameplay {
public static class SweetCheckoutLayoutRepair {
 public static Bounds Surface(Transform counter,int submesh) {
  var mesh=counter.GetComponent<MeshFilter>().sharedMesh;
  var bounds=new Bounds();bool first=true;
  foreach(var i in mesh.GetTriangles(submesh)) {
   var point=counter.TransformPoint(mesh.vertices[i]);
   if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
  }
  return bounds;
 }

 [MenuItem("Diwali/Gameplay/Align Sweet counter and checkout")]
 public static void Apply() {
  if(Application.isPlaying)throw new System.InvalidOperationException("Stop Play Mode before aligning the shop.");
  var game=Object.FindFirstObjectByType<SweetGame>();
  var venue=GameObjectUtility.FindExact("SweetShop - Approved Warm Palette");
  var counter=GameObjectUtility.FindUnder(venue,"Cube.012");
  var cabinet=GameObjectUtility.FindUnder(venue,"Cube");
  var cabinetGroup=GameObjectUtility.FindUnder(venue,"Accessible cabinet group");
  var cabinetBounds=GameObjectUtility.BoundsOf(cabinet);
  var cabinetDelta=new Vector3(-28.65f,cabinetBounds.center.y,1.70f)-cabinetBounds.center;
  Undo.RecordObject(cabinetGroup,"Bring Sweet cabinet forward");
  cabinetGroup.position+=cabinetDelta;
  foreach(var sweet in game.sweets)if(!sweet.transform.IsChildOf(cabinetGroup)) {
   Undo.RecordObject(sweet.transform,"Move sweet with cabinet");sweet.transform.position+=cabinetDelta;
  }
  foreach(var glass in game.glass)if(!glass.IsChildOf(cabinetGroup)) {
   Undo.RecordObject(glass,"Move glass with cabinet");glass.position+=cabinetDelta;
  }
  Undo.RecordObjects(new Object[]{counter,game,game.rest,game.checkout,game.cardReader,game.card.transform},"Align Sweet checkout");
  counter.rotation=Quaternion.Euler(0,37.69f,0);
  var bounds=Surface(counter,0);
  for(int i=1;i<counter.GetComponent<MeshFilter>().sharedMesh.subMeshCount;i++)bounds.Encapsulate(Surface(counter,i));
  counter.position+=new Vector3(-31.90f,bounds.center.y,1.20f)-bounds.center;
  var inset=Surface(counter,2);
  var payment=Surface(counter,3);
  var basketBounds=GameObjectUtility.BoundsOf(game.basket.transform);
  var seat=inset.center;
  var basketPose=seat+Vector3.up*(game.basket.transform.position.y-basketBounds.min.y+.015f);
  foreach(var target in new[]{game.rest,game.checkout}) {
   Undo.SetTransformParent(target,counter,"Attach basket target to counter");
   target.localScale=new Vector3(1/counter.lossyScale.x,1/counter.lossyScale.y,1/counter.lossyScale.z);
   target.SetPositionAndRotation(basketPose,Quaternion.identity);
  }
  Undo.SetTransformParent(game.cardReader,counter,"Attach card reader to counter");
  game.cardReader.SetPositionAndRotation(payment.center+Vector3.up*.015f,Quaternion.identity);
  Undo.SetTransformParent(game.card.transform,counter,"Attach card rest to counter");
  game.card.transform.rotation=Quaternion.identity;
  var cardBounds=GameObjectUtility.BoundsOf(game.card.transform);
  var cardCenter=payment.center+Vector3.right*.36f+Vector3.back*.16f;
  Physics.SyncTransforms();float cardSurface=payment.center.y;
  foreach(var hit in Physics.RaycastAll(cardCenter+Vector3.up*2,Vector3.down,3,~0,QueryTriggerInteraction.Ignore))
   if(hit.collider.transform==counter)cardSurface=Mathf.Max(cardSurface,hit.point.y);
  cardCenter.y=cardSurface+game.card.transform.position.y-cardBounds.min.y+.03f;
  game.card.transform.position=cardCenter;
  var body=game.card.GetComponent<Rigidbody>();body.position=cardCenter;body.rotation=game.card.transform.rotation;
  game.worktopY=inset.center.y;
  game.workspaceForward=Vector3.forward;
  game.panelPosition=new Vector3(-33.22f,1.57f,2.80f);
  var viewer=new Vector3(-31.15f,1.57f,2.85f);
  game.panelRotation=Quaternion.LookRotation(game.panelPosition-viewer,Vector3.up);
  SweetCheckoutDisplayInstaller.Apply();
  EditorUtility.SetDirty(game);
  EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
  Debug.Log("Sweet checkout aligned: counter="+GameObjectUtility.BoundsOf(counter)+", inset="+inset+", basket="+basketPose+", card pad="+game.cardReader.position+", menu="+game.panelPosition);
 }
}
}
#endif
