using UnityEngine;

/// <summary>
/// Day/night cycle. N = snap preset, T = pause time.
/// </summary>
public class DayNightController : MonoBehaviour
{
    [SerializeField] Light sun;
    [SerializeField] float dayLengthSeconds = 180f;
    [SerializeField] bool timePaused;
    [Range(0f, 1f)] [SerializeField] float timeOfDay = 0.78f; // evening default

    public float TimeOfDay => timeOfDay;
    public bool IsNight => timeOfDay < 0.22f || timeOfDay > 0.72f;
    public bool TimePaused => timePaused;
    public string PhaseLabel
    {
        get
        {
            if (timeOfDay > 0.72f || timeOfDay < 0.05f) return "NIGHT";
            if (timeOfDay < 0.22f) return "DAWN";
            if (timeOfDay < 0.5f) return "DAY";
            if (timeOfDay < 0.72f) return "DUSK";
            return "NIGHT";
        }
    }

    bool _neonOn = true;
    public bool NeonLightsOn => _neonOn;

    public void Bind(Light directional)
    {
        sun = directional;
        ApplyLighting();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
            timePaused = !timePaused;

        if (Input.GetKeyDown(KeyCode.N))
        {
            // Toggle day/night preset
            timeOfDay = IsNight ? 0.35f : 0.82f;
            ApplyLighting();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            _neonOn = !_neonOn;
            ApplyNeonVisibility(_neonOn);
        }

        if (!timePaused && dayLengthSeconds > 0.01f)
        {
            timeOfDay = Mathf.Repeat(timeOfDay + Time.deltaTime / dayLengthSeconds, 1f);
            ApplyLighting();
        }
    }

    public void ApplyLighting()
    {
        if (sun == null)
        {
            var lights = FindObjectsOfType<Light>();
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    sun = lights[i];
                    break;
                }
            }
        }

        if (sun != null)
        {
            float angle = timeOfDay * 360f - 90f;
            sun.transform.rotation = Quaternion.Euler(angle, 35f, 0f);

            float dayFactor = Mathf.Clamp01(1f - Mathf.Abs(timeOfDay - 0.5f) * 2.2f);
            sun.intensity = Mathf.Lerp(0.08f, 1.05f, dayFactor);
            sun.color = Color.Lerp(new Color(0.35f, 0.45f, 0.85f), new Color(1f, 0.95f, 0.85f), dayFactor);
        }

        // Fog + ambient
        float night = IsNight ? 1f : 0f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = Mathf.Lerp(0.008f, 0.018f, night);
        RenderSettings.fogColor = Color.Lerp(
            new Color(0.45f, 0.55f, 0.65f),
            new Color(0.02f, 0.04f, 0.08f),
            night);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.Lerp(
            new Color(0.45f, 0.48f, 0.55f),
            new Color(0.05f, 0.07f, 0.12f),
            night);

        if (Camera.main != null)
            Camera.main.backgroundColor = RenderSettings.fogColor;
    }

    void ApplyNeonVisibility(bool on)
    {
        // Dim/restore emissive materials globally
        CityMaterials.EnsureCreated();
        float mul = on ? 1f : 0.05f;
        SetEmission(CityMaterials.NeonCyan, new Color(0.1f, 1f, 1f) * 3.5f * mul);
        SetEmission(CityMaterials.NeonMagenta, new Color(1f, 0.15f, 0.85f) * 3.5f * mul);
        SetEmission(CityMaterials.NeonViolet, new Color(0.65f, 0.25f, 1f) * 3.2f * mul);
        SetEmission(CityMaterials.NeonOrange, new Color(1f, 0.45f, 0.1f) * 3.2f * mul);
        SetEmission(CityMaterials.WindowEmissive, new Color(0.55f, 0.75f, 1f) * 1.6f * (on ? 1f : 0.15f));
        SetEmission(CityMaterials.Headlight, new Color(1f, 0.95f, 0.75f) * 4f * mul);
        SetEmission(CityMaterials.Taillight, new Color(1f, 0.1f, 0.08f) * 3.5f * mul);
    }

    static void SetEmission(Material m, Color c)
    {
        if (m == null) return;
        m.SetColor("_EmissionColor", c);
    }

    public void ToggleNeon()
    {
        _neonOn = !_neonOn;
        ApplyNeonVisibility(_neonOn);
    }
}

