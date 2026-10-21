using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Connects VR input to InteractableItem.Interact() (XR Interaction Toolkit 3.x).
///  - Grabbable items: pick up with grip, press TRIGGER while holding  -> Interact()
///  - Fixed items (lever, button, oven): point/touch and press GRIP/select -> Interact()
/// Added automatically by Tools > Christmas Island > Setup Selected Island.
/// </summary>
[RequireComponent(typeof(InteractableItem))]
public class XRItemHook : MonoBehaviour
{
    public bool interactOnGrab = false;   // also fire Interact() when a grabbable item is picked up

    InteractableItem item;
    XRBaseInteractable xr;

    void Awake()
    {
        item = GetComponent<InteractableItem>();
        xr = GetComponent<XRBaseInteractable>();
    }

    void OnEnable()
    {
        if (!xr) return;
        if (xr is XRGrabInteractable)
        {
            xr.activated.AddListener(OnActivated);
            if (interactOnGrab) xr.selectEntered.AddListener(OnSelected);
        }
        else xr.selectEntered.AddListener(OnSelected);
    }

    void OnDisable()
    {
        if (!xr) return;
        xr.activated.RemoveListener(OnActivated);
        xr.selectEntered.RemoveListener(OnSelected);
    }

    void OnActivated(ActivateEventArgs args) { item.Interact(); }
    void OnSelected(SelectEnterEventArgs args) { item.Interact(); }
}
