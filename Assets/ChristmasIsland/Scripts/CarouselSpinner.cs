using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put on the merry-go-round site (B7_Carousel) - the setup tool adds it automatically.
/// Spins the rotor, bobs the horses/reindeer, eases in/out, and is started/stopped by the StartButton event object.
/// </summary>
public class CarouselSpinner : MonoBehaviour
{
    public Transform rotor;                     // auto-found: *_Rotor
    public float maxDegreesPerSecond = 20f;
    public float acceleration = 6f;
    public float bobHeight = 0.25f;
    public float bobSpeed = 2f;
    public bool spinning = false;                // spins as soon as you press Play
    public AudioSource music;                   // optional: plays while spinning

    float speed, phase;
    readonly List<Transform> animals = new List<Transform>();
    readonly List<float> baseY = new List<float>();
    InteractableItem button;
    Transform buttonT;
    Vector3 buttonScale;
    float pressTimer;

    void Awake()
    {
        if (!rotor) rotor = FerrisWheelSpinner.FindChildEndingWith(transform, "_Rotor");
        if (!rotor) { Debug.LogWarning("CarouselSpinner: no *_Rotor child found", this); enabled = false; return; }
        foreach (Transform c in rotor)
            if (c.name.Contains("_Horse_") || c.name.Contains("_Reindeer_")) { animals.Add(c); baseY.Add(c.localPosition.y); }

        buttonT = FerrisWheelSpinner.FindChildEndingWith(transform, "_StartButton");
        if (buttonT)
        {
            buttonScale = buttonT.localScale;
            button = buttonT.GetComponent<InteractableItem>();
            
        }
    }

    void Start() { UpdateMusic(); }

    void Update()
    {
        speed = Mathf.MoveTowards(speed, spinning ? maxDegreesPerSecond : 0f, acceleration * Time.deltaTime);
        if (speed > 0f)
        {
            rotor.Rotate(0f, speed * Time.deltaTime, 0f, Space.Self);
            phase += Time.deltaTime * bobSpeed * (speed / Mathf.Max(0.01f, maxDegreesPerSecond));
        }
        float amount = speed / Mathf.Max(0.01f, maxDegreesPerSecond);
        for (int i = 0; i < animals.Count; i++)
        {
            Vector3 p = animals[i].localPosition;
            p.y = baseY[i] + bobHeight * amount * Mathf.Sin(phase + i * Mathf.PI * 0.5f);
            animals[i].localPosition = p;
        }
        if (buttonT && pressTimer > 0f)       // little "pressed" squash on the button
        {
            pressTimer -= Time.deltaTime;
            buttonT.localScale = pressTimer > 0f ? new Vector3(buttonScale.x, buttonScale.y * 0.7f, buttonScale.z) : buttonScale;
        }
    }

    public void SetSpinning(bool on) { spinning = on; pressTimer = 0.2f; UpdateMusic(); }
    public void ToggleSpinning() { SetSpinning(!spinning); }

    void UpdateMusic()
    {
        if (!music) return;
        if (spinning && !music.isPlaying) music.Play();
        if (!spinning && music.isPlaying) music.Pause();
    }
}
