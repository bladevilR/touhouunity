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

        // Restore current HP from a card-run save, clamped to [0, MaxHp].
        public void RestoreHp(int currentHp)
        {
            CurrentHp = currentHp < 0 ? 0 : currentHp > MaxHp ? MaxHp : currentHp;
        }

        // Resolve a raw player-attack amount through the Cirno guard model (Godot
        // apply_player_attack_damage) and apply it: x1.0 + 0.16/rewritten-rule when the vulnerability
        // window is open, else a chip multiplier (x0.42 at terrain pressure <= 1, otherwise x0.18). The
        // result is at least 1 on any positive hit. Returns the HP actually removed; 0 for a non-positive
        // amount or an already-defeated boss.
        public int ApplyPlayerAttack(double amount, bool vulnerabilityOpen, int terrainPressure, int rewrittenRuleCount)
        {
            if (amount <= 0.0 || IsDefeated)
            {
                return 0;
            }

            double multiplier = 0.18;
            if (vulnerabilityOpen)
            {
                multiplier = 1.0 + rewrittenRuleCount * 0.16;
            }
            else if (terrainPressure <= 1)
            {
                multiplier = 0.42;
            }

            int resolved = Math.Max(1, (int)Math.Round(amount * multiplier, MidpointRounding.AwayFromZero));
            return Damage(resolved);
        }
    }
}
