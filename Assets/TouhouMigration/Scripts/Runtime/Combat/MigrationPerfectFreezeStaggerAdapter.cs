using TouhouMigration.Runtime.UI;
using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    [DisallowMultipleComponent]
    public sealed class MigrationPerfectFreezeStaggerAdapter : MonoBehaviour
    {
        [SerializeField] private MigrationProjectileSpecialSettlement settlement;
        [SerializeField] private MigrationSimpleEnemyController enemyController;

        private bool subscribed;

        public bool HasSettlement => ResolveSettlement() != null;
        public bool HasEnemyController => ResolveEnemyController() != null;
        public int StaggerEventCount { get; private set; }
        public int ReflectStunEventCount { get; private set; }
        public float LastStaggerSeconds { get; private set; }
        public float LastReflectStunSeconds { get; private set; }

        public void BindSettlement(MigrationProjectileSpecialSettlement settlement)
        {
            if (this.settlement == settlement)
            {
                Subscribe();
                return;
            }

            Unsubscribe();
            this.settlement = settlement;
            Subscribe();
        }

        public void BindEnemyController(MigrationSimpleEnemyController enemyController)
        {
            this.enemyController = enemyController;
        }

        private void Awake()
        {
            ResolveEnemyController();
        }

        private void OnEnable()
        {
            ResolveEnemyController();
            ResolveSettlement();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnPerfectFreezeStaggerReady(float seconds)
        {
            MigrationSimpleEnemyController controller = ResolveEnemyController();
            if (controller == null || seconds <= 0f)
            {
                return;
            }

            LastStaggerSeconds = seconds;
            StaggerEventCount++;
            controller.ApplyStun(seconds);
        }

        private void OnReflectStunReady(MigrationProjectileReflectResult result)
        {
            MigrationSimpleEnemyController controller = ResolveEnemyController();
            if (controller == null || result == null || result.StunSeconds <= 0f)
            {
                return;
            }

            LastReflectStunSeconds = result.StunSeconds;
            ReflectStunEventCount++;
            controller.ApplyStun(result.StunSeconds);
        }

        private MigrationSimpleEnemyController ResolveEnemyController()
        {
            if (enemyController == null)
            {
                enemyController = GetComponent<MigrationSimpleEnemyController>();
            }

            return enemyController;
        }

        private MigrationProjectileSpecialSettlement ResolveSettlement()
        {
            if (settlement == null)
            {
                settlement = MigrationGlobalUiController.FindProjectileSettlement();
            }

            return settlement;
        }

        private void Subscribe()
        {
            MigrationProjectileSpecialSettlement resolvedSettlement = ResolveSettlement();
            if (resolvedSettlement == null || subscribed)
            {
                return;
            }

            resolvedSettlement.PerfectFreezeStaggerReady += OnPerfectFreezeStaggerReady;
            resolvedSettlement.ReflectStunReady += OnReflectStunReady;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (settlement == null || !subscribed)
            {
                return;
            }

            settlement.PerfectFreezeStaggerReady -= OnPerfectFreezeStaggerReady;
            settlement.ReflectStunReady -= OnReflectStunReady;
            subscribed = false;
        }
    }
}
