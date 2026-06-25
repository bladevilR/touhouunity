using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Dialogue;
using TouhouMigration.Runtime.Social;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class DialogueBondEffectSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Dialogue Bond Effect Smoke Tests")]
        public static void RunAll()
        {
            TestPlainBondRoutesToCurrentNpc();
            TestPrefixedBondRoutesToNamedNpc();
            TestDialogueEffectFormsNpcMemory();
            Debug.Log("Dialogue bond effect smoke tests passed.");
        }

        private static void TestPlainBondRoutesToCurrentNpc()
        {
            SocialBondService bonds = new SocialBondService();
            DialogueEffectRouter router = new DialogueEffectRouter(bonds, null);

            bool handled = router.Apply("marisa", new Dictionary<string, object> { ["bond"] = 5 });

            AssertEqual(true, handled, "Plain bond effect should be handled.");
            // SocialBondService dialogue base is 10, plus the +5 bonus from the effect value.
            AssertEqual(15, bonds.GetBondPoints("marisa"), "Plain bond should add to the current NPC.");
            AssertEqual(0, bonds.GetBondPoints("keine"), "Plain bond should not touch other NPCs.");
        }

        private static void TestPrefixedBondRoutesToNamedNpc()
        {
            SocialBondService bonds = new SocialBondService();
            DialogueEffectRouter router = new DialogueEffectRouter(bonds, null);

            // Talking to Koishi, but the choice raises Keine's bond (Godot bond_keine cross-NPC effect).
            bool handled = router.Apply("koishi", new Dictionary<string, object> { ["bond_keine"] = 20 });

            AssertEqual(true, handled, "A bond_<npc> effect should be handled.");
            AssertEqual(30, bonds.GetBondPoints("keine"), "bond_keine should add to Keine (10 base + 20).");
            AssertEqual(0, bonds.GetBondPoints("koishi"), "bond_keine should not add to the current NPC.");
        }

        private static void TestDialogueEffectFormsNpcMemory()
        {
            SocialBondService bonds = new SocialBondService();
            MigrationNpcMemorySystem memory = new MigrationNpcMemorySystem();
            DialogueEffectRouter router = new DialogueEffectRouter(bonds, null);
            router.BindMemory(memory);

            bool handled = router.Apply("marisa", new Dictionary<string, object> { ["bond"] = 5 });

            AssertEqual(true, handled, "The dialogue bond effect should be handled.");
            AssertEqual(1, memory.GetMemoryCountOfType("marisa", NpcMemoryType.DialogueChoice), "An applied dialogue effect forms a DialogueChoice memory.");
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
