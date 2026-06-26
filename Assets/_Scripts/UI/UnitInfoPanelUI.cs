using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class UnitInfoPanelUI : MonoBehaviour {
    private GameObject _root;
    private Text _nameLabel, _classLabel, _acLabel, _hpText;
    private Image _hpBarFill;
    private Text[] _statLabels;

    void Awake() {
        CreateUI();
    }

    void CreateUI() {
        _root = UIFactory.CreatePanel(transform, new Vector2(220, 280),
            SpriteFactory.CreateFilledRect(220, 280, new Color(0.14f, 0.12f, 0.08f, 0.92f)),
            Color.white, new Vector2(1, 1), new Vector2(1, 1));
        var rect = _root.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(-10, -10);
        rect.pivot = new Vector2(1, 1);

        // Gold border
        var border = UIFactory.CreateImage(_root.transform, new Vector2(220, 280),
            SpriteFactory.CreateFilledRect(220, 280, Color.clear), Color.white);
        border.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 280);
        border.GetComponent<Image>().color = UIColorPalette.GoldTrim;

        // Name
        _nameLabel = UIFactory.CreateLabel(_root.transform, "---", 22, UIColorPalette.ParchmentLight,
            TextAnchor.UpperCenter).GetComponent<Text>();
        var nr = _nameLabel.GetComponent<RectTransform>();
        nr.anchoredPosition = new Vector2(0, -30);

        // Class
        _classLabel = UIFactory.CreateLabel(_root.transform, "", 16, UIColorPalette.GoldTrim,
            TextAnchor.UpperCenter).GetComponent<Text>();
        var cr = _classLabel.GetComponent<RectTransform>();
        cr.anchoredPosition = new Vector2(0, -55);

        // HP Bar
        var hpPanel = UIFactory.CreateHPBar(_root.transform, new Vector2(180, 18));
        var hpRect = hpPanel.GetComponent<RectTransform>();
        hpRect.anchoredPosition = new Vector2(0, -80);
        _hpBarFill = hpPanel.transform.Find("Fill")?.GetComponent<Image>();
        _hpText = UIFactory.CreateLabel(_root.transform, "", 14, Color.white,
            TextAnchor.MiddleCenter).GetComponent<Text>();
        var hr = _hpText.GetComponent<RectTransform>();
        hr.sizeDelta = new Vector2(180, 18);
        hr.anchoredPosition = new Vector2(0, -80);

        // AC
        _acLabel = UIFactory.CreateLabel(_root.transform, "AC: --", 16, UIColorPalette.ParchmentLight,
            TextAnchor.UpperLeft).GetComponent<Text>();
        var ar = _acLabel.GetComponent<RectTransform>();
        ar.anchoredPosition = new Vector2(-90, -108);

        // Stats
        _statLabels = new Text[6];
        string[] statNames = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
        for (int i = 0; i < 6; i++) {
            float y = -140 - (i / 2) * 24;
            float x = (i % 2 == 0) ? -80 : 10;

            var label = UIFactory.CreateLabel(_root.transform,
                $"{statNames[i]}: --", 14, UIColorPalette.ParchmentDark,
                TextAnchor.UpperLeft).GetComponent<Text>();
            var lr = label.GetComponent<RectTransform>();
            lr.anchoredPosition = new Vector2(x, y);
            _statLabels[i] = label;
        }

        _root.SetActive(false);
    }

    public void Show(bool visible) {
        _root.SetActive(visible);
    }

    public void DisplayUnit(Unit unit) {
        if (unit == null || unit.Data == null) {
            _root.SetActive(false);
            return;
        }

        _root.SetActive(true);
        var d = unit.Data;
        _nameLabel.text = d.unitName;
        _classLabel.text = d.unitClass.ToString();

        int maxHp = d.maxHp;
        int curHp = unit.CurrentHp;
        float ratio = (float)curHp / maxHp;
        _hpText.text = $"HP: {curHp}/{maxHp}";
        if (_hpBarFill != null) {
            _hpBarFill.fillAmount = ratio;
            _hpBarFill.color = UIColorPalette.HpColor(ratio);
        }

        _acLabel.text = $"AC: {d.armorClass}";

        int[] stats = { d.strength, d.dexterity, d.constitution, d.intelligence, d.wisdom, d.charisma };
        string[] statNames = { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
        for (int i = 0; i < 6; i++) {
            int mod = d.GetAbilityModifier(stats[i]);
            string modStr = mod >= 0 ? $"+{mod}" : $"{mod}";
            _statLabels[i].text = $"{statNames[i]}: {stats[i]} ({modStr})";
        }
    }
}
