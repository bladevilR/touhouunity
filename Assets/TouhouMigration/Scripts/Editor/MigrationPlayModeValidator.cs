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
        private const string RunStartKey = "TouhouMigration.PlayModeValidator.RunStart";

        private const float PlaySecondsPerScene = 3f;
        private const float MaxRunSeconds = 480f;
        private const int CaptureWidth = 1280;
        private const int CaptureHeight = 720;
        private const string OutputDir = "Verification/VisualChecks";
        private const string ReportPath = "Verification/VisualChecks/PlayModeValidationReport.md";

        private static readonly string[] ScenePaths =
        {
            "Assets/TouhouMigration/Scenes/Bootstrap.unity",
            "Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity",
            "Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity",
            "Assets/TouhouMigration/Scenes/PureNatureMeadows.unity",
            "Assets/TouhouMigration/Scenes/PureNatureClassic.unity",
            "Assets/TouhouMigration/Scenes/PureNatureJungle.unity",
            "Assets/TouhouMigration/Scenes/PureNatureIslands.unity",
            "Assets/TouhouMigration/Scenes/PureNatureMountains.unity",
            "Assets/TouhouMigration/Scenes/PureNatureFantasyForest.unity",
            "Assets/TouhouMigration/Scenes/AngryMeshMeadow.unity",
            "Assets/TouhouMigration/Scenes/MagicForest.unity",
            "Assets/TouhouMigration/Scenes/MistyLake.unity",
            "Assets/TouhouMigration/Scenes/TownWorld.unity",
            "Assets/TouhouMigration/Scenes/FantasyVillage.unity",
            "Assets/TouhouMigration/Scenes/SuntailVillagePlayable.unity",
            "Assets/TouhouMigration/Scenes/SuntailVillageImported.unity",
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
            SessionState.SetFloat(RunStartKey, (float)EditorApplication.timeSinceStartup);
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
            if (!MigrationPlayModeReport.IsRuntimeFailure(type, stackTrace))
            {
                return;
            }
            string firstStackLine = string.IsNullOrEmpty(stackTrace) ? "" : stackTrace.Split('\n')[0];
            _sceneErrors.Add($"[{type}] {condition} | {firstStackLine}");
        }

        private static void Pump()
        {
            // Watchdog: never let a stuck play-mode transition hang the batch forever.
            float runStart = SessionState.GetFloat(RunStartKey, -1f);
            if (runStart >= 0f && EditorApplication.timeSinceStartup - runStart > MaxRunSeconds)
            {
                AbortAsTimedOut();
                return;
            }

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

        private static void AbortAsTimedOut()
        {
            int cursor = SessionState.GetInt(CursorKey, 0);
            string sceneName = cursor < ScenePaths.Length
                ? Path.GetFileNameWithoutExtension(ScenePaths[cursor])
                : "unknown";

            var result = new MigrationPlayModeReport.SceneValidationResult
            {
                sceneName = sceneName,
                enteredPlay = false,
                screenshotPath = "<timed out>",
            };
            result.errors.Add($"Play-mode validation timed out after {MaxRunSeconds}s while processing '{sceneName}'.");

            string acc = SessionState.GetString(ResultsKey, MigrationPlayModeReport.EmptyResults());
            acc = MigrationPlayModeReport.AppendResult(acc, result);
            SessionState.SetString(ResultsKey, acc);

            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }
            Finish();
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
