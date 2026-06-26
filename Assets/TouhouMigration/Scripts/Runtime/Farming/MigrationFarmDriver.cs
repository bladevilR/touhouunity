using System;
using TouhouMigration.Runtime.Inventory;
using UnityEngine;

namespace TouhouMigration.Runtime.Farming
{
    // A self-contained bootstrapper that makes the closed E4 farming loop playable on its own: it builds the
    // crop/item databases (registering derived produce) + inventory + farming manager, grants starter seeds,
    // and offers an IMGUI plant/water/fertilize/harvest/advance-day loop. Routes around the concurrent owner
    // the same way the Cirno + shop scenes do; all farming logic lives in the unit-tested manager.
    public sealed class MigrationFarmDriver : MonoBehaviour
    {
        [SerializeField] private string cropDataPath = "Assets/TouhouMigration/Data/Farming/crops.json";
        [SerializeField] private string itemDataPath = "Assets/TouhouMigration/Data/Items/items.json";
        [SerializeField] private string starterSeed = "seed_turnip";
        [SerializeField] private string starterCrop = "crop_turnip";
        [SerializeField] private int plotCount = 4;

        private MigrationFarmingManager manager;
        private InventoryService inventory;
        private readonly System.Random rng = new System.Random(12345);
        private string lastMessage = string.Empty;

        private void Start()
        {
            ItemDatabase items = new ItemDatabase();
            items.LoadFromPath(itemDataPath);

            MigrationCropDatabase crops = new MigrationCropDatabase();
            crops.LoadFromPath(cropDataPath);
            crops.RegisterProduceInto(items); // every harvest is a real sellable produce item

            inventory = new InventoryService(items);
            manager = new MigrationFarmingManager(inventory, plotCount);
            manager.RegisterCropsFrom(crops);
            inventory.AddItem(starterSeed, plotCount); // a seed per plot to start
        }

        private void OnGUI()
        {
            if (manager == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(12, 12, 420, Screen.height - 24), GUI.skin.box);
            GUILayout.Label($"Seeds: {inventory.GetItemCount(starterSeed)}    Produce: {inventory.GetItemCount("turnip")}");
            GUILayout.Space(6);

            for (int i = 0; i < manager.PlotCount; i++)
            {
                MigrationFarmPlot plot = manager.GetPlot(i);
                GUILayout.BeginHorizontal();
                if (!plot.HasCrop)
                {
                    GUILayout.Label($"Plot {i}: empty", GUILayout.Width(200));
                    if (GUILayout.Button("Plant"))
                    {
                        lastMessage = manager.PlantFromInventory(i, starterCrop) ? $"planted plot {i}" : "no seeds";
                    }
                }
                else if (plot.IsReadyToHarvest)
                {
                    GUILayout.Label($"Plot {i}: READY ({plot.QualityTier})", GUILayout.Width(200));
                    if (GUILayout.Button("Harvest"))
                    {
                        MigrationHarvestResult result = manager.Harvest(i, (min, max) => rng.Next(min, max + 1));
                        lastMessage = $"harvested {result.Amount}x {result.ItemId}";
                    }
                }
                else
                {
                    string water = plot.IsWateredToday ? "watered" : "dry";
                    GUILayout.Label($"Plot {i}: growing {plot.DaysGrown}/{plot.TotalGrowthDays} ({water})", GUILayout.Width(200));
                    if (GUILayout.Button("Water"))
                    {
                        manager.Water(i);
                    }

                    if (GUILayout.Button("Fertilize"))
                    {
                        manager.Fertilize(i, 30.0);
                    }
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            if (GUILayout.Button("Advance Day"))
            {
                manager.AdvanceDay();
                lastMessage = "a day passed";
            }

            if (!string.IsNullOrEmpty(lastMessage))
            {
                GUILayout.Space(6);
                GUILayout.Label(lastMessage);
            }

            GUILayout.EndArea();
        }
    }
}
