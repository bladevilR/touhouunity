using System;
using TouhouMigration.Runtime.Player;
using TouhouMigration.Runtime.Social;
using TouhouMigration.Runtime.UI;
using UnityEngine;

namespace TouhouMigration.Runtime.Combat
{
    [RequireComponent(typeof(MigrationCombatTargetBehaviour))]
    public sealed class MigrationCombatDefeatRewardHandler : MonoBehaviour
    {
        [SerializeField] private int experienceReward = 10;
        [SerializeField] private int coinReward = 10;
        [SerializeField] private string questCounterId = "enemy_killed";

        private MigrationCombatTargetBehaviour target;
        private MigrationPlayerProgressService progressService;
        private QuestDeliveryService questDeliveryService;
        private bool rewarded;
        private bool subscribed;

        public event Action<int, int, string> RewardsGranted;

        public int RewardGrantCount { get; private set; }

        public void BindTarget(MigrationCombatTargetBehaviour target)
        {
            Unsubscribe();
            this.target = target;
            Subscribe();
        }

        public void BindRewards(MigrationPlayerProgressService progressService, QuestDeliveryService questDeliveryService)
        {
            this.progressService = progressService;
            this.questDeliveryService = questDeliveryService;
        }

        public void ConfigureRewards(int experienceReward, int coinReward, string questCounterId)
        {
            this.experienceReward = Mathf.Max(0, experienceReward);
            this.coinReward = Mathf.Max(0, coinReward);
            this.questCounterId = string.IsNullOrWhiteSpace(questCounterId)
                ? string.Empty
                : questCounterId.Trim().ToLowerInvariant();
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

        private void OnTargetDefeated(CombatBridgeResult result)
        {
            if (rewarded)
            {
                return;
            }

            rewarded = true;
            RewardGrantCount++;

            progressService ??= MigrationGlobalUiController.FindPlayerProgressService();
            questDeliveryService ??= MigrationGlobalUiController.FindQuestDeliveryService();

            progressService?.GainExperience(experienceReward);
            progressService?.AddCoins(coinReward);
            progressService?.RegisterKill();

            if (!string.IsNullOrEmpty(questCounterId))
            {
                questDeliveryService?.IncrementCounter(questCounterId, 1);
            }

            RewardsGranted?.Invoke(experienceReward, coinReward, questCounterId);
        }

        private void Subscribe()
        {
            if (target == null || subscribed)
            {
                return;
            }

            target.Defeated += OnTargetDefeated;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (target == null || !subscribed)
            {
                return;
            }

            target.Defeated -= OnTargetDefeated;
            subscribed = false;
        }
    }
}
