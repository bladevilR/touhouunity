using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class MigrationProjectileShatterResult
    {
        public MigrationProjectileShatterResult(
            MigrationEnemyProjectile projectile,
            string projectileFamily,
            string sourceFamily,
            float rawDamage,
            float damageMultiplier,
            float damageApplied,
            float remainingShatterHp,
            Vector3 position,
            bool wasWeakness,
            Object source)
        {
            Projectile = projectile;
            ProjectileFamily = string.IsNullOrWhiteSpace(projectileFamily) ? string.Empty : projectileFamily.Trim();
            SourceFamily = string.IsNullOrWhiteSpace(sourceFamily) ? string.Empty : sourceFamily.Trim();
            RawDamage = Mathf.Max(0f, rawDamage);
            DamageMultiplier = Mathf.Max(0f, damageMultiplier);
            DamageApplied = Mathf.Max(0f, damageApplied);
            RemainingShatterHp = Mathf.Max(0f, remainingShatterHp);
            Position = position;
            WasWeakness = wasWeakness;
            Source = source;
        }

        public MigrationEnemyProjectile Projectile { get; }
        public string ProjectileFamily { get; }
        public string SourceFamily { get; }
        public float RawDamage { get; }
        public float DamageMultiplier { get; }
        public float DamageApplied { get; }
        public float RemainingShatterHp { get; }
        public Vector3 Position { get; }
        public bool WasWeakness { get; }
        public Object Source { get; }
    }
}
