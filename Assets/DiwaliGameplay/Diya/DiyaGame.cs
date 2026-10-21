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
 public Shader originalFlameMaskShader;
 Material originalFlameMask;
 Button drawerButton,chestButton,screenButton,keyButton;
 VenuePanel controls; int panelStep=-1;
 public string CurrentObjective=>completed?"Festival flame delivered!":Step==3&&keyInserted?"Select Turn Inserted Key on the courtyard controls.":Step==5&&clean?"Release the feather on its marked bench rest.":Step==8?$"Carry the lit diya over floor tile {nextCheckpoint+1}. Keep holding it.":Step==9&&transferred?"Flame transferred. Release the diya on its central rest.":Step<Prompts.Length?Prompts[Step]:"Delivery complete";
 public const string HelpText="Open drawer; read and return scroll. Move pot to find key. Insert key at chest, then select Turn Key. Open chest, brush dusty diya, return feather. Reveal flame and touch the clean diya to the starter. Carry through tiles 1 > 2 > 3, touch central display, then set diya on its central rest.";
 void HideImportedFlames(){var venue=GameObjectUtility.FindExact("Diya - Approved Warm Palette");if(!venue)return;var shader=originalFlameMaskShader?originalFlameMaskShader:Shader.Find("Diwali/Hide Original Flame");if(!shader){Debug.LogError("Diya original flame mask shader missing.",this);return;}originalFlameMask=new Material(shader);foreach(var renderer in venue.GetComponentsInChildren<MeshRenderer>(true)){if(renderer.name!="Sphere"&&renderer.name!="Sphere.001"&&renderer.name!="Sphere.002")continue;var materials=renderer.sharedMaterials;if(materials.Length==3){materials[0]=originalFlameMask;renderer.sharedMaterials=materials;}}}
 void OnDestroy(){if(originalFlameMask)Destroy(originalFlameMask);}
 public Vector3 panelPosition;public Quaternion panelRotation=Quaternion.identity;
 public Vector3 keyTurnAxis;public Vector3 keyLockPoint;
 void Start(){HideImportedFlames();howToPlay=HelpText;var panel=VenuePanel.Create(this,panelPosition,panelRotation,"Diya courtyard");panel.SetPhysicalSize(1.05f,1.2f);if(checkpoints!=null&&checkpoints.Length>=2)panel.FaceToward((checkpoints[0].position+checkpoints[1].position)*.5f+Vector3.up*1.6f);drawerButton=panel.AddButton("Open Drawer",OpenDrawer);keyButton=panel.AddButton("Turn Inserted Key",TurnKey);chestButton=panel.AddButton("Open Chest",OpenChest);screenButton=panel.AddButton("Reveal Starter Flame",RevealStarter);if(scroll&&!scroll.GetComponent<DiyaScrollComfort>())scroll.gameObject.AddComponent<DiyaScrollComfort>();DiyaRestTarget.Create(transform,scrollRest,scroll,.6f,()=>begun&&!completed&&Step==1);DiyaRestTarget.Create(transform,potRest,pot,.6f,()=>begun&&!completed&&Step==2);DiyaRestTarget.Create(transform,keySocket,key,.55f,()=>begun&&!completed&&Step==3&&!keyInserted);DiyaRestTarget.Create(transform,featherRest,feather,.65f,()=>begun&&!completed&&Step==5&&clean);DiyaRestTarget.Create(transform,diyaRest,diya,.65f,()=>begun&&!completed&&Step==9&&transferred);if(guide){guide.transform.SetParent(transform,true);foreach(var t in guide.GetComponentsInChildren<TextMesh>(true))t.gameObject.SetActive(false);var card=guide.GetComponent<DiyaScrollGuide>()??guide.AddComponent<DiyaScrollGuide>();card.owner=this;}ResetGame();}
 public float proximity=.55f,brushDistance=.55f;
 public bool inspected,clean,lit,transferred,keyInserted;
 public int nextCheckpoint,brushPasses;
 public float inspectSeconds,lightSeconds;
 Vector3 drawerClosed,screenClosed,brushPrevious; Quaternion lidClosed,keyInsertedRotation;
 float drawerAmount,lidAmount,screenAmount,brushTravel; bool initialised,turningKey;
 static readonly string[] Prompts={"Select Open Drawer on the courtyard controls.","Lift the scroll to read your guide, then return it to the drawer.","Move the flowerpot to the marked bench rest to uncover the key.","Release the key over the glowing chest lock target, then select Turn Inserted Key.","Select Open Chest to reveal the feather.","Take the feather, sweep across the dusty diya, then return it to Feather rest.","Select Reveal Starter Flame to slide the screen aside.","Pick up the clean diya and touch it to the starter behind the screen.","Carry the lit diya over floor tiles 1, then 2, then 3. Keep holding it.","Touch the centre display with your diya, then release it on the central rest."};
 public int Step=>credited.Count;
 public void Initialise(){if(initialised)return;drawerClosed=drawer.localPosition;screenClosed=screen.localPosition;lidClosed=lid.localRotation;initialised=true;}
 public override void BeginGame(){ResetGame();base.BeginGame();Prompt();}
 public override void ResetGame(){Initialise();base.ResetGame();StopAllCoroutines();inspected=clean=lit=transferred=keyInserted=turningKey=false;nextCheckpoint=brushPasses=0;inspectSeconds=lightSeconds=brushTravel=drawerAmount=lidAmount=screenAmount=0;drawer.localPosition=drawerClosed;screen.localPosition=screenClosed;lid.localRotation=lidClosed;foreach(var item in new[]{scroll,pot,key,feather,diya})if(item)item.ResetItem();if(carriedFlame)carriedFlame.enabled=false;if(starterFlame)starterFlame.enabled=false;if(destinationFlame)destinationFlame.enabled=false;if(dust)dust.SetActive(false);if(guide)guide.SetActive(false);SetAccessible();MarkRest(scrollRest,false);MarkRest(potRest,false);MarkRest(keySocket,false);MarkRest(featherRest,false);MarkRest(diyaRest,false);status="Select Open Drawer to begin.";}
 void SetAccessible(){if(drawerButton)drawerButton.gameObject.SetActive(!begun||Step==0);if(chestButton)chestButton.gameObject.SetActive(begun&&Step==4);if(screenButton)screenButton.gameObject.SetActive(begun&&Step==6);if(keyButton){keyButton.gameObject.SetActive(begun&&Step==3&&keyInserted);keyButton.interactable=!turningKey;}if(dust)dust.SetActive(begun&&Step==5&&!clean);if(!scroll)return;scroll.grab.enabled=begun&&Step==1;pot.grab.enabled=begun&&Step==2;key.grab.enabled=begun&&Step==3&&!keyInserted;feather.grab.enabled=begun&&Step==5;diya.grab.enabled=begun&&Step>=7;PositionControls();}
 void PositionControls(){if(!controls)controls=GetComponentInChildren<VenuePanel>();if(!controls||panelStep==Step)return;panelStep=Step;Transform target=Step<2?drawer:Step==2?pot.transform:Step<=4?keySocket:Step==5?diya.transform:Step<=7?starter:destination;var inward=transform.position-target.position;inward.y=0;if(inward.sqrMagnitude<.1f)inward=transform.forward;inward.Normalize();var side=Vector3.Cross(Vector3.up,inward);controls.transform.position=new Vector3(target.position.x,transform.position.y+1.42f,target.position.z)+inward*.8f+side*.85f;controls.FaceToward(target.position+inward*2);}
 void Advance(string id){Credit(id);SetAccessible();Prompt();}
 void Prompt(){status=$"{Step}/10 complete. {CurrentObjective}";}
 public void TurnKey(){if(!begun||Step!=3||!keyInserted||turningKey){Prompt();return;}turningKey=true;SetAccessible();StartCoroutine(TurnInsertedKey());}
 System.Collections.IEnumerator TurnInsertedKey(){var from=key.transform.rotation;var axis=keyTurnAxis.sqrMagnitude>.1f?keyTurnAxis.normalized:-transform.right;var to=Quaternion.AngleAxis(60,axis)*from;float elapsed=0;while(elapsed<.4f){elapsed+=Time.deltaTime;key.transform.rotation=Quaternion.Slerp(from,to,Mathf.Clamp01(elapsed/.4f));yield return null;}turningKey=false;Advance("key");}
 void MarkRest(Transform target,bool active){if(!target)return;foreach(Transform child in target){var text=child.GetComponent<TextMesh>();if(text)text.gameObject.SetActive(false);}}
 public void OpenDrawer(){if(!begun)BeginGame();if(Step!=0)return;Advance("drawer");}
 public void OpenChest(){if(Step!=4){status="Insert and turn the key first.";return;}Advance("chest");}
 public void RevealStarter(){if(Step!=6){status="Finish cleaning and return the feather first.";return;}if(starterFlame)starterFlame.enabled=true;Advance("screen");}
 public bool TryCheckpoint(int index,bool held,bool flame){if(Step!=8||!held||!flame||index!=nextCheckpoint)return false;nextCheckpoint++;if(nextCheckpoint==3)Advance("route");else status=$"8/10  Route {nextCheckpoint}/3. Follow tile {nextCheckpoint+1}.";return true;}
 public bool TryTransfer(bool held,float distance){if(Step!=9||!held||!lit||nextCheckpoint!=3||distance>proximity)return false;transferred=true;if(destinationFlame)destinationFlame.enabled=true;status="Flame transferred. Set your diya on its marked rest.";return true;}
 bool RestReady(GrabbableItem item,Transform target,float radius){return item.WasGrabbed&&!item.IsHeld&&DiyaRestTarget.Contains(item,target,radius);}
 bool Near(GrabbableItem item,Transform target,float radius){return Vector3.Distance(item.transform.position,target.position)<radius;}
 public override void Tick(){if(!begun||completed)return;Initialise();drawerAmount=Mathf.MoveTowards(drawerAmount,Step>=1?1:0,Time.deltaTime*1.3f);drawer.localPosition=drawerClosed+drawer.parent.InverseTransformVector(transform.forward*.45f)*drawerAmount;lidAmount=Mathf.MoveTowards(lidAmount,Step>=5?1:0,Time.deltaTime);lid.localRotation=lidClosed*Quaternion.Euler(0,0,95*lidAmount);screenAmount=Mathf.MoveTowards(screenAmount,Step>=7?1:0,Time.deltaTime);screen.localPosition=screenClosed+screen.parent.InverseTransformVector(transform.right*1.5f)*screenAmount;
 if(Step==1){if(scroll.IsHeld){inspected=true;if(guide)guide.SetActive(true);status="Your scroll shows the whole route. Read it, then return it to the drawer rest.";}if(inspected&&RestReady(scroll,scrollRest,.6f)){scroll.SnapTo(scrollRest);Advance("scroll");}}
 if(Step==2&&RestReady(pot,potRest,.6f)){pot.SnapTo(potRest);Advance("pot");}
 if(Step==3&&!keyInserted&&RestReady(key,keySocket,.55f)){keyInserted=true;key.SnapTo(keySocket,true);SetAccessible();status="Key fitted. Select Turn Inserted Key on the courtyard controls.";}
 if(Step==5){var collider=feather.GetComponent<Collider>();var touch=collider?collider.ClosestPoint(diya.transform.position):feather.transform.position;if(feather.IsHeld&&Vector3.Distance(touch,diya.transform.position)<brushDistance){float d=Vector3.Distance(feather.transform.position,brushPrevious);brushTravel+=Mathf.Min(d,.2f);if(brushTravel>=.1f){brushTravel=0;brushPasses++;status=$"Cleaning {Mathf.Min(brushPasses,3)}/3. Sweep the feather across the diya.";}if(brushPasses>=3){clean=true;if(dust)dust.SetActive(false);status="Diya clean. Release the feather on its marked bench rest.";}}brushPrevious=feather.transform.position;if(clean&&RestReady(feather,featherRest,.65f)){feather.SnapTo(featherRest);Advance("clean");}}
 if(Step==7&&diya.IsHeld&&clean&&Near(diya,starter,proximity)){lit=true;if(carriedFlame)carriedFlame.enabled=true;Advance("light");}
 MarkRest(scrollRest,Step==1);MarkRest(potRest,Step==2);MarkRest(keySocket,Step==3);MarkRest(featherRest,Step==5&&clean);MarkRest(diyaRest,Step==9&&transferred);
 for(int i=0;i<checkpoints.Length;i++){var label=checkpoints[i].GetComponentInChildren<TextMesh>();if(label)label.color=i<nextCheckpoint?new Color(.5f,1,.6f):i==nextCheckpoint&&Step==8?Color.white:new Color(1,.7f,.2f);}
 if(Step==8&&diya.IsHeld&&lit){for(int i=0;i<checkpoints.Length;i++){var d=diya.transform.position-checkpoints[i].position;d.y=0;if(d.magnitude<.8f)TryCheckpoint(i,true,true);}}
 if(Step==9){TryTransfer(diya.IsHeld,Vector3.Distance(diya.transform.position,destination.position));if(transferred&&RestReady(diya,diyaRest,.65f)){diya.SnapTo(diyaRest);Advance("delivery");Complete("Festival flame delivered. 10/10 tasks complete.");}}

 }
}
}
