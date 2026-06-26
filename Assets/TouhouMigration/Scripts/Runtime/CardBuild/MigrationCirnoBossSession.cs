using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.CardBuild
{
    // A playable Cirno card-fight: wires the effect-block parser + run controller into a deck -> setup ->
    // draw -> play -> tick loop. This is the (UnityEngine-free, testable) orchestration a scene driver
    // (MonoBehaviour) sits on top of — build the session, StartFight, route card-play UI to
    // PlayCardFromHand, and Tick the run each frame.
    public sealed class MigrationCirnoBossSession
    {
        private readonly MigrationCardEffectBlockParser parser;

        public MigrationCirnoBossSession(MigrationCardEffectBlockParser parser, IEnumerable<string> deck)
        {
            this.parser = parser;
            Run = new MigrationCardBuildRunController(deck);
            Run.SetupCirnoRun();
        }

        public MigrationCardBuildRunController Run { get; }

        // Draw the opening hand (Godot run-profile opening draw).
        public void StartFight(int openingHandSize, Func<int, int> randomIndex = null)
        {
            Run.Deck.Draw(Math.Max(0, openingHandSize), randomIndex ?? (_ => 0));
        }

        // Play a held card, running its parsed effect blocks + bespoke resolution through the run.
        public CardPlayResult PlayCardFromHand(string cardId)
        {
            IReadOnlyList<MigrationCardEffectBlock> blocks = parser != null
                ? parser.GetEffectBlocks(cardId)
                : Array.Empty<MigrationCardEffectBlock>();
            return Run.PlayCard(cardId, blocks);
        }

        // Advance the run's real-time timers one frame.
        public void Tick(double deltaSeconds)
        {
            Run.TickVulnerability(deltaSeconds);
            Run.TickCardCooldowns(deltaSeconds);
            Run.TickTerrainSuppression(deltaSeconds);
        }
    }
}
