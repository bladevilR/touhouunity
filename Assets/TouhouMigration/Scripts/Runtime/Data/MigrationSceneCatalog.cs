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

        public static string ToSceneName(MigrationSceneId sceneId)
        {
            return sceneId switch
            {
                MigrationSceneId.Bootstrap => Bootstrap,
                MigrationSceneId.TitleScreen => TitleScreen,
                MigrationSceneId.BambooHomeVerticalSlice => BambooHomeVerticalSlice,
                MigrationSceneId.HumanVillageVerticalSlice => HumanVillageVerticalSlice,
                MigrationSceneId.PureNatureMeadows => PureNatureMeadows,
                _ => throw new ArgumentOutOfRangeException(nameof(sceneId), sceneId, "Unknown migration scene id.")
            };
        }
    }
}
