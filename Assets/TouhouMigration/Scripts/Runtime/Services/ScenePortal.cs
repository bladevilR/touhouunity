using TouhouMigration.Runtime.Bootstrap;
using TouhouMigration.Runtime.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TouhouMigration.Runtime.Services
{
    [RequireComponent(typeof(Collider))]
    public sealed class ScenePortal : MonoBehaviour
    {
        [SerializeField] private MigrationSceneId targetScene = MigrationSceneId.HumanVillageVerticalSlice;
        [SerializeField] private string requiredTag = "Player";
        [SerializeField] private bool loadOnTriggerEnter = true;

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
