using TouhouMigration.Runtime.Bootstrap;
using TouhouMigration.Runtime.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TouhouMigration.Runtime.Services
{
    [RequireComponent(typeof(Collider))]
    public sealed class ScenePortal : MonoBehaviour
    {
        [SerializeField] private MigrationSceneId targetScene = MigrationSceneId.HumanVillageVerticalSlice;
        [SerializeField] private string requiredTag = "Player";
        [SerializeField] private bool loadOnTriggerEnter = true;
        [SerializeField] private bool showVisualMarker = true;

        private const string MarkerName = "PortalVisualMarker";

        public MigrationSceneId TargetScene => targetScene;

        public void Configure(MigrationSceneId sceneId, string tagFilter = "Player", bool triggerOnEnter = true)
        {
            targetScene = sceneId;
            requiredTag = tagFilter;
            loadOnTriggerEnter = triggerOnEnter;
        }

        public void Activate()
        {
            if (TouhouMigrationBootstrap.Instance != null &&
                TouhouMigrationBootstrap.Instance.SceneTransitions != null)
            {
                TouhouMigrationBootstrap.Instance.SceneTransitions.Load(targetScene);
                return;
            }

            SceneManager.LoadScene(MigrationSceneCatalog.ToSceneName(targetScene));
        }

        private void Awake()
        {
            if (showVisualMarker)
            {
                EnsureVisualMarker();
            }
        }

        // A portal is otherwise an invisible trigger volume, so the player can't see where the doorway is.
        // Self-install a translucent glowing pillar at runtime (built-in shader, no art asset, no scene
        // rebuild) so every placed portal reads as a visible gateway — mirrors the follow-camera's
        // self-install pattern.
        private void EnsureVisualMarker()
        {
            if (transform.Find(MarkerName) != null)
            {
                return;
            }

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = MarkerName;

            Collider markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Destroy(markerCollider);
            }

            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            marker.transform.localScale = new Vector3(0.6f, 1.25f, 0.6f);

            Color glow = new Color(0.45f, 0.85f, 1f, 0.55f);
            Renderer markerRenderer = marker.GetComponent<Renderer>();
            if (markerRenderer != null)
            {
                markerRenderer.shadowCastingMode = ShadowCastingMode.Off;
                markerRenderer.sharedMaterial = CreateGlowMaterial(glow);
            }

            Light glowLight = marker.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = glow;
            glowLight.range = 6f;
            glowLight.intensity = 1.5f;
        }

        private static Material CreateGlowMaterial(Color color)
        {
            Material material = new Material(Shader.Find("Standard"));
            // Switch the Standard shader into Transparent rendering mode so the pillar glows translucently.
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.color = color;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(color.r, color.g, color.b, 1f) * 1.5f);
            return material;
        }

        private void Reset()
        {
            Collider portalCollider = GetComponent<Collider>();
            portalCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!loadOnTriggerEnter)
            {
                return;
            }

            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            {
                return;
            }

            Activate();
        }
    }
}
