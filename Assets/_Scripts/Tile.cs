using UnityEngine;

public class Tile : MonoBehaviour {
    private SpriteRenderer _sr;
    private Color _baseColor, _offsetColor;
    private GameObject _highlight;

    public void Init(bool isOffset) {
        _baseColor = new Color(0.94f, 0.85f, 0.71f);
        _offsetColor = new Color(0.71f, 0.53f, 0.39f);

        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.color = isOffset ? _offsetColor : _baseColor;
    }

    void OnMouseEnter() {
        if (_highlight != null) _highlight.SetActive(true);
    }

    void OnMouseExit() {
        if (_highlight != null) _highlight.SetActive(false);
    }
}
