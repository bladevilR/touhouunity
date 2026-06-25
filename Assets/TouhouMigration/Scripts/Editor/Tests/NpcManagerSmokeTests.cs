using System;
using TouhouMigration.Runtime.Social;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class NpcManagerSmokeTests
    {
        private const string RosterPath = "Assets/TouhouMigration/Data/Npc/human_village_roster.json";

        [MenuItem("Touhou Migration/Tests/Run Npc Manager Smoke Tests")]
        public static void RunAll()
        {
            TestRegisteredNpcResolvesLocationByHour();
            TestUnknownNpcResolvesToEmpty();
            TestRegisterNpcsFromRoster();
            Debug.Log("Npc manager smoke tests passed.");
        }

        private static void TestRegisteredNpcResolvesLocationByHour()
        {
            MigrationNpcSchedule keineSchedule = new MigrationNpcSchedule();
            keineSchedule.AddEntry(new MigrationNpcScheduleEntry(8, 15, "school"));

            MigrationNpcManager manager = new MigrationNpcManager();
            manager.RegisterNpc("keine", keineSchedule, "human_village");

            AssertEqual(true, manager.IsRegistered("keine"), "Keine should be registered.");
            AssertEqual("school", manager.LocationOf("keine", 10), "During the 8-15 block Keine is at school.");
            AssertEqual("human_village", manager.LocationOf("keine", 20), "Outside the block Keine is at her home location.");
        }

        private static void TestUnknownNpcResolvesToEmpty()
        {
            MigrationNpcManager manager = new MigrationNpcManager();
            AssertEqual(false, manager.IsRegistered("nobody"), "An unregistered NPC is not registered.");
            AssertEqual(string.Empty, manager.LocationOf("nobody", 10), "An unregistered NPC resolves to no location.");
        }

        private static void TestRegisterNpcsFromRoster()
        {
            MigrationNpcRoster roster = new MigrationNpcRoster();
            AssertEqual(true, roster.LoadFromPath(RosterPath), "Roster should load for the NPC manager.");

            MigrationNpcManager manager = new MigrationNpcManager();
            manager.RegisterFrom(roster, 8, 18);

            AssertEqual(true, manager.IsRegistered("uuz"), "Spawn-enabled roster NPCs should be registered.");
            AssertEqual("plaza", manager.LocationOf("uuz", 10), "During work hours uuz is at the work location (plaza).");
            AssertEqual("residential", manager.LocationOf("uuz", 22), "Outside work hours uuz is at home (residential).");
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
