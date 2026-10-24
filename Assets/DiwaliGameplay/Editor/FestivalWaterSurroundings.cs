#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DiwaliGameplay {
public static class FestivalWaterSurroundings {
 const string Root = "Assets/DiwaliFestival";
 static string Output => Path.GetFullPath("../Game_Plans/Finish_Today_2026-10-24");
 [MenuItem("Diwali/Final Pass/Water surroundings with sand backup")]
 public static void Apply() {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(scene.path!="Assets/Scenes/DiwaliFestivalGround.unity")throw new InvalidOperationException("Open the festival scene first.");
  var shader=Shader.Find("DiwaliFestival/Festival Water");
  if(!shader||!shader.isSupported||ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))throw new InvalidOperationException("Water shader is not ready.");
  var sand=GameObjectUtility.FindExact("Rolling earth landscape");
  var foundation=GameObjectUtility.FindExact("Courtyard stone foundation");
  var christmas=GameObjectUtility.FindExact("Island_Terrain");
  if(!sand||!foundation||!christmas)throw new InvalidOperationException("Expected island foundations and sand backup missing.");
  var bounds=GameObjectUtility.BoundsOf(foundation);var christmasBounds=GameObjectUtility.BoundsOf(christmas);
  float level=Mathf.Min(bounds.min.y,christmasBounds.min.y)-.35f;
  var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Festival surrounding water.mat");
  if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,Root+"/Materials/Festival surrounding water.mat");}
  Undo.RecordObject(material,"Surround festival islands with water");material.shader=shader;
  var normal=material.HasProperty("_WaveNormal")?material.GetTexture("_WaveNormal"):null;
  if(!normal||string.IsNullOrEmpty(AssetDatabase.GetAssetPath(normal))){
   material.SetColor("_DeepColor",new Color(.025f,.20f,.30f));material.SetColor("_ShallowColor",new Color(.12f,.42f,.48f));
   material.SetFloat("_WaveScale",1.1f);material.SetFloat("_WaveSpeed",.24f);material.SetFloat("_Smoothness",.82f);material.SetFloat("_Opacity",.84f);
  }
  EditorUtility.SetDirty(material);
  const string meshPath=Root+"/Surrounding ocean mesh.asset";
  var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
  if(!mesh){mesh=new Mesh{name="Surrounding ocean - two triangles"};mesh.vertices=new[]{new Vector3(-1500,0,-1500),new Vector3(-1500,0,1500),new Vector3(1500,0,1500),new Vector3(1500,0,-1500)};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,meshPath);}
  var water=GameObjectUtility.FindExact("Festival ocean - both islands");
  if(!water){var go=new GameObject("Festival ocean - both islands",typeof(MeshFilter),typeof(MeshRenderer));Undo.RegisterCreatedObjectUndo(go,"Create festival ocean");water=go.transform;}
  Undo.RecordObject(water,"Set sea level");water.SetPositionAndRotation(new Vector3(0,level,-50),Quaternion.identity);water.localScale=Vector3.one;
  water.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=water.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
  Undo.RecordObject(sand.gameObject,"Keep sand landscape as disabled backup");sand.gameObject.SetActive(false);
  var earth=GameObjectUtility.FindExact("Earth beyond the walls");
  if(earth){Undo.RecordObject(earth.gameObject,"Keep surrounding earth as disabled backup");earth.gameObject.SetActive(false);}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  Directory.CreateDirectory(Output);
  File.WriteAllText(Path.Combine(Output,"Water_Surroundings.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{waterLevel=level,waterSizeMetres=3000,triangles=2,sandBackup=sand.name,sandBackupActive=sand.gameObject.activeSelf,sandMesh=AssetDatabase.GetAssetPath(sand.GetComponent<MeshFilter>().sharedMesh),diwaliFoundationBottom=bounds.min.y,diwaliFoundationTop=bounds.max.y,christmasFoundationBottom=christmasBounds.min.y,christmasFoundationTop=christmasBounds.max.y,waterColliderCount=water.GetComponents<Collider>().Length,shaderMessages=ShaderUtil.GetShaderMessages(shader).Select(m=>m.message),sceneSaved=!scene.isDirty},Newtonsoft.Json.Formatting.Indented));
  Capture("Water-Islands-Overview",new Vector3(105,90,50),new Vector3(0,0,-40),75);
  Capture("Water-Diwali-Shore",new Vector3(58,4,15),new Vector3(30,-.3f,-8),70);
 }
 static void Capture(string name,Vector3 eye,Vector3 target,float fov){File.Copy(ChristmasIslandQA.Render(name,eye,target,fov),Path.Combine(Output,name+".png"),true);}
}
}
#endif
