using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>Floating damage/heal text — di-parent ke HUD Canvas biar scaling otomatis.</summary>
public class DamagePopup : MonoBehaviour {
    Text _text;
    Canvas _hudCanvas;
    Vector3 _worldPos;
    float _duration = 1.2f;
    float _riseSpeed = 0.6f;
    Vector2 _drift;

    void Awake() {
        _hudCanvas = GetHUDCanvas();
    }

    static Canvas GetHUDCanvas() {
        foreach (var c in FindObjectsOfType<Canvas>())
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) return c;
        return null;
    }

    public void Init(Vector3 worldPos, AttackResult result, bool isHeal = false) {
        _worldPos = worldPos;
        if (_hudCanvas == null) { Destroy(gameObject); return; }
        transform.SetParent(_hudCanvas.transform, false);

        var rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);

        _text = gameObject.AddComponent<Text>();
        _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _text.alignment = TextAnchor.MiddleCenter;
        _text.supportRichText = true;

        // Scale font size based on reference height
        float refH = 1080f;
        float scale = (_hudCanvas.GetComponent<CanvasScaler>()?.referenceResolution.y ?? refH) / refH;
        // Actual screen size factor
        float screenScale = Screen.height / refH;

        if (isHeal) {
            _text.text = $"+{result.damage}";
            _text.fontSize = Mathf.RoundToInt(26 * screenScale);
            _text.fontStyle = FontStyle.Bold;
            _text.color = UIColorPalette.DamageHeal;
            rt.sizeDelta = new Vector2(160 * screenScale, 40 * screenScale);
            _drift = new Vector2(Random.Range(-20f, 20f), 0);
            _duration = 1.0f;
            _riseSpeed = 0.5f;
            StartCoroutine(Animate());
            return;
        }

        if (result.miss) {
            _text.text = "MISS";
            _text.fontSize = Mathf.RoundToInt(18 * screenScale);
            _text.fontStyle = FontStyle.Italic;
            _text.color = UIColorPalette.DamageMiss;
            rt.sizeDelta = new Vector2(140 * screenScale, 30 * screenScale);
            _drift = new Vector2(Random.Range(-15f, 15f), 0);
        } else if (result.crit) {
            _text.text = $"CRIT! {result.damage}";
            _text.fontSize = Mathf.RoundToInt(32 * screenScale);
            _text.fontStyle = FontStyle.Bold;
            _text.color = UIColorPalette.DamageCrit;
            rt.sizeDelta = new Vector2(240 * screenScale, 50 * screenScale);
            _drift = Vector2.zero;
            _duration = 1.5f;
            _riseSpeed = 0.5f;
        } else {
            _text.text = $"-{result.damage}";
            _text.fontSize = Mathf.RoundToInt(26 * screenScale);
            _text.fontStyle = FontStyle.Bold;
            _text.color = result.damage <= 3 ? UIColorPalette.DamageMiss : UIColorPalette.DamageHit;
            rt.sizeDelta = new Vector2(160 * screenScale, 40 * screenScale);
            _drift = new Vector2(Random.Range(-30f, 30f), 0);
        }

        _worldPos += new Vector3(Random.Range(-0.3f, 0.3f), 0.2f, 0);
        StartCoroutine(Animate());
    }

    IEnumerator Animate() {
        // Bounce in
        transform.localScale = Vector3.zero;
        float t = 0;
        while (t < 0.15f) {
            t += Time.deltaTime;
            float s = Mathf.SmoothStep(0, 1, t / 0.15f);
            transform.localScale = new Vector3(s * 1.1f, s * 1.1f, 1);
            yield return null;
        }
        transform.localScale = Vector3.one;

        // Float & fade
        t = 0;
        while (t < _duration) {
            t += Time.deltaTime;
            float p = t / _duration;
            Vector3 w = _worldPos + Vector3.up * _riseSpeed * p;
            Vector3 sp = Camera.main.WorldToScreenPoint(w);
            var rt = GetComponent<RectTransform>();
            rt.anchoredPosition = (Vector2)sp - new Vector2(Screen.width / 2f, Screen.height / 2f)
                + new Vector2(_drift.x * p, _drift.y);

            if (_text != null) {
                float a = p > 0.5f ? 1f - (p - 0.5f) / 0.5f : 1f;
                _text.color = new Color(_text.color.r, _text.color.g, _text.color.b, a);
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}
