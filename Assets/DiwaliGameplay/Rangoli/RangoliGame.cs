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
   var front=Vector3.ProjectOnPlane(-venue.right,Vector3.up).normalized;
   var side=Vector3.Cross(Vector3.up,front);
   float half=Vector3.Dot(new Vector3(Mathf.Abs(side.x),0,Mathf.Abs(side.z)),b.extents);
   float frontHalf=Vector3.Dot(new Vector3(Mathf.Abs(front.x),0,Mathf.Abs(front.z)),b.extents);
   panelPosition=b.center+side*(half+.95f)+front*(frontHalf+.35f);
   panelPosition.y=floor.max.y+1.5f;
   approach=panelPosition+front*2;
  }
  var p=VenuePanel.Create(this,panelPosition,panelRotation,"Restore the Rangoli");panel=p;
  p.SetPhysicalSize(.96f,1.28f);p.FaceToward(approach);
  beginControl=p.AddButton("Begin Rangoli",BeginGame);amberControl=p.AddButton("Amber / terracotta",()=>ChoosePalette(0));magentaControl=p.AddButton("Magenta / gold",()=>ChoosePalette(1));rotateControl=p.AddButton("Rotate Sun",RotateSun);brightnessControl=p.AddSlider("Pavilion brightness",.65f,SetBrightness);celebrateControl=p.AddButton("Celebrate",Celebrate);
  for(int i=0;i<5;i++){int k=i;sockets[i].canAccept=x=>begun&&!completed;sockets[i].onSnapped=x=>Place(k,x);}
 }

 public Collider pavilionSurface;public Transform pavilionFloor;public bool preserveAuthoredLayout;public Vector3 sunRotationAxis=Vector3.up;
 public Transform sun; public Light displayLight; public int palette,orientation; public float brightness=.65f;
 public Renderer[] motifs; Quaternion initialSun; Color[][] initialColors,initialEmission; Texture[][] initialEmissionMaps; bool[][] initialEmissionEnabled; float initialIntensity; LineRenderer sunPointer; Quaternion sunTarget; Coroutine sunTurn;
 protected override void Awake(){base.Awake();PavilionGrounding.Ground(this);initialSun=sun.localRotation;initialIntensity=displayLight.intensity;initialColors=new Color[motifs.Length][];initialEmission=new Color[motifs.Length][];initialEmissionMaps=new Texture[motifs.Length][];initialEmissionEnabled=new bool[motifs.Length][];for(int i=0;i<motifs.Length;i++){var materials=motifs[i].materials;initialColors[i]=System.Array.ConvertAll(materials,m=>m.color);initialEmissionMaps[i]=System.Array.ConvertAll(materials,m=>m.HasProperty("_EmissionMap")?m.GetTexture("_EmissionMap"):null);initialEmissionEnabled[i]=System.Array.ConvertAll(materials,m=>m.IsKeywordEnabled("_EMISSION"));initialEmission[i]=System.Array.ConvertAll(materials,m=>m.HasProperty("_EmissionColor")?m.GetColor("_EmissionColor"):Color.black);}sunTarget=initialSun;}
 int lastRestored=-1; bool[] placed=new bool[5]; bool paletteChosen,rotated,adjusted;
 public override void BeginGame(){ResetGame();base.BeginGame();Credit("begin");status="Choose a warm palette, then match five pieces to the board.";}
 public override void ResetGame(){StopAllCoroutines();sunTurn=null;base.ResetGame();if(sun)sun.localRotation=initialSun;if(displayLight)displayLight.intensity=initialIntensity;if(initialColors!=null)for(int i=0;i<motifs.Length;i++){var ms=motifs[i].materials;for(int j=0;j<ms.Length;j++){ms[j].color=initialColors[i][j];if(ms[j].HasProperty("_EmissionColor"))ms[j].SetColor("_EmissionColor",initialEmission[i][j]);if(ms[j].HasProperty("_EmissionMap"))ms[j].SetTexture("_EmissionMap",initialEmissionMaps[i][j]);if(initialEmissionEnabled[i][j])ms[j].EnableKeyword("_EMISSION");else ms[j].DisableKeyword("_EMISSION");}}if(sunPointer)sunPointer.gameObject.SetActive(false);sunTarget=initialSun;brightness=.65f;if(brightnessControl)brightnessControl.SetValueWithoutNotify(brightness);lastRestored=-1;paletteChosen=rotated=adjusted=false;orientation=0;placed=new bool[5];if(pieces!=null)foreach(var p in pieces)p.ResetItem();if(sockets!=null)foreach(var s in sockets)s.occupant=null;ShowGroups();}
 void ShowGroups(){if(decorations==null)return;for(int i=0;i<decorations.Length;i++)if(decorations[i])decorations[i].gameObject.SetActive(decorations[i].name!="Sphere.027"&&(!begun||placed[decorationGroups[i]]));}
 void ApplyAppearance(){
  for(int i=0;i<motifs.Length;i++){
   if(!motifs[i])continue;
   int group=0;for(int j=0;j<decorations.Length;j++)if(motifs[i].transform==decorations[j]||motifs[i].transform.IsChildOf(decorations[j])){group=decorationGroups[j];break;}
   Color[] warm={new Color(1,.66f,.12f),new Color(1,.22f,.06f),new Color(1,.8f,.27f),new Color(.96f,.34f,.12f),new Color(1,.65f,.2f)};
   Color[] rose={new Color(1,.72f,.24f),new Color(.95f,.06f,.4f),new Color(.76f,.15f,.65f),new Color(1,.16f,.47f),new Color(1,.83f,.35f)};
   var tint=(palette==0?warm:rose)[group];
   foreach(var material in motifs[i].materials){
    if(material.HasProperty("_Cull"))material.SetFloat("_Cull",0);
    var colour=Color.Lerp(Color.white,tint,group==1?.7f:.22f)*Mathf.Lerp(.45f,1.3f,brightness);colour.a=1;material.color=colour;
    if(material.HasProperty("_EmissionColor")){material.EnableKeyword("_EMISSION");if(material.HasProperty("_EmissionMap"))material.SetTexture("_EmissionMap",null);material.SetColor("_EmissionColor",tint*Mathf.Lerp(.005f,.24f,brightness));material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;}
   }
  }
 }
 public void ChoosePalette(int value){if(!begun||completed)return;palette=Mathf.Clamp(value,0,1);paletteChosen=true;ApplyAppearance();Credit("palette");status="Palette applied to the whole pavilion. Match each shape to its recess.";}
 public void Place(int index,GrabbableItem item){if(!begun||completed||index<0||index>=pieces.Length||item!=pieces[index]||placed[index])return;placed[index]=true;lastRestored=index;item.SnapTo(sockets[index].transform,true);if(index==3)item.transform.position+=sockets[index].transform.position-GameObjectUtility.BoundsOf(item.transform).center;Credit("piece"+index);ShowGroups();if(paletteChosen)ApplyAppearance();status="Decorations restored: "+Count()+"/5. Rotate Sun, adjust light, then Celebrate.";}
 int Count(){int n=0;foreach(bool p in placed)if(p)n++;return n;}
 void EnsureSunPointer(){
  if(sunPointer)return;var marker=new GameObject("Sun direction - rotates with the actual motif");marker.transform.SetParent(sun,false);sunPointer=marker.AddComponent<LineRenderer>();sunPointer.useWorldSpace=false;sunPointer.positionCount=5;sunPointer.startWidth=sunPointer.endWidth=.035f;var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",new Color(.22f,.025f,.12f));sunPointer.material=material;
  var bounds=GameObjectUtility.BoundsOf(decorations[0]);float radius=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z))*.4f;var n=sunRotationAxis.normalized;var forward=Vector3.ProjectOnPlane(Vector3.up,n).normalized;if(forward.sqrMagnitude<.1f)forward=Vector3.forward;var side=Vector3.Cross(n,forward);var centre=bounds.center+n*.055f;var world=new[]{centre,centre+forward*radius,centre+forward*(radius-.12f)+side*.08f,centre+forward*radius,centre+forward*(radius-.12f)-side*.08f};for(int i=0;i<world.Length;i++)sunPointer.SetPosition(i,sun.InverseTransformPoint(world[i]));
 }
 System.Collections.IEnumerator TurnSun(){var from=sun.localRotation;for(float t=0;t<.65f;t+=Time.deltaTime){sun.localRotation=Quaternion.Slerp(from,sunTarget,Mathf.SmoothStep(0,1,t/.65f));yield return null;}sun.localRotation=sunTarget;sunTurn=null;}
 public void RotateSun(){if(!begun||completed||!placed[0]){status="Place the sun first.";return;}EnsureSunPointer();sunPointer.gameObject.SetActive(true);orientation=(orientation+1)%8;sunTarget=Quaternion.AngleAxis(45,sun.parent?sun.parent.InverseTransformDirection(sunRotationAxis):sunRotationAxis)*sunTarget;if(sunTurn!=null)StopCoroutine(sunTurn);sunTurn=StartCoroutine(TurnSun());rotated=true;Credit("orientation");status="Sun and direction pointer turned to "+orientation*45+" degrees.";}
 public void SetBrightness(float value){if(!begun||completed)return;brightness=Mathf.Clamp(value,.25f,1);if(displayLight)displayLight.intensity=brightness*5;ApplyAppearance();adjusted=true;Credit("brightness");status="Pavilion brightness: "+Mathf.RoundToInt(brightness*100)+"%.";}
 public void Celebrate(){if(!begun||completed)return;if(Count()!=5||!paletteChosen||!rotated||!adjusted){status="Restore 5/5 pieces, choose palette, Rotate Sun and adjust brightness first.";return;}Credit("celebrate");Complete("Rangoli complete! Your festival pavilion is restored.");StartCoroutine(Pulse());}
 System.Collections.IEnumerator Pulse(){float baseline=displayLight?displayLight.intensity:0;for(float t=0;t<1.5f;t+=Time.deltaTime){if(displayLight)displayLight.intensity=baseline*(1+.3f*Mathf.Sin(t/1.5f*Mathf.PI));yield return null;}if(displayLight)displayLight.intensity=baseline;}
 public override void Tick(){if(begun)ShowGroups();if(!panel)return;beginControl.gameObject.SetActive(!begun||completed);amberControl.gameObject.SetActive(begun&&!completed&&(!paletteChosen||Count()==5));magentaControl.gameObject.SetActive(begun&&!completed&&(!paletteChosen||Count()==5));rotateControl.gameObject.SetActive(begun&&!completed&&Count()==5);panel.SetControlVisible(brightnessControl,begun&&!completed&&Count()==5&&rotated);celebrateControl.gameObject.SetActive(begun&&!completed&&Count()==5&&paletteChosen&&rotated&&adjusted);if(begun&&!completed)status=!paletteChosen?"Choose a warm colour palette.":Count()<5?(lastRestored>=0?new[]{"Sun","Entire petal border","Diamond motifs","C-border motifs","Stars"}[lastRestored]+" restored in the pavilion ahead. ":"")+"Match the remaining pieces. "+Count()+" / 5 placed.":!rotated?"Select Rotate Sun to choose its orientation.":!adjusted?"Adjust Pavilion brightness.":"Select Celebrate to finish your Rangoli.";}
}
}
