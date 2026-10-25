using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace DiwaliGameplay {
public class FireworksGame : VenueGame {
 public GrabbableItem rocket,fountain,spinner,plunger;
 public Transform rocketSocket,fountainSocket,spinnerSocket,rocketPlatform,lift,spinnerPivot;
 public ParticleSystem rocketEffect,fountainEffect,spinnerEffect;
 public int stage,aimPreset,speedPreset;
 public Slider timingBar;
 public Vector3 panelPosition;public Quaternion panelRotation;
 public Vector3 rocketStagingPosition;
 public bool rocketLoaded,fountainLoaded,spinnerLoaded,raised;
 public float phase,holdProgress;
 public Vector3 liftHome,plungerHome;
 public Transform plungerFrame;
 public Quaternion rocketHome,spinnerHome;
 public Vector3 rocketTiltAxis=Vector3.right,rocketLocalLaunchAxis=Vector3.up;
 public Quaternion rocketWorldHome=Quaternion.identity;
 bool rocketRunning,fountainRunning,spinnerRunning;
 public float travel=.18f;
 public bool TimingReady=>true;
 VenuePanel panel;
 DiyaRestTarget rocketTarget,fountainTarget,spinnerTarget;TextMesh showBoard;
 Button beginButton,finaleButton;
 Slider speedSlider;FireworksTargetBoard targetBoard;bool launching;
 public FireworksGrabAdjustment rocketAdjustment,boardAdjustment,fountainAdjustment;
 float aimValue=.5f,fountainValue=.25f;
 public bool AdjustmentHeld=>(rocketAdjustment&&rocketAdjustment.IsDragging)||(boardAdjustment&&boardAdjustment.IsDragging)||(fountainAdjustment&&fountainAdjustment.IsDragging);
 bool rocketHandled,fountainHandled,spinnerHandled,triggerHeld,handBaselineSet,wasDown;
 float handStartY,liftAmount,plungerDepression,spinSpeed=45;
 Quaternion plungerRotation;
 Vector3 rocketStaging;
 float rocketCarryRadius;
 Quaternion rocketCarryHomeRotation;
 Transform playerHead;
 TextMesh plungerFeedback;
 public float PlungerHandTravel=>.12f;
 public float PlungerProgress=>Mathf.Clamp01(plungerDepression/Mathf.Max(.08f,travel));
 public bool ReadyToPlunge=>begun&&!completed&&!launching&&!AdjustmentHeld&&(stage==0&&rocketLoaded&&aimPreset==1||stage==1&&fountainLoaded&&raised||stage==2&&spinnerLoaded&&speedPreset==1);
 protected override void Awake(){base.Awake();if(rocketPlatform&&rocketWorldHome==Quaternion.identity)rocketWorldHome=rocketPlatform.rotation;if(rocket){rocketStaging=rocketStagingPosition!=Vector3.zero?rocketStagingPosition:rocket.transform.position;var delta=Vector3.ProjectOnPlane(rocketStaging-rocketSocket.position,Vector3.up);if(delta.magnitude<.7f){var venue=GameObjectUtility.FindExact("Fireworks - Native PBR");var side=venue?venue.right:Vector3.right;rocketStaging=rocketSocket.position+side*.95f;rocketStaging.y=rocketSocket.position.y;}}if(plunger){plungerFrame=plunger.transform.parent?plunger.transform.parent:transform;plungerHome=plungerFrame.InverseTransformPoint(plunger.transform.position);plungerRotation=plunger.transform.rotation;plunger.grab.trackPosition=false;plunger.grab.trackRotation=false;plunger.grab.trackScale=false;plunger.grab.activated.AddListener(_=>triggerHeld=true);plunger.grab.deactivated.AddListener(_=>triggerHeld=false);plunger.grab.selectEntered.AddListener(e=>{handStartY=e.interactorObject.GetAttachTransform(plunger.grab).position.y;handBaselineSet=true;});plunger.grab.selectExited.AddListener(_=>{triggerHeld=false;handBaselineSet=false;});}}
 void Start(){howToPlay="No timing challenge. Each station has three steps: LOAD, ADJUST, PRESS PLUNGER.\n1. Begin, grip the rocket from the ground, and release it near the support.\n2. Grip the loaded rocket and move your hand up or down to tilt its cradle. Grip the board and move it up or down along its stand. Aim at the rings; AIM READY confirms the path. A hit sends the rocket into the sky. Grab the plunger handle and push it down about 12 cm to launch.\n3. Load the fountain, then grip it and move up or down to adjust its height. Release it and press the plunger.\n4. Load the spinner, adjust Speed, then grab and press the plunger.\n5. Select Finale to launch more fireworks around the big central decoration. No other venue is required. All activated fireworks keep running until Restart. The target slides up and down its floor stand.";
  var venue=GameObjectUtility.FindExact("Fireworks - Native PBR");if(venue){var board=GameObjectUtility.FindUnder(venue,"board");if(board){var b=GameObjectUtility.BoundsOf(board);var toward=Vector3.back;float front=Mathf.Abs(toward.x)*b.extents.x+Mathf.Abs(toward.z)*b.extents.z;panelPosition=b.center+toward*(front+.04f);panelRotation=Quaternion.LookRotation(-toward,Vector3.up);}}
  panel=VenuePanel.Create(this,panelPosition,panelRotation,"LOAD / ADJUST / PRESS PLUNGER");panel.SetPhysicalSize(.75f,.8f);panel.autoFacePlayer=false;var recenter=panel.transform.Find("Activity tools/Move menu here");if(recenter)recenter.gameObject.SetActive(false);beginButton=panel.AddButton("Begin Light Show",BeginGame);speedSlider=panel.AddSlider("Spinner speed",.25f,AdjustSpeed);speedSlider.minValue=0;speedSlider.maxValue=1;finaleButton=panel.AddButton("Finale",Finale);rocket.transform.position=rocketStaging;ConfigureRocketGrip();BuildPlungerFeedback();rocketTarget=DiyaRestTarget.Create(transform,rocketSocket,rocket,1f,()=>begun&&!completed&&stage==0&&!rocketLoaded);fountainTarget=DiyaRestTarget.Create(transform,fountainSocket,fountain,1f,()=>begun&&!completed&&stage==1&&!fountainLoaded);spinnerTarget=DiyaRestTarget.Create(transform,spinnerSocket,spinner,1f,()=>begun&&!completed&&stage==2&&!spinnerLoaded);BuildShowBoard();rocketAdjustment=FireworksGrabAdjustment.Create(this,FireworksAdjustment.RocketTilt,rocket.transform);fountainAdjustment=FireworksGrabAdjustment.Create(this,FireworksAdjustment.FountainHeight,fountain.transform);var placement=gameObject.AddComponent<FireworksMenuPlacement>();placement.owner=this;placement.panel=panel;placement.board=targetBoard?targetBoard.board:null;foreach(var effect in new[]{rocketEffect,fountainEffect,spinnerEffect})FestivalParticleStyle.Apply(effect);var rocketMain=rocketEffect.main;rocketMain.startLifetime=2;rocketMain.startSpeed=3.8f;var trails=rocketEffect.trails;trails.enabled=true;trails.lifetime=.22f;FireworksEffectPolish.Configure(this);RefreshControls();}
 void BuildShowBoard(){var venue=GameObjectUtility.FindExact("Fireworks - Native PBR");var board=venue?GameObjectUtility.FindUnder(venue,"board"):null;if(!board)return;targetBoard=gameObject.AddComponent<FireworksTargetBoard>();targetBoard.Configure(board,GameObjectUtility.FindUnder(venue,"board stand"));boardAdjustment=FireworksGrabAdjustment.Create(this,FireworksAdjustment.TargetHeight,board);targetBoard.SetHeight(.5f);panelPosition=new Vector3(rocketSocket.position.x-2f,1.5f,rocketSocket.position.z-.35f);panel.transform.SetPositionAndRotation(panelPosition,panelRotation);panel.autoFacePlayer=true;}
 void PlacePanel(){if(panel)panel.transform.SetPositionAndRotation(panelPosition,panelRotation);}
 void RefreshControls(){if(!panel)return;beginButton.gameObject.SetActive(!begun);if(rocketAdjustment)rocketAdjustment.SetAvailable(CanAdjust(FireworksAdjustment.RocketTilt));if(boardAdjustment)boardAdjustment.SetAvailable(CanAdjust(FireworksAdjustment.TargetHeight));if(fountainAdjustment)fountainAdjustment.SetAvailable(CanAdjust(FireworksAdjustment.FountainHeight));panel.SetControlVisible(speedSlider,begun&&stage==2&&spinnerLoaded);finaleButton.gameObject.SetActive(begun&&stage==3&&!completed);}
 public bool CanAdjust(FireworksAdjustment kind)=>begun&&!completed&&!launching&&(kind==FireworksAdjustment.FountainHeight?stage==1&&fountainLoaded:stage==0&&(kind==FireworksAdjustment.TargetHeight||rocketLoaded));
 public float AdjustmentValue(FireworksAdjustment kind)=>kind==FireworksAdjustment.RocketTilt?aimValue:kind==FireworksAdjustment.FountainHeight?fountainValue:targetBoard?targetBoard.HeightValue:.5f;
 public void ApplyGrabAdjustment(FireworksAdjustment kind,float value){if(!CanAdjust(kind))return;if(kind==FireworksAdjustment.RocketTilt)AdjustAim(value);else if(kind==FireworksAdjustment.FountainHeight)AdjustLift(value);else{targetBoard.SetHeight(value);Credit("board-height");status="Release the board when ready. Grip the rocket to tilt toward the target, then press the plunger.";}}
 void ApplyRocketTilt(float value){rocketPlatform.rotation=rocketWorldHome*Quaternion.AngleAxis(-Mathf.Lerp(4,24,value),Quaternion.Inverse(rocketWorldHome)*rocketTiltAxis.normalized);}

 public override void BeginGame(){ResetGame();base.BeginGame();Credit("begin");status="ROCKET 1/3: grip the rocket from the ground and release it onto its support.";PlacePanel();}
 public bool Load(GrabbableItem item){if(!begun||completed||item.IsHeld)return false;if(stage==0&&item==rocket&&!rocketLoaded){rocketLoaded=true;rocket.SnapTo(rocketSocket,true);ApplyRocketTilt(aimValue);Credit("rocket-load");status="ROCKET 2/3: grip the loaded rocket and move up/down to tilt. Grip the board to change its height. Release when AIM READY appears.";}else if(stage==1&&item==fountain&&!fountainLoaded){fountainLoaded=true;fountain.SnapTo(fountainSocket,true);Credit("fountain-load");status="FOUNTAIN 2/3: grip the loaded fountain and move up/down to choose its height.";}else if(stage==2&&item==spinner&&!spinnerLoaded){spinnerLoaded=true;spinner.SnapTo(spinnerSocket,true);Credit("spinner-load");status="SPINNER 2/3: move the Speed slider to choose its speed.";}else return false;PlacePanel();RefreshControls();return true;}
 public void AdjustAim(float value){if(!CanAdjust(FireworksAdjustment.RocketTilt))return;aimValue=Mathf.Clamp01(value);aimPreset=1;ApplyRocketTilt(aimValue);Credit("aim");status="Release the rocket after aiming at the rings. Grip the target board to adjust its height.";RefreshControls();}
 public void AdjustLift(float value){if(!begun||stage!=1||!fountainLoaded)return;fountainValue=Mathf.Clamp01(value);raised=true;liftAmount=Mathf.Lerp(.05f,.35f,fountainValue);Credit("raise");status="FOUNTAIN 3/3: grip the plunger, then push down about 12 cm.";RefreshControls();}
 public void AdjustSpeed(float value){if(!begun||stage!=2||!spinnerLoaded)return;speedPreset=1;spinSpeed=Mathf.Lerp(25,150,value);Credit("spin");status="SPINNER 3/3: grip the plunger, then push down about 12 cm.";RefreshControls();}
 public void Aim()=>AdjustAim(.65f);public void Raise()=>AdjustLift(.65f);public void Spin()=>AdjustSpeed(.65f);
 public bool Press(float ignoredTiming){if(launching||AdjustmentHeld||!begun||completed||!plunger.IsHeld||!handBaselineSet||PlungerProgress<.9f)return false;if(stage==0&&rocketLoaded&&aimPreset==1){Credit("plunger-press");launching=true;StartCoroutine(TargetLaunch());status="Rocket launched toward target...";RefreshControls();}else if(stage==1&&fountainLoaded&&raised){Credit("plunger-press");holdProgress=2;fountainRunning=true;StartLoop(fountainEffect);stage=2;status="SPINNER 1/3: load the spinner onto its support.";}else if(stage==2&&spinnerLoaded&&speedPreset==1){Credit("plunger-press");spinnerRunning=true;StartLoop(spinnerEffect);stage=3;status="All three displays are ready. Select Finale.";}else{status="Load the current firework, then grip and adjust it. For the spinner, use its speed slider.";return false;}PlacePanel();RefreshControls();return true;}
 public void Finale(){if(!begun||stage!=3||completed)return;Credit("finale");StartLoop(rocketEffect);StartLoop(fountainEffect);StartLoop(spinnerEffect);Complete("Finale! Fireworks are launching around the central decoration.");RefreshControls();}
 void StartLoop(ParticleSystem effect){if(!effect)return;foreach(var particle in effect.GetComponentsInChildren<ParticleSystem>(true)){var main=particle.main;main.loop=true;}effect.Play(true);}
 void ShowRocket(bool visible){foreach(var renderer in rocket.GetComponentsInChildren<MeshRenderer>(true))renderer.enabled=visible;}
 Vector3 LaunchDirection()=>rocket.transform.TransformDirection(rocketLocalLaunchAxis.sqrMagnitude>.001f?rocketLocalLaunchAxis.normalized:Vector3.up).normalized;
 public bool TargetAligned(){if(!targetBoard)return false;return targetBoard.TryAim(rocketSocket.position,LaunchDirection(),out _);}
 IEnumerator TargetLaunch(){ShowRocket(true);rocket.SnapTo(rocketSocket,true);var start=rocket.transform.position;var direction=LaunchDirection();bool hit=targetBoard&&targetBoard.TryAim(start,direction,out _);Vector3 end=start+direction*3;if(hit)targetBoard.TryAim(start,direction,out end);rocket.transform.SetParent(transform,true);for(float t=0;t<.65f;t+=Time.deltaTime){rocket.transform.position=Vector3.Lerp(start,end,t/.65f);yield return null;}if(!hit){if(targetBoard)targetBoard.ShowMiss();rocket.SnapTo(rocketSocket,true);launching=false;status="Missed the target. Adjust tilt or target height until AIM READY, release the plunger, then press again.";RefreshControls();yield break;}rocket.transform.position=end;StartCoroutine(targetBoard.Hit(end));var launchRotation=rocket.transform.rotation;var skyRotation=Quaternion.FromToRotation(direction,Vector3.up)*launchRotation;for(float t=0;t<1.2f;t+=Time.deltaTime){rocket.transform.rotation=Quaternion.Slerp(launchRotation,skyRotation,Mathf.Clamp01(t/.3f));rocket.transform.position=end+Vector3.up*(t*6)+Vector3.back*(t*.6f);yield return null;}rocketEffect.transform.position=rocket.transform.position;ShowRocket(false);StartLoop(rocketEffect);rocketRunning=true;stage=1;launching=false;status="Target hit! ROCKET keeps celebrating. Load the fountain onto the lift.";RefreshControls();StartCoroutine(RocketFlight());}
 IEnumerator RocketFlight(){while(rocketRunning){yield return new WaitForSeconds(2);ShowRocket(true);rocket.SnapTo(rocketSocket,true);var start=rocket.transform.position;var direction=LaunchDirection();Vector3 target;if(!targetBoard.TryAim(start,direction,out target))target=start+direction*2;rocket.transform.SetParent(transform,true);var launchRotation=rocket.transform.rotation;var skyRotation=Quaternion.FromToRotation(direction,Vector3.up)*launchRotation;for(float t=0;t<1.6f&&rocketRunning;t+=Time.deltaTime){if(t>.5f)rocket.transform.rotation=Quaternion.Slerp(launchRotation,skyRotation,Mathf.Clamp01((t-.5f)/.3f));rocket.transform.position=t<.5f?Vector3.Lerp(start,target,t/.5f):target+Vector3.up*((t-.5f)*6);yield return null;}rocketEffect.transform.position=rocket.transform.position;ShowRocket(false);StartLoop(rocketEffect);}}
 public override void Tick(){RefreshControls();if(showBoard)showBoard.text="FESTIVAL SHOW\n"+(rocketRunning?"[ON] ROCKET":"[ ] ROCKET")+"\n"+(fountainRunning?"[ON] FOUNTAIN":"[ ] FOUNTAIN")+"\n"+(spinnerRunning?"[ON] SPINNER":"[ ] SPINNER")+"\n"+(stage==3?"READY FOR FINALE":"LOAD  >  ADJUST  >  PLUNGE");if(!begun){UpdatePlungerFeedback();return;}if(!completed){rocketHandled|=rocket.IsHeld;fountainHandled|=fountain.IsHeld;spinnerHandled|=spinner.IsHeld;if(rocketHandled&&!rocketLoaded&&!rocket.IsHeld&&rocketTarget.CanCapture())Load(rocket);if(fountainHandled&&!fountainLoaded&&!fountain.IsHeld&&fountainTarget.CanCapture())Load(fountain);if(spinnerHandled&&!spinnerLoaded&&!spinner.IsHeld&&spinnerTarget.CanCapture())Load(spinner);}if(stage==0&&rocketLoaded&&!launching&&targetBoard)targetBoard.ShowAim(aimPreset==1&&TargetAligned());UpdatePlungerFeedback();lift.localPosition=Vector3.MoveTowards(lift.localPosition,liftHome+Vector3.up*liftAmount,Time.deltaTime*.5f);if(spinnerRunning)spinnerPivot.Rotate(Vector3.right,spinSpeed*Time.deltaTime,Space.Self);float down=0;if(plunger.IsHeld&&handBaselineSet&&plunger.grab.interactorsSelecting.Count>0){var hand=plunger.grab.interactorsSelecting[0].GetAttachTransform(plunger.grab);down=Mathf.Clamp01((handStartY-hand.position.y)/PlungerHandTravel)*Mathf.Max(.08f,travel);}plunger.transform.SetPositionAndRotation(plungerFrame.TransformPoint(plungerHome)+Vector3.down*down,plungerRotation);plungerDepression=down;bool pressed=plunger.IsHeld&&PlungerProgress>=.9f;if(pressed&&!wasDown)Press(0);wasDown=pressed;}
 void ConfigureRocketGrip(){
  if(!rocket||!rocket.grab)return;
  var grab=rocket.grab;
  var pose=rocket.transform.Find("Rocket carrying grip");
  if(!pose){pose=new GameObject("Rocket carrying grip").transform;pose.SetParent(rocket.transform,false);}
  var axis=rocketLocalLaunchAxis.sqrMagnitude>.001f?rocketLocalLaunchAxis.normalized:Vector3.up;
  var rotation=rocket.transform.rotation*Quaternion.FromToRotation(Vector3.up,axis);
  var bounds=GameObjectUtility.BoundsOf(rocket.transform);
  // Keep the body beyond the controller and its long axis upright in the hand.
  float halfDepth=0;
  foreach(var mesh in rocket.GetComponentsInChildren<MeshFilter>()){
   if(!mesh.sharedMesh)continue;var local=mesh.sharedMesh.bounds;
   for(int k=0;k<8;k++){
    var corner=local.center+Vector3.Scale(local.extents,new Vector3((k&1)==0?-1:1,(k&2)==0?-1:1,(k&4)==0?-1:1));
    halfDepth=Mathf.Max(halfDepth,Mathf.Abs(Vector3.Dot(mesh.transform.TransformPoint(corner)-bounds.center,rotation*Vector3.forward)));
   }
  }
  float clearance=Mathf.Clamp(halfDepth+.14f,.36f,.7f);
  pose.SetPositionAndRotation(bounds.center-rotation*new Vector3(0,.12f,clearance),rotation);
  rocketCarryRadius=Mathf.Max(bounds.extents.x,bounds.extents.z);
  rocketCarryHomeRotation=rocket.transform.rotation;
  grab.attachTransform=pose;grab.useDynamicAttach=false;grab.trackPosition=false;grab.trackRotation=false;grab.attachEaseInTime=.15f;
  grab.selectEntered.AddListener(OnRocketGrab);
 }
 public static Pose RocketCarryingPose(Vector3 handPosition,Vector3 handForward,Transform head,Vector3 launchAxis,Quaternion homeRotation,float radius){
  var away=head?Vector3.ProjectOnPlane(handPosition-head.position,Vector3.up):Vector3.ProjectOnPlane(handForward,Vector3.up);
  if(away.sqrMagnitude<.01f&&head)away=Vector3.ProjectOnPlane(head.forward,Vector3.up);
  if(away.sqrMagnitude<.01f)away=Vector3.forward;
  away.Normalize();
  float distance=Mathf.Max(.7f,radius+.32f);
  var position=handPosition+away*distance;
  if(head){var separation=Vector3.ProjectOnPlane(position-head.position,Vector3.up).magnitude;float minimum=radius+.55f;if(separation<minimum)position+=away*(minimum-separation);}
  var axis=launchAxis.sqrMagnitude>.001f?launchAxis.normalized:Vector3.up;
  var upright=Quaternion.FromToRotation(homeRotation*axis,Vector3.up)*homeRotation;
  var reference=Vector3.ProjectOnPlane(upright*Vector3.forward,Vector3.up);
  if(reference.sqrMagnitude<.001f)reference=Vector3.ProjectOnPlane(upright*Vector3.right,Vector3.up);
  var yaw=Quaternion.FromToRotation(reference.normalized,away);
  return new Pose(position,yaw*upright);
 }
 void LateUpdate(){
  if(!rocket||rocketLoaded||!rocket.IsHeld||rocket.grab.interactorsSelecting.Count==0)return;
  var interactor=rocket.grab.interactorsSelecting[0];var nearFar=interactor as NearFarInteractor;
  var controller=nearFar!=null?nearFar.interactionAttachController.transformToFollow:interactor.GetAttachTransform(rocket.grab);
  if(!controller)return;
  if(!playerHead){var camera=Camera.main;if(camera)playerHead=camera.transform;}
  var pose=RocketCarryingPose(controller.position,controller.forward,playerHead,rocketLocalLaunchAxis,rocketCarryHomeRotation,rocketCarryRadius);
  rocket.transform.SetPositionAndRotation(pose.position,pose.rotation);
  var body=rocket.GetComponent<Rigidbody>();if(body&&body.isKinematic){body.position=pose.position;body.rotation=pose.rotation;}
 }
 void OnRocketGrab(SelectEnterEventArgs args){
  if(args.interactorObject is NearFarInteractor nearFar){
   var anchor=nearFar.interactionAttachController;var hand=anchor.transformToFollow;
   // The carrying pose already supplies this item's offset; avoid stacking the shared far offset.
   if(hand)anchor.MoveTo(hand.position);
  }
 }
 void BuildPlungerFeedback(){
  var label=new GameObject("Plunger travel feedback");label.transform.SetParent(transform,false);
  plungerFeedback=label.AddComponent<TextMesh>();plungerFeedback.anchor=TextAnchor.MiddleCenter;
  plungerFeedback.alignment=TextAlignment.Center;plungerFeedback.fontSize=44;plungerFeedback.characterSize=.008f;
 }
 void UpdatePlungerFeedback(){
  if(!plungerFeedback)return;
  plungerFeedback.gameObject.SetActive(begun&&!completed);
  plungerFeedback.transform.position=plungerFrame.TransformPoint(plungerHome)+Vector3.up*.22f;
  var camera=Camera.main;if(camera)plungerFeedback.transform.rotation=Quaternion.LookRotation(plungerFeedback.transform.position-camera.transform.position,camera.transform.up);
  plungerFeedback.color=ReadyToPlunge?new Color(.3f,1,.55f):new Color(1,.82f,.45f);
  plungerFeedback.text=AdjustmentHeld?"RELEASE THE ADJUSTED OBJECT":launching?"LAUNCHING":ReadyToPlunge?(plunger.IsHeld?"PUSH DOWN  "+Mathf.RoundToInt(PlungerProgress*100)+"%":"GRIP + PUSH DOWN 12 cm"):(stage==3?"SELECT FINALE":(stage==0?!rocketLoaded:stage==1?!fountainLoaded:!spinnerLoaded)?"LOAD THE FIREWORK FIRST":stage==2?"ADJUST SPINNER SPEED FIRST":"GRIP + MOVE THE LOADED FIREWORK");
 }
 void OnDestroy(){if(rocket&&rocket.grab)rocket.grab.selectEntered.RemoveListener(OnRocketGrab);}
 public override void ResetGame(){StopAllCoroutines();launching=false;if(rocketAdjustment)rocketAdjustment.SetAvailable(false);if(boardAdjustment)boardAdjustment.SetAvailable(false);if(fountainAdjustment)fountainAdjustment.SetAvailable(false);if(targetBoard){targetBoard.StopAllCoroutines();targetBoard.SetHeight(.5f);}rocketRunning=fountainRunning=spinnerRunning=false;base.ResetGame();stage=aimPreset=speedPreset=0;rocketLoaded=fountainLoaded=spinnerLoaded=raised=rocketHandled=fountainHandled=spinnerHandled=triggerHeld=handBaselineSet=wasDown=false;phase=holdProgress=liftAmount=plungerDepression=0;aimValue=.5f;fountainValue=.25f;if(speedSlider)speedSlider.SetValueWithoutNotify(.25f);foreach(var item in new[]{rocket,fountain,spinner,plunger})if(item)item.ResetItem();if(rocket){ShowRocket(true);rocket.transform.position=rocketStaging;}if(lift)lift.localPosition=liftHome;if(rocketPlatform)rocketPlatform.rotation=rocketWorldHome;if(spinnerPivot)spinnerPivot.localRotation=spinnerHome;foreach(var p in new[]{rocketEffect,fountainEffect,spinnerEffect})if(p)p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);UpdatePlungerFeedback();PlacePanel();RefreshControls();}
}
}
