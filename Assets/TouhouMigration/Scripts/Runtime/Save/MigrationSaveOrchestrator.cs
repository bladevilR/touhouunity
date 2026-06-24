using TouhouMigration.Runtime.Cooking;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Social;

namespace TouhouMigration.Runtime.Save
{
    // Single runtime owner that bridges the live gameplay services and the persisted save record.
    // Capture() pulls each service's snapshot into a MigrationSaveData; Apply() pushes them back into
    // the live services. Player scalar fields (name/level/hp/coins/scene/position) remain the caller's
    // responsibility. Missing services and missing snapshot sections are tolerated.
    public sealed class MigrationSaveOrchestrator
    {
        private readonly InventoryService inventory;
        private readonly CookingService cooking;
        private readonly CookingBuffService cookingBuffs;
        private readonly SocialBondService bonds;
        private readonly QuestDeliveryService quests;

        public MigrationSaveOrchestrator(
            InventoryService inventory,
            CookingService cooking,
            CookingBuffService cookingBuffs,
            SocialBondService bonds,
            QuestDeliveryService quests)
        {
            this.inventory = inventory;
            this.cooking = cooking;
            this.cookingBuffs = cookingBuffs;
            this.bonds = bonds;
            this.quests = quests;
        }

        public MigrationSaveData Capture(MigrationSaveData data)
        {
            data ??= new MigrationSaveData();
            if (inventory != null)
            {
                data.inventory = inventory.CreateSnapshot();
            }
            if (cooking != null)
            {
                data.cooking = cooking.CreateSnapshot();
            }
            if (cookingBuffs != null)
            {
                data.cooking_buffs = cookingBuffs.CreateSnapshot();
            }
            if (bonds != null)
            {
                data.social_bonds = bonds.CreateSnapshot();
            }
            if (quests != null)
            {
                data.quests = quests.CreateSnapshot();
            }
            return data;
        }

        public void Apply(MigrationSaveData data)
        {
            if (data == null)
            {
                return;
            }
            if (inventory != null && data.inventory != null)
            {
                inventory.LoadSnapshot(data.inventory);
            }
            if (cooking != null && data.cooking != null)
            {
                cooking.LoadSnapshot(data.cooking);
            }
            if (cookingBuffs != null && data.cooking_buffs != null)
            {
                cookingBuffs.LoadSnapshot(data.cooking_buffs);
            }
            if (bonds != null && data.social_bonds != null)
            {
                bonds.LoadSnapshot(data.social_bonds);
            }
            if (quests != null && data.quests != null)
            {
                quests.LoadSnapshot(data.quests);
            }
        }
    }
}
