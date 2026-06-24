using System;
using System.Reflection;
using TouhouMigration.Runtime.Combat;
using TouhouMigration.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class EnemyProjectilePerfectFreezeCycleSmokeTests
    {
        private const string BuilderTypeName = "TouhouMigration.Editor.TouhouMigrationProjectBuilder, Assembly-CSharp-Editor";
        private const string CombatFeedbackPrefabsRoot = "Assets/TouhouMigration/Prefabs/CombatFeedback";

        [MenuItem("Touhou Migration/Tests/Run Enemy Projectile Perfect Freeze Cycle Smoke Tests")]
        public static void RunAll()
        {
            TestPerfectFreezeProjectileSpraysFreezesThenThaws();
            TestPerfectFreezeProjectileArmDelayDefersDangerAndCycle();
            TestFrozenPerfectFreezeProjectileShattersIntoSettlementStaggerChain();
            TestFeedbackTemplateAppliesPerfectFreezeCycleData();
            TestGeneratedPerfectFreezeProjectileFeedbackPrefabCarriesCycleData();
            Debug.Log("Enemy projectile Perfect Freeze cycle smoke tests passed.");
        }

        private static void TestPerfectFreezeProjectileSpraysFreezesThenThaws()
        {
            GameObject projectileObject = new GameObject("EnemyProjectilePerfectFreezeCycleSmoke_Cycle");
            try
            {
                MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                projectile.Configure(1f, 1f, Vector3.forward, true, 0.35f);
                projectile.ConfigurePerfectFreezeCycle(
                    true,
                    1.6f,
                    2.4f,
                    4.2f,
                    8f,
                    7f,
                    8f,
                    10f,
                    20f);

                AssertEqual(true, projectile.PerfectFreezeCycleEnabled, "Projectile should expose the Perfect Freeze cycle flag.");
                AssertEqual("spray", projectile.CurrentPerfectFreezeState, "Perfect Freeze projectile should start in spray state.");
                AssertEqual(false, projectile.IsFrozen, "Spray state should not count as frozen.");
                AssertApproximately(4.2f, projectile.Speed, 0.001f, "Spray state should use Godot spray speed.");
                AssertApproximately(8f, projectile.Damage, 0.001f, "Spray state should use Godot spray damage.");
                AssertEqual(false, projectile.Shatterable, "Spray state should not be shatterable yet.");

                projectile.Tick(1.59f, new Vector3(20f, 0f, 20f));
                AssertEqual("spray", projectile.CurrentPerfectFreezeState, "Projectile should stay in spray before the threshold.");
                AssertApproximately(6.678f, projectileObject.transform.position.z, 0.001f, "Spray state should move at spray speed.");

                projectile.Tick(0.01f, new Vector3(20f, 0f, 20f));
                AssertEqual("frozen", projectile.CurrentPerfectFreezeState, "Projectile should enter frozen state at the spray threshold.");
                AssertEqual(true, projectile.IsFrozen, "Frozen state should expose frozen contact rules.");
                AssertApproximately(0f, projectile.Speed, 0.001f, "Frozen projectiles should stop moving.");
                AssertApproximately(7f, projectile.Damage, 0.001f, "Frozen state should use Godot frozen contact damage.");
                AssertEqual(true, projectile.Shatterable, "Frozen state should become shatterable.");
                AssertApproximately(20f, projectile.ShatterHp, 0.001f, "Frozen state should reset shatter HP.");
                AssertEqual("frozen_crystal", projectile.ProjectileFamily, "Frozen state should expose the settlement family.");
                AssertEqual(true, projectile.IsWeakTo("heavy"), "Frozen state should add heavy as a shatter weakness.");
                AssertEqual(true, projectile.IsWeakTo("fire"), "Frozen state should add fire as a shatter weakness.");
                Vector3 frozenPosition = projectileObject.transform.position;

                projectile.Tick(2.39f, new Vector3(20f, 0f, 20f));
                AssertEqual("frozen", projectile.CurrentPerfectFreezeState, "Projectile should remain frozen before freeze duration elapses.");
                AssertApproximately(frozenPosition.z, projectileObject.transform.position.z, 0.001f, "Frozen projectile should not drift.");

                projectile.Tick(0.01f, new Vector3(20f, 0f, 20f));
                AssertEqual("thawed", projectile.CurrentPerfectFreezeState, "Projectile should thaw after the frozen duration.");
                AssertEqual(false, projectile.IsFrozen, "Thawed state should clear the frozen flag.");
                AssertApproximately(8f, projectile.Speed, 0.001f, "Thawed projectile should use Godot thaw speed.");
                AssertApproximately(10f, projectile.Damage, 0.001f, "Thawed projectile should use Godot thaw damage.");
                AssertEqual(false, projectile.Shatterable, "Thawed projectile should no longer be shatterable.");
                AssertEqual(false, projectile.TryApplyShatterDamage(90f, "heavy", projectileObject.transform.position), "Thawed projectile should not shatter.");

                projectile.Tick(0.1f, new Vector3(20f, 0f, 20f));
                AssertApproximately(frozenPosition.z + 0.88f, projectileObject.transform.position.z, 0.001f, "Thawed projectile should resume movement immediately on the thaw frame.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
            }
        }

        private static void TestPerfectFreezeProjectileArmDelayDefersDangerAndCycle()
        {
            GameObject projectileObject = new GameObject("EnemyProjectilePerfectFreezeCycleSmoke_ArmDelay");
            try
            {
                MigrationPlayerHealthRuntime health = new MigrationPlayerHealthRuntime();
                health.SetHealth(100f, 100f);
                MigrationCombatRuntime combat = new MigrationCombatRuntime(null, health);

                MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                projectile.BindCombat(combat);
                projectile.Configure(4.2f, 8f, Vector3.forward, true, 0.45f);
                projectile.ConfigurePerfectFreezeCycle(
                    true,
                    1.6f,
                    2.4f,
                    4.2f,
                    8f,
                    7f,
                    8f,
                    10f,
                    20f);
                projectile.ConfigureArmDelay(0.5f);
                projectile.ConfigureGraze(true, 1.15f, 0.7f);

                AssertEqual(false, projectile.IsArmed, "Perfect Freeze projectile should expose an unarmed readability window.");
                AssertApproximately(0.5f, projectile.ArmDelayRemainingSeconds, 0.001f, "Arm delay should start at the configured Godot telegraph window.");

                projectile.Tick(0.49f, Vector3.zero);

                AssertEqual(false, projectile.IsArmed, "Projectile should stay unarmed before the delay elapses.");
                AssertApproximately(0f, projectileObject.transform.position.z, 0.001f, "Unarmed projectiles should not move into the player before the cue resolves.");
                AssertEqual(0, projectile.HitEventCount, "Unarmed projectiles should not damage the player.");
                AssertApproximately(100f, health.CurrentHp, 0.001f, "Player health should remain unchanged while the projectile is unarmed.");
                AssertEqual(0, projectile.GrazeEventCount, "Unarmed projectiles should not grant graze before they become real danger.");
                AssertEqual("spray", projectile.CurrentPerfectFreezeState, "Arm delay should pause Perfect Freeze cycle timing.");
                AssertApproximately(0f, projectile.PerfectFreezePhaseElapsed, 0.001f, "Perfect Freeze cycle elapsed time should not advance while unarmed.");

                projectile.Tick(0.01f, Vector3.zero);

                AssertEqual(true, projectile.IsArmed, "Projectile should arm when the delay elapses.");
                AssertApproximately(0f, projectile.ArmDelayRemainingSeconds, 0.001f, "Arm delay should count down to zero.");
                AssertEqual(0, projectile.HitEventCount, "Arming should not retroactively damage the player on the same readability frame.");

                projectile.Tick(0.1f, Vector3.zero);

                AssertEqual(1, projectile.HitEventCount, "Armed projectile should damage once it starts moving through the player.");
                AssertApproximately(92f, health.CurrentHp, 0.001f, "Armed Perfect Freeze spray damage should route through player health.");
                AssertEqual("spray", projectile.CurrentPerfectFreezeState, "Projectile should still be in spray shortly after arming.");
                AssertEqual(true, projectile.PerfectFreezePhaseElapsed > 0f, "Perfect Freeze cycle should advance after arming.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
            }
        }

        private static void TestFrozenPerfectFreezeProjectileShattersIntoSettlementStaggerChain()
        {
            GameObject settlementObject = new GameObject("EnemyProjectilePerfectFreezeCycleSmoke_Settlement");
            GameObject[] projectileObjects = new GameObject[12];
            try
            {
                MigrationProjectileSpecialSettlement settlement = settlementObject.AddComponent<MigrationProjectileSpecialSettlement>();
                settlement.BindGauge(new MigrationPhoenixGaugeRuntime());
                int staggerEvents = 0;
                settlement.PerfectFreezeStaggerReady += _ => staggerEvents++;

                for (int index = 0; index < projectileObjects.Length; index++)
                {
                    GameObject projectileObject = new GameObject("EnemyProjectilePerfectFreezeCycleSmoke_Frozen_" + index);
                    projectileObjects[index] = projectileObject;
                    MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                    projectile.Configure(1f, 1f, Vector3.forward, true, 0.35f);
                    projectile.ConfigurePerfectFreezeCycle(true, 0.1f, 2.4f, 4.2f, 8f, 7f, 8f, 10f, 20f);
                    settlement.BindProjectile(projectile);
                    projectile.Tick(0.1f, new Vector3(20f, 0f, 20f));

                    AssertEqual("frozen", projectile.CurrentPerfectFreezeState, "Projectile should be frozen before shatter.");
                    bool shattered = projectile.TryApplyShatterDamage(20f, "heavy", projectileObject.transform.position, settlement);
                    AssertEqual(true, shattered, "Heavy attacks should shatter frozen Perfect Freeze crystals.");
                }

                AssertEqual(12, settlement.FrozenCrystalBreakCount, "Twelve frozen cycle projectiles should aggregate as frozen crystal breaks.");
                AssertEqual(1, settlement.PerfectFreezeStaggerEventCount, "Twelve frozen cycle shatters should expose one stagger opportunity.");
                AssertEqual(1, staggerEvents, "Settlement event should fire once after twelve frozen cycle shatters.");
            }
            finally
            {
                foreach (GameObject projectileObject in projectileObjects)
                {
                    if (projectileObject != null)
                    {
                        UnityEngine.Object.DestroyImmediate(projectileObject);
                    }
                }

                UnityEngine.Object.DestroyImmediate(settlementObject);
            }
        }

        private static void TestFeedbackTemplateAppliesPerfectFreezeCycleData()
        {
            GameObject projectileObject = new GameObject("EnemyProjectilePerfectFreezeCycleSmoke_Template");
            try
            {
                MigrationCombatFeedbackTemplate template = projectileObject.AddComponent<MigrationCombatFeedbackTemplate>();
                template.ConfigureTemplate(
                    "perfect_freeze_projectile",
                    true,
                    "EnemyProjectile",
                    6f,
                    0.22f,
                    new Color(0.55f, 0.9f, 1f, 1f),
                    true,
                    true,
                    true,
                    1.15f,
                    0.7f,
                    "frozen_crystal",
                    false,
                    20f,
                    "fire,heavy,shatter",
                    true,
                    1.6f,
                    2.4f,
                    4.2f,
                    8f,
                    7f,
                    8f,
                    10f,
                    20f,
                    armDelaySeconds: 0.5f);

                MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                projectile.ApplyFeedbackTemplate(template);

                AssertEqual(true, template.PerfectFreezeCycleEnabled, "Template should serialize the Perfect Freeze cycle flag.");
                AssertApproximately(0.5f, template.ArmDelaySeconds, 0.001f, "Template should serialize the Perfect Freeze arm-delay readability window.");
                AssertEqual(true, projectile.PerfectFreezeCycleEnabled, "Projectile should apply the Perfect Freeze cycle flag from the template.");
                AssertEqual(false, projectile.IsArmed, "Template-applied Perfect Freeze projectile should begin unarmed.");
                AssertApproximately(0.5f, projectile.ArmDelaySeconds, 0.001f, "Projectile should apply arm-delay from the template.");
                AssertEqual("spray", projectile.CurrentPerfectFreezeState, "Template-applied projectile should begin in spray state.");
                AssertApproximately(1.6f, projectile.PerfectFreezeSpraySeconds, 0.001f, "Projectile should preserve spray duration from template.");
                AssertApproximately(2.4f, projectile.PerfectFreezeFreezeSeconds, 0.001f, "Projectile should preserve freeze duration from template.");
                AssertApproximately(20f, projectile.PerfectFreezeFrozenShatterHp, 0.001f, "Projectile should preserve frozen shatter HP from template.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
            }
        }

        private static void TestGeneratedPerfectFreezeProjectileFeedbackPrefabCarriesCycleData()
        {
            InvokeBuilder("BuildCombatFeedbackPrefabs");

            GameObject projectilePrefab = RequiredPrefab($"{CombatFeedbackPrefabsRoot}/MigrationPerfectFreezeProjectileFeedback.prefab");
            MigrationCombatFeedbackTemplate template = projectilePrefab.GetComponent<MigrationCombatFeedbackTemplate>();
            MigrationEnemyProjectile projectile = projectilePrefab.GetComponent<MigrationEnemyProjectile>();

            AssertEqual(true, template != null, "Generated Perfect Freeze projectile prefab should carry a template.");
            AssertEqual(true, template.PerfectFreezeCycleEnabled, "Generated Perfect Freeze template should enable the cycle.");
            AssertEqual("frozen_crystal", template.ProjectileFamily, "Generated Perfect Freeze template should use frozen-crystal family.");
            AssertEqual(true, projectile != null, "Generated Perfect Freeze projectile prefab should carry projectile runtime logic.");
            AssertEqual(true, projectile.PerfectFreezeCycleEnabled, "Generated Perfect Freeze runtime should enable the cycle.");
            AssertApproximately(0.5f, template.ArmDelaySeconds, 0.001f, "Generated Perfect Freeze template should preserve Godot's arm delay.");
            AssertApproximately(0.5f, projectile.ArmDelaySeconds, 0.001f, "Generated Perfect Freeze runtime should apply Godot's arm delay.");
            AssertEqual(false, projectile.IsArmed, "Generated Perfect Freeze runtime should start unarmed until its readability window elapses.");
            AssertEqual("spray", projectile.CurrentPerfectFreezeState, "Generated Perfect Freeze runtime should start in spray state.");
            AssertApproximately(4.2f, projectile.Speed, 0.001f, "Generated Perfect Freeze runtime should serialize spray speed.");
            AssertEqual(true, projectile.GetComponent<MigrationProjectileShatterPresenter>() != null, "Generated Perfect Freeze projectile should keep shatter presentation.");
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
