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

        // === TARGET PRIORITIZATION ===
        // Prioritaskan: low-HP > Wizard/Cleric > Fighter > tanky
        var validTargets = _playerUnits.Where(p => p.IsAlive).ToList();
        if (validTargets.Count == 0) {
            ActionSystem.Instance.ExecuteWait(enemy);
            yield return new WaitForSeconds(0.2f);
            EndUnitTurn();
            yield break;
        }

        Unit bestTarget = null;
        int bestScore = int.MinValue;
        foreach (var p in validTargets) {
            int dist = grid.GetDistance(origin, p.GridPos);
            if (dist < 0) continue;
            int score = 0;
            // Prioritize low HP
            float hpPct = (float)p.CurrentHp / p.Data.maxHp;
            score += Mathf.RoundToInt((1f - hpPct) * 20);
            // Prioritize squishy classes
            if (p.Data.unitClass == UnitClass.Wizard) score += 5;
            if (p.Data.unitClass == UnitClass.Cleric) score += 4;
            if (p.Data.unitClass == UnitClass.Rogue) score += 2;
            // Pinalti jarak
            score -= dist;
            if (score > bestScore) { bestScore = score; bestTarget = p; }
        }

        if (bestTarget == null) {
            ActionSystem.Instance.ExecuteWait(enemy);
            yield return new WaitForSeconds(0.2f);
            EndUnitTurn();
            yield break;
        }

        int atkRange = enemy.Data.attackRange;
        int targetDist = Mathf.Abs(origin.x - bestTarget.GridPos.x) + Mathf.Abs(origin.y - bestTarget.GridPos.y);
        float hpPercent = (float)enemy.CurrentHp / enemy.Data.maxHp;

        // === FLEE BEHAVIOR: low HP → move away from nearest threat ===
        if (hpPercent < 0.25f && targetDist < atkRange + 2) {
            // Try to move away
            var walkable = ActionRangeCalculator.GetWalkableTiles(origin, enemy.Data.moveRange, grid);
            Tile retreatTile = null;
            int retreatDist = int.MinValue;
            foreach (var tile in walkable) {
                int d = Mathf.Abs(tile.GridPos.x - bestTarget.GridPos.x) + Mathf.Abs(tile.GridPos.y - bestTarget.GridPos.y);
                if (d > retreatDist) { retreatDist = d; retreatTile = tile; }
            }
            if (retreatTile != null && retreatDist > targetDist) {
                yield return StartCoroutine(ActionSystem.Instance.ExecuteMove(enemy, retreatTile));
            }
            // After retreating, defend instead of attack
            ActionSystem.Instance.ExecuteDefend(enemy);
            enemy.SetDefendingVisual(true);
            yield return new WaitForSeconds(0.2f);
            EndUnitTurn();
            yield break;
        }

        if (targetDist <= atkRange) {
            yield return StartCoroutine(EnemyAttackCoroutine(enemy, bestTarget));
        } else {
            var walkable = ActionRangeCalculator.GetWalkableTiles(origin, enemy.Data.moveRange, grid);
            Tile bestTile = null;
            int bestDist = int.MaxValue;
            foreach (var tile in walkable) {
                int d = Mathf.Abs(tile.GridPos.x - bestTarget.GridPos.x) + Mathf.Abs(tile.GridPos.y - bestTarget.GridPos.y);
                if (d < bestDist) { bestDist = d; bestTile = tile; }
            }
            if (bestTile != null && bestDist < targetDist) {
                yield return StartCoroutine(ActionSystem.Instance.ExecuteMove(enemy, bestTile));
            }
            int newDist = Mathf.Abs(enemy.GridPos.x - bestTarget.GridPos.x) + Mathf.Abs(enemy.GridPos.y - bestTarget.GridPos.y);
            if (newDist <= atkRange) {
                yield return StartCoroutine(EnemyAttackCoroutine(enemy, bestTarget));
            } else if (!enemy.HasActedThisTurn) {
                // Strategic defend: if can't attack, defend for +4 AC
                ActionSystem.Instance.ExecuteDefend(enemy);
                enemy.SetDefendingVisual(true);
            }
        }

        yield return new WaitForSeconds(0.2f);
        EndUnitTurn();
    }

    IEnumerator EnemyAttackCoroutine(Unit enemy, Unit target) {
        // Check if enemy has shield buff — apply +5 AC for this attack
        if (target.IsShielded) {
            Debug.Log($"{target.Data.unitName}'s Shield of Faith is active!");
        }

        var result = DamageCalculator.CalculateDamage(enemy, target);

        // Check if target is blessed (reroll first miss once)
        if (!result.hit && result.miss == false && target.IsBlessed) {
            // Bless gives +1d4 to AC for this attack — re-check
            int blessBonus = DiceRoller.Roll(4);
            if (result.totalAttack + blessBonus >= result.targetAC) {
                result.hit = true;
                result.damage = DiceRoller.RollDamage(enemy.Data.weaponDamage);
                result.log = $"<color=white>Hit!</color> (blessed! +{blessBonus} on attack)";
            }
            target.IsBlessed = false; // bless consumed
        }

        // Show dice roll
        var diceGo = new GameObject("DiceRoll", typeof(DiceRollDisplay), typeof(RectTransform));
        bool diceDone = false;
        diceGo.GetComponent<DiceRollDisplay>().Init(result, () => diceDone = true);
        while (!diceDone) yield return null;

        ActionSystem.Instance.ExecuteAttack(enemy, target, result);

        // Camera shake
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

    /// <summary>
    /// Reset turn manager state for a new battle (no scene reload).
    /// Clears the turn queue and re-initializes from whatever units exist.
    /// </summary>
    public void ResetForNewBattle() {
        _turnQueue.Clear();
        _currentQueueIndex = -1;
        SubState = TurnSubState.Idle;
        ActiveUnit = null;
        _currentPhase = BattlePhase.Placement;
        OnPhaseChanged?.Invoke(_currentPhase);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
}
