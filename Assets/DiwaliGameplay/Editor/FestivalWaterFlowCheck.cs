#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace DiwaliGameplay {
public static class FestivalWaterFlowCheck {
 [MenuItem("Diwali/Final Pass/Verify continuous water flow")]
 public static async void Run(){
  if(!Application.isPlaying)throw new InvalidOperationException("Enter Play Mode for the flow check.");
  var water=GameObjectUtility.FindExact("Festival ocean - both islands");var material=water.GetComponent<Renderer>().sharedMaterial;
  var folder=Path.GetFullPath("../Game_Plans/Finish_Today_2026-10-24");Directory.CreateDirectory(folder);
  var eye=new Vector3(58,water.position.y+2.5f,15);var target=new Vector3(100,water.position.y,60);
  float before=Time.time;
  File.Copy(ChristmasIslandQA.Render("Continuous-Water-A",eye,target,58),Path.Combine(folder,"Water-Flow-A.png"),true);
  await Task.Delay(1800);
  File.Copy(ChristmasIslandQA.Render("Continuous-Water-B",eye,target,58),Path.Combine(folder,"Water-Flow-B.png"),true);
  File.WriteAllText(Path.Combine(folder,"Water_Flow_Check.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{timeBefore=before,timeAfter=Time.time,waveSpeed=material.GetFloat("_WaveSpeed"),normalMap=AssetDatabase.GetAssetPath(material.GetTexture("_WaveNormal")),shaderMessages=ShaderUtil.GetShaderMessages(material.shader).Select(m=>m.message),islands= new[]{"Diwali Festival Ground","Christmas Island Integration","Diwali Christmas Walkway"}.Select(n=>new{name=n,y=GameObjectUtility.FindExact(n).position.y}),seaLevel=water.position.y},Newtonsoft.Json.Formatting.Indented));
 }
}
}
#endif
