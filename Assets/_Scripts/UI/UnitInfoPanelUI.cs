using UnityEngine;
using UnityEngine.UI;

public class UnitInfoPanelUI : MonoBehaviour {
    private GameObject _root;
    private Text _nameLabel, _classLabel, _acLabel, _hpText;
    private Image _hpBarFill;
    private Text[] _statLabels;

    void Awake() {
        CreateUI();
    }

    void CreateUI() {
        _root = UIFactory.CreatePanel(transform, new Vector2(240, 300),
            new Color(0.14f, 0.12f, 0.08f, 0.95f),
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-12, -12));

        // Gold border — generate texture
        var borderGo = new GameObject("Border", typeof(Image));
        var bRect = borderGo.GetComponent<RectTransform>();
        bRect.SetParent(_root.transform, false);
        bRect.anchorMin = Vector2.zero;
        bRect.anchorMax = Vector2.one;
        bRect.offsetMin = Vector2.zero;
        bRect.offsetMax = Vector2.zero;

        var tex = new Texture2D(240, 300);
        for (int x = 0; x < 240; x++)
            for (int y = 0; y < 300; y++) {
                bool edge = x < 2 || x >= 238 || y < 2 || y >= 298;
                tex.SetPixel(x, y, edge ? UIColorPalette.GoldTrim : Color.clear);
            }
        tex.Apply();
        var bImg = borderGo.GetComponent<Image>();
        bImg.sprite = Sprite.Create(tex, new Rect(0, 0, 220, 280), Vector2.one * 0.5f, 1);
        bImg.raycastTarget = false;

        // Name
        var nameGo = UIFactory.CreateLabel(_root.transform, "---", 22, UIColorPalette.ParchmentLight, TextAnchor.UpperCenter);
        var nr = nameGo.GetComponent<RectTransform>();
        nr.anchoredPosition = new Vector2(0, -30);

        _nameLabel = nameGo.GetComponent<Text>();

        // Class
        var classGo = UIFactory.CreateLabel(_root.transform, "", 16, UIColorPalette.GoldTrim, TextAnchor.UpperCenter);
        var cr = classGo.GetComponent<RectTransform>();
        cr.anchoredPosition = new Vector2(0, -55);
        _classLabel = classGo.GetComponent<Text>();

        // HP bar
        var hpBarGo = UIFactory.CreateHPBar(_root.transform, new Vector2(180, 18));
        var hr = hpBarGo.GetComponent<RectTransform>();
        hr.anchoredPosition = new Vector2(0, -80);
        _hpBarFill = hpBarGo.transform.Find("Fill")?.GetComponent<Image>();
        _hpText = UIFactory.CreateLabel(_root.transform, "", 14, Color.white, TextAnchor.MiddleCenter).GetComponent<Text>();
        var hpr = _hpText.GetComponent<RectTransform>();
        hpr.sizeDelta = new Vector2(180, 18);
        hpr.anchoredPosition = new Vector2(0, -80);

        // AC
        var acGo = UIFactory.CreateLabel(_root.transform, "AC: --", 16, UIColorPalette.ParchmentLight, TextAnchor.UpperLeft);
        var ar = acGo.GetComponent<RectTransform>();
        ar.anchoredPosition = new Vector2(-90, -108);
        _acLabel = acGo.GetComponent<Text>();

        // Stats
        _statLabels = new Text[6];
        string[] statNames = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
        for (int i = 0; i < 6; i++) {
            float y = -140 - (i / 2) * 24;
            float x = (i % 2 == 0) ? -80 : 10;
            var lbl = UIFactory.CreateLabel(_root.transform, $"{statNames[i]}: --", 14, UIColorPalette.ParchmentDark, TextAnchor.UpperLeft);
            var lr = lbl.GetComponent<RectTransform>();
            lr.anchoredPosition = new Vector2(x, y);
            _statLabels[i] = lbl.GetComponent<Text>();
        }

        _root.SetActive(false);
    }

    public void Show(bool visible) { if (_root != null) _root.SetActive(visible); }

    public void DisplayUnit(Unit unit) {
        if (unit == null || unit.Data == null) { _root.SetActive(false); return; }
        _root.SetActive(true);
        var d = unit.Data;
        _nameLabel.text = d.unitName;
        _classLabel.text = d.unitClass.ToString();

        int curHp = unit.CurrentHp;
        float ratio = (float)curHp / d.maxHp;
        _hpText.text = $"HP: {curHp}/{d.maxHp}";
        if (_hpBarFill != null) {
            _hpBarFill.fillAmount = ratio;
            _hpBarFill.color = UIColorPalette.HpColor(ratio);
        }

        _acLabel.text = $"AC: {d.armorClass}";

        int[] stats = { d.strength, d.dexterity, d.constitution, d.intelligence, d.wisdom, d.charisma };
        string[] names = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
        for (int i = 0; i < 6; i++) {
            int mod = d.GetAbilityModifier(stats[i]);
            _statLabels[i].text = $"{names[i]}: {stats[i]} ({mod:+0;-0;0})";
        }
    }
}
