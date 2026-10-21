#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DiwaliGameplay.Editor {
public static class CoreGameplayInstaller {
 [MenuItem("Diwali Island/Gameplay/Rebuild From Baseline")]
 public static void RebuildFromBaseline(){if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");const string scene="Assets/Scenes/DiwaliFestivalGround.unity";const string baseline="../Scene_Backups/Gameplay-20261020/DiwaliFestivalGround.unity";if(!System.IO.File.Exists(baseline))throw new System.IO.FileNotFoundException("Verified pregameplay baseline missing",baseline);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);System.IO.File.Copy(baseline,scene,true);AssetDatabase.ImportAsset(scene,ImportAssetOptions.ForceUpdate);EditorSceneManager.OpenScene(scene);Install();}
 public static void UnpackGameplayVenues(){foreach(var name in new[]{"Rangoli - Approved Warm Palette","LanternRouting - Approved Warm Palette","Diya - Approved Warm Palette","SweetShop - Approved Warm Palette","Fireworks - Native PBR"}){var venue=GameObjectUtility.FindExact(name);if(!venue)throw new InvalidOperationException("Missing approved venue: "+name);var prefab=PrefabUtility.GetOutermostPrefabInstanceRoot(venue.gameObject);if(prefab)PrefabUtility.UnpackPrefabInstance(prefab,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);}}
 [MenuItem("Diwali Island/Gameplay/Reload Saved Scene")]
 public static void Reload(){EditorSceneManager.OpenScene("Assets/Scenes/DiwaliFestivalGround.unity");}
 [MenuItem("Diwali Island/Gameplay/Install Approved Games")]
 public static void Install(){
 if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before installing");
 if(GameObjectUtility.FindExact("Diwali approved gameplay"))throw new InvalidOperationException("Gameplay already installed");
 System.IO.Directory.CreateDirectory("../Game_Plans/Backups");System.IO.File.Copy("Assets/Scenes/DiwaliFestivalGround.unity","../Game_Plans/Backups/DiwaliFestivalGround-before-gameplay.unity",true);
 UnpackGameplayVenues();
 var root=new GameObject("Diwali approved gameplay").transform;
 CoreVenueInstallers.InstallRangoli(root);CoreVenueInstallers.InstallRouting(root);DiyaInstaller.Install(root);SweetInstaller.Install(root);DiwaliGameplay.FireworksInstaller.Install(root);
 FestivalWayfinding.Install();
 foreach(var interactor in UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor>(FindObjectsInactive.Include,FindObjectsSortMode.None))interactor.farAttachMode=UnityEngine.XR.Interaction.Toolkit.Attachment.InteractorFarAttachMode.Near;
 foreach(var attach in UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Attachment.InteractionAttachController>(FindObjectsInactive.Include,FindObjectsSortMode.None)){attach.useManipulationInput=false;attach.useMomentum=false;attach.useDistanceBasedVelocityScaling=false;}
 foreach(var visual in UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.CurveVisualController>(FindObjectsInactive.Include,FindObjectsSortMode.None)){visual.restingVisualLineLength=.25f;visual.maxVisualCurveDistance=8;visual.extendLineToEmptyHit=false;visual.snapToSelectedAttachIfAvailable=true;}
 EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveScene(root.gameObject.scene);Debug.Log("Installed five approved Diwali games; scene saved.");
 }
}
}
#endif
