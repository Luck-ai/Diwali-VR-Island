#if UNITY_EDITOR
using System;
using UnityEngine;
namespace DiwaliGameplay {
public static class DiyaLogicChecks {
 public static string Run(){var root=new GameObject("Temporary Diya logic verification");root.hideFlags=HideFlags.HideAndDontSave;try{var g=root.AddComponent<DiyaGame>();g.drawer=new GameObject("drawer").transform;g.drawer.SetParent(root.transform);g.screen=new GameObject("screen").transform;g.screen.SetParent(root.transform);g.lid=new GameObject("lid").transform;g.lid.SetParent(root.transform);g.Initialise();g.begun=true;
 void Check(bool pass,string name){if(!pass)throw new Exception("Diya test failed: "+name);}
 g.checkpoints=new Transform[3];
 Check(!g.TryCheckpoint(0,true,true),"route cannot start during preparation");
 for(int index=0;index<3;index++){
  g.begun=true;for(int i=0;i<8;i++)g.Credit("preparation"+i);g.lit=true;
  Check(!g.TryTransfer(true,0),"transfer blocked before any route tile");
  Check(!g.TryCheckpoint(index,false,true),"released diya rejected");
  Check(!g.TryCheckpoint(index,true,false),"unlit signal rejected");
  g.lit=false;Check(!g.TryCheckpoint(index,true,true),"unlit diya rejected");g.lit=true;
  Check(g.TryCheckpoint(index,true,true)&&g.Step==9&&g.nextCheckpoint==3,"one tile completes route: "+index);
  Check(!g.TryCheckpoint(index,true,true)&&g.credited.Count==9,"duplicate tile cannot add credit");
  Check(!g.TryTransfer(false,0),"transfer needs held diya");
  Check(!g.TryTransfer(true,2),"transfer needs proximity");
  g.lit=false;Check(!g.TryTransfer(true,0),"transfer needs lit diya");g.lit=true;
  Check(g.TryTransfer(true,0),"valid transfer");g.ResetGame();
  Check(g.nextCheckpoint==0&&!g.lit&&!g.transferred&&!g.begun&&g.credited.Count==0,"reset clears route flame and credits");
 }
 return "PASS: preparation gate, each tile alone completes route once, held/unlit gating, transfer gate, proximity, reset.";
 }finally{UnityEngine.Object.DestroyImmediate(root);}}
}
}
#endif
