using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared UI helpers — menggantikan duplikasi GetHUDCanvas di DamagePopup & DiceRollDisplay.</summary>
public static class UIHelper {

    /// <summary>Cari HUD Canvas (ScreenSpaceOverlay).</summary>
    public static Canvas FindHUDCanvas() {
        foreach (var c in Object.FindObjectsOfType<Canvas>())
            if (c.renderMode == RenderMode.ScreenSpaceOverlay) return c;
        return null;
    }

    /// <summary>Konversi world position ke canvas-local position (handle scaling).</summary>
    public static Vector2 WorldToCanvasPos(Canvas canvas, Vector3 worldPos) {
        if (canvas == null) return Vector2.zero;
        Vector3 sp = Camera.main.WorldToScreenPoint(worldPos);
        var canvasRect = canvas.transform as RectTransform;
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out localPos);
        return localPos;
    }
}
