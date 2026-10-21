using System.Collections.Generic;
using UnityEngine;
namespace DiwaliGameplay {
public abstract class VenueGame : MonoBehaviour {
 public bool begun, completed;
 [TextArea(4,16)] public string howToPlay="";
 [TextArea] public string status="Select Begin to start.";
 public HashSet<string> credited=new HashSet<string>();
 protected virtual void Awake() { }
 public virtual void BeginGame(){begun=true;completed=false;credited.Clear();}
 public virtual void ResetGame(){begun=false;completed=false;credited.Clear();status="Ready to begin.";}
 public bool Credit(string action){return begun && credited.Add(action);}
 protected void Complete(string message){completed=true;status=message;FestivalJourney.Record(this);}
 public virtual void Tick(){}
 void Update(){Tick();}
}
}
