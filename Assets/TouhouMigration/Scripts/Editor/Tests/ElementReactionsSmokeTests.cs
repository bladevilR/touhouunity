using System;
using TouhouMigration.Runtime.Combat;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationElementReactions: the elemental reaction table + order-independent lookup (Godot
    // ElementData.check_reaction / ELEMENT_REACTIONS).
    public static class ElementReactionsSmokeTests
    {
        private const double Tol = 1e-9;

        [MenuItem("Touhou Migration/Tests/Run Element Reactions Smoke Tests")]
        public static void RunAll()
        {
            TestFireOilExplosion();
            TestOrderIndependent();
            TestOtherReactions();
            TestNoReaction();
            Debug.Log("Element reactions smoke tests passed.");
        }

        private static void TestFireOilExplosion()
        {
            MigrationElementReactions reactions = new MigrationElementReactions();
            MigrationElementReaction r = reactions.CheckReaction(MigrationElementType.Fire, MigrationElementType.Oil);
            AssertEqual(true, r != null, "Fire + Oil reacts.");
            AssertEqual("地狱火", r.Name, "Fire + Oil is 地狱火.");
            AssertEqual("explosion", r.EffectType, "Its effect is an explosion.");
            AssertTrue(Math.Abs(3.0 - r.DamageMultiplier) < Tol, "It deals 300% damage.");
            AssertTrue(Math.Abs(200.0 - r.Radius) < Tol, "Its radius is 200.");
        }

        private static void TestOrderIndependent()
        {
            MigrationElementReactions reactions = new MigrationElementReactions();
            MigrationElementReaction r = reactions.CheckReaction(MigrationElementType.Oil, MigrationElementType.Fire);
            AssertEqual("地狱火", r.Name, "Reaction lookup is order-independent.");
        }

        private static void TestOtherReactions()
        {
            MigrationElementReactions reactions = new MigrationElementReactions();
            AssertEqual("寒霜瘟疫", reactions.CheckReaction(MigrationElementType.Ice, MigrationElementType.Poison).Name,
                "Ice + Poison is 寒霜瘟疫.");
            AssertEqual("蒸汽爆炸", reactions.CheckReaction(MigrationElementType.Ice, MigrationElementType.Fire).Name,
                "Ice + Fire is 蒸汽爆炸.");
            AssertTrue(Math.Abs(0.5 - reactions.CheckReaction(MigrationElementType.Gravity, MigrationElementType.Lightning).DamageMultiplier) < Tol,
                "Gravity + Lightning deals 50% sustained damage.");
        }

        private static void TestNoReaction()
        {
            MigrationElementReactions reactions = new MigrationElementReactions();
            AssertEqual(true, reactions.CheckReaction(MigrationElementType.Fire, MigrationElementType.Fire) == null,
                "Two of the same element do not react.");
            AssertEqual(true, reactions.CheckReaction(MigrationElementType.Ice, MigrationElementType.Gravity) == null,
                "An unpaired combination does not react.");
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
