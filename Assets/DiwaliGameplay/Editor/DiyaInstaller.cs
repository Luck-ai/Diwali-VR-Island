#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
namespace DiwaliGameplay {
public static class DiyaInstaller {
 public static VenueGame Install(Transform gameplayRoot){
 var venue=GameObjectUtility.FindExact("Diya - Approved Warm Palette");if(!venue)throw new InvalidOperationException("Approved Diya venue missing.");
 if(PrefabUtility.IsPartOfPrefabInstance(venue))PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(venue.gameObject),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
 var host=new GameObject("Diya gameplay");host.transform.SetParent(gameplayRoot,false);host.transform.SetPositionAndRotation(venue.position,venue.rotation);var g=host.AddComponent<DiyaGame>();g.originalFlameMaskShader=Shader.Find("Diwali/Hide Original Flame");
 Transform Part(string n){var t=GameObjectUtility.FindUnder(venue,n);if(!t)throw new InvalidOperationException("Diya part missing: "+n);return t;}
 Transform Wrap(Transform t,string name,Vector3? pivot=null){var w=new GameObject(name).transform;w.SetParent(t.parent,true);w.SetPositionAndRotation(pivot??GameObjectUtility.BoundsOf(t).center,venue.rotation);t.SetParent(w,true);return w;}
 Transform Marker(string name,Vector3 p){var t=new GameObject(name).transform;t.SetParent(host.transform,true);t.SetPositionAndRotation(p,venue.rotation);return t;}
 Transform Rest(string name,Transform item,Vector3? p=null){var t=Marker(name,p??item.position);Label(t,name,.18f);return t;}
 g.drawer=Wrap(Part("Cube"),"Preparation drawer motion");var scroll=Wrap(Part("BézierCurve.001"),"Ribbon scroll grab");scroll.SetParent(g.drawer,true);g.scroll=GameObjectUtility.MakeGrab(scroll,"diya-scroll");g.scrollRest=Rest("Scroll rest",scroll);g.scrollRest.SetParent(g.drawer,true);
 var pot=Wrap(Part("Cylinder"),"Flowerpot grab");Part("Plane").SetParent(pot,true);g.pot=GameObjectUtility.MakeGrab(pot,"diya-pot");var bench=GameObjectUtility.BoundsOf(Part("Cube.004"));var potTarget=pot.position-venue.forward*.65f;potTarget.y=bench.max.y+pot.position.y-GameObjectUtility.BoundsOf(pot).min.y;g.potRest=Rest("Pot bench rest",pot,potTarget);
 var key=Wrap(Part("Cube.002"),"Chest key grab");g.key=GameObjectUtility.MakeGrab(key,"diya-key");var chest=Part("Cube.001");var cb=GameObjectUtility.BoundsOf(chest);g.keySocket=Rest("Chest lock: insert key",key,cb.center+venue.right*(Extent(cb,venue.right)+.025f));
 var originalLid=Part("Cylinder.001");var lb=GameObjectUtility.BoundsOf(originalLid);g.lid=Wrap(originalLid,"Chest lid hinge",lb.center-venue.right*Extent(lb,venue.right));
 var feather=Wrap(Part("Cylinder.003"),"Peacock feather grab");g.feather=GameObjectUtility.MakeGrab(feather,"diya-feather");var featherTarget=bench.center+venue.forward*(Extent(bench,venue.forward)-Extent(GameObjectUtility.BoundsOf(feather),venue.forward)-.08f);featherTarget.y=bench.max.y+GameObjectUtility.BoundsOf(feather).extents.y+.02f;g.featherRest=Rest("Feather rest",feather,featherTarget);
 g.screen=Wrap(Part("Plane.001"),"Lattice screen motion");var diya=Wrap(Part("Sphere.002"),"Carried diya grab");g.diya=GameObjectUtility.MakeGrab(diya,"diya-lamp");var displayBench=GameObjectUtility.BoundsOf(Part("Cube.006"));var displayRest=GameObjectUtility.BoundsOf(Part("Sphere.001")).center+venue.right*.75f;displayRest.y=displayBench.max.y+GameObjectUtility.BoundsOf(diya).extents.y;g.diyaRest=Rest("Diya return rest",diya,displayRest);
 g.starter=Marker("Starter wick",GameObjectUtility.BoundsOf(Part("Sphere")).center+Vector3.up*.2f);g.destination=Marker("Display wick",GameObjectUtility.BoundsOf(Part("Sphere.001")).center+Vector3.up*.2f);
 g.carriedFlame=Flame(diya,diya.position+Vector3.up*.22f);g.starterFlame=Flame(g.starter,g.starter.position);g.destinationFlame=Flame(g.destination,g.destination.position);
 g.dust=new GameObject("Dust cleaning indicator");g.dust.transform.SetParent(diya,false);g.dust.transform.localPosition=Vector3.up*.24f;Label(g.dust.transform,"Brush here",.13f);
 g.guide=new GameObject("Scroll picture guide");g.guide.transform.SetParent(scroll,false);g.guide.transform.localPosition=Vector3.up*.2f;
 g.checkpoints=new Transform[3];for(int i=0;i<3;i++){g.checkpoints[i]=Marker("Route tile "+(i+1),venue.position);Label(g.checkpoints[i],(i+1).ToString(),.35f,false);}DiyaOrangeRoute.Place(g);g.gameObject.AddComponent<DiyaOrangeRoute>();
 g.panelPosition=venue.position+venue.rotation*new Vector3(0,1.8f,2);g.panelRotation=venue.rotation*Quaternion.Euler(0,180,0);g.Initialise();g.ResetGame();EditorUtility.SetDirty(g);return g;
 }
 static float Extent(Bounds b,Vector3 axis)=>Vector3.Dot(b.extents,new Vector3(Mathf.Abs(axis.x),Mathf.Abs(axis.y),Mathf.Abs(axis.z)));
 static TextMesh Label(Transform parent,string text,float height,bool billboard=true){var go=new GameObject(text+" label");go.transform.SetParent(parent,false);go.transform.localPosition=Vector3.up*.07f;var t=go.AddComponent<TextMesh>();t.text=text;t.fontSize=52;t.characterSize=.05f;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=new Color(1,.8f,.25f);var renderer=t.GetComponent<MeshRenderer>();float currentHeight=renderer.bounds.size.y;if(currentHeight>.0001f)t.characterSize*=height/currentHeight;if(billboard)go.AddComponent<DiyaBillboard>();return t;}
 static Light Flame(Transform parent,Vector3 pos){var go=new GameObject("Festival flame light");go.transform.SetParent(parent,true);go.transform.position=pos;var l=go.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.55f,.12f);l.range=1.7f;l.intensity=1.5f;l.enabled=false;var v=go.AddComponent<DiyaFlameVisual>();v.source=l;return l;}
}
}
#endif
