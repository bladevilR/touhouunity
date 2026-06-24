using TouhouMigration.Runtime.UI;
using UnityEngine;

namespace TouhouMigration.Runtime.Cooking
{
    public sealed class MigrationCookingStationInteractor : MonoBehaviour
    {
        [SerializeField] private string stationId = "default";
        [SerializeField] private string bubbleText = "做菜 [E]";

        private bool playerInRange;
        private GUIStyle labelStyle;

        public string StationId => stationId;
        public string BubbleText => bubbleText;

        public void Configure(string id, string text = "做菜 [E]")
        {
            stationId = string.IsNullOrWhiteSpace(id) ? "default" : id.Trim();
            bubbleText = string.IsNullOrWhiteSpace(text) ? "做菜 [E]" : text;
        }

        private void Reset()
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }

        private void Update()
        {
            if (!playerInRange || MigrationGlobalUiController.IsGameplayInputBlocked())
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                OpenCooking();
            }
        }

        private void OnMouseDown()
        {
            if (playerInRange && !MigrationGlobalUiController.IsGameplayInputBlocked())
            {
                OpenCooking();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = false;
            }
        }

        private void OnGUI()
        {
            if (!playerInRange)
            {
                return;
            }

            labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                normal = { textColor = new Color(1f, 0.92f, 0.66f, 1f) }
            };

            GUI.Label(new Rect(Screen.width * 0.5f - 90f, Screen.height - 142f, 180f, 30f), bubbleText, labelStyle);
        }

        private void OpenCooking()
        {
            MigrationGlobalUiController controller = FindAnyObjectByType<MigrationGlobalUiController>();
            controller?.OpenCooking(stationId);
        }
    }
}
