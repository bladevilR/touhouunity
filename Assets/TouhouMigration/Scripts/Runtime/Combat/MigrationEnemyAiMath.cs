using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    // Pure-math enemy-AI helpers (Godot EnemyAIHelper): chase/knockback/FOV + the circle/spread bullet
    // direction generators. UnityEngine.Vector2 in/out; the Node-bound separation/line-of-sight helpers are
    // scene work.
    public static class MigrationEnemyAiMath
    {
        // Godot get_chase_direction: close a big vertical gap first, else aim straight at the target.
        public static Vector2 ChaseDirection(Vector2 enemyPos, Vector2 targetPos, float yThreshold = 40f)
        {
            Vector2 toTarget = targetPos - enemyPos;
            if (Mathf.Abs(toTarget.y) > yThreshold)
            {
                return new Vector2(0f, Mathf.Sign(toTarget.y));
            }

            return (targetPos - enemyPos).normalized;
        }

        // Godot calculate_knockback: base force scales with damage, divided by mass.
        public static Vector2 Knockback(float damage, Vector2 direction, float mass, float baseForce = 600f)
        {
            if (mass == 0f)
            {
                return Vector2.zero;
            }

            float force = baseForce * (1f + damage * 0.05f);
            return direction * force / mass;
        }

        // Godot is_target_in_sight: within max distance and within the half-FOV cone.
        public static bool IsTargetInSight(Vector2 enemyPos, Vector2 targetPos, Vector2 facing, float fovAngle = 120f, float maxDistance = 500f)
        {
            Vector2 toTarget = targetPos - enemyPos;
            if (toTarget.magnitude > maxDistance)
            {
                return false;
            }

            float angle = Vector2.Angle(facing, toTarget.normalized);
            return angle <= fovAngle / 2f;
        }

        // Godot get_circle_directions: count evenly-spaced directions around the full circle from startAngle.
        public static Vector2[] CircleDirections(int count, float startAngle = 0f)
        {
            if (count <= 0)
            {
                return new Vector2[0];
            }

            Vector2[] directions = new Vector2[count];
            float step = 2f * Mathf.PI / count;
            for (int i = 0; i < count; i++)
            {
                directions[i] = Rotate(Vector2.right, startAngle + i * step);
            }

            return directions;
        }

        // Godot get_spread_directions: count directions fanned across spreadAngleDeg around baseDirection.
        public static Vector2[] SpreadDirections(Vector2 baseDirection, int count, float spreadAngleDeg)
        {
            if (count <= 0)
            {
                return new Vector2[0];
            }

            Vector2[] directions = new Vector2[count];
            float halfSpread = spreadAngleDeg / 2f;
            float angleStep = spreadAngleDeg / Mathf.Max(count - 1, 1);
            for (int i = 0; i < count; i++)
            {
                float offsetDeg = -halfSpread + i * angleStep;
                directions[i] = Rotate(baseDirection, offsetDeg * Mathf.Deg2Rad);
            }

            return directions;
        }

        private static Vector2 Rotate(Vector2 v, float radians)
        {
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
