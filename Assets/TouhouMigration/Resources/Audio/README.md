# Game Audio — TODO(codex/image2)

This folder is the **audio clip slot** for BGM + SFX. The code/routing hooks are wired
(`MigrationAudioCatalog` resolves names→keys; `MigrationAudioManager` plays them); the
**clips themselves are sourced/generated audio and are Codex/image2's job** (per the
project division of labor). Every play is null-safe — scenes run silent-but-clean until
the clips land.

## Convention (what the manager loads)

`Resources.Load<AudioClip>(key)` with the keys below. Drop each clip at
`Assets/TouhouMigration/Resources/<key>.<ext>` (`.ogg`/`.wav`/`.mp3`; `Resources.Load`
omits the extension).

### BGM — `Resources/Audio/BGM/`
- `town_theme` (track `town`) — villages: HumanVillage, TownWorld, FantasyVillage, Suntail×2
- `a_new_town` (track `town_alt`) — BambooHome/House, MokouHouse3D, MagicForest, MistyLake, Farm
- `battle_theme_a` (track `battle`) — CombatArena, CombatArenaHD2D, CirnoBossArena

Scene→track routing lives in `MigrationAudioCatalog.SceneBgm`; a scene with no entry
stops the music.

### SFX — `Resources/Audio/SFX/<category>/`
Multi-variant SFX pick one file at random. Categories/keys (mirroring Godot `SFX_MAP`):
- `combat/` — attack_light/heavy, hit_soft, ice_impact (impact*_NNN)
- `footsteps/` — footstep_grass_NNN, footstep_wood_NNN
- `ui/` — click_001-3, confirmation_001-2, select_006-7, open_001-2, close_001-2, error_001-2
- `rpg/` — handleCoins/handleCoins2 (coin), bookOpen/bookFlip1 (book), doorOpen_1/2
- `jingles/` — quest_complete, level_up, item_obtained, bond_up

See `MigrationAudioCatalog` for the exact key list. Partial delivery is safe — add
clips incrementally; missing ones stay silent.
