#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
public static class VenueReadability {
 static string Dir="../Scene_Previews/Venue-Readability";
 static string[] Names={"Rangoli","SweetShop","LanternRouting","Diya"};
 static GameObject Venue(string n)=>GameObject.Find(n+" - Approved Warm Palette");
 static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
 [MenuItem("Diwali Island/Readability/Apply")]
 public static void Apply(){
 if(UnityEditor.EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");
 if(GameObject.Find("Venue Readability Applied 20261020"))throw new Exception("Already applied");
 Undo.IncrementCurrentGroup();Undo.SetCurrentGroupName("Diwali venue visibility and scale");
 var root=Venue("Rangoli");
 var board=root.transform.Find("Cube.005").GetComponent<MeshFilter>();
 var path=AssetDatabase.GetAssetPath(board.sharedMesh);var importer=(ModelImporter)AssetImporter.GetAtPath(path);bool wasReadable=importer.isReadable;
 importer.isReadable=true;importer.SaveAndReimport();
 var mesh=UnityEngine.Object.Instantiate(board.sharedMesh);mesh.name="Rangoli Board - Outward Faces";
 for(int sub=0;sub<mesh.subMeshCount;sub++){var tris=mesh.GetTriangles(sub);for(int j=0;j<tris.Length;j+=3){int a=tris[j];tris[j]=tris[j+2];tris[j+2]=a;}mesh.SetTriangles(tris,sub);}
 mesh.normals=mesh.normals.Select(n=>-n).ToArray();mesh.tangents=mesh.tangents.Select(t=>new Vector4(t.x,t.y,t.z,-t.w)).ToArray();
 AssetDatabase.CreateAsset(mesh,"Assets/DiwaliApprovedGames/RangoliBoardCorrected.asset");Undo.RecordObject(board,"Correct board face direction");board.sharedMesh=mesh;
 var collider=board.GetComponent<MeshCollider>();if(collider){Undo.RecordObject(collider,"Correct board collider");collider.sharedMesh=mesh;}
 importer.isReadable=wasReadable;importer.SaveAndReimport();board.sharedMesh=mesh;PrefabUtility.RecordPrefabInstancePropertyModifications(board);
 foreach(var n in Names){var v=Venue(n).transform;string floorName=n=="SweetShop"?"Plane.001":n=="Diya"?"Plane.002":"Plane";var floor=v.Find(floorName).GetComponent<Renderer>();var pivot=floor.bounds.center;pivot.y=floor.bounds.max.y;Undo.RecordObject(v,"Enlarge venue and contents 20 percent");v.position=pivot+(v.position-pivot)*1.2f;v.localScale*=1.2f;PrefabUtility.RecordPrefabInstancePropertyModifications(v);}
 foreach(var n in Names){var v=Venue(n).transform;var lightGO=new GameObject(n+" warm courtyard fill");Undo.RegisterCreatedObjectUndo(lightGO,"Add courtyard fill");lightGO.transform.SetParent(v,true);lightGO.transform.position=v.position+Vector3.up*(n=="Rangoli"?5.5f:3.3f);var light=lightGO.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.85f,.65f);light.intensity=n=="Rangoli"?5:2.5f;light.range=17;light.shadows=LightShadows.None;}
 var rack=root.transform.Find("Cube").GetComponent<Renderer>();var fill=new GameObject("Rangoli front detail fill");Undo.RegisterCreatedObjectUndo(fill,"Add detail fill");fill.transform.SetParent(root.transform,true);fill.transform.position=rack.bounds.center+new Vector3(3,3,0);var f=fill.AddComponent<Light>();f.type=LightType.Point;f.color=new Color(1,.91f,.78f);f.intensity=6;f.range=10;f.shadows=LightShadows.None;
 var med=root.transform.Find("Plane.001").GetComponent<Renderer>();foreach(var mat in med.sharedMaterials.Distinct()){Undo.RecordObject(mat,"Show both faces of thin medallion");mat.SetFloat("_Cull",0);EditorUtility.SetDirty(mat);}
 var marker=new GameObject("Venue Readability Applied 20261020");Undo.RegisterCreatedObjectUndo(marker,"Record completed pass");marker.transform.SetParent(GameObject.Find("Diwali Festival Ground").transform,false);
 PrefabUtility.RecordPrefabInstancePropertyModifications(board);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.scene);Inspect();
 }
 [MenuItem("Diwali Island/Readability/Finish Lighting")]
 public static void FinishLighting(){
 var r=Venue("Rangoli");
 foreach(var mat in r.transform.Find("Plane.001").GetComponent<Renderer>().sharedMaterials.Distinct()){Undo.RecordObject(mat,"Readable satin medallion");mat.DisableKeyword("_METALLICSPECGLOSSMAP");mat.SetTexture("_MetallicGlossMap",null);mat.SetFloat("_Metallic",.12f);mat.SetFloat("_Smoothness",.4f);EditorUtility.SetDirty(mat);}
 var rack=Venue("LanternRouting").transform.Find("rack");foreach(var mat in rack.GetComponent<Renderer>().sharedMaterials.Distinct()){Undo.RecordObject(mat,"Render thin rack panels");mat.SetFloat("_Cull",0);EditorUtility.SetDirty(mat);}
 AssetDatabase.SaveAssets();
 }
 [MenuItem("Diwali Island/Readability/Closeups")]
 public static void Closeups(){var r=Venue("LanternRouting").transform.Find("rack").GetComponent<Renderer>();Shot("Routing-rack-front",r.bounds.center+new Vector3(-3,1.7f,-2),r.bounds.center);Shot("Routing-rack-back",r.bounds.center+new Vector3(3,1.7f,-2),r.bounds.center);var m=Venue("Rangoli").transform.Find("Plane.001").GetComponent<Renderer>();Shot("Medallion",m.bounds.center+new Vector3(2,.5f,-.5f),m.bounds.center);}
 [MenuItem("Diwali Island/Readability/Inspect")]
 public static void Inspect(){Directory.CreateDirectory(Dir);File.WriteAllText(Dir+"/inspection.json",Newtonsoft.Json.JsonConvert.SerializeObject(Names.Select(n=>new {name=n,parts=Venue(n).GetComponentsInChildren<MeshRenderer>().Select(r=>new {name=r.name,center=V(r.bounds.center),size=V(r.bounds.size),local=V(r.transform.localPosition),materials=r.sharedMaterials.Select(m=>new{name=m.name,path=AssetDatabase.GetAssetPath(m),metal=m.HasProperty("_Metallic")?m.GetFloat("_Metallic"):0})})}),Newtonsoft.Json.Formatting.Indented));}
 public static void Shot(string name,Vector3 eye,Vector3 target){Directory.CreateDirectory(Dir);var g=new GameObject("Review camera"){hideFlags=HideFlags.HideAndDontSave};var c=g.AddComponent<Camera>();c.GetUniversalAdditionalCameraData().allowXRRendering=false;c.GetUniversalAdditionalCameraData().renderPostProcessing=true;c.fieldOfView=52;c.nearClipPlane=.03f;c.farClipPlane=400;c.allowHDR=true;g.transform.position=eye;g.transform.LookAt(target);var rt=new RenderTexture(1280,960,24);var old=RenderTexture.active;Texture2D tex=null;try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex=new Texture2D(1280,960,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,960),0,0);tex.Apply();File.WriteAllBytes(Dir+"/"+name+".png",tex.EncodeToPNG());}finally{RenderTexture.active=old;c.targetTexture=null;if(tex)UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(g);}}
 [MenuItem("Diwali Island/Readability/Render")]
 public static void Render(){var r=Venue("Rangoli").transform;foreach(var name in new[]{"Cube.005","Cube.001","Cube"}){var m=r.Find(name).GetComponent<Renderer>();Shot("Rangoli-"+name,m.bounds.center+new Vector3(2.8f,3.4f,-2.2f),m.bounds.center);}foreach(var n in Names){var v=Venue(n);var b=v.GetComponentsInChildren<Renderer>().First().bounds;foreach(var m in v.GetComponentsInChildren<Renderer>())b.Encapsulate(m.bounds);var side=n=="Rangoli"||n=="SweetShop"?1:-1;Shot(n,v.transform.position+new Vector3(side*16,9,-11),b.center);}}
}
#endif
