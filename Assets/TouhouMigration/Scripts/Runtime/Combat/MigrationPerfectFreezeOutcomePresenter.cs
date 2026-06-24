using System;
using System.Globalization;
using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class MigrationPerfectFreezeOutcomePresenter : MonoBehaviour
    {
        [SerializeField] private float displaySeconds = 1.1f;
        [SerializeField] private Color clearColor = new Color(0.64f, 0.95f, 1f, 1f);
        [SerializeField] private Color captureColor = new Color(1f, 0.86f, 0.34f, 1f);
        [SerializeField] private Color timeoutColor = new Color(0.74f, 0.78f, 0.86f, 1f);

        private MigrationPerfectFreezeEncounterDirector director;
        private TextMesh outcomeTextMesh;
        private TextMesh bonusTextMesh;
        private bool subscribed;
        private float outcomeDisplayRemaining;
        private float bonusDisplayRemaining;

        public int OutcomeNotificationCount { get; private set; }
        public int BonusNotificationCount { get; private set; }
        public string LastOutcomeText { get; private set; } = string.Empty;
        public string LastBonusText { get; private set; } = string.Empty;
        public MigrationPerfectFreezePhaseResult LastPresentedResult { get; private set; }
        public bool HasActiveOutcomeNotification => outcomeTextMesh != null && outcomeTextMesh.gameObject.activeSelf;
        public bool HasActiveBonusNotification => bonusTextMesh != null && bonusTextMesh.gameObject.activeSelf;

        public void BindDirector(MigrationPerfectFreezeEncounterDirector director)
        {
            if (this.director == director)
            {
                Subscribe();
                return;
            }

            Unsubscribe();
            this.director = director;
            Subscribe();
        }

        public void ConfigurePresentation(
            float displaySeconds,
            Color clearColor,
            Color captureColor,
            Color timeoutColor)
        {
            this.displaySeconds = Mathf.Max(0f, displaySeconds);
            this.clearColor = clearColor;
            this.captureColor = captureColor;
            this.timeoutColor = timeoutColor;
            director ??= GetComponent<MigrationPerfectFreezeEncounterDirector>();
            Subscribe();
        }

        public void Tick(float deltaTime)
        {
            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            if (HasActiveOutcomeNotification)
            {
                outcomeDisplayRemaining = Mathf.Max(0f, outcomeDisplayRemaining - safeDeltaTime);
                if (outcomeDisplayRemaining <= 0.0001f)
                {
                    outcomeTextMesh.gameObject.SetActive(false);
                }
            }

            if (HasActiveBonusNotification)
            {
                bonusDisplayRemaining = Mathf.Max(0f, bonusDisplayRemaining - safeDeltaTime);
                if (bonusDisplayRemaining <= 0.0001f)
                {
                    bonusTextMesh.gameObject.SetActive(false);
                }
            }
        }

        private void Awake()
        {
            director ??= GetComponent<MigrationPerfectFreezeEncounterDirector>();
        }

        private void OnEnable()
        {
            director ??= GetComponent<MigrationPerfectFreezeEncounterDirector>();
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

        private void OnPhaseFinished(MigrationPerfectFreezePhaseResult result)
        {
            if (result == null)
            {
                return;
            }

            LastPresentedResult = result;
            LastOutcomeText = FormatOutcomeText(result);
            LastBonusText = FormatBonusText(result);

            OutcomeNotificationCount++;
            BonusNotificationCount++;

            TextMesh outcomeText = EnsureTextMesh(ref outcomeTextMesh, "MigrationPerfectFreezeOutcome", 2.12f, 36);
            outcomeText.text = LastOutcomeText;
            outcomeText.color = ResolveOutcomeColor(result);
            outcomeText.gameObject.SetActive(true);
            outcomeDisplayRemaining = displaySeconds;

            TextMesh bonusText = EnsureTextMesh(ref bonusTextMesh, "MigrationPerfectFreezeOutcomeBonus", 1.86f, 28);
            bonusText.text = LastBonusText;
            bonusText.color = ResolveOutcomeColor(result);
            bonusText.gameObject.SetActive(true);
            bonusDisplayRemaining = displaySeconds;
        }

        private TextMesh EnsureTextMesh(ref TextMesh textMesh, string objectName, float verticalOffset, int fontSize)
        {
            if (textMesh != null)
            {
                return textMesh;
            }

            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = new Vector3(0f, verticalOffset, 0f);
            textMesh = textObject.AddComponent<TextMesh>();
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.16f;
            textMesh.fontSize = fontSize;
            return textMesh;
        }

        private void Subscribe()
        {
            if (director == null || subscribed)
            {
                return;
            }

            director.PhaseFinished += OnPhaseFinished;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (director == null || !subscribed)
            {
                return;
            }

            director.PhaseFinished -= OnPhaseFinished;
            subscribed = false;
        }

        private Color ResolveOutcomeColor(MigrationPerfectFreezePhaseResult result)
        {
            if (result.Captured)
            {
                return captureColor;
            }

            return string.Equals(result.Reason, "timeout", StringComparison.Ordinal)
                ? timeoutColor
                : clearColor;
        }

        private static string FormatOutcomeText(MigrationPerfectFreezePhaseResult result)
        {
            if (result.Captured)
            {
                return "Perfect Freeze Capture";
            }

            if (string.Equals(result.Reason, "timeout", StringComparison.Ordinal))
            {
                return "Perfect Freeze Timeout";
            }

            return "Perfect Freeze Clear";
        }

        private static string FormatBonusText(MigrationPerfectFreezePhaseResult result)
        {
            if (result.TotalBonus <= 0.0001f && result.StunSeconds <= 0.0001f)
            {
                return "No bonus";
            }

            int roundedBonus = Mathf.Max(0, Mathf.RoundToInt(result.TotalBonus));
            string stunText = result.StunSeconds.ToString("0.#", CultureInfo.InvariantCulture);
            return $"+{roundedBonus} bonus  Stun {stunText}s";
        }
    }
}
