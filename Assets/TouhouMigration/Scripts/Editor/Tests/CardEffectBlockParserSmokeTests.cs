using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardEffectBlockParser: parsing cards.json effect_blocks into MigrationCardEffectBlock
    // lists (preserving absent fields via the dict-based MigrationJson) and running them through the engine.
    public static class CardEffectBlockParserSmokeTests
    {
        private const string CardsPath = "Assets/TouhouMigration/Data/CardBuild/cards.json";

        [MenuItem("Touhou Migration/Tests/Run Card Effect Block Parser Smoke Tests")]
        public static void RunAll()
        {
            TestParsesStarterCardBlocks();
            TestParsedBlocksDriveTheEngine();
            Debug.Log("Card effect block parser smoke tests passed.");
        }

        private static MigrationCardEffectBlockParser LoadParser()
        {
            MigrationCardEffectBlockParser parser = new MigrationCardEffectBlockParser();
            AssertEqual(true, parser.LoadFromPath(CardsPath),
                "cards.json should load. Errors: " + string.Join("; ", parser.Errors));
            return parser;
        }

        private static void TestParsesStarterCardBlocks()
        {
            MigrationCardEffectBlockParser parser = LoadParser();
            AssertEqual(true, parser.CardCount >= 10, "cards.json provides at least 10 cards with effect blocks.");

            IReadOnlyList<MigrationCardEffectBlock> blocks = parser.GetEffectBlocks("mokou_starter_fire_bird");
            AssertEqual(3, blocks.Count, "The starter card has three effect blocks.");

            MigrationCardEffectBlock terminal = blocks[0];
            AssertEqual("mokou_bind_terminal", terminal.Type, "The first block binds a terminal.");
            AssertEqual("T01", terminal.Id, "terminal_id parses into Id.");
            AssertEqual(65.0, terminal.BaseDamage, "base_damage parses.");
            AssertEqual(25.0, terminal.EnergyCost, "energy_cost parses.");
            AssertEqual(4, terminal.Flame, "flame parses.");

            MigrationCardEffectBlock status = blocks[1];
            AssertEqual("apply_status", status.Type, "The second block applies a status.");
            AssertEqual("burn", status.Status, "status parses.");
            AssertEqual(1, status.Amount, "amount parses.");

            AssertEqual("ember", blocks[2].Resource, "create_resource resource parses.");
            AssertEqual(0, parser.GetEffectBlocks("nonexistent_card").Count, "An unknown card has no blocks.");
        }

        private static void TestParsedBlocksDriveTheEngine()
        {
            MigrationCardEffectBlockParser parser = LoadParser();
            MigrationCardBuildRunController run = new MigrationCardBuildRunController(
                new List<string> { "a", "b" }, bossMaxHp: 540, bossClauseId: "cirno_domain");
            MigrationCardEffectExecutor executor = new MigrationCardEffectExecutor();

            IReadOnlyList<string> ignored = executor.Execute(parser.GetEffectBlocks("mokou_starter_fire_bird"), run);

            AssertEqual(0, ignored.Count, "Every starter-card effect block is handled by the engine.");
            AssertEqual(1, run.State.GetResource("ember"), "The card's create_resource block granted ember.");
            AssertEqual(1, run.State.GetStatus("enemy", "burn"), "The card's apply_status block applied burn.");

            // The bound terminal is releasable after a full charge.
            run.Mokou.BeginCharge();
            run.Mokou.AdvanceCharge(1.6);
            MokouReleaseResult result = run.Mokou.ReleaseCharge();
            AssertEqual(true, result.Success, "The card's mokou_bind_terminal bound a releasable terminal.");
            AssertEqual(4, result.Flame, "The terminal carries the parsed flame value.");
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
