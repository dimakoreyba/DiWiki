using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Deterministic procedural cyberpunk city (~200×200, ~12×12 blocks).
/// Roads, sidewalks, buildings, landmarks, rooftops.
/// </summary>
public class CityGenerator : MonoBehaviour
{
    [Header("City Layout")]
    public int seed = 1337;
    public float citySize = 200f;
    public int blocksPerSide = 12;
    public float roadWidth = 8f;
    public float sidewalkWidth = 2.2f;
    public float groundY = 0f;

    [Header("References")]
    public BuildingGenerator buildingGenerator;
    public TrafficController trafficController;
    public Transform cityRoot;

    readonly List<Vector3> _laneWaypoints = new List<Vector3>();
    readonly List<Renderer> _roadRenderers = new List<Renderer>();

    public IReadOnlyList<Vector3> LaneWaypoints => _laneWaypoints;
    public IReadOnlyList<Renderer> RoadRenderers => _roadRenderers;

    public void Generate()
    {
        CityMaterials.EnsureCreated();
        ClearCity();

        if (cityRoot == null)
        {
            var rootGo = new GameObject("NeonDistrict_City");
            cityRoot = rootGo.transform;
            cityRoot.SetParent(transform, false);
        }

        if (buildingGenerator == null)
            buildingGenerator = GetComponent<BuildingGenerator>() ?? gameObject.AddComponent<BuildingGenerator>();

        Random.InitState(seed);
        var rng = new System.Random(seed);

        float usable = citySize;
        float blockPitch = usable / blocksPerSide;
        float lotSize = blockPitch - roadWidth;
        float origin = -citySize * 0.5f;

        BuildGround(origin);
        BuildRoadGrid(origin, blockPitch, lotSize);
        BuildBlocks(origin, blockPitch, lotSize, rng);
        BuildLaneNetwork(origin, blockPitch);

        if (trafficController != null)
            trafficController.Initialize(this);
    }

    public void ClearCity()
    {
        _laneWaypoints.Clear();
        _roadRenderers.Clear();

        if (cityRoot != null)
        {
            for (int i = cityRoot.childCount - 1; i >= 0; i--)
            {
                var child = cityRoot.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }
    }

    void BuildGround(float origin)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "GroundPlane";
        ground.transform.SetParent(cityRoot, false);
        ground.transform.position = new Vector3(0f, groundY - 0.25f, 0f);
        ground.transform.localScale = new Vector3(citySize + 40f, 0.5f, citySize + 40f);
        var r = ground.GetComponent<Renderer>();
        r.sharedMaterial = CityMaterials.Asphalt;
    }

    void BuildRoadGrid(float origin, float blockPitch, float lotSize)
    {
        var roads = new GameObject("Roads");
        roads.transform.SetParent(cityRoot, false);

        // Horizontal roads (along X)
        for (int i = 0; i <= blocksPerSide; i++)
        {
            float z = origin + i * blockPitch;
            CreateRoadSegment(roads.transform,
                new Vector3(0f, groundY + 0.02f, z),
                new Vector3(citySize + roadWidth, 0.08f, roadWidth));
        }

        // Vertical roads (along Z)
        for (int i = 0; i <= blocksPerSide; i++)
        {
            float x = origin + i * blockPitch;
            CreateRoadSegment(roads.transform,
                new Vector3(x, groundY + 0.02f, 0f),
                new Vector3(roadWidth, 0.08f, citySize + roadWidth));
        }
    }

    void CreateRoadSegment(Transform parent, Vector3 pos, Vector3 scale)
    {
        var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "Road";
        road.transform.SetParent(parent, false);
        road.transform.position = pos;
        road.transform.localScale = scale;
        var r = road.GetComponent<Renderer>();
        r.sharedMaterial = CityMaterials.Asphalt;
        _roadRenderers.Add(r);

        // Center line neon dashes
        bool alongX = scale.x >= scale.z;
        float len = alongX ? scale.x : scale.z;
        int dashes = Mathf.Max(4, Mathf.FloorToInt(len / 12f));
        for (int d = 0; d < dashes; d++)
        {
            if ((d & 1) == 0) continue;
            var dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dash.name = "LaneMark";
            dash.transform.SetParent(road.transform, false);
            float t = (d + 0.5f) / dashes - 0.5f;
            if (alongX)
            {
                dash.transform.localPosition = new Vector3(t, 0.6f, 0f);
                dash.transform.localScale = new Vector3(0.35f / scale.x * len / dashes, 0.15f, 0.08f / scale.z * roadWidth);
            }
            else
            {
                dash.transform.localPosition = new Vector3(0f, 0.6f, t);
                dash.transform.localScale = new Vector3(0.08f / scale.x * roadWidth, 0.15f, 0.35f / scale.z * len / dashes);
            }
            var dr = dash.GetComponent<Renderer>();
            dr.sharedMaterial = CityMaterials.NeonCyan;
            var col = dash.GetComponent<Collider>();
            if (col)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
        }
    }

    void BuildBlocks(float origin, float blockPitch, float lotSize, System.Random rng)
    {
        var blocks = new GameObject("Blocks");
        blocks.transform.SetParent(cityRoot, false);

        // Landmark near center
        int landmarkI = blocksPerSide / 2;
        int landmarkJ = blocksPerSide / 2;

        for (int j = 0; j < blocksPerSide; j++)
        {
            for (int i = 0; i < blocksPerSide; i++)
            {
                float cx = origin + i * blockPitch + blockPitch * 0.5f;
                float cz = origin + j * blockPitch + blockPitch * 0.5f;
                var blockRoot = new GameObject($"Block_{i}_{j}");
                blockRoot.transform.SetParent(blocks.transform, false);
                blockRoot.transform.position = new Vector3(cx, groundY, cz);

                // Sidewalk slab
                float walk = lotSize;
                var sidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sidewalk.name = "Sidewalk";
                sidewalk.transform.SetParent(blockRoot.transform, false);
                sidewalk.transform.localPosition = new Vector3(0f, 0.06f, 0f);
                sidewalk.transform.localScale = new Vector3(walk, 0.12f, walk);
                sidewalk.GetComponent<Renderer>().sharedMaterial = CityMaterials.Sidewalk;

                bool landmark = i == landmarkI && j == landmarkJ;
                int localSeed = seed ^ (i * 73856093) ^ (j * 19349663);

                // Subdivide block into 1–4 buildings
                int div = landmark ? 1 : 1 + rng.Next(0, 3);
                PlaceBuildingsInLot(blockRoot.transform, walk - sidewalkWidth * 0.5f, div, localSeed, landmark);
            }
        }
    }

    void PlaceBuildingsInLot(Transform block, float lot, int divisions, int localSeed, bool landmark)
    {
        var rng = new System.Random(localSeed);
        float pad = 0.8f;
        float usable = lot - pad * 2f;

        if (landmark || divisions <= 1)
        {
            float w = usable * Mathf.Lerp(0.55f, 0.85f, (float)rng.NextDouble());
            float d = usable * Mathf.Lerp(0.55f, 0.85f, (float)rng.NextDouble());
            buildingGenerator.BuildTower(block, new Vector3(0f, 0f, 0f), new Vector2(w, d), localSeed, landmark);
            return;
        }

        // 2x2-ish packing
        int nx = divisions >= 3 ? 2 : (rng.NextDouble() < 0.5 ? 2 : 1);
        int nz = divisions >= 2 ? 2 : 1;
        float cellW = usable / nx;
        float cellD = usable / nz;

        int idx = 0;
        for (int z = 0; z < nz; z++)
        {
            for (int x = 0; x < nx; x++)
            {
                float fw = cellW * Mathf.Lerp(0.65f, 0.9f, (float)rng.NextDouble());
                float fd = cellD * Mathf.Lerp(0.65f, 0.9f, (float)rng.NextDouble());
                float px = -usable * 0.5f + cellW * (x + 0.5f);
                float pz = -usable * 0.5f + cellD * (z + 0.5f);
                buildingGenerator.BuildTower(block, new Vector3(px, 0f, pz), new Vector2(fw, fd),
                    localSeed + idx * 97, false);
                idx++;
            }
        }
    }

    void BuildLaneNetwork(float origin, float blockPitch)
    {
        // Two-way lane offsets from road center
        float laneOffset = roadWidth * 0.22f;
        _laneWaypoints.Clear();

        // Horizontal corridors (drive +X and -X)
        for (int i = 0; i <= blocksPerSide; i++)
        {
            float z = origin + i * blockPitch;
            for (int s = 0; s <= blocksPerSide; s++)
            {
                float x = origin + s * blockPitch;
                _laneWaypoints.Add(new Vector3(x, groundY + 0.4f, z + laneOffset));
                _laneWaypoints.Add(new Vector3(x, groundY + 0.4f, z - laneOffset));
            }
        }

        // Vertical corridors
        for (int i = 0; i <= blocksPerSide; i++)
        {
            float x = origin + i * blockPitch;
            for (int s = 0; s <= blocksPerSide; s++)
            {
                float z = origin + s * blockPitch;
                _laneWaypoints.Add(new Vector3(x + laneOffset, groundY + 0.4f, z));
                _laneWaypoints.Add(new Vector3(x - laneOffset, groundY + 0.4f, z));
            }
        }
    }

    public void ApplyWetRoads(bool wet)
    {
        CityMaterials.EnsureCreated();
        var mat = wet ? CityMaterials.WetRoad : CityMaterials.Asphalt;
        for (int i = 0; i < _roadRenderers.Count; i++)
        {
            if (_roadRenderers[i] != null)
                _roadRenderers[i].sharedMaterial = mat;
        }
    }
}
