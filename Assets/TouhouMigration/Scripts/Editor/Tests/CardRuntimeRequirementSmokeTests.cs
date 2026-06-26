using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardBuildRunController.RuntimeRequirementReason: the per-card runtime gate (Godot
    // _runtime_requirement_disabled_reason) reading the live resource/status/vulnerability/rewritten-rule
    // state, and its integration into PlayCard.
    public static class CardRuntimeRequirementSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Card Runtime Requirement Smoke Tests")]
        public static void RunAll()
        {
            TestBurnGate();
            TestVulnerabilityAndEmberGate();
            TestSealAndRewrittenRuleGate();
            TestUnknownCardHasNoRequirement();
            TestRequirementBlocksPlay();
            Debug.Log("Card runtime requirement smoke tests passed.");
        }

        private static MigrationCardBuildRunController NewRun()
        {
            return new MigrationCardBuildRunController(
                new List<string> { "a", "b" }, bossMaxHp: 540, bossClauseId: "cirno_domain");
        }

        private static void TestBurnGate()
        {
            MigrationCardBuildRunController run = NewRun();
            // detonation needs burn on the enemy.
            AssertEqual("需要灼烧层数来引爆", run.RuntimeRequirementReason("mokou_payoff_fujiyama_burst"),
                "Detonation is gated on enemy burn.");
            run.State.ApplyStatus("enemy", "burn", 1);
            AssertEqual("", run.RuntimeRequirementReason("mokou_payoff_fujiyama_burst"),
                "With burn present the detonation gate clears.");
        }

        private static void TestVulnerabilityAndEmberGate()
        {
            MigrationCardBuildRunController run = NewRun();
            AssertEqual("需要破绽窗口和2点火种", run.RuntimeRequirementReason("fire_terminal_hourai_phoenix"),
                "The phoenix terminal needs a vulnerability window and 2 ember.");

            run.OpenVulnerability(2.0);
            run.State.AddResource("ember", 2);
            AssertEqual("", run.RuntimeRequirementReason("fire_terminal_hourai_phoenix"),
                "With the window open and 2 ember the phoenix gate clears.");
        }

        private static void TestSealAndRewrittenRuleGate()
        {
            MigrationCardBuildRunController run = NewRun();
            AssertEqual("需要3层封印或规则破解", run.RuntimeRequirementReason("mechanism_terminal_fantasy_verdict"),
                "Fantasy verdict needs 3 seal-or-rewritten.");

            run.State.AddResource("seal", 1);
            run.State.ApplyStatus("enemy", "seal", 1);
            run.RewrittenRuleCount = 1; // 1 + 1 + 1 = 3
            AssertEqual("", run.RuntimeRequirementReason("mechanism_terminal_fantasy_verdict"),
                "seal resource + enemy seal + rewritten rules sum to the requirement.");
        }

        private static void TestUnknownCardHasNoRequirement()
        {
            MigrationCardBuildRunController run = NewRun();
            AssertEqual("", run.RuntimeRequirementReason("mokou_starter_fire_bird"),
                "A card without a special requirement is never gated.");
        }

        private static void TestRequirementBlocksPlay()
        {
            MigrationCardBuildRunController run = NewRun();
            run.Deck.Draw(2, _ => 0); // hand: a, b -> but we need the gated card in hand
            // Build a run whose hand contains the gated card.
            MigrationCardBuildRunController gatedRun = new MigrationCardBuildRunController(
                new List<string> { "mokou_terminal_hourai_doll" }, bossMaxHp: 540, bossClauseId: "cirno_domain");
            gatedRun.Deck.Draw(1, _ => 0);

            CardPlayResult blocked = gatedRun.PlayCard("mokou_terminal_hourai_doll",
                new List<MigrationCardEffectBlock>());
            AssertEqual(false, blocked.Success, "A card failing its runtime requirement cannot be played.");
            AssertEqual("需要2点火种", blocked.Reason, "PlayCard surfaces the runtime-requirement reason.");

            gatedRun.State.AddResource("ember", 2);
            CardPlayResult ok = gatedRun.PlayCard("mokou_terminal_hourai_doll",
                new List<MigrationCardEffectBlock>());
            AssertEqual(true, ok.Success, "With the requirement met the card plays.");
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
