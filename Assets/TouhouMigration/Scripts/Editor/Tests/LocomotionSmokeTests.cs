using System;
using TouhouMigration.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class LocomotionSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Locomotion Smoke Tests")]
        public static void RunAll()
        {
            TestIdle();
            TestWalkIsMovingNotRunning();
            TestRunSaturatesAndFlagsRunning();
            TestAirAndDashFlagsPassThrough();
            Debug.Log("Locomotion smoke tests passed.");
        }

        private static void TestIdle()
        {
            MigrationLocomotionParams p = MigrationLocomotion.Resolve(0f, 4.5f, 6.75f, true, false);
            AssertApprox(0f, p.NormalizedSpeed, "Idle has zero normalized speed.");
            AssertEqual(false, p.IsMoving, "Idle is not moving.");
            AssertEqual(false, p.IsRunning, "Idle is not running.");
        }

        private static void TestWalkIsMovingNotRunning()
        {
            MigrationLocomotionParams p = MigrationLocomotion.Resolve(4.5f, 4.5f, 6.75f, true, false);
            AssertApprox(0.6667f, p.NormalizedSpeed, "Walk normalized speed is walk/run.");
            AssertEqual(true, p.IsMoving, "Walking is moving.");
            AssertEqual(false, p.IsRunning, "Walk speed is below the run threshold.");
        }

        private static void TestRunSaturatesAndFlagsRunning()
        {
            MigrationLocomotionParams p = MigrationLocomotion.Resolve(6.75f, 4.5f, 6.75f, true, false);
            AssertApprox(1f, p.NormalizedSpeed, "Run saturates normalized speed at 1.");
            AssertEqual(true, p.IsMoving, "Running is moving.");
            AssertEqual(true, p.IsRunning, "Run speed is at/above the run threshold.");
        }

        private static void TestAirAndDashFlagsPassThrough()
        {
            MigrationLocomotionParams p = MigrationLocomotion.Resolve(6.75f, 4.5f, 6.75f, false, true);
            AssertEqual(false, p.IsGrounded, "Airborne grounded flag passes through.");
            AssertEqual(true, p.IsDashing, "Dashing flag passes through.");
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
