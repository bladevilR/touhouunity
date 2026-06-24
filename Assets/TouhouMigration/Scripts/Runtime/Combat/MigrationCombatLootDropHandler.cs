using System;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Social;
using TouhouMigration.Runtime.UI;
using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    [RequireComponent(typeof(MigrationCombatTargetBehaviour))]
    public sealed class MigrationCombatLootDropHandler : MonoBehaviour
    {
        [SerializeField] private string guaranteedItemId = string.Empty;
        [SerializeField] private int guaranteedAmount = 1;
        [SerializeField] private bool notifyQuestKillObjectives = true;
        [SerializeField] private bool useGodotLootTables;
        [SerializeField] private bool forceGodotLootTables;
        [SerializeField] private string enemyType = "fairy";
        [SerializeField] private string elementalGroup = string.Empty;

        private MigrationCombatTargetBehaviour target;
        private InventoryService inventoryService;
        private QuestDeliveryService questDeliveryService;
        private bool granted;
        private bool subscribed;

        public event Action<string, int> LootGranted;

        public int LootGrantCount { get; private set; }
        public string LastGrantedItemId { get; private set; } = string.Empty;
        public int LastGrantedAmount { get; private set; }

        public void BindTarget(MigrationCombatTargetBehaviour target)
        {
            Unsubscribe();
            this.target = target;
            Subscribe();
        }

        public void BindServices(InventoryService inventoryService, QuestDeliveryService questDeliveryService)
        {
            this.inventoryService = inventoryService;
            this.questDeliveryService = questDeliveryService;
        }

        public void ConfigureGuaranteedDrop(string itemId, int amount)
        {
            guaranteedItemId = string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim().ToLowerInvariant();
            guaranteedAmount = Mathf.Max(0, amount);
        }

        public void ConfigureGodotLootTables(string enemyType, string elementalGroup, bool forceAllTables)
        {
            useGodotLootTables = true;
            forceGodotLootTables = forceAllTables;
            this.enemyType = NormalizeId(enemyType, "fairy");
            this.elementalGroup = NormalizeId(elementalGroup, string.Empty);
        }

        public void ConfigureQuestKillNotification(bool enabled)
        {
            notifyQuestKillObjectives = enabled;
        }

        private void Awake()
        {
            target ??= GetComponent<MigrationCombatTargetBehaviour>();
        }

        private void OnEnable()
        {
            target ??= GetComponent<MigrationCombatTargetBehaviour>();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnTargetDefeated(CombatBridgeResult result)
        {
            if (granted)
            {
                return;
            }

            granted = true;

            inventoryService ??= MigrationGlobalUiController.FindInventoryService();
            questDeliveryService ??= MigrationGlobalUiController.FindQuestDeliveryService();

            bool grantedAnyLoot = TryGrantItem(guaranteedItemId, guaranteedAmount);
            if (useGodotLootTables)
            {
                grantedAnyLoot |= GrantGodotLootTables();
            }

            if (grantedAnyLoot)
            {
                LootGrantCount++;
            }

            if (notifyQuestKillObjectives)
            {
                questDeliveryService?.NotifyEnemyKilled();
            }
        }

        private void Subscribe()
        {
            if (target == null || subscribed)
            {
                return;
            }

            target.Defeated += OnTargetDefeated;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (target == null || !subscribed)
            {
                return;
            }

            target.Defeated -= OnTargetDefeated;
            subscribed = false;
        }

        private bool GrantGodotLootTables()
        {
            string classifiedEnemyType = ClassifyEnemyType(enemyType);
            bool grantedAny = false;

            if (ShouldDrop(MeatChance(classifiedEnemyType)))
            {
                grantedAny |= TryGrantItem(FirstMeatDrop(classifiedEnemyType), 1);
            }

            string crystalId = CrystalDropForGroup(elementalGroup);
            if (!string.IsNullOrEmpty(crystalId) && ShouldDrop(CrystalChance(elementalGroup)))
            {
                grantedAny |= TryGrantItem(crystalId, 1);
            }

            if (classifiedEnemyType == "boss")
            {
                if (ShouldDrop(0.40f))
                {
                    grantedAny |= TryGrantItem("seed_shadow_root", 1);
                }
            }
            else
            {
                if (ShouldDrop(0.08f))
                {
                    grantedAny |= TryGrantItem("seed_pumpkin", 1);
                }

                if (classifiedEnemyType == "elite" && ShouldDrop(0.03f))
                {
                    grantedAny |= TryGrantItem("seed_fire_eggplant", 1);
                }
            }

            if (ShouldDrop(0.10f))
            {
                grantedAny |= TryGrantItem("dungeon_compost", 1);
            }

            if ((classifiedEnemyType == "elite" || classifiedEnemyType == "boss") && ShouldDrop(0.06f))
            {
                grantedAny |= TryGrantItem("spirit_soil", 1);
            }

            return grantedAny;
        }

        private bool TryGrantItem(string itemId, int amount)
        {
            string normalizedItemId = NormalizeId(itemId, string.Empty);
            if (string.IsNullOrEmpty(normalizedItemId) || amount <= 0 || inventoryService == null)
            {
                return false;
            }

            if (!inventoryService.AddItem(normalizedItemId, amount))
            {
                return false;
            }

            LastGrantedItemId = normalizedItemId;
            LastGrantedAmount = amount;
            LootGranted?.Invoke(normalizedItemId, amount);
            return true;
        }

        private bool ShouldDrop(float chance)
        {
            return forceGodotLootTables || UnityEngine.Random.value < Mathf.Clamp01(chance);
        }

        private static string ClassifyEnemyType(string rawEnemyType)
        {
            string normalized = NormalizeId(rawEnemyType, "fairy");
            return normalized switch
            {
                "boss" => "boss",
                "elite" or "elite_enemy" => "elite",
                "beast" or "beast_enemy" => "beast",
                _ => "fairy"
            };
        }

        private static string FirstMeatDrop(string classifiedEnemyType)
        {
            return classifiedEnemyType switch
            {
                "boss" => "youkai_beast_meat",
                "elite" => "youkai_beast_meat",
                "beast" => "beast_meat",
                _ => "fairy_meat"
            };
        }

        private static float MeatChance(string classifiedEnemyType)
        {
            return classifiedEnemyType switch
            {
                "boss" => 1.0f,
                "elite" => 0.80f,
                "beast" => 0.65f,
                _ => 0.70f
            };
        }

        private static string CrystalDropForGroup(string rawElementalGroup)
        {
            string normalized = NormalizeId(rawElementalGroup, string.Empty);
            return normalized switch
            {
                "fire_enemy" => "element_crystal_fire",
                "ice_enemy" => "element_crystal_ice",
                "earth_enemy" => "element_crystal_earth",
                "wind_enemy" => "element_crystal_wind",
                _ => "element_crystal_fire"
            };
        }

        private static float CrystalChance(string rawElementalGroup)
        {
            string normalized = NormalizeId(rawElementalGroup, string.Empty);
            return string.IsNullOrEmpty(normalized) || normalized == "default" ? 0.08f : 0.20f;
        }

        private static string NormalizeId(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();
        }
    }
}
