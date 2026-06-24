using System.Collections.Generic;
using System;

namespace TouhouMigration.Runtime.Social
{
    public sealed class SocialBondService
    {
        private static readonly int[] PointsPerLevel =
        {
            0,
            100,
            250,
            500,
            800,
            1200,
            1700,
            2300,
            3000,
            4000,
            5000
        };

        private static readonly Dictionary<string, int> SourceBasePoints = new Dictionary<string, int>
        {
            ["dialogue"] = 10,
            ["gift"] = 20,
            ["gift_loved"] = 50,
            ["quest_help"] = 30,
            ["combat_together"] = 25,
            ["event_completion"] = 40,
            ["daily_interaction"] = 5
        };

        private readonly Dictionary<string, BondRecord> bondsByNpc = new Dictionary<string, BondRecord>();
        private readonly HashSet<string> dailyInteracted = new HashSet<string>();

        public string LastNpcId { get; private set; } = string.Empty;
        public int LastDelta { get; private set; }
        public string LastSource { get; private set; } = string.Empty;
        public int LastOldLevel { get; private set; }
        public int LastNewLevel { get; private set; }
        public int MaxBondLevel => 10;

        public int GetBondPoints(string npcId)
        {
            return EnsureBond(NormalizeId(npcId)).Points;
        }

        public int GetBondLevel(string npcId)
        {
            return EnsureBond(NormalizeId(npcId)).Level;
        }

        public int GetPointsForNextLevel(string npcId)
        {
            int level = GetBondLevel(npcId);
            return level >= MaxBondLevel ? 0 : PointsPerLevel[level];
        }

        public float GetBondProgress(string npcId)
        {
            int level = GetBondLevel(npcId);
            if (level >= MaxBondLevel)
            {
                return 1f;
            }

            int points = GetBondPoints(npcId);
            int levelStart = level > 0 ? PointsPerLevel[level - 1] : 0;
            int levelEnd = PointsPerLevel[level];
            int needed = Math.Max(1, levelEnd - levelStart);
            return Math.Clamp((points - levelStart) / (float)needed, 0f, 1f);
        }

        public void ApplyGiftResult(GiftDeliveryResult result)
        {
            if (result == null || !result.Success)
            {
                return;
            }

            string source = result.BondChange < 0 ? "gift_negative" : "gift_positive";
            AddBondDelta(result.NpcId, result.BondChange, source);
        }

        public void AddBondPoints(string npcId, string source)
        {
            AddBondPoints(npcId, source, 0);
        }

        public void AddBondPoints(string npcId, string source, int bonus)
        {
            string normalizedNpcId = NormalizeId(npcId);
            string normalizedSource = NormalizeId(source);
            int basePoints = SourceBasePoints.TryGetValue(normalizedSource, out int points) ? points : 10;
            AddBondDelta(normalizedNpcId, basePoints + bonus, normalizedSource);
        }

        public void SetBondLevel(string npcId, int level)
        {
            string normalizedNpcId = NormalizeId(npcId);
            BondRecord record = EnsureBond(normalizedNpcId);
            int oldLevel = record.Level;
            int clamped = Math.Clamp(level, 0, MaxBondLevel);
            record.Level = clamped;
            record.Points = clamped > 0 ? PointsPerLevel[clamped - 1] : 0;
            LastNpcId = normalizedNpcId;
            LastDelta = 0;
            LastSource = "set_level";
            LastOldLevel = oldLevel;
            LastNewLevel = clamped;
        }

        public bool TryDailyInteraction(string npcId)
        {
            string normalizedNpcId = NormalizeId(npcId);
            if (dailyInteracted.Contains(normalizedNpcId))
            {
                return false;
            }

            dailyInteracted.Add(normalizedNpcId);
            AddBondPoints(normalizedNpcId, "daily_interaction");
            return true;
        }

        public void StartNewDay()
        {
            dailyInteracted.Clear();
        }

        public SocialBondSnapshot CreateSnapshot()
        {
            SocialBondSnapshot snapshot = new SocialBondSnapshot();
            foreach (KeyValuePair<string, BondRecord> pair in bondsByNpc)
            {
                snapshot.Bonds.Add(new SocialBondSnapshotRecord
                {
                    NpcId = pair.Key,
                    Points = pair.Value.Points,
                    Level = pair.Value.Level
                });
            }

            snapshot.DailyInteracted.AddRange(dailyInteracted);
            return snapshot;
        }

        public void LoadSnapshot(SocialBondSnapshot snapshot)
        {
            bondsByNpc.Clear();
            dailyInteracted.Clear();
            if (snapshot == null)
            {
                return;
            }

            foreach (SocialBondSnapshotRecord record in snapshot.Bonds)
            {
                string normalizedNpcId = NormalizeId(record.NpcId);
                if (string.IsNullOrEmpty(normalizedNpcId))
                {
                    continue;
                }

                bondsByNpc[normalizedNpcId] = new BondRecord
                {
                    Points = record.Points,
                    Level = Math.Clamp(record.Level, 0, MaxBondLevel)
                };
            }

            foreach (string npcId in snapshot.DailyInteracted)
            {
                string normalizedNpcId = NormalizeId(npcId);
                if (!string.IsNullOrEmpty(normalizedNpcId))
                {
                    dailyInteracted.Add(normalizedNpcId);
                }
            }
        }

        private void AddBondDelta(string npcId, int delta, string source)
        {
            string normalizedNpcId = NormalizeId(npcId);
            BondRecord record = EnsureBond(normalizedNpcId);
            int oldLevel = record.Level;
            record.Points += delta;
            record.Level = CalculateLevel(record.Points, oldLevel);
            LastNpcId = normalizedNpcId;
            LastDelta = delta;
            LastSource = string.IsNullOrWhiteSpace(source) ? "unknown" : source;
            LastOldLevel = oldLevel;
            LastNewLevel = record.Level;
        }

        private BondRecord EnsureBond(string npcId)
        {
            string normalizedNpcId = NormalizeId(npcId);
            if (!bondsByNpc.TryGetValue(normalizedNpcId, out BondRecord record))
            {
                record = new BondRecord();
                bondsByNpc[normalizedNpcId] = record;
            }

            return record;
        }

        private int CalculateLevel(int points, int oldLevel)
        {
            if (oldLevel >= MaxBondLevel)
            {
                return oldLevel;
            }

            int newLevel = oldLevel;
            for (int index = oldLevel; index < MaxBondLevel; index++)
            {
                if (points >= PointsPerLevel[index])
                {
                    newLevel = index + 1;
                }
                else
                {
                    break;
                }
            }

            return Math.Clamp(newLevel, 0, MaxBondLevel);
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }

        private sealed class BondRecord
        {
            public int Points { get; set; }
            public int Level { get; set; }
        }
    }

    [Serializable]
    public sealed class SocialBondSnapshot
    {
        public List<SocialBondSnapshotRecord> bonds = new List<SocialBondSnapshotRecord>();
        public List<string> daily_interacted = new List<string>();

        public List<SocialBondSnapshotRecord> Bonds => bonds;
        public List<string> DailyInteracted => daily_interacted;
    }

    [Serializable]
    public sealed class SocialBondSnapshotRecord
    {
        public string npc_id = string.Empty;
        public int points;
        public int level;

        public string NpcId
        {
            get => npc_id;
            set => npc_id = value ?? string.Empty;
        }

        public int Points
        {
            get => points;
            set => points = value;
        }

        public int Level
        {
            get => level;
            set => level = value;
        }
    }
}
