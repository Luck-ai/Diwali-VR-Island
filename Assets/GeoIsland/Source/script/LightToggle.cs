using UnityEngine;

public class LightToggle : MonoBehaviour
{
    public Light targetLight;
    public Renderer glowRenderer;
    public Color glowColor = Color.yellow;
    bool isOn;
    void Start()
    {
        SetState(false);
    }

    public void Toggle()
    {
        SetState(!isOn);
    }

    void SetState(bool on)
    {
        isOn = on;
        if (targetLight != null)
            targetLight.enabled = on;
        if (glowRenderer != null)
        {
            Material m = glowRenderer.material;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", on ? glowColor * 2f :Color.black);
        }
    }
}
