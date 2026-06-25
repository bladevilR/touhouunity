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

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
