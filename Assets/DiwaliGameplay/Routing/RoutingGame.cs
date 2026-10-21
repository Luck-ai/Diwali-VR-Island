using UnityEngine;
namespace DiwaliGameplay {
public class RoutingGame : VenueGame {
 public Vector3 panelPosition; public Quaternion panelRotation;
 UnityEngine.UI.Button[] rotateButtons=new UnityEngine.UI.Button[3];
 TextMesh[] connectionLabels=new TextMesh[4];
 LineRenderer[] directionArrows=new LineRenderer[3];
 bool[] poseDirty=new bool[3];
 Vector3 stationPanelPosition,stationViewer,gardenPanelPosition,gardenViewer;bool gardenControls;
 UnityEngine.UI.Button beginControl,testControl,confirmControl;UnityEngine.UI.Slider brightnessControl;VenuePanel panel;
 void Start(){
  howToPlay="1  Select Begin Lighting. Grab the crystal and release it into the crystal holder.\n\n2  Take the elbow, T-junction and straight piece from the rack. Release each one over its matching circular recess.\n\n3  After fitting a piece, select its Rotate button. Each press makes one visible quarter-turn. Keep turning until the label reads CONNECTED. Pieces may be fitted in any order.\n\n4  Select Test Lights after all three connections are aligned.\n\n5  Adjust Garden brightness, then Confirm brightness to finish. Restart returns the pieces to the rack.";
  var venue=GameObjectUtility.FindExact("LanternRouting - Approved Warm Palette");
  var console=venue?GameObjectUtility.FindUnder(venue,"base"):null;
  Vector3 front=Vector3.ProjectOnPlane(socketPlaneNormal,Vector3.up).normalized;
  if(front.sqrMagnitude<.01f)front=Vector3.forward;
  if(console){
   var b=GameObjectUtility.BoundsOf(console);
   var side=Vector3.Cross(Vector3.up,front);
   float half=Vector3.Dot(new Vector3(Mathf.Abs(side.x),0,Mathf.Abs(side.z)),b.extents);
   panelPosition=b.center+side*(half+.65f)+front*.65f;
   var floor=GameObjectUtility.FindUnder(venue,"Plane");
   panelPosition.y=(floor?GameObjectUtility.BoundsOf(floor).max.y:0)+1.55f;
  }
  var p=VenuePanel.Create(this,panelPosition,panelRotation,"Route the Festival Lights");panel=p;
  p.SetPhysicalSize(.98f,1.28f);p.FaceToward(panelPosition+front*2);stationPanelPosition=panelPosition;stationViewer=panelPosition+front*2;gardenPanelPosition=panelPosition;gardenViewer=stationViewer;if(destinations!=null&&destinations.Length>0){var center=Vector3.zero;foreach(var light in destinations)center+=light.transform.position;center/=destinations.Length;gardenPanelPosition=Vector3.Lerp(panelPosition,center,.25f);gardenPanelPosition.y=panelPosition.y;gardenViewer=console?GameObjectUtility.BoundsOf(console).center+front*2.3f:stationViewer;gardenViewer.y=panelPosition.y;}
  beginControl=p.AddButton("Begin Lighting",BeginGame);
  rotateButtons[0]=p.AddButton("Rotate elbow ↻",()=>RotatePiece(0));
  rotateButtons[1]=p.AddButton("Rotate T-junction ↻",()=>RotatePiece(1));
  rotateButtons[2]=p.AddButton("Rotate straight ↻",()=>RotatePiece(2));
  testControl=p.AddButton("Test Lights",TestLights);brightnessControl=p.AddSlider("Garden brightness",.65f,SetBrightness);confirmControl=p.AddButton("Confirm brightness",ConfirmBrightness);
  for(int i=0;i<4;i++){
   int k=i;
   var marker=new GameObject("Connection status");marker.transform.SetParent(sockets[i].transform,false);
   marker.transform.position=sockets[i].transform.position+Vector3.up*.3f+front*.1f;
   marker.transform.rotation=p.transform.rotation;
   var text=marker.AddComponent<TextMesh>();text.characterSize=.007f;text.fontSize=48;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(1,.84f,.54f);connectionLabels[i]=text;
   sockets[i].canAccept=x=>begun&&!completed&&(k==0||placed[0]);sockets[i].onSnapped=x=>Place(k,x);
   if(i>0){var arrow=new GameObject("Quarter turn indicator").AddComponent<LineRenderer>();arrow.transform.SetParent(transform,false);arrow.useWorldSpace=true;arrow.positionCount=4;arrow.startWidth=arrow.endWidth=.006f;var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.SetColor("_BaseColor",new Color(1,.76f,.3f));arrow.material=mat;directionArrows[i-1]=arrow;}
  }
 }
 public override void Tick(){
  for(int i=0;i<3;i++){
   if(poseDirty[i]){if(placed[i+1])ApplyOrientation(i);poseDirty[i]=false;}
   if(rotateButtons[i]){rotateButtons[i].gameObject.SetActive(begun&&!completed&&placed[i+1]&&!Aligned(i));rotateButtons[i].interactable=begun&&!completed&&placed[i+1];var label=rotateButtons[i].GetComponentInChildren<UnityEngine.UI.Text>();label.text=(i==0?"Elbow":i==1?"T-junction":"Straight")+"  |  Turn 90 degrees  |  "+orientation[i]*90+" deg";}
   if(connectionLabels[i+1])connectionLabels[i+1].text=(i==0?"3 ELBOW":i==1?"2 T":"1 STRAIGHT")+"\n"+(Aligned(i)?"READY":placed[i+1]?"TURN":"DROP HERE");
   if(directionArrows[i]){directionArrows[i].enabled=placed[i+1];var n=socketPlaneNormal.normalized;var forward=Vector3.ProjectOnPlane(Vector3.up,n).normalized;if(forward.sqrMagnitude<.01f)forward=Vector3.forward;forward=Quaternion.AngleAxis(orientation[i]*90,n)*forward;var side=Vector3.Cross(n,forward);var c=sockets[i+1].transform.position+n*.12f;var tip=c+forward*.11f;directionArrows[i].SetPositions(new[]{c,tip,tip-forward*.035f+side*.025f,tip});}
  }
  if(panel){bool wantsGarden=tested||completed;if(wantsGarden!=gardenControls){gardenControls=wantsGarden;panel.FollowTaskAt(gardenControls?gardenPanelPosition:stationPanelPosition,gardenControls?gardenViewer:stationViewer);}bool aligned=Aligned(0)&&Aligned(1)&&Aligned(2);beginControl.gameObject.SetActive(!begun||completed);testControl.gameObject.SetActive(begun&&!completed&&aligned&&!tested);panel.SetControlVisible(brightnessControl,begun&&!completed&&tested);confirmControl.gameObject.SetActive(begun&&!completed&&tested&&brightnessChosen);if(begun&&!completed){int pending=-1;for(int j=0;j<3;j++)if(placed[j+1]&&!Aligned(j)){pending=j;break;}status=!placed[0]?"Grab the crystal and release it into its holder.":pending>=0?"Rotate the "+(pending==0?"elbow":pending==1?"T-junction":"straight piece")+" until its label reads CONNECTED.":!aligned?"Fit any remaining routing piece into its circular recess.":!tested?"Select Test Lights to illuminate the garden.":!brightnessChosen?"Adjust Garden brightness.":"Confirm brightness to finish the garden.";}}
  if(connectionLabels[0])connectionLabels[0].text=placed[0]?"POWER\nREADY":"POWER\nDROP CRYSTAL";
 }
 void ApplyOrientation(int i){var rotation=Quaternion.AngleAxis(orientation[i]*90,socketPlaneNormal)*sockets[i+1].transform.rotation;var rb=routePivots[i].GetComponent<Rigidbody>();if(rb)rb.rotation=rotation;routePivots[i].rotation=rotation;var position=routePivots[i].position+sockets[i+1].transform.position-GameObjectUtility.BoundsOf(routePivots[i]).center;if(rb)rb.position=position;routePivots[i].position=position;}

 public Vector3 socketPlaneNormal=Vector3.up;
 public GrabbableItem[] pieces; public SnapZone[] sockets; public Light[] destinations; public Transform[] routePivots;
 public int[] orientation=new int[3]; public int[] targetMasks={3,11,5}; public bool[] placed=new bool[4]; public float brightness=.65f; public bool tested,brightnessChosen;
 public override void BeginGame(){ResetGame();base.BeginGame();Credit("begin");status="Place crystal. Fit elbow, T-junction and straight piece, then rotate to arrows.";UpdateLights();}
 public override void ResetGame(){StopAllCoroutines();base.ResetGame();placed=new bool[4];orientation=new int[3];poseDirty=new bool[3];tested=brightnessChosen=false;brightness=.65f;if(pieces!=null)foreach(var p in pieces)p.ResetItem();if(sockets!=null)foreach(var s in sockets)s.occupant=null;UpdateLights();}
 public void Place(int index,GrabbableItem item){if(!begun||completed||index<0||index>=pieces.Length||item!=pieces[index]||placed[index])return;placed[index]=true;item.SnapTo(sockets[index].transform,true);Credit("place"+index);if(index>0){orientation[index-1]=1;routePivots[index-1].rotation=Quaternion.AngleAxis(90,socketPlaneNormal)*sockets[index].transform.rotation;ApplyOrientation(index-1);poseDirty[index-1]=true;}status=index==0?"Source powered. Place routing pieces.":"Piece fitted. Rotate until the connection arrows align.";UpdateLights();}
 public int ConnectionMask(int index){int mask=targetMasks[index];for(int k=0;k<orientation[index];k++)mask=((mask<<1)&15)|((mask>>3)&1);return mask;}
 public bool Aligned(int index)=>placed[index+1]&&ConnectionMask(index)==targetMasks[index];
 public void RotatePiece(int index){if(!begun||completed||!placed[index+1]){status="Place this routing piece first.";return;}orientation[index]=(orientation[index]+1)%4;ApplyOrientation(index);poseDirty[index]=true;if(Aligned(index))Credit("align"+index);tested=false;status=(index==0?"Elbow":index==1?"T-junction":"Straight")+" turned to "+orientation[index]*90+" degrees. "+(Aligned(index)?"CONNECTED. Test when all three labels read CONNECTED.":"Connection open. Select this Rotate button again.");UpdateLights();}
 public void TestLights(){if(!begun||completed)return;for(int i=0;i<3;i++)if(!Aligned(i)||!placed[0]){status="Broken connection: "+(i==0?"elbow":i==1?"T-junction":"straight")+". Match the arrows.";return;}tested=true;StartCoroutine(RoutePulse());Credit("test");status="All three destinations connected. Adjust Garden Brightness and confirm.";UpdateLights();}
 public void SetBrightness(float value){if(!begun||completed||!tested)return;brightness=Mathf.Clamp(value,.25f,1);brightnessChosen=true;UpdateLights();status="Garden brightness: "+Mathf.RoundToInt(brightness*100)+"%. Confirm to finish.";}
 public void ConfirmBrightness(){if(!begun||completed||!tested||!brightnessChosen){status="Test all connections, then adjust brightness first.";return;}Credit("brightness");Complete("Garden complete! All three lantern destinations glow.");}
 System.Collections.IEnumerator RoutePulse(){for(int i=0;i<destinations.Length;i++){destinations[i].intensity=5;yield return new WaitForSeconds(.3f);UpdateLights();}}
 void UpdateLights(){if(destinations==null)return;for(int i=0;i<destinations.Length;i++)destinations[i].intensity=begun&&placed[0]&&Aligned(i)?brightness*3:.03f;}
}
}
