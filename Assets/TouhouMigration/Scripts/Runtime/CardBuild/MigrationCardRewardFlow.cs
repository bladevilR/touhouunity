using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.CardBuild
{
    public enum CardRewardStatus
    {
        None,
        Pending,
        Claimed
    }

    // The post-run reward state machine (Godot CardBuildRunProgressionController reward_state):
    // none -> pending (on a win, offering rewards) -> claimed (when a valid offer is applied). Claiming an
    // unknown offer or one whose application fails records a validation error and stays pending so it can be
    // retried. UnityEngine-free; applying the reward to the profile is injected.
    public sealed class MigrationCardRewardFlow
    {
        public static readonly string[] DefaultCirnoRewards =
        {
            "reward_card_mokou_starter_fire_bird",
            "reward_upgrade_quickened_fire_bird",
            "reward_relic_phoenix_ash_lantern",
        };

        private readonly List<string> offers = new List<string>();
        private readonly List<string> validationErrors = new List<string>();

        public CardRewardStatus Status { get; private set; } = CardRewardStatus.None;
        public IReadOnlyList<string> Offers => offers;
        public string ClaimedOfferId { get; private set; } = string.Empty;
        public IReadOnlyList<string> ValidationErrors => validationErrors;
        public bool IsClaimable => Status == CardRewardStatus.Pending;

        // Record a win (Godot record_win): make the reward pending with its offers (the default Cirno
        // rewards when none are supplied).
        public void OnWin(IEnumerable<string> offers = null)
        {
            this.offers.Clear();
            this.offers.AddRange(offers ?? DefaultCirnoRewards);
            validationErrors.Clear();
            Status = CardRewardStatus.Pending;
            ClaimedOfferId = string.Empty;
        }

        // Claim a reward offer (Godot claim_reward): valid only while pending; rejects an unknown offer or
        // one whose application fails (recording a validation error and staying pending). Returns whether
        // the claim succeeded.
        public bool Claim(string offerId, Func<string, bool> applyReward)
        {
            if (Status != CardRewardStatus.Pending)
            {
                return false;
            }

            if (offerId == null || !offers.Contains(offerId))
            {
                validationErrors.Clear();
                validationErrors.Add("unknown reward offer: " + offerId);
                return false;
            }

            bool applied = applyReward != null && applyReward(offerId);
            if (!applied)
            {
                validationErrors.Clear();
                validationErrors.Add("failed to apply reward: " + offerId);
                return false;
            }

            Status = CardRewardStatus.Claimed;
            ClaimedOfferId = offerId;
            validationErrors.Clear();
            return true;
        }
    }
}
