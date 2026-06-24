using TouhouMigration.Runtime.Data;
using TouhouMigration.Runtime.Services;
using UnityEngine;

namespace TouhouMigration.Runtime.Bootstrap
{
    public sealed class TouhouMigrationBootstrap : MonoBehaviour
    {
        [SerializeField] private SceneTransitionService sceneTransitionService;
        [SerializeField] private bool loadInitialSceneOnStart = true;
        [SerializeField] private MigrationSceneId initialScene = MigrationSceneId.TitleScreen;

        private static TouhouMigrationBootstrap instance;

        public static TouhouMigrationBootstrap Instance => instance;
        public SceneTransitionService SceneTransitions => sceneTransitionService;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (sceneTransitionService == null)
            {
                sceneTransitionService = GetComponentInChildren<SceneTransitionService>();
            }
        }

        private void Start()
        {
            if (loadInitialSceneOnStart && sceneTransitionService != null)
            {
                sceneTransitionService.Load(initialScene);
            }
        }
    }
}
