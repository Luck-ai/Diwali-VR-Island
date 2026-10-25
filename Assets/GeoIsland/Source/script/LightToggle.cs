using UnityEngine;

public class LightToggle : MonoBehaviour
{
    public Light targetLight;
    public Renderer glowRenderer;
    public Color glowColor = Color.yellow;
    public int glowMaterialIndex;
    public bool dimSurfaceWhenOff;
    Material glowMaterial;
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
            if (!glowMaterial)
            {
                var materials = glowRenderer.materials;
                if (glowMaterialIndex < 0 || glowMaterialIndex >= materials.Length) return;
                glowMaterial = materials[glowMaterialIndex];
            }
            Material m = glowMaterial;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", on ? glowColor * 2f :Color.black);
            if (dimSurfaceWhenOff)
            {
                var surface = on ? glowColor : glowColor * .06f;
                surface.a = 1f;
                m.SetColor("_BaseColor", surface);
            }
        }
    }
}
