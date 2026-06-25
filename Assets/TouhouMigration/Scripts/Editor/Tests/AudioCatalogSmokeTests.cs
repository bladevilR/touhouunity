using System;
using TouhouMigration.Runtime.Audio;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // The E7 audio routing hook (Godot AudioManager BGM_TRACKS / SFX_MAP / SCENE_BGM): scene->BGM track
    // resolution, BGM track keys, and the random-variant SFX key pick. The clips are sourced art
    // (Codex/image2); this covers the routing Claude wired so the slot is ready.
    public static class AudioCatalogSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Audio Catalog Smoke Tests")]
        public static void RunAll()
        {
            TestSceneResolvesToTrackAndKey();
            TestUnmappedSceneHasNoTrack();
            TestBgmTrackKeys();
            TestSfxRandomVariantPick();
            TestUnknownSfxResolvesNull();
            Debug.Log("Audio catalog smoke tests passed.");
        }

        private static void TestSceneResolvesToTrackAndKey()
        {
            MigrationAudioCatalog catalog = new MigrationAudioCatalog();
            AssertEqual("town", catalog.ResolveSceneTrack("HumanVillage"), "HumanVillage plays the town track.");
            AssertEqual("town", catalog.ResolveSceneTrack("TownWorld"), "TownWorld plays the town track.");
            AssertEqual("town_alt", catalog.ResolveSceneTrack("MistyLake"), "MistyLake plays the town_alt track.");
            AssertEqual("battle", catalog.ResolveSceneTrack("CirnoBossArena"), "CirnoBossArena plays the battle track.");
            AssertEqual("Audio/BGM/town_theme", catalog.GetBgmKeyForScene("HumanVillage"), "Scene BGM key resolves through the track.");
        }

        private static void TestUnmappedSceneHasNoTrack()
        {
            MigrationAudioCatalog catalog = new MigrationAudioCatalog();
            AssertEqual(null, catalog.ResolveSceneTrack("TitleScreen"), "An unmapped scene has no BGM track (stop music).");
            AssertEqual(null, catalog.ResolveSceneTrack(""), "A blank scene name has no track.");
            AssertEqual(null, catalog.GetBgmKeyForScene("TitleScreen"), "An unmapped scene resolves to no BGM key.");
        }

        private static void TestBgmTrackKeys()
        {
            MigrationAudioCatalog catalog = new MigrationAudioCatalog();
            AssertEqual(true, catalog.HasBgmTrack("battle"), "battle is a known BGM track.");
            AssertEqual("Audio/BGM/a_new_town", catalog.GetBgmKey("town_alt"), "town_alt maps to a_new_town.");
            AssertEqual(null, catalog.GetBgmKey("nonexistent"), "An unknown track has no key.");
        }

        private static void TestSfxRandomVariantPick()
        {
            MigrationAudioCatalog catalog = new MigrationAudioCatalog();
            AssertEqual(true, catalog.HasSfx("ui_click"), "ui_click is a known SFX.");
            // ui_click has 3 variants; the injected rng selects the index deterministically.
            AssertEqual("Audio/SFX/ui/click_001", catalog.ResolveSfxKey("ui_click", _ => 0), "Index 0 picks the first variant.");
            AssertEqual("Audio/SFX/ui/click_003", catalog.ResolveSfxKey("ui_click", _ => 2), "Index 2 picks the third variant.");
            // A single-variant SFX ignores the rng.
            AssertEqual("Audio/SFX/jingles/level_up", catalog.ResolveSfxKey("level_up", null), "A single-variant SFX needs no rng.");
        }

        private static void TestUnknownSfxResolvesNull()
        {
            MigrationAudioCatalog catalog = new MigrationAudioCatalog();
            AssertEqual(null, catalog.ResolveSfxKey("no_such_sfx", _ => 0), "An unknown SFX resolves to null.");
            AssertEqual(null, catalog.ResolveSfxKey("", _ => 0), "A blank SFX name resolves to null.");
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
