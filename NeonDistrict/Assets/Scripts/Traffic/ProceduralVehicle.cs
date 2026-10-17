using UnityEngine;

/// <summary>
/// Procedural vehicle with emissive head/taillights; follows lane waypoints.
/// </summary>
public class ProceduralVehicle : MonoBehaviour
{
    public float speed = 12f;
    public float turnSpeed = 4.5f;
    public float arriveDistance = 1.5f;

    Vector3[] _path;
    int _index;
    bool _loop = true;
    bool _ready;

    public void Setup(Vector3[] path, int startIndex, float moveSpeed, int seed)
    {
        _path = path;
        _index = Mathf.Clamp(startIndex, 0, path.Length - 1);
        speed = moveSpeed;
        BuildMesh(seed);
        if (_path.Length > 0)
            transform.position = _path[_index];
        _ready = true;
    }

    void BuildMesh(int seed)
    {
        CityMaterials.EnsureCreated();
        var rng = new System.Random(seed);

        float len = Mathf.Lerp(3.2f, 5.2f, (float)rng.NextDouble());
        float wid = Mathf.Lerp(1.5f, 2.1f, (float)rng.NextDouble());
        float h = Mathf.Lerp(1.1f, 1.7f, (float)rng.NextDouble());

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(transform, false);
        body.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
        body.transform.localScale = new Vector3(wid, h, len);
        var br = body.GetComponent<Renderer>();
        // Tint via instance material for variety
        var bodyMat = new Material(CityMaterials.VehicleBody);
        Color[] palette =
        {
            new Color(0.12f, 0.14f, 0.2f),
            new Color(0.2f, 0.08f, 0.14f),
            new Color(0.08f, 0.16f, 0.18f),
            new Color(0.18f, 0.16f, 0.1f)
        };
        bodyMat.color = palette[Mathf.Abs(seed) % palette.Length];
        br.sharedMaterial = bodyMat;
        DestroyCollider(body);

        var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.name = "Cabin";
        cabin.transform.SetParent(transform, false);
        cabin.transform.localPosition = new Vector3(0f, h * 0.85f, len * 0.05f);
        cabin.transform.localScale = new Vector3(wid * 0.85f, h * 0.55f, len * 0.45f);
        cabin.GetComponent<Renderer>().sharedMaterial = CityMaterials.WindowEmissive;
        DestroyCollider(cabin);

        // Headlights (front = +Z)
        CreateLightQuad("HeadL", new Vector3(-wid * 0.3f, h * 0.35f, len * 0.51f),
            new Vector3(0.35f, 0.2f, 0.08f), CityMaterials.Headlight);
        CreateLightQuad("HeadR", new Vector3(wid * 0.3f, h * 0.35f, len * 0.51f),
            new Vector3(0.35f, 0.2f, 0.08f), CityMaterials.Headlight);

        // Taillights
        CreateLightQuad("TailL", new Vector3(-wid * 0.3f, h * 0.4f, -len * 0.51f),
            new Vector3(0.4f, 0.18f, 0.08f), CityMaterials.Taillight);
        CreateLightQuad("TailR", new Vector3(wid * 0.3f, h * 0.4f, -len * 0.51f),
            new Vector3(0.4f, 0.18f, 0.08f), CityMaterials.Taillight);

        // Underglow accent
        if (rng.NextDouble() < 0.5)
        {
            var glow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glow.name = "Underglow";
            glow.transform.SetParent(transform, false);
            glow.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            glow.transform.localScale = new Vector3(wid * 1.05f, 0.06f, len * 0.9f);
            glow.GetComponent<Renderer>().sharedMaterial = CityMaterials.NeonByIndex(seed);
            DestroyCollider(glow);
        }
    }

    void CreateLightQuad(string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        DestroyCollider(go);
    }

    static void DestroyCollider(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col == null) return;
        if (Application.isPlaying) Object.Destroy(col);
        else Object.DestroyImmediate(col);
    }

    void Update()
    {
        if (!_ready || _path == null || _path.Length == 0) return;

        Vector3 target = _path[_index];
        Vector3 flat = transform.position;
        flat.y = target.y;
        Vector3 to = target - flat;
        float dist = to.magnitude;

        if (dist < arriveDistance)
        {
            _index++;
            if (_index >= _path.Length)
            {
                if (_loop) _index = 0;
                else { _ready = false; return; }
            }
            target = _path[_index];
            to = target - flat;
        }

        if (to.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        transform.position += transform.forward * speed * Time.deltaTime;
        var p = transform.position;
        p.y = target.y;
        transform.position = p;
    }
}
