using TouhouMigration.Runtime.Save;

namespace TouhouMigration.Runtime.Player
{
    public sealed class MigrationPlayerProgressService
    {
        public int Experience { get; private set; }
        public int Coins { get; private set; }
        public int TotalKills { get; private set; }

        public void GainExperience(int amount)
        {
            if (amount > 0)
            {
                Experience += amount;
            }
        }

        public void AddCoins(int amount)
        {
            if (amount > 0)
            {
                Coins += amount;
            }
        }

        public void RegisterKill()
        {
            RegisterKill(1);
        }

        public void RegisterKill(int amount)
        {
            if (amount > 0)
            {
                TotalKills += amount;
            }
        }

        public void LoadFromSave(MigrationSaveData saveData)
        {
            Coins = saveData != null ? saveData.Coins : 0;
            TotalKills = saveData != null ? saveData.TotalKills : 0;
        }

        public void ApplyToSave(MigrationSaveData saveData)
        {
            if (saveData != null)
            {
                saveData.Coins = Coins;
                saveData.TotalKills = TotalKills;
            }
        }
    }
}
