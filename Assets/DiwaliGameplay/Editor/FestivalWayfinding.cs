#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;
namespace DiwaliGameplay.Editor {
public static class FestivalWayfinding {
const string Folder="Assets/DiwaliGameplay/Wayfinding";
static Material stone,gold,plum;
static Transform root;
static readonly List<string> obstacles=new List<string>();
public static void Install(){
 obstacles.Clear();
 var old=GameObjectUtility.FindExact("Festival walking routes");if(old)Object.DestroyImmediate(old.gameObject);
 Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
 stone=Material("Warm route sandstone",new Color(.68f,.42f,.22f));gold=Material("Route gold inlay",new Color(.96f,.7f,.29f));plum=Material("Wayfinding plum",new Color(.21f,.065f,.12f));
 root=new GameObject("Festival walking routes").transform;
 Route("Arrival",new[]{P(0,-20),P(0,3.83f)});
 Route("West promenade",new[]{P(0,3.83f),P(-10,10),P(-11,20.75f),P(-10,30),P(0,35.97f)});
 Route("East promenade",new[]{P(0,3.83f),P(10,10),P(11,20.75f),P(10,30),P(0,35.97f)});
 Route("02 Sweet Shop",new[]{P(0,3.83f),P(-24.7f,3.83f)});
 Route("04 Diya Courtyard",new[]{P(0,3.83f),P(25,3.83f)});
 Route("01 Rangoli Courtyard",new[]{P(0,35.97f),P(-23.3f,35.97f)});
 Route("03 Lantern Garden",new[]{P(0,35.97f),P(24.7f,35.97f)});
 Route("05 Fireworks",new[]{P(0,35.97f),P(0,53)});
 Sign("<b>Festival walk</b>\n←  Sweet Shop     Diya Courtyard  →\n←  Rangoli          Lantern Garden  →\nFireworks  ↑",P(2.5f,.8f),Vector3.back,3.4f);
 Sign("←  Rangoli     Lantern Garden  →\n<b>Fireworks  ↑</b>",P(2.5f,34),Vector3.back,3.2f);
 Sign("02  Sweet Shop",P(-22.5f,5.4f),Vector3.right,2.1f);
 Sign("04  Diya Courtyard",P(22.5f,5.4f),Vector3.left,2.1f);
 Sign("01  Rangoli Courtyard",P(-20.5f,37.55f),Vector3.right,2.3f);
 Sign("03  Lantern Garden",P(20.5f,37.55f),Vector3.left,2.3f);
 Sign("05  Fireworks",P(2,49),Vector3.back,2);
 var diwali=GameObjectUtility.FindExact("Diwali Festival Ground");if(diwali){foreach(var text in diwali.GetComponentsInChildren<TMPro.TMP_Text>(true)){text.text=Rename(text.text);if(text.transform.parent&&text.transform.parent.name.StartsWith("Venue wayfinding")){var labels=new[]{"Rangoli Courtyard","Sweet Shop","Lantern Garden","Diya Courtyard","Fireworks"};for(int i=0;i<labels.Length;i++)if(text.text.TrimStart().StartsWith((i+1).ToString("00"))){text.text="<color=#E4B55D>"+(i+1).ToString("00")+"</color>  <b>"+labels[i]+"</b>";break;}Style(text,4.5f);}}foreach(var text in diwali.GetComponentsInChildren<UnityEngine.UI.Text>(true))text.text=Rename(text.text);foreach(var text in diwali.GetComponentsInChildren<TextMesh>(true))text.text=Rename(text.text);}
 File.WriteAllLines("../Game_Plans/Route_Clearance_2026-10-20.txt",obstacles.Count==0?new List<string>{"No standing-height collider obstructions detected on route centre lines."}:obstacles);
}
static string Rename(string s)=>s.Replace("SWEET MEMORY SHOP","SWEET SHOP").Replace("Sweet Memory Shop","Sweet Shop").Replace("FESTIVAL GAMES","FIREWORKS").Replace("Festival Games","Fireworks");
static Vector3 P(float x,float z)=>new Vector3(x,.105f,z);
static Material Material(string name,Color color){string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Smoothness",.18f);if(name.Contains("gold")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.2f);}EditorUtility.SetDirty(m);return m;}
static void Route(string name,Vector3[] points){var vertices=new List<Vector3>();var tris=new List<int>();var edges=new List<Vector3>();var et=new List<int>();for(int k=0;k<points.Length-1;k++){var a=points[k];var b=points[k+1];var side=Vector3.Cross(Vector3.up,(b-a).normalized);Quad(vertices,tris,a-side*.95f,a+side*.95f,b+side*.95f,b-side*.95f);foreach(float sign in new[]{-1f,1f})Quad(edges,et,a+side*(sign*.95f)+Vector3.up*.006f,a+side*(sign*1.01f)+Vector3.up*.006f,b+side*(sign*1.01f)+Vector3.up*.006f,b+side*(sign*.95f)+Vector3.up*.006f);float length=Vector3.Distance(a,b);Physics.SyncTransforms();for(float sample=0;sample<=length;sample+=.75f){var p=Vector3.Lerp(a,b,sample/length);foreach(var hit in Physics.OverlapCapsule(p+Vector3.up*.5f,p+Vector3.up*1.65f,.24f,~0,QueryTriggerInteraction.Ignore)){if(hit.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>())continue;string problem=name+" at "+p+": "+hit.name;if(!obstacles.Contains(problem))obstacles.Add(problem);}}for(float d=3;d<length;d+=4){var c=Vector3.Lerp(a,b,d/length)+Vector3.up*.012f;var forward=(b-a).normalized;int v=edges.Count;edges.Add(c+forward*.32f);edges.Add(c-forward*.2f+side*.22f);edges.Add(c-forward*.2f-side*.22f);et.AddRange(new[]{v,v+1,v+2,v+2,v+1,v});}}Mesh(name+" walking surface",vertices,tris,stone);Mesh(name+" inlaid direction arrows",edges,et,gold);}
static void Quad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d){int n=v.Count;v.AddRange(new[]{a,b,c,d});t.AddRange(new[]{n,n+2,n+1,n,n+3,n+2,n,n+1,n+2,n,n+2,n+3});}
static void Mesh(string name,List<Vector3> vertices,List<int> tris,Material material){var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();string path=Folder+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;}
static void Sign(string label,Vector3 position,Vector3 toward,float width){var sign=new GameObject(label.Split('\n')[0]+" route sign").transform;sign.SetParent(root,false);sign.position=position;sign.rotation=Quaternion.LookRotation(-toward,Vector3.up);var post=GameObject.CreatePrimitive(PrimitiveType.Cube);post.name="Short brass sign post";post.transform.SetParent(sign,false);post.transform.localPosition=new Vector3(0,.65f,.06f);post.transform.localScale=new Vector3(.055f,1.3f,.055f);post.GetComponent<Renderer>().sharedMaterial=gold;Object.DestroyImmediate(post.GetComponent<Collider>());var plate=GameObject.CreatePrimitive(PrimitiveType.Cube);plate.name="Readable festival destination";plate.transform.SetParent(sign,false);plate.transform.localPosition=Vector3.up*1.45f;float height=label.Contains("Festival walk")?1.05f:label.Contains("\n")?.7f:.42f;plate.transform.localScale=new Vector3(width,height,.05f);plate.GetComponent<Renderer>().sharedMaterial=plum;Object.DestroyImmediate(plate.GetComponent<Collider>());var text=new GameObject("Destination text",typeof(RectTransform)).AddComponent<TextMeshPro>();text.transform.SetParent(sign,false);text.transform.localPosition=new Vector3(0,1.45f,-.031f);text.rectTransform.sizeDelta=new Vector2(width-.2f,height-.1f);text.text=label;Style(text,7);}
static void Style(TMP_Text text,float max){var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular SDF.asset");if(font){text.font=font;text.fontSharedMaterial=font.material;}text.color=new Color(1,.94f,.8f);text.alignment=TextAlignmentOptions.Center;text.enableAutoSizing=true;text.fontSizeMin=1.5f;text.fontSizeMax=max;text.fontSize=max;text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Truncate;text.characterSpacing=0;text.wordSpacing=0;text.raycastTarget=false;text.ForceMeshUpdate();}
}
}
#endif
