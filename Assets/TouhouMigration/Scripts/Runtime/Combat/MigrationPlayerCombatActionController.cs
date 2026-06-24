using TouhouMigration.Runtime.UI;
using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class MigrationPlayerCombatActionController : MonoBehaviour
    {
        [SerializeField] private MigrationPlayerAttackHitbox attackHitbox;
        [SerializeField] private MigrationProjectileSpecialSettlement projectileSettlement;
        [SerializeField] private float lightAttackDamage = 10f;
        [SerializeField] private float heavyAttackDamage = 20f;

        public bool IsAttacking { get; private set; }
        public string CurrentAttackType { get; private set; } = string.Empty;
        public int AttackWindowCount { get; private set; }
        public float LastHeavyBurstRadiusMultiplier { get; private set; } = 1f;
        public bool HasProjectileSettlement => projectileSettlement != null;

        public void BindAttackHitbox(MigrationPlayerAttackHitbox hitbox)
        {
            attackHitbox = hitbox;
        }

        public void BindProjectileSettlement(MigrationProjectileSpecialSettlement settlement)
        {
            projectileSettlement = settlement;
        }

        public void ConfigureDamage(float lightDamage, float heavyDamage)
        {
            lightAttackDamage = Mathf.Max(0f, lightDamage);
            heavyAttackDamage = Mathf.Max(0f, heavyDamage);
        }

        public void TriggerLightAttack()
        {
            TriggerAttack("light", lightAttackDamage);
        }

        public void TriggerHeavyAttack()
        {
            TriggerAttack("heavy", heavyAttackDamage);
        }

        public void CompleteAttackWindow()
        {
            IsAttacking = false;
            attackHitbox?.EndAttackWindow();
            attackHitbox?.ConfigureRangeMultiplier(1f);
        }

        private void Awake()
        {
            attackHitbox ??= GetComponentInChildren<MigrationPlayerAttackHitbox>();
            projectileSettlement ??= MigrationGlobalUiController.FindProjectileSettlement();
        }

        private void Update()
        {
            if (MigrationGlobalUiController.IsGameplayInputBlocked() || IsAttacking)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TriggerLightAttack();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                TriggerHeavyAttack();
            }
        }

        private void TriggerAttack(string attackType, float damage)
        {
            string normalizedAttackType = string.IsNullOrWhiteSpace(attackType)
                ? string.Empty
                : attackType.Trim().ToLowerInvariant();

            CurrentAttackType = normalizedAttackType;
            IsAttacking = true;
            AttackWindowCount++;
            float rangeMultiplier = ResolveRangeMultiplier(normalizedAttackType);
            LastHeavyBurstRadiusMultiplier = normalizedAttackType == "heavy" ? rangeMultiplier : 1f;

            if (attackHitbox != null)
            {
                attackHitbox.Configure(damage, normalizedAttackType);
                attackHitbox.ConfigureRangeMultiplier(rangeMultiplier);
                attackHitbox.BeginAttackWindow();
            }
        }

        private float ResolveRangeMultiplier(string normalizedAttackType)
        {
            if (normalizedAttackType != "heavy")
            {
                return 1f;
            }

            MigrationProjectileSpecialSettlement settlement = ResolveProjectileSettlement();
            return settlement != null ? settlement.ConsumePendingHeavyBurstRadiusMultiplier() : 1f;
        }

        private MigrationProjectileSpecialSettlement ResolveProjectileSettlement()
        {
            if (projectileSettlement == null)
            {
                projectileSettlement = MigrationGlobalUiController.FindProjectileSettlement();
            }

            return projectileSettlement;
        }
    }
}
