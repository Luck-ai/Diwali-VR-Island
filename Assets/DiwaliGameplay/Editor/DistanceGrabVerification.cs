#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace DiwaliGameplay {
public static class DistanceGrabVerification {
 static string PathFor(Transform t){var p=new List<string>();for(;t;t=t.parent)p.Add(t.name);p.Reverse();return string.Join("/",p);}
 static Collider[] GrabColliders(GrabbableItem item)=>item&&item.grab?item.grab.colliders.Where(c=>c&&c.enabled&&c.gameObject.activeInHierarchy).ToArray():Array.Empty<Collider>();
 static Bounds GrabBounds(GrabbableItem item){var c=GrabColliders(item);if(c.Length==0)return GameObjectUtility.BoundsOf(item.transform);var b=c[0].bounds;foreach(var x in c.Skip(1))b.Encapsulate(x.bounds);return b;}
 [MenuItem("Diwali/Gameplay/Verify Distance Grab Input")]
 public static async void Run(){
 if(!EditorApplication.isPlaying)return;
 var results=new List<object>();var go=new GameObject("QA distance controller");NearFarInteractor n=null;
 try{
 var rb=go.AddComponent<Rigidbody>();rb.isKinematic=true;var trigger=go.AddComponent<SphereCollider>();trigger.isTrigger=true;trigger.radius=.05f;n=go.AddComponent<NearFarInteractor>();n.enableNearCasting=false;n.enableUIInteraction=false;n.selectInput.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;
 var attach=n.interactionAttachController as UnityEngine.XR.Interaction.Toolkit.Attachment.InteractionAttachController;attach.transformToFollow=go.transform;attach.useMomentum=false;attach.useDistanceBasedVelocityScaling=false;attach.useManipulationInput=false;await Task.Delay(250);var caster=(CurveInteractionCaster)n.farInteractionCaster;caster.hitDetectionType=CurveInteractionCaster.HitDetectionType.Raycast;caster.castDistance=8;
 var sweet=UnityEngine.Object.FindFirstObjectByType<SweetGame>();if(sweet){sweet.BeginGame();sweet.OpenCabinet();}
 var approaches=new[]{new{type="SweetGame",p=new Vector3(-29.5f,1.5f,4.2f)},new{type="RangoliGame",p=new Vector3(-20.5f,1.5f,33.5f)},new{type="RoutingGame",p=new Vector3(28f,1.5f,35.6f)},new{type="DiyaGame",p=new Vector3(31f,1.5f,7.7f)},new{type="FireworksGame",p=new Vector3(0f,1.5f,58.5f)}};
 foreach(var item in UnityEngine.Object.FindObjectsByType<GrabbableItem>(FindObjectsSortMode.None).Where(i=>i.grab&&i.grab.enabled&&i.grab.trackPosition).ToArray()){
  Release(n);var colliders=GrabColliders(item);var b=GrabBounds(item);var scale=item.transform.lossyScale;var approach=approaches.OrderBy(a=>Vector3.Distance(a.p,b.center)).First();var toward=approach.p-b.center;toward.y=0;if(toward.sqrMagnitude<.01f)toward=Vector3.forward;toward.Normalize();
  async Task<(bool found,bool selected,bool moved,bool released,float clearance,string firstHit,int firstHitId)> Trial(Vector3 origin,Vector3 facing){go.transform.SetPositionAndRotation(origin,Quaternion.LookRotation(facing,Vector3.up));await Task.Delay(100);var valid=new List<IXRInteractable>();n.GetValidTargets(valid);bool found=valid.Contains(item.grab);var hits=Physics.RaycastAll(origin,facing,8f,~0,QueryTriggerInteraction.Ignore).OrderBy(x=>x.distance).ToArray();var hitItem=hits.Length==0?null:hits[0].collider.GetComponentInParent<GrabbableItem>();var first=hits.Length==0?"none":hits[0].collider.name+"/"+hits[0].collider.GetType().Name+(hitItem?" itemId="+hitItem.itemId+" iid="+hitItem.GetInstanceID():"");var firstId=hitItem?hitItem.GetInstanceID():0;if(!found)return(false,false,false,false,0,first,firstId);n.selectInput.QueueManualState(true,1f);await Task.Delay(180);bool selected=n.interactablesSelected.Contains(item.grab);float clearance=selected?Vector3.Distance(origin,GrabBounds(item).ClosestPoint(origin)):0;bool moved=false;if(selected){var offset=item.transform.position-go.transform.position;var before=item.transform.position;go.transform.position+=Vector3.right*.15f;await Task.Delay(180);moved=Vector3.Distance(before,item.transform.position)>.05f;}n.selectInput.QueueManualState(false,0f);await Task.Delay(150);return(found,selected,moved,!n.interactablesSelected.Contains(item.grab),clearance,first,firstId);}
  var standing=await Trial(approach.p,b.center-approach.p);bool sideStepFound=false,sideStepSelected=false,sideStepMoved=false,sideStepReleased=false;float sideStepClearance=0;int sideStepFirstHitId=0;string sideStepFirstHit="";if(!standing.found||!standing.selected){var sideOrigin=new Vector3(b.center.x,1.5f,2f);var side=await Trial(sideOrigin,b.center-sideOrigin);sideStepFound=side.found;sideStepSelected=side.selected;sideStepMoved=side.moved;sideStepReleased=side.released;sideStepClearance=side.clearance;sideStepFirstHit=side.firstHit;sideStepFirstHitId=side.firstHitId;}bool fallbackFound=false,fallbackSelected=false,fallbackMoved=false,fallbackReleased=false;float fallbackClearance=0;string fallbackFirstHit="";
  if((!standing.found||!standing.selected)&&(!sideStepFound||!sideStepSelected)){Release(n);foreach(var col in colliders){var dirs=new[]{Vector3.forward,Vector3.back,Vector3.right,Vector3.left,Vector3.up,Vector3.up+Vector3.forward,Vector3.up+Vector3.back,Vector3.up+Vector3.right,Vector3.up+Vector3.left};foreach(var dir0 in dirs){var dir=dir0.normalized;var candidate=await Trial(col.bounds.center+dir*1.15f,-dir);if(candidate.found){fallbackFound=candidate.found;fallbackSelected=candidate.selected;fallbackMoved=candidate.moved;fallbackReleased=candidate.released;fallbackClearance=candidate.clearance;fallbackFirstHit=candidate.firstHit;break;}}if(fallbackFound)break;}}
  bool scaleStable=Vector3.Distance(scale,item.transform.lossyScale)<.001f;bool pass=standing.found&&standing.selected&&standing.moved&&standing.released&&standing.clearance>.03f&&standing.clearance<1.3f&&scaleStable;
  results.Add(new{item=item.itemId,objectName=item.name,transformPath=PathFor(item.transform),instanceId=item.GetInstanceID(),activeGrabColliders=colliders.Select(c=>new{name=c.name,type=c.GetType().Name,bounds=c.bounds.ToString("F2")}),approachType=approach.type,approachPosition=approach.p.ToString("F2"),standingFrontApproach=new{standing.found,standing.selected,standing.moved,standing.released,clearance=standing.clearance,firstHit=standing.firstHit,firstHitInstanceId=standing.firstHitId},standingSideStep=new{sideStepFound,sideStepSelected,sideStepMoved,sideStepReleased,clearance=sideStepClearance,firstHit=sideStepFirstHit,firstHitInstanceId=sideStepFirstHitId},nearbyDiagnosticFallback=new{fallbackFound,fallbackSelected,fallbackMoved,fallbackReleased,clearance=fallbackClearance,firstHit=fallbackFirstHit},pass,scaleStable});
  Release(n);item.ResetItem();await Task.Delay(50);
 }
 if(sweet)sweet.ResetGame();
 }catch(Exception e){results.Add(new{exception=e.ToString(),pass=false});}
 finally{if(n)Release(n);if(go)UnityEngine.Object.Destroy(go);File.WriteAllText("../Game_Plans/Distance_Grab_Input_2026-10-20.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));}
 }
 static void Release(NearFarInteractor n){if(n&&n.selectInput!=null)n.selectInput.QueueManualState(false,0f);}
}
}
#endif
