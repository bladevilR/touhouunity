using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    [RequireComponent(typeof(MigrationEnemyProjectile))]
    public sealed class MigrationProjectileShatterPresenter : MonoBehaviour
    {
        [SerializeField] private MigrationEnemyProjectile projectile;
        [SerializeField] private float displaySeconds = 0.5f;
        [SerializeField] private Color shatterColor = new Color(0.55f, 0.95f, 1f, 1f);
        [SerializeField] private Color weaknessShatterColor = new Color(1f, 0.74f, 0.28f, 1f);

        private TextMesh shatterTextMesh;
        private bool subscribed;
        private float displayRemaining;

        public int ShatterNotificationCount { get; private set; }
        public string LastShatterText { get; private set; } = string.Empty;
        public bool HasActiveShatterNotification => shatterTextMesh != null && shatterTextMesh.gameObject.activeSelf;

        public void BindProjectile(MigrationEnemyProjectile projectile)
        {
            if (this.projectile == projectile)
            {
                Subscribe();
                return;
            }

            Unsubscribe();
            this.projectile = projectile;
            Subscribe();
        }

        public void ConfigurePresentation(float displaySeconds, Color shatterColor, Color weaknessShatterColor)
        {
            this.displaySeconds = Mathf.Max(0f, displaySeconds);
            this.shatterColor = shatterColor;
            this.weaknessShatterColor = weaknessShatterColor;
            projectile ??= GetComponent<MigrationEnemyProjectile>();
            Subscribe();
        }

        public void Tick(float deltaTime)
        {
            if (!HasActiveShatterNotification)
            {
                return;
            }

            displayRemaining = Mathf.Max(0f, displayRemaining - Mathf.Max(0f, deltaTime));
            if (displayRemaining <= 0.0001f)
            {
                shatterTextMesh.gameObject.SetActive(false);
            }
        }

        private void Awake()
        {
            projectile ??= GetComponent<MigrationEnemyProjectile>();
        }

        private void OnEnable()
        {
            projectile ??= GetComponent<MigrationEnemyProjectile>();
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

        private void OnProjectileShattered(MigrationProjectileShatterResult result)
        {
            ShatterNotificationCount++;
            LastShatterText = "Shatter";

            TextMesh textMesh = EnsureTextMesh();
            textMesh.text = LastShatterText;
            textMesh.color = result.WasWeakness ? weaknessShatterColor : shatterColor;
            textMesh.transform.position = result.Position + new Vector3(0f, 0.85f, 0f);
            textMesh.gameObject.SetActive(true);
            displayRemaining = displaySeconds;
        }

        private TextMesh EnsureTextMesh()
        {
            if (shatterTextMesh != null)
            {
                return shatterTextMesh;
            }

            GameObject textObject = new GameObject("MigrationProjectileShatterNotification");
            textObject.transform.SetParent(transform, false);
            shatterTextMesh = textObject.AddComponent<TextMesh>();
            shatterTextMesh.anchor = TextAnchor.MiddleCenter;
            shatterTextMesh.alignment = TextAlignment.Center;
            shatterTextMesh.characterSize = 0.14f;
            shatterTextMesh.fontSize = 28;
            return shatterTextMesh;
        }

        private void Subscribe()
        {
            if (projectile == null || subscribed)
            {
                return;
            }

            projectile.Shattered += OnProjectileShattered;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (projectile == null || !subscribed)
            {
                return;
            }

            projectile.Shattered -= OnProjectileShattered;
            subscribed = false;
        }
    }
}
