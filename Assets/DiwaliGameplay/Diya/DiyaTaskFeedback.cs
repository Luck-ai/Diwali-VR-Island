using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DiwaliGameplay {
public sealed class DiyaTaskFeedback : MonoBehaviour {
 DiyaGame owner;
 MeshRenderer coating;
 MeshFilter sourceMesh;
 Mesh coatMesh;
 Material dustMaterial,particleMaterial,lineMaterial;
 Texture2D particleTexture;
 ParticleSystem puffs;
 TaskCue pickup,starter;
 MaterialPropertyBlock lineColour;
 float visibleClean;
 public int DustParticlesEmitted {get;private set;}
 public bool DustPuffsAnimating=>puffs&&puffs.isPlaying;
 public float VisibleCleaningProgress=>visibleClean;
 public bool DustShaderReady=>dustMaterial&&dustMaterial.shader&&dustMaterial.shader.isSupported;
 public bool DustVisible=>coating&&coating.enabled;
 public bool PickupCueVisible=>pickup!=null&&pickup.root.activeSelf;
 public bool StarterCueVisible=>starter!=null&&starter.root.activeSelf;
 public Transform HighlightedDiya=>owner?owner.diya.transform:null;

 public void Initialise(DiyaGame game) {
  if(owner)return;
  owner=game;lineColour=new MaterialPropertyBlock();sourceMesh=owner.diya.GetComponentInChildren<MeshFilter>();
  if(owner.dust)foreach(var old in owner.dust.GetComponentsInChildren<TextMesh>(true))old.gameObject.SetActive(false);
  CreateCoating();CreatePuffs();
  lineMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
  lineMaterial.SetColor("_BaseColor",new Color(1,.76f,.27f));
  pickup=CreateCue("Working diya guide");starter=CreateCue("Starter flame guide");
  var venue=GameObjectUtility.FindExact("Diya - Approved Warm Palette");
  foreach(var name in new[]{"Sphere","Sphere.001"}) {
   var fixedDiya=GameObjectUtility.FindUnder(venue,name);if(!fixedDiya)continue;
   foreach(var grab in fixedDiya.GetComponentsInChildren<XRGrabInteractable>(true))grab.enabled=false;
  }
 }

 Bounds DiyaMeshBounds(){
  var mesh=sourceMesh.sharedMesh.bounds;
  var bounds=new Bounds(sourceMesh.transform.TransformPoint(mesh.center),Vector3.zero);
  for(int i=0;i<8;i++)bounds.Encapsulate(sourceMesh.transform.TransformPoint(mesh.center+Vector3.Scale(mesh.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
  return bounds;
 }

 void CreateCoating() {
  var source=sourceMesh;
  var mesh=source.sharedMesh;
  var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];
  var bounds=DiyaMeshBounds();
  for(int i=0;i<vertices.Length;i++) {
   var world=source.transform.TransformPoint(vertices[i]);
   float x=(world.x-bounds.min.x)/Mathf.Max(.01f,bounds.size.x),y=(world.y-bounds.min.y)/Mathf.Max(.01f,bounds.size.y),z=(world.z-bounds.min.z)/Mathf.Max(.01f,bounds.size.z);
   uv[i]=new Vector2(x+y*.3f,z+y*.7f);
  }
  coatMesh=new Mesh{name="Diya removable dust layer",vertices=vertices,normals=normals,uv=uv};
  var triangles=new System.Collections.Generic.List<int>();for(int sub=1;sub<mesh.subMeshCount;sub++)triangles.AddRange(mesh.GetTriangles(sub));
  coatMesh.SetTriangles(triangles,0);coatMesh.RecalculateBounds();
  var go=new GameObject("Brushable dust coating",typeof(MeshFilter),typeof(MeshRenderer));
  go.transform.SetParent(source.transform,false);go.GetComponent<MeshFilter>().sharedMesh=coatMesh;
  coating=go.GetComponent<MeshRenderer>();coating.shadowCastingMode=ShadowCastingMode.Off;coating.receiveShadows=false;
  dustMaterial=new Material(owner.dustCoatingShader?owner.dustCoatingShader:Shader.Find("Diwali/Diya Dust Coating"));
  dustMaterial.name="Progressive diya dust";coating.sharedMaterial=dustMaterial;coating.enabled=false;
 }

 void CreatePuffs() {
  var go=new GameObject("Brushed dust puffs");go.transform.SetParent(transform,false);puffs=go.AddComponent<ParticleSystem>();
  puffs.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var main=puffs.main;main.playOnAwake=false;main.loop=false;main.simulationSpace=ParticleSystemSimulationSpace.World;
  main.startLifetime=new ParticleSystem.MinMaxCurve(.35f,.75f);main.startSize=new ParticleSystem.MinMaxCurve(.018f,.045f);
  main.startSpeed=0;main.gravityModifier=-.035f;main.maxParticles=100;main.startColor=new Color(.72f,.62f,.43f,.7f);
  var emission=puffs.emission;emission.enabled=false;var shape=puffs.shape;shape.enabled=false;
  var size=puffs.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,.45f,1,1.3f));
  var colour=puffs.colorOverLifetime;colour.enabled=true;var gradient=new Gradient();
  gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.8f,0),new GradientAlphaKey(0,1)});colour.color=gradient;
  particleTexture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Soft dust puff alpha",wrapMode=TextureWrapMode.Clamp};
  for(int y=0;y<32;y++)for(int x=0;x<32;x++){float u=(x-15.5f)/15.5f,v=(y-15.5f)/15.5f;particleTexture.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-u*u-v*v),2)));}particleTexture.Apply();
  particleMaterial=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
  particleMaterial.SetTexture("_BaseMap",particleTexture);particleMaterial.SetColor("_BaseColor",Color.white);particleMaterial.SetFloat("_Surface",1);
  particleMaterial.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);particleMaterial.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);particleMaterial.SetFloat("_ZWrite",0);
  particleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");particleMaterial.renderQueue=3000;
  var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=particleMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;
 }

 public void Brush(Vector3 contact,float travel) {
  if(travel<.004f||!owner||owner.clean)return;
  if(!puffs.isPlaying)puffs.Play();
  var bounds=DiyaMeshBounds();
  var point=bounds.ClosestPoint(contact);point.y=Mathf.Max(point.y,bounds.center.y+.07f);
  int count=Mathf.Clamp(Mathf.CeilToInt(travel*70),2,8);
  for(int i=0;i<count;i++) {
   var emit=new ParticleSystem.EmitParams {position=point+Random.insideUnitSphere*.035f,velocity=new Vector3(Random.Range(-.1f,.1f),Random.Range(.08f,.2f),Random.Range(-.1f,.1f))};
   puffs.Emit(emit,1);
  }
  DustParticlesEmitted+=count;
 }

 public void ResetVisuals() {
  visibleClean=0;DustParticlesEmitted=0;if(dustMaterial)dustMaterial.SetFloat("_Clean",0);
  if(coating)coating.enabled=false;if(puffs)puffs.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  if(pickup!=null)pickup.root.SetActive(false);if(starter!=null)starter.root.SetActive(false);
 }

 void LateUpdate() {
  if(!owner)return;
  bool active=owner.begun&&!owner.completed;
  visibleClean=Mathf.MoveTowards(visibleClean,owner.CleaningProgress,Time.deltaTime*1.8f);
  coating.enabled=active&&owner.Step==5&&visibleClean<.999f;dustMaterial.SetFloat("_Clean",visibleClean);
  if(owner.dust)owner.dust.SetActive(coating.enabled);
  bool showPickup=active&&owner.Step>=5&&owner.Step<=7&&!owner.diya.IsHeld;
  string text=owner.Step==5&&!owner.clean?"BRUSH THIS DIYA\nDust remaining "+Mathf.RoundToInt((1-visibleClean)*100)+"%":owner.Step==7?"TAKE THIS DIYA\nCarry it to the starter flame":"CLEAN DIYA\n"+(owner.Step==5?"Return the feather":"Reveal the starter flame");
  UpdateCue(pickup,DiyaMeshBounds(),showPickup,text,owner.clean);
  var target=new Bounds(owner.starter.position-Vector3.up*.12f,new Vector3(.8f,.35f,.6f));
  UpdateCue(starter,target,active&&owner.Step==7,"STARTER FLAME\nKeep this diya on the table",true);
 }

 sealed class TaskCue {public GameObject root;public Transform tag;public Text text;public LineRenderer ring,arrow;}
 TaskCue CreateCue(string name) {
  var cue=new TaskCue {root=new GameObject(name)};cue.root.transform.SetParent(transform,false);
  var go=new GameObject("Task label",typeof(RectTransform),typeof(Canvas),typeof(Image));cue.tag=go.transform;cue.tag.SetParent(cue.root.transform,false);
  var rect=(RectTransform)cue.tag;rect.sizeDelta=new Vector2(420,100);rect.localScale=Vector3.one*.0015f;
  go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;var image=go.GetComponent<Image>();image.sprite=VenuePanel.Rounded();image.type=Image.Type.Sliced;image.color=new Color(.14f,.035f,.065f,.96f);image.raycastTarget=false;
  var label=new GameObject("Instruction",typeof(RectTransform),typeof(Text));label.transform.SetParent(cue.tag,false);cue.text=label.GetComponent<Text>();
  var lr=label.GetComponent<RectTransform>();lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=new Vector2(10,5);lr.offsetMax=new Vector2(-10,-5);
  cue.text.font=Resources.Load<Font>("Diwali/Inter");if(!cue.text.font)cue.text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");cue.text.fontSize=25;cue.text.alignment=TextAnchor.MiddleCenter;cue.text.color=new Color(1,.92f,.7f);cue.text.raycastTarget=false;
  cue.ring=Line(cue.root.transform,"Highlighted diya",48,true);cue.arrow=Line(cue.root.transform,"Pickup pointer",5,false);cue.root.SetActive(false);return cue;
 }
 LineRenderer Line(Transform parent,string name,int points,bool loop) {
  var go=new GameObject(name);go.transform.SetParent(parent,false);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=true;line.loop=loop;line.positionCount=points;line.widthMultiplier=.01f;line.sharedMaterial=lineMaterial;line.shadowCastingMode=ShadowCastingMode.Off;return line;
 }
 void UpdateCue(TaskCue cue,Bounds bounds,bool visible,string text,bool ready) {
  cue.root.SetActive(visible);if(!visible)return;
  cue.text.text=text;cue.tag.position=bounds.center+Vector3.up*(bounds.extents.y+.32f);
  var camera=Camera.main;if(camera){var toward=cue.tag.position-camera.transform.position;toward.y=0;if(toward.sqrMagnitude>.001f)cue.tag.rotation=Quaternion.LookRotation(toward);}
  float pulse=1+Mathf.Sin(Time.time*3)*.035f;var tint=ready?new Color(.42f,1,.65f):new Color(1,.76f,.27f);
  cue.ring.startColor=cue.ring.endColor=cue.arrow.startColor=cue.arrow.endColor=tint;
  lineColour.SetColor("_BaseColor",tint);cue.ring.SetPropertyBlock(lineColour);cue.arrow.SetPropertyBlock(lineColour);
  for(int i=0;i<48;i++){float angle=i*Mathf.PI*2/48;cue.ring.SetPosition(i,new Vector3(bounds.center.x+Mathf.Cos(angle)*(bounds.extents.x+.04f)*pulse,bounds.center.y-.02f,bounds.center.z+Mathf.Sin(angle)*(bounds.extents.z+.04f)*pulse));}
  var point=new Vector3(bounds.center.x,bounds.max.y+.07f,bounds.center.z);var side=cue.tag.right*.045f;
  cue.arrow.SetPositions(new[]{point+Vector3.up*.15f,point,point+Vector3.up*.06f+side,point,point+Vector3.up*.06f-side});
 }
 void OnDestroy() {if(coatMesh)Destroy(coatMesh);if(dustMaterial)Destroy(dustMaterial);if(particleMaterial)Destroy(particleMaterial);if(particleTexture)Destroy(particleTexture);if(lineMaterial)Destroy(lineMaterial);}
}
}
