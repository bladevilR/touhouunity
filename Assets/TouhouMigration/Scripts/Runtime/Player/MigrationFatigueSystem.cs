using System;

namespace TouhouMigration.Runtime.Player
{
    // Fatigue level bands (Godot FatigueSystem _get_fatigue_level).
    public enum FatigueLevel { Normal, Tired, Exhausted, Collapse }

    // The player's fatigue stat (Godot FatigueSystem): a 0-100 value that accumulates from activity and is
    // cleared by sleeping or reduced by resting. Crossing the Exhausted/Collapse thresholds latches flags
    // (over-exhaustion forces a collapse in Godot). Free of UnityEngine.
    //
    // The hourly/per-minute accumulation (driven by TimeManager + GameStateManager signals), the SignalBus
    // warning emissions, and the collapse->teleport-home cutscene are signal/scene-coupled and deferred; the
    // accrual rates are exposed as constants so a future driver can apply them on the right ticks.
    public sealed class MigrationFatigueSystem
    {
        public const double FatiguePerHourActive = 2.5;
        public const double FatiguePerMinuteCombat = 1.0;
        public const double FatiguePerHourFarming = 3.5;
        public const double FatiguePerHourMining = 4.0;

        public const double FatigueTired = 60.0;
        public const double FatigueExhausted = 80.0;
        public const double FatigueCollapse = 100.0;

        public double CurrentFatigue { get; private set; }
        public bool IsExhausted { get; private set; }
        public bool HasCollapsed { get; private set; }

        private FatigueLevel lastWarningLevel = FatigueLevel.Normal;

        public FatigueLevel Level
        {
            get
            {
                if (CurrentFatigue >= FatigueCollapse)
                {
                    return FatigueLevel.Collapse;
                }

                if (CurrentFatigue >= FatigueExhausted)
                {
                    return FatigueLevel.Exhausted;
                }

                if (CurrentFatigue >= FatigueTired)
                {
                    return FatigueLevel.Tired;
                }

                return FatigueLevel.Normal;
            }
        }

        // Accumulate fatigue (Godot add_fatigue): clamped to 0-100; re-evaluates the warning level on change.
        public void AddFatigue(double amount)
        {
            double old = CurrentFatigue;
            CurrentFatigue = Math.Clamp(CurrentFatigue + amount, 0.0, 100.0);
            if (old != CurrentFatigue)
            {
                CheckWarnings();
            }
        }

        // Partial recovery (Godot rest_recovery): reduce fatigue, clearing the exhausted flag once below the
        // exhausted threshold.
        public void RestRecovery(double amount)
        {
            CurrentFatigue = Math.Max(CurrentFatigue - amount, 0.0);
            if (CurrentFatigue < FatigueExhausted)
            {
                IsExhausted = false;
            }

            CheckWarnings();
        }

        // Full recovery (Godot sleep_full_recovery): clears fatigue and the latched warning state.
        public void SleepFullRecovery()
        {
            CurrentFatigue = 0.0;
            lastWarningLevel = FatigueLevel.Normal;
            IsExhausted = false;
            HasCollapsed = false;
        }

        // Restores a persisted fatigue value (save load): sets the absolute value (clamped 0-100) and
        // re-derives the latched exhausted/collapse flags + warning level from it, without emitting
        // spurious warnings.
        public void LoadFatigue(double value)
        {
            CurrentFatigue = Math.Clamp(value, 0.0, 100.0);
            IsExhausted = CurrentFatigue >= FatigueExhausted;
            HasCollapsed = CurrentFatigue >= FatigueCollapse;
            lastWarningLevel = Level;
        }

        public string GetFatigueDescription()
        {
            if (CurrentFatigue >= FatigueCollapse)
            {
                return "即将昏倒";
            }

            if (CurrentFatigue >= FatigueExhausted)
            {
                return "精疲力竭";
            }

            if (CurrentFatigue >= FatigueTired)
            {
                return "疲惫";
            }

            return "正常";
        }

        // Latch the exhausted/collapse flags when first crossing into that band (Godot _check_fatigue_warnings).
        private void CheckWarnings()
        {
            FatigueLevel level = Level;
            if (level == lastWarningLevel)
            {
                return;
            }

            switch (level)
            {
                case FatigueLevel.Exhausted:
                    IsExhausted = true;
                    break;
                case FatigueLevel.Collapse:
                    HasCollapsed = true;
                    break;
            }

            lastWarningLevel = level;
        }
    }
}
