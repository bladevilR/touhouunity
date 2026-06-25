using TouhouMigration.Runtime.Farming;
using TouhouMigration.Runtime.Player;
using TouhouMigration.Runtime.Social;

namespace TouhouMigration.Runtime.Foundation
{
    // Orchestrates the day loop (Godot SleepManager / day-start wiring): subscribes to
    // GameClock.DayStarted and fans the per-day resets out to the (already logic-complete) life-sim
    // services — farming growth (AdvanceDay), daily quests (ResetDailyQuests), and bond daily
    // interactions (StartNewDay). Sleeping advances the clock to the next morning, which crosses one
    // midnight (firing DayStarted -> the resets) and then fully restores fatigue.
    //
    // Pure C# (no UnityEngine): a MonoBehaviour owner constructs it once, forwards Sleep() from the
    // bed/HUD, and calls Detach() on teardown. Every dependency is optional and null-safe so partial
    // scenes (e.g. a location without farming) still get a working day rollover.
    public sealed class MigrationDayCycle
    {
        public const int WakeHour = 6;

        private readonly GameClock clock;
        private readonly MigrationFarmingManager farming;
        private readonly QuestDeliveryService quests;
        private readonly SocialBondService bonds;
        private readonly MigrationFatigueSystem fatigue;
        private readonly MigrationNpcMemorySystem npcMemory;
        private readonly WeatherService weather;

        public int DailyResetsRun { get; private set; }
        public int LastResetDay { get; private set; } = -1;

        public MigrationDayCycle(
            GameClock clock,
            MigrationFarmingManager farming = null,
            QuestDeliveryService quests = null,
            SocialBondService bonds = null,
            MigrationFatigueSystem fatigue = null,
            MigrationNpcMemorySystem npcMemory = null,
            WeatherService weather = null)
        {
            this.clock = clock;
            this.farming = farming;
            this.quests = quests;
            this.bonds = bonds;
            this.fatigue = fatigue;
            this.npcMemory = npcMemory;
            this.weather = weather;

            if (this.clock != null)
            {
                this.clock.DayStarted += OnDayStarted;
            }
        }

        // Runs the per-day resets. Fires for every new day, whether reached by sleeping or by time
        // naturally passing midnight, so the systems stay consistent either way.
        private void OnDayStarted(int day, GameSeason season, int year)
        {
            farming?.AdvanceDay();
            quests?.ResetDailyQuests(day);
            bonds?.StartNewDay();
            npcMemory?.DecayAllMemories();
            weather?.UpdateForDate(day, season.ToString());
            LastResetDay = day;
            DailyResetsRun++;
        }

        // Sleep until the next morning: advances the clock exactly one day to WakeHour (which fires the
        // per-day resets via DayStarted) and fully restores fatigue.
        public void Sleep()
        {
            AdvanceToNextMorning();
            fatigue?.SleepFullRecovery();
        }

        private void AdvanceToNextMorning()
        {
            if (clock == null)
            {
                return;
            }

            int minutesToWake = ((GameClock.HoursPerDay - clock.Hour) + WakeHour) * GameClock.MinutesPerHour - clock.Minute;
            if (minutesToWake <= 0)
            {
                minutesToWake += GameClock.HoursPerDay * GameClock.MinutesPerHour;
            }

            clock.AdvanceMinutes(minutesToWake);
        }

        // Detaches from the clock event; call on teardown to avoid a dangling subscription.
        public void Detach()
        {
            if (clock != null)
            {
                clock.DayStarted -= OnDayStarted;
            }
        }
    }
}
