using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TurnIndicatorUI : MonoBehaviour {
    private GameObject _root;
    private Text _label;
    private CanvasGroup _group;

    void Awake() {
        CreateUI();
    }

    void CreateUI() {
        _root = UIFactory.CreatePanel(transform, new Vector2(300, 50),
            SpriteFactory.CreateFilledRect(300, 50, new Color(0.10f, 0.08f, 0.06f, 0.90f)),
            Color.white, new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        var rect = _root.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, -10);
        rect.pivot = new Vector2(0.5f, 1);

        // Gold border
        var border = UIFactory.CreateImage(_root.transform, new Vector2(300, 50),
            SpriteFactory.CreateFilledRect(300, 50, Color.clear), Color.white);
        border.GetComponent<Image>().color = UIColorPalette.GoldTrim;
        var br = border.GetComponent<RectTransform>();
        br.anchorMin = Vector2.zero;
        br.anchorMax = Vector2.one;
        br.sizeDelta = Vector2.zero;

        _group = _root.AddComponent<CanvasGroup>();

        _label = UIFactory.CreateLabel(_root.transform, "PLAYER TURN", 24,
            UIColorPalette.ParchmentLight, TextAnchor.MiddleCenter).GetComponent<Text>();
        var lr = _label.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.sizeDelta = Vector2.zero;
    }

    public void Show(BattlePhase phase) {
        switch (phase) {
            case BattlePhase.PlayerTurn:
                _label.text = "⚔ PLAYER TURN ⚔";
                _label.color = UIColorPalette.ParchmentLight;
                break;
            case BattlePhase.EnemyTurn:
                _label.text = "👹 ENEMY TURN 👹";
                _label.color = UIColorPalette.DeepRed;
                break;
            case BattlePhase.Victory:
                _label.text = "🎉 VICTORY! 🎉";
                _label.color = UIColorPalette.HpGreen;
                break;
            case BattlePhase.Defeat:
                _label.text = "💀 DEFEAT... 💀";
                _label.color = UIColorPalette.DeepRed;
                break;
            default:
                _root.SetActive(false);
                return;
        }
        _root.SetActive(true);
    }

    public void SetPulse(bool active) {
        if (active) StartCoroutine(PulseRoutine());
    }

    IEnumerator PulseRoutine() {
        while (true) {
            if (_group == null) yield break;
            _group.alpha = 0.8f + Mathf.Sin(Time.time * 3f) * 0.2f;
            yield return null;
        }
    }
}
