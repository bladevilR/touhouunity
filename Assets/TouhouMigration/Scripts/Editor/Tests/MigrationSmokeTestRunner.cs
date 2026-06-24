using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // One-command regression: discovers every *SmokeTests class with a public static RunAll()
    // and invokes them in a single editor session, so a full regression is one Unity launch
    // instead of ~35. Continues past a failing suite and throws a summary at the end, so a
    // batch run (-executeMethod ... -quit) exits non-zero iff any suite failed.
    public static class MigrationSmokeTestRunner
    {
        [MenuItem("Touhou Migration/Tests/Run ALL Smoke Tests")]
        public static void RunAll()
        {
            List<MethodInfo> runners = DiscoverRunners();
            Debug.Log($"[MigrationSmokeTestRunner] Discovered {runners.Count} smoke-test suites.");

            int passed = 0;
            List<string> failures = new List<string>();

            foreach (MethodInfo runner in runners)
            {
                string suite = runner.DeclaringType != null ? runner.DeclaringType.Name : "<unknown>";
                try
                {
                    runner.Invoke(null, Array.Empty<object>());
                    passed++;
                    Debug.Log($"[MigrationSmokeTestRunner] PASS {suite}");
                }
                catch (Exception ex)
                {
                    Exception inner = ex is TargetInvocationException tie && tie.InnerException != null
                        ? tie.InnerException
                        : ex;
                    failures.Add($"{suite}: {inner.Message}");
                    Debug.LogError($"[MigrationSmokeTestRunner] FAIL {suite}: {inner.Message}");
                }
            }

            Debug.Log($"[MigrationSmokeTestRunner] Total {runners.Count}: {passed} passed, {failures.Count} failed.");
            if (failures.Count > 0)
            {
                throw new Exception(
                    $"MigrationSmokeTestRunner: {failures.Count} suite(s) failed:\n  " +
                    string.Join("\n  ", failures));
            }

            Debug.Log("All migration smoke tests passed.");
        }

        private static List<MethodInfo> DiscoverRunners()
        {
            List<MethodInfo> runners = new List<MethodInfo>();
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.Namespace != "TouhouMigration.Editor.Tests")
                {
                    continue;
                }
                if (!type.Name.EndsWith("SmokeTests", StringComparison.Ordinal))
                {
                    continue;
                }
                MethodInfo runAll = type.GetMethod(
                    "RunAll",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    Type.EmptyTypes,
                    null);
                if (runAll != null)
                {
                    runners.Add(runAll);
                }
            }
            return runners.OrderBy(m => m.DeclaringType.Name, StringComparer.Ordinal).ToList();
        }
    }
}
