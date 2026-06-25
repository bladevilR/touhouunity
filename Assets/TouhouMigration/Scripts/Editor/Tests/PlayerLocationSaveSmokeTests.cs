using System;
using TouhouMigration.Runtime.Save;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // The caller-owned player position/scene save scalars (the last save-parity gap): writing the active
    // scene + player position into the save data, and the same-scene gate that decides whether a load can
    // restore the position in place.
    public static class PlayerLocationSaveSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Player Location Save Smoke Tests")]
        public static void RunAll()
        {
            TestWriteRecordsSceneAndPosition();
            TestBlankSceneKeepsExistingScene();
            TestSameSceneGate();
            Debug.Log("Player location save smoke tests passed.");
        }

        private static void TestWriteRecordsSceneAndPosition()
        {
            MigrationSaveData data = new MigrationSaveData();
            MigrationPlayerLocation.Write(data, "HumanVillage", 3.5f, 1f, -7.25f);

            AssertEqual("HumanVillage", data.CurrentScene, "Write should record the active scene name.");
            AssertEqual(3.5f, data.Position.x, "Write should record position x.");
            AssertEqual(1f, data.Position.y, "Write should record position y.");
            AssertEqual(-7.25f, data.Position.z, "Write should record position z.");
        }

        private static void TestBlankSceneKeepsExistingScene()
        {
            MigrationSaveData data = new MigrationSaveData();
            data.CurrentScene = "TownWorld";
            MigrationPlayerLocation.Write(data, "", 1f, 2f, 3f);

            AssertEqual("TownWorld", data.CurrentScene, "A blank scene name should not clobber the existing scene.");
            AssertEqual(2f, data.Position.y, "Position should still be written even when the scene name is blank.");
        }

        private static void TestSameSceneGate()
        {
            MigrationSaveData data = new MigrationSaveData();
            data.CurrentScene = "MistyLake";

            AssertEqual(true, MigrationPlayerLocation.IsSameScene(data, "MistyLake"), "An identical scene name should gate true.");
            AssertEqual(true, MigrationPlayerLocation.IsSameScene(data, "mistylake"), "The scene match should be case-insensitive.");
            AssertEqual(false, MigrationPlayerLocation.IsSameScene(data, "Farm"), "A different scene should gate false.");
            AssertEqual(false, MigrationPlayerLocation.IsSameScene(data, ""), "A blank active scene should gate false.");
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
