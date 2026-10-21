using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;

public class InteractableItem : MonoBehaviour
{
    public string buildingId;
    public string itemName;
    public int eventNumber;
    public UnityEvent onInteract = new UnityEvent();
    public int interactionCount;

    protected virtual void Reset() { ParseName(); }
    protected virtual void Awake() { ParseName(); }
    public void ParseName()
    {
        var match = Regex.Match(name, @"^(.*)_Int(\d+)_(.+)$");
        if (!match.Success) { itemName = name; return; }
        buildingId = match.Groups[1].Value;
        eventNumber = int.Parse(match.Groups[2].Value);
        itemName = match.Groups[3].Value;
    }
    public Transform Part(string suffix)
    {
        foreach (var part in GetComponentsInChildren<Transform>(true))
            if (part != transform && part.name.EndsWith("_" + suffix)) return part;
        return null;
    }
    public virtual void Interact()
    {
        interactionCount++;
        onInteract.Invoke();
        GetComponent<ChristmasItemAction>()?.Perform();
    }
    [ContextMenu("Test Christmas action")]
    void TestAction() { Interact(); }
}
