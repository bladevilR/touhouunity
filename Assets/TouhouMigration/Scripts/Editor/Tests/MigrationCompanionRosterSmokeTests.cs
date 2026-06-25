using System;
using TouhouMigration.Runtime.Social;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCompanionRoster: companion recruitment + the single-companion party (Godot
    // CompanionSystem recruit_companion / add_to_party / remove_from_party). Combat stats, skills, HP,
    // recruitment-condition checks, and schedule conflicts are deferred (data/signal-coupled).
    public static class MigrationCompanionRosterSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Migration Companion Roster Smoke Tests")]
        public static void RunAll()
        {
            TestRecruitOncePerCompanion();
            TestAddToPartyRequiresRecruitment();
            TestPartyHoldsOneCompanion();
            TestRemoveFromPartyFreesTheSlot();
            TestRemoveUnrecruitedOrAbsentFails();
            TestGetAllRecruited();
            TestSkillStartsReady();
            TestPutSkillOnCooldownRequiresRecruitment();
            TestTickReducesCooldownToReady();
            TestTickAffectsEachSkillIndependently();
            Debug.Log("Migration companion roster smoke tests passed.");
        }

        private static void TestRecruitOncePerCompanion()
        {
            MigrationCompanionRoster roster = new MigrationCompanionRoster();
            AssertEqual(true, roster.Recruit("reimu"), "A new companion can be recruited.");
            AssertEqual(true, roster.IsRecruited("reimu"), "A recruited companion is recorded.");
            AssertEqual(false, roster.Recruit("reimu"), "An already-recruited companion is not recruited again.");
            AssertEqual(false, roster.IsRecruited("marisa"), "An unrecruited companion is not recorded.");
        }

        private static void TestAddToPartyRequiresRecruitment()
        {
            MigrationCompanionRoster roster = new MigrationCompanionRoster();
            AssertEqual(false, roster.AddToParty("reimu"), "An unrecruited companion cannot join the party.");
            roster.Recruit("reimu");
            AssertEqual(true, roster.AddToParty("reimu"), "A recruited companion can join the party.");
            AssertEqual(true, roster.IsInParty("reimu"), "The companion is in the party.");
            AssertEqual("reimu", roster.ActiveCompanionId, "The active companion is set.");
        }

        private static void TestPartyHoldsOneCompanion()
        {
            MigrationCompanionRoster roster = new MigrationCompanionRoster();
            roster.Recruit("reimu");
            roster.Recruit("marisa");
            AssertEqual(true, roster.AddToParty("reimu"), "The first companion joins.");
            AssertEqual(false, roster.AddToParty("marisa"), "A second companion cannot join while one is active.");
            AssertEqual("reimu", roster.ActiveCompanionId, "The active companion is unchanged.");
            AssertEqual(false, roster.IsInParty("marisa"), "The rejected companion is not in the party.");
        }

        private static void TestRemoveFromPartyFreesTheSlot()
        {
            MigrationCompanionRoster roster = new MigrationCompanionRoster();
            roster.Recruit("reimu");
            roster.Recruit("marisa");
            roster.AddToParty("reimu");
            AssertEqual(true, roster.RemoveFromParty("reimu"), "The active companion can leave the party.");
            AssertEqual(false, roster.IsInParty("reimu"), "The companion is no longer in the party.");
            AssertEqual(string.Empty, roster.ActiveCompanionId, "The active slot is cleared.");
            AssertEqual(true, roster.AddToParty("marisa"), "Another companion can join once the slot is free.");
        }

        private static void TestRemoveUnrecruitedOrAbsentFails()
        {
            MigrationCompanionRoster roster = new MigrationCompanionRoster();
            AssertEqual(false, roster.RemoveFromParty("nobody"), "Removing an unrecruited companion fails.");
            roster.Recruit("reimu");
            AssertEqual(false, roster.RemoveFromParty("reimu"), "Removing a recruited companion not in the party fails.");
        }

        private static void TestGetAllRecruited()
        {
            MigrationCompanionRoster roster = new MigrationCompanionRoster();
            roster.Recruit("reimu");
            roster.Recruit("marisa");
            AssertEqual(2, roster.GetAllRecruited().Count, "All recruited companions are listed.");
        }

        private static void TestSkillStartsReady()
        {
            MigrationCompanionRoster roster = new MigrationCompanionRoster();
            roster.Recruit("reimu");
            AssertEqual(0.0, roster.GetSkillCooldown("reimu", "spell"), "An unused skill has no cooldown.");
            AssertEqual(true, roster.IsSkillReady("reimu", "spell"), "An un-cooled skill is ready.");
        }

        private static void TestPutSkillOnCooldownRequiresRecruitment()
        {
            MigrationCompanionRoster roster = new MigrationCompanionRoster();
            roster.PutSkillOnCooldown("reimu", "spell", 5.0);
            AssertEqual(0.0, roster.GetSkillCooldown("reimu", "spell"), "An unrecruited companion holds no cooldown.");
            roster.Recruit("reimu");
            roster.PutSkillOnCooldown("reimu", "spell", 5.0);
            AssertEqual(5.0, roster.GetSkillCooldown("reimu", "spell"), "Using a skill puts it on cooldown.");
            AssertEqual(false, roster.IsSkillReady("reimu", "spell"), "A skill on cooldown is not ready.");
        }

        private static void TestTickReducesCooldownToReady()
        {
            MigrationCompanionRoster roster = new MigrationCompanionRoster();
            roster.Recruit("reimu");
            roster.PutSkillOnCooldown("reimu", "spell", 5.0);
            roster.TickSkillCooldowns(2.0);
            AssertEqual(3.0, roster.GetSkillCooldown("reimu", "spell"), "Ticking reduces the cooldown by the delta.");
            roster.TickSkillCooldowns(5.0);
            AssertEqual(0.0, roster.GetSkillCooldown("reimu", "spell"), "The cooldown clamps at zero.");
            AssertEqual(true, roster.IsSkillReady("reimu", "spell"), "A counted-down skill is ready again.");
        }

        private static void TestTickAffectsEachSkillIndependently()
        {
            MigrationCompanionRoster roster = new MigrationCompanionRoster();
            roster.Recruit("reimu");
            roster.PutSkillOnCooldown("reimu", "spell1", 4.0);
            roster.PutSkillOnCooldown("reimu", "spell2", 6.0);
            roster.TickSkillCooldowns(4.0);
            AssertEqual(true, roster.IsSkillReady("reimu", "spell1"), "The shorter cooldown is ready.");
            AssertEqual(2.0, roster.GetSkillCooldown("reimu", "spell2"), "The longer cooldown still has time left.");
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
