using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Social;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationNpcRosterReconciler: mapping village-roster entries (whose ids are sometimes Chinese
    // display names) to canonical npc ids via the dialogue name->id map (exact, then substring), and
    // surfacing the genuinely-ambiguous remainder for author review rather than guessing.
    public static class NpcRosterReconcilerSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run NPC Roster Reconciler Smoke Tests")]
        public static void RunAll()
        {
            TestResolvesExactSubstringAndCanonical();
            TestSurfacesTrulyAmbiguous();
            TestCanonicalAliasesResolveNicknames();
            TestModelOnlySpawnsVersusBroken();
            Debug.Log("NPC roster reconciler smoke tests passed.");
        }

        private static MigrationNpcRosterEntry Entry(string id, string displayName)
        {
            return new MigrationNpcRosterEntry(id, displayName, string.Empty, true, "mid", 80f, string.Empty, string.Empty, string.Empty);
        }

        private static Dictionary<string, string> NameMap()
        {
            return new Dictionary<string, string>
            {
                ["八意永琳"] = "eirin",
                ["十六夜咲夜"] = "sakuya",
                ["琪露诺"] = "cirno",
            };
        }

        private static void TestResolvesExactSubstringAndCanonical()
        {
            List<MigrationNpcRosterEntry> entries = new List<MigrationNpcRosterEntry>
            {
                Entry("八意永琳", "八意永琳"),  // exact name match -> eirin
                Entry("咲夜", "咲夜"),          // substring of 十六夜咲夜 -> sakuya
                Entry("cirno", "琪露诺"),       // id is already canonical -> cirno
            };

            NpcRosterReconcileResult result = MigrationNpcRosterReconciler.Reconcile(entries, NameMap());

            AssertEqual("eirin", result.Matched["八意永琳"], "An exact display-name match resolves.");
            AssertEqual("sakuya", result.Matched["咲夜"], "A short-form name resolves by substring.");
            AssertEqual("cirno", result.Matched["cirno"], "An already-canonical id resolves to itself.");
            AssertEqual(0, result.Unmatched.Count, "Everything derivable is matched.");
        }

        private static void TestSurfacesTrulyAmbiguous()
        {
            List<MigrationNpcRosterEntry> entries = new List<MigrationNpcRosterEntry>
            {
                Entry("八意永琳", "八意永琳"), // -> eirin
                Entry("uuz", "uuz"),          // no canonical id, no name match -> ambiguous
                Entry("大狸子", "大狸子"),     // nickname, no match -> ambiguous
            };

            NpcRosterReconcileResult result = MigrationNpcRosterReconciler.Reconcile(entries, NameMap());

            AssertEqual(1, result.Matched.Count, "Only the derivable entry is matched.");
            AssertEqual(2, result.Unmatched.Count, "The two genuinely-ambiguous entries are surfaced, not guessed.");
            AssertEqual(true, result.Unmatched.Contains("uuz") && result.Unmatched.Contains("大狸子"),
                "The ambiguous ids are listed for author review.");
        }

        private static void TestCanonicalAliasesResolveNicknames()
        {
            // 夜雀 ("night sparrow") is not in the name map, but the canonical alias map maps it to mystia.
            List<MigrationNpcRosterEntry> entries = new List<MigrationNpcRosterEntry>
            {
                Entry("夜雀", "夜雀"),
                Entry("uuz", "uuz"), // still genuinely ambiguous
            };

            NpcRosterReconcileResult result = MigrationNpcRosterReconciler.Reconcile(
                entries, NameMap(), MigrationNpcRosterReconciler.CanonicalAliases);

            AssertEqual("mystia", result.Matched["夜雀"], "A canonical species nickname resolves via the alias map.");
            AssertEqual(true, result.Unmatched.Contains("uuz"), "Genuinely-unknown entries stay surfaced.");
        }

        private static void TestModelOnlySpawnsVersusBroken()
        {
            List<MigrationNpcRosterEntry> entries = new List<MigrationNpcRosterEntry>
            {
                // No dialogue NPC, but has a model -> intentional background spawn (like 橙/白莲/Flandre).
                new MigrationNpcRosterEntry("橙", "橙", "res://models/chen.glb", true, "mid", 80f, "", "", ""),
                // No dialogue NPC and no model -> genuinely broken, needs attention.
                new MigrationNpcRosterEntry("???", "???", "", true, "mid", 80f, "", "", ""),
            };

            NpcRosterReconcileResult result = MigrationNpcRosterReconciler.Reconcile(entries, NameMap());

            AssertEqual(true, result.ModelOnlySpawns.Contains("橙"),
                "A dialogue-less entry with a model is a model-only background spawn, not an error.");
            AssertEqual(false, result.Unmatched.Contains("橙"), "It is not flagged as broken.");
            AssertEqual(true, result.Unmatched.Contains("???"),
                "An entry with neither dialogue nor model is genuinely unmatched.");
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
