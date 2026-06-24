using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    [CreateAssetMenu(
        fileName = "MigrationPerfectFreezePhasePlan",
        menuName = "Touhou Migration/Combat/Perfect Freeze Phase Plan")]
    public sealed class MigrationPerfectFreezePhasePlan : ScriptableObject
    {
        [SerializeField] private float phaseMaxHp = 300f;
        [SerializeField] private float phaseDurationSeconds = 70f;
        [SerializeField] private float patternIntervalSeconds = 2.2f;
        [SerializeField] private int maxProjectilesPerCast = 18;
        [SerializeField] private float safeLaneHalfAngleDegrees = 22f;
        [SerializeField] private float safeLaneCueDurationSeconds = 1.05f;
        [SerializeField] private float clearBonus = 70f;
        [SerializeField] private float captureBonus = 100f;
        [SerializeField] private float clearStunSeconds = 3.5f;
        [SerializeField] private float captureStunSeconds = 4.5f;
        [SerializeField] private int orbSpreadProjectileCount = 11;
        [SerializeField] private float orbSpreadDegrees = 82f;
        [SerializeField] private int fieldRingCount = 2;
        [SerializeField] private int fieldBulletsPerRing = 12;
        [SerializeField] private int shardFanRowCount = 3;
        [SerializeField] private int shardFanColumnCount = 6;
        [SerializeField] private float shardFanSpreadDegrees = 68f;
        [SerializeField] private float closeRangeDistance = 4.2f;
        [SerializeField] private float iceLanceMinDistance = 12f;
        [SerializeField] private float snowballPreferredDistance = 8f;

        public float PhaseMaxHp => phaseMaxHp;
        public float PhaseDurationSeconds => phaseDurationSeconds;
        public float PatternIntervalSeconds => patternIntervalSeconds;
        public int MaxProjectilesPerCast => maxProjectilesPerCast;
        public float SafeLaneHalfAngleDegrees => safeLaneHalfAngleDegrees;
        public float SafeLaneCueDurationSeconds => safeLaneCueDurationSeconds;
        public float ClearBonus => clearBonus;
        public float CaptureBonus => captureBonus;
        public float ClearStunSeconds => clearStunSeconds;
        public float CaptureStunSeconds => captureStunSeconds;
        public int OrbSpreadProjectileCount => orbSpreadProjectileCount;
        public float OrbSpreadDegrees => orbSpreadDegrees;
        public int FieldRingCount => fieldRingCount;
        public int FieldBulletsPerRing => fieldBulletsPerRing;
        public int ShardFanRowCount => shardFanRowCount;
        public int ShardFanColumnCount => shardFanColumnCount;
        public float ShardFanSpreadDegrees => shardFanSpreadDegrees;
        public float CloseRangeDistance => closeRangeDistance;
        public float IceLanceMinDistance => iceLanceMinDistance;
        public float SnowballPreferredDistance => snowballPreferredDistance;

        public void ConfigurePhase(
            float phaseMaxHp,
            float phaseDurationSeconds,
            float patternIntervalSeconds,
            int maxProjectilesPerCast,
            float safeLaneHalfAngleDegrees,
            float safeLaneCueDurationSeconds)
        {
            this.phaseMaxHp = Mathf.Max(1f, phaseMaxHp);
            this.phaseDurationSeconds = Mathf.Max(0f, phaseDurationSeconds);
            this.patternIntervalSeconds = Mathf.Max(0.01f, patternIntervalSeconds);
            this.maxProjectilesPerCast = Mathf.Max(1, maxProjectilesPerCast);
            this.safeLaneHalfAngleDegrees = Mathf.Clamp(Mathf.Abs(safeLaneHalfAngleDegrees), 0f, 89f);
            this.safeLaneCueDurationSeconds = Mathf.Max(0f, safeLaneCueDurationSeconds);
        }

        public void ConfigureOutcomes(
            float clearBonus,
            float captureBonus,
            float clearStunSeconds,
            float captureStunSeconds)
        {
            this.clearBonus = Mathf.Max(0f, clearBonus);
            this.captureBonus = Mathf.Max(0f, captureBonus);
            this.clearStunSeconds = Mathf.Max(0f, clearStunSeconds);
            this.captureStunSeconds = Mathf.Max(0f, captureStunSeconds);
        }

        public void ConfigureCastPlan(
            int orbSpreadProjectileCount,
            float orbSpreadDegrees,
            int fieldRingCount,
            int fieldBulletsPerRing,
            int shardFanRowCount,
            int shardFanColumnCount,
            float shardFanSpreadDegrees,
            float iceLanceMinDistance = 12f,
            float snowballPreferredDistance = 8f,
            float closeRangeDistance = 4.2f)
        {
            this.orbSpreadProjectileCount = Mathf.Max(0, orbSpreadProjectileCount);
            this.orbSpreadDegrees = Mathf.Max(0f, orbSpreadDegrees);
            this.fieldRingCount = Mathf.Max(0, fieldRingCount);
            this.fieldBulletsPerRing = Mathf.Max(1, fieldBulletsPerRing);
            this.shardFanRowCount = Mathf.Max(0, shardFanRowCount);
            this.shardFanColumnCount = Mathf.Max(0, shardFanColumnCount);
            this.shardFanSpreadDegrees = Mathf.Max(0f, shardFanSpreadDegrees);
            this.iceLanceMinDistance = Mathf.Max(0f, iceLanceMinDistance);
            this.snowballPreferredDistance = Mathf.Max(0f, snowballPreferredDistance);
            this.closeRangeDistance = Mathf.Max(0f, closeRangeDistance);
        }
    }
}
