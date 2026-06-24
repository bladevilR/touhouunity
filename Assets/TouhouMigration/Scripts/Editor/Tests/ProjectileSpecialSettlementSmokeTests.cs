using System;
using System.Reflection;
using TouhouMigration.Runtime.Combat;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class ProjectileSpecialSettlementSmokeTests
    {
        private const string BuilderTypeName = "TouhouMigration.Editor.TouhouMigrationProjectBuilder, Assembly-CSharp-Editor";
        private const string CombatFeedbackPrefabsRoot = "Assets/TouhouMigration/Prefabs/CombatFeedback";

        [MenuItem("Touhou Migration/Tests/Run Projectile Special Settlement Smoke Tests")]
        public static void RunAll()
        {
            TestPhoenixGaugeUsesGodotLikeAttackGrazeAndSoftCapRules();
            TestSettlementTurnsProjectileGrazeAndShatterIntoGaugeRewards();
            TestSettlementTracksThreeIceCrystalHeavyBurstWindow();
            TestSettlementHandlesPerfectDashGrazeAndFrozenCrystalStaggerCounter();
            TestGeneratedProjectileFeedbackPrefabCarriesSettlementSeam();
            Debug.Log("Projectile special settlement smoke tests passed.");
        }

        private static void TestPhoenixGaugeUsesGodotLikeAttackGrazeAndSoftCapRules()
        {
            MigrationPhoenixGaugeRuntime gauge = new MigrationPhoenixGaugeRuntime();
            gauge.Reset(50f);

            float attackApplied = gauge.AddAttack(12f, "ice_crystal");
            float grazeApplied = gauge.AddGraze(60f, "graze:normal");
            gauge.Tick(1.01f);
            float nextGrazeApplied = gauge.AddGraze(8f, "graze:perfect");

            AssertApproximately(12f, attackApplied, 0.001f, "Attack reward should apply directly.");
            AssertApproximately(45f, grazeApplied, 0.001f, "Graze reward should respect the Godot 45/s soft cap.");
            AssertApproximately(8f, nextGrazeApplied, 0.001f, "Graze soft cap should reset after the one-second window.");
            AssertApproximately(115f, gauge.CurrentValue, 0.001f, "Gauge should contain starting value plus accepted gains.");
            AssertEqual(1, gauge.FilledSegments, "115 gauge should fill one 100-point segment.");
            AssertEqual("graze:perfect", gauge.LastReason, "Gauge should remember the last accepted reason.");
            AssertEqual(3, gauge.ChangeEventCount, "Gauge should count accepted changes.");
        }

        private static void TestSettlementTurnsProjectileGrazeAndShatterIntoGaugeRewards()
        {
            GameObject projectileObject = new GameObject("ProjectileSpecialSettlementSmoke_Projectiles");
            try
            {
                MigrationPhoenixGaugeRuntime gauge = new MigrationPhoenixGaugeRuntime();
                gauge.Reset(50f);

                MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                projectile.Configure(20f, 7f, Vector3.forward, true, 0.35f);
                projectile.ConfigureGraze(true, 1.15f, 0.7f);
                projectile.ConfigureShatterRules("ice_crystal", true, 25f, "fire,heavy,shatter");

                MigrationProjectileSpecialSettlement settlement = projectileObject.AddComponent<MigrationProjectileSpecialSettlement>();
                settlement.BindGauge(gauge);
                settlement.BindProjectile(projectile);

                projectile.Tick(0.3f, new Vector3(0.9f, 0f, 5f));
                bool shattered = projectile.TryApplyShatterDamage(20f, "fire", new Vector3(2f, 0f, 0f));

                AssertEqual(true, shattered, "Projectile should shatter for the settlement test.");
                AssertApproximately(64f, gauge.CurrentValue, 0.001f, "Normal graze + ice crystal shatter should grant 2 + 12 gauge.");
                AssertEqual(1, settlement.GrazeSettlementCount, "Settlement should count graze rewards.");
                AssertEqual(1, settlement.ShatterSettlementCount, "Settlement should count shatter rewards.");
                AssertApproximately(12f, settlement.LastGaugeGain, 0.001f, "Last settlement should be the shatter reward.");
                AssertEqual("ice_crystal", settlement.LastGaugeReason, "Shatter reason should preserve projectile family.");
                AssertEqual(1, projectile.ShatterEventCount, "Settlement should not own or duplicate projectile shatter authority.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
            }
        }

        private static void TestSettlementHandlesPerfectDashGrazeAndFrozenCrystalStaggerCounter()
        {
            MigrationPhoenixGaugeRuntime gauge = new MigrationPhoenixGaugeRuntime();
            gauge.Reset(0f);
            MigrationProjectileSpecialSettlement settlement = new GameObject("ProjectileSpecialSettlementSmoke_Stagger").AddComponent<MigrationProjectileSpecialSettlement>();
            try
            {
                settlement.BindGauge(gauge);
                settlement.SetPlayerDashing(true);
                settlement.SettleGraze(new MigrationProjectileGrazeResult(
                    null,
                    "perfect",
                    0.5f,
                    0.35f,
                    1.15f,
                    0.7f,
                    Vector3.zero,
                    Vector3.zero));

                for (int index = 0; index < 12; index++)
                {
                    settlement.SettleShatter(new MigrationProjectileShatterResult(
                        null,
                        "frozen_crystal",
                        "heavy",
                        20f,
                        1.5f,
                        30f,
                        0f,
                        Vector3.zero,
                        true,
                        null));
                }

                AssertApproximately(152f, gauge.CurrentValue, 0.001f, "Perfect dash graze plus twelve frozen crystals should grant 8 + 144 gauge.");
                AssertEqual(12, settlement.FrozenCrystalBreakCount, "Settlement should remember total frozen crystal breaks.");
                AssertEqual(1, settlement.PerfectFreezeStaggerEventCount, "Twelve frozen crystals should expose one stagger opportunity.");
                AssertApproximately(1.2f, settlement.LastPerfectFreezeStaggerSeconds, 0.001f, "Perfect Freeze stagger should match Godot's 1.2 seconds.");
                AssertEqual("perfect_freeze_crystal", settlement.LastGaugeReason, "Frozen crystal settlement should expose the Perfect Freeze reward reason.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settlement.gameObject);
            }
        }

        private static void TestSettlementTracksThreeIceCrystalHeavyBurstWindow()
        {
            MigrationPhoenixGaugeRuntime gauge = new MigrationPhoenixGaugeRuntime();
            gauge.Reset(0f);
            MigrationProjectileSpecialSettlement settlement = new GameObject("ProjectileSpecialSettlementSmoke_IceCrystalStreak").AddComponent<MigrationProjectileSpecialSettlement>();
            try
            {
                settlement.BindGauge(gauge);
                for (int index = 0; index < 3; index++)
                {
                    settlement.SettleShatter(new MigrationProjectileShatterResult(
                        null,
                        "ice_crystal",
                        "heavy",
                        20f,
                        1.5f,
                        30f,
                        0f,
                        Vector3.zero,
                        true,
                        null));
                }

                AssertApproximately(36f, gauge.CurrentValue, 0.001f, "Three ice crystals should grant 36 gauge total.");
                AssertEqual(3, settlement.IceCrystalBreakCount, "Settlement should count ice crystal breaks.");
                AssertEqual(0, settlement.IceCrystalBreakStreak, "Third ice crystal should consume the streak.");
                AssertApproximately(1.25f, settlement.PendingHeavyBurstRadiusMultiplier, 0.001f, "Three ice crystals should empower the next heavy burst radius.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settlement.gameObject);
            }
        }

        private static void TestGeneratedProjectileFeedbackPrefabCarriesSettlementSeam()
        {
            InvokeBuilder("BuildCombatFeedbackPrefabs");

            GameObject projectilePrefab = RequiredPrefab($"{CombatFeedbackPrefabsRoot}/MigrationEnemyProjectileFeedback.prefab");
            MigrationProjectileSpecialSettlement settlement = projectilePrefab.GetComponent<MigrationProjectileSpecialSettlement>();

            AssertEqual(true, settlement != null, "Projectile feedback prefab should carry projectile special settlement.");
            AssertApproximately(2f, settlement.NormalGrazeGauge, 0.001f, "Settlement should serialize normal graze gauge.");
            AssertApproximately(5f, settlement.DashGrazeGauge, 0.001f, "Settlement should serialize dash graze gauge.");
            AssertApproximately(8f, settlement.PerfectDashGrazeGauge, 0.001f, "Settlement should serialize perfect dash graze gauge.");
            AssertApproximately(12f, settlement.ShatterGauge, 0.001f, "Settlement should serialize shatter gauge reward.");
        }

        private static GameObject RequiredPrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new Exception($"Missing prefab at {path}.");
            }

            return prefab;
        }

        private static void InvokeBuilder(string methodName)
        {
            Type builderType = Type.GetType(BuilderTypeName);
            if (builderType == null)
            {
                throw new Exception($"Missing builder type: {BuilderTypeName}");
            }

            MethodInfo method = builderType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            if (method == null)
            {
                throw new Exception($"Missing builder method: {methodName}");
            }

            method.Invoke(null, Array.Empty<object>());
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }

        private static void AssertApproximately(float expected, float actual, float tolerance, string message)
        {
            if (Mathf.Abs(expected - actual) > tolerance)
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
