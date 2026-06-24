using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    [RequireComponent(typeof(MigrationCombatTargetBehaviour))]
    public sealed class MigrationCombatHurtFeedback : MonoBehaviour
    {
        [SerializeField] private float flashDurationSeconds = 0.16f;
        [SerializeField] private Color flashColor = new Color(1f, 0.15f, 0.1f, 1f);
        [SerializeField] private float knockbackDistance = 0.08f;

        private MigrationCombatTargetBehaviour target;
        private Renderer[] renderers = System.Array.Empty<Renderer>();
        private Color[] originalColors = System.Array.Empty<Color>();
        private Vector3 lastHitSource;
        private bool hasHitSource;
        private bool subscribed;
        private float flashRemaining;

        public bool IsFlashActive { get; private set; }
        public int FlashEventCount { get; private set; }
        public int KnockbackEventCount { get; private set; }
        public Vector3 LastKnockbackDirection { get; private set; }
        public float FlashDurationSeconds => flashDurationSeconds;

        public void ConfigureFeedback(float durationSeconds, Color color, float knockbackDistance)
        {
            flashDurationSeconds = Mathf.Max(0f, durationSeconds);
            flashColor = color;
            this.knockbackDistance = Mathf.Max(0f, knockbackDistance);
            target ??= GetComponent<MigrationCombatTargetBehaviour>();
            CacheRenderers();
            Subscribe();
        }

        public void BindTarget(MigrationCombatTargetBehaviour target)
        {
            if (this.target == target)
            {
                Subscribe();
                return;
            }

            Unsubscribe();
            this.target = target;
            Subscribe();
        }

        public void SetLastHitSource(Vector3 sourcePosition)
        {
            lastHitSource = sourcePosition;
            hasHitSource = true;
            target ??= GetComponent<MigrationCombatTargetBehaviour>();
            Subscribe();
        }

        public void Tick(float deltaTime)
        {
            if (!IsFlashActive)
            {
                return;
            }

            flashRemaining = Mathf.Max(0f, flashRemaining - Mathf.Max(0f, deltaTime));
            if (flashRemaining <= 0.0001f)
            {
                RestoreRendererColors();
                IsFlashActive = false;
            }
        }

        private void Awake()
        {
            target ??= GetComponent<MigrationCombatTargetBehaviour>();
            CacheRenderers();
        }

        private void OnEnable()
        {
            target ??= GetComponent<MigrationCombatTargetBehaviour>();
            CacheRenderers();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void Subscribe()
        {
            if (target == null || subscribed)
            {
                return;
            }

            target.Damaged += OnTargetDamaged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (target == null || !subscribed)
            {
                return;
            }

            target.Damaged -= OnTargetDamaged;
            subscribed = false;
        }

        private void OnTargetDamaged(CombatBridgeResult result)
        {
            StartFlash();
            ApplyKnockback();
        }

        private void StartFlash()
        {
            CacheRenderers();
            flashRemaining = flashDurationSeconds;
            IsFlashActive = flashDurationSeconds > 0f;
            FlashEventCount++;

            foreach (Renderer renderer in renderers)
            {
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    renderer.sharedMaterial.color = flashColor;
                }
            }
        }

        private void ApplyKnockback()
        {
            if (knockbackDistance <= 0f)
            {
                return;
            }

            Vector3 direction = hasHitSource
                ? transform.position - lastHitSource
                : transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = transform.forward.sqrMagnitude > 0.0001f ? transform.forward : Vector3.forward;
                direction.y = 0f;
            }

            LastKnockbackDirection = direction.normalized;
            transform.position += LastKnockbackDirection * knockbackDistance;
            KnockbackEventCount++;
        }

        private void CacheRenderers()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                originalColors[i] = renderer != null && renderer.sharedMaterial != null
                    ? renderer.sharedMaterial.color
                    : Color.white;
            }
        }

        private void RestoreRendererColors()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer != null && renderer.sharedMaterial != null && i < originalColors.Length)
                {
                    renderer.sharedMaterial.color = originalColors[i];
                }
            }
        }
    }
}
