using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Social;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationNpcCrossDialogue: the ambient NPC-to-NPC dialogue trigger engine (Godot
    // NPCCrossDialogueSystem) — order-independent pair/triple keys, the 5-minute cooldown, and the
    // triple-before-dual priority.
    public static class NpcCrossDialogueSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Npc Cross Dialogue Smoke Tests")]
        public static void RunAll()
        {
            TestDualTriggerAndCooldown();
            TestPairKeyIsOrderIndependent();
            TestTripleTakesPriority();
            TestNotEnoughNpcs();
            Debug.Log("Npc cross dialogue smoke tests passed.");
        }

        private static void TestDualTriggerAndCooldown()
        {
            MigrationNpcCrossDialogue cross = new MigrationNpcCrossDialogue();
            cross.RegisterDual("reimu", "marisa", "rm_banter");

            string fired = cross.CheckAndTrigger(new[] { "reimu", "marisa" });
            AssertEqual("rm_banter", fired, "A present registered pair triggers its dialogue.");
            AssertEqual(true, cross.IsOnCooldown("rm_banter"), "Triggering starts the dialogue cooldown.");

            AssertEqual(null, cross.CheckAndTrigger(new[] { "reimu", "marisa" }), "A dialogue on cooldown does not retrigger.");

            cross.TickCooldowns(MigrationNpcCrossDialogue.DialogueCooldownSeconds);
            AssertEqual(false, cross.IsOnCooldown("rm_banter"), "The cooldown lapses after 5 minutes.");
            AssertEqual("rm_banter", cross.CheckAndTrigger(new[] { "reimu", "marisa" }), "After the cooldown it can fire again.");
        }

        private static void TestPairKeyIsOrderIndependent()
        {
            MigrationNpcCrossDialogue cross = new MigrationNpcCrossDialogue();
            cross.RegisterDual("marisa", "reimu", "rm_banter");
            // Registered (marisa, reimu); present in the other order.
            AssertEqual("rm_banter", cross.CheckAndTrigger(new[] { "reimu", "marisa" }),
                "Pair matching is order-independent.");
        }

        private static void TestTripleTakesPriority()
        {
            MigrationNpcCrossDialogue cross = new MigrationNpcCrossDialogue();
            cross.RegisterDual("reimu", "marisa", "rm_banter");
            cross.RegisterTriple("reimu", "marisa", "sakuya", "rms_meeting");

            string fired = cross.CheckAndTrigger(new[] { "reimu", "marisa", "sakuya" });
            AssertEqual("rms_meeting", fired, "A triple dialogue triggers before a dual when all three are present.");
        }

        private static void TestNotEnoughNpcs()
        {
            MigrationNpcCrossDialogue cross = new MigrationNpcCrossDialogue();
            cross.RegisterDual("reimu", "marisa", "rm_banter");
            AssertEqual(null, cross.CheckAndTrigger(new[] { "reimu" }), "A lone NPC triggers no cross dialogue.");
            AssertEqual(null, cross.CheckAndTrigger(new[] { "reimu", "sakuya" }), "An unregistered pair triggers nothing.");
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
