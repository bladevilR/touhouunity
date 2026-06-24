using System;
using TouhouMigration.Runtime.UI;
using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class MigrationProjectileSpecialSettlement : MonoBehaviour
    {
        [SerializeField] private MigrationEnemyProjectile projectile;
        [SerializeField] private MigrationProjectileSpecialSettlement sharedSettlement;
        [SerializeField] private bool useSharedSettlementFallback = true;
        [SerializeField] private float normalGrazeGauge = 2f;
        [SerializeField] private float dashGrazeGauge = 5f;
        [SerializeField] private float perfectDashGrazeGauge = 8f;
        [SerializeField] private float shatterGauge = 12f;
        [SerializeField] private int perfectFreezeStaggerBreaks = 12;
        [SerializeField] private float perfectFreezeStaggerSeconds = 1.2f;

        private MigrationPhoenixGaugeRuntime gauge;
        private bool subscribed;
        private bool playerDashing;
        private bool explicitGaugeBinding;
        private int frozenCrystalBreakStreak;
        private int iceCrystalBreakStreak;

        public event Action<float, string> GaugeGranted;
        public event Action<float> PerfectFreezeStaggerReady;
        public event Action<MigrationProjectileReflectResult> ReflectStunReady;

        public float NormalGrazeGauge => normalGrazeGauge;
        public float DashGrazeGauge => dashGrazeGauge;
        public float PerfectDashGrazeGauge => perfectDashGrazeGauge;
        public float ShatterGauge => shatterGauge;
        public bool UsesSharedSettlementFallback => useSharedSettlementFallback;
        public int GrazeSettlementCount { get; private set; }
        public int ShatterSettlementCount { get; private set; }
        public int IceCrystalBreakCount { get; private set; }
        public int IceCrystalBreakStreak => iceCrystalBreakStreak;
        public float PendingHeavyBurstRadiusMultiplier { get; private set; } = 1f;
        public int FrozenCrystalBreakCount { get; private set; }
        public int PerfectFreezeStaggerEventCount { get; private set; }
        public int ReflectSettlementCount { get; private set; }
        public int ReflectStunEventCount { get; private set; }
        public int HeavyBurstConsumeCount { get; private set; }
        public float LastGaugeGain { get; private set; }
        public string LastGaugeReason { get; private set; } = string.Empty;
        public float LastPerfectFreezeStaggerSeconds { get; private set; }
        public float LastReflectStunSeconds { get; private set; }
        public float LastConsumedHeavyBurstRadiusMultiplier { get; private set; } = 1f;

        public void BindGauge(MigrationPhoenixGaugeRuntime gauge)
        {
            this.gauge = gauge;
            explicitGaugeBinding = gauge != null;
        }

        public void BindSharedSettlement(MigrationProjectileSpecialSettlement settlement)
        {
            sharedSettlement = settlement != this ? settlement : null;
        }

        public void ConfigureSharedSettlementFallback(bool enabled)
        {
            useSharedSettlementFallback = enabled;
        }

        public void BindProjectile(MigrationEnemyProjectile projectile)
        {
            if (this.projectile == projectile)
            {
                Subscribe();
                return;
            }

            Unsubscribe();
            this.projectile = projectile;
            Subscribe();
        }

        public void ConfigureRewards(
            float normalGrazeGauge,
            float dashGrazeGauge,
            float perfectDashGrazeGauge,
            float shatterGauge,
            int perfectFreezeStaggerBreaks,
            float perfectFreezeStaggerSeconds)
        {
            this.normalGrazeGauge = Mathf.Max(0f, normalGrazeGauge);
            this.dashGrazeGauge = Mathf.Max(0f, dashGrazeGauge);
            this.perfectDashGrazeGauge = Mathf.Max(0f, perfectDashGrazeGauge);
            this.shatterGauge = Mathf.Max(0f, shatterGauge);
            this.perfectFreezeStaggerBreaks = Mathf.Max(1, perfectFreezeStaggerBreaks);
            this.perfectFreezeStaggerSeconds = Mathf.Max(0f, perfectFreezeStaggerSeconds);
        }

        public void SetPlayerDashing(bool isDashing)
        {
            playerDashing = isDashing;
        }

        public float ConsumePendingHeavyBurstRadiusMultiplier()
        {
            float multiplier = Mathf.Max(1f, PendingHeavyBurstRadiusMultiplier);
            LastConsumedHeavyBurstRadiusMultiplier = multiplier;
            if (multiplier > 1.001f)
            {
                PendingHeavyBurstRadiusMultiplier = 1f;
                HeavyBurstConsumeCount++;
            }

            return multiplier;
        }

        public float SettleGraze(MigrationProjectileGrazeResult result)
        {
            MigrationProjectileSpecialSettlement shared = ResolveSharedSettlement();
            if (shared != null)
            {
                return shared.SettleGraze(result);
            }

            MigrationPhoenixGaugeRuntime resolvedGauge = ResolveGauge();
            if (resolvedGauge == null)
            {
                return 0f;
            }

            float amount = normalGrazeGauge;
            if (playerDashing)
            {
                amount = result.IsPerfect ? perfectDashGrazeGauge : dashGrazeGauge;
            }

            string quality = string.IsNullOrWhiteSpace(result.Quality) ? "normal" : result.Quality.Trim().ToLowerInvariant();
            float applied = resolvedGauge.AddGraze(amount, "graze:" + quality);
            if (applied > 0f)
            {
                GrazeSettlementCount++;
                RecordGaugeGrant(applied, "graze:" + quality);
            }

            return applied;
        }

        public float SettleReflect(MigrationProjectileReflectResult result)
        {
            MigrationProjectileSpecialSettlement shared = ResolveSharedSettlement();
            if (shared != null)
            {
                return shared.SettleReflect(result);
            }

            if (result == null || !result.StunReward || result.StunSeconds <= 0f)
            {
                return 0f;
            }

            ReflectSettlementCount++;
            ReflectStunEventCount++;
            LastReflectStunSeconds = result.StunSeconds;
            ReflectStunReady?.Invoke(result);
            return result.StunSeconds;
        }

        public float SettleShatter(MigrationProjectileShatterResult result)
        {
            MigrationProjectileSpecialSettlement shared = ResolveSharedSettlement();
            if (shared != null)
            {
                return shared.SettleShatter(result);
            }

            if (result == null)
            {
                return 0f;
            }

            string family = NormalizeFamily(result.ProjectileFamily);
            string reason = ResolveShatterReason(family);
            float applied = 0f;
            MigrationPhoenixGaugeRuntime resolvedGauge = ResolveGauge();
            if (resolvedGauge != null)
            {
                applied = resolvedGauge.AddAttack(shatterGauge, reason);
                if (applied > 0f)
                {
                    ShatterSettlementCount++;
                    RecordGaugeGrant(applied, reason);
                }
            }

            if (family == "frozen_crystal")
            {
                FrozenCrystalBreakCount++;
                frozenCrystalBreakStreak++;
                if (frozenCrystalBreakStreak >= Mathf.Max(1, perfectFreezeStaggerBreaks))
                {
                    frozenCrystalBreakStreak = 0;
                    PerfectFreezeStaggerEventCount++;
                    LastPerfectFreezeStaggerSeconds = perfectFreezeStaggerSeconds;
                    PerfectFreezeStaggerReady?.Invoke(perfectFreezeStaggerSeconds);
                }
            }
            else if (family == "ice_crystal")
            {
                IceCrystalBreakCount++;
                iceCrystalBreakStreak++;
                if (iceCrystalBreakStreak >= 3)
                {
                    iceCrystalBreakStreak = 0;
                    PendingHeavyBurstRadiusMultiplier = 1.25f;
                }
            }

            return applied;
        }

        private void Awake()
        {
            projectile ??= GetComponent<MigrationEnemyProjectile>();
        }

        private void Start()
        {
            ResolveGauge();
        }

        private void OnEnable()
        {
            projectile ??= GetComponent<MigrationEnemyProjectile>();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private MigrationPhoenixGaugeRuntime ResolveGauge()
        {
            if (gauge == null)
            {
                gauge = MigrationGlobalUiController.FindPhoenixGaugeRuntime();
            }

            return gauge;
        }

        private MigrationProjectileSpecialSettlement ResolveSharedSettlement()
        {
            if (sharedSettlement != null && sharedSettlement != this)
            {
                return sharedSettlement;
            }

            if (!useSharedSettlementFallback)
            {
                return null;
            }

            if (explicitGaugeBinding)
            {
                return null;
            }

            MigrationProjectileSpecialSettlement globalSettlement = MigrationGlobalUiController.FindProjectileSettlement();
            if (globalSettlement != null && globalSettlement != this)
            {
                sharedSettlement = globalSettlement;
                return sharedSettlement;
            }

            return null;
        }

        private void OnProjectileGrazed(MigrationProjectileGrazeResult result)
        {
            SettleGraze(result);
        }

        private void OnProjectileShattered(MigrationProjectileShatterResult result)
        {
            SettleShatter(result);
        }

        private void OnProjectileReflected(MigrationProjectileReflectResult result)
        {
            SettleReflect(result);
        }

        private void Subscribe()
        {
            if (projectile == null || subscribed)
            {
                return;
            }

            projectile.Grazed += OnProjectileGrazed;
            projectile.Shattered += OnProjectileShattered;
            projectile.Reflected += OnProjectileReflected;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (projectile == null || !subscribed)
            {
                return;
            }

            projectile.Grazed -= OnProjectileGrazed;
            projectile.Shattered -= OnProjectileShattered;
            projectile.Reflected -= OnProjectileReflected;
            subscribed = false;
        }

        private void RecordGaugeGrant(float applied, string reason)
        {
            LastGaugeGain = applied;
            LastGaugeReason = reason;
            GaugeGranted?.Invoke(applied, reason);
        }

        private static string ResolveShatterReason(string family)
        {
            return family switch
            {
                "frozen_crystal" => "perfect_freeze_crystal",
                "ice_wall" => "ice_wall",
                "ice_crystal" => "ice_crystal",
                "snowball" => "snowball",
                _ => string.IsNullOrWhiteSpace(family) ? "shatter" : family
            };
        }

        private static string NormalizeFamily(string family)
        {
            return string.IsNullOrWhiteSpace(family) ? string.Empty : family.Trim().ToLowerInvariant();
        }
    }
}
