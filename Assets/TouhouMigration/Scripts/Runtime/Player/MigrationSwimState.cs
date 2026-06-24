using System;

namespace TouhouMigration.Runtime.Player
{
    // Pure swim/buoyancy model. While submerged, horizontal movement is slowed and vertical motion uses
    // reduced ("buoyant") gravity clamped to gentle sink/rise speeds, instead of full free-fall gravity.
    // Submersion is set by the player controller from water-volume detection (wired during integration).
    public sealed class MigrationSwimState
    {
        private float swimSpeedMultiplier = 0.6f;
        private float buoyancyGravityScale = 0.2f;
        private float maxSinkSpeed = 2f;
        private float maxRiseSpeed = 3f;

        public bool Submerged { get; private set; }
        public bool IsSwimming => Submerged;

        public void SetSubmerged(bool submerged)
        {
            Submerged = submerged;
        }

        public void Configure(float speedMultiplier, float buoyancyScale, float maxSink, float maxRise)
        {
            swimSpeedMultiplier = Math.Max(0f, speedMultiplier);
            buoyancyGravityScale = Math.Max(0f, buoyancyScale);
            maxSinkSpeed = Math.Max(0f, maxSink);
            maxRiseSpeed = Math.Max(0f, maxRise);
        }

        public float ResolveHorizontalSpeed(float baseSpeed)
        {
            return Submerged ? baseSpeed * swimSpeedMultiplier : baseSpeed;
        }

        public float ResolveVerticalVelocity(float currentVertical, float gravity, float delta)
        {
            if (delta <= 0f)
            {
                return currentVertical;
            }
            if (!Submerged)
            {
                return currentVertical + gravity * delta;
            }
            float buoyant = currentVertical + gravity * buoyancyGravityScale * delta;
            return Math.Max(-maxSinkSpeed, Math.Min(maxRiseSpeed, buoyant));
        }
    }
}
