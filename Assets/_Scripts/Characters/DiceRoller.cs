using System;
using System.Text.RegularExpressions;
using UnityEngine;

public static class DiceRoller {
    private static readonly System.Random _rng = new System.Random();

    /// <summary>Roll a single die with N sides.</summary>
    public static int Roll(int sides) => _rng.Next(1, sides + 1);

    public static int RollD20() => Roll(20);

    /// <summary>Roll advantage: roll 2d20, take higher.</summary>
    public static int RollAdvantage() => Math.Max(Roll(20), Roll(20));

    /// <summary>Roll disadvantage: roll 2d20, take lower.</summary>
    public static int RollDisadvantage() => Math.Min(Roll(20), Roll(20));

    /// <summary>
    /// Parse dice notation like "2d6+3", "1d8", "3d10-2".
    /// </summary>
    public static int RollDamage(string notation) {
        if (string.IsNullOrEmpty(notation)) return 0;
        var match = Regex.Match(notation, @"^(\d+)d(\d+)([+-]\d+)?$");
        if (!match.Success) {
            Debug.LogWarning($"Invalid dice notation: {notation}");
            return 0;
        }
        int count = int.Parse(match.Groups[1].Value);
        int sides = int.Parse(match.Groups[2].Value);
        int bonus = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

        return RollDamage(count, sides, bonus);
    }

    public static int RollDamage(int count, int sides, int bonus = 0) {
        int total = bonus;
        for (int i = 0; i < count; i++) total += Roll(sides);
        return Math.Max(0, total);
    }

    /// <summary>
    /// Check if d20 + modifier meets or exceeds DC.
    /// </summary>
    public static bool RollCheck(int dc, int modifier) {
        return RollD20() + modifier >= dc;
    }
}
