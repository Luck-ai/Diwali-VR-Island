#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DiwaliGameplay {
public static class DiyaKeyDockUpgrade {
 [MenuItem("Diwali/Gameplay/Align key shaft with chest lock")]
 public static void Apply(){if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play Mode first");var game=Object.FindFirstObjectByType<DiyaGame>();var art=game.key.GetComponentsInChildren<MeshFilter>().First(x=>x.sharedMesh);var bounds=art.sharedMesh.bounds;int longest=0;if(bounds.size.y>bounds.size.x)longest=1;if(bounds.size.z>bounds.size[longest])longest=2;var localAxis=Vector3.zero;localAxis[longest]=1;var points=art.sharedMesh.vertices;float min=points.Min(v=>v[longest]),max=points.Max(v=>v[longest]);float width=(max-min)*.22f;
 float Spread(bool high){var vertices=points.Where(p=>high?p[longest]>max-width:p[longest]<min+width).ToArray();if(vertices.Length==0)return 0;var center=vertices.Aggregate(Vector3.zero,(a,b)=>a+b)/vertices.Length;return vertices.Max(v=>Vector3.ProjectOnPlane(v-center,localAxis).sqrMagnitude);}
 if(Spread(true)>Spread(false))localAxis=-localAxis;
 var shaft=art.transform.TransformDirection(localAxis).normalized;var target=-game.transform.right.normalized;var pose=Quaternion.FromToRotation(shaft,target)*game.key.transform.rotation;
 var lockPoint=game.keyLockPoint!=Vector3.zero?game.keyLockPoint:game.keySocket.position;game.keyLockPoint=lockPoint;var tip=points.OrderByDescending(v=>Vector3.Dot(v,localAxis)).First();var tipInRoot=game.key.transform.InverseTransformPoint(art.transform.TransformPoint(tip));var rootScale=game.key.transform.lossyScale;var scaledTip=Vector3.Scale(tipInRoot,rootScale);
 game.keySocket.SetPositionAndRotation(lockPoint+target*.015f-pose*scaledTip,pose);game.keyTurnAxis=target;
 EditorUtility.SetDirty(game);EditorUtility.SetDirty(game.keySocket);EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
 Debug.Log("Key shaft aligned into chest. Turn axis="+target+"; blade tip is 15mm inside lock face. Docked root="+game.keySocket.position);
 }
}
}
#endif
