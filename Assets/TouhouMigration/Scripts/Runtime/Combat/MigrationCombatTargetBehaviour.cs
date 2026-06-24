using System;
using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class MigrationCombatTargetBehaviour : MonoBehaviour
    {
        [SerializeField] private float maxHp = 20f;

        private MigrationCombatTargetRuntime runtime;

        public event Action<CombatBridgeResult> Damaged;
        public event Action<CombatBridgeResult> Defeated;

        public MigrationCombatTargetRuntime Runtime => EnsureRuntime();
        public float MaxHp => EnsureRuntime().MaxHp;
        public float CurrentHp => EnsureRuntime().CurrentHp;
        public bool IsDefeated => EnsureRuntime().IsDefeated;
        public int DamageEventCount { get; private set; }
        public int DefeatEventCount { get; private set; }

        public void Initialize(float maxHp)
        {
            this.maxHp = Mathf.Max(1f, maxHp);
            runtime = new MigrationCombatTargetRuntime(this.maxHp);
            DamageEventCount = 0;
            DefeatEventCount = 0;
        }

        public CombatBridgeResult ApplyDamage(float amount)
        {
            MigrationCombatTargetRuntime targetRuntime = EnsureRuntime();
            bool wasDefeated = targetRuntime.IsDefeated;
            CombatBridgeResult result = targetRuntime.ApplyDamage(amount);
            if (result.DamageApplied > 0f)
            {
                DamageEventCount++;
                Damaged?.Invoke(result);
            }

            if (!wasDefeated && result.TargetDefeated)
            {
                DefeatEventCount++;
                Defeated?.Invoke(result);
            }

            return result;
        }

        private void Awake()
        {
            EnsureRuntime();
        }

        private MigrationCombatTargetRuntime EnsureRuntime()
        {
            if (runtime == null)
            {
                runtime = new MigrationCombatTargetRuntime(maxHp);
            }

            return runtime;
        }
    }
}
