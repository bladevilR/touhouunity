using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.UI;
using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class MigrationPlayerAttackHitbox : MonoBehaviour
    {
        [SerializeField] private float baseDamage = 10f;
        [SerializeField] private string attackType = "light";
        [SerializeField] private bool activeOnStart;
        [SerializeField] private float rangeMultiplier = 1f;

        private readonly HashSet<MigrationCombatTargetBehaviour> hitTargets = new HashSet<MigrationCombatTargetBehaviour>();
        private readonly HashSet<MigrationEnemyProjectile> hitProjectiles = new HashSet<MigrationEnemyProjectile>();
        private readonly Dictionary<BoxCollider, Vector3> baseBoxColliderSizes = new Dictionary<BoxCollider, Vector3>();
        private readonly Dictionary<SphereCollider, float> baseSphereColliderRadii = new Dictionary<SphereCollider, float>();
        private readonly Dictionary<CapsuleCollider, float> baseCapsuleColliderRadii = new Dictionary<CapsuleCollider, float>();
        private readonly Dictionary<CapsuleCollider, float> baseCapsuleColliderHeights = new Dictionary<CapsuleCollider, float>();
        private MigrationCombatRuntime combatRuntime;

        public event Action<MigrationCombatTargetBehaviour, CombatBridgeResult> HitLanded;

        public bool IsAttackWindowActive { get; private set; }
        public int HitEventCount { get; private set; }
        public int ProjectileShatterEventCount { get; private set; }
        public int ProjectileReflectEventCount { get; private set; }
        public float CurrentRangeMultiplier => rangeMultiplier;

        public void BindCombat(MigrationCombatRuntime combat)
        {
            combatRuntime = combat;
        }

        public void Configure(float baseDamage, string attackType)
        {
            this.baseDamage = Mathf.Max(0f, baseDamage);
            this.attackType = NormalizeAttackType(attackType);
        }

        public void ConfigureRangeMultiplier(float multiplier)
        {
            rangeMultiplier = Mathf.Max(0.01f, multiplier);
            ApplyRangeMultiplierToColliders();
        }

        public void BeginAttackWindow()
        {
            IsAttackWindowActive = true;
            ApplyRangeMultiplierToColliders();
            hitTargets.Clear();
            hitProjectiles.Clear();
        }

        public void EndAttackWindow()
        {
            IsAttackWindowActive = false;
            hitTargets.Clear();
            hitProjectiles.Clear();
        }

        public CombatBridgeResult TryHit(MigrationCombatTargetBehaviour target)
        {
            string normalizedAttackType = NormalizeAttackType(attackType);
            CombatBridgeResult emptyResult = new CombatBridgeResult
            {
                AttackType = normalizedAttackType,
                TargetCurrentHp = target != null ? target.CurrentHp : 0f,
                TargetMaxHp = target != null ? target.MaxHp : 0f
            };

            if (!IsAttackWindowActive || target == null || hitTargets.Contains(target))
            {
                return emptyResult;
            }

            MigrationCombatRuntime combat = ResolveCombatRuntime();
            if (combat == null)
            {
                return emptyResult;
            }

            CombatBridgeResult result = combat.ApplyPlayerAttackToBehaviour(target, baseDamage, normalizedAttackType);
            if (result.DamageApplied > 0f || result.TargetDefeated)
            {
                hitTargets.Add(target);
                HitEventCount++;
                HitLanded?.Invoke(target, result);
            }

            return result;
        }

        public bool TryHitProjectile(MigrationEnemyProjectile projectile, Vector3 hitPosition)
        {
            if (!IsAttackWindowActive || projectile == null || hitProjectiles.Contains(projectile))
            {
                return false;
            }

            if (projectile.Reflectable)
            {
                Vector3 reflectDirection = ResolveProjectileReflectDirection(projectile, hitPosition);
                bool reflected = projectile.TryReflect(NormalizeAttackType(attackType), hitPosition, reflectDirection, this);
                if (reflected)
                {
                    hitProjectiles.Add(projectile);
                    ProjectileReflectEventCount++;
                    return true;
                }
            }

            if (!projectile.Shatterable || projectile.IsShattered)
            {
                return false;
            }

            float previousHp = projectile.ShatterHp;
            bool shattered = projectile.TryApplyShatterDamage(baseDamage, NormalizeAttackType(attackType), hitPosition, this);
            if (shattered || projectile.ShatterHp < previousHp)
            {
                hitProjectiles.Add(projectile);
            }

            if (shattered)
            {
                ProjectileShatterEventCount++;
            }

            return shattered;
        }

        private void Awake()
        {
            IsAttackWindowActive = activeOnStart;
            CaptureBaseColliderShapes();
            ApplyRangeMultiplierToColliders();
        }

        private void OnTriggerEnter(Collider other)
        {
            MigrationEnemyProjectile projectile = other.GetComponentInParent<MigrationEnemyProjectile>();
            if (projectile != null && TryHitProjectile(projectile, other.ClosestPoint(transform.position)))
            {
                return;
            }

            MigrationCombatTargetBehaviour target = other.GetComponentInParent<MigrationCombatTargetBehaviour>();
            TryHit(target);
        }

        private MigrationCombatRuntime ResolveCombatRuntime()
        {
            if (combatRuntime == null)
            {
                combatRuntime = MigrationGlobalUiController.FindCombatRuntime();
            }

            return combatRuntime;
        }

        private static string NormalizeAttackType(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

        private Vector3 ResolveProjectileReflectDirection(MigrationEnemyProjectile projectile, Vector3 hitPosition)
        {
            Vector3 awayFromHitbox = projectile.transform.position - transform.position;
            if (awayFromHitbox.sqrMagnitude > 0.0001f)
            {
                return awayFromHitbox.normalized;
            }

            Vector3 awayFromHitPoint = hitPosition - transform.position;
            if (awayFromHitPoint.sqrMagnitude > 0.0001f)
            {
                return awayFromHitPoint.normalized;
            }

            return Vector3.forward;
        }

        private void ApplyRangeMultiplierToColliders()
        {
            CaptureBaseColliderShapes();

            foreach (KeyValuePair<BoxCollider, Vector3> entry in baseBoxColliderSizes)
            {
                if (entry.Key != null)
                {
                    entry.Key.size = entry.Value * rangeMultiplier;
                }
            }

            foreach (KeyValuePair<SphereCollider, float> entry in baseSphereColliderRadii)
            {
                if (entry.Key != null)
                {
                    entry.Key.radius = entry.Value * rangeMultiplier;
                }
            }

            foreach (KeyValuePair<CapsuleCollider, float> entry in baseCapsuleColliderRadii)
            {
                if (entry.Key != null)
                {
                    entry.Key.radius = entry.Value * rangeMultiplier;
                    entry.Key.height = baseCapsuleColliderHeights[entry.Key] * rangeMultiplier;
                }
            }
        }

        private void CaptureBaseColliderShapes()
        {
            foreach (BoxCollider boxCollider in GetComponents<BoxCollider>())
            {
                if (!baseBoxColliderSizes.ContainsKey(boxCollider))
                {
                    baseBoxColliderSizes.Add(boxCollider, boxCollider.size);
                }
            }

            foreach (SphereCollider sphereCollider in GetComponents<SphereCollider>())
            {
                if (!baseSphereColliderRadii.ContainsKey(sphereCollider))
                {
                    baseSphereColliderRadii.Add(sphereCollider, sphereCollider.radius);
                }
            }

            foreach (CapsuleCollider capsuleCollider in GetComponents<CapsuleCollider>())
            {
                if (!baseCapsuleColliderRadii.ContainsKey(capsuleCollider))
                {
                    baseCapsuleColliderRadii.Add(capsuleCollider, capsuleCollider.radius);
                    baseCapsuleColliderHeights.Add(capsuleCollider, capsuleCollider.height);
                }
            }
        }
    }
}
