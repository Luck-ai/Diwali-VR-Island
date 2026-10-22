using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using DiwaliIsland;
namespace DiwaliGameplay {
public sealed class VenueTravelKiosk : MonoBehaviour {
 public DiwaliIslandNavigation navigation;
 public string venueTitle;
 GameObject destinations;RectTransform backdrop;
 static readonly string[] Names={"Rangoli Courtyard","Sweet Shop","Lantern Garden","Diya Courtyard","Fireworks","Festival Plaza","Christmas Island"};
 void Start(){
  var canvasObject=new GameObject("Travel panel",typeof(RectTransform),typeof(Canvas),typeof(TrackedDeviceGraphicRaycaster));canvasObject.transform.SetParent(transform,false);canvasObject.transform.localScale=Vector3.one*.0014f;var rect=canvasObject.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(600,150);var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=Camera.main;
  var bg=new GameObject("Travel cream backing",typeof(RectTransform),typeof(Image));bg.transform.SetParent(canvasObject.transform,false);backdrop=bg.GetComponent<RectTransform>();backdrop.sizeDelta=new Vector2(600,150);bg.GetComponent<Image>().color=new Color(.98f,.91f,.77f);
  AddText(canvasObject.transform,"FESTIVAL TRAVEL",new Vector2(0,37),new Vector2(570,46),29,new Color(.26f,.08f,.14f));
  AddButton(canvasObject.transform,"Teleport / choose destination",new Vector2(0,-28),()=>Toggle());
  destinations=new GameObject("Destinations",typeof(RectTransform));destinations.transform.SetParent(canvasObject.transform,false);
  for(int i=0;i<navigation.destinations.Length;i++){int index=i;AddButton(destinations.transform,i<Names.Length?Names[i]:navigation.destinations[i].name,new Vector2(0,-107-i*69),()=>navigation.VisitVenue(index));}
  destinations.SetActive(false);
  var post=GameObject.CreatePrimitive(PrimitiveType.Cube);post.name="Travel kiosk post";post.transform.SetParent(transform,false);post.transform.localPosition=new Vector3(0,-.7f,.08f);post.transform.localScale=new Vector3(.065f,1.4f,.065f);var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=new Color(.32f,.11f,.08f);post.GetComponent<Renderer>().sharedMaterial=mat;
 }
 public void Toggle(){bool open=!destinations.activeSelf;destinations.SetActive(open);backdrop.sizeDelta=new Vector2(600,open?150+navigation.destinations.Length*69:150);backdrop.anchoredPosition=new Vector2(0,open?-navigation.destinations.Length*69*.5f:0);}
 static void AddText(Transform parent,string value,Vector2 position,Vector2 size,int fontSize,Color color){var go=new GameObject(value,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var rt=go.GetComponent<RectTransform>();rt.sizeDelta=size;rt.anchoredPosition=position;var text=go.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text=value;text.fontSize=fontSize;text.alignment=TextAnchor.MiddleCenter;text.color=color;text.raycastTarget=false;}
 static void AddButton(Transform parent,string title,Vector2 position,UnityEngine.Events.UnityAction action){var go=new GameObject(title,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);var rt=go.GetComponent<RectTransform>();rt.sizeDelta=new Vector2(550,58);rt.anchoredPosition=position;var image=go.GetComponent<Image>();image.color=new Color(.48f,.14f,.08f);var button=go.GetComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);AddText(go.transform,title,Vector2.zero,new Vector2(530,56),28,new Color(1,.95f,.82f));}
}
}
