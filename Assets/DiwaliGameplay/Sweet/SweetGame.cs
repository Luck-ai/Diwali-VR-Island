using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace DiwaliGameplay {
[DefaultExecutionOrder(100)]
public class SweetGame : VenueGame {
 public GrabbableItem basket,card;
 public GrabbableItem[] baskets;
 GrabbableItem defaultBasket;
 readonly HashSet<GrabbableItem> heldBaskets=new HashSet<GrabbableItem>();
 readonly Dictionary<GrabbableItem,Vector3> basketScales=new Dictionary<GrabbableItem,Vector3>();
 Transform playerHead;
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
 public float worktopY;public Vector3 workspaceForward=Vector3.forward,cabinetForward=Vector3.forward;
 public Transform basketZoneSign;
 VenuePanel panel;SweetPlacementGhost restGhost,dockGhost,shelfGhost;TextMesh shelfHint;bool placing,cardMustLeaveReader;
 readonly Dictionary<GrabbableItem,Transform> shelfTargets=new Dictionary<GrabbableItem,Transform>();
 readonly Dictionary<GrabbableItem,Transform> shelfParents=new Dictionary<GrabbableItem,Transform>();
 readonly Dictionary<GrabbableItem,float> shelfAimAt=new Dictionary<GrabbableItem,float>();
 readonly HashSet<GrabbableItem> releasedSweets=new HashSet<GrabbableItem>();
 readonly Dictionary<GrabbableItem,Transform> armedPlacement=new Dictionary<GrabbableItem,Transform>();
 readonly Dictionary<GrabbableItem,float> armedAt=new Dictionary<GrabbableItem,float>();
 readonly Dictionary<GrabbableItem,Transform> releasedPlacements=new Dictionary<GrabbableItem,Transform>();
 Vector3 packingMenuAnchor;LineRenderer restRing,dockRing;

 UnityEngine.UI.Button beginControl,openControl,switchControl;
 public VenuePanel ActivityPanel=>panel;
 public Quaternion gujiyaRestRotation=Quaternion.identity;
 readonly Dictionary<BoxCollider,(Vector3 center,Vector3 size)> shelfColliderShapes=new Dictionary<BoxCollider,(Vector3 center,Vector3 size)>();
 readonly Dictionary<GrabbableItem,Vector3> sweetScales=new Dictionary<GrabbableItem,Vector3>();
 protected override void Awake(){base.Awake();defaultBasket=basket;if(baskets==null||baskets.Length==0)baskets=new[]{basket};foreach(var b in baskets){basketScales[b]=b.transform.localScale;MakeBasketInteriorReachable(b);b.ConfigureGrab();b.grab.trackPosition=false;b.grab.trackRotation=false;b.grab.useDynamicAttach=false;var item=b;b.grab.selectExited.AddListener(_=>QueueBasketRelease(item));}snapRadius=Mathf.Max(snapRadius,.65f);basketScale=basket.transform.localScale;if(sweets!=null)foreach(var sweet in sweets)if(sweet){sweet.ConfigureGrab();foreach(var box in sweet.GetComponentsInChildren<BoxCollider>(true))if(box.GetComponentInParent<GrabbableItem>()==sweet)shelfColliderShapes[box]=(box.center,box.size);sweetScales[sweet]=sweet.transform.localScale;shelfParents[sweet]=sweet.transform.parent;
 var shelf=new GameObject(sweet.itemId+" original shelf pose").transform;shelf.SetParent(transform,false);shelf.SetPositionAndRotation(sweet.transform.position,sweet.transform.rotation);shelfTargets[sweet]=shelf;
 var item=sweet;sweet.grab.selectEntered.AddListener(_=>{if(!confirmed)RemoveSweet(item);});sweet.grab.selectExited.AddListener(_=>{if(begun&&!completed)releasedSweets.Add(item);});sweet.grab.trackScale=false;sweet.grab.useDynamicAttach=true;sweet.grab.matchAttachPosition=true;sweet.grab.matchAttachRotation=true;}}

 void MakeBasketInteriorReachable(GrabbableItem item){
  // The rim remains solid at its base; its upper collision strip must not hide the sweets from controller rays.
  foreach(var box in item.GetComponents<BoxCollider>()){
   if(!box.enabled)continue;var world=box.bounds.size;
   if(world.y<.15f||world.y>.28f||Mathf.Min(world.x,world.z)>.05f)continue;
   var size=box.size;float reduction=size.y*(1-.08f/world.y);var centre=box.center;
   size.y-=reduction;centre.y-=reduction*.5f;box.size=size;box.center=centre;
  }
 }
 void Start(){
  card.grab.selectEntered.AddListener(args=>{if(args.interactorObject is NearFarInteractor interactor)interactor.interactionAttachController.MoveTo(card.transform.position);});
  var checkoutDisplay=GetComponent<SweetCheckoutDisplay>();if(checkoutDisplay)checkoutDisplay.Create(this);
  openGlass=(Vector3[])closedGlass.Clone();alternateGlass=(Vector3[])closedGlass.Clone();
  var first=GameObjectUtility.BoundsOf(glass[0]);var second=GameObjectUtility.BoundsOf(glass[1]);
  // Keep both panels within the cabinet, on separate depth tracks.
  var across=second.center-first.center;across.y=0;
  var depth=cabinetForward.normalized*.035f;
  openGlass[0]+=glass[0].parent.InverseTransformVector(across+depth);
  alternateGlass[1]+=glass[1].parent.InverseTransformVector(-across-depth);
  howToPlay="Grip to grab; release to place.\n1. Make a Gift Basket, then Open Cabinet. Switch open side to reach the other half.\n2. Choose either basket on the wall shelves and release it over BASKET REST.\n3. Release any 4 sweets over the basket; repeats welcome. Selected sweets float above the rim for easy swapping. Grip one to swap it; aim at its green shelf preview and release to return it.\n4. Leave the basket in its recess and select Checkout on the small monitor.\n5. Touch the outlined reader with the festival card. CARD ACCEPTED appears on the monitor.\n6. Select Share Gift on the monitor to add your basket to the central rangoli celebration.\nPoint at buttons and press trigger. Restart restores all props. No payment or budget.";
  float table=worktopY>0?worktopY:rest.position.y-GameObjectUtility.BoundsOf(basket.transform).extents.y;
  if(panelRotation==default)panelRotation=Quaternion.LookRotation(-workspaceForward,Vector3.up);
  restRing=Mark(rest,"BASKET REST",table+.025f,.42f);dockRing=Mark(checkout,"CHECKOUT",table+.025f,.42f);Mark(cardReader,"TAP CARD",cardReader.position.y,.14f);
  panel=VenuePanel.Create(this,panelPosition,panelRotation,"Festival gift • any 4 sweets");beginControl=panel.AddButton("Make a Gift Basket",BeginGame);openControl=panel.AddButton("Open Cabinet",OpenCabinet);switchControl=panel.AddButton("Switch open side",SwitchOpenSide);restGhost=new GameObject("Basket rest preview").AddComponent<SweetPlacementGhost>();restGhost.transform.SetParent(transform,false);dockGhost=new GameObject("Checkout preview").AddComponent<SweetPlacementGhost>();dockGhost.transform.SetParent(transform,false);shelfGhost=new GameObject("Sweet shelf return preview").AddComponent<SweetPlacementGhost>();shelfGhost.transform.SetParent(transform,false);shelfHint=new GameObject("Sweet shelf return hint").AddComponent<TextMesh>();shelfHint.transform.SetParent(transform,false);shelfHint.fontSize=42;shelfHint.characterSize=.012f;shelfHint.anchor=TextAnchor.MiddleCenter;shelfHint.gameObject.SetActive(false);ChoosePackingMenuAnchor();MovePanelToPackingSide();RefreshControls();panel.SetPhysicalSize(.68f,.9f);panel.autoFacePlayer=false;var menuHere=panel.transform.Find("Activity tools/Move menu here");if(menuHere)menuHere.gameObject.SetActive(false);}
 public override void BeginGame(){ResetGame();base.BeginGame();Credit("begin");status="Open the cabinet, then place the basket on its outlined rest. Choose any 4 sweets.";}
 LineRenderer Mark(Transform target,string label,float surfaceY,float radius){var go=new GameObject(label+" outline");go.transform.SetParent(target,false);go.transform.position=new Vector3(target.position.x,surfaceY,target.position.z);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.widthMultiplier=label=="BASKET REST"?.018f:.008f;line.positionCount=4;line.SetPositions(new[]{new Vector3(-radius,0,-radius),new Vector3(radius,0,-radius),new Vector3(radius,0,radius),new Vector3(-radius,0,radius)});var amber=new Color(.9f,.65f,.28f);var shader=Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Sprites/Default");if(shader){line.material=new Material(shader);line.material.color=amber;}line.startColor=line.endColor=amber;var text=new GameObject(label+" label").AddComponent<TextMesh>();text.transform.SetParent(go.transform,false);text.transform.localPosition=Vector3.up*.07f;text.transform.rotation=panelRotation;text.text=label;text.fontSize=40;text.characterSize=label=="BASKET REST"?.02f:.012f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(1,.9f,.7f);return line;}
 public void OpenCabinet(){if(!begun||completed)return;cabinetOpen=true;Credit("cabinet");status="Grip the basket; release it over the large BASKET REST outline on the counter.";}
 public void SwitchOpenSide(){if(!cabinetOpen||completed)return;otherSide=!otherSide;status="Cabinet opening switched. Choose any 4 sweets.";}
 public void CheckoutBasket(){if(!begun||completed||!basketReady||selected.Count!=4||basket.IsHeld||docked)return;PlaceBasket(checkout,true);}
 public void ConfirmGift(){if(!begun||!docked||!authorized||selected.Count!=4){status="Choose 4 sweets, select Checkout on the monitor, then tap your festival card.";return;}confirmed=true;ShowShelfReturn(null);Credit("confirm");foreach(var s in selected)s.grab.enabled=false;Complete("Gift shared! Your basket is now beside the central rangoli. Happy Diwali!");MovePanelToReader();}
 void Invalidate(){if(authorized)cardMustLeaveReader=true;foreach(var label in GetComponentsInChildren<TextMesh>(true))if(label.name=="CARD ACCEPTED")Destroy(label.gameObject);authorized=false;confirmed=false;docked=docked&&!basket.IsHeld&&Vector3.Distance(basket.transform.position,checkout.position)<snapRadius;}
 public bool AddSweet(GrabbableItem sweet){if(!sweet||sweet.IsHeld||!begun||!cabinetOpen||!basketReady||confirmed||selected.Count>=4||selected.Contains(sweet)||!sweets.Contains(sweet))return false;Invalidate();selected.Add(sweet);Repack();Credit("sweet-"+selected.Count);status=selected.Count+"/4: "+string.Join(", ",selected.Select(s=>s.itemId))+ (selected.Count==4?(docked?". Tap the festival card again.":". Select Checkout on the small monitor."):". Choose any sweet.");return true;}
 Bounds NativeBounds(GrabbableItem item,Transform frame){var b=new Bounds();bool first=true;foreach(var mf in item.GetComponentsInChildren<MeshFilter>(true)){if(!mf.sharedMesh||mf.GetComponentInParent<GrabbableItem>()!=item)continue;var mb=mf.sharedMesh.bounds;for(int k=0;k<8;k++){var p=frame.InverseTransformPoint(mf.transform.TransformPoint(mb.center+Vector3.Scale(mb.extents,new Vector3((k&1)==0?-1:1,(k&2)==0?-1:1,(k&4)==0?-1:1))));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}}return b;}
 Bounds BasketFloor(GrabbableItem item){
  var visual=NativeBounds(item,item.transform);BoxCollider floor=null;float lowest=float.PositiveInfinity;
  foreach(var box in item.GetComponents<BoxCollider>()){
   if(!box.enabled||box.size.y>=Mathf.Min(box.size.x,box.size.z)*.35f)continue;
   if(box.size.x<visual.size.x*.5f||box.size.z<visual.size.z*.5f)continue;
   float top=box.center.y+box.size.y*.5f;
   if(top<lowest){lowest=top;floor=box;}
  }
  if(floor)return new Bounds(new Vector3(floor.center.x,lowest,floor.center.z),new Vector3(floor.size.x,0,floor.size.z));
  return new Bounds(new Vector3(visual.center.x,visual.min.y+visual.size.y*.035f,visual.center.z),new Vector3(visual.size.x,0,visual.size.z));
 }
 public float BasketInteriorFloorY(GrabbableItem item)=>BasketFloor(item).center.y;
 void Repack(){
  var order=selected.OrderBy(s=>s.itemId=="Modak"?1:0).ThenBy(s=>NativeBounds(s,s.transform).size.y*Mathf.Abs(s.transform.lossyScale.y)).ToArray();
  for(int i=0;i<order.Length;i++)Seat(order[i],i);
 }
 void RestoreSweetColliders(GrabbableItem sweet){foreach(var box in sweet.GetComponentsInChildren<BoxCollider>(true))if(shelfColliderShapes.TryGetValue(box,out var shape)){box.center=shape.center;box.size=shape.size;}}
 void ThickenPackedPickup(GrabbableItem sweet){
  if(sweet.itemId!="Gujiya"&&sweet.itemId!="Barfi")return;
  var up=basket.transform.up;
  foreach(var box in sweet.GetComponentsInChildren<BoxCollider>(true)){
   if(!box.enabled||box.GetComponentInParent<GrabbableItem>()!=sweet)continue;
   var x=box.transform.TransformVector(Vector3.right);var y=box.transform.TransformVector(Vector3.up);var z=box.transform.TransformVector(Vector3.forward);
   var projection=new Vector3(Vector3.Dot(x,up),Vector3.Dot(y,up),Vector3.Dot(z,up));
   float height=Mathf.Abs(projection.x)*box.size.x+Mathf.Abs(projection.y)*box.size.y+Mathf.Abs(projection.z)*box.size.z;
   if(height>=.09f)continue;
   int axis=0;float alignment=Mathf.Abs(projection.x)/Mathf.Max(.0001f,x.magnitude);
   if(Mathf.Abs(projection.y)/Mathf.Max(.0001f,y.magnitude)>alignment){axis=1;alignment=Mathf.Abs(projection.y)/Mathf.Max(.0001f,y.magnitude);}
   if(Mathf.Abs(projection.z)/Mathf.Max(.0001f,z.magnitude)>alignment)axis=2;
   float projectedScale=Mathf.Abs(projection[axis]);if(projectedScale<.0001f)continue;
   float extra=(.09f-height)/projectedScale;var size=box.size;size[axis]+=extra;box.size=size;
   var center=box.center;center[axis]+=extra*.5f*Mathf.Sign(projection[axis]);box.center=center;
  }
 }
 void Seat(GrabbableItem sweet,int index){RestoreSweetColliders(sweet);var floor=BasketFloor(basket);var slot=slots[index];slot.localPosition=Vector3.zero;slot.localRotation=sweet.itemId=="Gujiya"?gujiyaRestRotation:Quaternion.identity;sweet.SnapTo(slot);if(sweetScales.TryGetValue(sweet,out var scale))sweet.transform.localScale=scale;var food=NativeBounds(sweet,basket.transform);float spanX=floor.size.x*1.05f,spanZ=floor.size.z*1.05f;float cellX=spanX*.5f-.035f,cellZ=spanZ*.5f-.035f;float fit=Mathf.Min(1,cellX/Mathf.Max(.001f,food.size.x),cellZ/Mathf.Max(.001f,food.size.z));sweet.transform.localScale*=fit;food=NativeBounds(sweet,basket.transform);var front=basket.transform.InverseTransformDirection(workspaceForward);front.y=0;front=front.sqrMagnitude>.001f?front.normalized:Vector3.forward;var side=Vector3.Cross(Vector3.up,front);
 var center=floor.center+side*((index%2==0?-1:1)*spanX*.25f)+front*((index<2?1:-1)*spanZ*.25f);float upScale=basket.transform.TransformVector(Vector3.up).magnitude;float linerY=floor.center.y+.14f/Mathf.Max(.001f,upScale);
 slot.localPosition=new Vector3(center.x-food.center.x,linerY-food.min.y,center.z-food.center.z);ThickenPackedPickup(sweet);}
 public bool RemoveSweet(GrabbableItem sweet){if(confirmed||!selected.Remove(sweet))return false;RestoreSweetColliders(sweet);sweet.transform.SetParent(transform,true);if(sweetScales.TryGetValue(sweet,out var scale))sweet.transform.localScale=scale;Invalidate();Repack();status=selected.Count+"/4. Selection changed; tap card again at checkout.";return true;}
 public bool AuthorizeCard(){if(!begun||!docked||selected.Count!=4||confirmed||cardMustLeaveReader)return false;if(authorized)return true;authorized=true;Credit("card");status="CARD ACCEPTED ✓ Your gift is authorized. Select Share Gift on the small monitor.";MovePanelToReader();StartCoroutine(CardFeedback());return true;}
 public bool CanPlaceBasket(GrabbableItem item,Transform target){if(!item||!target)return false;var local=NativeBounds(item,item.transform);var world=new Bounds(item.transform.TransformPoint(local.center),Vector3.zero);for(int k=0;k<8;k++)world.Encapsulate(item.transform.TransformPoint(local.center+Vector3.Scale(local.extents,new Vector3((k&1)==0?-1:1,(k&2)==0?-1:1,(k&4)==0?-1:1))));float dx=Mathf.Max(0,Mathf.Abs(world.center.x-target.position.x)-world.extents.x-.42f);float dz=Mathf.Max(0,Mathf.Abs(world.center.z-target.position.z)-world.extents.z-.42f);float center=Vector2.Distance(new Vector2(world.center.x,world.center.z),new Vector2(target.position.x,target.position.z));float surface=worktopY>0?worktopY:target.position.y-world.extents.y;return dx*dx+dz*dz<1.15f*1.15f&&center<1.75f&&world.min.y>surface-.9f&&world.min.y<surface+1.8f;}
 bool NearPlacement(GrabbableItem item,Transform target)=>CanPlaceBasket(item,target);
 Transform NearestPlacement(GrabbableItem item){if(selected.Count<4)return CanPlaceBasket(item,rest)?rest:null;bool r=CanPlaceBasket(item,rest),d=CanPlaceBasket(item,checkout);if(!r)return d?checkout:null;if(!d)return rest;var p=item.transform.position;return Vector2.Distance(new Vector2(p.x,p.z),new Vector2(rest.position.x,rest.position.z))<Vector2.Distance(new Vector2(p.x,p.z),new Vector2(checkout.position.x,checkout.position.z))?rest:checkout;}
 void ArmBasket(GrabbableItem item){var target=NearestPlacement(item);if(target){armedPlacement[item]=target;armedAt[item]=Time.unscaledTime;}else if(armedAt.TryGetValue(item,out var time)&&Time.unscaledTime-time>.65f){armedPlacement.Remove(item);armedAt.Remove(item);}}
 void QueueBasketRelease(GrabbableItem item){if(!begun||completed||confirmed)return;var target=NearestPlacement(item);if(!target&&armedPlacement.TryGetValue(item,out var armed)&&armedAt.TryGetValue(item,out var time)&&Time.unscaledTime-time<.85f)target=armed;if(target)releasedPlacements[item]=target;}
 void ProcessBasketReleases(){if(placing)return;foreach(var pair in releasedPlacements.ToArray()){var item=pair.Key;if(item.IsHeld)continue;releasedPlacements.Remove(item);armedPlacement.Remove(item);armedAt.Remove(item);if(item!=basket){if(selected.Count!=0||basket.IsHeld){status="Return the active basket before choosing the other one.";continue;}if(basketReady)basket.ResetItem();basket=item;basketScale=basketScales[item];foreach(var slot in slots)slot.SetParent(item.transform,false);basketReady=false;}if(pair.Value==checkout&&selected.Count!=4)continue;PlaceBasket(pair.Value,pair.Value==checkout);break;}}
 void ChoosePackingMenuAnchor(){
  var forward=Vector3.ProjectOnPlane(workspaceForward,Vector3.up).normalized;
  if(forward.sqrMagnitude<.01f)forward=Vector3.forward;
  if(panelPosition.sqrMagnitude>.01f){packingMenuAnchor=panelPosition;return;}
  var side=Vector3.Cross(Vector3.up,forward);
  packingMenuAnchor=rest.position-side*1.2f+forward*1.2f;
  packingMenuAnchor.y=(worktopY>0?worktopY:rest.position.y)+.5f;
 }
 void MovePanelToPackingSide(){if(!panel)return;panel.transform.SetPositionAndRotation(packingMenuAnchor,panelRotation);RefreshControls();}
 void MovePanelToReader(){RefreshControls();}
 IEnumerator CardFeedback(){var checkoutDisplay=GetComponent<SweetCheckoutDisplay>();if(checkoutDisplay)checkoutDisplay.RefreshStatus();yield break;}
 void PlaceBasket(Transform target,bool atCheckout){if(!placing)StartCoroutine(SettleBasket(target,atCheckout));}
 IEnumerator SettleBasket(Transform target,bool atCheckout){placing=true;var item=basket;item.transform.SetParent(transform,true);var body=item.GetComponent<Rigidbody>();if(body){if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}body.isKinematic=true;}var start=item.transform.position;var rotation=item.transform.rotation;float t=0;while(t<1&&!item.IsHeld){t+=Time.deltaTime*3;item.transform.SetPositionAndRotation(Vector3.Lerp(start,target.position,Mathf.SmoothStep(0,1,t)),Quaternion.Slerp(rotation,target.rotation,Mathf.SmoothStep(0,1,t)));yield return null;}placing=false;if(item.IsHeld)yield break;item.SnapTo(target);if(atCheckout){docked=true;Credit("checkout");status="4/4 sweets checked. Tap the festival card on the outlined reader.";MovePanelToReader();}else{basketReady=true;docked=authorized=false;Credit("basket-rest");status=selected.Count==4?"Basket ready. Select Checkout on the small monitor.":"Basket safely resting — "+selected.Count+"/4 sweets. Release sweets over the basket. Grip a floating sweet to swap it.";MovePanelToPackingSide();}}
 public override void Tick(){
  RefreshControls();
  var heldBasket=baskets.FirstOrDefault(b=>b&&b.IsHeld);var preview=heldBasket?NearestPlacement(heldBasket):null;
  bool restClose=heldBasket&&preview==rest&&CanPlaceBasket(heldBasket,rest);
  bool dockClose=heldBasket&&preview==checkout&&CanPlaceBasket(heldBasket,checkout);
  bool sharedRecess=(rest.position-checkout.position).sqrMagnitude<.01f;
  if(restRing){restRing.enabled=true;var label=restRing.GetComponentInChildren<TextMesh>(true);if(label)label.gameObject.SetActive(!basketReady||heldBasket);}
  if(dockRing){dockRing.enabled=!sharedRecess;var label=dockRing.GetComponentInChildren<TextMesh>(true);if(label)label.gameObject.SetActive(dockRing.enabled&&(!docked||heldBasket));}
  if(restGhost){restGhost.Show(heldBasket?heldBasket:basket,rest,begun&&!completed&&heldBasket&&(selected.Count<4||preview!=checkout),restClose);dockGhost.Show(heldBasket?heldBasket:basket,checkout,begun&&!completed&&heldBasket&&selected.Count==4&&(preview==null||preview==checkout),dockClose);}
  if(restRing){restRing.startColor=restRing.endColor=restClose?new Color(.3f,1,.55f):new Color(1,.72f,.28f);restRing.widthMultiplier=restClose?.028f:.018f;}
  if(dockRing){dockRing.startColor=dockRing.endColor=dockClose?new Color(.3f,1,.55f):new Color(1,.72f,.28f);dockRing.widthMultiplier=dockClose?.028f:.018f;}
  for(int i=0;i<glass.Length;i++)glass[i].localPosition=Vector3.Lerp(glass[i].localPosition,(cabinetOpen?(otherSide?alternateGlass[i]:openGlass[i]):closedGlass[i]),Time.deltaTime*4);
  if(!begun||completed)return;
  foreach(var candidate in baskets){if(candidate.IsHeld){ArmBasket(candidate);heldBaskets.Add(candidate);}else if(heldBaskets.Remove(candidate))QueueBasketRelease(candidate);}
  ProcessBasketReleases();
  if(basket.IsHeld&&docked){docked=false;authorized=false;}
  GrabbableItem heldSweet=null;
  foreach(var sweet in sweets){
   if(!sweet)continue;
   if(sweet.IsHeld){heldSweet=sweet;heldLast.Add(sweet);if(selected.Contains(sweet)&&!confirmed)RemoveSweet(sweet);if(PointingAtShelf(sweet))shelfAimAt[sweet]=Time.unscaledTime;else shelfAimAt.Remove(sweet);}
   else if(heldLast.Remove(sweet))releasedSweets.Add(sweet);
  }
  foreach(var sweet in releasedSweets.ToArray()){
   if(sweet.IsHeld)continue;
   releasedSweets.Remove(sweet);
   if(ShelfReturnArmed(sweet)&&ReturnSweet(sweet))continue;
   var bowl=NativeBounds(basket,basket.transform);var p=basket.transform.InverseTransformPoint(sweet.transform.position);
   bool inBowl=DiyaRestTarget.Contains(sweet,basket.transform,.8f)||Mathf.Abs(p.x-bowl.center.x)<bowl.extents.x+.2f&&Mathf.Abs(p.z-bowl.center.z)<bowl.extents.z+.2f&&p.y>bowl.min.y-.15f&&p.y<bowl.max.y+.3f;
   if(inBowl&&AddSweet(sweet))continue;
   if(CanReturnSweet(sweet))ReturnSweet(sweet);
  }
  ShowShelfReturn(heldSweet);
  if(!card.IsHeld||Vector3.Distance(card.transform.position,cardReader.position)>.4f)cardMustLeaveReader=false;
  if(docked&&card.IsHeld&&Vector3.Distance(card.transform.position,cardReader.position)<.28f)AuthorizeCard();
  basketWasHeld=basket.IsHeld;
  
 }
 public Transform ShelfReturnTarget(GrabbableItem sweet)=>sweet&&shelfTargets.TryGetValue(sweet,out var target)?target:null;
 bool PointingAtShelf(GrabbableItem sweet){
  var target=ShelfReturnTarget(sweet);if(!target||sweet.grab.interactorsSelecting.Count==0)return false;
  var hand=sweet.grab.interactorsSelecting[0].transform;var delta=target.position-hand.position;
  return delta.magnitude<3&&Vector3.Angle(hand.forward,delta)<7.5f;
 }
 bool ShelfReturnArmed(GrabbableItem sweet)=>shelfAimAt.TryGetValue(sweet,out var time)&&Time.unscaledTime-time<.35f;
 public bool CanReturnSweet(GrabbableItem sweet){
  var target=ShelfReturnTarget(sweet);if(!target||confirmed)return false;
  var delta=sweet.transform.position-target.position;
  return ShelfReturnArmed(sweet)||new Vector2(delta.x,delta.z).sqrMagnitude<.9f*.9f&&Mathf.Abs(delta.y)<.65f;
 }
 public bool ReturnSweet(GrabbableItem sweet){
  var target=ShelfReturnTarget(sweet);if(!begun||confirmed||!target||sweet.IsHeld)return false;
  RemoveSweet(sweet);RestoreSweetColliders(sweet);
  sweet.transform.SetParent(shelfParents[sweet],true);sweet.transform.localScale=sweetScales[sweet];sweet.transform.SetPositionAndRotation(target.position,target.rotation);
  var body=sweet.GetComponent<Rigidbody>();if(body){if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}body.isKinematic=true;body.position=target.position;body.rotation=target.rotation;}
  releasedSweets.Remove(sweet);heldLast.Remove(sweet);shelfAimAt.Remove(sweet);
  status=sweet.itemId+" returned to its shelf. "+selected.Count+"/4 in the basket.";
  if(selected.Count==4)status+=" Select Checkout on the small monitor.";
  return true;
 }
 void ShowShelfReturn(GrabbableItem sweet){
  var target=ShelfReturnTarget(sweet);bool visible=begun&&!completed&&sweet&&target;
  if(shelfGhost)shelfGhost.Show(sweet,target,visible,visible&&CanReturnSweet(sweet),sweet&&sweetScales.TryGetValue(sweet,out var scale)?scale:(Vector3?)null);
  if(!shelfHint)return;shelfHint.gameObject.SetActive(visible);if(!visible)return;
  bool close=CanReturnSweet(sweet);shelfHint.transform.position=target.position+Vector3.up*.18f;shelfHint.transform.rotation=panelRotation;shelfHint.text=close?"RELEASE TO RETURN "+sweet.itemId.ToUpperInvariant():"RETURN "+sweet.itemId.ToUpperInvariant()+" HERE";shelfHint.color=close?new Color(.35f,1,.65f):new Color(1,.8f,.4f);
 }
 void RefreshControls(){
  if(!panel||!beginControl)return;
  bool visible=!docked&&!authorized&&!confirmed;
  if(!visible)panel.CloseHelp();
  if(panel.gameObject.activeSelf!=visible)panel.gameObject.SetActive(visible);
  var labels=panel.GetComponentsInChildren<UnityEngine.UI.Text>(true).Where(t=>t.transform.parent==panel.transform).ToArray();if(labels.Length>2)labels[2].gameObject.SetActive(!begun||completed);
  beginControl.gameObject.SetActive(!begun);openControl.gameObject.SetActive(begun&&!cabinetOpen);switchControl.gameObject.SetActive(begun&&cabinetOpen&&!confirmed&&!completed);
 }
 public static Pose ComfortableBasketPose(Vector3 handPosition,Vector3 handForward,Transform head,float radius,float topOffset=.30f){
  var away=head?Vector3.ProjectOnPlane(handPosition-head.position,Vector3.up):Vector3.ProjectOnPlane(handForward,Vector3.up);
  if(away.sqrMagnitude<.01f&&head)away=Vector3.ProjectOnPlane(head.forward,Vector3.up);
  if(away.sqrMagnitude<.01f)away=Vector3.forward;
  away.Normalize();
  float distance=Mathf.Max(.65f,radius+.36f);
  var position=handPosition+away*distance+Vector3.down*Mathf.Max(.34f,topOffset+.08f);
  if(head){
   var horizontal=Vector3.ProjectOnPlane(position-head.position,Vector3.up);
   float clearance=radius+.55f;
   if(horizontal.magnitude<clearance)position+=away*(clearance-horizontal.magnitude);
   position.y=Mathf.Min(position.y,head.position.y-.32f-topOffset);
  }
  return new Pose(position,Quaternion.LookRotation(away,Vector3.up));
 }
 void OnEnable(){Application.onBeforeRender+=ApplyBasketCarry;}
 void OnDisable(){Application.onBeforeRender-=ApplyBasketCarry;}
 void LateUpdate(){ApplyBasketCarry();}
 [BeforeRenderOrder(10000)]
 void ApplyBasketCarry(){
  if(!playerHead){var camera=Camera.main;if(camera)playerHead=camera.transform;}
  foreach(var pair in basketScales)if(pair.Key){
   var item=pair.Key;item.transform.localScale=pair.Value;
   if(!item.IsHeld||item.grab.interactorsSelecting.Count==0)continue;
   var interactor=item.grab.interactorsSelecting[0];
   var hand=interactor.transform;
   if(!hand)continue;
   if(!playerHead){var camera=Camera.main;if(camera)playerHead=camera.transform;}
   var bounds=NativeBounds(item,item.transform);
   if(item==basket)foreach(var sweet in selected)if(sweet&&!sweet.IsHeld)bounds.Encapsulate(NativeBounds(sweet,item.transform));
   float x=bounds.extents.x*Mathf.Abs(item.transform.lossyScale.x),z=bounds.extents.z*Mathf.Abs(item.transform.lossyScale.z);
   float radius=Mathf.Sqrt(x*x+z*z);
   float topOffset=bounds.max.y*Mathf.Abs(item.transform.lossyScale.y);
   item.grab.trackPosition=false;item.grab.trackRotation=false;item.grab.trackScale=false;
   var pose=ComfortableBasketPose(hand.position,hand.forward,playerHead,radius,topOffset);
   item.transform.SetPositionAndRotation(pose.position,pose.rotation);
   var body=item.GetComponent<Rigidbody>();if(body&&body.isKinematic){body.position=pose.position;body.rotation=pose.rotation;}
  }
 }
 public override void ResetGame(){StopAllCoroutines();foreach(var label in GetComponentsInChildren<TextMesh>(true))if(label.name=="CARD ACCEPTED")Destroy(label.gameObject);ChoosePackingMenuAnchor();MovePanelToPackingSide();placing=false;cardMustLeaveReader=false;shelfAimAt.Clear();releasedSweets.Clear();ShowShelfReturn(null);armedPlacement.Clear();armedAt.Clear();releasedPlacements.Clear();base.ResetGame();heldLast.Clear();heldBaskets.Clear();basketWasHeld=false;selected.Clear();otherSide=false;cabinetOpen=basketReady=docked=authorized=confirmed=false;if(baskets!=null)foreach(var b in baskets)if(b)b.ResetItem();basket=defaultBasket?defaultBasket:basket;if(basket){basketScale=basket.transform.localScale;foreach(var slot in slots)slot.SetParent(basket.transform,false);}if(card)card.ResetItem();if(sweets!=null)foreach(var s in sweets)if(s){RestoreSweetColliders(s);s.ResetItem();if(sweetScales.TryGetValue(s,out var scale))s.transform.localScale=scale;}if(glass!=null&&closedGlass!=null)for(int i=0;i<glass.Length;i++)glass[i].localPosition=closedGlass[i];RefreshControls();}
}
}
