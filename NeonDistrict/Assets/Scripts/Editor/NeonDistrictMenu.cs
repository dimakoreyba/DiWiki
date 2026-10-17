#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > Neon District > Build City Scene
/// Creates/saves the playable scene, wires refs, and avoids duplicate cities on re-run.
/// </summary>
public static class NeonDistrictMenu
{
    const string SceneDir = "Assets/Scenes";
    const string ScenePath = "Assets/Scenes/NeonDistrict.unity";

    [MenuItem("Tools/Neon District/Build City Scene")]
    public static void BuildCityScene()
    {
        if (!Directory.Exists(SceneDir))
            Directory.CreateDirectory(SceneDir);

        Scene scene;
        if (File.Exists(ScenePath))
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        // Remove default leftovers that would stack on re-run except our systems
        CleanupDuplicateSystems();

        // Ensure single bootstrap + generate
        var boot = Object.FindObjectOfType<CityBootstrap>();
        if (boot == null)
        {
            var systems = GameObject.Find(CityBootstrap.CityRootName) ?? new GameObject(CityBootstrap.CityRootName);
            boot = systems.AddComponent<CityBootstrap>();
        }

        boot.EnsureSystems();
        boot.BuildOrRefreshCity();

        // Parent any orphan city root under systems (avoid duplicates via ClearCity on generate)
        foreach (var t in Object.FindObjectsOfType<Transform>())
        {
            if (t.name != "NeonDistrict_City" || t.parent != null) continue;
            var systems = GameObject.Find(CityBootstrap.CityRootName);
            if (systems != null) t.SetParent(systems.transform, true);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        // Add to build settings if missing
        EnsureBuildSettings(ScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = boot.gameObject;
        EditorGUIUtility.PingObject(boot.gameObject);

        Debug.Log($"[Neon District] Scene ready at {ScenePath}. Press Play to explore.");
    }

    [MenuItem("Tools/Neon District/Regenerate City In Open Scene")]
    public static void RegenerateInOpenScene()
    {
        var boot = Object.FindObjectOfType<CityBootstrap>();
        if (boot == null)
        {
            BuildCityScene();
            return;
        }

        boot.BuildOrRefreshCity();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Neon District] City regenerated.");
    }

    static void CleanupDuplicateSystems()
    {
        var boots = Object.FindObjectsOfType<CityBootstrap>();
        for (int i = 1; i < boots.Length; i++)
            Object.DestroyImmediate(boots[i].gameObject);

        // Multiple city roots
        var all = Object.FindObjectsOfType<Transform>();
        Transform keep = null;
        foreach (var t in all)
        {
            if (t.name != "NeonDistrict_City") continue;
            if (keep == null) { keep = t; continue; }
            Object.DestroyImmediate(t.gameObject);
        }
    }

    static void EnsureBuildSettings(string path)
    {
        var scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path == path)
                return;
        }

        var list = new EditorBuildSettingsScene[scenes.Length + 1];
        for (int i = 0; i < scenes.Length; i++)
            list[i] = scenes[i];
        list[scenes.Length] = new EditorBuildSettingsScene(path, true);
        EditorBuildSettings.scenes = list;
    }
}
#endif
