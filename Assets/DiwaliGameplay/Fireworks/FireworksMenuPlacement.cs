using UnityEngine;
namespace DiwaliGameplay {
public sealed class FireworksMenuPlacement : MonoBehaviour {
 public FireworksGame owner;public VenuePanel panel;public Transform board;
 bool placed;
 void LateUpdate(){
  if(placed||!owner||!panel||!owner.plunger)return;
  var handle=owner.plungerFrame.TransformPoint(owner.plungerHome);
  var position=handle+Vector3.left*1.25f+Vector3.forward*.15f;
  position.y=1.5f;
  panel.autoFacePlayer=false;
  panel.FollowTaskAt(position,position+Vector3.back*2);
  owner.panelPosition=panel.transform.position;owner.panelRotation=panel.transform.rotation;
  placed=true;
 }
}
}
