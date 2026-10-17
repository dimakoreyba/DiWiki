using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural buildings with emissive window quads (no per-window Lights).
/// Windows are batched per-facade into a single mesh for performance.
/// </summary>
public class BuildingGenerator : MonoBehaviour
{
    [SerializeField] int maxFloors = 28;
    [SerializeField] int minFloors = 3;
    [SerializeField] float floorHeight = 3.2f;
    [SerializeField] float windowChance = 0.72f;

    public GameObject BuildTower(
        Transform parent,
        Vector3 center,
        Vector2 footprint,
        int seed,
        bool isLandmark = false)
    {
        CityMaterials.EnsureCreated();
        var rng = new System.Random(seed);

        int floors = isLandmark
            ? maxFloors + rng.Next(8, 18)
            : rng.Next(minFloors, maxFloors + 1);

        float height = floors * floorHeight;
        var root = new GameObject(isLandmark ? "Landmark" : "Building");
        root.transform.SetParent(parent, false);
        root.transform.position = center;

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
        body.transform.localScale = new Vector3(footprint.x, height, footprint.y);
        ApplyMat(body, isLandmark ? CityMaterials.Landmark : CityMaterials.Building);

        if (isLandmark)
        {
            float tipH = height * 0.22f;
            var tip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tip.name = "Spire";
            tip.transform.SetParent(root.transform, false);
            tip.transform.localPosition = new Vector3(0f, height + tipH * 0.5f, 0f);
            tip.transform.localScale = new Vector3(footprint.x * 0.35f, tipH, footprint.y * 0.35f);
            ApplyMat(tip, CityMaterials.Building);

            var neonRing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            neonRing.name = "NeonBand";
            neonRing.transform.SetParent(root.transform, false);
            neonRing.transform.localPosition = new Vector3(0f, height * 0.65f, 0f);
            neonRing.transform.localScale = new Vector3(footprint.x * 1.05f, 0.45f, footprint.y * 1.05f);
            ApplyMat(neonRing, CityMaterials.NeonByIndex(seed));
            DestroyCollider(neonRing);
        }

        AddRooftop(root.transform, footprint, height, rng);
        AddWindowsBatched(root.transform, footprint, floors, floorHeight, seed);
        if (rng.NextDouble() < 0.45 || isLandmark)
            AddNeonStrips(root.transform, footprint, height, seed, rng);

        return root;
    }

    void AddRooftop(Transform root, Vector2 footprint, float height, System.Random rng)
    {
        int props = rng.Next(1, 4);
        for (int i = 0; i < props; i++)
        {
            var prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prop.name = "RoofProp";
            prop.transform.SetParent(root, false);
            float pw = Mathf.Lerp(1.2f, footprint.x * 0.35f, (float)rng.NextDouble());
            float pd = Mathf.Lerp(1.2f, footprint.y * 0.35f, (float)rng.NextDouble());
            float ph = Mathf.Lerp(1.5f, 4.5f, (float)rng.NextDouble());
            float ox = (float)(rng.NextDouble() - 0.5) * (footprint.x - pw) * 0.7f;
            float oz = (float)(rng.NextDouble() - 0.5) * (footprint.y - pd) * 0.7f;
            prop.transform.localPosition = new Vector3(ox, height + ph * 0.5f, oz);
            prop.transform.localScale = new Vector3(pw, ph, pd);
            ApplyMat(prop, CityMaterials.Building);

            if (rng.NextDouble() < 0.4)
            {
                var dish = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                dish.name = "Antenna";
                dish.transform.SetParent(root, false);
                dish.transform.localPosition = new Vector3(ox, height + ph + 1.2f, oz);
                dish.transform.localScale = new Vector3(0.25f, 1.2f, 0.25f);
                ApplyMat(dish, CityMaterials.NeonByIndex(rng.Next()));
                DestroyCollider(dish);
            }
        }
    }

    void AddWindowsBatched(Transform root, Vector2 footprint, int floors, float floorH, int seed)
    {
        float wx = 1.0f;
        float wy = 1.35f;
        float inset = 0.07f;
        var faces = new[]
        {
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 0f, -1f),
            new Vector3(1f, 0f, 0f),
            new Vector3(-1f, 0f, 0f)
        };

        // Two material buckets to avoid thousands of unique materials
        var coolVerts = new List<Vector3>();
        var coolTris = new List<int>();
        var warmVerts = new List<Vector3>();
        var warmTris = new List<int>();

        for (int f = 0; f < faces.Length; f++)
        {
            Vector3 n = faces[f];
            bool alongX = Mathf.Abs(n.z) > 0.5f;
            float faceWidth = alongX ? footprint.x : footprint.y;
            float halfOther = (alongX ? footprint.y : footprint.x) * 0.5f + inset;
            int cols = Mathf.Max(1, Mathf.FloorToInt(faceWidth / 2.6f));
            float spacing = faceWidth / (cols + 1);

            for (int floor = 1; floor < floors; floor++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int h = Hash(seed, f, floor, c);
                    if ((h & 255) / 255f > windowChance) continue;

                    float along = -faceWidth * 0.5f + spacing * (c + 1);
                    Vector3 pos = alongX
                        ? new Vector3(along, floor * floorH + floorH * 0.45f, n.z * halfOther)
                        : new Vector3(n.x * halfOther, floor * floorH + floorH * 0.45f, along);

                    Quaternion rot = Quaternion.LookRotation(-n, Vector3.up);
                    bool warm = ((h >> 8) & 3) == 0;
                    if (warm) AddQuad(warmVerts, warmTris, pos, rot, wx, wy);
                    else AddQuad(coolVerts, coolTris, pos, rot, wx, wy);
                }
            }
        }

        if (coolVerts.Count > 0)
            CreateWindowMesh(root, "WindowsCool", coolVerts, coolTris, CityMaterials.WindowEmissive);
        if (warmVerts.Count > 0)
            CreateWindowMesh(root, "WindowsWarm", warmVerts, warmTris, CityMaterials.NeonOrange);
    }

    static void AddQuad(List<Vector3> verts, List<int> tris, Vector3 center, Quaternion rot, float w, float h)
    {
        int i = verts.Count;
        Vector3 r = rot * new Vector3(w * 0.5f, 0f, 0f);
        Vector3 u = rot * new Vector3(0f, h * 0.5f, 0f);
        verts.Add(center - r - u);
        verts.Add(center + r - u);
        verts.Add(center + r + u);
        verts.Add(center - r + u);
        tris.Add(i); tris.Add(i + 2); tris.Add(i + 1);
        tris.Add(i); tris.Add(i + 3); tris.Add(i + 2);
    }

    static void CreateWindowMesh(Transform root, string name, List<Vector3> verts, List<int> tris, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        var mesh = new Mesh { name = name };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    void AddNeonStrips(Transform root, Vector2 footprint, float height, int seed, System.Random rng)
    {
        int strips = rng.Next(1, 3);
        for (int i = 0; i < strips; i++)
        {
            bool onX = rng.NextDouble() < 0.5;
            var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = "NeonStrip";
            strip.transform.SetParent(root, false);
            float h = height * Mathf.Lerp(0.4f, 0.9f, (float)rng.NextDouble());
            if (onX)
            {
                float z = (rng.NextDouble() < 0.5 ? -1f : 1f) * (footprint.y * 0.5f + 0.08f);
                float x = (float)(rng.NextDouble() - 0.5) * footprint.x * 0.8f;
                strip.transform.localPosition = new Vector3(x, h * 0.5f, z);
                strip.transform.localScale = new Vector3(0.2f, h, 0.12f);
            }
            else
            {
                float x = (rng.NextDouble() < 0.5 ? -1f : 1f) * (footprint.x * 0.5f + 0.08f);
                float z = (float)(rng.NextDouble() - 0.5) * footprint.y * 0.8f;
                strip.transform.localPosition = new Vector3(x, h * 0.5f, z);
                strip.transform.localScale = new Vector3(0.12f, h, 0.2f);
            }
            ApplyMat(strip, CityMaterials.NeonByIndex(seed + i));
            DestroyCollider(strip);
        }
    }

    static void ApplyMat(GameObject go, Material mat)
    {
        var r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }

    static void DestroyCollider(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col == null) return;
        if (Application.isPlaying) Object.Destroy(col);
        else Object.DestroyImmediate(col);
    }

    static int Hash(int a, int b, int c, int d)
    {
        unchecked
        {
            int h = a;
            h = (h * 73856093) ^ b;
            h = (h * 19349663) ^ c;
            h = (h * 83492791) ^ d;
            return h;
        }
    }
}
