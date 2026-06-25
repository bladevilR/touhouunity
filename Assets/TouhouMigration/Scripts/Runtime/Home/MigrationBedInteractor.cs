using TouhouMigration.Runtime.UI;
using UnityEngine;

namespace TouhouMigration.Runtime.Home
{
    // A bed the player can interact with to sleep (Godot HomeInteractionSystem sleep): when the player
    // is in range and presses the interact key, it calls MigrationGlobalUiController.Sleep(), which
    // advances the day (running the daily resets) and fully restores fatigue. Mirrors the
    // MigrationNpcInteractor proximity+input pattern and respects modal UI input blocking.
    public sealed class MigrationBedInteractor : MonoBehaviour
    {
        [SerializeField] private float interactionRadius = 3.2f;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private Transform player;
        private MigrationGlobalUiController globalUi;

        public float InteractionRadius => interactionRadius;

        public void Bind(MigrationGlobalUiController controller)
        {
            globalUi = controller;
        }

        public bool IsPlayerInRange()
        {
            Transform target = ResolvePlayer();
            return target != null && Vector3.Distance(transform.position, target.position) <= interactionRadius;
        }

        // Trigger a sleep through the global UI owner. Returns false if no owner is present.
        public bool Sleep()
        {
            ResolveGlobalUi();
            if (globalUi == null)
            {
                return false;
            }

            globalUi.Sleep();
            return true;
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
                Sleep();
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
