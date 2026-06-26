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
            TestCollectionEffectsAppend();
            TestMokouEffectBlocks();
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
                new MigrationCardEffectBlock { Type = "damage", Amount = 40 },      // handled no-op (Godot logs)
                new MigrationCardEffectBlock { Type = "totally_unknown" },
                new MigrationCardEffectBlock { Type = "another_unknown" },
            }, run);

            AssertEqual(2, ignored.Count, "Only genuinely-unknown effect types are reported as ignored.");
            AssertEqual(1, run.State.GetResource("ash"), "The handled block still applied alongside ignored ones.");
            AssertEqual(true, Contains(ignored, "totally_unknown"), "An unknown effect type is reported ignored.");
            AssertEqual(false, Contains(ignored, "damage"), "damage is a handled (logged) no-op, not ignored.");
        }

        private static void TestMokouEffectBlocks()
        {
            MigrationCardBuildRunController run = NewRun();
            MigrationCardEffectExecutor executor = new MigrationCardEffectExecutor();

            IReadOnlyList<string> ignored = executor.Execute(new[]
            {
                new MigrationCardEffectBlock
                {
                    Type = "mokou_bind_terminal", Id = "T01",
                    BaseDamage = 65, EnergyCost = 25, Flame = 4, TriggerCoefficient = 1.0,
                },
                new MigrationCardEffectBlock { Type = "mokou_process_modifier", Id = "P02", ChargeSpeedMultiplier = 2.0 },
                new MigrationCardEffectBlock { Type = "mokou_trigger", Id = "G02" },
            }, run);

            AssertEqual(0, ignored.Count, "Mokou effect blocks are handled, not ignored.");
            AssertEqual(2.0, run.Mokou.ChargeSpeedMultiplier, "mokou_process_modifier reaches the Mokou chain.");
            AssertEqual(1, run.Mokou.TriggerCount, "mokou_trigger installs a trigger.");

            // The bound terminal is now releasable: charge fully and release for its base damage.
            run.Mokou.BeginCharge();
            run.Mokou.AdvanceCharge(1.6 / 2.0); // x2 speed -> full charge in half the time
            MokouReleaseResult result = run.Mokou.ReleaseCharge();
            AssertEqual(true, result.Success, "mokou_bind_terminal bound a releasable terminal.");
            AssertEqual(4, result.Flame, "The bound terminal's flame came through the effect block.");
        }

        private static void TestCollectionEffectsAppend()
        {
            MigrationCardBuildRunController run = NewRun();
            MigrationCardEffectExecutor executor = new MigrationCardEffectExecutor();

            IReadOnlyList<string> ignored = executor.Execute(new[]
            {
                new MigrationCardEffectBlock { Type = "summon", Id = "ice_fairy" },
                new MigrationCardEffectBlock { Type = "install", Id = "frost_rule" },
                new MigrationCardEffectBlock { Type = "create_field", Id = "frozen_lake" },
                new MigrationCardEffectBlock { Type = "trigger_partner" },
                new MigrationCardEffectBlock { Type = "modify_bullet" },
            }, run);

            AssertEqual(0, ignored.Count, "Collection effects are handled, not ignored.");
            AssertEqual(1, run.Summons.Count, "summon appends to the summons collection.");
            AssertEqual("ice_fairy", run.Summons[0].Id, "The summon block is stored with its id.");
            AssertEqual(1, run.InstalledCards.Count, "install appends to the installed-cards collection.");
            AssertEqual(1, run.FieldObjects.Count, "create_field appends to the field-objects collection.");
            AssertEqual(1, run.PartnerEvents.Count, "trigger_partner appends to the partner-events collection.");
            AssertEqual(1, run.BulletModifiers.Count, "modify_bullet appends to the bullet-modifiers collection.");
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
