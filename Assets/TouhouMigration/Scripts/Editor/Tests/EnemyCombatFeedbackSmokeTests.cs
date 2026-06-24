using System;
using System.Reflection;
using TouhouMigration.Runtime.Combat;
using TouhouMigration.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class EnemyCombatFeedbackSmokeTests
    {
        private const string EnemyPrefabsRoot = "Assets/TouhouMigration/Prefabs/Enemies";

        [MenuItem("Touhou Migration/Tests/Run Enemy Combat Feedback Smoke Tests")]
        public static void RunAll()
        {
            TestMeleeDamageSourceIsOnlyVisibleAndDangerousDuringActiveWindow();
            TestEnemyProjectileHasVisibleFeedbackAndLifetime();
            TestCombatTargetEmitsDamageFeedbackBeforeDefeat();
            TestDefeatHandlerCreatesDeathFeedbackDuringDelay();
            TestGeneratedMeleeEnemyDamageSourceUsesActiveWindow();
        }

        private static void TestMeleeDamageSourceIsOnlyVisibleAndDangerousDuringActiveWindow()
        {
            GameObject playerObject = CreatePlayerObject();
            GameObject enemyObject = new GameObject("EnemyCombatFeedbackSmoke_MeleeEnemy");
            GameObject damageObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            try
            {
                MigrationPlayerController player = playerObject.GetComponent<MigrationPlayerController>();
                MigrationPlayerHealthRuntime health = new MigrationPlayerHealthRuntime();
                health.SetHealth(100f, 100f);
                MigrationCombatRuntime combat = new MigrationCombatRuntime(player, health);

                MigrationCombatTargetBehaviour target = enemyObject.AddComponent<MigrationCombatTargetBehaviour>();
                target.Initialize(20f);

                damageObject.name = "EnemyCombatFeedbackSmoke_DamageSource";
                damageObject.transform.SetParent(enemyObject.transform);
                MigrationEnemyDamageSource damageSource = damageObject.AddComponent<MigrationEnemyDamageSource>();
                damageSource.BindCombat(combat);
                damageSource.Configure(9f);
                Invoke(damageSource, "ConfigureWindowing", true, false);

                MigrationSimpleEnemyController enemy = enemyObject.AddComponent<MigrationSimpleEnemyController>();
                enemy.BindTarget(target);
                enemy.BindDamageSource(damageSource);
                enemy.ConfigureMovement(5f, 1.5f, 2f);
                Invoke(enemy, "ConfigureActionTimings", 0.2f, 0.1f, 0.3f);

                AssertEqual(false, GetProperty<bool>(damageSource, "IsWindowActive"), "Generated-style melee damage source should start inactive.");
                AssertEqual(false, damageObject.GetComponent<Collider>().enabled, "Inactive melee damage source should not collide.");
                AssertEqual(false, damageObject.GetComponent<Renderer>().enabled, "Inactive melee damage source should not be visible.");
                damageSource.TryDamagePlayer();
                AssertApproximately(100f, health.CurrentHp, 0.001f, "Inactive melee damage source should not damage the player.");
                AssertEqual(1, GetProperty<int>(damageSource, "WindowBlockedCount"), "Inactive melee damage should be counted as blocked.");

                enemy.Tick(0.2f, new Vector3(1.2f, 0f, 0f));
                AssertEqual(true, GetProperty<bool>(damageSource, "IsWindowActive"), "Melee active phase should enable the danger source.");
                AssertEqual(true, damageObject.GetComponent<Collider>().enabled, "Melee active phase should enable collision.");
                AssertEqual(true, damageObject.GetComponent<Renderer>().enabled, "Melee active phase should make the danger source visible.");
                AssertEqual(1, damageSource.DamageEventCount, "Melee active phase should damage once.");
                AssertApproximately(91f, health.CurrentHp, 0.001f, "Melee active phase should apply configured damage.");

                enemy.Tick(0.1f, new Vector3(1.2f, 0f, 0f));
                AssertEqual(false, GetProperty<bool>(damageSource, "IsWindowActive"), "Melee source should turn off after active phase.");
                AssertEqual(false, damageObject.GetComponent<Collider>().enabled, "Melee source collider should turn off after active phase.");
                AssertEqual(false, damageObject.GetComponent<Renderer>().enabled, "Melee source renderer should turn off after active phase.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(damageObject);
                UnityEngine.Object.DestroyImmediate(enemyObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        private static void TestEnemyProjectileHasVisibleFeedbackAndLifetime()
        {
            GameObject projectileObject = new GameObject("EnemyCombatFeedbackSmoke_Projectile");
            try
            {
                MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                projectile.Configure(8f, 12f, Vector3.forward, true);
                Invoke(projectile, "ConfigureFeedback", 1.25f, 0.18f, Color.red);

                AssertEqual(true, GetProperty<bool>(projectile, "HasVisualFeedback"), "Enemy projectile should create visible feedback.");
                AssertEqual(true, projectileObject.GetComponentInChildren<Renderer>() != null, "Enemy projectile should have a renderer.");
                AssertEqual(true, projectileObject.GetComponentInChildren<TrailRenderer>() != null, "Enemy projectile should have a trail renderer.");
                AssertApproximately(1.25f, GetProperty<float>(projectile, "LifetimeSeconds"), 0.001f, "Projectile lifetime should be configurable.");

                projectile.Tick(0.6f, new Vector3(100f, 0f, 0f));
                AssertEqual(false, GetProperty<bool>(projectile, "IsExpired"), "Projectile should remain alive before lifetime expires.");

                projectile.Tick(0.65f, new Vector3(100f, 0f, 0f));
                AssertEqual(true, GetProperty<bool>(projectile, "IsExpired"), "Projectile should expire after lifetime.");
                AssertEqual(1, GetProperty<int>(projectile, "ExpiredEventCount"), "Projectile should count expiry once.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
            }
        }

        private static void TestCombatTargetEmitsDamageFeedbackBeforeDefeat()
        {
            GameObject targetObject = new GameObject("EnemyCombatFeedbackSmoke_Target");
            try
            {
                MigrationCombatTargetBehaviour target = targetObject.AddComponent<MigrationCombatTargetBehaviour>();
                target.Initialize(20f);

                int damagedCount = 0;
                float lastDamage = 0f;
                target.Damaged += result =>
                {
                    damagedCount++;
                    lastDamage = result.DamageApplied;
                };

                target.ApplyDamage(5f);
                AssertEqual(1, damagedCount, "Target should emit damage feedback for non-lethal hits.");
                AssertApproximately(5f, lastDamage, 0.001f, "Target damage feedback should report applied damage.");
                AssertEqual(1, target.DamageEventCount, "Target should count damage feedback events.");

                target.ApplyDamage(30f);
                AssertEqual(2, damagedCount, "Target should emit damage feedback for lethal hits too.");
                AssertEqual(1, target.DefeatEventCount, "Target should still emit defeat once.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        private static void TestDefeatHandlerCreatesDeathFeedbackDuringDelay()
        {
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                MigrationCombatTargetBehaviour target = targetObject.AddComponent<MigrationCombatTargetBehaviour>();
                target.Initialize(10f);
                MigrationCombatDefeatHandler defeatHandler = targetObject.AddComponent<MigrationCombatDefeatHandler>();
                defeatHandler.BindTarget(target);
                defeatHandler.ConfigureDefeatDelay(0.3f);
                Invoke(defeatHandler, "ConfigureDeathFeedback", 0.45f, new Color(1f, 0.35f, 0.12f, 1f));

                Renderer renderer = targetObject.GetComponent<Renderer>();
                target.ApplyDamage(12f);

                AssertEqual(true, GetProperty<bool>(defeatHandler, "HasActiveDeathFeedback"), "Defeat handler should start death feedback during the delay window.");
                AssertEqual(1, GetProperty<int>(defeatHandler, "DeathFeedbackStartedCount"), "Death feedback should start once.");
                AssertEqual(true, targetObject.GetComponentInChildren<ParticleSystem>(true) != null, "Death feedback should use a Unity ParticleSystem.");
                AssertEqual(true, renderer.enabled, "Renderer should remain visible while death feedback plays.");

                defeatHandler.Tick(0.15f);
                AssertEqual(true, GetProperty<bool>(defeatHandler, "HasActiveDeathFeedback"), "Death feedback should keep playing before cleanup.");
                AssertEqual(true, GetProperty<float>(defeatHandler, "DeathFeedbackProgress") > 0f, "Death feedback should expose delay progress.");

                defeatHandler.Tick(0.15f);
                AssertEqual(false, GetProperty<bool>(defeatHandler, "HasActiveDeathFeedback"), "Death feedback should stop when delayed cleanup completes.");
                AssertEqual(false, renderer.enabled, "Renderer should hide after death feedback cleanup.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        private static void TestGeneratedMeleeEnemyDamageSourceUsesActiveWindow()
        {
            GameObject bat = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemyPrefabsRoot}/MigrationEnemy_Bat.prefab");
            AssertEqual(true, bat != null, "Bat prefab should exist for feedback verification.");
            MigrationEnemyDamageSource damageSource = bat.GetComponentInChildren<MigrationEnemyDamageSource>(true);
            AssertEqual(true, damageSource != null, "Generated melee-capable enemy should keep a damage source marker.");
            AssertEqual(true, GetProperty<bool>(damageSource, "RequiresActiveWindow"), "Generated damage source should require an active action window.");
            AssertEqual(false, GetProperty<bool>(damageSource, "IsWindowActive"), "Generated damage source should serialize inactive outside attack windows.");

            MigrationCombatDefeatHandler defeatHandler = bat.GetComponent<MigrationCombatDefeatHandler>();
            AssertEqual(true, defeatHandler != null, "Generated melee-capable enemy should keep defeat handling.");
            AssertEqual(true, GetProperty<bool>(defeatHandler, "DeathFeedbackEnabled"), "Generated enemies should serialize death feedback.");
        }

        private static GameObject CreatePlayerObject()
        {
            GameObject player = new GameObject("EnemyCombatFeedbackSmoke_Player");
            player.tag = "Player";
            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 1f, 0f);
            player.AddComponent<MigrationPlayerController>();
            return player;
        }

        private static void Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = null;
            foreach (MethodInfo candidate in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (candidate.Name == methodName && candidate.GetParameters().Length == args.Length)
                {
                    method = candidate;
                    break;
                }
            }

            if (method == null)
            {
                throw new Exception($"Missing method {target.GetType().FullName}.{methodName}");
            }

            method.Invoke(target, args);
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                throw new Exception($"Missing property {target.GetType().FullName}.{propertyName}");
            }

            return (T)property.GetValue(target);
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
