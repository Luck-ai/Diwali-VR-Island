using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace DiwaliGameplay {
public class VenuePanel : MonoBehaviour {
    static readonly Color Ink = new Color(.11f, .065f, .12f, 1);
    static readonly Color Cream = new Color(1, .94f, .82f, 1);
    static readonly Color Gold = new Color(1, .77f, .37f, 1);
    static readonly Color Clay = new Color(.59f, .22f, .14f, 1);
    static readonly Color Paper = new Color(1, .965f, .885f, 1);
    static readonly Color Muted = new Color(.45f, .29f, .12f, 1);
    static Sprite rounded, keyline;
    public bool autoFacePlayer=true;
    const float Width = 720;
    VenueGame owner;
    Text statusText, stageText, journeyText;
    RectTransform content;
    Transform restart, helpButton, recenterButton, backing, footer;
    Canvas mainCanvas, helpCanvas;
    TrackedDeviceGraphicRaycaster mainRaycaster;
    RectTransform helpContent;
    Transform helpBacking;
    Text helpBody;
    string title, instructions;
    int fittedChildren = -1;
    float physicalWidth = .94f, maximumHeight = 1.28f;
    bool showingHelp;

    public static VenuePanel Create(VenueGame owner, Vector3 position, Quaternion rotation, string title) {
        var go = Shell(owner.transform, title + " controls", position, rotation, out var canvas, out var rect, out var back);
        var panel = go.AddComponent<VenuePanel>();
        panel.owner = owner;
        panel.title = title;
        panel.content = rect;
        panel.mainCanvas = canvas;
        panel.mainRaycaster = go.GetComponent<TrackedDeviceGraphicRaycaster>();
        panel.backing = back;
        panel.instructions = string.IsNullOrWhiteSpace(owner.howToPlay)
            ? "Point a controller ray at a button and select it.\n\nGrab a highlighted object, carry it to its matching marker, then release.\n\nFollow the current task shown on the main panel. Restart returns this activity to its beginning."
            : owner.howToPlay;
        panel.Label(title, 36, 76, Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        panel.stageText = panel.Label("DIWALI ACTIVITY", 19, 26, Muted, TextAnchor.MiddleLeft);
        panel.journeyText = panel.Label(FestivalJourney.Purpose(owner)+"\n"+FestivalJourney.Summary, 20, 72, Muted, TextAnchor.MiddleLeft);
        panel.statusText = panel.Objective(owner.status);
        var footer = new GameObject("Activity tools", typeof(RectTransform));
        footer.transform.SetParent(rect, false);
        footer.AddComponent<LayoutElement>().preferredHeight = 62;
        var row = footer.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 10; row.childControlWidth = true; row.childForceExpandWidth = true;
        row.childControlHeight = true; row.childForceExpandHeight = true;
        panel.footer = footer.transform;
        panel.helpButton = MakeButton(footer.transform, "How To Play", ()=>{panel.SetHelp(owner.howToPlay);panel.ShowHelp();}).transform;
        panel.recenterButton = MakeButton(footer.transform, "Move menu here", panel.MoveMenuHere).transform;
        panel.restart = MakeButton(footer.transform, "Restart", owner.ResetGame).transform;
        panel.AddButton("Festival progress / next venue", panel.ShowJourney);
        foreach(var button in footer.GetComponentsInChildren<Button>())button.GetComponentInChildren<Text>().fontSize = 21;
        return panel;
    }

    static GameObject Shell(Transform parent, string name, Vector3 position, Quaternion rotation,
        out Canvas canvas, out RectTransform rect, out Transform back) {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, rotation);
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var raycaster = go.AddComponent<TrackedDeviceGraphicRaycaster>();
        raycaster.ignoreReversedGraphics = true;
        rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(Width, 600);
        var surface=go.AddComponent<Image>();surface.color=Paper;surface.sprite=Rounded();surface.type=Image.Type.Sliced;
        var frame = go.AddComponent<Outline>();
        frame.effectColor = new Color(.64f, .43f, .17f, 1);
        frame.effectDistance = new Vector2(3, -3);
        AddFrameDecoration(rect);
        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 22, 22);
        layout.spacing = 10;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        var plate = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plate.name = "Opaque menu back";
        Object.Destroy(plate.GetComponent<Collider>());
        plate.transform.SetParent(go.transform, false);
        plate.transform.localPosition = new Vector3(0, 0, 3);
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader) shader = Shader.Find("Unlit/Color");
        var material = new Material(shader) { name = "Diwali menu backing" };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Ink);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Ink);
        if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0);
        plate.GetComponent<MeshRenderer>().material = material;
        back = plate.transform;
        return go;
    }

    static Sprite Rounded(bool outline=false) {
        if(outline&&keyline)return keyline;
        if(!outline&&rounded)return rounded;
        const int size=64; const float radius=10;
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
        texture.name="Diwali rounded UI surface";
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++){
            float qx=Mathf.Abs(x-31.5f)-21.5f, qy=Mathf.Abs(y-31.5f)-21.5f;
            float signed=Mathf.Sqrt(Mathf.Max(qx,0)*Mathf.Max(qx,0)+Mathf.Max(qy,0)*Mathf.Max(qy,0))+Mathf.Min(Mathf.Max(qx,qy),0);
            float alpha=Mathf.Clamp01(radius-signed+.5f);
            if(outline)alpha-=Mathf.Clamp01(radius-1-signed+.5f);
            pixels[y*size+x]=new Color(1,1,1,alpha);
        }
        texture.SetPixels(pixels);texture.Apply();
        var sprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(12,12,12,12));
        if(outline)keyline=sprite;else rounded=sprite;return sprite;
    }
    static void AddFrameDecoration(RectTransform rect){
        var inner=new GameObject("Inner gold keyline",typeof(RectTransform));inner.transform.SetParent(rect,false);
        var r=inner.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(12,12);r.offsetMax=new Vector2(-12,-12);
        inner.AddComponent<LayoutElement>().ignoreLayout=true;
        var line=inner.AddComponent<Image>();line.sprite=Rounded(true);line.type=Image.Type.Sliced;line.color=new Color(.78f,.61f,.32f,.7f);line.raycastTarget=false;
        foreach(var corner in new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,0),new Vector2(1,1)}){
            var diamond=new GameObject("Festival corner",typeof(RectTransform));diamond.transform.SetParent(rect,false);
            var d=diamond.GetComponent<RectTransform>();d.anchorMin=d.anchorMax=corner;d.sizeDelta=new Vector2(7,7);d.anchoredPosition=new Vector2(corner.x==0?16:-16,corner.y==0?16:-16);d.localRotation=Quaternion.Euler(0,0,45);
            diamond.AddComponent<LayoutElement>().ignoreLayout=true;var image=diamond.AddComponent<Image>();image.color=new Color(.67f,.43f,.14f);image.raycastTarget=false;
        }
    }
    Text Objective(string value){
        var go=new GameObject("Current objective",typeof(RectTransform));go.transform.SetParent(content,false);go.AddComponent<LayoutElement>().preferredHeight=126;
        var image=go.AddComponent<Image>();image.sprite=Rounded();image.type=Image.Type.Sliced;image.color=new Color(.95f,.905f,.81f);
        var caption=MakeText(go.transform,"CURRENT TASK",17,0,Muted,TextAnchor.UpperLeft);caption.rectTransform.anchorMin=new Vector2(0,1);caption.rectTransform.anchorMax=Vector2.one;caption.rectTransform.pivot=new Vector2(.5f,1);caption.rectTransform.offsetMin=new Vector2(17,-34);caption.rectTransform.offsetMax=new Vector2(-17,-10);Object.Destroy(caption.GetComponent<LayoutElement>());
        var body=MakeText(go.transform,value,26,0,Ink,TextAnchor.MiddleLeft);body.rectTransform.anchorMin=Vector2.zero;body.rectTransform.anchorMax=Vector2.one;body.rectTransform.offsetMin=new Vector2(17,8);body.rectTransform.offsetMax=new Vector2(-17,-35);body.verticalOverflow=VerticalWrapMode.Truncate;Object.Destroy(body.GetComponent<LayoutElement>());return body;
    }
    Text Label(string value, int fontSize, float height, Color color, TextAnchor alignment) {
        return MakeText(content, value, fontSize, height, color, alignment);
    }
    static Text MakeText(Transform parent, string value, int fontSize, float height, Color color, TextAnchor alignment) {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = Resources.Load<Font>("Diwali/Inter");
        if(!text.font)text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.lineSpacing = 1.12f;
        go.AddComponent<LayoutElement>().preferredHeight = height;
        return text;
    }
    public Button AddButton(string label, UnityAction action) {
        return MakeButton(content, label, action);
    }
    static Button MakeButton(Transform parent, string label, UnityAction action) {
        var go = new GameObject(label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredHeight = 62;
        bool primary = label.StartsWith("Begin") || label.StartsWith("Make a") || label == "Celebrate" || label == "Finale";
        var image = go.AddComponent<Image>();
        image.sprite = Rounded(); image.type = Image.Type.Sliced;
        image.color = primary ? Clay : new Color(.94f,.885f,.765f,1);
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1);
        colors.pressedColor = new Color(.72f, .72f, .72f, 1);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(.45f, .45f, .45f, 1);
        button.colors = colors;
        button.onClick.AddListener(action);
        var text = MakeText(go.transform, label, 27, 0, primary ? Cream : Ink, TextAnchor.MiddleCenter);
        var rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(16, 2);
        rect.offsetMax = new Vector2(-16, -2);
        Object.Destroy(text.GetComponent<LayoutElement>());
        return button;
    }
    public Slider AddSlider(string label, float value, UnityAction<float> action) {
        var group=new GameObject(label+" control",typeof(RectTransform));group.transform.SetParent(content,false);group.AddComponent<LayoutElement>().preferredHeight=92;
        var layout=group.AddComponent<VerticalLayoutGroup>();layout.spacing=6;layout.childControlHeight=true;layout.childForceExpandHeight=false;
        MakeText(group.transform,label,25,34,Ink,TextAnchor.MiddleLeft);
        var go = new GameObject(label + " slider", typeof(RectTransform));
        go.transform.SetParent(group.transform, false);
        go.AddComponent<LayoutElement>().preferredHeight = 52;
        go.AddComponent<Image>().color = new Color(.85f, .77f, .61f);
        var slider = go.AddComponent<Slider>();
        slider.minValue = .25f;
        slider.maxValue = 1;
        slider.value = value;
        var area = new GameObject("Handle area", typeof(RectTransform)).GetComponent<RectTransform>();
        area.SetParent(go.transform, false);
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(24, 0);
        area.offsetMax = new Vector2(-24, 0);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(area, false);
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(42, 52);
        handle.GetComponent<Image>().color = Ink;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.onValueChanged.AddListener(action);
        return slider;
    }
    public void SetControlVisible(Slider control,bool visible){if(control)control.transform.parent.gameObject.SetActive(visible);}
    public void SetHelp(string value) {
        instructions = value;
        if (helpBody) helpBody.text = value;
    }
    public void SetPhysicalSize(float widthMeters, float maxHeightMeters = 1.2f) {
        physicalWidth = Mathf.Clamp(widthMeters, .35f, 1.5f);
        maximumHeight = Mathf.Clamp(maxHeightMeters, .45f, 1.6f);
        fittedChildren = -1;
    }
    public void FollowTaskAt(Vector3 panelPosition, Vector3 viewerPosition) {
        transform.position=panelPosition;FaceToward(viewerPosition);
    }
    public void FaceToward(Vector3 viewerPoint) {
        var direction = transform.position - viewerPoint;
        direction.y = 0;
        if (direction.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        PositionHelp();
    }
    public void MoveMenuHere() {
        var camera = Camera.main;
        if (!camera) return;
        var forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
        transform.position = camera.transform.position + forward * 1.35f - Vector3.up * .15f;
        FaceToward(camera.transform.position);
    }
    public void ShowJourney() {
        SetHelp("Prepare the festival together.\n\nRangoli welcomes guests. Sweets are gifts to share. Diya brings the flame. Routing lights the lanterns. Fireworks opens the celebration.\n\nComplete the first four in any order, then select Play Finale at Fireworks.\n\n"+FestivalJourney.Summary+"\n\n"+FestivalJourney.Next);
        ShowHelp();
    }
    public void ShowHelp() {
        if (!helpCanvas) {
            Shell(owner.transform, title + " help", transform.position, transform.rotation,
                out helpCanvas, out helpContent, out helpBacking);
            MakeText(helpContent, "How to play", 36, 70, Ink, TextAnchor.MiddleLeft);
            helpBody = MakeText(helpContent, instructions, 27, 600, Ink, TextAnchor.UpperLeft);
            MakeButton(helpContent, "Close", CloseHelp);
            LayoutRebuilder.ForceRebuildLayoutImmediate(helpContent);
            helpBody.GetComponent<LayoutElement>().preferredHeight = Mathf.Clamp(helpBody.preferredHeight + 24, 340, 1020);
        }
        showingHelp = true;
        helpCanvas.gameObject.SetActive(true);
        mainCanvas.enabled = false;
        mainRaycaster.enabled = false;
        PositionHelp();
        Fit(helpContent, helpBacking, Mathf.Max(.9f, physicalWidth), 1.45f);
    }
    public void CloseHelp() {
        showingHelp = false;
        if (helpCanvas) helpCanvas.gameObject.SetActive(false);
        mainCanvas.enabled = true;
        mainRaycaster.enabled = true;
    }
    void PositionHelp() {
        if (!helpCanvas) return;
        helpCanvas.transform.SetPositionAndRotation(transform.position - transform.forward * .015f, transform.rotation);
    }
    public void Refresh() { if(journeyText)journeyText.text=FestivalJourney.Purpose(owner)+"\n"+FestivalJourney.Summary;if (statusText) statusText.text = owner.status+(owner.completed?"\n"+FestivalJourney.Next:""); if(stageText){int total=owner.GetType().Name=="FireworksGame"?11:10;stageText.text=owner.completed?"FESTIVAL DISPLAY COMPLETE":owner.begun?"PROGRESS   "+owner.credited.Count+" / "+total:"DIWALI ACTIVITY  |  READY TO BEGIN";} }
    static void Fit(RectTransform rect, Transform back, float widthMeters, float maxHeightMeters) {
        float height = 44;
        int count = 0;
        for (int i = 0; i < rect.childCount; i++) {
            var element = rect.GetChild(i).GetComponent<LayoutElement>();
            if (!element || element.ignoreLayout || !element.gameObject.activeSelf) continue;
            height += element.preferredHeight;
            count++;
        }
        height += 10 * Mathf.Max(0, count - 1);
        rect.sizeDelta = new Vector2(Width, height);
        rect.localScale = Vector3.one * Mathf.Min(widthMeters / Width, maxHeightMeters / height);
        back.localScale = new Vector3(Width + 8, height + 8, 1);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }
    void LateUpdate() {
        int signature=content.childCount;
        for(int i=0;i<content.childCount;i++)if(content.GetChild(i).gameObject.activeSelf)signature=unchecked(signature*31+i+1);
        if (signature != fittedChildren) {
            footer.SetAsLastSibling();
            Fit(content, backing, physicalWidth, maximumHeight);
            fittedChildren = signature;
        }
        var camera=Camera.main;
        if(autoFacePlayer&&camera){var toward=Vector3.ProjectOnPlane(camera.transform.position-transform.position,Vector3.up);if(toward.sqrMagnitude<16&&toward.sqrMagnitude>.25f&&Vector3.Dot(-transform.forward,toward.normalized)<.5f)FaceToward(camera.transform.position);}
        if (showingHelp) PositionHelp();
    }
    void Update() { Refresh(); }
}
}
