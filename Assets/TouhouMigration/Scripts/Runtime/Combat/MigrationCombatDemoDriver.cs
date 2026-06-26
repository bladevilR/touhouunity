using System.Collections.Generic;
using TouhouMigration.Runtime.Inventory;
using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    // A self-contained combat demo: an enemy target that takes player attacks and, on defeat, rolls
    // rank-bonus loot into the inventory (composes the tested MigrationCombatTargetRuntime +
    // MigrationLootDropRoller). Deliberately enemy-side only — it does NOT touch the player MonoBehaviour /
    // locomotion (the concurrent session's domain), so it stays collision-free while still showing the
    // attack -> defeat -> loot pipeline in a launchable scene.
    public sealed class MigrationCombatDemoDriver : MonoBehaviour
    {
        [SerializeField] private string itemDataPath = "Assets/TouhouMigration/Data/Items/items.json";
        [SerializeField] private float enemyMaxHp = 120f;
        [SerializeField] private float attackDamage = 30f;

        private InventoryService inventory;
        private MigrationCombatTargetRuntime enemy;
        private readonly MigrationLootDropRoller roller = new MigrationLootDropRoller();
        private string status = "Attack the enemy!";

        private void Start()
        {
            ItemDatabase items = new ItemDatabase();
            items.LoadFromPath(itemDataPath);
            inventory = new InventoryService(items);
            enemy = new MigrationCombatTargetRuntime(enemyMaxHp);
        }

        private void OnGUI()
        {
            if (enemy == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(12, 12, 380, 200), GUI.skin.box);
            GUILayout.Label($"Enemy HP: {enemy.CurrentHp:0} / {enemy.MaxHp:0}");
            GUILayout.Space(6);

            if (!enemy.IsDefeated)
            {
                if (GUILayout.Button($"Attack ({attackDamage:0} dmg)", GUILayout.Height(40)))
                {
                    CombatBridgeResult result = enemy.ApplyDamage(attackDamage);
                    if (result.TargetDefeated)
                    {
                        GrantDefeatLoot();
                    }
                }
            }
            else
            {
                GUILayout.Label(status);
                if (GUILayout.Button("Spawn New Enemy", GUILayout.Height(40)))
                {
                    enemy = new MigrationCombatTargetRuntime(enemyMaxHp);
                    status = "Attack the enemy!";
                }
            }

            GUILayout.EndArea();
        }

        private void GrantDefeatLoot()
        {
            IReadOnlyList<LootDrop> drops = roller.RollRankBonusDrops("S", 5, () => 0.0, _ => 0);
            System.Text.StringBuilder summary = new System.Text.StringBuilder("Defeated! Loot: ");
            foreach (LootDrop drop in drops)
            {
                inventory.AddItem(drop.ItemId, drop.Count);
                summary.Append($"{drop.Count}x {drop.ItemId}  ");
            }

            status = drops.Count > 0 ? summary.ToString() : "Defeated! (no bonus loot)";
        }
    }
}
