#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiwaliIsland;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace DiwaliGameplay {
public static class FinalTravelVerification {
 public static bool IsRunning;
 public static string ReportPath=>Path.GetFullPath("../Game_Plans/FinalTravelVerification.json");
 static readonly List<object> rows=new List<object>();
 static readonly string[] names={"Rangoli Courtyard","Sweet Shop","Lantern Garden","Diya Courtyard","Fireworks","Festival Plaza","Christmas Island","Geo Island"};
 static XROrigin rig;static Camera eye;static NearFarInteractor hand;static int failures;static DateTime started;
 static Task Pause(int ms=180)=>Task.Delay(ms);
 static void Save(string state){Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));File.WriteAllText(ReportPath,Newtonsoft.Json.JsonConvert.SerializeObject(new{state,failures,started,scope="Queued XR UI presses on world travel kiosks; explicit floor-validated approach fixtures; arrival support/capsule checks and actual CharacterController.Move collision probe. Synthetic controller, not physical headset verification.",results=rows},Newtonsoft.Json.Formatting.Indented));}
 static void Check(string name,bool pass,string detail=""){if(!pass)failures++;rows.Add(new{name,pass,detail,head=eye?eye.transform.position.ToString("F3"):""});Save("running");if(!pass)Debug.LogWarning("Travel QA: "+name+": "+detail);}
 static bool External(Collider c)=>c&&c.enabled&&!c.isTrigger&&!c.transform.IsChildOf(rig.transform);
 static void Capsule(CharacterController cc,out Vector3 a,out Vector3 b,out float radius){var scale=cc.transform.lossyScale;radius=cc.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));float half=Mathf.Max(radius,cc.height*Mathf.Abs(scale.y)*.5f);var centre=cc.transform.TransformPoint(cc.center);a=centre+Vector3.up*(half-radius);b=centre-Vector3.up*(half-radius);}
 static bool Supported(out string detail){var p=eye.transform.position;var hits=Physics.RaycastAll(p+Vector3.up*.2f,Vector3.down,3f,~0,QueryTriggerInteraction.Ignore).Where(h=>External(h.collider)).OrderBy(h=>h.distance).ToArray();var hit=hits.FirstOrDefault(h=>h.normal.y>.55f);bool ok=hit.collider&&hit.distance>.45f&&hit.distance<2.4f;detail=hit.collider?hit.collider.name+" distance="+hit.distance.ToString("F3"):"No upward-facing support below head";return ok;}
 static bool ClearBody(out string detail){var cc=rig.GetComponentInChildren<CharacterController>();if(!cc){detail="CharacterController missing";return false;}Capsule(cc,out var a,out var b,out var r);var hits=Physics.OverlapCapsule(a,b,Mathf.Max(.03f,r-.035f),~0,QueryTriggerInteraction.Ignore).Where(External).ToArray();detail=string.Join(",",hits.Select(c=>c.name));return hits.Length==0;}
 static void FaceHead(Vector3 target){var from=Vector3.ProjectOnPlane(eye.transform.forward,Vector3.up);var to=Vector3.ProjectOnPlane(target-eye.transform.position,Vector3.up);if(from.sqrMagnitude>.001f&&to.sqrMagnitude>.001f)rig.transform.RotateAround(eye.transform.position,Vector3.up,Vector3.SignedAngle(from,to,Vector3.up));}
 static async Task Stage(VenueTravelKiosk kiosk){var canvas=kiosk.GetComponentInChildren<Canvas>();if(!canvas){Check("kiosk canvas exists",false,kiosk.name);return;}var point=canvas.transform.position-canvas.transform.forward*1.45f;point.y=eye.transform.position.y;rig.transform.position+=point-eye.transform.position;FaceHead(canvas.transform.position);Physics.SyncTransforms();await Pause(240);Check("approach floor support",Supported(out var floor),kiosk.name+": "+floor);Check("approach capsule clear",ClearBody(out var body),kiosk.name+": "+body);}
 static async Task<bool> Click(Button button){if(!button||!button.gameObject.activeInHierarchy||!button.interactable){Check("travel button available",false,button?button.name:"missing");return false;}var rect=button.GetComponent<RectTransform>();var centre=rect.TransformPoint(rect.rect.center);var direction=Vector3.ProjectOnPlane(centre-eye.transform.position,Vector3.up).normalized;var origin=eye.transform.position-Vector3.up*.3f+direction*.25f;var delta=centre-origin;bool reach=delta.magnitude<=3f;Check("head-based travel ray within 3m",reach,button.name+" distance="+delta.magnitude.ToString("F2"));if(!reach)return false;var blocking=Physics.RaycastAll(origin,delta.normalized,delta.magnitude-.025f,~0,QueryTriggerInteraction.Ignore).Where(h=>External(h.collider)).OrderBy(h=>h.distance).ToArray();Check("travel physical line of sight",blocking.Length==0,button.name+": "+string.Join(",",blocking.Select(h=>h.collider.name)));hand.transform.SetPositionAndRotation(origin,Quaternion.LookRotation(delta.normalized));await Pause(180);int count=0;UnityEngine.Events.UnityAction action=()=>count++;button.onClick.AddListener(action);hand.uiPressInput.QueueManualState(true,1);await Pause(140);hand.uiPressInput.QueueManualState(false,0);await Pause(240);button.onClick.RemoveListener(action);Check("queued travel UI dispatch",count==1,button.name+" clicks="+count);return count==1;}
 static async Task Visit(VenueTravelKiosk kiosk,DiwaliIslandNavigation nav,int index){await Stage(kiosk);var buttons=kiosk.GetComponentsInChildren<Button>(true);var toggle=buttons.FirstOrDefault(b=>b.name=="Teleport / choose destination");var destination=buttons.FirstOrDefault(b=>b.name==(kiosk.IslandDestinationsOnly&&index==5?"Diwali Island":names[index]));if(!destination||!destination.gameObject.activeInHierarchy)await Click(toggle);bool sent=await Click(destination);await Pause(450);var d=eye.transform.position-nav.destinations[index].position;d.y=0;Check("arrival "+names[index],sent&&d.magnitude<.35f,"XZ error="+d.magnitude.ToString("F3")+", kiosk="+kiosk.name);Check("arrival floor "+names[index],Supported(out var floor),floor);Check("arrival capsule "+names[index],ClearBody(out var body),body);}
 static void PanelApproach(VenueGame game){
  var panel=game.GetComponentInChildren<VenuePanel>();
  if(!panel){Check("venue approach panel",false,game.name);return;}
  var cc=rig.GetComponentInChildren<CharacterController>();
  Capsule(cc,out var a,out var b,out var radius);
  float eyeToFeet=eye.transform.position.y-(Mathf.Min(a.y,b.y)-radius);
  var target=panel.transform.position;var forward=-panel.transform.forward;
  bool staged=false;string fixture="No floor-supported clear approach within 1.8m.";
  foreach(float distance in new[]{1.5f,1.05f,1.8f}){
   foreach(float angle in new[]{0f,25f,-25f,45f,-45f,60f,-60f}){
    var approach=target+Quaternion.AngleAxis(angle,Vector3.up)*forward*distance;
    var floorHit=Physics.RaycastAll(new Vector3(approach.x,target.y+2f,approach.z),Vector3.down,5f,~0,QueryTriggerInteraction.Ignore)
     .Where(h=>External(h.collider)&&!h.collider.attachedRigidbody&&h.normal.y>.75f&&
      h.point.y>=game.transform.position.y-.25f&&h.point.y<=game.transform.position.y+.8f&&
      h.collider.bounds.size.x>1f&&h.collider.bounds.size.z>1f).OrderByDescending(h=>h.point.y).FirstOrDefault();
    if(!floorHit.collider)continue;
    approach.y=floorHit.point.y+eyeToFeet+.025f;
    rig.transform.position+=approach-eye.transform.position;FaceHead(target);Physics.SyncTransforms();
    if(!Supported(out _)||!ClearBody(out _))continue;
    var origin=eye.transform.position-Vector3.up*.25f;var delta=target-origin;
    if(Physics.RaycastAll(origin,delta.normalized,delta.magnitude-.03f,~0,QueryTriggerInteraction.Ignore).Any(h=>External(h.collider)))continue;
    staged=true;fixture="floor="+floorHit.collider.name+", distance="+distance+", angle="+angle;break;
   }
   if(staged)break;
  }
  Check("venue standing approach "+game.GetType().Name,staged,fixture);
  if(!staged)return;
  Check("venue approach floor "+game.GetType().Name,Supported(out var support),support);
  Check("venue approach capsule "+game.GetType().Name,ClearBody(out var capsule),capsule);
  var from=eye.transform.position-Vector3.up*.25f;var d=target-from;
  var hits=Physics.RaycastAll(from,d.normalized,d.magnitude-.03f,~0,QueryTriggerInteraction.Ignore).Where(h=>External(h.collider)).ToArray();
  Check("venue panel physical LOS "+game.GetType().Name,hits.Length==0,string.Join(",",hits.Select(h=>h.collider.name)));
 }
 static async Task CollisionProbe(){var cc=rig.GetComponentInChildren<CharacterController>();if(!cc){Check("movement controller exists",false);return;}var saved=rig.transform.position;bool tested=false;foreach(var game in UnityEngine.Object.FindObjectsByType<VenueGame>(FindObjectsSortMode.None)){PanelApproach(game);if(!ClearBody(out _))continue;Capsule(cc,out var top,out var bottom,out var radius);var centre=(top+bottom)*.5f;foreach(var dir in new[]{Vector3.forward,Vector3.back,Vector3.right,Vector3.left}){var hits=Physics.SphereCastAll(centre,radius*.85f,dir,2f,~0,QueryTriggerInteraction.Ignore).Where(h=>External(h.collider)&&!h.collider.GetComponentInParent<Rigidbody>()&&Mathf.Abs(h.normal.y)<.3f&&h.distance>.15f).OrderBy(h=>h.distance).ToArray();if(hits.Length==0)continue;var hit=hits[0];var before=cc.transform.position;float requested=hit.distance+.65f;CollisionFlags flags=0;for(int i=0;i<20;i++){flags|=cc.Move(dir*(requested/20));await Pause(10);}float moved=Vector3.Dot(cc.transform.position-before,dir);Check("CharacterController wall collision",cc.enabled&&cc.detectCollisions&&(flags&CollisionFlags.Sides)!=0&&moved<requested-.15f&&moved<=hit.distance+.15f,"wall="+hit.collider.name+", requested="+requested.ToString("F2")+", moved="+moved.ToString("F2")+", hit="+hit.distance.ToString("F2")+", flags="+flags);Check("wall probe ends outside geometry",ClearBody(out var detail),detail);tested=true;break;}if(tested)break;}Check("wall probe fixture found",tested);rig.transform.position=saved;Physics.SyncTransforms();}
 public static async void Run(){if(IsRunning||!Application.isPlaying)return;IsRunning=true;rows.Clear();failures=0;started=DateTime.UtcNow;GameObject controller=null;Vector3 saved=Vector3.zero;Quaternion rotation=Quaternion.identity;var menuStates=new Dictionary<VenueTravelKiosk,bool>();try{rig=UnityEngine.Object.FindFirstObjectByType<XROrigin>();eye=rig?rig.Camera:null;if(!rig||!eye){Check("XR Origin and camera",false);return;}saved=rig.transform.position;rotation=rig.transform.rotation;var nav=UnityEngine.Object.FindFirstObjectByType<DiwaliIslandNavigation>();var kiosks=UnityEngine.Object.FindObjectsByType<VenueTravelKiosk>(FindObjectsSortMode.None);foreach(var kiosk in kiosks){var menu=kiosk.transform.Find("Travel panel/Destinations");if(menu)menuStates[kiosk]=menu.gameObject.activeSelf;}Check("all travel destinations",nav&&nav.destinations.Length==names.Length);Check("travel kiosks installed",kiosks.Length>=names.Length);if(!nav||nav.destinations.Length!=names.Length||kiosks.Length==0)return;controller=new GameObject("Travel verification queued controller");controller.AddComponent<Rigidbody>().isKinematic=true;var sphere=controller.AddComponent<SphereCollider>();sphere.isTrigger=true;sphere.radius=.045f;hand=controller.AddComponent<NearFarInteractor>();hand.interactionManager=UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>();hand.enableUIInteraction=true;hand.selectInput.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;hand.uiPressInput.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;var attach=hand.interactionAttachController as InteractionAttachController;attach.transformToFollow=controller.transform;attach.useMomentum=false;attach.useDistanceBasedVelocityScaling=false;attach.useManipulationInput=false;await Pause(250);var entrance=kiosks.OrderBy(k=>Mathf.Abs(k.transform.position.z+23.1f)).First();for(int i=0;i<names.Length;i++){var kiosk=i==0?entrance:kiosks.OrderBy(k=>Vector3.Distance(k.transform.position,eye.transform.position)).First();await Visit(kiosk,nav,i);}var geo=kiosks.OrderBy(k=>Vector3.Distance(k.transform.position,nav.destinations[7].position)).First();await Visit(geo,nav,5);var christmas=kiosks.OrderBy(k=>Vector3.Distance(k.transform.position,nav.destinations[6].position)).First();await Visit(christmas,nav,5);foreach(var game in UnityEngine.Object.FindObjectsByType<VenueGame>(FindObjectsSortMode.None))PanelApproach(game);await CollisionProbe();}catch(Exception e){Check("travel harness exception",false,e.ToString());}finally{if(hand){hand.uiPressInput.QueueManualState(false,0);hand.selectInput.QueueManualState(false,0);}foreach(var state in menuStates){if(!state.Key)continue;var menu=state.Key.transform.Find("Travel panel/Destinations");if(menu&&menu.gameObject.activeSelf!=state.Value)state.Key.Toggle();}if(rig)rig.transform.SetPositionAndRotation(saved,rotation);if(controller)UnityEngine.Object.Destroy(controller);hand=null;IsRunning=false;Save(failures==0?"passed":"failed");Debug.Log("Travel verification: "+failures+" failures. "+ReportPath);}}
}
}
#endif
