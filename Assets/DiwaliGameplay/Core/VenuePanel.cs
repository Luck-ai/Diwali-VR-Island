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
    float nextRefresh;
    readonly System.Collections.Generic.List<(Slider slider,Text text,string label)> sliderReadouts=new System.Collections.Generic.List<(Slider,Text,string)>();
    int helpPage;
    string[] helpPages;
    Button previousHelp, nextHelp;
    Text helpPageLabel;

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
        panel.Label(title, 36, 62, Ink, TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        panel.stageText = panel.Label("DIWALI ACTIVITY", 22, 30, Muted, TextAnchor.MiddleLeft);
        panel.journeyText = panel.Label(FestivalJourney.Purpose(owner)+"\n"+FestivalJourney.Summary, 23, 78, Muted, TextAnchor.MiddleLeft);
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
        panel.recenterButton.GetComponentInChildren<Text>().text="Move menu";
        panel.restart = MakeButton(footer.transform, "Restart", owner.ResetGame).transform;
        panel.AddButton("Festival progress", panel.ShowJourney);
        foreach(var button in footer.GetComponentsInChildren<Button>())button.GetComponentInChildren<Text>().fontSize = 25;
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
        raycaster.checkFor3DOcclusion = true;
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

    internal static Sprite Rounded(bool outline=false) {
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
        var go=new GameObject("Current objective",typeof(RectTransform));go.transform.SetParent(content,false);go.AddComponent<LayoutElement>().preferredHeight=154;
        var image=go.AddComponent<Image>();image.sprite=Rounded();image.type=Image.Type.Sliced;image.color=new Color(.95f,.905f,.81f);
        var caption=MakeText(go.transform,"NEXT STEP",21,0,Muted,TextAnchor.UpperLeft);caption.rectTransform.anchorMin=new Vector2(0,1);caption.rectTransform.anchorMax=Vector2.one;caption.rectTransform.pivot=new Vector2(.5f,1);caption.rectTransform.offsetMin=new Vector2(17,-39);caption.rectTransform.offsetMax=new Vector2(-17,-10);Object.Destroy(caption.GetComponent<LayoutElement>());
        var body=MakeText(go.transform,value,29,0,Ink,TextAnchor.MiddleLeft);body.rectTransform.anchorMin=Vector2.zero;body.rectTransform.anchorMax=Vector2.one;body.rectTransform.offsetMin=new Vector2(17,8);body.rectTransform.offsetMax=new Vector2(-17,-43);body.verticalOverflow=VerticalWrapMode.Truncate;Object.Destroy(body.GetComponent<LayoutElement>());return body;
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
        text.verticalOverflow = VerticalWrapMode.Truncate;
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
        bool primary = label.StartsWith("Begin") || label.StartsWith("Make a") || label == "Celebrate" || label == "Finale" || label == "Open Drawer" || label.StartsWith("Confirm") || label == "Test Lights";
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
        var group=new GameObject(label+" control",typeof(RectTransform));group.transform.SetParent(content,false);group.AddComponent<LayoutElement>().preferredHeight=108;
        var layout=group.AddComponent<VerticalLayoutGroup>();layout.spacing=6;layout.childControlHeight=true;layout.childForceExpandHeight=false;
        var valueLabel=MakeText(group.transform,label,27,40,Ink,TextAnchor.MiddleLeft);
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
        sliderReadouts.Add((slider,valueLabel,label));
        slider.onValueChanged.AddListener(action);
        slider.onValueChanged.AddListener(v=>valueLabel.text=label+"   "+Mathf.RoundToInt(slider.normalizedValue*100)+"%");
        return slider;
    }
    public void SetControlVisible(Slider control,bool visible){if(control)control.transform.parent.gameObject.SetActive(visible);}
    public void SetHelp(string value) {
        instructions = value;
        helpPages = PaginateHelp(value);
        helpPage = 0;
        UpdateHelpPage();
    }
    public void SetPhysicalSize(float widthMeters, float maxHeightMeters = 1.2f) {
        physicalWidth = Mathf.Clamp(widthMeters, .68f, 1.5f);
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
        var side = Vector3.Cross(Vector3.up, forward);
        transform.position = camera.transform.position + forward * 1.15f + side * .65f - Vector3.up * .18f;
        FaceToward(camera.transform.position);
    }
    public void ShowJourney() {
        SetHelp("Prepare the festival together.\n\nRangoli welcomes guests. Sweets are gifts to share. Diya brings the flame. Routing lights the lanterns. Fireworks opens the celebration.\n\nVisit the venues in any order. Fireworks and its Finale are available without completing the other games.\n\n"+FestivalJourney.Summary+"\n\n"+FestivalJourney.Next);
        ShowHelp();
    }
    public void ShowHelp() {
        if (!helpCanvas) {
            Shell(owner.transform, title + " help", transform.position, transform.rotation,
                out helpCanvas, out helpContent, out helpBacking);
            MakeText(helpContent, "How to play", 36, 70, Ink, TextAnchor.MiddleLeft);
            helpBody = MakeText(helpContent, instructions, 29, 500, Ink, TextAnchor.UpperLeft);
            var paging = new GameObject("Help pages", typeof(RectTransform));
            paging.transform.SetParent(helpContent, false);
            paging.AddComponent<LayoutElement>().preferredHeight=64;
            var row=paging.AddComponent<HorizontalLayoutGroup>();row.spacing=12;row.childControlWidth=true;row.childForceExpandWidth=true;
            previousHelp=MakeButton(paging.transform,"Previous",()=>{helpPage--;UpdateHelpPage();});
            helpPageLabel=MakeText(paging.transform,"",25,62,Muted,TextAnchor.MiddleCenter);
            nextHelp=MakeButton(paging.transform,"Next",()=>{helpPage++;UpdateHelpPage();});
            MakeButton(helpContent, "Close", CloseHelp);
            LayoutRebuilder.ForceRebuildLayoutImmediate(helpContent);
            helpPages=PaginateHelp(instructions);
            UpdateHelpPage();
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
    static string[] PaginateHelp(string value) {
        var pages=new System.Collections.Generic.List<string>();
        var page="";
        foreach(var line in (value??"").Split('\n')) {
            if(page.Length>0&&page.Length+line.Length>440){pages.Add(page.Trim());page="";}
            page+=line+"\n";
        }
        if(page.Length>0)pages.Add(page.Trim());
        return pages.Count>0?pages.ToArray():new[]{"Follow the next step on the activity panel."};
    }
    void UpdateHelpPage() {
        if(!helpBody)return;
        if(helpPages==null||helpPages.Length==0)helpPages=PaginateHelp(instructions);
        helpPage=Mathf.Clamp(helpPage,0,helpPages.Length-1);
        helpBody.text=helpPages[helpPage];
        if(previousHelp)previousHelp.interactable=helpPage>0;
        if(nextHelp)nextHelp.interactable=helpPage<helpPages.Length-1;
        if(helpPageLabel)helpPageLabel.text=(helpPage+1)+" / "+helpPages.Length;
    }
    public void Refresh() {
        foreach(var readout in sliderReadouts)if(readout.slider&&readout.text)readout.text.text=readout.label+"   "+Mathf.RoundToInt(readout.slider.normalizedValue*100)+"%";
        if(journeyText)journeyText.text=FestivalJourney.Purpose(owner)+"\n"+FestivalJourney.Summary;
        if(statusText)statusText.text=owner.status;
        if(stageText)stageText.text=owner.completed?"ACTIVITY COMPLETE":owner.begun?"ACTIONS COMPLETED   "+owner.credited.Count:"DIWALI ACTIVITY  |  READY TO BEGIN";
    }
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
    void Update() { if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.15f;Refresh(); }
}
}
