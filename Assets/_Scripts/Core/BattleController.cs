using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// BattleController — jembatan antara Grid, TurnManager, ActionSystem, dan input.
/// Mengatur alur: klik tile → pilih aksi → pilih target → eksekusi.
/// </summary>
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

    // Color scheme untuk highlight
    private static readonly Color MoveColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);    // Hijau
    private static readonly Color AttackColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);  // Merah
    private static readonly Color SelectColor = new Color(0.2f, 0.4f, 0.8f, 0.5f);  // Biru
    private static readonly Color HealColor = new Color(0.2f, 0.8f, 0.6f, 0.5f);    // Cyan

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start() {
        // Subscribe ke events
        if (GridManager.Instance != null) {
            GridManager.Instance.OnTileClickedEvent += HandleTileClicked;
        }

        if (_turnManager != null) {
            _turnManager.OnPhaseChanged += OnPhaseChanged;
            _turnManager.OnActiveUnitChanged += OnActiveUnitChanged;
            _turnManager.OnBattleEnd += OnBattleEnd;
        }

        // Spawn unit setelah grid jadi
        Invoke(nameof(SpawnUnits), 0.1f);
    }

    void SpawnUnits() {
        if (_unitPrefab == null || _playerUnitParent == null || _enemyUnitParent == null) return;

        // Load UnitData assets direct dari folder
        var allData = new List<UnitData>();
#if UNITY_EDITOR
        var guids = UnityEditor.AssetDatabase.FindAssets("t:UnitData");
        foreach (var guid in guids) {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<UnitData>(path);
            if (data != null) allData.Add(data);
        }
#endif

        // Filter player & enemy
        var playerData = new List<UnitData>();
        var enemyData = new List<UnitData>();
        foreach (var d in allData) {
            if (d.isPlayerControlled) playerData.Add(d);
            else enemyData.Add(d);
        }

        if (playerData.Count == 0 || enemyData.Count == 0) {
            Debug.LogWarning("Need both player and enemy UnitData assets.");
            return;
        }

        var grid = GridManager.Instance;

        // Spawn 4 player units di baris bawah (y=0..1)
        for (int i = 0; i < playerData.Count && i < 4; i++) {
            var unit = Instantiate(_unitPrefab, _playerUnitParent);
            unit.SetData(playerData[i]);

            var xPos = 2 + i * 3;
            var yPos = 0;
            if (grid.PlaceUnit(unit, new Vector2Int(xPos, yPos))) {
                unit.name = playerData[i].unitName;
                Debug.Log($"Spawned {playerData[i].unitName} at ({xPos},{yPos})");
            }
        }

        // Spawn enemy di baris atas (y=8)
        for (int i = 0; i < enemyData.Count && i < 6; i++) {
            var unit = Instantiate(_unitPrefab, _enemyUnitParent);
            unit.SetData(enemyData[i]);

            var xPos = 2 + i * 3;
            var yPos = grid.Height - 1;
            if (grid.PlaceUnit(unit, new Vector2Int(xPos, yPos))) {
                unit.name = enemyData[i].unitName;
                Debug.Log($"Spawned {enemyData[i].unitName} at ({xPos},{yPos})");
            }
        }

        // Auto-start battle
        _turnManager?.StartBattle();
    }

    // ===== EVENT HANDLERS =====

    void OnPhaseChanged(BattlePhase phase) {
        ClearHighlights();

        if (phase == BattlePhase.PlayerTurn) {
            _selectedUnit = null;
            _isWaitingForTarget = false;
        }
    }

    void OnActiveUnitChanged(Unit unit) {
        _selectedUnit = unit;
        ClearHighlights();

        if (unit != null && _turnManager.CurrentPhase == BattlePhase.PlayerTurn) {
            // Highlight tile unit ini
            var tile = GridManager.Instance?.GetTileAtPosition(unit.GridPos);
            if (tile != null) tile.SetHighlight(true, SelectColor);
        }
    }

    void OnBattleEnd(bool playerWon) {
        ClearHighlights();
        _selectedUnit = null;
        _isWaitingForTarget = false;
        Debug.Log(playerWon ? "🎉 PLAYER VICTORY!" : "💀 DEFEAT...");
    }

    // ===== TILE CLICK HANDLING =====

    void HandleTileClicked(Tile clickedTile) {
        if (_turnManager == null) return;
        if (_turnManager.CurrentPhase != BattlePhase.PlayerTurn) return;
        if (_turnManager.SubState != TurnSubState.SelectingAction &&
            _turnManager.SubState != TurnSubState.SelectingTarget) return;

        if (_isWaitingForTarget) {
            HandleTargetSelection(clickedTile);
            return;
        }

        if (_selectedUnit == null) return;

        // Cek apakah tile yang diklik adalah unit player sendiri
        if (clickedTile.Occupant != null &&
            clickedTile.Occupant.IsPlayerControlled &&
            clickedTile.Occupant.IsAlive) {
            // Pilih unit itu
            _selectedUnit = clickedTile.Occupant;
            OnActiveUnitChanged(_selectedUnit);
            return;
        }

        // Cek apakah tile yang diklik adalah musuh (auto-attack)
        if (clickedTile.Occupant != null &&
            !clickedTile.Occupant.IsPlayerControlled &&
            clickedTile.Occupant.IsAlive &&
            _actionSystem.CanAttackTarget(_selectedUnit, clickedTile)) {
            _actionSystem.ExecuteAttack(_selectedUnit, clickedTile.Occupant);
            _turnManager.EndPlayerUnitTurn();
            return;
        }

        // Klik tile kosong → move
        if (clickedTile.IsWalkable && _actionSystem.CanMove(_selectedUnit)) {
            var walkable = ActionRangeCalculator.GetWalkableTiles(
                _selectedUnit.GridPos,
                _selectedUnit.Data?.moveRange ?? 5,
                GridManager.Instance
            );

            if (walkable.Contains(clickedTile)) {
                StartCoroutine(_actionSystem.ExecuteMove(_selectedUnit, clickedTile));
            }
        }
    }

    void HandleTargetSelection(Tile clickedTile) {
        switch (_pendingAction) {
            case ActionType.Attack:
                if (clickedTile.Occupant != null &&
                    !clickedTile.Occupant.IsPlayerControlled &&
                    clickedTile.Occupant.IsAlive &&
                    _actionSystem.CanAttackTarget(_selectedUnit, clickedTile)) {
                    _actionSystem.ExecuteAttack(_selectedUnit, clickedTile.Occupant);
                    _isWaitingForTarget = false;
                    ClearHighlights();
                    _turnManager.EndPlayerUnitTurn();
                }
                break;

            case ActionType.Move:
                if (clickedTile.IsWalkable) {
                    var walkable = ActionRangeCalculator.GetWalkableTiles(
                        _selectedUnit.GridPos,
                        _selectedUnit.Data?.moveRange ?? 5,
                        GridManager.Instance
                    );
                    if (walkable.Contains(clickedTile)) {
                        StartCoroutine(_actionSystem.ExecuteMove(_selectedUnit, clickedTile));
                        _isWaitingForTarget = false;
                        ClearHighlights();
                        _turnManager.SubState = TurnSubState.SelectingAction;
                    }
                }
                break;
        }
    }

    // ===== PUBLIC API — dipanggil dari UI =====

    public void OnMoveButton() => BeginAction(ActionType.Move);
    public void OnAttackButton() => BeginAction(ActionType.Attack);
    public void OnDefendButton() => BeginAction(ActionType.Defend);
    public void OnWaitButton() => BeginAction(ActionType.Wait);

    void BeginAction(ActionType actionType) {
        if (_turnManager.CurrentPhase != BattlePhase.PlayerTurn) return;
        if (_selectedUnit == null || !_selectedUnit.IsAlive) return;

        ClearHighlights();

        switch (actionType) {
            case ActionType.Move:
                if (!_actionSystem.CanMove(_selectedUnit)) return;
                _isWaitingForTarget = true;
                _pendingAction = ActionType.Move;
                _turnManager.SubState = TurnSubState.SelectingTarget;
                ShowMoveRange();
                break;

            case ActionType.Attack:
                if (!_actionSystem.CanAct(_selectedUnit)) return;
                _isWaitingForTarget = true;
                _pendingAction = ActionType.Attack;
                _turnManager.SubState = TurnSubState.SelectingTarget;
                ShowAttackRange();
                break;

            case ActionType.Defend:
                _actionSystem.ExecuteDefend(_selectedUnit);
                _turnManager.EndPlayerUnitTurn();
                break;

            case ActionType.Wait:
                _actionSystem.ExecuteWait(_selectedUnit);
                _turnManager.EndPlayerUnitTurn();
                break;
        }
    }

    // ===== HIGHLIGHT =====

    void ShowMoveRange() {
        if (_selectedUnit == null) return;
        var tiles = ActionRangeCalculator.GetWalkableTiles(
            _selectedUnit.GridPos,
            _selectedUnit.Data?.moveRange ?? 5,
            GridManager.Instance
        );
        foreach (var t in tiles) t.SetHighlight(true, MoveColor);
        _highlightedTiles = tiles;
    }

    void ShowAttackRange() {
        if (_selectedUnit == null) return;
        var tiles = ActionRangeCalculator.GetAttackableTiles(
            _selectedUnit.GridPos,
            _selectedUnit.Data?.attackRange ?? 1,
            GridManager.Instance
        );
        foreach (var t in tiles) {
            if (t.Occupant != null && !t.Occupant.IsPlayerControlled && t.Occupant.IsAlive) {
                t.SetHighlight(true, AttackColor);
            } else {
                t.SetHighlight(true, new Color(0.8f, 0.2f, 0.2f, 0.2f));
            }
        }
        _highlightedTiles = tiles;
    }

    public void ClearHighlights() {
        foreach (var t in _highlightedTiles) {
            if (t != null) t.SetHighlight(false, Color.clear);
        }
        _highlightedTiles.Clear();
    }

    // ===== CAMERA =====

    public void FocusOnUnit(Unit unit) {
        var camController = FindObjectOfType<CameraController>();
        if (camController != null) {
            camController.SetFollowTarget(unit.transform);
        }
    }

    void OnDestroy() {
        if (GridManager.Instance != null)
            GridManager.Instance.OnTileClickedEvent -= HandleTileClicked;
        if (_turnManager != null) {
            _turnManager.OnPhaseChanged -= OnPhaseChanged;
            _turnManager.OnActiveUnitChanged -= OnActiveUnitChanged;
            _turnManager.OnBattleEnd -= OnBattleEnd;
        }
        if (Instance == this) Instance = null;
    }
}
