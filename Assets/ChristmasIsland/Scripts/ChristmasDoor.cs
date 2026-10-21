using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public sealed class ChristmasDoor : MonoBehaviour
{
    Quaternion closed;
    bool open;
    XRSimpleInteractable xr;
    void Awake() { closed = transform.localRotation; xr = GetComponent<XRSimpleInteractable>(); }
    void OnEnable() { if (!xr) xr = GetComponent<XRSimpleInteractable>(); xr.selectEntered.AddListener(Toggle); }
    void OnDisable() { if (xr) xr.selectEntered.RemoveListener(Toggle); }
    void Toggle(SelectEnterEventArgs args) { open = !open; }
    void Update() { transform.localRotation = Quaternion.Slerp(transform.localRotation, closed * Quaternion.Euler(0, open ? 100 : 0, 0), Time.deltaTime * 5); }
}
