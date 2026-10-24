using UnityEngine;
using UnityEngine.UI;

namespace DiwaliGameplay {
public sealed class SweetCheckoutDisplay : MonoBehaviour {
 public Transform screen;
 public Vector3 localCenter, localNormal, localUp;
 public Vector2 localSize;
 public bool configured;
 SweetGame owner;
 Text heading, detail;
 Image background;
 GameObject canvasObject;
 int displayedState = -1;

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
  var canvas = go.GetComponent<Canvas>();
  canvas.renderMode = RenderMode.WorldSpace;
  canvas.sortingOrder = 1;
  background = go.AddComponent<Image>();
  background.raycastTarget = false;
  heading = Label(rect, "Status", new Vector2(0, 22), new Vector2(280, 100), 30, FontStyle.Bold);
  detail = Label(rect, "Instruction", new Vector2(0, -53), new Vector2(270, 40), 20, FontStyle.Normal);
  RefreshStatus();
  return true;
 }

 void Update() { RefreshStatus(); }
 void OnDestroy() { if (canvasObject) Destroy(canvasObject); }

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
  int state = owner.confirmed || owner.completed ? 3 : owner.authorized ? 2 : owner.docked && owner.selected.Count == 4 ? 1 : 0;
  if (state == displayedState) return;
  displayedState = state;
  bool accepted = state >= 2;
  background.color = accepted ? new Color(.025f, .13f, .075f) : new Color(.025f, .045f, .055f);
  heading.color = accepted ? new Color(.5f, 1, .68f) : new Color(1, .9f, .62f);
  detail.color = new Color(.88f, .95f, .91f);
  heading.text = state == 3 ? "GIFT READY" : state == 2 ? "CARD\nACCEPTED" : state == 1 ? "TAP CARD" : "CHOOSE\n4 SWEETS";
  detail.text = state == 3 ? "Happy Diwali!" : state == 2 ? "Select Confirm Gift" : state == 1 ? "Touch the reader" : "Fill your gift basket";
 }

 }
}
