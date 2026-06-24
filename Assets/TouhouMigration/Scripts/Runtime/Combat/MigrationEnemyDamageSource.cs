using System;
using TouhouMigration.Runtime.Player;
using TouhouMigration.Runtime.UI;
using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class MigrationEnemyDamageSource : MonoBehaviour
    {
        [SerializeField] private float damage = 10f;
        [SerializeField] private bool requiresActiveWindow;
        [SerializeField] private bool visibleWhenInactive = true;
        [SerializeField] private bool windowActive = true;

        private MigrationCombatRuntime combatRuntime;

        public event Action<PlayerHealthResult> PlayerDamaged;

        public bool RequiresActiveWindow => requiresActiveWindow;
        public bool IsWindowActive => !requiresActiveWindow || windowActive;
        public int WindowBlockedCount { get; private set; }
        public int DamageEventCount { get; private set; }

        public void BindCombat(MigrationCombatRuntime combat)
        {
            combatRuntime = combat;
        }

        public void Configure(float damage)
        {
            this.damage = Mathf.Max(0f, damage);
        }

        public void ConfigureWindowing(bool sourceRequiresActiveWindow, bool sourceVisibleWhenInactive)
        {
            requiresActiveWindow = sourceRequiresActiveWindow;
            visibleWhenInactive = sourceVisibleWhenInactive;
            windowActive = !requiresActiveWindow;
            ApplyWindowState();
        }

        public void SetWindowActive(bool active)
        {
            windowActive = !requiresActiveWindow || active;
            ApplyWindowState();
        }

        public PlayerHealthResult TryDamagePlayer()
        {
            if (requiresActiveWindow && !windowActive)
            {
                WindowBlockedCount++;
                return new PlayerHealthResult { RawDamage = damage };
            }

            MigrationCombatRuntime combat = ResolveCombatRuntime();
            if (combat == null)
            {
                return new PlayerHealthResult { RawDamage = damage };
            }

            PlayerHealthResult result = combat.ApplyDamageToPlayer(damage);
            if (result.DamageApplied > 0f || result.RebirthTriggered)
            {
                DamageEventCount++;
                PlayerDamaged?.Invoke(result);
            }

            return result;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                TryDamagePlayer();
            }
        }

        private void Awake()
        {
            ApplyWindowState();
        }

        private void OnEnable()
        {
            ApplyWindowState();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider != null && collision.collider.CompareTag("Player"))
            {
                TryDamagePlayer();
            }
        }

        private MigrationCombatRuntime ResolveCombatRuntime()
        {
            if (combatRuntime == null)
            {
                combatRuntime = MigrationGlobalUiController.FindCombatRuntime();
            }

            return combatRuntime;
        }

        private void ApplyWindowState()
        {
            bool canCollide = !requiresActiveWindow || windowActive;
            bool isVisible = canCollide || visibleWhenInactive;

            foreach (Collider sourceCollider in GetComponentsInChildren<Collider>(true))
            {
                sourceCollider.enabled = canCollide;
            }

            foreach (Renderer sourceRenderer in GetComponentsInChildren<Renderer>(true))
            {
                sourceRenderer.enabled = isVisible;
            }
        }
    }
}
