using System;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardBossClauses: the boss-clause lifecycle (Godot CardBossClauseController) —
    // install/reveal/expose, sealing only an exposed clause with a matching answer family + >=2 cards,
    // and the disable-turns countdown.
    public static class CardBossClausesSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Card Boss Clauses Smoke Tests")]
        public static void RunAll()
        {
            TestInstallRevealExpose();
            TestSealRequiresExposedMatchingAnswerAndTwoCards();
            TestSealOnceThenLocked();
            TestDisableTicksDown();
            TestSnapshotRoundTrip();
            Debug.Log("Card boss clauses smoke tests passed.");
        }

        private static void TestInstallRevealExpose()
        {
            MigrationCardBossClauses clauses = new MigrationCardBossClauses();
            clauses.Install("cirno_clause", new[] { "field_replace", "melt_terrain" });

            AssertEqual(false, clauses.IsRevealed("cirno_clause"), "A clause starts unrevealed by default.");
            AssertEqual(false, clauses.IsExposed("cirno_clause"), "A clause starts unexposed by default.");
            AssertEqual(false, clauses.IsSealed("cirno_clause"), "A clause starts unsealed.");

            clauses.Reveal("cirno_clause");
            AssertEqual(true, clauses.IsRevealed("cirno_clause"), "Reveal marks the clause revealed.");

            clauses.Expose("cirno_clause");
            AssertEqual(true, clauses.IsExposed("cirno_clause"), "Expose marks the clause exposed.");

            // An unknown clause is inert.
            AssertEqual(false, clauses.IsRevealed("ghost"), "An uninstalled clause reports false.");
            clauses.Expose("ghost");
            AssertEqual(false, clauses.IsExposed("ghost"), "Exposing an uninstalled clause does nothing.");
        }

        private static void TestSealRequiresExposedMatchingAnswerAndTwoCards()
        {
            MigrationCardBossClauses clauses = new MigrationCardBossClauses();
            clauses.Install("cirno_clause", new[] { "field_replace", "melt_terrain" });

            AssertEqual(false, clauses.CanSealWithAnswer("cirno_clause", "field_replace", 2),
                "An unexposed clause cannot be sealed.");

            clauses.Expose("cirno_clause");
            AssertEqual(false, clauses.CanSealWithAnswer("cirno_clause", "field_replace", 1),
                "Sealing needs at least two answer cards.");
            AssertEqual(false, clauses.CanSealWithAnswer("cirno_clause", "ice_slow", 2),
                "Sealing needs a matching answer family.");
            AssertEqual(true, clauses.CanSealWithAnswer("cirno_clause", "field_replace", 2),
                "An exposed clause seals with a matching family and two cards.");
        }

        private static void TestSealOnceThenLocked()
        {
            MigrationCardBossClauses clauses = new MigrationCardBossClauses();
            clauses.Install("cirno_clause", new[] { "field_replace" });
            clauses.Expose("cirno_clause");

            AssertEqual(true, clauses.SealWithAnswer("cirno_clause", "field_replace", 2), "The first valid seal succeeds.");
            AssertEqual(true, clauses.IsSealed("cirno_clause"), "The clause is now sealed.");
            AssertEqual(false, clauses.CanSealWithAnswer("cirno_clause", "field_replace", 2),
                "An already-sealed clause cannot be sealed again.");
            AssertEqual(false, clauses.SealWithAnswer("cirno_clause", "field_replace", 2), "Re-sealing fails.");
        }

        private static void TestDisableTicksDown()
        {
            MigrationCardBossClauses clauses = new MigrationCardBossClauses();
            clauses.Install("cirno_clause", new[] { "field_replace" });

            clauses.Disable("cirno_clause", 2);
            AssertEqual(true, clauses.IsDisabled("cirno_clause"), "A disabled clause reads disabled.");

            clauses.TickDisabled();
            AssertEqual(true, clauses.IsDisabled("cirno_clause"), "Still disabled after one tick of a two-turn disable.");

            clauses.TickDisabled();
            AssertEqual(false, clauses.IsDisabled("cirno_clause"), "The disable lapses after its turns elapse.");
        }

        private static void TestSnapshotRoundTrip()
        {
            MigrationCardBossClauses clauses = new MigrationCardBossClauses();
            clauses.Install("cirno", new[] { "field_replace", "melt_terrain" }, revealed: true, exposed: true);
            clauses.Disable("cirno", 2);

            CardBossClausesSnapshot snapshot = clauses.CreateSnapshot();

            MigrationCardBossClauses restored = new MigrationCardBossClauses();
            restored.LoadSnapshot(snapshot);

            AssertEqual(true, restored.IsRevealed("cirno"), "Revealed round-trips.");
            AssertEqual(true, restored.IsExposed("cirno"), "Exposed round-trips.");
            AssertEqual(true, restored.IsDisabled("cirno"), "Disabled-turns round-trips.");
            // The answer families round-trip: an exposed clause seals with a saved family + 2 cards.
            AssertEqual(true, restored.SealWithAnswer("cirno", "field_replace", 2),
                "The saved answer families round-trip (the clause still seals).");
            AssertEqual(true, restored.IsSealed("cirno"), "The restored clause is now sealed.");
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
