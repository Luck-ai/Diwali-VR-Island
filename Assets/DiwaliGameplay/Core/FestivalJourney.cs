using System.Collections.Generic;
using UnityEngine;
namespace DiwaliGameplay {
public static class FestivalJourney {
 static readonly HashSet<string> prepared=new HashSet<string>();
 static readonly string[] types={"RangoliGame","SweetGame","DiyaGame","RoutingGame"};
 static readonly string[] names={"Rangoli courtyard","Sweet shop","Diya courtyard","Lantern garden"};
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset(){prepared.Clear();}
 public static void Record(VenueGame game){if(game){prepared.Add(game.GetType().Name);FestivalRewards.Award(game);}}
 public static bool ReadyForFinale {get {foreach(var type in types)if(!prepared.Contains(type))return false;return true;}}
 public static string Summary {get {int n=0;foreach(var type in types)if(prepared.Contains(type))n++;if(prepared.Contains("FireworksGame")&&n==4)return "FESTIVAL OPEN • all five venues ready";if(n==4)return "4 / 4 preparations ready • finish at Fireworks";return n+" / 4 courtyards ready • Fireworks is playable anytime";}}
 public static string Purpose(VenueGame game){switch(game.GetType().Name){case "RangoliGame":return "Welcome guests with colour and decoration.";case "SweetGame":return "Prepare a sweet gift to share.";case "DiyaGame":return "Bring the festival flame to the courtyard.";case "RoutingGame":return "Connect the lanterns to light the grounds.";default:return "Celebrate with fireworks; this venue can be played at any time.";}}
 public static string Next {get {for(int i=0;i<types.Length;i++)if(!prepared.Contains(types[i]))return "Suggested next: "+names[i]+". Any unfinished preparation is welcome.";return prepared.Contains("FireworksGame")?"All five venues are ready. Enjoy the festival!":"Next: Fireworks • Play Finale to open the festival.";}}
 public static Vector3 NextPosition {get {var games=Object.FindObjectsByType<VenueGame>(FindObjectsSortMode.None);foreach(var type in types)if(!prepared.Contains(type))foreach(var game in games)if(game.GetType().Name==type)return game.transform.position;foreach(var game in games)if(game.GetType().Name=="FireworksGame")return game.transform.position;return Vector3.zero;}}
}
}
