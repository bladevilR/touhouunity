using System;
using TouhouMigration.Runtime.Fishing;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationFishingMinigame: the catch-bar minigame core (Godot FishingMinigame box physics +
    // catch progress) — box lift/gravity, progress when the fish is/ isn't in the box, win/lose.
    public static class FishingMinigameSmokeTests
    {
        private const double Tol = 1e-9;

        [MenuItem("Touhou Migration/Tests/Run Fishing Minigame Smoke Tests")]
        public static void RunAll()
        {
            TestStartState();
            TestBoxPhysics();
            TestKeepingFishInBoxCatches();
            TestLettingFishEscapeFails();
            Debug.Log("Fishing minigame smoke tests passed.");
        }

        private static void TestStartState()
        {
            MigrationFishingMinigame game = new MigrationFishingMinigame();
            game.Start(0.2);
            AssertTrue(Math.Abs(0.3 - game.CatchProgress) < Tol, "Catch progress starts at 0.3.");
            AssertTrue(Math.Abs(0.5 - game.BoxPosition) < Tol, "The box starts centred.");
            AssertEqual(true, game.IsActive, "The game is active at the start.");
            AssertEqual(false, game.IsCaught, "Not yet caught.");
            AssertEqual(false, game.IsFailed, "Not yet failed.");
        }

        private static void TestBoxPhysics()
        {
            MigrationFishingMinigame game = new MigrationFishingMinigame();
            game.Start(0.2);

            // No lift -> gravity pulls the box down (position decreases).
            game.Tick(0.1, lifting: false, fishPosition: 0.5);
            AssertTrue(game.BoxPosition < 0.5, "Without lift the box falls.");

            // Sustained gravity drives the box to the floor and clamps at 0.
            for (int i = 0; i < 100; i++)
            {
                game.Tick(0.1, lifting: false, fishPosition: 1.0);
            }

            AssertTrue(game.BoxPosition >= 0.0, "The box never goes below 0.");
        }

        private static void TestKeepingFishInBoxCatches()
        {
            MigrationFishingMinigame game = new MigrationFishingMinigame();
            game.Start(0.2);

            // Fish always at the box position -> always in box -> progress climbs to a catch.
            for (int i = 0; i < 30 && game.IsActive; i++)
            {
                game.Tick(0.1, lifting: true, fishPosition: game.BoxPosition);
            }

            AssertEqual(true, game.IsCaught, "Holding the fish in the box catches it.");
            AssertEqual(false, game.IsActive, "A caught game is no longer active.");
        }

        private static void TestLettingFishEscapeFails()
        {
            MigrationFishingMinigame game = new MigrationFishingMinigame();
            game.Start(0.2);

            // Box falls to the floor; fish sits at the top -> never in box -> progress drains to a fail.
            for (int i = 0; i < 30 && game.IsActive; i++)
            {
                game.Tick(0.1, lifting: false, fishPosition: 1.0);
            }

            AssertEqual(true, game.IsFailed, "Letting the fish escape fails the catch.");
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
