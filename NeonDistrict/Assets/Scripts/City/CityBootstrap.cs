using UnityEngine;

/// <summary>
/// Ensures Neon District systems exist at runtime and regenerates the city if missing.
/// Safe to place in an empty bootstrap scene or on the main city scene root.
/// </summary>
[DefaultExecutionOrder(-100)]
public class CityBootstrap : MonoBehaviour
{
    public const string CityRootName = "NeonDistrict_Systems";
    public const int DefaultSeed = 1337;

    [SerializeField] int seed = DefaultSeed;
    [SerializeField] bool generateOnAwake = true;
    [SerializeField] bool createCameraIfMissing = true;

    CityGenerator _city;
    BuildingGenerator _buildings;
    TrafficController _traffic;
    RainController _rain;
    DayNightController _dayNight;
    CityHUD _hud;
    FreeFlyCamera _flyCam;

    void Awake()
    {
        EnsureSystems();
        if (generateOnAwake)
            BuildOrRefreshCity();
    }

    public void EnsureSystems()
    {
        CityMaterials.EnsureCreated();

        _city = FindObjectOfType<CityGenerator>();
        if (_city == null)
        {
            var systems = GameObject.Find(CityRootName);
            if (systems == null)
                systems = new GameObject(CityRootName);
            _city = systems.AddComponent<CityGenerator>();
            _city.seed = seed;
        }

        _buildings = _city.GetComponent<BuildingGenerator>();
        if (_buildings == null)
            _buildings = _city.gameObject.AddComponent<BuildingGenerator>();
        _city.buildingGenerator = _buildings;

        _traffic = _city.GetComponent<TrafficController>();
        if (_traffic == null)
            _traffic = _city.gameObject.AddComponent<TrafficController>();
        _city.trafficController = _traffic;

        _rain = FindObjectOfType<RainController>();
        if (_rain == null)
            _rain = _city.gameObject.AddComponent<RainController>();

        _dayNight = FindObjectOfType<DayNightController>();
        if (_dayNight == null)
            _dayNight = _city.gameObject.AddComponent<DayNightController>();

        _hud = FindObjectOfType<CityHUD>();
        if (_hud == null)
            _hud = _city.gameObject.AddComponent<CityHUD>();
        _hud.Bind(_dayNight, _rain);

        EnsureLighting();
        EnsureCamera();
    }

    public void BuildOrRefreshCity()
    {
        EnsureSystems();
        _city.seed = seed;

        // Avoid duplicate city geometry: clear then regenerate under single root
        if (_city.cityRoot == null)
        {
            var existing = GameObject.Find("NeonDistrict_City");
            if (existing != null)
                _city.cityRoot = existing.transform;
        }

        _city.Generate();
        _rain.Bind(_city);
        _dayNight.ApplyLighting();
    }

    void EnsureLighting()
    {
        Light sun = null;
        var lights = FindObjectsOfType<Light>();
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type == LightType.Directional)
            {
                sun = lights[i];
                break;
            }
        }

        if (sun == null)
        {
            var sunGo = new GameObject("Directional Light");
            sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
        }

        _dayNight.Bind(sun);
    }

    void EnsureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var cams = FindObjectsOfType<Camera>();
            if (cams.Length > 0) cam = cams[0];
        }

        if (cam == null && createCameraIfMissing)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }

        if (cam == null) return;

        cam.nearClipPlane = 0.2f;
        cam.farClipPlane = 600f;
        cam.fieldOfView = 65f;
        cam.backgroundColor = new Color(0.02f, 0.04f, 0.08f);

        _flyCam = cam.GetComponent<FreeFlyCamera>();
        bool createdFly = _flyCam == null;
        if (createdFly)
            _flyCam = cam.gameObject.AddComponent<FreeFlyCamera>();

        // Only place the camera when first wiring a fly cam (avoid resetting on regenerate)
        if (createdFly || cam.transform.position.sqrMagnitude < 0.01f)
        {
            cam.transform.position = new Vector3(-40f, 55f, -70f);
            cam.transform.rotation = Quaternion.Euler(25f, 35f, 0f);
        }
    }

#if UNITY_EDITOR
    /// <summary>Editor helper used by the Tools menu.</summary>
    public static CityBootstrap CreateInScene(int seedValue = DefaultSeed)
    {
        var existing = Object.FindObjectOfType<CityBootstrap>();
        if (existing != null)
        {
            existing.seed = seedValue;
            existing.EnsureSystems();
            existing.BuildOrRefreshCity();
            return existing;
        }

        var go = GameObject.Find(CityRootName) ?? new GameObject(CityRootName);
        var boot = go.GetComponent<CityBootstrap>() ?? go.AddComponent<CityBootstrap>();
        boot.seed = seedValue;
        boot.generateOnAwake = true;
        boot.EnsureSystems();
        boot.BuildOrRefreshCity();
        return boot;
    }
#endif
}
