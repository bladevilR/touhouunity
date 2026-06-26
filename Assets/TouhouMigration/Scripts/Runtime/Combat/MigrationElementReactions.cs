namespace TouhouMigration.Runtime.Combat
{
    // Element types (Godot GameConstants.ElementType subset used by ElementData reactions).
    public enum MigrationElementType
    {
        None,
        Ice,
        Fire,
        Poison,
        Oil,
        Lightning,
        Gravity
    }

    // An elemental reaction's payload (Godot ElementData.ElementReaction).
    public sealed class MigrationElementReaction
    {
        public MigrationElementReaction(string name, string effectType, double damageMultiplier, double radius)
        {
            Name = name;
            EffectType = effectType;
            DamageMultiplier = damageMultiplier;
            Radius = radius;
        }

        public string Name { get; }
        public string EffectType { get; }
        public double DamageMultiplier { get; }
        public double Radius { get; }
    }

    // The elemental reaction table + order-independent lookup (Godot ElementData.ELEMENT_REACTIONS /
    // check_reaction): mixing two elements yields a reaction (explosion / freeze-shatter / corrosion /
    // steam / thunder-field) with a damage multiplier + radius. UnityEngine-free + unit-testable.
    public sealed class MigrationElementReactions
    {
        private readonly struct ReactionEntry
        {
            public ReactionEntry(MigrationElementType a, MigrationElementType b, MigrationElementReaction reaction)
            {
                A = a;
                B = b;
                Reaction = reaction;
            }

            public MigrationElementType A { get; }
            public MigrationElementType B { get; }
            public MigrationElementReaction Reaction { get; }
        }

        private static readonly ReactionEntry[] Reactions =
        {
            new ReactionEntry(MigrationElementType.Fire, MigrationElementType.Oil,
                new MigrationElementReaction("地狱火", "explosion", 3.0, 200.0)),
            new ReactionEntry(MigrationElementType.Ice, MigrationElementType.Poison,
                new MigrationElementReaction("寒霜瘟疫", "freeze_shatter", 2.0, 120.0)),
            new ReactionEntry(MigrationElementType.Lightning, MigrationElementType.Poison,
                new MigrationElementReaction("腐蚀雷电", "corrosion", 1.5, 0.0)),
            new ReactionEntry(MigrationElementType.Ice, MigrationElementType.Fire,
                new MigrationElementReaction("蒸汽爆炸", "steam", 1.0, 150.0)),
            new ReactionEntry(MigrationElementType.Gravity, MigrationElementType.Lightning,
                new MigrationElementReaction("雷暴领域", "thunder_field", 0.5, 180.0)),
        };

        public MigrationElementReaction CheckReaction(MigrationElementType element1, MigrationElementType element2)
        {
            foreach (ReactionEntry entry in Reactions)
            {
                if ((entry.A == element1 && entry.B == element2) || (entry.A == element2 && entry.B == element1))
                {
                    return entry.Reaction;
                }
            }

            return null;
        }
    }
}
