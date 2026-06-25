using TouhouMigration.Runtime.Cooking;
using TouhouMigration.Runtime.Foundation;
using TouhouMigration.Runtime.Home;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Player;
using TouhouMigration.Runtime.Social;

namespace TouhouMigration.Runtime.Save
{
    // Single runtime owner that bridges the live gameplay services and the persisted save record.
    // Capture() pulls each service's snapshot into a MigrationSaveData; Apply() pushes them back into
    // the live services. Service-owned stats are handled here (snapshots + the humanity scalar); the
    // remaining player scalars (name/level/hp/coins/scene/position) stay the caller's responsibility.
    // Missing services and missing snapshot sections are tolerated.
    public sealed class MigrationSaveOrchestrator
    {
        private readonly InventoryService inventory;
        private readonly CookingService cooking;
        private readonly CookingBuffService cookingBuffs;
        private readonly SocialBondService bonds;
        private readonly QuestDeliveryService quests;
        private readonly HumanityService humanity;
        private readonly MigrationFatigueSystem fatigue;
        private readonly GameClock clock;
        private readonly MigrationCompanionRoster companions;
        private readonly MigrationHomeStorage homeStorage;

        public MigrationSaveOrchestrator(
            InventoryService inventory,
            CookingService cooking,
            CookingBuffService cookingBuffs,
            SocialBondService bonds,
            QuestDeliveryService quests,
            HumanityService humanity,
            MigrationFatigueSystem fatigue = null,
            GameClock clock = null,
            MigrationCompanionRoster companions = null,
            MigrationHomeStorage homeStorage = null)
        {
            this.inventory = inventory;
            this.cooking = cooking;
            this.cookingBuffs = cookingBuffs;
            this.bonds = bonds;
            this.quests = quests;
            this.humanity = humanity;
            this.fatigue = fatigue;
            this.clock = clock;
            this.companions = companions;
            this.homeStorage = homeStorage;
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
            if (humanity != null)
            {
                data.Humanity = humanity.Humanity;
            }
            if (fatigue != null)
            {
                data.Fatigue = fatigue.CurrentFatigue;
            }
            if (clock != null)
            {
                data.Calendar = new MigrationCalendarSnapshot
                {
                    day = clock.Day,
                    season = clock.Season.ToString(),
                    year = clock.Year,
                    hour = clock.Hour,
                    minute = clock.Minute,
                };
            }
            if (companions != null)
            {
                data.Companions = companions.CreateSnapshot();
            }
            if (homeStorage != null)
            {
                data.HomeStorage = homeStorage.CreateSnapshot();
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
            if (humanity != null)
            {
                humanity.Set(data.Humanity);
            }
            if (fatigue != null)
            {
                fatigue.LoadFatigue(data.Fatigue);
            }
            if (clock != null && data.calendar != null)
            {
                clock.SetDate(data.calendar.day, data.calendar.season, data.calendar.year);
                clock.SetTime(data.calendar.hour, data.calendar.minute);
            }
            if (companions != null && data.companions != null)
            {
                companions.LoadSnapshot(data.companions);
            }
            if (homeStorage != null && data.home_storage != null)
            {
                homeStorage.LoadSnapshot(data.home_storage);
            }
        }
    }
}
