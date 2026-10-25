#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

[InitializeOnLoad]
public static class GeoIslandFeedQuestSetup
{
    const string AutoAppliedKey = "GeoIsland.FeedQuestApplied";
    const string Mp3 = "Assets/GeoIsland/Source/mp3/";
    static readonly string[] FoodNames =
    {
        "Cylinder.021", "Cylinder.022", "Cylinder.002", "Cube", "Cube.059", "Cylinder.001", "Cylinder.038",
        "Cylinder.004", "Cylinder.036", "Cylinder.037", "Cylinder.035", "Cylinder.034", "Cylinder.025", "Cylinder.003",
    };

    // Applies the setup once, the first time the festival scene is open after this
    // script compiles. The scene is only marked dirty; Ctrl+S keeps the result.
    static GeoIslandFeedQuestSetup()
    {
        EditorApplication.delayCall += AutoApply;
        EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += AutoApply;
    }

    static void AutoApply()
    {
        if (EditorPrefs.GetBool(AutoAppliedKey) || EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || !GameObject.Find("Geo Island Integration")) return;
        try
        {
            Apply();
            EditorPrefs.SetBool(AutoAppliedKey, true);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Geo feed quest auto-setup skipped: " + e.Message);
        }
    }

    // Cube.070 (parent) gets a Talk button and speech; Cube.078 (kid) gets an "0/10" counter
    // and eats the listed foods when they are carried to it.
    [MenuItem("Tools/Geo Island/Add Feed The Kid Quest")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var root = GameObject.Find("Geo Island Integration");
        if (!root) throw new InvalidOperationException("Geo island is missing.");
        var all = root.GetComponentsInChildren<Transform>(true);
        var parent = all.Single(t => t.name == "Cube.070").gameObject;
        var kid = all.Single(t => t.name == "Cube.078").GetComponent<Renderer>();
        var eat = Clip("pizza-eat-forsaken.mp3");
        var meow = Clip("meowrgh.mp3");
        Undo.RegisterFullObjectHierarchyUndo(parent, "Add feed quest");

        var foods = new List<XRGrabInteractable>();
        foreach (var name in FoodNames)
        {
            var food = FindFood(all, name);
            if (food) foods.Add(food);
        }
        if (foods.Count < 10) Debug.LogWarning("Geo feed quest: only " + foods.Count + " foods found; the kid needs 10.");

        var quest = parent.GetComponent<FeedQuest>();
        if (!quest) quest = Undo.AddComponent<FeedQuest>(parent);
        BuildTalkUi(root.transform, parent.GetComponent<Renderer>().bounds, quest);
        quest.kidCounter = KidCounter(root.transform, kid.bounds);
        quest.kid = kid;
        quest.foods = foods.ToArray();
        quest.eatClip = eat;
        quest.thanksClip = meow;
        EditorUtility.SetDirty(quest);
        EditorSceneManager.MarkSceneDirty(parent.scene);
        Selection.activeGameObject = parent;
        Debug.Log("Geo feed quest: Cube.070 asks to feed Cube.078; " + foods.Count + " foods: " +
            string.Join(", ", foods.Select(f => f.name)) + ". Save the scene to keep it.");
    }

    static AudioClip Clip(string file)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Mp3 + file);
        if (!clip) throw new InvalidOperationException("Clip is missing: " + Mp3 + file);
        return clip;
    }

    // Prefers an existing pickup with that name; otherwise turns the one plain object with
    // that name into a pickup. Objects that already do something else (e.g. a light) are skipped.
    static XRGrabInteractable FindFood(Transform[] all, string name)
    {
        var matches = all.Where(t => t.name == name).ToArray();
        var grab = matches.Select(t => t.GetComponent<XRGrabInteractable>()).FirstOrDefault(g => g);
        if (grab)
        {
            Debug.Log("Geo feed quest: " + name + " was already VR grabbable.");
            return grab;
        }
        var plain = matches.Where(t => !t.GetComponent<LightToggle>() && t.GetComponent<MeshFilter>()).ToArray();
        if (plain.Length != 1)
        {
            Debug.LogWarning("Geo feed quest: skipped " + name + " (" + matches.Length + " objects with that name, none usable as food).");
            return null;
        }
        Undo.RegisterFullObjectHierarchyUndo(plain[0].gameObject, "Add feed quest");
        Debug.Log("Geo feed quest: " + name + " was NOT grabbable; it is now.");
        return GeoIslandPickups.EnsureGrab(plain[0].gameObject, out _);
    }

    static void BuildTalkUi(Transform root, Bounds head, FeedQuest quest)
    {
        var old = root.Find("Feed quest talk UI");
        if (old) Undo.DestroyObjectImmediate(old.gameObject);
        var canvasObject = new GameObject("Feed quest talk UI", typeof(RectTransform), typeof(Canvas),
            typeof(TrackedDeviceGraphicRaycaster), typeof(FaceCamera));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Add feed quest");
        var canvasRect = (RectTransform)canvasObject.transform;
        canvasRect.SetParent(root, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvasObject.GetComponent<TrackedDeviceGraphicRaycaster>().checkFor3DOcclusion = true;
        canvasRect.sizeDelta = new Vector2(520f, 220f);
        canvasRect.localScale = Vector3.one * (.0025f / root.lossyScale.x);
        canvasRect.position = new Vector3(head.center.x, head.max.y + .45f, head.center.z);
        var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        var button = Box("Talk button", canvasRect, new Vector2(220f, 90f), new Color(1f, .95f, .8f), sprite);
        var buttonComponent = button.gameObject.AddComponent<Button>();
        Label(button, "Talk", 44f, Color.black);
        UnityEventTools.AddPersistentListener(buttonComponent.onClick, quest.Talk);

        var panel = Box("Speech", canvasRect, new Vector2(520f, 220f), new Color(0f, 0f, 0f, .75f), sprite);
        quest.speechText = Label(panel, quest.request, 34f, Color.white);
        panel.gameObject.SetActive(false);
        quest.talkButton = button.gameObject;
        quest.speechPanel = panel.gameObject;
        quest.talkUi = canvasObject;
    }

    static RectTransform Box(string name, RectTransform parent, Vector2 size, Color color, Sprite sprite)
    {
        var box = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        box.SetParent(parent, false);
        box.sizeDelta = size;
        var image = box.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        return box;
    }

    static TextMeshProUGUI Label(RectTransform parent, string value, float size, Color color)
    {
        var label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
        label.SetParent(parent, false);
        label.anchorMin = Vector2.zero;
        label.anchorMax = Vector2.one;
        label.offsetMin = new Vector2(16f, 10f);
        label.offsetMax = new Vector2(-16f, -10f);
        var text = label.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    static TextMeshPro KidCounter(Transform root, Bounds head)
    {
        var sign = root.Find("Kid food counter");
        if (!sign)
        {
            sign = new GameObject("Kid food counter", typeof(RectTransform), typeof(TextMeshPro), typeof(FaceCamera)).transform;
            Undo.RegisterCreatedObjectUndo(sign.gameObject, "Add feed quest");
            sign.SetParent(root, false);
        }
        sign.position = new Vector3(head.center.x, head.max.y + .35f, head.center.z);
        sign.localScale = Vector3.one / root.lossyScale.x;
        var text = sign.GetComponent<TextMeshPro>();
        text.text = "0/10";
        text.fontSize = 3f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(2f, .6f);
        EditorUtility.SetDirty(text);
        return text;
    }
}
#endif
