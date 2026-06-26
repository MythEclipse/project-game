using UnityEngine;
using UnityEngine.UI;
using System;

public class ActionPanelUI : MonoBehaviour {
    private GameObject _root;
    private Button _moveBtn, _attackBtn, _defendBtn, _waitBtn, _endTurnBtn;

    void Awake() {
        CreateUI();
    }

    void CreateUI() {
        _root = new GameObject("ActionPanel", typeof(Image));
        var rect = _root.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0.5f, 0);
        rect.anchorMax = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 30);
        rect.sizeDelta = new Vector2(600, 60);

        var bg = _root.GetComponent<Image>();
        bg.sprite = SpriteFactory.CreateFilledRect(600, 60, new Color(0.12f, 0.10f, 0.08f, 0.92f));
        bg.type = Image.Type.Sliced;

        var borderGo = new GameObject("Border", typeof(Image));
        var borderRect = borderGo.GetComponent<RectTransform>();
        borderRect.SetParent(rect, false);
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;
        var borderImg = borderGo.GetComponent<Image>();
        borderImg.sprite = SpriteFactory.GetIcon(IconType.Shield); // temp
        borderImg.type = Image.Type.Sliced;
        borderImg.color = UIColorPalette.GoldTrim;

        // Buttons
        _moveBtn = CreateActionButton(rect, "Move", 0);
        _attackBtn = CreateActionButton(rect, "Attack", 1);
        _defendBtn = CreateActionButton(rect, "Defend", 2);
        _waitBtn = CreateActionButton(rect, "Wait", 3);

        // End Turn
        _endTurnBtn = CreateEndTurnButton(rect);

        _root.SetActive(false);
    }

    Button CreateActionButton(Transform parent, string label, int index) {
        float spacing = 130f;
        float startX = -spacing * 1.5f;
        float x = startX + index * spacing;

        var go = UIFactory.CreateButton(parent, new Vector2(110, 44), label,
            SpriteFactory.CreateFilledRect(110, 44, UIColorPalette.StoneMid),
            UIColorPalette.ParchmentLight, 18, null);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0);

        return go.GetComponent<Button>();
    }

    Button CreateEndTurnButton(Transform parent) {
        var go = UIFactory.CreateButton(parent, new Vector2(100, 44), "End Turn",
            SpriteFactory.CreateFilledRect(100, 44, UIColorPalette.DeepRed),
            Color.white, 16, null);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0.5f);
        rect.anchorMax = new Vector2(1, 0.5f);
        rect.anchoredPosition = new Vector2(-10, 0);

        return go.GetComponent<Button>();
    }

    public void Init() {
        _moveBtn.onClick.AddListener(() => BattleController.Instance?.OnMoveButton());
        _attackBtn.onClick.AddListener(() => BattleController.Instance?.OnAttackButton());
        _defendBtn.onClick.AddListener(() => BattleController.Instance?.OnDefendButton());
        _waitBtn.onClick.AddListener(() => BattleController.Instance?.OnWaitButton());
        _endTurnBtn.onClick.AddListener(() => {
            if (TurnManager.Instance?.ActiveUnit != null)
                TurnManager.Instance.EndPlayerUnitTurn();
        });
    }

    public void Show(bool visible) {
        _root.SetActive(visible);
    }

    public void Refresh(bool canMove, bool canAct) {
        _moveBtn.interactable = canMove;
        _attackBtn.interactable = canAct;
        _defendBtn.interactable = canAct;
    }
}
