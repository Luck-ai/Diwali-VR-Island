using UnityEngine;
namespace DiwaliGameplay {
public static class PavilionGrounding {
 public static void Ground(RangoliGame game){
  if(game.preserveAuthoredLayout)return;
  if(!game.pavilionSurface||game.decorations==null||game.decorations.Length==0)return;
  var center=GameObjectUtility.BoundsOf(game.decorations[0]);
  float surfaceY=game.pavilionFloor?GameObjectUtility.BoundsOf(game.pavilionFloor).max.y:0;
  bool found=false;var dais=GameObjectUtility.FindExact("Rangoli display dais");var floorCollider=dais?dais.GetComponent<Collider>():game.pavilionSurface;
  foreach(var offset in new[]{Vector3.zero,Vector3.right*.3f,Vector3.left*.3f,Vector3.forward*.3f,Vector3.back*.3f}){
   var origin=center.center+offset;origin.y=center.min.y+.15f;
   if(floorCollider.Raycast(new Ray(new Vector3(origin.x,3.5f,origin.z),Vector3.down),out var hit,8)&&Vector3.Dot(hit.normal,Vector3.up)>.65f){surfaceY=found?Mathf.Max(surfaceY,hit.point.y):hit.point.y;found=true;}
  }
  for(int i=0;i<game.decorations.Length;i++){var part=game.decorations[i];if(!part)continue;var b=GameObjectUtility.BoundsOf(part);float support=surfaceY;
   if(game.decorationGroups!=null&&i<game.decorationGroups.Length&&game.decorationGroups[i]==1){var origin=b.center;origin.y=3.5f;if(game.pavilionSurface.Raycast(new Ray(origin,Vector3.down),out var edgeHit,6)&&Vector3.Dot(edgeHit.normal,Vector3.up)>.65f)support=edgeHit.point.y;}
   part.position+=Vector3.up*(support+.012f-b.min.y);
  }
 }
}
}
