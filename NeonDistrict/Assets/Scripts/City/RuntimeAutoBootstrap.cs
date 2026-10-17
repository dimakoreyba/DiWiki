using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// If Play starts with no CityBootstrap in the loaded scene, create systems and generate.
/// </summary>
public static class RuntimeAutoBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureCity()
    {
        if (Object.FindObjectOfType<CityBootstrap>() != null)
            return;

        if (SceneManager.GetActiveScene().name == "InitTestScene")
            return;

        var go = new GameObject(CityBootstrap.CityRootName);
        // Awake on CityBootstrap runs EnsureSystems + Generate (generateOnAwake).
        go.AddComponent<CityBootstrap>();
    }
}
