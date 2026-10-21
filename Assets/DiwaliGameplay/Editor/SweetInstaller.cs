#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace DiwaliGameplay {
public static class VenueInstallHelpers {
 public static Transform Part(Transform root,string name){var p=GameObjectUtility.FindUnder(root,name);if(!p)throw new InvalidOperationException("Missing venue part: "+name);return p;}
 public static Transform Point(Transform parent,string name,Vector3 world){var t=new GameObject(name).transform;t.SetParent(parent,true);t.position=world;return t;}
 public static Transform Pivot(Transform mesh,string name,Transform parent){var t=Point(parent,name,GameObjectUtility.BoundsOf(mesh).center);t.rotation=Quaternion.identity;mesh.SetParent(t,true);if(mesh.parent!=t)throw new InvalidOperationException("Native mesh parenting failed: "+mesh.name+". Unpack its venue first.");return t;}
 public static GrabbableItem Grab(Transform mesh,string id,Transform parent){return GameObjectUtility.MakeGrab(Pivot(mesh,id,parent),id);}
 public static Vector3 At(Transform venue,Vector3 local)=>venue.TransformPoint(local);
}
public static class SweetInstaller {
 public static VenueGame Install(Transform gameplayRoot){
  var venue=GameObjectUtility.FindExact("SweetShop - Approved Warm Palette")??GameObjectUtility.FindExact("SweetShop - Native PBR");if(!venue)throw new InvalidOperationException("SweetShop - Native PBR missing");
  var layout=SweetAccessibleLayout.Apply(venue);
  var root=new GameObject("Sweet gift gameplay").transform;root.SetParent(gameplayRoot,false);var g=root.gameObject.AddComponent<SweetGame>();
  Func<Vector3,Vector3> at=p=>VenueInstallHelpers.At(venue,p);
  var original=VenueInstallHelpers.Part(venue,"Circle");var shelfBounds=GameObjectUtility.BoundsOf(original);g.basket=VenueInstallHelpers.Grab(original,"Festival basket",root);g.basket.transform.localScale=Vector3.one*(.75f/Mathf.Max(.01f,GameObjectUtility.BoundsOf(g.basket.transform).size.x));float halfHeight=GameObjectUtility.BoundsOf(g.basket.transform).extents.y;var initial=shelfBounds.center;initial.y=shelfBounds.min.y+halfHeight;g.basket.transform.position=initial;
  SweetBasketUpgrade.Configure(g,venue);
  g.card=VenueInstallHelpers.Grab(VenueInstallHelpers.Part(venue,"Plane"),"Festival card",root);
  g.rest=VenueInstallHelpers.Point(root,"Basket rest — place here",layout.rest+Vector3.up*(halfHeight+.015f));g.checkout=VenueInstallHelpers.Point(root,"Checkout recess",layout.dock+Vector3.up*(halfHeight+.015f));g.collection=VenueInstallHelpers.Point(root,"Collection",layout.dock-venue.forward*1.6f+Vector3.up*halfHeight);g.cardReader=VenueInstallHelpers.Point(root,"Tap card here",layout.reader);g.worktopY=layout.worktop;g.workspaceForward=-venue.forward;
  g.slots=new Transform[4];for(int i=0;i<4;i++){g.slots[i]=VenueInstallHelpers.Point(g.basket.transform,"Sweet slot "+(i+1),g.basket.transform.position);g.slots[i].localPosition=new Vector3((i%2==0?-.2f:.2f),.08f,(i<2?-.2f:.2f));}
  var sweets=new List<GrabbableItem>();for(int i=1;i<=17;i++)sweets.Add(VenueInstallHelpers.Grab(VenueInstallHelpers.Part(venue,"Circle."+i.ToString("000")),"Gujiya",root));for(int i=3;i<=11;i++)sweets.Add(VenueInstallHelpers.Grab(VenueInstallHelpers.Part(venue,"Cube."+i.ToString("000")),"Barfi",root));for(int i=1;i<=10;i++)sweets.Add(VenueInstallHelpers.Grab(VenueInstallHelpers.Part(venue,"Sphere."+i.ToString("000")),"Modak",root));g.sweets=sweets.ToArray();g.gujiyaRestRotation=Quaternion.FromToRotation(venue.TransformDirection(Vector3.right),Vector3.up);
  g.glass=new[]{VenueInstallHelpers.Part(venue,"Cube.001"),VenueInstallHelpers.Part(venue,"Cube.002")};g.closedGlass=g.glass.Select(t=>t.localPosition).ToArray();
  g.panelPosition=at(new Vector3(.4f,0,-1.4f));g.panelPosition=new Vector3(g.panelPosition.x,venue.position.y+1.65f,g.panelPosition.z);var toward=venue.TransformPoint(Vector3.zero)-g.panelPosition;toward.y=0;g.panelRotation=Quaternion.LookRotation(-toward.normalized,Vector3.up);
  return g;
 }
}
}
#endif
