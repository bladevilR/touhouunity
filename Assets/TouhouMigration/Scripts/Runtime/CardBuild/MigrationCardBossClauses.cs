using System.Collections.Generic;

namespace TouhouMigration.Runtime.CardBuild
{
    // The boss-clause lifecycle (Godot CardBossClauseController + CardBuildRuntimeState clause storage):
    // installed clauses move through revealed -> exposed -> sealed, can be temporarily disabled for N
    // turns, and can only be sealed once they are exposed and answered with a matching family of >= 2
    // cards. UnityEngine-free + unit-testable. (Godot splits the storage onto the run state and the rules
    // onto the controller; merged here since the split adds no value.)
    public sealed class MigrationCardBossClauses
    {
        private sealed class Clause
        {
            public bool Revealed;
            public bool Exposed;
            public bool Sealed;
            public int DisabledTurns;
            public readonly HashSet<string> AnswerFamilies = new HashSet<string>();
        }

        private readonly Dictionary<string, Clause> clauses = new Dictionary<string, Clause>();

        public void Install(string clauseId, IEnumerable<string> answerFamilies, bool revealed = false, bool exposed = false)
        {
            if (string.IsNullOrEmpty(clauseId))
            {
                return;
            }

            Clause clause = new Clause { Revealed = revealed, Exposed = exposed };
            if (answerFamilies != null)
            {
                foreach (string family in answerFamilies)
                {
                    if (!string.IsNullOrEmpty(family))
                    {
                        clause.AnswerFamilies.Add(family);
                    }
                }
            }

            clauses[clauseId] = clause;
        }

        public void Reveal(string clauseId)
        {
            if (TryGet(clauseId, out Clause clause))
            {
                clause.Revealed = true;
            }
        }

        public void Expose(string clauseId)
        {
            if (TryGet(clauseId, out Clause clause))
            {
                clause.Exposed = true;
            }
        }

        // Whether a clause can be sealed by the given answer (Godot can_seal_with_answer): the clause must
        // exist, be unsealed, be exposed, the answer must carry >= 2 cards, and its family must be one of
        // the clause's answer families.
        public bool CanSealWithAnswer(string clauseId, string family, int cardCount)
        {
            return TryGet(clauseId, out Clause clause)
                && !clause.Sealed
                && clause.Exposed
                && cardCount >= 2
                && family != null
                && clause.AnswerFamilies.Contains(family);
        }

        public bool SealWithAnswer(string clauseId, string family, int cardCount)
        {
            if (!CanSealWithAnswer(clauseId, family, cardCount))
            {
                return false;
            }

            clauses[clauseId].Sealed = true;
            return true;
        }

        public void Disable(string clauseId, int turns)
        {
            if (TryGet(clauseId, out Clause clause))
            {
                clause.DisabledTurns = turns < 1 ? 1 : turns;
            }
        }

        // Count every disabled clause down one turn (Godot tick_disabled_clauses).
        public void TickDisabled()
        {
            foreach (Clause clause in clauses.Values)
            {
                if (clause.DisabledTurns > 0)
                {
                    clause.DisabledTurns--;
                }
            }
        }

        public bool IsRevealed(string clauseId) => TryGet(clauseId, out Clause clause) && clause.Revealed;

        public bool IsExposed(string clauseId) => TryGet(clauseId, out Clause clause) && clause.Exposed;

        public bool IsSealed(string clauseId) => TryGet(clauseId, out Clause clause) && clause.Sealed;

        public bool IsDisabled(string clauseId) => TryGet(clauseId, out Clause clause) && clause.DisabledTurns > 0;

        private bool TryGet(string clauseId, out Clause clause)
        {
            if (clauseId != null)
            {
                return clauses.TryGetValue(clauseId, out clause);
            }

            clause = null;
            return false;
        }
    }
}
