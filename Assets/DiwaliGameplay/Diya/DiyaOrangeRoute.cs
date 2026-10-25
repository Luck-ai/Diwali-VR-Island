using UnityEngine;
namespace DiwaliGameplay {
/// <summary>Waypoints follow the orange strips in the approved source floor.</summary>
public sealed class DiyaOrangeRoute : MonoBehaviour {
 public DiyaGame owner;
 TextMesh[] arrows;
 public static void Place(DiyaGame game){
 var venue=GameObjectUtility.FindExact("Diya - Approved Warm Palette");if(!venue||game.checkpoints==null||game.checkpoints.Length!=3)return;
 Transform floor=null;foreach(var t in venue.GetComponentsInChildren<Transform>(true))if(t.name=="Plane.002"){floor=t;break;}
 float y=floor?GameObjectUtility.BoundsOf(floor).max.y+.03f:venue.position.y+.03f;
 var source=new[]{new Vector2(-27.544f,8),new Vector2(-27.544f,2.299f),new Vector2(-36.753f,2.299f)};
 for(int i=0;i<3;i++){var p=venue.TransformPoint(new Vector3(-(source[i].x+35.983276367f)*.240268708f,0,-(source[i].y+.689340591f)*.240268708f));p.y=y;game.checkpoints[i].position=p;game.checkpoints[i].rotation=Quaternion.identity;foreach(var text in game.checkpoints[i].GetComponentsInChildren<TextMesh>(true)){text.text=(i+1).ToString();text.transform.rotation=Quaternion.Euler(90,venue.eulerAngles.y,0);text.anchor=TextAnchor.MiddleCenter;}}
 }
 void Start(){if(!owner)owner=GetComponent<DiyaGame>();if(!owner)return;Place(owner);arrows=new TextMesh[3];for(int i=0;i<3;i++){var go=new GameObject("Orange path direction "+(i+1));go.transform.SetParent(transform,true);arrows[i]=go.AddComponent<TextMesh>();arrows[i].text="> >";arrows[i].fontSize=64;arrows[i].characterSize=.05f;arrows[i].anchor=TextAnchor.MiddleCenter;}}
 void LateUpdate(){if(!owner||arrows==null)return;for(int i=0;i<3;i++){var a=owner.checkpoints[i].position;var b=owner.destination.position;b.y=a.y;var d=b-a;arrows[i].transform.position=a+d.normalized*.45f+Vector3.up*.012f;arrows[i].transform.rotation=Quaternion.LookRotation(Vector3.down,d)*Quaternion.Euler(0,0,-90);arrows[i].text=owner.Step==8?"ANY TILE":"> >";arrows[i].color=owner.nextCheckpoint>0?new Color(.5f,1,.6f):Color.white;arrows[i].gameObject.SetActive(owner.Step==8||owner.Step==9);}}
}
}
