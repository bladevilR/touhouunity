using System;

namespace TouhouMigration.Runtime.Fishing
{
    // A playable fishing cast: composes the catch-bar minigame (which lands the fish) with the fishing
    // service (which rolls which fish bites + grants it). CastLine starts the reel; each Reel ticks the
    // minigame, and when the bar fills the service rolls the catch into the inventory; if the bar drains the
    // fish gets away. UnityEngine-free + unit-testable; input/rendering are the caller's.
    public sealed class MigrationFishingSession
    {
        private readonly MigrationFishingService service;
        private readonly int fishingLevel;
        private readonly Func<int, int> nextInt;

        public MigrationFishingSession(MigrationFishingService service, int fishingLevel, Func<int, int> nextInt)
        {
            this.service = service;
            this.fishingLevel = fishingLevel;
            this.nextInt = nextInt ?? (_ => 0);
            Minigame = new MigrationFishingMinigame();
        }

        public MigrationFishingMinigame Minigame { get; }
        public bool IsReeling { get; private set; }
        public bool GotAway { get; private set; }
        public MigrationFishCatchResult LandedCatch { get; private set; }

        public void CastLine(double boxHeight = 0.2)
        {
            Minigame.Start(boxHeight);
            IsReeling = true;
            GotAway = false;
            LandedCatch = null;
        }

        public void Reel(double deltaSeconds, bool lifting, double fishPosition)
        {
            if (!IsReeling)
            {
                return;
            }

            Minigame.Tick(deltaSeconds, lifting, fishPosition);
            if (Minigame.IsCaught)
            {
                LandedCatch = service != null ? service.Catch(nextInt, fishingLevel) : null;
                IsReeling = false;
            }
            else if (Minigame.IsFailed)
            {
                GotAway = true;
                IsReeling = false;
            }
        }
    }
}
