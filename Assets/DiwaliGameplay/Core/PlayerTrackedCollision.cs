using UnityEngine;
using Unity.XR.CoreUtils;

namespace DiwaliGameplay {
[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(XROrigin),typeof(CharacterController))]
public sealed class PlayerTrackedCollision : MonoBehaviour {
 XROrigin origin;CharacterController body;Vector3 previousHeadLocal;bool ready;
 void Awake(){origin=GetComponent<XROrigin>();body=GetComponent<CharacterController>();}
 void OnEnable(){ready=false;}
 public void ResetReference(){if(!origin||!origin.Camera)return;previousHeadLocal=origin.Origin.transform.InverseTransformPoint(origin.Camera.transform.position);ready=true;}
 void LateUpdate(){ConstrainTrackedMotion();}
 public void ConstrainTrackedMotion(){
  if(!origin||!origin.Camera||!body||!body.enabled){ready=false;return;}
  if(!ready){ResetReference();return;}
  var frame=origin.Origin.transform;var head=origin.Camera.transform;
  var before=frame.TransformPoint(previousHeadLocal);var motion=Vector3.ProjectOnPlane(head.position-before,frame.up);
  if(motion.sqrMagnitude<=.000001f)return;
  if(motion.sqrMagnitude>.000001f){
   float height=Mathf.Clamp(origin.CameraInOriginSpaceHeight,.6f,2.5f);
   body.height=height;body.center=new Vector3(previousHeadLocal.x,height*.5f+body.skinWidth,previousHeadLocal.z);
   // Sweep only tracked motion; joystick movement and explicit travel already move the origin.
   var start=frame.position;body.Move(motion);var allowed=frame.position-start;
   frame.position=start+allowed-motion;
   var local=frame.InverseTransformPoint(head.position);body.center=new Vector3(local.x,height*.5f+body.skinWidth,local.z);
  }
  previousHeadLocal=frame.InverseTransformPoint(head.position);
 }
}
}
