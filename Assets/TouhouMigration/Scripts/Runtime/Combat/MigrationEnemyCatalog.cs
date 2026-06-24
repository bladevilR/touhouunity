using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class MigrationEnemyCatalog
    {
        private const string SceneBaseDir = "res://scenes/monsters/";
        private readonly Dictionary<string, MigrationEnemyVariantProfile> profiles = new();
        private readonly List<MigrationEnemyVariantProfile> orderedProfiles = new();

        public int Count => profiles.Count;

        public void LoadGodotDefaults()
        {
            profiles.Clear();
            orderedProfiles.Clear();

            Register("bat", "蝙蝠", "fly", 45f, 12f, 1.8f, 12, true, true, 1f, 1f);
            Register("bee", "蜜蜂", "fly", 35f, 10f, 2f, 10, true, true, 1f, 0.8f);
            Register("bird", "小鸟", "fly", 40f, 11f, 2.2f, 11, true, true, 1f, 1.2f);
            Register("bumble", "大黄蜂", "fly", 55f, 14f, 1.5f, 15, true, false, 1f, 0.6f);
            Register("ghost", "幽灵", "fly", 35f, 9f, 2.5f, 10, true, false, 1f, 0.8f);
            Register("phantom", "魅影", "fly", 50f, 13f, 2f, 14, true, true, 1f, 1f);
            Register("spook", "鬼怪", "fly", 45f, 11f, 1.7f, 12, true, true, 1f, 0.8f);
            Register("sting", "毒刺", "fly", 47f, 14f, 1.6f, 13, true, false, 1f, 0.8f);
            Register("fungi", "真菌", "walk", 50f, 13f, 1f, 14, true, true);
            Register("mushroom", "蘑菇", "walk", 48f, 12f, 0.9f, 13, true, true);
            Register("seed", "种子", "walk", 20f, 6f, 2.3f, 6, true, false);
            Register("shade", "暗影", "walk", 42f, 11f, 2.1f, 12, true, false);
            Register("shadow", "影魔", "walk", 65f, 15f, 1.8f, 18, true, true);
            Register("sprout", "芽苗", "walk", 22f, 5f, 1.5f, 5, true, false);
            Register("toadstool", "毒蘑菇", "walk", 58f, 16f, 0.8f, 16, true, true);
            Register("chick", "小鸡", "jump", 30f, 8f, 1.7f, 8, true, false);
            Register("egg", "蛋", "walk", 100f, 6f, 0.5f, 20, false, false);
            Register("fledgling", "雏鸟", "jump", 25f, 7f, 1.9f, 7, true, false);
            Register("spider", "蜘蛛", "crawl", 52f, 13f, 1.9f, 14, true, true);
            Register("vampire", "吸血蝠", "fly", 60f, 16f, 1.9f, 20, true, true, 1f, 1f);
        }

        public MigrationEnemyVariantProfile GetProfile(string monsterId)
        {
            string normalizedId = NormalizeId(monsterId);
            return profiles.TryGetValue(normalizedId, out MigrationEnemyVariantProfile profile)
                ? profile
                : null;
        }

        public IReadOnlyList<MigrationEnemyVariantProfile> GetAllProfiles()
        {
            return orderedProfiles;
        }

        private void Register(
            string id,
            string displayName,
            string moveStyle,
            float hp,
            float damage,
            float speed,
            int xp,
            bool canMelee,
            bool canShoot,
            float scale = 1f,
            float floatHeight = 0f)
        {
            MigrationEnemyVariantProfile profile = new MigrationEnemyVariantProfile();
            profile.ConfigureGodotMonster(
                id,
                displayName,
                moveStyle,
                SceneBaseDir + ToPascal(id) + "Monster.tscn",
                hp,
                damage,
                speed,
                xp,
                canMelee,
                canShoot,
                scale,
                floatHeight);
            string normalizedId = NormalizeId(id);
            profiles[normalizedId] = profile;
            orderedProfiles.Add(profile);
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }

        private static string ToPascal(string value)
        {
            string[] parts = NormalizeId(value).Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return string.Empty;
            }

            string result = string.Empty;
            foreach (string part in parts)
            {
                result += char.ToUpperInvariant(part[0]) + part.Substring(1);
            }

            return result;
        }
    }
}
