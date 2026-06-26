using System;
using TouhouMigration.Runtime.Social;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationNpcGaitProfiles: gait timing params + classify-by-personality (Godot NpcGaitProfiles
    // GAITS / profile / classify).
    public static class NpcGaitProfilesSmokeTests
    {
        private const double Tol = 1e-9;

        [MenuItem("Touhou Migration/Tests/Run NPC Gait Profiles Smoke Tests")]
        public static void RunAll()
        {
            TestProfileParams();
            TestUnknownGaitFallsBackToCalm();
            TestClassifyByPersonality();
            Debug.Log("NPC gait profiles smoke tests passed.");
        }

        private static void TestProfileParams()
        {
            MigrationGaitProfile lady = MigrationNpcGaitProfiles.Profile("lady");
            AssertClose(0.90, lady.Cadence, "Lady cadence.");
            AssertClose(0.92, lady.StrideScale, "Lady stride.");

            MigrationGaitProfile child = MigrationNpcGaitProfiles.Profile("child");
            AssertClose(1.15, child.Cadence, "Child cadence.");
            AssertClose(0.80, child.StrideScale, "Child stride.");

            MigrationGaitProfile brisk = MigrationNpcGaitProfiles.Profile("brisk");
            AssertClose(1.10, brisk.Cadence, "Brisk cadence.");
        }

        private static void TestUnknownGaitFallsBackToCalm()
        {
            MigrationGaitProfile calm = MigrationNpcGaitProfiles.Profile("nonsense");
            AssertClose(1.00, calm.Cadence, "Unknown gait falls back to calm cadence 1.0.");
            AssertClose(1.00, calm.StrideScale, "Unknown gait falls back to calm stride 1.0.");
        }

        private static void TestClassifyByPersonality()
        {
            AssertEqual("child", MigrationNpcGaitProfiles.Classify("琪露诺"), "Cirno is classified as a child gait.");
            AssertEqual("lady", MigrationNpcGaitProfiles.Classify("十六夜咲夜"), "Sakuya (contains 咲夜) is a lady gait.");
            AssertEqual("brisk", MigrationNpcGaitProfiles.Classify("雾雨魔理沙"), "Marisa (contains 魔理沙) is a brisk gait.");
            AssertEqual("calm", MigrationNpcGaitProfiles.Classify("某个路人"), "An unlisted NPC defaults to calm.");
        }

        private static void AssertClose(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > Tol)
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
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
