using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class MigrationEnemyAnimationBridge : MonoBehaviour
    {
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int MotionStateHash = Animator.StringToHash("MotionState");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int ProjectileHash = Animator.StringToHash("Projectile");
        private static readonly int TakeDamageHash = Animator.StringToHash("TakeDamage");
        private static readonly int DieHash = Animator.StringToHash("Die");

        [SerializeField] private MigrationSimpleEnemyController controller;
        [SerializeField] private MigrationCombatTargetBehaviour target;
        [SerializeField] private MigrationEnemyAnimationSource animationSource;
        [SerializeField] private Animator animator;

        private bool subscribed;
        private bool targetSubscribed;
        private bool deathTriggered;

        public string LastAnimationState { get; private set; } = "Idle";
        public int LastMotionState { get; private set; }
        public bool IsMoving { get; private set; }
        public int AttackTriggerCount { get; private set; }
        public int ProjectileTriggerCount { get; private set; }
        public int TakeDamageTriggerCount { get; private set; }
        public int DeathTriggerCount { get; private set; }
        public bool UsesFallbackAnimation => animationSource != null && animationSource.UsesFallbackAnimations;

        public void BindController(MigrationSimpleEnemyController controller)
        {
            if (this.controller == controller)
            {
                Subscribe();
                return;
            }

            Unsubscribe();
            this.controller = controller;
            Subscribe();
        }

        public void BindAnimator(Animator animator)
        {
            this.animator = animator;
            SyncNow();
        }

        public void BindTarget(MigrationCombatTargetBehaviour target)
        {
            if (this.target == target)
            {
                Subscribe();
                SyncNow();
                return;
            }

            UnsubscribeTarget();
            this.target = target;
            Subscribe();
            SyncNow();
        }

        public void SyncNow()
        {
            if (target != null && target.IsDefeated)
            {
                ApplyControllerState("defeated");
                return;
            }

            ApplyControllerState(controller != null ? controller.CurrentState : "idle");
        }

        private void Awake()
        {
            controller ??= GetComponent<MigrationSimpleEnemyController>();
            target ??= GetComponent<MigrationCombatTargetBehaviour>();
            animationSource ??= GetComponent<MigrationEnemyAnimationSource>();
            animator ??= GetComponentInChildren<Animator>(true);
        }

        private void OnEnable()
        {
            controller ??= GetComponent<MigrationSimpleEnemyController>();
            target ??= GetComponent<MigrationCombatTargetBehaviour>();
            animationSource ??= GetComponent<MigrationEnemyAnimationSource>();
            animator ??= GetComponentInChildren<Animator>(true);
            Subscribe();
            SyncNow();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            controller ??= GetComponent<MigrationSimpleEnemyController>();
            target ??= GetComponent<MigrationCombatTargetBehaviour>();

            if (controller != null && !subscribed)
            {
                controller.StateChanged += OnControllerStateChanged;
                controller.MeleeAttackPerformed += OnMeleeAttackPerformed;
                controller.ProjectileAttackPerformed += OnProjectileAttackPerformed;
                controller.WindupStarted += OnWindupStarted;
                subscribed = true;
            }

            if (target != null && !targetSubscribed)
            {
                target.Damaged += OnTargetDamaged;
                target.Defeated += OnTargetDefeated;
                targetSubscribed = true;
            }
        }

        private void Unsubscribe()
        {
            if (controller != null && subscribed)
            {
                controller.StateChanged -= OnControllerStateChanged;
                controller.MeleeAttackPerformed -= OnMeleeAttackPerformed;
                controller.ProjectileAttackPerformed -= OnProjectileAttackPerformed;
                controller.WindupStarted -= OnWindupStarted;
                subscribed = false;
            }

            UnsubscribeTarget();
        }

        private void UnsubscribeTarget()
        {
            if (target == null || !targetSubscribed)
            {
                return;
            }

            target.Damaged -= OnTargetDamaged;
            target.Defeated -= OnTargetDefeated;
            targetSubscribed = false;
        }

        private void OnControllerStateChanged(string state)
        {
            ApplyControllerState(state);
        }

        private void OnWindupStarted(bool ranged)
        {
            ApplyAnimationState(ranged ? "Projectile" : "Attack", 0, false);
        }

        private void OnMeleeAttackPerformed()
        {
            AttackTriggerCount++;
            ApplyAnimationState("Attack", 0, false);
            SetTriggerIfPresent(AttackHash);
        }

        private void OnProjectileAttackPerformed()
        {
            ProjectileTriggerCount++;
            ApplyAnimationState("Projectile", 0, false);
            SetTriggerIfPresent(ProjectileHash);
        }

        private void OnTargetDamaged(CombatBridgeResult result)
        {
            ApplyTakeDamageReaction();
        }

        private void OnTargetDefeated(CombatBridgeResult result)
        {
            ApplyDeath();
        }

        private void ApplyControllerState(string state)
        {
            switch (NormalizeState(state))
            {
                case "chase":
                case "ranged_reposition":
                    ApplyAnimationState("Move", 1, true);
                    break;
                case "windup":
                    ApplyAnimationState(LastAnimationState is "Projectile" ? "Projectile" : "Attack", 0, false);
                    break;
                case "attack":
                    ApplyAnimationState("Attack", 0, false);
                    break;
                case "ranged_attack":
                    ApplyAnimationState("Projectile", 0, false);
                    break;
                case "stunned":
                    ApplyTakeDamageReaction();
                    break;
                case "defeated":
                    ApplyDeath();
                    break;
                default:
                    ApplyAnimationState("Idle", 0, false);
                    break;
            }
        }

        private void ApplyDeath()
        {
            ApplyAnimationState("Die", 2, false);
            if (deathTriggered)
            {
                return;
            }

            deathTriggered = true;
            DeathTriggerCount++;
            SetTriggerIfPresent(DieHash);
        }

        private void ApplyTakeDamageReaction()
        {
            TakeDamageTriggerCount++;
            ApplyAnimationState("TakeDamage", 0, false);
            SetTriggerIfPresent(TakeDamageHash);
        }

        private void ApplyAnimationState(string animationState, int motionState, bool isMoving)
        {
            LastAnimationState = animationState;
            LastMotionState = motionState;
            IsMoving = isMoving;

            if (animator == null)
            {
                return;
            }

            SetBoolIfPresent(IsMovingHash, isMoving);
            SetIntegerIfPresent(MotionStateHash, motionState);
        }

        private void SetBoolIfPresent(int parameterHash, bool value)
        {
            if (HasParameter(parameterHash, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(parameterHash, value);
            }
        }

        private void SetIntegerIfPresent(int parameterHash, int value)
        {
            if (HasParameter(parameterHash, AnimatorControllerParameterType.Int))
            {
                animator.SetInteger(parameterHash, value);
            }
        }

        private void SetTriggerIfPresent(int parameterHash)
        {
            if (HasParameter(parameterHash, AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger(parameterHash);
            }
        }

        private bool HasParameter(int parameterHash, AnimatorControllerParameterType parameterType)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == parameterHash && parameter.type == parameterType)
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeState(string state)
        {
            return string.IsNullOrWhiteSpace(state) ? string.Empty : state.Trim().ToLowerInvariant();
        }
    }
}
