using UnityEngine;
using System.Collections;

public class CaveLightingController : MonoBehaviour
{
    [Header("Settings")]
    public Light sunLight;

    [Header("Lighting Intensities")]
    public float outsideIntensity = 1.0f;
    public float insideIntensity = 0.1f;

    [Header("Ambient Colors")]
    public Color outsideAmbient = new Color(0.5f, 0.5f, 0.5f);
    public Color insideAmbient = new Color(0.1f, 0.1f, 0.1f);

    public float fadeSpeed = 2.0f;

    private Coroutine changeLightingCoroutine;

    private void Awake()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = outsideAmbient;
        sunLight = GameObject.FindGameObjectWithTag("Light").GetComponent<Light>();
        if (sunLight != null)
        {
            sunLight.intensity = outsideIntensity;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (changeLightingCoroutine != null) StopCoroutine(changeLightingCoroutine);
            changeLightingCoroutine = StartCoroutine(FadeLighting(insideIntensity, insideAmbient));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (changeLightingCoroutine != null) StopCoroutine(changeLightingCoroutine);
            changeLightingCoroutine = StartCoroutine(FadeLighting(outsideIntensity, outsideAmbient));
        }
    }

    private IEnumerator FadeLighting(float targetIntensity, Color targetAmbient)
    {
        float currentIntensity = sunLight != null ? sunLight.intensity : 0f;
        Color currentAmbient = RenderSettings.ambientLight;
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * fadeSpeed;

            if (sunLight != null)
            {
                sunLight.intensity = Mathf.Lerp(currentIntensity, targetIntensity, t);
            }

            RenderSettings.ambientLight = Color.Lerp(currentAmbient, targetAmbient, t);

            yield return null;
        }
    }
}