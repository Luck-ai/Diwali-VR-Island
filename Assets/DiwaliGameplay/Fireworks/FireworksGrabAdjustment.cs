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
 TextMesh hint;
 MeshRenderer[] surfaces;
 public bool IsDragging=>isSelected;

 public static FireworksGrabAdjustment Create(FireworksGame game,FireworksAdjustment kind,Transform visual){
  var go=new GameObject(kind+" grab adjustment");go.SetActive(false);go.transform.SetParent(game.transform,true);go.transform.position=visual.position;
  var control=go.AddComponent<FireworksGrabAdjustment>();control.owner=game;control.adjustment=kind;control.surface=visual;
  control.surfaces=visual.GetComponentsInChildren<MeshRenderer>().Where(r=>r.GetComponent<MeshFilter>()).ToArray();
  var grab=visual.GetComponent<XRGrabInteractable>();
  if(grab)control.colliders.AddRange(grab.colliders.Where(c=>c&&c.enabled&&!c.isTrigger));
  else control.colliders.AddRange(visual.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger));
  if(control.colliders.Count==0){
   var bounds=new Bounds();bool first=true;
   foreach(var filter in visual.GetComponentsInChildren<MeshFilter>()){
    if(!filter.sharedMesh)continue;var b=filter.sharedMesh.bounds;
    for(int i=0;i<8;i++){
     var corner=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
     var p=visual.InverseTransformPoint(filter.transform.TransformPoint(corner));
     if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);
    }
   }
   var collider=visual.gameObject.AddComponent<BoxCollider>();collider.center=bounds.center;collider.size=bounds.size;control.colliders.Add(collider);
  }
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
  if(hand){startY=hand.position.y;startValue=owner.AdjustmentValue(adjustment);}
 }
 protected override void OnSelectExited(SelectExitEventArgs args){base.OnSelectExited(args);hand=null;}
 void Update(){
  if(surface)transform.position=surface.position;
  if(isSelected&&hand&&owner&&owner.CanAdjust(adjustment)){
   float delta=hand.position.y-startY;
   if(Mathf.Abs(delta)>.008f)owner.ApplyGrabAdjustment(adjustment,Mathf.Clamp01(startValue+delta/Mathf.Max(.1f,handTravel)));
  }
  if(!hint||surfaces==null||surfaces.Length==0)return;
  var bounds=surfaces[0].bounds;foreach(var renderer in surfaces.Skip(1))if(renderer)bounds.Encapsulate(renderer.bounds);
  hint.transform.position=bounds.center+Vector3.back*(bounds.extents.z+.06f)+Vector3.up*.12f;
  var camera=Camera.main;if(camera)hint.transform.rotation=Quaternion.LookRotation(hint.transform.position-camera.transform.position,Vector3.up);
  hint.color=isSelected||isHovered?new Color(.35f,1,.65f):new Color(1,.85f,.5f);
  hint.text=(adjustment==FireworksAdjustment.RocketTilt?"GRIP ROCKET + MOVE UP / DOWN\nTILT":adjustment==FireworksAdjustment.TargetHeight?"GRIP BOARD + MOVE UP / DOWN":"GRIP FOUNTAIN + MOVE UP / DOWN")+"  "+Mathf.RoundToInt(owner.AdjustmentValue(adjustment)*100)+"%";
 }
}
}
