using UnityEngine;
using UnityEngine.UI;

public class TurnIndicatorUI : MonoBehaviour {
    private GameObject _root;
    private Text _label;

    void Awake() {
        CreateUI();
    }

    void CreateUI() {
        _root = UIFactory.CreatePanel(transform, new Vector2(300, 50),
            new Color(0.10f, 0.08f, 0.06f, 0.90f),
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -10));

        // Set pivot top-center
        var rt = _root.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 1f);

        // Gold border
        var borderGo = new GameObject("Border", typeof(Image));
        var bRect = borderGo.GetComponent<RectTransform>();
        bRect.SetParent(_root.transform, false);
        bRect.anchorMin = Vector2.zero;
        bRect.anchorMax = Vector2.one;
        bRect.offsetMin = Vector2.zero;
        bRect.offsetMax = Vector2.zero;

        var tex = new Texture2D(300, 50);
        for (int x = 0; x < 300; x++)
            for (int y = 0; y < 50; y++) {
                bool edge = x < 2 || x >= 298 || y < 2 || y >= 48;
                tex.SetPixel(x, y, edge ? UIColorPalette.GoldTrim : Color.clear);
            }
        tex.Apply();
        var bImg = borderGo.GetComponent<Image>();
        bImg.sprite = Sprite.Create(tex, new Rect(0, 0, 300, 50), Vector2.one * 0.5f, 1);
        bImg.raycastTarget = false;

        var labelGo = UIFactory.CreateLabel(_root.transform, "", 24,
            UIColorPalette.ParchmentLight, TextAnchor.MiddleCenter);
        var lr = labelGo.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;
        _label = labelGo.GetComponent<Text>();
    }

    public void Show(BattlePhase phase, Unit unit = null) {
        if (_root == null) return;
        switch (phase) {
            case BattlePhase.Battle:
                if (unit != null)
                    _label.text = $"{unit.Data?.unitName ?? "Unit"}'s Turn";
                else
                    _label.text = "Battle";
                _label.color = unit != null && unit.IsPlayerControlled
                    ? UIColorPalette.ParchmentLight : UIColorPalette.DeepRed;
                _root.SetActive(true);
                break;
            case BattlePhase.Victory:
                _label.text = "VICTORY!";
                _label.color = UIColorPalette.HpGreen;
                _root.SetActive(true);
                break;
            case BattlePhase.Defeat:
                _label.text = "DEFEAT...";
                _label.color = UIColorPalette.DeepRed;
                _root.SetActive(true);
                break;
            default:
                _root.SetActive(false);
                break;
        }
    }
}
