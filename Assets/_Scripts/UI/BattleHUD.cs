using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BattleHUD : MonoBehaviour {
    private Canvas _canvas;
    private ActionPanelUI _actionPanel;
    private UnitInfoPanelUI _unitInfoPanel;
    private TurnIndicatorUI _turnIndicator;
    private GameObject _victoryOverlay;

    public Canvas Canvas => _canvas;

    public void Init() {
        // Create Canvas
        _canvas = UIFactory.CreateCanvas("BattleHUD_Canvas");
        _canvas.transform.SetParent(transform, false);

        // Create panels
        var apGo = new GameObject("ActionPanelUI");
        apGo.transform.SetParent(_canvas.transform, false);
        _actionPanel = apGo.AddComponent<ActionPanelUI>();
        _actionPanel.Init();

        var uipGo = new GameObject("UnitInfoPanelUI");
        uipGo.transform.SetParent(_canvas.transform, false);
        _unitInfoPanel = uipGo.AddComponent<UnitInfoPanelUI>();

        var tiGo = new GameObject("TurnIndicatorUI");
        tiGo.transform.SetParent(_canvas.transform, false);
        _turnIndicator = tiGo.AddComponent<TurnIndicatorUI>();

        // Subscribe events
        if (TurnManager.Instance != null) {
            TurnManager.Instance.OnPhaseChanged += OnPhaseChanged;
            TurnManager.Instance.OnActiveUnitChanged += OnActiveUnitChanged;
            TurnManager.Instance.OnBattleEnd += OnBattleEnd;
        }

        if (ActionSystem.Instance != null) {
            ActionSystem.Instance.OnAttackPerformed += OnAttackPerformed;
        }
    }

    void OnPhaseChanged(BattlePhase phase) {
        _actionPanel.Show(phase == BattlePhase.PlayerTurn);
        _turnIndicator.Show(phase);

        if (phase == BattlePhase.Victory || phase == BattlePhase.Defeat) {
            ShowEndOverlay(phase == BattlePhase.Victory);
        }

        if (phase == BattlePhase.PlayerTurn) _turnIndicator.SetPulse(true);
    }

    void OnActiveUnitChanged(Unit unit) {
        if (unit != null && TurnManager.Instance.CurrentPhase == BattlePhase.PlayerTurn) {
            _unitInfoPanel.DisplayUnit(unit);
            _actionPanel.Refresh(
                ActionSystem.Instance.CanMove(unit),
                ActionSystem.Instance.CanAct(unit)
            );
        } else {
            _unitInfoPanel.Show(false);
        }
    }

    void OnAttackPerformed(Unit attacker, Unit defender, AttackResult result) {
        SpawnDamagePopup(defender.transform.position, result);
    }

    void OnBattleEnd(bool playerWon) {
        _actionPanel.Show(false);
    }

    void SpawnDamagePopup(Vector3 worldPos, AttackResult result) {
        var go = new GameObject("DamagePopup", typeof(DamagePopup));
        go.transform.SetParent(_canvas.transform, false);
        var popup = go.GetComponent<DamagePopup>();
        popup.Init(worldPos, result);
    }

    void ShowEndOverlay(bool victory) {
        if (_victoryOverlay != null) Destroy(_victoryOverlay);

        _victoryOverlay = UIFactory.CreatePanel(_canvas.transform, new Vector2(1920, 1080),
            SpriteFactory.CreateFilledRect(1920, 1080, new Color(0, 0, 0, 0.7f)),
            Color.white, Vector2.zero, Vector2.one);

        _victoryOverlay.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        var text = victory ? "🎉 VICTORY! 🎉" : "💀 DEFEAT... 💀";
        var color = victory ? UIColorPalette.HpGreen : UIColorPalette.DeepRed;
        var label = UIFactory.CreateLabel(_victoryOverlay.transform, text, 64,
            color, TextAnchor.MiddleCenter).GetComponent<Text>();
        var lr = label.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.sizeDelta = Vector2.zero;
    }
}
