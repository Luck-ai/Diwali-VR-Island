#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace DiwaliGameplay {
public static class FinalLayoutReview {
 [MenuItem("Diwali/Gameplay/Capture final layout details")]
 public static void Capture(){if(!EditorApplication.isPlaying)throw new System.InvalidOperationException("Enter Play Mode for layout review");var diya=Object.FindFirstObjectByType<DiyaGame>();diya.BeginGame();diya.key.SnapTo(diya.keySocket,true);var lockPoint=diya.keyLockPoint;ChristmasIslandQA.Render("Final-Key-docked",lockPoint+diya.transform.right*.65f+diya.transform.forward*.55f+Vector3.up*.2f,lockPoint,48);
 var r=Object.FindFirstObjectByType<RangoliGame>();r.ResetGame();var center=GameObjectUtility.BoundsOf(r.decorations[0]).center;var venue=GameObjectUtility.FindExact("Rangoli - Approved Warm Palette");ChristmasIslandQA.Render("Final-Pavilion-floor-contact",center+venue.forward*2.1f+Vector3.up*1.0f,center,68);
 var f=Object.FindFirstObjectByType<FireworksGame>();f.BeginGame();f.stage=2;f.Load(f.spinner);f.AdjustSpeed(.5f);var panel=f.GetComponentInChildren<VenuePanel>();var eye=panel.transform.position-panel.transform.forward*2.1f;eye.y=1.6f;ChristmasIslandQA.Render("Final-Spinner-controls",eye,panel.transform.position,60);
 File.WriteAllText("../Game_Plans/Final_Layout_Review_2026-10-21.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{scope="Staged presentation views only. Interaction evidence is the separate 487-check run.",pavilion=r.decorations.Select(t=>new{name=t.name,bottom=GameObjectUtility.BoundsOf(t).min.y}),keyAxis=diya.keyTurnAxis.ToString(),keyLock=diya.keyLockPoint.ToString(),spinnerPanel=panel.transform.position.ToString()},Newtonsoft.Json.Formatting.Indented));
 }
}
}
#endif
