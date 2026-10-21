#if UNITY_EDITOR
using UnityEngine;
namespace DiwaliGameplay {
public static class FireworksAccessibleLayout {
 public static void Apply(Transform venue){float floor=venue.position.y+.03f;var board=VenueInstallHelpers.Part(venue,"board");var stand=VenueInstallHelpers.Part(venue,"board stand");var bounds=GameObjectUtility.BoundsOf(stand);var group=VenueInstallHelpers.Point(venue,"Accessible cue board",new Vector3(bounds.center.x,bounds.min.y,bounds.center.z));board.SetParent(group,true);stand.SetParent(group,true);group.localScale*=Mathf.Min(1,1.55f/bounds.size.y);var p=group.position;p.y=floor;group.position=p;
 var lift=VenueInstallHelpers.Part(venue,"fountain lift ");var fountain=VenueInstallHelpers.Part(venue,"fountain firework");var platform=VenueInstallHelpers.Part(venue,"platform");var low=GameObjectUtility.BoundsOf(platform);var high=GameObjectUtility.BoundsOf(fountain);var fountainGroup=VenueInstallHelpers.Point(venue,"Accessible fountain station",new Vector3(low.center.x,low.min.y,low.center.z));platform.SetParent(fountainGroup,true);lift.SetParent(fountainGroup,true);fountain.SetParent(fountainGroup,true);fountainGroup.localScale*=Mathf.Min(1,1.4f/Mathf.Max(.1f,high.max.y-low.min.y));p=fountainGroup.position;p.y=floor;fountainGroup.position=p;}
}
}
#endif
