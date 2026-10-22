#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace DiwaliGameplay {
public static class AxisLayoutRevision {
 static Transform Part(string venue,string n)=>GameObjectUtility.FindUnder(GameObjectUtility.FindExact(venue),n);
 static void Move(Transform t,Vector3 p){var b=GameObjectUtility.BoundsOf(t);t.position+=p-b.center;}
 static Vector3 Surface(Transform t,float y){Vector3 sum=Vector3.zero;float total=0;foreach(var f in t.GetComponentsInChildren<MeshFilter>()){var v=f.sharedMesh.vertices;var tr=f.sharedMesh.triangles;for(int i=0;i<tr.Length;i+=3){var a=f.transform.TransformPoint(v[tr[i]]);var b=f.transform.TransformPoint(v[tr[i+1]]);var c=f.transform.TransformPoint(v[tr[i+2]]);if(Mathf.Abs(a.y-y)>.003f||Mathf.Abs(b.y-y)>.003f||Mathf.Abs(c.y-y)>.003f)continue;float area=Vector3.Cross(b-a,c-a).magnitude*.5f;sum+=(a+b+c)/3*area;total+=area;}}if(total<.01f)throw new System.Exception("Authored counter surface not found: "+y);return sum/total;}
 static void Rest(Transform target,GrabbableItem item,Vector3 surface){var b=GameObjectUtility.BoundsOf(item.transform);target.position=surface+Vector3.up*(item.transform.position.y-b.min.y+.015f);target.rotation=item.transform.rotation;}
 [MenuItem("Diwali/Gameplay/Apply measured axis and venue layouts")]
 public static void Apply(){if(Application.isPlaying)throw new System.Exception("Stop Play first");
 var s=Object.FindFirstObjectByType<SweetGame>();var counter=Part("SweetShop - Approved Warm Palette","Cube.012");
 if(!GameObjectUtility.FindExact("Counter reversed to customer side")){var stamp=new GameObject("Counter reversed to customer side");stamp.transform.SetParent(s.transform,true);var group=counter.parent;group.RotateAround(GameObjectUtility.BoundsOf(counter).center,Vector3.up,180);}
 var inset=Surface(counter,1.090f);var payment=Surface(counter,1.105f);Rest(s.rest,s.basket,inset);Rest(s.checkout,s.basket,payment);s.worktopY=1.09f;s.workspaceForward=Vector3.forward;s.cardReader.position=Surface(counter,1.071f)+Vector3.up*.035f;EditorUtility.SetDirty(s);
 var r=Object.FindFirstObjectByType<RoutingGame>();DiwaliGameplay.Editor.CoreVenueInstallers.ConfigureRoutingGeometry(r);EditorUtility.SetDirty(r);
 DiyaKeyDockUpgrade.Apply();
 var g=Object.FindFirstObjectByType<RangoliGame>();var pav=Part("Rangoli - Approved Warm Palette","Cube.002");var floor=Part("Rangoli - Approved Warm Palette","Plane");var pivot=GameObjectUtility.BoundsOf(floor).center;
 if(!GameObjectUtility.FindExact("Rangoli human scale display")){var stamp=new GameObject("Rangoli human scale display").transform;stamp.SetParent(g.transform,true);stamp.position=pivot;var roots=g.decorations.Where(t=>t&&t!=g.decorations[0]).Concat(new[]{g.sun,pav}).Distinct().ToArray();foreach(var t in roots){t.position=pivot+(t.position-pivot)*.48f;t.localScale*=.48f;}floor.localScale*=.68f;
 var relief=new GameObject("Shallow floor motifs").transform;relief.SetParent(g.transform,true);relief.position=pivot;for(int i=0;i<g.decorations.Length;i++)if(g.decorationGroups[i]!=1){var t=i==0?g.sun:g.decorations[i];t.SetParent(relief,true);}relief.localScale=new Vector3(1,.16f,1);
 }
 PavilionGrounding.Ground(g);var pb=GameObjectUtility.BoundsOf(pav);var board=Part("Rangoli - Approved Warm Palette","Cube.005");var table=Part("Rangoli - Approved Warm Palette","Cube.001");var rack=Part("Rangoli - Approved Warm Palette","Cube");var frame=GameObjectUtility.FindUnder(GameObjectUtility.FindExact("Rangoli - Approved Warm Palette"),"Compact Rangoli work station");var target=new Vector3(pb.max.x+1.25f,GameObjectUtility.BoundsOf(board).center.y,pb.center.z-1.45f);var delta=target-GameObjectUtility.BoundsOf(board).center;
 if(frame)frame.position+=delta;else{board.position+=delta;table.position+=delta;rack.position+=delta;}foreach(var p in g.pieces)if(!frame||!p.transform.IsChildOf(frame))p.transform.position+=delta;foreach(var z in g.sockets)z.transform.position+=delta;
 float tableDrop=GameObjectUtility.BoundsOf(table).min.y-.075f;table.position-=Vector3.up*tableDrop;board.position-=Vector3.up*tableDrop;foreach(var z in g.sockets)z.transform.position-=Vector3.up*tableDrop;
 var rackTarget=new Vector3(target.x,.8f+GameObjectUtility.BoundsOf(rack).extents.y,pb.center.z+1.3f);var rd=rackTarget-GameObjectUtility.BoundsOf(rack).center;rack.position+=rd;foreach(var p in g.pieces)p.transform.position+=rd;
 var plinth=GameObjectUtility.FindExact("Rangoli piece rack pedestal");if(!plinth){plinth=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;plinth.name="Rangoli piece rack pedestal";plinth.SetParent(g.transform,true);}plinth.position=new Vector3(rackTarget.x,.425f,rackTarget.z);plinth.localScale=new Vector3(.86f,.75f,3.02f);var stone=new Material(Shader.Find("Universal Render Pipeline/Lit"));stone.color=new Color(.3f,.19f,.15f);stone.SetFloat("_Smoothness",.2f);var mp="Assets/DiwaliGameplay/Rangoli/Rack pedestal stone.mat";if(AssetDatabase.LoadAssetAtPath<Material>(mp))Object.DestroyImmediate(stone);else AssetDatabase.CreateAsset(stone,mp);plinth.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(mp);
 var entrance=GameObjectUtility.FindExact("Rangoli accessible entrance");if(entrance)Object.DestroyImmediate(entrance.gameObject);var destination=GameObjectUtility.FindExact("Pavilion standing destination");if(destination)Object.DestroyImmediate(destination.gameObject);
 EditorUtility.SetDirty(g);
 var fw=Object.FindFirstObjectByType<FireworksGame>();fw.rocketWorldHome=fw.rocketPlatform.rotation;fw.rocketTiltAxis=Vector3.right;fw.rocketLocalLaunchAxis=fw.rocket.transform.InverseTransformDirection(Vector3.up).normalized;
 var boardArt=Part("Fireworks - Native PBR","board");var stand=Part("Fireworks - Native PBR","board stand");var boardBounds=GameObjectUtility.BoundsOf(boardArt);var boardDelta=new Vector3(0,1.25f,56.6f)-boardBounds.center;boardArt.position+=boardDelta;stand.position+=boardDelta;var plungerBase=Part("Fireworks - Native PBR","plunger base");var pdelta=new Vector3(-1.2f,.64f,56.5f)-GameObjectUtility.BoundsOf(plungerBase).center;plungerBase.position+=pdelta;fw.plunger.transform.position+=pdelta;
 var spinnerStand=Part("Fireworks - Native PBR","spinner stand");var spinDelta=new Vector3(-5.5f,1.72f,58f)-GameObjectUtility.BoundsOf(spinnerStand).center;spinnerStand.position+=spinDelta;fw.spinnerPivot.position+=spinDelta;fw.spinner.transform.position+=spinDelta;
 var fountainBase=Part("Fireworks - Native PBR","platform");var liftDelta=new Vector3(5.5f,1.32f,61f)-GameObjectUtility.BoundsOf(fountainBase).center;fountainBase.position+=liftDelta;fw.lift.position+=liftDelta;fw.fountain.transform.position+=liftDelta;fw.liftHome=fw.lift.localPosition;
 EditorUtility.SetDirty(fw);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(g.gameObject.scene);Debug.Log("Measured axis and compact venue layouts applied");}
}
}
#endif
