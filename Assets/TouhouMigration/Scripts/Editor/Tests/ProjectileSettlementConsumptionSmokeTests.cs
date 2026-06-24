using System;
using TouhouMigration.Editor;
using TouhouMigration.Runtime.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class ProjectileSettlementConsumptionSmokeTests
    {
        private const string HumanVillageScenePath = "Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity";

        [MenuItem("Touhou Migration/Tests/Run Projectile Settlement Consumption Smoke Tests")]
        public static void RunAll()
        {
            TestHeavyAttackConsumesPendingCrystalBurstRadiusOnce();
            TestProjectileInstancesForwardHeavyBurstStateToSharedSettlement();
            TestHumanVillagePlayerActionBindsSharedProjectileSettlement();
            Debug.Log("Projectile settlement consumption smoke tests passed.");
        }

        private static void TestHeavyAttackConsumesPendingCrystalBurstRadiusOnce()
        {
            GameObject hitboxObject = new GameObject("ProjectileSettlementConsumption_Hitbox");
            GameObject actionObject = new GameObject("ProjectileSettlementConsumption_Action");
            GameObject settlementObject = new GameObject("ProjectileSettlementConsumption_Settlement");
            try
            {
                BoxCollider hitboxCollider = hitboxObject.AddComponent<BoxCollider>();
                hitboxCollider.isTrigger = true;
                hitboxCollider.size = new Vector3(2f, 1f, 1f);

                MigrationPlayerAttackHitbox hitbox = hitboxObject.AddComponent<MigrationPlayerAttackHitbox>();
                MigrationPlayerCombatActionController action = actionObject.AddComponent<MigrationPlayerCombatActionController>();
                MigrationProjectileSpecialSettlement settlement = settlementObject.AddComponent<MigrationProjectileSpecialSettlement>();
                settlement.BindGauge(new MigrationPhoenixGaugeRuntime());

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

                action.BindAttackHitbox(hitbox);
                action.BindProjectileSettlement(settlement);
                action.ConfigureDamage(7f, 15f);

                action.TriggerLightAttack();
                AssertApproximately(1f, hitbox.CurrentRangeMultiplier, 0.001f, "Light attacks should not consume the pending heavy burst.");
                AssertApproximately(1.25f, settlement.PendingHeavyBurstRadiusMultiplier, 0.001f, "Light attacks should leave the heavy burst pending.");
                AssertApproximately(2f, hitboxCollider.size.x, 0.001f, "Light attacks should keep the base hitbox width.");
                action.CompleteAttackWindow();

                action.TriggerHeavyAttack();
                AssertApproximately(1.25f, action.LastHeavyBurstRadiusMultiplier, 0.001f, "Heavy attack should consume the pending crystal burst multiplier.");
                AssertApproximately(1.25f, hitbox.CurrentRangeMultiplier, 0.001f, "Heavy hitbox should expose the consumed range multiplier.");
                AssertApproximately(1f, settlement.PendingHeavyBurstRadiusMultiplier, 0.001f, "Consumed heavy burst should reset to the neutral multiplier.");
                AssertEqual(1, settlement.HeavyBurstConsumeCount, "Heavy burst should be consumed exactly once.");
                AssertApproximately(2.5f, hitboxCollider.size.x, 0.001f, "Heavy burst should widen the hitbox for the active window.");
                AssertApproximately(1.25f, hitboxCollider.size.y, 0.001f, "Heavy burst should scale the hitbox uniformly like Godot's effect scale.");

                action.CompleteAttackWindow();
                AssertApproximately(1f, hitbox.CurrentRangeMultiplier, 0.001f, "Completing the attack should restore neutral range.");
                AssertApproximately(2f, hitboxCollider.size.x, 0.001f, "Completing the attack should restore the base hitbox width.");
                AssertApproximately(1f, hitboxCollider.size.y, 0.001f, "Completing the attack should restore the base hitbox height.");

                action.TriggerHeavyAttack();
                AssertApproximately(1f, action.LastHeavyBurstRadiusMultiplier, 0.001f, "A second heavy attack should not reuse the consumed burst.");
                AssertEqual(1, settlement.HeavyBurstConsumeCount, "Neutral heavy attacks should not count as burst consumption.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settlementObject);
                UnityEngine.Object.DestroyImmediate(actionObject);
                UnityEngine.Object.DestroyImmediate(hitboxObject);
            }
        }

        private static void TestProjectileInstancesForwardHeavyBurstStateToSharedSettlement()
        {
            GameObject sharedObject = new GameObject("ProjectileSettlementConsumption_Shared");
            GameObject[] projectileObjects = new GameObject[3];
            try
            {
                MigrationPhoenixGaugeRuntime gauge = new MigrationPhoenixGaugeRuntime();
                gauge.Reset(0f);
                MigrationProjectileSpecialSettlement sharedSettlement = sharedObject.AddComponent<MigrationProjectileSpecialSettlement>();
                sharedSettlement.BindGauge(gauge);

                for (int index = 0; index < projectileObjects.Length; index++)
                {
                    GameObject projectileObject = new GameObject("ProjectileSettlementConsumption_Projectile_" + index);
                    projectileObjects[index] = projectileObject;

                    MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                    projectile.Configure(20f, 7f, Vector3.forward, true, 0.35f);
                    projectile.ConfigureShatterRules("ice_crystal", true, 25f, "fire,heavy,shatter");

                    MigrationProjectileSpecialSettlement localSettlement = projectileObject.AddComponent<MigrationProjectileSpecialSettlement>();
                    localSettlement.BindProjectile(projectile);
                    localSettlement.BindSharedSettlement(sharedSettlement);

                    bool shattered = projectile.TryApplyShatterDamage(20f, "heavy", Vector3.zero, localSettlement);
                    AssertEqual(true, shattered, "Each local projectile should still own shatter detection.");
                    AssertEqual(0, localSettlement.IceCrystalBreakCount, "Local projectile settlement should forward encounter counters to the shared settlement.");
                }

                AssertApproximately(36f, gauge.CurrentValue, 0.001f, "Shared settlement should receive gauge rewards from all projectile instances.");
                AssertEqual(3, sharedSettlement.IceCrystalBreakCount, "Shared settlement should aggregate ice crystal breaks across projectile instances.");
                AssertApproximately(1.25f, sharedSettlement.PendingHeavyBurstRadiusMultiplier, 0.001f, "Shared settlement should expose one heavy burst after three forwarded crystal breaks.");
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

                UnityEngine.Object.DestroyImmediate(sharedObject);
            }
        }

        private static void TestHumanVillagePlayerActionBindsSharedProjectileSettlement()
        {
            TouhouMigrationProjectBuilder.BuildInitialProject();
            EditorSceneManager.OpenScene(HumanVillageScenePath);

            MigrationPlayerCombatActionController action = FirstComponent<MigrationPlayerCombatActionController>();
            MigrationProjectileSpecialSettlement settlement = FirstComponent<MigrationProjectileSpecialSettlement>();

            AssertEqual(true, action.HasProjectileSettlement, "Human Village player action should bind the shared projectile settlement.");
            AssertEqual(true, settlement != null, "Human Village should contain one shared projectile settlement consumer.");
        }

        private static T FirstComponent<T>() where T : UnityEngine.Object
        {
            foreach (GameObject gameObject in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                T component = gameObject.GetComponent<T>();
                if (component != null)
                {
                    return component;
                }
            }

            throw new Exception($"Missing component {typeof(T).FullName}.");
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
