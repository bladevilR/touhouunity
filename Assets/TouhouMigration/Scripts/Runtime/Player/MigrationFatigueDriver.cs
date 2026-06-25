using TouhouMigration.Runtime.Foundation;

namespace TouhouMigration.Runtime.Player
{
    // Drives fatigue accumulation off the game clock (Godot FatigueSystem's per-hour-active accrual):
    // each game hour that passes adds activity fatigue, scaled by the current activity. This is the
    // accrual half of the fatigue loop; MigrationDayCycle.Sleep() is the recovery half (full reset).
    //
    // Pure C# (no UnityEngine). A MonoBehaviour owner constructs it, sets the activity as gameplay
    // mode changes (farming/mining/combat raise the rate), and calls Detach() on teardown.
    public sealed class MigrationFatigueDriver
    {
        public enum Activity { Idle, Active, Farming, Mining }

        private readonly GameClock clock;
        private readonly MigrationFatigueSystem fatigue;

        public Activity CurrentActivity { get; set; } = Activity.Active;
        public int HoursAccrued { get; private set; }

        public MigrationFatigueDriver(GameClock clock, MigrationFatigueSystem fatigue)
        {
            this.clock = clock;
            this.fatigue = fatigue;
            if (this.clock != null)
            {
                this.clock.HourChanged += OnHourChanged;
            }
        }

        private void OnHourChanged(int hour)
        {
            double amount = RateFor(CurrentActivity);
            if (amount <= 0.0)
            {
                return;
            }

            fatigue?.AddFatigue(amount);
            HoursAccrued++;
        }

        // Per-hour fatigue for an activity, mapped from the Godot FatigueSystem accrual constants.
        public static double RateFor(Activity activity)
        {
            switch (activity)
            {
                case Activity.Farming:
                    return MigrationFatigueSystem.FatiguePerHourFarming;
                case Activity.Mining:
                    return MigrationFatigueSystem.FatiguePerHourMining;
                case Activity.Active:
                    return MigrationFatigueSystem.FatiguePerHourActive;
                default:
                    return 0.0;
            }
        }

        public void Detach()
        {
            if (clock != null)
            {
                clock.HourChanged -= OnHourChanged;
            }
        }
    }
}
