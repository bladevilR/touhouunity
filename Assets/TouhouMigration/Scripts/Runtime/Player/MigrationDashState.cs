using System;

namespace TouhouMigration.Runtime.Player
{
    // Pure dash state machine: a brief active burst window plus a cooldown. The MonoBehaviour player
    // applies movement while IsDashing; this class owns timing/eligibility only. Cooldown starts when the
    // dash starts (Godot intent), so dash cadence is cooldown-limited rather than recovery-then-cooldown.
    public sealed class MigrationDashState
    {
        private float cooldownSeconds = 1.5f;
        private float durationSeconds = 0.2f;
        private float dashSpeed = 14f;
        private float activeRemaining;
        private float cooldownRemaining;

        public bool IsDashing => activeRemaining > 0f;
        public bool IsOnCooldown => cooldownRemaining > 0f;
        public bool CanDash => !IsDashing && !IsOnCooldown;
        public float DashSpeed => dashSpeed;
        public float ActiveRemainingSeconds => activeRemaining;
        public float CooldownRemainingSeconds => cooldownRemaining;

        public void Configure(float cooldown, float duration, float speed)
        {
            cooldownSeconds = Math.Max(0f, cooldown);
            durationSeconds = Math.Max(0f, duration);
            dashSpeed = Math.Max(0f, speed);
        }

        public bool TryStartDash()
        {
            if (!CanDash)
            {
                return false;
            }
            activeRemaining = durationSeconds;
            cooldownRemaining = cooldownSeconds;
            return true;
        }

        public void Tick(float delta)
        {
            if (delta <= 0f)
            {
                return;
            }
            if (activeRemaining > 0f)
            {
                activeRemaining = Math.Max(0f, activeRemaining - delta);
            }
            if (cooldownRemaining > 0f)
            {
                cooldownRemaining = Math.Max(0f, cooldownRemaining - delta);
            }
        }
    }
}
