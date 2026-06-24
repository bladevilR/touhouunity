using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class MigrationPerfectFreezePhaseResult
    {
        public MigrationPerfectFreezePhaseResult(
            string reason,
            bool captured,
            float clearBonus,
            float captureBonus,
            float stunSeconds,
            int phaseIndex,
            int nextPhaseIndex,
            int phaseHitCount,
            int totalHitCount,
            int totalCaptureCount,
            float phaseElapsedSeconds,
            float phaseDurationSeconds)
        {
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            Captured = captured;
            ClearBonus = Mathf.Max(0f, clearBonus);
            CaptureBonus = Mathf.Max(0f, captureBonus);
            StunSeconds = Mathf.Max(0f, stunSeconds);
            PhaseIndex = Mathf.Max(0, phaseIndex);
            NextPhaseIndex = Mathf.Max(0, nextPhaseIndex);
            PhaseHitCount = Mathf.Max(0, phaseHitCount);
            TotalHitCount = Mathf.Max(0, totalHitCount);
            TotalCaptureCount = Mathf.Max(0, totalCaptureCount);
            PhaseElapsedSeconds = Mathf.Max(0f, phaseElapsedSeconds);
            PhaseDurationSeconds = Mathf.Max(0f, phaseDurationSeconds);
        }

        public string Reason { get; }
        public bool Captured { get; }
        public float ClearBonus { get; }
        public float CaptureBonus { get; }
        public float TotalBonus => ClearBonus + CaptureBonus;
        public float StunSeconds { get; }
        public int PhaseIndex { get; }
        public int NextPhaseIndex { get; }
        public int PhaseHitCount { get; }
        public int TotalHitCount { get; }
        public int TotalCaptureCount { get; }
        public float PhaseElapsedSeconds { get; }
        public float PhaseDurationSeconds { get; }
    }
}
