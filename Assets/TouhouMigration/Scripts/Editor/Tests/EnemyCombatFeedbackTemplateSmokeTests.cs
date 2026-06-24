using System;
using System.Reflection;
using TouhouMigration.Runtime.Combat;
using TouhouMigration.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class EnemyCombatFeedbackTemplateSmokeTests
    {
        private const string CombatFeedbackPrefabsRoot = "Assets/TouhouMigration/Prefabs/CombatFeedback";
        private const string EnemyPrefabsRoot = "Assets/TouhouMigration/Prefabs/Enemies";
        private const string BuilderTypeName = "TouhouMigration.Editor.TouhouMigrationProjectBuilder, Assembly-CSharp-Editor";

        [MenuItem("Touhou Migration/Tests/Run Enemy Combat Feedback Template Smoke Tests")]
        public static void RunAll()
        {
            TestFeedbackPrefabTemplatesExistAndArePoolingReady();
            TestProjectileTemplateEnablesSweepImpactAndPoolingSeam();
            TestPrefabPoolReusesProjectileInstancesAndResetsLifecycle();
            TestCombatTargetHurtFeedbackPulsesRendererAndKnockbackHook();
            TestGeneratedEnemyPrefabsCarryReusableFeedbackSeams();
        }

        private static void TestFeedbackPrefabTemplatesExistAndArePoolingReady()
        {
            InvokeBuilder("BuildCombatFeedbackPrefabs");

            GameObject projectile = RequiredPrefab($"{CombatFeedbackPrefabsRoot}/MigrationEnemyProjectileFeedback.prefab");
            MigrationCombatFeedbackTemplate projectileTemplate = projectile.GetComponent<MigrationCombatFeedbackTemplate>();
            AssertEqual(true, projectileTemplate != null, "Projectile feedback prefab should carry a feedback template marker.");
            AssertEqual("enemy_projectile", projectileTemplate.TemplateKind, "Projectile feedback prefab should identify its template kind.");
            AssertEqual(true, projectileTemplate.PoolingReady, "Projectile feedback prefab should be marked pooling-ready.");
            AssertEqual(true, projectileTemplate.ImpactFeedbackEnabled, "Projectile feedback prefab should enable impact feedback.");
            AssertEqual(true, projectileTemplate.SweepCollisionEnabled, "Projectile feedback prefab should enable anti-tunneling sweep checks.");
            AssertEqual(true, projectile.GetComponent<MigrationEnemyProjectile>() != null, "Projectile feedback prefab should carry projectile runtime logic.");
            AssertEqual(true, projectile.GetComponent<TrailRenderer>() != null, "Projectile feedback prefab should carry a trail renderer.");
            SphereCollider projectileCollider = projectile.GetComponent<SphereCollider>();
            AssertEqual(true, projectileCollider != null && projectileCollider.isTrigger, "Projectile feedback prefab should expose an explicit trigger collider policy.");

            GameObject danger = RequiredPrefab($"{CombatFeedbackPrefabsRoot}/MigrationMeleeDangerFeedback.prefab");
            MigrationCombatFeedbackTemplate dangerTemplate = danger.GetComponent<MigrationCombatFeedbackTemplate>();
            AssertEqual(true, dangerTemplate != null, "Melee danger prefab should carry a feedback template marker.");
            AssertEqual("melee_danger", dangerTemplate.TemplateKind, "Melee danger prefab should identify its template kind.");
            AssertEqual(true, danger.GetComponent<MigrationEnemyDamageSource>() != null, "Melee danger prefab should carry enemy damage-source logic.");

            GameObject death = RequiredPrefab($"{CombatFeedbackPrefabsRoot}/MigrationEnemyDeathFeedback.prefab");
            MigrationCombatFeedbackTemplate deathTemplate = death.GetComponent<MigrationCombatFeedbackTemplate>();
            AssertEqual(true, deathTemplate != null, "Death feedback prefab should carry a feedback template marker.");
            AssertEqual("death_feedback", deathTemplate.TemplateKind, "Death feedback prefab should identify its template kind.");
            AssertEqual(true, death.GetComponent<ParticleSystem>() != null, "Death feedback prefab should use Unity ParticleSystem presentation.");
        }

        private static void TestProjectileTemplateEnablesSweepImpactAndPoolingSeam()
        {
            GameObject playerObject = CreatePlayerObject();
            GameObject projectileObject = new GameObject("EnemyCombatFeedbackTemplateSmoke_Projectile");
            try
            {
                MigrationPlayerController player = playerObject.GetComponent<MigrationPlayerController>();
                MigrationPlayerHealthRuntime health = new MigrationPlayerHealthRuntime();
                health.SetHealth(100f, 100f);
                MigrationCombatRuntime combat = new MigrationCombatRuntime(player, health);

                MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                MigrationCombatFeedbackTemplate template = projectileObject.AddComponent<MigrationCombatFeedbackTemplate>();
                template.ConfigureTemplate("enemy_projectile", true, "EnemyProjectile", 2f, 0.2f, new Color(1f, 0.15f, 0.1f, 1f), true, true);

                projectile.BindCombat(combat);
                projectile.Configure(20f, 13f, Vector3.forward, true, 0.4f);
                projectile.ApplyFeedbackTemplate(template);

                projectile.Tick(0.3f, new Vector3(0f, 0f, 5f));

                AssertEqual(true, projectile.UsesFeedbackTemplate, "Projectile should remember that reusable feedback template data was applied.");
                AssertEqual(true, projectile.PoolingReady, "Projectile should expose pooling-ready ownership when configured by a template.");
                AssertEqual(true, projectile.SweepCollisionEnabled, "Projectile should use segment checks for fast projectiles.");
                AssertEqual(1, projectile.HitEventCount, "Sweep-enabled projectile should hit a player crossed between frames.");
                AssertApproximately(87f, health.CurrentHp, 0.001f, "Sweep-enabled projectile should still route damage through player health.");
                AssertEqual(1, projectile.ImpactEventCount, "Projectile should count impact feedback when it hits.");
                AssertEqual(true, projectile.HasActiveImpactFeedback, "Projectile should spawn visible impact feedback on hit.");
                AssertEqual(true, projectileObject.GetComponentInChildren<ParticleSystem>(true) != null, "Projectile impact should use a ParticleSystem hook.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        private static void TestPrefabPoolReusesProjectileInstancesAndResetsLifecycle()
        {
            GameObject poolObject = new GameObject("EnemyCombatFeedbackTemplateSmoke_Pool");
            GameObject projectilePrefab = CreateProjectilePrefab("EnemyCombatFeedbackTemplateSmoke_PooledProjectilePrefab", "enemy_projectile");
            GameObject iceProjectilePrefab = CreateProjectilePrefab("EnemyCombatFeedbackTemplateSmoke_IceProjectilePrefab", "ice_orb");
            try
            {
                MigrationPrefabPoolService pool = poolObject.AddComponent<MigrationPrefabPoolService>();

                GameObject firstObject = pool.Get(projectilePrefab, new Vector3(1f, 0f, 0f), Quaternion.identity);
                MigrationEnemyProjectile firstProjectile = firstObject.GetComponent<MigrationEnemyProjectile>();
                firstProjectile.Configure(20f, 13f, Vector3.forward, true, 0.4f);
                firstProjectile.ApplyFeedbackTemplate(firstObject.GetComponent<MigrationCombatFeedbackTemplate>());
                firstProjectile.Tick(0.3f, new Vector3(1f, 0f, 5f));

                AssertEqual(1, firstProjectile.HitEventCount, "Pooled projectile should still run normal projectile gameplay before release.");
                AssertEqual(1, firstProjectile.ImpactEventCount, "Pooled projectile should keep impact feedback before release.");
                AssertEqual(true, firstProjectile.HasActiveImpactFeedback, "Pooled projectile should expose active impact feedback before release.");
                AssertEqual(true, pool.Release(firstObject), "Pool should accept a tracked projectile instance for release.");
                AssertEqual(false, firstObject.activeSelf, "Released projectile should be inactive instead of destroyed.");
                AssertEqual(1, pool.TotalCreatedCount, "First get should create one projectile instance.");
                AssertEqual(1, pool.TotalReleasedCount, "Release should count one returned projectile instance.");
                AssertEqual(1, pool.InactiveInstanceCount, "Pool should hold one inactive projectile after release.");

                GameObject reusedObject = pool.Get(projectilePrefab, new Vector3(3f, 0f, 0f), Quaternion.identity);
                MigrationEnemyProjectile reusedProjectile = reusedObject.GetComponent<MigrationEnemyProjectile>();
                AssertEqual(firstObject, reusedObject, "Pool should reuse an inactive instance for the same prefab key.");
                AssertEqual(true, reusedObject.activeSelf, "Reused projectile should be active when checked out again.");
                AssertVectorApproximately(new Vector3(3f, 0f, 0f), reusedObject.transform.position, 0.001f, "Pool should apply the requested spawn position to reused instances.");

                reusedProjectile.Configure(7f, 4f, Vector3.right, true, 0.2f);
                reusedProjectile.ApplyFeedbackTemplate(reusedObject.GetComponent<MigrationCombatFeedbackTemplate>());
                ParticleSystem reusedImpactParticles = reusedObject.GetComponentInChildren<ParticleSystem>(true);
                AssertEqual(0, reusedProjectile.HitEventCount, "Configure should reset hit count on a reused projectile.");
                AssertEqual(0, reusedProjectile.ImpactEventCount, "Configure should reset impact count on a reused projectile.");
                AssertEqual(false, reusedProjectile.HasActiveImpactFeedback, "Configure should clear stale impact feedback state on a reused projectile.");
                AssertEqual(false, reusedImpactParticles != null && reusedImpactParticles.gameObject.activeSelf, "Configure should hide stale impact particle objects on a reused projectile.");
                AssertEqual(false, reusedImpactParticles != null && reusedImpactParticles.isPlaying, "Configure should stop stale impact particles on a reused projectile.");
                AssertEqual(false, reusedProjectile.IsExpired, "Configure should clear stale expired state on a reused projectile.");
                AssertEqual(false, reusedProjectile.IsShattered, "Configure should clear stale shatter state on a reused projectile.");
                AssertEqual(1, pool.TotalCreatedCount, "Same prefab reuse should not create another projectile.");
                AssertEqual(1, pool.TotalReusedCount, "Pool should count one same-key reuse.");

                GameObject otherObject = pool.Get(iceProjectilePrefab, Vector3.zero, Quaternion.identity);
                AssertEqual(false, ReferenceEquals(reusedObject, otherObject), "Different prefab keys should not reuse the wrong projectile instance.");
                AssertEqual(2, pool.TotalCreatedCount, "A distinct prefab key should create its own pool entry.");
                AssertEqual(2, pool.PrefabKeyCount, "Pool should track projectile instances by prefab key.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(iceProjectilePrefab);
                UnityEngine.Object.DestroyImmediate(projectilePrefab);
                UnityEngine.Object.DestroyImmediate(poolObject);
            }
        }

        private static void TestCombatTargetHurtFeedbackPulsesRendererAndKnockbackHook()
        {
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                Renderer renderer = targetObject.GetComponent<Renderer>();
                Material baseMaterial = new Material(Shader.Find("Standard"));
                baseMaterial.color = Color.white;
                renderer.sharedMaterial = baseMaterial;

                MigrationCombatTargetBehaviour target = targetObject.AddComponent<MigrationCombatTargetBehaviour>();
                target.Initialize(20f);
                MigrationCombatHurtFeedback hurtFeedback = targetObject.AddComponent<MigrationCombatHurtFeedback>();
                hurtFeedback.ConfigureFeedback(0.2f, Color.red, 0.35f);
                hurtFeedback.SetLastHitSource(new Vector3(-2f, 0f, 0f));

                target.ApplyDamage(4f);

                AssertEqual(true, hurtFeedback.IsFlashActive, "Hurt feedback should flash immediately when target is damaged.");
                AssertEqual(1, hurtFeedback.FlashEventCount, "Hurt feedback should count flash events.");
                AssertEqual(1, hurtFeedback.KnockbackEventCount, "Hurt feedback should count knockback hooks.");
                AssertEqual(true, targetObject.transform.position.x > 0.2f, "Hurt feedback should move the target away from the last hit source.");
                AssertEqual(true, hurtFeedback.LastKnockbackDirection.x > 0.9f, "Hurt feedback should expose the last knockback direction.");
                AssertEqual(Color.red, renderer.sharedMaterial.color, "Hurt feedback should apply the configured flash color.");

                hurtFeedback.Tick(0.2f);
                AssertEqual(false, hurtFeedback.IsFlashActive, "Hurt feedback should end after the configured duration.");
                AssertEqual(Color.white, renderer.sharedMaterial.color, "Hurt feedback should restore the original material color.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        private static void TestGeneratedEnemyPrefabsCarryReusableFeedbackSeams()
        {
            InvokeBuilder("BuildEnemyCatalogPrefabs");

            GameObject bat = RequiredPrefab($"{EnemyPrefabsRoot}/MigrationEnemy_Bat.prefab");
            MigrationSimpleEnemyController controller = bat.GetComponent<MigrationSimpleEnemyController>();
            AssertEqual(true, controller != null, "Generated bat prefab should keep enemy controller.");
            AssertEqual(true, controller.HasProjectilePrefab, "Generated ranged enemies should serialize a projectile feedback prefab reference.");

            MigrationCombatHurtFeedback hurtFeedback = bat.GetComponent<MigrationCombatHurtFeedback>();
            AssertEqual(true, hurtFeedback != null, "Generated enemies should carry reusable hurt feedback.");
            AssertEqual(true, hurtFeedback.FlashDurationSeconds > 0f, "Generated hurt feedback should serialize a visible flash duration.");

            MigrationCombatDefeatHandler defeatHandler = bat.GetComponent<MigrationCombatDefeatHandler>();
            AssertEqual(true, defeatHandler != null, "Generated enemies should keep defeat handling.");
            AssertEqual(true, defeatHandler.HasDeathFeedbackPrefab, "Generated enemies should serialize a reusable death feedback prefab seam.");
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

        private static GameObject CreatePlayerObject()
        {
            GameObject player = new GameObject("EnemyCombatFeedbackTemplateSmoke_Player");
            player.tag = "Player";
            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 1f, 0f);
            player.AddComponent<MigrationPlayerController>();
            return player;
        }

        private static GameObject CreateProjectilePrefab(string name, string family)
        {
            GameObject projectileObject = new GameObject(name);
            MigrationCombatFeedbackTemplate template = projectileObject.AddComponent<MigrationCombatFeedbackTemplate>();
            template.ConfigureTemplate(
                family,
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
                family,
                false,
                0f,
                string.Empty,
                false);

            MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
            projectile.ApplyFeedbackTemplate(template);
            return projectileObject;
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

        private static void AssertVectorApproximately(Vector3 expected, Vector3 actual, float tolerance, string message)
        {
            if (Vector3.Distance(expected, actual) > tolerance)
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
