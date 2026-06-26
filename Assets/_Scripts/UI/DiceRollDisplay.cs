using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>Dice roll d20 — compact, scaling otomatis ikut resolusi layar.</summary>
public class DiceRollDisplay : MonoBehaviour {
    Text _d20Txt, _labelTxt, _calcTxt, _dmgTxt;
    Image _panelBg;

    static Canvas GetHUDCanvas() {
        return UIHelper.FindHUDCanvas();
    }

    public void Init(AttackResult r, System.Action onComplete) {
        var canvas = GetHUDCanvas();
        if (canvas == null) { onComplete?.Invoke(); Destroy(gameObject); return; }
        transform.SetParent(canvas.transform, false);

        // Scale factor berdasarkan reference height
        float refH = 1080f;
        float s = Screen.height / refH;
        s = Mathf.Clamp(s, 0.5f, 2f);

        var rt = GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(220 * s, 140 * s);

        _panelBg = gameObject.AddComponent<Image>();
        _panelBg.color = new Color(0.06f, 0.04f, 0.02f, 0.92f);
        _panelBg.raycastTarget = false;

        // D20 number
        var d20go = new GameObject("D20", typeof(Text));
        d20go.transform.SetParent(transform, false);
        var d20rt = d20go.GetComponent<RectTransform>();
        d20rt.anchorMin = new Vector2(0.5f, 1);
        d20rt.anchorMax = new Vector2(0.5f, 1);
        d20rt.pivot = new Vector2(0.5f, 1);
        d20rt.sizeDelta = new Vector2(44 * s, 34 * s);
        d20rt.anchoredPosition = new Vector2(0, -4 * s);
        _d20Txt = d20go.GetComponent<Text>();
        _d20Txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _d20Txt.fontSize = Mathf.RoundToInt(28 * s);
        _d20Txt.fontStyle = FontStyle.Bold;
        _d20Txt.alignment = TextAnchor.MiddleCenter;
        _d20Txt.color = new Color(0.9f, 0.85f, 0.7f);

        // Label HIT/CRIT/MISS
        var lblgo = new GameObject("Label", typeof(Text));
        lblgo.transform.SetParent(transform, false);
        var lrt = lblgo.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0.5f, 0.5f);
        lrt.anchorMax = new Vector2(0.5f, 0.5f);
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.sizeDelta = new Vector2(140 * s, 20 * s);
        lrt.anchoredPosition = new Vector2(0, 12 * s);
        _labelTxt = lblgo.GetComponent<Text>();
        _labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _labelTxt.fontSize = Mathf.RoundToInt(16 * s);
        _labelTxt.fontStyle = FontStyle.Bold;
        _labelTxt.alignment = TextAnchor.MiddleCenter;
        _labelTxt.supportRichText = true;

        // Calculation breakdown
        var calcgo = new GameObject("Calc", typeof(Text));
        calcgo.transform.SetParent(transform, false);
        var crt = calcgo.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(140 * s, 16 * s);
        crt.anchoredPosition = new Vector2(0, -6 * s);
        _calcTxt = calcgo.GetComponent<Text>();
        _calcTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _calcTxt.fontSize = Mathf.RoundToInt(11 * s);
        _calcTxt.alignment = TextAnchor.MiddleCenter;
        _calcTxt.color = UIColorPalette.ParchmentDark;
        _calcTxt.supportRichText = true;

        // Damage text
        var dmgGo = new GameObject("Damage", typeof(Text));
        dmgGo.transform.SetParent(transform, false);
        var drt = dmgGo.GetComponent<RectTransform>();
        drt.anchorMin = new Vector2(0.5f, 0);
        drt.anchorMax = new Vector2(0.5f, 0);
        drt.pivot = new Vector2(0.5f, 0);
        drt.sizeDelta = new Vector2(140 * s, 18 * s);
        drt.anchoredPosition = new Vector2(0, 6 * s);
        _dmgTxt = dmgGo.GetComponent<Text>();
        _dmgTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _dmgTxt.fontSize = Mathf.RoundToInt(12 * s);
        _dmgTxt.fontStyle = FontStyle.Bold;
        _dmgTxt.alignment = TextAnchor.MiddleCenter;
        _dmgTxt.supportRichText = true;

        StartCoroutine(AnimateDice(r, onComplete));
    }

    IEnumerator AnimateDice(AttackResult r, System.Action onComplete) {
        float elapsed = 0f;
        float cycleTime = 0.8f;

        while (elapsed < cycleTime) {
            elapsed += Time.deltaTime;
            int shown;
            if (cycleTime - elapsed < 0.12f)
                shown = Mathf.Clamp(r.attackRoll + Random.Range(-2, 3), 1, 20);
            else
                shown = Random.Range(1, 21);
            _d20Txt.text = shown.ToString();
            float pulse = 1f + Mathf.Sin(elapsed * 50f) * 0.06f;
            _d20Txt.transform.localScale = new Vector3(pulse, pulse, 1);
            yield return null;
        }

        _d20Txt.text = r.attackRoll.ToString();
        _d20Txt.transform.localScale = Vector3.one;

        if (r.miss) { _labelTxt.text = "MISS!"; _labelTxt.color = UIColorPalette.DamageMiss; }
        else if (r.crit) { _labelTxt.text = "CRIT!"; _labelTxt.color = UIColorPalette.DamageCrit; }
        else { _labelTxt.text = "HIT!"; _labelTxt.color = Color.white; }

        if (r.miss) _calcTxt.text = "Natural 1";
        else if (r.crit) _calcTxt.text = "Natural 20!";
        else _calcTxt.text = $"{r.attackRoll}+{r.attackBonus}={r.totalAttack} vs {r.targetAC}";

        if (r.miss) _dmgTxt.text = "";
        else if (r.crit) _dmgTxt.text = $"<color=yellow>{r.damage}!</color>";
        else _dmgTxt.text = $"{r.damage} dmg";

        float be = 0f;
        while (be < 0.25f) {
            be += Time.deltaTime;
            float s = 1f + (1f - be / 0.25f) * 0.12f;
            _d20Txt.transform.localScale = new Vector3(s, s, 1);
            yield return null;
        }
        _d20Txt.transform.localScale = Vector3.one;

        yield return new WaitForSeconds(0.4f);

        float fe = 0f;
        while (fe < 0.25f) {
            fe += Time.deltaTime;
            float a = 1f - fe / 0.25f;
            _panelBg.color = new Color(0.06f, 0.04f, 0.02f, 0.92f * a);
            foreach (var txt in new[] { _d20Txt, _labelTxt, _calcTxt, _dmgTxt })
                if (txt != null) txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, a);
            yield return null;
        }
        onComplete?.Invoke();
        Destroy(gameObject);
    }
}
