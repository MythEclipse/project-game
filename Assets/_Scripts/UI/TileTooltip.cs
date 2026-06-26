using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>Floating tooltip saat hover tile — nampilin nama unit, HP, AC, class.</summary>
public class TileTooltip : MonoBehaviour {
    static TileTooltip _instance;
    public static TileTooltip Instance => _instance;

    Canvas _canvas;
    RectTransform _bgRect;
    Text _label;
    GameObject _root;

    void Awake() {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    public void Init(Canvas hudCanvas) {
        _canvas = hudCanvas;

        // Harus 2 GameObject terpisah: Image + Text (gak bisa satu GameObject karena
        // Image dan Text sama-sama Graphic, Unity cuma izinkan 1 per GameObject)
        var bgGo = new GameObject("TooltipBg", typeof(Image));
        _bgRect = bgGo.GetComponent<RectTransform>();
        _bgRect.SetParent(_canvas.transform, false);
        _bgRect.anchorMin = Vector2.zero;
        _bgRect.anchorMax = Vector2.zero;
        _bgRect.pivot = new Vector2(0, 1);
        _bgRect.sizeDelta = new Vector2(160, 60);

        var bg = bgGo.GetComponent<Image>();
        bg.color = new Color(0.06f, 0.04f, 0.02f, 0.92f);
        bg.raycastTarget = false;

        var tGo = new GameObject("TooltipLabel", typeof(Text));
        _label = tGo.GetComponent<Text>();
        var trt = _label.GetComponent<RectTransform>();
        trt.SetParent(_bgRect, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(6, 4);
        trt.offsetMax = new Vector2(-6, -4);

        _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _label.fontSize = 13;
        _label.color = UIColorPalette.ParchmentLight;
        _label.alignment = TextAnchor.UpperLeft;
        _label.supportRichText = true;
        _label.raycastTarget = false;

        _root = bgGo;
        _root.SetActive(false);

        // Subscribe ke grid events
        if (GridManager.Instance != null) {
            GridManager.Instance.OnTileHoverStartEvent += OnTileHoverStart;
            GridManager.Instance.OnTileHoverEndEvent += OnTileHoverEnd;
        }
    }

    void OnTileHoverStart(Tile tile) {
        if (tile.Occupant == null || !tile.Occupant.IsAlive) {
            _root.SetActive(false);
            return;
        }

        var unit = tile.Occupant;
        var d = unit.Data;
        if (d == null) return;

        _label.text = $"<b>{d.unitName}</b>\n" +
                      $"{d.unitClass} | HP {unit.CurrentHp}/{d.maxHp} | AC {d.armorClass}";
        _root.SetActive(true);
    }

    void OnTileHoverEnd(Tile tile) {
        _root.SetActive(false);
    }

    void Update() {
        if (!_root.activeSelf) return;

        var mouse = Mouse.current;
        if (mouse == null) return;
        Vector2 mousePos = mouse.position.ReadValue();
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            mousePos, null, out pos);
        _bgRect.anchoredPosition = pos + new Vector2(12, -12);
    }

    void OnDestroy() {
        if (_instance == this) _instance = null;
        if (GridManager.Instance != null) {
            GridManager.Instance.OnTileHoverStartEvent -= OnTileHoverStart;
            GridManager.Instance.OnTileHoverEndEvent -= OnTileHoverEnd;
        }
    }
}
