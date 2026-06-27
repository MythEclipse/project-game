using UnityEngine;

/// <summary>
/// Tracks the current difficulty level and provides scaling formulas:
/// - More enemies as level increases
/// - Enemy stats (HP, damage, AC) scale up
/// - Resets to level 1 on defeat
/// </summary>
public class LevelManager : MonoBehaviour {
    public static LevelManager Instance { get; private set; }

    [Header("Current Level")]
    [SerializeField] private int _currentLevel = 1;

    [Header("Scaling")]
    [SerializeField] private int _maxLevel = 10;
    [SerializeField] private int _baseEnemyCount = 3;    // level 1 enemy count
    [SerializeField] private int _extraEnemiesPerLevel = 1;
    [SerializeField] private int _maxEnemies = 8;
    [SerializeField] private float _hpMultPerLevel = 0.25f;
    [SerializeField] private int _dmgBonusPerLevel = 1;
    [SerializeField] private int _acBonusEveryNLevels = 2; // +1 AC every 2 levels

    public int CurrentLevel => _currentLevel;
    public int MaxLevel => _maxLevel;

    /// <summary>Auto-create singleton if it doesn't exist yet.</summary>
    public static LevelManager EnsureExists() {
        if (Instance != null) return Instance;
        var go = new GameObject("LevelManager");
        DontDestroyOnLoad(go);
        return go.AddComponent<LevelManager>();
    }

    void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Number of enemies to spawn at the current level.</summary>
    public int EnemyCountForLevel() {
        return Mathf.Min(_baseEnemyCount + (_currentLevel - 1) * _extraEnemiesPerLevel, _maxEnemies);
    }

    /// <summary>HP multiplier for enemy units at the current level.</summary>
    public float HpMultiplier() {
        return 1f + (_currentLevel - 1) * _hpMultPerLevel;
    }

    /// <summary>Flat damage bonus added to enemy weapon damage at the current level.</summary>
    public int DamageBonus() {
        return (_currentLevel - 1) * _dmgBonusPerLevel;
    }

    /// <summary>Flat AC bonus for enemy units at the current level.</summary>
    public int AcBonus() {
        return (_currentLevel - 1) / _acBonusEveryNLevels;
    }

    /// <summary>Advance to the next level, capped at MaxLevel.</summary>
    public void AdvanceLevel() {
        if (_currentLevel < _maxLevel) {
            _currentLevel++;
            Debug.Log($"=== LEVEL {_currentLevel} ===");
        }
    }

    /// <summary>Reset to level 1 (on defeat / restart).</summary>
    public void Reset() {
        _currentLevel = 1;
        Debug.Log("=== RESET TO LEVEL 1 ===");
    }

    /// <summary>Jump to a specific level (from level select).</summary>
    public void SetLevel(int level) {
        _currentLevel = Mathf.Clamp(level, 1, _maxLevel);
        Debug.Log($"=== JUMP TO LEVEL {_currentLevel} ===");
    }
}
