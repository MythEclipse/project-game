using UnityEngine;

public class Unit : MonoBehaviour {
    [Header("Data")]
    [SerializeField] private UnitData _data;
    [SerializeField] private string _unitName = "Unit";

    [Header("State")]
    [SerializeField] private int _currentHp;
    private SpriteRenderer _sr;

    public UnitData Data => _data;
    public Vector2Int GridPos { get; private set; }
    public bool IsAlive => _currentHp > 0;
    public bool IsPlayerControlled => _data != null ? _data.isPlayerControlled : true;

    // Turn state
    public bool HasMovedThisTurn { get; set; }
    public bool HasActedThisTurn { get; set; }
    public bool IsDefending { get; set; }

    void Awake() {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();

        if (_data != null) {
            _unitName = _data.unitName;
            _currentHp = _data.maxHp;
        } else {
            _currentHp = 10;
        }

        // Generate sprite warna berdasarkan class
        GenerateUnitSprite();
    }

    void Start() {
        // Register ke TurnManager begitu masuk scene
        if (TurnManager.Instance != null) {
            TurnManager.Instance.RegisterUnit(this);
        }
    }

    void GenerateUnitSprite() {
        var tex = new Texture2D(8, 8);
        Color color = GetClassColor();
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
                tex.SetPixel(x, y, color);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        _sr.sprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), Vector2.one * 0.5f, 8);
        _sr.sortingOrder = 2;
    }

    Color GetClassColor() {
        if (_data == null) return Color.gray;
        return _data.unitClass switch {
            UnitClass.Fighter => new Color(0.8f, 0.2f, 0.2f),  // Merah
            UnitClass.Rogue   => new Color(0.2f, 0.6f, 0.2f),  // Hijau
            UnitClass.Wizard  => new Color(0.2f, 0.3f, 0.8f),  // Biru
            UnitClass.Cleric  => new Color(0.9f, 0.8f, 0.2f),  // Kuning
            _ => Color.gray
        };
    }

    public void SetGridPos(Vector2Int pos) {
        GridPos = pos;
    }

    public void SetData(UnitData data) {
        _data = data;
        if (data != null) {
            _unitName = data.unitName;
            _currentHp = data.maxHp;
        }
    }

    // ===== COMBAT =====

    public int CurrentHp => _currentHp;

    public void TakeDamage(int amount) {
        if (!IsAlive) return;

        if (IsDefending) amount = Mathf.Max(1, Mathf.FloorToInt(amount * 0.5f));

        _currentHp = Mathf.Max(0, _currentHp - amount);

        // Flash merah
        if (_sr != null) {
            _sr.color = Color.red;
            Invoke(nameof(RestoreColor), 0.15f);
        }

        if (_currentHp <= 0) Die();
    }

    void RestoreColor() {
        if (_sr != null && IsAlive) {
            _sr.color = GetClassColor();
        }
    }

    public void Heal(int amount) {
        if (!IsAlive || _data == null) return;
        _currentHp = Mathf.Min(_data.maxHp, _currentHp + amount);
    }

    void Die() {
        gameObject.SetActive(false);
        GridManager.Instance?.RemoveUnit(this);
    }

    public void ResetTurnState() {
        HasMovedThisTurn = false;
        HasActedThisTurn = false;
        IsDefending = false;
    }
}
