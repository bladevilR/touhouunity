using System;
using TouhouMigration.Runtime.Cooking;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Social;

namespace TouhouMigration.Runtime.Save
{
    [Serializable]
    public sealed class MigrationSaveData
    {
        public int save_schema = 3;
        public string version = "3.0.0";
        public string timestamp = string.Empty;
        public string player_name = "藤原妹红";
        public int level = 1;
        public int max_hp = 100;
        public int current_hp = 100;
        public int coins;
        public string current_scene = "town";
        public MigrationSavePosition position = new MigrationSavePosition();
        public InventorySnapshot inventory = new InventorySnapshot();
        public CookingRuntimeSnapshot cooking = new CookingRuntimeSnapshot();
        public CookingBuffRuntimeSnapshot cooking_buffs = new CookingBuffRuntimeSnapshot();
        public SocialBondSnapshot social_bonds = new SocialBondSnapshot();
        public QuestRuntimeSnapshot quests = new QuestRuntimeSnapshot();
        public float play_time;
        public int total_kills;
        public int humanity = 100;

        public int SaveSchema => save_schema;
        public string GameVersion => version;

        public string Timestamp
        {
            get => timestamp;
            set => timestamp = value ?? string.Empty;
        }

        public string PlayerName
        {
            get => player_name;
            set => player_name = string.IsNullOrWhiteSpace(value) ? "藤原妹红" : value;
        }

        public int Level
        {
            get => level;
            set => level = Math.Max(1, value);
        }

        public int MaxHp
        {
            get => max_hp;
            set => max_hp = Math.Max(1, value);
        }

        public int CurrentHp
        {
            get => current_hp;
            set => current_hp = Math.Max(0, value);
        }

        public int Coins
        {
            get => coins;
            set => coins = Math.Max(0, value);
        }

        public string CurrentScene
        {
            get => current_scene;
            set => current_scene = string.IsNullOrWhiteSpace(value) ? "town" : value;
        }

        public MigrationSavePosition Position
        {
            get => position;
            set => position = value ?? new MigrationSavePosition();
        }

        public InventorySnapshot Inventory
        {
            get => inventory;
            set => inventory = value ?? new InventorySnapshot();
        }

        public CookingRuntimeSnapshot Cooking
        {
            get => cooking;
            set => cooking = value ?? new CookingRuntimeSnapshot();
        }

        public CookingBuffRuntimeSnapshot CookingBuffs
        {
            get => cooking_buffs;
            set => cooking_buffs = value ?? new CookingBuffRuntimeSnapshot();
        }

        public SocialBondSnapshot SocialBonds
        {
            get => social_bonds;
            set => social_bonds = value ?? new SocialBondSnapshot();
        }

        public QuestRuntimeSnapshot Quests
        {
            get => quests;
            set => quests = value ?? new QuestRuntimeSnapshot();
        }

        public float PlayTime
        {
            get => play_time;
            set => play_time = Math.Max(0f, value);
        }

        public int TotalKills
        {
            get => total_kills;
            set => total_kills = Math.Max(0, value);
        }

        public int Humanity
        {
            get => humanity;
            set => humanity = Math.Clamp(value, 0, 100);
        }

        public static MigrationSaveData CreateDefault()
        {
            return new MigrationSaveData
            {
                save_schema = 3,
                version = "3.0.0",
                timestamp = DateTime.Now.ToString("s"),
                inventory = new InventorySnapshot(),
                cooking = new CookingRuntimeSnapshot(),
                cooking_buffs = new CookingBuffRuntimeSnapshot(),
                social_bonds = new SocialBondSnapshot(),
                quests = new QuestRuntimeSnapshot()
            };
        }
    }

    [Serializable]
    public sealed class MigrationSavePosition
    {
        public float x;
        public float y;
        public float z;
    }

    public sealed class MigrationSaveInfo
    {
        public MigrationSaveInfo(int slot, MigrationSaveData data)
        {
            Slot = slot;
            PlayerName = data.PlayerName;
            Level = data.Level;
            PlayTime = data.PlayTime;
            CurrentScene = data.CurrentScene;
            Timestamp = data.Timestamp;
        }

        public int Slot { get; }
        public string PlayerName { get; }
        public int Level { get; }
        public float PlayTime { get; }
        public string CurrentScene { get; }
        public string Timestamp { get; }
    }
}
