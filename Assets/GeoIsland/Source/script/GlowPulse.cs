using System.Collections;
using UnityEngine;

// Lights an object up for a moment: its glow light switches on and its materials glow,
// then both fade out over the last half second.
public class GlowPulse : MonoBehaviour
{
    public Light glowLight;
    public Renderer glowRenderer;
    public Color glowColor = new Color(1f, .85f, .2f);
    public float seconds = 2f;
    public float emissionStrength = 2f;
    Material[] materials;
    Color[] baseEmission;
    float lightIntensity;
    Coroutine running;

    void Awake()
    {
        if (glowLight)
        {
            lightIntensity = glowLight.intensity;
            glowLight.enabled = false;
        }
        if (!glowRenderer) return;
        materials = glowRenderer.materials;
        baseEmission = new Color[materials.Length];
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].EnableKeyword("_EMISSION");
            baseEmission[i] = materials[i].HasProperty("_EmissionColor") ? materials[i].GetColor("_EmissionColor") : Color.black;
        }
    }

    public void Pulse()
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Glow());
    }

    IEnumerator Glow()
    {
        const float fade = .5f;
        if (glowLight) glowLight.enabled = true;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            Apply(Mathf.Clamp01((seconds - t) / fade));
            yield return null;
        }
        Apply(0f);
        if (glowLight) glowLight.enabled = false;
        running = null;
    }

    void Apply(float amount)
    {
        if (glowLight) glowLight.intensity = lightIntensity * amount;
        if (materials == null) return;
        for (int i = 0; i < materials.Length; i++)
            materials[i].SetColor("_EmissionColor", Color.Lerp(baseEmission[i], glowColor * emissionStrength, amount));
    }
}
