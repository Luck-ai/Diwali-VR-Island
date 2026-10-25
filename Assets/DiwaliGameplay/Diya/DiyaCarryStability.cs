using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace DiwaliGameplay {
public sealed class DiyaCarryStability : XRGeneralGrabTransformer {
 InteractionAttachController attachment;
 bool velocityScaling,momentum,manipulation,smoothing;
 MotionStabilizationMode stabilization;
 Transform rig;Vector3 lastPosition,lastRigPosition;BoxCollider shape;
 readonly RaycastHit[] hits=new RaycastHit[32];
 public void Initialise(GrabbableItem item){
  shape=GetComponent<BoxCollider>();
  foreach(var other in GetComponents<XRGeneralGrabTransformer>())if(other!=this){other.enabled=false;item.grab.RemoveSingleGrabTransformer(other);}
  item.grab.AddSingleGrabTransformer(this);item.grab.selectExited.AddListener(Released);
 }
 public override void OnGrab(XRGrabInteractable grab){
  base.OnGrab(grab);lastPosition=transform.position;rig=Camera.main?Camera.main.transform.root:null;lastRigPosition=rig?rig.position:Vector3.zero;
  if(grab.interactorsSelecting.Count==0||!(grab.interactorsSelecting[0] is NearFarInteractor near))return;
  attachment=near.interactionAttachController as InteractionAttachController;if(!attachment)return;
  velocityScaling=attachment.useDistanceBasedVelocityScaling;momentum=attachment.useMomentum;manipulation=attachment.useManipulationInput;smoothing=attachment.smoothOffset;stabilization=attachment.motionStabilizationMode;
  attachment.useDistanceBasedVelocityScaling=false;attachment.useMomentum=false;attachment.useManipulationInput=false;attachment.smoothOffset=false;attachment.motionStabilizationMode=MotionStabilizationMode.Never;
 }
 void Released(SelectExitEventArgs args){RestoreAttachment();}
 void OnDisable(){RestoreAttachment();}
 void RestoreAttachment(){if(!attachment)return;attachment.useDistanceBasedVelocityScaling=velocityScaling;attachment.useMomentum=momentum;attachment.useManipulationInput=manipulation;attachment.smoothOffset=smoothing;attachment.motionStabilizationMode=stabilization;attachment=null;}
 bool Blocks(Collider collider){return collider&&!collider.transform.IsChildOf(transform)&&!(rig&&collider.transform.IsChildOf(rig))&&!collider.isTrigger&&!(collider.GetComponentInParent<GrabbableItem>() is GrabbableItem item&&item.IsHeld);}
 public override void Process(XRGrabInteractable grab,XRInteractionUpdateOrder.UpdatePhase phase,ref Pose target,ref Vector3 localScale){
  base.Process(grab,phase,ref target,ref localScale);
  if(grab.interactorsSelecting.Count==0)return;
  var interactor=grab.interactorsSelecting[0];var hand=interactor is NearFarInteractor near?near.interactionAttachController.transformToFollow:interactor.transform;if(!hand)return;
  target.position=hand.position+Vector3.ClampMagnitude(target.position-hand.position,.8f);
  if(rig&&Vector3.Distance(rig.position,lastRigPosition)>2f)lastPosition=hand.position;
  if(shape){
   var scale=transform.lossyScale;var center=target.rotation*Vector3.Scale(shape.center,scale);var half=Vector3.Scale(shape.size*.48f,new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z)));
   var delta=target.position-lastPosition;float distance=delta.magnitude;
   if(distance>.0001f){
    int count=Physics.BoxCastNonAlloc(lastPosition+center,half,delta/distance,hits,target.rotation,distance+.015f,~0,QueryTriggerInteraction.Ignore);float allowed=distance;
    for(int i=0;i<count;i++)if(hits[i].distance>.001f&&Blocks(hits[i].collider))allowed=Mathf.Min(allowed,Mathf.Max(0,hits[i].distance-.015f));
    target.position=lastPosition+delta/distance*allowed;
   }
  }
  lastPosition=target.position;if(rig)lastRigPosition=rig.position;
 }
}
}
