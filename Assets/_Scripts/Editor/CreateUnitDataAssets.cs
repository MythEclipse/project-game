using UnityEngine;
using UnityEditor;

public class CreateUnitDataAssets {
    [MenuItem("DNDTactics/Create Unit Data Assets")]
    public static void Create() {
        string folder = "Assets/Data/Characters";
        System.IO.Directory.CreateDirectory(folder);

        var classes = new (string name, UnitClass cls, int str, int dex, int con, int intel, int wis, int cha, int hp, int atkRange, string dmg, int ac, bool player)[] {
            ("FighterData", UnitClass.Fighter, 16, 12, 15, 8, 10, 10, 14, 1, "1d10+3", 16, true),
            ("RogueData", UnitClass.Rogue, 10, 17, 12, 13, 10, 12, 10, 1, "1d6+3", 14, true),
            ("WizardData", UnitClass.Wizard, 8, 12, 13, 17, 12, 10, 8, 6, "1d8+3", 12, true),
            ("ClericData", UnitClass.Cleric, 14, 8, 14, 10, 16, 12, 12, 1, "1d6+3", 15, true),
            ("GoblinData", UnitClass.Rogue, 10, 16, 12, 8, 8, 6, 10, 1, "1d6+3", 14, false),
            ("SkeletonData", UnitClass.Fighter, 14, 8, 14, 6, 6, 4, 12, 1, "1d8+2", 14, false),
        };

        foreach (var c in classes) {
            var asset = ScriptableObject.CreateInstance<UnitData>();
            asset.unitName = c.name.Replace("Data", "");
            asset.unitClass = c.cls;
            asset.strength = c.str;
            asset.dexterity = c.dex;
            asset.constitution = c.con;
            asset.intelligence = c.intel;
            asset.wisdom = c.wis;
            asset.charisma = c.cha;
            asset.maxHp = c.hp;
            asset.attackRange = c.atkRange;
            asset.weaponDamage = c.dmg;
            asset.armorClass = c.ac;
            asset.isPlayerControlled = c.player;
            asset.moveRange = 5;

            string path = $"{folder}/{c.name}.asset";
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"Created: {path}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All UnitData assets created!");
    }
}
