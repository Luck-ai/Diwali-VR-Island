using UnityEngine;
namespace DiwaliGameplay {
public sealed class DiyaScrollComfort : MonoBehaviour {
 GrabbableItem item;Vector3 originalWorldScale;float heldScale=1;
 void Start(){item=GetComponent<GrabbableItem>();originalWorldScale=transform.lossyScale;var b=GameObjectUtility.BoundsOf(transform);heldScale=Mathf.Min(1,.48f/Mathf.Max(.01f,Mathf.Max(b.size.x,b.size.y,b.size.z)));}
 void LateUpdate(){if(!item)return;var desired=originalWorldScale*(item.IsHeld?heldScale:1);var parent=transform.parent?transform.parent.lossyScale:Vector3.one;transform.localScale=new Vector3(desired.x/parent.x,desired.y/parent.y,desired.z/parent.z);}
}
}
