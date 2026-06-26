using System;

namespace TouhouMigration.Runtime.CardBuild
{
    // The CardBuild boss HP pool (Godot CardBuildMvpRunController: CIRNO_MAX_HP / _boss_hp / _damage_boss /
    // is_boss_defeated). Damage is clamped at 0 (never negative, never heals); IsDefeated reads hp <= 0.
    // The default max is Cirno's 540. UnityEngine-free + unit-testable. The damage multiplier from
    // apply_player_attack_damage (vulnerability / terrain / rewritten-rule) is a later bespoke slice — this
    // pool takes an already-resolved damage amount.
    public sealed class MigrationCardBossHp
    {
        public const int CirnoMaxHp = 540;

        public MigrationCardBossHp(int maxHp = CirnoMaxHp)
        {
            MaxHp = Math.Max(1, maxHp);
            CurrentHp = MaxHp;
        }

        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public bool IsDefeated => CurrentHp <= 0;

        // Remove HP (Godot _damage_boss): clamps current HP at 0 and returns the amount actually removed.
        // Non-positive damage and damage to an already-defeated boss are no-ops returning 0.
        public int Damage(int amount)
        {
            if (amount <= 0 || IsDefeated)
            {
                return 0;
            }

            int dealt = Math.Min(amount, CurrentHp);
            CurrentHp -= dealt;
            return dealt;
        }
    }
}
