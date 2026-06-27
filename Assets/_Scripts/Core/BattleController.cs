using System.Collections.Generic;
using System.Collections;
using System.Linq;
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
    private Coroutine _attackAnimCoroutine;
    private bool _isAnimating;
    private AttackResult _lastAttackResult;
    private Unit _lastDefender;
    private SpellData _pendingSpell;

    private static readonly Color MoveColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
    private static readonly Color AttackColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);
    private static readonly Color SelectColor = new Color(0.2f, 0.4f, 0.8f, 0.5f);

    void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() {
        // Ensure LevelManager exists (auto-created if not in scene)
        LevelManager.EnsureExists();

        if (GridManager.Instance != null)
            GridManager.Instance.OnTileClickedEvent += HandleTileClicked;

        if (_turnManager != null) {
            _turnManager.OnPhaseChanged += OnPhaseChanged;
            _turnManager.OnActiveUnitChanged += OnActiveUnitChanged;
            _turnManager.OnBattleEnd += OnBattleEnd;
        }

        if (_actionSystem != null)
            _actionSystem.OnAttackPerformed += OnAttackPerformed;

        // Init HUD (show level select first — don't auto-spawn)
        _battleHud = new GameObject("BattleHUD").AddComponent<BattleHUD>();
        _battleHud.Init();
        _battleHud.ShowLevelSelect();
    }

    /// <summary>Start battle at a specific level (called from level select UI).</summary>
    public void BeginBattleAtLevel(int level) {
        if (LevelManager.Instance != null)
            LevelManager.Instance.SetLevel(level);
        Invoke(nameof(SpawnUnits), 0.1f);
    }

    void SpawnUnits() {
        if (_unitPrefab == null || _playerUnitParent == null || _enemyUnitParent == null) return;

        var allData = new List<UnitData>();

        // Production: load dari Resources (wajib ada folder Assets/Resources/Data/Characters/)
        var loaded = Resources.LoadAll<UnitData>("Data/Characters");
        if (loaded != null && loaded.Length > 0) {
            allData.AddRange(loaded);
        }

#if UNITY_EDITOR
        // Editor: fallback ke AssetDatabase (biar support asset yang belum di Resources)
        if (allData.Count == 0) {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:UnitData");
            foreach (var guid in guids) {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var data = UnityEditor.AssetDatabase.LoadAssetAtPath<UnitData>(path);
                if (data != null) allData.Add(data);
            }
        }
#endif

        var playerData = new List<UnitData>();
        var enemyData = new List<UnitData>();
        foreach (var d in allData) {
            if (d.isPlayerControlled) playerData.Add(d); else enemyData.Add(d);
        }

        if (playerData.Count == 0 || enemyData.Count == 0) {
            Debug.LogWarning("Need both player and enemy UnitData assets. Place UnitData assets in Assets/Resources/Data/Characters/");
            return;
        }

        int level = 1;
        if (LevelManager.Instance != null) {
            level = LevelManager.Instance.CurrentLevel;
        }

        var grid = GridManager.Instance;
        int gridH = grid.Height;
        int playerRow = gridH / 2 - 1;
        int enemyRow = gridH / 2 + 1;
        int spacing = 3;

        // Gunakan EnemyCountForLevel untuk menentukan jumlah musuh
        int enemyCount = LevelManager.Instance != null
            ? LevelManager.Instance.EnemyCountForLevel()
            : enemyData.Count;

        int startX = (grid.Width - (Mathf.Max(playerData.Count, enemyCount) - 1) * spacing) / 2;

        // Spawn player units (tetap 4 atau sesuai data)
        for (int i = 0; i < playerData.Count && i < 4; i++) {
            var unit = Instantiate(_unitPrefab, _playerUnitParent);
            unit.SetData(playerData[i]);
            int xPos = startX + i * spacing;
            if (grid.PlaceUnit(unit, new Vector2Int(xPos, playerRow)))
                unit.name = playerData[i].unitName;
        }

        // Spawn enemy units — scaled dengan level
        int enemySpawnCount = Mathf.Min(enemyData.Count, enemyCount);
        for (int i = 0; i < enemySpawnCount; i++) {
            var unit = Instantiate(_unitPrefab, _enemyUnitParent);

            // Buat scaled copy per level
            var scaledData = enemyData[i].CreateScaledCopy(
                LevelManager.Instance.HpMultiplier(),
                LevelManager.Instance.DamageBonus(),
                LevelManager.Instance.AcBonus()
            );
            scaledData.unitName = $"{enemyData[i].unitName} Lv{level}";
            unit.SetData(scaledData);

            int xPos = startX + i * spacing;
            if (grid.PlaceUnit(unit, new Vector2Int(xPos, enemyRow)))
                unit.name = scaledData.unitName;
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

        if (playerWon && LevelManager.Instance != null) {
            LevelManager.Instance.AdvanceLevel();
            Debug.Log($"Advancing to Level {LevelManager.Instance.CurrentLevel}");
        }
    }

    /// <summary>
    /// Clear the battlefield and respawn units at the current level.
    /// Called after victory to advance to the next level.
    /// </summary>
    public void StartNewBattle() {
        // DestroyImmediate agar unit langsung terhapus (tidak deferred)
        var toDestroy = new List<GameObject>();
        foreach (Transform child in _playerUnitParent) toDestroy.Add(child.gameObject);
        foreach (Transform child in _enemyUnitParent) toDestroy.Add(child.gameObject);
        foreach (var go in toDestroy) DestroyImmediate(go);

        // Bersihkan grid occupants
        var grid = GridManager.Instance;
        if (grid != null) {
            foreach (var kvp in grid.Tiles) {
                kvp.Value.ClearOccupant();
            }
        }

        ClearHighlights();
        _selectedUnit = null;
        _isWaitingForTarget = false;

        // Hapus combat log entries
        var combatLog = FindObjectOfType<CombatLog>();
        if (combatLog != null) combatLog.Clear();

        // Reset turn state dan spawn baru
        if (_turnManager != null) _turnManager.ResetForNewBattle();
        SpawnUnits();
    }

    void OnAttackPerformed(Unit attacker, Unit defender, AttackResult result) {
        var cam = FindObjectOfType<CameraController>();
        if (cam != null && result.hit && result.damage > 0)
            cam.Shake(result.crit ? 0.25f : 0.1f, 0.15f);
    }

    /// <summary>Animated attack: dice roll → apply → auto-end-turn.</summary>
    public void DoAttackAnim(Unit attacker, Unit defender) {
        if (_attackAnimCoroutine != null) StopCoroutine(_attackAnimCoroutine);
        _attackAnimCoroutine = StartCoroutine(ExecuteAttackAnim(attacker, defender));
    }

    IEnumerator ExecuteAttackAnim(Unit attacker, Unit defender) {
        _isAnimating = true;
        _turnManager.SubState = TurnSubState.Animating;
        ClearHighlights();

        // Hitung damage
        var result = DamageCalculator.CalculateDamage(attacker, defender);

        // Show dice roll UI
        var diceGo = new GameObject("DiceRoll", typeof(DiceRollDisplay), typeof(RectTransform));
        bool diceDone = false;
        diceGo.GetComponent<DiceRollDisplay>().Init(result, () => diceDone = true);
        while (!diceDone) yield return null;

        // Apply damage
        _actionSystem.ExecuteAttack(attacker, defender, result);

        _isAnimating = false;
        _turnManager.SubState = TurnSubState.SelectingAction;

        // Auto-end-turn jika sudah abis action points
        if (attacker.HasMovedThisTurn && attacker.HasActedThisTurn) {
            yield return new WaitForSeconds(0.3f);
            _turnManager.EndUnitTurn();
        }
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
        if (_isAnimating) return; // block input during dice roll

        var active = _turnManager.ActiveUnit;
        if (active == null || !active.IsAlive) return;
        if (!active.IsPlayerControlled) return;
        _selectedUnit = active;

        if (_isWaitingForTarget) { HandleTargetSelection(clickedTile); return; }

        // Klik musuh → serang
        if (clickedTile.Occupant != null && !clickedTile.Occupant.IsPlayerControlled && clickedTile.Occupant.IsAlive
            && _actionSystem.CanAttackTarget(_selectedUnit, clickedTile)) {
            DoAttackAnim(_selectedUnit, clickedTile.Occupant);
            return;
        }

        // Klik tile kosong → move
        if (!_selectedUnit.HasMovedThisTurn && clickedTile.IsWalkable) {
            var walkable = ActionRangeCalculator.GetWalkableTiles(_selectedUnit.GridPos, _selectedUnit.Data?.moveRange ?? 5, GridManager.Instance);
            if (walkable.Contains(clickedTile))
                StartCoroutine(_actionSystem.ExecuteMove(_selectedUnit, clickedTile));
        }
    }

    void HandleTargetSelection(Tile clickedTile) {
        switch (_pendingAction) {
            case ActionType.Attack:
                if (clickedTile.Occupant != null && !clickedTile.Occupant.IsPlayerControlled && clickedTile.Occupant.IsAlive
                    && _actionSystem.CanAttackTarget(_selectedUnit, clickedTile)) {
                    DoAttackAnim(_selectedUnit, clickedTile.Occupant);
                    _isWaitingForTarget = false; ClearHighlights();
                    _turnManager.SubState = TurnSubState.SelectingAction;
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
            case ActionType.Spell:
                if (_pendingSpell != null && SpellSystem.CastSpell(_selectedUnit, clickedTile, _pendingSpell, OnSpellResult)) {
                    _isWaitingForTarget = false; ClearHighlights();
                    _turnManager.SubState = TurnSubState.SelectingAction;
                    _pendingSpell = null;
                }
                break;
        }
    }

    // ===== PUBLIC API =====

    public void OnMoveButton() => BeginAction(ActionType.Move);
    public void OnAttackButton() => BeginAction(ActionType.Attack);
    public void OnDefendButton() => BeginAction(ActionType.Defend);
    public void OnWaitButton() => BeginAction(ActionType.Wait);
    public void OnSpellButton(int spellIndex) {
        var cls = _selectedUnit?.Data?.unitClass ?? UnitClass.Fighter;
        if (!SpellSystem.SpellsByClass.TryGetValue(cls, out var spells)) return;
        if (spellIndex < 0 || spellIndex >= spells.Length) return;
        BeginSpell(spells[spellIndex]);
    }

    void BeginAction(ActionType actionType) {
        if (_turnManager.CurrentPhase != BattlePhase.Battle || _selectedUnit == null || !_selectedUnit.IsAlive) return;
        if (!_selectedUnit.IsPlayerControlled) return;
        if (_isAnimating) return; // block input during dice roll
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
            case ActionType.Spell:
                // Handled via OnSpellButton → BeginSpell
                break;
        }
    }

    void BeginSpell(SpellData spell) {
        if (_selectedUnit == null || _selectedUnit.HasActedThisTurn) return;
        _isWaitingForTarget = true;
        _pendingAction = ActionType.Spell;
        _pendingSpell = spell;
        _turnManager.SubState = TurnSubState.SelectingTarget;
        ShowSpellRange(spell);
    }

    void ShowSpellRange(SpellData spell) {
        if (_selectedUnit == null) return;
        var tiles = SpellSystem.GetSpellTargetTiles(_selectedUnit.GridPos, spell, GridManager.Instance);
        Color spellColor = new Color(0.6f, 0.2f, 0.8f, 0.5f); // purple for spells
        foreach (var t in tiles) {
            if (t.Occupant != null && t.Occupant.IsAlive)
                t.SetHighlight(true, spellColor);
            else
                t.SetHighlight(true, new Color(0.6f, 0.2f, 0.8f, 0.2f));
        }
        _highlightedTiles = tiles;
    }

    void OnSpellResult(AttackResult result) {
        // Create floating damage popup like normal attack
        var dp = new GameObject("SpellPopup", typeof(DamagePopup), typeof(RectTransform));
        dp.transform.SetParent(FindObjectOfType<BattleHUD>()?.transform, false);
        // Find target from result
        var defender = _turnManager?.EnemyUnits?.FirstOrDefault(u => u.IsAlive);
        if (defender != null) {
            dp.GetComponent<DamagePopup>().Init(defender.transform.position, result);
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
