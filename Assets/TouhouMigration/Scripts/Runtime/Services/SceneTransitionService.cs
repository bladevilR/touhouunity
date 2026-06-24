using System.Collections;
using TouhouMigration.Runtime.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TouhouMigration.Runtime.Services
{
    public sealed class SceneTransitionService : MonoBehaviour
    {
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeDurationSeconds = 0.35f;

        private bool isTransitioning;

        public bool IsTransitioning => isTransitioning;

        public void Load(MigrationSceneId sceneId)
        {
            Load(MigrationSceneCatalog.ToSceneName(sceneId));
        }

        public void Load(string sceneName)
        {
            if (isTransitioning)
            {
                Debug.LogWarning($"Scene transition already in progress. Ignored request for {sceneName}.");
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            isTransitioning = true;
            yield return FadeTo(1f);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogError($"Unable to load scene: {sceneName}");
                yield return FadeTo(0f);
                isTransitioning = false;
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }

            yield return FadeTo(0f);
            isTransitioning = false;
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            if (fadeCanvasGroup == null || fadeDurationSeconds <= 0f)
            {
                yield break;
            }

            float startAlpha = fadeCanvasGroup.alpha;
            float elapsed = 0f;
            fadeCanvasGroup.blocksRaycasts = targetAlpha > 0.01f;

            while (elapsed < fadeDurationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDurationSeconds);
                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            fadeCanvasGroup.alpha = targetAlpha;
            fadeCanvasGroup.blocksRaycasts = targetAlpha > 0.01f;
        }
    }
}
