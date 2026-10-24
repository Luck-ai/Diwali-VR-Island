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
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public static class ChristmasInteractionVerification
{
 static bool running;static NearFarInteractor hand;static readonly List<object> checks=new List<object>();static int failures;
 static string Output=>Path.GetFullPath("../Game_Plans/Christmas_Interaction_Verification_2026-10-24.json");
 static void Check(string name,bool pass,string detail=""){checks.Add(new{name,pass,detail});if(!pass)failures++;Save("running");}
 static void Save(string status){Directory.CreateDirectory(Path.GetDirectoryName(Output));File.WriteAllText(Output,JsonConvert.SerializeObject(new{status,checks=checks.ToArray(),failures,limitations="Queued synthetic NearFar controller input; no physical headset/user reach verification. Snowball target hit uses a staged physics launch after a real queued grab/release. Existing island has ten event types across eight places; no five-place 10-action progression exists.",utc=DateTime.UtcNow.ToString("o")},Formatting.Indented));}
 [MenuItem("Tools/Christmas Island/Verify actual interactions in Play mode")]
 public static async void Run(){
  if(!EditorApplication.isPlaying||running)return;running=true;checks.Clear();failures=0;
  var root=GameObject.Find("Christmas Island Integration");var go=new GameObject("QA Christmas queued controller");var originalFeet=PlayerRig.Feet;
  try{
   if(!root)throw new InvalidOperationException("Christmas Island Integration not active.");
   var rb=go.AddComponent<Rigidbody>();rb.isKinematic=true;var sphere=go.AddComponent<SphereCollider>();sphere.isTrigger=true;sphere.radius=.04f;
   hand=go.AddComponent<NearFarInteractor>();hand.enableNearCasting=false;hand.enableUIInteraction=false;
   hand.selectInput.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;hand.activateInput.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;
   var attach=(InteractionAttachController)hand.interactionAttachController;attach.transformToFollow=go.transform;attach.useMomentum=false;attach.useDistanceBasedVelocityScaling=false;attach.useManipulationInput=false;
   await Wait(200);var caster=(CurveInteractionCaster)hand.farInteractionCaster;caster.hitDetectionType=CurveInteractionCaster.HitDetectionType.Raycast;caster.castDistance=5;
   var items=root.GetComponentsInChildren<InteractableItem>(true).OrderBy(i=>i.eventNumber).ToArray();
   Check("Exactly ten supplied event types",items.Length==10&&items.Select(i=>i.eventNumber).Distinct().Count()==10,string.Join("; ",items.Select(i=>$"{i.eventNumber}:{i.name}")));
   Check("No missing scripts",root.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==0);
   foreach(var item in items){
    var action=item.GetComponent<ChristmasItemAction>();var xr=item.GetComponent<XRBaseInteractable>();
    Check(item.name+" wiring",action&&xr&&item.GetComponent<XRItemHook>()&&xr.colliders.Any(c=>c&&c.enabled));if(!action||!xr)continue;
    int before=action.completedCount;var local=item.transform.localPosition;var partRotation=action.motionPart?action.motionPart.localRotation:Quaternion.identity;var partPosition=action.motionPart?action.motionPart.localPosition:Vector3.zero;
    if(!await Select(xr,item.name))continue;
    switch(item.eventNumber){
     case 1:await Wait(850);Check("Gift lid and surprise animate",action.active&&action.completedCount==before+1&&action.motionPart&&Quaternion.Angle(partRotation,action.motionPart.localRotation)>50);await Release();await Select(xr,"gift close");await Wait(650);Check("Gift closes",!action.active&&Quaternion.Angle(partRotation,action.motionPart.localRotation)<15);break;
     case 2:await Wait(900);Check("Train moves around track",action.active&&action.completedCount==before+1&&Vector3.Distance(local,item.transform.localPosition)>.05f);await Release();await Select(xr,"train stop");await Wait(100);var stopped=item.transform.position;await Wait(400);Check("Train stops",!action.active&&Vector3.Distance(stopped,item.transform.position)<.01f);break;
     case 3:MoveItem(item.transform,action.target.position);await Wait(200);await Trigger();await Wait(700);Check("Fishing trigger reels visible fish",action.completedCount==before+1&&action.motionPart&&action.motionPart.gameObject.activeInHierarchy&&Vector3.Distance(partPosition,action.motionPart.localPosition)>.4f);break;
     case 4:var ferris=item.GetComponentInParent<FerrisWheelSpinner>();var wr=ferris.wheel.rotation;await Wait(1400);Check("Ferris lever starts actual wheel",ferris.spinning&&action.completedCount==before+1&&Quaternion.Angle(wr,ferris.wheel.rotation)>.3f);Check("Ferris cabins stay upright",ferris.wheel.Cast<Transform>().Where(t=>t.name.Contains("_Cabin_")).All(t=>Vector3.Dot(t.up,Vector3.up)>.95f));await Release();await Select(xr,"Ferris stop");Check("Ferris lever stops command",!ferris.spinning);break;
     case 5:await Wait(850);Check("Oven door and tray animate",action.active&&action.completedCount==before+1&&action.motionPart&&Quaternion.Angle(partRotation,action.motionPart.localRotation)>45&&action.secondaryPart);await Release();await Select(xr,"oven close");await Wait(650);Check("Oven closes",!action.active&&Quaternion.Angle(partRotation,action.motionPart.localRotation)<15);break;
     case 6:await Wait(500);var r1=item.transform.rotation;await Wait(450);Check("Primary swing rocks",action.active&&action.completedCount==before+1&&Quaternion.Angle(r1,item.transform.rotation)>.3f);await Release();await Select(xr,"primary swing stop");await Wait(900);Check("Primary swing returns home and remains anchored",!action.active&&Vector3.Distance(local,item.transform.localPosition)<.02f);break;
     case 7:await Snowball(item,action,before,local);break;
     case 8:MoveItem(item.transform,Bounds(action.target).center);await Wait(300);Check("Carrot feeds reindeer while held",action.active&&action.completedCount==before+1&&item.GetComponentsInChildren<MeshRenderer>().All(r=>!r.enabled));await Release();await Wait(3300);Check("Carrot recalls visible",!action.active&&item.GetComponentsInChildren<MeshRenderer>().Any(r=>r.enabled));break;
     case 9:var carousel=item.GetComponentInParent<CarouselSpinner>();var cr=carousel.rotor.rotation;await Wait(1400);Check("Carousel button starts actual rotor",carousel.spinning&&action.completedCount==before+1&&Quaternion.Angle(cr,carousel.rotor.rotation)>1);await Release();await Select(xr,"carousel stop");Check("Carousel button stops command",!carousel.spinning);break;
     case 10:await Trigger();await Wait(300);Check("Snowglobe trigger emits particles",action.completedCount==before+1&&action.snow&&action.snow.particleCount>0);break;
    }
    await Release();if(item.GetComponent<XRGrabInteractable>())action.Recall();
   }
   var secondSwing=root.GetComponentsInChildren<ChristmasSwing>(true).SingleOrDefault(s=>s.name=="B5_SnowPark_Prop_SwingSeat2");
   Check("Second park swing has fixed selectable wiring",secondSwing&&secondSwing.GetComponent<XRSimpleInteractable>()&&!secondSwing.GetComponent<XRGrabInteractable>()&&!secondSwing.GetComponent<Rigidbody>());
   if(secondSwing){var rest=secondSwing.transform.localPosition;var beforeSwing=secondSwing.completedCount;
    if(await Select(secondSwing.GetComponent<XRSimpleInteractable>(),"second swing start")){await Wait(500);var angle=secondSwing.transform.localRotation;await Wait(450);Check("Second swing rocks without falling",secondSwing.active&&secondSwing.completedCount==beforeSwing+1&&Quaternion.Angle(angle,secondSwing.transform.localRotation)>.3f&&Vector3.Distance(rest,secondSwing.transform.localPosition)<.02f);await Release();await Select(secondSwing.GetComponent<XRSimpleInteractable>(),"second swing stop");await Wait(900);Check("Second swing returns home and remains anchored",!secondSwing.active&&Vector3.Distance(rest,secondSwing.transform.localPosition)<.02f);await Release();}}
   foreach(var door in root.GetComponentsInChildren<ChristmasDoor>()){
    var closed=door.transform.localRotation;var xr=door.GetComponent<XRSimpleInteractable>();if(await Select(xr,"door "+door.name)){await Wait(700);Check(door.name+" opens",Quaternion.Angle(closed,door.transform.localRotation)>50);await Release();await Select(xr,"door close "+door.name);await Wait(800);Check(door.name+" closes",Quaternion.Angle(closed,door.transform.localRotation)<10);await Release();}
   }
   var seat=root.GetComponentInChildren<RideSeat>();if(seat&&await Select(seat.GetComponent<XRSimpleInteractable>(),"Ferris cabin board")){
    await Wait(150);Check("Cabin boarding moves actual rig",RideSeat.current==seat&&Vector3.Distance(PlayerRig.Feet,seat.transform.TransformPoint(seat.seatOffset))<.2f);
    var cabinCollider=seat.GetComponent<Collider>();bool cabinSupport=cabinCollider&&cabinCollider.Raycast(new Ray(PlayerRig.Feet+Vector3.up*.35f,Vector3.down),out var cabinHit,1f);Check("Cabin feet have nearby solid support",cabinSupport);
    var ferris=seat.GetComponentInParent<FerrisWheelSpinner>();ferris.SetSpinning(true);var feet=PlayerRig.Feet;await Wait(1500);Check("Seated rig follows moving cabin",RideSeat.current==seat&&Vector3.Distance(feet,PlayerRig.Feet)>.02f);ferris.SetSpinning(false);
    await Release();await Select(seat.GetComponent<XRSimpleInteractable>(),"Ferris cabin exit");await Wait(150);var exitFeet=PlayerRig.Feet;var exitMarker=seat.exitPoint?seat.exitPoint.position:Vector3.zero;var exitXZ=Vector2.Distance(new Vector2(exitFeet.x,exitFeet.z),new Vector2(exitMarker.x,exitMarker.z));Check("Cabin exit returns rig to platform",!RideSeat.current&&seat.exitPoint&&exitXZ<.1f&&Mathf.Abs((exitMarker.y-exitFeet.y)-.3f)<.08f,$"current={(RideSeat.current ? RideSeat.current.name : "none")}, feet={exitFeet}, exit={exitMarker}, horizontalGap={exitXZ:F3}, markerAboveFeet={(exitMarker.y-exitFeet.y):F3}");await Release();
   }
   var areas=root.GetComponentsInChildren<TeleportationArea>(true);Check("Teleport areas have enabled usable colliders and provider",areas.Length>0&&areas.All(a=>a.teleportationProvider&&a.colliders.Any(c=>c&&c.enabled&&c.gameObject.activeInHierarchy)),"areas="+areas.Length);
   foreach(var ride in root.GetComponentsInChildren<RidePlatform>()){
    var deck=ride.GetComponents<Collider>().FirstOrDefault(c=>c.enabled&&!c.isTrigger);Check("Ride deck has solid collider",deck);
    var carousel=ride.GetComponentInParent<CarouselSpinner>();
    if(deck&&carousel){
     bool hit=deck.Raycast(new Ray(ride.transform.position+Vector3.right+Vector3.up*8,Vector3.down),out var support,15);
     Check("Carousel deck has usable standing surface",hit);
     if(hit){RideSeat.GetOut();PlayerRig.MoveFeetTo(support.point+Vector3.up*.02f);carousel.SetSpinning(true);await Wait(300);var oldFeet=PlayerRig.Feet;await Wait(1200);Check("Standing rig carried by carousel deck",Vector3.Distance(oldFeet,PlayerRig.Feet)>.015f);carousel.SetSpinning(false);PlayerRig.MoveFeetTo(originalFeet);}
    }
   }
   var decorative=root.GetComponentsInChildren<ChristmasPropReset>().Where(p=>!p.GetComponent<ChristmasItemAction>()).GroupBy(p=>p.name.Split('_')[0]).Select(g=>g.First()).ToArray();
   foreach(var prop in decorative){var xr=prop.GetComponent<XRGrabInteractable>();if(await Select(xr,"Loose prop "+prop.name)){var start=prop.transform.position;hand.transform.position+=Vector3.up*.12f;await Wait(220);Check(prop.name+" follows held controller",Vector3.Distance(start,prop.transform.position)>.04f);await Release();Check(prop.name+" releases into physics",!xr.isSelected&&!prop.GetComponent<Rigidbody>().isKinematic);}}

   Check("All external decorative props have grab/reset wiring",root.GetComponentsInChildren<ChristmasPropReset>(true).All(p=>p.GetComponent<XRGrabInteractable>()&&p.GetComponent<Rigidbody>()));
   Save("completed");Debug.Log($"Christmas actual interaction QA completed: {checks.Count} checks; {failures} failed. {Output}");
  }catch(Exception e){Check("Unhandled verification error",false,e.ToString());Save("failed");Debug.LogException(e);}
  finally{if(hand){hand.selectInput.QueueManualState(false,0);hand.activateInput.QueueManualState(false,0);}RideSeat.GetOut();PlayerRig.MoveFeetTo(originalFeet);UnityEngine.Object.Destroy(go);hand=null;running=false;}
 }
 static async Task Snowball(InteractableItem item,ChristmasItemAction action,int before,Vector3 home){
  var body=item.GetComponent<Rigidbody>();await Release();Check("Snowball release enables physics",body&&!body.isKinematic);if(!body||!action.target)return;
  var bounds=Bounds(action.target);Vector3 axis=Vector3.back;float radius=Bounds(item.transform).extents.magnitude;
  body.position=bounds.center+axis*(bounds.extents.z+radius+.25f);body.linearVelocity=-axis*3;body.angularVelocity=Vector3.zero;
  await Wait(1100);Check("Released snowball physically hits snowman",action.completedCount>before,"collisions="+action.collisionCount+",last="+action.lastCollision);await Wait(1400);Check("Snowball recalls after hit",action.completedCount>before&&Vector3.Distance(item.transform.localPosition,home)<.35f&&!action.active);
 }
 static Bounds Bounds(Transform t){var rs=t.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();if(rs.Length==0)return new Bounds(t.position,Vector3.one*.1f);var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
 static async Task<bool> Select(XRBaseInteractable target,string label){
  await Release();if(!target){Check(label+" XR exists",false);return false;}
  bool found=false;foreach(var collider in target.colliders.Where(c=>c&&c.enabled&&c.gameObject.activeInHierarchy)){
   foreach(var direction in new[]{Vector3.forward,Vector3.back,Vector3.right,Vector3.left,Vector3.up,(Vector3.up+Vector3.back).normalized}){
    hand.transform.SetPositionAndRotation(collider.bounds.center+direction*.85f,Quaternion.LookRotation(-direction,Vector3.up));await Wait(70);var valid=new List<IXRInteractable>();hand.GetValidTargets(valid);if(valid.Contains(target)){found=true;break;}
   }if(found)break;
  }
  Check(label+" actual ray target",found);if(!found)return false;hand.selectInput.QueueManualState(true,1);await Wait(180);bool selected=hand.interactablesSelected.Contains(target);Check(label+" queued grip selects",selected);return selected;
 }
 static void MoveItem(Transform item,Vector3 destination){hand.transform.position+=destination-item.position;}
 static async Task Trigger(){hand.activateInput.QueueManualState(true,1);await Wait(130);hand.activateInput.QueueManualState(false,0);await Wait(80);}
 static async Task Release(){if(hand){hand.activateInput.QueueManualState(false,0);hand.selectInput.QueueManualState(false,0);}await Wait(100);}
 static async Task Wait(int ms){if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play mode ended during Christmas verification.");await Task.Delay(ms);}
}
#endif
