using UnityEngine;

/// <summary>
/// Classic Input Manager free-fly camera for exploring Neon District.
/// WASD / mouse / Space / Ctrl / Shift / wheel / Escape / R
/// </summary>
[RequireComponent(typeof(Camera))]
public class FreeFlyCamera : MonoBehaviour
{
    [SerializeField] float baseSpeed = 22f;
    [SerializeField] float fastMultiplier = 3.2f;
    [SerializeField] float lookSensitivity = 2.4f;
    [SerializeField] float wheelSpeedStep = 4f;
    [SerializeField] float minSpeed = 4f;
    [SerializeField] float maxSpeed = 120f;

    float _yaw;
    float _pitch;
    bool _cursorLocked = true;
    Vector3 _startPos;
    Quaternion _startRot;
    float _speed;

    void Awake()
    {
        _speed = baseSpeed;
        _startPos = transform.position;
        _startRot = transform.rotation;
        Vector3 e = transform.eulerAngles;
        _yaw = e.y;
        _pitch = e.x;
        SetCursorLock(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            SetCursorLock(!_cursorLocked);

        if (Input.GetKeyDown(KeyCode.R))
        {
            transform.position = _startPos;
            transform.rotation = _startRot;
            Vector3 e = transform.eulerAngles;
            _yaw = e.y;
            _pitch = e.x;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
            _speed = Mathf.Clamp(_speed + scroll * wheelSpeedStep * 10f, minSpeed, maxSpeed);

        if (_cursorLocked)
        {
            _yaw += Input.GetAxis("Mouse X") * lookSensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
            _pitch = Mathf.Clamp(_pitch, -89f, 89f);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        float spd = _speed * (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
            ? fastMultiplier : 1f);

        Vector3 dir = Vector3.zero;
        // Classic Input Manager axes (WASD / arrows already mapped)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        dir += transform.forward * v;
        dir += transform.right * h;
        if (Input.GetKey(KeyCode.Space)) dir += Vector3.up;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) dir -= Vector3.up;

        if (dir.sqrMagnitude > 1f) dir.Normalize();
        transform.position += dir * spd * Time.deltaTime;
    }

    void SetCursorLock(bool locked)
    {
        _cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
