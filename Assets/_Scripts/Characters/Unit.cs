using UnityEngine;
using UnityEngine.UI;

public class Unit : MonoBehaviour {
    [Header("Data")]
    [SerializeField] private UnitData _data;
    [SerializeField] private string _unitName = "Unit";

    [Header("State")]
    [SerializeField] private int _currentHp;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer _sr;
    private Image _hpBarFill;
    private Color _classColor;
    private bool _isDead;
    private Vector3 _baseScale;

    public UnitData Data => _data;
    public Vector2Int GridPos { get; private set; }
    public bool IsAlive => !_isDead && _currentHp > 0;
    public bool IsPlayerControlled => _data != null ? _data.isPlayerControlled : true;
    public int CurrentHp => _currentHp;

    // Initiative
    public int InitiativeRoll { get; set; }
    public void RollInitiative() {
        InitiativeRoll = DiceRoller.RollD20() + (Data?.DexterityMod ?? 0);
    }

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

        _classColor = _data != null ? UIColorPalette.GetClassColor(_data.unitClass) : Color.gray;
        SetupSprite();
        SetupScale();
        SetupHpBar();
    }

    void Start() {
        if (TurnManager.Instance != null)
            TurnManager.Instance.RegisterUnit(this);
    }

    // ===== VISUAL =====

    void SetupSprite() {
        if (_data != null) {
            var cls = _data.unitClass;
            string name = _data.unitName.ToLower();
            bool isPlayer = _data.isPlayerControlled;

            // Warna berdasarkan team: biru untuk ally, merah untuk enemy
            Color body = UIColorPalette.GetTeamColor(isPlayer, false);
            Color accent = UIColorPalette.GetTeamColor(isPlayer, true);

            if (name.Contains("goblin")) {
                _sr.sprite = SpriteFactory.UnitSpriteRaw(
                    TextureFactory.GenerateEnemySprite(16, "goblin", body, accent));
            } else if (name.Contains("skeleton")) {
                _sr.sprite = SpriteFactory.UnitSpriteRaw(
                    TextureFactory.GenerateEnemySprite(16, "skeleton", body, accent));
            } else {
                _sr.sprite = SpriteFactory.UnitSpriteRaw(
                    TextureFactory.GenerateUnitSprite(16, cls, body, accent));
            }
        }
        _sr.sortingOrder = 10;
    }

    void SetupScale() {
        if (_data == null) return;
        string name = _data.unitName.ToLower();
        float s = name.Contains("goblin") ? 0.85f :
                  name.Contains("skeleton") ? 0.95f :
                  _data.unitClass == UnitClass.Fighter ? 1.10f :
                  _data.unitClass == UnitClass.Rogue ? 0.95f : 1.0f;
        _baseScale = new Vector3(s, s, s);
        transform.localScale = _baseScale;
    }

    void SetupHpBar() {
        var hpGo = new GameObject("HpBar");
        hpGo.transform.SetParent(transform);
        hpGo.transform.localPosition = new Vector3(0, 0.7f, 0);

        var canvas = hpGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        var scaler = hpGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 20;

        // Background
        var bgGo = new GameObject("Bg", typeof(Image));
        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.SetParent(canvas.transform, false);
        bgRect.sizeDelta = new Vector2(0.6f, 0.08f);
        bgRect.anchorMin = new Vector2(0.5f, 0);
        bgRect.anchorMax = new Vector2(0.5f, 0);
        bgRect.anchoredPosition = Vector2.zero;
        var bgImg = bgGo.GetComponent<Image>();
        bgImg.color = UIColorPalette.HpBg;

        // Fill
        var fillGo = new GameObject("Fill", typeof(Image));
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.SetParent(bgRect, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.one * 1;
        fillRect.offsetMax = Vector2.one * -1;

        _hpBarFill = fillGo.GetComponent<Image>();
        _hpBarFill.type = Image.Type.Filled;
        _hpBarFill.fillMethod = Image.FillMethod.Horizontal;
        _hpBarFill.fillAmount = 1f;
        _hpBarFill.color = UIColorPalette.HpGreen;
    }

    void UpdateHpBar() {
        if (_hpBarFill == null || _data == null) return;
        float ratio = (float)_currentHp / _data.maxHp;
        _hpBarFill.fillAmount = ratio;
        _hpBarFill.color = UIColorPalette.HpColor(ratio);
    }

    // ===== DATA =====

    public void SetGridPos(Vector2Int pos) { GridPos = pos; }

    public void SetData(UnitData data) {
        _data = data;
        if (data != null) {
            _unitName = data.unitName;
            _currentHp = data.maxHp;
            _classColor = UIColorPalette.GetClassColor(data.unitClass);
            SetupSprite();
            SetupScale();
        }
    }

    // ===== COMBAT =====

    public void TakeDamage(int amount) {
        if (!IsAlive) return;
        if (IsDefending) amount = Mathf.Max(1, Mathf.FloorToInt(amount * 0.5f));

        _currentHp = Mathf.Max(0, _currentHp - amount);
        UpdateHpBar();

        // Flash merah
        if (_sr != null) {
            _sr.color = Color.red;
            Invoke(nameof(RestoreColor), 0.15f);
        }

        if (_currentHp <= 0) Die();
    }

    void RestoreColor() {
        if (_sr != null && IsAlive) {
            _sr.color = _classColor;
        }
    }

    public void Heal(int amount) {
        if (!IsAlive || _data == null) return;
        _currentHp = Mathf.Min(_data.maxHp, _currentHp + amount);
        UpdateHpBar();
    }

    void Die() {
        _isDead = true;
        // Death fade
        var sr = _sr;
        if (sr != null)
            StartCoroutine(FadeAndDestroy(sr));
    }

    System.Collections.IEnumerator FadeAndDestroy(SpriteRenderer renderer) {
        float t = 0;
        Color c = renderer.color;
        while (t < 0.4f) {
            t += Time.deltaTime;
            c.a = 1f - t / 0.4f;
            renderer.color = c;
            yield return null;
        }
        renderer.color = new Color(c.r, c.g, c.b, 0);
        gameObject.SetActive(false);
        GridManager.Instance?.RemoveUnit(this);
    }

    public void ResetTurnState() {
        HasMovedThisTurn = false;
        HasActedThisTurn = false;
        IsDefending = false;
    }

    public void SetDefendingVisual(bool defending) {
        if (_sr == null) return;
        if (defending)
            _sr.color = Color.Lerp(_classColor, UIColorPalette.RoyalBlue, 0.4f);
        else
            _sr.color = _classColor;
    }
}
