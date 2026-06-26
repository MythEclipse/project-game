using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TurnSubState { Idle, SelectingAction, SelectingTarget, Executing, Animating }
public enum BattlePhase { Placement, Battle, Victory, Defeat }

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

    private List<Unit> _turnQueue = new List<Unit>();
    private int _currentQueueIndex = -1;
    private int _roundNumber = 0;

    public BattlePhase CurrentPhase => _currentPhase;
    public IReadOnlyList<Unit> PlayerUnits => _playerUnits;
    public IReadOnlyList<Unit> EnemyUnits => _enemyUnits;
    public Unit ActiveUnit { get; private set; }
    public int RoundNumber => _roundNumber;
    public TurnSubState SubState { get; set; } = TurnSubState.Idle;

    public event Action<BattlePhase> OnPhaseChanged;
    public event Action<Unit> OnActiveUnitChanged;
    public event Action<bool> OnBattleEnd;

    void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() {
        RefreshUnitList();
        if (_allUnits.Count > 0) StartBattle();
    }

    public void RegisterUnit(Unit unit) {
        if (!_allUnits.Contains(unit)) {
            _allUnits.Add(unit);
            if (unit.IsPlayerControlled) { if (!_playerUnits.Contains(unit)) _playerUnits.Add(unit); }
            else { if (!_enemyUnits.Contains(unit)) _enemyUnits.Add(unit); }
        }
    }

    void RefreshUnitList() {
        _allUnits.Clear(); _playerUnits.Clear(); _enemyUnits.Clear();
        if (_playerUnitParent != null) {
            var p = _playerUnitParent.GetComponentsInChildren<Unit>(true);
            _playerUnits.AddRange(p); _allUnits.AddRange(p);
        }
        if (_enemyUnitParent != null) {
            var e = _enemyUnitParent.GetComponentsInChildren<Unit>(true);
            _enemyUnits.AddRange(e); _allUnits.AddRange(e);
        }
    }

    // ===== BATTLE FLOW =====

    public void StartBattle() {
        RefreshUnitList();
        _roundNumber = 0;
        StartNewRound();
    }

    void StartNewRound() {
        _roundNumber++;
        Debug.Log($"=== ROUND {_roundNumber} ===");

        // Roll initiative untuk semua unit alive
        var alive = _allUnits.Where(u => u.IsAlive).ToList();
        foreach (var u in alive) u.RollInitiative();

        // Sort by initiative descending, tie-break: player first
        _turnQueue = alive.OrderByDescending(u => u.InitiativeRoll)
                          .ThenBy(u => u.IsPlayerControlled ? 0 : 1)
                          .ToList();

        // Reset turn state semua unit
        foreach (var u in alive) u.ResetTurnState();

        _currentQueueIndex = -1;

        _currentPhase = BattlePhase.Battle;
        OnPhaseChanged?.Invoke(_currentPhase);

        NextTurn();
    }

    public void NextTurn() {
        // Advance ke unit berikutnya
        _currentQueueIndex++;

        if (_currentQueueIndex >= _turnQueue.Count) {
            // Round selesai — mulai round baru
            // Cek kondisi dulu
            if (CheckVictory()) return;
            if (CheckDefeat()) return;
            StartNewRound();
            return;
        }

        ActiveUnit = _turnQueue[_currentQueueIndex];

        if (!ActiveUnit.IsAlive) {
            NextTurn();
            return;
        }

        SubState = TurnSubState.SelectingAction;
        OnActiveUnitChanged?.Invoke(ActiveUnit);

        // Kalau enemy-controlled, jalankan AI
        if (!ActiveUnit.IsPlayerControlled) {
            StartCoroutine(RunEnemyAI(ActiveUnit));
        }
    }

    public void EndUnitTurn() {
        ActiveUnit?.ResetTurnState();
        SubState = TurnSubState.Idle;
        if (CheckVictory()) return;
        if (CheckDefeat()) return;
        NextTurn();
    }

    // ===== ENEMY AI =====

    IEnumerator RunEnemyAI(Unit enemy) {
        SubState = TurnSubState.Animating;
        yield return new WaitForSeconds(0.3f);

        if (enemy.Data == null) {
            ActionSystem.Instance.ExecuteWait(enemy);
            yield return new WaitForSeconds(0.2f);
            EndUnitTurn();
            yield break;
        }

        var grid = GridManager.Instance;
        var origin = enemy.GridPos;

        Unit closestTarget = null;
        int closestDist = int.MaxValue;

        foreach (var player in _playerUnits.Where(p => p.IsAlive)) {
            int dist = grid.GetDistance(origin, player.GridPos);
            if (dist >= 0 && dist < closestDist) { closestDist = dist; closestTarget = player; }
        }

        if (closestTarget == null) {
            ActionSystem.Instance.ExecuteWait(enemy);
            yield return new WaitForSeconds(0.2f);
            EndUnitTurn();
            yield break;
        }

        int atkRange = enemy.Data.attackRange;
        int targetDist = Mathf.Abs(origin.x - closestTarget.GridPos.x) + Mathf.Abs(origin.y - closestTarget.GridPos.y);

        if (targetDist <= atkRange) {
            // Attack with dice roll animation
            if (BattleController.Instance != null) {
                yield return StartCoroutine(EnemyAttackCoroutine(enemy, closestTarget));
            } else {
                ActionSystem.Instance.ExecuteAttack(enemy, closestTarget);
                yield return new WaitForSeconds(0.5f);
            }
        } else {
            var walkable = ActionRangeCalculator.GetWalkableTiles(origin, enemy.Data.moveRange, grid);
            Tile bestTile = null;
            int bestDist = int.MaxValue;
            foreach (var tile in walkable) {
                int d = Mathf.Abs(tile.GridPos.x - closestTarget.GridPos.x) + Mathf.Abs(tile.GridPos.y - closestTarget.GridPos.y);
                if (d < bestDist) { bestDist = d; bestTile = tile; }
            }
            if (bestTile != null && bestDist < targetDist) {
                yield return StartCoroutine(ActionSystem.Instance.ExecuteMove(enemy, bestTile));
            }
            int newDist = Mathf.Abs(enemy.GridPos.x - closestTarget.GridPos.x) + Mathf.Abs(enemy.GridPos.y - closestTarget.GridPos.y);
            if (newDist <= atkRange) {
                // Attack with dice roll animation
                if (BattleController.Instance != null) {
                    yield return StartCoroutine(EnemyAttackCoroutine(enemy, closestTarget));
                } else {
                    ActionSystem.Instance.ExecuteAttack(enemy, closestTarget);
                    yield return new WaitForSeconds(0.5f);
                }
            } else if (!enemy.HasActedThisTurn) {
                ActionSystem.Instance.ExecuteWait(enemy);
            }
        }

        yield return new WaitForSeconds(0.2f);
        EndUnitTurn();
    }

    IEnumerator EnemyAttackCoroutine(Unit enemy, Unit target) {
        var result = DamageCalculator.CalculateDamage(enemy, target);

        // Show dice roll
        var diceGo = new GameObject("DiceRoll", typeof(DiceRollDisplay), typeof(RectTransform));
        bool diceDone = false;
        diceGo.GetComponent<DiceRollDisplay>().Init(result, () => diceDone = true);
        while (!diceDone) yield return null;

        ActionSystem.Instance.ExecuteAttack(enemy, target, result);

        // Camera shake via BattleController
        var cam = FindObjectOfType<CameraController>();
        if (cam != null && result.hit && result.damage > 0)
            cam.Shake(result.crit ? 0.25f : 0.1f, 0.15f);
    }

    // ===== CONDITIONS =====

    bool CheckVictory() {
        if (_enemyUnits.All(e => !e.IsAlive)) {
            _currentPhase = BattlePhase.Victory;
            OnPhaseChanged?.Invoke(_currentPhase);
            OnBattleEnd?.Invoke(true);
            Debug.Log("=== VICTORY! ===");
            return true;
        }
        return false;
    }

    bool CheckDefeat() {
        if (_playerUnits.All(p => !p.IsAlive)) {
            _currentPhase = BattlePhase.Defeat;
            OnPhaseChanged?.Invoke(_currentPhase);
            OnBattleEnd?.Invoke(false);
            Debug.Log("=== DEFEAT... ===");
            return true;
        }
        return false;
    }

    public void OnUnitDied(Unit unit) {
        _allUnits.Remove(unit);
        if (unit.IsPlayerControlled) _playerUnits.Remove(unit);
        else _enemyUnits.Remove(unit);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
}
