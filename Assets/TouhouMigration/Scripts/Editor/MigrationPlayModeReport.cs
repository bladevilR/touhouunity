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
