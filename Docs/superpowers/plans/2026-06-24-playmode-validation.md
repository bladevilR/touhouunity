# Play-Mode Validation & Capture (Cycle A) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an editor batch tool that runs four scenes in real Play mode, captures a live screenshot of each, asserts zero runtime errors, and writes a pass/fail report with a non-zero exit on failure.

**Architecture:** Split into pure logic (`MigrationPlayModeReport` — classification, serialization, report formatting; unit-testable) and editor orchestration (`MigrationPlayModeValidator` — a `SessionState`-backed state machine that survives the domain reloads from entering/exiting Play mode; verified by actually running it). Follows the project's existing bespoke smoke-test pattern (static class + `RunAll()` + per-file `AssertEqual` + throw-on-fail), not Unity Test Runner (that is Cycle B).

**Tech Stack:** Unity 6000.5.0f1, C# editor scripting (`EditorApplication`, `EditorSceneManager`, `SessionState`, `JsonUtility`), macOS batchmode.

> **Version control note:** This project is **not** a git repository. There are no `git commit` steps. Each task ends with a **Checkpoint** (verify state on disk) instead. If you want version control, ask the user before `git init`.

> **TDD note:** The Play-mode state machine cannot be unit-tested (it requires a live editor + domain reloads). TDD is applied to the pure logic in Task 1. Task 2's orchestration is verified by an **acceptance run** of the real batch tool with concrete expected outputs. This is the correct testing altitude for editor/play-mode tooling.

> **Scratchpad log dir:** `/private/tmp/claude-501/-Users-Shared-TouhouUnityMigration/661073c8-4552-400c-b339-1e2f0ffc9275/scratchpad`

> **Unity binary:** `/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity`

---

## File Structure

- Create `Assets/TouhouMigration/Scripts/Editor/MigrationPlayModeReport.cs` — pure logic + the `[Serializable]` result type. No `EditorApplication`/play-mode calls, so it is directly testable in batch.
- Create `Assets/TouhouMigration/Scripts/Editor/Tests/PlayModeValidatorSmokeTests.cs` — bespoke smoke tests for the pure logic, matching the existing test pattern.
- Create `Assets/TouhouMigration/Scripts/Editor/MigrationPlayModeValidator.cs` — the editor orchestration (scene queue, reload-safe state machine, capture, log sink, report, exit code).
- Modify `Docs/PROJECT_PROGRESS.md` — record the milestone per project discipline (rule 5/6).

---

## Task 1: Pure report logic (`MigrationPlayModeReport`)

**Files:**
- Create: `Assets/TouhouMigration/Scripts/Editor/Tests/PlayModeValidatorSmokeTests.cs`
- Create: `Assets/TouhouMigration/Scripts/Editor/MigrationPlayModeReport.cs`

- [ ] **Step 1: Write the failing smoke test**

Create `Assets/TouhouMigration/Scripts/Editor/Tests/PlayModeValidatorSmokeTests.cs`:

```csharp
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

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run:
```bash
"/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration \
  -executeMethod TouhouMigration.Editor.Tests.PlayModeValidatorSmokeTests.RunAll \
  -logFile /private/tmp/claude-501/-Users-Shared-TouhouUnityMigration/661073c8-4552-400c-b339-1e2f0ffc9275/scratchpad/pmv_test.log
echo "exit=$?"
```
Expected: **non-zero exit**; the log contains C# compile errors referencing `MigrationPlayModeReport` (type does not exist yet). This proves the test exercises the missing type.

- [ ] **Step 3: Write the minimal implementation**

Create `Assets/TouhouMigration/Scripts/Editor/MigrationPlayModeReport.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TouhouMigration.Editor
{
    // Pure, reload-safe data + formatting for the play-mode validator.
    // Deliberately free of EditorApplication/play-mode calls so it is unit-testable in batch.
    public static class MigrationPlayModeReport
    {
        [Serializable]
        public class SceneValidationResult
        {
            public string sceneName;
            public bool enteredPlay;
            public string screenshotPath;
            public List<string> errors = new List<string>();
        }

        [Serializable]
        private class ResultSet
        {
            public List<SceneValidationResult> results = new List<SceneValidationResult>();
        }

        public static bool IsFailureLog(LogType type)
        {
            return type == LogType.Error || type == LogType.Exception || type == LogType.Assert;
        }

        public static string EmptyResults()
        {
            return JsonUtility.ToJson(new ResultSet());
        }

        public static string AppendResult(string serialized, SceneValidationResult result)
        {
            ResultSet set = string.IsNullOrEmpty(serialized)
                ? new ResultSet()
                : JsonUtility.FromJson<ResultSet>(serialized);
            if (set == null)
            {
                set = new ResultSet();
            }
            set.results.Add(result);
            return JsonUtility.ToJson(set);
        }

        public static List<SceneValidationResult> Deserialize(string serialized)
        {
            if (string.IsNullOrEmpty(serialized))
            {
                return new List<SceneValidationResult>();
            }
            ResultSet set = JsonUtility.FromJson<ResultSet>(serialized);
            return set != null && set.results != null ? set.results : new List<SceneValidationResult>();
        }

        public static bool HasFailures(IEnumerable<SceneValidationResult> results)
        {
            foreach (SceneValidationResult r in results)
            {
                if (!r.enteredPlay)
                {
                    return true;
                }
                if (r.errors != null && r.errors.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static string BuildReportMarkdown(IEnumerable<SceneValidationResult> results)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Play-Mode Validation Report");
            sb.AppendLine();
            sb.AppendLine("| Scene | Entered Play | Screenshot | Errors |");
            sb.AppendLine("|---|---|---|---|");
            foreach (SceneValidationResult r in results)
            {
                int errorCount = r.errors != null ? r.errors.Count : 0;
                sb.AppendLine($"| {r.sceneName} | {(r.enteredPlay ? "yes" : "NO")} | {r.screenshotPath} | {errorCount} |");
            }
            sb.AppendLine();
            foreach (SceneValidationResult r in results)
            {
                if (r.errors != null && r.errors.Count > 0)
                {
                    sb.AppendLine($"## Errors in {r.sceneName}");
                    foreach (string e in r.errors)
                    {
                        sb.AppendLine($"- {e}");
                    }
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run:
```bash
"/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration \
  -executeMethod TouhouMigration.Editor.Tests.PlayModeValidatorSmokeTests.RunAll \
  -logFile /private/tmp/claude-501/-Users-Shared-TouhouUnityMigration/661073c8-4552-400c-b339-1e2f0ffc9275/scratchpad/pmv_test.log
echo "exit=$?"
grep -c "Play mode validator smoke tests passed." /private/tmp/claude-501/-Users-Shared-TouhouUnityMigration/661073c8-4552-400c-b339-1e2f0ffc9275/scratchpad/pmv_test.log
```
Expected: **exit=0** and the grep prints `1` (the pass line is present), with no `error CS` in the log.

- [ ] **Step 5: Checkpoint**

Confirm both files exist and the smoke test passed:
```bash
ls Assets/TouhouMigration/Scripts/Editor/MigrationPlayModeReport.cs \
   Assets/TouhouMigration/Scripts/Editor/Tests/PlayModeValidatorSmokeTests.cs
```
Expected: both paths listed. Pure logic is now proven green.

---

## Task 2: Orchestration (`MigrationPlayModeValidator`)

**Files:**
- Create: `Assets/TouhouMigration/Scripts/Editor/MigrationPlayModeValidator.cs`

- [ ] **Step 1: Implement the validator**

Create `Assets/TouhouMigration/Scripts/Editor/MigrationPlayModeValidator.cs`:

```csharp
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TouhouMigration.Editor
{
    // Drives real Play mode across several scenes in batch, captures a live screenshot of each,
    // collects runtime errors, writes a report, and exits non-zero on failure.
    //
    // Batch entry (NOTE: no -quit, no -nographics):
    //   Unity -batchmode -projectPath <proj> \
    //     -executeMethod TouhouMigration.Editor.MigrationPlayModeValidator.ValidateAndCapture
    [InitializeOnLoad]
    public static class MigrationPlayModeValidator
    {
        private const string ActiveKey = "TouhouMigration.PlayModeValidator.Active";
        private const string CursorKey = "TouhouMigration.PlayModeValidator.Cursor";
        private const string ResultsKey = "TouhouMigration.PlayModeValidator.Results";
        private const string AwaitingExitKey = "TouhouMigration.PlayModeValidator.AwaitingExit";
        private const string ExitOnFinishKey = "TouhouMigration.PlayModeValidator.ExitOnFinish";

        private const float PlaySecondsPerScene = 3f;
        private const int CaptureWidth = 1280;
        private const int CaptureHeight = 720;
        private const string OutputDir = "Verification/VisualChecks";
        private const string ReportPath = "Verification/VisualChecks/PlayModeValidationReport.md";

        private static readonly string[] ScenePaths =
        {
            "Assets/TouhouMigration/Scenes/Bootstrap.unity",
            "Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity",
            "Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity",
            "Assets/TouhouMigration/Scenes/TitleScreen.unity",
        };

        // Static fields reset on every domain reload; cross-reload state lives in SessionState.
        private static double _sceneStartTime;
        private static bool _captured;
        private static readonly List<string> _sceneErrors = new List<string>();

        static MigrationPlayModeValidator()
        {
            // Runs on every domain load, including the reloads from entering/exiting play mode.
            if (SessionState.GetBool(ActiveKey, false))
            {
                Application.logMessageReceived -= OnLogMessage;
                Application.logMessageReceived += OnLogMessage;
                EditorApplication.update -= Pump;
                EditorApplication.update += Pump;
            }
        }

        // Batch entry point: quits the editor with a pass/fail exit code when done.
        public static void ValidateAndCapture()
        {
            StartRun(exitOnFinish: true);
        }

        // Interactive entry point: keeps the editor open (does NOT quit) so a human can use it safely.
        [MenuItem("Touhou Migration/Validate/Play Mode Capture (keeps editor open)")]
        public static void ValidateAndCaptureInteractive()
        {
            StartRun(exitOnFinish: false);
        }

        private static void StartRun(bool exitOnFinish)
        {
            Directory.CreateDirectory(OutputDir);
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(ExitOnFinishKey, exitOnFinish);
            SessionState.SetBool(AwaitingExitKey, false);
            SessionState.SetInt(CursorKey, 0);
            SessionState.SetString(ResultsKey, MigrationPlayModeReport.EmptyResults());
            BeginScene(0);
        }

        private static void BeginScene(int index)
        {
            _captured = false;
            _sceneStartTime = 0.0;
            _sceneErrors.Clear();

            EditorSceneManager.OpenScene(ScenePaths[index]);

            // Subscribe before the first scene's reload; the static ctor handles later reloads.
            Application.logMessageReceived -= OnLogMessage;
            Application.logMessageReceived += OnLogMessage;
            EditorApplication.update -= Pump;
            EditorApplication.update += Pump;

            EditorApplication.EnterPlaymode();
        }

        private static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (!MigrationPlayModeReport.IsFailureLog(type))
            {
                return;
            }
            string firstStackLine = string.IsNullOrEmpty(stackTrace) ? "" : stackTrace.Split('\n')[0];
            _sceneErrors.Add($"[{type}] {condition} | {firstStackLine}");
        }

        private static void Pump()
        {
            if (EditorApplication.isPlaying)
            {
                // Mark the start time lazily on the first in-play tick after the enter-play reload.
                if (_sceneStartTime <= 0.0)
                {
                    _sceneStartTime = EditorApplication.timeSinceStartup;
                    return;
                }
                if (!_captured && EditorApplication.timeSinceStartup - _sceneStartTime >= PlaySecondsPerScene)
                {
                    int cursor = SessionState.GetInt(CursorKey, 0);
                    CaptureCurrentScene(cursor);
                    _captured = true;
                    SessionState.SetBool(AwaitingExitKey, true);
                    EditorApplication.ExitPlaymode();
                }
                return;
            }

            // Not playing: advance only after we have captured and requested exit.
            if (SessionState.GetBool(AwaitingExitKey, false))
            {
                SessionState.SetBool(AwaitingExitKey, false);
                int cursor = SessionState.GetInt(CursorKey, 0);
                int next = cursor + 1;
                SessionState.SetInt(CursorKey, next);
                if (next < ScenePaths.Length)
                {
                    BeginScene(next);
                }
                else
                {
                    Finish();
                }
            }
        }

        private static void CaptureCurrentScene(int cursor)
        {
            string sceneName = Path.GetFileNameWithoutExtension(ScenePaths[cursor]);
            string outputPath = $"{OutputDir}/{sceneName}_PlayMode.png";

            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = Object.FindObjectOfType<Camera>();
            }

            var result = new MigrationPlayModeReport.SceneValidationResult
            {
                sceneName = sceneName,
                enteredPlay = true,
                screenshotPath = outputPath,
                errors = new List<string>(_sceneErrors),
            };

            if (camera == null)
            {
                result.screenshotPath = "<no camera>";
            }
            else
            {
                RenderTexture target = new RenderTexture(CaptureWidth, CaptureHeight, 24);
                RenderTexture previousActive = RenderTexture.active;
                RenderTexture previousTarget = camera.targetTexture;
                try
                {
                    camera.targetTexture = target;
                    RenderTexture.active = target;
                    camera.Render();

                    Texture2D shot = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
                    shot.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
                    shot.Apply();
                    File.WriteAllBytes(outputPath, shot.EncodeToPNG());
                    Object.DestroyImmediate(shot);
                }
                finally
                {
                    camera.targetTexture = previousTarget;
                    RenderTexture.active = previousActive;
                    target.Release();
                    Object.DestroyImmediate(target);
                }
            }

            string acc = SessionState.GetString(ResultsKey, MigrationPlayModeReport.EmptyResults());
            acc = MigrationPlayModeReport.AppendResult(acc, result);
            SessionState.SetString(ResultsKey, acc);
        }

        private static void Finish()
        {
            string acc = SessionState.GetString(ResultsKey, MigrationPlayModeReport.EmptyResults());
            List<MigrationPlayModeReport.SceneValidationResult> results = MigrationPlayModeReport.Deserialize(acc);
            File.WriteAllText(ReportPath, MigrationPlayModeReport.BuildReportMarkdown(results));
            AssetDatabase.Refresh();

            bool failed = MigrationPlayModeReport.HasFailures(results);
            Debug.Log($"Play-mode validation complete. Scenes: {results.Count}. Failed: {failed}. Report: {ReportPath}");

            SessionState.SetBool(ActiveKey, false);
            Application.logMessageReceived -= OnLogMessage;
            EditorApplication.update -= Pump;

            if (SessionState.GetBool(ExitOnFinishKey, true))
            {
                EditorApplication.Exit(failed ? 1 : 0);
            }
        }
    }
}
```

- [ ] **Step 2: Verify it compiles (smoke test still green)**

The orchestration shares the assembly with the smoke test, so a clean compile is confirmed by re-running Task 1's test:
```bash
"/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration \
  -executeMethod TouhouMigration.Editor.Tests.PlayModeValidatorSmokeTests.RunAll \
  -logFile /private/tmp/claude-501/-Users-Shared-TouhouUnityMigration/661073c8-4552-400c-b339-1e2f0ffc9275/scratchpad/pmv_test2.log
echo "exit=$?"
grep -c "error CS" /private/tmp/claude-501/-Users-Shared-TouhouUnityMigration/661073c8-4552-400c-b339-1e2f0ffc9275/scratchpad/pmv_test2.log
```
Expected: **exit=0**, grep prints `0` (no compile errors).

- [ ] **Step 3: Checkpoint**

```bash
ls Assets/TouhouMigration/Scripts/Editor/MigrationPlayModeValidator.cs
```
Expected: path listed. Orchestration compiles cleanly alongside the green pure-logic tests.

---

## Task 3: Acceptance run + record milestone

**Files:**
- Produces: `Verification/VisualChecks/{Bootstrap,BambooHomeVerticalSlice,HumanVillageVerticalSlice,TitleScreen}_PlayMode.png` and `Verification/VisualChecks/PlayModeValidationReport.md`
- Modify: `Docs/PROJECT_PROGRESS.md`

- [ ] **Step 1: Run the full play-mode validator (acceptance)**

NOTE: no `-quit` (the tool controls its own exit), no `-nographics` (rendering required). This run launches Play mode four times with domain reloads and may take several minutes; allow a generous timeout.
```bash
"/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath /Users/Shared/TouhouUnityMigration \
  -executeMethod TouhouMigration.Editor.MigrationPlayModeValidator.ValidateAndCapture \
  -logFile /private/tmp/claude-501/-Users-Shared-TouhouUnityMigration/661073c8-4552-400c-b339-1e2f0ffc9275/scratchpad/pmv_run.log
echo "exit=$?"
```
Expected: process exits on its own (no hang). Exit code `0` if every scene entered Play and logged no Error/Exception/Assert; `1` if any scene failed (this is a real finding, not a plan failure).

- [ ] **Step 2: Inspect the produced artifacts**

```bash
ls -la Verification/VisualChecks/*_PlayMode.png
cat Verification/VisualChecks/PlayModeValidationReport.md
```
Expected: four non-zero-byte PNGs (or fewer with a `<no camera>` note in the report), and a report table with one row per scene. Read the report: any `Errors in <scene>` section is a genuine runtime issue to triage (see Step 3).

- [ ] **Step 3: Triage (only if the run found errors)**

If `exit=1` or the report lists errors, do NOT silently "fix" the validator. Inspect `pmv_run.log` around the error, decide whether it is (a) a real runtime bug in a migrated scene/script — record it as a finding for follow-up, or (b) a validator artifact (e.g. capture timing). Use the systematic-debugging skill before changing code. A `1` exit that reflects a real broken scene is a **successful** validation outcome — it found what batch builds could not.

- [ ] **Step 4: Record the milestone in PROJECT_PROGRESS.md**

Append a milestone entry to `Docs/PROJECT_PROGRESS.md` following the existing "Handoff Template" shape (Milestone / Status / Date / Changed files / Verification run / Verification result / Known blockers / Source assets imported / Next recommended step). Include: the two new editor scripts + one test file, the exact acceptance command from Step 1, the report's pass/fail outcome and per-scene results, and "Next recommended step: Cycle B — assembly definitions + migrate smoke tests to Unity Test Runner."

- [ ] **Step 5: Checkpoint**

```bash
ls Verification/VisualChecks/PlayModeValidationReport.md
grep -c "Play Mode Capture\|PlayMode" Docs/PROJECT_PROGRESS.md
```
Expected: report exists; progress doc mentions the new milestone. Cycle A done.

---

## Self-Review

**1. Spec coverage** (checked against `2026-06-24-playmode-validation-design.md`):
- Four target scenes (Bootstrap/BambooHome/HumanVillage/Title) → `ScenePaths` in Task 2. ✓
- Real Play mode for a configurable duration (default 3s) → `PlaySecondsPerScene` + lazy start-time + pump. ✓
- Live screenshot per scene → `CaptureCurrentScene`. ✓
- Zero Error/Exception/Assert assertion → `IsFailureLog` + `OnLogMessage` + `HasFailures`. ✓ (TDD'd in Task 1.)
- Report written → `BuildReportMarkdown` + `Finish`. ✓
- Non-zero exit on failure → `EditorApplication.Exit(failed ? 1 : 0)`. ✓
- Entry-flow special case (Bootstrap transitions, capture landing) → captures `Camera.main` after 3s of play, which follows any transition. ✓
- No-camera fallback (don't throw) → `<no camera>` branch. ✓
- SessionState across domain reload identified as the main risk → handled by persisted cursor/results/awaiting-exit + `[InitializeOnLoad]` resubscribe. ✓
- Run without `-nographics`/`-quit` → stated in Task 3 Step 1 and the validator header comment. ✓
- Independent of Cycle B (no Test Runner) → uses the bespoke smoke pattern. ✓

**2. Placeholder scan:** No TBD/TODO/"handle edge cases"/"similar to". All code blocks are complete and compilable; all commands are concrete with expected output. ✓

**3. Type consistency:** `MigrationPlayModeReport.SceneValidationResult` (fields `sceneName`, `enteredPlay`, `screenshotPath`, `errors`) is used identically in the test (Task 1), `CaptureCurrentScene`, and `Finish` (Task 2). Method names `IsFailureLog`, `EmptyResults`, `AppendResult`, `Deserialize`, `HasFailures`, `BuildReportMarkdown` match between definition (Task 1 Step 3), test (Task 1 Step 1), and caller (Task 2). ✓

**Known residual risk (documented, not a blocker):** the `SessionState` state machine is the fragile part. If batch Play-mode proves flaky, the fallback is to temporarily set `EditorApplication.ExecuteMenuItem`-style `EnterPlayModeOptions.DisableDomainReload` for the run and restore it in `Finish` — a single continuous pump with no reloads. Try the spec'd approach first.
