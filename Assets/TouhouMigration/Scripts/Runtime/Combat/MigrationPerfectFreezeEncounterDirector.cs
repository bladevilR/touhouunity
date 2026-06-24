using System;
using System.Collections.Generic;
using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    [DisallowMultipleComponent]
    public sealed class MigrationPerfectFreezeEncounterDirector : MonoBehaviour
    {
        [SerializeField] private MigrationEnemyProjectile projectilePrefab;
        [SerializeField] private MigrationEnemyProjectile iceOrbProjectilePrefab;
        [SerializeField] private MigrationEnemyProjectile iceShardProjectilePrefab;
        [SerializeField] private MigrationEnemyProjectile iceLanceProjectilePrefab;
        [SerializeField] private MigrationProjectileSpecialSettlement scopedSettlement;
        [SerializeField] private MigrationSimpleEnemyController bossController;
        [SerializeField] private MigrationCombatTargetBehaviour bossTarget;
        [SerializeField] private MigrationPerfectFreezeStaggerAdapter staggerAdapter;
        [SerializeField] private MigrationPerfectFreezeSafeLaneCue safeLaneCue;
        [SerializeField] private MigrationPerfectFreezeSnowballHazard snowballHazard;
        [SerializeField] private MigrationPerfectFreezePhasePlan phasePlan;
        [SerializeField] private MigrationPrefabPoolService projectilePool;
        [SerializeField] private int activeProjectileCap = 80;
        [SerializeField] private int burstProjectileCount = 12;
        [SerializeField] private float burstRadius = 4f;
        [SerializeField] private float spawnHeight = 1.2f;
        [SerializeField] private float phaseMaxHp = 300f;
        [SerializeField] private float phaseDurationSeconds = 70f;
        [SerializeField] private float phaseClearBonus = 70f;
        [SerializeField] private float phaseCaptureBonus = 100f;
        [SerializeField] private float phaseClearStunSeconds = 3.5f;
        [SerializeField] private float phaseCaptureStunSeconds = 4.5f;
        [SerializeField] private float patternIntervalSeconds = 2.2f;
        [SerializeField] private int maxProjectilesPerCast = 18;
        [SerializeField] private float safeLaneHalfAngleDegrees = 22f;
        [SerializeField] private float safeLaneCueDurationSeconds = 1.05f;
        [SerializeField] private Color safeLaneCueColor = new Color(1f, 0.54f, 0.18f, 0.3f);
        [SerializeField] private int perfectFreezeOrbSpreadProjectileCount = 11;
        [SerializeField] private float perfectFreezeOrbSpreadDegrees = 82f;
        [SerializeField] private int perfectFreezeFieldRingCount = 2;
        [SerializeField] private int perfectFreezeFieldBulletsPerRing = 12;
        [SerializeField] private int iceShardFanRowCount = 3;
        [SerializeField] private int iceShardFanColumnCount = 6;
        [SerializeField] private float iceShardFanSpreadDegrees = 68f;
        [SerializeField] private float closeRangeDistance = 4.2f;
        [SerializeField] private float iceLanceMinDistance = 12f;
        [SerializeField] private float snowballPreferredDistance = 8f;
        [SerializeField] private float snowballPushOffset = 1.35f;
        [SerializeField] private float closeEvadeBackDistance = 2.6f;
        [SerializeField] private float closeEvadeSideDistance = 1.75f;
        [SerializeField] private float bossMovementLerp = 2.2f;
        [SerializeField] private bool snowballPressureActive;

        private const float IceOrbSpeed = 6.5f;
        private const float IceOrbDamage = 8f;
        private const float IceOrbHitRadius = 0.4f;
        private const float IceShardBaseSpeed = 10.5f;
        private const float IceShardRowSpeedStep = 0.5f;
        private const float IceShardDamage = 12f;
        private const float IceShardHitRadius = 0.52f;
        private const float IceShardRowHeightStep = 0.45f;
        private const float IceLanceSpeed = 22.5f;
        private const float IceLanceDamage = 16f;
        private const float IceLanceHitRadius = 0.24f;
        private const float IceLanceForwardOffset = 0.55f;
        private const float PerfectFreezeFieldHitRadius = 0.46f;
        private const string PerfectFreezeFieldPatternKind = "perfect_freeze_field";
        private const string IceShardFanPatternKind = "ice_shard_fan";
        private const string IceLanceSnipePatternKind = "ice_lance_snipe";
        private const string SnowballRollPatternKind = "snowball_roll";
        private const string SnowballPressurePatternKind = "snowball_pressure";
        private const string BossMovementIdleKind = "idle_hover";
        private const string BossMovementSnowballPushKind = "snowball_push_position";
        private const string BossMovementCloseEvadeKind = "evade_close";

        private readonly List<MigrationEnemyProjectile> activeProjectiles = new();
        private float phaseElapsedSeconds;
        private float patternCooldownSeconds;
        private MigrationCombatTargetBehaviour subscribedBossTarget;
        private int phaseHitCount;
        private int totalPlayerHitCount;
        private int totalCaptureCount;

        public event Action<MigrationPerfectFreezePhaseResult> PhaseFinished;

        public MigrationEnemyProjectile ProjectilePrefab => projectilePrefab;
        public MigrationEnemyProjectile IceOrbProjectilePrefab => iceOrbProjectilePrefab;
        public MigrationEnemyProjectile IceShardProjectilePrefab => iceShardProjectilePrefab;
        public MigrationEnemyProjectile IceLanceProjectilePrefab => iceLanceProjectilePrefab;
        public MigrationProjectileSpecialSettlement ScopedSettlement => ResolveScopedSettlement();
        public MigrationSimpleEnemyController BossController => ResolveBossController();
        public MigrationCombatTargetBehaviour BossTarget => ResolveBossTarget();
        public MigrationPerfectFreezeStaggerAdapter StaggerAdapter => ResolveStaggerAdapter();
        public MigrationPerfectFreezeSafeLaneCue SafeLaneCue => ResolveSafeLaneCue();
        public MigrationPerfectFreezeSnowballHazard SnowballHazard => ResolveSnowballHazard();
        public MigrationPerfectFreezePhasePlan PhasePlan => phasePlan;
        public MigrationPrefabPoolService ProjectilePool => ResolveProjectilePool();
        public bool HasProjectilePrefab => projectilePrefab != null;
        public bool HasIceOrbProjectilePrefab => iceOrbProjectilePrefab != null;
        public bool HasIceShardProjectilePrefab => iceShardProjectilePrefab != null;
        public bool HasIceLanceProjectilePrefab => iceLanceProjectilePrefab != null;
        public bool HasScopedSettlement => ResolveScopedSettlement() != null;
        public bool HasBossController => ResolveBossController() != null;
        public bool HasBossTarget => ResolveBossTarget() != null;
        public bool HasStaggerAdapter => ResolveStaggerAdapter() != null;
        public bool HasSafeLaneCue => ResolveSafeLaneCue() != null;
        public bool HasSnowballHazard => ResolveSnowballHazard() != null;
        public bool HasPhasePlan => phasePlan != null;
        public bool HasProjectilePool => ResolveProjectilePool() != null;
        public int ActiveProjectileCap => activeProjectileCap;
        public int BurstProjectileCount => burstProjectileCount;
        public float BurstRadius => burstRadius;
        public float SpawnHeight => spawnHeight;
        public float PhaseMaxHp => phaseMaxHp;
        public float PhaseDurationSeconds => phaseDurationSeconds;
        public float PhaseClearBonus => phaseClearBonus;
        public float PhaseCaptureBonus => phaseCaptureBonus;
        public float PhaseClearStunSeconds => phaseClearStunSeconds;
        public float PhaseCaptureStunSeconds => phaseCaptureStunSeconds;
        public float PhaseElapsedSeconds => phaseElapsedSeconds;
        public float PatternIntervalSeconds => patternIntervalSeconds;
        public int MaxProjectilesPerCast => maxProjectilesPerCast;
        public float SafeLaneHalfAngleDegrees => safeLaneHalfAngleDegrees;
        public float SafeLaneCueDurationSeconds => safeLaneCueDurationSeconds;
        public int PerfectFreezeOrbSpreadProjectileCount => perfectFreezeOrbSpreadProjectileCount;
        public float PerfectFreezeOrbSpreadDegrees => perfectFreezeOrbSpreadDegrees;
        public int PerfectFreezeFieldRingCount => perfectFreezeFieldRingCount;
        public int PerfectFreezeFieldBulletsPerRing => perfectFreezeFieldBulletsPerRing;
        public int IceShardFanRowCount => iceShardFanRowCount;
        public int IceShardFanColumnCount => iceShardFanColumnCount;
        public float IceShardFanSpreadDegrees => iceShardFanSpreadDegrees;
        public float CloseRangeDistance => closeRangeDistance;
        public float IceLanceMinDistance => iceLanceMinDistance;
        public float SnowballPreferredDistance => snowballPreferredDistance;
        public float SnowballPushOffset => snowballPushOffset;
        public float CloseEvadeBackDistance => closeEvadeBackDistance;
        public float CloseEvadeSideDistance => closeEvadeSideDistance;
        public float BossMovementLerp => bossMovementLerp;
        public bool SnowballPressureActive => snowballPressureActive;
        public bool PhaseActive { get; private set; }
        public bool CaptureEligible => PhaseActive && phaseHitCount == 0;
        public int PatternCastEventCount { get; private set; }
        public int LastCastProjectileCount { get; private set; }
        public int PhaseFinishedEventCount { get; private set; }
        public int PhaseHitCount => phaseHitCount;
        public int TotalPlayerHitCount => totalPlayerHitCount;
        public int TotalCaptureCount => totalCaptureCount;
        public MigrationPerfectFreezePhaseResult LastPhaseResult { get; private set; }
        public int LastCastIndex { get; private set; } = -1;
        public string LastCastPatternKind { get; private set; } = string.Empty;
        public int LastAuthoredProjectileCount { get; private set; }
        public int LastCastOrbProjectileCount { get; private set; }
        public int LastCastPerfectFreezeProjectileCount { get; private set; }
        public int LastCastIceShardProjectileCount { get; private set; }
        public int LastCastIceLanceProjectileCount { get; private set; }
        public int LastAuthoredPerfectFreezeProjectileCount { get; private set; }
        public int LastAuthoredIceShardProjectileCount { get; private set; }
        public string LastBossMovementIntentKind { get; private set; } = BossMovementIdleKind;
        public Vector3 LastDesiredBossPosition { get; private set; }
        public int ActiveProjectileCount
        {
            get
            {
                PruneActiveProjectiles();
                return activeProjectiles.Count;
            }
        }

        public MigrationEnemyProjectile LastSpawnedProjectile { get; private set; }

        public void BindProjectilePrefab(MigrationEnemyProjectile prefab)
        {
            projectilePrefab = prefab;
        }

        public void BindIceOrbProjectilePrefab(MigrationEnemyProjectile prefab)
        {
            iceOrbProjectilePrefab = prefab;
        }

        public void BindIceShardProjectilePrefab(MigrationEnemyProjectile prefab)
        {
            iceShardProjectilePrefab = prefab;
        }

        public void BindIceLanceProjectilePrefab(MigrationEnemyProjectile prefab)
        {
            iceLanceProjectilePrefab = prefab;
        }

        public void SetSnowballPressureActive(bool active)
        {
            snowballPressureActive = active;
            if (snowballPressureActive)
            {
                ResolveSafeLaneCue()?.HideCue();
            }
        }

        public void BindScopedSettlement(MigrationProjectileSpecialSettlement settlement)
        {
            scopedSettlement = settlement;
            if (scopedSettlement != null)
            {
                scopedSettlement.ConfigureSharedSettlementFallback(false);
            }

            WireStaggerAdapter();
        }

        public void BindBossController(MigrationSimpleEnemyController controller)
        {
            bossController = controller;
            if (bossController != null && bossTarget == null)
            {
                bossTarget = bossController.GetComponent<MigrationCombatTargetBehaviour>();
            }

            WireStaggerAdapter();
        }

        public void BindBossTarget(MigrationCombatTargetBehaviour target)
        {
            UnsubscribeBossTargetEvents();
            bossTarget = target;
            SubscribeBossTargetEvents();
        }

        public void BindStaggerAdapter(MigrationPerfectFreezeStaggerAdapter adapter)
        {
            staggerAdapter = adapter;
            WireStaggerAdapter();
        }

        public void BindSafeLaneCue(MigrationPerfectFreezeSafeLaneCue cue)
        {
            safeLaneCue = cue;
            ConfigureSafeLaneCue();
        }

        public void BindSnowballHazard(MigrationPerfectFreezeSnowballHazard hazard)
        {
            snowballHazard = hazard;
            if (snowballHazard != null && snowballHazard.EncounterDirector != this)
            {
                snowballHazard.BindEncounterDirector(this);
            }
        }

        public void BindProjectilePool(MigrationPrefabPoolService pool)
        {
            projectilePool = pool;
        }

        public void BindPhasePlan(MigrationPerfectFreezePhasePlan plan)
        {
            phasePlan = plan;
            ApplyPhasePlan();
        }

        public void ApplyPhasePlan()
        {
            if (phasePlan == null)
            {
                return;
            }

            ConfigurePhase(
                phasePlan.PhaseMaxHp,
                phasePlan.PhaseDurationSeconds,
                phasePlan.PatternIntervalSeconds,
                phasePlan.MaxProjectilesPerCast,
                phasePlan.SafeLaneHalfAngleDegrees,
                phasePlan.SafeLaneCueDurationSeconds);
            ConfigurePhaseOutcomes(
                phasePlan.ClearBonus,
                phasePlan.CaptureBonus,
                phasePlan.ClearStunSeconds,
                phasePlan.CaptureStunSeconds);
            ConfigurePerfectFreezeCastPlan(
                phasePlan.OrbSpreadProjectileCount,
                phasePlan.OrbSpreadDegrees,
                phasePlan.FieldRingCount,
                phasePlan.FieldBulletsPerRing,
                phasePlan.ShardFanRowCount,
                phasePlan.ShardFanColumnCount,
                phasePlan.ShardFanSpreadDegrees,
                phasePlan.IceLanceMinDistance,
                phasePlan.SnowballPreferredDistance,
                phasePlan.CloseRangeDistance);
        }

        public void ConfigurePattern(int activeProjectileCap, int burstProjectileCount, float burstRadius, float spawnHeight)
        {
            this.activeProjectileCap = Mathf.Max(1, activeProjectileCap);
            this.burstProjectileCount = Mathf.Max(1, burstProjectileCount);
            this.burstRadius = Mathf.Max(0f, burstRadius);
            this.spawnHeight = Mathf.Max(0f, spawnHeight);
        }

        public void ConfigurePerfectFreezeCastPlan(
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
            perfectFreezeOrbSpreadProjectileCount = Mathf.Max(0, orbSpreadProjectileCount);
            perfectFreezeOrbSpreadDegrees = Mathf.Max(0f, orbSpreadDegrees);
            perfectFreezeFieldRingCount = Mathf.Max(0, fieldRingCount);
            perfectFreezeFieldBulletsPerRing = Mathf.Max(1, fieldBulletsPerRing);
            iceShardFanRowCount = Mathf.Max(0, shardFanRowCount);
            iceShardFanColumnCount = Mathf.Max(0, shardFanColumnCount);
            iceShardFanSpreadDegrees = Mathf.Max(0f, shardFanSpreadDegrees);
            this.iceLanceMinDistance = Mathf.Max(0f, iceLanceMinDistance);
            this.snowballPreferredDistance = Mathf.Max(0f, snowballPreferredDistance);
            this.closeRangeDistance = Mathf.Max(0f, closeRangeDistance);
        }

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
            ConfigureSafeLaneCue();
        }

        public void ConfigurePhaseOutcomes(
            float clearBonus,
            float captureBonus,
            float clearStunSeconds,
            float captureStunSeconds)
        {
            phaseClearBonus = Mathf.Max(0f, clearBonus);
            phaseCaptureBonus = Mathf.Max(0f, captureBonus);
            phaseClearStunSeconds = Mathf.Max(0f, clearStunSeconds);
            phaseCaptureStunSeconds = Mathf.Max(0f, captureStunSeconds);
        }

        public void ConfigureBossMovement(
            float snowballPushOffset,
            float closeEvadeBackDistance,
            float closeEvadeSideDistance,
            float bossMovementLerp)
        {
            this.snowballPushOffset = Mathf.Max(0f, snowballPushOffset);
            this.closeEvadeBackDistance = Mathf.Max(0f, closeEvadeBackDistance);
            this.closeEvadeSideDistance = Mathf.Max(0f, closeEvadeSideDistance);
            this.bossMovementLerp = Mathf.Max(0f, bossMovementLerp);
        }

        public void BeginPerfectFreezePhase(Vector3 center, Vector3 playerPosition)
        {
            ApplyPhasePlan();
            MigrationCombatTargetBehaviour target = ResolveBossTarget();
            target?.Initialize(phaseMaxHp);
            SubscribeBossTargetEvents();
            ResolveScopedSettlement();
            ResolveSafeLaneCue();
            PhaseActive = true;
            phaseElapsedSeconds = 0f;
            patternCooldownSeconds = 0f;
            phaseHitCount = 0;
            totalPlayerHitCount = 0;
            totalCaptureCount = 0;
            PhaseFinishedEventCount = 0;
            LastPhaseResult = null;
            PatternCastEventCount = 0;
            LastCastProjectileCount = 0;
            LastCastIndex = -1;
            LastCastPatternKind = string.Empty;
            LastAuthoredProjectileCount = 0;
            LastCastOrbProjectileCount = 0;
            LastCastPerfectFreezeProjectileCount = 0;
            LastCastIceShardProjectileCount = 0;
            LastCastIceLanceProjectileCount = 0;
            LastAuthoredPerfectFreezeProjectileCount = 0;
            LastAuthoredIceShardProjectileCount = 0;
            LastBossMovementIntentKind = BossMovementIdleKind;
            LastDesiredBossPosition = ResolveBossTransformPosition(center);
            CastPerfectFreezePattern(center, playerPosition);
        }

        public void RegisterPlayerHit()
        {
            if (!PhaseActive)
            {
                return;
            }

            phaseHitCount++;
            totalPlayerHitCount++;
        }

        public void TickPhase(float deltaTime, Vector3 center, Vector3 playerPosition)
        {
            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            ResolveSafeLaneCue()?.Tick(safeDeltaTime);
            if (!PhaseActive)
            {
                return;
            }

            UpdateBossMovement(safeDeltaTime, center, playerPosition);
            phaseElapsedSeconds += safeDeltaTime;
            if (phaseDurationSeconds > 0f && phaseElapsedSeconds >= phaseDurationSeconds - 0.0001f)
            {
                FinishPhase("timeout");
                return;
            }

            patternCooldownSeconds -= safeDeltaTime;
            if (patternCooldownSeconds <= 0.0001f)
            {
                CastPerfectFreezePattern(center, playerPosition);
            }
        }

        public int SpawnPerfectFreezeBurst(Vector3 center, Vector3 playerPosition)
        {
            return SpawnPerfectFreezeProjectiles(center, playerPosition, burstProjectileCount);
        }

        public MigrationEnemyProjectile[] GetActiveProjectilesSnapshot()
        {
            PruneActiveProjectiles();
            return activeProjectiles.ToArray();
        }

        private void Awake()
        {
            ApplyPhasePlan();
            ResolveScopedSettlement();
            ResolveBossController();
            ResolveBossTarget();
            ResolveStaggerAdapter();
            ResolveSafeLaneCue();
            ResolveSnowballHazard();
            WireStaggerAdapter();
        }

        private void OnDisable()
        {
            UnsubscribeBossTargetEvents();
        }

        private void Update()
        {
            if (!PhaseActive)
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                TickPhase(Time.deltaTime, transform.position, player.transform.position);
            }
        }

        private int SpawnPerfectFreezeProjectiles(Vector3 center, Vector3 playerPosition, int requestedCount)
        {
            PruneActiveProjectiles();
            if (projectilePrefab == null)
            {
                return 0;
            }

            MigrationProjectileSpecialSettlement settlement = ResolveScopedSettlement();
            int spawnCount = Mathf.Min(Mathf.Max(0, requestedCount), Mathf.Max(0, activeProjectileCap - activeProjectiles.Count));
            for (int index = 0; index < spawnCount; index++)
            {
                float angleRadians = spawnCount <= 1
                    ? 0f
                    : (Mathf.PI * 2f * index) / spawnCount;
                Vector3 radial = new Vector3(Mathf.Cos(angleRadians), 0f, Mathf.Sin(angleRadians));
                Vector3 spawnPosition = center + radial * burstRadius + Vector3.up * spawnHeight;
                Vector3 direction = playerPosition - spawnPosition;
                if (direction.sqrMagnitude <= 0.0001f)
                {
                    direction = -radial;
                }

                GameObject projectileObject = CheckoutProjectileObject(
                    projectilePrefab,
                    "MigrationPerfectFreezeEncounterProjectile",
                    spawnPosition);
                projectileObject.name = "MigrationPerfectFreezeEncounterProjectile";

                MigrationEnemyProjectile projectile = projectileObject.GetComponent<MigrationEnemyProjectile>();
                if (projectile == null)
                {
                    projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
                }

                projectile.Configure(projectilePrefab.Speed, projectilePrefab.Damage, direction, true);
                MigrationCombatFeedbackTemplate template = projectileObject.GetComponent<MigrationCombatFeedbackTemplate>();
                if (template != null)
                {
                    projectile.ApplyFeedbackTemplate(template);
                }

                BindProjectileSettlement(projectileObject, projectile, settlement);
                activeProjectiles.Add(projectile);
                LastSpawnedProjectile = projectile;
            }

            return spawnCount;
        }

        private void CastPerfectFreezePattern(Vector3 center, Vector3 playerPosition)
        {
            PruneActiveProjectiles();
            LastCastIndex = PatternCastEventCount;
            LastCastProjectileCount = 0;
            LastCastOrbProjectileCount = 0;
            LastCastPerfectFreezeProjectileCount = 0;
            LastCastIceShardProjectileCount = 0;
            LastCastIceLanceProjectileCount = 0;
            LastAuthoredPerfectFreezeProjectileCount = 0;
            LastAuthoredIceShardProjectileCount = 0;

            float distanceToPlayer = FlatDistance(center, playerPosition);
            MigrationPerfectFreezeSnowballHazard resolvedSnowball = ResolveSnowballHazard();
            if (resolvedSnowball != null && resolvedSnowball.IsActive && !snowballPressureActive)
            {
                SetSnowballPressureActive(true);
            }

            if (snowballPressureActive)
            {
                LastCastPatternKind = SnowballPressurePatternKind;
                LastAuthoredProjectileCount = 0;
                LastCastProjectileCount = 0;
                ResolveSafeLaneCue()?.HideCue();
                PatternCastEventCount++;
                patternCooldownSeconds = patternIntervalSeconds;
                return;
            }

            if (ShouldCastIceLanceSnipe(distanceToPlayer))
            {
                LastCastPatternKind = IceLanceSnipePatternKind;
                LastAuthoredProjectileCount = 1;
                LastCastIceLanceProjectileCount = SpawnIceLanceSnipe(center, playerPosition);
                LastCastProjectileCount = LastCastIceLanceProjectileCount;
                PatternCastEventCount++;
                patternCooldownSeconds = patternIntervalSeconds;
                return;
            }

            if (ShouldCastSnowballRoll(distanceToPlayer, resolvedSnowball))
            {
                LastCastPatternKind = SnowballRollPatternKind;
                LastAuthoredProjectileCount = 1;
                LastCastProjectileCount = 0;
                ResolveSafeLaneCue()?.HideCue();
                float growthSeed = 1f + Mathf.Max(0, LastCastIndex % 3) * 0.22f;
                resolvedSnowball.BeginRolling(center, playerPosition, growthSeed);
                PatternCastEventCount++;
                patternCooldownSeconds = patternIntervalSeconds;
                return;
            }

            bool fieldCast = LastCastIndex % 2 == 0;
            LastCastPatternKind = fieldCast ? PerfectFreezeFieldPatternKind : IceShardFanPatternKind;
            LastAuthoredProjectileCount = perfectFreezeOrbSpreadProjectileCount;

            int remainingCastBudget = Mathf.Max(0, maxProjectilesPerCast);
            LastCastOrbProjectileCount = SpawnIceOrbSpread(center, playerPosition, remainingCastBudget);
            remainingCastBudget -= LastCastOrbProjectileCount;

            if (fieldCast)
            {
                LastAuthoredPerfectFreezeProjectileCount = CountPerfectFreezeFieldCandidates(center, playerPosition);
                LastAuthoredProjectileCount += LastAuthoredPerfectFreezeProjectileCount;
                MigrationPerfectFreezeSafeLaneCue cue = ResolveSafeLaneCue();
                if (cue != null)
                {
                    ConfigureSafeLaneCue();
                    cue.ShowCue(center, playerPosition);
                }

                LastCastPerfectFreezeProjectileCount = SpawnPerfectFreezeField(center, playerPosition, remainingCastBudget);
                remainingCastBudget -= LastCastPerfectFreezeProjectileCount;
            }
            else
            {
                LastAuthoredIceShardProjectileCount = iceShardFanRowCount * iceShardFanColumnCount;
                LastAuthoredProjectileCount += LastAuthoredIceShardProjectileCount;
                LastCastIceShardProjectileCount = SpawnIceShardFan(center, playerPosition, remainingCastBudget);
                remainingCastBudget -= LastCastIceShardProjectileCount;
            }

            LastCastProjectileCount =
                LastCastOrbProjectileCount +
                LastCastPerfectFreezeProjectileCount +
                LastCastIceShardProjectileCount +
                LastCastIceLanceProjectileCount;

            PatternCastEventCount++;
            patternCooldownSeconds = patternIntervalSeconds;
        }

        private void FinishPhase(string reason)
        {
            if (!PhaseActive)
            {
                return;
            }

            bool isClear = string.Equals(reason, "clear", StringComparison.Ordinal);
            bool captured = isClear && phaseHitCount == 0;
            if (captured)
            {
                totalCaptureCount++;
            }

            LastPhaseResult = new MigrationPerfectFreezePhaseResult(
                reason,
                captured,
                isClear ? phaseClearBonus : 0f,
                captured ? phaseCaptureBonus : 0f,
                isClear ? (captured ? phaseCaptureStunSeconds : phaseClearStunSeconds) : 0f,
                0,
                1,
                phaseHitCount,
                totalPlayerHitCount,
                totalCaptureCount,
                phaseElapsedSeconds,
                phaseDurationSeconds);

            PhaseActive = false;
            patternCooldownSeconds = 0f;
            ResolveSafeLaneCue()?.HideCue();
            PhaseFinishedEventCount++;
            PhaseFinished?.Invoke(LastPhaseResult);
        }

        private int SpawnIceOrbSpread(Vector3 center, Vector3 playerPosition, int remainingCastBudget)
        {
            int budget = Mathf.Min(remainingCastBudget, RemainingActiveProjectileCapacity());
            if (budget <= 0 || iceOrbProjectilePrefab == null)
            {
                return 0;
            }

            Vector3 forward = FlatDirectionTo(center, playerPosition, Vector3.forward);
            int requestedCount = Mathf.Min(perfectFreezeOrbSpreadProjectileCount, budget);
            int spawned = 0;
            float halfSpread = perfectFreezeOrbSpreadDegrees * 0.5f;
            for (int index = 0; index < requestedCount; index++)
            {
                float t = perfectFreezeOrbSpreadProjectileCount <= 1
                    ? 0.5f
                    : (float)index / (perfectFreezeOrbSpreadProjectileCount - 1);
                float yawDegrees = Mathf.Lerp(-halfSpread, halfSpread, t);
                Vector3 direction = RotateYaw(forward, yawDegrees);
                Vector3 spawnPosition = center + Vector3.up * spawnHeight;
                if (SpawnProjectile(
                    iceOrbProjectilePrefab,
                    "MigrationPerfectFreezeIceOrbProjectile",
                    spawnPosition,
                    direction,
                    IceOrbSpeed,
                    IceOrbDamage,
                    IceOrbHitRadius))
                {
                    spawned++;
                }
            }

            return spawned;
        }

        private int SpawnPerfectFreezeField(Vector3 center, Vector3 playerPosition, int remainingCastBudget)
        {
            int budget = Mathf.Min(remainingCastBudget, RemainingActiveProjectileCapacity());
            if (budget <= 0 || projectilePrefab == null)
            {
                return 0;
            }

            Vector3 safeDirection = FlatDirectionTo(center, playerPosition, Vector3.forward);
            int bulletsPerRing = Mathf.Clamp(perfectFreezeFieldBulletsPerRing, 1, 10);
            int spawned = 0;
            for (int ring = 0; ring < perfectFreezeFieldRingCount && spawned < budget; ring++)
            {
                float radius = 2.5f + ring * 1.8f;
                for (int index = 0; index < bulletsPerRing && spawned < budget; index++)
                {
                    float angleRadians = Mathf.PI * 2f * index / Mathf.Max(1, bulletsPerRing);
                    Vector3 radial = new Vector3(Mathf.Cos(angleRadians), 0f, Mathf.Sin(angleRadians)).normalized;
                    if (FlatAngleBetween(radial, safeDirection) < safeLaneHalfAngleDegrees)
                    {
                        continue;
                    }

                    Vector3 spawnPosition = center + radial * radius + Vector3.up * spawnHeight;
                    if (SpawnProjectile(
                        projectilePrefab,
                        "MigrationPerfectFreezeFieldProjectile",
                        spawnPosition,
                        radial,
                        projectilePrefab.Speed,
                        projectilePrefab.Damage,
                        PerfectFreezeFieldHitRadius))
                    {
                        spawned++;
                    }
                }
            }

            return spawned;
        }

        private int SpawnIceShardFan(Vector3 center, Vector3 playerPosition, int remainingCastBudget)
        {
            int budget = Mathf.Min(remainingCastBudget, RemainingActiveProjectileCapacity());
            if (budget <= 0 || iceShardProjectilePrefab == null)
            {
                return 0;
            }

            Vector3 forward = FlatDirectionTo(center, playerPosition, Vector3.forward);
            int spawned = 0;
            float halfSpread = iceShardFanSpreadDegrees * 0.5f;
            for (int row = 0; row < iceShardFanRowCount && spawned < budget; row++)
            {
                float speed = IceShardBaseSpeed + row * IceShardRowSpeedStep;
                for (int column = 0; column < iceShardFanColumnCount && spawned < budget; column++)
                {
                    float t = iceShardFanColumnCount <= 1
                        ? 0.5f
                        : (float)column / (iceShardFanColumnCount - 1);
                    float yawDegrees = Mathf.Lerp(-halfSpread, halfSpread, t);
                    Vector3 direction = RotateYaw(forward, yawDegrees);
                    Vector3 spawnPosition = center + Vector3.up * (spawnHeight + row * IceShardRowHeightStep);
                    if (SpawnProjectile(
                        iceShardProjectilePrefab,
                        "MigrationPerfectFreezeIceShardProjectile",
                        spawnPosition,
                        direction,
                        speed,
                        IceShardDamage,
                        IceShardHitRadius))
                    {
                        spawned++;
                    }
                }
            }

            return spawned;
        }

        private int SpawnIceLanceSnipe(Vector3 center, Vector3 playerPosition)
        {
            int budget = Mathf.Min(1, Mathf.Min(RemainingActiveProjectileCapacity(), maxProjectilesPerCast));
            if (budget <= 0 || iceLanceProjectilePrefab == null)
            {
                return 0;
            }

            Vector3 forward = FlatDirectionTo(center, playerPosition, Vector3.forward);
            Vector3 spawnPosition = center + Vector3.up * spawnHeight + forward * IceLanceForwardOffset;
            return SpawnProjectile(
                iceLanceProjectilePrefab,
                "MigrationPerfectFreezeIceLanceProjectile",
                spawnPosition,
                forward,
                IceLanceSpeed,
                IceLanceDamage,
                IceLanceHitRadius)
                ? 1
                : 0;
        }

        private bool SpawnProjectile(
            MigrationEnemyProjectile prefab,
            string objectName,
            Vector3 spawnPosition,
            Vector3 direction,
            float speed,
            float damage,
            float hitRadius)
        {
            if (prefab == null || RemainingActiveProjectileCapacity() <= 0)
            {
                return false;
            }

            MigrationProjectileSpecialSettlement settlement = ResolveScopedSettlement();
            GameObject projectileObject = CheckoutProjectileObject(prefab, objectName, spawnPosition);

            MigrationEnemyProjectile projectile = projectileObject.GetComponent<MigrationEnemyProjectile>();
            if (projectile == null)
            {
                projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
            }

            projectile.Configure(speed, damage, direction, true, hitRadius);
            MigrationCombatFeedbackTemplate template = projectileObject.GetComponent<MigrationCombatFeedbackTemplate>();
            if (template != null)
            {
                projectile.ApplyFeedbackTemplate(template);
            }

            BindProjectileSettlement(projectileObject, projectile, settlement);
            activeProjectiles.Add(projectile);
            LastSpawnedProjectile = projectile;
            return true;
        }

        private GameObject CheckoutProjectileObject(MigrationEnemyProjectile prefab, string objectName, Vector3 spawnPosition)
        {
            MigrationPrefabPoolService pool = ResolveProjectilePool();
            GameObject projectileObject = pool != null
                ? pool.Get(prefab.gameObject, spawnPosition, Quaternion.identity)
                : Instantiate(prefab.gameObject);
            projectileObject.name = objectName;
            projectileObject.transform.position = spawnPosition;
            return projectileObject;
        }

        private int CountPerfectFreezeFieldCandidates(Vector3 center, Vector3 playerPosition)
        {
            Vector3 safeDirection = FlatDirectionTo(center, playerPosition, Vector3.forward);
            int bulletsPerRing = Mathf.Clamp(perfectFreezeFieldBulletsPerRing, 1, 10);
            int count = 0;
            for (int ring = 0; ring < perfectFreezeFieldRingCount; ring++)
            {
                for (int index = 0; index < bulletsPerRing; index++)
                {
                    float angleRadians = Mathf.PI * 2f * index / Mathf.Max(1, bulletsPerRing);
                    Vector3 radial = new Vector3(Mathf.Cos(angleRadians), 0f, Mathf.Sin(angleRadians)).normalized;
                    if (FlatAngleBetween(radial, safeDirection) >= safeLaneHalfAngleDegrees)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private int RemainingActiveProjectileCapacity()
        {
            PruneActiveProjectiles();
            return Mathf.Max(0, activeProjectileCap - activeProjectiles.Count);
        }

        private bool ShouldCastIceLanceSnipe(float distanceToPlayer)
        {
            return iceLanceProjectilePrefab != null
                && iceLanceMinDistance > 0f
                && distanceToPlayer + 0.0001f >= iceLanceMinDistance;
        }

        private bool ShouldCastSnowballRoll(float distanceToPlayer, MigrationPerfectFreezeSnowballHazard resolvedSnowball)
        {
            return resolvedSnowball != null
                && !resolvedSnowball.IsActive
                && snowballPreferredDistance > 0f
                && distanceToPlayer > closeRangeDistance + 0.0001f
                && distanceToPlayer <= snowballPreferredDistance + 0.0001f;
        }

        private void UpdateBossMovement(float deltaTime, Vector3 center, Vector3 playerPosition)
        {
            Transform bossTransform = ResolveBossTransform();
            Vector3 bossPosition = bossTransform != null ? bossTransform.position : center;
            Vector3 desiredPosition = bossPosition;
            string movementKind = BossMovementIdleKind;

            MigrationPerfectFreezeSnowballHazard resolvedSnowball = ResolveSnowballHazard();
            if (resolvedSnowball != null && resolvedSnowball.IsActive)
            {
                Vector3 snowballPosition = resolvedSnowball.transform.position;
                Vector3 snowballForward = playerPosition - snowballPosition;
                snowballForward.y = 0f;
                if (snowballForward.sqrMagnitude <= 0.0001f)
                {
                    snowballForward = FlatDirectionTo(bossPosition, playerPosition, Vector3.forward);
                }

                desiredPosition = snowballPosition - snowballForward.normalized * (resolvedSnowball.Radius + snowballPushOffset);
                desiredPosition.y = bossPosition.y;
                movementKind = BossMovementSnowballPushKind;
            }
            else if (FlatDistance(bossPosition, playerPosition) <= closeRangeDistance + 0.0001f)
            {
                Vector3 forward = FlatDirectionTo(bossPosition, playerPosition, Vector3.forward);
                Vector3 side = Vector3.Cross(forward, Vector3.up);
                side.y = 0f;
                if (side.sqrMagnitude <= 0.0001f)
                {
                    side = Vector3.left;
                }

                desiredPosition = bossPosition - forward * closeEvadeBackDistance + side.normalized * closeEvadeSideDistance;
                desiredPosition.y = bossPosition.y;
                movementKind = BossMovementCloseEvadeKind;
            }

            LastBossMovementIntentKind = movementKind;
            LastDesiredBossPosition = desiredPosition;

            if (bossTransform == null)
            {
                return;
            }

            float t = Mathf.Clamp01(Mathf.Max(0f, deltaTime) * bossMovementLerp);
            bossTransform.position = Vector3.Lerp(bossTransform.position, desiredPosition, t);
        }

        private Transform ResolveBossTransform()
        {
            MigrationSimpleEnemyController controller = ResolveBossController();
            if (controller != null)
            {
                return controller.transform;
            }

            MigrationCombatTargetBehaviour target = ResolveBossTarget();
            return target != null ? target.transform : null;
        }

        private Vector3 ResolveBossTransformPosition(Vector3 fallback)
        {
            Transform bossTransform = ResolveBossTransform();
            return bossTransform != null ? bossTransform.position : fallback;
        }

        private static float FlatDistance(Vector3 origin, Vector3 target)
        {
            origin.y = 0f;
            target.y = 0f;
            return Vector3.Distance(origin, target);
        }

        private static Vector3 FlatDirectionTo(Vector3 origin, Vector3 target, Vector3 fallback)
        {
            Vector3 direction = target - origin;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = fallback;
                direction.y = 0f;
            }

            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }

        private static Vector3 RotateYaw(Vector3 direction, float yawDegrees)
        {
            Vector3 flatDirection = direction;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude <= 0.0001f)
            {
                flatDirection = Vector3.forward;
            }

            return (Quaternion.AngleAxis(yawDegrees, Vector3.up) * flatDirection.normalized).normalized;
        }

        private static float FlatAngleBetween(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            if (a.sqrMagnitude <= 0.0001f || b.sqrMagnitude <= 0.0001f)
            {
                return 180f;
            }

            return Vector3.Angle(a.normalized, b.normalized);
        }

        private MigrationProjectileSpecialSettlement ResolveScopedSettlement()
        {
            if (scopedSettlement == null)
            {
                scopedSettlement = GetComponent<MigrationProjectileSpecialSettlement>();
            }

            if (scopedSettlement != null)
            {
                scopedSettlement.ConfigureSharedSettlementFallback(false);
            }

            return scopedSettlement;
        }

        private MigrationSimpleEnemyController ResolveBossController()
        {
            if (bossController == null)
            {
                bossController = GetComponentInChildren<MigrationSimpleEnemyController>();
            }

            return bossController;
        }

        private MigrationCombatTargetBehaviour ResolveBossTarget()
        {
            if (bossTarget == null && bossController != null)
            {
                bossTarget = bossController.GetComponent<MigrationCombatTargetBehaviour>();
            }

            if (bossTarget == null)
            {
                bossTarget = GetComponentInChildren<MigrationCombatTargetBehaviour>();
            }

            return bossTarget;
        }

        private MigrationPerfectFreezeStaggerAdapter ResolveStaggerAdapter()
        {
            if (staggerAdapter == null)
            {
                staggerAdapter = GetComponentInChildren<MigrationPerfectFreezeStaggerAdapter>();
            }

            return staggerAdapter;
        }

        private MigrationPerfectFreezeSafeLaneCue ResolveSafeLaneCue()
        {
            if (safeLaneCue == null)
            {
                safeLaneCue = GetComponentInChildren<MigrationPerfectFreezeSafeLaneCue>();
            }

            ConfigureSafeLaneCue();
            return safeLaneCue;
        }

        private MigrationPerfectFreezeSnowballHazard ResolveSnowballHazard()
        {
            if (snowballHazard == null)
            {
                snowballHazard = GetComponentInChildren<MigrationPerfectFreezeSnowballHazard>();
            }

            if (snowballHazard != null && snowballHazard.EncounterDirector != this)
            {
                snowballHazard.BindEncounterDirector(this);
            }

            return snowballHazard;
        }

        private MigrationPrefabPoolService ResolveProjectilePool()
        {
            if (projectilePool == null)
            {
                projectilePool = GetComponentInChildren<MigrationPrefabPoolService>();
            }

            return projectilePool;
        }

        private void SubscribeBossTargetEvents()
        {
            if (bossTarget == subscribedBossTarget)
            {
                return;
            }

            UnsubscribeBossTargetEvents();
            if (bossTarget == null)
            {
                return;
            }

            subscribedBossTarget = bossTarget;
            subscribedBossTarget.Defeated += OnBossTargetDefeated;
        }

        private void UnsubscribeBossTargetEvents()
        {
            if (subscribedBossTarget == null)
            {
                return;
            }

            subscribedBossTarget.Defeated -= OnBossTargetDefeated;
            subscribedBossTarget = null;
        }

        private void OnBossTargetDefeated(CombatBridgeResult result)
        {
            FinishPhase("clear");
        }

        private void ConfigureSafeLaneCue()
        {
            if (safeLaneCue != null)
            {
                safeLaneCue.ConfigureCue(safeLaneHalfAngleDegrees, safeLaneCueDurationSeconds, safeLaneCueColor);
            }
        }

        private void WireStaggerAdapter()
        {
            if (staggerAdapter == null)
            {
                return;
            }

            if (scopedSettlement != null)
            {
                staggerAdapter.BindSettlement(scopedSettlement);
            }

            if (bossController != null)
            {
                staggerAdapter.BindEnemyController(bossController);
            }
        }

        private static void BindProjectileSettlement(
            GameObject projectileObject,
            MigrationEnemyProjectile projectile,
            MigrationProjectileSpecialSettlement settlement)
        {
            MigrationProjectileSpecialSettlement[] localSettlements =
                projectileObject.GetComponents<MigrationProjectileSpecialSettlement>();
            if (localSettlements.Length == 0)
            {
                MigrationProjectileSpecialSettlement localSettlement =
                    projectileObject.AddComponent<MigrationProjectileSpecialSettlement>();
                localSettlement.ConfigureRewards(2f, 5f, 8f, 12f, 12, 1.2f);
                localSettlements = new[] { localSettlement };
            }

            foreach (MigrationProjectileSpecialSettlement localSettlement in localSettlements)
            {
                localSettlement.BindProjectile(projectile);
                localSettlement.BindSharedSettlement(settlement);
            }
        }

        private void PruneActiveProjectiles()
        {
            MigrationPrefabPoolService pool = ResolveProjectilePool();
            for (int index = activeProjectiles.Count - 1; index >= 0; index--)
            {
                MigrationEnemyProjectile projectile = activeProjectiles[index];
                if (projectile == null || projectile.IsExpired || projectile.IsShattered)
                {
                    if (projectile != null && pool != null)
                    {
                        pool.Release(projectile.gameObject);
                    }

                    activeProjectiles.RemoveAt(index);
                }
            }
        }
    }
}
