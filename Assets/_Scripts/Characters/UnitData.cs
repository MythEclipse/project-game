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
}
