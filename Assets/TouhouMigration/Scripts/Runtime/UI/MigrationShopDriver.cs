using TouhouMigration.Runtime.Economy;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Player;
using UnityEngine;

namespace TouhouMigration.Runtime.UI
{
    // A self-contained bootstrapper that makes the shop playable on its own: it builds the item/shop
    // databases + inventory + progress + shop service, binds a MigrationShopController, grants starting
    // coins, and opens a shop. This routes AROUND the concurrent-session blocker — instead of the in-flight
    // MigrationGlobalUiController wiring the shop, this drives the (already-tested) MigrationShopController
    // directly, with zero edits to the 4 concurrent files. Editor/play-mode oriented (loads data by asset
    // path); all economy logic lives in the unit-tested services.
    public sealed class MigrationShopDriver : MonoBehaviour
    {
        [SerializeField] private string itemDataPath = "Assets/TouhouMigration/Data/Items/items.json";
        [SerializeField] private string shopDataPath = "Assets/TouhouMigration/Data/Shops/shops.json";
        [SerializeField] private string openShopId = "town_general";
        [SerializeField] private int startingCoins = 5000;
        [SerializeField] private int hour = 12; // noon — within most shops' open hours

        private void Start()
        {
            ItemDatabase items = new ItemDatabase();
            items.LoadFromPath(itemDataPath);

            MigrationShopDatabase shops = new MigrationShopDatabase();
            shops.LoadFromPath(shopDataPath);

            InventoryService inventory = new InventoryService(items);
            MigrationPlayerProgressService progress = new MigrationPlayerProgressService();
            progress.AddCoins(startingCoins);
            MigrationShopService service = new MigrationShopService(inventory, items, progress);

            MigrationShopController controller = gameObject.AddComponent<MigrationShopController>();
            controller.Bind(shops, service, inventory, items, progress, () => hour);
            controller.OpenForShop(openShopId);
        }
    }
}
