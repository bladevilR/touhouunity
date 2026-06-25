using TouhouMigration.Runtime.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TouhouMigration.Runtime.Player
{
    // Self-installing third-person follow camera. The migration scenes were authored with a STATIC
    // camera (CreateFollowCamera only set a fixed transform), so the view never tracked the player and
    // could not be rotated. This component auto-attaches to the tagged MainCamera on every scene load
    // and gives a behind-the-player view with right-mouse orbit and scroll-wheel zoom — no per-scene
    // authoring or scene rebuild required. Respects modal UI input blocking.
    public sealed class MigrationFollowCamera : MonoBehaviour
    {
        [SerializeField] private float distance = 7.5f;
        [SerializeField] private float minDistance = 3f;
        [SerializeField] private float maxDistance = 18f;
        [SerializeField] private float targetHeight = 2.2f;   // look point above the player root
        [SerializeField] private float yaw;
        [SerializeField] private float pitch = 16f;
        [SerializeField] private float minPitch = -8f;
        [SerializeField] private float maxPitch = 72f;
        [SerializeField] private float orbitSensitivity = 3.4f;
        [SerializeField] private float zoomSensitivity = 6f;
        [SerializeField] private float followSharpness = 12f;

        private Transform player;
        private bool initializedBehindPlayer;

        // Install on the main camera at every scene load (initial scene + portal scene swaps).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Install();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Install();

        private static void Install()
        {
            Camera cam = Camera.main;
            if (cam != null && cam.GetComponent<MigrationFollowCamera>() == null)
            {
                cam.gameObject.AddComponent<MigrationFollowCamera>();
            }
        }

        // Current ground-plane yaw, so player movement can be made camera-relative.
        public float Yaw => yaw;

        private void LateUpdate()
        {
            if (!ResolvePlayer())
            {
                return;
            }

            if (!initializedBehindPlayer)
            {
                yaw = player.eulerAngles.y;
                initializedBehindPlayer = true;
            }

            if (!MigrationGlobalUiController.IsGameplayInputBlocked())
            {
                if (Input.GetMouseButton(1))
                {
                    yaw += Input.GetAxisRaw("Mouse X") * orbitSensitivity;
                    pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * orbitSensitivity, minPitch, maxPitch);
                }

                float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.0001f)
                {
                    distance = Mathf.Clamp(distance - scroll * zoomSensitivity, minDistance, maxDistance);
                }
            }

            Vector3 target = player.position + Vector3.up * targetHeight;
            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desired = target - orbit * Vector3.forward * distance;

            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);
            transform.rotation = Quaternion.LookRotation((target - transform.position).normalized, Vector3.up);
        }

        private bool ResolvePlayer()
        {
            if (player != null)
            {
                return true;
            }

            GameObject found = GameObject.FindGameObjectWithTag("Player");
            player = found != null ? found.transform : null;
            return player != null;
        }
    }
}
