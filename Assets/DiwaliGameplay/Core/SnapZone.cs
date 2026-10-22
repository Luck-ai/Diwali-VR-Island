using System;
using UnityEngine;
namespace DiwaliGameplay {
public class SnapZone : MonoBehaviour {
 public string acceptedId; public float radius=.25f; public GrabbableItem occupant;
 public Action<GrabbableItem> onSnapped; public Func<GrabbableItem,bool> canAccept;
 public bool enabledForGame=true;
 LineRenderer guide; Material guideMaterial;
 public float captureRadius=>Mathf.Max(radius,1f);
 void Start(){foreach(var item in GrabbableItem.Active)if(item&&item.itemId==acceptedId){DiyaRestTarget.Create(transform,transform,item,captureRadius,()=>enabledForGame&&!occupant&&item.WasGrabbed&&(canAccept==null||canAccept(item)));break;}}

 void Update(){if(occupant && (occupant.IsHeld || Vector3.Distance(occupant.transform.position,transform.position)>radius*1.8f))occupant=null;
 UpdateGuide();
 if(!enabledForGame||occupant)return;
 foreach(var item in GrabbableItem.Active)if(TryAccept(item))break;}
 void UpdateGuide(){if(!guide)return;GrabbableItem candidate=null;if(enabledForGame&&!occupant)foreach(var item in GrabbableItem.Active)if(item&&item.IsHeld&&item.itemId==acceptedId&&(canAccept==null||canAccept(item))){candidate=item;break;}guide.enabled=candidate;if(!candidate)return;bool close=Vector3.Distance(candidate.transform.position,transform.position)<=captureRadius;var color=close?new Color(.25f,1,.55f):new Color(1,.72f,.2f);guide.startColor=guide.endColor=color;if(guideMaterial)guideMaterial.color=color;for(int i=0;i<48;i++){float angle=i*Mathf.PI*2/48;guide.SetPosition(i,transform.position+new Vector3(Mathf.Cos(angle)*.22f,.025f,Mathf.Sin(angle)*.22f));}}
 public bool TryAccept(GrabbableItem item){if(!enabledForGame||occupant||!item||!item.WasGrabbed||item.IsHeld||item.itemId!=acceptedId||!DiyaRestTarget.Contains(item,transform,captureRadius)||(canAccept!=null&&!canAccept(item)))return false;occupant=item;item.SnapTo(transform);onSnapped?.Invoke(item);return true;}
 void OnDestroy(){if(guideMaterial)Destroy(guideMaterial);}
}
}
