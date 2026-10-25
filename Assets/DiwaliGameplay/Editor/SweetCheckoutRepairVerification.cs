#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace DiwaliGameplay {
public static class SweetCheckoutRepairVerification {
 public static bool Running;
 public static string Folder=>Path.GetFullPath("Documentation/Sweet_Checkout_2026-10-25");
 static readonly List<object> rows=new List<object>();
 static NearFarInteractor hand;static int failures;
 static async Task Wait(int ms=150){await Task.Delay(ms);}
 static void Check(string name,bool passed,string detail="") {rows.Add(new{name,passed,detail});if(!passed){failures++;Debug.LogWarning("Sweet checkout QA: "+name+" "+detail);}}
 static async Task Release(){hand.selectInput.QueueManualState(false,0);hand.uiPressInput.QueueManualState(false,0);await Wait();}
 static async Task<bool> Grip(GrabbableItem item) {
  await Release();bool target=false;var center=GameObjectUtility.BoundsOf(item.transform).center;
  foreach(var side in new[]{Vector3.forward,Vector3.right,Vector3.left,Vector3.back}) {
   var origin=center+side*.85f;origin.y=Mathf.Clamp(origin.y+.25f,.9f,1.45f);
   hand.transform.SetPositionAndRotation(origin,Quaternion.LookRotation(center-origin));await Wait();
   var valid=new List<IXRInteractable>();hand.GetValidTargets(valid);if(valid.Contains(item.grab)){target=true;break;}
  }
  Check("Controller ray targets "+item.itemId,target);if(!target)return false;
  hand.selectInput.QueueManualState(true,1);await Wait(250);Check("Queued grip selects "+item.itemId,item.IsHeld);return item.IsHeld;
 }
 static async Task Move(GrabbableItem item,Vector3 target) {
  for(int i=0;i<5;i++){hand.transform.position+=target-item.transform.position;await Wait(80);}
 }
 static async Task Click(Button button,string label) {
  await Release();Canvas.ForceUpdateCanvases();
  bool available=button&&button.gameObject.activeInHierarchy&&button.interactable;Check("Visible enabled "+label,available);if(!available)return;
  var canvas=button.GetComponentInParent<Canvas>();var rect=button.GetComponent<RectTransform>();var center=rect.TransformPoint(rect.rect.center);
  var origin=center-canvas.transform.forward*.7f;origin.y=Mathf.Clamp(origin.y,.9f,1.45f);
  hand.transform.SetPositionAndRotation(origin,Quaternion.LookRotation(center-origin));await Wait(220);
  var hits=new List<RaycastResult>();var ray=new TrackedDeviceEventData(EventSystem.current){rayPoints=new List<Vector3>{origin,center+canvas.transform.forward*.05f},layerMask=~0};
  canvas.GetComponent<TrackedDeviceGraphicRaycaster>().Raycast(ray,hits);
  Check("Unblocked tracked UI ray "+label,hits.Any(h=>h.gameObject==button.gameObject),string.Join(",",hits.Select(h=>h.gameObject.name)));
  int clicks=0;UnityEngine.Events.UnityAction counted=()=>clicks++;button.onClick.AddListener(counted);
  hand.uiPressInput.QueueManualState(true,1);await Wait(180);hand.uiPressInput.QueueManualState(false,0);await Wait(250);
  button.onClick.RemoveListener(counted);Check("Queued trigger dispatch "+label,clicks==1,"clicks="+clicks);
 }
 static void Capture(string name,Vector3 eye,Vector3 look,float fov=65) {
  var path=ChristmasIslandQA.Render("Sweet-Checkout-"+name,eye,look,fov);File.Copy(path,Path.Combine(Folder,name+".png"),true);
 }
 [MenuItem("Diwali/Gameplay/Verify Sweet checkout repair")]
 public static async void Run() {
  if(Running||!Application.isPlaying)return;Running=true;rows.Clear();failures=0;Directory.CreateDirectory(Folder);
  var game=UnityEngine.Object.FindFirstObjectByType<SweetGame>();var display=game.GetComponent<SweetCheckoutDisplay>();
  var rig=UnityEngine.Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();var saved=rig.transform.position;var rotation=rig.transform.rotation;GameObject controller=null;
  try {
   rig.transform.position+=new Vector3(-31.15f,1.65f,3.05f)-rig.Camera.transform.position;
   controller=new GameObject("Sweet checkout queued controller");controller.AddComponent<Rigidbody>().isKinematic=true;
   var collider=controller.AddComponent<SphereCollider>();collider.isTrigger=true;collider.radius=.04f;
   hand=controller.AddComponent<NearFarInteractor>();hand.interactionManager=UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>();hand.enableUIInteraction=true;
   hand.selectInput.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;hand.uiPressInput.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;
   var attach=(InteractionAttachController)hand.interactionAttachController;attach.transformToFollow=controller.transform;attach.useMomentum=false;attach.useDistanceBasedVelocityScaling=false;attach.useManipulationInput=false;
   if(hand.farInteractionCaster is CurveInteractionCaster caster){caster.hitDetectionType=CurveInteractionCaster.HitDetectionType.Raycast;caster.castDistance=3;}
   await Wait(300);game.ResetGame();
   await Click(game.ActivityPanel.GetComponentsInChildren<Button>().First(b=>b.name=="Make a Gift Basket"),"Begin");
   await Click(game.ActivityPanel.GetComponentsInChildren<Button>().First(b=>b.name=="Open Cabinet"),"Open cabinet");await Wait(600);
   var basket=game.basket;var scale=basket.transform.lossyScale;
   Check("Packing and checkout use the authored recess",Vector3.Distance(game.rest.position,game.checkout.position)<.001f);
   Check("Basket targets retain unit world scale",Vector3.Distance(game.rest.lossyScale,Vector3.one)<.001f);
   Check("Counter anchors move together",game.rest.parent==display.screen&&game.checkout.parent==display.screen&&game.cardReader.parent==display.screen&&game.card.transform.parent==display.screen);
   if(await Grip(basket)){await Move(basket,game.rest.position+new Vector3(.3f,.16f,.12f));await Release();await Wait(650);}
   Check("Loose basket release snaps into recess",game.basketReady&&Vector3.Distance(basket.transform.position,game.rest.position)<.03f);
   Check("Docking preserves basket size",Vector3.Distance(scale,basket.transform.lossyScale)<.001f);
   foreach(var id in new[]{"Barfi","Modak","Gujiya","Gujiya"}) {
    var sweet=game.sweets.First(s=>s.itemId==id&&!game.selected.Contains(s));Check("Pack preverified sweet "+id,game.AddSweet(sweet));
   }
   await Wait(150);display.RefreshStatus();
   var panel=game.ActivityPanel.GetComponent<RectTransform>();
   foreach(var eye in new[]{new Vector3(-31.15f,1.65f,3.05f),new Vector3(-30.55f,1.65f,2.8f),new Vector3(-31.65f,1.6f,3.2f)}) {
    bool blocked=false;foreach(var target in new[]{basket.transform.position,display.DisplayCanvas.transform.position,game.cardReader.position}) {
     var delta=target-eye;var ray=new Ray(eye,delta.normalized);var plane=new Plane(panel.forward,panel.position);
     if(plane.Raycast(ray,out var distance)&&distance>0&&distance<delta.magnitude&&panel.rect.Contains(panel.InverseTransformPoint(ray.GetPoint(distance))))blocked=true;
    }Check("Side menu clears basket and payment view",!blocked,"eye="+eye);
   }
   Capture("Packing",new Vector3(-30.25f,1.85f,3.5f),new Vector3(-30.9f,1.15f,1.5f),80);
   await Click(display.ActionButton,"Monitor checkout");await Wait(650);
   Check("Checkout keeps basket in recess",game.docked&&Vector3.Distance(basket.transform.position,game.rest.position)<.03f);
   Check("Large menu hidden at checkout",!game.ActivityPanel.gameObject.activeInHierarchy);
   Check("Resting card does not auto-authorize",!game.authorized);
   if(await Grip(game.card)){Check("Card pickup alone is not a tap",!game.authorized);await Move(game.card,game.cardReader.position);await Wait(150);}
   Check("Card touch authorizes",game.authorized);Check("Menu stays hidden on card accepted",!game.ActivityPanel.gameObject.activeInHierarchy);
   display.RefreshStatus();Check("Acceptance displayed on native monitor",display.DisplayCanvas.GetComponentsInChildren<Text>().Any(t=>t.text=="CARD\nACCEPTED"));
   Capture("Card-accepted",new Vector3(-30.75f,1.6f,2.9f),display.DisplayCanvas.transform.position,50);
   await Click(display.ActionButton,"Share gift");
   Check("Share gift completes",game.completed&&game.confirmed&&game.credited.Contains("confirm"));
   var rewards=UnityEngine.Object.FindFirstObjectByType<FestivalRewards>();var gift=rewards.SweetGiftDisplay;
   Check("Central gift display created",rewards.SweetsShared&&gift);
   if(gift){Check("Central gift contains basket and four sweets",gift.GetComponentsInChildren<MeshFilter>().Length>=5);Check("Central gift has no interaction or physics",gift.GetComponentsInChildren<Collider>().Length==0&&gift.GetComponentsInChildren<Rigidbody>().Length==0&&gift.GetComponentsInChildren<GrabbableItem>().Length==0);Capture("Central-gift",gift.position+new Vector3(2,1.8f,3.2f),gift.position+Vector3.up*.2f,60);}
   await Wait(4200);Check("Central gift persists after celebration",rewards.SweetGiftDisplay&&!rewards.SweetCelebrationActive);
   await Click(display.ActionButton,"New gift");Check("New gift restores side menu",!game.begun&&!game.completed&&game.ActivityPanel.gameObject.activeInHierarchy&&game.selected.Count==0);
  }catch(Exception error){Check("Verification exception",false,error.ToString());Debug.LogException(error);}
  finally {
   if(hand)await Release();game.ResetGame();rig.transform.SetPositionAndRotation(saved,rotation);if(controller)UnityEngine.Object.Destroy(controller);hand=null;Running=false;
   File.WriteAllText(Path.Combine(Folder,"Play_Mode.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{failures,scope="Focused Unity desktop Play Mode: actual queued NearFar grips, XR UI triggers, basket release, card touch, monitor states and central reward. Sweet stocking is preconditioned using previously verified AddSweet; no physical Quest test.",results=rows},Newtonsoft.Json.Formatting.Indented));
   Debug.Log("Sweet checkout verification: "+failures+" failures; "+Folder);
  }
 }
}
}
#endif
