#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

namespace DiwaliGameplay {
public static class SweetReachAndCarryVerification {
 public static bool Running;
 public static string Folder=>Path.GetFullPath("Documentation/Sweet_Basket_Motion_2026-10-25");
 static SweetGame game;static NearFarInteractor hand;static Unity.XR.CoreUtils.XROrigin rig;
 static readonly List<object> rows=new List<object>();static int failures;static Vector3 workEye=new Vector3(-30.30f,1.6f,1.70f);
 static async Task Wait(int ms=150){await Task.Delay(ms);}
 static void Save(string state){File.WriteAllText(Path.Combine(Folder,"Play_Mode.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{state,failures,scope="Desktop Play Mode, queued NearFar inputs. Stocking, swapping, checkout use one fixed head position and hand positions within 0.7 m. Initial basket collection and carry-motion checks move the rig explicitly. No physical Quest verification.",results=rows},Newtonsoft.Json.Formatting.Indented));}
 static void Check(string name,bool passed,string detail=""){rows.Add(new{name,passed,detail});if(!passed){failures++;Debug.LogWarning("Sweet reach/carry: "+name+" "+detail);}Save("running");}
 static void Eye(Vector3 point){var delta=point-rig.Camera.transform.position;rig.transform.position+=delta;if(hand)hand.transform.position+=delta;}
 static Vector3 Origin(Vector3 target,float reach=.48f,float drop=.35f){var direction=Vector3.ProjectOnPlane(target-rig.Camera.transform.position,Vector3.up).normalized;return rig.Camera.transform.position+direction*reach-Vector3.up*drop;}
 static void Aim(Vector3 target,float reach=.48f,float drop=.35f){var origin=Origin(target,reach,drop);hand.transform.SetPositionAndRotation(origin,Quaternion.LookRotation(target-origin));}
 static async Task Release(){hand.selectInput.QueueManualState(false,0);hand.uiPressInput.QueueManualState(false,0);await Wait(220);}
 static async Task<bool> Target(GrabbableItem item){
  var colliders=item.grab.colliders.Where(c=>c&&c.enabled&&c.gameObject.activeInHierarchy);
  var points=(game.baskets.Contains(item)?colliders.OrderByDescending(c=>c.bounds.center.y):colliders.OrderBy(c=>Vector3.Distance(c.bounds.center,rig.Camera.transform.position))).Select(c=>c.bounds.center).ToArray();
  foreach(var point in points)foreach(float drop in new[]{.1f,.25f,.4f}){Aim(point,.42f,drop);await Wait(80);var valid=new List<IXRInteractable>();hand.GetValidTargets(valid);if(valid.Contains(item.grab))return true;}return false;
 }
 static async Task<bool> Grip(GrabbableItem item,string label){await Release();bool found=await Target(item);Check("Ray reaches "+label,found,"handDistance="+Vector3.Distance(hand.transform.position,rig.Camera.transform.position).ToString("F2"));if(!found)return false;hand.selectInput.QueueManualState(true,1);await Wait(220);Check("Queued grip "+label,item.IsHeld);return item.IsHeld;}
 static async Task DropToward(Vector3 target){Aim(target,.58f,.30f);await Wait(300);Check("Drop hand remains within reach",Vector3.Distance(hand.transform.position,rig.Camera.transform.position)<.7f);await Release();await Wait(550);}
 static async Task Click(Button button,string label){await Release();bool available=button&&button.gameObject.activeInHierarchy&&button.interactable;Check("Visible "+label,available);if(!available)return;Canvas.ForceUpdateCanvases();var rect=button.GetComponent<RectTransform>();var target=rect.TransformPoint(rect.rect.center);Aim(target,.48f,.3f);await Wait(220);int clicks=0;UnityEngine.Events.UnityAction count=()=>clicks++;button.onClick.AddListener(count);hand.uiPressInput.QueueManualState(true,1);await Wait(180);hand.uiPressInput.QueueManualState(false,0);await Wait(250);button.onClick.RemoveListener(count);Check("Queued trigger "+label,clicks==1,"clicks="+clicks+",rayLength="+Vector3.Distance(hand.transform.position,target).ToString("F2"));}
 static void Capture(string name,Vector3 eye,Vector3 look,float fov=85){File.Copy(ChristmasIslandQA.Render("Sweet-Reach-"+name,eye,look,fov),Path.Combine(Folder,name+".png"),true);}
 static async Task CarryCases(GrabbableItem basket,string label){
  if(!await Grip(basket,label))return;
  Eye(workEye);
  var startHand=workEye+new Vector3(0,-.35f,-.42f);
  hand.transform.SetPositionAndRotation(startHand,Quaternion.LookRotation(Vector3.back));await Wait(200);
  var startBasket=basket.transform.position;
  hand.transform.position=startHand+Vector3.up*.45f;await Wait(200);
  Check(label+" follows vertical hand movement",basket.IsHeld&&basket.transform.position.y-startBasket.y>.30f,"lift="+(basket.transform.position.y-startBasket.y).ToString("F3"));
  hand.transform.SetPositionAndRotation(startHand,Quaternion.LookRotation(Vector3.back));await Wait(200);startBasket=basket.transform.position;
  hand.transform.position=startHand+Vector3.right*.4f;await Wait(200);
  Check(label+" follows sideways hand movement",basket.IsHeld&&basket.transform.position.x-startBasket.x>.25f,"side="+(basket.transform.position.x-startBasket.x).ToString("F3"));
  hand.transform.SetPositionAndRotation(startHand,Quaternion.LookRotation(Vector3.back));await Wait(200);startBasket=basket.transform.position;
  hand.transform.rotation=Quaternion.LookRotation(new Vector3(0,.5f,-.866f));await Wait(200);
  Check(label+" follows upward wrist aiming",basket.IsHeld&&basket.transform.position.y-startBasket.y>.30f,"lift="+(basket.transform.position.y-startBasket.y).ToString("F3"));
  hand.transform.rotation=Quaternion.LookRotation(new Vector3(.6f,0,-.8f));await Wait(200);
  Check(label+" follows sideways wrist aiming",basket.IsHeld&&basket.transform.position.x-startBasket.x>.25f,"side="+(basket.transform.position.x-startBasket.x).ToString("F3"));
  var attach=(InteractionAttachController)hand.interactionAttachController;
  hand.transform.SetPositionAndRotation(startHand,Quaternion.LookRotation(Vector3.back));
  ((IInteractionAttachController)attach).MoveTo(startHand+Vector3.back*.95f);await Wait(200);startBasket=basket.transform.position;
  ((IInteractionAttachController)attach).ApplyLocalPositionOffset(Vector3.forward*.45f);await Wait(200);
  Check(label+" follows far grab distance adjustment",basket.IsHeld&&Vector3.Distance(basket.transform.position,startBasket)>.25f,"movement="+Vector3.Distance(basket.transform.position,startBasket).ToString("F3"));
  ((IInteractionAttachController)attach).MoveTo(hand.transform.position+hand.transform.forward*1.05f);
  foreach(float height in new[]{1.15f,1.6f,1.85f})foreach(float side in new[]{-.23f,.23f})foreach(float lift in new[]{-.45f,0f}){
   Eye(new Vector3(workEye.x,height,workEye.z));var direction=new Vector3(-1,0,-.1f).normalized;
   var position=rig.Camera.transform.position+direction*.38f+Vector3.forward*side+Vector3.up*lift;
   hand.transform.SetPositionAndRotation(position,Quaternion.Euler(lift==0?-45:55,side<0?225:315,0));await Wait(110);
   var bounds=GameObjectUtility.BoundsOf(basket.transform);float handGap=Vector3.Distance(bounds.ClosestPoint(hand.transform.position),hand.transform.position);float headGap=Vector3.Distance(bounds.ClosestPoint(rig.Camera.transform.position),rig.Camera.transform.position);
   bool inside=basket==game.basket?game.selected.All(s=>Vector3.Distance(GameObjectUtility.BoundsOf(s.transform).center,bounds.center)<1):true;
   Check(label+" h"+height+" side"+side+" lift"+lift,basket.IsHeld&&handGap>.18f&&headGap>.45f&&Vector3.Angle(basket.transform.up,Vector3.up)<2&&inside,"handGap="+handGap.ToString("F3")+",headGap="+headGap.ToString("F3")+",top="+bounds.max.y.ToString("F3"));
  }
  Eye(workEye);Aim(game.rest.position,.48f,.3f);await Wait(160);
  Capture(label+"-forward-view",rig.Camera.transform.position,rig.Camera.transform.position+new Vector3(-2,0,-.4f));
  Capture(label+"-basket-view",rig.Camera.transform.position,GameObjectUtility.BoundsOf(basket.transform).center,75);
  await DropToward(game.rest.position);Check(label+" releases into basket zone",Vector3.Distance(basket.transform.position,game.rest.position)<.04f);
 }
 [MenuItem("Diwali/Gameplay/Verify Sweet fixed reach and loaded carry")]
 public static async void Run(){
  if(Running||!Application.isPlaying)return;Running=true;rows.Clear();failures=0;Directory.CreateDirectory(Folder);
  game=UnityEngine.Object.FindFirstObjectByType<SweetGame>();rig=UnityEngine.Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();var saved=rig.transform.position;var rotation=rig.transform.rotation;GameObject controller=null;
  var locomotion=UnityEngine.Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.XRBodyTransformer>();bool locomotionEnabled=locomotion&&locomotion.enabled;if(locomotion)locomotion.enabled=false;
  try{
   controller=new GameObject("Sweet fixed reach queued controller");controller.AddComponent<Rigidbody>().isKinematic=true;var col=controller.AddComponent<SphereCollider>();col.isTrigger=true;col.radius=.04f;
   hand=controller.AddComponent<NearFarInteractor>();hand.interactionManager=UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>();hand.enableUIInteraction=true;
   hand.selectInput.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;hand.uiPressInput.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;
   var attach=(InteractionAttachController)hand.interactionAttachController;attach.transformToFollow=controller.transform;attach.useMomentum=false;attach.useDistanceBasedVelocityScaling=false;attach.useManipulationInput=false;
   if(hand.farInteractionCaster is CurveInteractionCaster caster){caster.hitDetectionType=CurveInteractionCaster.HitDetectionType.Raycast;caster.castDistance=3;}
   Eye(workEye);await Wait(200);game.ResetGame();
   await Click(game.ActivityPanel.GetComponentsInChildren<Button>().First(b=>b.name=="Make a Gift Basket"),"Begin");
   await Click(game.ActivityPanel.GetComponentsInChildren<Button>().First(b=>b.name=="Open Cabinet"),"Open cabinet");await Wait(700);
   Check("Permanent basket checkout sign exists",game.basketZoneSign&&game.basketZoneSign.GetComponentInChildren<Text>().text.Contains("CHECKOUT"));
   Eye(new Vector3(-33.1f,1.6f,2.84f));
   if(await Grip(game.basket,"wall shelf basket")){Eye(new Vector3(-32.9f,1.6f,3.1f));await Wait(150);Eye(workEye);await Wait(150);await DropToward(game.rest.position);}
   Eye(workEye);Check("Basket collected and placed",game.basketReady&&Vector3.Distance(game.basket.transform.position,game.rest.position)<.04f);
   if(!game.basketReady)throw new InvalidOperationException("Basket pickup/drop failed; stocking precondition is unmet.");
   var sourceOrder=new[]{"Barfi","Modak","Gujiya","Barfi"};
   foreach(var id in sourceOrder){
    var candidates=game.sweets.Where(s=>s.itemId==id&&!game.selected.Contains(s)).OrderBy(s=>Vector3.Distance(s.transform.position,workEye)).ToArray();GrabbableItem chosen=null;
    for(int opening=0;opening<2&&!chosen;opening++){
     foreach(var candidate in candidates)if(await Target(candidate)){chosen=candidate;break;}
     if(!chosen){var change=game.ActivityPanel.GetComponentsInChildren<Button>().First(b=>b.name=="Switch open side");await Click(change,"Switch cabinet side");await Wait(700);}
    }
    int before=game.selected.Count;if(chosen&&await Grip(chosen,"stock "+id))await DropToward(game.basket.transform.position+Vector3.up*.10f);
    Check("Stock and pack "+id+" without walking",game.selected.Count==before+1&&Vector3.Distance(rig.Camera.transform.position,workEye)<.02f,"selected="+game.selected.Count);
   }
   Check("Four actual released sweets packed",game.selected.Count==4);if(game.selected.Count!=4)throw new InvalidOperationException("Actual stocking failed; skipping dependent checkout checks.");
   var swap=game.selected.First(s=>s.itemId=="Barfi");
   if(await Grip(swap,"packed sweet for swapping")){Check("Grabbing a packed sweet removes it",game.selected.Count==3);await DropToward(game.ShelfReturnTarget(swap).position);Check("Sweet returns to its original shelf",!game.selected.Contains(swap)&&Vector3.Distance(swap.transform.position,game.ShelfReturnTarget(swap).position)<.05f);}
   if(await Grip(swap,"returned shelf sweet"))await DropToward(game.basket.transform.position+Vector3.up*.10f);
   Check("Four sweets restored after swap",game.selected.Count==4);
   Capture("Packing-from-work-position",workEye,(game.rest.position+game.checkout.position)*.5f);
   await CarryCases(game.basket,"loaded basket");
   Check("Loaded carry preserves four sweets",game.selected.Count==4);
   var display=game.GetComponent<SweetCheckoutDisplay>();display.RefreshStatus();
   if(await Grip(game.basket,"full basket for checkout"))await DropToward(game.checkout.position);await Wait(500);
   Check("Separate checkout area exists",Vector3.Distance(game.rest.position,game.checkout.position)>.70f);
   Check("Full basket snaps to dedicated checkout",game.docked&&Vector3.Distance(game.basket.transform.position,game.checkout.position)<.04f);
   Check("Large menu hidden for payment",!game.ActivityPanel.gameObject.activeInHierarchy);
   if(await Grip(game.card,"festival card")){
    for(int i=0;i<8&&!game.authorized;i++){Aim(game.cardReader.position,.48f+i*.02f,.3f);await Wait(150);}
   }
   Check("Card tap from work position",game.authorized&&Vector3.Distance(rig.Camera.transform.position,workEye)<.02f,"cardDistance="+Vector3.Distance(game.card.transform.position,game.cardReader.position).ToString("F3"));
   Capture("Payment-clear-view",workEye,display.DisplayCanvas.transform.position,65);
   await Release();display.RefreshStatus();await Click(display.ActionButton,"Share gift");Check("Share completes Sweet game",game.completed&&game.confirmed);
   game.BeginGame();Eye(new Vector3(-33,1.6f,4.58f));await Wait();await CarryCases(game.baskets[1],"second empty basket");
  }catch(Exception e){Check("Verifier exception",false,e.ToString());Debug.LogException(e);}
  finally{if(hand)await Release();game.ResetGame();rig.transform.SetPositionAndRotation(saved,rotation);if(locomotion)locomotion.enabled=locomotionEnabled;if(controller)UnityEngine.Object.Destroy(controller);hand=null;Running=false;Save(failures==0?"passed":"failed");Debug.Log("Sweet reach and carry: "+failures+" failures; "+Folder);}
 }
}
}
#endif
