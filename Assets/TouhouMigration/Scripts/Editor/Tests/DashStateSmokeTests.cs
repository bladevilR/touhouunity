using System;
using TouhouMigration.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class DashStateSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Dash State Smoke Tests")]
        public static void RunAll()
        {
            TestDashStartsAndBlocksUntilCooldownClears();
            TestActiveWindowEndsBeforeCooldown();
            TestConfigureClampsNegativeValues();
            Debug.Log("Dash state smoke tests passed.");
        }

        private static void TestDashStartsAndBlocksUntilCooldownClears()
        {
            MigrationDashState dash = new MigrationDashState();
            dash.Configure(1.5f, 0.2f, 14f);

            AssertEqual(true, dash.CanDash, "A fresh dash state can dash.");
            AssertEqual(true, dash.TryStartDash(), "First dash starts.");
            AssertEqual(true, dash.IsDashing, "Player is dashing right after start.");
            AssertEqual(true, dash.IsOnCooldown, "Cooldown begins when the dash starts.");
            AssertEqual(false, dash.TryStartDash(), "Cannot dash again while dashing or cooling.");
        }

        private static void TestActiveWindowEndsBeforeCooldown()
        {
            MigrationDashState dash = new MigrationDashState();
            dash.Configure(1.5f, 0.2f, 14f);
            dash.TryStartDash();

            dash.Tick(0.2f);
            AssertEqual(false, dash.IsDashing, "Dash active window ends after its duration.");
            AssertEqual(true, dash.IsOnCooldown, "Still on cooldown after the active window (1.5 > 0.2).");
            AssertEqual(false, dash.TryStartDash(), "Cannot re-dash during cooldown.");

            dash.Tick(1.3f);
            AssertEqual(false, dash.IsOnCooldown, "Cooldown clears after its full duration.");
            AssertEqual(true, dash.CanDash, "Can dash again once cooldown clears.");
            AssertEqual(true, dash.TryStartDash(), "Dash starts again after cooldown.");
        }

        private static void TestConfigureClampsNegativeValues()
        {
            MigrationDashState dash = new MigrationDashState();
            dash.Configure(-1f, -1f, -5f);
            AssertApprox(0f, dash.DashSpeed, "Negative dash speed clamps to zero.");
            AssertEqual(true, dash.TryStartDash(), "A zero-cooldown/duration dash can still start.");
            AssertEqual(false, dash.IsDashing, "Zero-duration dash is not active.");
            AssertEqual(true, dash.CanDash, "Zero-cooldown dash is immediately ready again.");
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
