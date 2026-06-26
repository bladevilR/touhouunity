using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardProgression: between-run card upgrades + relic effect-block collection (Godot
    // CardProgressionController apply_upgrade_to_card / apply_upgrades_to_cards / collect_relic_effect_blocks).
    public static class CardProgressionSmokeTests
    {
        private const double Tol = 1e-9;

        [MenuItem("Touhou Migration/Tests/Run Card Progression Smoke Tests")]
        public static void RunAll()
        {
            TestSetCooldownAndCharges();
            TestAppendAnswerTagsAndActivationMode();
            TestApplyUpgradesToTargets();
            TestCollectRelicEffectBlocks();
            Debug.Log("Card progression smoke tests passed.");
        }

        private static void TestSetCooldownAndCharges()
        {
            MigrationCardProgression prog = new MigrationCardProgression();
            MigrationUpgradeableCard card = new MigrationUpgradeableCard { Id = "c", Cooldown = 3.0, Charges = 1 };

            prog.ApplyUpgradeToCard(card, new MigrationCardUpgrade { Operation = "set_cooldown", Cooldown = 1.5 });
            AssertTrue(Math.Abs(1.5 - card.Cooldown) < Tol, "set_cooldown sets the cooldown.");

            prog.ApplyUpgradeToCard(card, new MigrationCardUpgrade { Operation = "set_cooldown", Cooldown = -2.0 });
            AssertTrue(Math.Abs(0.0 - card.Cooldown) < Tol, "A negative cooldown clamps to 0.");

            prog.ApplyUpgradeToCard(card, new MigrationCardUpgrade { Operation = "set_charges", Charges = 3 });
            AssertEqual(3, card.Charges, "set_charges sets the charges.");
            prog.ApplyUpgradeToCard(card, new MigrationCardUpgrade { Operation = "set_charges", Charges = 0 });
            AssertEqual(1, card.Charges, "Charges clamp to a minimum of 1.");
        }

        private static void TestAppendAnswerTagsAndActivationMode()
        {
            MigrationCardProgression prog = new MigrationCardProgression();
            MigrationUpgradeableCard card = new MigrationUpgradeableCard { Id = "c" };
            card.AnswerTags.Add("melt_terrain");

            MigrationCardUpgrade upgrade = new MigrationCardUpgrade { Operation = "append_answer_tags" };
            upgrade.AnswerTags.Add("field_replace");
            upgrade.AnswerTags.Add("melt_terrain"); // duplicate -> not re-added
            prog.ApplyUpgradeToCard(card, upgrade);

            AssertEqual(2, card.AnswerTags.Count, "append_answer_tags adds new tags and dedups.");

            prog.ApplyUpgradeToCard(card, new MigrationCardUpgrade { Operation = "set_activation_mode", ActivationMode = "passive" });
            AssertEqual("passive", card.ActivationMode, "set_activation_mode updates the mode.");
        }

        private static void TestApplyUpgradesToTargets()
        {
            MigrationCardProgression prog = new MigrationCardProgression();
            Dictionary<string, MigrationUpgradeableCard> cards = new Dictionary<string, MigrationUpgradeableCard>
            {
                ["fire_bird"] = new MigrationUpgradeableCard { Id = "fire_bird", Cooldown = 5.0 },
            };

            prog.ApplyUpgradesToCards(cards, new[]
            {
                new MigrationCardUpgrade { TargetCardId = "fire_bird", Operation = "set_cooldown", Cooldown = 2.0 },
                new MigrationCardUpgrade { TargetCardId = "absent_card", Operation = "set_cooldown", Cooldown = 9.0 },
            });

            AssertTrue(Math.Abs(2.0 - cards["fire_bird"].Cooldown) < Tol, "An upgrade applies to its target card.");
            AssertEqual(1, cards.Count, "An upgrade targeting an absent card is ignored (no card added).");
        }

        private static void TestCollectRelicEffectBlocks()
        {
            MigrationCardProgression prog = new MigrationCardProgression();
            MigrationRelic relicA = new MigrationRelic();
            relicA.EffectBlocks.Add(new MigrationCardEffectBlock { Type = "create_resource", Resource = "ember", Amount = 1 });
            MigrationRelic relicB = new MigrationRelic();
            relicB.EffectBlocks.Add(new MigrationCardEffectBlock { Type = "apply_status", Status = "burn", Amount = 1 });
            relicB.EffectBlocks.Add(new MigrationCardEffectBlock { Type = "draw", Amount = 1 });

            IReadOnlyList<MigrationCardEffectBlock> blocks = prog.CollectRelicEffectBlocks(new[] { relicA, relicB });
            AssertEqual(3, blocks.Count, "Relic effect blocks are flattened across relics.");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
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
