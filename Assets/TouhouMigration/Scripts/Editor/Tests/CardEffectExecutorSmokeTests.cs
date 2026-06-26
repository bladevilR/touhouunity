using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardEffectExecutor: the data-driven effect-block interpreter (Godot
    // CardEffectExecutor) dispatching against the run-controller facade — resources, statuses, deck moves,
    // clause reveal/seal, per-type amount defaults, and the ignored-effect fallback.
    public static class CardEffectExecutorSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Card Effect Executor Smoke Tests")]
        public static void RunAll()
        {
            TestResourceAndStatusBlocks();
            TestDeckBlocksWithAmountDefaults();
            TestClauseRevealAndSeal();
            TestUnhandledEffectsAreReported();
            Debug.Log("Card effect executor smoke tests passed.");
        }

        private static MigrationCardBuildRunController NewRun()
        {
            return new MigrationCardBuildRunController(
                new List<string> { "a", "b", "c", "d", "e" }, bossMaxHp: 540, bossClauseId: "cirno_domain");
        }

        private static void TestResourceAndStatusBlocks()
        {
            MigrationCardBuildRunController run = NewRun();
            MigrationCardEffectExecutor executor = new MigrationCardEffectExecutor();

            executor.Execute(new[]
            {
                new MigrationCardEffectBlock { Type = "create_resource", Resource = "ember", Amount = 3 },
                new MigrationCardEffectBlock { Type = "apply_status", Status = "burn", Amount = 2 }, // default target enemy
                new MigrationCardEffectBlock { Type = "spend_resource", Resource = "ember", Amount = 1 },
            }, run);

            AssertEqual(2, run.State.GetResource("ember"), "create then spend leaves 2 ember.");
            AssertEqual(2, run.State.GetStatus("enemy", "burn"), "apply_status defaults to the enemy target.");

            // spend with no amount drains the whole pool (Godot default -1).
            executor.Execute(new[] { new MigrationCardEffectBlock { Type = "spend_resource", Resource = "ember" } }, run);
            AssertEqual(0, run.State.GetResource("ember"), "A spend with no amount drains the resource.");
        }

        private static void TestDeckBlocksWithAmountDefaults()
        {
            MigrationCardBuildRunController run = NewRun();
            MigrationCardEffectExecutor executor = new MigrationCardEffectExecutor();
            run.Deck.Draw(5, _ => 0); // hand: a b c d e

            executor.Execute(new[]
            {
                new MigrationCardEffectBlock { Type = "discard" },            // default amount 1
                new MigrationCardEffectBlock { Type = "exhaust", Amount = 2 },
            }, run);

            AssertEqual(2, run.Deck.HandCount, "discard(1) + exhaust(2) removes three from a five-card hand.");
            AssertEqual(1, run.Deck.DiscardPileCount, "The default-amount discard moved one card.");
            AssertEqual(2, run.Deck.ExhaustPileCount, "The exhaust block moved two cards.");
        }

        private static void TestClauseRevealAndSeal()
        {
            MigrationCardBuildRunController run = NewRun();
            run.Clauses.Install("cirno_domain", new[] { "field_replace" });
            MigrationCardEffectExecutor executor = new MigrationCardEffectExecutor();

            executor.Execute(new[] { new MigrationCardEffectBlock { Type = "reveal_clause", ClauseId = "cirno_domain" } }, run);
            AssertEqual(true, run.Clauses.IsRevealed("cirno_domain"), "reveal_clause reveals the clause.");

            // seal_clause seals directly (Godot bypasses the answer gate).
            executor.Execute(new[] { new MigrationCardEffectBlock { Type = "seal_clause", ClauseId = "cirno_domain" } }, run);
            AssertEqual(true, run.Clauses.IsSealed("cirno_domain"), "seal_clause seals the clause directly.");
            AssertEqual(true, run.IsVulnerabilityOpen, "A sealed boss clause opens vulnerability via the facade.");
        }

        private static void TestUnhandledEffectsAreReported()
        {
            MigrationCardBuildRunController run = NewRun();
            MigrationCardEffectExecutor executor = new MigrationCardEffectExecutor();

            IReadOnlyList<string> ignored = executor.Execute(new[]
            {
                new MigrationCardEffectBlock { Type = "create_resource", Resource = "ash", Amount = 1 },
                new MigrationCardEffectBlock { Type = "summon" },
                new MigrationCardEffectBlock { Type = "create_field" },
                new MigrationCardEffectBlock { Type = "totally_unknown" },
            }, run);

            AssertEqual(3, ignored.Count, "The three un-ported effect types are reported as ignored.");
            AssertEqual(1, run.State.GetResource("ash"), "The handled block still applied alongside ignored ones.");
            AssertEqual(true, Contains(ignored, "summon"), "summon is reported ignored (its collection is a later slice).");
        }

        private static bool Contains(IReadOnlyList<string> list, string value)
        {
            foreach (string entry in list)
            {
                if (entry == value)
                {
                    return true;
                }
            }

            return false;
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
