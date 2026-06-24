using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    [RequireComponent(typeof(MigrationEnemyProjectile))]
    public sealed class MigrationProjectileGrazePresenter : MonoBehaviour
    {
        [SerializeField] private MigrationEnemyProjectile projectile;
        [SerializeField] private float displaySeconds = 0.45f;
        [SerializeField] private Color normalGrazeColor = new Color(1f, 0.72f, 0.25f, 1f);
        [SerializeField] private Color perfectGrazeColor = new Color(0.4f, 0.95f, 1f, 1f);

        private TextMesh grazeTextMesh;
        private bool subscribed;
        private float displayRemaining;

        public int GrazeNotificationCount { get; private set; }
        public string LastGrazeText { get; private set; } = string.Empty;
        public bool HasActiveGrazeNotification => grazeTextMesh != null && grazeTextMesh.gameObject.activeSelf;

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

        public void ConfigurePresentation(float displaySeconds, Color normalGrazeColor, Color perfectGrazeColor)
        {
            this.displaySeconds = Mathf.Max(0f, displaySeconds);
            this.normalGrazeColor = normalGrazeColor;
            this.perfectGrazeColor = perfectGrazeColor;
            projectile ??= GetComponent<MigrationEnemyProjectile>();
            Subscribe();
        }

        public void Tick(float deltaTime)
        {
            if (!HasActiveGrazeNotification)
            {
                return;
            }

            displayRemaining = Mathf.Max(0f, displayRemaining - Mathf.Max(0f, deltaTime));
            if (displayRemaining <= 0.0001f)
            {
                grazeTextMesh.gameObject.SetActive(false);
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

        private void OnProjectileGrazed(MigrationProjectileGrazeResult result)
        {
            GrazeNotificationCount++;
            LastGrazeText = result.IsPerfect ? "Perfect Graze" : "Graze";

            TextMesh textMesh = EnsureTextMesh();
            textMesh.text = LastGrazeText;
            textMesh.color = result.IsPerfect ? perfectGrazeColor : normalGrazeColor;
            textMesh.transform.position = result.PlayerPosition + new Vector3(0f, 1.1f, 0f);
            textMesh.gameObject.SetActive(true);
            displayRemaining = displaySeconds;
        }

        private TextMesh EnsureTextMesh()
        {
            if (grazeTextMesh != null)
            {
                return grazeTextMesh;
            }

            GameObject textObject = new GameObject("MigrationProjectileGrazeNotification");
            textObject.transform.SetParent(transform, false);
            grazeTextMesh = textObject.AddComponent<TextMesh>();
            grazeTextMesh.anchor = TextAnchor.MiddleCenter;
            grazeTextMesh.alignment = TextAlignment.Center;
            grazeTextMesh.characterSize = 0.14f;
            grazeTextMesh.fontSize = 28;
            return grazeTextMesh;
        }

        private void Subscribe()
        {
            if (projectile == null || subscribed)
            {
                return;
            }

            projectile.Grazed += OnProjectileGrazed;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (projectile == null || !subscribed)
            {
                return;
            }

            projectile.Grazed -= OnProjectileGrazed;
            subscribed = false;
        }
    }
}
