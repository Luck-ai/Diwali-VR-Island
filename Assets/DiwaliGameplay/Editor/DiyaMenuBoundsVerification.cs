#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace DiwaliGameplay {
public static class DiyaMenuBoundsVerification {
 public static bool Running;
 static readonly List<object> rows=new List<object>();
 static int failures;
 static readonly string Folder=Path.GetFullPath("Documentation/Diya_Menu_Bounds_2026-10-25");
 static void Check(string name,bool passed,string detail=""){rows.Add(new{name,passed,detail});if(!passed)failures++;}
 static async Task Wait(int ms=100){await Task.Delay(ms);}
 public static async void Run(){
  if(Running||!Application.isPlaying)return;Running=true;rows.Clear();failures=0;Directory.CreateDirectory(Folder);
  var game=UnityEngine.Object.FindFirstObjectByType<DiyaGame>();var head=Camera.main.transform;var rig=head.root;var saved=rig.position;
  var locomotion=UnityEngine.Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.XRBodyTransformer>();bool enabled=locomotion&&locomotion.enabled;if(locomotion)locomotion.enabled=false;
  try{
   var venue=GameObjectUtility.FindExact("Diya - Approved Warm Palette");var floor=GameObjectUtility.FindUnder(venue,"Plane.002").GetComponent<Renderer>().bounds;
   var heads=new[]{new Vector3(30.4f,1.7f,3.8f),new Vector3(32.8f,1.7f,7.4f),new Vector3(28.1f,1.7f,8.0f),new Vector3(31.5f,1.7f,.2f),new Vector3(32.4f,1.7f,4.2f),new Vector3(30.4f,1.7f,9.7f),new Vector3(24.5f,1.7f,4),new Vector3(24.8f,1.7f,-2),new Vector3(36.2f,1.7f,-2)};
   for(int step=0;step<=10;step++){
    game.BeginGame();for(int i=0;i<step;i++)game.Credit("menu-layout-fixture-"+i);await Wait();
    var panel=game.GetComponentInChildren<VenuePanel>();
    for(int i=0;i<heads.Length;i++){
     rig.position+=heads[i]-head.position;await Wait();
     var position=panel.transform.position;float gap=Vector3.ProjectOnPlane(position-head.position,Vector3.up).magnitude;
     bool inside=position.x-.61f>floor.min.x&&position.x+.61f<floor.max.x&&position.z-.61f>floor.min.z&&position.z+.61f<floor.max.z;
     var blocked=Physics.OverlapBox(position,new Vector3(.60f,.60f,.05f),panel.transform.rotation,~0,QueryTriggerInteraction.Ignore).Where(c=>!c.transform.IsChildOf(rig)&&!c.transform.IsChildOf(panel.transform)).Select(c=>c.name).ToArray();
     Check("Stage "+step+" head "+i,inside&&gap>=1.70f&&blocked.Length==0,"menu="+position.ToString("F2")+",headGap="+gap.ToString("F2")+",collisions="+string.Join(",",blocked));
    }
    var stable=panel.transform.position;await Wait(250);Check("Stage "+step+" stationary player does not cause drift",Vector3.Distance(stable,panel.transform.position)<.01f);
    if(step==0||step==3||step==5||step==6||step==9){
     rig.position+=new Vector3(30.4f,1.7f,3.8f)-head.position;await Wait();
     var image=ChristmasIslandQA.Render("Diya-bounded-menu-stage-"+step,new Vector3(29.2f,3.2f,5.5f),panel.transform.position,85);
     File.Copy(image,Path.Combine(Folder,"Stage-"+step+".png"),true);
    }
   }
  }catch(Exception e){Check("exception",false,e.ToString());}
  finally{
   game.ResetGame();rig.position=saved;if(locomotion)locomotion.enabled=enabled;Running=false;
   File.WriteAllText(Path.Combine(Folder,"Layout_Play_Mode.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{state=failures==0?"passed":"failed",failures,scope="Menu layout fixtures for all 11 stages, nine player positions per stage; checks full horizontal panel clearance, physical overlaps, head distance, stationary drift. Gameplay interactions verified separately.",results=rows},Newtonsoft.Json.Formatting.Indented));
   Debug.Log("Diya menu bounds verification: "+failures+" failures");
  }
 }
}
}
#endif
