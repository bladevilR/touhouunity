using System;
using System.Reflection;
using TouhouMigration.Runtime.Combat;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class EnemyProjectileSpecialRulesSmokeTests
    {
        private const string BuilderTypeName = "TouhouMigration.Editor.TouhouMigrationProjectBuilder, Assembly-CSharp-Editor";
        private const string CombatFeedbackPrefabsRoot = "Assets/TouhouMigration/Prefabs/CombatFeedback";

        [MenuItem("Touhou Migration/Tests/Run Enemy Projectile Special Rules Smoke Tests")]
        public static void RunAll()
        {
            TestWeakSourceShattersIceCrystalAndEmitsResult();
            TestNonWeakSourceOnlyChipsShatterHp();
            TestFeedbackTemplateAppliesProjectileRuleData();
            TestReflectableProjectileConsumesIntoStunRewardResult();
            TestReflectRewardSettlementEmitsStunSeam();
            TestPlayerAttackHitboxShattersProjectileOncePerWindow();
            TestPlayerAttackHitboxReflectsEligibleProjectileBeforeShatter();
            TestGeneratedProjectileFeedbackPrefabCarriesSpecialRuleSeam();
            TestGeneratedIceLanceProjectileFeedbackCarriesReflectReward();
            Debug.Log("Enemy projectile special rules smoke tests passed.");
        }

        private static void TestWeakSourceShattersIceCrystalAndEmitsResult()
        {
            GameObject projectileObject = new GameObject("EnemyProjectileSpecialRulesSmoke_Shatter");
            try
            {
                MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                projectile.Configure(0f, 7f, Vector3.forward, true, 0.35f);
                projectile.ConfigureShatterRules("ice_crystal", true, 25f, "fire,heavy,shatter");

                MigrationProjectileShatterPresenter presenter = projectileObject.AddComponent<MigrationProjectileShatterPresenter>();
                presenter.BindProjectile(projectile);
                presenter.ConfigurePresentation(0.5f, new Color(0.55f, 0.95f, 1f, 1f), new Color(1f, 0.74f, 0.28f, 1f));

                int eventCount = 0;
                MigrationProjectileShatterResult lastResult = null;
                projectile.Shattered += result =>
                {
                    eventCount++;
                    lastResult = result;
                };

                bool shattered = projectile.TryApplyShatterDamage(20f, "fire", new Vector3(2f, 0f, 0f));

                AssertEqual(true, shattered, "Fire damage should shatter a weak ice crystal after the weakness multiplier.");
                AssertEqual(1, eventCount, "Shatter should emit one result event.");
                AssertEqual(1, projectile.ShatterEventCount, "Projectile should count one shatter event.");
                AssertEqual(true, projectile.IsShattered, "Projectile should expose the shattered state.");
                AssertEqual(true, projectile.IsExpired, "Shattered projectiles should leave the active danger lifecycle.");
                AssertEqual("ice_crystal", projectile.ProjectileFamily, "Projectile should preserve its special-rule family.");
                AssertEqual("fire", projectile.LastShatterSourceFamily, "Projectile should remember the source family that shattered it.");
                AssertApproximately(30f, projectile.LastShatterDamageApplied, 0.001f, "Weakness multiplier should be reflected in damage applied.");
                AssertApproximately(0f, projectile.ShatterHp, 0.001f, "Shatter HP should be depleted.");
                AssertEqual(true, lastResult != null && lastResult.WasWeakness, "Shatter result should report that a weakness was used.");
                AssertEqual("ice_crystal", lastResult.ProjectileFamily, "Shatter result should expose projectile family.");
                AssertEqual(1, presenter.ShatterNotificationCount, "Presenter should react to shatter events.");
                AssertEqual("Shatter", presenter.LastShatterText, "Presenter should display a clear shatter callout.");
                AssertEqual(true, presenter.HasActiveShatterNotification, "Presenter should leave a visible shatter notification active.");

                bool secondAttempt = projectile.TryApplyShatterDamage(20f, "fire", Vector3.zero);
                AssertEqual(false, secondAttempt, "Already-shattered projectiles should not shatter twice.");
                AssertEqual(1, projectile.ShatterEventCount, "Already-shattered projectiles should not emit duplicate events.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
            }
        }

        private static void TestNonWeakSourceOnlyChipsShatterHp()
        {
            GameObject projectileObject = new GameObject("EnemyProjectileSpecialRulesSmoke_Chip");
            try
            {
                MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                projectile.Configure(0f, 7f, Vector3.forward, true, 0.35f);
                projectile.ConfigureShatterRules("ice_crystal", true, 25f, "fire,heavy,shatter");

                bool shattered = projectile.TryApplyShatterDamage(10f, "star", Vector3.zero);

                AssertEqual(false, shattered, "Non-weak damage should be allowed to chip without shattering.");
                AssertEqual(false, projectile.IsExpired, "A chipped projectile should remain active.");
                AssertEqual(false, projectile.IsShattered, "A chipped projectile should not be marked shattered.");
                AssertEqual(0, projectile.ShatterEventCount, "Chip damage should not emit a shatter event.");
                AssertApproximately(15f, projectile.ShatterHp, 0.001f, "Non-weak chip damage should reduce shatter HP by raw damage.");
                AssertApproximately(10f, projectile.LastShatterDamageApplied, 0.001f, "Projectile should remember the last special damage amount.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
            }
        }

        private static void TestFeedbackTemplateAppliesProjectileRuleData()
        {
            GameObject projectileObject = new GameObject("EnemyProjectileSpecialRulesSmoke_Template");
            try
            {
                MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                MigrationCombatFeedbackTemplate template = projectileObject.AddComponent<MigrationCombatFeedbackTemplate>();
                template.ConfigureTemplate(
                    "enemy_projectile",
                    true,
                    "EnemyProjectile",
                    4f,
                    0.18f,
                    new Color(0.55f, 0.9f, 1f, 1f),
                    true,
                    true,
                    true,
                    1.15f,
                    0.7f,
                    "frozen_crystal",
                    true,
                    20f,
                    "fire,heavy,shatter",
                    reflectable: true,
                    reflectStunReward: true,
                    reflectStunSeconds: 2f);

                projectile.ApplyFeedbackTemplate(template);

                AssertEqual("frozen_crystal", projectile.ProjectileFamily, "Template should apply projectile family data.");
                AssertEqual(true, projectile.Shatterable, "Template should apply shatterable state.");
                AssertApproximately(20f, projectile.ShatterHp, 0.001f, "Template should apply shatter HP.");
                AssertEqual(true, projectile.IsWeakTo("heavy"), "Template should apply shatter weaknesses.");
                AssertEqual(true, projectile.Reflectable, "Template should apply reflectable state.");
                AssertEqual(true, projectile.ReflectStunReward, "Template should apply reflect stun reward state.");
                AssertApproximately(2f, projectile.ReflectStunSeconds, 0.001f, "Template should apply reflect stun duration.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
            }
        }

        private static void TestReflectableProjectileConsumesIntoStunRewardResult()
        {
            GameObject projectileObject = new GameObject("EnemyProjectileSpecialRulesSmoke_Reflect");
            try
            {
                MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                projectile.Configure(22.5f, 16f, Vector3.forward, true, 0.35f);
                projectile.ConfigureShatterRules("ice_lance", false, 0f, string.Empty);
                projectile.ConfigureReflectRules(true, true, 2f);

                int eventCount = 0;
                MigrationProjectileReflectResult lastResult = null;
                projectile.Reflected += result =>
                {
                    eventCount++;
                    lastResult = result;
                };

                bool reflected = projectile.TryReflect("light", new Vector3(1f, 0f, 0f), Vector3.back, projectileObject);

                AssertEqual(true, reflected, "Reflectable projectiles should accept a valid reflect request.");
                AssertEqual(1, eventCount, "Reflect should emit one result event.");
                AssertEqual(1, projectile.ReflectEventCount, "Projectile should count one reflect event.");
                AssertEqual(true, projectile.IsExpired, "Reflected enemy projectiles should be consumed so pools can reclaim them.");
                AssertEqual(true, projectile.IsReflected, "Projectile should expose the reflected lifecycle state.");
                AssertEqual(false, projectile.IsShattered, "Reflect should not route through the shatter lifecycle.");
                AssertEqual(0, projectile.ShatterEventCount, "Reflect should not emit shatter events.");
                AssertEqual(true, lastResult != null, "Reflect should provide a result object.");
                AssertEqual("ice_lance", lastResult.ProjectileFamily, "Reflect result should preserve the Godot projectile family.");
                AssertEqual("light", projectile.LastReflectSourceFamily, "Projectile should remember the source family that reflected it.");
                AssertEqual(true, projectile.LastReflectStunReward, "Projectile should preserve the Godot stun reward marker.");
                AssertApproximately(2f, projectile.LastReflectStunSeconds, 0.001f, "Projectile should preserve the Godot stun reward duration.");
                AssertApproximately(-1f, projectile.LastReflectDirection.z, 0.001f, "Reflect should apply the requested return direction.");
                AssertEqual(true, lastResult != null && lastResult.StunReward, "Reflect result should expose stun reward data for a settlement layer.");
                AssertApproximately(2f, lastResult.StunSeconds, 0.001f, "Reflect result should expose stun reward seconds.");
                AssertApproximately(16f, lastResult.Damage, 0.001f, "Reflect result should expose the ice-lance damage value for future return-shot tuning.");

                bool duplicate = projectile.TryReflect("light", Vector3.zero, Vector3.back, projectileObject);
                AssertEqual(false, duplicate, "Consumed reflected projectiles should not reflect twice.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
            }
        }

        private static void TestReflectRewardSettlementEmitsStunSeam()
        {
            GameObject projectileObject = new GameObject("EnemyProjectileSpecialRulesSmoke_ReflectSettlement");
            try
            {
                MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                projectile.Configure(22.5f, 16f, Vector3.forward, true, 0.35f);
                projectile.ConfigureShatterRules("ice_lance", false, 0f, string.Empty);
                projectile.ConfigureReflectRules(true, true, 2f);

                MigrationProjectileSpecialSettlement settlement = projectileObject.AddComponent<MigrationProjectileSpecialSettlement>();
                settlement.ConfigureSharedSettlementFallback(false);
                settlement.BindProjectile(projectile);

                int stunEventCount = 0;
                MigrationProjectileReflectResult lastResult = null;
                settlement.ReflectStunReady += result =>
                {
                    stunEventCount++;
                    lastResult = result;
                };

                bool reflected = projectile.TryReflect("heavy", new Vector3(1f, 0f, 0f), Vector3.back, projectileObject);

                AssertEqual(true, reflected, "Reflect should succeed before settlement rewards can fire.");
                AssertEqual(1, stunEventCount, "Settlement should expose one reflect stun opportunity.");
                AssertEqual(1, settlement.ReflectSettlementCount, "Settlement should count one reflect reward.");
                AssertEqual(1, settlement.ReflectStunEventCount, "Settlement should count one reflect stun event.");
                AssertApproximately(2f, settlement.LastReflectStunSeconds, 0.001f, "Settlement should preserve reflect stun duration.");
                AssertEqual(true, lastResult != null && lastResult.StunReward, "Settlement event should carry the reflect result.");
                AssertEqual("ice_lance", lastResult.ProjectileFamily, "Settlement event should preserve the ice-lance family.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
            }
        }

        private static void TestPlayerAttackHitboxShattersProjectileOncePerWindow()
        {
            GameObject hitboxObject = new GameObject("EnemyProjectileSpecialRulesSmoke_Hitbox");
            GameObject projectileObject = new GameObject("EnemyProjectileSpecialRulesSmoke_HitboxProjectile");
            try
            {
                MigrationPlayerAttackHitbox hitbox = hitboxObject.AddComponent<MigrationPlayerAttackHitbox>();
                hitbox.Configure(20f, "heavy");
                hitbox.BeginAttackWindow();

                MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                projectile.Configure(0f, 7f, Vector3.forward, true, 0.35f);
                projectile.ConfigureShatterRules("ice_crystal", true, 25f, "fire,heavy,shatter");

                bool shattered = hitbox.TryHitProjectile(projectile, new Vector3(1f, 0f, 0f));
                bool duplicate = hitbox.TryHitProjectile(projectile, new Vector3(1f, 0f, 0f));

                AssertEqual(true, shattered, "Heavy attack hitbox should shatter a weak ice crystal projectile.");
                AssertEqual(false, duplicate, "One attack window should not shatter the same projectile twice.");
                AssertEqual(1, projectile.ShatterEventCount, "Projectile should receive one shatter event from the hitbox.");
                AssertEqual(1, hitbox.ProjectileShatterEventCount, "Hitbox should count one projectile shatter.");
                AssertEqual("heavy", projectile.LastShatterSourceFamily, "Hitbox attack type should be used as shatter source family.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
                UnityEngine.Object.DestroyImmediate(hitboxObject);
            }
        }

        private static void TestPlayerAttackHitboxReflectsEligibleProjectileBeforeShatter()
        {
            GameObject hitboxObject = new GameObject("EnemyProjectileSpecialRulesSmoke_ReflectHitbox");
            GameObject projectileObject = new GameObject("EnemyProjectileSpecialRulesSmoke_ReflectHitboxProjectile");
            try
            {
                MigrationPlayerAttackHitbox hitbox = hitboxObject.AddComponent<MigrationPlayerAttackHitbox>();
                hitbox.Configure(20f, "heavy");
                hitbox.BeginAttackWindow();

                projectileObject.transform.position = new Vector3(0f, 0f, 2f);
                MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                projectile.Configure(8f, 12f, Vector3.back, true, 0.35f);
                projectile.ConfigureReflectRules(true, true, 2f);

                bool reflected = hitbox.TryHitProjectile(projectile, projectileObject.transform.position);
                bool duplicate = hitbox.TryHitProjectile(projectile, projectileObject.transform.position);

                AssertEqual(true, reflected, "Active attack hitboxes should reflect eligible projectiles.");
                AssertEqual(false, duplicate, "One attack window should not reflect the same projectile twice.");
                AssertEqual(true, projectile.IsExpired, "Hitbox-reflected projectiles should be consumed for pool reclamation.");
                AssertEqual(1, projectile.ReflectEventCount, "Projectile should receive one reflect event from the hitbox.");
                AssertEqual(1, hitbox.ProjectileReflectEventCount, "Hitbox should count one projectile reflect.");
                AssertEqual(0, hitbox.ProjectileShatterEventCount, "Reflecting a projectile should not count as a shatter.");
                AssertEqual("heavy", projectile.LastReflectSourceFamily, "Hitbox attack type should be used as reflect source family.");
                AssertEqual(true, projectile.LastReflectDirection.z > 0.99f, "Hitbox reflect should send the projectile away from the attacker.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
                UnityEngine.Object.DestroyImmediate(hitboxObject);
            }
        }

        private static void TestGeneratedProjectileFeedbackPrefabCarriesSpecialRuleSeam()
        {
            InvokeBuilder("BuildCombatFeedbackPrefabs");

            GameObject projectilePrefab = RequiredPrefab($"{CombatFeedbackPrefabsRoot}/MigrationEnemyProjectileFeedback.prefab");
            MigrationCombatFeedbackTemplate template = projectilePrefab.GetComponent<MigrationCombatFeedbackTemplate>();
            MigrationEnemyProjectile projectile = projectilePrefab.GetComponent<MigrationEnemyProjectile>();

            AssertEqual(true, template != null, "Generated projectile feedback prefab should carry a template.");
            AssertEqual("enemy_projectile", template.ProjectileFamily, "Default projectile template should serialize a family for future special rules.");
            AssertEqual(false, template.Shatterable, "Default reusable enemy bullets should not all be breakable.");
            AssertEqual(false, template.Reflectable, "Default reusable enemy bullets should not all be reflectable.");
            AssertEqual(true, projectile != null, "Generated projectile feedback prefab should carry projectile runtime logic.");
            AssertEqual("enemy_projectile", projectile.ProjectileFamily, "Runtime projectile should apply default special-rule family from the template.");
            AssertEqual(false, projectile.Shatterable, "Runtime projectile should keep default shatter disabled.");
            AssertEqual(false, projectile.Reflectable, "Runtime projectile should keep default reflect disabled.");
            AssertEqual(true, projectile.GetComponent<MigrationProjectileShatterPresenter>() != null, "Projectile feedback prefab should carry a shatter presenter seam.");
        }

        private static void TestGeneratedIceLanceProjectileFeedbackCarriesReflectReward()
        {
            InvokeBuilder("BuildCombatFeedbackPrefabs");

            GameObject projectilePrefab = RequiredPrefab($"{CombatFeedbackPrefabsRoot}/MigrationIceLanceProjectileFeedback.prefab");
            MigrationCombatFeedbackTemplate template = projectilePrefab.GetComponent<MigrationCombatFeedbackTemplate>();
            MigrationEnemyProjectile projectile = projectilePrefab.GetComponent<MigrationEnemyProjectile>();

            AssertEqual(true, template != null, "Generated ice-lance prefab should carry a template.");
            AssertEqual("ice_lance", template.ProjectileFamily, "Generated ice-lance template should serialize its projectile family.");
            AssertEqual(true, template.Reflectable, "Generated ice-lance template should preserve Godot ice-lance reflectability.");
            AssertEqual(true, template.ReflectStunReward, "Generated ice-lance template should preserve Godot ice-lance stun reward.");
            AssertApproximately(2f, template.ReflectStunSeconds, 0.001f, "Generated ice-lance template should preserve Godot stun seconds.");
            AssertEqual(true, projectile != null, "Generated ice-lance prefab should carry projectile runtime logic.");
            AssertEqual(true, projectile.Reflectable, "Generated ice-lance runtime should apply reflectability from the template.");
            AssertEqual(true, projectile.ReflectStunReward, "Generated ice-lance runtime should apply reflect stun reward from the template.");
            AssertApproximately(2f, projectile.ReflectStunSeconds, 0.001f, "Generated ice-lance runtime should apply reflect stun seconds from the template.");
            AssertEqual("ice_lance", projectile.ProjectileFamily, "Generated ice-lance runtime should keep the Godot family distinct from ice shard.");

            GameObject iceShardPrefab = RequiredPrefab($"{CombatFeedbackPrefabsRoot}/MigrationIceShardProjectileFeedback.prefab");
            MigrationEnemyProjectile iceShard = iceShardPrefab.GetComponent<MigrationEnemyProjectile>();
            AssertEqual(false, iceShard.Reflectable, "Ice shard fan projectiles should stay separate from Godot ice-lance reflect rules.");
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
