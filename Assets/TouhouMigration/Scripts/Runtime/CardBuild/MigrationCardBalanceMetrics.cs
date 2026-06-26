using System.Collections.Generic;

namespace TouhouMigration.Runtime.CardBuild
{
    // Card-run telemetry recorder (Godot CardBalanceMetrics): tallies actions, card plays, resource
    // overflow, vulnerability-window conversion, per-source damage, dead draws, and clause-break times, and
    // derives the per-minute / ratio summaries used for balance analysis. UnityEngine-free + unit-testable.
    public sealed class MigrationCardBalanceMetrics
    {
        private readonly Dictionary<string, double> damageBreakdown = new Dictionary<string, double>();
        private readonly Dictionary<string, int> deadDrawsByCard = new Dictionary<string, int>();
        private readonly Dictionary<string, double> clauseBreakTimes = new Dictionary<string, double>();

        public int ActionCount { get; private set; }
        public int CardPlayCount { get; private set; }
        public int WindowCount { get; private set; }
        public int ConvertedWindowCount { get; private set; }
        public int ResourceOverflowCount { get; private set; }

        public void StartRun()
        {
            ActionCount = 0;
            CardPlayCount = 0;
            WindowCount = 0;
            ConvertedWindowCount = 0;
            ResourceOverflowCount = 0;
            damageBreakdown.Clear();
            deadDrawsByCard.Clear();
            clauseBreakTimes.Clear();
        }

        public void RecordAction(string actionId, double timeSeconds) => ActionCount++;

        public void RecordCardPlay(string cardId, string activationMode, double timeSeconds) => CardPlayCount++;

        public void RecordResource(string resourceId, double before, double after, double cap = -1)
        {
            if (cap >= 0.0 && after > cap)
            {
                ResourceOverflowCount++;
            }
        }

        public void RecordWindow(double openTime, bool converted)
        {
            WindowCount++;
            if (converted)
            {
                ConvertedWindowCount++;
            }
        }

        public void RecordClauseBreak(string clauseId, double timeSeconds)
        {
            if (!string.IsNullOrEmpty(clauseId))
            {
                clauseBreakTimes[clauseId] = timeSeconds;
            }
        }

        public void RecordDamage(string sourceType, double amount)
        {
            if (string.IsNullOrEmpty(sourceType))
            {
                return;
            }

            damageBreakdown[sourceType] = (damageBreakdown.TryGetValue(sourceType, out double current) ? current : 0.0) + amount;
        }

        public void RecordDeadDraw(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                return;
            }

            deadDrawsByCard[cardId] = (deadDrawsByCard.TryGetValue(cardId, out int count) ? count : 0) + 1;
        }

        public double WindowConversionRate => Ratio(ConvertedWindowCount, WindowCount);

        public double DamageBySource(string sourceType)
        {
            return sourceType != null && damageBreakdown.TryGetValue(sourceType, out double amount) ? amount : 0.0;
        }

        public double TotalDamage
        {
            get
            {
                double total = 0.0;
                foreach (double amount in damageBreakdown.Values)
                {
                    total += amount;
                }

                return total;
            }
        }

        public double ClauseBreakTime(string clauseId)
        {
            return clauseId != null && clauseBreakTimes.TryGetValue(clauseId, out double time) ? time : 0.0;
        }

        public int DeadDrawCount(string cardId)
        {
            return cardId != null && deadDrawsByCard.TryGetValue(cardId, out int count) ? count : 0;
        }

        public double ActionsPerMinute(double durationSeconds) => RatePerMinute(ActionCount, durationSeconds);

        public double CardsPerMinute(double durationSeconds) => RatePerMinute(CardPlayCount, durationSeconds);

        private static double RatePerMinute(int count, double durationSeconds)
        {
            double durationMinutes = durationSeconds / 60.0;
            return durationMinutes <= 0.0 ? 0.0 : count / durationMinutes;
        }

        private static double Ratio(int numerator, int denominator)
        {
            return denominator <= 0 ? 0.0 : (double)numerator / denominator;
        }
    }
}
