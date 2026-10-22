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
 readonly Dictionary<GrabbableItem,Transform> armedPlacement=new Dictionary<GrabbableItem,Transform>();
 readonly Dictionary<GrabbableItem,float> armedAt=new Dictionary<GrabbableItem,float>();
 readonly Dictionary<GrabbableItem,Transform> releasedPlacements=new Dictionary<GrabbableItem,Transform>();
 Vector3 packingMenuAnchor;LineRenderer restRing,dockRing;

 UnityEngine.UI.Button beginControl,openControl,switchControl,confirmControl;
 public Quaternion gujiyaRestRotation=Quaternion.identity;
 readonly Dictionary<GrabbableItem,Vector3> sweetScales=new Dictionary<GrabbableItem,Vector3>();
 protected override void Awake(){base.Awake();defaultBasket=basket;if(baskets==null||baskets.Length==0)baskets=new[]{basket};foreach(var b in baskets){basketScales[b]=b.transform.localScale;b.ConfigureGrab();b.grab.trackRotation=false;var item=b;b.grab.selectExited.AddListener(_=>QueueBasketRelease(item));}snapRadius=Mathf.Max(snapRadius,.65f);basketScale=basket.transform.localScale;basket.grab.trackScale=false;basket.grab.useDynamicAttach=true;basket.grab.matchAttachPosition=true;basket.grab.matchAttachRotation=true;if(sweets!=null)foreach(var sweet in sweets)if(sweet){sweetScales[sweet]=sweet.transform.localScale;sweet.grab.trackScale=false;sweet.grab.useDynamicAttach=true;sweet.grab.matchAttachPosition=true;sweet.grab.matchAttachRotation=true;}}

 void Start(){
  openGlass=(Vector3[])closedGlass.Clone();alternateGlass=(Vector3[])closedGlass.Clone();
  var first=GameObjectUtility.BoundsOf(glass[0]);var second=GameObjectUtility.BoundsOf(glass[1]);
  // Keep both panels within the cabinet, on separate depth tracks.
  var across=second.center-first.center;across.y=0;
  var depth=workspaceForward.normalized*.035f;
  openGlass[0]+=glass[0].parent.InverseTransformVector(across+depth);
  alternateGlass[1]+=glass[1].parent.InverseTransformVector(-across-depth);
  howToPlay="Grip to grab; release to place.\n1. Make a Gift Basket, then Open Cabinet. Use Switch open side to reach the other half.\n2. Choose either basket on the wall shelves and grip to bring it in front of your hand. Release it over BASKET REST.\n3. Release any 4 sweets into the bowl; repeats welcome. Grip a packed sweet to swap it.\n4. Release the full basket at CHECKOUT.\n5. Touch the reader with the festival card, then Confirm Gift.\n6. Confirm Gift finishes the activity at checkout.\nPoint at buttons and press trigger. Restart restores all props. No payment or budget.";var venue=GameObjectUtility.FindExact("SweetShop - Approved Warm Palette")??GameObjectUtility.FindExact("SweetShop - Native PBR");float floor=venue?venue.position.y:0;float table=rest.position.y-GameObjectUtility.BoundsOf(basket.transform).extents.y;if(venue){var counter=GameObjectUtility.FindUnder(venue,"Cube.012");if(worktopY>0)table=worktopY;panelPosition=(rest.position+checkout.position)*.5f+workspaceForward*1.4f;panelPosition=new Vector3(panelPosition.x,floor+1.35f,panelPosition.z);panelRotation=Quaternion.LookRotation(-workspaceForward,Vector3.up);}restRing=Mark(rest,"BASKET REST",table+.025f,.42f);dockRing=Mark(checkout,"Checkout",table+.02f,.42f);Mark(cardReader,"Tap card",table+.02f,.14f);panel=VenuePanel.Create(this,panelPosition,panelRotation,"Festival gift • any 4 sweets");beginControl=panel.AddButton("Make a Gift Basket",BeginGame);openControl=panel.AddButton("Open Cabinet",OpenCabinet);switchControl=panel.AddButton("Switch open side",SwitchOpenSide);confirmControl=panel.AddButton("Confirm Gift",ConfirmGift);restGhost=new GameObject("Basket rest preview").AddComponent<SweetPlacementGhost>();restGhost.transform.SetParent(transform,false);dockGhost=new GameObject("Checkout preview").AddComponent<SweetPlacementGhost>();dockGhost.transform.SetParent(transform,false);ChoosePackingMenuAnchor();MovePanelToPackingSide();RefreshControls();panel.SetPhysicalSize(.48f,.68f);panel.autoFacePlayer=false;var menuHere=panel.transform.Find("Activity tools/Move menu here");if(menuHere)menuHere.gameObject.SetActive(false);}
 public override void BeginGame(){ResetGame();base.BeginGame();Credit("begin");status="Open the cabinet, then place the basket on its outlined rest. Choose any 4 sweets.";}
 LineRenderer Mark(Transform target,string label,float surfaceY,float radius){var go=new GameObject(label+" outline");go.transform.SetParent(target,false);go.transform.position=new Vector3(target.position.x,surfaceY,target.position.z);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.widthMultiplier=label=="BASKET REST"?.018f:.008f;line.positionCount=4;line.SetPositions(new[]{new Vector3(-radius,0,-radius),new Vector3(radius,0,-radius),new Vector3(radius,0,radius),new Vector3(-radius,0,radius)});var amber=new Color(.9f,.65f,.28f);var shader=Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Sprites/Default");if(shader){line.material=new Material(shader);line.material.color=amber;}line.startColor=line.endColor=amber;var text=new GameObject(label+" label").AddComponent<TextMesh>();text.transform.SetParent(go.transform,false);text.transform.localPosition=Vector3.up*.07f;text.transform.rotation=panelRotation;text.text=label;text.fontSize=40;text.characterSize=label=="BASKET REST"?.02f:.012f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(1,.9f,.7f);return line;}
 public void OpenCabinet(){if(!begun||completed)return;cabinetOpen=true;Credit("cabinet");status="Grip the basket; release it over the large BASKET REST outline on the counter.";}
 public void SwitchOpenSide(){if(!cabinetOpen||completed)return;otherSide=!otherSide;status="Cabinet opening switched. Choose any 4 sweets.";}
 public void ConfirmGift(){if(!begun||!docked||!authorized||selected.Count!=4){status="Dock exactly 4 sweets and tap your festival card first.";return;}confirmed=true;Credit("confirm");foreach(var s in selected)s.grab.enabled=false;Complete("Gift confirmed! Your four sweets are ready at checkout. Happy Diwali!");MovePanelToReader();}
 void Invalidate(){authorized=false;confirmed=false;docked=docked&&!basket.IsHeld&&Vector3.Distance(basket.transform.position,checkout.position)<snapRadius;}
 public bool AddSweet(GrabbableItem sweet){if(!begun||!cabinetOpen||!basketReady||confirmed||selected.Count>=4||selected.Contains(sweet)||!sweets.Contains(sweet))return false;Invalidate();selected.Add(sweet);Seat(sweet,selected.Count-1);Credit("sweet-"+selected.Count);status=selected.Count+"/4: "+string.Join(", ",selected.Select(s=>s.itemId))+ (selected.Count==4?(docked?". Tap the festival card again.":". Carry basket to checkout."):". Choose any sweet.");return true;}
 Bounds NativeBounds(GrabbableItem item,Transform frame){var b=new Bounds();bool first=true;foreach(var mf in item.GetComponentsInChildren<MeshFilter>(true)){if(!mf.sharedMesh||mf.GetComponentInParent<GrabbableItem>()!=item)continue;var mb=mf.sharedMesh.bounds;for(int k=0;k<8;k++){var p=frame.InverseTransformPoint(mf.transform.TransformPoint(mb.center+Vector3.Scale(mb.extents,new Vector3((k&1)==0?-1:1,(k&2)==0?-1:1,(k&4)==0?-1:1))));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}}return b;}
 void Seat(GrabbableItem sweet,int index){var bowl=NativeBounds(basket,basket.transform);var slot=slots[index];slot.localPosition=Vector3.zero;slot.localRotation=sweet.itemId=="Gujiya"?gujiyaRestRotation:Quaternion.identity;sweet.SnapTo(slot);if(sweetScales.TryGetValue(sweet,out var scale))sweet.transform.localScale=scale;var food=NativeBounds(sweet,basket.transform);float spanX=bowl.size.x*.68f,spanZ=bowl.size.z*.68f;float cellX=spanX*.5f-.08f,cellZ=spanZ*.5f-.08f;float fit=Mathf.Min(1,cellX/Mathf.Max(.001f,food.size.x),cellZ/Mathf.Max(.001f,food.size.z));sweet.transform.localScale*=fit;food=NativeBounds(sweet,basket.transform);float x=bowl.center.x+(index%2==0?-1:1)*spanX*.25f;float z=bowl.center.z+(index<2?-1:1)*spanZ*.25f;float linerY=bowl.min.y+.08f;slot.localPosition=new Vector3(x-food.center.x,linerY-food.min.y,z-food.center.z);}
 public bool RemoveSweet(GrabbableItem sweet){if(confirmed||!selected.Remove(sweet))return false;sweet.transform.SetParent(transform,true);if(sweetScales.TryGetValue(sweet,out var scale))sweet.transform.localScale=scale;Invalidate();for(int i=0;i<selected.Count;i++)Seat(selected[i],i);status=selected.Count+"/4. Selection changed; tap card again at checkout.";return true;}
 public bool AuthorizeCard(){if(!begun||!docked||selected.Count!=4||confirmed)return false;if(authorized)return true;authorized=true;Credit("card");status="CARD ACCEPTED ✓ Your gift is authorized. Select Confirm Gift beside the reader.";MovePanelToReader();StartCoroutine(CardFeedback());return true;}
 public bool CanPlaceBasket(GrabbableItem item,Transform target){if(!item||!target)return false;var local=NativeBounds(item,item.transform);var world=new Bounds(item.transform.TransformPoint(local.center),Vector3.zero);for(int k=0;k<8;k++)world.Encapsulate(item.transform.TransformPoint(local.center+Vector3.Scale(local.extents,new Vector3((k&1)==0?-1:1,(k&2)==0?-1:1,(k&4)==0?-1:1))));float dx=Mathf.Max(0,Mathf.Abs(world.center.x-target.position.x)-world.extents.x-.42f);float dz=Mathf.Max(0,Mathf.Abs(world.center.z-target.position.z)-world.extents.z-.42f);float center=Vector2.Distance(new Vector2(world.center.x,world.center.z),new Vector2(target.position.x,target.position.z));float surface=worktopY>0?worktopY:target.position.y-world.extents.y;return dx*dx+dz*dz<1.15f*1.15f&&center<1.75f&&world.min.y>surface-.9f&&world.min.y<surface+1.8f;}
 bool NearPlacement(GrabbableItem item,Transform target)=>CanPlaceBasket(item,target);
 Transform NearestPlacement(GrabbableItem item){if(selected.Count<4)return CanPlaceBasket(item,rest)?rest:null;bool r=CanPlaceBasket(item,rest),d=CanPlaceBasket(item,checkout);if(!r)return d?checkout:null;if(!d)return rest;var p=item.transform.position;return Vector2.Distance(new Vector2(p.x,p.z),new Vector2(rest.position.x,rest.position.z))<Vector2.Distance(new Vector2(p.x,p.z),new Vector2(checkout.position.x,checkout.position.z))?rest:checkout;}
 void ArmBasket(GrabbableItem item){var target=NearestPlacement(item);if(target){armedPlacement[item]=target;armedAt[item]=Time.unscaledTime;}else if(armedAt.TryGetValue(item,out var time)&&Time.unscaledTime-time>.65f){armedPlacement.Remove(item);armedAt.Remove(item);}}
 void QueueBasketRelease(GrabbableItem item){if(!begun||completed||confirmed)return;var target=NearestPlacement(item);if(!target&&armedPlacement.TryGetValue(item,out var armed)&&armedAt.TryGetValue(item,out var time)&&Time.unscaledTime-time<.85f)target=armed;if(target)releasedPlacements[item]=target;}
 void ProcessBasketReleases(){if(placing)return;foreach(var pair in releasedPlacements.ToArray()){var item=pair.Key;if(item.IsHeld)continue;releasedPlacements.Remove(item);armedPlacement.Remove(item);armedAt.Remove(item);if(item!=basket){if(selected.Count!=0||basket.IsHeld){status="Return the active basket before choosing the other one.";continue;}if(basketReady)basket.ResetItem();basket=item;basketScale=basketScales[item];foreach(var slot in slots)slot.SetParent(item.transform,false);basketReady=false;}if(pair.Value==checkout&&selected.Count!=4)continue;PlaceBasket(pair.Value,pair.Value==checkout);break;}}
 void ChoosePackingMenuAnchor(){
  var forward=Vector3.ProjectOnPlane(workspaceForward,Vector3.up).normalized;
  if(forward.sqrMagnitude<.01f)forward=Vector3.forward;
  var side=Vector3.Cross(Vector3.up,forward);
  packingMenuAnchor=rest.position+side*1.25f+forward*.65f;
  packingMenuAnchor.y=(worktopY>0?worktopY:rest.position.y)+.4f;
 }
 void MovePanelToPackingSide(){if(!panel)return;panel.transform.position=packingMenuAnchor;panel.FaceToward(rest.position+workspaceForward*2.2f);}
 void MovePanelToReader(){if(!panel)return;panel.transform.position=cardReader.position+workspaceForward*.45f+Vector3.Cross(Vector3.up,workspaceForward)*.7f+Vector3.up*.35f;panel.FaceToward(rest.position+workspaceForward*2.2f);}
 IEnumerator CardFeedback(){var marker=new GameObject("CARD ACCEPTED").AddComponent<TextMesh>();marker.transform.SetParent(transform,false);marker.transform.position=cardReader.position+Vector3.up*.22f;marker.transform.rotation=panelRotation;marker.text="CARD ACCEPTED ✓";marker.fontSize=48;marker.characterSize=.016f;marker.color=new Color(.4f,1,.6f);marker.anchor=TextAnchor.MiddleCenter;yield return new WaitForSeconds(3);Destroy(marker.gameObject);}
 void PlaceBasket(Transform target,bool atCheckout){if(!placing)StartCoroutine(SettleBasket(target,atCheckout));}
 IEnumerator SettleBasket(Transform target,bool atCheckout){placing=true;var item=basket;item.transform.SetParent(transform,true);var body=item.GetComponent<Rigidbody>();if(body){if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}body.isKinematic=true;}var start=item.transform.position;var rotation=item.transform.rotation;float t=0;while(t<1&&!item.IsHeld){t+=Time.deltaTime*3;item.transform.SetPositionAndRotation(Vector3.Lerp(start,target.position,Mathf.SmoothStep(0,1,t)),Quaternion.Slerp(rotation,target.rotation,Mathf.SmoothStep(0,1,t)));yield return null;}placing=false;if(item.IsHeld)yield break;item.SnapTo(target);if(atCheckout){docked=true;Credit("checkout");status="4/4 sweets checked. Tap the festival card on the outlined reader.";MovePanelToReader();}else{basketReady=true;docked=authorized=false;Credit("basket-rest");status=selected.Count==4?"Basket safely returned. Carry it to CHECKOUT when ready.":"Basket safely resting — "+selected.Count+"/4 sweets. Release sweets into the bowl.";MovePanelToPackingSide();}}
 public override void Tick(){
  RefreshControls();
  var heldBasket=baskets.FirstOrDefault(b=>b&&b.IsHeld);var preview=heldBasket?NearestPlacement(heldBasket):null;
  bool restClose=heldBasket&&preview==rest&&CanPlaceBasket(heldBasket,rest);
  bool dockClose=heldBasket&&preview==checkout&&CanPlaceBasket(heldBasket,checkout);
  if(restGhost){restGhost.Show(heldBasket?heldBasket:basket,rest,begun&&!completed&&heldBasket&&(selected.Count<4||preview!=checkout),restClose);dockGhost.Show(heldBasket?heldBasket:basket,checkout,begun&&!completed&&heldBasket&&selected.Count==4&&(preview==null||preview==checkout),dockClose);}
  if(restRing){restRing.startColor=restRing.endColor=restClose?new Color(.3f,1,.55f):new Color(1,.72f,.28f);restRing.widthMultiplier=restClose?.028f:.018f;}
  if(dockRing){dockRing.startColor=dockRing.endColor=dockClose?new Color(.3f,1,.55f):new Color(1,.72f,.28f);dockRing.widthMultiplier=dockClose?.028f:.018f;}
  for(int i=0;i<glass.Length;i++)glass[i].localPosition=Vector3.Lerp(glass[i].localPosition,(cabinetOpen?(otherSide?alternateGlass[i]:openGlass[i]):closedGlass[i]),Time.deltaTime*4);
  if(!begun||completed)return;
  foreach(var candidate in baskets){if(candidate.IsHeld){ArmBasket(candidate);heldBaskets.Add(candidate);}else if(heldBaskets.Remove(candidate))QueueBasketRelease(candidate);}
  ProcessBasketReleases();
  if(basket.IsHeld&&docked){docked=false;authorized=false;}
  foreach(var sweet in sweets){if(selected.Contains(sweet)){if(sweet.IsHeld&&!confirmed)RemoveSweet(sweet);}else if(heldLast.Contains(sweet)&&!sweet.IsHeld){var bowl=NativeBounds(basket,basket.transform);var p=basket.transform.InverseTransformPoint(sweet.transform.position);if(DiyaRestTarget.Contains(sweet,basket.transform,.8f)||Mathf.Abs(p.x-bowl.center.x)<bowl.extents.x+.2f&&Mathf.Abs(p.z-bowl.center.z)<bowl.extents.z+.2f&&p.y>bowl.min.y-.15f&&p.y<bowl.max.y+.3f)AddSweet(sweet);}if(sweet.IsHeld)heldLast.Add(sweet);else heldLast.Remove(sweet);}

  if(docked&&card.IsHeld&&Vector3.Distance(card.transform.position,cardReader.position)<.55f)AuthorizeCard();
  basketWasHeld=basket.IsHeld;
  
 }
 void RefreshControls(){if(!beginControl)return;var labels=panel.GetComponentsInChildren<UnityEngine.UI.Text>(true).Where(t=>t.transform.parent==panel.transform).ToArray();if(labels.Length>2)labels[2].gameObject.SetActive(!begun||completed);beginControl.gameObject.SetActive(!begun);openControl.gameObject.SetActive(begun&&!cabinetOpen);switchControl.gameObject.SetActive(begun&&cabinetOpen&&!confirmed&&!completed);confirmControl.gameObject.SetActive(begun&&docked&&authorized&&!confirmed&&!completed);}
 void LateUpdate(){foreach(var pair in basketScales)if(pair.Key){pair.Key.transform.localScale=pair.Value;if(pair.Key.IsHeld){var forward=Vector3.ProjectOnPlane(pair.Key.transform.forward,Vector3.up);pair.Key.transform.rotation=Quaternion.LookRotation(forward.sqrMagnitude>.001f?forward:Vector3.forward,Vector3.up);}}}
 public override void ResetGame(){StopAllCoroutines();foreach(var label in GetComponentsInChildren<TextMesh>(true))if(label.name=="CARD ACCEPTED")Destroy(label.gameObject);ChoosePackingMenuAnchor();MovePanelToPackingSide();placing=false;armedPlacement.Clear();armedAt.Clear();releasedPlacements.Clear();base.ResetGame();heldLast.Clear();heldBaskets.Clear();basketWasHeld=false;selected.Clear();otherSide=false;cabinetOpen=basketReady=docked=authorized=confirmed=false;if(baskets!=null)foreach(var b in baskets)if(b)b.ResetItem();basket=defaultBasket?defaultBasket:basket;if(basket){basketScale=basket.transform.localScale;foreach(var slot in slots)slot.SetParent(basket.transform,false);}if(card)card.ResetItem();if(sweets!=null)foreach(var s in sweets)if(s){s.ResetItem();if(sweetScales.TryGetValue(s,out var scale))s.transform.localScale=scale;}if(glass!=null&&closedGlass!=null)for(int i=0;i<glass.Length;i++)glass[i].localPosition=closedGlass[i];}
}
}
