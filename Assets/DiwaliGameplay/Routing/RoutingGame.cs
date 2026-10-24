using UnityEngine;
namespace DiwaliGameplay {
public class RoutingGame : VenueGame {
 public Vector3 panelPosition; public Quaternion panelRotation;
 UnityEngine.UI.Button[] rotateButtons=new UnityEngine.UI.Button[3];
 TextMesh[] connectionLabels=new TextMesh[4];
 LineRenderer[] directionArrows=new LineRenderer[3];
 bool[] poseDirty=new bool[3];
 bool testInProgress;float[] flashUntil=new float[3];int[] lastTest=new[]{-1,-1,-1};public Shader pulseShader;LineRenderer ray,rayGlow;Renderer[][] lampRenderers;readonly System.Collections.Generic.List<LineRenderer> testTraces=new System.Collections.Generic.List<LineRenderer>();
 void ClearTraces(){foreach(var line in testTraces)if(line){Destroy(line.sharedMaterial);Destroy(line.gameObject);}testTraces.Clear();}
 Vector3 stationPanelPosition,stationViewer,gardenPanelPosition,gardenViewer;bool gardenControls;
 UnityEngine.UI.Button beginControl,testControl,confirmControl,brightnessControl;VenuePanel panel;
 void Start(){
  howToPlay="1  Select Begin Lighting. Grab the crystal and release it into the crystal holder.\n\n2  Take the elbow, T-junction and straight piece from the rack. Release each one over its matching circular recess.\n\n3  After fitting a piece, select its Rotate button. Each press makes one visible quarter-turn. Select Test Lights to check the route, then rotate and test again if needed. Pieces may be fitted in any order.\n\n4  Select Test Lights after all three connections are aligned.\n\n5  Tap Change garden brightness to choose a gentle, warm or bright glow, then Confirm brightness to finish. Restart returns the pieces to the rack.";
  var venue=GameObjectUtility.FindExact("LanternRouting - Approved Warm Palette");
  var console=venue?GameObjectUtility.FindUnder(venue,"base"):null;
  Vector3 front=Vector3.ProjectOnPlane(socketPlaneNormal,Vector3.up).normalized;
  if(front.sqrMagnitude<.01f)front=Vector3.forward;
  if(console){
   var b=GameObjectUtility.BoundsOf(console);
   var side=Vector3.Cross(Vector3.up,front);
   float half=Vector3.Dot(new Vector3(Mathf.Abs(side.x),0,Mathf.Abs(side.z)),b.extents);
   float frontHalf=Vector3.Dot(new Vector3(Mathf.Abs(front.x),0,Mathf.Abs(front.z)),b.extents);
   panelPosition=b.center+side*(half+.95f)+front*(frontHalf+.55f);
   var floor=GameObjectUtility.FindUnder(venue,"Plane");
   panelPosition.y=(floor?GameObjectUtility.BoundsOf(floor).max.y:0)+1.55f;
  }
  var p=VenuePanel.Create(this,panelPosition,panelRotation,"Route the Festival Lights");panel=p;
  p.SetPhysicalSize(.98f,1.28f);p.FaceToward(panelPosition+front*2);p.autoFacePlayer=false;stationPanelPosition=panelPosition;stationViewer=panelPosition+front*2;gardenPanelPosition=stationPanelPosition;gardenViewer=stationViewer;
  beginControl=p.AddButton("Begin Lighting",BeginGame);
  rotateButtons[0]=p.AddButton("Rotate elbow ↻",()=>RotatePiece(0));
  rotateButtons[1]=p.AddButton("Rotate T-junction ↻",()=>RotatePiece(1));
  rotateButtons[2]=p.AddButton("Rotate straight ↻",()=>RotatePiece(2));
  testControl=p.AddButton("Test Lights",TestLights);brightnessControl=p.AddButton("Change garden brightness",()=>SetBrightness(brightness<.7f?.85f:brightness<.95f?1f:.5f));confirmControl=p.AddButton("Confirm brightness",ConfirmBrightness);
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
   if(rotateButtons[i]){rotateButtons[i].gameObject.SetActive(begun&&!completed&&placed[i+1]);rotateButtons[i].interactable=begun&&!completed&&placed[i+1]&&!testInProgress;var label=rotateButtons[i].GetComponentInChildren<UnityEngine.UI.Text>();label.text=(i==0?"Elbow":i==1?"T-junction":"Straight")+"  |  Turn 90 degrees  |  "+orientation[i]*90+" deg";}
   if(connectionLabels[i+1])connectionLabels[i+1].text=(i==0?"3 ELBOW":i==1?"2 T":"1 STRAIGHT")+"\n"+(placed[i+1]?(lastTest[i]==orientation[i]?(Aligned(i)?"READY":"TURN"):"TEST TO CHECK"):"DROP HERE");
   if(directionArrows[i]){directionArrows[i].enabled=placed[i+1];var n=socketPlaneNormal.normalized;var forward=Vector3.ProjectOnPlane(Vector3.up,n).normalized;if(forward.sqrMagnitude<.01f)forward=Vector3.forward;forward=Quaternion.AngleAxis(orientation[i]*90+(i==2?90:180),n)*forward;var side=Vector3.Cross(n,forward);var c=sockets[i+1].transform.position+n*.12f;var tip=c+forward*.11f;directionArrows[i].SetPositions(new[]{c,tip,tip-forward*.035f+side*.025f,tip});}
  }
  if(panel){bool wantsGarden=tested||completed;if(wantsGarden!=gardenControls){gardenControls=wantsGarden;panel.FollowTaskAt(gardenControls?gardenPanelPosition:stationPanelPosition,gardenControls?gardenViewer:stationViewer);}beginControl.gameObject.SetActive(!begun||completed);testControl.gameObject.SetActive(begun&&!completed&&placed[0]&&placed[1]&&placed[2]&&placed[3]);brightnessControl.gameObject.SetActive(begun&&!completed&&tested);brightnessControl.GetComponentInChildren<UnityEngine.UI.Text>().text="Garden brightness: "+Mathf.RoundToInt(brightness*100)+"%  |  Tap to change";confirmControl.gameObject.SetActive(begun&&!completed&&tested&&brightnessChosen);if(begun&&!completed&&!testInProgress){int pending=-1;for(int j=0;j<3;j++)if(placed[j+1]&&lastTest[j]==orientation[j]&&!Aligned(j)){pending=j;break;}status=!placed[0]?"Grab the crystal and release it into its holder.":pending>=0?"Rotate the "+(pending==0?"elbow":pending==1?"T-junction":"straight piece")+" and select Test Lights to check. Repeat until connected.":!placed[1]||!placed[2]||!placed[3]?"Fit the remaining routing pieces into their circular recesses.":!tested?"Select Test Lights to illuminate the garden.":!brightnessChosen?"Adjust Garden brightness.":"Confirm brightness to finish the garden.";}}
  UpdateLights();
  if(connectionLabels[0])connectionLabels[0].text=placed[0]?"POWER\nREADY":"POWER\nDROP CRYSTAL";
 }
 void ApplyOrientation(int i){var rotation=Quaternion.AngleAxis(orientation[i]*90,socketPlaneNormal)*sockets[i+1].transform.rotation;var rb=routePivots[i].GetComponent<Rigidbody>();if(rb)rb.rotation=rotation;routePivots[i].rotation=rotation;var position=routePivots[i].position+sockets[i+1].transform.position-GameObjectUtility.BoundsOf(routePivots[i]).center;if(rb)rb.position=position;routePivots[i].position=position;}

 public Vector3 socketPlaneNormal=Vector3.up;
 public GrabbableItem[] pieces; public SnapZone[] sockets; public Light[] destinations; public Transform[] routePivots;
 public int[] orientation=new int[3]; public int[] targetMasks={3,11,10}; public bool[] placed=new bool[4]; public float brightness=.65f; public bool tested,brightnessChosen;
 public override void BeginGame(){ResetGame();base.BeginGame();Credit("begin");status="Place crystal. Fit elbow, T-junction and straight piece, then rotate to arrows.";UpdateLights();}
 public override void ResetGame(){StopAllCoroutines();ClearTraces();base.ResetGame();placed=new bool[4];orientation=new int[3];flashUntil=new float[3];lastTest=new[]{-1,-1,-1};if(ray)ray.enabled=false;if(rayGlow)rayGlow.enabled=false;poseDirty=new bool[3];testInProgress=false;tested=brightnessChosen=false;brightness=.65f;if(pieces!=null)foreach(var p in pieces)p.ResetItem();if(sockets!=null)foreach(var s in sockets)s.occupant=null;UpdateLights();}
 public void Place(int index,GrabbableItem item){if(!begun||completed||index<0||index>=pieces.Length||item!=pieces[index]||placed[index])return;placed[index]=true;item.SnapTo(sockets[index].transform,true);Credit("place"+index);if(index>0){orientation[index-1]=1;routePivots[index-1].rotation=Quaternion.AngleAxis(90,socketPlaneNormal)*sockets[index].transform.rotation;ApplyOrientation(index-1);poseDirty[index-1]=true;flashUntil[index-1]=Time.time+.65f;}status=index==0?"Source powered. Place routing pieces.":"Piece fitted. Rotate until the connection arrows align.";UpdateLights();}
 public int ConnectionMask(int index){int mask=targetMasks[index];for(int k=0;k<orientation[index];k++)mask=((mask<<1)&15)|((mask>>3)&1);return mask;}
 public bool Aligned(int index)=>placed[index+1]&&ConnectionMask(index)==targetMasks[index];
 public void RotatePiece(int index){if(index<0||index>=orientation.Length)return;if(!begun||completed||testInProgress||!placed[index+1]){status="Place this routing piece first.";return;}ClearTraces();orientation[index]=(orientation[index]+1)%4;ApplyOrientation(index);poseDirty[index]=true;if(Aligned(index))Credit("align"+index);tested=false;status=(index==0?"Elbow":index==1?"T-junction":"Straight")+" turned to "+orientation[index]*90+" degrees. Select Test Lights to check this orientation.";UpdateLights();}
 public void TestLights(){if(!begun||completed||testInProgress)return;for(int i=0;i<4;i++)if(!placed[i]){status="Fit the crystal and all three pieces before testing.";return;}StopAllCoroutines();ClearTraces();for(int i=0;i<3;i++)lastTest[i]=orientation[i];tested=false;testInProgress=true;status="Testing the light route...";StartCoroutine(RoutePulse());}

 public void SetBrightness(float value){if(!begun||completed||!tested)return;brightness=Mathf.Clamp(value,.25f,1);brightnessChosen=true;UpdateLights();status="Garden brightness: "+Mathf.RoundToInt(brightness*100)+"%. Confirm to finish.";}
 public void ConfirmBrightness(){if(!begun||completed||!tested||!brightnessChosen){status="Test all connections, then adjust brightness first.";return;}Credit("brightness");Complete("Garden complete! All three lantern destinations glow.");}
 LineRenderer MakePulse(string name,float width){var go=new GameObject(name);go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();var material=new Material(pulseShader?pulseShader:Shader.Find("Diwali/Routing Light Pulse"));material.SetColor("_BaseColor",new Color(2,1.4f,.5f,.8f));material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.One);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3100;line.material=material;line.widthMultiplier=width;line.alignment=LineAlignment.View;line.textureMode=LineTextureMode.Stretch;line.positionCount=32;line.numCapVertices=6;line.numCornerVertices=4;line.useWorldSpace=true;return line;}
 Vector3[] ConnectorPath(int i,bool fork=false){var n=socketPlaneNormal.normalized;var right=Vector3.Cross(Vector3.up,n).normalized;if(right.sqrMagnitude<.01f)right=Vector3.ProjectOnPlane(Vector3.right,n).normalized;var up=Vector3.Cross(n,right);var turn=Quaternion.AngleAxis(orientation[i]*90,n);var c=sockets[i+1].transform.position+n*.035f;var points=new System.Collections.Generic.List<Vector2>();if(i==2){points.Add(new Vector2(-.25f,0));points.Add(new Vector2(.25f,0));}else if(i==1){if(fork){points.Add(new Vector2(0,.10f));points.Add(new Vector2(0,-.25f));}else{points.Add(new Vector2(-.25f,.10f));points.Add(new Vector2(.25f,.10f));}}else{points.Add(new Vector2(-.25f,.13f));for(int k=0;k<=16;k++){float a=Mathf.Lerp(90,0,k/16f)*Mathf.Deg2Rad;points.Add(new Vector2(-.10f+.23f*Mathf.Cos(a),-.10f+.23f*Mathf.Sin(a)));}points.Add(new Vector2(.13f,-.25f));}float rx=0,ry=0;var ax=turn*right;var ay=turn*up;foreach(var mf in routePivots[i].GetComponentsInChildren<MeshFilter>())foreach(var v in mf.sharedMesh.vertices){var delta=mf.transform.TransformPoint(v)-sockets[i+1].transform.position;rx=Mathf.Max(rx,Mathf.Abs(Vector3.Dot(delta,ax)));ry=Mathf.Max(ry,Mathf.Abs(Vector3.Dot(delta,ay)));}var result=new Vector3[points.Count];for(int k=0;k<points.Count;k++)result[k]=c+turn*(right*points[k].x*Mathf.Max(.01f,rx-.012f)/.25f+up*points[k].y*Mathf.Max(.01f,ry-.012f)/.25f);return result;}
 Vector3 Along(Vector3[] p,float t){float length=0;for(int k=1;k<p.Length;k++)length+=Vector3.Distance(p[k-1],p[k]);float remaining=Mathf.Clamp01(t)*length;for(int k=1;k<p.Length;k++){float part=Vector3.Distance(p[k-1],p[k]);if(remaining<=part)return Vector3.Lerp(p[k-1],p[k],part>.00001f?remaining/part:0);remaining-=part;}return p[p.Length-1];}
 System.Collections.IEnumerator DrawRay(Vector3[] path,Color color,float duration=.65f){
  var line=MakePulse("Test ray - connected light route",.024f);testTraces.Add(line);
  line.startColor=line.endColor=Color.white;
  var material=line.sharedMaterial;material.SetColor("_BaseColor",new Color(color.r*1.6f,color.g*1.6f,color.b*1.6f,.85f));
  for(int k=0;k<line.positionCount;k++)line.SetPosition(k,Along(path,k/(line.positionCount-1f)));
  material.SetFloat("_Head",0);
  for(float t=0;t<duration;t+=Time.deltaTime){material.SetFloat("_Head",Mathf.Lerp(0,1.18f,t/duration));yield return null;}
  material.SetFloat("_Head",1.3f);
 }

 System.Collections.IEnumerator RoutePulse(){Vector3 previous=GameObjectUtility.BoundsOf(pieces[0].transform).center+socketPlaneNormal*.07f;bool connected=true;for(int i=2;i>=0;i--){var path=ConnectorPath(i);if(Vector3.Distance(previous,path[path.Length-1])<Vector3.Distance(previous,path[0]))System.Array.Reverse(path);yield return DrawRay(new[]{previous,path[0]},new Color(1,.72f,.18f),.3f);if(!Aligned(i)){yield return DrawRay(new[]{path[0],Along(path,.35f)},new Color(1,.16f,.04f),.45f);connected=false;status="Light stopped at "+(i==2?"Straight":i==1?"T-junction":"Elbow")+". Rotate that piece and test again.";break;}yield return DrawRay(path,new Color(1,.78f,.24f));previous=path[path.Length-1];}if(connected){for(int i=0;i<destinations.Length;i++){var end=destinations[i].transform.position;yield return DrawRay(new[]{previous,Vector3.Lerp(previous,end,.5f)+Vector3.up*.25f,end},new Color(1,.78f,.24f),.45f);flashUntil[i]=Time.time+.7f;}}testInProgress=false;tested=connected;if(tested){Credit("test");status="Light reached all three lanterns. Set garden brightness and confirm.";}UpdateLights();yield return new WaitForSeconds(.8f);ClearTraces();}

 void UpdateLights(){if(destinations==null)return;if(lampRenderers==null){lampRenderers=new Renderer[3][];var venue=GameObjectUtility.FindExact("LanternRouting - Approved Warm Palette");var names=new[]{"lamp1lamp","lamp2","lamp3"};for(int i=0;i<3;i++){var art=venue?GameObjectUtility.FindUnder(venue,names[i]):null;lampRenderers[i]=art?art.GetComponentsInChildren<Renderer>(true):new Renderer[0];}}for(int i=0;i<destinations.Length;i++){bool on=begun&&(tested||Time.time<flashUntil[i]);destinations[i].intensity=on?brightness*.65f:0;destinations[i].range=1.15f;destinations[i].shadows=LightShadows.None;foreach(var renderer in lampRenderers[i])foreach(var material in renderer.materials){if(!material.HasProperty("_EmissionColor"))continue;material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",on?new Color(1,.63f,.26f)*brightness*.45f:Color.black);}}}
}
}
