using System;
using System.Reflection;
using TouhouMigration.Runtime.Combat;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Player;
using TouhouMigration.Runtime.Settings;
using TouhouMigration.Runtime.Social;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class EnemyCombatReadabilitySmokeTests
    {
        private const string BuilderTypeName = "TouhouMigration.Editor.TouhouMigrationProjectBuilder, Assembly-CSharp-Editor";
        private const string CombatFeedbackPrefabsRoot = "Assets/TouhouMigration/Prefabs/CombatFeedback";
        private const string EnemyPrefabsRoot = "Assets/TouhouMigration/Prefabs/Enemies";
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";

        [MenuItem("Touhou Migration/Tests/Run Enemy Combat Readability Smoke Tests")]
        public static void RunAll()
        {
            TestDamageNumberPresenterRespectsSettingsToggle();
            TestRewardAndLootPresentationFollowOneShotGrantEvents();
            TestProjectileEnvironmentImpactUsesPhysicsSweepBeforePlayerDamage();
            TestGeneratedEnemiesCarryCombatReadabilityPresenters();
            Debug.Log("Enemy combat readability smoke tests passed.");
        }

        private static void TestDamageNumberPresenterRespectsSettingsToggle()
        {
            GameObject targetObject = new GameObject("EnemyCombatReadabilitySmoke_DamageTarget");
            try
            {
                MigrationGameSettings settings = new MigrationGameSettings { ShowDamageNumbers = true };
                MigrationCombatTargetBehaviour target = targetObject.AddComponent<MigrationCombatTargetBehaviour>();
                target.Initialize(20f);

                MigrationDamageNumberPresenter presenter = targetObject.AddComponent<MigrationDamageNumberPresenter>();
                presenter.BindTarget(target);
                presenter.BindSettings(settings);
                presenter.ConfigurePresentation(0.45f, 1.35f, new Color(1f, 0.92f, 0.35f, 1f));

                target.ApplyDamage(4f);

                AssertEqual(1, presenter.DamageNumberEventCount, "Damage presenter should react to the Damaged event.");
                AssertEqual("-4", presenter.LastDamageText, "Damage presenter should format readable damage text.");
                AssertEqual(true, presenter.HasActiveDamageNumber, "Damage presenter should create an active visual marker.");
                AssertEqual(true, targetObject.GetComponentInChildren<TextMesh>(true) != null, "Damage presenter should use a lightweight Unity TextMesh marker.");

                settings.ShowDamageNumbers = false;
                target.ApplyDamage(3f);

                AssertEqual(1, presenter.DamageNumberEventCount, "Disabled damage numbers should not create another visible marker.");
                AssertEqual(1, presenter.SuppressedDamageNumberCount, "Disabled damage numbers should still be tracked as intentionally suppressed.");
                AssertEqual("-4", presenter.LastDamageText, "Disabled damage numbers should leave the last visible text unchanged.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        private static void TestRewardAndLootPresentationFollowOneShotGrantEvents()
        {
            GameObject targetObject = new GameObject("EnemyCombatReadabilitySmoke_RewardTarget");
            try
            {
                ItemDatabase itemDatabase = new ItemDatabase();
                AssertEqual(true, itemDatabase.LoadFromPath(ItemDataPath), "Item database should load for reward presentation smoke.");
                InventoryService inventory = new InventoryService(itemDatabase, 48);
                MigrationPlayerProgressService progress = new MigrationPlayerProgressService();
                QuestDeliveryService quests = new QuestDeliveryService();

                MigrationCombatTargetBehaviour target = targetObject.AddComponent<MigrationCombatTargetBehaviour>();
                target.Initialize(5f);

                MigrationCombatDefeatRewardHandler rewardHandler = targetObject.AddComponent<MigrationCombatDefeatRewardHandler>();
                rewardHandler.BindTarget(target);
                rewardHandler.BindRewards(progress, quests);
                rewardHandler.ConfigureRewards(12, 3, "enemy_killed");

                MigrationCombatLootDropHandler lootHandler = targetObject.AddComponent<MigrationCombatLootDropHandler>();
                lootHandler.BindTarget(target);
                lootHandler.BindServices(inventory, quests);
                lootHandler.ConfigureGuaranteedDrop("fairy_meat", 2);
                lootHandler.ConfigureQuestKillNotification(false);

                MigrationCombatRewardPresentation presentation = targetObject.AddComponent<MigrationCombatRewardPresentation>();
                presentation.BindRewardHandler(rewardHandler);
                presentation.BindLootDropHandler(lootHandler);
                presentation.ConfigurePresentation(0.8f, new Color(1f, 0.86f, 0.32f, 1f), new Color(0.42f, 1f, 0.58f, 1f));

                target.ApplyDamage(6f);

                AssertEqual(1, rewardHandler.RewardGrantCount, "Reward handler should still grant once.");
                AssertEqual(1, lootHandler.LootGrantCount, "Loot handler should still grant once.");
                AssertEqual(1, presentation.RewardNotificationCount, "Reward presentation should display the XP/coin grant once.");
                AssertEqual(1, presentation.LootNotificationCount, "Loot presentation should display the granted item once.");
                AssertContains("12 XP", presentation.LastRewardText, "Reward notification should include XP.");
                AssertContains("3 coins", presentation.LastRewardText, "Reward notification should include coins.");
                AssertContains("fairy_meat", presentation.LastLootText, "Loot notification should include migrated item id.");
                AssertEqual(12, progress.Experience, "Reward presentation should not replace progress reward logic.");
                AssertEqual(2, inventory.GetItemCount("fairy_meat"), "Loot presentation should not replace inventory grant logic.");

                target.ApplyDamage(6f);

                AssertEqual(1, rewardHandler.RewardGrantCount, "Duplicate defeat should not duplicate reward grants.");
                AssertEqual(1, lootHandler.LootGrantCount, "Duplicate defeat should not duplicate loot grants.");
                AssertEqual(1, presentation.RewardNotificationCount, "Duplicate defeat should not duplicate reward presentation.");
                AssertEqual(1, presentation.LootNotificationCount, "Duplicate defeat should not duplicate loot presentation.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        private static void TestProjectileEnvironmentImpactUsesPhysicsSweepBeforePlayerDamage()
        {
            GameObject projectileObject = new GameObject("EnemyCombatReadabilitySmoke_Projectile");
            GameObject wallObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                wallObject.name = "EnemyCombatReadabilitySmoke_Wall";
                wallObject.transform.position = new Vector3(0f, 0f, 3f);
                wallObject.transform.localScale = new Vector3(4f, 4f, 0.25f);

                MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                MigrationCombatFeedbackTemplate template = projectileObject.AddComponent<MigrationCombatFeedbackTemplate>();
                template.ConfigureTemplate(
                    "enemy_projectile",
                    true,
                    "Default",
                    2f,
                    0.2f,
                    new Color(1f, 0.15f, 0.1f, 1f),
                    true,
                    true);

                projectile.Configure(20f, 10f, Vector3.forward, true, 0.35f);
                projectile.ApplyFeedbackTemplate(template);
                projectile.ConfigureEnvironmentImpact(true, Physics.DefaultRaycastLayers);

                Physics.SyncTransforms();
                projectile.Tick(0.3f, new Vector3(0f, 0f, 100f));

                AssertEqual(1, projectile.EnvironmentImpactEventCount, "Projectile should register an environment impact when a wall is crossed.");
                AssertEqual(0, projectile.HitEventCount, "Environment impact should stop the projectile before player damage.");
                AssertEqual(true, projectile.IsExpired, "Environment impact should expire or stop the projectile.");
                AssertEqual(1, projectile.ImpactEventCount, "Environment impact should reuse the projectile impact feedback hook.");
                AssertEqual(true, projectile.HasActiveImpactFeedback, "Environment impact should leave visible impact feedback active.");
                AssertEqual(true, projectile.LastEnvironmentImpactPoint.z > 2.7f && projectile.LastEnvironmentImpactPoint.z < 3.3f, "Environment impact point should be near the blocking wall.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(wallObject);
                UnityEngine.Object.DestroyImmediate(projectileObject);
            }
        }

        private static void TestGeneratedEnemiesCarryCombatReadabilityPresenters()
        {
            InvokeBuilder("BuildCombatFeedbackPrefabs");
            InvokeBuilder("BuildEnemyCatalogPrefabs");

            GameObject projectilePrefab = RequiredPrefab($"{CombatFeedbackPrefabsRoot}/MigrationEnemyProjectileFeedback.prefab");
            MigrationEnemyProjectile projectile = projectilePrefab.GetComponent<MigrationEnemyProjectile>();
            AssertEqual(true, projectile != null, "Projectile feedback prefab should keep projectile runtime.");
            AssertEqual(true, projectile.EnvironmentImpactEnabled, "Projectile feedback prefab should be configured for environment impacts.");

            GameObject bat = RequiredPrefab($"{EnemyPrefabsRoot}/MigrationEnemy_Bat.prefab");
            AssertEqual(true, bat.GetComponent<MigrationDamageNumberPresenter>() != null, "Generated enemies should carry damage-number presentation.");
            AssertEqual(true, bat.GetComponent<MigrationCombatRewardPresentation>() != null, "Generated enemies should carry reward/loot presentation.");
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

        private static void AssertContains(string expectedFragment, string actual, string message)
        {
            if (actual == null || !actual.Contains(expectedFragment, StringComparison.Ordinal))
            {
                throw new Exception($"{message} Expected fragment: {expectedFragment}. Actual: {actual}.");
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
