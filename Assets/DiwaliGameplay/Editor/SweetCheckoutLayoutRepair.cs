#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

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
  var pivot=cabinetBounds.center;
  var turn=Quaternion.AngleAxis(Mathf.DeltaAngle(cabinet.eulerAngles.y,270),Vector3.up);
  Undo.RecordObject(cabinetGroup,"Face Sweet cabinet into the working area");
  cabinetGroup.SetPositionAndRotation(pivot+turn*(cabinetGroup.position-pivot),turn*cabinetGroup.rotation);
  foreach(var sweet in game.sweets)if(!sweet.transform.IsChildOf(cabinetGroup)) {
   Undo.RecordObject(sweet.transform,"Rotate sweet with cabinet");sweet.transform.SetPositionAndRotation(pivot+turn*(sweet.transform.position-pivot),turn*sweet.transform.rotation);
  }
  foreach(var glass in game.glass)if(!glass.IsChildOf(cabinetGroup)) {
   Undo.RecordObject(glass,"Rotate glass with cabinet");glass.SetPositionAndRotation(pivot+turn*(glass.position-pivot),turn*glass.rotation);
  }
  cabinetBounds=GameObjectUtility.BoundsOf(cabinet);
  var cabinetDelta=new Vector3(-31.65f,cabinetBounds.center.y,5.50f)-cabinetBounds.center;
  cabinetGroup.position+=cabinetDelta;
  foreach(var sweet in game.sweets)if(!sweet.transform.IsChildOf(cabinetGroup))sweet.transform.position+=cabinetDelta;
  foreach(var glass in game.glass)if(!glass.IsChildOf(cabinetGroup))glass.position+=cabinetDelta;
  game.closedGlass=new Vector3[game.glass.Length];for(int i=0;i<game.glass.Length;i++)game.closedGlass[i]=game.glass[i].localPosition;
  Undo.RecordObjects(new Object[]{counter,game,game.rest,game.checkout,game.cardReader,game.card.transform},"Align Sweet checkout");
  counter.rotation=Quaternion.Euler(0,37.69f,0);
  var bounds=Surface(counter,0);
  for(int i=1;i<counter.GetComponent<MeshFilter>().sharedMesh.subMeshCount;i++)bounds.Encapsulate(Surface(counter,i));
  counter.position+=new Vector3(-31.00f,bounds.center.y,2.90f)-bounds.center;
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
  game.card.transform.rotation=Quaternion.FromToRotation(Vector3.up,new Vector3(.68f,.45f,.58f).normalized);
  var cardBounds=GameObjectUtility.BoundsOf(game.card.transform);
  var cardCenter=payment.center+Vector3.right*.36f+Vector3.back*.16f;
  Physics.SyncTransforms();float cardSurface=payment.center.y;
  foreach(var hit in Physics.RaycastAll(cardCenter+Vector3.up*2,Vector3.down,3,~0,QueryTriggerInteraction.Ignore))
   if(hit.collider.transform==counter)cardSurface=Mathf.Max(cardSurface,hit.point.y);
  cardCenter.y=cardSurface+game.card.transform.position.y-cardBounds.min.y+.015f;
  game.card.transform.position=cardCenter;
  var body=game.card.GetComponent<Rigidbody>();body.position=cardCenter;body.rotation=game.card.transform.rotation;
  game.worktopY=inset.center.y;
  game.workspaceForward=Vector3.forward;
  game.cabinetForward=Vector3.right;
  game.panelPosition=new Vector3(-30.65f,1.57f,7.10f);
  var viewer=new Vector3(-30.0f,1.57f,4.45f);
  game.panelRotation=Quaternion.LookRotation(game.panelPosition-viewer,Vector3.up);
  bounds=Surface(counter,0);for(int i=1;i<counter.GetComponent<MeshFilter>().sharedMesh.subMeshCount;i++)bounds.Encapsulate(Surface(counter,i));
  var sign=game.basketZoneSign;
  if(!sign){sign=new GameObject("Basket and checkout zone",typeof(RectTransform),typeof(Canvas)).transform;Undo.RegisterCreatedObjectUndo(sign.gameObject,"Label Sweet checkout");sign.SetParent(counter,true);game.basketZoneSign=sign;
   var rect=(RectTransform)sign;rect.sizeDelta=new Vector2(360,90);
   sign.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
   var background=sign.gameObject.AddComponent<Image>();background.color=new Color(.14f,.025f,.06f);background.raycastTarget=false;
   var label=new GameObject("Basket checkout label",typeof(RectTransform),typeof(Text));label.transform.SetParent(sign,false);
   var lr=label.GetComponent<RectTransform>();lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=new Vector2(8,4);lr.offsetMax=new Vector2(-8,-4);
   var text=label.GetComponent<Text>();text.font=Resources.Load<Font>("Diwali/Inter");if(!text.font)text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=27;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(1,.93f,.72f);text.raycastTarget=false;text.text="BASKET / CHECKOUT\nLeave basket here";
  }
  sign.SetParent(null,true);sign.localScale=Vector3.one*.0015f;
  sign.SetPositionAndRotation(new Vector3(inset.center.x,game.worktopY-.12f,bounds.max.z+.012f),Quaternion.LookRotation(-game.workspaceForward));sign.SetParent(counter,true);
  SweetCheckoutDisplayInstaller.Apply();
  EditorUtility.SetDirty(game);
  EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
  Debug.Log("Sweet checkout aligned: counter="+GameObjectUtility.BoundsOf(counter)+", inset="+inset+", basket="+basketPose+", card pad="+game.cardReader.position+", menu="+game.panelPosition);
 }
}
}
#endif
