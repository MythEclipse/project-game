using UnityEngine;
using UnityEngine.UI;

public class ActionPanelUI : MonoBehaviour {
    private GameObject _root;
    private Button _moveBtn, _attackBtn, _defendBtn, _waitBtn, _endTurnBtn;

    void Awake() {
        UIFactory.EnsureEventSystem();
        CreateUI();
    }

    void CreateUI() {
        _root = UIFactory.CreatePanel(transform, new Vector2(620, 64),
            new Color(0.12f, 0.10f, 0.08f, 0.92f),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 24));

        var border = UIFactory.CreateImage(_root.transform, new Vector2(620, 64), new Color(0,0,0,0));
        var bImg = border.GetComponent<Image>();
        // Border: generate border texture prosedural
        var tex = new Texture2D(620, 64);
        for (int x = 0; x < 620; x++)
            for (int y = 0; y < 64; y++) {
                bool edge = x < 2 || x >= 618 || y < 2 || y >= 62;
                tex.SetPixel(x, y, edge ? UIColorPalette.GoldTrim : Color.clear);
            }
        tex.Apply();
        bImg.sprite = Sprite.Create(tex, new Rect(0, 0, 620, 64), Vector2.one * 0.5f, 1);
        bImg.raycastTarget = false;
        var bRect = border.GetComponent<RectTransform>();
        bRect.anchorMin = Vector2.zero;
        bRect.anchorMax = Vector2.one;
        bRect.offsetMin = Vector2.zero;
        bRect.offsetMax = Vector2.zero;

        // Action buttons
        _moveBtn = CreateBtn("Move", -195);
        _attackBtn = CreateBtn("Attack", -65);
        _defendBtn = CreateBtn("Defend", 65);
        _waitBtn = CreateBtn("Wait", 195);

        // End Turn
        _endTurnBtn = CreateEndTurnBtn();

        _root.SetActive(false);
    }

    Button CreateBtn(string label, float x) {
        var go = UIFactory.CreateButton(_root.transform, new Vector2(110, 44), label,
            UIColorPalette.StoneMid, UIColorPalette.ParchmentLight, 18, null);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0);

        return go.GetComponent<Button>();
    }

    Button CreateEndTurnBtn() {
        var go = UIFactory.CreateButton(_root.transform, new Vector2(110, 44), "End Turn",
            UIColorPalette.DeepRed, Color.white, 16, null);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(330, 0);

        return go.GetComponent<Button>();
    }

    public void Init() {
        _moveBtn.onClick.AddListener(() => BattleController.Instance?.OnMoveButton());
        _attackBtn.onClick.AddListener(() => BattleController.Instance?.OnAttackButton());
        _defendBtn.onClick.AddListener(() => BattleController.Instance?.OnDefendButton());
        _waitBtn.onClick.AddListener(() => BattleController.Instance?.OnWaitButton());
        _endTurnBtn.onClick.AddListener(() => {
            if (TurnManager.Instance?.ActiveUnit != null)
                TurnManager.Instance.EndUnitTurn();
        });
    }

    public void Show(bool visible) => _root.SetActive(visible);
    public void Refresh(bool canMove, bool canAct) {
        if (_moveBtn != null) _moveBtn.interactable = canMove;
        if (_attackBtn != null) _attackBtn.interactable = canAct;
        if (_defendBtn != null) _defendBtn.interactable = canAct;
    }
}
