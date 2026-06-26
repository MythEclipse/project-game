using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public static class UIFactory {
    public static void EnsureEventSystem() {
        if (GameObject.FindObjectOfType<EventSystem>() == null) {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    public static Canvas CreateCanvas(string name) {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static GameObject CreatePanel(Transform parent, Vector2 size, Color color,
                                          Vector2 anchorMin, Vector2 anchorMax,
                                          Vector2 anchoredPos) {
        var go = new GameObject("Panel", typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        go.GetComponent<Image>().color = color;
        go.GetComponent<Image>().raycastTarget = false;
        return go;
    }

    public static GameObject CreateButton(Transform parent, Vector2 size, string label,
                                           Color bgColor, Color textColor, int fontSize,
                                           Action onClick) {
        var go = new GameObject("Button", typeof(Image), typeof(Button));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        var cb = btn.colors;
        cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.3f);
        cb.pressedColor = Color.Lerp(bgColor, Color.black, 0.3f);
        btn.colors = cb;
        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        if (onClick != null) btn.onClick.AddListener(() => onClick());

        var l = CreateLabel(go.transform, label, fontSize, textColor, TextAnchor.MiddleCenter);
        var lr = l.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;

        return go;
    }

    public static GameObject CreateLabel(Transform parent, string text, int fontSize,
                                          Color color, TextAnchor alignment) {
        var go = new GameObject("Label", typeof(Text));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        var txt = go.GetComponent<Text>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = alignment;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.supportRichText = true;
        txt.raycastTarget = false;
        return go;
    }

    public static GameObject CreateImage(Transform parent, Vector2 size, Color tint) {
        var go = new GameObject("Image", typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = tint;
        img.raycastTarget = false;
        return go;
    }

    public static GameObject CreateHPBar(Transform parent, Vector2 size) {
        var go = CreateImage(parent, size, UIColorPalette.HpBg);
        var fillGo = new GameObject("Fill", typeof(Image));
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.SetParent(go.GetComponent<RectTransform>(), false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.one * 1;
        fillRect.offsetMax = Vector2.one * -1;
        var fill = fillGo.GetComponent<Image>();
        fill.color = UIColorPalette.HpGreen;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 1f;
        fill.raycastTarget = false;
        return go;
    }
}
