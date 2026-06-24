using System;
using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    [RequireComponent(typeof(MigrationCombatTargetBehaviour))]
    public sealed class MigrationSimpleEnemyController : MonoBehaviour
    {
        [SerializeField] private MigrationCombatTargetBehaviour target;
        [SerializeField] private MigrationEnemyDamageSource damageSource;
        [SerializeField] private MigrationCombatLootDropHandler lootDropHandler;
        [SerializeField] private float chaseRange = 6f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float attackCooldown = 0.8f;
        [SerializeField] private float attackWindupSeconds;
        [SerializeField] private float attackActiveSeconds;
        [SerializeField] private float attackRecoverySeconds;
        [SerializeField] private string currentVariantId = string.Empty;
        [SerializeField] private bool canMelee = true;
        [SerializeField] private bool canShoot;
        [SerializeField] private float projectileSpeed = 8f;
        [SerializeField] private float rangedMinDistance = 5f;
        [SerializeField] private float projectileSpawnHeight = 0.3f;
        [SerializeField] private float currentAttackDamage = 10f;
        [SerializeField] private MigrationEnemyProjectile projectilePrefab;
        [SerializeField] private MigrationPrefabPoolService projectilePool;
        [SerializeField] private float stunRemainingSeconds;

        private MigrationCombatRuntime combatRuntime;
        private float attackCooldownRemaining;
        private float windupRemaining;
        private bool windupActive;
        private bool windupIsRanged;
        private Vector3 windupTargetPosition;
        private float actionPhaseRemaining;
        private bool actionHitResolved;
        private bool subscribed;

        public event Action<string> StateChanged;
        public event Action MeleeAttackPerformed;
        public event Action ProjectileAttackPerformed;
        public event Action<bool> WindupStarted;

        public string CurrentState { get; private set; } = "idle";
        public string CurrentActionPhase { get; private set; } = "idle";
        public string CurrentVariantId => currentVariantId;
        public int AttackEventCount { get; private set; }
        public int WindupEventCount { get; private set; }
        public int ProjectileEventCount { get; private set; }
        public int ActionTelegraphEventCount { get; private set; }
        public int ActionActiveEventCount { get; private set; }
        public int ActionRecoveryEventCount { get; private set; }
        public int StunEventCount { get; private set; }
        public bool IsStunned => stunRemainingSeconds > 0f;
        public float StunRemainingSeconds => stunRemainingSeconds;
        public bool HasProjectilePrefab => projectilePrefab != null;
        public bool HasProjectilePool => projectilePool != null;
        public MigrationPrefabPoolService ProjectilePool => projectilePool;
        public MigrationEnemyProjectile LastSpawnedProjectile { get; private set; }

        public void BindTarget(MigrationCombatTargetBehaviour target)
        {
            Unsubscribe();
            this.target = target;
            Subscribe();
            if (this.target != null && this.target.IsDefeated)
            {
                SetState("defeated");
            }
        }

        public void BindDamageSource(MigrationEnemyDamageSource damageSource)
        {
            this.damageSource = damageSource;
        }

        public void BindLootDropHandler(MigrationCombatLootDropHandler lootDropHandler)
        {
            this.lootDropHandler = lootDropHandler;
        }

        public void BindCombat(MigrationCombatRuntime combat)
        {
            combatRuntime = combat;
        }

        public void ConfigureProjectilePrefab(MigrationEnemyProjectile prefab)
        {
            projectilePrefab = prefab;
        }

        public void BindProjectilePool(MigrationPrefabPoolService pool)
        {
            projectilePool = pool;
        }

        public void ApplyVariant(MigrationEnemyVariantProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            currentVariantId = profile.VariantId;
            target?.Initialize(profile.MaxHp);
            ConfigureMovement(profile.ChaseRange, profile.AttackRange, profile.MoveSpeed);
            ConfigureAttackCooldown(profile.AttackCooldown);
            ConfigureAttackWindup(profile.AttackWindupSeconds);
            damageSource?.Configure(profile.AttackDamage);
            lootDropHandler?.ConfigureGodotLootTables(profile.EnemyType, profile.ElementalGroup, profile.ForceLootTables);
            canMelee = profile.CanMelee;
            canShoot = profile.CanShoot;
            projectileSpeed = profile.ProjectileSpeed;
            rangedMinDistance = profile.RangedMinDistance;
            projectileSpawnHeight = profile.FloatHeight + 0.3f;
            currentAttackDamage = profile.AttackDamage;
        }

        public void ConfigureMovement(float chaseRange, float attackRange, float moveSpeed)
        {
            this.chaseRange = Mathf.Max(0f, chaseRange);
            this.attackRange = Mathf.Max(0f, attackRange);
            this.moveSpeed = Mathf.Max(0f, moveSpeed);
        }

        public void ConfigureAttackCooldown(float cooldown)
        {
            attackCooldown = Mathf.Max(0f, cooldown);
        }

        public void ConfigureAttackWindup(float windupSeconds)
        {
            attackWindupSeconds = Mathf.Max(0f, windupSeconds);
            windupRemaining = 0f;
            windupActive = false;
            CurrentActionPhase = "idle";
        }

        public void ConfigureActionTimings(float telegraphSeconds, float activeSeconds, float recoverySeconds)
        {
            attackWindupSeconds = Mathf.Max(0f, telegraphSeconds);
            attackActiveSeconds = Mathf.Max(0f, activeSeconds);
            attackRecoverySeconds = Mathf.Max(0f, recoverySeconds);
            windupRemaining = 0f;
            actionPhaseRemaining = 0f;
            actionHitResolved = false;
            windupActive = false;
            CurrentActionPhase = "idle";
        }

        public void ApplyStun(float durationSeconds)
        {
            float duration = Mathf.Max(0f, durationSeconds);
            if (duration <= 0f || (target != null && target.IsDefeated))
            {
                return;
            }

            ResetActionSequence();
            stunRemainingSeconds = Mathf.Max(stunRemainingSeconds, duration);
            StunEventCount++;
            SetState("stunned");
        }

        public void Tick(float deltaTime, Vector3 playerPosition)
        {
            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            if (target != null && target.IsDefeated)
            {
                stunRemainingSeconds = 0f;
                ResetActionSequence();
                SetState("defeated");
                return;
            }

            attackCooldownRemaining = Mathf.Max(0f, attackCooldownRemaining - safeDeltaTime);
            if (IsStunned)
            {
                AdvanceStun(safeDeltaTime);
                return;
            }

            if (windupActive)
            {
                windupTargetPosition = playerPosition;
                AdvanceWindup(safeDeltaTime);
                return;
            }

            float distance = Vector3.Distance(transform.position, playerPosition);

            if (canShoot)
            {
                if (distance < rangedMinDistance)
                {
                    SetState("ranged_reposition");
                    MoveAwayFrom(playerPosition, safeDeltaTime);
                    return;
                }

                if (distance <= attackRange)
                {
                    TryAttack(safeDeltaTime, true, playerPosition);
                    return;
                }
            }

            if (canMelee && distance <= attackRange)
            {
                TryAttack(safeDeltaTime, false, playerPosition);
                return;
            }

            if (distance <= chaseRange)
            {
                SetState("chase");
                MoveToward(playerPosition, safeDeltaTime);
                return;
            }

            SetState("idle");
        }

        private void Awake()
        {
            target ??= GetComponent<MigrationCombatTargetBehaviour>();
            damageSource ??= GetComponentInChildren<MigrationEnemyDamageSource>();
            lootDropHandler ??= GetComponent<MigrationCombatLootDropHandler>();
        }

        private void OnEnable()
        {
            target ??= GetComponent<MigrationCombatTargetBehaviour>();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Tick(Time.deltaTime, player.transform.position);
            }
        }

        private void TryAttack(float elapsedDeltaTime, bool ranged, Vector3 playerPosition)
        {
            if (attackCooldownRemaining > 0f)
            {
                SetState(ranged ? "ranged_attack" : "attack");
                return;
            }

            if (!ranged && damageSource == null)
            {
                SetState("attack");
                return;
            }

            if (attackWindupSeconds > 0f)
            {
                windupActive = true;
                windupIsRanged = ranged;
                windupTargetPosition = playerPosition;
                windupRemaining = attackWindupSeconds;
                actionPhaseRemaining = windupRemaining;
                actionHitResolved = false;
                CurrentActionPhase = "telegraph";
                WindupEventCount++;
                ActionTelegraphEventCount++;
                WindupStarted?.Invoke(ranged);
                AdvanceWindup(elapsedDeltaTime);
                return;
            }

            if (attackActiveSeconds > 0f || attackRecoverySeconds > 0f)
            {
                windupActive = true;
                windupIsRanged = ranged;
                windupTargetPosition = playerPosition;
                windupRemaining = 0f;
                actionPhaseRemaining = 0f;
                actionHitResolved = false;
                BeginActiveWindow();
                AdvanceWindup(elapsedDeltaTime);
                return;
            }

            if (ranged)
            {
                PerformRangedAttack(playerPosition);
            }
            else
            {
                PerformMeleeAttack();
            }
        }

        private void AdvanceWindup(float elapsedDeltaTime)
        {
            float safeDeltaTime = Mathf.Max(0f, elapsedDeltaTime);
            if (CurrentActionPhase == "telegraph")
            {
                windupRemaining = Mathf.Max(0f, windupRemaining - safeDeltaTime);
                actionPhaseRemaining = windupRemaining;
                if (windupRemaining > 0f)
                {
                    SetState("windup");
                    return;
                }

                BeginActiveWindow();
                return;
            }

            if (CurrentActionPhase == "active")
            {
                if (!actionHitResolved)
                {
                    ResolveActiveWindow();
                }

                actionPhaseRemaining = Mathf.Max(0f, actionPhaseRemaining - safeDeltaTime);
                if (actionPhaseRemaining > 0f)
                {
                    return;
                }

                BeginRecoveryWindow();
                return;
            }

            if (CurrentActionPhase == "recovery")
            {
                actionPhaseRemaining = Mathf.Max(0f, actionPhaseRemaining - safeDeltaTime);
                if (actionPhaseRemaining > 0f)
                {
                    return;
                }

                windupActive = false;
                CurrentActionPhase = "idle";
                return;
            }

            windupActive = false;
        }

        private void AdvanceStun(float elapsedDeltaTime)
        {
            ResetActionSequence();
            stunRemainingSeconds = Mathf.Max(0f, stunRemainingSeconds - Mathf.Max(0f, elapsedDeltaTime));
            if (stunRemainingSeconds > 0f)
            {
                SetState("stunned");
                return;
            }

            SetState("idle");
        }

        private void PerformMeleeAttack()
        {
            SetState("attack");
            if (attackCooldownRemaining > 0f || damageSource == null)
            {
                return;
            }

            damageSource.TryDamagePlayer();
            AttackEventCount++;
            MeleeAttackPerformed?.Invoke();
            attackCooldownRemaining = attackCooldown;
        }

        private void PerformRangedAttack(Vector3 playerPosition)
        {
            SetState("ranged_attack");
            if (attackCooldownRemaining > 0f)
            {
                return;
            }

            SpawnProjectile(playerPosition);
            ProjectileEventCount++;
            ProjectileAttackPerformed?.Invoke();
            attackCooldownRemaining = attackCooldown;
        }

        private void SpawnProjectile(Vector3 playerPosition)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0f, projectileSpawnHeight, 0f);
            Vector3 direction = playerPosition - spawnPosition;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = transform.forward;
            }

            GameObject projectileObject = projectilePrefab != null
                ? CheckoutProjectileObject(projectilePrefab, spawnPosition)
                : new GameObject("MigrationEnemyProjectile");
            projectileObject.name = "MigrationEnemyProjectile";
            projectileObject.transform.position = spawnPosition;
            MigrationEnemyProjectile projectile = projectileObject.GetComponent<MigrationEnemyProjectile>();
            if (projectile == null)
            {
                projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
            }

            projectile.BindCombat(combatRuntime);
            projectile.Configure(projectileSpeed, currentAttackDamage, direction, true);
            MigrationCombatFeedbackTemplate template = projectileObject.GetComponent<MigrationCombatFeedbackTemplate>();
            if (template != null)
            {
                projectile.ApplyFeedbackTemplate(template);
            }
            else
            {
                projectile.ConfigureFeedback(4f, 0.18f, new Color(1f, 0.2f, 0.12f, 1f));
            }

            LastSpawnedProjectile = projectile;
        }

        private GameObject CheckoutProjectileObject(MigrationEnemyProjectile prefab, Vector3 spawnPosition)
        {
            if (projectilePool != null)
            {
                return projectilePool.Get(prefab.gameObject, spawnPosition, Quaternion.identity);
            }

            return Instantiate(prefab.gameObject);
        }

        private void MoveToward(Vector3 playerPosition, float deltaTime)
        {
            Vector3 direction = playerPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 movement = direction.normalized * moveSpeed * Mathf.Max(0f, deltaTime);
            transform.position += movement;
        }

        private void MoveAwayFrom(Vector3 playerPosition, float deltaTime)
        {
            Vector3 direction = transform.position - playerPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = -transform.forward;
            }

            Vector3 movement = direction.normalized * moveSpeed * Mathf.Max(0f, deltaTime);
            transform.position += movement;
        }

        private void OnTargetDefeated(CombatBridgeResult result)
        {
            stunRemainingSeconds = 0f;
            ResetActionSequence();
            SetState("defeated");
        }

        private void BeginActiveWindow()
        {
            CurrentActionPhase = "active";
            actionPhaseRemaining = attackActiveSeconds;
            ActionActiveEventCount++;
            if (!windupIsRanged)
            {
                damageSource?.SetWindowActive(true);
            }

            ResolveActiveWindow();
        }

        private void ResolveActiveWindow()
        {
            if (actionHitResolved)
            {
                return;
            }

            actionHitResolved = true;
            if (windupIsRanged)
            {
                PerformRangedAttack(windupTargetPosition);
            }
            else
            {
                PerformMeleeAttack();
            }

            if (attackActiveSeconds <= 0f)
            {
                BeginRecoveryWindow();
            }
        }

        private void BeginRecoveryWindow()
        {
            damageSource?.SetWindowActive(false);
            if (attackRecoverySeconds <= 0f)
            {
                windupActive = false;
                CurrentActionPhase = "idle";
                return;
            }

            CurrentActionPhase = "recovery";
            actionPhaseRemaining = attackRecoverySeconds;
            ActionRecoveryEventCount++;
            SetState("recovery");
        }

        private void ResetActionSequence()
        {
            windupActive = false;
            windupRemaining = 0f;
            actionPhaseRemaining = 0f;
            actionHitResolved = false;
            CurrentActionPhase = "idle";
            damageSource?.SetWindowActive(false);
        }

        private void SetState(string state)
        {
            string normalizedState = string.IsNullOrWhiteSpace(state)
                ? "idle"
                : state.Trim().ToLowerInvariant();
            if (CurrentState == normalizedState)
            {
                return;
            }

            CurrentState = normalizedState;
            StateChanged?.Invoke(CurrentState);
        }

        private void Subscribe()
        {
            if (target == null || subscribed)
            {
                return;
            }

            target.Defeated += OnTargetDefeated;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (target == null || !subscribed)
            {
                return;
            }

            target.Defeated -= OnTargetDefeated;
            subscribed = false;
        }
    }
}
