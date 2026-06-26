using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCirnoBossSession: the playable Cirno fight that wires the effect-block parser + run
    // controller into a deck -> setup -> draw -> play -> tick loop end to end.
    public static class CirnoBossSessionSmokeTests
    {
        private const string CardsPath = "Assets/TouhouMigration/Data/CardBuild/cards.json";

        [MenuItem("Touhou Migration/Tests/Run Cirno Boss Session Smoke Tests")]
        public static void RunAll()
        {
            TestSessionSetsUpTheFight();
            TestPlayingACardRunsItsEffects();
            TestTickAdvancesTimers();
            Debug.Log("Cirno boss session smoke tests passed.");
        }

        private static MigrationCirnoBossSession NewSession(out MigrationCardEffectBlockParser parser)
        {
            parser = new MigrationCardEffectBlockParser();
            AssertEqual(true, parser.LoadFromPath(CardsPath), "cards.json should load. Errors: " + string.Join("; ", parser.Errors));
            List<string> deck = new List<string>
            {
                "mokou_resource_hourai_embers",
                "mokou_starter_fire_bird",
                "mokou_attack_flame_fist",
            };
            return new MigrationCirnoBossSession(parser, deck);
        }

        private static void TestSessionSetsUpTheFight()
        {
            MigrationCirnoBossSession session = NewSession(out _);
            AssertEqual(540, session.Run.BossHp, "The boss starts at full HP.");
            AssertEqual(true, session.Run.Clauses.IsExposed("terrain_tyranny"), "Setup installs the exposed Cirno clause.");

            session.StartFight(2, _ => 0);
            AssertEqual(2, session.Run.Deck.HandCount, "Starting the fight draws the opening hand.");
        }

        private static void TestPlayingACardRunsItsEffects()
        {
            MigrationCirnoBossSession session = NewSession(out _);
            session.StartFight(2, _ => 0); // hand: hourai_embers, fire_bird

            CardPlayResult result = session.PlayCardFromHand("mokou_resource_hourai_embers");
            AssertEqual(true, result.Success, "An in-hand card plays.");
            AssertEqual(2, session.Run.State.GetResource("ember"), "The card's create_resource block granted 2 ember (cards.json).");
            AssertEqual(1, session.Run.Deck.HandCount, "The played card left the hand.");
            AssertEqual(true, session.Run.IsCardOnCooldown("mokou_resource_hourai_embers"),
                "The played card is on its replay cooldown.");
        }

        private static void TestTickAdvancesTimers()
        {
            MigrationCirnoBossSession session = NewSession(out _);
            session.StartFight(2, _ => 0);
            session.Run.OpenVulnerability(2.0);
            AssertEqual(true, session.Run.IsVulnerabilityOpen, "Vulnerability is open before ticking.");

            session.Tick(3.0);
            AssertEqual(false, session.Run.IsVulnerabilityOpen, "Ticking drains the vulnerability window.");
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
