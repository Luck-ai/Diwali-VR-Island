#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using DiwaliGameplay;
namespace DiwaliGameplay.Editor {
public static class CoreVenueInstallers {
 static void FitBounds(Transform item,Vector3 center,Vector3 size){var b=GameObjectUtility.BoundsOf(item);item.localScale=Vector3.Scale(item.localScale,new Vector3(size.x/Mathf.Max(.001f,b.size.x),size.y/Mathf.Max(.001f,b.size.y),size.z/Mathf.Max(.001f,b.size.z)));item.position+=center-GameObjectUtility.BoundsOf(item).center;}
 static Transform CompactFrame(Transform venue,string name,Vector3 pivot,Transform[] parts){var frame=new GameObject(name).transform;frame.SetParent(venue,true);frame.SetPositionAndRotation(pivot,venue.rotation);foreach(var part in parts)part.SetParent(frame,true);frame.localScale*=.65f;return frame;}
 static void CompactRangoli(Transform venue){
  var board=GameObjectUtility.FindUnder(venue,"Cube.005");var table=GameObjectUtility.FindUnder(venue,"Cube.001");var rack=GameObjectUtility.FindUnder(venue,"Cube");
  var floor=GameObjectUtility.BoundsOf(GameObjectUtility.FindUnder(venue,"Plane"));var b=GameObjectUtility.BoundsOf(board);var r=GameObjectUtility.BoundsOf(rack);
  var side=Vector3.forward;float boardHalf=Vector3.Dot(new Vector3(Mathf.Abs(side.x),0,Mathf.Abs(side.z)),b.extents);float rackHalf=Vector3.Dot(new Vector3(Mathf.Abs(side.x),0,Mathf.Abs(side.z)),r.extents);
  var desired=b.center+side*(boardHalf+rackHalf+.3f);desired.y=r.center.y;var delta=desired-r.center;
  string[] loose={"Plane.001","Sphere.001","Cube.004","Cube.003","Star.002"};var pieces=loose.Select(n=>GameObjectUtility.FindUnder(venue,n)).ToArray();rack.position+=delta;foreach(var piece in pieces)piece.position+=delta;
  var all=new[]{board,table,rack}.Concat(pieces).ToArray();var pivot=new Vector3(b.center.x,floor.max.y,b.center.z);var frame=CompactFrame(venue,"Compact Rangoli work station",pivot,all);
  float lift=floor.max.y+.015f-GameObjectUtility.BoundsOf(rack).min.y;rack.position+=Vector3.up*lift;foreach(var piece in pieces)piece.position+=Vector3.up*lift;
 }
 static Vector3[] CompactRouting(Transform venue){
  var console=GameObjectUtility.FindUnder(venue,"base");var rack=GameObjectUtility.FindUnder(venue,"rack");var b=GameObjectUtility.BoundsOf(console);var r=GameObjectUtility.BoundsOf(rack);var floor=GameObjectUtility.BoundsOf(GameObjectUtility.FindUnder(venue,"Plane"));
  Vector3[] rawRims={new Vector3(-1.34469296f,.76059295f,-1.56221589f),new Vector3(-.82829433f,.76059295f,-1.47311015f),new Vector3(-.31189605f,.76059295f,-1.38400459f)};var rimWorld=rawRims.Select(venue.TransformPoint).ToArray();
  var n=venue.TransformDirection(new Vector3(-.137803f,.585851f,.798617f)).normalized;var side=Vector3.Cross(Vector3.up,n).normalized;float cHalf=Vector3.Dot(new Vector3(Mathf.Abs(side.x),0,Mathf.Abs(side.z)),b.extents);float rHalf=Vector3.Dot(new Vector3(Mathf.Abs(side.x),0,Mathf.Abs(side.z)),r.extents);var desired=b.center-side*(cHalf+rHalf+.35f);desired.y=r.center.y;var delta=desired-r.center;
  var pieces=new[]{"object1","object2","object3"}.Select(x=>GameObjectUtility.FindUnder(venue,x)).ToArray();rack.position+=delta;foreach(var p in pieces)p.position+=delta;
  var crystal=GameObjectUtility.FindUnder(venue,"crystal");var holder=GameObjectUtility.FindUnder(venue,"crystal holder");var all=new[]{console,rack,crystal,holder}.Concat(pieces).ToArray();var pivot=new Vector3(b.center.x,floor.max.y,b.center.z);
  var frame=new GameObject("Compact routing work station").transform;frame.SetParent(venue,true);frame.SetPositionAndRotation(pivot,venue.rotation);var localRims=rimWorld.Select(frame.InverseTransformPoint).ToArray();foreach(var part in all)part.SetParent(frame,true);frame.localScale*=.65f;
  var compactB=GameObjectUtility.BoundsOf(console);var front=Vector3.ProjectOnPlane(n,Vector3.up).normalized;var target=compactB.center+side*(cHalf*.65f+.45f)+front*.2f;target.y=floor.max.y+.9f;var cd=target-GameObjectUtility.BoundsOf(crystal).center;crystal.position+=cd;holder.position+=cd;
  var hb=GameObjectUtility.BoundsOf(holder);float supportHeight=hb.min.y-floor.max.y;if(supportHeight>.05f){var source=GameObjectUtility.FindUnder(venue,"LightHolder1");var support=Object.Instantiate(source.gameObject,frame).transform;support.name="Crystal pedestal - reused light holder mesh";FitBounds(support,new Vector3(hb.center.x,floor.max.y+supportHeight*.5f,hb.center.z),new Vector3(hb.size.x*.85f,supportHeight,hb.size.z*.85f));}
  return localRims.Select(frame.TransformPoint).ToArray();
 }
 static Vector3 NativeRoutingPoint(Transform console,Vector3 point){var origin=new Vector3(-4.88145447f,-58.74601746f,1.27009177f);var d=(point-origin)*.171915575f;return console.TransformPoint(new Vector3(-d.x,d.z,-d.y));}
 static Quaternion ConnectorFrame(GrabbableItem piece,Vector3 meshRight,Vector3 meshUp,Vector3 normal){
  var mesh=piece.GetComponentInChildren<MeshFilter>();
  var right=piece.transform.InverseTransformDirection(meshRight).normalized;
  var up=Vector3.ProjectOnPlane(piece.transform.InverseTransformDirection(meshUp),right).normalized;
  var originalNormal=Vector3.Cross(right,up).normalized;
  var targetRight=Vector3.Cross(Vector3.up,normal).normalized;var targetUp=Vector3.Cross(normal,targetRight).normalized;
  return Quaternion.LookRotation(normal,targetUp)*Quaternion.Inverse(Quaternion.LookRotation(originalNormal,up));
 }
 public static void ConfigureRoutingGeometry(RoutingGame game){
  var venue=GameObjectUtility.FindExact("LanternRouting - Approved Warm Palette");var console=GameObjectUtility.FindUnder(venue,"base");
  game.socketPlaneNormal=new Vector3(-.1377436f,.58634084f,.79826758f).normalized;
  Vector3[] sourceRims={new Vector3(14.056559f,-55.944513f,6.502191f),new Vector3(11.052767f,-56.462824f,6.502191f),new Vector3(8.048977f,-56.981134f,6.502191f)};
  Vector3[] right={new Vector3(-.998281f,0,.058606f),new Vector3(-.97983f,-.03494f,.19667f),new Vector3(-.998281f,0,.058606f)};
  Vector3[] up={Vector3.up,new Vector3(-.0723654f,.9797983f,-.1864364f),Vector3.up};
  for(int i=0;i<3;i++){game.sockets[i+1].transform.position=new[]{new Vector3(27.832578f,1.153425f,32.327393f),new Vector3(28.605624f,1.151840f,32.461949f),new Vector3(29.388258f,1.154027f,32.595389f)}[i]+game.socketPlaneNormal*.025f;game.sockets[i+1].transform.rotation=Quaternion.AngleAxis(0,game.socketPlaneNormal)*ConnectorFrame(game.pieces[i+1],right[i],up[i],game.socketPlaneNormal);}
  game.targetMasks=new[]{12,14,10};
  var lower=NativeRoutingPoint(console,new Vector3(7.599f,-58.65f,3.31324f));var lowerNormal=console.TransformDirection(new Vector3(-.193365f,-.030210f,.980662f)).normalized;
  game.sockets[0].transform.position=lower+lowerNormal*.065f;game.sockets[0].transform.rotation=Quaternion.identity;
 }
 static void MakeFixed(Transform root,RangoliGame game){
  if(!root)return;
  foreach(var t in root.GetComponentsInChildren<Transform>(true)){
   if(game.pieces.Any(p=>p&&(t==p.transform||t.IsChildOf(p.transform))))continue;
   foreach(var interactable in t.GetComponents<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>())Object.DestroyImmediate(interactable);
   foreach(var item in t.GetComponents<GrabbableItem>())Object.DestroyImmediate(item);
   foreach(var rb in t.GetComponents<Rigidbody>())Object.DestroyImmediate(rb);
   foreach(var collider in t.GetComponents<Collider>()){collider.enabled=true;collider.isTrigger=false;}
   var mf=t.GetComponent<MeshFilter>();if(mf&&t.GetComponents<Collider>().Length==0){var c=t.gameObject.AddComponent<MeshCollider>();c.sharedMesh=mf.sharedMesh;}
   t.gameObject.isStatic=true;
  }
 }
 public static void ConfigureRangoliGeometry(RangoliGame game){
  if(game.preserveAuthoredLayout)return;
  var venue=GameObjectUtility.FindExact("Rangoli - Approved Warm Palette");
  var rack=GameObjectUtility.FindUnder(venue,"Cube");var riser=GameObjectUtility.FindUnder(venue,"Rack riser - reused table mesh");var board=GameObjectUtility.FindUnder(venue,"Cube.005");var table=GameObjectUtility.FindUnder(venue,"Cube.001");
  if(riser)Object.DestroyImmediate(riser.gameObject);
  MakeFixed(rack,game);MakeFixed(board,game);MakeFixed(table,game);
  foreach(var box in rack.GetComponentsInChildren<BoxCollider>())Object.DestroyImmediate(box);
  foreach(var filter in rack.GetComponentsInChildren<MeshFilter>()){var meshCollider=filter.GetComponent<MeshCollider>();if(!meshCollider)meshCollider=filter.gameObject.AddComponent<MeshCollider>();meshCollider.sharedMesh=filter.sharedMesh;meshCollider.convex=false;meshCollider.isTrigger=false;}
  var floorBounds=GameObjectUtility.BoundsOf(GameObjectUtility.FindUnder(venue,"Plane"));var pavilionBounds=GameObjectUtility.BoundsOf(GameObjectUtility.FindUnder(venue,"Cube.002"));var bb=GameObjectUtility.BoundsOf(board);
  float interiorY=floorBounds.max.y;if(game.pavilionSurface&&game.pavilionSurface.Raycast(new Ray(new Vector3(pavilionBounds.center.x,3,pavilionBounds.center.z),Vector3.down),out var interiorHit,5))interiorY=interiorHit.point.y;float tableLift=interiorY+.015f-GameObjectUtility.BoundsOf(table).min.y;var target=new Vector3(pavilionBounds.center.x+2.8f,bb.center.y+tableLift,pavilionBounds.center.z-2f);var stationDelta=target-bb.center;var frame=GameObjectUtility.FindUnder(venue,"Compact Rangoli work station");
  if(frame)frame.position+=stationDelta;else{board.position+=stationDelta;table.position+=stationDelta;rack.position+=stationDelta;}
  foreach(var piece in game.pieces)if(piece&&(!frame||!piece.transform.IsChildOf(frame)))piece.transform.position+=stationDelta;
  foreach(var socket in game.sockets)socket.transform.position+=stationDelta;
  var rb=GameObjectUtility.BoundsOf(rack);bb=GameObjectUtility.BoundsOf(board);var destination=bb.center+Vector3.forward*(bb.extents.z+rb.extents.z+.2f);float supportY=floorBounds.max.y;if(game.pavilionSurface&&game.pavilionSurface.Raycast(new Ray(new Vector3(destination.x,3,destination.z),Vector3.down),out var supportHit,5))supportY=supportHit.point.y;destination.y=supportY+.015f+rb.extents.y;var delta=destination-rb.center;
  rack.position+=delta;foreach(var piece in game.pieces)if(piece)piece.transform.position+=delta;
  string[] decorationNames={"Plane.002","Cube.006","Cube.007","Cube.008","Cube.009","Cube.010","Cube.011","Cube.012","Cube.013","Sphere.002","Sphere.019","Sphere.027","Sphere.037","Star.001","Star.003","Star.004","Star.005"};
  game.decorations=decorationNames.Select(name=>GameObjectUtility.FindUnder(venue,name)).ToArray();game.decorationGroups=new[]{0,3,3,3,3,2,2,2,2,1,1,1,1,4,4,4,4};game.motifs=game.decorations.SelectMany(part=>part.GetComponentsInChildren<Renderer>(true)).Distinct().ToArray();
  game.pavilionFloor=GameObjectUtility.FindUnder(venue,"Plane");var pavilion=GameObjectUtility.FindUnder(venue,"Cube.002");var collider=pavilion.GetComponent<MeshCollider>();if(!collider){collider=pavilion.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=pavilion.GetComponent<MeshFilter>().sharedMesh;}collider.enabled=true;game.pavilionSurface=collider;
  PavilionGrounding.Ground(game);
  if(game.sun&&game.sun.name!="Sun orientation pivot"){
   var art=game.sun;var pivot=new GameObject("Sun orientation pivot").transform;pivot.SetParent(art.parent,true);pivot.position=GameObjectUtility.BoundsOf(art).center;art.SetParent(pivot,true);game.sun=pivot;
  }
 }
 [UnityEditor.MenuItem("Diwali Island/Gameplay/Upgrade Rangoli and Routing geometry")]
 public static void UpgradeCurrentGeometry(){
  if(UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play Mode before upgrading the saved scene.");
  var rangoli=Object.FindFirstObjectByType<RangoliGame>();var routing=Object.FindFirstObjectByType<RoutingGame>();if(!rangoli||!routing)throw new System.InvalidOperationException("Installed Rangoli and Routing games are required.");
  ConfigureRangoliGeometry(rangoli);ConfigureRoutingGeometry(routing);UnityEditor.EditorUtility.SetDirty(rangoli);UnityEditor.EditorUtility.SetDirty(routing);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(rangoli.gameObject.scene);
 }
 public static GrabbableItem CenterGrab(Transform art,string id,Transform parent){var bounds=GameObjectUtility.BoundsOf(art);var g=new GameObject(id);g.transform.SetParent(parent,true);g.transform.position=bounds.center;art.SetParent(g.transform,true);return GameObjectUtility.MakeGrab(g.transform,id);}
 public static RangoliGame InstallRangoli(Transform root){var venue=GameObjectUtility.FindExact("Rangoli - Approved Warm Palette");CompactRangoli(venue);var g=new GameObject("Rangoli gameplay");g.transform.SetParent(root,false);var game=g.AddComponent<RangoliGame>();string[] names={"Plane.001","Sphere.001","Cube.004","Cube.003","Star.002"};string[] ids={"Sun","Petal","Diamond","Border","Star"};game.pieces=new GrabbableItem[5];game.sockets=new SnapZone[5];var boardTransform=GameObjectUtility.FindUnder(venue,"Cube.005");var board=GameObjectUtility.BoundsOf(boardTransform);Vector2[] centers={new Vector2(14.3759f,-23.2253f),new Vector2(12.0693f,-21.8043f),new Vector2(13.6369f,-20.8293f),new Vector2(12.828205f,-23.390084f),new Vector2(15.2852f,-22.0388f)};
 for(int i=0;i<5;i++){game.pieces[i]=CenterGrab(GameObjectUtility.FindUnder(venue,names[i]),ids[i],g.transform);// FBX vertex comparison confirms source X -> local -X and source Y -> local -Z.
 var local=new Vector3(-(centers[i].x-13.696125f)*.164084489f,(3.528929f-3.314536f)*.164084489f,-(centers[i].y+22.130573f)*.164084489f);var pos=boardTransform.TransformPoint(local)+boardTransform.up*.055f;game.sockets[i]=GameObjectUtility.MakeZone(g.transform,ids[i],pos,.38f);Vector3[] sourceNormals={new Vector3(.985992f,-.001721f,.166782f),Vector3.right,Vector3.right,new Vector3(.982517f,.108297f,.151435f),Vector3.right};var n=sourceNormals[i];var normal=boardTransform.TransformDirection(new Vector3(-n.x,n.z,-n.y));game.sockets[i].transform.rotation=Quaternion.FromToRotation(normal,boardTransform.up);if(i==3)game.sockets[i].transform.rotation=Quaternion.AngleAxis(61.03f,boardTransform.up)*game.sockets[i].transform.rotation;}
 string[] deco={"Plane.002","Cube.006","Cube.007","Cube.008","Cube.009","Cube.010","Cube.011","Cube.012","Cube.013","Sphere.002","Sphere.019","Sphere.027","Sphere.037","Star.001","Star.003","Star.004","Star.005"};game.decorations=deco.Select(n=>GameObjectUtility.FindUnder(venue,n)).ToArray();game.decorationGroups=new[]{0,3,3,3,3,2,2,2,2,1,1,1,1,4,4,4,4};game.sun=game.decorations[0];game.motifs=game.decorations.SelectMany(t=>t.GetComponentsInChildren<Renderer>()).ToArray();game.displayLight=GameObjectUtility.FindUnder(venue,"Rangoli warm courtyard fill").GetComponent<Light>();game.panelPosition=board.center+new Vector3(1.8f,.6f,0);game.panelRotation=Quaternion.Euler(0,90,0);ConfigureRangoliGeometry(game);return game;}
 static Vector3 SurfacePoint(Transform holder,int index){var b=GameObjectUtility.BoundsOf(holder);var p=b.center+Vector3.right*((index-1)*b.size.x*.28f);p.y=b.max.y+1;var meshCollider=holder.GetComponent<MeshCollider>();if(meshCollider&&meshCollider.Raycast(new Ray(p,Vector3.down),out var hit,b.size.y+3))return hit.point+Vector3.up*.13f;return new Vector3(p.x,b.max.y+.13f,p.z);}
 public static RoutingGame InstallRouting(Transform root){var venue=GameObjectUtility.FindExact("LanternRouting - Approved Warm Palette");var compactRims=CompactRouting(venue);var g=new GameObject("Lantern routing gameplay");g.transform.SetParent(root,false);var game=g.AddComponent<RoutingGame>();game.pieces=new GrabbableItem[4];game.sockets=new SnapZone[4];game.routePivots=new Transform[3];string[] names={"crystal","object2","object1","object3"};string[] ids={"Crystal","Elbow","T junction","Straight"};var baseB=GameObjectUtility.BoundsOf(GameObjectUtility.FindUnder(venue,"base"));Vector3[] routingRims={new Vector3(-1.34469296f,.76059295f,-1.56221589f),new Vector3(-.82829433f,.76059295f,-1.47311015f),new Vector3(-.31189605f,.76059295f,-1.38400459f)};Vector3[] pieceNormals={new Vector3(-.05860649f,0,-.99828116f),new Vector3(-.93698118f,.31819118f,-.14429355f),Vector3.up};game.socketPlaneNormal=venue.TransformDirection(new Vector3(-.137803f,.585851f,.798617f)).normalized;var crystal=GameObjectUtility.BoundsOf(GameObjectUtility.FindUnder(venue,"crystal holder"));for(int i=0;i<4;i++){game.pieces[i]=CenterGrab(GameObjectUtility.FindUnder(venue,names[i]),ids[i],g.transform);var pos=i==0?game.pieces[0].transform.position:compactRims[i-1]+game.socketPlaneNormal*.052f;game.sockets[i]=GameObjectUtility.MakeZone(g.transform,ids[i],pos,.38f);if(i>0){game.routePivots[i-1]=game.pieces[i].transform;game.sockets[i].transform.rotation=Quaternion.FromToRotation(venue.TransformDirection(pieceNormals[i-1]),game.socketPlaneNormal);}}
 game.destinations=new Light[3];string[] lamps={"lamp1lamp","lamp2","lamp3"};for(int i=0;i<3;i++){var l=new GameObject("Destination "+(i+1));l.transform.SetParent(g.transform,true);l.transform.position=GameObjectUtility.BoundsOf(GameObjectUtility.FindUnder(venue,lamps[i])).center;var light=l.AddComponent<Light>();light.color=new Color(1,.64f,.2f);light.range=4;light.intensity=.03f;game.destinations[i]=light;}game.panelPosition=baseB.center+new Vector3(-2,.6f,0);game.panelRotation=Quaternion.Euler(0,-90,0);ConfigureRoutingGeometry(game);return game;}
}
}
#endif
