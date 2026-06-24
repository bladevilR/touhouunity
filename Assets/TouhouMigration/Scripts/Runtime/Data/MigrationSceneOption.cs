namespace TouhouMigration.Runtime.Data
{
    public readonly struct MigrationSceneOption
    {
        public MigrationSceneOption(string key, string label, bool isAvailable, MigrationSceneId sceneId)
        {
            Key = key;
            Label = label;
            IsAvailable = isAvailable;
            SceneId = sceneId;
        }

        public string Key { get; }
        public string Label { get; }
        public bool IsAvailable { get; }
        public MigrationSceneId SceneId { get; }
    }
}
