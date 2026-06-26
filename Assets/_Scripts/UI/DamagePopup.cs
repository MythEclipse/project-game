using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>Floating damage/heal text anchored to world position via HUD Canvas.</summary>
public class DamagePopup : MonoBehaviour {
    Text _text;
    Canvas _hudCanvas;
    RectTransform _canvasRect;
    Vector3 _worldPos;
    float _duration = 1.2f;
    float _riseSpeed = 0.6f;
    Vector2 _drift;

    void Awake() {
        _hudCanvas = UIHelper.FindHUDCanvas();
        if (_hudCanvas != null)
            _canvasRect = _hudCanvas.transform as RectTransform;
    }

    public void Init(Vector3 worldPos, AttackResult result, bool isHeal = false) {
        _worldPos = worldPos;
        if (_hudCanvas == null) { Destroy(gameObject); return; }
        transform.SetParent(_hudCanvas.transform, false);

        var rt = GetComponent<RectTransform>();
        // Anchor di tengah, jadi anchoredPosition (0,0) = center screen
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        _text = gameObject.AddComponent<Text>();
        _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _text.alignment = TextAnchor.MiddleCenter;
        _text.supportRichText = true;

        float refH = 1080f;
        float scale = Screen.height / refH;

        if (isHeal) {
            _text.text = $"+{result.damage}";
            _text.fontSize = Mathf.RoundToInt(24 * scale);
            _text.fontStyle = FontStyle.Bold;
            _text.color = UIColorPalette.DamageHeal;
            rt.sizeDelta = new Vector2(140 * scale, 36 * scale);
            _drift = new Vector2(Random.Range(-20f, 20f), 0);
            _duration = 1.0f;
            _riseSpeed = 0.5f;
            StartCoroutine(Animate());
            return;
        }

        if (result.miss) {
            _text.text = "MISS";
            _text.fontSize = Mathf.RoundToInt(18 * scale);
            _text.fontStyle = FontStyle.Italic;
            _text.color = UIColorPalette.DamageMiss;
            rt.sizeDelta = new Vector2(120 * scale, 28 * scale);
            _drift = new Vector2(Random.Range(-15f, 15f), 0);
        } else if (result.crit) {
            _text.text = $"CRIT! {result.damage}";
            _text.fontSize = Mathf.RoundToInt(30 * scale);
            _text.fontStyle = FontStyle.Bold;
            _text.color = UIColorPalette.DamageCrit;
            rt.sizeDelta = new Vector2(220 * scale, 46 * scale);
            _drift = Vector2.zero;
            _duration = 1.5f;
            _riseSpeed = 0.5f;
        } else {
            _text.text = $"-{result.damage}";
            _text.fontSize = Mathf.RoundToInt(24 * scale);
            _text.fontStyle = FontStyle.Bold;
            _text.color = result.damage <= 3 ? UIColorPalette.DamageMiss : UIColorPalette.DamageHit;
            rt.sizeDelta = new Vector2(140 * scale, 36 * scale);
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

            // Konversi screen-space ke canvas-local coordinates (handle scaling)
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, sp, null, out localPos);
            rt.anchoredPosition = localPos + new Vector2(_drift.x * p, _drift.y);

            if (_text != null) {
                float a = p > 0.5f ? 1f - (p - 0.5f) / 0.5f : 1f;
                _text.color = new Color(_text.color.r, _text.color.g, _text.color.b, a);
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}
