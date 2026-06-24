using TouhouMigration.Runtime.Settings;
using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    [RequireComponent(typeof(MigrationCombatTargetBehaviour))]
    public sealed class MigrationDamageNumberPresenter : MonoBehaviour
    {
        [SerializeField] private float displaySeconds = 0.45f;
        [SerializeField] private float verticalOffset = 1.35f;
        [SerializeField] private Color textColor = new Color(1f, 0.92f, 0.35f, 1f);

        private MigrationCombatTargetBehaviour target;
        private MigrationGameSettings settings;
        private TextMesh activeTextMesh;
        private float displayRemaining;
        private bool subscribed;

        public int DamageNumberEventCount { get; private set; }
        public int SuppressedDamageNumberCount { get; private set; }
        public string LastDamageText { get; private set; } = string.Empty;
        public bool HasActiveDamageNumber => activeTextMesh != null && activeTextMesh.gameObject.activeSelf;

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

        public void BindSettings(MigrationGameSettings settings)
        {
            this.settings = settings;
        }

        public void ConfigurePresentation(float displaySeconds, float verticalOffset, Color textColor)
        {
            this.displaySeconds = Mathf.Max(0f, displaySeconds);
            this.verticalOffset = Mathf.Max(0f, verticalOffset);
            this.textColor = textColor;
            target ??= GetComponent<MigrationCombatTargetBehaviour>();
            Subscribe();
        }

        public void Tick(float deltaTime)
        {
            if (!HasActiveDamageNumber)
            {
                return;
            }

            displayRemaining = Mathf.Max(0f, displayRemaining - Mathf.Max(0f, deltaTime));
            if (displayRemaining <= 0.0001f)
            {
                activeTextMesh.gameObject.SetActive(false);
            }
        }

        private void Awake()
        {
            target ??= GetComponent<MigrationCombatTargetBehaviour>();
        }

        private void OnEnable()
        {
            target ??= GetComponent<MigrationCombatTargetBehaviour>();
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

        private void OnTargetDamaged(CombatBridgeResult result)
        {
            if (settings != null && !settings.ShowDamageNumbers)
            {
                SuppressedDamageNumberCount++;
                return;
            }

            DamageNumberEventCount++;
            LastDamageText = FormatDamage(result.DamageApplied);
            ShowDamageText(LastDamageText);
        }

        private void ShowDamageText(string text)
        {
            TextMesh textMesh = EnsureTextMesh();
            textMesh.text = text;
            textMesh.color = textColor;
            textMesh.transform.localPosition = new Vector3(0f, verticalOffset, 0f);
            textMesh.gameObject.SetActive(true);
            displayRemaining = displaySeconds;
        }

        private TextMesh EnsureTextMesh()
        {
            if (activeTextMesh != null)
            {
                return activeTextMesh;
            }

            GameObject textObject = new GameObject("MigrationDamageNumber");
            textObject.transform.SetParent(transform, false);
            activeTextMesh = textObject.AddComponent<TextMesh>();
            activeTextMesh.anchor = TextAnchor.MiddleCenter;
            activeTextMesh.alignment = TextAlignment.Center;
            activeTextMesh.characterSize = 0.18f;
            activeTextMesh.fontSize = 36;
            return activeTextMesh;
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

        private static string FormatDamage(float amount)
        {
            int roundedDamage = Mathf.Max(0, Mathf.RoundToInt(amount));
            return $"-{roundedDamage}";
        }
    }
}
