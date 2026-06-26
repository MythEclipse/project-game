using UnityEngine;

public class Tile : MonoBehaviour {
    [Header("Visual")]
    [SerializeField] private Color _baseColor = new Color(0.94f, 0.85f, 0.71f);
    [SerializeField] private Color _offsetColor = new Color(0.71f, 0.53f, 0.39f);

    private SpriteRenderer _sr;
    private SpriteRenderer _highlightSr;
    private GameObject _highlight;
    private BoxCollider2D _collider;

    public Vector2Int GridPos { get; private set; }
    public Unit Occupant { get; private set; }
    public bool IsWalkable => Occupant == null;
    public bool IsOffset { get; private set; }

    void Awake() {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
    }

    public void Init(Vector2Int pos, bool isOffset) {
        GridPos = pos;
        IsOffset = isOffset;

        // Warna checkerboard
        _sr.color = isOffset ? _offsetColor : _baseColor;

        // BoxCollider2D untuk mendeteksi klik
        _collider = GetComponent<BoxCollider2D>();
        if (_collider == null) _collider = gameObject.AddComponent<BoxCollider2D>();

        // Buat highlight visual
        CreateHighlight();
        SetHighlight(false, Color.clear);
    }

    void CreateHighlight() {
        _highlight = new GameObject("Highlight");
        _highlight.transform.SetParent(transform);
        _highlight.transform.localPosition = Vector3.zero;
        _highlight.transform.localScale = Vector3.one * 0.95f;

        _highlightSr = _highlight.AddComponent<SpriteRenderer>();
        _highlightSr.sprite = GenerateWhiteSquare();
        _highlightSr.sortingOrder = 1;
        _highlightSr.color = Color.clear;
    }

    Sprite GenerateWhiteSquare() {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
    }

    public void SetHighlight(bool active, Color color) {
        if (_highlight == null || _highlightSr == null) return;
        _highlight.SetActive(active);
        if (active) _highlightSr.color = color;
    }

    public void SetOccupant(Unit unit) {
        Occupant = unit;
    }

    public void ClearOccupant() {
        Occupant = null;
    }

    void OnMouseDown() {
        // Beri tahu GridManager bahwa tile ini diklik
        GridManager.Instance?.OnTileClicked(this);
    }

    void OnMouseEnter() {
        GridManager.Instance?.OnTileHoverStart(this);
    }

    void OnMouseExit() {
        GridManager.Instance?.OnTileHoverEnd(this);
    }
}
