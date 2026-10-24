using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit;
namespace DiwaliGameplay {
[RequireComponent(typeof(Rigidbody))]
public class GrabbableItem : MonoBehaviour {
 public static readonly System.Collections.Generic.HashSet<GrabbableItem> Active=new System.Collections.Generic.HashSet<GrabbableItem>();
 readonly System.Collections.Generic.List<(Collider item,Collider body)> ignoredCollisionPairs=new System.Collections.Generic.List<(Collider,Collider)>();
 public string itemId;
 public XRGrabInteractable grab;
 public bool IsHeld=>grab && grab.isSelected;
 public bool WasGrabbed {get;private set;}
 Vector3 initialPosition,initialScale; Quaternion initialRotation; Transform initialParent; bool captured;
 void OnEnable(){Active.Add(this);}
 void OnDisable(){Active.Remove(this);RestoreBodyCollisions();}
 void Awake(){Capture();grab=GetComponent<XRGrabInteractable>();ConfigureGrab();}
 public void ConfigureGrab(){if(!grab)grab=GetComponent<XRGrabInteractable>();if(!grab)return;grab.farAttachMode=InteractableFarAttachMode.Far;grab.trackScale=false;grab.movementType=XRBaseInteractable.MovementType.Kinematic;grab.throwOnDetach=false;grab.forceGravityOnDetach=false;grab.retainTransformParent=false;grab.useDynamicAttach=true;grab.matchAttachPosition=true;grab.matchAttachRotation=true;grab.snapToColliderVolume=true;grab.attachEaseInTime=.15f;grab.selectEntered.RemoveListener(OnGrab);grab.selectEntered.AddListener(OnGrab);grab.selectExited.RemoveListener(OnRelease);grab.selectExited.AddListener(OnRelease);}
 void OnGrab(SelectEnterEventArgs args){WasGrabbed=true;var heldPlayerBody=Object.FindFirstObjectByType<CharacterController>();if(heldPlayerBody)foreach(var collider in GetComponentsInChildren<Collider>())IgnoreWhileHeld(collider,heldPlayerBody);
  if(itemId=="diya-feather"||itemId=="diya-lamp"){
   transform.SetParent(null,true);
   var rig=Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
   if(rig)foreach(var col in GetComponentsInChildren<Collider>())foreach(var body in rig.GetComponentsInChildren<Collider>())IgnoreWhileHeld(col,body);
  }
  if(!grab.trackPosition)return;
  if(args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor nearFar){
   var anchor=nearFar.interactionAttachController;var hand=anchor.transformToFollow;
   if(hand && anchor.hasOffset)
    anchor.MoveTo(hand.position+hand.forward*.5f);
  }
 }
 void OnRelease(SelectExitEventArgs args){RestoreBodyCollisions();}
 void IgnoreWhileHeld(Collider item,Collider body){if(!item||!body||item==body||Physics.GetIgnoreCollision(item,body))return;Physics.IgnoreCollision(item,body,true);ignoredCollisionPairs.Add((item,body));}
 void RestoreBodyCollisions(){foreach(var pair in ignoredCollisionPairs)if(pair.item&&pair.body)Physics.IgnoreCollision(pair.item,pair.body,false);ignoredCollisionPairs.Clear();}
 public void Capture(){if(captured)return;initialPosition=transform.position;initialRotation=transform.rotation;initialParent=transform.parent;initialScale=transform.localScale;captured=true;}
 public void ResetItem(){Capture();if(grab)grab.enabled=false;RestoreBodyCollisions();transform.SetParent(initialParent,true);transform.localScale=initialScale;transform.SetPositionAndRotation(initialPosition,initialRotation);var rb=GetComponent<Rigidbody>();if(!rb.isKinematic){rb.linearVelocity=Vector3.zero;rb.angularVelocity=Vector3.zero;}rb.isKinematic=true;rb.position=initialPosition;rb.rotation=initialRotation;WasGrabbed=false;if(grab)grab.enabled=true;}
 public void SnapTo(Transform target,bool lockGrab=false){if(grab&&lockGrab)grab.enabled=false;transform.SetParent(target,true);transform.SetPositionAndRotation(target.position,target.rotation);var rb=GetComponent<Rigidbody>();if(!rb.isKinematic){rb.linearVelocity=Vector3.zero;rb.angularVelocity=Vector3.zero;}rb.isKinematic=true;rb.position=target.position;rb.rotation=target.rotation;if(grab)grab.enabled=!lockGrab;}
 void Update(){if(!IsHeld && transform.position.y < -8)ResetItem();}
}
}
