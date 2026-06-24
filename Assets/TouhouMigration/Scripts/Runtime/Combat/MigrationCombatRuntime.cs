using TouhouMigration.Runtime.Player;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class MigrationCombatRuntime
    {
        private readonly MigrationPlayerController playerController;
        private readonly MigrationPlayerHealthRuntime playerHealthRuntime;

        public MigrationCombatRuntime(
            MigrationPlayerController playerController,
            MigrationPlayerHealthRuntime playerHealthRuntime)
        {
            this.playerController = playerController;
            this.playerHealthRuntime = playerHealthRuntime;
        }

        public CombatBridgeResult ApplyPlayerAttack(
            MigrationCombatTargetRuntime target,
            float baseDamage,
            string attackType)
        {
            return ApplyPlayerAttackInternal(
                baseDamage,
                attackType,
                modifiedDamage => target != null
                    ? target.ApplyDamage(modifiedDamage)
                    : new CombatBridgeResult { RawDamage = modifiedDamage });
        }

        public CombatBridgeResult ApplyPlayerAttackToBehaviour(
            MigrationCombatTargetBehaviour target,
            float baseDamage,
            string attackType)
        {
            return ApplyPlayerAttackInternal(
                baseDamage,
                attackType,
                modifiedDamage => target != null
                    ? target.ApplyDamage(modifiedDamage)
                    : new CombatBridgeResult { RawDamage = modifiedDamage });
        }

        public PlayerHealthResult ApplyDamageToPlayer(float rawDamage)
        {
            return playerHealthRuntime != null
                ? playerHealthRuntime.ApplyDamage(rawDamage)
                : new PlayerHealthResult { RawDamage = rawDamage };
        }

        private CombatBridgeResult ApplyPlayerAttackInternal(
            float baseDamage,
            string attackType,
            System.Func<float, CombatBridgeResult> applyDamage)
        {
            string normalizedAttackType = string.IsNullOrWhiteSpace(attackType)
                ? string.Empty
                : attackType.Trim().ToLowerInvariant();
            float modifiedDamage = playerController != null
                ? playerController.GetModifiedAttackDamage(baseDamage, normalizedAttackType)
                : baseDamage;

            CombatBridgeResult result = applyDamage != null
                ? applyDamage(modifiedDamage)
                : new CombatBridgeResult { RawDamage = modifiedDamage };
            result.AttackType = normalizedAttackType;

            if (result.TargetDefeated && playerHealthRuntime != null)
            {
                PlayerHealthResult killHeal = playerHealthRuntime.NotifyEnemyKilled();
                result.PlayerHealthResult = killHeal;
                result.PlayerHealApplied = killHeal?.HealApplied ?? 0f;
            }

            return result;
        }
    }
}
