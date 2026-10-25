#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

namespace DiwaliGameplay {
public static class SweetBasketComfortVerification {
 static readonly List<object> observations=new List<object>();
 static readonly Dictionary<Mesh,Vector3[]> meshVertices=new Dictionary<Mesh,Vector3[]>();
 static readonly Dictionary<string,string> captures=new Dictionary<string,string>();
 static string path;static int failures;static NearFarInteractor hand;
 static Vector3[] Vertices(Mesh mesh){
  if(meshVertices.TryGetValue(mesh,out var vertices))return vertices;
  using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh))
  using(var native=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp)){
   data[0].GetVertices(native);vertices=new Vector3[native.Length];native.CopyTo(vertices);
  }
  meshVertices[mesh]=vertices;return vertices;
 }
 static async Task Frames(int count=5){for(int i=0;i<count;i++)await Task.Delay(35);}
 static void Check(string name,bool passed,string detail=""){
  observations.Add(new{name,passed,detail});if(!passed){failures++;Debug.LogWarning("Sweet basket comfort: "+name+" "+detail);}
 }
 static Bounds MeshBounds(GrabbableItem item){
  var bounds=new Bounds();bool first=true;
  foreach(var filter in item.GetComponentsInChildren<MeshFilter>(true)){
   if(!filter.sharedMesh||filter.GetComponentInParent<GrabbableItem>()!=item)continue;
   foreach(var vertex in Vertices(filter.sharedMesh)){var world=filter.transform.TransformPoint(vertex);if(first){bounds=new Bounds(world,Vector3.zero);first=false;}else bounds.Encapsulate(world);}
  }
  return bounds;
 }
 static Bounds ColliderBounds(GrabbableItem item){
  var colliders=item.grab.colliders.Where(c=>c&&c.enabled&&c.gameObject.activeInHierarchy).ToArray();
  if(colliders.Length==0)return MeshBounds(item);
  var bounds=colliders[0].bounds;foreach(var collider in colliders.Skip(1))bounds.Encapsulate(collider.bounds);return bounds;
 }
 static async Task<bool> Select(GrabbableItem item,bool near){
  hand.selectInput.QueueManualState(false,0);await Frames();
  hand.enableFarCasting=!near;hand.enableNearCasting=near;
  var colliders=item.grab.colliders.Where(c=>c&&c.enabled&&c.gameObject.activeInHierarchy).ToArray();
  bool targeted=false;
  foreach(var collider in colliders){
   var bounds=collider.bounds;
   foreach(var direction in new[]{Vector3.forward,Vector3.back,Vector3.right,Vector3.left,Vector3.up}){
    var origin=near?collider.ClosestPoint(bounds.center+direction*bounds.extents.magnitude)+direction*.025f:bounds.center+direction*1.6f;
    hand.transform.SetPositionAndRotation(origin,Quaternion.LookRotation(bounds.center-origin,Vector3.up));
    await Frames(3);var valid=new List<UnityEngine.XR.Interaction.Toolkit.Interactables.IXRInteractable>();hand.GetValidTargets(valid);
    if(valid.Contains(item.grab)){targeted=true;break;}
   }
   if(targeted)break;
  }
  Check((near?"Near":"Far")+" cast targets "+item.name,targeted);
  if(!targeted)return false;
  hand.selectInput.QueueManualState(true,1);await Frames(6);
  Check((near?"Near":"Far")+" queued grip selects "+item.name,item.IsHeld);
  return item.IsHeld;
 }
 static async Task Release(){hand.selectInput.QueueManualState(false,0);await Frames(6);}
 static void Capture(string label,SweetGame game){
  var eye=new Vector3(-30.4f,1.65f,3.5f);
  if(label=="packed-top")eye=game.basket.transform.position+new Vector3(.45f,1.0f,.8f);
  try{captures[label]=ChristmasIslandQA.Render("SweetBasketComfort-"+label+"-"+DateTime.Now.ToString("yyyyMMdd_HHmmss"),eye,game.basket.transform.position+Vector3.up*.05f,60);}
  catch(Exception error){captures[label]="";Check("Capture "+label,false,error.Message);}
  if(string.IsNullOrEmpty(captures[label]))Check("Capture path "+label,false,"Renderer returned no image path");
 }
 static void CheckWorkspace(SweetGame game){
  var eye=new Vector3(-30.4f,1.65f,3.5f);
  var nearest=game.sweets.Where(s=>s).OrderBy(s=>Vector3.Distance(s.transform.position,eye)).FirstOrDefault();
  if(!nearest){Check("Workspace has sweet stock",false);return;}
  var targets=new[]{nearest.transform.position,game.rest.position,game.cardReader.position,game.checkout.position};
  var yaw=targets.Select(p=>{var d=p-eye;return Mathf.Repeat(Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,360f);}).OrderBy(v=>v).ToArray();
  float largest=0;for(int i=0;i<yaw.Length;i++)largest=Mathf.Max(largest,(i==yaw.Length-1?yaw[0]+360f:yaw[i+1])-yaw[i]);
  float span=360f-largest;float stockDistance=Vector3.Distance(eye,nearest.transform.position);
  Check("Customer workspace fits one forward arc",span<110f&&stockDistance<=3f,$"yawSpan={span:F1},closestStock={stockDistance:F2}m,stock={nearest.name},eye={eye}");
 }
 static int SweetSceneCount(SweetGame game){
  var types=new HashSet<string>(game.sweets.Where(s=>s).Select(s=>s.itemId));
  return UnityEngine.Object.FindObjectsByType<GrabbableItem>(FindObjectsSortMode.None).Count(s=>types.Contains(s.itemId));
 }
 static int SweetMeshCount(SweetGame game){
  var types=new HashSet<string>(game.sweets.Where(s=>s).Select(s=>s.itemId));
  return UnityEngine.Object.FindObjectsByType<GrabbableItem>(FindObjectsSortMode.None).Where(s=>types.Contains(s.itemId)).Sum(s=>s.GetComponentsInChildren<MeshFilter>(true).Count(m=>m.GetComponentInParent<GrabbableItem>()==s));
 }
 static void Save(string state){
  File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(new{state,time=DateTime.Now.ToString("s"),scope="Unity Play Mode synthetic NearFar controller; physical Quest comfort is unverified",failures,captures,observations},Newtonsoft.Json.Formatting.Indented));
 }
 [MenuItem("Diwali/Gameplay/Verify Sweet Basket Comfort")]
 public static async void Run(){
  if(!EditorApplication.isPlaying){Debug.LogError("Run Sweet Basket Comfort in Play Mode.");return;}
  if(hand){Debug.LogWarning("Sweet basket comfort verification already running.");return;}
  var game=UnityEngine.Object.FindFirstObjectByType<SweetGame>();var manager=UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>();var camera=Camera.main;
  if(!game||!manager||!camera){Debug.LogError("SweetGame, XRInteractionManager, or player camera missing.");return;}
  observations.Clear();captures.Clear();meshVertices.Clear();failures=0;
  path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Game_Plans/SweetBasketComfort_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".json"));
  var playerRoot=camera.transform.root;var playerPosition=playerRoot.position;var playerRotation=playerRoot.rotation;
  GameObject controller=null;
  try{
   controller=new GameObject("QA sweet basket controller");var body=controller.AddComponent<Rigidbody>();body.isKinematic=true;var touch=controller.AddComponent<SphereCollider>();touch.isTrigger=true;touch.radius=.05f;
   hand=controller.AddComponent<NearFarInteractor>();hand.interactionManager=manager;hand.selectInput.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;
   var attach=hand.interactionAttachController as UnityEngine.XR.Interaction.Toolkit.Attachment.InteractionAttachController;
   attach.transformToFollow=controller.transform;attach.useMomentum=false;attach.useDistanceBasedVelocityScaling=false;attach.useManipulationInput=false;
   if(hand.farInteractionCaster is CurveInteractionCaster caster){caster.hitDetectionType=CurveInteractionCaster.HitDetectionType.Raycast;caster.castDistance=8f;}
   await Frames();game.BeginGame();game.OpenCabinet();CheckWorkspace(game);
   playerRoot.position+=new Vector3(game.rest.position.x-camera.transform.position.x,0,game.rest.position.z-camera.transform.position.z)+game.workspaceForward*1.65f;
   await Frames();
   int basketIndex=0;
   foreach(var basket in game.baskets.Where(b=>b)){
    if(basketIndex++>0){game.SwitchOpenSide();await Frames(25);}
    var scale=basket.transform.localScale;
    foreach(var near in new[]{true,false}){
     if(!await Select(basket,near))continue;
     var relative=Vector3.ProjectOnPlane(camera.transform.position-game.rest.position,Vector3.up).normalized;
     if(relative.sqrMagnitude<.01f)relative=-game.workspaceForward.normalized;
     foreach(float height in new[]{.9f,1.2f,1.4f})foreach(float yaw in new[]{-60f,0f,60f})foreach(float pitch in new[]{-45f,45f}){
      var direction=Quaternion.Euler(pitch,yaw,0)*(-relative);
      hand.transform.SetPositionAndRotation(camera.transform.position-relative*.42f+Vector3.up*(height-camera.transform.position.y),Quaternion.LookRotation(direction,Vector3.up));
      await Frames(3);
      var mesh=MeshBounds(basket);var collider=ColliderBounds(basket);var controllerGap=Vector3.Distance(collider.ClosestPoint(hand.transform.position),hand.transform.position);
      float headGap=Vector3.Distance(mesh.ClosestPoint(camera.transform.position),camera.transform.position);
      float flatDistance=Vector3.ProjectOnPlane(mesh.center-camera.transform.position,Vector3.up).magnitude;
      float upright=Vector3.Angle(basket.transform.up,Vector3.up);
      Check($"{basket.name} {(near?"near":"far")} h{height:F1} yaw{yaw:F0} pitch{pitch:F0}",
       controllerGap>.06f&&headGap>.25f&&flatDistance>.68f&&upright<2f&&Vector3.Distance(scale,basket.transform.localScale)<.001f,
       $"controllerGap={controllerGap:F3},headGap={headGap:F3},horizontalCenter={flatDistance:F3},upright={upright:F2},scale={basket.transform.localScale}");
     }
     await Release();
     Check("Basket released "+basket.name,!basket.IsHeld);
     basket.ResetItem();
    }
   }
   game.ResetGame();game.BeginGame();game.OpenCabinet();game.basket.SnapTo(game.rest);game.basketReady=true;Capture("empty",game);
   var types=new[]{"Modak","Barfi","Gujiya","Gujiya"};
   foreach(var type in types){var sweet=game.sweets.FirstOrDefault(s=>s.itemId==type&&!game.selected.Contains(s));Check("Sweet available "+type,sweet);if(!sweet)continue;
    Check("Pack "+type,game.AddSweet(sweet));
   }
   Check("Four mixed sweets packed",game.selected.Count==4);
   foreach(var sweet in game.selected.ToArray()){
    var mesh=MeshBounds(sweet);var floorWorld=game.basket.transform.TransformPoint(new Vector3(0,game.BasketInteriorFloorY(game.basket),0)).y;
    var basketMesh=MeshBounds(game.basket);float rimWorld=basketMesh.min.y+basketMesh.size.y*.35f;
    Check("Selected sweet floats above basket for swapping "+sweet.name,mesh.min.y>=floorWorld+.24f&&mesh.min.y<=floorWorld+.4f,
     $"bottom={mesh.min.y:F3},top={mesh.max.y:F3},floor={floorWorld:F3},rim={rimWorld:F3}");
   }
   await Frames();Capture("packed",game);Capture("packed-top",game);
   foreach(var removable in game.selected.ToArray()){
    if(!await Select(removable,true))continue;
    Check("Gripping packed sweet removes "+removable.name,!game.selected.Contains(removable));
    var target=game.ShelfReturnTarget(removable);
    if(target){hand.transform.position=target.position;await Frames(8);}await Release();
    Check("Packed sweet returns to shelf "+removable.name,target&&Vector3.Distance(removable.transform.position,target.position)<.08f);
    Check("Returned sweet repacks "+removable.name,game.AddSweet(removable));
   }
   int sweetsBefore=SweetSceneCount(game),meshesBefore=SweetMeshCount(game);
   game.basket.SnapTo(game.checkout);game.docked=true;
   Check("Card authorizes four packed sweets",game.AuthorizeCard());game.ConfirmGift();
   Check("Confirm Gift completes",game.confirmed&&game.completed);
   Check("Confirm does not clone sweet items or meshes",SweetSceneCount(game)==sweetsBefore&&SweetMeshCount(game)==meshesBefore,
    $"items={sweetsBefore}->{SweetSceneCount(game)},meshes={meshesBefore}->{SweetMeshCount(game)}");
   await Frames();Capture("confirmed",game);
   await VerifyRocket(camera,playerRoot);
  }catch(Exception error){Check("Verifier exception",false,error.ToString());Debug.LogException(error);}
  finally{
   if(hand)await Release();game.ResetGame();playerRoot.SetPositionAndRotation(playerPosition,playerRotation);
   if(controller)UnityEngine.Object.Destroy(controller);hand=null;meshVertices.Clear();Save(failures==0?"passed":"failed");
   Debug.Log("Sweet basket comfort verification: "+failures+" failures; "+path);
  }
 }
 static async Task VerifyRocket(Camera camera,Transform rig){
  var fire=UnityEngine.Object.FindFirstObjectByType<FireworksGame>();if(!fire)return;
  fire.BeginGame();var item=fire.rocket;
  rig.position+=new Vector3(item.transform.position.x-camera.transform.position.x,0,item.transform.position.z-camera.transform.position.z)-Vector3.forward*2.2f;
  await Frames();
  foreach(bool near in new[]{true,false}){
   if(!await Select(item,near))continue;
   foreach(float yaw in new[]{-60f,0f,60f})foreach(float pitch in new[]{-45f,0f,45f}){
    var direction=Quaternion.Euler(pitch,yaw,0)*Vector3.forward;
    var position=camera.transform.position+Vector3.forward*.42f;position.y=1.2f;
    hand.transform.SetPositionAndRotation(position,Quaternion.LookRotation(direction,Vector3.up));await Frames(3);
    var bounds=MeshBounds(item);float gap=Vector3.Distance(bounds.ClosestPoint(hand.transform.position),hand.transform.position);
    float headGap=Vector3.Distance(bounds.ClosestPoint(camera.transform.position),camera.transform.position);
    float upright=Vector3.Angle(item.transform.TransformDirection(fire.rocketLocalLaunchAxis),Vector3.up);
    Check($"Rocket {(near?"near":"far")} yaw{yaw} pitch{pitch}",gap>.08f&&headGap>.25f&&upright<2,$"meshHandGap={gap:F3},headGap={headGap:F3},upright={upright:F2}");
   }
   captures[near?"rocket-near":"rocket-far"]=ChristmasIslandQA.Render("Rocket-clearance-"+(near?"near":"far"),camera.transform.position,MeshBounds(item).center,70);
   await Release();item.ResetItem();
  }
  fire.ResetGame();
 }
}
}
#endif
