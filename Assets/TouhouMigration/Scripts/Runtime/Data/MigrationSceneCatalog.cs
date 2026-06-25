using System;

namespace TouhouMigration.Runtime.Data
{
    public static class MigrationSceneCatalog
    {
        public const string Bootstrap = "Bootstrap";
        public const string TitleScreen = "TitleScreen";
        public const string BambooHomeVerticalSlice = "BambooHomeVerticalSlice";
        public const string HumanVillageVerticalSlice = "HumanVillageVerticalSlice";
        public const string PureNatureMeadows = "PureNatureMeadows";
        public const string PureNatureClassic = "PureNatureClassic";
        public const string PureNatureJungle = "PureNatureJungle";
        public const string PureNatureIslands = "PureNatureIslands";
        public const string PureNatureMountains = "PureNatureMountains";
        public const string PureNatureFantasyForest = "PureNatureFantasyForest";
        public const string AngryMeshMeadow = "AngryMeshMeadow";
        public const string MagicForest = "MagicForest";
        public const string MistyLake = "MistyLake";
        public const string TownWorld = "TownWorld";
        public const string FantasyVillage = "FantasyVillage";
        public const string SuntailVillagePlayable = "SuntailVillagePlayable";
        public const string SuntailVillageImported = "SuntailVillageImported";
        public const string HakureiShrine = "HakureiShrine";
        public const string ScarletMansionFront = "ScarletMansionFront";
        public const string DungeonEntrance = "DungeonEntrance";
        public const string Farm = "Farm";
        public const string MokouHouse3D = "MokouHouse3D";
        public const string BambooHouse = "BambooHouse";
        public const string CombatArena = "CombatArena";
        public const string CombatArenaHD2D = "CombatArenaHD2D";
        public const string CirnoBossArena = "CirnoBossArena";
        public const string MainMenu = "MainMenu";
        public const string LoadingScreen = "LoadingScreen";
        public const string World = "World";

        public static string ToSceneName(MigrationSceneId sceneId)
        {
            return sceneId switch
            {
                MigrationSceneId.Bootstrap => Bootstrap,
                MigrationSceneId.TitleScreen => TitleScreen,
                MigrationSceneId.BambooHomeVerticalSlice => BambooHomeVerticalSlice,
                MigrationSceneId.HumanVillageVerticalSlice => HumanVillageVerticalSlice,
                MigrationSceneId.PureNatureMeadows => PureNatureMeadows,
                MigrationSceneId.PureNatureClassic => PureNatureClassic,
                MigrationSceneId.PureNatureJungle => PureNatureJungle,
                MigrationSceneId.PureNatureIslands => PureNatureIslands,
                MigrationSceneId.PureNatureMountains => PureNatureMountains,
                MigrationSceneId.PureNatureFantasyForest => PureNatureFantasyForest,
                MigrationSceneId.AngryMeshMeadow => AngryMeshMeadow,
                MigrationSceneId.MagicForest => MagicForest,
                MigrationSceneId.MistyLake => MistyLake,
                MigrationSceneId.TownWorld => TownWorld,
                MigrationSceneId.FantasyVillage => FantasyVillage,
                MigrationSceneId.SuntailVillagePlayable => SuntailVillagePlayable,
                MigrationSceneId.SuntailVillageImported => SuntailVillageImported,
                MigrationSceneId.HakureiShrine => HakureiShrine,
                MigrationSceneId.ScarletMansionFront => ScarletMansionFront,
                MigrationSceneId.DungeonEntrance => DungeonEntrance,
                MigrationSceneId.Farm => Farm,
                MigrationSceneId.MokouHouse3D => MokouHouse3D,
                MigrationSceneId.BambooHouse => BambooHouse,
                MigrationSceneId.CombatArena => CombatArena,
                MigrationSceneId.CombatArenaHD2D => CombatArenaHD2D,
                MigrationSceneId.CirnoBossArena => CirnoBossArena,
                MigrationSceneId.MainMenu => MainMenu,
                MigrationSceneId.LoadingScreen => LoadingScreen,
                MigrationSceneId.World => World,
                _ => throw new ArgumentOutOfRangeException(nameof(sceneId), sceneId, "Unknown migration scene id.")
            };
        }
    }
}
