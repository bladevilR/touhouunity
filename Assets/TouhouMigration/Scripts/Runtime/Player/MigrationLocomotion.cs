using System;

namespace TouhouMigration.Runtime.Player
{
    public readonly struct MigrationLocomotionParams
    {
        public readonly float NormalizedSpeed;
        public readonly bool IsMoving;
        public readonly bool IsRunning;
        public readonly bool IsGrounded;
        public readonly bool IsDashing;

        public MigrationLocomotionParams(float normalizedSpeed, bool isMoving, bool isRunning, bool isGrounded, bool isDashing)
        {
            NormalizedSpeed = normalizedSpeed;
            IsMoving = isMoving;
            IsRunning = isRunning;
            IsGrounded = isGrounded;
            IsDashing = isDashing;
        }
    }

    // Pure mapping from movement state to Animator-facing locomotion parameters. The MonoBehaviour player
    // computes these each frame and pushes them onto a Mecanim humanoid AnimatorController (E1.5). Kept
    // pure so the idle/walk/run/dash/air decision logic is unit-tested without an Animator.
    public static class MigrationLocomotion
    {
        public static MigrationLocomotionParams Resolve(float horizontalSpeed, float walkSpeed, float runSpeed, bool grounded, bool dashing)
        {
            float safeRun = Math.Max(0.0001f, runSpeed);
            float speed = Math.Max(0f, horizontalSpeed);
            float normalized = Math.Min(1f, speed / safeRun);
            bool moving = speed > 0.05f;
            float runThreshold = walkSpeed + Math.Max(0f, runSpeed - walkSpeed) * 0.5f;
            bool running = moving && speed >= runThreshold;
            return new MigrationLocomotionParams(normalized, moving, running, grounded, dashing);
        }
    }
}
