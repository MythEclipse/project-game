using UnityEngine;

/// <summary>
/// Simple camera controller — mengikuti target dengan lerp.
/// Alternatif: bisa pakai Cinemachine nanti untuk smooth follow.
/// </summary>
public class CameraController : MonoBehaviour {
    [Header("Follow")]
    [SerializeField] private Transform _target;
    [SerializeField] private float _followSpeed = 5f;

    [Header("Zoom")]
    [SerializeField] private float _zoomSpeed = 2f;
    [SerializeField] private float _minSize = 3f;
    [SerializeField] private float _maxSize = 15f;

    private Camera _cam;

    void Start() {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;
    }

    void LateUpdate() {
        if (_target == null) return;
        if (_cam == null) return;

        Vector3 targetPos = new Vector3(_target.position.x, _target.position.y, _cam.transform.position.z);
        _cam.transform.position = Vector3.Lerp(_cam.transform.position, targetPos, Time.deltaTime * _followSpeed);
    }

    public void SetFollowTarget(Transform target) {
        _target = target;
    }

    void Update() {
        // Scroll wheel zoom via Input System
        float scroll = 0f;
        if (UnityEngine.InputSystem.Mouse.current != null) {
            scroll = UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y * 0.01f;
        }
        if (Mathf.Abs(scroll) > 0.01f && _cam != null && _cam.orthographic) {
            _cam.orthographicSize = Mathf.Clamp(
                _cam.orthographicSize - scroll * _zoomSpeed,
                _minSize,
                _maxSize
            );
        }
    }
}
