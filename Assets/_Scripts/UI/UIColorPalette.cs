using UnityEngine;

public static class UIColorPalette {
    // --- Panel & Background ---
    public static readonly Color ParchmentLight = new Color(0.96f, 0.90f, 0.78f); // #F5E6C8
    public static readonly Color ParchmentDark  = new Color(0.86f, 0.76f, 0.60f); // #DCC299
    public static readonly Color StoneDark      = new Color(0.16f, 0.16f, 0.16f); // #2A2A2A
    public static readonly Color StoneMid       = new Color(0.35f, 0.35f, 0.35f); // #595959
    public static readonly Color StoneLight     = new Color(0.55f, 0.55f, 0.55f); // #8C8C8C

    // --- Accents ---
    public static readonly Color GoldTrim    = new Color(0.79f, 0.64f, 0.29f); // #C9A34B
    public static readonly Color DeepRed     = new Color(0.55f, 0.00f, 0.00f); // #8B0000
    public static readonly Color RoyalBlue   = new Color(0.12f, 0.23f, 0.37f); // #1E3A5F
    public static readonly Color ForestGreen = new Color(0.13f, 0.33f, 0.13f); // #215421

    // --- HP Bar ---
    public static readonly Color HpGreen  = new Color(0.00f, 0.80f, 0.00f);
    public static readonly Color HpYellow = new Color(0.80f, 0.80f, 0.00f);
    public static readonly Color HpRed    = new Color(0.80f, 0.00f, 0.00f);
    public static readonly Color HpBg     = new Color(0.10f, 0.10f, 0.10f);

    // --- UI Overlay ---
    public static readonly Color PanelBg     = new Color(0.10f, 0.10f, 0.10f, 0.85f);
    public static readonly Color PanelBg2    = new Color(0.16f, 0.14f, 0.10f, 0.90f);

    // --- Damage Colors ---
    public static readonly Color DamageHit  = Color.white;
    public static readonly Color DamageCrit = Color.yellow;
    public static readonly Color DamageMiss = new Color(0.60f, 0.60f, 0.60f);
    public static readonly Color DamageHeal = new Color(0.00f, 1.00f, 0.30f);

    public static Color GetClassColor(UnitClass cls) => cls switch {
        UnitClass.Fighter => new Color(0.85f, 0.15f, 0.15f), // more saturated red
        UnitClass.Rogue   => new Color(0.15f, 0.65f, 0.15f), // more saturated green
        UnitClass.Wizard  => new Color(0.15f, 0.35f, 0.90f), // brighter blue
        UnitClass.Cleric  => new Color(0.95f, 0.85f, 0.15f), // brighter gold
        _                 => Color.gray
    };

    public static Color GetAccentColor(UnitClass cls) => cls switch {
        UnitClass.Fighter => new Color(0.55f, 0.08f, 0.08f), // darker red (armor)
        UnitClass.Rogue   => new Color(0.10f, 0.35f, 0.10f), // dark green (hood)
        UnitClass.Wizard  => new Color(0.50f, 0.10f, 0.80f), // purple arcane (robe)
        UnitClass.Cleric  => new Color(0.80f, 0.70f, 0.15f), // muted gold (vestments)
        _                 => Color.gray
    };

    // Team colors: biru untuk ally, merah untuk enemy
    public static readonly Color AllyBlue    = new Color(0.25f, 0.40f, 0.80f); // medium blue
    public static readonly Color AllyDark    = new Color(0.15f, 0.25f, 0.55f); // dark blue for accents
    public static readonly Color EnemyRed    = new Color(0.80f, 0.20f, 0.15f); // medium red
    public static readonly Color EnemyDark   = new Color(0.55f, 0.10f, 0.08f); // dark red for accents

    public static Color GetTeamColor(bool isPlayer, bool dark = false) {
        if (isPlayer) return dark ? AllyDark : AllyBlue;
        return dark ? EnemyDark : EnemyRed;
    }

    public static Color HpColor(float ratio) {
        if (ratio > 0.66f) return HpGreen;
        if (ratio > 0.33f) return HpYellow;
        return HpRed;
    }
}
