using UnityEngine;
using UnityEngine.UI;
namespace DiwaliGameplay {
public sealed class DiyaScrollGuide : MonoBehaviour {
 public DiyaGame owner;Text heading,body;
 void Awake(){var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;var rect=GetComponent<RectTransform>();rect.sizeDelta=new Vector2(1050,810);transform.localScale=Vector3.one*.00078f;var background=gameObject.AddComponent<Image>();background.color=new Color(.09f,.055f,.025f,.97f);background.raycastTarget=false;heading=Text("Heading",25,110,44);body=Text("Recipe",150,635,38);}
 Text Text(string name,float top,float height,int size){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(transform,false);var r=go.GetComponent<RectTransform>();r.anchorMin=new Vector2(0,1);r.anchorMax=new Vector2(1,1);r.offsetMin=new Vector2(35,-top-height);r.offsetMax=new Vector2(-35,-top);var t=go.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=size;t.color=new Color(1,.9f,.68f);t.supportRichText=true;t.alignment=TextAnchor.UpperLeft;t.raycastTarget=false;return t;}
 void LateUpdate(){if(!owner||!heading||!body)return;heading.text="THE FESTIVAL FLAME\nYour scroll guide";body.text="1  Move pot > find key > unlock chest\n2  Open chest > take feather\n3  Sweep feather over dusty diya\n4  Return feather > reveal starter flame\n5  Touch clean diya to the starter\n6  Carry through floor tiles 1 > 2 > 3\n7  Touch centre display > return diya\n\n<b>NOW: "+owner.CurrentObjective+"</b>";transform.position=owner.scrollRest.position-owner.transform.right*.9f+owner.transform.forward*.25f+Vector3.up*.85f;var cam=Camera.main;if(cam)transform.rotation=Quaternion.LookRotation(transform.position-cam.transform.position,cam.transform.up);}
}
}
