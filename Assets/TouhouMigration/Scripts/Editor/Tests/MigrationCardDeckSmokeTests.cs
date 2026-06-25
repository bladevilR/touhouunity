using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class MigrationCardDeckSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Migration Card Deck Smoke Tests")]
        public static void RunAll()
        {
            TestDrawMovesCardsToHand();
            TestDrawTakesFromFrontLikeGodot();
            TestDiscardThenReshuffleOnEmptyDraw();
            TestDrawStopsWhenExhausted();
            TestDiscardFromHand();
            TestRetainFromHandMovesToRetainedPile();
            TestExhaustFromHandMovesToExhaustPile();
            TestMoveRetainedToHandReturnsCards();
            TestPutOnCooldownClampsToOneTurnThenReturnsToDiscard();
            TestTickCooldownsDecrementsMultiTurn();
            Debug.Log("Migration card deck smoke tests passed.");
        }

        private static void TestDrawMovesCardsToHand()
        {
            MigrationCardDeck deck = new MigrationCardDeck(new List<string> { "a", "b", "c" });
            AssertEqual(3, deck.DrawPileCount, "A new deck holds all cards in the draw pile.");
            AssertEqual(0, deck.HandCount, "A new deck has an empty hand.");

            int drawn = deck.Draw(2, max => 0);
            AssertEqual(2, drawn, "Draw returns the number of cards drawn.");
            AssertEqual(2, deck.HandCount, "Drawn cards land in the hand.");
            AssertEqual(1, deck.DrawPileCount, "Drawing reduces the draw pile.");
        }

        private static void TestDrawTakesFromFrontLikeGodot()
        {
            // Godot CardDeckController.draw() pops from the front of the deck, so the first card
            // listed is drawn first. Match that order for fidelity.
            MigrationCardDeck deck = new MigrationCardDeck(new List<string> { "first", "second", "third" });
            deck.Draw(1, max => 0);
            AssertEqual("first", deck.Hand[0], "Draw takes from the front of the deck (Godot pop_front order).");
        }

        private static void TestDiscardThenReshuffleOnEmptyDraw()
        {
            MigrationCardDeck deck = new MigrationCardDeck(new List<string> { "a", "b" });
            deck.Draw(2, max => 0);
            deck.DiscardHand();
            AssertEqual(2, deck.DiscardPileCount, "DiscardHand moves the hand to the discard pile.");
            AssertEqual(0, deck.DrawPileCount, "The draw pile is now empty.");

            int drawn = deck.Draw(1, max => 0);
            AssertEqual(1, drawn, "Drawing with an empty draw pile reshuffles the discard pile and draws.");
            AssertEqual(1, deck.HandCount, "The reshuffled card is in the hand.");
            AssertEqual(1, deck.DrawPileCount, "The remaining reshuffled card is back in the draw pile.");
            AssertEqual(0, deck.DiscardPileCount, "The discard pile was consumed by the reshuffle.");
        }

        private static void TestDrawStopsWhenExhausted()
        {
            MigrationCardDeck deck = new MigrationCardDeck(new List<string> { "a", "b", "c" });
            int drawn = deck.Draw(5, max => 0);
            AssertEqual(3, drawn, "Draw stops when both piles are exhausted.");
            AssertEqual(3, deck.HandCount, "All available cards were drawn into the hand.");
        }

        private static void TestDiscardFromHand()
        {
            MigrationCardDeck deck = new MigrationCardDeck(new List<string> { "a", "b" });
            deck.Draw(1, max => 0);
            string held = deck.Hand[0];
            AssertEqual(true, deck.DiscardFromHand(held), "A held card can be discarded.");
            AssertEqual(0, deck.HandCount, "Discarding removes the card from the hand.");
            AssertEqual(1, deck.DiscardPileCount, "The discarded card is in the discard pile.");
            AssertEqual(false, deck.DiscardFromHand("not_in_hand"), "Discarding a card not in hand fails.");
        }

        private static void TestRetainFromHandMovesToRetainedPile()
        {
            MigrationCardDeck deck = new MigrationCardDeck(new List<string> { "a", "b" });
            deck.Draw(2, max => 0);
            string held = deck.Hand[0];
            AssertEqual(true, deck.RetainFromHand(held), "A held card can be retained.");
            AssertEqual(1, deck.HandCount, "Retaining removes the card from the hand.");
            AssertEqual(1, deck.RetainedCount, "The retained card is in the retained pile.");
            AssertEqual(false, deck.RetainFromHand("not_in_hand"), "Retaining a card not in hand fails.");
        }

        private static void TestExhaustFromHandMovesToExhaustPile()
        {
            MigrationCardDeck deck = new MigrationCardDeck(new List<string> { "a", "b" });
            deck.Draw(2, max => 0);
            string held = deck.Hand[0];
            AssertEqual(true, deck.ExhaustFromHand(held), "A held card can be exhausted.");
            AssertEqual(1, deck.HandCount, "Exhausting removes the card from the hand.");
            AssertEqual(1, deck.ExhaustPileCount, "The exhausted card is in the exhaust pile.");
            AssertEqual(false, deck.ExhaustFromHand("not_in_hand"), "Exhausting a card not in hand fails.");
        }

        private static void TestMoveRetainedToHandReturnsCards()
        {
            MigrationCardDeck deck = new MigrationCardDeck(new List<string> { "a", "b" });
            deck.Draw(2, max => 0);
            deck.RetainFromHand(deck.Hand[0]);
            AssertEqual(1, deck.HandCount, "Precondition: one card remains after retaining one.");
            deck.MoveRetainedToHand();
            AssertEqual(2, deck.HandCount, "Retained cards move back into the hand.");
            AssertEqual(0, deck.RetainedCount, "The retained pile is emptied after moving to hand.");
        }

        private static void TestPutOnCooldownClampsToOneTurnThenReturnsToDiscard()
        {
            // Godot put_on_cooldown stores max(1, turns); a one-turn cooldown returns to discard on the next tick.
            MigrationCardDeck deck = new MigrationCardDeck(new List<string>());
            deck.PutOnCooldown("flare", 0);
            AssertEqual(1, deck.CooldownCount, "A card put on cooldown is tracked (turns clamped to >= 1).");
            deck.TickCooldowns();
            AssertEqual(0, deck.CooldownCount, "A one-turn cooldown clears after a tick.");
            AssertEqual(1, deck.DiscardPileCount, "An expired cooldown card returns to the discard pile.");
        }

        private static void TestTickCooldownsDecrementsMultiTurn()
        {
            MigrationCardDeck deck = new MigrationCardDeck(new List<string>());
            deck.PutOnCooldown("flare", 2);
            deck.TickCooldowns();
            AssertEqual(1, deck.CooldownCount, "A two-turn cooldown is still cooling after one tick.");
            AssertEqual(0, deck.DiscardPileCount, "A still-cooling card is not yet in the discard pile.");
            deck.TickCooldowns();
            AssertEqual(0, deck.CooldownCount, "The cooldown clears after the second tick.");
            AssertEqual(1, deck.DiscardPileCount, "The card returns to the discard pile when the cooldown expires.");
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
