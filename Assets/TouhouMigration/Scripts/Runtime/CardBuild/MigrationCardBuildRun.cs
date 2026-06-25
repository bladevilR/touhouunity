using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.CardBuild
{
    // A CardBuild run's runtime deck state, seeded from a profile's active deck. Mirrors the layered-profile
    // setup in Godot CardBuildMvpRunController._apply_run_profile: the active deck becomes the draw pile and an
    // opening hand is drawn from the front, leaving the remainder in the draw pile and an empty discard.
    //
    // The per-turn cycle composes the CardDeckController primitives (via MigrationCardDeck):
    //   - EndTurn discards the still-held hand and counts every cooldown down by one turn (expired cards
    //     return to discard). Cards retained during the turn are already set aside and survive.
    //   - StartTurn returns the retained cards to the hand and draws the requested number of cards,
    //     reshuffling the discard pile back into the draw pile when the draw pile empties.
    // Free of UnityEngine.
    public sealed class MigrationCardBuildRun
    {
        private readonly MigrationCardDeck deck;

        public MigrationCardBuildRun(CardBuildProfile profile, int openingHandSize)
        {
            IReadOnlyList<string> activeDeck = profile != null ? profile.ActiveDeck : Array.Empty<string>();
            deck = new MigrationCardDeck(activeDeck);

            // The opening hand is deterministic (drawn from the front of a full draw pile, no reshuffle), so it
            // does not consume the run's RNG; later StartTurn draws may reshuffle and do.
            deck.Draw(Math.Max(0, openingHandSize), max => 0);
        }

        public IReadOnlyList<string> Hand => deck.Hand;
        public int HandCount => deck.HandCount;
        public int DrawPileCount => deck.DrawPileCount;
        public int DiscardPileCount => deck.DiscardPileCount;
        public int RetainedCount => deck.RetainedCount;
        public int ExhaustPileCount => deck.ExhaustPileCount;
        public int CooldownCount => deck.CooldownCount;

        // Set a held card aside so it survives the end-of-turn discard (returns to hand next StartTurn).
        public bool Retain(string cardId) => deck.RetainFromHand(cardId);

        // Move a held card to the discard pile (e.g. after it is played).
        public bool Discard(string cardId) => deck.DiscardFromHand(cardId);

        // Remove a held card from the run permanently (never reshuffles back).
        public bool Exhaust(string cardId) => deck.ExhaustFromHand(cardId);

        // Put a card on cooldown for max(1, turns) turns; it returns to discard when the cooldown expires.
        public void Cooldown(string cardId, int turns) => deck.PutOnCooldown(cardId, turns);

        // End-of-turn upkeep: discard the still-held hand, then count every cooldown down by one turn.
        public void EndTurn()
        {
            deck.DiscardHand();
            deck.TickCooldowns();
        }

        // Start-of-turn: return retained cards to the hand, then draw the requested number of cards.
        public void StartTurn(int drawCount, Func<int, int> randomIndex)
        {
            deck.MoveRetainedToHand();
            deck.Draw(Math.Max(0, drawCount), randomIndex);
        }
    }
}
