using TouhouMigration.Runtime.Inventory;
using UnityEngine;

namespace TouhouMigration.Runtime.Fishing
{
    // A self-contained bootstrapper that makes fishing playable on its own: it builds the fish/item
    // databases + inventory + fishing service, then runs the tested MigrationFishingSession through an IMGUI
    // cast -> hold-to-reel -> land/escape loop (the fish oscillates; hold REEL to keep it in the box).
    // Routes around the concurrent owner like the Cirno/shop/farm scenes; all logic lives in the unit-tested
    // session/minigame/service.
    public sealed class MigrationFishDriver : MonoBehaviour
    {
        [SerializeField] private string itemDataPath = "Assets/TouhouMigration/Data/Items/items.json";
        [SerializeField] private string fishDataPath = "Assets/TouhouMigration/Data/Fishing/fish.json";

        private MigrationFishingSession session;
        private InventoryService inventory;
        private readonly System.Random rng = new System.Random(2024);
        private bool pulling;
        private string status = "Press Cast Line to fish.";

        private void Start()
        {
            ItemDatabase items = new ItemDatabase();
            items.LoadFromPath(itemDataPath);

            MigrationFishDatabase fish = new MigrationFishDatabase();
            fish.LoadFromPath(fishDataPath);

            inventory = new InventoryService(items);
            MigrationFishingService service = new MigrationFishingService(inventory);
            service.RegisterFrom(fish);

            session = new MigrationFishingSession(service, fishingLevel: 0, nextInt: max => rng.Next(max));
        }

        private void Update()
        {
            if (session == null || !session.IsReeling)
            {
                return;
            }

            // The fish bobs; the player holds REEL to raise the box to meet it.
            double fishPosition = 0.5 + 0.42 * Mathf.Sin(Time.time * 2.5f);
            session.Reel(Time.deltaTime, pulling, fishPosition);

            if (!session.IsReeling)
            {
                status = session.LandedCatch != null && session.LandedCatch.Success
                    ? $"Caught {session.LandedCatch.FishId}! (in your bag)"
                    : "It got away...";
            }
        }

        private void OnGUI()
        {
            if (session == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(12, 12, 380, 220), GUI.skin.box);
            GUILayout.Label(status);

            if (session.IsReeling)
            {
                GUILayout.Label($"Reel progress: {(session.Minigame.CatchProgress * 100.0):0}%");
                GUILayout.Label($"Box: {session.Minigame.BoxPosition:0.00}");
                pulling = GUILayout.RepeatButton("REEL (hold)", GUILayout.Height(44));
            }
            else
            {
                pulling = false;
                if (GUILayout.Button("Cast Line", GUILayout.Height(44)))
                {
                    session.CastLine(0.25);
                    status = "A fish is biting — hold REEL to land it!";
                }
            }

            GUILayout.EndArea();
        }
    }
}
