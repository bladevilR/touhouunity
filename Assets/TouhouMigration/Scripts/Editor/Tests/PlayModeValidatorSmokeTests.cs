using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class PlayModeValidatorSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Play Mode Validator Smoke Tests")]
        public static void RunAll()
        {
            TestFailureLogClassification();
            TestHasFailuresDetectsErrorsAndMissedPlay();
            TestReportContainsScenesAndErrors();
            TestAppendAndDeserializeRoundTrip();
            TestRuntimeFailureExcludesEditorOnlyNoise();
            Debug.Log("Play mode validator smoke tests passed.");
        }

        private static void TestFailureLogClassification()
        {
            AssertEqual(true, MigrationPlayModeReport.IsFailureLog(LogType.Error), "Error must count as failure.");
            AssertEqual(true, MigrationPlayModeReport.IsFailureLog(LogType.Exception), "Exception must count as failure.");
            AssertEqual(true, MigrationPlayModeReport.IsFailureLog(LogType.Assert), "Assert must count as failure.");
            AssertEqual(false, MigrationPlayModeReport.IsFailureLog(LogType.Log), "Log must not count as failure.");
            AssertEqual(false, MigrationPlayModeReport.IsFailureLog(LogType.Warning), "Warning must not count as failure.");
        }

        private static void TestHasFailuresDetectsErrorsAndMissedPlay()
        {
            var ok = new MigrationPlayModeReport.SceneValidationResult { sceneName = "A", enteredPlay = true, screenshotPath = "p.png" };
            var withError = new MigrationPlayModeReport.SceneValidationResult { sceneName = "B", enteredPlay = true, screenshotPath = "p.png" };
            withError.errors.Add("[Exception] boom");
            var missedPlay = new MigrationPlayModeReport.SceneValidationResult { sceneName = "C", enteredPlay = false, screenshotPath = "" };

            AssertEqual(false, MigrationPlayModeReport.HasFailures(new List<MigrationPlayModeReport.SceneValidationResult> { ok }), "Clean scene is not a failure.");
            AssertEqual(true, MigrationPlayModeReport.HasFailures(new List<MigrationPlayModeReport.SceneValidationResult> { ok, withError }), "An error makes the run a failure.");
            AssertEqual(true, MigrationPlayModeReport.HasFailures(new List<MigrationPlayModeReport.SceneValidationResult> { missedPlay }), "Failing to enter play is a failure.");
        }

        private static void TestReportContainsScenesAndErrors()
        {
            var r = new MigrationPlayModeReport.SceneValidationResult { sceneName = "Bootstrap", enteredPlay = true, screenshotPath = "Verification/VisualChecks/Bootstrap_PlayMode.png" };
            r.errors.Add("[Error] NullReference in Foo");
            string md = MigrationPlayModeReport.BuildReportMarkdown(new List<MigrationPlayModeReport.SceneValidationResult> { r });

            AssertEqual(true, md.Contains("Bootstrap"), "Report should list the scene name.");
            AssertEqual(true, md.Contains("Bootstrap_PlayMode.png"), "Report should list the screenshot path.");
            AssertEqual(true, md.Contains("NullReference in Foo"), "Report should include the error message.");
        }

        private static void TestAppendAndDeserializeRoundTrip()
        {
            string acc = MigrationPlayModeReport.EmptyResults();
            var r1 = new MigrationPlayModeReport.SceneValidationResult { sceneName = "S1", enteredPlay = true, screenshotPath = "s1.png" };
            var r2 = new MigrationPlayModeReport.SceneValidationResult { sceneName = "S2", enteredPlay = false, screenshotPath = "" };
            acc = MigrationPlayModeReport.AppendResult(acc, r1);
            acc = MigrationPlayModeReport.AppendResult(acc, r2);
            List<MigrationPlayModeReport.SceneValidationResult> back = MigrationPlayModeReport.Deserialize(acc);

            AssertEqual(2, back.Count, "Two results should round-trip.");
            AssertEqual("S1", back[0].sceneName, "First scene name should survive serialization.");
            AssertEqual(false, back[1].enteredPlay, "Second scene enteredPlay flag should survive serialization.");
        }

        private static void TestRuntimeFailureExcludesEditorOnlyNoise()
        {
            string quickSearchStack =
                "  at System.Collections.Generic.List`1[T].get_Item (System.Int32 index)\n"
                + "  at UnityEditor.Search.SearchDatabase+<EnumerateAll>d__75.MoveNext ()\n"
                + "  at UnityEditor.Search.SearchInit.IndexationOnStartup ()";
            string gameStack =
                "  at TouhouMigration.Runtime.Combat.MigrationCombatRuntime.Foo () (at Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatRuntime.cs:42)";
            string engineRuntimeStack = "  at UnityEngine.Material.GetColor (System.String name)";

            AssertEqual(false, MigrationPlayModeReport.IsRuntimeFailure(LogType.Exception, quickSearchStack), "Editor-only QuickSearch exceptions must not count as game runtime failures.");
            AssertEqual(true, MigrationPlayModeReport.IsRuntimeFailure(LogType.Exception, gameStack), "Exceptions from project code must count as runtime failures.");
            AssertEqual(true, MigrationPlayModeReport.IsRuntimeFailure(LogType.Error, engineRuntimeStack), "Engine runtime errors (non-editor) must count as runtime failures.");
            AssertEqual(false, MigrationPlayModeReport.IsRuntimeFailure(LogType.Warning, gameStack), "Warnings must not count as runtime failures.");
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
