using UnityEngine;

/// <summary>
/// OnGUI HUD: "NEON DISTRICT" + status for phase/rain/neon. Hotkeys F/L/R/Escape documented.
/// </summary>
public class CityHUD : MonoBehaviour
{
    [SerializeField] DayNightController dayNight;
    [SerializeField] RainController rain;

    GUIStyle _title;
    GUIStyle _body;
    bool _stylesReady;

    public void Bind(DayNightController dn, RainController r)
    {
        dayNight = dn;
        rain = r;
    }

    void EnsureStyles()
    {
        if (_stylesReady) return;
        _title = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft
        };
        _title.normal.textColor = new Color(0.2f, 1f, 1f, 0.95f);

        _body = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.UpperLeft
        };
        _body.normal.textColor = new Color(0.85f, 0.9f, 1f, 0.9f);
        _stylesReady = true;
    }

    void OnGUI()
    {
        EnsureStyles();
        float x = 18f;
        float y = 14f;

        GUI.Label(new Rect(x, y, 640f, 36f), "NEON DISTRICT", _title);
        y += 36f;

        string phase = dayNight != null ? dayNight.PhaseLabel : "—";
        string rainState = rain != null && rain.RainEnabled ? "ON" : "OFF";
        string neon = dayNight != null && dayNight.NeonLightsOn ? "ON" : "OFF";
        string timePause = dayNight != null && dayNight.TimePaused ? "PAUSED" : "RUNNING";

        GUI.Label(new Rect(x, y, 720f, 22f),
            $"Phase {phase}   Rain {rainState}   Neon {neon}   Time {timePause}", _body);
        y += 22f;
        GUI.Label(new Rect(x, y, 900f, 40f),
            "WASD move · Mouse look · Space/Ctrl up/down · Shift sprint · Wheel speed · R reset · Esc cursor · F rain · L neon · N day/night · T pause time",
            _body);
    }
}
