using System;

namespace TouhouMigration.Runtime.Fishing
{
    // The catch-bar fishing minigame core (Godot FishingMinigame box physics + catch progress): a catch
    // box drifts down under gravity and rises while lifting; the catch progress climbs while the fish is
    // inside the box and drains otherwise, winning at 1.0 and failing at 0.0. Positions are normalized
    // 0..1. UnityEngine-free + unit-testable; the fish AI / rendering / input are the caller's (the fish
    // position is passed in).
    public sealed class MigrationFishingMinigame
    {
        private const double BoxGravity = 1.8;
        private const double BoxLift = 4.5;
        private const double ProgressGain = 0.4;
        private const double ProgressLoss = 0.25;
        private const double StartProgress = 0.3;

        private double boxVelocity;
        private double boxHalfHeight = 0.1;

        public double CatchProgress { get; private set; }
        public double BoxPosition { get; private set; }
        public bool IsCaught => CatchProgress >= 1.0;
        public bool IsFailed => CatchProgress <= 0.0;
        public bool IsActive { get; private set; }

        // Start a catch (Godot start_game): centre the box, reset progress to 0.3. boxHeight is the box's
        // normalized span (clamped 0.1..0.3, Godot box_ratio).
        public void Start(double boxHeight)
        {
            double clamped = boxHeight < 0.1 ? 0.1 : boxHeight > 0.3 ? 0.3 : boxHeight;
            boxHalfHeight = clamped / 2.0;
            CatchProgress = StartProgress;
            BoxPosition = 0.5;
            boxVelocity = 0.0;
            IsActive = true;
        }

        // Advance the simulation one frame (Godot _process): box physics + catch-progress update.
        public void Tick(double deltaSeconds, bool lifting, double fishPosition)
        {
            if (!IsActive)
            {
                return;
            }

            double dt = Math.Max(0.0, deltaSeconds);

            boxVelocity += (lifting ? BoxLift : -BoxGravity) * dt;
            BoxPosition += boxVelocity * dt;
            if (BoxPosition <= 0.0)
            {
                BoxPosition = 0.0;
                boxVelocity = 0.0;
            }
            else if (BoxPosition >= 1.0)
            {
                BoxPosition = 1.0;
                boxVelocity = 0.0;
            }

            bool fishInBox = Math.Abs(BoxPosition - fishPosition) <= boxHalfHeight;
            CatchProgress += (fishInBox ? ProgressGain : -ProgressLoss) * dt;
            if (CatchProgress >= 1.0)
            {
                CatchProgress = 1.0;
                IsActive = false;
            }
            else if (CatchProgress <= 0.0)
            {
                CatchProgress = 0.0;
                IsActive = false;
            }
        }
    }
}
