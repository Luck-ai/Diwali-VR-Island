using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public sealed class ChristmasSwing : MonoBehaviour
{
    public bool active;
    public int completedCount;

    Quaternion rest;
    float phase;
    XRSimpleInteractable interactable;

    void Awake()
    {
        rest = transform.localRotation;
        interactable = GetComponent<XRSimpleInteractable>();
    }

    void OnEnable()
    {
        if (!interactable) interactable = GetComponent<XRSimpleInteractable>();
        interactable.selectEntered.AddListener(Toggle);
    }

    void OnDisable() { if (interactable) interactable.selectEntered.RemoveListener(Toggle); }

    void Toggle(SelectEnterEventArgs args) { active = !active; completedCount++; }

    void Update()
    {
        phase += Time.deltaTime * 1.6f;
        transform.localRotation = Quaternion.Slerp(transform.localRotation,
            rest * Quaternion.Euler(active ? Mathf.Sin(phase) * 28f : 0, 0, 0), Time.deltaTime * 4f);
    }
}
