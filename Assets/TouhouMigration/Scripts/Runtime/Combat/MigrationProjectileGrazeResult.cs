using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    public readonly struct MigrationProjectileGrazeResult
    {
        public MigrationProjectileGrazeResult(
            MigrationEnemyProjectile projectile,
            string quality,
            float distance,
            float hitRadius,
            float grazeRadius,
            float perfectGrazeRadius,
            Vector3 playerPosition,
            Vector3 closestProjectilePoint)
        {
            Projectile = projectile;
            Quality = string.IsNullOrWhiteSpace(quality) ? "normal" : quality;
            Distance = Mathf.Max(0f, distance);
            HitRadius = Mathf.Max(0f, hitRadius);
            GrazeRadius = Mathf.Max(0f, grazeRadius);
            PerfectGrazeRadius = Mathf.Max(0f, perfectGrazeRadius);
            PlayerPosition = playerPosition;
            ClosestProjectilePoint = closestProjectilePoint;
        }

        public MigrationEnemyProjectile Projectile { get; }
        public string Quality { get; }
        public float Distance { get; }
        public float HitRadius { get; }
        public float GrazeRadius { get; }
        public float PerfectGrazeRadius { get; }
        public Vector3 PlayerPosition { get; }
        public Vector3 ClosestProjectilePoint { get; }
        public bool IsPerfect => Quality == "perfect";
    }
}
