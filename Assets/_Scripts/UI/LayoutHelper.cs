using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>Fungsi helper untuk layout Unity UI yang responsive.</summary>
public static class LayoutHelper {

    public static void EnsureEventSystem() {
        if (GameObject.FindObjectOfType<EventSystem>() == null) {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>();
        }
    }

    /// <summary>Buat canvas ScreenSpaceOverlay, reference 1920x1080, match height.</summary>
    public static Canvas CreateCanvas(string name) {
        EnsureEventSystem();
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f; // scaling by height = lebih konsisten
        return canvas;
    }

    /// <summary>Panel stretch penuh (fill parent).</summary>
    public static Image CreateFill(Transform parent, Color color) {
        var go = new GameObject("Fill", typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>Panel top bar — stretch horizontal di atas.</summary>
    public static Image CreateTopBar(Transform parent, float height, Color color) {
        var go = new GameObject("TopBar", typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(0, height);
        rt.anchoredPosition = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>Panel bottom bar — stretch horizontal di bawah.</summary>
    public static Image CreateBottomBar(Transform parent, float height, Color color) {
        var go = new GameObject("BotBar", typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.sizeDelta = new Vector2(0, height);
        rt.anchoredPosition = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>Panel side — anchor di kanan, full height.</summary>
    public static Image CreateRightPanel(Transform parent, float width, float margin, Color color) {
        var go = new GameObject("SidePanel", typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 0.5f);
        rt.sizeDelta = new Vector2(width, 0);
        rt.anchoredPosition = new Vector2(-margin, 0);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>Text label — stretch penuh di parent.</summary>
    public static Text CreateText(Transform parent, string text, int size, Color color,
                                   TextAnchor align = TextAnchor.MiddleCenter,
                                   bool raycast = false) {
        var go = new GameObject("Text", typeof(Text));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        var txt = go.GetComponent<Text>();
        txt.text = text;
        txt.fontSize = size;
        txt.color = color;
        txt.alignment = align;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.supportRichText = true;
        txt.raycastTarget = raycast;
        return txt;
    }

    /// <summary>Text label — anchor (0.5,1) dari atas, fixed size.</summary>
    public static Text CreateTextFromTop(Transform parent, string text, int size, Color color,
                                          float x, float y, float w, float h,
                                          TextAnchor align = TextAnchor.UpperLeft,
                                          bool raycast = false) {
        var go = new GameObject("Text", typeof(Text));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
        var txt = go.GetComponent<Text>();
        txt.text = text;
        txt.fontSize = size;
        txt.color = color;
        txt.alignment = align;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.supportRichText = true;
        txt.raycastTarget = raycast;
        return txt;
    }

    /// <summary>Buat tombol — anchor tengah, fixed size.</summary>
    public static (Image img, Button btn) CreateButton(Transform parent, string label, float w, float h,
                                                        Color bg, Color fg, int fontSize,
                                                        float posX, float posY,
                                                        UnityEngine.Events.UnityAction onClick) {
        var go = new GameObject("Btn", typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(posX, posY);

        var img = go.GetComponent<Image>();
        img.color = bg;
        img.raycastTarget = true;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        var cb = btn.colors;
        cb.highlightedColor = Color.Lerp(bg, Color.white, 0.3f);
        cb.pressedColor = Color.Lerp(bg, Color.black, 0.3f);
        btn.colors = cb;
        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;
        btn.onClick.AddListener(onClick);

        var t = CreateText(go.transform, label, fontSize, fg);
        return (img, btn);
    }

    /// <summary>Buat HP bar — anchor (0.5,1) dari atas.</summary>
    public static (Image bg, Image fill) CreateHPBar(Transform parent, float w, float h, float posY) {
        var go = new GameObject("HPBar", typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(0, posY);

        var bg = go.GetComponent<Image>();
        bg.color = UIColorPalette.HpBg;
        bg.raycastTarget = false;

        var fgo = new GameObject("Fill", typeof(Image));
        var fr = fgo.GetComponent<RectTransform>();
        fr.SetParent(rt, false);
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fr.offsetMin = Vector2.one * 1;
        fr.offsetMax = Vector2.one * -1;
        var fill = fgo.GetComponent<Image>();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 1f;
        fill.color = UIColorPalette.HpGreen;
        fill.raycastTarget = false;

        return (bg, fill);
    }

    /// <summary>Separator horizontal.</summary>
    public static Image CreateSeparator(Transform parent, float w, float posY) {
        var go = new GameObject("Sep", typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(w, 1);
        rt.anchoredPosition = new Vector2(0, posY);
        var img = go.GetComponent<Image>();
        img.color = new Color(0.6f, 0.5f, 0.2f, 0.4f);
        img.raycastTarget = false;
        return img;
    }
}
