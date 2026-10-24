#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace DiwaliGameplay {
public static class HeadsetComfortLayout {
 [MenuItem("Diwali/Gameplay/Apply headset comfort shelf")]
 public static void Apply(){
  if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Apply the shelf in Edit mode.");
  var game=Object.FindFirstObjectByType<SweetGame>();
  if(!game||game.baskets==null||game.baskets.Length==0)return;
  var venue=GameObjectUtility.FindExact("SweetShop - Approved Warm Palette");
  var shelf=venue.Find("Reachable basket shelf");
  if(!shelf){var go=new GameObject("Reachable basket shelf");Undo.RegisterCreatedObjectUndo(go,"Add reachable basket shelf");shelf=go.transform;shelf.SetParent(venue,true);}
  var centre=game.baskets[0].transform.position;centre.x=-33.12f;centre.y=venue.position.y+.9f;centre.z=3.5f;
  shelf.position=centre;shelf.rotation=Quaternion.identity;var scale=venue.lossyScale;shelf.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
  if(shelf.childCount==0){
   var clay=ShelfMaterial("Basket shelf warm plum",new Color(.25f,.055f,.075f),0);
   var gold=ShelfMaterial("Basket shelf soft brass",new Color(.66f,.39f,.12f),.25f);
   Part(shelf,"Shelf",new Vector3(0,-.05f,0),new Vector3(.94f,.1f,1.95f),clay);
   Part(shelf,"Brass shelf edge",new Vector3(.47f,-.025f,0),new Vector3(.018f,.05f,1.95f),gold);
   Part(shelf,"Left bracket",new Vector3(-.12f,-.25f,-.64f),new Vector3(.6f,.4f,.055f),clay);
   Part(shelf,"Right bracket",new Vector3(-.12f,-.25f,.64f),new Vector3(.6f,.4f,.055f),clay);
  }
  for(int i=0;i<game.baskets.Length;i++){
   var basket=game.baskets[i];Undo.RecordObject(basket.transform,"Lower basket to supported shelf");
   var bounds=GameObjectUtility.BoundsOf(basket.transform);var p=centre+Vector3.forward*((i-(game.baskets.Length-1)*.5f)*.94f);
   p.y=centre.y+(basket.transform.position.y-bounds.min.y)+.015f;basket.transform.position=p;
   EditorUtility.SetDirty(basket.transform);
  }
  UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(game.gameObject.scene);Physics.SyncTransforms();
 }
 static Material ShelfMaterial(string name,Color color,float metallic){var path="Assets/DiwaliGameplay/Wayfinding/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.18f);mat.SetFloat("_Metallic",metallic);AssetDatabase.CreateAsset(mat,path);}return mat;}
 static void Part(Transform parent,string name,Vector3 position,Vector3 size,Material material){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);Undo.RegisterCreatedObjectUndo(go,"Build shelf support");go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;}
}
}
#endif
