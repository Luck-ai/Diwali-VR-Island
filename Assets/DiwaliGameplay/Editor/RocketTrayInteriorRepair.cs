#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DiwaliGameplay {
public static class RocketTrayInteriorRepair {
 const string MaterialPath="Assets/DiwaliGameplay/Fireworks/RocketTrayInterior.mat";
 const string SourceMaterialPath="Assets/DiwaliFireworks/Materials/Fireworks_10_00.mat";

 [MenuItem("Diwali Island/Gameplay/Repair rocket tray interior visibility")]
 public static void Apply(){
  if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode before repairing the tray.");
  var game=UnityEngine.Object.FindFirstObjectByType<FireworksGame>();
  if(!game||!game.rocketPlatform)throw new InvalidOperationException("Existing Fireworks rocket platform is required.");
  MeshRenderer tray=null;
  foreach(var filter in game.rocketPlatform.GetComponentsInChildren<MeshFilter>(true)){
   if(filter.sharedMesh&&filter.sharedMesh.name=="rocket stand rotating"){
    tray=filter.GetComponent<MeshRenderer>();
    break;
   }
  }
  if(!tray||tray.sharedMaterials.Length<2)throw new InvalidOperationException("The authored rocket stand rotating mesh and its two material slots are required.");
  var source=AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
  if(!source||!source.HasProperty("_Cull"))throw new InvalidOperationException("The original tray material must support culling.");
  var interior=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
  if(!interior){
   interior=new Material(source){name="RocketTrayInterior"};
   AssetDatabase.CreateAsset(interior,MaterialPath);
  }else{
   Undo.RecordObject(interior,"Repair rocket tray interior material");
   interior.shader=source.shader;
   interior.CopyPropertiesFromMaterial(source);
  }
  // The imported floor faces down; its reverse is visible through the authored top opening.
  interior.SetFloat("_Cull",(float)CullMode.Off);
  interior.doubleSidedGI=true;
  EditorUtility.SetDirty(interior);
  Undo.RecordObject(tray,"Show authored rocket tray interior");
  var materials=tray.sharedMaterials;
  materials[0]=interior;
  tray.sharedMaterials=materials;
  PrefabUtility.RecordPrefabInstancePropertyModifications(tray);
  EditorUtility.SetDirty(tray);
  EditorSceneManager.MarkSceneDirty(tray.gameObject.scene);
  AssetDatabase.SaveAssets();
  Debug.Log("Rocket tray floor now renders from both sides. Mesh, UVs, rim material, source FBX and source material are preserved. Save the scene to retain its material assignment.",tray);
 }
}
}
#endif
