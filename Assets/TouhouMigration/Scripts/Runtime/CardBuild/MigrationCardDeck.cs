using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.CardBuild
{
    // Runtime card deck for a CardBuild run: draw / hand / discard piles plus retained, exhaust, and
    // cooldown tracks (mirroring Godot CardDeckController). Draw moves cards from the top of the draw pile
    // to the hand, reshuffling the discard pile back into the draw pile (Fisher-Yates via an injected
    // randomIndex) when the draw pile empties. Free of UnityEngine.
    public sealed class MigrationCardDeck
    {
        private readonly List<string> drawPile = new List<string>();
        private readonly List<string> hand = new List<string>();
        private readonly List<string> discardPile = new List<string>();
        private readonly List<string> retainedCards = new List<string>();
        private readonly List<string> exhaustPile = new List<string>();
        private readonly List<(string Card, int Turns)> cooldownCards = new List<(string, int)>();

        public MigrationCardDeck(IEnumerable<string> cards)
        {
            if (cards == null)
            {
                return;
            }

            foreach (string card in cards)
            {
                if (!string.IsNullOrWhiteSpace(card))
                {
                    drawPile.Add(card);
                }
            }
        }

        public int DrawPileCount => drawPile.Count;
        public int HandCount => hand.Count;
        public int DiscardPileCount => discardPile.Count;
        public IReadOnlyList<string> Hand => hand;

        // Draw up to 'count' cards into the hand, reshuffling the discard pile into the draw pile when
        // the draw pile empties. randomIndex(maxExclusive) -> [0, maxExclusive). Returns the count drawn.
        public int Draw(int count, Func<int, int> randomIndex)
        {
            int drawn = 0;
            for (int i = 0; i < count; i++)
            {
                if (drawPile.Count == 0)
                {
                    if (discardPile.Count == 0)
                    {
                        break;
                    }

                    ReshuffleDiscardIntoDraw(randomIndex);
                }

                // Draw from the front of the deck to match Godot CardDeckController.draw (pop_front).
                string card = drawPile[0];
                drawPile.RemoveAt(0);
                hand.Add(card);
                drawn++;
            }

            return drawn;
        }

        public bool DiscardFromHand(string cardId)
        {
            int index = hand.IndexOf(cardId);
            if (index < 0)
            {
                return false;
            }

            hand.RemoveAt(index);
            discardPile.Add(cardId);
            return true;
        }

        public void DiscardHand()
        {
            discardPile.AddRange(hand);
            hand.Clear();
        }

        // Count-based hand moves (Godot CardBuildRuntimeState discard/exhaust/retain(amount)): pop the last
        // 'count' cards off the hand into the matching pile, stopping early if the hand empties. Returns
        // the number actually moved.
        public int DiscardFromHand(int count) => MoveFromHandBack(count, discardPile);

        public int ExhaustFromHand(int count) => MoveFromHandBack(count, exhaustPile);

        public int RetainFromHand(int count) => MoveFromHandBack(count, retainedCards);

        private int MoveFromHandBack(int count, List<string> destination)
        {
            int moved = 0;
            for (int i = 0; i < count && hand.Count > 0; i++)
            {
                int last = hand.Count - 1;
                destination.Add(hand[last]);
                hand.RemoveAt(last);
                moved++;
            }

            return moved;
        }

        public int RetainedCount => retainedCards.Count;
        public int ExhaustPileCount => exhaustPile.Count;
        public int CooldownCount => cooldownCards.Count;

        // Whether a card is currently held out of play on cooldown (Godot get_card_cooldown > 0).
        public bool IsOnCooldown(string cardId)
        {
            foreach ((string card, int _) in cooldownCards)
            {
                if (card == cardId)
                {
                    return true;
                }
            }

            return false;
        }

        // Move a held card to the retained pile (Godot CardDeckController.retain_from_hand). Retained cards
        // survive the end-of-turn discard and are returned to the hand by MoveRetainedToHand.
        public bool RetainFromHand(string cardId)
        {
            int index = hand.IndexOf(cardId);
            if (index < 0)
            {
                return false;
            }

            hand.RemoveAt(index);
            retainedCards.Add(cardId);
            return true;
        }

        // Move a held card to the exhaust pile (Godot CardDeckController.exhaust_from_hand). Exhausted cards
        // leave the run: they never reshuffle back into the draw pile.
        public bool ExhaustFromHand(string cardId)
        {
            int index = hand.IndexOf(cardId);
            if (index < 0)
            {
                return false;
            }

            hand.RemoveAt(index);
            exhaustPile.Add(cardId);
            return true;
        }

        // Return all retained cards to the hand (Godot CardDeckController.move_retained_to_hand), e.g. at the
        // start of the next turn.
        public void MoveRetainedToHand()
        {
            hand.AddRange(retainedCards);
            retainedCards.Clear();
        }

        // Put a card on cooldown for max(1, turns) turns (Godot CardDeckController.put_on_cooldown). The card
        // is held out of play until TickCooldowns counts it down to zero, then it returns to the discard pile.
        public void PutOnCooldown(string cardId, int turns)
        {
            cooldownCards.Add((cardId, Math.Max(1, turns)));
        }

        // Count every cooldown down by one turn (Godot CardDeckController.tick_cooldowns). Cards whose cooldown
        // reaches zero return to the discard pile; the rest stay on cooldown with their reduced count.
        public void TickCooldowns()
        {
            List<(string Card, int Turns)> remaining = new List<(string, int)>();
            foreach ((string card, int turns) in cooldownCards)
            {
                int next = turns - 1;
                if (next <= 0)
                {
                    discardPile.Add(card);
                }
                else
                {
                    remaining.Add((card, next));
                }
            }

            cooldownCards.Clear();
            cooldownCards.AddRange(remaining);
        }

        // Snapshot every pile for a card-run save.
        public CardDeckSnapshot CreateSnapshot()
        {
            CardDeckSnapshot snapshot = new CardDeckSnapshot
            {
                drawPile = new List<string>(drawPile),
                hand = new List<string>(hand),
                discardPile = new List<string>(discardPile),
                retainedCards = new List<string>(retainedCards),
                exhaustPile = new List<string>(exhaustPile),
            };
            foreach ((string card, int turns) in cooldownCards)
            {
                snapshot.cooldownCardIds.Add(card);
                snapshot.cooldownTurns.Add(turns);
            }

            return snapshot;
        }

        public void LoadSnapshot(CardDeckSnapshot snapshot)
        {
            drawPile.Clear();
            hand.Clear();
            discardPile.Clear();
            retainedCards.Clear();
            exhaustPile.Clear();
            cooldownCards.Clear();
            if (snapshot == null)
            {
                return;
            }

            if (snapshot.drawPile != null) drawPile.AddRange(snapshot.drawPile);
            if (snapshot.hand != null) hand.AddRange(snapshot.hand);
            if (snapshot.discardPile != null) discardPile.AddRange(snapshot.discardPile);
            if (snapshot.retainedCards != null) retainedCards.AddRange(snapshot.retainedCards);
            if (snapshot.exhaustPile != null) exhaustPile.AddRange(snapshot.exhaustPile);

            if (snapshot.cooldownCardIds != null && snapshot.cooldownTurns != null)
            {
                int count = Math.Min(snapshot.cooldownCardIds.Count, snapshot.cooldownTurns.Count);
                for (int i = 0; i < count; i++)
                {
                    cooldownCards.Add((snapshot.cooldownCardIds[i], snapshot.cooldownTurns[i]));
                }
            }
        }

        private void ReshuffleDiscardIntoDraw(Func<int, int> randomIndex)
        {
            drawPile.AddRange(discardPile);
            discardPile.Clear();

            if (randomIndex == null)
            {
                return;
            }

            for (int i = drawPile.Count - 1; i > 0; i--)
            {
                int bound = i + 1;
                int j = ((randomIndex(bound) % bound) + bound) % bound;
                (drawPile[i], drawPile[j]) = (drawPile[j], drawPile[i]);
            }
        }
    }

    // Persisted card-deck piles (Godot CardBuildRuntimeState deck/hand/discard/retained/exhaust/cooldown).
    [Serializable]
    public sealed class CardDeckSnapshot
    {
        public List<string> drawPile = new List<string>();
        public List<string> hand = new List<string>();
        public List<string> discardPile = new List<string>();
        public List<string> retainedCards = new List<string>();
        public List<string> exhaustPile = new List<string>();
        public List<string> cooldownCardIds = new List<string>();
        public List<int> cooldownTurns = new List<int>();
    }
}
