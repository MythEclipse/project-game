using UnityEngine;

public static class SpriteFactory {
    private const float DefaultPpu = 16f;

    private static Sprite _stoneTileCache;
    private static Sprite _parchmentTileCache;
    private static Sprite _bgCache;

    public static Sprite CreateSprite(Texture2D tex, float pixelsPerUnit = DefaultPpu) {
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, pixelsPerUnit);
    }

    // ===== TILES =====

    public static Sprite StoneTile {
        get {
            if (_stoneTileCache == null)
                _stoneTileCache = CreateSprite(TextureFactory.GenerateStoneTile(16));
            return _stoneTileCache;
        }
    }

    public static Sprite ParchmentTile {
        get {
            if (_parchmentTileCache == null)
                _parchmentTileCache = CreateSprite(TextureFactory.GenerateParchmentTile(16));
            return _parchmentTileCache;
        }
    }

    // ===== BACKGROUND =====

    public static Sprite Background {
        get {
            if (_bgCache == null)
                _bgCache = CreateSprite(TextureFactory.GenerateDarkStoneBg(32, 32));
            return _bgCache;
        }
    }

    // ===== UNITS =====

    static Sprite _fighterCache, _rogueCache, _wizardCache, _clericCache, _enemyCache;

    public static Sprite UnitSprite(UnitClass cls) {
        Sprite cache;
        switch (cls) {
            case UnitClass.Fighter: cache = _fighterCache; break;
            case UnitClass.Rogue:   cache = _rogueCache; break;
            case UnitClass.Wizard:  cache = _wizardCache; break;
            case UnitClass.Cleric:  cache = _clericCache; break;
            default:                cache = _enemyCache; break;
        }

        if (cache != null) return cache;

        var color = UIColorPalette.GetClassColor(cls);
        var accent = UIColorPalette.GetAccentColor(cls);
        var tex = TextureFactory.GenerateUnitSprite(16, color, accent);
        cache = CreateSprite(tex);
        return cache;
    }

    // ===== ICONS =====

    static Sprite _swordIcon, _shieldIcon, _bootIcon, _hourglassIcon, _crossIcon;

    public static Sprite GetIcon(IconType type) {
        switch (type) {
            case IconType.Sword: return GetOrCreate(ref _swordIcon, () => TextureFactory.GenerateSwordIcon(16, UIColorPalette.GoldTrim));
            case IconType.Shield: return GetOrCreate(ref _shieldIcon, () => TextureFactory.GenerateShieldIcon(16, UIColorPalette.GoldTrim));
            case IconType.Boot: return GetOrCreate(ref _bootIcon, () => TextureFactory.GenerateBootIcon(16, UIColorPalette.GoldTrim));
            case IconType.Cross: return GetOrCreate(ref _crossIcon, () => TextureFactory.GenerateFilledRect(16, 16, UIColorPalette.DeepRed));
            default: return null;
        }
    }

    static Sprite GetOrCreate(ref Sprite cache, System.Func<Texture2D> factory) {
        if (cache == null) cache = CreateSprite(factory());
        return cache;
    }

    // ===== UI PANELS =====

    public static Sprite CreateRoundedPanel(int w, int h, int r, Color c)
        => CreateSprite(TextureFactory.GenerateRoundedRect(w, h, r, c), 1);

    public static Sprite CreateBorder(int w, int h, int bw, Color c)
        => CreateSprite(TextureFactory.GenerateBorderFrame(w, h, bw, c), 1);

    public static Sprite CreateFilledRect(int w, int h, Color c)
        => CreateSprite(TextureFactory.GenerateFilledRect(w, h, c), 1);
}

public enum IconType { Sword, Shield, Boot, Hourglass, Cross }
