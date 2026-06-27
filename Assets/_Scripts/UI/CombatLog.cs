using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Scrollable combat log — menampilkan riwayat attack, heal, spell.</summary>
public class CombatLog : MonoBehaviour {
    static CombatLog _instance;
    public static CombatLog Instance => _instance;

    Text _logText;
    List<string> _entries = new List<string>();
    const int MaxEntries = 50;

    void Awake() {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    public void Init(Canvas parentCanvas) {
        // Side panel bawah — anchor di kanan, di bawah side panel unit info
        var rt = GetComponent<RectTransform>();
        rt.SetParent(parentCanvas.transform, false);
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 0.35f);
        rt.pivot = new Vector2(1, 0);
        rt.sizeDelta = new Vector2(230, 0);
        rt.anchoredPosition = new Vector2(-6, 92);

        var bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.04f, 0.02f, 0.9f);
        bg.raycastTarget = false;

        // Scroll view
        var scrollGo = new GameObject("ScrollView", typeof(ScrollRect));
        var srt = scrollGo.GetComponent<RectTransform>();
        srt.SetParent(transform, false);
        srt.anchorMin = Vector2.zero;
        srt.anchorMax = Vector2.one;
        srt.offsetMin = Vector2.zero;
        srt.offsetMax = Vector2.zero;

        var sr = scrollGo.GetComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        var vpGo = new GameObject("Viewport", typeof(RectTransform));
        var vprt = vpGo.GetComponent<RectTransform>();
        vprt.SetParent(srt, false);
        vprt.anchorMin = Vector2.zero;
        vprt.anchorMax = Vector2.one;
        vprt.offsetMin = new Vector2(4, 4);
        vprt.offsetMax = new Vector2(-4, -4);
        sr.viewport = vprt;
        vpGo.AddComponent<Image>().color = Color.clear;
        vpGo.AddComponent<Mask>().showMaskGraphic = false;

        // Content
        var cGo = new GameObject("Content", typeof(RectTransform));
        var crt = cGo.GetComponent<RectTransform>();
        crt.SetParent(vprt, false);
        crt.anchorMin = new Vector2(0, 1);
        crt.anchorMax = new Vector2(1, 1);
        crt.pivot = new Vector2(0.5f, 1);
        crt.sizeDelta = new Vector2(0, 0);
        sr.content = crt;

        // Text label
        var tGo = new GameObject("LogText", typeof(Text));
        var trt = tGo.GetComponent<RectTransform>();
        trt.SetParent(crt, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.pivot = new Vector2(0, 1);
        trt.sizeDelta = Vector2.zero;
        trt.anchoredPosition = Vector2.zero;

        _logText = tGo.GetComponent<Text>();
        _logText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _logText.fontSize = 11;
        _logText.color = UIColorPalette.ParchmentLight;
        _logText.alignment = TextAnchor.UpperLeft;
        _logText.supportRichText = true;
        _logText.raycastTarget = false;
        _logText.horizontalOverflow = HorizontalWrapMode.Wrap;

        var csf = tGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        AddEntry("<color=#888>⚔ Battle started</color>");
    }

    public void AddEntry(string text) {
        _entries.Add(text);
        if (_entries.Count > MaxEntries) _entries.RemoveAt(0);

        _logText.text = string.Join("\n", _entries);

        // Auto-scroll ke bawah
        var scrollRect = GetComponentInChildren<ScrollRect>();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    /// <summary>Clear all log entries (on new battle).</summary>
    public void Clear() {
        _entries.Clear();
        _logText.text = "";
        AddEntry("<color=#888>⚔ New battle started</color>");
    }

    public void LogAttack(Unit attacker, Unit defender, AttackResult result) {
        string txt;
        if (result.miss)
            txt = $"<color=#aaa>{attacker.Data?.unitName}</color> → <color=#aaa>{defender.Data?.unitName}</color>: <color=#888>MISS</color>";
        else if (result.crit)
            txt = $"<color=#fc3>{attacker.Data?.unitName}</color> → <color=#f55>{defender.Data?.unitName}</color>: <color=#ff0>CRIT {result.damage}!</color>";
        else
            txt = $"<color=#ccc>{attacker.Data?.unitName}</color> → <color=#f55>{defender.Data?.unitName}</color>: <color=#fff>{result.damage} dmg</color>";
        AddEntry(txt);
    }

    public void LogHeal(Unit healer, Unit target, int amount) {
        AddEntry($"<color=#3f3>{healer.Data?.unitName}</color> heals <color=#3f3>{target.Data?.unitName}</color>: +{amount} HP");
    }

    public void LogSpell(Unit caster, string spellName) {
        AddEntry($"<color=#c6f>{caster.Data?.unitName}</color> casts <color=#c6f>{spellName}</color>");
    }

    public void Log(string text) {
        AddEntry($"<color=#888>{text}</color>");
    }

    void OnDestroy() {
        if (_instance == this) _instance = null;
    }
}
