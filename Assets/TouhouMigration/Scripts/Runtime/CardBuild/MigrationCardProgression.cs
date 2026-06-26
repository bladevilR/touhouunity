using System.Collections.Generic;

namespace TouhouMigration.Runtime.CardBuild
{
    // An upgradeable card's mutable fields (Godot card dict fields touched by card upgrades).
    public sealed class MigrationUpgradeableCard
    {
        public string Id = string.Empty;
        public double Cooldown;
        public int Charges = 1;
        public string ActivationMode = "tactical_hand";
        public List<string> AnswerTags = new List<string>();
    }

    // A between-run card upgrade (Godot upgrade dict).
    public sealed class MigrationCardUpgrade
    {
        public string TargetCardId = string.Empty;
        public string Operation = string.Empty;
        public double? Cooldown;
        public int? Charges;
        public string ActivationMode;
        public List<string> AnswerTags = new List<string>();
    }

    // A relic that contributes effect blocks (Godot relic dict).
    public sealed class MigrationRelic
    {
        public List<MigrationCardEffectBlock> EffectBlocks = new List<MigrationCardEffectBlock>();
    }

    // Between-run card progression (Godot CardProgressionController): apply card upgrades by operation
    // (set_cooldown / set_charges / append_answer_tags / set_activation_mode) and flatten relic effect
    // blocks. UnityEngine-free; composes the E6 effect-block model.
    public sealed class MigrationCardProgression
    {
        public void ApplyUpgradeToCard(MigrationUpgradeableCard card, MigrationCardUpgrade upgrade)
        {
            if (card == null || upgrade == null)
            {
                return;
            }

            switch (upgrade.Operation)
            {
                case "set_cooldown":
                    card.Cooldown = System.Math.Max(0.0, upgrade.Cooldown ?? card.Cooldown);
                    break;
                case "set_charges":
                    card.Charges = System.Math.Max(1, upgrade.Charges ?? card.Charges);
                    break;
                case "append_answer_tags":
                    if (upgrade.AnswerTags != null)
                    {
                        foreach (string tag in upgrade.AnswerTags)
                        {
                            if (!string.IsNullOrEmpty(tag) && !card.AnswerTags.Contains(tag))
                            {
                                card.AnswerTags.Add(tag);
                            }
                        }
                    }

                    break;
                case "set_activation_mode":
                    card.ActivationMode = upgrade.ActivationMode ?? card.ActivationMode ?? "tactical_hand";
                    break;
            }
        }

        public void ApplyUpgradesToCards(IDictionary<string, MigrationUpgradeableCard> cardsById, IEnumerable<MigrationCardUpgrade> upgrades)
        {
            if (cardsById == null || upgrades == null)
            {
                return;
            }

            foreach (MigrationCardUpgrade upgrade in upgrades)
            {
                if (upgrade == null || string.IsNullOrEmpty(upgrade.TargetCardId))
                {
                    continue;
                }

                if (cardsById.TryGetValue(upgrade.TargetCardId, out MigrationUpgradeableCard card))
                {
                    ApplyUpgradeToCard(card, upgrade);
                }
            }
        }

        public IReadOnlyList<MigrationCardEffectBlock> CollectRelicEffectBlocks(IEnumerable<MigrationRelic> relics)
        {
            List<MigrationCardEffectBlock> blocks = new List<MigrationCardEffectBlock>();
            if (relics == null)
            {
                return blocks;
            }

            foreach (MigrationRelic relic in relics)
            {
                if (relic?.EffectBlocks == null)
                {
                    continue;
                }

                foreach (MigrationCardEffectBlock block in relic.EffectBlocks)
                {
                    if (block != null)
                    {
                        blocks.Add(block);
                    }
                }
            }

            return blocks;
        }
    }
}
