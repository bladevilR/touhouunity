using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.CardBuild
{
    // Runtime card deck for a CardBuild run: a draw pile, a hand, and a discard pile. Draw moves cards
    // from the top of the draw pile to the hand, reshuffling the discard pile back into the draw pile
    // (Fisher-Yates via an injected randomIndex) when the draw pile empties. Free of UnityEngine.
    public sealed class MigrationCardDeck
    {
        private readonly List<string> drawPile = new List<string>();
        private readonly List<string> hand = new List<string>();
        private readonly List<string> discardPile = new List<string>();

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
}
