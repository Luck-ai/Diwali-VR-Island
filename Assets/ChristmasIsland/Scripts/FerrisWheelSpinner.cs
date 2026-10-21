using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put on the Ferris wheel site (B3_FerrisWheel) - the setup tool adds it automatically.
/// Spins the wheel, keeps the cabins hanging level, speeds up / slows down smoothly,
/// and is started/stopped by the site's ControlLever event object.
/// </summary>
public class FerrisWheelSpinner : MonoBehaviour
{
    public Transform wheel;                     // auto-found: *_Wheel
    public float maxDegreesPerSecond = 8f;
    public float acceleration = 2f;             // degrees/sec per second
    public bool spinning = false;                // spins as soon as you press Play

    float speed;
    readonly List<Transform> cabins = new List<Transform>();
    InteractableItem lever;
    Transform leverHandle;

    void Awake()
    {
        if (!wheel) wheel = FindChildEndingWith(transform, "_Wheel");
        if (!wheel) { Debug.LogWarning("FerrisWheelSpinner: no *_Wheel child found", this); enabled = false; return; }
        foreach (Transform c in wheel) if (c.name.Contains("_Cabin_")) cabins.Add(c);

        var leverT = FindChildEndingWith(transform, "_ControlLever");
        if (leverT) lever = leverT.GetComponent<InteractableItem>();
        if (lever)
        {
            
            leverHandle = lever.Part("Handle");
        }
        UpdateLever();
    }

    void Update()
    {
        speed = Mathf.MoveTowards(speed, spinning ? maxDegreesPerSecond : 0f, acceleration * Time.deltaTime);
        if (speed > 0f) wheel.Rotate(0f, 0f, speed * Time.deltaTime, Space.Self);
        Quaternion level = Quaternion.LookRotation(wheel.forward, Vector3.up);
        foreach (var c in cabins) c.rotation = level;
    }

    public void SetSpinning(bool on) { spinning = on; UpdateLever(); }
    public void ToggleSpinning() { SetSpinning(!spinning); }

    void UpdateLever()
    {
        if (leverHandle) leverHandle.localRotation = Quaternion.Euler(spinning ? -25f : 25f, 0f, 0f);
    }

    public static Transform FindChildEndingWith(Transform root, string suffix)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.EndsWith(suffix)) return t;
        return null;
    }
}
