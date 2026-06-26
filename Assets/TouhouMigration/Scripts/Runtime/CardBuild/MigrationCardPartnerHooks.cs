using System.Collections.Generic;

namespace TouhouMigration.Runtime.CardBuild
{
    // A registered partner hook (Godot CardPartnerHookController hook_data): fires its partner event when
    // an event with the matching trigger + filter occurs.
    public sealed class MigrationPartnerHook
    {
        public string Id = string.Empty;
        public string Trigger = string.Empty;
        public string PartnerEventId = string.Empty;
        public string RequiredTag;                       // the "tag" filter (event must carry this tag)
        public Dictionary<string, string> RequiredFields = new Dictionary<string, string>();
    }

    // An event that may fire partner hooks.
    public sealed class MigrationPartnerEventTrigger
    {
        public string Trigger = string.Empty;
        public HashSet<string> Tags = new HashSet<string>();
        public Dictionary<string, string> Fields = new Dictionary<string, string>();
    }

    // Partner-hook registration + event resolution (Godot CardPartnerHookController): hooks fire their
    // partner event when an event matches the hook's trigger and filter (a required tag and/or required
    // field values). UnityEngine-free + unit-testable.
    public sealed class MigrationCardPartnerHooks
    {
        private readonly List<MigrationPartnerHook> hooks = new List<MigrationPartnerHook>();
        private readonly List<string> firedPartnerEvents = new List<string>();

        public int HookCount => hooks.Count;
        public IReadOnlyList<string> FiredPartnerEvents => firedPartnerEvents;

        public void RegisterHook(MigrationPartnerHook hook)
        {
            if (hook != null)
            {
                hooks.Add(hook);
            }
        }

        // Resolve an event (Godot resolve_event): every hook whose trigger + filter match fires its partner
        // event. Returns the number of hooks that fired.
        public int ResolveEvent(MigrationPartnerEventTrigger trigger)
        {
            if (trigger == null)
            {
                return 0;
            }

            int fired = 0;
            foreach (MigrationPartnerHook hook in hooks)
            {
                if (hook.Trigger != trigger.Trigger || !MatchesFilter(hook, trigger))
                {
                    continue;
                }

                firedPartnerEvents.Add(hook.PartnerEventId);
                fired++;
            }

            return fired;
        }

        private static bool MatchesFilter(MigrationPartnerHook hook, MigrationPartnerEventTrigger trigger)
        {
            if (!string.IsNullOrEmpty(hook.RequiredTag) && !trigger.Tags.Contains(hook.RequiredTag))
            {
                return false;
            }

            if (hook.RequiredFields != null)
            {
                foreach (KeyValuePair<string, string> field in hook.RequiredFields)
                {
                    if (!trigger.Fields.TryGetValue(field.Key, out string value) || value != field.Value)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
