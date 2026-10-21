using UnityEngine;
namespace DiwaliGameplay {
public class RangoliGame : VenueGame {
 public GrabbableItem[] pieces; public SnapZone[] sockets; public Transform[] decorations; public int[] decorationGroups;
 UnityEngine.UI.Button beginControl,amberControl,magentaControl,rotateControl,celebrateControl;UnityEngine.UI.Slider brightnessControl;VenuePanel panel;
 public Vector3 panelPosition; public Quaternion panelRotation;
 void Start(){
  howToPlay="1  Begin Rangoli, then choose a warm colour palette.\n\n2  Grab each piece from the rack. Carry it to the matching outline on the board and release. A placed piece restores its matching pavilion decoration.\n\n3  Place all five shapes: sun, petal, diamond, C-border and star. You may choose the order.\n\n4  Select Rotate Sun to choose its orientation. Adjust Pavilion brightness.\n\n5  Select Celebrate to finish. Restart returns all pieces to the rack.";
  var venue=GameObjectUtility.FindExact("Rangoli - Approved Warm Palette");
  var holder=venue?GameObjectUtility.FindUnder(venue,"Cube.005"):null;
  Vector3 approach=panelPosition-panelRotation*Vector3.forward*2;
  if(holder){
   var b=GameObjectUtility.BoundsOf(holder);
   var floor=GameObjectUtility.BoundsOf(GameObjectUtility.FindUnder(venue,"Plane"));
   var front=Vector3.ProjectOnPlane(b.center-floor.center,Vector3.up).normalized;
   var side=Vector3.Cross(Vector3.up,front);
   float half=Vector3.Dot(new Vector3(Mathf.Abs(side.x),0,Mathf.Abs(side.z)),b.extents);
   panelPosition=b.center+side*(half+.65f)+front*.35f;
   panelPosition.y=floor.max.y+1.5f;
   approach=panelPosition+front*2;
  }
  var p=VenuePanel.Create(this,panelPosition,panelRotation,"Restore the Rangoli");panel=p;
  p.SetPhysicalSize(.96f,1.28f);p.FaceToward(approach);stationPanelPosition=panelPosition;stationViewer=approach;displayPanelPosition=panelPosition;displayViewer=approach;if(holder){var b=GameObjectUtility.BoundsOf(holder);var front=Vector3.ProjectOnPlane(approach-panelPosition,Vector3.up).normalized;var side=Vector3.Cross(Vector3.up,front);displayPanelPosition=b.center-front*.65f+side*(Mathf.Max(b.extents.x,b.extents.z)+.45f);displayPanelPosition.y=panelPosition.y;displayViewer=b.center+front*1.7f;displayViewer.y=panelPosition.y;}
  beginControl=p.AddButton("Begin Rangoli",BeginGame);amberControl=p.AddButton("Amber / terracotta",()=>ChoosePalette(0));magentaControl=p.AddButton("Magenta / gold",()=>ChoosePalette(1));rotateControl=p.AddButton("Rotate Sun",RotateSun);brightnessControl=p.AddSlider("Pavilion brightness",.65f,SetBrightness);celebrateControl=p.AddButton("Celebrate",Celebrate);
  for(int i=0;i<5;i++){int k=i;sockets[i].canAccept=x=>begun&&!completed;sockets[i].onSnapped=x=>Place(k,x);}
 }

 public Collider pavilionSurface;public Transform pavilionFloor;
 Vector3 stationPanelPosition,stationViewer,displayPanelPosition,displayViewer;bool displayControls;
 public Transform sun; public Light displayLight; public int palette,orientation; public float brightness=.65f;
 public Renderer[] motifs; Quaternion initialSun; Color[][] initialColors; float initialIntensity;
 protected override void Awake(){base.Awake();PavilionGrounding.Ground(this);initialSun=sun.localRotation;initialIntensity=displayLight.intensity;initialColors=new Color[motifs.Length][];for(int i=0;i<motifs.Length;i++)initialColors[i]=System.Array.ConvertAll(motifs[i].materials,m=>m.color);}
 bool[] placed=new bool[5]; bool paletteChosen,rotated,adjusted;
 public override void BeginGame(){ResetGame();base.BeginGame();Credit("begin");status="Choose a warm palette, then match five pieces to the board.";}
 public override void ResetGame(){StopAllCoroutines();base.ResetGame();if(sun)sun.localRotation=initialSun;if(displayLight)displayLight.intensity=initialIntensity;if(initialColors!=null)for(int i=0;i<motifs.Length;i++){var ms=motifs[i].materials;for(int j=0;j<ms.Length;j++)ms[j].color=initialColors[i][j];}paletteChosen=rotated=adjusted=false;orientation=0;placed=new bool[5];if(pieces!=null)foreach(var p in pieces)p.ResetItem();if(sockets!=null)foreach(var s in sockets)s.occupant=null;ShowGroups();}
 void ShowGroups(){if(decorations==null)return;for(int i=0;i<decorations.Length;i++)if(decorations[i])decorations[i].gameObject.SetActive(!begun||placed[decorationGroups[i]]);}
 public void ChoosePalette(int value){if(!begun||completed)return;palette=value;paletteChosen=true;Credit("palette");foreach(var r in motifs)foreach(var m in r.materials)if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",value==0?new Color(1,.48f,.13f):new Color(.9f,.13f,.45f));status="Warm palette selected. Match each shape to its recess.";}
 public void Place(int index,GrabbableItem item){if(!begun||completed||index<0||index>=pieces.Length||item!=pieces[index]||placed[index])return;placed[index]=true;item.SnapTo(sockets[index].transform,true);if(index==3)item.transform.position+=sockets[index].transform.position-GameObjectUtility.BoundsOf(item.transform).center;Credit("piece"+index);ShowGroups();status="Decorations restored: "+Count()+"/5. Rotate Sun, adjust light, then Celebrate.";}
 int Count(){int n=0;foreach(bool p in placed)if(p)n++;return n;}
 public void RotateSun(){if(!begun||completed||!placed[0]){status="Place the sun first.";return;}orientation=(orientation+1)%4;sun.Rotate(Vector3.up,90,Space.World);rotated=true;Credit("orientation");status="Sun orientation: "+orientation*90+" degrees.";}
 public void SetBrightness(float value){if(!begun||completed)return;brightness=Mathf.Clamp(value,.25f,1);if(displayLight)displayLight.intensity=brightness*5;adjusted=true;Credit("brightness");status="Pavilion brightness: "+Mathf.RoundToInt(brightness*100)+"%.";}
 public void Celebrate(){if(!begun||completed)return;if(Count()!=5||!paletteChosen||!rotated||!adjusted){status="Restore 5/5 pieces, choose palette, Rotate Sun and adjust brightness first.";return;}Credit("celebrate");Complete("Rangoli complete! Your festival pavilion is restored.");StartCoroutine(Pulse());}
 System.Collections.IEnumerator Pulse(){float baseline=displayLight?displayLight.intensity:0;for(float t=0;t<1.5f;t+=Time.deltaTime){if(displayLight)displayLight.intensity=baseline*(1+.3f*Mathf.Sin(t/1.5f*Mathf.PI));yield return null;}if(displayLight)displayLight.intensity=baseline;}
 public override void Tick(){if(begun)ShowGroups();if(!panel)return;bool wantsDisplay=Count()==5||completed;if(wantsDisplay!=displayControls){displayControls=wantsDisplay;panel.FollowTaskAt(displayControls?displayPanelPosition:stationPanelPosition,displayControls?displayViewer:stationViewer);}beginControl.gameObject.SetActive(!begun||completed);amberControl.gameObject.SetActive(begun&&!completed&&!paletteChosen);magentaControl.gameObject.SetActive(begun&&!completed&&!paletteChosen);rotateControl.gameObject.SetActive(begun&&!completed&&Count()==5);panel.SetControlVisible(brightnessControl,begun&&!completed&&Count()==5&&rotated);celebrateControl.gameObject.SetActive(begun&&!completed&&Count()==5&&paletteChosen&&rotated&&adjusted);if(begun&&!completed)status=!paletteChosen?"Choose a warm colour palette.":Count()<5?"Match each piece to its board outline, then release. "+Count()+" / 5 placed.":!rotated?"Select Rotate Sun to choose its orientation.":!adjusted?"Adjust Pavilion brightness.":"Select Celebrate to finish your Rangoli.";}
}
}
