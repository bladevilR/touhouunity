using System;
using TouhouMigration.Runtime.Player;
using TouhouMigration.Runtime.UI;
using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class MigrationEnemyProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 8f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private bool isEnemyProjectile = true;
        [SerializeField] private float hitRadius = 0.45f;
        [SerializeField] private Vector3 direction = Vector3.forward;
        [SerializeField] private float lifetimeSeconds = 4f;
        [SerializeField] private float visualRadius = 0.18f;
        [SerializeField] private Color projectileColor = new Color(1f, 0.2f, 0.12f, 1f);
        [SerializeField] private bool poolingReady;
        [SerializeField] private bool impactFeedbackEnabled;
        [SerializeField] private bool sweepCollisionEnabled;
        [SerializeField] private bool environmentImpactEnabled;
        [SerializeField] private int environmentLayerMask = Physics.DefaultRaycastLayers;
        [SerializeField] private bool grazeEnabled;
        [SerializeField] private float grazeRadius;
        [SerializeField] private float perfectGrazeRadius;
        [SerializeField] private string projectileFamily = "enemy_projectile";
        [SerializeField] private bool shatterable;
        [SerializeField] private float shatterHp;
        [SerializeField] private string shatterWeaknesses = string.Empty;
        [SerializeField] private bool reflectable;
        [SerializeField] private bool reflectStunReward;
        [SerializeField] private float reflectStunSeconds;
        [SerializeField] private bool perfectFreezeCycleEnabled;
        [SerializeField] private string currentPerfectFreezeState = string.Empty;
        [SerializeField] private float perfectFreezePhaseElapsed;
        [SerializeField] private float perfectFreezeSpraySeconds = 1.6f;
        [SerializeField] private float perfectFreezeFreezeSeconds = 2.4f;
        [SerializeField] private float perfectFreezeSpraySpeed = 4.2f;
        [SerializeField] private float perfectFreezeSprayDamage = 8f;
        [SerializeField] private float perfectFreezeFrozenDamage = 7f;
        [SerializeField] private float perfectFreezeThawSpeed = 8f;
        [SerializeField] private float perfectFreezeThawDamage = 10f;
        [SerializeField] private float perfectFreezeFrozenShatterHp = 20f;
        [SerializeField] private float armDelaySeconds;

        private MigrationCombatRuntime combatRuntime;
        private bool hasHit;
        private float ageSeconds;
        private bool isExpired;
        [SerializeField] private bool isArmed = true;
        [SerializeField] private float armDelayRemainingSeconds;
        private bool hasVisualFeedback;
        private bool usesFeedbackTemplate;
        private bool hasActiveImpactFeedback;
        private GameObject visualObject;
        private MeshRenderer visualRenderer;
        private TrailRenderer trailRenderer;
        private ParticleSystem impactParticles;
        private bool hasGrazedPlayer;
        private bool isShattered;
        private bool isReflected;

        public event Action<MigrationProjectileGrazeResult> Grazed;
        public event Action<MigrationProjectileShatterResult> Shattered;
        public event Action<MigrationProjectileReflectResult> Reflected;

        public float Speed => speed;
        public float Damage => damage;
        public bool IsEnemyProjectile => isEnemyProjectile;
        public float HitRadius => hitRadius;
        public float LifetimeSeconds => lifetimeSeconds;
        public bool IsExpired => isExpired;
        public bool HasVisualFeedback => hasVisualFeedback;
        public bool UsesFeedbackTemplate => usesFeedbackTemplate;
        public bool PoolingReady => poolingReady;
        public bool SweepCollisionEnabled => sweepCollisionEnabled;
        public bool EnvironmentImpactEnabled => environmentImpactEnabled;
        public bool GrazeEnabled => grazeEnabled;
        public float GrazeRadius => grazeRadius;
        public float PerfectGrazeRadius => perfectGrazeRadius;
        public string ProjectileFamily => projectileFamily;
        public bool Shatterable => shatterable;
        public float ShatterHp => shatterHp;
        public string ShatterWeaknesses => shatterWeaknesses;
        public bool Reflectable => reflectable;
        public bool ReflectStunReward => reflectStunReward;
        public float ReflectStunSeconds => reflectStunSeconds;
        public bool IsShattered => isShattered;
        public bool IsReflected => isReflected;
        public bool HasActiveImpactFeedback => hasActiveImpactFeedback;
        public bool PerfectFreezeCycleEnabled => perfectFreezeCycleEnabled;
        public string CurrentPerfectFreezeState => currentPerfectFreezeState;
        public bool IsFrozen => currentPerfectFreezeState == "frozen";
        public float PerfectFreezePhaseElapsed => perfectFreezePhaseElapsed;
        public float PerfectFreezeSpraySeconds => perfectFreezeSpraySeconds;
        public float PerfectFreezeFreezeSeconds => perfectFreezeFreezeSeconds;
        public float PerfectFreezeFrozenShatterHp => perfectFreezeFrozenShatterHp;
        public float ArmDelaySeconds => armDelaySeconds;
        public float ArmDelayRemainingSeconds => armDelayRemainingSeconds;
        public bool IsArmed => isArmed;
        public int HitEventCount { get; private set; }
        public int ExpiredEventCount { get; private set; }
        public int ImpactEventCount { get; private set; }
        public int EnvironmentImpactEventCount { get; private set; }
        public int GrazeEventCount { get; private set; }
        public int ShatterEventCount { get; private set; }
        public int ReflectEventCount { get; private set; }
        public Vector3 LastEnvironmentImpactPoint { get; private set; }
        public string LastGrazeQuality { get; private set; } = string.Empty;
        public float LastGrazeDistance { get; private set; }
        public string LastShatterSourceFamily { get; private set; } = string.Empty;
        public float LastShatterDamageApplied { get; private set; }
        public float LastShatterRemainingHp { get; private set; }
        public Vector3 LastShatterPosition { get; private set; }
        public string LastReflectSourceFamily { get; private set; } = string.Empty;
        public bool LastReflectStunReward { get; private set; }
        public float LastReflectStunSeconds { get; private set; }
        public Vector3 LastReflectDirection { get; private set; }
        public Vector3 LastReflectPosition { get; private set; }

        public void BindCombat(MigrationCombatRuntime combat)
        {
            combatRuntime = combat;
        }

        public void Configure(float speed, float damage, Vector3 direction, bool isEnemyProjectile = true, float hitRadius = 0.45f)
        {
            this.speed = Mathf.Max(0f, speed);
            this.damage = Mathf.Max(0f, damage);
            this.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            this.isEnemyProjectile = isEnemyProjectile;
            this.hitRadius = Mathf.Max(0f, hitRadius);
            hasHit = false;
            isExpired = false;
            isShattered = false;
            ResetReflectState();
            hasActiveImpactFeedback = false;
            StopImpactFeedback();
            ResetArmDelayState();
            ageSeconds = 0f;
            HitEventCount = 0;
            ExpiredEventCount = 0;
            ImpactEventCount = 0;
            EnvironmentImpactEventCount = 0;
            GrazeEventCount = 0;
            ShatterEventCount = 0;
            LastEnvironmentImpactPoint = Vector3.zero;
            LastGrazeQuality = string.Empty;
            LastGrazeDistance = 0f;
            LastShatterSourceFamily = string.Empty;
            LastShatterDamageApplied = 0f;
            LastShatterRemainingHp = shatterHp;
            LastShatterPosition = Vector3.zero;
            hasGrazedPlayer = false;
            if (perfectFreezeCycleEnabled)
            {
                lifetimeSeconds = Mathf.Max(lifetimeSeconds, perfectFreezeSpraySeconds + perfectFreezeFreezeSeconds + 1f);
                ApplyPerfectFreezeSprayState();
            }
            else
            {
                currentPerfectFreezeState = string.Empty;
                perfectFreezePhaseElapsed = 0f;
            }

            SetVisualEnabled(true);
        }

        public void ConfigureFeedback(float lifetimeSeconds, float visualRadius, Color color)
        {
            this.lifetimeSeconds = Mathf.Max(0f, lifetimeSeconds);
            this.visualRadius = Mathf.Max(0.01f, visualRadius);
            projectileColor = color;
            EnsureVisualFeedback();
            ApplyVisualFeedback();
        }

        public void ApplyFeedbackTemplate(MigrationCombatFeedbackTemplate template)
        {
            if (template == null)
            {
                return;
            }

            usesFeedbackTemplate = true;
            poolingReady = template.PoolingReady;
            impactFeedbackEnabled = template.ImpactFeedbackEnabled;
            sweepCollisionEnabled = template.SweepCollisionEnabled;
            ConfigureEnvironmentImpact(template.ImpactFeedbackEnabled && template.SweepCollisionEnabled, Physics.DefaultRaycastLayers);
            ConfigureGraze(template.GrazeEnabled, template.GrazeRadius, template.PerfectGrazeRadius);
            ConfigureShatterRules(template.ProjectileFamily, template.Shatterable, template.ShatterHp, template.ShatterWeaknesses);
            ConfigureReflectRules(template.Reflectable, template.ReflectStunReward, template.ReflectStunSeconds);
            ConfigurePerfectFreezeCycle(
                template.PerfectFreezeCycleEnabled,
                template.PerfectFreezeSpraySeconds,
                template.PerfectFreezeFreezeSeconds,
                template.PerfectFreezeSpraySpeed,
                template.PerfectFreezeSprayDamage,
                template.PerfectFreezeFrozenDamage,
                template.PerfectFreezeThawSpeed,
                template.PerfectFreezeThawDamage,
                template.PerfectFreezeFrozenShatterHp);
            ConfigureArmDelay(template.ArmDelaySeconds);
            MigrationCombatFeedbackTemplate.ApplyLayerPolicy(gameObject, template.LayerName);
            ConfigureFeedback(template.LifetimeSeconds, template.VisualRadius, template.FeedbackColor);
            EnsureColliderPolicy(template.VisualRadius);
        }

        public void ConfigureArmDelay(float seconds)
        {
            armDelaySeconds = Mathf.Max(0f, seconds);
            ResetArmDelayState();
        }

        public void ConfigureEnvironmentImpact(bool enabled, int layerMask)
        {
            environmentImpactEnabled = enabled;
            environmentLayerMask = layerMask == 0 ? Physics.DefaultRaycastLayers : layerMask;
        }

        public void ConfigureGraze(bool enabled, float grazeRadius, float perfectGrazeRadius)
        {
            grazeEnabled = enabled;
            this.grazeRadius = Mathf.Max(0f, grazeRadius);
            this.perfectGrazeRadius = Mathf.Min(this.grazeRadius, Mathf.Max(0f, perfectGrazeRadius));
            hasGrazedPlayer = false;
            GrazeEventCount = 0;
            LastGrazeQuality = string.Empty;
            LastGrazeDistance = 0f;
        }

        public void ConfigureShatterRules(string family, bool shatterable, float shatterHp, string weakTo)
        {
            projectileFamily = string.IsNullOrWhiteSpace(family) ? string.Empty : family.Trim();
            this.shatterable = shatterable;
            this.shatterHp = Mathf.Max(0f, shatterHp);
            shatterWeaknesses = NormalizeWeaknessCsv(weakTo);
            isShattered = false;
            ShatterEventCount = 0;
            LastShatterSourceFamily = string.Empty;
            LastShatterDamageApplied = 0f;
            LastShatterRemainingHp = this.shatterHp;
            LastShatterPosition = Vector3.zero;
        }

        public void ConfigureReflectRules(bool reflectable, bool stunReward, float stunSeconds)
        {
            this.reflectable = reflectable;
            reflectStunReward = stunReward;
            reflectStunSeconds = Mathf.Max(0f, stunSeconds);
            ResetReflectState();
        }

        public void ConfigurePerfectFreezeCycle(
            bool enabled,
            float spraySeconds,
            float freezeSeconds,
            float spraySpeed,
            float sprayDamage,
            float frozenDamage,
            float thawSpeed,
            float thawDamage,
            float frozenShatterHp)
        {
            perfectFreezeCycleEnabled = enabled;
            perfectFreezeSpraySeconds = Mathf.Max(0f, spraySeconds);
            perfectFreezeFreezeSeconds = Mathf.Max(0f, freezeSeconds);
            perfectFreezeSpraySpeed = Mathf.Max(0f, spraySpeed);
            perfectFreezeSprayDamage = Mathf.Max(0f, sprayDamage);
            perfectFreezeFrozenDamage = Mathf.Max(0f, frozenDamage);
            perfectFreezeThawSpeed = Mathf.Max(0f, thawSpeed);
            perfectFreezeThawDamage = Mathf.Max(0f, thawDamage);
            perfectFreezeFrozenShatterHp = Mathf.Max(0f, frozenShatterHp);

            if (perfectFreezeCycleEnabled)
            {
                lifetimeSeconds = Mathf.Max(lifetimeSeconds, perfectFreezeSpraySeconds + perfectFreezeFreezeSeconds + 1f);
                ApplyPerfectFreezeSprayState();
            }
            else
            {
                currentPerfectFreezeState = string.Empty;
                perfectFreezePhaseElapsed = 0f;
            }
        }

        public bool IsWeakTo(string sourceFamily)
        {
            string normalizedSource = NormalizeFamily(sourceFamily);
            if (string.IsNullOrEmpty(normalizedSource) || string.IsNullOrWhiteSpace(shatterWeaknesses))
            {
                return false;
            }

            string[] weaknesses = shatterWeaknesses.Split(',');
            for (int index = 0; index < weaknesses.Length; index++)
            {
                if (NormalizeFamily(weaknesses[index]) == normalizedSource)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryApplyShatterDamage(float amount, string sourceFamily, Vector3 hitPosition, UnityEngine.Object source = null)
        {
            if (amount <= 0f || !shatterable || isShattered || hasHit || isExpired)
            {
                return false;
            }

            string normalizedSourceFamily = NormalizeFamily(sourceFamily);
            bool wasWeakness = IsWeakTo(normalizedSourceFamily);
            float multiplier = wasWeakness ? 1.5f : 1f;
            float damageApplied = Mathf.Max(0f, amount) * multiplier;
            shatterHp = Mathf.Max(0f, shatterHp - damageApplied);
            LastShatterSourceFamily = normalizedSourceFamily;
            LastShatterDamageApplied = damageApplied;
            LastShatterRemainingHp = shatterHp;
            LastShatterPosition = hitPosition;

            if (shatterHp > 0.0001f)
            {
                return false;
            }

            ApplyShatter(hitPosition, normalizedSourceFamily, Mathf.Max(0f, amount), multiplier, damageApplied, wasWeakness, source);
            return true;
        }

        public bool TryReflect(string sourceFamily, Vector3 hitPosition, Vector3 reflectedDirection, UnityEngine.Object source = null)
        {
            if (!reflectable || isReflected || hasHit || isExpired || isShattered || !isArmed)
            {
                return false;
            }

            Vector3 normalizedDirection = reflectedDirection.sqrMagnitude > 0.0001f
                ? reflectedDirection.normalized
                : ResolveFallbackReflectDirection();
            string normalizedSourceFamily = NormalizeFamily(sourceFamily);
            ApplyReflect(hitPosition, normalizedSourceFamily, normalizedDirection, source);
            return true;
        }

        public void Tick(float deltaTime, Vector3 playerPosition)
        {
            if (hasHit || isExpired)
            {
                return;
            }

            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            ageSeconds += safeDeltaTime;
            if (!isArmed)
            {
                armDelayRemainingSeconds = Mathf.Max(0f, armDelayRemainingSeconds - safeDeltaTime);
                if (armDelayRemainingSeconds <= 0f)
                {
                    isArmed = true;
                }

                if (lifetimeSeconds > 0f && ageSeconds >= lifetimeSeconds - 0.0001f)
                {
                    Expire();
                }

                return;
            }

            UpdatePerfectFreezeCycle(safeDeltaTime);
            Vector3 startPosition = transform.position;
            Vector3 nextPosition = startPosition + direction * speed * safeDeltaTime;
            if (TryResolveEnvironmentImpact(startPosition, nextPosition, out Vector3 environmentImpactPoint))
            {
                TryApplyGraze(startPosition, environmentImpactPoint, playerPosition);
                transform.position = environmentImpactPoint;
                ApplyEnvironmentImpact(environmentImpactPoint);
                return;
            }

            transform.position = nextPosition;
            float playerDistance = sweepCollisionEnabled
                ? DistancePointToSegment(playerPosition, startPosition, transform.position)
                : Vector3.Distance(transform.position, playerPosition);
            bool playerHit = playerDistance <= hitRadius;
            if (isEnemyProjectile && playerHit)
            {
                ApplyPlayerHit(playerPosition);
            }
            else
            {
                TryApplyGraze(startPosition, transform.position, playerPosition);
            }

            if (!hasHit && lifetimeSeconds > 0f && ageSeconds >= lifetimeSeconds - 0.0001f)
            {
                Expire();
            }
        }

        private void Update()
        {
            if (!isEnemyProjectile || hasHit || isExpired)
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Tick(Time.deltaTime, player.transform.position);
            }
        }

        private void UpdatePerfectFreezeCycle(float deltaTime)
        {
            if (!perfectFreezeCycleEnabled || string.IsNullOrEmpty(currentPerfectFreezeState))
            {
                return;
            }

            perfectFreezePhaseElapsed += Mathf.Max(0f, deltaTime);
            if (currentPerfectFreezeState == "spray" && perfectFreezePhaseElapsed + 0.0001f >= perfectFreezeSpraySeconds)
            {
                ApplyPerfectFreezeFrozenState();
            }
            else if (currentPerfectFreezeState == "frozen" && perfectFreezePhaseElapsed + 0.0001f >= perfectFreezeFreezeSeconds)
            {
                ApplyPerfectFreezeThawedState();
            }
        }

        private void ApplyPerfectFreezeSprayState()
        {
            currentPerfectFreezeState = "spray";
            perfectFreezePhaseElapsed = 0f;
            speed = perfectFreezeSpraySpeed;
            damage = perfectFreezeSprayDamage;
            shatterable = false;
            shatterHp = perfectFreezeFrozenShatterHp;
            LastShatterRemainingHp = shatterHp;
        }

        private void ApplyPerfectFreezeFrozenState()
        {
            currentPerfectFreezeState = "frozen";
            perfectFreezePhaseElapsed = 0f;
            speed = 0f;
            damage = perfectFreezeFrozenDamage;
            projectileFamily = "frozen_crystal";
            shatterable = true;
            shatterHp = perfectFreezeFrozenShatterHp;
            LastShatterRemainingHp = shatterHp;
            shatterWeaknesses = MergeWeaknesses(shatterWeaknesses, "fire,heavy,shatter");
        }

        private void ApplyPerfectFreezeThawedState()
        {
            currentPerfectFreezeState = "thawed";
            perfectFreezePhaseElapsed = 0f;
            speed = perfectFreezeThawSpeed;
            damage = perfectFreezeThawDamage;
            shatterable = false;
            shatterHp = 0f;
            LastShatterRemainingHp = shatterHp;
        }

        private void ResetArmDelayState()
        {
            armDelayRemainingSeconds = armDelaySeconds;
            isArmed = armDelaySeconds <= 0.0001f;
            if (isArmed)
            {
                armDelayRemainingSeconds = 0f;
            }
        }

        private void ResetReflectState()
        {
            isReflected = false;
            ReflectEventCount = 0;
            LastReflectSourceFamily = string.Empty;
            LastReflectStunReward = false;
            LastReflectStunSeconds = 0f;
            LastReflectDirection = Vector3.zero;
            LastReflectPosition = Vector3.zero;
        }

        private void ApplyPlayerHit(Vector3 impactPosition)
        {
            MigrationCombatRuntime combat = combatRuntime ?? MigrationGlobalUiController.FindCombatRuntime();
            PlayerHealthResult result = combat != null
                ? combat.ApplyDamageToPlayer(damage)
                : new PlayerHealthResult { RawDamage = damage };

            if (result.DamageApplied > 0f || result.RebirthTriggered || combat == null)
            {
                HitEventCount++;
            }

            hasHit = true;
            SpawnImpactFeedback(impactPosition);
            SetVisualEnabled(false);
        }

        private void ApplyEnvironmentImpact(Vector3 impactPosition)
        {
            if (hasHit || isExpired)
            {
                return;
            }

            isExpired = true;
            EnvironmentImpactEventCount++;
            LastEnvironmentImpactPoint = impactPosition;
            SpawnImpactFeedback(impactPosition);
            SetVisualEnabled(false);
        }

        private bool TryApplyGraze(Vector3 startPosition, Vector3 endPosition, Vector3 playerPosition)
        {
            if (!isEnemyProjectile || !grazeEnabled || grazeRadius <= 0f || hasGrazedPlayer || hasHit || isExpired)
            {
                return false;
            }

            Vector3 closestPoint = ClosestPointOnSegment(playerPosition, startPosition, endPosition);
            float distance = Vector3.Distance(playerPosition, closestPoint);
            if (distance > grazeRadius)
            {
                return false;
            }

            if (hitRadius > 0f && distance <= hitRadius)
            {
                return false;
            }

            hasGrazedPlayer = true;
            GrazeEventCount++;
            LastGrazeDistance = distance;
            LastGrazeQuality = perfectGrazeRadius > 0f && distance <= perfectGrazeRadius
                ? "perfect"
                : "normal";

            Grazed?.Invoke(new MigrationProjectileGrazeResult(
                this,
                LastGrazeQuality,
                distance,
                hitRadius,
                grazeRadius,
                perfectGrazeRadius,
                playerPosition,
                closestPoint));
            return true;
        }

        private void ApplyShatter(
            Vector3 hitPosition,
            string sourceFamily,
            float rawDamage,
            float damageMultiplier,
            float damageApplied,
            bool wasWeakness,
            UnityEngine.Object source)
        {
            if (isShattered)
            {
                return;
            }

            isShattered = true;
            isExpired = true;
            ShatterEventCount++;
            SpawnImpactFeedback(hitPosition);
            SetVisualEnabled(false);
            Shattered?.Invoke(new MigrationProjectileShatterResult(
                this,
                projectileFamily,
                sourceFamily,
                rawDamage,
                damageMultiplier,
                damageApplied,
                shatterHp,
                hitPosition,
                wasWeakness,
                source));
        }

        private void ApplyReflect(
            Vector3 hitPosition,
            string sourceFamily,
            Vector3 reflectedDirection,
            UnityEngine.Object source)
        {
            if (isReflected)
            {
                return;
            }

            isReflected = true;
            isExpired = true;
            ReflectEventCount++;
            LastReflectSourceFamily = sourceFamily;
            LastReflectStunReward = reflectStunReward;
            LastReflectStunSeconds = reflectStunSeconds;
            LastReflectDirection = reflectedDirection;
            LastReflectPosition = hitPosition;
            SpawnImpactFeedback(hitPosition);
            SetVisualEnabled(false);
            Reflected?.Invoke(new MigrationProjectileReflectResult(
                this,
                projectileFamily,
                sourceFamily,
                hitPosition,
                reflectedDirection,
                speed,
                damage,
                reflectStunReward,
                reflectStunSeconds,
                source));
        }

        private void Expire()
        {
            if (isExpired)
            {
                return;
            }

            isExpired = true;
            ExpiredEventCount++;
            SetVisualEnabled(false);
        }

        private Vector3 ResolveFallbackReflectDirection()
        {
            if (direction.sqrMagnitude > 0.0001f)
            {
                return -direction.normalized;
            }

            return Vector3.back;
        }

        private void EnsureVisualFeedback()
        {
            if (visualObject == null)
            {
                visualObject = gameObject;
            }

            MeshFilter meshFilter = visualObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = visualObject.AddComponent<MeshFilter>();
            }

            if (meshFilter.sharedMesh == null)
            {
                meshFilter.sharedMesh = CreateProjectileMesh();
            }

            if (visualRenderer == null)
            {
                visualRenderer = visualObject.GetComponent<MeshRenderer>();
            }

            if (visualRenderer == null)
            {
                visualRenderer = visualObject.AddComponent<MeshRenderer>();
            }

            if (trailRenderer == null)
            {
                trailRenderer = gameObject.GetComponent<TrailRenderer>();
            }

            if (trailRenderer == null)
            {
                trailRenderer = gameObject.AddComponent<TrailRenderer>();
            }

            hasVisualFeedback = true;
        }

        private void EnsureColliderPolicy(float radius)
        {
            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider == null)
            {
                sphereCollider = gameObject.AddComponent<SphereCollider>();
            }

            sphereCollider.isTrigger = true;
            sphereCollider.radius = Mathf.Max(0.01f, radius);

            Rigidbody rigidbody = GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = gameObject.AddComponent<Rigidbody>();
            }

            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
        }

        private void SpawnImpactFeedback(Vector3 impactPosition)
        {
            if (!impactFeedbackEnabled)
            {
                return;
            }

            if (impactParticles == null)
            {
                GameObject impactObject = new GameObject("ProjectileImpactFeedback");
                impactObject.transform.SetParent(transform, false);
                impactParticles = impactObject.AddComponent<ParticleSystem>();

                ParticleSystem.MainModule main = impactParticles.main;
                main.loop = false;
                main.duration = 0.18f;
                main.startLifetime = 0.18f;
                main.startSpeed = visualRadius * 5f;
                main.startSize = visualRadius * 0.9f;
                main.startColor = projectileColor;

                ParticleSystem.EmissionModule emission = impactParticles.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[]
                {
                    new ParticleSystem.Burst(0f, (short)10)
                });

                ParticleSystem.ShapeModule shape = impactParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = visualRadius;
            }

            impactParticles.transform.position = impactPosition;
            impactParticles.gameObject.SetActive(true);
            impactParticles.Clear(true);
            impactParticles.Play(true);
            hasActiveImpactFeedback = true;
            ImpactEventCount++;
        }

        private void StopImpactFeedback()
        {
            if (impactParticles == null)
            {
                return;
            }

            impactParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            impactParticles.Clear(true);
            impactParticles.gameObject.SetActive(false);
        }

        private bool TryResolveEnvironmentImpact(Vector3 startPosition, Vector3 nextPosition, out Vector3 impactPosition)
        {
            impactPosition = Vector3.zero;
            if (!environmentImpactEnabled)
            {
                return false;
            }

            Vector3 segment = nextPosition - startPosition;
            float distance = segment.magnitude;
            if (distance <= 0.0001f)
            {
                return false;
            }

            RaycastHit[] hits = Physics.RaycastAll(
                startPosition,
                segment / distance,
                distance,
                environmentLayerMask,
                QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                {
                    continue;
                }

                Transform hitTransform = hit.collider.transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform) || hit.collider.gameObject.CompareTag("Player"))
                {
                    continue;
                }

                impactPosition = hit.point;
                return true;
            }

            return false;
        }

        private static string NormalizeFamily(string family)
        {
            return string.IsNullOrWhiteSpace(family) ? string.Empty : family.Trim().ToLowerInvariant();
        }

        private static string NormalizeWeaknessCsv(string weakTo)
        {
            if (string.IsNullOrWhiteSpace(weakTo))
            {
                return string.Empty;
            }

            string[] rawWeaknesses = weakTo.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (rawWeaknesses.Length == 0)
            {
                return string.Empty;
            }

            for (int index = 0; index < rawWeaknesses.Length; index++)
            {
                rawWeaknesses[index] = NormalizeFamily(rawWeaknesses[index]);
            }

            return string.Join(",", rawWeaknesses);
        }

        private static string MergeWeaknesses(string existingWeaknesses, string addedWeaknesses)
        {
            string normalizedExisting = NormalizeWeaknessCsv(existingWeaknesses);
            string normalizedAdded = NormalizeWeaknessCsv(addedWeaknesses);
            if (string.IsNullOrEmpty(normalizedExisting))
            {
                return normalizedAdded;
            }

            if (string.IsNullOrEmpty(normalizedAdded))
            {
                return normalizedExisting;
            }

            string[] merged = (normalizedExisting + "," + normalizedAdded).Split(
                new[] { ',' },
                StringSplitOptions.RemoveEmptyEntries);
            int writeIndex = 0;
            for (int readIndex = 0; readIndex < merged.Length; readIndex++)
            {
                string candidate = NormalizeFamily(merged[readIndex]);
                bool seen = false;
                for (int compareIndex = 0; compareIndex < writeIndex; compareIndex++)
                {
                    if (merged[compareIndex] == candidate)
                    {
                        seen = true;
                        break;
                    }
                }

                if (!seen)
                {
                    merged[writeIndex] = candidate;
                    writeIndex++;
                }
            }

            Array.Resize(ref merged, writeIndex);
            return string.Join(",", merged);
        }

        private void ApplyVisualFeedback()
        {
            if (visualObject != null)
            {
                visualObject.transform.localScale = Vector3.one * visualRadius * 2f;
                if (visualRenderer != null)
                {
                    visualRenderer.sharedMaterial = CreateProjectileMaterial();
                    visualRenderer.enabled = !isExpired && !hasHit;
                }
            }

            if (trailRenderer != null)
            {
                trailRenderer.time = 0.18f;
                trailRenderer.startWidth = visualRadius * 1.2f;
                trailRenderer.endWidth = 0.01f;
                trailRenderer.numCornerVertices = 4;
                trailRenderer.numCapVertices = 4;
                trailRenderer.material = CreateProjectileMaterial();
                trailRenderer.startColor = projectileColor;
                trailRenderer.endColor = new Color(projectileColor.r, projectileColor.g, projectileColor.b, 0f);
                trailRenderer.enabled = true;
                trailRenderer.emitting = !isExpired && !hasHit;
            }
        }

        private void SetVisualEnabled(bool enabled)
        {
            if (visualRenderer == null && visualObject != null)
            {
                visualRenderer = visualObject.GetComponent<MeshRenderer>();
            }

            if (visualRenderer != null)
            {
                visualRenderer.enabled = enabled;
            }

            if (trailRenderer != null)
            {
                trailRenderer.emitting = enabled;
            }
        }

        private Material CreateProjectileMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            material.color = projectileColor;
            return material;
        }

        private static float DistancePointToSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector3 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= 0.0001f)
            {
                return Vector3.Distance(point, end);
            }

            float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
            Vector3 closest = start + segment * t;
            return Vector3.Distance(point, closest);
        }

        private static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector3 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= 0.0001f)
            {
                return end;
            }

            float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
            return start + segment * t;
        }

        private static Mesh CreateProjectileMesh()
        {
            Mesh mesh = new Mesh { name = "MigrationEnemyProjectileVisualMesh" };
            mesh.vertices = new[]
            {
                Vector3.up,
                Vector3.down,
                Vector3.left,
                Vector3.right,
                Vector3.forward,
                Vector3.back
            };
            mesh.triangles = new[]
            {
                0, 4, 3,
                0, 3, 5,
                0, 5, 2,
                0, 2, 4,
                1, 3, 4,
                1, 5, 3,
                1, 2, 5,
                1, 4, 2
            };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
