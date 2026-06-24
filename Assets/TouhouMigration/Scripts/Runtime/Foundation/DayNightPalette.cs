using System;
using UnityEngine;

namespace TouhouMigration.Runtime.Foundation
{
    public sealed class DayNightPalette
    {
        public DayNightLightingProfile GetProfile(string period)
        {
            if (!Enum.TryParse(period, true, out GameTimePeriod parsed))
            {
                parsed = GameTimePeriod.Morning;
            }

            return GetProfileForPeriod(parsed);
        }

        public DayNightLightingProfile GetProfileForPeriod(GameTimePeriod period)
        {
            return period switch
            {
                GameTimePeriod.Dawn => new DayNightLightingProfile(new Color(1.0f, 0.85f, 0.7f, 1.0f), 0.7f),
                GameTimePeriod.Morning => new DayNightLightingProfile(Color.white, 1.0f),
                GameTimePeriod.Noon => new DayNightLightingProfile(new Color(1.0f, 0.98f, 0.95f, 1.0f), 1.0f),
                GameTimePeriod.Afternoon => new DayNightLightingProfile(new Color(1.0f, 0.95f, 0.9f, 1.0f), 0.95f),
                GameTimePeriod.Evening => new DayNightLightingProfile(new Color(1.0f, 0.7f, 0.5f, 1.0f), 0.75f),
                GameTimePeriod.Night => new DayNightLightingProfile(new Color(0.4f, 0.4f, 0.6f, 1.0f), 0.4f),
                GameTimePeriod.Midnight => new DayNightLightingProfile(new Color(0.2f, 0.2f, 0.35f, 1.0f), 0.25f),
                _ => new DayNightLightingProfile(Color.white, 1.0f)
            };
        }
    }
}
