using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Data
{
    public static class MigrationSceneRegistry
    {
        private static readonly MigrationSceneOption[] Options =
        {
            new MigrationSceneOption("bamboo_home", "竹林小屋", true, MigrationSceneId.BambooHomeVerticalSlice),
            new MigrationSceneOption("town", "人之里", true, MigrationSceneId.HumanVillageVerticalSlice),
            new MigrationSceneOption("farm", "农场", false, MigrationSceneId.BambooHomeVerticalSlice),
            new MigrationSceneOption("dungeon_entrance", "地牢入口", false, MigrationSceneId.BambooHomeVerticalSlice),
            new MigrationSceneOption("magic_forest", "魔法森林", true, MigrationSceneId.MagicForest),
            new MigrationSceneOption("misty_lake", "雾之湖", true, MigrationSceneId.MistyLake),
            new MigrationSceneOption("hakurei_shrine", "博丽神社", false, MigrationSceneId.BambooHomeVerticalSlice),
            new MigrationSceneOption("scarlet_mansion_front", "红魔馆门前", false, MigrationSceneId.BambooHomeVerticalSlice),
            new MigrationSceneOption("pure_nature_classic", "纯自然·经典", true, MigrationSceneId.PureNatureClassic),
            new MigrationSceneOption("pure_nature_fantasy_forest", "纯自然·幻林", true, MigrationSceneId.PureNatureFantasyForest),
            new MigrationSceneOption("pure_nature_mountains", "纯自然·群山", true, MigrationSceneId.PureNatureMountains),
            new MigrationSceneOption("pure_nature_islands", "纯自然·群岛", true, MigrationSceneId.PureNatureIslands),
            new MigrationSceneOption("pure_nature_jungle", "纯自然·雨林", true, MigrationSceneId.PureNatureJungle),
            new MigrationSceneOption("pure_nature_meadows", "纯自然·草甸", true, MigrationSceneId.PureNatureMeadows),
            new MigrationSceneOption("angrymesh_meadow", "草甸·野境", true, MigrationSceneId.AngryMeshMeadow),
            new MigrationSceneOption("town_world", "旧版城镇", false, MigrationSceneId.BambooHomeVerticalSlice),
            new MigrationSceneOption("combat", "战斗竞技场", false, MigrationSceneId.BambooHomeVerticalSlice),
            new MigrationSceneOption("cirno_mvp", "琪露诺试炼", false, MigrationSceneId.BambooHomeVerticalSlice)
        };

        public static IReadOnlyList<MigrationSceneOption> GetAllOptions()
        {
            return Array.AsReadOnly(Options);
        }

        public static bool TryGetAvailableScene(string key, out MigrationSceneId sceneId)
        {
            string normalizedKey = NormalizeKey(key);
            foreach (MigrationSceneOption option in Options)
            {
                if (option.Key == normalizedKey && option.IsAvailable)
                {
                    sceneId = option.SceneId;
                    return true;
                }
            }

            sceneId = MigrationSceneId.BambooHomeVerticalSlice;
            return false;
        }

        public static string NormalizeKey(string key)
        {
            return key switch
            {
                "human_village" => "town",
                "codex_human_village" => "town",
                "battle" => "combat",
                null => "bamboo_home",
                "" => "bamboo_home",
                _ => key
            };
        }
    }
}
