#if UNITY_EDITOR
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace DiwaliGameplay {
public static class GrabCabinetCheck {
 [MenuItem("Diwali/Gameplay/Check Grab And Cabinet")]
 public static async void Run(){
 if(!EditorApplication.isPlaying)return;
 var g=Object.FindFirstObjectByType<SweetGame>();g.BeginGame();
 var before=g.glass.Select(t=>GameObjectUtility.BoundsOf(t)).ToArray();
 g.OpenCabinet();await Task.Delay(2500);
 var after=g.glass.Select(t=>GameObjectUtility.BoundsOf(t)).ToArray();
 var n=Object.FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None).First(i=>i.interactionAttachController.transformToFollow);
 var a=n.interactionAttachController;var h=a.transformToFollow;
 g.basket.grab.selectEntered.Invoke(new SelectEnterEventArgs{interactorObject=n,interactableObject=g.basket.grab});
 float offset=Vector3.Distance(a.GetOrCreateAnchorTransform().position,h.position);
 a.ResetOffset();
 var result=new {grabAnchorMetres=offset,grabOffsetCorrect=Mathf.Abs(offset-.5f)<.01f,doors=before.Select((b,i)=>new {index=i,travel=Vector3.Distance(b.center,after[i].center),clearsOriginalOpening=!b.Intersects(after[i])}).ToArray()};
 System.IO.File.WriteAllText("../Game_Plans/Grab_Cabinet_Check_2026-10-20.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
 ChristmasIslandQA.Render("Sweet-Cabinet-fully-open",new Vector3(-29.5f,1.7f,4.2f),new Vector3(-32,1.1f,1.5f),75);
 g.ResetGame();
 }
}
}
#endif
