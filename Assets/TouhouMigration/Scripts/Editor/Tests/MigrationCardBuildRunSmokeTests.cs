using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardBuildRun: seeding the runtime deck from a profile's active deck (mirroring
    // Godot CardBuildMvpRunController._apply_run_profile) and the per-turn cycle composed from the
    // CardDeckController primitives (EndTurn discards + ticks cooldowns; StartTurn returns retained + draws).
    public static class MigrationCardBuildRunSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Migration CardBuild Run Smoke Tests")]
        public static void RunAll()
        {
            TestSeedsOpeningHandFromActiveDeck();
            TestSetupDoesNotMutateProfileDeck();
            TestEndTurnDiscardsHandThenStartTurnDraws();
            TestRetainedCardSurvivesTurnBoundary();
            TestExhaustedCardLeavesTheRun();
            Debug.Log("Migration cardbuild run smoke tests passed.");
        }

        private static CardBuildProfile ProfileWithDeck(params string[] cards)
        {
            return CardBuildProfile.Create("fujiwara_no_mokou", cards, new Dictionary<string, string>());
        }

        private static void TestSeedsOpeningHandFromActiveDeck()
        {
            CardBuildProfile profile = ProfileWithDeck("c0", "c1", "c2", "c3", "c4", "c5", "c6", "c7");
            MigrationCardBuildRun run = new MigrationCardBuildRun(profile, 6);
            AssertEqual(6, run.HandCount, "The opening hand draws openingHandSize cards from the active deck.");
            AssertEqual(2, run.DrawPileCount, "The remainder of the active deck stays in the draw pile.");
            AssertEqual(0, run.DiscardPileCount, "The run starts with an empty discard pile.");
            AssertEqual("c0", run.Hand[0], "The opening hand is drawn from the front of the active deck.");
        }

        private static void TestSetupDoesNotMutateProfileDeck()
        {
            CardBuildProfile profile = ProfileWithDeck("c0", "c1", "c2", "c3");
            MigrationCardBuildRun run = new MigrationCardBuildRun(profile, 2);
            AssertEqual(2, run.HandCount, "Precondition: the opening hand draws two cards.");
            AssertEqual(4, profile.ActiveDeck.Count, "Seeding the run does not mutate the profile's active deck.");
        }

        private static void TestEndTurnDiscardsHandThenStartTurnDraws()
        {
            CardBuildProfile profile = ProfileWithDeck("c0", "c1", "c2", "c3", "c4", "c5", "c6", "c7");
            MigrationCardBuildRun run = new MigrationCardBuildRun(profile, 3); // hand c0,c1,c2 | draw c3..c7
            run.EndTurn();
            AssertEqual(0, run.HandCount, "EndTurn discards the whole hand.");
            AssertEqual(3, run.DiscardPileCount, "The discarded hand lands in the discard pile.");
            run.StartTurn(2, max => 0);
            AssertEqual(2, run.HandCount, "StartTurn draws the requested number of cards.");
            AssertEqual("c3", run.Hand[0], "StartTurn draws from the front of the remaining draw pile.");
        }

        private static void TestRetainedCardSurvivesTurnBoundary()
        {
            CardBuildProfile profile = ProfileWithDeck("c0", "c1", "c2", "c3");
            MigrationCardBuildRun run = new MigrationCardBuildRun(profile, 3); // hand c0,c1,c2 | draw c3
            AssertEqual(true, run.Retain("c1"), "A held card can be retained during the turn.");
            run.EndTurn(); // discards c0,c2; c1 is retained and survives
            AssertEqual(0, run.HandCount, "EndTurn discards only the non-retained hand.");
            AssertEqual(2, run.DiscardPileCount, "The two non-retained cards are discarded.");
            run.StartTurn(0, max => 0); // no draw; the retained card returns
            AssertEqual(1, run.HandCount, "The retained card returns to the hand at StartTurn.");
            AssertEqual("c1", run.Hand[0], "The returned card is the one that was retained.");
        }

        private static void TestExhaustedCardLeavesTheRun()
        {
            CardBuildProfile profile = ProfileWithDeck("c0", "c1", "c2", "c3");
            MigrationCardBuildRun run = new MigrationCardBuildRun(profile, 3); // hand c0,c1,c2 | draw c3
            AssertEqual(true, run.Exhaust("c2"), "A held card can be exhausted.");
            AssertEqual(1, run.ExhaustPileCount, "The exhausted card enters the exhaust pile.");
            AssertEqual(2, run.HandCount, "Exhausting removes the card from the hand.");
            run.EndTurn();
            AssertEqual(2, run.DiscardPileCount, "EndTurn discards the remaining hand; the exhausted card never reaches discard.");
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
