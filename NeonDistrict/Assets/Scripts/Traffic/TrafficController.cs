using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns procedural vehicles on deterministic lane loops around the city grid.
/// </summary>
public class TrafficController : MonoBehaviour
{
    [SerializeField] int vehicleCount = 28;
    [SerializeField] float minSpeed = 9f;
    [SerializeField] float maxSpeed = 16f;
    [SerializeField] Transform vehicleRoot;

    readonly List<ProceduralVehicle> _vehicles = new List<ProceduralVehicle>();

    public void Initialize(CityGenerator city)
    {
        ClearVehicles();
        if (city == null) return;

        if (vehicleRoot == null)
        {
            var go = new GameObject("Traffic");
            go.transform.SetParent(city.cityRoot != null ? city.cityRoot : transform, false);
            vehicleRoot = go.transform;
        }

        var loops = BuildLoops(city);
        if (loops.Count == 0) return;

        var rng = new System.Random(city.seed ^ 0x7ACE);
        int count = Mathf.Clamp(vehicleCount, 0, 80);

        for (int v = 0; v < count; v++)
        {
            var path = loops[rng.Next(loops.Count)];
            if (path.Length < 2) continue;

            int start = rng.Next(path.Length);
            float speed = Mathf.Lerp(minSpeed, maxSpeed, (float)rng.NextDouble());

            var go = new GameObject($"Vehicle_{v}");
            go.transform.SetParent(vehicleRoot, false);
            var vehicle = go.AddComponent<ProceduralVehicle>();
            vehicle.Setup(path, start, speed, city.seed + v * 131);
            _vehicles.Add(vehicle);
        }
    }

    void ClearVehicles()
    {
        _vehicles.Clear();

        if (vehicleRoot != null)
        {
            for (int i = vehicleRoot.childCount - 1; i >= 0; i--)
            {
                var c = vehicleRoot.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }
        }
    }

    List<Vector3[]> BuildLoops(CityGenerator city)
    {
        var loops = new List<Vector3[]>();
        float origin = -city.citySize * 0.5f;
        float pitch = city.citySize / city.blocksPerSide;
        float y = city.groundY + 0.4f;
        float lane = city.roadWidth * 0.22f;

        for (int i = 0; i <= city.blocksPerSide; i += 2)
        {
            float z = origin + i * pitch;
            var forward = new List<Vector3>();
            var back = new List<Vector3>();
            for (int s = 0; s <= city.blocksPerSide; s++)
            {
                float x = origin + s * pitch;
                forward.Add(new Vector3(x, y, z + lane));
                back.Add(new Vector3(origin + (city.blocksPerSide - s) * pitch, y, z - lane));
            }
            loops.Add(forward.ToArray());
            loops.Add(back.ToArray());
        }

        for (int i = 1; i <= city.blocksPerSide; i += 2)
        {
            float x = origin + i * pitch;
            var forward = new List<Vector3>();
            var back = new List<Vector3>();
            for (int s = 0; s <= city.blocksPerSide; s++)
            {
                float z = origin + s * pitch;
                forward.Add(new Vector3(x + lane, y, z));
                back.Add(new Vector3(x - lane, y, origin + (city.blocksPerSide - s) * pitch));
            }
            loops.Add(forward.ToArray());
            loops.Add(back.ToArray());
        }

        return loops;
    }
}
