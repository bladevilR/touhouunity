using System;
using TouhouMigration.Runtime.Combat;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationEnemyAiMath: the pure-math enemy-AI helpers (Godot EnemyAIHelper get_chase_direction
    // / calculate_knockback / is_target_in_sight / get_circle_directions / get_spread_directions).
    public static class EnemyAiMathSmokeTests
    {
        private const float Tol = 1e-4f;

        [MenuItem("Touhou Migration/Tests/Run Enemy AI Math Smoke Tests")]
        public static void RunAll()
        {
            TestChaseDirection();
            TestKnockback();
            TestLineOfSightFov();
            TestCircleDirections();
            TestSpreadDirections();
            Debug.Log("Enemy AI math smoke tests passed.");
        }

        private static void TestChaseDirection()
        {
            // Same horizontal line (y diff within threshold) -> aim straight at the target.
            Vector2 horizontal = MigrationEnemyAiMath.ChaseDirection(new Vector2(0, 0), new Vector2(100, 0));
            AssertClose(new Vector2(1, 0), horizontal, "On the same line the enemy chases horizontally.");

            // Far vertically -> close the vertical gap first (unit y, no x).
            Vector2 vertical = MigrationEnemyAiMath.ChaseDirection(new Vector2(0, 0), new Vector2(10, 100));
            AssertClose(new Vector2(0, 1), vertical, "A big vertical gap is closed vertically first.");
        }

        private static void TestKnockback()
        {
            Vector2 k = MigrationEnemyAiMath.Knockback(0f, new Vector2(1, 0), 1f);
            AssertClose(new Vector2(600, 0), k, "Zero-damage knockback is the base force / mass.");

            // damage 20 -> force 600 * (1 + 1.0) = 1200; mass 2 -> 600.
            Vector2 scaled = MigrationEnemyAiMath.Knockback(20f, new Vector2(1, 0), 2f);
            AssertClose(new Vector2(600, 0), scaled, "Knockback scales with damage and divides by mass.");
        }

        private static void TestLineOfSightFov()
        {
            AssertEqual(true, MigrationEnemyAiMath.IsTargetInSight(Vector2.zero, new Vector2(100, 0), new Vector2(1, 0)),
                "A target dead ahead is in sight.");
            AssertEqual(false, MigrationEnemyAiMath.IsTargetInSight(Vector2.zero, new Vector2(0, 100), new Vector2(1, 0)),
                "A target 90 degrees off (> 60 half-FOV) is out of sight.");
            AssertEqual(false, MigrationEnemyAiMath.IsTargetInSight(Vector2.zero, new Vector2(1000, 0), new Vector2(1, 0)),
                "A target beyond max distance is out of sight.");
        }

        private static void TestCircleDirections()
        {
            Vector2[] dirs = MigrationEnemyAiMath.CircleDirections(4, 0f);
            AssertEqual(4, dirs.Length, "Four directions form the ring.");
            AssertClose(new Vector2(1, 0), dirs[0], "The first points right.");
            AssertClose(new Vector2(0, 1), dirs[1], "The second is a quarter turn.");
            AssertClose(new Vector2(-1, 0), dirs[2], "The third points left.");
        }

        private static void TestSpreadDirections()
        {
            // Base right, 3 bullets, 90-degree spread -> -45, 0, +45.
            Vector2[] dirs = MigrationEnemyAiMath.SpreadDirections(new Vector2(1, 0), 3, 90f);
            AssertEqual(3, dirs.Length, "Three spread directions.");
            AssertClose(new Vector2(1, 0), dirs[1], "The middle bullet keeps the base direction.");
            // -45 degrees from +x is (cos -45, sin -45).
            AssertClose(new Vector2(Mathf.Cos(-45f * Mathf.Deg2Rad), Mathf.Sin(-45f * Mathf.Deg2Rad)), dirs[0],
                "The first bullet is rotated -45 degrees.");
        }

        private static void AssertClose(Vector2 expected, Vector2 actual, string message)
        {
            if ((expected - actual).sqrMagnitude > Tol)
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
