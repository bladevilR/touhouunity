using System;
using TouhouMigration.Runtime.Combat;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationCombatBondSystem: the combat-ally bond (Godot CombatBondSystem) — bond selection +
    // per-character cooldown, the skill cooldown gate, and the permanent passive modifiers.
    public static class CombatBondSystemSmokeTests
    {
        private const double Tol = 1e-9;

        [MenuItem("Touhou Migration/Tests/Run Combat Bond System Smoke Tests")]
        public static void RunAll()
        {
            TestSetBondAndConfig();
            TestSkillCooldownGate();
            TestPassiveModifiers();
            TestNoBondIsInert();
            Debug.Log("Combat bond system smoke tests passed.");
        }

        private static void TestSetBondAndConfig()
        {
            MigrationCombatBondSystem bond = new MigrationCombatBondSystem();
            AssertEqual(false, bond.HasBond, "No bond is selected initially.");

            bond.SetBond(MigrationBondCharacter.Reimu);
            AssertEqual(true, bond.HasBond, "Selecting a bond sets it.");
            AssertEqual(MigrationBondCharacter.Reimu, bond.CurrentBond, "The current bond is Reimu.");
            AssertEqual(30.0, bond.MaxCooldown, "Reimu's skill cooldown is 30s.");
            AssertEqual("梦想封印", bond.SkillName, "Reimu's skill is 梦想封印.");

            bond.SetBond(MigrationBondCharacter.Marisa);
            AssertEqual(25.0, bond.MaxCooldown, "Switching to Marisa updates the cooldown to 25s.");
        }

        private static void TestSkillCooldownGate()
        {
            MigrationCombatBondSystem bond = new MigrationCombatBondSystem();
            bond.SetBond(MigrationBondCharacter.Mokou); // 20s cooldown
            AssertEqual(true, bond.IsSkillReady, "A fresh bond skill is ready.");

            AssertEqual(true, bond.TryActivateSkill(), "The first activation succeeds.");
            AssertEqual(20.0, bond.SkillCooldown, "Activation starts the full cooldown.");
            AssertEqual(false, bond.IsSkillReady, "The skill is not ready during cooldown.");
            AssertEqual(false, bond.TryActivateSkill(), "A second activation on cooldown fails.");

            bond.TickCooldown(19.0);
            AssertEqual(false, bond.IsSkillReady, "Still cooling after 19 of 20 seconds.");
            bond.TickCooldown(5.0);
            AssertEqual(true, AlmostZero(bond.SkillCooldown), "The cooldown clamps at zero.");
            AssertEqual(true, bond.IsSkillReady, "The skill is ready again once cooled.");
        }

        private static void TestPassiveModifiers()
        {
            MigrationCombatBondSystem reimu = new MigrationCombatBondSystem();
            reimu.SetBond(MigrationBondCharacter.Reimu);
            AssertEqual(1.0, reimu.GetPassiveModifier("bounce_bonus"), "Reimu adds a bounce.");
            AssertEqual(0.1, reimu.GetPassiveModifier("homing_after_bounce"), "Reimu's bounce homes weakly.");
            AssertEqual(0.0, reimu.GetPassiveModifier("size_bonus"), "Reimu has no size bonus.");

            MigrationCombatBondSystem marisa = new MigrationCombatBondSystem();
            marisa.SetBond(MigrationBondCharacter.Marisa);
            AssertEqual(1.3, marisa.GetPassiveModifier("size_bonus"), "Marisa enlarges bullets.");
            AssertEqual(1.5, marisa.GetPassiveModifier("knockback_bonus"), "Marisa boosts knockback.");

            MigrationCombatBondSystem yuma = new MigrationCombatBondSystem();
            yuma.SetBond(MigrationBondCharacter.Yuma);
            AssertEqual(50.0, yuma.GetPassiveModifier("gravity_pull"), "Yuma pulls enemies.");
        }

        private static void TestNoBondIsInert()
        {
            MigrationCombatBondSystem bond = new MigrationCombatBondSystem();
            AssertEqual(false, bond.IsSkillReady, "No bond means no ready skill.");
            AssertEqual(false, bond.TryActivateSkill(), "No bond cannot activate a skill.");
            AssertEqual(0.0, bond.GetPassiveModifier("bounce_bonus"), "No bond yields no passive modifier.");

            bond.SetBond(MigrationBondCharacter.None);
            AssertEqual(false, bond.HasBond, "Selecting None is no bond.");
        }

        private static bool AlmostZero(double value) => Math.Abs(value) < Tol;

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
