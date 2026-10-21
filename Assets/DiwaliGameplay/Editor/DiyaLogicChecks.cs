#if UNITY_EDITOR
using System;
using UnityEngine;
namespace DiwaliGameplay {
public static class DiyaLogicChecks {
 public static string Run(){var root=new GameObject("Temporary Diya logic verification");root.hideFlags=HideFlags.HideAndDontSave;try{var g=root.AddComponent<DiyaGame>();g.drawer=new GameObject("drawer").transform;g.drawer.SetParent(root.transform);g.screen=new GameObject("screen").transform;g.screen.SetParent(root.transform);g.lid=new GameObject("lid").transform;g.lid.SetParent(root.transform);g.Initialise();g.begun=true;
 void Check(bool pass,string name){if(!pass)throw new Exception("Diya test failed: "+name);}
 Check(!g.TryCheckpoint(0,true,true),"route cannot start during preparation");for(int i=0;i<8;i++)g.Credit("preparation"+i);g.lit=true;
 Check(!g.TryCheckpoint(1,true,true)&&g.nextCheckpoint==0,"wrong first checkpoint rejected");Check(!g.TryCheckpoint(0,false,true),"released diya rejected");Check(!g.TryCheckpoint(0,true,false),"unlit diya rejected");Check(g.TryCheckpoint(0,true,true),"first checkpoint");Check(!g.TryCheckpoint(0,true,true)&&g.nextCheckpoint==1,"duplicate checkpoint rejected");Check(!g.TryTransfer(true,0),"transfer blocked before complete route");Check(g.TryCheckpoint(1,true,true)&&g.TryCheckpoint(2,true,true),"remaining ordered checkpoints");Check(g.credited.Count==9,"three checkpoints count once");Check(!g.TryTransfer(false,0),"transfer needs held diya");Check(!g.TryTransfer(true,2),"transfer needs proximity");Check(g.TryTransfer(true,0),"valid transfer");g.ResetGame();Check(g.nextCheckpoint==0&&!g.lit&&!g.transferred&&!g.begun&&g.credited.Count==0,"reset clears route flame and credits");return "PASS: preparation gate, invalid order, duplicate, held/unlit gating, ordered route counts once, transfer gate, proximity, reset.";
 }finally{UnityEngine.Object.DestroyImmediate(root);}}
}
}
#endif
