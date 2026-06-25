using System;
using TouhouMigration.Runtime.Foundation;
using TouhouMigration.Runtime.Home;
using TouhouMigration.Runtime.Player;
using TouhouMigration.Runtime.Progression;
using TouhouMigration.Runtime.Save;
using TouhouMigration.Runtime.Social;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class SaveOrchestratorSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Save Orchestrator Smoke Tests")]
        public static void RunAll()
        {
            TestBondStateRoundTripsThroughSaveData();
            TestHumanityRoundTripsThroughSaveData();
            TestFatigueRoundTripsThroughSaveData();
            TestCalendarRoundTripsThroughSaveData();
            TestCompanionRosterRoundTripsThroughSaveData();
            TestHomeStorageRoundTripsThroughSaveData();
            TestMetaProgressionRoundTripsThroughSaveData();
            TestNpcRelationshipRoundTripsThroughSaveData();
            TestCaptureFillsProvidedServiceSnapshots();
            TestNullToleranceForMissingServicesAndData();
            Debug.Log("Save orchestrator smoke tests passed.");
        }

        private static void TestFatigueRoundTripsThroughSaveData()
        {
            MigrationFatigueSystem source = new MigrationFatigueSystem();
            source.AddFatigue(75.0);
            AssertEqual(75.0, source.CurrentFatigue, "Source fatigue should reflect the accrual before capture.");

            MigrationSaveOrchestrator captureOrchestrator =
                new MigrationSaveOrchestrator(null, null, null, null, null, null, source);
            MigrationSaveData data = captureOrchestrator.Capture(new MigrationSaveData());
            AssertEqual(75.0, data.Fatigue, "Capture should store the live fatigue value into the save record.");

            MigrationFatigueSystem restored = new MigrationFatigueSystem();
            AssertEqual(0.0, restored.CurrentFatigue, "A fresh fatigue system should start at zero fatigue.");

            MigrationSaveOrchestrator applyOrchestrator =
                new MigrationSaveOrchestrator(null, null, null, null, null, null, restored);
            applyOrchestrator.Apply(data);
            AssertEqual(75.0, restored.CurrentFatigue, "Apply should restore captured fatigue into the live service.");
            AssertEqual(false, restored.IsExhausted, "Restored fatigue below the exhausted threshold should not latch exhausted.");
        }

        private static void TestCalendarRoundTripsThroughSaveData()
        {
            GameClock source = new GameClock();
            source.SetDate(15, "Autumn", 3);
            source.SetTime(14, 30);

            MigrationSaveOrchestrator captureOrchestrator =
                new MigrationSaveOrchestrator(null, null, null, null, null, null, null, source);
            MigrationSaveData data = captureOrchestrator.Capture(new MigrationSaveData());
            AssertEqual(15, data.calendar.day, "Capture stores the calendar day.");
            AssertEqual("Autumn", data.calendar.season, "Capture stores the season.");
            AssertEqual(3, data.calendar.year, "Capture stores the year.");
            AssertEqual(14, data.calendar.hour, "Capture stores the hour.");
            AssertEqual(30, data.calendar.minute, "Capture stores the minute.");

            GameClock restored = new GameClock();
            MigrationSaveOrchestrator applyOrchestrator =
                new MigrationSaveOrchestrator(null, null, null, null, null, null, null, restored);
            applyOrchestrator.Apply(data);
            AssertEqual(15, restored.Day, "Apply restores the day.");
            AssertEqual(GameSeason.Autumn, restored.Season, "Apply restores the season.");
            AssertEqual(3, restored.Year, "Apply restores the year.");
            AssertEqual(14, restored.Hour, "Apply restores the hour.");
            AssertEqual(30, restored.Minute, "Apply restores the minute.");
        }

        private static void TestCompanionRosterRoundTripsThroughSaveData()
        {
            MigrationCompanionRoster source = new MigrationCompanionRoster();
            source.Recruit("marisa");
            source.Recruit("keine");
            source.AddToParty("keine");

            MigrationSaveOrchestrator captureOrchestrator =
                new MigrationSaveOrchestrator(null, null, null, null, null, null, null, null, source);
            MigrationSaveData data = captureOrchestrator.Capture(new MigrationSaveData());
            AssertEqual(2, data.companions.recruited.Count, "Capture stores the recruited companions.");
            AssertEqual("keine", data.companions.active, "Capture stores the active party member.");

            MigrationCompanionRoster restored = new MigrationCompanionRoster();
            AssertEqual(false, restored.IsRecruited("marisa"), "A fresh roster has no recruited companions.");

            MigrationSaveOrchestrator applyOrchestrator =
                new MigrationSaveOrchestrator(null, null, null, null, null, null, null, null, restored);
            applyOrchestrator.Apply(data);
            AssertEqual(true, restored.IsRecruited("marisa"), "Apply restores recruited companions.");
            AssertEqual(true, restored.IsRecruited("keine"), "Apply restores all recruited companions.");
            AssertEqual("keine", restored.ActiveCompanionId, "Apply restores the active party member.");
        }

        private static void TestHomeStorageRoundTripsThroughSaveData()
        {
            MigrationHomeStorage source = new MigrationHomeStorage();
            source.StoreItem("wood", 12);
            source.StoreItem("mushroom", 5);

            MigrationSaveOrchestrator captureOrchestrator =
                new MigrationSaveOrchestrator(null, null, null, null, null, null, null, null, null, source);
            MigrationSaveData data = captureOrchestrator.Capture(new MigrationSaveData());
            AssertEqual(2, data.home_storage.itemIds.Count, "Capture stores the home storage entries.");

            MigrationHomeStorage restored = new MigrationHomeStorage();
            AssertEqual(0, restored.GetStoredAmount("wood"), "A fresh storage box is empty.");

            MigrationSaveOrchestrator applyOrchestrator =
                new MigrationSaveOrchestrator(null, null, null, null, null, null, null, null, null, restored);
            applyOrchestrator.Apply(data);
            AssertEqual(12, restored.GetStoredAmount("wood"), "Apply restores stored item amounts.");
            AssertEqual(5, restored.GetStoredAmount("mushroom"), "Apply restores all stored items.");
        }

        private static void TestMetaProgressionRoundTripsThroughSaveData()
        {
            MigrationMetaProgression source = new MigrationMetaProgression();
            source.AddCurrency(250);
            source.RegisterUpgrade(new MigrationMetaUpgrade("hp_boost", 5, 100, 1.5, 10.0, "max_hp"));
            source.PurchaseUpgrade("hp_boost");

            MigrationSaveOrchestrator captureOrchestrator =
                new MigrationSaveOrchestrator(null, null, null, null, null, null, null, null, null, null, source);
            MigrationSaveData data = captureOrchestrator.Capture(new MigrationSaveData());
            AssertEqual(150, data.meta_progression.currency, "Capture stores remaining currency after the purchase.");
            AssertEqual(1, data.meta_progression.upgradeIds.Count, "Capture stores purchased upgrade levels.");

            MigrationMetaProgression restored = new MigrationMetaProgression();
            MigrationSaveOrchestrator applyOrchestrator =
                new MigrationSaveOrchestrator(null, null, null, null, null, null, null, null, null, null, restored);
            applyOrchestrator.Apply(data);
            AssertEqual(150, restored.Currency, "Apply restores the meta currency.");
            AssertEqual(1, restored.GetUpgradeLevel("hp_boost"), "Apply restores the purchased upgrade level.");
        }

        private static void TestNpcRelationshipRoundTripsThroughSaveData()
        {
            MigrationNpcRelationshipNetwork source = new MigrationNpcRelationshipNetwork();
            source.ModifyRelationship("reimu", "marisa", 30);
            source.SetNpcFaction("reimu", 2);
            source.ModifyFactionReputation(2, 10);

            MigrationSaveOrchestrator captureOrchestrator =
                new MigrationSaveOrchestrator(null, null, null, null, null, null, null, null, null, null, null, source);
            MigrationSaveData data = captureOrchestrator.Capture(new MigrationSaveData());

            MigrationNpcRelationshipNetwork restored = new MigrationNpcRelationshipNetwork();
            MigrationSaveOrchestrator applyOrchestrator =
                new MigrationSaveOrchestrator(null, null, null, null, null, null, null, null, null, null, null, restored);
            applyOrchestrator.Apply(data);

            AssertEqual(80, restored.GetRelationshipValue("reimu", "marisa"), "Apply restores modified relationship values.");
            AssertEqual(2, restored.GetNpcFaction("reimu"), "Apply restores faction membership.");
            AssertEqual(60, restored.GetFactionReputation(2), "Apply restores faction reputation.");
        }

        private static void TestBondStateRoundTripsThroughSaveData()
        {
            SocialBondService source = new SocialBondService();
            source.AddBondPoints("marisa", "gift", 50);
            int capturedPoints = source.GetBondPoints("marisa");
            AssertEqual(true, capturedPoints > 0, "Bond points should be added to the source service before capture.");

            MigrationSaveOrchestrator captureOrchestrator =
                new MigrationSaveOrchestrator(null, null, null, source, null, null);
            MigrationSaveData data = captureOrchestrator.Capture(new MigrationSaveData());
            AssertEqual(true, data.social_bonds != null, "Capture should fill the bond snapshot from the live service.");

            SocialBondService restored = new SocialBondService();
            AssertEqual(0, restored.GetBondPoints("marisa"), "A fresh bond service should start with no points for the npc.");

            MigrationSaveOrchestrator applyOrchestrator =
                new MigrationSaveOrchestrator(null, null, null, restored, null, null);
            applyOrchestrator.Apply(data);
            AssertEqual(capturedPoints, restored.GetBondPoints("marisa"), "Apply should restore captured bond points into the live service.");
        }

        private static void TestHumanityRoundTripsThroughSaveData()
        {
            HumanityService source = new HumanityService();
            source.Adjust(-30);
            int capturedHumanity = source.Humanity;
            AssertEqual(70, capturedHumanity, "Source humanity should reflect the applied delta before capture.");

            MigrationSaveOrchestrator captureOrchestrator =
                new MigrationSaveOrchestrator(null, null, null, null, null, source);
            MigrationSaveData data = captureOrchestrator.Capture(new MigrationSaveData());
            AssertEqual(capturedHumanity, data.Humanity, "Capture should store the live humanity value into the save record.");

            HumanityService restored = new HumanityService();
            AssertEqual(100, restored.Humanity, "A fresh humanity service should start at full humanity (100).");

            MigrationSaveOrchestrator applyOrchestrator =
                new MigrationSaveOrchestrator(null, null, null, null, null, restored);
            applyOrchestrator.Apply(data);
            AssertEqual(capturedHumanity, restored.Humanity, "Apply should restore captured humanity into the live service.");
        }

        private static void TestCaptureFillsProvidedServiceSnapshots()
        {
            SocialBondService bonds = new SocialBondService();
            QuestDeliveryService quests = new QuestDeliveryService();
            MigrationSaveOrchestrator orchestrator =
                new MigrationSaveOrchestrator(null, null, null, bonds, quests, null);

            MigrationSaveData data = orchestrator.Capture(new MigrationSaveData());
            AssertEqual(true, data.social_bonds != null, "Bond snapshot should be captured when the service is present.");
            AssertEqual(true, data.quests != null, "Quest snapshot should be captured when the service is present.");
        }

        private static void TestNullToleranceForMissingServicesAndData()
        {
            MigrationSaveOrchestrator empty = new MigrationSaveOrchestrator(null, null, null, null, null, null);
            MigrationSaveData data = empty.Capture(new MigrationSaveData());
            AssertEqual(true, data != null, "Capture should return a save record even with no services.");
            empty.Apply(data);
            empty.Apply(null);
            AssertEqual(true, true, "Apply should tolerate null services and null data without throwing.");
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
