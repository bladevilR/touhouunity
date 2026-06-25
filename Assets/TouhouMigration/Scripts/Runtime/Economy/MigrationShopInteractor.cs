using TouhouMigration.Runtime.UI;
using UnityEngine;

namespace TouhouMigration.Runtime.Economy
{
    // A shopkeeper the player can talk to (Godot shop interaction): press the interact key in range to
    // open the scoped shop modal for this stall's shop id, where the player buys/sells against the live
    // economy (coins + inventory, open-hours/catalog gated). Connects the logic-complete shop services
    // (owned by the global UI controller) to in-scene gameplay. Mirrors the bed/farm-plot/fishing
    // interactor proximity+input pattern and respects modal input blocking.
    public sealed class MigrationShopInteractor : MonoBehaviour
    {
        [SerializeField] private string shopId = "town_general";
        [SerializeField] private float interactionRadius = 3.5f;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private Transform player;
        private MigrationGlobalUiController globalUi;

        public string ShopId => shopId;
        public float InteractionRadius => interactionRadius;

        public void Configure(string id)
        {
            shopId = string.IsNullOrWhiteSpace(id) ? "town_general" : id.Trim();
        }

        public bool IsPlayerInRange()
        {
            Transform target = ResolvePlayer();
            return target != null && Vector3.Distance(transform.position, target.position) <= interactionRadius;
        }

        // Open the scoped shop modal through the global UI owner. Returns false if no owner is present
        // or the shop could not be opened (unknown id / unbound).
        public bool Open()
        {
            ResolveGlobalUi();
            return globalUi != null && globalUi.OpenShop(shopId);
        }

        private void Update()
        {
            ResolveGlobalUi();
            if (globalUi != null && globalUi.BlocksGameplayInput)
            {
                return;
            }

            if (!IsPlayerInRange())
            {
                return;
            }

            if (Input.GetKeyDown(interactKey))
            {
                Open();
            }
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
