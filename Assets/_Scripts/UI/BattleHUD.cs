using UnityEngine;
using UnityEngine.UI;

public class BattleHUD : MonoBehaviour {
    private Canvas _canvas;
    private ActionPanelUI _actionPanel;
    private UnitInfoPanelUI _unitInfoPanel;
    private TurnIndicatorUI _turnIndicator;
    private GameObject _victoryOverlay;

    public Canvas Canvas => _canvas;

    public void Init() {
        _canvas = UIFactory.CreateCanvas("BattleHUD_Canvas");
        _canvas.transform.SetParent(transform, false);

        // Action panel
        var apGo = new GameObject("ActionPanelUI");
        apGo.transform.SetParent(_canvas.transform, false);
        _actionPanel = apGo.AddComponent<ActionPanelUI>();

        // Init harus dipanggil setelah Awake si panel selesai
        _actionPanel.Init();

        // Unit Info panel
        var uipGo = new GameObject("UnitInfoPanelUI");
        uipGo.transform.SetParent(_canvas.transform, false);
        _unitInfoPanel = uipGo.AddComponent<UnitInfoPanelUI>();

        // Turn indicator
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
            new Color(0, 0, 0, 0.75f),
            Vector2.zero, Vector2.one, Vector2.zero);

        var text = victory ? "VICTORY!" : "DEFEAT...";
        var color = victory ? UIColorPalette.HpGreen : UIColorPalette.DeepRed;
        var label = UIFactory.CreateLabel(_victoryOverlay.transform, text, 64,
            color, TextAnchor.MiddleCenter).GetComponent<Text>();
        var lr = label.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;
    }
}
