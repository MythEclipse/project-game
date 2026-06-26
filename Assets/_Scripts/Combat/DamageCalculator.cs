using UnityEngine;

public struct AttackResult {
    public bool hit;
    public bool crit;
    public bool miss;
    public int damage;
    public int attackRoll;      // d20 roll (1-20)
    public int attackBonus;     // attacker's attack bonus
    public int totalAttack;     // attackRoll + attackBonus
    public int targetAC;        // defender's AC (including defend bonus)
    public string damageNotation; // e.g. "1d10+3"
    public string log;

    public override string ToString() => log;
}

public static class DamageCalculator {

    /// <summary>
    /// Hit roll: d20 + attack bonus vs target AC.
    /// Natural 20 = crit. Natural 1 = miss.
    /// </summary>
    public static AttackResult CalculateDamage(Unit attacker, Unit defender) {
        var result = new AttackResult();

        if (attacker.Data == null || defender.Data == null) {
            result.hit = true;
            result.damage = DiceRoller.RollDamage("1d4");
            result.log = $"Basic attack: {result.damage} damage";
            return result;
        }

        int attackRoll = DiceRoller.RollD20();
        int attackBonus = attacker.Data.AttackBonus;
        int totalAttack = attackRoll + attackBonus;
        int targetAc = defender.Data.armorClass;
        if (defender.IsDefending) targetAc += 4; // +4 AC kalau defend

        result.attackRoll = attackRoll;
        result.attackBonus = attackBonus;
        result.totalAttack = totalAttack;
        result.targetAC = targetAc;
        result.damageNotation = attacker.Data.weaponDamage;

        result.crit = attackRoll == 20;
        result.miss = attackRoll == 1;
        result.hit = result.crit || (!result.miss && totalAttack >= targetAc);

        if (result.miss) {
            result.damage = 0;
            result.log = $"<color=white>Miss!</color> (rolled {attackRoll} + {attackBonus} = {totalAttack} vs AC {targetAc})";
            return result;
        }

        if (result.crit) {
            // Crit: roll damage dice 2x, tambah bonus 1x
            int diceCount = 0;
            int diceSides = 0;
            int bonus = 0;
            ParseDamageNotation(attacker.Data.weaponDamage, out diceCount, out diceSides, out bonus);
            result.damage = DiceRoller.RollDamage(diceCount * 2, diceSides) + bonus;
            result.log = $"<color=yellow>CRIT!</color> {result.damage} damage! (rolled 20 + {attackBonus} vs AC {targetAc})";
        } else {
            result.damage = DiceRoller.RollDamage(attacker.Data.weaponDamage);
            result.log = $"<color=white>Hit!</color> {result.damage} damage (rolled {attackRoll} + {attackBonus} = {totalAttack} vs AC {targetAc})";
        }

        return result;
    }

    static void ParseDamageNotation(string notation, out int count, out int sides, out int bonus) {
        count = 1; sides = 6; bonus = 0;
        if (string.IsNullOrEmpty(notation)) return;

        // Parse "XdY+Z" or "XdY-Z"
        var parts = notation.Split('d');
        if (parts.Length != 2) return;

        int.TryParse(parts[0], out count);

        var rest = parts[1];
        if (rest.Contains('+')) {
            var sub = rest.Split('+');
            int.TryParse(sub[0], out sides);
            int.TryParse(sub[1], out bonus);
        } else if (rest.Contains('-')) {
            var sub = rest.Split('-');
            int.TryParse(sub[0], out sides);
            int.TryParse(sub[1], out bonus);
            bonus = -bonus;
        } else {
            int.TryParse(rest, out sides);
        }
    }
}
