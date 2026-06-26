using TouhouMigration.Runtime.Player;

namespace TouhouMigration.Runtime.Home
{
    // The bamboo-home interactions' fatigue effects (Godot HomeInteractionSystem interact_sleep / tea /
    // meal / read_book): sleeping fully recovers fatigue, tea/meal rest-recover 5/10, and reading a book
    // costs 2 fatigue. UnityEngine-free; the sleep day-loop + signals are owner/scene wiring. The fatigue
    // system is optional/null-safe (the storage half of the home is MigrationHomeStorage).
    public sealed class MigrationHomeInteractions
    {
        public const double TeaRecovery = 5.0;
        public const double MealRecovery = 10.0;
        public const double ReadBookFatigue = 2.0;

        private readonly MigrationFatigueSystem fatigue;

        public MigrationHomeInteractions(MigrationFatigueSystem fatigue)
        {
            this.fatigue = fatigue;
        }

        public void Sleep() => fatigue?.SleepFullRecovery();

        public void Tea() => fatigue?.RestRecovery(TeaRecovery);

        public void Meal() => fatigue?.RestRecovery(MealRecovery);

        public void ReadBook() => fatigue?.AddFatigue(ReadBookFatigue);
    }
}
