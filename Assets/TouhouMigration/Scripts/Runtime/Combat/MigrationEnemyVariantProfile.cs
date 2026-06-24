using System;
using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    [Serializable]
    public sealed class MigrationEnemyVariantProfile
    {
        public string VariantId { get; private set; } = string.Empty;
        public string DisplayName { get; private set; } = string.Empty;
        public string MoveStyle { get; private set; } = "walk";
        public string GodotScenePath { get; private set; } = string.Empty;
        public string EnemyType { get; private set; } = "fairy";
        public string ElementalGroup { get; private set; } = string.Empty;
        public float MaxHp { get; private set; } = 20f;
        public float ChaseRange { get; private set; } = 6f;
        public float AttackRange { get; private set; } = 1.5f;
        public float MoveSpeed { get; private set; } = 2f;
        public float AttackDamage { get; private set; } = 10f;
        public float AttackCooldown { get; private set; } = 0.8f;
        public float AttackWindupSeconds { get; private set; }
        public int XpValue { get; private set; }
        public bool CanMelee { get; private set; } = true;
        public bool CanShoot { get; private set; }
        public float ModelScale { get; private set; } = 1f;
        public float FloatHeight { get; private set; }
        public float ProjectileSpeed { get; private set; } = 8f;
        public float RangedMinDistance { get; private set; } = 5f;
        public bool ForceLootTables { get; private set; }

        public void Configure(
            string variantId,
            string enemyType,
            string elementalGroup,
            float maxHp,
            float chaseRange,
            float attackRange,
            float moveSpeed,
            float attackDamage,
            float attackCooldown,
            float attackWindupSeconds,
            bool forceLootTables)
        {
            VariantId = NormalizeId(variantId, string.Empty);
            DisplayName = VariantId;
            MoveStyle = "walk";
            GodotScenePath = string.Empty;
            EnemyType = NormalizeId(enemyType, "fairy");
            ElementalGroup = NormalizeId(elementalGroup, string.Empty);
            MaxHp = Mathf.Max(1f, maxHp);
            ChaseRange = Mathf.Max(0f, chaseRange);
            AttackRange = Mathf.Max(0f, attackRange);
            MoveSpeed = Mathf.Max(0f, moveSpeed);
            AttackDamage = Mathf.Max(0f, attackDamage);
            AttackCooldown = Mathf.Max(0f, attackCooldown);
            AttackWindupSeconds = Mathf.Max(0f, attackWindupSeconds);
            XpValue = 0;
            CanMelee = true;
            CanShoot = false;
            ModelScale = 1f;
            FloatHeight = 0f;
            ProjectileSpeed = 8f;
            RangedMinDistance = 5f;
            ForceLootTables = forceLootTables;
        }

        public void ConfigureGodotMonster(
            string monsterId,
            string displayName,
            string moveStyle,
            string godotScenePath,
            float maxHp,
            float damage,
            float speed,
            int xpValue,
            bool canMelee,
            bool canShoot,
            float modelScale,
            float floatHeight)
        {
            string normalizedId = NormalizeId(monsterId, string.Empty);
            string normalizedMoveStyle = NormalizeId(moveStyle, "walk");
            VariantId = normalizedId;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? normalizedId : displayName.Trim();
            MoveStyle = normalizedMoveStyle;
            GodotScenePath = string.IsNullOrWhiteSpace(godotScenePath) ? string.Empty : godotScenePath.Trim();
            EnemyType = InferEnemyType(normalizedId);
            ElementalGroup = InferElementalGroup(normalizedId);
            MaxHp = Mathf.Max(1f, maxHp);
            ChaseRange = canShoot ? 10f : 8f;
            AttackRange = canShoot ? 8f : 1.2f;
            MoveSpeed = Mathf.Max(0f, speed);
            AttackDamage = Mathf.Max(0f, damage);
            AttackCooldown = 1f;
            AttackWindupSeconds = 0.5f;
            XpValue = Mathf.Max(0, xpValue);
            CanMelee = canMelee;
            CanShoot = canShoot;
            ModelScale = Mathf.Max(0.01f, modelScale);
            FloatHeight = Mathf.Max(0f, floatHeight);
            ProjectileSpeed = 8f;
            RangedMinDistance = 5f;
            ForceLootTables = false;
        }

        private static string NormalizeId(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();
        }

        private static string InferEnemyType(string monsterId)
        {
            return monsterId switch
            {
                "phantom" or "shadow" or "vampire" => "elite",
                _ => "beast"
            };
        }

        private static string InferElementalGroup(string monsterId)
        {
            return monsterId switch
            {
                "ghost" or "phantom" or "spook" or "shade" or "shadow" => "wind_enemy",
                "bee" or "bumble" or "sting" or "toadstool" or "spider" => "earth_enemy",
                "vampire" => "fire_enemy",
                _ => string.Empty
            };
        }
    }
}
