#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace DiwaliGameplay.Editor {
public static class UsabilityAudit {
[MenuItem("Diwali/Gameplay/Capture Player Approaches")]
public static void Capture(){
 ChristmasIslandQA.Render("Usability-Sweet-approach",new Vector3(-29.5f,1.7f,4.2f),new Vector3(-29.5f,1.15f,1.5f),75);
 ChristmasIslandQA.Render("Usability-Rangoli-approach",new Vector3(-20.5f,1.7f,33.5f),new Vector3(-23,1.1f,32.8f),75);
 ChristmasIslandQA.Render("Usability-Routing-approach",new Vector3(28,1.7f,35.6f),new Vector3(28.5f,1.1f,32.8f),75);
 ChristmasIslandQA.Render("Usability-Diya-approach",new Vector3(31,1.7f,7.7f),new Vector3(34,1,8.5f),75);
 ChristmasIslandQA.Render("Usability-Fireworks-approach",new Vector3(0,1.7f,58.5f),new Vector3(1.3f,1.2f,61.7f),75);
 ChristmasIslandQA.Render("Usability-Walking-routes",new Vector3(0,75,10),new Vector3(0,0,21),65);
 ChristmasIslandQA.Render("Usability-Arrival",new Vector3(0,1.7f,-8),new Vector3(0,1,8),75);
}
[MenuItem("Diwali/Gameplay/Inspect Usability")]
public static void Inspect(){var games=Object.FindObjectsByType<VenueGame>(FindObjectsSortMode.None);var report=new {playing=EditorApplication.isPlaying,venues=new[]{"SweetShop - Approved Warm Palette","Rangoli - Approved Warm Palette","LanternRouting - Approved Warm Palette","Diya - Approved Warm Palette","Fireworks - Native PBR"}.Select(n=>GameObjectUtility.FindExact(n)).Where(t=>t).Select(t=>new{t.name,pos=t.position.ToString(),scale=t.lossyScale.ToString(),rot=t.eulerAngles.ToString(),meshes=t.GetComponentsInChildren<MeshRenderer>(true).Select(r=>new{r.name,center=r.bounds.center.ToString(),size=r.bounds.size.ToString()})}),cameras=Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Select(c=>new {c.name,pos=c.transform.position.ToString(),rot=c.transform.eulerAngles.ToString()}),interactors=Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(i=>new{i.name,mode=i.farAttachMode.ToString(),pos=i.transform.position.ToString()}),games=games.Select(g=>new {type=g.GetType().Name,items=g.GetComponentsInChildren<GrabbableItem>(true).Select(i=>new{i.name,pos=i.transform.position.ToString(),scale=i.transform.lossyScale.ToString(),bounds=GameObjectUtility.BoundsOf(i.transform).ToString(),parent=i.transform.parent?i.transform.parent.name:"none"}),zones=g.GetComponentsInChildren<SnapZone>(true).Select(z=>new {z.name,pos=z.transform.position.ToString(),radius=z.captureRadius}),panels=g.GetComponentsInChildren<VenuePanel>(true).Select(p=>new{p.name,pos=p.transform.position.ToString(),rot=p.transform.eulerAngles.ToString(),scale=p.transform.lossyScale.ToString()})})};File.WriteAllText("../Game_Plans/Usability_Audit_2026-10-20.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));}
}
}
#endif
