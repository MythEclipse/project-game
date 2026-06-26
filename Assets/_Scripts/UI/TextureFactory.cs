using UnityEngine;

public static class TextureFactory {
    // ============ NOISE ============

    public static float[,] GeneratePerlinNoise(int width, int height, float scale) {
        var noise = new float[width, height];
        float offsetX = Random.Range(0f, 9999f);
        float offsetY = Random.Range(0f, 9999f);
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                noise[x, y] = Mathf.PerlinNoise(
                    x / scale + offsetX,
                    y / scale + offsetY
                );
            }
        }
        return noise;
    }

    static Texture2D MakeTex(int w, int h) {
        var t = new Texture2D(w, h);
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Clamp;
        return t;
    }

    // ============ TILES ============

    public static Texture2D GenerateStoneTile(int tileSize = 16) {
        var tex = MakeTex(tileSize, tileSize);
        var noise = GeneratePerlinNoise(tileSize, tileSize, 4f);

        for (int x = 0; x < tileSize; x++) {
            for (int y = 0; y < tileSize; y++) {
                float n = noise[x, y] * 0.25f;
                float c = 0.45f - n;

                // Bevel: lebih gelap di bottom dan right
                if (x == tileSize - 1) c -= 0.12f;
                if (y == 0) c -= 0.12f;
                if (x == 0) c += 0.06f;
                if (y == tileSize - 1) c += 0.06f;

                // Random crack
                if (Random.value < 0.02f) c = 0.15f;

                tex.SetPixel(x, y, new Color(c, c, c, 1f));
            }
        }
        tex.Apply();
        return tex;
    }

    public static Texture2D GenerateParchmentTile(int tileSize = 16) {
        var tex = MakeTex(tileSize, tileSize);
        var noise = GeneratePerlinNoise(tileSize, tileSize, 6f);

        for (int x = 0; x < tileSize; x++) {
            for (int y = 0; y < tileSize; y++) {
                float n = noise[x, y] * 0.15f;
                float r = 0.94f - n;
                float g = 0.86f - n;
                float b = 0.72f - n;

                if (x == tileSize - 1) { r -= 0.08f; g -= 0.08f; b -= 0.08f; }
                if (y == 0) { r -= 0.08f; g -= 0.08f; b -= 0.08f; }

                tex.SetPixel(x, y, new Color(r, g, b, 1f));
            }
        }
        tex.Apply();
        return tex;
    }

    public static Texture2D GenerateDarkStoneBg(int width, int height) {
        var tex = MakeTex(width, height);
        var noise = GeneratePerlinNoise(width, height, 4f);

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                float n = noise[x, y] * 0.2f;
                float c = 0.18f - n;
                if (Random.value < 0.005f) c = 0.30f; // speckle
                tex.SetPixel(x, y, new Color(c, c, c, 1f));
            }
        }
        tex.Apply();
        return tex;
    }

    // ============ UNIT SPRITE (16x16 pixel art) ============

    public static Texture2D GenerateUnitSprite(int size, Color bodyColor, Color accentColor) {
        var tex = MakeTex(size, size);

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                tex.SetPixel(x, y, Color.clear);

        int cx = size / 2; // center x = 8 untuk 16px

        // Head — circle radius 3 at top
        DrawCircle(tex, cx, size - 3, 3, bodyColor);

        // Eyes — two dark dots
        DrawPixel(tex, cx - 1, size - 3, Color.black);
        DrawPixel(tex, cx + 1, size - 3, Color.black);

        // Torso — from y=4 to y=10
        for (int y = 4; y < size - 4; y++) {
            int halfW = Mathf.RoundToInt(Mathf.Lerp(4, 5, (y - 4f) / 8f));
            for (int x = cx - halfW; x <= cx + halfW; x++) {
                DrawPixel(tex, x, y, bodyColor);
            }
        }

        // Belt — row 7 with accent
        for (int x = cx - 3; x <= cx + 3; x++)
            DrawPixel(tex, x, 7, accentColor);

        // Belt buckle (gold dot)
        DrawPixel(tex, cx, 7, Color.yellow);

        // Legs — two columns
        for (int y = 1; y <= 3; y++) {
            DrawPixel(tex, cx - 2, y, bodyColor);
            DrawPixel(tex, cx - 1, y, bodyColor);
            DrawPixel(tex, cx + 1, y, bodyColor);
            DrawPixel(tex, cx + 2, y, bodyColor);
        }

        // Feet
        DrawPixel(tex, cx - 3, 0, accentColor);
        DrawPixel(tex, cx - 2, 0, accentColor);
        DrawPixel(tex, cx + 2, 0, accentColor);
        DrawPixel(tex, cx + 3, 0, accentColor);

        // Arms — one pixel each side
        for (int y = 6; y <= 8; y++) {
            DrawPixel(tex, cx - 5, y, bodyColor);
            DrawPixel(tex, cx + 5, y, bodyColor);
        }

        tex.Apply();
        return tex;
    }

    // ============ ICONS ============

    public static Texture2D GenerateSwordIcon(int size, Color color) {
        var tex = MakeTex(size, size);
        int cx = size / 2;

        // Blade — vertical line with horizontal crossguard
        for (int y = 0; y < size; y++)
            DrawPixel(tex, cx, y, color);

        // Crossguard
        for (int x = cx - 2; x <= cx + 2; x++)
            DrawPixel(tex, x, size / 3, color);

        // Handle
        for (int y = 0; y < size / 3 - 1; y++)
            DrawPixel(tex, cx - 1, y, color);

        // Tip
        DrawPixel(tex, cx, size - 1, Color.white);

        tex.Apply();
        return tex;
    }

    public static Texture2D GenerateShieldIcon(int size, Color color) {
        var tex = MakeTex(size, size);
        int cx = size / 2;

        for (int y = 0; y < size; y++) {
            int halfW = Mathf.RoundToInt(Mathf.Lerp(3, 1, (float)y / size)) + 1;
            for (int x = cx - halfW; x <= cx + halfW; x++)
                DrawPixel(tex, x, y, color);
        }
        // Border highlight
        for (int x = cx - 3; x <= cx + 3; x++)
            DrawPixel(tex, x, size - 1, Color.white);

        tex.Apply();
        return tex;
    }

    public static Texture2D GenerateBootIcon(int size, Color color) {
        var tex = MakeTex(size, size);
        int cx = size / 2;

        // Boot shape
        for (int y = 0; y < size / 2; y++) {
            for (int x = cx - 1; x <= cx + 2; x++)
                DrawPixel(tex, x, y, color);
        }
        // Upper
        for (int y = size / 2; y < size; y++) {
            for (int x = cx - 1; x <= cx; x++)
                DrawPixel(tex, x, y, color);
        }

        tex.Apply();
        return tex;
    }

    // ============ UI PANELS ============

    public static Texture2D GenerateRoundedRect(int width, int height, int radius, Color color) {
        var tex = MakeTex(width, height);

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                bool inCornerTL = x < radius && y >= height - radius;
                bool inCornerTR = x >= width - radius && y >= height - radius;
                bool inCornerBL = x < radius && y < radius;
                bool inCornerBR = x >= width - radius && y < radius;

                if (inCornerTL) {
                    float dx = x - radius;
                    float dy = y - (height - radius);
                    tex.SetPixel(x, y, dx * dx + dy * dy <= radius * radius ? color : Color.clear);
                } else if (inCornerTR) {
                    float dx = x - (width - radius);
                    float dy = y - (height - radius);
                    tex.SetPixel(x, y, dx * dx + dy * dy <= radius * radius ? color : Color.clear);
                } else if (inCornerBL) {
                    float dx = x - radius;
                    float dy = y - radius;
                    tex.SetPixel(x, y, dx * dx + dy * dy <= radius * radius ? color : Color.clear);
                } else if (inCornerBR) {
                    float dx = x - (width - radius);
                    float dy = y - radius;
                    tex.SetPixel(x, y, dx * dx + dy * dy <= radius * radius ? color : Color.clear);
                } else {
                    tex.SetPixel(x, y, color);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    public static Texture2D GenerateBorderFrame(int width, int height, int borderWidth, Color color) {
        var tex = MakeTex(width, height);

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                bool isBorder = x < borderWidth || x >= width - borderWidth
                             || y < borderWidth || y >= height - borderWidth;
                tex.SetPixel(x, y, isBorder ? color : Color.clear);
            }
        }
        tex.Apply();
        return tex;
    }

    public static Texture2D GenerateFilledRect(int width, int height, Color color) {
        var tex = MakeTex(width, height);
        Fill(tex, color);
        tex.Apply();
        return tex;
    }

    public static Texture2D GenerateVerticalGradient(int height, Color top, Color bottom) {
        var tex = MakeTex(1, height);
        for (int y = 0; y < height; y++) {
            float t = y / (float)(height - 1);
            tex.SetPixel(0, y, Color.Lerp(bottom, top, t));
        }
        tex.Apply();
        return tex;
    }

    // ============ HELPERS ============

    static void DrawPixel(Texture2D tex, int x, int y, Color c) {
        if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
            tex.SetPixel(x, y, c);
    }

    static void DrawCircle(Texture2D tex, int cx, int cy, int r, Color color) {
        for (int x = -r; x <= r; x++) {
            for (int y = -r; y <= r; y++) {
                if (x * x + y * y <= r * r)
                    DrawPixel(tex, cx + x, cy + y, color);
            }
        }
    }

    static void Fill(Texture2D tex, Color color) {
        var colors = new Color[tex.width * tex.height];
        System.Array.Fill(colors, color);
        tex.SetPixels(colors);
    }
}
