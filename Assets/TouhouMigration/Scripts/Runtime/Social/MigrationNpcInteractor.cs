using TouhouMigration.Runtime.UI;
using UnityEngine;

namespace TouhouMigration.Runtime.Social
{
    public sealed class MigrationNpcInteractor : MonoBehaviour
    {
        [SerializeField] private string npcId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private string preferredGiftId = string.Empty;
        [SerializeField] private float interactionRadius = 3.2f;

        private Transform player;
        private MigrationGlobalUiController globalUi;

        public string NpcId => npcId;
        public string DisplayName => displayName;
        public string PreferredGiftId => preferredGiftId;
        public float InteractionRadius => interactionRadius;

        public void Configure(string id, string name, string giftId)
        {
            npcId = id;
            displayName = name;
            preferredGiftId = giftId;
        }

        public void Bind(MigrationGlobalUiController controller)
        {
            globalUi = controller;
        }

        public bool IsPlayerInRange()
        {
            Transform target = ResolvePlayer();
            if (target == null)
            {
                return false;
            }

            return Vector3.Distance(transform.position, target.position) <= interactionRadius;
        }

        public bool StartDialogue()
        {
            ResolveGlobalUi();
            return globalUi != null && globalUi.StartDialogueForNpc(npcId);
        }

        public bool GivePreferredGift()
        {
            ResolveGlobalUi();
            return globalUi != null && globalUi.GiveGiftToNpc(npcId, preferredGiftId);
        }

        public bool OpenGiftSelection()
        {
            ResolveGlobalUi();
            return globalUi != null && globalUi.OpenGiftSelectionForNpc(npcId, displayName);
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

            if (Input.GetKeyDown(KeyCode.E))
            {
                StartDialogue();
            }
            else if (Input.GetKeyDown(KeyCode.G))
            {
                OpenGiftSelection();
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
