using UnityEngine;
using UnityEngine.UI;
using System;

public static class UIFactory {
    public static Canvas CreateCanvas(string name = "BattleHUD") {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    public static GameObject CreatePanel(Transform parent, Vector2 size, Sprite bg, Color tint,
                                          Vector2 anchorMin, Vector2 anchorMax) {
        var go = new GameObject("Panel", typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.sprite = bg;
        img.type = Image.Type.Sliced;
        img.color = tint;
        return go;
    }

    public static GameObject CreateButton(Transform parent, Vector2 size, string label,
                                           Sprite bg, Color textColor, int fontSize,
                                           Action onClick) {
        var go = new GameObject("Button", typeof(Image), typeof(Button));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.sprite = bg;
        img.type = Image.Type.Sliced;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        if (onClick != null) btn.onClick.AddListener(() => onClick());

        // Text child
        var labelGo = CreateLabel(go.transform, label, fontSize, textColor, TextAnchor.MiddleCenter);
        var lr = labelGo.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.sizeDelta = Vector2.zero;

        return go;
    }

    public static GameObject CreateIconButton(Transform parent, Vector2 size, Sprite icon,
                                               Color tint, Action onClick) {
        var go = new GameObject("IconButton", typeof(Image), typeof(Button));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.sprite = icon;
        img.color = tint;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        if (onClick != null) btn.onClick.AddListener(() => onClick());

        return go;
    }

    public static GameObject CreateLabel(Transform parent, string text, int fontSize,
                                          Color color, TextAnchor alignment) {
        var go = new GameObject("Label", typeof(Text));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        var txt = go.GetComponent<Text>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = alignment;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.supportRichText = true;

        return go;
    }

    public static GameObject CreateImage(Transform parent, Vector2 size, Sprite sprite, Color tint) {
        var go = new GameObject("Image", typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = tint;

        return go;
    }

    public static GameObject CreateHPBar(Transform parent, Vector2 size) {
        var go = new GameObject("HPBar", typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;

        var bg = go.GetComponent<Image>();
        bg.color = UIColorPalette.HpBg;

        // Fill child
        var fillGo = new GameObject("Fill", typeof(Image));
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.SetParent(rect, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.one * 1;
        fillRect.offsetMax = Vector2.one * -1;

        var fill = fillGo.GetComponent<Image>();
        fill.color = UIColorPalette.HpGreen;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 1f;

        return go;
    }
}
