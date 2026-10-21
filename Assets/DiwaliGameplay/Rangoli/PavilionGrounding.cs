using UnityEngine;
namespace DiwaliGameplay {
public static class PavilionGrounding {
 public static void Ground(RangoliGame game){
  if(!game.pavilionSurface||game.decorations==null||game.decorations.Length==0)return;
  var center=GameObjectUtility.BoundsOf(game.decorations[0]);
  float surfaceY=game.pavilionFloor?GameObjectUtility.BoundsOf(game.pavilionFloor).max.y:0;
  bool found=false;
  foreach(var offset in new[]{Vector3.zero,Vector3.right*.3f,Vector3.left*.3f,Vector3.forward*.3f,Vector3.back*.3f}){
   var origin=center.center+offset;origin.y=center.min.y+.15f;
   if(game.pavilionSurface.Raycast(new Ray(origin,Vector3.down),out var hit,8)&&Vector3.Dot(hit.normal,Vector3.up)>.65f){surfaceY=found?Mathf.Max(surfaceY,hit.point.y):hit.point.y;found=true;}
  }
  foreach(var part in game.decorations){if(!part)continue;var b=GameObjectUtility.BoundsOf(part);part.position+=Vector3.up*(surfaceY+.012f-b.min.y);}
 }
}
}
