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

    // ============ UNIT SPRITE (16x16 pixel art) — class-specific ============

    public static Texture2D GenerateUnitSprite(int size, UnitClass cls, Color bodyColor, Color accentColor) {
        return cls switch {
            UnitClass.Fighter => FighterSprite(size, bodyColor, accentColor),
            UnitClass.Rogue   => RogueSprite(size, bodyColor, accentColor),
            UnitClass.Wizard  => WizardSprite(size, bodyColor, accentColor),
            UnitClass.Cleric  => ClericSprite(size, bodyColor, accentColor),
            _                 => FighterSprite(size, bodyColor, accentColor),
        };
    }

    /// <summary>Generate enemy-specific sprite by name.</summary>
    public static Texture2D GenerateEnemySprite(int size, string enemyType, Color bodyColor, Color accentColor) {
        string t = enemyType.ToLower();
        if (t.Contains("goblin")) return GoblinSprite(size, bodyColor, accentColor);
        if (t.Contains("skeleton")) return SkeletonSprite(size, bodyColor, accentColor);
        return SkeletonSprite(size, bodyColor, accentColor);
    }

    // ─── FIGHTER: Full helm, broad torso, sword ───
    static Texture2D FighterSprite(int size, Color c, Color a) {
        var tex = MakeTex(size, size); Clear(tex);
        int cx = size / 2;

        // Feet (armored boots — wider)
        for (int x = cx - 3; x <= cx + 3; x++) { DrawPixel(tex, x, 0, a); DrawPixel(tex, x, 1, a); }

        // Legs (thick)
        for (int y = 2; y <= 3; y++) for (int x = cx - 2; x <= cx + 2; x++) DrawPixel(tex, x, y, c);

        // Torso (broad — full width 11px)
        for (int y = 4; y <= 8; y++) {
            int hw = y >= 7 ? 4 : 5;
            for (int x = cx - hw; x <= cx + hw; x++) DrawPixel(tex, x, y, c);
        }

        // Chest plate (accent band)
        for (int x = cx - 4; x <= cx + 4; x++) DrawPixel(tex, x, 6, a);

        // Shoulder plates (wide)
        DrawPixel(tex, cx - 6, 8, a); DrawPixel(tex, cx + 6, 8, a);
        DrawPixel(tex, cx - 6, 9, a); DrawPixel(tex, cx + 6, 9, a);

        // Arms
        for (int y = 5; y <= 7; y++) { DrawPixel(tex, cx - 6, y, c); DrawPixel(tex, cx + 6, y, c); }

        // Neck
        DrawPixel(tex, cx, 9, a);

        // Helmet (full enclosure)
        for (int x = cx - 3; x <= cx + 3; x++) {
            DrawPixel(tex, x, 10, a);
            DrawPixel(tex, x, 14, a); // top
        }
        DrawPixel(tex, cx - 3, 11, a); DrawPixel(tex, cx + 3, 11, a);
        DrawPixel(tex, cx - 3, 12, a); DrawPixel(tex, cx + 3, 12, a);
        DrawPixel(tex, cx - 3, 13, a); DrawPixel(tex, cx + 3, 13, a);
        // Helmet visor slit (eye glow)
        DrawPixel(tex, cx - 1, 12, Color.yellow);
        DrawPixel(tex, cx + 1, 12, Color.yellow);
        // Helmet crest
        DrawPixel(tex, cx, 15, a);
        DrawPixel(tex, cx - 1, 15, a); DrawPixel(tex, cx + 1, 15, a);

        // Sword at right side
        DrawPixel(tex, cx + 7, 4, UIColorPalette.GoldTrim);
        DrawPixel(tex, cx + 7, 5, UIColorPalette.GoldTrim);
        DrawPixel(tex, cx + 7, 3, Color.white);
        DrawPixel(tex, cx + 7, 2, Color.white);
        DrawPixel(tex, cx + 7, 1, Color.white);
        DrawPixel(tex, cx + 8, 3, a);

        tex.Apply(); return tex;
    }

    // ─── ROGUE: Hood, slim build, dagger ───
    static Texture2D RogueSprite(int size, Color c, Color a) {
        var tex = MakeTex(size, size); Clear(tex);
        int cx = size / 2;

        // Feet (light boots)
        for (int x = cx - 2; x <= cx + 2; x++) DrawPixel(tex, x, 0, a);
        DrawPixel(tex, cx - 1, 1, a); DrawPixel(tex, cx + 1, 1, a);

        // Legs (narrow)
        for (int x = cx - 1; x <= cx + 1; x++) { DrawPixel(tex, x, 1, c); DrawPixel(tex, x, 2, c); }

        // Torso (slim — 7px wide)
        for (int y = 3; y <= 8; y++) {
            int hw = y >= 6 ? 3 : 4;
            for (int x = cx - hw; x <= cx + hw; x++) DrawPixel(tex, x, y, c);
        }

        // Tunic V-neck
        DrawPixel(tex, cx - 2, 7, a); DrawPixel(tex, cx + 2, 7, a);
        DrawPixel(tex, cx - 1, 8, a); DrawPixel(tex, cx + 1, 8, a);
        DrawPixel(tex, cx, 8, a);

        // Belt
        DrawPixel(tex, cx - 2, 5, a); DrawPixel(tex, cx - 1, 5, a);
        DrawPixel(tex, cx, 5, a); DrawPixel(tex, cx + 1, 5, a); DrawPixel(tex, cx + 2, 5, a);

        // Arms (slim, angled)
        for (int y = 6; y <= 8; y++) { DrawPixel(tex, cx - 5, y, c); DrawPixel(tex, cx + 5, y, c); }

        // Hood (pointy, shadow over eyes)
        for (int x = cx - 3; x <= cx + 3; x++) {
            DrawPixel(tex, x, 9, a);
            DrawPixel(tex, x, 10, a);
            DrawPixel(tex, x, 13, a);
        }
        DrawPixel(tex, cx - 3, 11, a); DrawPixel(tex, cx + 3, 11, a);
        DrawPixel(tex, cx - 3, 12, a); DrawPixel(tex, cx + 3, 12, a);
        DrawPixel(tex, cx - 1, 14, a); DrawPixel(tex, cx + 1, 14, a);
        DrawPixel(tex, cx, 15, a); // hood tip
        // Eyes (menacing glow)
        DrawPixel(tex, cx - 1, 11, Color.red);
        DrawPixel(tex, cx + 1, 11, Color.red);

        // Dagger at belt
        DrawPixel(tex, cx + 6, 3, a);
        DrawPixel(tex, cx + 6, 4, Color.white);
        DrawPixel(tex, cx + 6, 5, Color.white);

        tex.Apply(); return tex;
    }

    // ─── WIZARD: Pointed hat, robe, staff ───
    static Texture2D WizardSprite(int size, Color c, Color a) {
        var tex = MakeTex(size, size); Clear(tex);
        int cx = size / 2;

        // Feet (shoes)
        DrawPixel(tex, cx - 2, 0, a); DrawPixel(tex, cx - 1, 0, a);
        DrawPixel(tex, cx + 1, 0, a); DrawPixel(tex, cx + 2, 0, a);

        // Robe bottom (wide — 9px)
        for (int y = 1; y <= 3; y++) for (int x = cx - 3; x <= cx + 3; x++) DrawPixel(tex, x, y, c);
        for (int x = cx - 4; x <= cx + 4; x++) DrawPixel(tex, x, 1, c);

        // Robe middle
        for (int y = 4; y <= 8; y++) for (int x = cx - 3; x <= cx + 3; x++) DrawPixel(tex, x, y, c);

        // Robe trim (accent)
        for (int x = cx - 3; x <= cx + 3; x++) DrawPixel(tex, x, 3, a);
        for (int x = cx - 2; x <= cx + 2; x++) DrawPixel(tex, x, 7, a);

        // Arcane symbol (star-like)
        DrawPixel(tex, cx, 6, Color.cyan);
        DrawPixel(tex, cx - 1, 6, Color.cyan); DrawPixel(tex, cx + 1, 6, Color.cyan);
        DrawPixel(tex, cx, 5, Color.cyan); DrawPixel(tex, cx, 7, Color.cyan);

        // Arms (hidden in robe, only sleeves)
        DrawPixel(tex, cx - 5, 7, c); DrawPixel(tex, cx - 5, 8, c);
        DrawPixel(tex, cx + 5, 7, c); DrawPixel(tex, cx + 5, 8, c);

        // Head
        DrawCircle(tex, cx, 10, 3, c);
        // Eyes (arcane glow)
        DrawPixel(tex, cx - 1, 10, Color.cyan);
        DrawPixel(tex, cx + 1, 10, Color.cyan);
        // Beard
        DrawPixel(tex, cx, 8, Color.white);
        DrawPixel(tex, cx - 1, 8, Color.white);

        // Pointed hat
        for (int y = 13; y <= 15; y++) {
            int hw = 15 - y;
            for (int x = cx - hw; x <= cx + hw; x++) DrawPixel(tex, x, y, a);
        }
        // Hat brim
        for (int x = cx - 3; x <= cx + 3; x++) DrawPixel(tex, x, 12, a);
        // Star on hat tip
        DrawPixel(tex, cx, 15, Color.yellow);

        // Staff at right
        DrawPixel(tex, cx + 7, 2, UIColorPalette.ParchmentDark);
        DrawPixel(tex, cx + 7, 3, UIColorPalette.ParchmentDark);
        DrawPixel(tex, cx + 7, 4, UIColorPalette.ParchmentDark);
        DrawPixel(tex, cx + 7, 5, UIColorPalette.ParchmentDark);
        DrawPixel(tex, cx + 7, 6, UIColorPalette.ParchmentDark);
        DrawPixel(tex, cx + 7, 7, UIColorPalette.ParchmentDark);
        DrawPixel(tex, cx + 7, 8, UIColorPalette.ParchmentDark);
        // Staff gem
        DrawPixel(tex, cx + 7, 9, Color.cyan);
        DrawPixel(tex, cx + 8, 9, Color.cyan);
        DrawPixel(tex, cx + 7, 10, Color.cyan);

        tex.Apply(); return tex;
    }

    // ─── CLERIC: Mace, cape, holy symbol ───
    static Texture2D ClericSprite(int size, Color c, Color a) {
        var tex = MakeTex(size, size); Clear(tex);
        int cx = size / 2;

        // Feet
        for (int x = cx - 3; x <= cx + 3; x++) DrawPixel(tex, x, 0, a);
        DrawPixel(tex, cx - 2, 1, a); DrawPixel(tex, cx + 2, 1, a);

        // Legs (medium)
        for (int y = 1; y <= 3; y++) for (int x = cx - 2; x <= cx + 2; x++) DrawPixel(tex, x, y, c);

        // Torso
        for (int y = 4; y <= 8; y++) for (int x = cx - 4; x <= cx + 4; x++) DrawPixel(tex, x, y, c);

        // Robe front panel (accent)
        for (int x = cx - 1; x <= cx + 1; x++) { DrawPixel(tex, x, 3, a); DrawPixel(tex, x, 4, a); }
        DrawPixel(tex, cx, 5, a); DrawPixel(tex, cx, 6, a);

        // Cape (flows behind, visible at sides)
        DrawPixel(tex, cx - 5, 4, a); DrawPixel(tex, cx + 5, 4, a);
        DrawPixel(tex, cx - 5, 5, a); DrawPixel(tex, cx + 5, 5, a);
        DrawPixel(tex, cx - 5, 6, a); DrawPixel(tex, cx + 5, 6, a);
        DrawPixel(tex, cx - 5, 7, a); DrawPixel(tex, cx + 5, 7, a);
        DrawPixel(tex, cx - 5, 8, a); DrawPixel(tex, cx + 5, 8, a);

        // Arms
        for (int y = 5; y <= 7; y++) { DrawPixel(tex, cx - 6, y, c); DrawPixel(tex, cx + 6, y, c); }

        // Head
        DrawCircle(tex, cx, 10, 3, c);
        // Eyes (serene)
        DrawPixel(tex, cx - 1, 10, Color.black);
        DrawPixel(tex, cx + 1, 10, Color.black);

        // Hair / tonsure
        DrawPixel(tex, cx, 12, UIColorPalette.ParchmentDark);
        DrawPixel(tex, cx - 1, 12, UIColorPalette.ParchmentDark);
        DrawPixel(tex, cx + 1, 12, UIColorPalette.ParchmentDark);
        DrawPixel(tex, cx, 13, UIColorPalette.ParchmentDark);

        // Mace in right hand
        DrawPixel(tex, cx + 7, 5, a);
        DrawPixel(tex, cx + 7, 6, a);
        DrawPixel(tex, cx + 7, 7, a);
        DrawPixel(tex, cx + 7, 8, a);
        // Mace head
        DrawPixel(tex, cx + 6, 9, UIColorPalette.GoldTrim);
        DrawPixel(tex, cx + 7, 9, UIColorPalette.GoldTrim);
        DrawPixel(tex, cx + 8, 9, UIColorPalette.GoldTrim);
        DrawPixel(tex, cx + 7, 10, UIColorPalette.GoldTrim);
        DrawPixel(tex, cx + 7, 8, UIColorPalette.GoldTrim);

        // Holy symbol on chest
        DrawPixel(tex, cx, 6, Color.yellow);
        DrawPixel(tex, cx - 1, 5, Color.yellow); DrawPixel(tex, cx + 1, 5, Color.yellow);

        tex.Apply(); return tex;
    }

    // ─── GOBLIN ENEMY: small, hunched, pointy ears ───
    static Texture2D GoblinSprite(int size, Color c, Color a) {
        var tex = MakeTex(size, size); Clear(tex);
        int cx = size / 2;

        // Feet (small)
        DrawPixel(tex, cx - 1, 0, a); DrawPixel(tex, cx + 1, 0, a);

        // Legs (short)
        DrawPixel(tex, cx - 1, 1, c); DrawPixel(tex, cx, 1, c); DrawPixel(tex, cx + 1, 1, c);

        // Torso (small, hunched)
        for (int y = 2; y <= 6; y++) for (int x = cx - 3; x <= cx + 3; x++) DrawPixel(tex, x, y, c);

        // Ragged clothes
        DrawPixel(tex, cx - 3, 3, a); DrawPixel(tex, cx + 3, 3, a);
        DrawPixel(tex, cx - 2, 4, a); DrawPixel(tex, cx + 2, 4, a);

        // Head (larger proportionally)
        DrawCircle(tex, cx, 9, 3, new Color(0.5f, 0.7f, 0.3f)); // greenish skin

        // Pointy ears
        DrawPixel(tex, cx - 5, 9, new Color(0.5f, 0.7f, 0.3f));
        DrawPixel(tex, cx - 4, 10, new Color(0.5f, 0.7f, 0.3f));
        DrawPixel(tex, cx + 5, 9, new Color(0.5f, 0.7f, 0.3f));
        DrawPixel(tex, cx + 4, 10, new Color(0.5f, 0.7f, 0.3f));

        // Eyes (red, fierce)
        DrawPixel(tex, cx - 1, 9, Color.red);
        DrawPixel(tex, cx + 1, 9, Color.red);

        // Teeth
        DrawPixel(tex, cx - 1, 7, Color.white);
        DrawPixel(tex, cx + 1, 7, Color.white);

        // Arms (short, holding weapon)
        DrawPixel(tex, cx - 5, 4, c); DrawPixel(tex, cx + 5, 4, c);
        DrawPixel(tex, cx - 5, 5, c); DrawPixel(tex, cx + 5, 5, c);

        // Crude weapon (right hand)
        DrawPixel(tex, cx + 6, 5, a);
        DrawPixel(tex, cx + 6, 6, a);
        DrawPixel(tex, cx + 6, 7, UIColorPalette.GoldTrim);

        tex.Apply(); return tex;
    }

    // ─── SKELETON ENEMY: skull, thin frame ───
    static Texture2D SkeletonSprite(int size, Color c, Color a) {
        var tex = MakeTex(size, size); Clear(tex);
        int cx = size / 2;
        Color bone = new Color(0.85f, 0.82f, 0.75f);

        // Feet (bone)
        DrawPixel(tex, cx - 2, 0, bone); DrawPixel(tex, cx + 2, 0, bone);
        DrawPixel(tex, cx - 1, 1, bone); DrawPixel(tex, cx + 1, 1, bone);
        DrawPixel(tex, cx, 0, bone);

        // Legs (thin)
        DrawPixel(tex, cx - 2, 2, bone); DrawPixel(tex, cx + 2, 2, bone);
        DrawPixel(tex, cx - 2, 3, bone); DrawPixel(tex, cx + 2, 3, bone);
        DrawPixel(tex, cx - 1, 4, bone); DrawPixel(tex, cx + 1, 4, bone);

        // Ribcage
        DrawPixel(tex, cx - 3, 5, bone); DrawPixel(tex, cx + 3, 5, bone);
        DrawPixel(tex, cx - 3, 6, bone); DrawPixel(tex, cx + 3, 6, bone);
        DrawPixel(tex, cx - 3, 7, bone); DrawPixel(tex, cx + 3, 7, bone);
        DrawPixel(tex, cx - 3, 8, bone); DrawPixel(tex, cx + 3, 8, bone);
        // Spine
        DrawPixel(tex, cx, 5, bone); DrawPixel(tex, cx, 6, bone);
        DrawPixel(tex, cx, 7, bone); DrawPixel(tex, cx, 8, bone);

        // Dark void in chest
        DrawPixel(tex, cx - 1, 6, Color.black); DrawPixel(tex, cx + 1, 6, Color.black);
        DrawPixel(tex, cx - 1, 7, Color.black); DrawPixel(tex, cx + 1, 7, Color.black);

        // Arms (bony)
        DrawPixel(tex, cx - 5, 6, bone); DrawPixel(tex, cx + 5, 6, bone);
        DrawPixel(tex, cx - 5, 7, bone); DrawPixel(tex, cx + 5, 7, bone);
        DrawPixel(tex, cx - 5, 8, bone); DrawPixel(tex, cx + 5, 8, bone);

        // Hands
        DrawPixel(tex, cx - 6, 6, bone); DrawPixel(tex, cx + 6, 6, bone);

        // Skull
        DrawCircle(tex, cx, 11, 3, bone);
        // Eye sockets (empty)
        DrawPixel(tex, cx - 1, 11, Color.black);
        DrawPixel(tex, cx + 1, 11, Color.black);
        // Nose hole
        DrawPixel(tex, cx, 10, Color.black);
        // Teeth
        DrawPixel(tex, cx - 1, 9, bone); DrawPixel(tex, cx + 1, 9, bone);
        DrawPixel(tex, cx, 9, Color.black);
        // Skull crack
        DrawPixel(tex, cx, 13, new Color(0.5f, 0.5f, 0.5f));

        // Weapon — rusty blade
        DrawPixel(tex, cx + 7, 6, a);
        DrawPixel(tex, cx + 7, 7, a);
        DrawPixel(tex, cx + 7, 8, a);
        DrawPixel(tex, cx + 7, 9, a);
        DrawPixel(tex, cx + 7, 10, a);

        tex.Apply(); return tex;
    }

    static void Clear(Texture2D tex) {
        for (int x = 0; x < tex.width; x++)
            for (int y = 0; y < tex.height; y++)
                tex.SetPixel(x, y, Color.clear);
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
