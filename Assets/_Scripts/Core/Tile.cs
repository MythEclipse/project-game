using UnityEngine;

public class Tile : MonoBehaviour {
    private SpriteRenderer _sr;
    private SpriteRenderer _highlightSr;
    private GameObject _highlight;
    private BoxCollider2D _collider;

    public Vector2Int GridPos { get; private set; }
    public Unit Occupant { get; private set; }
    public bool IsWalkable => Occupant == null;

    void Awake() {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
    }

    public void Init(Vector2Int pos, bool isOffset) {
        GridPos = pos;

        // Pakai sprite prosedural dari SpriteFactory
        _sr.sprite = isOffset ? SpriteFactory.ParchmentTile : SpriteFactory.StoneTile;
        _sr.sortingOrder = 0;

        // BoxCollider2D untuk mendeteksi klik
        _collider = GetComponent<BoxCollider2D>();
        if (_collider == null) _collider = gameObject.AddComponent<BoxCollider2D>();

        CreateHighlight();
        SetHighlight(false, Color.clear);
    }

    void CreateHighlight() {
        _highlight = new GameObject("Highlight");
        _highlight.transform.SetParent(transform);
        _highlight.transform.localPosition = Vector3.zero;
        _highlight.transform.localScale = Vector3.one * 0.95f;

        _highlightSr = _highlight.AddComponent<SpriteRenderer>();
        _highlightSr.sprite = WhiteSquareSprite();
        _highlightSr.sortingOrder = 1;
        _highlightSr.color = Color.clear;
    }

    static Sprite _whiteCache;
    static Sprite WhiteSquareSprite() {
        if (_whiteCache == null) {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _whiteCache = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1);
        }
        return _whiteCache;
    }

    public void SetHighlight(bool active, Color color) {
        if (_highlight == null || _highlightSr == null) return;
        _highlight.SetActive(active);
        if (active) _highlightSr.color = color;
    }

    public void SetOccupant(Unit unit) { Occupant = unit; }
    public void ClearOccupant() { Occupant = null; }

    void OnMouseDown() {
        GridManager.Instance?.OnTileClicked(this);
    }

    void OnMouseEnter() {
        GridManager.Instance?.OnTileHoverStart(this);
    }

    void OnMouseExit() {
        GridManager.Instance?.OnTileHoverEnd(this);
    }
}
