using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class MigrationProjectileReflectResult
    {
        public MigrationProjectileReflectResult(
            MigrationEnemyProjectile projectile,
            string projectileFamily,
            string sourceFamily,
            Vector3 position,
            Vector3 reflectedDirection,
            float speed,
            float damage,
            bool stunReward,
            float stunSeconds,
            Object source)
        {
            Projectile = projectile;
            ProjectileFamily = string.IsNullOrWhiteSpace(projectileFamily) ? string.Empty : projectileFamily.Trim();
            SourceFamily = string.IsNullOrWhiteSpace(sourceFamily) ? string.Empty : sourceFamily.Trim();
            Position = position;
            ReflectedDirection = reflectedDirection.sqrMagnitude > 0.0001f
                ? reflectedDirection.normalized
                : Vector3.forward;
            Speed = Mathf.Max(0f, speed);
            Damage = Mathf.Max(0f, damage);
            StunReward = stunReward;
            StunSeconds = Mathf.Max(0f, stunSeconds);
            Source = source;
        }

        public MigrationEnemyProjectile Projectile { get; }
        public string ProjectileFamily { get; }
        public string SourceFamily { get; }
        public Vector3 Position { get; }
        public Vector3 ReflectedDirection { get; }
        public float Speed { get; }
        public float Damage { get; }
        public bool StunReward { get; }
        public float StunSeconds { get; }
        public Object Source { get; }
    }
}
