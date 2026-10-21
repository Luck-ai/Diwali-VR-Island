#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
namespace DiwaliGameplay {
public static class SweetAccessibleLayout {
 public static float CounterSurface(Transform counter){
  var areas=new Dictionary<int,float>();var heights=new Dictionary<int,float>();
  foreach(var mf in counter.GetComponentsInChildren<MeshFilter>()){
   var mesh=mf.sharedMesh;if(!mesh)continue;var vertices=mesh.vertices;var triangles=mesh.triangles;
   for(int i=0;i<triangles.Length;i+=3){var a=mf.transform.TransformPoint(vertices[triangles[i]]);var b=mf.transform.TransformPoint(vertices[triangles[i+1]]);var c=mf.transform.TransformPoint(vertices[triangles[i+2]]);
    if(Mathf.Abs(a.y-b.y)>.001f||Mathf.Abs(a.y-c.y)>.001f)continue;
    var area=Mathf.Abs(Vector3.Cross(b-a,c-a).y)*.5f;int bin=Mathf.RoundToInt(a.y*100);
    if(!areas.ContainsKey(bin)){areas[bin]=0;heights[bin]=a.y;}areas[bin]+=area;
   }
  }
  float top=float.NegativeInfinity;foreach(var pair in areas)if(pair.Value>.1f)top=Mathf.Max(top,heights[pair.Key]);
  if(float.IsNegativeInfinity(top))throw new System.InvalidOperationException("No broad horizontal counter surface found");return top;
 }
 public struct Result {public Vector3 rest,dock,initial,reader;public float worktop;}
 public static Result Apply(Transform venue){float floor=venue.position.y+.03f;var cabinet=VenueInstallHelpers.Part(venue,"Cube");var cb=GameObjectUtility.BoundsOf(cabinet);var cabinetRoot=VenueInstallHelpers.Point(venue,"Accessible cabinet group",new Vector3(cb.center.x,cb.min.y,cb.center.z));var parts=new List<Transform>{cabinet,VenueInstallHelpers.Part(venue,"Cube.001"),VenueInstallHelpers.Part(venue,"Cube.002")};for(int i=1;i<=17;i++)parts.Add(VenueInstallHelpers.Part(venue,"Circle."+i.ToString("000")));for(int i=3;i<=11;i++)parts.Add(VenueInstallHelpers.Part(venue,"Cube."+i.ToString("000")));for(int i=1;i<=10;i++)parts.Add(VenueInstallHelpers.Part(venue,"Sphere."+i.ToString("000")));foreach(var p in parts)p.SetParent(cabinetRoot,true);cabinetRoot.localScale*=.65f;var cp=cabinetRoot.position;cp.y=floor;cabinetRoot.position=cp;
 var counter=VenueInstallHelpers.Part(venue,"Cube.012");var card=VenueInstallHelpers.Part(venue,"Plane");var old=GameObjectUtility.BoundsOf(counter);float oldTop=old.min.y+old.size.y*.625f;var counterRoot=VenueInstallHelpers.Point(venue,"Accessible checkout group",new Vector3(old.center.x,old.min.y,old.center.z));counter.SetParent(counterRoot,true);card.SetParent(counterRoot,true);counterRoot.localScale*=.86f;var currentCabinet=GameObjectUtility.BoundsOf(cabinet);var right=venue.right;var front=-venue.forward;float cabinetWidth=currentCabinet.size.x;float counterWidth=old.size.x*.86f;var target=currentCabinet.center+right*(cabinetWidth*.5f+counterWidth*.5f+.12f)+front*.25f;target.y=floor;counterRoot.position=target;float top=CounterSurface(counter);var center=GameObjectUtility.BoundsOf(counter).center;var rest=center-right*(counterWidth*.24f)+front*.1f;var dock=center+right*(counterWidth*.24f)+front*.1f;rest.y=dock.y=top;var initial=center+front*.65f;initial.y=top;return new Result{rest=rest,dock=dock,initial=initial,reader=card.position+Vector3.up*.1f,worktop=top};}
}
}
#endif
