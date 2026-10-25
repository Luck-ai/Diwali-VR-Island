using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace DiwaliGameplay {
public sealed class SweetCheckoutDisplay : MonoBehaviour {
 public Transform screen;
 public Vector3 localCenter, localNormal, localUp;
 public Vector2 localSize;
 public bool configured;
 SweetGame owner;
 Text heading, detail, actionLabel;
 Image background;
 GameObject canvasObject;
 Button actionButton;
 Canvas displayCanvas;
 int displayedState = -1;
 public Button ActionButton => actionButton;
 public Canvas DisplayCanvas => displayCanvas;

 public bool Create(SweetGame game) {
  owner = game;
  if (canvasObject) return true;
  if (!configured || !screen || localSize.x <= 0 || localSize.y <= 0 || localNormal.sqrMagnitude < .1f) return false;
  var outward = screen.TransformDirection(localNormal).normalized;
  var up = Vector3.ProjectOnPlane(screen.TransformDirection(localUp), outward).normalized;
  if (up.sqrMagnitude < .1f) return false;
  var localRight = Vector3.Cross(localNormal, localUp).normalized;
  var size = new Vector2(screen.TransformVector(localRight * localSize.x).magnitude,
   screen.TransformVector(localUp * localSize.y).magnitude);
  var go = new GameObject("Checkout monitor status", typeof(RectTransform), typeof(Canvas));
  canvasObject = go;
  var rect = go.GetComponent<RectTransform>();
  rect.sizeDelta = new Vector2(300, 180);
  rect.SetPositionAndRotation(screen.TransformPoint(localCenter) + outward * .003f, Quaternion.LookRotation(-outward, up));
  rect.localScale = Vector3.one * Mathf.Min(size.x / 300, size.y / 180) * .88f;
  rect.SetParent(screen, true);
  displayCanvas = go.GetComponent<Canvas>();
  displayCanvas.renderMode = RenderMode.WorldSpace;
  displayCanvas.worldCamera = Camera.main;
  displayCanvas.sortingOrder = 1;
  var raycaster = go.AddComponent<TrackedDeviceGraphicRaycaster>();
  raycaster.ignoreReversedGraphics = true;
  raycaster.checkFor3DOcclusion = true;
  background = go.AddComponent<Image>();
  background.raycastTarget = false;
  heading = Label(rect, "Status", new Vector2(0, 48), new Vector2(280, 70), 26, FontStyle.Bold);
  detail = Label(rect, "Instruction", new Vector2(0, -7), new Vector2(280, 42), 16, FontStyle.Normal);
  var buttonObject = new GameObject("Share Gift", typeof(RectTransform), typeof(Image), typeof(Button));
  var buttonRect = buttonObject.GetComponent<RectTransform>();
  buttonRect.SetParent(rect, false);
  buttonRect.sizeDelta = new Vector2(280, 56);
  buttonRect.anchoredPosition = new Vector2(0, -56);
  var buttonImage = buttonObject.GetComponent<Image>();
  buttonImage.color = new Color(.5f, 1, .68f);
  actionButton = buttonObject.GetComponent<Button>();
  actionButton.targetGraphic = buttonImage;
  var colors = actionButton.colors;
  colors.highlightedColor = new Color(1, 1, .78f);
  colors.selectedColor = colors.highlightedColor;
  colors.pressedColor = new Color(.65f, .8f, .65f);
  actionButton.colors = colors;
  actionButton.onClick.AddListener(ActivateAction);
  actionLabel = Label(buttonRect, "Action", Vector2.zero, new Vector2(270, 52), 24, FontStyle.Bold);
  actionLabel.color = new Color(.025f, .13f, .075f);
  RefreshStatus();
  return true;
 }

 void Update() { RefreshStatus(); }
 void OnDestroy() { if (canvasObject) Destroy(canvasObject); }

 void ActivateAction() {
  if (!owner) return;
  if (owner.confirmed || owner.completed) owner.ResetGame();
  else if (owner.authorized) owner.ConfirmGift();
  RefreshStatus();
 }

 static Text Label(Transform parent, string name, Vector2 position, Vector2 size, int fontSize, FontStyle style) {
  var go = new GameObject(name, typeof(RectTransform));
  var rect = go.GetComponent<RectTransform>();
  rect.SetParent(parent, false);
  rect.sizeDelta = size;
  rect.anchoredPosition = position;
  var text = go.AddComponent<Text>();
  text.font = Resources.Load<Font>("Diwali/Inter");
  if (!text.font) text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
  text.fontSize = fontSize;
  text.fontStyle = style;
  text.alignment = TextAnchor.MiddleCenter;
  text.horizontalOverflow = HorizontalWrapMode.Wrap;
  text.verticalOverflow = VerticalWrapMode.Truncate;
  text.raycastTarget = false;
  return text;
 }

 public void RefreshStatus() {
  if (!owner || !heading) return;
  int state = owner.confirmed || owner.completed ? 3 : owner.authorized ? 2 : owner.docked && owner.selected.Count == 4 ? 1 : owner.selected.Count == 4 ? 4 : 0;
  actionButton.gameObject.SetActive(state == 2 || state == 3);
  actionButton.interactable = state == 2 || state == 3;
  if (state == displayedState) return;
  displayedState = state;
  bool accepted = state == 2 || state == 3;
  background.color = accepted ? new Color(.025f, .13f, .075f) : new Color(.025f, .045f, .055f);
  heading.color = accepted ? new Color(.5f, 1, .68f) : new Color(1, .9f, .62f);
  detail.color = new Color(.88f, .95f, .91f);
  heading.text = state == 3 ? "GIFT SHARED" : state == 2 ? "CARD\nACCEPTED" : state == 1 ? "TAP CARD" : state == 4 ? "BASKET READY" : "CHOOSE\n4 SWEETS";
  detail.text = state == 3 ? "See your basket in\nthe central plaza" : state == 2 ? "Share your basket\nwith the central plaza" : state == 1 ? "Touch the reader" : state == 4 ? "Place basket on\nCHECKOUT area" : "Fill your gift basket";
  actionLabel.text = state == 3 ? "New Gift" : "Share Gift";
 }

 }
}
