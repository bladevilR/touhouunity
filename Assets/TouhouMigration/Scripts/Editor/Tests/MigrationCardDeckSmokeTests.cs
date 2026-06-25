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

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
