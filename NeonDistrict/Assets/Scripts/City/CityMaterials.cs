using UnityEngine;

/// <summary>
/// Shared procedural materials for Neon District (Built-in Standard shader).
/// Created at runtime / editor build — no external textures required.
/// </summary>
public static class CityMaterials
{
    public static Material Asphalt { get; private set; }
    public static Material Sidewalk { get; private set; }
    public static Material Building { get; private set; }
    public static Material WindowEmissive { get; private set; }
    public static Material NeonCyan { get; private set; }
    public static Material NeonMagenta { get; private set; }
    public static Material NeonViolet { get; private set; }
    public static Material NeonOrange { get; private set; }
    public static Material Landmark { get; private set; }
    public static Material VehicleBody { get; private set; }
    public static Material Headlight { get; private set; }
    public static Material Taillight { get; private set; }
    public static Material WetRoad { get; private set; }

    static bool _ready;

    public static void EnsureCreated()
    {
        if (_ready && Asphalt != null) return;

        Asphalt = Make("ND_Asphalt", new Color(0.08f, 0.09f, 0.12f), 0.15f, 0.55f);
        WetRoad = Make("ND_WetRoad", new Color(0.05f, 0.06f, 0.09f), 0.75f, 0.2f);
        Sidewalk = Make("ND_Sidewalk", new Color(0.18f, 0.19f, 0.22f), 0.25f, 0.7f);
        Building = Make("ND_Building", new Color(0.12f, 0.14f, 0.18f), 0.2f, 0.65f);
        Landmark = Make("ND_Landmark", new Color(0.1f, 0.12f, 0.16f), 0.35f, 0.4f);
        VehicleBody = Make("ND_Vehicle", new Color(0.15f, 0.16f, 0.2f), 0.55f, 0.35f);

        WindowEmissive = MakeEmissive("ND_Window", new Color(0.05f, 0.06f, 0.08f),
            new Color(0.55f, 0.75f, 1f) * 1.6f);
        NeonCyan = MakeEmissive("ND_NeonCyan", new Color(0.02f, 0.08f, 0.1f),
            new Color(0.1f, 1f, 1f) * 3.5f);
        NeonMagenta = MakeEmissive("ND_NeonMagenta", new Color(0.1f, 0.02f, 0.08f),
            new Color(1f, 0.15f, 0.85f) * 3.5f);
        NeonViolet = MakeEmissive("ND_NeonViolet", new Color(0.06f, 0.02f, 0.1f),
            new Color(0.65f, 0.25f, 1f) * 3.2f);
        NeonOrange = MakeEmissive("ND_NeonOrange", new Color(0.1f, 0.04f, 0.02f),
            new Color(1f, 0.45f, 0.1f) * 3.2f);
        Headlight = MakeEmissive("ND_Headlight", new Color(0.2f, 0.2f, 0.15f),
            new Color(1f, 0.95f, 0.75f) * 4f);
        Taillight = MakeEmissive("ND_Taillight", new Color(0.15f, 0.02f, 0.02f),
            new Color(1f, 0.1f, 0.08f) * 3.5f);

        _ready = true;
    }

    public static Material NeonByIndex(int i)
    {
        EnsureCreated();
        switch (Mathf.Abs(i) % 4)
        {
            case 0: return NeonCyan;
            case 1: return NeonMagenta;
            case 2: return NeonViolet;
            default: return NeonOrange;
        }
    }

    public static void SetWetStreets(bool wet)
    {
        EnsureCreated();
        // Callers swap asphalt/wet materials on road renderers.
    }

    static Material Make(string name, Color albedo, float metallic, float smoothness)
    {
        var m = new Material(Shader.Find("Standard")) { name = name };
        m.color = albedo;
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Glossiness", smoothness);
        return m;
    }

    static Material MakeEmissive(string name, Color albedo, Color emission)
    {
        var m = Make(name, albedo, 0.1f, 0.5f);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        m.SetColor("_EmissionColor", emission);
        return m;
    }
}
