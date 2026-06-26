using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Narrative;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCutsceneSequencer: timed playback of cutscene steps (Godot CutsceneSequencer play /
    // _run_step) — text steps show a line then wait, the sequence finishes after the last step.
    public static class CutsceneSequencerSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Cutscene Sequencer Smoke Tests")]
        public static void RunAll()
        {
            TestPlaysTimedSteps();
            TestEmptySequenceFinishesImmediately();
            Debug.Log("Cutscene sequencer smoke tests passed.");
        }

        private static List<MigrationCutsceneStep> Script()
        {
            return new List<MigrationCutsceneStep>
            {
                new MigrationCutsceneStep { Type = "text", Speaker = "Reimu", Text = "Hello.", Seconds = 1.5 },
                new MigrationCutsceneStep { Type = "wait", Seconds = 1.0 },
                new MigrationCutsceneStep { Type = "text", Speaker = "Marisa", Text = "Bye.", Seconds = 1.5 },
            };
        }

        private static void TestPlaysTimedSteps()
        {
            MigrationCutsceneSequencer seq = new MigrationCutsceneSequencer();
            seq.Play(Script());

            AssertEqual(true, seq.IsPlaying, "The sequence is playing after Play.");
            AssertEqual(1, seq.ShownLines.Count, "The first text line shows immediately.");
            AssertEqual("Reimu", seq.ShownLines[0].Speaker, "The first line is Reimu's.");

            seq.Tick(1.5); // advance past the first text step into the wait step
            AssertEqual(1, seq.CurrentStepIndex, "After 1.5s the wait step is current.");
            AssertEqual(1, seq.ShownLines.Count, "The wait step shows no new line.");

            seq.Tick(1.0); // advance into the second text step
            AssertEqual(2, seq.ShownLines.Count, "The second text step shows a line.");
            AssertEqual("Marisa", seq.ShownLines[1].Speaker, "The second line is Marisa's.");

            seq.Tick(1.5); // past the end
            AssertEqual(true, seq.IsFinished, "The sequence finishes after the last step.");
            AssertEqual(false, seq.IsPlaying, "A finished sequence is no longer playing.");

            seq.Tick(5.0); // safe no-op after finishing
            AssertEqual(2, seq.ShownLines.Count, "Ticking a finished sequence adds no lines.");
        }

        private static void TestEmptySequenceFinishesImmediately()
        {
            MigrationCutsceneSequencer seq = new MigrationCutsceneSequencer();
            seq.Play(new List<MigrationCutsceneStep>());
            AssertEqual(true, seq.IsFinished, "An empty cutscene finishes immediately.");
            AssertEqual(false, seq.IsPlaying, "An empty cutscene is not playing.");
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
