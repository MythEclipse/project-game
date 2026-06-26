using System.Collections.Generic;
using UnityEngine;

/// <summary>Spell definitions per class, casting logic, and targeting.</summary>
public enum SpellType {
    None,
    Fireball,       // Wizard — AoE 3x3, Dex save half
    MagicMissile,   // Wizard — auto-hit, 3 projectiles
    HealWounds,     // Cleric — heal ally
    Bless,          // Cleric — +1d4 to next attack
    Shield,         // Cleric — +5 AC reaction
}

[System.Serializable]
public class SpellData {
    public SpellType type;
    public string displayName;
    public string description;
    public int manaCost;
    public int minRange;
    public int maxRange;
    public int areaRadius; // 0 = single target
    public string damageNotation; // e.g. "3d6" for Fireball
    public int healAmount; // for healing spells
    public int duration; // turns (0 = instant)
}

public static class SpellSystem {

    public static readonly Dictionary<UnitClass, SpellData[]> SpellsByClass = new() {
        [UnitClass.Wizard] = new[] {
            new SpellData {
                type = SpellType.Fireball,
                displayName = "Fireball",
                description = "AoE 3×3, Dex save half damage",
                manaCost = 3, minRange = 2, maxRange = 6, areaRadius = 1,
                damageNotation = "3d6",
            },
            new SpellData {
                type = SpellType.MagicMissile,
                displayName = "Magic Missile",
                description = "Auto-hit, 3 bolts",
                manaCost = 2, minRange = 1, maxRange = 6, areaRadius = 0,
                damageNotation = "1d4+1",
            },
        },
        [UnitClass.Cleric] = new[] {
            new SpellData {
                type = SpellType.HealWounds,
                displayName = "Heal Wounds",
                description = "Restore HP to ally",
                manaCost = 2, minRange = 1, maxRange = 3, areaRadius = 0,
                healAmount = 8,
            },
            new SpellData {
                type = SpellType.Bless,
                displayName = "Bless",
                description = "Ally gets +1d4 to next attack",
                manaCost = 1, minRange = 1, maxRange = 4, areaRadius = 0,
                duration = 1,
            },
            new SpellData {
                type = SpellType.Shield,
                displayName = "Shield of Faith",
                description = "Self +5 AC for 2 turns",
                manaCost = 2, minRange = 0, maxRange = 0, areaRadius = 0,
                duration = 2,
            },
        },
    };

    /// <summary>Cek apakah unit class memiliki spell.</summary>
    public static bool HasSpells(UnitClass cls) => SpellsByClass.ContainsKey(cls);

    /// <summary>Dapatkan tiles dalam range untuk targeting spell.</summary>
    public static List<Tile> GetSpellTargetTiles(Vector2Int origin, SpellData spell, GridManager grid) {
        if (spell.areaRadius > 0) {
            // For AoE, show all tiles in maxRange
            return ActionRangeCalculator.GetTilesInRadius(origin, spell.maxRange, grid);
        } else {
            // For single target, show attackable tiles in range
            return ActionRangeCalculator.GetAttackableTiles(origin, spell.maxRange, grid);
        }
    }

    /// <summary>Execute spell cast.</summary>
    public static bool CastSpell(Unit caster, Tile targetTile, SpellData spell, System.Action<AttackResult> onDamage = null) {
        if (caster == null || targetTile == null || spell == null) return false;

        switch (spell.type) {
            case SpellType.Fireball:
                return CastFireball(caster, targetTile, spell, onDamage);
            case SpellType.MagicMissile:
                return CastMagicMissile(caster, targetTile, spell, onDamage);
            case SpellType.HealWounds:
                return CastHealWounds(caster, targetTile, spell);
            case SpellType.Bless:
                return CastBless(caster, targetTile, spell);
            case SpellType.Shield:
                return CastShield(caster, spell);
            default:
                return false;
        }
    }

    static bool CastFireball(Unit caster, Tile centerTile, SpellData spell, System.Action<AttackResult> onDamage) {
        var grid = GridManager.Instance;
        if (grid == null) return false;

        // Hit all units in radius
        var affected = grid.GetTilesInRange(centerTile.GridPos, spell.areaRadius, includeOccupied: true);
        affected.Add(centerTile); // include center

        bool anyHit = false;
        foreach (var tile in affected) {
            if (tile.Occupant != null && tile.Occupant.IsAlive) {
                var target = tile.Occupant;
                // Dex save: DC = 8 + INT mod + proficiency (use 2 for simplicity)
                int dc = 10 + caster.Data.IntelligenceMod;
                int dexSave = DiceRoller.RollD20() + target.Data.DexterityMod;
                bool halfDamage = dexSave >= dc;

                int damage = DiceRoller.RollDamage(spell.damageNotation);
                if (halfDamage) damage = Mathf.Max(1, damage / 2);

                var result = new AttackResult {
                    hit = true,
                    damage = damage,
                    log = $"Fireball hits {target.Data.unitName}: {damage} dmg{(halfDamage ? " (half)" : "")}",
                    damageNotation = spell.damageNotation,
                };

                if (!target.IsPlayerControlled == !caster.IsPlayerControlled) {
                    // Don't friendly-fire for now
                    continue;
                }

                target.TakeDamage(damage);
                onDamage?.Invoke(result);
                anyHit = true;
            }
        }

        caster.HasActedThisTurn = true;
        return anyHit;
    }

    static bool CastMagicMissile(Unit caster, Tile targetTile, SpellData spell, System.Action<AttackResult> onDamage) {
        if (targetTile.Occupant == null || !targetTile.Occupant.IsAlive) return false;

        var target = targetTile.Occupant;
        int totalDamage = 0;

        // 3 bolts, each 1d4+1 (auto-hit)
        for (int i = 0; i < 3; i++) {
            totalDamage += DiceRoller.RollDamage(spell.damageNotation);
        }

        target.TakeDamage(totalDamage);

        var result = new AttackResult {
            hit = true,
            damage = totalDamage,
            log = $"Magic Missile hits {target.Data.unitName}: {totalDamage} total damage (auto-hit)",
            damageNotation = "3×(1d4+1)",
        };
        onDamage?.Invoke(result);

        caster.HasActedThisTurn = true;
        return true;
    }

    static bool CastHealWounds(Unit caster, Tile targetTile, SpellData spell) {
        if (targetTile.Occupant == null || !targetTile.Occupant.IsAlive) return false;

        var target = targetTile.Occupant;
        int heal = DiceRoller.RollDamage("2d4") + caster.Data.WisdomMod;
        target.Heal(heal);

        Debug.Log($"{caster.Data.unitName} heals {target.Data.unitName} for {heal} HP");
        caster.HasActedThisTurn = true;
        return true;
    }

    static bool CastBless(Unit caster, Tile targetTile, SpellData spell) {
        if (targetTile.Occupant == null || !targetTile.Occupant.IsAlive) return false;

        // Apply bless buff
        targetTile.Occupant.IsBlessed = true;
        Debug.Log($"{caster.Data.unitName} blesses {targetTile.Occupant.Data.unitName}");

        caster.HasActedThisTurn = true;
        return true;
    }

    static bool CastShield(Unit caster, SpellData spell) {
        caster.IsShielded = true;
        caster.ShieldTurnsRemaining = spell.duration;
        Debug.Log($"{caster.Data.unitName} casts Shield of Faith (+5 AC)");

        caster.HasActedThisTurn = true;
        return true;
    }
}
