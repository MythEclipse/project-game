using UnityEngine;
using UnityEngine.UI;

public class BattleHUD : MonoBehaviour {
    private Canvas _canvas;
    private ActionPanelUI _actionPanel;
    private UnitInfoPanelUI _unitInfoPanel;
    private TurnIndicatorUI _turnIndicator;
    private GameObject _victoryOverlay;

    public void Init() {
        _canvas = UIFactory.CreateCanvas("BattleHUD_Canvas");
        _canvas.transform.SetParent(transform, false);

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

        if (TurnManager.Instance != null) {
            TurnManager.Instance.OnPhaseChanged += OnPhaseChanged;
            TurnManager.Instance.OnActiveUnitChanged += OnActiveUnitChanged;
            TurnManager.Instance.OnBattleEnd += OnBattleEnd;
        }

        if (ActionSystem.Instance != null) {
            ActionSystem.Instance.OnAttackPerformed += OnAttackPerformed;
            ActionSystem.Instance.OnActionExecuted += OnActionExecuted;
        }
    }

    void OnPhaseChanged(BattlePhase phase) {
        _turnIndicator.Show(phase, TurnManager.Instance?.ActiveUnit);

        if (phase == BattlePhase.Victory || phase == BattlePhase.Defeat) {
            _actionPanel.Show(false);
            ShowEndOverlay(phase == BattlePhase.Victory);
        }
        if (phase == BattlePhase.Battle) {
            var unit = TurnManager.Instance?.ActiveUnit;
            _actionPanel.Show(unit != null && unit.IsPlayerControlled);
        }
    }

    void OnActiveUnitChanged(Unit unit) {
        if (unit == null) return;

        _turnIndicator.Show(TurnManager.Instance.CurrentPhase, unit);

        if (unit.IsPlayerControlled && TurnManager.Instance.CurrentPhase == BattlePhase.Battle) {
            _unitInfoPanel.DisplayUnit(unit);
            _actionPanel.Show(true);
            _actionPanel.Refresh(!unit.HasMovedThisTurn, !unit.HasActedThisTurn);
        } else {
            _unitInfoPanel.Show(false);
            _actionPanel.Show(false);
        }
    }

    void OnAttackPerformed(Unit attacker, Unit defender, AttackResult result) {
        SpawnDamagePopup(defender.transform.position, result);
        // Refresh action panel setelah attack
        if (_actionPanel != null && TurnManager.Instance?.ActiveUnit != null) {
            var u = TurnManager.Instance.ActiveUnit;
            _actionPanel.Refresh(!u.HasMovedThisTurn, !u.HasActedThisTurn);
        }
    }

    void OnActionExecuted(Unit unit, ActionType action) {
        if (unit == null || !unit.IsPlayerControlled) return;
        if (action == ActionType.Move && _actionPanel != null) {
            _actionPanel.Refresh(!unit.HasMovedThisTurn, !unit.HasActedThisTurn);
        }
    }

    void OnBattleEnd(bool playerWon) {
        _actionPanel.Show(false);
    }

    void SpawnDamagePopup(Vector3 worldPos, AttackResult result) {
        var go = new GameObject("DamagePopup", typeof(DamagePopup));
        go.transform.SetParent(_canvas.transform, false);
        go.GetComponent<DamagePopup>().Init(worldPos, result);
    }

    void ShowEndOverlay(bool victory) {
        if (_victoryOverlay != null) Destroy(_victoryOverlay);

        _victoryOverlay = UIFactory.CreatePanel(_canvas.transform, new Vector2(1920, 1080),
            new Color(0, 0, 0, 0.75f), Vector2.zero, Vector2.one, Vector2.zero);

        var text = victory ? "VICTORY!" : "DEFEAT...";
        var color = victory ? UIColorPalette.HpGreen : UIColorPalette.DeepRed;
        var label = UIFactory.CreateLabel(_victoryOverlay.transform, text, 64, color, TextAnchor.MiddleCenter).GetComponent<Text>();
        var lr = label.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;
    }
}
