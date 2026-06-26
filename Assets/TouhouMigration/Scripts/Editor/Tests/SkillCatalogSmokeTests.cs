using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationSkillCatalog: the character skill table + queries (Godot SkillDatabase get_skill /
    // get_skills_by_type / get_character_skills).
    public static class SkillCatalogSmokeTests
    {
        private const double Tol = 1e-9;

        [MenuItem("Touhou Migration/Tests/Run Skill Catalog Smoke Tests")]
        public static void RunAll()
        {
            TestPhoenixDash();
            TestImmortalFlame();
            TestQueries();
            Debug.Log("Skill catalog smoke tests passed.");
        }

        private static void TestPhoenixDash()
        {
            MigrationSkillCatalog cat = new MigrationSkillCatalog();
            MigrationSkillDefinition dash = cat.GetSkill("mokou_phoenix_dash");
            AssertEqual(true, dash != null, "Phoenix dash is present.");
            AssertEqual("凤凰之翼", dash.Name, "Its name.");
            AssertEqual(MigrationSkillDamageType.Fire, dash.DamageType, "It deals fire damage.");
            AssertEqual(MigrationSkillTargetType.Line, dash.TargetType, "It is a line attack.");
            AssertTrue(Math.Abs(50.0 - dash.BaseDamage) < Tol, "It hits for 50.");
            AssertTrue(Math.Abs(8.0 - dash.Cooldown) < Tol, "Its cooldown is 8s.");
            AssertEqual(true, dash.Effects.Contains("burn") && dash.Effects.Contains("knockback"),
                "It burns and knocks back.");
        }

        private static void TestImmortalFlame()
        {
            MigrationSkillDefinition flame = new MigrationSkillCatalog().GetSkill("mokou_immortal_flame");
            AssertEqual(MigrationSkillTargetType.Self, flame.TargetType, "Immortal flame targets self.");
            AssertTrue(Math.Abs(20.0 - flame.Cooldown) < Tol, "Its cooldown is 20s.");
            AssertTrue(Math.Abs(5.0 - flame.Duration) < Tol, "It lasts 5s.");
        }

        private static void TestQueries()
        {
            MigrationSkillCatalog cat = new MigrationSkillCatalog();
            AssertEqual(5, cat.Count, "The catalog holds five skills.");
            AssertEqual(true, cat.GetSkill("unknown_skill") == null, "An unknown skill returns null.");

            IReadOnlyList<MigrationSkillDefinition> mokou = cat.GetCharacterSkills("mokou");
            AssertEqual(2, mokou.Count, "Mokou has two skills.");

            AssertEqual(1, cat.GetCharacterSkills("reimu").Count, "Reimu has one (placeholder) skill.");
            AssertEqual(5, cat.GetSkillsByType(MigrationSkillType.Active).Count, "All five skills are active.");
            AssertEqual(0, cat.GetSkillsByType(MigrationSkillType.Ultimate).Count, "There are no ultimate skills yet.");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
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
