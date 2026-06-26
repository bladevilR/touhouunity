using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.CardBuild
{
    // The CardBuild run's resource + per-target status substrate (Godot CardBuildRuntimeState resource/
    // status methods) that card resolution reads and mutates: accumulating resource pools (ember/fate/
    // seal/...) and per-target status stacks (enemy/player -> burn/fate_lock/...). UnityEngine-free so it
    // stays unit-testable. The deck piles, field objects, boss clauses, and Mokou action chain are
    // separate concerns (MigrationCardDeck covers the piles; the rest are deferred boss-specific slices).
    public sealed class MigrationCardRunState
    {
        private readonly Dictionary<string, int> resources = new Dictionary<string, int>();
        private readonly Dictionary<string, Dictionary<string, int>> statuses =
            new Dictionary<string, Dictionary<string, int>>();

        // Accumulate a resource (Godot add_resource). Ignores an empty id or a zero amount.
        public void AddResource(string resourceId, int amount)
        {
            if (string.IsNullOrEmpty(resourceId) || amount == 0)
            {
                return;
            }

            resources[resourceId] = GetResource(resourceId) + amount;
        }

        // Spend up to `amount` of a resource (Godot spend_resource); a negative amount spends the whole
        // pool. Returns how much was actually spent (0 when empty or the pool is already depleted).
        public int SpendResource(string resourceId, int amount = -1)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                return 0;
            }

            int current = GetResource(resourceId);
            if (current <= 0)
            {
                return 0;
            }

            int spent = amount < 0 ? current : Math.Min(current, Math.Max(0, amount));
            resources[resourceId] = current - spent;
            return spent;
        }

        public int GetResource(string resourceId)
        {
            return resourceId != null && resources.TryGetValue(resourceId, out int value) ? value : 0;
        }

        public IReadOnlyDictionary<string, int> ResourcesSnapshot()
        {
            return new Dictionary<string, int>(resources);
        }

        // Add a status stack to a target (Godot apply_status). Ignores empty ids or a zero amount.
        public void ApplyStatus(string targetId, string statusId, int amount)
        {
            if (string.IsNullOrEmpty(targetId) || string.IsNullOrEmpty(statusId) || amount == 0)
            {
                return;
            }

            if (!statuses.TryGetValue(targetId, out Dictionary<string, int> targetStatuses))
            {
                targetStatuses = new Dictionary<string, int>();
                statuses[targetId] = targetStatuses;
            }

            targetStatuses[statusId] = (targetStatuses.TryGetValue(statusId, out int existing) ? existing : 0) + amount;
        }

        // Consume up to `amount` of a status stack (Godot consume_status); a negative amount consumes the
        // whole stack. The status entry is erased when it reaches zero. Returns the amount consumed.
        public int ConsumeStatus(string targetId, string statusId, int amount = -1)
        {
            if (string.IsNullOrEmpty(targetId) || string.IsNullOrEmpty(statusId)
                || !statuses.TryGetValue(targetId, out Dictionary<string, int> targetStatuses)
                || !targetStatuses.TryGetValue(statusId, out int current)
                || current <= 0)
            {
                return 0;
            }

            int consumed = amount < 0 ? current : Math.Min(current, Math.Max(0, amount));
            int remaining = current - consumed;
            if (remaining > 0)
            {
                targetStatuses[statusId] = remaining;
            }
            else
            {
                targetStatuses.Remove(statusId);
            }

            return consumed;
        }

        public int GetStatus(string targetId, string statusId)
        {
            return targetId != null && statusId != null
                && statuses.TryGetValue(targetId, out Dictionary<string, int> targetStatuses)
                && targetStatuses.TryGetValue(statusId, out int value)
                ? value
                : 0;
        }
    }
}
