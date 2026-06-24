using System;
using System.Reflection;
using TouhouMigration.Runtime.Combat;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class EnemyProjectileGrazeSmokeTests
    {
        private const string BuilderTypeName = "TouhouMigration.Editor.TouhouMigrationProjectBuilder, Assembly-CSharp-Editor";
        private const string CombatFeedbackPrefabsRoot = "Assets/TouhouMigration/Prefabs/CombatFeedback";

        [MenuItem("Touhou Migration/Tests/Run Enemy Projectile Graze Smoke Tests")]
        public static void RunAll()
        {
            TestProjectileGrazeFiresOnceForNearMissOutsideHitRadius();
            TestProjectilePerfectGrazeUsesInnerBandAndPresenter();
            TestProjectileHitSuppressesGraze();
            TestGeneratedProjectileFeedbackPrefabCarriesGrazeDefaults();
            Debug.Log("Enemy projectile graze smoke tests passed.");
        }

        private static void TestProjectileGrazeFiresOnceForNearMissOutsideHitRadius()
        {
            GameObject projectileObject = new GameObject("EnemyProjectileGrazeSmoke_Normal");
            try
            {
                MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                projectile.Configure(20f, 10f, Vector3.forward, true, 0.35f);
                projectile.ConfigureGraze(true, 1.15f, 0.7f);

                int eventCount = 0;
                MigrationProjectileGrazeResult lastResult = default;
                projectile.Grazed += result =>
                {
                    eventCount++;
                    lastResult = result;
                };

                projectile.Tick(0.3f, new Vector3(0.9f, 0f, 5f));
                projectile.Tick(0.1f, new Vector3(0.9f, 0f, 5.5f));

                AssertEqual(1, eventCount, "A single projectile should graze the same player only once.");
                AssertEqual(1, projectile.GrazeEventCount, "Projectile should count exactly one graze event.");
                AssertEqual(0, projectile.HitEventCount, "Near-miss graze should not damage the player.");
                AssertEqual("normal", projectile.LastGrazeQuality, "Outer graze band should be normal quality.");
                AssertEqual("normal", lastResult.Quality, "Graze event should report normal quality.");
                AssertEqual(true, projectile.LastGrazeDistance > 0.8f && projectile.LastGrazeDistance < 1.0f, "Graze distance should reflect the near miss.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
            }
        }

        private static void TestProjectilePerfectGrazeUsesInnerBandAndPresenter()
        {
            GameObject projectileObject = new GameObject("EnemyProjectileGrazeSmoke_Perfect");
            try
            {
                MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                projectile.Configure(20f, 10f, Vector3.forward, true, 0.35f);
                projectile.ConfigureGraze(true, 1.15f, 0.7f);

                MigrationProjectileGrazePresenter presenter = projectileObject.AddComponent<MigrationProjectileGrazePresenter>();
                presenter.BindProjectile(projectile);
                presenter.ConfigurePresentation(0.45f, new Color(1f, 0.72f, 0.25f, 1f), new Color(0.4f, 0.95f, 1f, 1f));

                projectile.Tick(0.3f, new Vector3(0.5f, 0f, 5f));

                AssertEqual(1, projectile.GrazeEventCount, "Perfect graze should still count as one graze.");
                AssertEqual("perfect", projectile.LastGrazeQuality, "Inner graze band should be perfect quality.");
                AssertEqual(1, presenter.GrazeNotificationCount, "Graze presenter should display one notification.");
                AssertEqual("Perfect Graze", presenter.LastGrazeText, "Graze presenter should distinguish perfect quality.");
                AssertEqual(true, presenter.HasActiveGrazeNotification, "Graze presenter should leave a visible notification active.");
                AssertEqual(true, projectileObject.GetComponentInChildren<TextMesh>(true) != null, "Graze presenter should use a lightweight TextMesh marker.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
            }
        }

        private static void TestProjectileHitSuppressesGraze()
        {
            GameObject projectileObject = new GameObject("EnemyProjectileGrazeSmoke_Hit");
            try
            {
                MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                projectile.Configure(20f, 10f, Vector3.forward, true, 0.35f);
                projectile.ConfigureGraze(true, 1.15f, 0.7f);

                projectile.Tick(0.3f, new Vector3(0.2f, 0f, 6f));

                AssertEqual(1, projectile.HitEventCount, "Player inside hit radius should be hit.");
                AssertEqual(0, projectile.GrazeEventCount, "Player inside hit radius should not also graze.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
            }
        }

        private static void TestGeneratedProjectileFeedbackPrefabCarriesGrazeDefaults()
        {
            InvokeBuilder("BuildCombatFeedbackPrefabs");

            GameObject projectilePrefab = RequiredPrefab($"{CombatFeedbackPrefabsRoot}/MigrationEnemyProjectileFeedback.prefab");
            MigrationCombatFeedbackTemplate template = projectilePrefab.GetComponent<MigrationCombatFeedbackTemplate>();
            MigrationEnemyProjectile projectile = projectilePrefab.GetComponent<MigrationEnemyProjectile>();

            AssertEqual(true, template != null && template.GrazeEnabled, "Projectile feedback template should enable graze detection.");
            AssertEqual(true, template.GrazeRadius > template.PerfectGrazeRadius, "Graze radius should be wider than perfect graze radius.");
            AssertEqual(true, projectile != null && projectile.GrazeEnabled, "Projectile runtime should apply graze defaults from the template.");
            AssertEqual(true, template.PerfectGrazeRadius > projectile.HitRadius, "Perfect graze radius should remain outside the default hit radius.");
            AssertEqual(true, projectile.GetComponent<MigrationProjectileGrazePresenter>() != null, "Projectile feedback prefab should carry a graze presenter.");
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
    }
}
