using System;
using TouhouMigration.Runtime.Cooking;

namespace TouhouMigration.Runtime.Player
{
    public sealed class MigrationPlayerHealthRuntime
    {
        private const float BaseHitstunSeconds = 0.1f;

        private CookingBuffService cookingBuffService;

        // Opt-in general player i-frame window. Default 0 = disabled (no behavior change to existing
        // callers); the live player sets a real duration. A landed hit starts the window; damage while
        // it is active is blocked. The window counts down in Tick, independent of cooking buffs.
        private float invulnerabilitySeconds;
        private float invulnerabilityRemaining;

        public float MaxHp { get; private set; } = 100f;
        public float CurrentHp { get; private set; } = 100f;
        public bool IsDead { get; private set; }
        public bool RebirthUsed { get; private set; }

        public bool IsInvulnerable => invulnerabilityRemaining > 0f;
        public float InvulnerabilityRemainingSeconds => invulnerabilityRemaining;

        public void BindCookingBuffs(CookingBuffService buffs)
        {
            cookingBuffService = buffs;
            SyncBuffHpRatio();
        }

        public void SetInvulnerabilityDuration(float seconds)
        {
            invulnerabilitySeconds = Math.Max(0f, seconds);
        }

        public void SetHealth(float currentHp, float maxHp)
        {
            MaxHp = Math.Max(1f, maxHp);
            CurrentHp = Clamp(currentHp, 0f, MaxHp);
            IsDead = CurrentHp <= 0f;
            SyncBuffHpRatio();
        }

        public PlayerHealthResult ApplyDamage(float amount)
        {
            float rawDamage = Math.Max(0f, amount);
            PlayerHealthResult result = NewResult();
            result.RawDamage = rawDamage;

            if (rawDamage <= 0f || IsDead)
            {
                return result;
            }

            if (IsInvulnerable)
            {
                result.BlockedByInvulnerability = true;
                return result;
            }

            SyncBuffHpRatio();
            float reduction = Clamp(cookingBuffService?.GetDamageReduction() ?? 0f, 0f, 0.9f);
            float finalDamage = rawDamage * (1f - reduction);
            result.DamageApplied = finalDamage;
            result.WasLethal = finalDamage + 0.001f >= CurrentHp;

            // A landed hit opens the i-frame window (both the rebirth and the normal path count as a hit).
            invulnerabilityRemaining = invulnerabilitySeconds;

            if (result.WasLethal &&
                !RebirthUsed &&
                cookingBuffService != null &&
                cookingBuffService.HasSpecialEffect("rebirth_once"))
            {
                RebirthUsed = true;
                CurrentHp = MaxHp * 0.5f;
                IsDead = false;
                result.RebirthTriggered = true;
                SyncBuffHpRatio();
                result.CurrentHp = CurrentHp;
                result.MaxHp = MaxHp;
                return result;
            }

            CurrentHp = Math.Max(0f, CurrentHp - finalDamage);
            IsDead = CurrentHp <= 0f;
            SyncBuffHpRatio();
            result.CurrentHp = CurrentHp;
            result.MaxHp = MaxHp;
            return result;
        }

        public float Heal(float amount)
        {
            if (IsDead)
            {
                return 0f;
            }

            float before = CurrentHp;
            CurrentHp = Math.Min(MaxHp, CurrentHp + Math.Max(0f, amount));
            SyncBuffHpRatio();
            return CurrentHp - before;
        }

        public PlayerHealthResult Tick(float delta)
        {
            PlayerHealthResult result = NewResult();
            if (delta <= 0f || IsDead)
            {
                return result;
            }

            if (invulnerabilityRemaining > 0f)
            {
                invulnerabilityRemaining = Math.Max(0f, invulnerabilityRemaining - delta);
            }

            if (cookingBuffService == null)
            {
                return result;
            }

            float regenPerSecond = cookingBuffService.GetRegenerationPerSecond(MaxHp, CurrentHp);
            result.HealApplied = Heal(regenPerSecond * delta);
            result.CurrentHp = CurrentHp;
            result.MaxHp = MaxHp;
            return result;
        }

        public PlayerHealthResult NotifyEnemyKilled()
        {
            PlayerHealthResult result = NewResult();
            if (IsDead || cookingBuffService == null)
            {
                return result;
            }

            float healPercent = cookingBuffService.GetEnemyKillHealPercent();
            result.HealApplied = Heal(MaxHp * (healPercent / 100f));
            result.CurrentHp = CurrentHp;
            result.MaxHp = MaxHp;
            return result;
        }

        public float GetHitstunSeconds()
        {
            float hitstun = BaseHitstunSeconds;
            if (cookingBuffService != null && cookingBuffService.IsThresholdActive("def", 6))
            {
                hitstun *= 0.5f;
            }

            if (cookingBuffService != null && cookingBuffService.HasSpecialEffect("hitstun_resist_20"))
            {
                hitstun *= 0.8f;
            }

            return hitstun;
        }

        public bool ShouldSuppressHitFeedbackWhileAttacking()
        {
            return cookingBuffService != null &&
                (cookingBuffService.IsThresholdActive("def", 10) ||
                 cookingBuffService.IsComboActiveAtkDef());
        }

        private PlayerHealthResult NewResult()
        {
            return new PlayerHealthResult
            {
                CurrentHp = CurrentHp,
                MaxHp = MaxHp
            };
        }

        private void SyncBuffHpRatio()
        {
            cookingBuffService?.SetPlayerHpRatio(MaxHp > 0f ? CurrentHp / MaxHp : 1f);
        }

        private static float Clamp(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
