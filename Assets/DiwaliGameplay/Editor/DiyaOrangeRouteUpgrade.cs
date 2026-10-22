#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DiwaliGameplay {
public static class DiyaOrangeRouteUpgrade {
 [MenuItem("Diwali/Gameplay/Align Diya route with orange floor")]
 public static void Apply(){if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode first");var game=Object.FindFirstObjectByType<DiyaGame>();if(!game)throw new System.InvalidOperationException("No Diya game in the current scene");Undo.RecordObjects(game.checkpoints,"Align orange route");DiyaOrangeRoute.Place(game);if(!game.GetComponent<DiyaOrangeRoute>())Undo.AddComponent<DiyaOrangeRoute>(game.gameObject);foreach(var marker in game.checkpoints)EditorUtility.SetDirty(marker);EditorSceneManager.MarkSceneDirty(game.gameObject.scene);Debug.Log("Diya tiles follow the short orange route: right strip, header, centre strip.");}
}
}
#endif
