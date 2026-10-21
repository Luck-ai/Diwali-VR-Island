#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace FireworksImport {
public static class FireworksSceneInspector {
[MenuItem("Diwali Island/Fireworks/Inspect")]
public static void Inspect(){
 var scene=SceneManager.GetActiveScene();
 var plot=GameObject.Find("Diwali Festival Ground/06 Five Empty Venue Plots/05 FESTIVAL GAMES - EMPTY 10m x 10m");
 var data=new {scene=scene.path,dirty=scene.isDirty,plot=plot.GetComponentsInChildren<Transform>(true).Select(t=>new{name=t.name,localPosition=t.localPosition,position=t.position,scale=t.localScale,rotation=t.eulerAngles,renderer=t.GetComponent<Renderer>()?new{bounds=t.GetComponent<Renderer>().bounds}:null}).ToArray()};
 File.WriteAllText("AgentScripts/FireworksImport/live-inspection.json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented,new Newtonsoft.Json.JsonSerializerSettings{ReferenceLoopHandling=Newtonsoft.Json.ReferenceLoopHandling.Ignore}));
}
}}
#endif
