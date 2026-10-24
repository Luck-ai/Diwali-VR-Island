#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Attachment;

namespace DiwaliGameplay
{
 public static class GrabClearanceVerification
 {
  static bool running;
  [MenuItem("Diwali/Gameplay/Verify four-game grab clearance")]
  public static async void Run()
  {
   if(!EditorApplication.isPlaying||running)return;
   running=true;
   var rows=new List<object>();var errors=new List<string>();
   var go=new GameObject("QA standing grip clearance controller");NearFarInteractor interactor=null;
   int itemCount=0,selectedCount=0,trialCount=0,clearanceFlags=0,collisionFlags=0;
   try{
    var body=go.AddComponent<Rigidbody>();body.isKinematic=true;
    var trigger=go.AddComponent<SphereCollider>();trigger.isTrigger=true;trigger.radius=.05f;
    interactor=go.AddComponent<NearFarInteractor>();interactor.enableUIInteraction=false;
    interactor.selectInput.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;
    var attach=(InteractionAttachController)interactor.interactionAttachController;
    attach.transformToFollow=go.transform;attach.useMomentum=false;attach.useDistanceBasedVelocityScaling=false;attach.useManipulationInput=false;
    await Task.Delay(200);
    var caster=(CurveInteractionCaster)interactor.farInteractionCaster;caster.hitDetectionType=CurveInteractionCaster.HitDetectionType.Raycast;caster.castDistance=5;
    var items=new List<(string venue,GrabbableItem item)>();
    var rangoli=UnityEngine.Object.FindFirstObjectByType<RangoliGame>();if(rangoli)items.AddRange(rangoli.pieces.Select(i=>("Rangoli",i)));
    var routing=UnityEngine.Object.FindFirstObjectByType<RoutingGame>();if(routing)items.AddRange(routing.pieces.Select(i=>("Routing",i)));
    var diya=UnityEngine.Object.FindFirstObjectByType<DiyaGame>();if(diya)items.AddRange(new[]{diya.scroll,diya.pot,diya.key,diya.feather,diya.diya}.Select(i=>("Diya",i)));
    var fire=UnityEngine.Object.FindFirstObjectByType<FireworksGame>();if(fire)items.AddRange(new[]{fire.rocket,fire.fountain,fire.spinner,fire.plunger}.Select(i=>("Fireworks",i)));
    foreach(var entry in items.Where(e=>e.item&&e.item.grab).GroupBy(e=>e.item).Select(g=>g.First())){
     var item=entry.item;itemCount++;bool enabledBefore=item.grab.enabled;
     try{
      foreach(string mode in new[]{"near","far-2m","far-3m"}){
       if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play mode ended during clearance verification.");
       Release(interactor);await Task.Delay(80);item.ResetItem();item.grab.enabled=true;await Task.Delay(60);
       var bounds=VisibleBounds(item);var target=bounds.center;
       var camera=Camera.main;Vector3 front=camera?Vector3.ProjectOnPlane(camera.transform.position-target,Vector3.up):Vector3.back;
       if(front.sqrMagnitude<.01f)front=Vector3.back;front.Normalize();
       float floor=item.GetComponentInParent<VenueGame>()?item.GetComponentInParent<VenueGame>().transform.position.y:0;
       float handY=floor+1.25f;
       float reach=mode=="near"?Vector3.Dot(bounds.extents,new Vector3(Mathf.Abs(front.x),0,Mathf.Abs(front.z)))+.07f:mode=="far-2m"?2:3;
       Vector3 origin=target+front*reach;origin.y=mode=="near"?Mathf.Clamp(target.y,floor+.85f,floor+1.55f):handY;
       Vector3 head=origin+front*.30f+Vector3.up*.30f;
       go.transform.SetPositionAndRotation(origin,Quaternion.LookRotation(target-origin,Vector3.up));
       interactor.enableNearCasting=mode=="near";interactor.enableFarCasting=mode!="near";
       var pairs=CollisionPairs(item);
       await Task.Delay(100);var valid=new List<IXRInteractable>();interactor.GetValidTargets(valid);bool found=valid.Contains(item.grab);
       interactor.selectInput.QueueManualState(true,1);await Task.Delay(260);
       bool selected=interactor.interactablesSelected.Contains(item.grab);trialCount++;if(selected)selectedCount++;
       float controllerGap=float.PositiveInfinity,headGap=float.PositiveInfinity,colliderGap=float.PositiveInfinity;
       var rotation=go.transform.rotation;
       if(selected)foreach(float angle in new[]{0f,45f,-45f}){
        go.transform.rotation=rotation*Quaternion.Euler(0,angle,0);await Task.Delay(130);
        controllerGap=Mathf.Min(controllerGap,RendererGap(item,go.transform.position));
        headGap=Mathf.Min(headGap,RendererGap(item,head));
        foreach(var collider in item.GetComponentsInChildren<Collider>())if(collider.enabled&&!collider.isTrigger)colliderGap=Mathf.Min(colliderGap,Vector3.Distance(go.transform.position,collider.ClosestPoint(go.transform.position)));
       }
       Release(interactor);await Task.Delay(120);bool released=!interactor.interactablesSelected.Contains(item.grab);
       int changedPairs=pairs.Count(p=>p.a&&p.b&&Physics.GetIgnoreCollision(p.a,p.b)!=p.ignored);
       bool clearanceFlag=selected&&item.grab.trackPosition&&(controllerGap<.035f||headGap<.12f);
       if(clearanceFlag)clearanceFlags++;if(changedPairs>0)collisionFlags++;
       rows.Add(new{venue=entry.venue,id=item.itemId,name=item.name,mode,found,selected,released,trackPosition=item.grab.trackPosition,controllerOrigin=V(origin),simulatedStandingHead=V(head),visibleBoundsSize=V(bounds.size),controllerRenderedBoundsGap=Finite(controllerGap),simulatedHeadRenderedBoundsGap=Finite(headGap),controllerColliderGap=Finite(colliderGap),clearanceFlag,collisionPairsChecked=pairs.Count,collisionPairsChangedAfterRelease=changedPairs});
      }
     }finally{Release(interactor);item.ResetItem();item.grab.enabled=enabledBefore;}
    }
   }catch(Exception e){errors.Add(e.ToString());Debug.LogException(e);}
   finally{
    if(interactor)Release(interactor);if(go)UnityEngine.Object.Destroy(go);running=false;
    var report=new{items=itemCount,trials=trialCount,selected=selectedCount,clearanceFlags,collisionFlags,notes="Queued NearFar input. Synthetic standing hand/head poses; AABB renderer gaps conservatively flag candidates, not exact mesh penetration. Fixed plunger controller contact is intentional and excluded from clearance flags. Near failures may reflect inaccessible standing reach or occlusion. Test temporarily enables gated grabs; gameplay state is not advanced. Original grab enabled flags are restored; item poses reset.",errors,results=rows};
    string output=Path.GetFullPath("../Game_Plans/Grab_Clearance_2026-10-24.json");Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonConvert.SerializeObject(report,Formatting.Indented));
    Debug.Log($"Four-game clearance: {itemCount} items, {trialCount} trials, {selectedCount} selected, {clearanceFlags} clearance candidates, {collisionFlags} collision-state failures. {output}");
   }
  }
  static List<(Collider a,Collider b,bool ignored)> CollisionPairs(GrabbableItem item){
   var bodies=new List<Collider>();var rig=UnityEngine.Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();if(rig)bodies.AddRange(rig.GetComponentsInChildren<Collider>());var cc=UnityEngine.Object.FindFirstObjectByType<CharacterController>();if(cc&&!bodies.Contains(cc))bodies.Add(cc);
   var result=new List<(Collider,Collider,bool)>();foreach(var a in item.GetComponentsInChildren<Collider>())foreach(var b in bodies)if(a!=b&&!b.transform.IsChildOf(item.transform))result.Add((a,b,Physics.GetIgnoreCollision(a,b)));return result;
  }
  static Bounds VisibleBounds(GrabbableItem item){var rs=item.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();if(rs.Length==0)return new Bounds(item.transform.position,Vector3.zero);var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
  static float RendererGap(GrabbableItem item,Vector3 p){var rs=item.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();return rs.Length==0?float.PositiveInfinity:rs.Min(r=>Vector3.Distance(p,r.bounds.ClosestPoint(p)));}
  static float? Finite(float value)=>float.IsInfinity(value)||float.IsNaN(value)?(float?)null:value;
  static float[] V(Vector3 p)=>new[]{p.x,p.y,p.z};
  static void Release(NearFarInteractor n){if(n)n.selectInput.QueueManualState(false,0);}
 }
}
#endif
