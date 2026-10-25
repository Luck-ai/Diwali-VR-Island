using System;
using UnityEngine;
using UnityEngine.UI;
namespace DiwaliGameplay {
public sealed class DiyaGame : VenueGame {
 public GrabbableItem scroll,pot,key,feather,diya;
 public Transform drawer,lid,screen,scrollRest,potRest,keySocket,featherRest,diyaRest,starter,destination;
 public Transform[] checkpoints;
 public Light carriedFlame,starterFlame,destinationFlame;
 public GameObject dust,guide;
 public Shader dustCoatingShader;
 DiyaTaskFeedback taskFeedback;
 public float CleaningProgress=>clean?1:Mathf.Clamp01((brushPasses+brushTravel/.1f)/3f);
 public Shader originalFlameMaskShader;
 Material originalFlameMask;
 Button drawerButton,chestButton,screenButton,keyButton;
 VenuePanel controls; int panelStep=-1;
 Vector3 controlsAnchor;
 Bounds controlsCourtyard;bool controlsBoundsReady;
 readonly Collider[] controlsObstacles=new Collider[24];
 DiyaRestTarget scrollTarget,potTarget,keyTarget,featherTarget,diyaTarget;
 public string CurrentObjective=>completed?"Festival flame delivered!":Step==3&&keyInserted?"Select Turn Inserted Key on the courtyard controls.":Step==5&&clean?"Release the feather on its marked bench rest.":Step==8?"Carry the lit diya over any one of the three floor tiles. Keep holding it.":Step==9&&transferred?"Flame transferred. Release the diya on its central rest.":Step<Prompts.Length?Prompts[Step]:"Delivery complete";
 public const string HelpText="Open drawer; read and return scroll. Move pot to find key. Insert key at chest, then select Turn Key. Open chest, brush the marked dusty diya, return feather. Reveal the flame, pick up the clean diya marked TAKE THIS DIYA and touch its wick to the starter. The starter and display diyas stay on their tables. Carry the lit diya over any one of the three floor tiles, touch central display, then set diya on its central rest.";
 void HideImportedFlames(){var venue=GameObjectUtility.FindExact("Diya - Approved Warm Palette");if(!venue)return;originalFlameMask=new Material(Shader.Find("Universal Render Pipeline/Lit"));originalFlameMask.name="Modeled white cotton wick";originalFlameMask.SetColor("_BaseColor",new Color(.92f,.88f,.76f));originalFlameMask.SetFloat("_Smoothness",.12f);foreach(var renderer in venue.GetComponentsInChildren<MeshRenderer>(true)){if(renderer.name!="Sphere"&&renderer.name!="Sphere.001"&&renderer.name!="Sphere.002")continue;var materials=renderer.sharedMaterials;if(materials.Length==3){materials[0]=originalFlameMask;renderer.sharedMaterials=materials;}}ReanchorWicks();}
 public static Vector3 WickTip(MeshFilter mf){if(!mf||!mf.sharedMesh)return mf?mf.transform.position:Vector3.zero;var mesh=mf.sharedMesh;if(!mesh.isReadable){var b0=mesh.GetSubMesh(0).bounds;return mf.transform.TransformPoint(b0.center+Vector3.up*b0.extents.y);}var vs=mesh.vertices;var ids=mesh.GetTriangles(0);var b=new Bounds(vs[ids[0]],Vector3.zero);foreach(var id in ids)b.Encapsulate(vs[id]);var size=b.size;var axis=size.x>size.y&&size.x>size.z?Vector3.right:size.y>size.z?Vector3.up:Vector3.forward;var bowlIds=mesh.GetTriangles(1);var bowl=new Bounds(vs[bowlIds[0]],Vector3.zero);foreach(var id in bowlIds)bowl.Encapsulate(vs[id]);if(Vector3.Dot(b.center-bowl.center,axis)<0)axis=-axis;float top=float.NegativeInfinity;foreach(var id in ids)top=Mathf.Max(top,Vector3.Dot(vs[id],axis));Vector3 sum=Vector3.zero;int count=0;foreach(var id in ids)if(Vector3.Dot(vs[id],axis)>top-Mathf.Max(size.x,Mathf.Max(size.y,size.z))*.025f){sum+=vs[id];count++;}return mf.transform.TransformPoint(sum/Mathf.Max(1,count));}
 public void ReanchorWicks(){var venue=GameObjectUtility.FindExact("Diya - Approved Warm Palette");if(!venue)return;var carried=diya.GetComponentInChildren<MeshFilter>();carriedFlame.transform.position=WickTip(carried)+carried.transform.TransformDirection(Vector3.up)*.005f;var start=GameObjectUtility.FindUnder(venue,"Sphere");var end=GameObjectUtility.FindUnder(venue,"Sphere.001");starter.SetParent(transform,true);starter.position=WickTip(start.GetComponent<MeshFilter>());destination.SetParent(transform,true);destination.position=WickTip(end.GetComponent<MeshFilter>());starterFlame.transform.position=starter.position;destinationFlame.transform.position=destination.position;}

 void OnDestroy(){if(originalFlameMask)Destroy(originalFlameMask);}
 public Vector3 panelPosition;public Quaternion panelRotation=Quaternion.identity;
 public Vector3 keyTurnAxis;public Vector3 keyLockPoint;
 void Start(){var carry=diya.GetComponent<DiyaCarryStability>()??diya.gameObject.AddComponent<DiyaCarryStability>();carry.Initialise(diya);if(!GetComponent<DiyaProgressivePath>())gameObject.AddComponent<DiyaProgressivePath>();if(!GetComponent<DiyaOrangeRoute>())gameObject.AddComponent<DiyaOrangeRoute>();HideImportedFlames();taskFeedback=GetComponent<DiyaTaskFeedback>()??gameObject.AddComponent<DiyaTaskFeedback>();taskFeedback.Initialise(this);howToPlay=HelpText;var panel=VenuePanel.Create(this,panelPosition,panelRotation,"Diya courtyard");panel.SetPhysicalSize(1.05f,1.2f);if(checkpoints!=null&&checkpoints.Length>=2)panel.FaceToward((checkpoints[0].position+checkpoints[1].position)*.5f+Vector3.up*1.6f);drawerButton=panel.AddButton("Open Drawer",OpenDrawer);keyButton=panel.AddButton("Turn Inserted Key",TurnKey);chestButton=panel.AddButton("Open Chest",OpenChest);screenButton=panel.AddButton("Reveal Starter Flame",RevealStarter);if(scroll&&!scroll.GetComponent<DiyaScrollComfort>())scroll.gameObject.AddComponent<DiyaScrollComfort>();scrollTarget=DiyaRestTarget.Create(transform,scrollRest,scroll,1f,()=>begun&&!completed&&Step==1);potTarget=DiyaRestTarget.Create(transform,potRest,pot,1f,()=>begun&&!completed&&Step==2);keyTarget=DiyaRestTarget.Create(transform,keySocket,key,.55f,()=>begun&&!completed&&Step==3&&!keyInserted);featherTarget=DiyaRestTarget.Create(transform,featherRest,feather,1f,()=>begun&&!completed&&Step==5&&clean);diyaTarget=DiyaRestTarget.Create(transform,diyaRest,diya,1f,()=>begun&&!completed&&Step==9&&transferred);if(guide){guide.transform.SetParent(transform,true);foreach(var t in guide.GetComponentsInChildren<TextMesh>(true))t.gameObject.SetActive(false);var card=guide.GetComponent<DiyaScrollGuide>()??guide.AddComponent<DiyaScrollGuide>();card.owner=this;}ResetGame();}
 public float proximity=.55f,brushDistance=1.25f;
 public bool inspected,clean,lit,transferred,keyInserted;
 public int nextCheckpoint,brushPasses;
 public float inspectSeconds,lightSeconds;
 Vector3 drawerClosed,screenClosed,brushPrevious; Quaternion lidClosed,keyInsertedRotation;
 float drawerAmount,lidAmount,screenAmount,brushTravel; bool initialised,turningKey,brushContact;
 static readonly string[] Prompts={"Select Open Drawer on the courtyard controls.","Lift the scroll to read your guide, then return it to the drawer.","Move the flowerpot to the marked bench rest to uncover the key.","Release the key over the glowing chest lock target, then select Turn Inserted Key.","Select Open Chest to reveal the feather.","Take the feather, sweep across the dusty diya, then return it to Feather rest.","Select Reveal Starter Flame to slide the screen aside.","Take the clean diya marked TAKE THIS DIYA. Touch its wick to the starter flame.","Carry the lit diya over any one of the three floor tiles. Keep holding it.","Touch the centre display with your diya, then release it on the central rest."};
 public int Step=>credited.Count;
 public void Initialise(){if(initialised)return;drawerClosed=drawer.localPosition;screenClosed=screen.localPosition;lidClosed=lid.localRotation;initialised=true;}
 public override void BeginGame(){ResetGame();base.BeginGame();Prompt();}
 public override void ResetGame(){Initialise();base.ResetGame();StopAllCoroutines();inspected=clean=lit=transferred=keyInserted=turningKey=brushContact=false;nextCheckpoint=brushPasses=0;inspectSeconds=lightSeconds=brushTravel=drawerAmount=lidAmount=screenAmount=0;drawer.localPosition=drawerClosed;screen.localPosition=screenClosed;lid.localRotation=lidClosed;foreach(var item in new[]{scroll,pot,key,feather,diya})if(item)item.ResetItem();if(carriedFlame)carriedFlame.enabled=false;if(starterFlame)starterFlame.enabled=false;if(destinationFlame)destinationFlame.enabled=false;if(dust)dust.SetActive(false);if(taskFeedback)taskFeedback.ResetVisuals();if(guide)guide.SetActive(false);SetAccessible();MarkRest(scrollRest,false);MarkRest(potRest,false);MarkRest(keySocket,false);MarkRest(featherRest,false);MarkRest(diyaRest,false);status="Select Open Drawer to begin.";}
 void SetAccessible(){if(drawerButton)drawerButton.gameObject.SetActive(!begun||Step==0);if(chestButton)chestButton.gameObject.SetActive(begun&&Step==4);if(screenButton)screenButton.gameObject.SetActive(begun&&Step==6);if(keyButton){keyButton.gameObject.SetActive(begun&&Step==3&&keyInserted);keyButton.interactable=!turningKey;}if(!scroll)return;scroll.grab.enabled=begun&&Step==1;pot.grab.enabled=begun&&Step==2;key.grab.enabled=begun&&Step==3&&!keyInserted;feather.grab.enabled=begun&&Step==5;diya.grab.enabled=begun&&Step>=7;PositionControls();}
 void PositionControls(){
  if(!controls)controls=GetComponentInChildren<VenuePanel>();
  if(!controls)return;
  if(panelStep!=Step){
   panelStep=Step;controls.autoFacePlayer=false;
   Transform target=Step<2?drawer:Step==2?pot.transform:Step<=4?keySocket:Step==5?diya.transform:Step<=7?starter:destination;
   var inward=Vector3.ProjectOnPlane(transform.position-target.position,Vector3.up);
   if(inward.sqrMagnitude<.1f)inward=transform.forward;
   inward.Normalize();var side=Vector3.Cross(Vector3.up,inward);
   controls.transform.position=new Vector3(target.position.x,transform.position.y+1.42f,target.position.z)+inward*.8f+side*.85f;
   controls.FaceToward(target.position+inward*2);
   if(Step==5){controls.autoFacePlayer=false;var position=diya.transform.position+inward*.25f+side*1.6f;position.y=transform.position.y+1.55f;controls.transform.position=position;controls.FaceToward(transform.position+Vector3.up*1.6f);}
   if(Step>=8){controls.autoFacePlayer=false;var approach=transform.position+Vector3.up*1.6f;var finalSide=Vector3.ProjectOnPlane(diyaRest.position-destination.position,Vector3.up).normalized;if(finalSide.sqrMagnitude<.1f)finalSide=side;var finalPosition=destination.position-finalSide*1.3f+inward*.2f;finalPosition.y=transform.position.y+1.42f;controls.transform.position=finalPosition;controls.FaceToward(approach);}
   if(Step==6||Step==7){var approach=transform.position+Vector3.up*1.6f;var bounds=GameObjectUtility.BoundsOf(screen);var normal=bounds.size.x<bounds.size.z?Vector3.right:Vector3.forward;var courtyard=transform.position-bounds.center;courtyard.y=0;if(Vector3.Dot(courtyard,normal)<0)normal=-normal;var screenSide=Vector3.Cross(Vector3.up,normal);var extent=Vector3.Dot(bounds.extents,new Vector3(Mathf.Abs(normal.x),0,Mathf.Abs(normal.z)));var p=bounds.center+normal*(extent+.95f)+screenSide*.7f;p.y=transform.position.y+1.42f;controls.transform.position=p;controls.FaceToward(approach+Vector3.up*.1f);}
   if(Step>=2&&Step<=4){var inwardFromChest=Vector3.ProjectOnPlane(transform.position-keySocket.position,Vector3.up).normalized;var chestSide=Vector3.Cross(Vector3.up,inwardFromChest);var position=keySocket.position+inwardFromChest*1.5f-chestSide*2.2f;position.y=transform.position.y+1.42f;controls.transform.position=position;}
   controlsAnchor=ClampControlsInside(controls.transform.position);
   PlaceControlsAtTaskAnchor();
  }
 }
 Vector3 ClampControlsInside(Vector3 point){
  if(!controlsBoundsReady){
   var venue=GameObjectUtility.FindExact("Diya - Approved Warm Palette");var floor=venue?GameObjectUtility.FindUnder(venue,"Plane.002"):null;
   if(floor&&floor.GetComponent<Renderer>())controlsCourtyard=floor.GetComponent<Renderer>().bounds;
   else controlsCourtyard=new Bounds(transform.position,new Vector3(14,0,14));
   controlsBoundsReady=true;
  }
  const float inset=.85f;
  point.x=Mathf.Clamp(point.x,controlsCourtyard.min.x+inset,controlsCourtyard.max.x-inset);
  point.z=Mathf.Clamp(point.z,controlsCourtyard.min.z+inset,controlsCourtyard.max.z-inset);
  return point;
 }
 bool ControlsPositionClear(Vector3 point){
  if((ClampControlsInside(point)-point).sqrMagnitude>.0001f)return false;
  int count=Physics.OverlapBoxNonAlloc(point,new Vector3(.61f,.75f,.61f),controlsObstacles,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
  for(int i=0;i<count;i++){
   var obstacle=controlsObstacles[i];if(!obstacle||!obstacle.enabled)continue;
   if(Camera.main&&obstacle.transform.IsChildOf(Camera.main.transform.root))continue;
   if(obstacle.transform.IsChildOf(controls.transform))continue;
   var item=obstacle.GetComponentInParent<GrabbableItem>();if(item&&item.IsHeld)continue;
   return false;
  }
  return count<controlsObstacles.Length;
 }
 void PlaceControlsAtTaskAnchor(){
  var best=controlsAnchor;float bestScore=float.PositiveInfinity;
  for(int x=-5;x<=5;x++)for(int z=-5;z<=5;z++){
   var candidate=ClampControlsInside(controlsAnchor+new Vector3(x*.4f,0,z*.4f));
   float score=(candidate-controlsAnchor).sqrMagnitude;
   if(score>=bestScore||!ControlsPositionClear(candidate))continue;
   best=candidate;bestScore=score;
  }
  if(float.IsPositiveInfinity(bestScore)){
   for(float x=controlsCourtyard.min.x+.9f;x<controlsCourtyard.max.x-.9f;x+=.8f)
    for(float z=controlsCourtyard.min.z+.9f;z<controlsCourtyard.max.z-.9f;z+=.8f){
     var candidate=new Vector3(x,controlsAnchor.y,z);float score=(candidate-controlsAnchor).sqrMagnitude;
     if(score>=bestScore||!ControlsPositionClear(candidate))continue;
     best=candidate;bestScore=score;
    }
  }
  controls.transform.position=best;
  controls.FaceToward(transform.position+Vector3.up*1.6f);
 }
 void LateUpdate(){PositionControls();}
 void Advance(string id){Credit(id);SetAccessible();Prompt();}
 void Prompt(){status=$"{Step}/10 complete. {CurrentObjective}";}
 public void TurnKey(){if(!begun||Step!=3||!keyInserted||turningKey){Prompt();return;}turningKey=true;SetAccessible();StartCoroutine(TurnInsertedKey());}
 System.Collections.IEnumerator TurnInsertedKey(){var from=key.transform.rotation;var axis=keyTurnAxis.sqrMagnitude>.1f?keyTurnAxis.normalized:-transform.right;var to=Quaternion.AngleAxis(60,axis)*from;float elapsed=0;while(elapsed<.4f){elapsed+=Time.deltaTime;key.transform.rotation=Quaternion.Slerp(from,to,Mathf.Clamp01(elapsed/.4f));yield return null;}turningKey=false;Advance("key");}
 void MarkRest(Transform target,bool active){if(!target)return;foreach(Transform child in target){var text=child.GetComponent<TextMesh>();if(text)text.gameObject.SetActive(false);}}
 public void OpenDrawer(){if(!begun)BeginGame();if(Step!=0)return;Advance("drawer");}
 public void OpenChest(){if(Step!=4){status="Insert and turn the key first.";return;}Advance("chest");StartCoroutine(PresentFeather());}
 System.Collections.IEnumerator PresentFeather(){yield return new WaitForSeconds(.85f);if(Step!=5||feather.IsHeld)yield break;var venue=GameObjectUtility.FindExact("Diya - Approved Warm Palette");var chest=GameObjectUtility.FindUnder(venue,"Cube.001");if(!chest)yield break;var bounds=GameObjectUtility.BoundsOf(chest);var fb=GameObjectUtility.BoundsOf(feather.transform);var front=bounds.size.x<bounds.size.z?Vector3.right:Vector3.forward;if(Vector3.Dot(transform.position-bounds.center,front)<0)front=-front;if(lid&&Vector3.Dot(lid.position-bounds.center,front)>.02f)front=-front;float frontExtent=Vector3.Dot(bounds.extents,new Vector3(Mathf.Abs(front.x),0,Mathf.Abs(front.z)));float featherExtent=Vector3.Dot(fb.extents,new Vector3(Mathf.Abs(front.x),0,Mathf.Abs(front.z)));var target=bounds.center+front*(frontExtent+featherExtent+.12f);target.y=bounds.max.y+.12f+fb.extents.y;var offset=feather.transform.position-fb.center;feather.transform.SetParent(transform,true);var start=feather.transform.position;for(float t=0;t<1;t+=Time.deltaTime*3){if(Step!=5||feather.IsHeld)yield break;feather.transform.position=Vector3.Lerp(start,target+offset,Mathf.SmoothStep(0,1,t));yield return null;}feather.transform.position=target+offset;}

 public void RevealStarter(){if(Step!=6){status="Finish cleaning and return the feather first.";return;}if(starterFlame)starterFlame.enabled=true;Advance("screen");}
 public bool TryCheckpoint(int index,bool held,bool flame){if(Step!=8||!held||!flame||!lit||checkpoints==null||index<0||index>=checkpoints.Length)return false;nextCheckpoint=checkpoints.Length;Advance("route");return true;}
 public bool TryTransfer(bool held,float distance){if(Step!=9||!held||!lit||nextCheckpoint!=3||distance>proximity)return false;transferred=true;if(destinationFlame)destinationFlame.enabled=true;status="Flame transferred. Set your diya on its marked rest.";return true;}
 bool RestReady(GrabbableItem item,Transform target,float radius){var helper=item==scroll?scrollTarget:item==pot?potTarget:item==key?keyTarget:item==feather?featherTarget:diyaTarget;return helper?helper.CanCapture():item.WasGrabbed&&!item.IsHeld&&DiyaRestTarget.Contains(item,target,item==key?.55f:1f);}
 bool Near(GrabbableItem item,Transform target,float radius){return Vector3.Distance(item.transform.position,target.position)<radius;}
 public override void Tick(){if(!begun||completed)return;Initialise();drawerAmount=Mathf.MoveTowards(drawerAmount,Step>=1?1:0,Time.deltaTime*1.3f);drawer.localPosition=drawerClosed+drawer.parent.InverseTransformVector(transform.forward*.45f)*drawerAmount;lidAmount=Mathf.MoveTowards(lidAmount,Step>=5?1:0,Time.deltaTime);lid.localRotation=lidClosed*Quaternion.Euler(0,0,95*lidAmount);screenAmount=Mathf.MoveTowards(screenAmount,Step>=7?1:0,Time.deltaTime);screen.localPosition=screenClosed+screen.parent.InverseTransformVector(transform.right*1.5f)*screenAmount;
 if(Step==1){if(scroll.IsHeld){inspected=true;if(guide)guide.SetActive(true);status="Your scroll shows the whole route. Read it, then return it to the drawer rest.";}if(inspected&&RestReady(scroll,scrollRest,.6f)){scroll.SnapTo(scrollRest);if(guide)guide.SetActive(false);Advance("scroll");}}
 if(Step==2&&RestReady(pot,potRest,.6f)){pot.SnapTo(potRest);Advance("pot");}
 if(Step==3&&!keyInserted&&RestReady(key,keySocket,.55f)){keyInserted=true;key.SnapTo(keySocket,true);SetAccessible();status="Key fitted. Select Turn Inserted Key on the courtyard controls.";}
 if(Step==5){var collider=feather.GetComponent<Collider>();var touch=collider?collider.ClosestPoint(diya.transform.position):feather.transform.position;bool touching=!clean&&feather.IsHeld&&Vector3.Distance(touch,diya.transform.position)<brushDistance;if(touching){float d=brushContact?Vector3.Distance(feather.transform.position,brushPrevious):0;brushTravel+=Mathf.Min(d,.2f);if(taskFeedback)taskFeedback.Brush(touch,d);if(brushTravel>=.1f){brushTravel=0;brushPasses++;status=$"Cleaning {Mathf.Min(brushPasses,3)}/3. Sweep the feather across the diya.";}if(brushPasses>=3){clean=true;status="Diya clean. Release the feather on its marked bench rest.";}}brushContact=touching;brushPrevious=feather.transform.position;if(clean&&RestReady(feather,featherRest,.65f)){feather.SnapTo(featherRest);Advance("clean");}}
 if(Step==7&&diya.IsHeld&&clean&&Vector3.Distance(carriedFlame.transform.position,starter.position)<=proximity){lit=true;if(carriedFlame)carriedFlame.enabled=true;Advance("light");}
 MarkRest(scrollRest,Step==1);MarkRest(potRest,Step==2);MarkRest(keySocket,Step==3);MarkRest(featherRest,Step==5&&clean);MarkRest(diyaRest,Step==9&&transferred);
 for(int i=0;i<checkpoints.Length;i++){var label=checkpoints[i].GetComponentInChildren<TextMesh>();if(label)label.color=nextCheckpoint>0?new Color(.5f,1,.6f):Step==8?Color.white:new Color(1,.7f,.2f);}
 if(Step==8&&diya.IsHeld&&lit){for(int i=0;i<checkpoints.Length;i++){var target=checkpoints[i].position;var d=diya.transform.position-target;d.y=0;var camera=Camera.main;var player=camera?camera.transform.position-target:d;player.y=0;if(d.magnitude<=.8f||player.magnitude<=.8f){TryCheckpoint(i,true,true);break;}}}
 if(Step==9){TryTransfer(diya.IsHeld,Vector3.Distance(carriedFlame.transform.position,destination.position));if(transferred&&RestReady(diya,diyaRest,.65f)){diya.SnapTo(diyaRest);Advance("delivery");Complete("Festival flame delivered. 10/10 tasks complete.");}}

 }
}
}
