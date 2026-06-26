using TouhouMigration.Runtime.UI;
using UnityEngine;

namespace TouhouMigration.Runtime.Farming
{
    // A farm plot the player can work (Godot FarmPlotNode): press the interact key in range to plant a
    // seed in an empty plot, water a growing plot, or harvest a ready one (growth advances with the
    // day-loop / sleeping). Connects the logic-complete MigrationFarmingManager (owned by the global UI
    // controller) to in-scene gameplay; the crop visual scales with growth progress. Mirrors the
    // NPC/bed interactor proximity+input pattern and respects modal input blocking.
    public sealed class MigrationFarmPlotInteractor : MonoBehaviour
    {
        [SerializeField] private int plotIndex;
        [SerializeField] private string cropId = "crop_turnip";
        [SerializeField] private float interactionRadius = 3.5f;
        [SerializeField] private KeyCode interactKey = KeyCode.F;
        [SerializeField] private KeyCode fertilizeKey = KeyCode.G;
        [SerializeField] private double fertilizePower = 35.0;

        private Transform player;
        private MigrationGlobalUiController globalUi;
        private Vector3 baseScale = Vector3.one;
        private bool capturedBaseScale;

        public int PlotIndex => plotIndex;

        public void Configure(int index, string crop)
        {
            plotIndex = index;
            cropId = crop;
        }

        public bool IsPlayerInRange()
        {
            Transform target = ResolvePlayer();
            return target != null && Vector3.Distance(transform.position, target.position) <= interactionRadius;
        }

        // Advance the plot one step: plant if empty, harvest if ready, else water if it needs it.
        public bool Work()
        {
            MigrationFarmingManager farming = ResolveFarming();
            MigrationFarmPlot plot = farming?.GetPlot(plotIndex);
            if (plot == null)
            {
                return false;
            }

            if (!plot.HasCrop)
            {
                return farming.Plant(plotIndex, cropId);
            }

            if (plot.IsReadyToHarvest)
            {
                return farming.Harvest(plotIndex, (min, max) => Random.Range(min, max + 1)).Success;
            }

            if (plot.NeedsWaterDaily && !plot.IsWateredToday)
            {
                farming.Water(plotIndex);
                return true;
            }

            return false;
        }

        // Apply fertilizer to a growing plot (raises quality -> harvest yield). Returns false if the plot
        // is empty/missing.
        public bool Fertilize()
        {
            return ResolveFarming()?.Fertilize(plotIndex, fertilizePower) ?? false;
        }

        private void Update()
        {
            if (!capturedBaseScale)
            {
                baseScale = transform.localScale;
                capturedBaseScale = true;
            }

            UpdateCropVisual();

            ResolveGlobalUi();
            if (globalUi != null && globalUi.BlocksGameplayInput)
            {
                return;
            }

            if (IsPlayerInRange())
            {
                if (Input.GetKeyDown(interactKey))
                {
                    Work();
                }
                else if (Input.GetKeyDown(fertilizeKey))
                {
                    Fertilize();
                }
            }
        }

        private void UpdateCropVisual()
        {
            MigrationFarmPlot plot = ResolveFarming()?.GetPlot(plotIndex);
            if (plot == null)
            {
                return;
            }

            float grow = plot.HasCrop ? Mathf.Clamp01(0.35f + plot.GrowthProgress * 0.65f) : 0.15f;
            transform.localScale = new Vector3(baseScale.x, baseScale.y * grow, baseScale.z);
        }

        private MigrationFarmingManager ResolveFarming()
        {
            ResolveGlobalUi();
            return globalUi != null ? globalUi.FarmingManager : null;
        }

        private Transform ResolvePlayer()
        {
            if (player != null)
            {
                return player;
            }

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;
            return player;
        }

        private void ResolveGlobalUi()
        {
            if (globalUi != null)
            {
                return;
            }

            globalUi = FindAnyObjectByType<MigrationGlobalUiController>();
        }
    }
}
