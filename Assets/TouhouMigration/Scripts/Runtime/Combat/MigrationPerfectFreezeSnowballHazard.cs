using TouhouMigration.Runtime.Player;
using TouhouMigration.Runtime.UI;
using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    [DisallowMultipleComponent]
    public sealed class MigrationPerfectFreezeSnowballHazard : MonoBehaviour
    {
        [SerializeField] private MigrationPerfectFreezeEncounterDirector encounterDirector;
        [SerializeField] private float speed = 4.2f;
        [SerializeField] private float damage = 16f;
        [SerializeField] private float spawnForwardOffset = 2.4f;
        [SerializeField] private float initialRadiusBase = 0.78f;
        [SerializeField] private float initialRadiusSeedStep = 0.1f;
        [SerializeField] private float maxInitialRadius = 1.05f;
        [SerializeField] private float maxRadiusBonus = 1.55f;
        [SerializeField] private float growthPerSecond = 0.18f;
        [SerializeField] private float durationSeconds = 5.8f;
        [SerializeField] private float initialShatterHp = 42f;
        [SerializeField] private float arenaRadius = 34f;
        [SerializeField] private float playerDamageCooldownSeconds = 0.75f;
        [SerializeField] private string weakFamilies = "fire,heavy,shatter";

        private MigrationCombatRuntime combatRuntime;
        private Vector3 direction = Vector3.forward;
        private Vector3 arenaCenter;
        private float radius;
        private float maxRadius;
        private float elapsedSeconds;
        private float shatterHp;
        private float playerDamageCooldownRemainingSeconds;
        private bool arenaCenterConfigured;
        private bool active;
        private bool expired;
        private bool shattered;

        public MigrationPerfectFreezeEncounterDirector EncounterDirector => encounterDirector;
        public float Speed => speed;
        public float Damage => damage;
        public float SpawnForwardOffset => spawnForwardOffset;
        public float GrowthPerSecond => growthPerSecond;
        public float DurationSeconds => durationSeconds;
        public float InitialShatterHp => initialShatterHp;
        public float ArenaRadius => arenaRadius;
        public float PlayerDamageCooldownSeconds => playerDamageCooldownSeconds;
        public float PlayerDamageCooldownRemainingSeconds => playerDamageCooldownRemainingSeconds;
        public Vector3 ArenaCenter => arenaCenter;
        public Vector3 Direction => direction;
        public float Radius => radius;
        public float MaxRadius => maxRadius;
        public float ElapsedSeconds => elapsedSeconds;
        public float ShatterHp => shatterHp;
        public bool IsActive => active;
        public bool IsExpired => expired;
        public bool IsShattered => shattered;
        public int ShatterEventCount { get; private set; }
        public int ExpireEventCount { get; private set; }
        public int BounceEventCount { get; private set; }
        public int PlayerDamageEventCount { get; private set; }
        public Vector3 LastBounceNormal { get; private set; }
        public PlayerHealthResult LastPlayerDamageResult { get; private set; }
        public string LastCounterSourceFamily { get; private set; } = string.Empty;

        public void BindEncounterDirector(MigrationPerfectFreezeEncounterDirector director)
        {
            encounterDirector = director;
        }

        public void BindCombatRuntime(MigrationCombatRuntime combat)
        {
            combatRuntime = combat;
        }

        public void ConfigureSnowball(
            float speed,
            float damage,
            float durationSeconds,
            float initialShatterHp,
            float growthPerSecond,
            float spawnForwardOffset)
        {
            this.speed = Mathf.Max(0f, speed);
            this.damage = Mathf.Max(0f, damage);
            this.durationSeconds = Mathf.Max(0f, durationSeconds);
            this.initialShatterHp = Mathf.Max(0f, initialShatterHp);
            this.growthPerSecond = Mathf.Max(0f, growthPerSecond);
            this.spawnForwardOffset = Mathf.Max(0f, spawnForwardOffset);
        }

        public void ConfigureArena(Vector3 center, float radius)
        {
            arenaCenter = center;
            arenaCenter.y = 0f;
            arenaRadius = Mathf.Max(0f, radius);
            arenaCenterConfigured = true;
        }

        public void BeginRolling(Vector3 origin, Vector3 target, float growthSeed = 1f)
        {
            direction = FlatDirectionTo(origin, target);
            if (!arenaCenterConfigured)
            {
                arenaCenter = origin;
                arenaCenter.y = 0f;
            }

            radius = Mathf.Clamp(
                initialRadiusBase + growthSeed * initialRadiusSeedStep,
                initialRadiusBase,
                maxInitialRadius);
            maxRadius = radius + Mathf.Max(0f, maxRadiusBonus);
            elapsedSeconds = 0f;
            shatterHp = initialShatterHp;
            playerDamageCooldownRemainingSeconds = 0f;
            expired = false;
            shattered = false;
            active = true;
            LastCounterSourceFamily = string.Empty;

            Vector3 spawnPosition = origin + direction * spawnForwardOffset;
            spawnPosition.y = radius;
            transform.position = spawnPosition;
            ApplyRadiusScale();
            SetVisualActive(true);
            encounterDirector?.SetSnowballPressureActive(true);
        }

        public void TickSnowball(float deltaTime)
        {
            if (!active)
            {
                return;
            }

            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            elapsedSeconds = Mathf.Min(durationSeconds, elapsedSeconds + safeDeltaTime);
            playerDamageCooldownRemainingSeconds = Mathf.Max(0f, playerDamageCooldownRemainingSeconds - safeDeltaTime);
            radius = Mathf.Min(maxRadius, radius + growthPerSecond * safeDeltaTime);

            Vector3 nextPosition = transform.position + direction * speed * safeDeltaTime;
            nextPosition.y = radius;
            if (TryBounceAtArenaBoundary(nextPosition, safeDeltaTime, out Vector3 bouncedPosition))
            {
                nextPosition = bouncedPosition;
                nextPosition.y = radius;
            }

            transform.position = nextPosition;
            ApplyRadiusScale();

            if (durationSeconds <= 0f || elapsedSeconds >= durationSeconds - 0.0001f)
            {
                Expire();
            }
        }

        public bool TryApplyCounterDamage(float amount, string sourceFamily)
        {
            if (!active || amount <= 0f)
            {
                return false;
            }

            string normalizedFamily = NormalizeFamily(sourceFamily);
            float multiplier = IsWeakFamily(normalizedFamily) ? 1.5f : 1f;
            shatterHp = Mathf.Max(0f, shatterHp - amount * multiplier);
            LastCounterSourceFamily = normalizedFamily;
            if (shatterHp > 0f)
            {
                return false;
            }

            Shatter();
            return true;
        }

        public PlayerHealthResult TryDamagePlayer()
        {
            PlayerHealthResult result;
            if (!active || playerDamageCooldownRemainingSeconds > 0.0001f)
            {
                result = new PlayerHealthResult { RawDamage = damage };
                LastPlayerDamageResult = result;
                return result;
            }

            MigrationCombatRuntime combat = ResolveCombatRuntime();
            result = combat != null
                ? combat.ApplyDamageToPlayer(damage)
                : new PlayerHealthResult { RawDamage = damage };
            LastPlayerDamageResult = result;
            if (result.DamageApplied > 0f || result.RebirthTriggered)
            {
                PlayerDamageEventCount++;
                playerDamageCooldownRemainingSeconds = playerDamageCooldownSeconds;
                encounterDirector?.RegisterPlayerHit();
            }

            return result;
        }

        private void Update()
        {
            TickSnowball(Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other != null && other.CompareTag("Player"))
            {
                TryDamagePlayer();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider != null && collision.collider.CompareTag("Player"))
            {
                TryDamagePlayer();
            }
        }

        private void Expire()
        {
            if (!active)
            {
                return;
            }

            active = false;
            expired = true;
            ExpireEventCount++;
            SetVisualActive(false);
            encounterDirector?.SetSnowballPressureActive(false);
        }

        private void Shatter()
        {
            if (!active)
            {
                return;
            }

            active = false;
            shattered = true;
            ShatterEventCount++;
            SetVisualActive(false);
            encounterDirector?.SetSnowballPressureActive(false);
        }

        private void ApplyRadiusScale()
        {
            transform.localScale = Vector3.one * radius;
        }

        private void SetVisualActive(bool visible)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = visible;
            }
        }

        private bool TryBounceAtArenaBoundary(Vector3 nextPosition, float deltaTime, out Vector3 bouncedPosition)
        {
            bouncedPosition = nextPosition;
            if (arenaRadius <= 0f)
            {
                return false;
            }

            Vector3 flatOffset = nextPosition - arenaCenter;
            flatOffset.y = 0f;
            if (flatOffset.magnitude <= arenaRadius + 0.0001f)
            {
                return false;
            }

            Vector3 arenaNormal = flatOffset.sqrMagnitude > 0.0001f ? flatOffset.normalized : Vector3.forward;
            direction = Vector3.Reflect(direction, arenaNormal);
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : -arenaNormal;
            LastBounceNormal = arenaNormal;
            BounceEventCount++;

            bouncedPosition = transform.position + direction * speed * deltaTime;
            return true;
        }

        private MigrationCombatRuntime ResolveCombatRuntime()
        {
            if (combatRuntime == null)
            {
                combatRuntime = MigrationGlobalUiController.FindCombatRuntime();
            }

            return combatRuntime;
        }

        private bool IsWeakFamily(string sourceFamily)
        {
            if (string.IsNullOrWhiteSpace(sourceFamily) || string.IsNullOrWhiteSpace(weakFamilies))
            {
                return false;
            }

            string[] families = weakFamilies.Split(',');
            foreach (string family in families)
            {
                if (string.Equals(NormalizeFamily(family), sourceFamily, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeFamily(string family)
        {
            return string.IsNullOrWhiteSpace(family) ? "unknown" : family.Trim().ToLowerInvariant();
        }

        private static Vector3 FlatDirectionTo(Vector3 origin, Vector3 target)
        {
            Vector3 flatDirection = target - origin;
            flatDirection.y = 0f;
            return flatDirection.sqrMagnitude > 0.0001f ? flatDirection.normalized : Vector3.forward;
        }
    }
}
