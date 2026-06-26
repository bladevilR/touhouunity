using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.CardBuild
{
    // The boss-domain contest (Godot CardBossDomainController): each domain accumulates contest progress
    // toward a threshold and breaks+seals once reached; contests can carry an answer-tag/family bonus, and
    // a separate pressure value ticks independently. UnityEngine-free + unit-testable.
    //
    // The actual Cirno domain data uses answer_tags + answer_families (each worth +1 on a match); Godot's
    // optional per-tag/per-family override-bonus dicts (answer_tag_bonus / answer_family_bonus) are unused
    // by the shipped data and are deferred.
    public sealed class MigrationCardBossDomains
    {
        private sealed class Domain
        {
            public int Threshold = 1;
            public int Pressure;
            public int Progress;
            public bool Broken;
            public bool Sealed;
            public readonly HashSet<string> AnswerTags = new HashSet<string>();
            public readonly HashSet<string> AnswerFamilies = new HashSet<string>();
        }

        private readonly Dictionary<string, Domain> domains = new Dictionary<string, Domain>();

        public void Install(string domainId, int threshold, int pressure = 0,
            IEnumerable<string> answerTags = null, IEnumerable<string> answerFamilies = null, int progress = 0)
        {
            if (string.IsNullOrEmpty(domainId))
            {
                return;
            }

            Domain domain = new Domain
            {
                Threshold = Math.Max(1, threshold),
                Pressure = pressure,
                Progress = Math.Max(0, progress),
            };
            AddAll(domain.AnswerTags, answerTags);
            AddAll(domain.AnswerFamilies, answerFamilies);

            if (domain.Progress >= domain.Threshold)
            {
                domain.Broken = true;
                domain.Sealed = true;
            }

            domains[domainId] = domain;
        }

        // Push contest progress against a domain (Godot contest_domain). Fails on an unknown or sealed
        // domain, or when the effective amount (raw + answer bonus) is not positive. Breaks + seals the
        // domain once progress reaches the threshold.
        public bool Contest(string domainId, int amount, string answerTag = "")
        {
            if (!TryGet(domainId, out Domain domain) || domain.Sealed)
            {
                return false;
            }

            int contestAmount = Math.Max(0, amount) + AnswerBonus(domain, answerTag);
            if (contestAmount <= 0)
            {
                return false;
            }

            domain.Progress = Math.Max(0, domain.Progress + contestAmount);
            if (domain.Progress >= domain.Threshold)
            {
                domain.Broken = true;
                domain.Sealed = true;
            }

            return true;
        }

        public void TickPressure(string domainId, int delta)
        {
            if (TryGet(domainId, out Domain domain))
            {
                domain.Pressure = Math.Max(0, domain.Pressure + delta);
            }
        }

        public int GetProgress(string domainId) => TryGet(domainId, out Domain domain) ? domain.Progress : 0;

        public int GetThreshold(string domainId) => TryGet(domainId, out Domain domain) ? domain.Threshold : 0;

        public int GetPressure(string domainId) => TryGet(domainId, out Domain domain) ? domain.Pressure : 0;

        public bool IsBroken(string domainId) => TryGet(domainId, out Domain domain) && domain.Broken;

        public bool IsSealed(string domainId) => TryGet(domainId, out Domain domain) && domain.Sealed;

        // Godot _get_answer_bonus (shipped-data subset): +1 for a matching answer tag, +1 for a matching
        // answer family (the tag's prefix before ':' or '/').
        private static int AnswerBonus(Domain domain, string answerTag)
        {
            if (string.IsNullOrEmpty(answerTag))
            {
                return 0;
            }

            int bonus = 0;
            if (domain.AnswerTags.Contains(answerTag))
            {
                bonus += 1;
            }

            string family = AnswerFamily(answerTag);
            if (family.Length > 0 && domain.AnswerFamilies.Contains(family))
            {
                bonus += 1;
            }

            return bonus;
        }

        private static string AnswerFamily(string answerTag)
        {
            int colon = answerTag.IndexOf(':');
            if (colon >= 0)
            {
                return answerTag.Substring(0, colon);
            }

            int slash = answerTag.IndexOf('/');
            if (slash >= 0)
            {
                return answerTag.Substring(0, slash);
            }

            return string.Empty;
        }

        private static void AddAll(HashSet<string> set, IEnumerable<string> values)
        {
            if (values == null)
            {
                return;
            }

            foreach (string value in values)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    set.Add(value);
                }
            }
        }

        private bool TryGet(string domainId, out Domain domain)
        {
            if (domainId != null)
            {
                return domains.TryGetValue(domainId, out domain);
            }

            domain = null;
            return false;
        }
    }
}
