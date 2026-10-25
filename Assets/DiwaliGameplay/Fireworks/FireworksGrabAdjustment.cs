using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace DiwaliGameplay {
public enum FireworksAdjustment { TargetHeight, RocketTilt, FountainHeight }

public sealed class FireworksGrabAdjustment : XRSimpleInteractable {
 public FireworksGame owner;
 public FireworksAdjustment adjustment;
 public float handTravel=.22f;
 Transform surface,hand;
 float startY,startValue;
 Plane dragPlane;
 bool rayDrag;
 BoxCollider target;
 TextMesh hint;
 MeshRenderer[] surfaces;
 public bool IsDragging=>isSelected;

 public static FireworksGrabAdjustment Create(FireworksGame game,FireworksAdjustment kind,Transform visual){
  var go=new GameObject(kind+" grab adjustment");go.SetActive(false);go.transform.SetParent(game.transform,true);go.transform.position=visual.position;
  var control=go.AddComponent<FireworksGrabAdjustment>();control.owner=game;control.adjustment=kind;control.surface=visual;
  control.surfaces=visual.GetComponentsInChildren<MeshRenderer>().Where(r=>r.GetComponent<MeshFilter>()).ToArray();
  // Give each adjustment its own solid target, independent of the carry interactable.
  control.target=go.AddComponent<BoxCollider>();control.colliders.Add(control.target);
  control.UpdateTarget();
  var label=new GameObject("Grip adjustment hint");label.transform.SetParent(go.transform,false);control.hint=label.AddComponent<TextMesh>();
  control.hint.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");control.hint.GetComponent<MeshRenderer>().sharedMaterial=control.hint.font.material;control.hint.fontSize=40;control.hint.characterSize=.008f;
  control.hint.anchor=TextAnchor.MiddleCenter;control.hint.alignment=TextAlignment.Center;
  return control;
 }
 public void SetAvailable(bool available){if(gameObject.activeSelf!=available)gameObject.SetActive(available);}
 public override bool IsSelectableBy(IXRSelectInteractor interactor)=>owner&&owner.CanAdjust(adjustment)&&base.IsSelectableBy(interactor);
 protected override void OnSelectEntered(SelectEnterEventArgs args){
  base.OnSelectEntered(args);
  var nearFar=args.interactorObject as NearFarInteractor;
  hand=nearFar!=null?nearFar.interactionAttachController.transformToFollow:args.interactorObject.GetAttachTransform(this);
  if(hand){
   var bounds=SurfaceBounds();
   rayDrag=nearFar!=null?nearFar.interactionAttachController.hasOffset:Vector3.Distance(hand.position,bounds.ClosestPoint(hand.position))>.35f;
   var normal=Vector3.ProjectOnPlane(hand.forward,Vector3.up).normalized;
   if(normal.sqrMagnitude<.01f)normal=Vector3.forward;
   dragPlane=new Plane(normal,bounds.center);
   startY=DragPoint().y;startValue=owner.AdjustmentValue(adjustment);
  }
 }
 protected override void OnSelectExited(SelectExitEventArgs args){base.OnSelectExited(args);hand=null;}
 void Update(){
  UpdateTarget();
  if(isSelected&&hand&&owner&&owner.CanAdjust(adjustment)){
   float delta=DragPoint().y-startY;
   if(Mathf.Abs(delta)>.008f)owner.ApplyGrabAdjustment(adjustment,Mathf.Clamp01(startValue+delta/Mathf.Max(.1f,handTravel)));
  }
  if(!hint||surfaces==null||surfaces.Length==0)return;
  var bounds=surfaces[0].bounds;foreach(var renderer in surfaces.Skip(1))if(renderer)bounds.Encapsulate(renderer.bounds);
  hint.transform.position=bounds.center+Vector3.back*(bounds.extents.z+.06f)+Vector3.up*.12f;
  var camera=Camera.main;if(camera)hint.transform.rotation=Quaternion.LookRotation(hint.transform.position-camera.transform.position,Vector3.up);
  hint.color=isSelected||isHovered?new Color(.35f,1,.65f):new Color(1,.85f,.5f);
  hint.text=(adjustment==FireworksAdjustment.RocketTilt?"POINT + HOLD GRIP\nDRAG UP / DOWN TO TILT":adjustment==FireworksAdjustment.TargetHeight?"POINT + HOLD GRIP\nDRAG BOARD UP / DOWN":"POINT + HOLD GRIP\nDRAG FOUNTAIN UP / DOWN")+"  "+Mathf.RoundToInt(owner.AdjustmentValue(adjustment)*100)+"%";
 }
 Bounds SurfaceBounds(){var b=surfaces[0].bounds;foreach(var r in surfaces.Skip(1))if(r)b.Encapsulate(r.bounds);return b;}
 void UpdateTarget(){if(!surface||!target||surfaces==null||surfaces.Length==0)return;var b=SurfaceBounds();transform.position=b.center;target.center=Vector3.zero;target.size=Vector3.Max(b.size+Vector3.one*.08f,Vector3.one*.16f);}
 Vector3 DragPoint(){if(!rayDrag)return hand.position;var ray=new Ray(hand.position,hand.forward);return dragPlane.Raycast(ray,out float distance)&&distance>0&&distance<10?ray.GetPoint(distance):hand.position;}
}
}
