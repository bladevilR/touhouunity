using System;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCardRewardFlow: the post-run reward state machine (Godot
    // CardBuildRunProgressionController reward_state: none -> pending on win -> claimed).
    public static class CardRewardFlowSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Card Reward Flow Smoke Tests")]
        public static void RunAll()
        {
            TestInitialState();
            TestWinOffersDefaultRewards();
            TestClaimAppliesAndLocks();
            TestUnknownOfferIsRejected();
            TestFailedApplyKeepsPending();
            Debug.Log("Card reward flow smoke tests passed.");
        }

        private static void TestInitialState()
        {
            MigrationCardRewardFlow flow = new MigrationCardRewardFlow();
            AssertEqual(CardRewardStatus.None, flow.Status, "A fresh run has no reward.");
            AssertEqual(false, flow.IsClaimable, "Nothing is claimable before a win.");
            AssertEqual(false, flow.Claim("any", _ => true), "Claiming before a win does nothing.");
        }

        private static void TestWinOffersDefaultRewards()
        {
            MigrationCardRewardFlow flow = new MigrationCardRewardFlow();
            flow.OnWin();
            AssertEqual(CardRewardStatus.Pending, flow.Status, "Winning makes the reward pending.");
            AssertEqual(true, flow.IsClaimable, "A pending reward is claimable.");
            AssertEqual(3, flow.Offers.Count, "The Cirno win offers three rewards.");
        }

        private static void TestClaimAppliesAndLocks()
        {
            MigrationCardRewardFlow flow = new MigrationCardRewardFlow();
            flow.OnWin();
            string offer = "reward_relic_phoenix_ash_lantern";

            bool granted = false;
            AssertEqual(true, flow.Claim(offer, id => { granted = id == offer; return true; }),
                "Claiming a valid offer succeeds.");
            AssertEqual(true, granted, "The reward is applied via the callback.");
            AssertEqual(CardRewardStatus.Claimed, flow.Status, "The reward is now claimed.");
            AssertEqual(offer, flow.ClaimedOfferId, "The claimed offer id is recorded.");
            AssertEqual(false, flow.IsClaimable, "A claimed reward is no longer claimable.");

            // Claiming again is idempotent.
            AssertEqual(false, flow.Claim("reward_card_mokou_starter_fire_bird", _ => true),
                "A second claim does nothing.");
            AssertEqual(offer, flow.ClaimedOfferId, "The original claim stands.");
        }

        private static void TestUnknownOfferIsRejected()
        {
            MigrationCardRewardFlow flow = new MigrationCardRewardFlow();
            flow.OnWin();
            AssertEqual(false, flow.Claim("not_an_offer", _ => true), "An unknown offer cannot be claimed.");
            AssertEqual(CardRewardStatus.Pending, flow.Status, "It stays pending after an unknown offer.");
            AssertEqual(true, flow.ValidationErrors.Count > 0, "An unknown offer records a validation error.");
        }

        private static void TestFailedApplyKeepsPending()
        {
            MigrationCardRewardFlow flow = new MigrationCardRewardFlow();
            flow.OnWin();
            AssertEqual(false, flow.Claim("reward_relic_phoenix_ash_lantern", _ => false),
                "A reward whose application fails is not claimed.");
            AssertEqual(CardRewardStatus.Pending, flow.Status, "It stays pending so it can be retried.");
            AssertEqual(true, flow.ValidationErrors.Count > 0, "A failed apply records validation errors.");
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
