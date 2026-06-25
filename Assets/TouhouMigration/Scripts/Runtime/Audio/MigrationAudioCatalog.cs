using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Audio
{
    // The E7 audio routing hook (Godot AudioManager BGM_TRACKS / SFX_MAP / SCENE_BGM): resolves a BGM
    // track name, a scene's BGM, and a (possibly multi-variant) SFX name to a Resources lookup key. The
    // audio CLIPS themselves are generated/sourced art and are left to Codex/image2 — they drop into
    // Resources/Audio/BGM/<name> and Resources/Audio/SFX/<...>. This catalog is UnityEngine-free so the
    // routing is unit-tested; MigrationAudioManager does the actual AudioSource playback (null-safe until
    // the clips exist).
    public sealed class MigrationAudioCatalog
    {
        public const string BgmRoot = "Audio/BGM";
        public const string SfxRoot = "Audio/SFX";

        // Godot BGM_TRACKS (track name -> Resources key).
        private static readonly Dictionary<string, string> BgmTracks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "town", BgmRoot + "/town_theme" },
            { "town_alt", BgmRoot + "/a_new_town" },
            { "battle", BgmRoot + "/battle_theme_a" }
        };

        // Godot SCENE_BGM (scene name -> track name), extended with the Unity migration scene names.
        private static readonly Dictionary<string, string> SceneBgm = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "HumanVillage", "town" },
            { "HumanVillageVerticalSlice", "town" },
            { "TownWorld", "town" },
            { "FantasyVillage", "town" },
            { "SuntailVillagePlayable", "town" },
            { "SuntailVillageImported", "town" },
            { "BambooHome", "town_alt" },
            { "BambooHomeVerticalSlice", "town_alt" },
            { "BambooHouse", "town_alt" },
            { "MokouHouse3D", "town_alt" },
            { "MagicForest", "town_alt" },
            { "MistyLake", "town_alt" },
            { "Farm", "town_alt" },
            { "CombatArena", "battle" },
            { "CombatArenaHD2D", "battle" },
            { "CirnoBossArena", "battle" }
        };

        // Godot SFX_MAP (name -> one-or-more Resources keys; a multi-entry SFX picks one at random).
        private static readonly Dictionary<string, string[]> SfxMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "attack_light", new[] { SfxRoot + "/combat/impactPunch_medium_000", SfxRoot + "/combat/impactPunch_medium_001", SfxRoot + "/combat/impactPunch_medium_002" } },
            { "attack_heavy", new[] { SfxRoot + "/combat/impactPunch_heavy_000", SfxRoot + "/combat/impactPunch_heavy_001", SfxRoot + "/combat/impactPunch_heavy_002" } },
            { "hit_soft", new[] { SfxRoot + "/combat/impactSoft_heavy_000", SfxRoot + "/combat/impactSoft_heavy_001", SfxRoot + "/combat/impactSoft_heavy_002" } },
            { "ice_impact", new[] { SfxRoot + "/combat/impactGlass_medium_000", SfxRoot + "/combat/impactGlass_medium_001" } },
            { "footstep_grass", new[] { SfxRoot + "/footsteps/footstep_grass_000", SfxRoot + "/footsteps/footstep_grass_001", SfxRoot + "/footsteps/footstep_grass_002" } },
            { "footstep_wood", new[] { SfxRoot + "/footsteps/footstep_wood_000", SfxRoot + "/footsteps/footstep_wood_001", SfxRoot + "/footsteps/footstep_wood_002" } },
            { "ui_click", new[] { SfxRoot + "/ui/click_001", SfxRoot + "/ui/click_002", SfxRoot + "/ui/click_003" } },
            { "ui_confirm", new[] { SfxRoot + "/ui/confirmation_001", SfxRoot + "/ui/confirmation_002" } },
            { "ui_select", new[] { SfxRoot + "/ui/select_006", SfxRoot + "/ui/select_007" } },
            { "ui_open", new[] { SfxRoot + "/ui/open_001", SfxRoot + "/ui/open_002" } },
            { "ui_close", new[] { SfxRoot + "/ui/close_001", SfxRoot + "/ui/close_002" } },
            { "ui_error", new[] { SfxRoot + "/ui/error_001", SfxRoot + "/ui/error_002" } },
            { "coin", new[] { SfxRoot + "/rpg/handleCoins", SfxRoot + "/rpg/handleCoins2" } },
            { "book", new[] { SfxRoot + "/rpg/bookOpen", SfxRoot + "/rpg/bookFlip1" } },
            { "door_open", new[] { SfxRoot + "/rpg/doorOpen_1", SfxRoot + "/rpg/doorOpen_2" } },
            { "quest_complete", new[] { SfxRoot + "/jingles/quest_complete" } },
            { "level_up", new[] { SfxRoot + "/jingles/level_up" } },
            { "item_obtained", new[] { SfxRoot + "/jingles/item_obtained" } },
            { "bond_up", new[] { SfxRoot + "/jingles/bond_up" } }
        };

        public bool HasBgmTrack(string trackName)
        {
            return !string.IsNullOrWhiteSpace(trackName) && BgmTracks.ContainsKey(trackName.Trim());
        }

        public bool HasSfx(string sfxName)
        {
            return !string.IsNullOrWhiteSpace(sfxName) && SfxMap.ContainsKey(sfxName.Trim());
        }

        // The track name a scene plays, or null if the scene has no mapped BGM (stop music).
        public string ResolveSceneTrack(string sceneName)
        {
            return !string.IsNullOrWhiteSpace(sceneName) && SceneBgm.TryGetValue(sceneName.Trim(), out string track)
                ? track
                : null;
        }

        // Resources key for a BGM track name, or null when unknown.
        public string GetBgmKey(string trackName)
        {
            return !string.IsNullOrWhiteSpace(trackName) && BgmTracks.TryGetValue(trackName.Trim(), out string key)
                ? key
                : null;
        }

        public string GetBgmKeyForScene(string sceneName)
        {
            return GetBgmKey(ResolveSceneTrack(sceneName));
        }

        // Resolve a SFX name to one Resources key, picking a random variant via the injected rng
        // (nextInt(maxExclusive) -> [0, maxExclusive)). Null when the name is unknown.
        public string ResolveSfxKey(string sfxName, Func<int, int> nextInt)
        {
            if (string.IsNullOrWhiteSpace(sfxName) || !SfxMap.TryGetValue(sfxName.Trim(), out string[] keys) || keys.Length == 0)
            {
                return null;
            }

            if (keys.Length == 1 || nextInt == null)
            {
                return keys[0];
            }

            int index = nextInt(keys.Length);
            index = ((index % keys.Length) + keys.Length) % keys.Length;
            return keys[index];
        }
    }
}
