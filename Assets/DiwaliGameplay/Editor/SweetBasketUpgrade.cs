#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace DiwaliGameplay {
public static class SweetBasketUpgrade {
 public static void Configure(SweetGame game,Transform venue){
  var upper=GameObjectUtility.FindUnder(venue,"Circle.018");
  var existing=GameObjectUtility.FindExact("Festival basket — upper shelf");
  GrabbableItem second=existing?existing.GetComponent<GrabbableItem>():null;
  if(!second&&upper){
   var b=GameObjectUtility.BoundsOf(upper);
   second=VenueInstallHelpers.Grab(upper,"Festival basket — upper shelf",game.transform);
   second.transform.localScale*=.75f/GameObjectUtility.BoundsOf(second.transform).size.x;
   var p=b.center;p.y=b.min.y+GameObjectUtility.BoundsOf(second.transform).extents.y;second.transform.position=p;
  }
  if(!second)throw new System.InvalidOperationException("Upper basket mesh missing");
  var counter=GameObjectUtility.FindUnder(venue,"Cube.012");
  if(counter&&game.rest&&game.checkout){game.worktopY=SweetAccessibleLayout.CounterSurface(counter);float offset=game.basket.transform.position.y-GameObjectUtility.BoundsOf(game.basket.transform).min.y;foreach(var point in new[]{game.rest,game.checkout}){var p=point.position;p.y=game.worktopY+offset+.015f;point.position=p;EditorUtility.SetDirty(point);}}
  game.baskets=new[]{game.basket,second};foreach(var b in game.baskets)HollowColliders(b);EditorUtility.SetDirty(game);
 }
 public static void HollowColliders(GrabbableItem item){
  var bounds=new Bounds();bool first=true;
  foreach(var mf in item.GetComponentsInChildren<MeshFilter>())foreach(var v in mf.sharedMesh.vertices){var p=item.transform.InverseTransformPoint(mf.transform.TransformPoint(v));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
  foreach(var collider in item.GetComponentsInChildren<Collider>())collider.enabled=false;
  item.grab.colliders.Clear();
  void Box(Vector3 c,Vector3 size){var box=item.gameObject.AddComponent<BoxCollider>();box.center=c;box.size=size;item.grab.colliders.Add(box);}
  float wall=.045f,bodyHeight=bounds.size.y*.35f;
  var c=bounds.center;var min=bounds.min;var max=bounds.max;
  Box(new Vector3(c.x,min.y+wall*.5f,c.z),new Vector3(bounds.size.x,wall,bounds.size.z));
  Box(new Vector3(min.x+wall*.5f,min.y+bodyHeight*.5f,c.z),new Vector3(wall,bodyHeight,bounds.size.z));
  Box(new Vector3(max.x-wall*.5f,min.y+bodyHeight*.5f,c.z),new Vector3(wall,bodyHeight,bounds.size.z));
  Box(new Vector3(c.x,min.y+bodyHeight*.5f,min.z+wall*.5f),new Vector3(bounds.size.x,bodyHeight,wall));
  Box(new Vector3(c.x,min.y+bodyHeight*.5f,max.z-wall*.5f),new Vector3(bounds.size.x,bodyHeight,wall));
  var handle=new Bounds();first=true;float cutoff=min.y+bounds.size.y*.65f;
  foreach(var mf in item.GetComponentsInChildren<MeshFilter>())foreach(var v in mf.sharedMesh.vertices){var p=item.transform.InverseTransformPoint(mf.transform.TransformPoint(v));if(p.y<cutoff)continue;if(first){handle=new Bounds(p,Vector3.zero);first=false;}else handle.Encapsulate(p);}
  if(!first){
   bool alongX=handle.size.x>=handle.size.z;float depth=Mathf.Max(.035f,alongX?handle.size.z:handle.size.x);
   Box(new Vector3(handle.center.x,handle.max.y-wall*.5f,handle.center.z),alongX?new Vector3(handle.size.x,wall,depth):new Vector3(depth,wall,handle.size.z));
   float handleHeight=handle.max.y-(min.y+bodyHeight);
   for(int side=-1;side<=1;side+=2){var p=handle.center;p.y=handle.max.y-handleHeight*.5f;if(alongX)p.x+=side*(handle.extents.x-wall*.5f);else p.z+=side*(handle.extents.z-wall*.5f);Box(p,alongX?new Vector3(wall,handleHeight,depth):new Vector3(depth,handleHeight,wall));}
  }
  EditorUtility.SetDirty(item.grab);
 }
 [MenuItem("Diwali/Gameplay/Enable Both Sweet Baskets")]
 public static void Apply(){if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode first");var g=Object.FindFirstObjectByType<SweetGame>();var venue=GameObjectUtility.FindExact("SweetShop - Approved Warm Palette");Configure(g,venue);EditorSceneManager.MarkSceneDirty(g.gameObject.scene);EditorSceneManager.SaveScene(g.gameObject.scene);}
}
}
#endif
