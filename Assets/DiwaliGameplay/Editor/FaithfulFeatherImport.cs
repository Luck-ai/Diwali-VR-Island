#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DiwaliGameplay {
public static class FaithfulFeatherImport {
 const string Dir="Assets/DiwaliApprovedGames/ImportRepairs/HairFaithful/";
 [MenuItem("Diwali/Gameplay/Preserve original feather material")]
 public static void Apply(){if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode first");
 var game=Object.FindFirstObjectByType<DiyaGame>();var approved=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DiwaliApprovedGames/Models/Diya_Approved.fbx");var original=approved.GetComponentsInChildren<MeshFilter>().First(x=>x.name=="Cylinder.003");
 ApplyPart(GameObjectUtility.FindUnder(game.feather.transform,"Original feather strands"),"FeatherHairFaithful","FeatherSource",original);
 ApplyPart(GameObjectUtility.FindUnder(game.feather.transform,"Cylinder.003"),"FeatherBodyFaithful","FeatherBodySource",original);
 AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
 }
 static void ApplyPart(Transform target,string modelName,string prefix,MeshFilter original){var model=AssetDatabase.LoadAssetAtPath<GameObject>(Dir+modelName+".fbx");var source=model.GetComponentsInChildren<MeshFilter>().First();var mesh=Object.Instantiate(source.sharedMesh);mesh.name=modelName+" - original standalone material UV";var points=mesh.vertices;for(int i=0;i<points.Length;i++)points[i]=original.transform.worldToLocalMatrix.MultiplyPoint3x4(source.transform.localToWorldMatrix.MultiplyPoint3x4(points[i]));mesh.vertices=points;mesh.RecalculateBounds();mesh.RecalculateNormals();string path=Dir+modelName+".asset";if(AssetDatabase.LoadAssetAtPath<Mesh>(path))AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(mesh,path);
 var materialPath=Dir+prefix+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,materialPath);}foreach(var pair in new[]{new[]{"BaseColor","_BaseMap"},new[]{"Normal","_BumpMap"},new[]{"MetallicSmoothness","_MetallicGlossMap"}}){string tex=Dir+"Textures/Baked/"+prefix+"_"+pair[0]+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(tex);importer.textureType=pair[0]=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=pair[0]=="BaseColor";importer.SaveAndReimport();material.SetTexture(pair[1],AssetDatabase.LoadAssetAtPath<Texture2D>(tex));}material.SetColor("_BaseColor",Color.white);material.SetFloat("_Cull",0);material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",1);material.EnableKeyword("_NORMALMAP");material.EnableKeyword("_METALLICSPECGLOSSMAP");target.GetComponent<MeshFilter>().sharedMesh=mesh;target.GetComponent<MeshRenderer>().sharedMaterials=Enumerable.Repeat(material,mesh.subMeshCount).ToArray();EditorUtility.SetDirty(target.GetComponent<MeshFilter>());EditorUtility.SetDirty(target.GetComponent<MeshRenderer>());EditorUtility.SetDirty(material);
 }
}
}
#endif
