using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class BattleController : MonoBehaviour {
    public static BattleController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TurnManager _turnManager;
    [SerializeField] private ActionSystem _actionSystem;

    [Header("Combatants")]
    [SerializeField] private Transform _playerUnitParent;
    [SerializeField] private Transform _enemyUnitParent;

    [Header("Unit Prefabs")]
    [SerializeField] private Unit _unitPrefab;

    private Unit _selectedUnit;
    private ActionType _pendingAction;
    private List<Tile> _highlightedTiles = new List<Tile>();
    private bool _isWaitingForTarget;
    private BattleHUD _battleHud;
    private Coroutine _pulseCoroutine;

    private static readonly Color MoveColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
    private static readonly Color AttackColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);
    private static readonly Color SelectColor = new Color(0.2f, 0.4f, 0.8f, 0.5f);
    private int _width => GridManager.Instance != null ? GridManager.Instance.Width : 16;
    private int _height => GridManager.Instance != null ? GridManager.Instance.Height : 9;
    private int _playerRow => Mathf.Max(0, _height / 2 - 2);
    private int _enemyRow => Mathf.Min(_height - 1, _height / 2 + 2);

    void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() {
        if (GridManager.Instance != null)
            GridManager.Instance.OnTileClickedEvent += HandleTileClicked;

        if (_turnManager != null) {
            _turnManager.OnPhaseChanged += OnPhaseChanged;
            _turnManager.OnActiveUnitChanged += OnActiveUnitChanged;
            _turnManager.OnBattleEnd += OnBattleEnd;
        }

        if (_actionSystem != null)
            _actionSystem.OnAttackPerformed += OnAttackPerformed;

        _battleHud = new GameObject("BattleHUD").AddComponent<BattleHUD>();
        _battleHud.Init();

        Invoke(nameof(SpawnUnits), 0.1f);
    }

    void SpawnUnits() {
        if (_unitPrefab == null || _playerUnitParent == null || _enemyUnitParent == null) return;

        var allData = new List<UnitData>();
#if UNITY_EDITOR
        var guids = UnityEditor.AssetDatabase.FindAssets("t:UnitData");
        foreach (var guid in guids) {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<UnitData>(path);
            if (data != null) allData.Add(data);
        }
#endif
        var playerData = new List<UnitData>();
        var enemyData = new List<UnitData>();
        foreach (var d in allData) {
            if (d.isPlayerControlled) playerData.Add(d); else enemyData.Add(d);
        }

        if (playerData.Count == 0 || enemyData.Count == 0) {
            Debug.LogWarning("Need both player and enemy UnitData assets.");
            return;
        }

        var grid = GridManager.Instance;
        int midX = _width / 2;
        for (int i = 0; i < playerData.Count && i < 4; i++) {
            var unit = Instantiate(_unitPrefab, _playerUnitParent);
            unit.SetData(playerData[i]);
            int xPos = midX - 2 + i * 2;
            if (grid.PlaceUnit(unit, new Vector2Int(xPos, _playerRow)))
                unit.name = playerData[i].unitName;
        }

        for (int i = 0; i < enemyData.Count && i < 4; i++) {
            var unit = Instantiate(_unitPrefab, _enemyUnitParent);
            unit.SetData(enemyData[i]);
            int xPos = midX - 2 + i * 2;
            if (grid.PlaceUnit(unit, new Vector2Int(xPos, _enemyRow)))
                unit.name = enemyData[i].unitName;
        }

        _turnManager?.StartBattle();
    }

    // ===== EVENTS =====

    void OnPhaseChanged(BattlePhase phase) {
        ClearHighlights();
        if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }
        if (phase == BattlePhase.Battle) { _selectedUnit = null; _isWaitingForTarget = false; }
    }

    void OnActiveUnitChanged(Unit unit) {
        _selectedUnit = unit;
        ClearHighlights();
        if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }

        if (unit != null && _turnManager.CurrentPhase == BattlePhase.Battle) {
            var tile = GridManager.Instance?.GetTileAtPosition(unit.GridPos);
            if (tile != null) _pulseCoroutine = StartCoroutine(PulseHighlight(tile));
            FocusOnUnit(unit);
        }
    }

    void OnBattleEnd(bool playerWon) {
        ClearHighlights();
        if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }
        _selectedUnit = null; _isWaitingForTarget = false;
    }

    void OnAttackPerformed(Unit attacker, Unit defender, AttackResult result) {
        var cam = FindObjectOfType<CameraController>();
        if (cam != null && result.hit && result.damage > 0)
            cam.Shake(result.crit ? 0.25f : 0.1f, 0.15f);
    }

    IEnumerator PulseHighlight(Tile tile) {
        while (tile != null && _selectedUnit != null) {
            float alpha = 0.3f + Mathf.Sin(Time.time * 3f) * 0.35f;
            var c = SelectColor; c.a = Mathf.Clamp01(alpha);
            tile.SetHighlight(true, c);
            yield return null;
        }
    }

    // ===== TILE CLICK =====

    void HandleTileClicked(Tile clickedTile) {
        if (_turnManager == null || _turnManager.CurrentPhase != BattlePhase.Battle) return;
        if (_turnManager.SubState != TurnSubState.SelectingAction && _turnManager.SubState != TurnSubState.SelectingTarget) return;

        // HANYA active unit yang bisa dikontrol
        var active = _turnManager.ActiveUnit;
        if (active == null || !active.IsAlive) return;
        if (!active.IsPlayerControlled) return;
        _selectedUnit = active;

        if (_isWaitingForTarget) { HandleTargetSelection(clickedTile); return; }

        // Klik musuh → serang langsung (kalau player controlled)
        if (_selectedUnit.IsPlayerControlled &&
            clickedTile.Occupant != null && !clickedTile.Occupant.IsPlayerControlled && clickedTile.Occupant.IsAlive
            && _actionSystem.CanAttackTarget(_selectedUnit, clickedTile)) {
            _actionSystem.ExecuteAttack(_selectedUnit, clickedTile.Occupant);
            _turnManager.EndUnitTurn();
            return;
        }

        // Klik tile kosong → move
        if (_selectedUnit.IsPlayerControlled && clickedTile.IsWalkable && !_selectedUnit.HasMovedThisTurn) {
            var walkable = ActionRangeCalculator.GetWalkableTiles(_selectedUnit.GridPos, _selectedUnit.Data?.moveRange ?? 5, GridManager.Instance);
            if (walkable.Contains(clickedTile)) StartCoroutine(_actionSystem.ExecuteMove(_selectedUnit, clickedTile));
        }
    }

    void HandleTargetSelection(Tile clickedTile) {
        switch (_pendingAction) {
            case ActionType.Attack:
                if (clickedTile.Occupant != null && !clickedTile.Occupant.IsPlayerControlled && clickedTile.Occupant.IsAlive
                    && _actionSystem.CanAttackTarget(_selectedUnit, clickedTile)) {
                    _actionSystem.ExecuteAttack(_selectedUnit, clickedTile.Occupant);
                    _isWaitingForTarget = false; ClearHighlights(); _turnManager.EndUnitTurn();
                }
                break;
            case ActionType.Move:
                if (clickedTile.IsWalkable) {
                    var walkable = ActionRangeCalculator.GetWalkableTiles(_selectedUnit.GridPos, _selectedUnit.Data?.moveRange ?? 5, GridManager.Instance);
                    if (walkable.Contains(clickedTile)) {
                        StartCoroutine(_actionSystem.ExecuteMove(_selectedUnit, clickedTile));
                        _isWaitingForTarget = false; ClearHighlights();
                        _turnManager.SubState = TurnSubState.SelectingAction;
                    }
                }
                break;
        }
    }

    // ===== PUBLIC API =====

    public void OnMoveButton() => BeginAction(ActionType.Move);
    public void OnAttackButton() => BeginAction(ActionType.Attack);
    public void OnDefendButton() => BeginAction(ActionType.Defend);
    public void OnWaitButton() => BeginAction(ActionType.Wait);

    void BeginAction(ActionType actionType) {
        if (_turnManager.CurrentPhase != BattlePhase.Battle || _selectedUnit == null || !_selectedUnit.IsAlive) return;
        if (!_selectedUnit.IsPlayerControlled) return;
        ClearHighlights();

        switch (actionType) {
            case ActionType.Move:
                if (_selectedUnit.HasMovedThisTurn) return;
                _isWaitingForTarget = true; _pendingAction = ActionType.Move;
                _turnManager.SubState = TurnSubState.SelectingTarget; ShowMoveRange();
                break;
            case ActionType.Attack:
                if (_selectedUnit.HasActedThisTurn) return;
                _isWaitingForTarget = true; _pendingAction = ActionType.Attack;
                _turnManager.SubState = TurnSubState.SelectingTarget; ShowAttackRange();
                break;
            case ActionType.Defend:
                _actionSystem.ExecuteDefend(_selectedUnit);
                _selectedUnit.SetDefendingVisual(true);
                _turnManager.EndUnitTurn();
                break;
            case ActionType.Wait:
                _actionSystem.ExecuteWait(_selectedUnit);
                _turnManager.EndUnitTurn();
                break;
        }
    }

    void ShowMoveRange() {
        if (_selectedUnit == null) return;
        var tiles = ActionRangeCalculator.GetWalkableTiles(_selectedUnit.GridPos, _selectedUnit.Data?.moveRange ?? 5, GridManager.Instance);
        foreach (var t in tiles) t.SetHighlight(true, MoveColor);
        _highlightedTiles = tiles;
    }

    void ShowAttackRange() {
        if (_selectedUnit == null) return;
        var tiles = ActionRangeCalculator.GetAttackableTiles(_selectedUnit.GridPos, _selectedUnit.Data?.attackRange ?? 1, GridManager.Instance);
        foreach (var t in tiles) {
            if (t.Occupant != null && !t.Occupant.IsPlayerControlled && t.Occupant.IsAlive)
                t.SetHighlight(true, AttackColor);
            else
                t.SetHighlight(true, new Color(0.8f, 0.2f, 0.2f, 0.2f));
        }
        _highlightedTiles = tiles;
    }

    public void ClearHighlights() {
        foreach (var t in _highlightedTiles) { if (t != null) t.SetHighlight(false, Color.clear); }
        _highlightedTiles.Clear();
    }

    public void FocusOnUnit(Unit unit) {
        var cam = FindObjectOfType<CameraController>();
        if (cam != null) cam.SetFollowTarget(unit.transform);
    }

    void OnDestroy() {
        if (GridManager.Instance != null) GridManager.Instance.OnTileClickedEvent -= HandleTileClicked;
        if (_turnManager != null) {
            _turnManager.OnPhaseChanged -= OnPhaseChanged;
            _turnManager.OnActiveUnitChanged -= OnActiveUnitChanged;
            _turnManager.OnBattleEnd -= OnBattleEnd;
        }
        if (_actionSystem != null) _actionSystem.OnAttackPerformed -= OnAttackPerformed;
        if (Instance == this) Instance = null;
    }
}
