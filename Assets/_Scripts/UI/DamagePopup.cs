using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DamagePopup : MonoBehaviour {
    private Text _text;
    private Vector3 _worldPos;
    private float _duration = 1f;
    private float _riseSpeed = 0.5f;

    public void Init(Vector3 worldPos, AttackResult result) {
        _worldPos = worldPos;

        _text = gameObject.AddComponent<Text>();
        _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _text.fontSize = 24;
        _text.fontStyle = FontStyle.Bold;
        _text.alignment = TextAnchor.MiddleCenter;
        _text.supportRichText = false;

        // Konten & warna
        if (result.miss) {
            _text.text = "MISS!";
            _text.color = UIColorPalette.DamageMiss;
        } else if (result.crit) {
            _text.text = $"CRIT! {result.damage}";
            _text.color = UIColorPalette.DamageCrit;
        } else {
            _text.text = $"-{result.damage}";
            _text.color = UIColorPalette.DamageHit;
        }

        var rect = GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 50);

        // Random offset X
        _worldPos += new Vector3(Random.Range(-0.3f, 0.3f), 0.5f, 0);

        StartCoroutine(Animate());
    }

    IEnumerator Animate() {
        float t = 0;
        while (t < _duration) {
            t += Time.deltaTime;
            float progress = t / _duration;

            // Naik
            Vector3 worldPos = _worldPos + Vector3.up * _riseSpeed * progress;

            // Convert world → screen
            var cam = Camera.main;
            if (cam != null) {
                Vector2 screenPos = cam.WorldToScreenPoint(worldPos);
                var rect = GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.anchoredPosition = screenPos - new Vector2(Screen.width / 2f, Screen.height / 2f);
            }

            // Fade
            if (_text != null) {
                _text.color = new Color(_text.color.r, _text.color.g, _text.color.b, 1f - progress);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
