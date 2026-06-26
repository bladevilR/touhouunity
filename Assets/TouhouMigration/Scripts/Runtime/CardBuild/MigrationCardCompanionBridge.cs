using System.Collections.Generic;

namespace TouhouMigration.Runtime.CardBuild
{
    // A companion intent drained from the run (Godot CardCompanionBridge normalized intent).
    public sealed class MigrationCompanionIntent
    {
        public MigrationCompanionIntent(string action, string source)
        {
            Action = action;
            Source = source;
        }

        public string Action { get; }
        public string Source { get; }
    }

    // Drains newly-fired run partner events + bridge-queued intents into companion intents (Godot
    // CardCompanionBridge): collect is non-destructive (reads from a consumed cursor), consume advances the
    // cursors so each intent is delivered once. UnityEngine-free; composes the run facade's PartnerEvents.
    public sealed class MigrationCardCompanionBridge
    {
        private readonly List<string> queuedIntents = new List<string>();
        private int consumedPartnerCount;
        private int consumedQueueCount;

        public void QueueIntent(string action)
        {
            if (!string.IsNullOrEmpty(action))
            {
                queuedIntents.Add(action);
            }
        }

        public IReadOnlyList<MigrationCompanionIntent> CollectIntents(MigrationCardBuildRunController run)
        {
            List<MigrationCompanionIntent> intents = new List<MigrationCompanionIntent>();
            if (run?.PartnerEvents != null)
            {
                for (int i = consumedPartnerCount; i < run.PartnerEvents.Count; i++)
                {
                    intents.Add(new MigrationCompanionIntent(NormalizeAction(run.PartnerEvents[i]), "partner_event"));
                }
            }

            for (int i = consumedQueueCount; i < queuedIntents.Count; i++)
            {
                intents.Add(new MigrationCompanionIntent(queuedIntents[i], "bridge_queue"));
            }

            return intents;
        }

        public IReadOnlyList<MigrationCompanionIntent> ConsumeIntents(MigrationCardBuildRunController run)
        {
            IReadOnlyList<MigrationCompanionIntent> intents = CollectIntents(run);
            consumedPartnerCount = run?.PartnerEvents?.Count ?? consumedPartnerCount;
            consumedQueueCount = queuedIntents.Count;
            return intents;
        }

        private static string NormalizeAction(MigrationCardEffectBlock block)
        {
            if (block == null)
            {
                return string.Empty;
            }

            return !string.IsNullOrEmpty(block.Id) ? block.Id : block.Type ?? string.Empty;
        }
    }
}
