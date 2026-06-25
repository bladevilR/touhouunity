using System;
using TouhouMigration.Runtime.Social;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class NpcRosterSmokeTests
    {
        private const string RosterPath = "Assets/TouhouMigration/Data/Npc/human_village_roster.json";

        [MenuItem("Touhou Migration/Tests/Run Npc Roster Smoke Tests")]
        public static void RunAll()
        {
            TestLoadsRoster();
            Debug.Log("Npc roster smoke tests passed.");
        }

        private static void TestLoadsRoster()
        {
            MigrationNpcRoster roster = new MigrationNpcRoster();
            bool loaded = roster.LoadFromPath(RosterPath);
            AssertEqual(true, loaded, "Roster should load. Errors: " + string.Join("; ", roster.Errors));
            AssertEqual(26, roster.Count, "The human village roster lists 26 NPCs.");

            MigrationNpcRosterEntry uuz = roster.GetEntry("uuz");
            AssertEqual(true, uuz != null, "uuz should be in the roster.");
            AssertEqual("uuz", uuz.NpcId, "Entry preserves the npc id.");
            AssertEqual(true, uuz.SpawnEnabled, "uuz is spawn-enabled.");
            AssertEqual("mid", uuz.Tier, "uuz tier is mid.");
            AssertEqual("residential", uuz.Home, "uuz home is residential.");
            AssertEqual("plaza", uuz.WorkLocation, "uuz works at the plaza.");
            AssertEqual(true, roster.GetEntry("nonexistent") == null, "An unknown npc returns null.");
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
