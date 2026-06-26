using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TurnSubState {
    Idle,
    SelectingAction,
    SelectingTarget,
    Executing,
    Animating
}

public enum BattlePhase {
    Placement,
    PlayerTurn,
    EnemyTurn,
    Victory,
    Defeat
}

public class TurnManager : MonoBehaviour {
    public static TurnManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform _playerUnitParent;
    [SerializeField] private Transform _enemyUnitParent;

    [Header("State")]
    [SerializeField] private BattlePhase _currentPhase = BattlePhase.Placement;

    private List<Unit> _allUnits = new List<Unit>();
    private List<Unit> _playerUnits = new List<Unit>();
    private List<Unit> _enemyUnits = new List<Unit>();
    private int _currentUnitIndex;

    public BattlePhase CurrentPhase => _currentPhase;
    public IReadOnlyList<Unit> PlayerUnits => _playerUnits;
    public IReadOnlyList<Unit> EnemyUnits => _enemyUnits;
    public Unit ActiveUnit { get; private set; }

    public event Action<BattlePhase> OnPhaseChanged;
    public event Action<Unit> OnActiveUnitChanged;
    public event Action<bool> OnBattleEnd; // true = player menang

    public TurnSubState SubState { get; set; } = TurnSubState.Idle;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start() {
        // Register dari children kalau ada
        RefreshUnitList();

        // Kalo ada unit langsung mulai battle
        if (_allUnits.Count > 0) {
            StartBattle();
        }
    }

    public void RegisterUnit(Unit unit) {
        if (!_allUnits.Contains(unit)) {
            _allUnits.Add(unit);
            if (unit.IsPlayerControlled) {
                if (!_playerUnits.Contains(unit)) _playerUnits.Add(unit);
            } else {
                if (!_enemyUnits.Contains(unit)) _enemyUnits.Add(unit);
            }
        }
    }

    void RefreshUnitList() {
        _allUnits.Clear();
        _playerUnits.Clear();
        _enemyUnits.Clear();

        if (_playerUnitParent != null) {
            var players = _playerUnitParent.GetComponentsInChildren<Unit>(true);
            _playerUnits.AddRange(players);
            _allUnits.AddRange(players);
            foreach (var u in players) TurnManager.Instance.RegisterUnit(u);
        }

        if (_enemyUnitParent != null) {
            var enemies = _enemyUnitParent.GetComponentsInChildren<Unit>(true);
            _enemyUnits.AddRange(enemies);
            _allUnits.AddRange(enemies);
            foreach (var u in enemies) TurnManager.Instance.RegisterUnit(u);
        }
    }

    public void StartBattle() {
        RefreshUnitList();
        Debug.Log("=== BATTLE START ===");
        StartPlayerTurn();
    }

    void StartPlayerTurn() {
        _currentPhase = BattlePhase.PlayerTurn;
        SubState = TurnSubState.Idle;
        OnPhaseChanged?.Invoke(_currentPhase);

        // Reset state untuk semua player units
        foreach (var unit in _playerUnits.Where(u => u.IsAlive)) {
            unit.ResetTurnState();
        }

        _currentUnitIndex = 0;
        ActivateNextPlayerUnit();
    }

    void ActivateNextPlayerUnit() {
        // Cari player unit yang masih alive dan belum selesai
        for (int i = _currentUnitIndex; i < _playerUnits.Count; i++) {
            var unit = _playerUnits[i];
            if (unit != null && unit.IsAlive) {
                ActiveUnit = unit;
                _currentUnitIndex = i + 1;
                SubState = TurnSubState.SelectingAction;
                OnActiveUnitChanged?.Invoke(unit);
                Debug.Log($"Player turn: {unit.Data?.unitName ?? "Unit"}");
                return;
            }
        }

        // Semua player unit sudah selesai → enemy turn
        StartEnemyTurn();
    }

    public void EndPlayerUnitTurn() {
        if (ActiveUnit != null) {
            ActiveUnit.HasActedThisTurn = true;
            ActiveUnit.HasMovedThisTurn = true;
        }
        SubState = TurnSubState.Idle;
        ActivateNextPlayerUnit();
    }

    void StartEnemyTurn() {
        _currentPhase = BattlePhase.EnemyTurn;
        SubState = TurnSubState.Executing;
        OnPhaseChanged?.Invoke(_currentPhase);
        Debug.Log("=== ENEMY TURN ===");

        StartCoroutine(RunEnemyAI());
    }

    IEnumerator RunEnemyAI() {
        foreach (var enemy in _enemyUnits.Where(e => e.IsAlive)) {
            enemy.ResetTurnState();
            ActiveUnit = enemy;
            OnActiveUnitChanged?.Invoke(enemy);

            yield return StartCoroutine(EnemyTakeTurn(enemy));
            yield return new WaitForSeconds(0.3f);
        }

        // Cek kondisi battle
        if (CheckVictoryCondition()) {
            _currentPhase = BattlePhase.Victory;
            OnPhaseChanged?.Invoke(_currentPhase);
            OnBattleEnd?.Invoke(true);
            Debug.Log("=== VICTORY! ===");
        } else {
            StartPlayerTurn();
        }
    }

    IEnumerator EnemyTakeTurn(Unit enemy) {
        if (enemy.Data == null) {
            ActionSystem.Instance.ExecuteWait(enemy);
            yield break;
        }

        var grid = GridManager.Instance;
        var origin = enemy.GridPos;

        // 1. Cari target player terdekat
        Unit closestTarget = null;
        int closestDist = int.MaxValue;

        foreach (var player in _playerUnits.Where(p => p.IsAlive)) {
            int dist = grid.GetDistance(origin, player.GridPos);
            if (dist >= 0 && dist < closestDist) {
                closestDist = dist;
                closestTarget = player;
            }
        }

        if (closestTarget == null) {
            ActionSystem.Instance.ExecuteWait(enemy);
            yield break;
        }

        int attackRange = enemy.Data.attackRange;
        int targetDist = Mathf.Abs(origin.x - closestTarget.GridPos.x) + Mathf.Abs(origin.y - closestTarget.GridPos.y);

        if (targetDist <= attackRange) {
            // Dalam range → attack
            ActionSystem.Instance.ExecuteAttack(enemy, closestTarget);
        } else {
            // Move toward target
            var walkable = ActionRangeCalculator.GetWalkableTiles(origin, enemy.Data.moveRange, grid);

            Tile bestTile = null;
            int bestDist = int.MaxValue;

            foreach (var tile in walkable) {
                int dist = Mathf.Abs(tile.GridPos.x - closestTarget.GridPos.x) + Mathf.Abs(tile.GridPos.y - closestTarget.GridPos.y);
                if (dist < bestDist) {
                    bestDist = dist;
                    bestTile = tile;
                }
            }

            if (bestTile != null && bestDist < targetDist) {
                yield return StartCoroutine(ActionSystem.Instance.ExecuteMove(enemy, bestTile));
            }

            // Attack lagi kalau setelah move masuk range
            int newDist = Mathf.Abs(enemy.GridPos.x - closestTarget.GridPos.x) + Mathf.Abs(enemy.GridPos.y - closestTarget.GridPos.y);
            if (newDist <= attackRange) {
                ActionSystem.Instance.ExecuteAttack(enemy, closestTarget);
            } else if (!enemy.HasActedThisTurn) {
                ActionSystem.Instance.ExecuteWait(enemy);
            }
        }

        yield return null;
    }

    bool CheckVictoryCondition() {
        return _enemyUnits.All(e => !e.IsAlive);
    }

    bool CheckDefeatCondition() {
        return _playerUnits.All(p => !p.IsAlive);
    }

    // Dipanggil dari luar saat unit mati
    public void OnUnitDied(Unit unit) {
        _allUnits.Remove(unit);

        if (unit.IsPlayerControlled) {
            _playerUnits.Remove(unit);
            if (CheckDefeatCondition()) {
                _currentPhase = BattlePhase.Defeat;
                OnPhaseChanged?.Invoke(_currentPhase);
                OnBattleEnd?.Invoke(false);
                Debug.Log("=== DEFEAT ===");
            }
        } else {
            _enemyUnits.Remove(unit);
            if (CheckVictoryCondition()) {
                _currentPhase = BattlePhase.Victory;
                OnPhaseChanged?.Invoke(_currentPhase);
                OnBattleEnd?.Invoke(true);
                Debug.Log("=== VICTORY! ===");
            }
        }
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }
}
