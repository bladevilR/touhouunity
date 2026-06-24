# Godot Project Overview For Unity Migration

Last updated: 2026-06-23 22:31 CST

Source project: `/Users/Shared/Touhougodot`

## Boot Contract

- Engine: Godot 4.x, Forward Plus.
- Product name: `Touhou Phantom`.
- Main scene: `res://scenes/main/TitleScreen.tscn`.
- Formal scene router: `core/autoloads/SceneManager.gd`.
- System catalog: `/Users/Shared/Touhougodot/docs/SYSTEM_CATALOG.md`.

## Formal Autoload Families

The source project currently has 59 runtime autoloads. For Unity, treat these
as system requirements, not as one-to-one singletons.

### Core Runtime

- `SignalBus`: broad event bus for combat, UI, time, calendar, weather, NPC,
  item, cooking, fishing, companion, and notification events.
- `SceneManager`: string/enum scene registry, fade transitions, spawn-point
  placement, combat enter/exit, synchronous loading exceptions for large scenes.
- `GameStateManager`: current mode (`MENU`, `HOME`, `OVERWORLD`, `COMBAT`,
  `DIALOGUE`, `CUTSCENE`, `SLEEPING`), player data, combat session data, rank
  rewards.
- `SaveSystem`: JSON save/load under `user://saves`, schema normalization,
  collection/application of inventory, quest, time, calendar, fatigue, NPC,
  companion, home, cooking, fishing, and buffs.
- `ResourceManager`: resource cache and object pools.
- `ServiceLocator`: small service registry.
- `AudioManager`: BGM/SFX maps, volume state, music fades, scene BGM hooks.
- `VFX3D` and `VFXPool`: immediate VFX helpers and pooled VFX spawning.

### Time, Day, Weather

- `TimeManager`: advances one game minute per real second by default; emits
  minute tick, hour changed, day changed, and time-of-day period events.
- `CalendarManager`: 28-day seasons, 7-day weeks, festivals, day/week/season/year
  events, farming day sync.
- `WeatherSystem`: seasonal weather probabilities, weather duration by hour,
  moon phases, full-moon events, movement/visibility modifiers.
- `DayNightManager`: listens to time period and scene changes, applies global
  color/brightness state and indoor/forced-period overrides.

### Data And Saves

- `ItemData`: loads `data/items.json`.
- `QuestData`: in-code main/side/daily quest definitions.
- `NPCDatabase`: static NPC info, companion stats, gifts, recruitment, schedule
  templates, action text.
- `CropDatabase`, `FishDatabase`, `CookingDatabase`, `ShopData`, `GiftDatabase`,
  `MonsterDatabase`, `GameConstants`, `GameSettings`.
- `scripts/architecture/*` already contains cleaner boundary code for inventory,
  save schema, dialogue runtime, and notifications.

### UI And Dialogue

- `GlobalUIManager`: persistent HUD/menu/notification layer, pause ownership,
  scene-based HUD suppression, deck editor safety checks.
- `UIManager`: simpler older HUD loader for `GameUI.tscn`.
- `DialogicBridge`: runtime dialogue facade with rune UI, Dialogic, and fallback
  UI modes.
- `DialogueRuntimeFacade`: cleaner architecture layer for dialogue sessions,
  choices, snapshots, resume, and effect application.
- `SimpleDialogueUI`, `GeneralShopUI`, `DialogueChoiceUI`,
  `GiftSelectionController`, `CharacterStatusPanel`.

### Life-Sim And RPG Runtime

- `InventoryManager`: slot inventory, stack rules, equip/use flow, cooking buff
  hooks, save adapter.
- `QuestManager`: active/completed quests, progress counters, daily resets,
  delivery/talk/craft/inspect notifications.
- `TimeManager`, `CalendarManager`, `WeatherSystem`, `FatigueSystem`,
  `NPCScheduleManager`, `NPCManager`, `BondSystem`, `BondEventSystem`,
  `NPCMemorySystem`, `NPCRelationshipNetwork`, `CompanionSystem`,
  `EventManager`, `HomeInteractionSystem`.
- `FishingManager`, `CookingManager`, `CookingBuffSystem`,
  `LootDropManager`, `ShopManager`, `CropSpriteManager`.

### Combat And Card-Build

- `Player3D` is the main 3D player/combat controller and currently combines
  movement, dash, jump, water/swim, attack data, VFX, buffs, animation loading,
  bone mapping, tool attachment, and retarget logic.
- `scripts/entities/3D/states/*` define player/enemy FSM states.
- Combat components include `HealthComponent`, `HitboxComponent`,
  `HurtboxComponent`, `PhoenixGaugeComponent`, `SpellCardBeltComponent`,
  `StatusEffectComponent`, and older 2D action systems.
- `scripts/systems/cardbuild/*` is relatively domain-oriented and already
  separable: database, runtime state, deck, effects, boss clauses/domains,
  profile/run progression, metrics, companion bridge.

## Registered Runtime Scenes

`SceneManager.gd` directly maps these scene keys:

| Key | Godot Scene |
| --- | --- |
| `menu` | `scenes/main/TitleScreen.tscn` |
| `town`, `codex_human_village` | `scenes/locations/HumanVillageEnvironmentSlice.tscn` |
| `bamboo_home` | `scenes/overworld/bamboo_home/BambooHome.tscn` |
| `fantasy_village`, `suntail_village` | `scenes/locations/SuntailVillagePlayable.tscn` |
| `hakurei_shrine` | `scenes/locations/hakurei_shrine/HakureiShrineCourtyard.tscn` |
| `magic_forest` | `scenes/locations/MagicForest.tscn` |
| `misty_lake` | `scenes/locations/MistyLake.tscn` |
| `scarlet_mansion_front` | `scenes/locations/scarlet_devil_mansion/ScarletDevilMansionFront.tscn` |
| `pure_nature_classic` | `scenes/locations/PureNatureClassic.tscn` |
| `pure_nature_fantasy_forest` | `scenes/locations/PureNatureFantasyForest.tscn` |
| `pure_nature_mountains` | `scenes/locations/PureNatureMountains.tscn` |
| `pure_nature_islands` | `scenes/locations/PureNatureIslands.tscn` |
| `pure_nature_jungle` | `scenes/locations/PureNatureJungle.tscn` |
| `pure_nature_meadows` | `scenes/locations/PureNatureMeadows.tscn` |
| `angrymesh_meadow`, `meadow_environment` | `scenes/locations/angrymesh_meadow/AngryMeshMeadow.tscn` |
| `town_world` | `scenes/locations/TownWorld.tscn` |
| `farm` | `scenes/overworld/farm/Farm.tscn` |
| `dungeon_entrance` | `scenes/overworld/dungeon_entrance/DungeonEntrance.tscn` |
| `combat`, `battle` | `scenes/combat/CombatArenaHD2D.tscn` |
| `settings` | `scenes/ui/SettingsMenu.tscn` |
| `loading` | `scenes/main/LoadingScreen.tscn` |
| `cirno_mvp`, `cirno_spell` | `scenes/bosses/cirno/CirnoBossArena.tscn` |

Synchronous loading exceptions in Godot: Hakurei Shrine, Scarlet Mansion front,
Suntail Village, and AngryMesh Meadow. This likely reflects heavy generated
content or import-resource sensitivity.

## Character, Skeleton, Animation Notes

- Main player character: Mokou.
- Main player scene: `scenes/entities/3D/Player3D.tscn`.
- Main player script: `scripts/entities/3D/Player3D.gd`.
- `Player3D.gd` contains runtime bone mapping between humanoid names and
  `CC_Base_*` names, plus imported action-library loading.
- Existing runbook warns that visual validation must inspect actual posed
  screenshots/bone probes, not just `AnimationPlayer.is_playing()`.
- Reliable Human Village runtime NPC models are documented as under
  `assets/characters/model/NEW/_rigged_mokou_cc`.
- Unity should use Mecanim humanoid avatars and `AnimatorController`/Playable
  graphs instead of copying animation tracks at runtime.

## Important Non-Production Boundaries

Per `docs/SYSTEM_CATALOG.md`:

- Test/validation: `scripts/test`, `scripts/tests`, `scenes/test`, `scenes/tests`.
- Debug-only: `scripts/debug`, `scenes/debug`.
- Dev tools: `tools`, `scripts/tools`, `scripts/dev`, `scenes/tools`.
- Legacy/archive: `docs/archive`, `tools/_deprecated`, legacy root helpers.
- `scenes/effects/unity_generated` is runtime-review, not disposable; some
  generated effect scenes are runtime-referenced.

## Migration Implications

- Do not port all autoloads one-to-one. Convert them into grouped Unity services
  with explicit events and serializable state.
- Favor pure C# domain classes for inventory, save, dialogue, card-build,
  schedules, weather, and time. Use MonoBehaviours as adapters.
- Treat `Player3D.gd` as a feature inventory, not a target file shape. Split it
  into movement, combat actions, animation, VFX, water, interaction, and buffs.
- Rebuild UI as Unity-native screens rather than converting `.tscn` nodes.
- Reuse original Unity-pack assets from
  `/Users/Shared/TouhouUnityMigration/ExternalUnityAssets/unity_imports` where
  possible.
- For character animation, prefer Unity humanoid import/retargeting and avoid
  recreating Godot's runtime track-copy workaround.
