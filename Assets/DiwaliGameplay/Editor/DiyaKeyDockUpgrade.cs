#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DiwaliGameplay {
public static class DiyaKeyDockUpgrade {
 static void MakeKeyhole(DiyaGame game,Vector3 point,Vector3 outward){
  var old=GameObjectUtility.FindExact("Chest keyhole marker");if(!old)old=new GameObject("Chest keyhole marker").transform;old.transform.SetParent(game.transform,true);old.transform.SetPositionAndRotation(point+outward*.003f,Quaternion.LookRotation(outward,Vector3.up));old.transform.localScale=Vector3.one;
  var texture=new Texture2D(32,64,TextureFormat.RGBA32,false);for(int y=0;y<64;y++)for(int x=0;x<32;x++){bool on=(x-16)*(x-16)+(y-44)*(y-44)<11*11||x>=12&&x<=20&&y>=8&&y<=42;texture.SetPixel(x,y,on?Color.black:Color.clear);}texture.Apply();string root="Assets/DiwaliApprovedGames/ImportRepairs/";var png=root+"KeyholeMarker.png";System.IO.File.WriteAllBytes(png,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(png);var importer=(TextureImporter)AssetImporter.GetAtPath(png);importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=500;importer.alphaIsTransparency=true;importer.SaveAndReimport();var renderer=old.GetComponent<SpriteRenderer>();if(!renderer)renderer=old.gameObject.AddComponent<SpriteRenderer>();renderer.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(png);EditorUtility.SetDirty(old);
 }
 [MenuItem("Diwali/Gameplay/Align key shaft with chest lock")]
 public static void Apply(){if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode first");var game=Object.FindFirstObjectByType<DiyaGame>();var art=game.key.GetComponentsInChildren<MeshFilter>().First(x=>x.sharedMesh);var bounds=art.sharedMesh.bounds;int longest=0;if(bounds.size.y>bounds.size.x)longest=1;if(bounds.size.z>bounds.size[longest])longest=2;var localAxis=Vector3.zero;localAxis[longest]=1;var points=art.sharedMesh.vertices;float min=points.Min(v=>v[longest]),max=points.Max(v=>v[longest]);float width=(max-min)*.22f;
 float Spread(bool high){var vertices=points.Where(p=>high?p[longest]>max-width:p[longest]<min+width).ToArray();if(vertices.Length==0)return 0;var center=vertices.Aggregate(Vector3.zero,(a,b)=>a+b)/vertices.Length;return vertices.Max(v=>Vector3.ProjectOnPlane(v-center,localAxis).sqrMagnitude);}
 if(Spread(true)<Spread(false))localAxis=-localAxis;
 var shaft=art.transform.TransformDirection(localAxis).normalized;var target=-game.transform.right.normalized;int wide=Enumerable.Range(0,3).Where(i=>i!=longest).OrderByDescending(i=>bounds.size[i]).First();var localWide=Vector3.zero;localWide[wide]=1;float baseWide=points.Where(v=>Vector3.Dot(v,localAxis)<points.Min(p=>Vector3.Dot(p,localAxis))+(max-min)*.2f).Average(v=>v[wide]);if(points.Max(v=>v[wide])-baseWide<baseWide-points.Min(v=>v[wide]))localWide=-localWide;var worldWide=Vector3.ProjectOnPlane(art.transform.TransformDirection(localWide),shaft).normalized;var pose=Quaternion.LookRotation(target,Vector3.down)*Quaternion.Inverse(Quaternion.LookRotation(shaft,worldWide))*game.key.transform.rotation;
 var lockPoint=game.keyLockPoint!=Vector3.zero?game.keyLockPoint:game.keySocket.position;game.keyLockPoint=lockPoint;var end=points.Max(v=>Vector3.Dot(v,localAxis));var cap=points.Where(v=>Vector3.Dot(v,localAxis)>end-(max-min)*.01f).ToArray();var tip=cap.Aggregate(Vector3.zero,(a,b)=>a+b)/cap.Length;var tipInRoot=game.key.transform.InverseTransformPoint(art.transform.TransformPoint(tip));var rootScale=game.key.transform.lossyScale;var scaledTip=Vector3.Scale(tipInRoot,rootScale);
 game.keySocket.SetPositionAndRotation(lockPoint+target*.10f-pose*scaledTip,pose);game.keyTurnAxis=target;MakeKeyhole(game,lockPoint,-target);
 EditorUtility.SetDirty(game);EditorUtility.SetDirty(game.keySocket);EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
 Debug.Log("Key shaft aligned into chest. Turn axis="+target+"; blade tip is 100mm inside lock face. Docked root="+game.keySocket.position);
 }
}
}
#endif
