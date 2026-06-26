using UnityEngine;

public class CameraController : MonoBehaviour {
    [Header("Follow")]
    [SerializeField] private Transform _target;
    [SerializeField] private float _followSpeed = 6f;

    [Header("Zoom")]
    [SerializeField] private float _zoomSpeed = 2f;
    [SerializeField] private float _minSize = 3f;
    [SerializeField] private float _maxSize = 15f;

    private Camera _cam;
    private Vector3 _shakeOffset;
    private float _shakeIntensity;
    private float _shakeDuration;
    private Vector3 _originalPos;

    void Start() {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;
        _originalPos = transform.localPosition;
    }

    void LateUpdate() {
        if (_target != null && _cam != null) {
            Vector3 targetPos = new Vector3(
                _target.position.x,
                _target.position.y,
                _cam.transform.position.z
            );
            _cam.transform.position = Vector3.Lerp(
                _cam.transform.position,
                targetPos + _shakeOffset,
                Time.deltaTime * _followSpeed
            );
        }

        // Screen shake decay
        if (_shakeDuration > 0) {
            _shakeDuration -= Time.deltaTime;
            if (_shakeDuration <= 0) {
                _shakeOffset = Vector3.zero;
                _shakeDuration = 0;
            } else {
                _shakeOffset = new Vector3(
                    Random.Range(-1f, 1f) * _shakeIntensity * _shakeDuration,
                    Random.Range(-1f, 1f) * _shakeIntensity * _shakeDuration,
                    0
                );
            }
        }
    }

    void Update() {
        float scroll = 0f;
        if (UnityEngine.InputSystem.Mouse.current != null) {
            scroll = UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y * 0.01f;
        }
        if (Mathf.Abs(scroll) > 0.01f && _cam != null && _cam.orthographic) {
            _cam.orthographicSize = Mathf.Clamp(
                _cam.orthographicSize - scroll * _zoomSpeed,
                _minSize, _maxSize
            );
        }
    }

    public void SetFollowTarget(Transform target) {
        _target = target;
    }

    public void Shake(float intensity = 0.1f, float duration = 0.2f) {
        _shakeIntensity = intensity;
        _shakeDuration = duration;
    }
}
