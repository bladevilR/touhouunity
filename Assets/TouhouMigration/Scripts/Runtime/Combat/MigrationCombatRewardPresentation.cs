using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class MigrationCombatRewardPresentation : MonoBehaviour
    {
        [SerializeField] private float displaySeconds = 0.8f;
        [SerializeField] private Color rewardColor = new Color(1f, 0.86f, 0.32f, 1f);
        [SerializeField] private Color lootColor = new Color(0.42f, 1f, 0.58f, 1f);

        private MigrationCombatDefeatRewardHandler rewardHandler;
        private MigrationCombatLootDropHandler lootDropHandler;
        private TextMesh rewardTextMesh;
        private TextMesh lootTextMesh;
        private bool rewardSubscribed;
        private bool lootSubscribed;
        private float rewardDisplayRemaining;
        private float lootDisplayRemaining;

        public int RewardNotificationCount { get; private set; }
        public int LootNotificationCount { get; private set; }
        public string LastRewardText { get; private set; } = string.Empty;
        public string LastLootText { get; private set; } = string.Empty;
        public bool HasActiveRewardNotification => rewardTextMesh != null && rewardTextMesh.gameObject.activeSelf;
        public bool HasActiveLootNotification => lootTextMesh != null && lootTextMesh.gameObject.activeSelf;

        public void BindRewardHandler(MigrationCombatDefeatRewardHandler rewardHandler)
        {
            if (this.rewardHandler == rewardHandler)
            {
                SubscribeReward();
                return;
            }

            UnsubscribeReward();
            this.rewardHandler = rewardHandler;
            SubscribeReward();
        }

        public void BindLootDropHandler(MigrationCombatLootDropHandler lootDropHandler)
        {
            if (this.lootDropHandler == lootDropHandler)
            {
                SubscribeLoot();
                return;
            }

            UnsubscribeLoot();
            this.lootDropHandler = lootDropHandler;
            SubscribeLoot();
        }

        public void ConfigurePresentation(float displaySeconds, Color rewardColor, Color lootColor)
        {
            this.displaySeconds = Mathf.Max(0f, displaySeconds);
            this.rewardColor = rewardColor;
            this.lootColor = lootColor;
            rewardHandler ??= GetComponent<MigrationCombatDefeatRewardHandler>();
            lootDropHandler ??= GetComponent<MigrationCombatLootDropHandler>();
            SubscribeReward();
            SubscribeLoot();
        }

        public void Tick(float deltaTime)
        {
            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            if (HasActiveRewardNotification)
            {
                rewardDisplayRemaining = Mathf.Max(0f, rewardDisplayRemaining - safeDeltaTime);
                if (rewardDisplayRemaining <= 0.0001f)
                {
                    rewardTextMesh.gameObject.SetActive(false);
                }
            }

            if (HasActiveLootNotification)
            {
                lootDisplayRemaining = Mathf.Max(0f, lootDisplayRemaining - safeDeltaTime);
                if (lootDisplayRemaining <= 0.0001f)
                {
                    lootTextMesh.gameObject.SetActive(false);
                }
            }
        }

        private void Awake()
        {
            rewardHandler ??= GetComponent<MigrationCombatDefeatRewardHandler>();
            lootDropHandler ??= GetComponent<MigrationCombatLootDropHandler>();
        }

        private void OnEnable()
        {
            rewardHandler ??= GetComponent<MigrationCombatDefeatRewardHandler>();
            lootDropHandler ??= GetComponent<MigrationCombatLootDropHandler>();
            SubscribeReward();
            SubscribeLoot();
        }

        private void OnDisable()
        {
            UnsubscribeReward();
            UnsubscribeLoot();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void OnRewardsGranted(int experience, int coins, string questCounterId)
        {
            RewardNotificationCount++;
            LastRewardText = FormatRewardText(experience, coins);
            TextMesh textMesh = EnsureTextMesh(ref rewardTextMesh, "MigrationRewardNotification", 1.72f);
            textMesh.text = LastRewardText;
            textMesh.color = rewardColor;
            textMesh.gameObject.SetActive(true);
            rewardDisplayRemaining = displaySeconds;
        }

        private void OnLootGranted(string itemId, int amount)
        {
            LootNotificationCount++;
            LastLootText = $"+{Mathf.Max(0, amount)} {itemId}";
            TextMesh textMesh = EnsureTextMesh(ref lootTextMesh, "MigrationLootNotification", 1.48f);
            textMesh.text = LastLootText;
            textMesh.color = lootColor;
            textMesh.gameObject.SetActive(true);
            lootDisplayRemaining = displaySeconds;
        }

        private TextMesh EnsureTextMesh(ref TextMesh textMesh, string objectName, float verticalOffset)
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
            textMesh.characterSize = 0.15f;
            textMesh.fontSize = 30;
            return textMesh;
        }

        private void SubscribeReward()
        {
            if (rewardHandler == null || rewardSubscribed)
            {
                return;
            }

            rewardHandler.RewardsGranted += OnRewardsGranted;
            rewardSubscribed = true;
        }

        private void UnsubscribeReward()
        {
            if (rewardHandler == null || !rewardSubscribed)
            {
                return;
            }

            rewardHandler.RewardsGranted -= OnRewardsGranted;
            rewardSubscribed = false;
        }

        private void SubscribeLoot()
        {
            if (lootDropHandler == null || lootSubscribed)
            {
                return;
            }

            lootDropHandler.LootGranted += OnLootGranted;
            lootSubscribed = true;
        }

        private void UnsubscribeLoot()
        {
            if (lootDropHandler == null || !lootSubscribed)
            {
                return;
            }

            lootDropHandler.LootGranted -= OnLootGranted;
            lootSubscribed = false;
        }

        private static string FormatRewardText(int experience, int coins)
        {
            return $"+{Mathf.Max(0, experience)} XP  +{Mathf.Max(0, coins)} coins";
        }
    }
}
