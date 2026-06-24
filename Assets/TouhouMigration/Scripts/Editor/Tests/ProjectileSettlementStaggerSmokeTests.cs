using System;
using TouhouMigration.Editor;
using TouhouMigration.Runtime.Combat;
using TouhouMigration.Runtime.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class ProjectileSettlementStaggerSmokeTests
    {
        private const string HumanVillageScenePath = "Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity";

        [MenuItem("Touhou Migration/Tests/Run Projectile Settlement Stagger Smoke Tests")]
        public static void RunAll()
        {
            TestPerfectFreezeSettlementAppliesTimedEnemyStun();
            TestReflectSettlementAppliesTimedEnemyStun();
            TestHumanVillageWiresPerfectFreezeStaggerAdapter();
            Debug.Log("Projectile settlement stagger smoke tests passed.");
        }

        private static void TestPerfectFreezeSettlementAppliesTimedEnemyStun()
        {
            GameObject playerObject = CreatePlayerObject();
            GameObject enemyObject = new GameObject("ProjectileSettlementStagger_Enemy");
            GameObject damageObject = new GameObject("ProjectileSettlementStagger_DamageSource");
            GameObject settlementObject = new GameObject("ProjectileSettlementStagger_Settlement");
            try
            {
                MigrationPlayerController player = playerObject.GetComponent<MigrationPlayerController>();
                MigrationPlayerHealthRuntime health = new MigrationPlayerHealthRuntime();
                health.SetHealth(100f, 100f);
                MigrationCombatRuntime combat = new MigrationCombatRuntime(player, health);

                MigrationCombatTargetBehaviour target = enemyObject.AddComponent<MigrationCombatTargetBehaviour>();
                target.Initialize(25f);

                damageObject.transform.SetParent(enemyObject.transform);
                MigrationEnemyDamageSource damageSource = damageObject.AddComponent<MigrationEnemyDamageSource>();
                damageSource.BindCombat(combat);
                damageSource.Configure(9f);

                MigrationSimpleEnemyController enemy = enemyObject.AddComponent<MigrationSimpleEnemyController>();
                enemy.BindTarget(target);
                enemy.BindDamageSource(damageSource);
                enemy.ConfigureMovement(5f, 1.5f, 2f);
                enemy.ConfigureAttackCooldown(0f);

                MigrationProjectileSpecialSettlement settlement = settlementObject.AddComponent<MigrationProjectileSpecialSettlement>();
                settlement.BindGauge(new MigrationPhoenixGaugeRuntime());
                settlement.ConfigureRewards(2f, 5f, 8f, 12f, 12, 1.2f);

                MigrationPerfectFreezeStaggerAdapter adapter = enemyObject.AddComponent<MigrationPerfectFreezeStaggerAdapter>();
                adapter.BindSettlement(settlement);
                adapter.BindEnemyController(enemy);

                for (int index = 0; index < 12; index++)
                {
                    settlement.SettleShatter(new MigrationProjectileShatterResult(
                        null,
                        "frozen_crystal",
                        "fire",
                        20f,
                        1.5f,
                        30f,
                        0f,
                        Vector3.zero,
                        true,
                        null));
                }

                AssertEqual(1, settlement.PerfectFreezeStaggerEventCount, "Frozen crystal streak should emit one Perfect Freeze stagger event.");
                AssertEqual(1, adapter.StaggerEventCount, "Adapter should consume the settlement stagger event exactly once.");
                AssertApproximately(1.2f, adapter.LastStaggerSeconds, 0.001f, "Adapter should preserve the settlement stagger duration.");
                AssertEqual(true, enemy.IsStunned, "Enemy should enter a stunned state after Perfect Freeze.");
                AssertApproximately(1.2f, enemy.StunRemainingSeconds, 0.001f, "Enemy stun duration should come from the settlement event.");
                AssertEqual("stunned", enemy.CurrentState, "Enemy state should expose Perfect Freeze stun to animation bridges.");

                enemy.Tick(0.5f, new Vector3(1.2f, 0f, 0f));
                AssertEqual(true, enemy.IsStunned, "Enemy should stay stunned before the duration elapses.");
                AssertEqual(0, enemy.AttackEventCount, "Stunned enemy should not attack even when the player is in range.");
                AssertApproximately(100f, health.CurrentHp, 0.001f, "Stunned enemy should not damage the player.");

                enemy.Tick(0.8f, new Vector3(1.2f, 0f, 0f));
                AssertEqual(false, enemy.IsStunned, "Enemy should recover when the stun timer elapses.");
                AssertEqual("idle", enemy.CurrentState, "Enemy should return to idle after stun instead of attacking on the same frame.");
                AssertEqual(0, enemy.AttackEventCount, "Enemy should wait until the next AI tick before attacking after stun recovery.");

                enemy.Tick(0.1f, new Vector3(1.2f, 0f, 0f));
                AssertEqual("attack", enemy.CurrentState, "Recovered enemy should resume normal attack behavior on the next tick.");
                AssertEqual(1, enemy.AttackEventCount, "Recovered enemy should be allowed to attack again.");
                AssertApproximately(91f, health.CurrentHp, 0.001f, "Recovered enemy attack should still route through the combat runtime.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settlementObject);
                UnityEngine.Object.DestroyImmediate(damageObject);
                UnityEngine.Object.DestroyImmediate(enemyObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        private static void TestReflectSettlementAppliesTimedEnemyStun()
        {
            GameObject enemyObject = new GameObject("ProjectileSettlementStagger_ReflectEnemy");
            GameObject settlementObject = new GameObject("ProjectileSettlementStagger_ReflectSettlement");
            try
            {
                MigrationSimpleEnemyController enemy = enemyObject.AddComponent<MigrationSimpleEnemyController>();

                MigrationProjectileSpecialSettlement settlement = settlementObject.AddComponent<MigrationProjectileSpecialSettlement>();
                settlement.ConfigureSharedSettlementFallback(false);

                MigrationPerfectFreezeStaggerAdapter adapter = enemyObject.AddComponent<MigrationPerfectFreezeStaggerAdapter>();
                adapter.BindSettlement(settlement);
                adapter.BindEnemyController(enemy);

                float stunSeconds = settlement.SettleReflect(new MigrationProjectileReflectResult(
                    null,
                    "ice_lance",
                    "heavy",
                    Vector3.zero,
                    Vector3.back,
                    22.5f,
                    16f,
                    true,
                    2f,
                    null));

                AssertApproximately(2f, stunSeconds, 0.001f, "Reflect settlement should return the stun duration.");
                AssertEqual(1, settlement.ReflectStunEventCount, "Reflect settlement should expose one stun event.");
                AssertEqual(1, adapter.ReflectStunEventCount, "Adapter should consume one reflect stun event.");
                AssertApproximately(2f, adapter.LastReflectStunSeconds, 0.001f, "Adapter should preserve reflect stun duration.");
                AssertEqual(true, enemy.IsStunned, "Enemy should enter stun after reflect reward settlement.");
                AssertApproximately(2f, enemy.StunRemainingSeconds, 0.001f, "Enemy stun duration should come from the reflect result.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settlementObject);
                UnityEngine.Object.DestroyImmediate(enemyObject);
            }
        }

        private static void TestHumanVillageWiresPerfectFreezeStaggerAdapter()
        {
            TouhouMigrationProjectBuilder.BuildInitialProject();
            EditorSceneManager.OpenScene(HumanVillageScenePath);

            MigrationPerfectFreezeStaggerAdapter adapter = FirstComponent<MigrationPerfectFreezeStaggerAdapter>();
            AssertEqual(true, adapter.HasSettlement, "Human Village stagger adapter should bind the shared projectile settlement.");
            AssertEqual(true, adapter.HasEnemyController, "Human Village stagger adapter should target a generated enemy controller.");
        }

        private static GameObject CreatePlayerObject()
        {
            GameObject player = new GameObject("ProjectileSettlementStagger_Player");
            player.tag = "Player";
            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 1f, 0f);
            player.AddComponent<MigrationPlayerController>();
            return player;
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
