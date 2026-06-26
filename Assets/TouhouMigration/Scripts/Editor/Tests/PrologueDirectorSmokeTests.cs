using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Narrative;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationPrologueDirector: the prologue beat-progression state machine (Godot
    // PrologueDirector start / advance) — gameplay beats wait, cutscene beats auto-advance, complete at end.
    public static class PrologueDirectorSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Prologue Director Smoke Tests")]
        public static void RunAll()
        {
            TestGameplayBeatsAdvanceManually();
            TestCutsceneBeatsAutoSkip();
            TestTrailingCutsceneCompletes();
            TestEmptyAndCompleteAreSafe();
            Debug.Log("Prologue director smoke tests passed.");
        }

        private static MigrationPrologueBeat Beat(string id, string kind)
        {
            return new MigrationPrologueBeat { Id = id, Kind = kind };
        }

        private static void TestGameplayBeatsAdvanceManually()
        {
            MigrationPrologueDirector dir = new MigrationPrologueDirector();
            dir.Start(new List<MigrationPrologueBeat> { Beat("g0", "gameplay"), Beat("g1", "gameplay") });

            AssertEqual(true, dir.IsActive, "The prologue is active after starting.");
            AssertEqual("g0", dir.CurrentBeat.Id, "It begins on the first gameplay beat.");

            dir.Advance();
            AssertEqual("g1", dir.CurrentBeat.Id, "Advance moves to the next gameplay beat.");

            dir.Advance();
            AssertEqual(true, dir.IsComplete, "Advancing past the last beat completes the prologue.");
            AssertEqual(false, dir.IsActive, "A complete prologue is inactive.");
        }

        private static void TestCutsceneBeatsAutoSkip()
        {
            MigrationPrologueDirector dir = new MigrationPrologueDirector();
            dir.Start(new List<MigrationPrologueBeat>
            {
                Beat("intro_cutscene", "cutscene"),
                Beat("g1", "gameplay"),
                Beat("mid_cutscene", "cutscene"),
                Beat("g3", "gameplay"),
            });

            AssertEqual("g1", dir.CurrentBeat.Id, "A leading cutscene auto-advances to the first gameplay beat.");

            dir.Advance();
            AssertEqual("g3", dir.CurrentBeat.Id, "Advancing skips the mid cutscene to the next gameplay beat.");
        }

        private static void TestTrailingCutsceneCompletes()
        {
            MigrationPrologueDirector dir = new MigrationPrologueDirector();
            dir.Start(new List<MigrationPrologueBeat> { Beat("g0", "gameplay"), Beat("outro", "cutscene") });

            dir.Advance(); // into the trailing cutscene -> auto-advances past the end -> complete
            AssertEqual(true, dir.IsComplete, "A trailing cutscene auto-completes the prologue.");
        }

        private static void TestEmptyAndCompleteAreSafe()
        {
            MigrationPrologueDirector empty = new MigrationPrologueDirector();
            empty.Start(new List<MigrationPrologueBeat>());
            AssertEqual(false, empty.IsActive, "An empty beat table does not activate.");

            MigrationPrologueDirector done = new MigrationPrologueDirector();
            done.Start(new List<MigrationPrologueBeat> { Beat("g0", "gameplay") });
            done.Advance(); // completes
            done.Advance(); // no-op
            AssertEqual(true, done.IsComplete, "Advancing a completed prologue is a safe no-op.");
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
