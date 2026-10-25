#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

namespace DiwaliGameplay {
public static class SweetMenuFacingVerification {
 public static bool Running;
 static readonly List<object> rows=new List<object>();
 static int failures;
 static NearFarInteractor hand;
 static SweetGame game;
 static Transform head,rig;
 static string Folder=>Path.GetFullPath("Documentation/Venue_Feedback_2026-10-25");
 static async Task Wait(int ms=180){await Task.Delay(ms);}
 static void Check(string name,bool passed,string detail=""){rows.Add(new{name,passed,detail});if(!passed)failures++;}
 static void Eye(Vector3 eye){rig.position+=eye-head.position;}
 static async Task Click(string name){
  var button=game.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>b.name==name&&b.gameObject.activeInHierarchy);
  Check("Visible "+name,button&&button.interactable);if(!button)return;
  Canvas.ForceUpdateCanvases();var rect=button.GetComponent<RectTransform>();var target=rect.TransformPoint(rect.rect.center);
  var forward=Vector3.ProjectOnPlane(target-head.position,Vector3.up).normalized;
  var origin=head.position+forward*.48f-Vector3.up*.25f;
  hand.transform.SetPositionAndRotation(origin,Quaternion.LookRotation(target-origin));await Wait();
  int count=0;UnityEngine.Events.UnityAction record=()=>count++;button.onClick.AddListener(record);
  hand.uiPressInput.QueueManualState(true,1);await Wait();hand.uiPressInput.QueueManualState(false,0);await Wait();button.onClick.RemoveListener(record);
  Check("Queued trigger "+name,count==1,"clicks="+count+",ray="+Vector3.Distance(origin,target).ToString("F2"));
 }
 public static async void Run(){
  if(Running||!Application.isPlaying)return;Running=true;failures=0;rows.Clear();Directory.CreateDirectory(Folder);
  game=UnityEngine.Object.FindFirstObjectByType<SweetGame>();head=Camera.main.transform;rig=head.root;
  var saved=rig.position;var rotation=rig.rotation;GameObject controller=null;
  var locomotion=UnityEngine.Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.XRBodyTransformer>();bool enabled=locomotion&&locomotion.enabled;if(locomotion)locomotion.enabled=false;
  try{
   controller=new GameObject("Sweet entrance menu queued controller");controller.AddComponent<Rigidbody>().isKinematic=true;
   var collider=controller.AddComponent<SphereCollider>();collider.isTrigger=true;collider.radius=.04f;
   hand=controller.AddComponent<NearFarInteractor>();hand.interactionManager=UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>();hand.enableUIInteraction=true;
   hand.selectInput.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;hand.uiPressInput.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;
   var attach=(InteractionAttachController)hand.interactionAttachController;attach.transformToFollow=controller.transform;attach.useMomentum=false;attach.useDistanceBasedVelocityScaling=false;
   if(hand.farInteractionCaster is CurveInteractionCaster cast){cast.hitDetectionType=CurveInteractionCaster.HitDetectionType.Raycast;cast.castDistance=3;}
   game.ResetGame();Eye(new Vector3(-24.8f,1.6f,3.3f));await Wait(350);
   var panel=game.ActivityPanel;
   Check("Menu faces entering player",Vector3.Dot(-panel.transform.forward,Vector3.ProjectOnPlane(head.position-panel.transform.position,Vector3.up).normalized)>.9f);
   File.Copy(ChristmasIslandQA.Render("Sweet-menu-from-entrance",head.position,panel.transform.position,75),Path.Combine(Folder,"Sweet-menu-entrance.png"),true);
   await Click("Make a Gift Basket");Check("Begin from entrance",game.begun);
   Eye(new Vector3(-30.3f,1.6f,1.7f));await Wait(350);
   Check("Menu turns toward packing side",Vector3.Dot(-panel.transform.forward,Vector3.ProjectOnPlane(head.position-panel.transform.position,Vector3.up).normalized)>.9f);
   Check("Menu stays beside the rack",Vector3.Distance(panel.transform.position,game.panelPosition)<.01f);
   await Click("Open Cabinet");Check("Cabinet opens from packing side",game.cabinetOpen);
   await Click("How To Play");await Click("Close");
   File.Copy(ChristmasIslandQA.Render("Sweet-menu-from-workspace",head.position,panel.transform.position,75),Path.Combine(Folder,"Sweet-menu-workspace.png"),true);
  }catch(Exception e){Check("exception",false,e.ToString());}
  finally{
   game.ResetGame();rig.SetPositionAndRotation(saved,rotation);if(locomotion)locomotion.enabled=enabled;
   if(controller)UnityEngine.Object.Destroy(controller);hand=null;Running=false;
   File.WriteAllText(Path.Combine(Folder,"Sweet_Menu_Play_Mode.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{state=failures==0?"passed":"failed",failures,scope="Queued XR UI input from entrance and packing positions; desktop Play Mode",results=rows},Newtonsoft.Json.Formatting.Indented));
   Debug.Log("Sweet menu orientation: "+failures+" failures");
  }
 }
}
}
#endif
