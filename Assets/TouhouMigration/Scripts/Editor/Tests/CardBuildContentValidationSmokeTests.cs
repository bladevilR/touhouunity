using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardBuildContentDatabase.Validate: cross-reference checks across the loaded cardbuild
    // content (Godot CardBuildDatabase validate_all) — upgrades target real cards with valid operations;
    // characters reference real archetypes.
    public static class CardBuildContentValidationSmokeTests
    {
        private const string Root = "Assets/TouhouMigration/Data/CardBuild/";

        [MenuItem("Touhou Migration/Tests/Run CardBuild Content Validation Smoke Tests")]
        public static void RunAll()
        {
            TestValidatorCatchesThePartialCardPort();
            TestMokouUpgradeValidatesAgainstItsRealCard();
            TestDanglingUpgradeTargetsAreFlagged();
            Debug.Log("CardBuild content validation smoke tests passed.");
        }

        private static MigrationCardBuildContentDatabase LoadAll()
        {
            MigrationCardBuildContentDatabase db = new MigrationCardBuildContentDatabase();
            db.LoadFromPaths(Root + "relics.json", Root + "upgrades.json");
            db.LoadDefinitions(Root + "resources.json", Root + "statuses.json");
            db.LoadBossRules(Root + "boss_rules.json");
            db.LoadArchetypesAndCharacters(Root + "archetypes.json", Root + "characters.json");
            return db;
        }

        // The validator surfaces a real, pre-existing data gap: Unity's cards.json is a Mokou-only subset, so
        // the two upgrades targeting un-ported mechanism cards correctly dangle. (Documents the partial card
        // port — see the handoff. The validator working IS the value here.)
        private static void TestValidatorCatchesThePartialCardPort()
        {
            MigrationCardEffectBlockParser cards = new MigrationCardEffectBlockParser();
            AssertEqual(true, cards.LoadFromPath(Root + "cards.json"), "cards.json loads.");

            MigrationCardBuildContentDatabase db = LoadAll();
            IReadOnlyList<string> errors = db.Validate(new HashSet<string>(cards.CardIds));
            AssertEqual(2, errors.Count, "Exactly the two mechanism-card upgrades dangle (Mokou-only card port). Errors: " + string.Join("; ", errors));
            AssertEqual(true, Contains(errors, "mechanism_terminal_fantasy_verdict"), "The verdict upgrade's missing card is named.");
            AssertEqual(true, Contains(errors, "mechanism_boss_clause_lock"), "The clause-lock upgrade's missing card is named.");
        }

        private static void TestMokouUpgradeValidatesAgainstItsRealCard()
        {
            MigrationCardEffectBlockParser cards = new MigrationCardEffectBlockParser();
            cards.LoadFromPath(Root + "cards.json");
            MigrationCardBuildContentDatabase db = LoadAll();
            IReadOnlyList<string> errors = db.Validate(new HashSet<string>(cards.CardIds));
            // The Mokou fire-bird upgrade targets a card that IS in the port -> it must not be flagged.
            AssertEqual(false, Contains(errors, "quickened_fire_bird"), "The Mokou fire-bird upgrade validates against its real card.");
        }

        private static bool Contains(IReadOnlyList<string> errors, string needle)
        {
            foreach (string e in errors)
            {
                if (e.Contains(needle))
                {
                    return true;
                }
            }

            return false;
        }

        private static void TestDanglingUpgradeTargetsAreFlagged()
        {
            MigrationCardBuildContentDatabase db = LoadAll();
            // No cards are "known" -> every upgrade's target_card_id dangles, so validation flags them.
            IReadOnlyList<string> errors = db.Validate(new HashSet<string>());
            AssertEqual(true, errors.Count > 0, "Upgrades targeting unknown cards are flagged.");
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
