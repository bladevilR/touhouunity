using System;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class MigrationCombatTargetRuntime
    {
        public MigrationCombatTargetRuntime(float maxHp)
        {
            MaxHp = Math.Max(1f, maxHp);
            CurrentHp = MaxHp;
        }

        public float MaxHp { get; private set; }
        public float CurrentHp { get; private set; }
        public bool IsDefeated { get; private set; }

        public CombatBridgeResult ApplyDamage(float amount)
        {
            CombatBridgeResult result = new CombatBridgeResult
            {
                RawDamage = Math.Max(0f, amount),
                TargetCurrentHp = CurrentHp,
                TargetMaxHp = MaxHp
            };

            if (IsDefeated || result.RawDamage <= 0f)
            {
                return result;
            }

            result.DamageApplied = result.RawDamage;
            CurrentHp = Math.Max(0f, CurrentHp - result.DamageApplied);
            bool newlyDefeated = CurrentHp <= 0f;
            if (newlyDefeated)
            {
                IsDefeated = true;
            }

            result.TargetDefeated = newlyDefeated;
            result.TargetCurrentHp = CurrentHp;
            result.TargetMaxHp = MaxHp;
            return result;
        }
    }
}
