#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace DiwaliGameplay {
public static class UsabilityRevisionReview {
 const string Dir="../Scene_Previews/Usability-20261021";
 [MenuItem("Diwali/Gameplay/Inspect repaired assets")]
 public static void Inspect(){Directory.CreateDirectory(Dir);var sweet=Object.FindFirstObjectByType<SweetGame>();var diya=Object.FindFirstObjectByType<DiyaGame>();var routing=Object.FindFirstObjectByType<RoutingGame>();var rangoli=Object.FindFirstObjectByType<RangoliGame>();var fireworks=Object.FindFirstObjectByType<FireworksGame>();
 var items=new[]{sweet.baskets[0].transform,sweet.baskets[1].transform,diya.feather.transform,diya.diya.transform};
 File.WriteAllText(Dir+"/asset-inspection.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{items=items.Select(t=>new{name=t.name,bounds=GameObjectUtility.BoundsOf(t).ToString(),meshes=t.GetComponentsInChildren<MeshFilter>().Select(m=>new{name=m.name,vertices=m.sharedMesh.vertexCount,asset=AssetDatabase.GetAssetPath(m.sharedMesh),bounds=m.sharedMesh.bounds.ToString(),materials=m.GetComponent<MeshRenderer>().sharedMaterials.Select(x=>new{name=x.name,cull=x.HasProperty("_Cull")?x.GetFloat("_Cull"):-1})})}),routingCrystal=routing.sockets[0].transform.position.ToString(),rocket=fireworks.rocket.transform.position.ToString(),launcher=fireworks.rocketSocket.position.ToString()},Newtonsoft.Json.Formatting.Indented));
 foreach(var item in items)Isolated(item,item.name.Replace('/','-'));
 }
 static void Isolated(Transform item,string name){var renderers=item.GetComponentsInChildren<MeshRenderer>(true);var layers=renderers.Select(r=>r.gameObject.layer).ToArray();for(int i=0;i<renderers.Length;i++)renderers[i].gameObject.layer=31;
 var b=GameObjectUtility.BoundsOf(item);var cameraGo=new GameObject("Temporary asset inspection camera");var camera=cameraGo.AddComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.12f,.16f);camera.orthographic=true;camera.orthographicSize=Mathf.Max(b.size.x,b.size.y,b.size.z)*.7f;camera.nearClipPlane=.01f;camera.farClipPlane=100;cameraGo.transform.position=b.center+new Vector3(1,1.1f,1).normalized*Mathf.Max(2,b.size.magnitude*2);cameraGo.transform.LookAt(b.center);var light=cameraGo.AddComponent<Light>();light.type=LightType.Point;light.intensity=3;light.range=20;var rt=new RenderTexture(1000,800,24);var old=RenderTexture.active;var texture=new Texture2D(1000,800,TextureFormat.RGB24,false);try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1000,800),0,0);texture.Apply();File.WriteAllBytes(Dir+"/"+name+".png",texture.EncodeToPNG());}finally{camera.targetTexture=null;RenderTexture.active=old;Object.DestroyImmediate(texture);Object.DestroyImmediate(rt);Object.DestroyImmediate(cameraGo);for(int i=0;i<renderers.Length;i++)renderers[i].gameObject.layer=layers[i];}}
}
}
#endif
