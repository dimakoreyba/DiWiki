using UnityEngine;

/// <summary>
/// Rain particle system, wet street look, optional subtle lightning flashes.
/// Toggle with F (wired via CityHUD / input here).
/// </summary>
public class RainController : MonoBehaviour
{
    [SerializeField] bool rainEnabled = true;
    [SerializeField] bool lightningEnabled = true;
    [SerializeField] float emissionRate = 1200f;
    [SerializeField] float lightningChancePerSecond = 0.08f;

    public bool RainEnabled => rainEnabled;

    ParticleSystem _rain;
    CityGenerator _city;
    Light _flash;
    float _flashTimer;
    bool _wetApplied;

    public void Bind(CityGenerator city)
    {
        _city = city;
        EnsureRainSystem();
        SetRain(rainEnabled);
    }

    void EnsureRainSystem()
    {
        if (_rain != null) return;

        var go = new GameObject("RainParticles");
        go.transform.SetParent(transform, false);
        _rain = go.AddComponent<ParticleSystem>();

        var main = _rain.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(18f, 28f);
        main.startColor = new Color(0.55f, 0.7f, 0.9f, 0.55f);
        main.maxParticles = 5000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = _rain.emission;
        emission.rateOverTime = emissionRate;

        var shape = _rain.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(220f, 1f, 220f);

        var vel = _rain.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-2f, 2f);
        vel.y = new ParticleSystem.MinMaxCurve(-22f, -30f);
        vel.z = new ParticleSystem.MinMaxCurve(-2f, 2f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2.2f;
        renderer.velocityScale = 0.08f;
        var mat = new Material(Shader.Find("Particles/Standard Unlit")
                               ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                               ?? Shader.Find("Sprites/Default"));
        mat.color = new Color(0.6f, 0.75f, 0.95f, 0.45f);
        renderer.sharedMaterial = mat;

        go.transform.position = new Vector3(0f, 55f, 0f);

        var flashGo = new GameObject("LightningFlash");
        flashGo.transform.SetParent(transform, false);
        _flash = flashGo.AddComponent<Light>();
        _flash.type = LightType.Directional;
        _flash.intensity = 0f;
        _flash.color = new Color(0.7f, 0.8f, 1f);
        _flash.shadows = LightShadows.None;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            SetRain(!rainEnabled);

        if (!rainEnabled || !lightningEnabled || _flash == null) return;

        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            _flash.intensity = Mathf.Lerp(0f, 1.8f, _flashTimer / 0.12f);
            if (_flashTimer <= 0f) _flash.intensity = 0f;
        }
        else if (Random.value < lightningChancePerSecond * Time.deltaTime)
        {
            _flashTimer = 0.12f;
            _flash.intensity = 1.6f;
        }
    }

    public void SetRain(bool on)
    {
        rainEnabled = on;
        EnsureRainSystem();
        if (rainEnabled) _rain.Play();
        else _rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (_city != null)
        {
            _city.ApplyWetRoads(rainEnabled);
            _wetApplied = rainEnabled;
        }
    }

    public void ToggleRain() => SetRain(!rainEnabled);
}
