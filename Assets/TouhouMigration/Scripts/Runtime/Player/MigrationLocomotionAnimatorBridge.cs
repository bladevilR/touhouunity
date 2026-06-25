using UnityEngine;

namespace TouhouMigration.Runtime.Player
{
    // Feeds the player's locomotion state into the Humanoid Animator so the rigged character visual
    // plays idle/run animations instead of standing in a static bind pose. Reads
    // MigrationPlayerController.CurrentLocomotion each frame and drives a "Speed" float (0..1) that the
    // locomotion blend tree consumes. Root motion is disabled — the CharacterController owns movement.
    public sealed class MigrationLocomotionAnimatorBridge : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");

        [SerializeField] private Animator animator;
        private MigrationPlayerController controller;

        public void BindAnimator(Animator boundAnimator)
        {
            animator = boundAnimator;
        }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                animator.applyRootMotion = false;
            }

            controller = GetComponent<MigrationPlayerController>() ?? GetComponentInParent<MigrationPlayerController>();
        }

        private void Update()
        {
            if (animator == null || controller == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            MigrationLocomotionParams locomotion = controller.CurrentLocomotion;
            animator.SetFloat(SpeedHash, locomotion.NormalizedSpeed);
            if (HasParameter(GroundedHash))
            {
                animator.SetBool(GroundedHash, locomotion.IsGrounded);
            }
        }

        private bool HasParameter(int hash)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == hash)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
