using System;
using TouhouMigration.Runtime.UI.Dialogue;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // The E5 portrait presentation hook: expression normalization/fallback and the Resources lookup key
    // the dialogue view renders into. The portrait PNGs are generated art (Codex/image2); this covers the
    // resolution logic Claude wired so the slot is ready.
    public static class PortraitCatalogSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Portrait Catalog Smoke Tests")]
        public static void RunAll()
        {
            TestStandardExpressionsResolveToTheirOwnKey();
            TestUnknownAndEmptyExpressionFallBackToNeutral();
            TestExpressionSynonymsNormalize();
            TestBlankSpeakerIsNarration();
            TestResourceKeyLowercasesAndUsesRoot();
            Debug.Log("Portrait catalog smoke tests passed.");
        }

        private static void TestStandardExpressionsResolveToTheirOwnKey()
        {
            MigrationPortraitCatalog catalog = new MigrationPortraitCatalog();
            foreach (string expression in MigrationPortraitCatalog.StandardExpressions)
            {
                AssertEqual(expression, catalog.NormalizeExpression(expression), $"Standard expression {expression} should resolve to itself.");
                AssertEqual($"Portraits/reimu/{expression}", catalog.ResolveResourceKey("reimu", expression), $"Key for reimu/{expression} should follow the convention.");
            }
        }

        private static void TestUnknownAndEmptyExpressionFallBackToNeutral()
        {
            MigrationPortraitCatalog catalog = new MigrationPortraitCatalog();
            AssertEqual("neutral", catalog.NormalizeExpression("smug"), "An unknown expression should fall back to neutral.");
            AssertEqual("neutral", catalog.NormalizeExpression(""), "An empty expression should fall back to neutral.");
            AssertEqual("neutral", catalog.NormalizeExpression(null), "A null expression should fall back to neutral.");
            AssertEqual("Portraits/marisa/neutral", catalog.ResolveResourceKey("marisa", "whatever"), "An unknown expression key should resolve to neutral.");
        }

        private static void TestExpressionSynonymsNormalize()
        {
            MigrationPortraitCatalog catalog = new MigrationPortraitCatalog();
            AssertEqual("angry", catalog.NormalizeExpression("mad"), "mad should normalize to angry.");
            AssertEqual("sad", catalog.NormalizeExpression("upset"), "upset should normalize to sad.");
            AssertEqual("surprised", catalog.NormalizeExpression("surprise"), "surprise should normalize to surprised.");
            AssertEqual("surprised", catalog.NormalizeExpression("Shocked"), "shocked should normalize to surprised (case-insensitive).");
        }

        private static void TestBlankSpeakerIsNarration()
        {
            MigrationPortraitCatalog catalog = new MigrationPortraitCatalog();
            AssertEqual(true, catalog.IsNarration(""), "A blank speaker is narration.");
            AssertEqual(true, catalog.IsNarration("   "), "A whitespace speaker is narration.");
            AssertEqual(false, catalog.IsNarration("稗田阿求"), "A named speaker is not narration.");
            AssertEqual(null, catalog.ResolveResourceKey("", "happy"), "A blank npc id resolves to no portrait key.");
        }

        private static void TestResourceKeyLowercasesAndUsesRoot()
        {
            MigrationPortraitCatalog catalog = new MigrationPortraitCatalog("Art/Faces");
            AssertEqual("Art/Faces/keine/happy", catalog.ResolveResourceKey("Keine", "Happy"), "The key should lowercase ids/expressions and use the configured root.");
            AssertEqual("Portraits", new MigrationPortraitCatalog().ResourceRoot, "The default resource root is Portraits.");
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
