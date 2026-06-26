using System;
using TouhouMigration.Runtime.UI;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationUiPalette: the canonical washi-paper UI colour palette (Godot GameUiStyle colour
    // constants), carried over with exact RGBA values.
    public static class UiPaletteSmokeTests
    {
        private const float Tol = 1e-4f;

        [MenuItem("Touhou Migration/Tests/Run UI Palette Smoke Tests")]
        public static void RunAll()
        {
            AssertColor(new Color(0.91f, 0.85f, 0.72f, 0.98f), MigrationUiPalette.Paper, "Paper");
            AssertColor(new Color(0.25f, 0.18f, 0.14f, 1.0f), MigrationUiPalette.Ink, "Ink");
            AssertColor(new Color(0.55f, 0.16f, 0.13f, 1.0f), MigrationUiPalette.Crimson, "Crimson");
            AssertColor(new Color(0.90f, 0.58f, 0.18f, 1.0f), MigrationUiPalette.Gold, "Gold");
            AssertColor(new Color(0.22f, 0.50f, 0.28f, 1.0f), MigrationUiPalette.Green, "Green");
            AssertColor(new Color(0.04f, 0.035f, 0.03f, 0.62f), MigrationUiPalette.Overlay, "Overlay");
            Debug.Log("UI palette smoke tests passed.");
        }

        private static void AssertColor(Color expected, Color actual, string name)
        {
            if (Mathf.Abs(expected.r - actual.r) > Tol || Mathf.Abs(expected.g - actual.g) > Tol ||
                Mathf.Abs(expected.b - actual.b) > Tol || Mathf.Abs(expected.a - actual.a) > Tol)
            {
                throw new Exception($"{name} colour mismatch. Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
