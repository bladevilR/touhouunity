using System;
using TouhouMigration.Runtime.UI;
using UnityEngine;

namespace TouhouMigration.Runtime.Fishing
{
    // A fishing spot the player can work (Godot fishing interaction): press the interact key in range
    // to cast and roll a weighted catch, granting the caught fish to the inventory. Connects the
    // logic-complete MigrationFishingService (owned by the global UI controller, exposed as Fishing) to
    // in-scene gameplay. Mirrors the bed/farm-plot interactor proximity+input pattern and respects modal
    // input blocking. Fishing-level boost is wired through once player progression tracks it
    // (passes 0 for now — see fishingLevel).
    public sealed class MigrationFishingSpotInteractor : MonoBehaviour
    {
        [SerializeField] private float interactionRadius = 4f;
        [SerializeField] private KeyCode interactKey = KeyCode.F;

        private Transform player;
        private MigrationGlobalUiController globalUi;
        private System.Random rng;

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

        // Cast once: roll a weighted fish from the owner's fishing service into the inventory.
        // Returns a failed result if no owner/service is present. Injectable RNG keeps the roll testable.
        public MigrationFishCatchResult Fish()
        {
            return Fish(NextInt);
        }

        public MigrationFishCatchResult Fish(Func<int, int> nextInt)
        {
            MigrationFishingService fishing = ResolveFishing();
            if (fishing == null)
            {
                return MigrationFishCatchResult.Fail("no_fishing_service");
            }

            // TODO: pass the player's fishing level once progression tracks it (rare/legendary boost).
            return fishing.Catch(nextInt, 0);
        }

        private int NextInt(int maxExclusive)
        {
            if (rng == null)
            {
                rng = new System.Random();
            }

            return maxExclusive <= 0 ? 0 : rng.Next(maxExclusive);
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
                Fish();
            }
        }

        private MigrationFishingService ResolveFishing()
        {
            ResolveGlobalUi();
            return globalUi != null ? globalUi.Fishing : null;
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
