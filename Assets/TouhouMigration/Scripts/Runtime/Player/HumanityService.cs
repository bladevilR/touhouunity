using System;

namespace TouhouMigration.Runtime.Player
{
    // Mokou's humanity stat. Source of intent: Godot DialogueDatabaseExpanded "humanity" dialogue
    // effects (+/- deltas) and MokouMonologueSystem level thresholds. Global player stat (not
    // per-NPC like bonds): default 100, clamped 0..100, adjusted up and down by dialogue fx.
    public sealed class HumanityService
    {
        public const int MinHumanity = 0;
        public const int MaxHumanity = 100;
        public const int DefaultHumanity = 100;

        public int Humanity { get; private set; } = DefaultHumanity;
        public int LastDelta { get; private set; }

        public HumanityLevel Level => Classify(Humanity);

        public int Adjust(int delta)
        {
            int previous = Humanity;
            Humanity = Math.Clamp(Humanity + delta, MinHumanity, MaxHumanity);
            LastDelta = Humanity - previous;
            return Humanity;
        }

        public void Set(int value)
        {
            Humanity = Math.Clamp(value, MinHumanity, MaxHumanity);
            LastDelta = 0;
        }

        // Godot MokouMonologueSystem: humanity >= 70 -> high, >= 40 -> medium, else low.
        public static HumanityLevel Classify(int humanity)
        {
            if (humanity >= 70)
            {
                return HumanityLevel.High;
            }

            if (humanity >= 40)
            {
                return HumanityLevel.Medium;
            }

            return HumanityLevel.Low;
        }
    }

    public enum HumanityLevel
    {
        Low,
        Medium,
        High
    }
}
