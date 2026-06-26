using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardBuildRunController.PlayCard: the generic play_card_by_id orchestration (Godot) —
    // the blocking reason (in-hand + cooldown + resource cost), running the effect blocks, moving the card
    // to discard, and putting it on cooldown. The bespoke per-card resolution is a later slice.
    public static class CardPlayOrchestrationSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Card Play Orchestration Smoke Tests")]
        public static void RunAll()
        {
            TestPlaySucceedsRunsEffectsDiscardsAndCools();
            TestBlockedWhenNotInHand();
            TestBlockedWhenOnCooldown();
            TestBlockedWhenResourceUnaffordable();
            Debug.Log("Card play orchestration smoke tests passed.");
        }

        private static MigrationCardBuildRunController NewRunWithHand()
        {
            MigrationCardBuildRunController run = new MigrationCardBuildRunController(
                new List<string> { "fire_bird", "embers", "flame_fist" }, bossMaxHp: 540, bossClauseId: "cirno_domain");
            run.Deck.Draw(3, _ => 0); // hand: fire_bird, embers, flame_fist
            return run;
        }

        private static List<MigrationCardEffectBlock> EmberBlock()
        {
            return new List<MigrationCardEffectBlock>
            {
                new MigrationCardEffectBlock { Type = "create_resource", Resource = "ember", Amount = 2 },
            };
        }

        private static void TestPlaySucceedsRunsEffectsDiscardsAndCools()
        {
            MigrationCardBuildRunController run = NewRunWithHand();

            CardPlayResult result = run.PlayCard("fire_bird", EmberBlock(), cost: null, cooldownTurns: 2);

            AssertEqual(true, result.Success, "A playable card in hand succeeds.");
            AssertEqual(2, run.State.GetResource("ember"), "The card's effect blocks ran.");
            AssertEqual(2, run.Deck.HandCount, "The played card left the hand.");
            AssertEqual(1, run.Deck.DiscardPileCount, "The played card went to the discard pile.");
            AssertEqual(true, run.Deck.IsOnCooldown("fire_bird"), "The played card is on cooldown.");
        }

        private static void TestBlockedWhenNotInHand()
        {
            MigrationCardBuildRunController run = NewRunWithHand();
            CardPlayResult result = run.PlayCard("not_drawn", EmberBlock());
            AssertEqual(false, result.Success, "A card not in hand cannot be played.");
            AssertEqual("not_in_hand", result.Reason, "The block reason is not_in_hand.");
            AssertEqual(0, run.State.GetResource("ember"), "A blocked play runs no effects.");
        }

        private static void TestBlockedWhenOnCooldown()
        {
            MigrationCardBuildRunController run = NewRunWithHand();
            run.PlayCard("fire_bird", EmberBlock(), cost: null, cooldownTurns: 2); // now on cooldown, discarded
            // Re-draw it: put a copy back in hand by drawing the reshuffle... simpler: it is in discard +
            // on cooldown; we assert that the cooldown gate fires even if it were in hand again.
            run.Deck.Draw(10, _ => 0); // refill hand from remaining/discard via reshuffle if needed

            // fire_bird is on cooldown; even if drawn back, playing it is blocked by cooldown.
            CardPlayResult result = run.PlayCard("fire_bird", EmberBlock());
            AssertEqual(false, result.Success, "A card on cooldown cannot be played.");
            AssertEqual("cooldown", result.Reason, "The block reason is cooldown.");
        }

        private static void TestBlockedWhenResourceUnaffordable()
        {
            MigrationCardBuildRunController run = NewRunWithHand();
            Dictionary<string, int> cost = new Dictionary<string, int> { ["ember"] = 3 };

            CardPlayResult result = run.PlayCard("fire_bird", EmberBlock(), cost);
            AssertEqual(false, result.Success, "A card whose cost exceeds the pool is blocked.");
            AssertEqual("resource:ember", result.Reason, "The block reason names the missing resource.");
            AssertEqual(3, run.Deck.HandCount, "A cost-blocked card stays in hand.");
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
