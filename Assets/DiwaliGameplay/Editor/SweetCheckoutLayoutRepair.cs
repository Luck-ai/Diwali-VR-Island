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
  var turn=Quaternion.AngleAxis(Mathf.DeltaAngle(cabinet.eulerAngles.y,180),Vector3.up);
  Undo.RecordObject(cabinetGroup,"Face Sweet cabinet into the working area");
  cabinetGroup.SetPositionAndRotation(pivot+turn*(cabinetGroup.position-pivot),turn*cabinetGroup.rotation);
  foreach(var sweet in game.sweets)if(!sweet.transform.IsChildOf(cabinetGroup)) {
   Undo.RecordObject(sweet.transform,"Rotate sweet with cabinet");sweet.transform.SetPositionAndRotation(pivot+turn*(sweet.transform.position-pivot),turn*sweet.transform.rotation);
  }
  foreach(var glass in game.glass)if(!glass.IsChildOf(cabinetGroup)) {
   Undo.RecordObject(glass,"Rotate glass with cabinet");glass.SetPositionAndRotation(pivot+turn*(glass.position-pivot),turn*glass.rotation);
  }
  cabinetBounds=GameObjectUtility.BoundsOf(cabinet);
  var cabinetDelta=new Vector3(-28.60f,cabinetBounds.center.y,-.55f)-cabinetBounds.center;
  cabinetGroup.position+=cabinetDelta;
  foreach(var sweet in game.sweets)if(!sweet.transform.IsChildOf(cabinetGroup))sweet.transform.position+=cabinetDelta;
  foreach(var glass in game.glass)if(!glass.IsChildOf(cabinetGroup))glass.position+=cabinetDelta;
  game.closedGlass=new Vector3[game.glass.Length];for(int i=0;i<game.glass.Length;i++)game.closedGlass[i]=game.glass[i].localPosition;
  Undo.RecordObjects(new Object[]{counter,game,game.rest,game.checkout,game.cardReader,game.card.transform},"Align Sweet checkout");
  counter.rotation=Quaternion.Euler(0,37.69f,0);
  var bounds=Surface(counter,0);
  for(int i=1;i<counter.GetComponent<MeshFilter>().sharedMesh.subMeshCount;i++)bounds.Encapsulate(Surface(counter,i));
  counter.position+=new Vector3(-31.72f,bounds.center.y,-.10f)-bounds.center;
  var inset=Surface(counter,2);
  var payment=Surface(counter,3);
  var basketBounds=GameObjectUtility.BoundsOf(game.basket.transform);
  var seatForward=new Vector3(.225f,0,.974f).normalized;
  var seatAcross=Vector3.Cross(Vector3.up,seatForward);
  var packingSeat=inset.center+seatAcross*.37f;
  var checkoutSeat=inset.center-seatAcross*.37f;
  float bottomOffset=game.basket.transform.position.y-basketBounds.min.y+.015f;
  var targets=new[]{game.rest,game.checkout};
  var seats=new[]{packingSeat,checkoutSeat};
  for(int i=0;i<targets.Length;i++) {
   var target=targets[i];
   Undo.SetTransformParent(target,counter,"Attach basket target to counter");
   target.localScale=new Vector3(1/counter.lossyScale.x,1/counter.lossyScale.y,1/counter.lossyScale.z);
   target.SetPositionAndRotation(seats[i]+Vector3.up*bottomOffset,Quaternion.LookRotation(seatForward));
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
  game.cabinetForward=Vector3.forward;
  game.panelPosition=new Vector3(-27.40f,1.65f,1.70f);
  var viewer=new Vector3(-30.30f,1.57f,1.70f);
  game.panelRotation=Quaternion.LookRotation(game.panelPosition-viewer,Vector3.up);
  bounds=Surface(counter,0);for(int i=1;i<counter.GetComponent<MeshFilter>().sharedMesh.subMeshCount;i++)bounds.Encapsulate(Surface(counter,i));
  game.basketZoneSign=ZoneSign(counter,game.basketZoneSign,"Checkout zone label",checkoutSeat,seatForward,"CHECKOUT\nPlace full basket",false);
  ZoneSign(counter,counter.Find("Packing zone label"),"Packing zone label",packingSeat,seatForward,"FILL BASKET\nChoose 4 sweets",false);
  ZoneSign(counter,counter.Find("Checkout counter pad"),"Checkout counter pad",checkoutSeat,seatForward,"CHECKOUT\n\n4 SWEETS → TAP CARD",true);
  ZoneSign(counter,counter.Find("Packing counter pad"),"Packing counter pad",packingSeat,seatForward,"FILL BASKET\n\nANY 4 SWEETS",true);
  SweetCheckoutDisplayInstaller.Apply();
  EditorUtility.SetDirty(game);
  EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
  Debug.Log("Sweet checkout aligned: counter="+GameObjectUtility.BoundsOf(counter)+", inset="+inset+", packing="+game.rest.position+", checkout="+game.checkout.position+", card pad="+game.cardReader.position+", menu="+game.panelPosition);
 }
 static Transform ZoneSign(Transform counter,Transform sign,string name,Vector3 seat,Vector3 front,string copy,bool flat) {
  if(!sign){sign=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(Image)).transform;Undo.RegisterCreatedObjectUndo(sign.gameObject,"Label Sweet counter");}
  sign.name=name;sign.SetParent(null,true);sign.localScale=Vector3.one*(flat?.0022f:.0015f);
  var rect=(RectTransform)sign;rect.sizeDelta=flat?new Vector2(320,320):new Vector2(360,90);
  var canvas=sign.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
  var image=sign.GetComponent<Image>();if(!image)image=sign.gameObject.AddComponent<Image>();
  bool checkout=name.StartsWith("Checkout");image.color=flat?(checkout?new Color(.025f,.17f,.13f,.85f):new Color(.25f,.12f,.045f,.65f)):new Color(.14f,.025f,.06f);
  image.raycastTarget=false;
  var outline=sign.GetComponent<Outline>();if(!outline)outline=sign.gameObject.AddComponent<Outline>();outline.effectColor=checkout?new Color(.42f,1,.72f):new Color(1,.78f,.35f);outline.effectDistance=new Vector2(3,-3);
  var text=sign.GetComponentInChildren<Text>();
  if(!text){var label=new GameObject("Label",typeof(RectTransform),typeof(Text));label.transform.SetParent(sign,false);text=label.GetComponent<Text>();}
  var lr=text.GetComponent<RectTransform>();lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=new Vector2(10,6);lr.offsetMax=new Vector2(-10,-6);
  text.font=Resources.Load<Font>("Diwali/Inter");if(!text.font)text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=flat?23:26;text.fontStyle=FontStyle.Bold;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(1,.93f,.72f);text.raycastTarget=false;text.text=copy;
  var point=flat?seat+Vector3.up*.009f:seat+front*.60f+Vector3.down*.12f;
  if(!flat){
   Physics.SyncTransforms();var origin=seat+Vector3.down*.12f+front*2;
   foreach(var hit in Physics.RaycastAll(origin,-front,3,~0,QueryTriggerInteraction.Ignore))
    if(hit.collider.transform==counter){point=hit.point+front*.012f;break;}
  }
  sign.SetPositionAndRotation(point,flat?Quaternion.LookRotation(Vector3.down,-front):Quaternion.LookRotation(-front));sign.SetParent(counter,true);return sign;
 }

}
}
#endif
