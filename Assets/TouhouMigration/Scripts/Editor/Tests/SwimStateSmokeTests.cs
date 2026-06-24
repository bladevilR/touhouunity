using System;
using TouhouMigration.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class SwimStateSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Swim State Smoke Tests")]
        public static void RunAll()
        {
            TestSubmergedSlowsHorizontalSpeed();
            TestSubmergedUsesBuoyantClampedVertical();
            TestSurfacedUsesNormalGravity();
            Debug.Log("Swim state smoke tests passed.");
        }

        private static void TestSubmergedSlowsHorizontalSpeed()
        {
            MigrationSwimState swim = new MigrationSwimState();
            swim.Configure(0.6f, 0.2f, 2f, 3f);

            AssertApprox(10f, swim.ResolveHorizontalSpeed(10f), "Surfaced speed is unchanged.");
            swim.SetSubmerged(true);
            AssertEqual(true, swim.IsSwimming, "Submerged means swimming.");
            AssertApprox(6f, swim.ResolveHorizontalSpeed(10f), "Submerged speed uses the swim multiplier.");
        }

        private static void TestSubmergedUsesBuoyantClampedVertical()
        {
            MigrationSwimState swim = new MigrationSwimState();
            swim.Configure(0.6f, 0.2f, 2f, 3f);
            swim.SetSubmerged(true);

            // currentVertical 0, gravity -24, delta 1 -> 0 + (-24 * 0.2) = -4.8, clamped to -maxSink (-2).
            float v = swim.ResolveVerticalVelocity(0f, -24f, 1f);
            AssertApprox(-2f, v, "Submerged sink speed is clamped to the buoyant max sink.");
        }

        private static void TestSurfacedUsesNormalGravity()
        {
            MigrationSwimState swim = new MigrationSwimState();
            swim.Configure(0.6f, 0.2f, 2f, 3f);

            float v = swim.ResolveVerticalVelocity(0f, -24f, 1f);
            AssertApprox(-24f, v, "Surfaced vertical motion uses full gravity.");
        }

        private static void AssertApprox(float expected, float actual, string message)
        {
            if (Math.Abs(expected - actual) > 0.001f)
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
