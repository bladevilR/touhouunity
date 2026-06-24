using System;
using System.Reflection;
using TouhouMigration.Runtime.Combat;
using TouhouMigration.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class EnemyActionTimingSmokeTests
    {
        private const string EnemyPrefabsRoot = "Assets/TouhouMigration/Prefabs/Enemies";

        [MenuItem("Touhou Migration/Tests/Run Enemy Action Timing Smoke Tests")]
        public static void RunAll()
        {
            TestMeleeActionUsesTelegraphActiveAndRecoveryWindows();
            TestRangedActionUsesActiveProjectileWindowAndRecovery();
            TestRangedActionCanCheckoutProjectilesFromPrefabPool();
            TestDefeatHandlerCanDelayRendererAndColliderDisable();
            TestGeneratedEnemyPrefabsCarryDeathDelay();
        }

        private static void TestMeleeActionUsesTelegraphActiveAndRecoveryWindows()
        {
            GameObject playerObject = CreatePlayerObject();
            GameObject enemyObject = new GameObject("EnemyActionTimingSmoke_MeleeEnemy");
            GameObject damageObject = new GameObject("EnemyActionTimingSmoke_MeleeDamage");
            try
            {
                MigrationPlayerController player = playerObject.GetComponent<MigrationPlayerController>();
                MigrationPlayerHealthRuntime health = new MigrationPlayerHealthRuntime();
                health.SetHealth(100f, 100f);
                MigrationCombatRuntime combat = new MigrationCombatRuntime(player, health);

                MigrationCombatTargetBehaviour target = enemyObject.AddComponent<MigrationCombatTargetBehaviour>();
                target.Initialize(20f);

                damageObject.transform.SetParent(enemyObject.transform);
                MigrationEnemyDamageSource damageSource = damageObject.AddComponent<MigrationEnemyDamageSource>();
                damageSource.BindCombat(combat);
                damageSource.Configure(11f);

                MigrationSimpleEnemyController enemy = enemyObject.AddComponent<MigrationSimpleEnemyController>();
                enemy.BindTarget(target);
                enemy.BindDamageSource(damageSource);
                enemy.ConfigureMovement(5f, 1.5f, 2f);
                enemy.ConfigureAttackCooldown(0.2f);
                Invoke(enemy, "ConfigureActionTimings", 0.2f, 0.1f, 0.3f);

                enemy.Tick(0.1f, new Vector3(1.2f, 0f, 0f));
                AssertEqual("telegraph", GetProperty<string>(enemy, "CurrentActionPhase"), "Melee enemy should telegraph before the active hit window.");
                AssertEqual("windup", enemy.CurrentState, "Melee telegraph should keep the controller in windup state.");
                AssertEqual(0, enemy.AttackEventCount, "Melee telegraph should not apply damage.");
                AssertApproximately(100f, health.CurrentHp, 0.001f, "Player health should not change during telegraph.");

                enemy.Tick(0.1f, new Vector3(1.2f, 0f, 0f));
                AssertEqual("active", GetProperty<string>(enemy, "CurrentActionPhase"), "Melee enemy should enter an active window after telegraph.");
                AssertEqual("attack", enemy.CurrentState, "Melee active window should enter attack state.");
                AssertEqual(1, enemy.AttackEventCount, "Melee active window should apply damage once.");
                AssertApproximately(89f, health.CurrentHp, 0.001f, "Melee active window should damage the player once.");

                enemy.Tick(0.05f, new Vector3(1.2f, 0f, 0f));
                AssertEqual(1, enemy.AttackEventCount, "Melee active window should not multi-hit without a new action.");
                AssertApproximately(89f, health.CurrentHp, 0.001f, "Melee active window should not apply repeated damage.");

                enemy.Tick(0.1f, new Vector3(1.2f, 0f, 0f));
                AssertEqual("recovery", GetProperty<string>(enemy, "CurrentActionPhase"), "Melee enemy should enter recovery after active window.");
                AssertEqual("recovery", enemy.CurrentState, "Recovery should be visible as a controller state.");

                enemy.Tick(0.3f, new Vector3(1.2f, 0f, 0f));
                AssertEqual("idle", GetProperty<string>(enemy, "CurrentActionPhase"), "Melee action should finish after recovery.");
                AssertEqual(1, GetProperty<int>(enemy, "ActionTelegraphEventCount"), "Melee action should count one telegraph.");
                AssertEqual(1, GetProperty<int>(enemy, "ActionActiveEventCount"), "Melee action should count one active window.");
                AssertEqual(1, GetProperty<int>(enemy, "ActionRecoveryEventCount"), "Melee action should count one recovery.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(damageObject);
                UnityEngine.Object.DestroyImmediate(enemyObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        private static void TestRangedActionUsesActiveProjectileWindowAndRecovery()
        {
            GameObject playerObject = CreatePlayerObject();
            GameObject enemyObject = new GameObject("EnemyActionTimingSmoke_RangedEnemy");
            try
            {
                MigrationPlayerController player = playerObject.GetComponent<MigrationPlayerController>();
                MigrationPlayerHealthRuntime health = new MigrationPlayerHealthRuntime();
                health.SetHealth(100f, 100f);
                MigrationCombatRuntime combat = new MigrationCombatRuntime(player, health);

                MigrationCombatTargetBehaviour target = enemyObject.AddComponent<MigrationCombatTargetBehaviour>();
                target.Initialize(45f);

                MigrationEnemyCatalog catalog = new MigrationEnemyCatalog();
                catalog.LoadGodotDefaults();
                MigrationSimpleEnemyController enemy = enemyObject.AddComponent<MigrationSimpleEnemyController>();
                enemy.BindTarget(target);
                enemy.BindCombat(combat);
                enemy.ApplyVariant(catalog.GetProfile("bat"));
                Invoke(enemy, "ConfigureActionTimings", 0.2f, 0.1f, 0.3f);

                enemy.Tick(0.1f, new Vector3(7f, 0f, 0f));
                AssertEqual("telegraph", GetProperty<string>(enemy, "CurrentActionPhase"), "Ranged enemy should telegraph before firing.");
                AssertEqual(0, enemy.ProjectileEventCount, "Ranged telegraph should not spawn a projectile.");

                enemy.Tick(0.1f, new Vector3(7f, 0f, 0f));
                AssertEqual("active", GetProperty<string>(enemy, "CurrentActionPhase"), "Ranged enemy should enter an active projectile window.");
                AssertEqual("ranged_attack", enemy.CurrentState, "Ranged active window should use ranged attack state.");
                AssertEqual(1, enemy.ProjectileEventCount, "Ranged active window should spawn one projectile.");

                enemy.Tick(0.05f, new Vector3(7f, 0f, 0f));
                AssertEqual(1, enemy.ProjectileEventCount, "Ranged active window should not spawn repeated projectiles.");

                DestroyProjectiles();
                enemy.Tick(0.1f, new Vector3(7f, 0f, 0f));
                AssertEqual("recovery", GetProperty<string>(enemy, "CurrentActionPhase"), "Ranged enemy should recover after firing.");
                enemy.Tick(0.3f, new Vector3(7f, 0f, 0f));
                AssertEqual("idle", GetProperty<string>(enemy, "CurrentActionPhase"), "Ranged action should finish after recovery.");
            }
            finally
            {
                DestroyProjectiles();
                UnityEngine.Object.DestroyImmediate(enemyObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        private static void TestRangedActionCanCheckoutProjectilesFromPrefabPool()
        {
            GameObject playerObject = CreatePlayerObject();
            GameObject enemyObject = new GameObject("EnemyActionTimingSmoke_PooledRangedEnemy");
            GameObject projectilePrefab = CreateProjectilePrefab();
            GameObject poolObject = new GameObject("EnemyActionTimingSmoke_RangedProjectilePool");
            try
            {
                MigrationPlayerController player = playerObject.GetComponent<MigrationPlayerController>();
                MigrationPlayerHealthRuntime health = new MigrationPlayerHealthRuntime();
                health.SetHealth(100f, 100f);
                MigrationCombatRuntime combat = new MigrationCombatRuntime(player, health);

                MigrationCombatTargetBehaviour target = enemyObject.AddComponent<MigrationCombatTargetBehaviour>();
                target.Initialize(45f);

                MigrationEnemyCatalog catalog = new MigrationEnemyCatalog();
                catalog.LoadGodotDefaults();
                MigrationSimpleEnemyController enemy = enemyObject.AddComponent<MigrationSimpleEnemyController>();
                enemy.BindTarget(target);
                enemy.BindCombat(combat);
                enemy.ApplyVariant(catalog.GetProfile("bat"));
                enemy.ConfigureProjectilePrefab(projectilePrefab.GetComponent<MigrationEnemyProjectile>());
                enemy.BindProjectilePool(poolObject.AddComponent<MigrationPrefabPoolService>());
                enemy.ConfigureAttackCooldown(0f);
                enemy.ConfigureActionTimings(0f, 0f, 0f);

                enemy.Tick(0.01f, new Vector3(7f, 0f, 0f));
                AssertEqual(1, enemy.ProjectileEventCount, "Pooled ranged enemy should still fire one projectile.");
                AssertEqual(true, enemy.HasProjectilePool, "Ranged enemy should expose its bound projectile pool.");
                AssertEqual(1, enemy.ProjectilePool.TotalCreatedCount, "First pooled ranged shot should create one projectile instance.");
                MigrationEnemyProjectile firstProjectile = enemy.LastSpawnedProjectile;
                AssertEqual(true, firstProjectile != null, "Ranged enemy should expose its last spawned projectile.");
                AssertApproximately(8f, firstProjectile.Speed, 0.001f, "Pooled ranged projectile should preserve the variant projectile speed.");
                AssertApproximately(12f, firstProjectile.Damage, 0.001f, "Pooled ranged projectile should inherit the variant attack damage.");

                AssertEqual(true, enemy.ProjectilePool.Release(firstProjectile.gameObject), "Test should be able to return the pooled ranged projectile.");
                enemy.Tick(0.01f, new Vector3(7f, 0f, 0f));
                AssertEqual(2, enemy.ProjectileEventCount, "Pooled ranged enemy should fire again after cooldown.");
                AssertEqual(firstProjectile, enemy.LastSpawnedProjectile, "Second pooled ranged shot should reuse the same prefab-keyed projectile instance.");
                AssertEqual(1, enemy.ProjectilePool.TotalCreatedCount, "Second pooled ranged shot should not instantiate another projectile.");
                AssertEqual(1, enemy.ProjectilePool.TotalReusedCount, "Second pooled ranged shot should count a pool reuse.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(poolObject);
                UnityEngine.Object.DestroyImmediate(projectilePrefab);
                UnityEngine.Object.DestroyImmediate(enemyObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        private static void TestDefeatHandlerCanDelayRendererAndColliderDisable()
        {
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                MigrationCombatTargetBehaviour target = targetObject.AddComponent<MigrationCombatTargetBehaviour>();
                target.Initialize(5f);
                MigrationCombatDefeatHandler defeatHandler = targetObject.AddComponent<MigrationCombatDefeatHandler>();
                defeatHandler.BindTarget(target);
                Invoke(defeatHandler, "ConfigureDefeatDelay", 0.3f);

                Collider collider = targetObject.GetComponent<Collider>();
                Renderer renderer = targetObject.GetComponent<Renderer>();

                target.ApplyDamage(6f);
                AssertEqual(true, GetProperty<bool>(defeatHandler, "IsDefeatPending"), "Defeat handler should wait during configured death delay.");
                AssertEqual(0, defeatHandler.HandledDefeatCount, "Defeat handler should not disable immediately when delay is configured.");
                AssertEqual(true, collider.enabled, "Collider should remain enabled during death delay.");
                AssertEqual(true, renderer.enabled, "Renderer should remain enabled during death delay.");

                Invoke(defeatHandler, "Tick", 0.2f);
                AssertEqual(true, collider.enabled, "Collider should still be enabled before the delay elapses.");
                AssertEqual(true, renderer.enabled, "Renderer should still be enabled before the delay elapses.");

                Invoke(defeatHandler, "Tick", 0.1f);
                AssertEqual(false, GetProperty<bool>(defeatHandler, "IsDefeatPending"), "Defeat handler should finish pending defeat after delay.");
                AssertEqual(1, defeatHandler.HandledDefeatCount, "Defeat handler should disable once after delay.");
                AssertEqual(false, collider.enabled, "Collider should disable after death delay.");
                AssertEqual(false, renderer.enabled, "Renderer should disable after death delay.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        private static void TestGeneratedEnemyPrefabsCarryDeathDelay()
        {
            GameObject bat = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemyPrefabsRoot}/MigrationEnemy_Bat.prefab");
            AssertEqual(true, bat != null, "Bat prefab should exist for generated death-delay verification.");
            MigrationCombatDefeatHandler defeatHandler = bat.GetComponent<MigrationCombatDefeatHandler>();
            AssertEqual(true, defeatHandler != null, "Generated enemy prefabs should carry defeat handling.");
            AssertEqual(true, GetProperty<float>(defeatHandler, "DefeatDelaySeconds") > 0f, "Generated enemy prefabs should preserve a visible death delay.");
        }

        private static GameObject CreatePlayerObject()
        {
            GameObject player = new GameObject("EnemyActionTimingSmoke_Player");
            player.tag = "Player";
            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 1f, 0f);
            player.AddComponent<MigrationPlayerController>();
            return player;
        }

        private static GameObject CreateProjectilePrefab()
        {
            GameObject projectileObject = new GameObject("EnemyActionTimingSmoke_ProjectilePrefab");
            MigrationCombatFeedbackTemplate template = projectileObject.AddComponent<MigrationCombatFeedbackTemplate>();
            template.ConfigureTemplate(
                "enemy_projectile",
                true,
                "EnemyProjectile",
                2f,
                0.2f,
                new Color(1f, 0.15f, 0.1f, 1f),
                true,
                true,
                true,
                1.15f,
                0.7f,
                "enemy_projectile",
                false,
                0f,
                string.Empty,
                false);

            MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
            projectile.ApplyFeedbackTemplate(template);
            return projectileObject;
        }

        private static void DestroyProjectiles()
        {
            foreach (MigrationEnemyProjectile projectile in UnityEngine.Object.FindObjectsByType<MigrationEnemyProjectile>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                UnityEngine.Object.DestroyImmediate(projectile.gameObject);
            }
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
