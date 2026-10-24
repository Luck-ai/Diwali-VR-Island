#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Newtonsoft.Json;

namespace DiwaliGameplay {
public static class FinishTodayVerification {
 static string Folder=>Path.GetFullPath("../Game_Plans/Finish_Today_2026-10-24");
 static void Write(string name,object value){Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,name+".json"),JsonConvert.SerializeObject(value,Formatting.Indented));}
 static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
 static object B(Bounds b)=>new{center=V(b.center),size=V(b.size),min=V(b.min),max=V(b.max)};
 static object T(Transform t)=>t?new{name=t.name,position=V(t.position),rotation=V(t.eulerAngles),local=V(t.localPosition),scale=V(t.lossyScale)}:null;
 [MenuItem("Diwali/Finish Today/Snapshot")]
 public static void Snapshot(){
  var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var games=UnityEngine.Object.FindObjectsByType<VenueGame>(FindObjectsSortMode.None);
  Write("Snapshot",new{time=DateTime.Now.ToString("s"),playing=EditorApplication.isPlaying,scene=s.path,dirty=s.isDirty,sky=RenderSettings.skybox?AssetDatabase.GetAssetPath(RenderSettings.skybox):null,games=games.Select(g=>new{type=g.GetType().Name,g.begun,g.completed,g.status,panels=g.GetComponentsInChildren<VenuePanel>(true).Select(p=>new{pose=T(p.transform),p.autoFacePlayer,width=p.GetComponent<RectTransform>().rect.width,buttons=p.GetComponentsInChildren<Button>(true).Select(b=>new{b.name,active=b.gameObject.activeInHierarchy,b.interactable})}),items=g.GetComponentsInChildren<GrabbableItem>(true).Select(i=>new{i.name,i.itemId,pose=T(i.transform),bounds=B(GameObjectUtility.BoundsOf(i.transform))})})});
 }
 [MenuItem("Diwali/Finish Today/Preview Packed Sweets")]
 public static async void PreviewPacked(){
  if(!EditorApplication.isPlaying)throw new InvalidOperationException("Preview in Play mode.");
  var g=UnityEngine.Object.FindFirstObjectByType<SweetGame>();g.BeginGame();g.OpenCabinet();g.basket.SnapTo(g.rest);g.basketReady=true;
  var choices=g.sweets.GroupBy(s=>s.itemId).Select(s=>s.First()).Take(3).ToList();choices.Add(g.sweets.First(s=>!choices.Contains(s)));foreach(var item in choices)g.AddSweet(item);
  await Task.Delay(250);PackedSnapshot();
 }
 [MenuItem("Diwali/Finish Today/Packed Snapshot")]
 public static void PackedSnapshot(){
  var g=UnityEngine.Object.FindFirstObjectByType<SweetGame>();
  Write("Packed_Sweets",new{g.begun,g.basketReady,workspaceForward=V(g.workspaceForward),basket=T(g.basket.transform),basketBounds=B(GameObjectUtility.BoundsOf(g.basket.transform)),rest=T(g.rest),checkout=T(g.checkout),selected=g.selected.Select(i=>new{i.itemId,pose=T(i.transform),renderBounds=B(GameObjectUtility.BoundsOf(i.transform)),mesh=i.GetComponentsInChildren<MeshFilter>(true).Select(m=>new{m.name,localBounds=B(m.sharedMesh.bounds),pose=T(m.transform)}),colliders=i.grab.colliders.Where(c=>c&&c.enabled).Select(c=>new{c.name,type=c.GetType().Name,bounds=B(c.bounds),pose=T(c.transform)})}),slots=g.slots.Select(T)});
  var eye=g.rest.position+g.workspaceForward*1.15f+Vector3.up*.45f;eye.y=1.65f;
  CopyRender("Packed-Sweets-Customer",eye,g.basket.transform.position+Vector3.up*.1f,62);
 }
 [MenuItem("Diwali/Finish Today/Preview Rangoli Display")]
 public static async void PreviewRangoli(){
  if(!EditorApplication.isPlaying)throw new InvalidOperationException("Preview in Play mode.");
  var g=UnityEngine.Object.FindFirstObjectByType<RangoliGame>();g.BeginGame();g.ChoosePalette(1);for(int i=0;i<5;i++)g.Place(i,g.pieces[i]);await Task.Delay(350);
  var p=g.GetComponentInChildren<VenuePanel>();var canvas=p.GetComponent<Canvas>();var button=p.GetComponentsInChildren<Button>(true).First(b=>b.name=="Rotate Sun");var rect=button.GetComponent<RectTransform>();var center=rect.TransformPoint(rect.rect.center);var origin=center-canvas.transform.forward*1.2f;origin.y=Mathf.Clamp(origin.y,.8f,1.45f);
  var hits=new System.Collections.Generic.List<RaycastResult>();var ray=new TrackedDeviceEventData(EventSystem.current){rayPoints=new System.Collections.Generic.List<Vector3>{origin,center+canvas.transform.forward*.05f},layerMask=~0};canvas.GetComponent<TrackedDeviceGraphicRaycaster>().Raycast(ray,hits);
  Write("Rangoli_Menu",new{panel=T(p.transform),center=V(center),origin=V(origin),hits=hits.Select(h=>h.gameObject.name),blockers=Physics.RaycastAll(origin,(center-origin).normalized,Vector3.Distance(center,origin),~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).Select(h=>new{h.collider.name,h.distance})});
  CopyRender("Rangoli-Menu-Standing",origin+Vector3.up*.2f,center,72);
 }
 [MenuItem("Diwali/Finish Today/Run All Interactions")]
 public static void RunAll(){FinalPassVerification.VenueFilter="";FinalPassVerification.Run();}
 [MenuItem("Diwali/Finish Today/Run Sweet Interactions")]
 public static void RunSweet(){FinalPassVerification.VenueFilter="SweetGame";FinalPassVerification.Run();}
 [MenuItem("Diwali/Finish Today/Run Rangoli Interactions")]
 public static void RunRangoli(){FinalPassVerification.VenueFilter="RangoliGame";FinalPassVerification.Run();}
 [MenuItem("Diwali/Finish Today/Run Diya Interactions")]
 public static void RunDiya(){FinalPassVerification.VenueFilter="DiyaGame";FinalPassVerification.Run();}
 [MenuItem("Diwali/Finish Today/Run Travel And Collisions")]
 public static void RunTravel(){FinalTravelVerification.Run();}
 [MenuItem("Diwali/Finish Today/Preview Accepted Monitor")]
 public static async void PreviewMonitor(){
  if(!EditorApplication.isPlaying)throw new InvalidOperationException("Preview in Play mode.");
  var g=UnityEngine.Object.FindFirstObjectByType<SweetGame>();g.BeginGame();g.OpenCabinet();g.basket.SnapTo(g.rest);g.basketReady=true;
  var choices=g.sweets.GroupBy(s=>s.itemId).Select(s=>s.First()).Take(3).ToList();choices.Add(g.sweets.First(s=>!choices.Contains(s)));foreach(var item in choices)g.AddSweet(item);
  g.basket.SnapTo(g.checkout);g.docked=true;g.AuthorizeCard();await Task.Delay(200);
  var display=g.GetComponent<SweetCheckoutDisplay>();var center=display.screen.TransformPoint(display.localCenter);var eye=center+g.workspaceForward*1.1f;eye.y=1.65f;
  CopyRender("Monitor-Card-Accepted-Standing",eye,center,56);
  Write("Monitor_Standing",new{center=V(center),eye=V(eye),accepted=MonitorShows("ACCEPTED")});
 }
 [MenuItem("Diwali/Finish Today/Check Rounded Path Clearance")]
 public static void CheckRoundedPaths(){
  var plaza=GameObjectUtility.FindExact("02 Rangoli and Ceremonial Lamps");var art=GameObjectUtility.FindExact("Flower rangoli - walkable inlaid carpet");var ring=GameObjectUtility.FindExact("Rounded Rangoli promenade walking surface");
  if(!plaza||!art||!ring)throw new InvalidOperationException("Rounded routes have not been applied.");
  var center=GameObjectUtility.BoundsOf(art).center;center.y=.105f;float obstacle=0;
  foreach(var lamp in plaza.GetComponentsInChildren<Transform>(false).Where(t=>t.name=="Diya (Blender model)"&&Vector3.ProjectOnPlane(t.position-center,Vector3.up).magnitude>6)){
   var bounds=GameObjectUtility.BoundsOf(lamp);foreach(var c in lamp.GetComponentsInChildren<Collider>())if(c.enabled&&!c.isTrigger)bounds.Encapsulate(c.bounds);
   foreach(float x in new[]{bounds.min.x,bounds.max.x})foreach(float z in new[]{bounds.min.z,bounds.max.z})obstacle=Mathf.Max(obstacle,Vector2.Distance(new Vector2(x,z),new Vector2(center.x,center.z)));
  }
  var radii=ring.GetComponent<MeshFilter>().sharedMesh.vertices.Select(p=>Vector3.ProjectOnPlane(ring.transform.TransformPoint(p)-center,Vector3.up).magnitude).ToArray();float inner=radii.Min(),outer=radii.Max(),radius=(inner+outer)*.5f;
  var blocked=new System.Collections.Generic.List<object>();
  for(int i=0;i<96;i++)foreach(float offset in new[]{-.65f,0,.65f}){float angle=2*Mathf.PI*i/96;var p=center+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(radius+offset);var collisions=Physics.OverlapCapsule(p+Vector3.up*.5f,p+Vector3.up*1.65f,.24f,~0,QueryTriggerInteraction.Ignore).Where(c=>!c.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>()).ToArray();if(collisions.Length>0)blocked.Add(new{point=V(p),colliders=collisions.Select(c=>c.name)});}
  Write("Rounded_Path_Clearance",new{center=V(center),innerRadius=inner,outerRadius=outer,minimumLaneWidth=(outer-inner)*Mathf.Cos(Mathf.PI/96),minimumDiyaClearance=inner*Mathf.Cos(Mathf.PI/96)-obstacle,samples=288,blockedSamples=blocked.Count,blocked});
 }
 [MenuItem("Diwali/Finish Today/Check Floor And Monitor")]
 public static void CheckFloorMonitor(){
  var v=GameObjectUtility.FindExact("Diya - Approved Warm Palette");var f=GameObjectUtility.FindUnder(v,"Plane.002").GetComponent<Renderer>();var block=new MaterialPropertyBlock();f.GetPropertyBlock(block,1);
  var monitor=UnityEngine.Object.FindFirstObjectByType<SweetCheckoutDisplay>();var status=GameObjectUtility.FindExact("Checkout monitor status");
  Write("Floor_And_Monitor",new{playing=EditorApplication.isPlaying,materials=f.sharedMaterials.Select(m=>new{m.name,shader=m.shader.name,m.shader.isSupported,baseMap=m.HasProperty("_BaseMap")&&m.GetTexture("_BaseMap")?AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")):null,emissionMap=m.HasProperty("_EmissionMap")&&m.GetTexture("_EmissionMap")?AssetDatabase.GetAssetPath(m.GetTexture("_EmissionMap")):null}),visited=block.GetInt("_VisitedCount"),shaders=new[]{"Diwali/Progressive Orange Path","DiwaliFestival/Festival Twilight Sky"}.Select(n=>new{name=n,messages=ShaderUtil.GetShaderMessages(Shader.Find(n)).Select(m=>new{m.message,severity=m.severity.ToString()})}),monitor=monitor?new{monitor.configured,screen=T(monitor.screen),size=new[]{monitor.localSize.x,monitor.localSize.y}}:null,monitorText=status?status.GetComponentsInChildren<Text>(true).Select(t=>t.text).ToArray():null});
 }
 public static bool MonitorShows(string value){var status=GameObjectUtility.FindExact("Checkout monitor status");return status&&status.GetComponentsInChildren<Text>(true).Any(t=>t.text.Contains(value));}
 public static void CaptureCheckoutStatus(string label){var display=UnityEngine.Object.FindFirstObjectByType<SweetCheckoutDisplay>();if(!display||!display.configured)return;var center=display.screen.TransformPoint(display.localCenter);var normal=display.screen.TransformDirection(display.localNormal).normalized;CopyRender("Monitor-"+label,center+normal*.7f+Vector3.up*.08f,center,40);}
 [MenuItem("Diwali/Finish Today/Capture Layouts")]
 public static void CaptureLayouts(){
  CopyRender("Sweet-Shop-Final",new Vector3(-27.5f,1.65f,3.7f),new Vector3(-33,1.1f,3.7f),75);
  CopyRender("Pathways-Arrival",new Vector3(1,1.65f,-25),new Vector3(-.5f,1,7),70);
  CopyRender("Pathways-Overview",new Vector3(0,30,3),new Vector3(0,0,23),65);
  CopyRender("Diya-Bench-Final",new Vector3(21,7.8f,10),new Vector3(30,1.2f,3.8f),68);
  CopyRender("Sky-Final",new Vector3(7,2.2f,25),new Vector3(-3,12,48),82);
  CopyRender("Diya-Floor-Final",new Vector3(30.4f,14,3.8f),new Vector3(30.4f,0,3.8f),66);
 }
 [MenuItem("Diwali/Finish Today/Save Scene")]
 public static void Save(){if(EditorApplication.isPlaying)throw new InvalidOperationException("Do not save a Play mode scene.");EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();Snapshot();}
 static void CopyRender(string name,Vector3 eye,Vector3 look,float fov){Directory.CreateDirectory(Folder);var path=ChristmasIslandQA.Render("FinishToday-"+name,eye,look,fov);File.Copy(path,Path.Combine(Folder,name+".png"),true);}
}
}
#endif
