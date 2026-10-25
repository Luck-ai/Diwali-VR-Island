#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
namespace DiwaliGameplay {
public static class QuickMenuBasketVerification {
 public static bool Running;
 static readonly List<object> rows=new List<object>();static int failures;
 static readonly string Folder=Path.GetFullPath("Documentation/Stationary_Menu_Basket_2026-10-25");
 static void Check(string name,bool pass,string detail=""){rows.Add(new{name,pass,detail});if(!pass)failures++;}
 static Bounds MeshBounds(GrabbableItem item,Transform frame){var bounds=new Bounds();bool first=true;foreach(var filter in item.GetComponentsInChildren<MeshFilter>(true)){if(!filter.sharedMesh||filter.GetComponentInParent<GrabbableItem>()!=item)continue;var b=filter.sharedMesh.bounds;for(int i=0;i<8;i++){var p=frame.InverseTransformPoint(filter.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}}return bounds;}
 public static async void Run(){
  if(Running||!Application.isPlaying)return;Running=true;rows.Clear();failures=0;Directory.CreateDirectory(Folder);
  var diya=UnityEngine.Object.FindFirstObjectByType<DiyaGame>();var sweet=UnityEngine.Object.FindFirstObjectByType<SweetGame>();var head=Camera.main.transform;var rig=head.root;var saved=rig.position;
  var locomotion=UnityEngine.Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.XRBodyTransformer>();bool enabled=locomotion&&locomotion.enabled;if(locomotion)locomotion.enabled=false;
  try{
   var floor=GameObjectUtility.FindUnder(GameObjectUtility.FindExact("Diya - Approved Warm Palette"),"Plane.002").GetComponent<Renderer>().bounds;
   for(int step=0;step<=10;step++){
    diya.BeginGame();for(int i=0;i<step;i++)diya.Credit("layout-"+i);await Task.Delay(100);
    var panel=diya.GetComponentInChildren<VenuePanel>();var position=panel.transform.position;var rotation=panel.transform.rotation;
    bool inside=position.x-.61f>floor.min.x&&position.x+.61f<floor.max.x&&position.z-.61f>floor.min.z&&position.z+.61f<floor.max.z;
    var blocked=Physics.OverlapBox(position,new Vector3(.6f,.6f,.05f),rotation,~0,QueryTriggerInteraction.Ignore).Where(c=>!c.transform.IsChildOf(rig)&&!c.transform.IsChildOf(panel.transform)).Select(c=>c.name).ToArray();
    Check("Diya stage "+step+" inside courtyard and clear",inside&&blocked.Length==0,string.Join(",",blocked));
    foreach(var eye in new[]{new Vector3(28.1f,1.7f,8),new Vector3(32.8f,1.7f,.2f),position+Vector3.forward*.25f}){
     rig.position+=eye-head.position;await Task.Delay(75);
     Check("Diya stage "+step+" stays fixed as player moves",Vector3.Distance(position,panel.transform.position)<.001f&&Quaternion.Angle(rotation,panel.transform.rotation)<.01f&&!panel.autoFacePlayer);
    }
   }
   diya.ResetGame();
   var floorMethod=typeof(SweetGame).GetMethod("BasketFloor",BindingFlags.NonPublic|BindingFlags.Instance);
   var menus=new[]{new[]{"Gujiya","Gujiya","Gujiya","Gujiya"},new[]{"Modak","Modak","Modak","Modak"},new[]{"Barfi","Barfi","Barfi","Barfi"},new[]{"Gujiya","Modak","Barfi","Gujiya"}};
   int basketIndex=0;
   foreach(var basket in sweet.baskets){foreach(var types in menus)foreach(float yaw in new[]{0f,45f,90f,180f}){
    sweet.BeginGame();sweet.OpenCabinet();sweet.basket=basket;foreach(var slot in sweet.slots)slot.SetParent(basket.transform,false);basket.SnapTo(sweet.rest);basket.transform.rotation=Quaternion.Euler(0,yaw,0)*basket.transform.rotation;sweet.basketReady=true;
    foreach(var type in types){var item=sweet.sweets.FirstOrDefault(x=>x.itemId==type&&!sweet.selected.Contains(x));Check("Pack "+type,item&&sweet.AddSweet(item));}
    var footprint=(Bounds)floorMethod.Invoke(sweet,new object[]{basket});
    foreach(var item in sweet.selected){var b=MeshBounds(item,basket.transform);bool fits=b.min.x>footprint.min.x+.001f&&b.max.x<footprint.max.x-.001f&&b.min.z>footprint.min.z+.001f&&b.max.z<footprint.max.z-.001f;
     Check("Basket "+basketIndex+" "+item.itemId+" yaw "+yaw+" inside rim",fits,"sweet="+b+",floor="+footprint);
     Check("Packed sweet remains attached and grabbable",item.transform.IsChildOf(basket.transform)&&item.GetComponent<Rigidbody>().isKinematic&&item.grab.enabled);
    }
    if(yaw==0&&types==menus[3]){var b=MeshBounds(basket,basket.transform);var look=basket.transform.TransformPoint(b.center);File.Copy(ChristmasIslandQA.Render("Basket-rim-"+basketIndex,look+new Vector3(.65f,.6f,.75f),look,55),Path.Combine(Folder,"Basket-"+basketIndex+".png"),true);}
   }basketIndex++;}
  }catch(Exception e){Check("exception",false,e.ToString());}
  finally{sweet.ResetGame();diya.ResetGame();rig.position=saved;if(locomotion)locomotion.enabled=enabled;Running=false;File.WriteAllText(Path.Combine(Folder,"Focused_Play_Mode.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{state=failures==0?"passed":"failed",failures,scope="Diya menu world position and rotation under player movement at all stages; basket mesh containment for repeated and mixed sweets, both baskets, four yaw angles. Desktop Unity Play Mode.",results=rows},Newtonsoft.Json.Formatting.Indented));Debug.Log("Focused menu/basket check: "+failures+" failures");}
 }
}
}
#endif
