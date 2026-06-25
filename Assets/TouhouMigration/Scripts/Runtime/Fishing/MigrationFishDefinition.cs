namespace TouhouMigration.Runtime.Fishing
{
    // A catchable fish (Godot FishDatabase entry): its id, rarity (drives catch weight), and the
    // inventory item granted when caught. Immutable.
    public sealed class MigrationFishDefinition
    {
        public string FishId { get; }
        public MigrationFishRarity Rarity { get; }
        public string ItemId { get; }

        public MigrationFishDefinition(string fishId, MigrationFishRarity rarity, string itemId)
        {
            FishId = fishId ?? string.Empty;
            Rarity = rarity;
            ItemId = itemId ?? string.Empty;
        }
    }
}
