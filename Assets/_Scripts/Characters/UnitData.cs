using UnityEngine;

[CreateAssetMenu(fileName = "UnitData", menuName = "DNDTactics/UnitData")]
public class UnitData : ScriptableObject {
    [Header("Info")]
    public string unitName;
    public UnitClass unitClass;

    [Header("D&D Stats (8-20)")]
    public int strength = 10;
    public int dexterity = 10;
    public int constitution = 10;
    public int intelligence = 10;
    public int wisdom = 10;
    public int charisma = 10;

    [Header("Combat")]
    public int maxHp = 10;
    public int moveRange = 5;
    public int attackRange = 1;
    public string weaponDamage = "1d8";
    public int armorClass = 10;

    [Header("AI Hint")]
    public bool isPlayerControlled = true;

    // Derived stat: ability modifier = (stat - 10) / 2 (floor)
    public int GetAbilityModifier(int stat) => Mathf.FloorToInt((stat - 10) / 2f);

    public int StrengthMod => GetAbilityModifier(strength);
    public int DexterityMod => GetAbilityModifier(dexterity);
    public int ConstitutionMod => GetAbilityModifier(constitution);
    public int IntelligenceMod => GetAbilityModifier(intelligence);
    public int WisdomMod => GetAbilityModifier(wisdom);
    public int CharismaMod => GetAbilityModifier(charisma);

    /// <summary>
    /// Attack bonus depends on class:
    /// Fighter/Cleric → STR, Rogue → DEX, Wizard → INT
    /// </summary>
    public int AttackBonus => unitClass switch {
        UnitClass.Rogue => DexterityMod,
        UnitClass.Wizard => IntelligenceMod,
        _ => StrengthMod,
    };

    /// <summary>
    /// Create a scaled copy of this UnitData for the current difficulty level.
    /// HP is multiplied, damage gains a flat bonus, and AC increases.
    /// The original asset is never mutated — each level gets fresh copies.
    /// </summary>
    public UnitData CreateScaledCopy(float hpMult, int dmgBonus, int acBonus) {
        var copy = CreateInstance<UnitData>();
        copy.unitName = unitName;
        copy.unitClass = unitClass;
        copy.strength = strength;
        copy.dexterity = dexterity;
        copy.constitution = constitution;
        copy.intelligence = intelligence;
        copy.wisdom = wisdom;
        copy.charisma = charisma;
        copy.moveRange = moveRange;
        copy.attackRange = attackRange;
        copy.isPlayerControlled = isPlayerControlled;

        // Scale HP
        copy.maxHp = Mathf.RoundToInt(maxHp * hpMult);

        // Scale damage notation "XdY+Z" → add dmgBonus to Z
        copy.weaponDamage = ScaleDamageNotation(weaponDamage, dmgBonus);

        // Scale AC
        copy.armorClass = armorClass + acBonus;

        return copy;
    }

    /// <summary>Parse "XdY+Z" or "XdY-Z", add bonus, return new notation.</summary>
    static string ScaleDamageNotation(string notation, int bonus) {
        if (string.IsNullOrEmpty(notation)) return notation;
        var match = System.Text.RegularExpressions.Regex.Match(notation, @"^(\d+)d(\d+)([+-]\d+)?$");
        if (!match.Success) return notation;

        int count = int.Parse(match.Groups[1].Value);
        int sides = int.Parse(match.Groups[2].Value);
        int existingBonus = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
        int newBonus = existingBonus + bonus;

        if (newBonus > 0) return $"{count}d{sides}+{newBonus}";
        if (newBonus < 0) return $"{count}d{sides}{newBonus}";
        return $"{count}d{sides}";
    }
}
