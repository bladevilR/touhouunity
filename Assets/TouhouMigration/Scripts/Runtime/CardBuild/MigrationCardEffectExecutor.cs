using System.Collections.Generic;

namespace TouhouMigration.Runtime.CardBuild
{
    // One card effect block (Godot card effect_blocks entry): a typed "type" + the fields the dispatch
    // reads. Amount is nullable so each type can fall back to its own default (e.g. spend = all).
    public sealed class MigrationCardEffectBlock
    {
        public string Type;
        public string Resource;
        public string Status;
        public string Target;
        public string ClauseId;
        public int? Amount;

        public static MigrationCardEffectBlock Create(string type)
        {
            return new MigrationCardEffectBlock { Type = type };
        }
    }

    // The card effect interpreter (Godot CardEffectExecutor): runs a card's effect blocks against the run
    // facade. The effect types whose target units are ported — resources, statuses, deck draw/discard/
    // exhaust/retain, clause reveal/seal — are applied; the rest (summon/install/field/partner/bullet/
    // mokou_*/damage/self_damage and unknowns) are collected and returned as ignored, pending the slices
    // that port their collections. Returns the list of ignored effect types (Godot logs "ignored_effect").
    public sealed class MigrationCardEffectExecutor
    {
        public IReadOnlyList<string> Execute(
            IEnumerable<MigrationCardEffectBlock> blocks,
            MigrationCardBuildRunController run,
            System.Func<int, int> randomIndex = null)
        {
            List<string> ignored = new List<string>();
            if (blocks == null || run == null)
            {
                return ignored;
            }

            System.Func<int, int> rng = randomIndex ?? (_ => 0);
            foreach (MigrationCardEffectBlock block in blocks)
            {
                if (block == null)
                {
                    continue;
                }

                if (!Apply(block, run, rng))
                {
                    ignored.Add(block.Type ?? string.Empty);
                }
            }

            return ignored;
        }

        // Apply one block. Returns false when its type isn't handled (so the caller records it as ignored).
        private static bool Apply(MigrationCardEffectBlock block, MigrationCardBuildRunController run, System.Func<int, int> rng)
        {
            switch (block.Type)
            {
                case "create_resource":
                    run.State.AddResource(block.Resource ?? string.Empty, block.Amount ?? 1);
                    return true;
                case "spend_resource":
                    run.State.SpendResource(block.Resource ?? string.Empty, block.Amount ?? -1);
                    return true;
                case "apply_status":
                    run.State.ApplyStatus(block.Target ?? "enemy", block.Status ?? string.Empty, block.Amount ?? 1);
                    return true;
                case "consume_status":
                    run.State.ConsumeStatus(block.Target ?? "enemy", block.Status ?? string.Empty, block.Amount ?? -1);
                    return true;
                case "draw":
                    run.Deck.Draw(block.Amount ?? 1, rng);
                    return true;
                case "discard":
                    run.Deck.DiscardFromHand(block.Amount ?? 1);
                    return true;
                case "exhaust":
                    run.Deck.ExhaustFromHand(block.Amount ?? 1);
                    return true;
                case "retain":
                    run.Deck.RetainFromHand(block.Amount ?? 1);
                    return true;
                case "reveal_clause":
                    run.Clauses.Reveal(block.ClauseId ?? string.Empty);
                    return true;
                case "seal_clause":
                    run.Clauses.Seal(block.ClauseId ?? string.Empty);
                    return true;
                default:
                    return false;
            }
        }
    }
}
