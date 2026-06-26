using System;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardBossDomains: the boss-domain contest (Godot CardBossDomainController) —
    // accumulating progress toward a threshold (breaks + seals on reach), answer-tag/family contest
    // bonuses, the sealed guard, and pressure ticks.
    public static class CardBossDomainsSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Card Boss Domains Smoke Tests")]
        public static void RunAll()
        {
            TestInstallClampsAndAutoSealsAtThreshold();
            TestContestAccumulatesAndBreaksAtThreshold();
            TestAnswerTagAndFamilyBonus();
            TestZeroContestRejectedAndSealedGuard();
            TestPressureTickClampsAtZero();
            Debug.Log("Card boss domains smoke tests passed.");
        }

        private static void TestInstallClampsAndAutoSealsAtThreshold()
        {
            MigrationCardBossDomains domains = new MigrationCardBossDomains();
            domains.Install("cirno", threshold: 0, pressure: 2);
            AssertEqual(1, domains.GetThreshold("cirno"), "Threshold clamps to at least 1.");
            AssertEqual(2, domains.GetPressure("cirno"), "Pressure is stored.");
            AssertEqual(false, domains.IsBroken("cirno"), "A fresh domain is not broken.");

            // Installing already at/over threshold breaks + seals immediately.
            domains.Install("ready", threshold: 3, progress: 3);
            AssertEqual(true, domains.IsBroken("ready"), "Progress >= threshold on install marks broken.");
            AssertEqual(true, domains.IsSealed("ready"), "Progress >= threshold on install marks sealed.");
        }

        private static void TestContestAccumulatesAndBreaksAtThreshold()
        {
            MigrationCardBossDomains domains = new MigrationCardBossDomains();
            domains.Install("cirno", threshold: 3);

            AssertEqual(true, domains.Contest("cirno", 2), "A positive contest succeeds.");
            AssertEqual(2, domains.GetProgress("cirno"), "Contest accumulates progress.");
            AssertEqual(false, domains.IsBroken("cirno"), "Below threshold is not broken.");

            AssertEqual(true, domains.Contest("cirno", 1), "Reaching the threshold succeeds.");
            AssertEqual(true, domains.IsBroken("cirno"), "Hitting the threshold breaks the domain.");
            AssertEqual(true, domains.IsSealed("cirno"), "A broken domain is sealed.");
        }

        private static void TestAnswerTagAndFamilyBonus()
        {
            MigrationCardBossDomains domains = new MigrationCardBossDomains();
            domains.Install("cirno", threshold: 99,
                answerTags: new[] { "melt_terrain" },
                answerFamilies: new[] { "field_replace" });

            // amount 1 + matching answer tag (+1) = 2 progress.
            domains.Contest("cirno", 1, "melt_terrain");
            AssertEqual(2, domains.GetProgress("cirno"), "A matching answer tag adds its bonus.");

            // amount 1 + matching family of "field_replace:rewrite" (+1) = 2 more -> 4.
            domains.Contest("cirno", 1, "field_replace:rewrite");
            AssertEqual(4, domains.GetProgress("cirno"), "A matching answer family (tag before ':') adds its bonus.");

            // amount 1 + non-matching tag/family = 1 more -> 5.
            domains.Contest("cirno", 1, "unrelated");
            AssertEqual(5, domains.GetProgress("cirno"), "A non-matching answer tag adds no bonus.");
        }

        private static void TestZeroContestRejectedAndSealedGuard()
        {
            MigrationCardBossDomains domains = new MigrationCardBossDomains();
            domains.Install("cirno", threshold: 5);

            AssertEqual(false, domains.Contest("cirno", 0), "A zero-amount contest with no bonus is rejected.");
            AssertEqual(0, domains.GetProgress("cirno"), "A rejected contest does not advance progress.");

            domains.Contest("cirno", 5); // seals it
            AssertEqual(false, domains.Contest("cirno", 3), "A sealed domain cannot be contested further.");
        }

        private static void TestPressureTickClampsAtZero()
        {
            MigrationCardBossDomains domains = new MigrationCardBossDomains();
            domains.Install("cirno", threshold: 3, pressure: 2);

            domains.TickPressure("cirno", 3);
            AssertEqual(5, domains.GetPressure("cirno"), "Pressure ticks up by the delta.");

            domains.TickPressure("cirno", -10);
            AssertEqual(0, domains.GetPressure("cirno"), "Pressure clamps at zero.");
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
