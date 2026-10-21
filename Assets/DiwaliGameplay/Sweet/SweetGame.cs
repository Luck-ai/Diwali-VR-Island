using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace DiwaliGameplay {
public class SweetGame : VenueGame {
 public GrabbableItem basket,card;
 public GrabbableItem[] baskets;
 GrabbableItem defaultBasket;
 readonly HashSet<GrabbableItem> heldBaskets=new HashSet<GrabbableItem>();
 readonly Dictionary<GrabbableItem,Vector3> basketScales=new Dictionary<GrabbableItem,Vector3>();
 public GrabbableItem[] sweets;
 public Transform rest,checkout,collection,cardReader;
 public Transform[] slots,glass;
 public Vector3[] closedGlass;
 Vector3[] openGlass, alternateGlass;
 bool otherSide;
 public bool cabinetOpen,basketReady,docked,authorized,confirmed;
 public readonly List<GrabbableItem> selected=new List<GrabbableItem>();
 public float snapRadius=.65f;
 readonly HashSet<GrabbableItem> heldLast=new HashSet<GrabbableItem>();
 Vector3 basketScale=Vector3.one;
 bool basketWasHeld;
 public Vector3 panelPosition;public Quaternion panelRotation;
 public float worktopY;public Vector3 workspaceForward=Vector3.forward;
 VenuePanel panel;SweetPlacementGhost restGhost,dockGhost;bool placing;
 UnityEngine.UI.Button beginControl,openControl,switchControl,confirmControl;
 public Quaternion gujiyaRestRotation=Quaternion.identity;
 readonly Dictionary<GrabbableItem,Vector3> sweetScales=new Dictionary<GrabbableItem,Vector3>();
 protected override void Awake(){base.Awake();defaultBasket=basket;if(baskets==null||baskets.Length==0)baskets=new[]{basket};foreach(var b in baskets){basketScales[b]=b.transform.localScale;b.ConfigureGrab();b.grab.trackRotation=false;}snapRadius=Mathf.Max(snapRadius,.65f);basketScale=basket.transform.localScale;basket.grab.trackScale=false;basket.grab.useDynamicAttach=true;basket.grab.matchAttachPosition=true;basket.grab.matchAttachRotation=true;if(sweets!=null)foreach(var sweet in sweets)if(sweet){sweetScales[sweet]=sweet.transform.localScale;sweet.grab.trackScale=false;sweet.grab.useDynamicAttach=true;sweet.grab.matchAttachPosition=true;sweet.grab.matchAttachRotation=true;}}

 void Start(){
  openGlass=(Vector3[])closedGlass.Clone();alternateGlass=(Vector3[])closedGlass.Clone();
  var first=GameObjectUtility.BoundsOf(glass[0]);var second=GameObjectUtility.BoundsOf(glass[1]);
  // Keep both panels within the cabinet, on separate depth tracks.
  var across=second.center-first.center;across.y=0;
  var depth=workspaceForward.normalized*.035f;
  openGlass[0]+=glass[0].parent.InverseTransformVector(across+depth);
  alternateGlass[1]+=glass[1].parent.InverseTransformVector(-across-depth);
  howToPlay="Grip to grab; release to place.\n1. Make a Gift Basket, then Open Cabinet. Use Switch open side to reach the other half.\n2. Choose either basket on the wall shelves and grip to bring it in front of your hand. Release it over BASKET REST.\n3. Release any 4 sweets into the bowl; repeats welcome. Grip a packed sweet to swap it.\n4. Release the full basket at CHECKOUT.\n5. Touch the reader with the festival card, then Confirm Gift.\n6. Confirm Gift finishes the activity at checkout.\nPoint at buttons and press trigger. Restart restores all props. No payment or budget.";var venue=GameObjectUtility.FindExact("SweetShop - Approved Warm Palette")??GameObjectUtility.FindExact("SweetShop - Native PBR");float floor=venue?venue.position.y:0;float table=rest.position.y-GameObjectUtility.BoundsOf(basket.transform).extents.y;if(venue){var counter=GameObjectUtility.FindUnder(venue,"Cube.012");if(worktopY>0)table=worktopY;panelPosition=(rest.position+checkout.position)*.5f+workspaceForward*1.4f;panelPosition=new Vector3(panelPosition.x,floor+1.35f,panelPosition.z);panelRotation=Quaternion.LookRotation(-workspaceForward,Vector3.up);}Mark(rest,"BASKET REST",table+.025f,.42f);Mark(checkout,"Checkout",table+.02f,.28f);Mark(cardReader,"Tap card",table+.02f,.14f);panel=VenuePanel.Create(this,panelPosition,panelRotation,"Festival gift • any 4 sweets");beginControl=panel.AddButton("Make a Gift Basket",BeginGame);openControl=panel.AddButton("Open Cabinet",OpenCabinet);switchControl=panel.AddButton("Switch open side",SwitchOpenSide);confirmControl=panel.AddButton("Confirm Gift",ConfirmGift);restGhost=new GameObject("Basket rest preview").AddComponent<SweetPlacementGhost>();restGhost.transform.SetParent(transform,false);dockGhost=new GameObject("Checkout preview").AddComponent<SweetPlacementGhost>();dockGhost.transform.SetParent(transform,false);RefreshControls();panel.SetPhysicalSize(.9f,1.2f);}
 public override void BeginGame(){ResetGame();base.BeginGame();Credit("begin");status="Open the cabinet, then place the basket on its outlined rest. Choose any 4 sweets.";}
 void Mark(Transform target,string label,float surfaceY,float radius){var go=new GameObject(label+" outline");go.transform.SetParent(target,false);go.transform.position=new Vector3(target.position.x,surfaceY,target.position.z);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.widthMultiplier=label=="BASKET REST"?.018f:.008f;line.positionCount=4;line.SetPositions(new[]{new Vector3(-radius,0,-radius),new Vector3(radius,0,-radius),new Vector3(radius,0,radius),new Vector3(-radius,0,radius)});var amber=new Color(.9f,.65f,.28f);var shader=Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Sprites/Default");if(shader){line.material=new Material(shader);line.material.color=amber;}line.startColor=line.endColor=amber;var text=new GameObject(label+" label").AddComponent<TextMesh>();text.transform.SetParent(go.transform,false);text.transform.localPosition=Vector3.up*.07f;text.transform.rotation=panelRotation;text.text=label;text.fontSize=40;text.characterSize=label=="BASKET REST"?.02f:.012f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(1,.9f,.7f);}
 public void OpenCabinet(){if(!begun||completed)return;cabinetOpen=true;Credit("cabinet");status="Grip the basket; release it over the large BASKET REST outline on the counter.";}
 public void SwitchOpenSide(){if(!cabinetOpen||completed)return;otherSide=!otherSide;status="Cabinet opening switched. Choose any 4 sweets.";}
 public void ConfirmGift(){if(!begun||!docked||!authorized||selected.Count!=4){status="Dock exactly 4 sweets and tap your festival card first.";return;}confirmed=true;Credit("confirm");foreach(var s in selected)s.grab.enabled=false;Complete("Gift confirmed! Your four sweets are ready at checkout. Happy Diwali!");MovePanelToReader();}
 void Invalidate(){authorized=false;confirmed=false;docked=docked&&!basket.IsHeld&&Vector3.Distance(basket.transform.position,checkout.position)<snapRadius;}
 public bool AddSweet(GrabbableItem sweet){if(!begun||!cabinetOpen||!basketReady||confirmed||selected.Count>=4||selected.Contains(sweet)||!sweets.Contains(sweet))return false;Invalidate();selected.Add(sweet);Seat(sweet,selected.Count-1);Credit("sweet-"+selected.Count);status=selected.Count+"/4: "+string.Join(", ",selected.Select(s=>s.itemId))+ (selected.Count==4?(docked?". Tap the festival card again.":". Carry basket to checkout."):". Choose any sweet.");return true;}
 Bounds NativeBounds(GrabbableItem item,Transform frame){var b=new Bounds();bool first=true;foreach(var mf in item.GetComponentsInChildren<MeshFilter>(true)){if(!mf.sharedMesh||mf.GetComponentInParent<GrabbableItem>()!=item)continue;var mb=mf.sharedMesh.bounds;for(int k=0;k<8;k++){var p=frame.InverseTransformPoint(mf.transform.TransformPoint(mb.center+Vector3.Scale(mb.extents,new Vector3((k&1)==0?-1:1,(k&2)==0?-1:1,(k&4)==0?-1:1))));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}}return b;}
 void Seat(GrabbableItem sweet,int index){var bowl=NativeBounds(basket,basket.transform);var slot=slots[index];slot.localPosition=Vector3.zero;slot.localRotation=sweet.itemId=="Gujiya"?gujiyaRestRotation:Quaternion.identity;sweet.SnapTo(slot);if(sweetScales.TryGetValue(sweet,out var scale))sweet.transform.localScale=scale;var food=NativeBounds(sweet,basket.transform);float spanX=bowl.size.x*.68f,spanZ=bowl.size.z*.68f;float cellX=spanX*.5f-.08f,cellZ=spanZ*.5f-.08f;float fit=Mathf.Min(1,cellX/Mathf.Max(.001f,food.size.x),cellZ/Mathf.Max(.001f,food.size.z));sweet.transform.localScale*=fit;food=NativeBounds(sweet,basket.transform);float x=bowl.center.x+(index%2==0?-1:1)*spanX*.25f;float z=bowl.center.z+(index<2?-1:1)*spanZ*.25f;float linerY=bowl.min.y+.08f;slot.localPosition=new Vector3(x-food.center.x,linerY-food.min.y,z-food.center.z);}
 public bool RemoveSweet(GrabbableItem sweet){if(confirmed||!selected.Remove(sweet))return false;sweet.transform.SetParent(transform,true);if(sweetScales.TryGetValue(sweet,out var scale))sweet.transform.localScale=scale;Invalidate();for(int i=0;i<selected.Count;i++)Seat(selected[i],i);status=selected.Count+"/4. Selection changed; tap card again at checkout.";return true;}
 public bool AuthorizeCard(){if(!begun||!docked||selected.Count!=4||confirmed)return false;if(authorized)return true;authorized=true;Credit("card");status="CARD ACCEPTED ✓ Your gift is authorized. Select Confirm Gift beside the reader.";MovePanelToReader();StartCoroutine(CardFeedback());return true;}
 bool NearPlacement(GrabbableItem item,Transform target){var d=item.transform.position-target.position;return new Vector2(d.x,d.z).magnitude<.9f&&d.y>-.5f&&d.y<1.5f;}
 void MovePanelToReader(){if(!panel)return;panel.transform.position=cardReader.position+workspaceForward*.45f+Vector3.Cross(Vector3.up,workspaceForward)*.7f+Vector3.up*.35f;if(Camera.main)panel.FaceToward(Camera.main.transform.position);}
 IEnumerator CardFeedback(){var marker=new GameObject("CARD ACCEPTED").AddComponent<TextMesh>();marker.transform.SetParent(transform,false);marker.transform.position=cardReader.position+Vector3.up*.22f;marker.transform.rotation=panelRotation;marker.text="CARD ACCEPTED ✓";marker.fontSize=48;marker.characterSize=.016f;marker.color=new Color(.4f,1,.6f);marker.anchor=TextAnchor.MiddleCenter;yield return new WaitForSeconds(3);Destroy(marker.gameObject);}
 void PlaceBasket(Transform target,bool atCheckout){if(!placing)StartCoroutine(SettleBasket(target,atCheckout));}
 IEnumerator SettleBasket(Transform target,bool atCheckout){placing=true;var item=basket;var start=item.transform.position;var rotation=item.transform.rotation;float t=0;while(t<1&&!item.IsHeld){t+=Time.deltaTime*3;item.transform.SetPositionAndRotation(Vector3.Lerp(start,target.position,t*t),Quaternion.Slerp(rotation,target.rotation,t));yield return null;}placing=false;if(item.IsHeld)yield break;item.SnapTo(target);if(atCheckout){docked=true;Credit("checkout");status="4/4 sweets checked. Tap the festival card on the outlined reader.";MovePanelToReader();}else{basketReady=true;Credit("basket-rest");status="Basket ready. Release any four sweets into the bowl.";if(panel){panel.transform.position=rest.position+workspaceForward*.45f+Vector3.Cross(Vector3.up,workspaceForward)*.95f+Vector3.up*.25f;if(Camera.main)panel.FaceToward(Camera.main.transform.position);}}}
 public override void Tick(){
  RefreshControls();if(restGhost){var heldBasket=baskets.FirstOrDefault(b=>b&&b.IsHeld)??basket;restGhost.Show(heldBasket,rest,begun&&!completed&&!basketReady&&heldBasket.IsHeld&&NearPlacement(heldBasket,rest));dockGhost.Show(basket,checkout,begun&&!completed&&basketReady&&selected.Count==4&&basket.IsHeld&&NearPlacement(basket,checkout));}
  for(int i=0;i<glass.Length;i++)glass[i].localPosition=Vector3.Lerp(glass[i].localPosition,(cabinetOpen?(otherSide?alternateGlass[i]:openGlass[i]):closedGlass[i]),Time.deltaTime*4);
  if(!begun||completed)return;
  foreach(var candidate in baskets){
   bool released=heldBaskets.Contains(candidate)&&!candidate.IsHeld;
   if(released&&candidate!=basket&&NearPlacement(candidate,rest)){
    if(selected.Count==0&&!confirmed){
     if(basketReady&&!basket.IsHeld)basket.ResetItem();
     basket=candidate;basketScale=basketScales[basket];
     foreach(var slot in slots)slot.SetParent(basket.transform,false);
     basketWasHeld=true;basketReady=false;docked=authorized=false;
    }else status="Finish or empty your current basket before choosing the other one.";
   }
   if(candidate.IsHeld)heldBaskets.Add(candidate);else heldBaskets.Remove(candidate);
  }
  bool basketReleased=basketWasHeld&&!basket.IsHeld;
  if(basket.IsHeld&&docked){docked=false;if(!confirmed)authorized=false;}
  if(!basketReady&&basketReleased&&NearPlacement(basket,rest))PlaceBasket(rest,false);
  foreach(var sweet in sweets){if(selected.Contains(sweet)){if(sweet.IsHeld&&!confirmed)RemoveSweet(sweet);}else if(heldLast.Contains(sweet)&&!sweet.IsHeld){var bowl=NativeBounds(basket,basket.transform);var p=basket.transform.InverseTransformPoint(sweet.transform.position);if(Mathf.Abs(p.x-bowl.center.x)<bowl.extents.x+.2f&&Mathf.Abs(p.z-bowl.center.z)<bowl.extents.z+.2f&&p.y>bowl.min.y-.15f&&p.y<bowl.max.y+.3f)AddSweet(sweet);}if(sweet.IsHeld)heldLast.Add(sweet);else heldLast.Remove(sweet);}
  if(basketReady&&selected.Count==4&&basketReleased&&!docked&&NearPlacement(basket,checkout))PlaceBasket(checkout,true);
  if(docked&&card.IsHeld&&Vector3.Distance(card.transform.position,cardReader.position)<.3f)AuthorizeCard();
  basketWasHeld=basket.IsHeld;
  
 }
 void RefreshControls(){if(!beginControl)return;beginControl.gameObject.SetActive(!begun);openControl.gameObject.SetActive(begun&&!cabinetOpen);switchControl.gameObject.SetActive(begun&&cabinetOpen&&!confirmed&&!completed);confirmControl.gameObject.SetActive(begun&&docked&&authorized&&!confirmed&&!completed);}
 void LateUpdate(){foreach(var pair in basketScales)if(pair.Key){pair.Key.transform.localScale=pair.Value;if(pair.Key.IsHeld){var forward=Vector3.ProjectOnPlane(pair.Key.transform.forward,Vector3.up);pair.Key.transform.rotation=Quaternion.LookRotation(forward.sqrMagnitude>.001f?forward:Vector3.forward,Vector3.up);}}}
 public override void ResetGame(){StopAllCoroutines();placing=false;base.ResetGame();heldLast.Clear();heldBaskets.Clear();basketWasHeld=false;selected.Clear();otherSide=false;cabinetOpen=basketReady=docked=authorized=confirmed=false;if(baskets!=null)foreach(var b in baskets)if(b)b.ResetItem();basket=defaultBasket?defaultBasket:basket;if(basket){basketScale=basket.transform.localScale;foreach(var slot in slots)slot.SetParent(basket.transform,false);}if(card)card.ResetItem();if(sweets!=null)foreach(var s in sweets)if(s){s.ResetItem();if(sweetScales.TryGetValue(s,out var scale))s.transform.localScale=scale;}if(glass!=null&&closedGlass!=null)for(int i=0;i<glass.Length;i++)glass[i].localPosition=closedGlass[i];}
}
}
