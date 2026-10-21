#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
namespace DiwaliGameplay.Editor {
public static class CoreRangoliPose {
[MenuItem("Diwali Island/Gameplay/Inspect Rangoli Pose")]
public static void Inspect(){if(!EditorApplication.isPlaying)throw new System.InvalidOperationException("Play Mode only");var g=Object.FindFirstObjectByType<RangoliGame>();g.BeginGame();for(int i=0;i<5;i++){g.pieces[i].transform.position=g.sockets[i].transform.position;g.sockets[i].TryAccept(g.pieces[i]);}var board=GameObjectUtility.FindUnder(GameObjectUtility.FindExact("Rangoli - Approved Warm Palette"),"Cube.005");System.IO.File.WriteAllText("../Game_Plans/RangoliPose.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{boardMesh=board.GetComponent<MeshFilter>().sharedMesh.name,boardMatrix=board.localToWorldMatrix.ToString(),items=g.pieces.Select((p,i)=>new{id=p.itemId,position=p.transform.position.ToString("F4"),socket=g.sockets[i].transform.position.ToString("F4"),center=GameObjectUtility.BoundsOf(p.transform).center.ToString("F4"),size=GameObjectUtility.BoundsOf(p.transform).size.ToString("F4"),childLocal=p.transform.GetChild(0).localPosition.ToString("F4")})},Newtonsoft.Json.Formatting.Indented));ChristmasIslandQA.Render("Gameplay-Rangoli-pose",board.position+Vector3.up*3+Vector3.right*1.5f,board.position,60);}
}
}
#endif
