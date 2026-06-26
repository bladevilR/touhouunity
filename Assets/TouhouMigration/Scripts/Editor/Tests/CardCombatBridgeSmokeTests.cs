using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardCombatBridge: extracting modify_bullet modifiers + applying them to an attack
    // (Godot CardBuildCombatBridge extract_bullet_modifiers / apply_bullet_modifiers).
    public static class CardCombatBridgeSmokeTests
    {
        private const double Tol = 1e-9;

        [MenuItem("Touhou Migration/Tests/Run Card Combat Bridge Smoke Tests")]
        public static void RunAll()
        {
            TestExtractModifiers();
            TestScorchTrail();
            TestSplitAndSealedHoming();
            TestPauseResumeAndStacking();
            Debug.Log("Card combat bridge smoke tests passed.");
        }

        private static void TestExtractModifiers()
        {
            MigrationCardCombatBridge bridge = new MigrationCardCombatBridge();
            IReadOnlyList<MigrationBulletModifier> mods = bridge.ExtractBulletModifiers(new[]
            {
                new MigrationCardEffectBlock { Type = "create_resource", Resource = "ember" },
                new MigrationCardEffectBlock { Type = "modify_bullet", Family = "fire", Modifier = "scorch_trail" },
            });

            AssertEqual(1, mods.Count, "Only modify_bullet blocks yield modifiers.");
            AssertEqual("fire", mods[0].Family, "The modifier carries its family.");
            AssertEqual("scorch_trail", mods[0].Modifier, "The modifier carries its modifier name.");
        }

        private static void TestScorchTrail()
        {
            MigrationCardCombatBridge bridge = new MigrationCardCombatBridge();
            MigrationCardAttack attack = bridge.ApplyBulletModifiers(
                new MigrationCardAttack { Damage = 100.0 },
                new[] { new MigrationBulletModifier { Family = "fire", Modifier = "scorch_trail" } });

            AssertTrue(Math.Abs(115.0 - attack.Damage) < Tol, "Scorch trail multiplies damage by 1.15.");
            AssertEqual("burn", attack.StatusOnHit, "Scorch trail applies burn on hit.");
            AssertEqual(true, attack.Tags.Contains("scorch"), "The scorch tag is added.");
        }

        private static void TestSplitAndSealedHoming()
        {
            MigrationCardCombatBridge bridge = new MigrationCardCombatBridge();
            MigrationCardAttack split = bridge.ApplyBulletModifiers(
                new MigrationCardAttack { Damage = 50.0 },
                new[] { new MigrationBulletModifier { Family = "wind", Modifier = "split" } });
            AssertEqual(2, split.ProjectileCountBonus, "Split adds two projectiles.");
            AssertEqual("split", split.BulletPattern, "Split sets the bullet pattern.");

            MigrationCardAttack homing = bridge.ApplyBulletModifiers(
                new MigrationCardAttack { Damage = 50.0 },
                new[] { new MigrationBulletModifier { Family = "mechanism", Modifier = "sealed_homing" } });
            AssertEqual(true, homing.Homing, "Sealed homing makes the bullet home.");
            AssertEqual("seal", homing.StatusOnHit, "Sealed homing applies seal on hit.");
        }

        private static void TestPauseResumeAndStacking()
        {
            MigrationCardCombatBridge bridge = new MigrationCardCombatBridge();
            MigrationCardAttack pause = bridge.ApplyBulletModifiers(
                new MigrationCardAttack { Damage = 50.0, SpeedMultiplier = 1.0 },
                new[] { new MigrationBulletModifier { Family = "time", Modifier = "pause_resume" } });
            AssertEqual(true, pause.DelayedRelease, "Pause-resume delays the release.");
            AssertTrue(Math.Abs(0.75 - pause.SpeedMultiplier) < Tol, "Pause-resume slows the bullet to 0.75x.");

            // Two scorch trails stack the damage multiplier (1.15 * 1.15).
            MigrationCardAttack stacked = bridge.ApplyBulletModifiers(
                new MigrationCardAttack { Damage = 100.0 },
                new[]
                {
                    new MigrationBulletModifier { Family = "fire", Modifier = "scorch_trail" },
                    new MigrationBulletModifier { Family = "fire", Modifier = "scorch_trail" },
                });
            AssertTrue(Math.Abs(132.25 - stacked.Damage) < 1e-6, "Stacked scorch trails multiply damage (1.15^2).");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
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
