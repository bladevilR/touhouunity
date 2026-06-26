using System.Collections.Generic;

namespace TouhouMigration.Runtime.Social
{
    // A gait's animation-timing parameters (Godot NpcGaitProfiles GAITS entry). The walk/idle clip asset
    // paths are the asset layer's; the cadence/stride driving playback speed is the migratable logic.
    public readonly struct MigrationGaitProfile
    {
        public MigrationGaitProfile(double cadence, double strideScale)
        {
            Cadence = cadence;
            StrideScale = strideScale;
        }

        public double Cadence { get; }
        public double StrideScale { get; }
    }

    // NPC gait timing + personality classification (Godot NpcGaitProfiles): each gait carries cadence +
    // stride-scale; an unspecified NPC is classified by name (child first, then lady, then brisk, else calm).
    public static class MigrationNpcGaitProfiles
    {
        private static readonly Dictionary<string, MigrationGaitProfile> Gaits = new Dictionary<string, MigrationGaitProfile>
        {
            ["lady"] = new MigrationGaitProfile(0.90, 0.92),
            ["brisk"] = new MigrationGaitProfile(1.10, 1.12),
            ["child"] = new MigrationGaitProfile(1.15, 0.80),
            ["calm"] = new MigrationGaitProfile(1.00, 1.00),
        };

        private static readonly string[] Lady =
        {
            "咲夜", "爱丽丝", "八意永琳", "永琳", "蕾米莉亚", "四季映姬", "辉夜", "白莲", "圣白莲",
            "慧音", "上白泽慧音", "秦心", "诹坊子", "洩矢诹访子", "幽幽子", "西行寺幽幽子",
        };

        private static readonly string[] Child = { "琪露诺", "橙", "古明地恋" };

        private static readonly string[] Brisk =
        {
            "魔理沙", "雾雨魔理沙", "早苗", "东风谷早苗", "河童", "河城荷取", "阿求", "稗田阿求",
            "铃仙", "小铃", "本居小铃", "夜雀",
        };

        public static MigrationGaitProfile Profile(string gait)
        {
            return gait != null && Gaits.TryGetValue(gait, out MigrationGaitProfile profile) ? profile : Gaits["calm"];
        }

        // Godot classify: child first, then lady, then brisk, else calm (substring match on the npc name).
        public static string Classify(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
            {
                return "calm";
            }

            if (ContainsAny(npcId, Child))
            {
                return "child";
            }

            if (ContainsAny(npcId, Lady))
            {
                return "lady";
            }

            if (ContainsAny(npcId, Brisk))
            {
                return "brisk";
            }

            return "calm";
        }

        private static bool ContainsAny(string npcId, string[] names)
        {
            foreach (string name in names)
            {
                if (npcId.Contains(name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
