using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace DiwaliGameplay {
public static class GameObjectUtility {
 public static Transform FindExact(string name)=>Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
 public static Transform FindUnder(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
 public static Bounds BoundsOf(Transform t){var rs=t.GetComponentsInChildren<Renderer>(true);var b=new Bounds(t.position,Vector3.zero);if(rs.Length>0){b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);}return b;}
 public static GrabbableItem MakeGrab(Transform t,string id){if(t.GetComponentsInChildren<MeshFilter>(true).Length==0)throw new System.InvalidOperationException("Cannot make invisible grab wrapper: "+id+". Unpack venue prefab before reparenting its mesh.");var old=t.GetComponents<Collider>();foreach(var c in old)c.enabled=false;var localBounds=new Bounds(Vector3.zero,Vector3.zero);bool first=true;foreach(var mf in t.GetComponentsInChildren<MeshFilter>()){if(!mf.sharedMesh)continue;var mb=mf.sharedMesh.bounds;for(int k=0;k<8;k++){var corner=mb.center+Vector3.Scale(mb.extents,new Vector3((k&1)==0?-1:1,(k&2)==0?-1:1,(k&4)==0?-1:1));var p=t.InverseTransformPoint(mf.transform.TransformPoint(corner));if(first){localBounds=new Bounds(p,Vector3.zero);first=false;}else localBounds.Encapsulate(p);}}var box=t.gameObject.AddComponent<BoxCollider>();box.center=localBounds.center;box.size=localBounds.size;var rb=t.GetComponent<Rigidbody>();if(!rb)rb=t.gameObject.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;var grab=t.GetComponent<XRGrabInteractable>();if(!grab)grab=t.gameObject.AddComponent<XRGrabInteractable>();grab.movementType=XRGrabInteractable.MovementType.Kinematic;grab.throwOnDetach=false;var item=t.GetComponent<GrabbableItem>();if(!item)item=t.gameObject.AddComponent<GrabbableItem>();item.itemId=id;item.grab=grab;item.ConfigureGrab();return item;}
 public static SnapZone MakeZone(Transform parent,string id,Vector3 worldPosition,float radius=.25f){var go=new GameObject(id+" socket");go.transform.SetParent(parent,true);go.transform.position=worldPosition;var z=go.AddComponent<SnapZone>();z.acceptedId=id;z.radius=radius;return z;}
}
}
