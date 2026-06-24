# Unity Foundation Architecture

Last updated: 2026-06-23 23:05 CST

This document defines the migration foundation. It is intentionally not a direct
Godot-to-Unity file map. The goal is a Unity-shaped architecture that preserves
game behavior while reducing global coupling.

## Architecture Principles

1. Domain state should be pure C# where possible.
2. Unity scene objects adapt domain state to visuals, input, physics, audio, and
   animation.
3. Avoid one singleton per Godot autoload. Group related systems into coherent
   foundations.
4. Prefer serializable state snapshots over broad manager reach-through.
5. Keep each playable slice independently testable.

## Foundation Layers

### 1. App And Scene Foundation

Unity names:

- `MigrationBootstrap`
- `SceneFlowService`
- `SceneCatalog`
- `GameModeService`
- `SaveCoordinator`

Responsibilities:

- Load persistent services once.
- Register Unity scene names and migration IDs.
- Own fade/loading transitions.
- Track game mode: menu, home, overworld, combat, dialogue, cutscene, sleeping.
- Route save/load through system snapshots.

Godot sources:

- `SceneManager.gd`
- `GameStateManager.gd`
- `SaveSystem.gd`
- `SaveSchema.gd`

Recommended change:

- Replace broad global `SignalBus` scene messages with typed C# events:
  `SceneChanged`, `GameModeChanged`, `BeforeSave`, `AfterLoad`.

### 2. World Clock Foundation

Unity names:

- `GameClock`
- `CalendarService`
- `WeatherService`
- `DayNightLightingController`
- `WorldSimulationBehaviour`
- `WorldTimeSnapshot`

Responsibilities:

- Advance time by configurable real seconds per game minute.
- Emit minute/hour/day/season/year/period events.
- Store calendar state, festivals, weather, moon phase.
- Let visual controllers subscribe to time/weather without owning simulation.

Godot sources:

- `TimeManager.gd`
- `CalendarManager.gd`
- `WeatherSystem.gd`
- `DayNightManager.gd`

Recommended change:

- Merge simulation state into one testable clock/weather domain package.
- Keep lighting separate: Unity `Light`, `Volume`, skybox, fog, and postprocess
  controllers should observe domain events.
- Keep third-party sky/weather packages behind adapters. The simulation rules
  must not depend on any one asset-store or GitHub day-night package.

### 3. Input And Player Action Foundation

Unity names:

- `PlayerInputAdapter`
- `PlayerMotor`
- `PlayerActionStateMachine`
- `CombatActionController`
- `ActionDefinition`
- `ActionEventTimeline`

Responsibilities:

- Normalize input for move, jump, dash, interact, light/heavy attack, skills,
  lock-on, inventory, quest log, and character status.
- Split movement/physics from combat actions.
- Represent attacks and skills as data-driven definitions.
- Emit action events for VFX, hitboxes, animation triggers, hitstop, and sound.

Godot sources:

- `Player3D.gd`
- `scripts/entities/3D/states/*`
- `HealthComponent.gd`
- `HitboxComponent.gd`
- `HurtboxComponent.gd`
- `PhoenixGaugeComponent.gd`
- `SpellCardBeltComponent.gd`
- `StatusEffectComponent.gd`
- `CardBuildMokouActionChain.gd`

Recommended change:

- Do not recreate the giant `Player3D.gd` shape. Unity should split:
  movement, state machine, combat actions, animation bridge, VFX bridge,
  interaction, water/swimming, and buff modifiers.
- Use ScriptableObjects for action definitions once the first combat slice is
  ready.

### 4. Character Animation And Skeleton Foundation

Unity names:

- `CharacterAvatarProfile`
- `HumanoidImportValidator`
- `CharacterAnimatorBridge`
- `NpcAnimationProfile`

Responsibilities:

- Import characters as Unity humanoid avatars where possible.
- Validate rig/avatar configuration.
- Centralize animation clip naming and action-trigger mapping.
- Keep bone attachments explicit for tools and VFX.

Godot sources:

- `Player3D.gd` runtime bone-map logic.
- `Player3DVisuals.gd`.
- `docs/character_rig_animation_debug_runbook.md`.
- `assets/characters/BoneMap_*.tres`.
- Reliable Human Village NPC models under `_rigged_mokou_cc`.

Recommended change:

- Replace Godot runtime track-copy retargeting with Unity humanoid retargeting.
- Keep a validation scene that displays close camera views of idle/walk/action
  poses before accepting imported characters.

### 5. UI Foundation

Unity names:

- `UiShell`
- `HudPresenter`
- `PauseMenuPresenter`
- `NotificationPresenter`
- `DialoguePresenter`
- `DeckEditorPresenter`

Responsibilities:

- Maintain persistent HUD/menu/notification layer.
- Suppress or show HUD by game mode and scene tags.
- Own pause-menu input and pause ownership.
- Present dialogue, inventory, quests, deck editor, shops, cooking, fishing.

Godot sources:

- `GlobalUIManager.gd`
- `UIManager.gd`
- `HUD.tscn`
- `UnifiedGameMenu.gd`
- `DialogicBridge.gd`
- `DialogueRuntimeFacade.gd`
- `MokouDeckEditor.gd`
- `CookingUI.gd`
- `FishingMinigame.gd`

Recommended change:

- Use Unity UI Toolkit for menu-heavy screens if styling iteration matters.
- Use uGUI only where world-space HUDs or existing Unity packages need it.
- Keep dialogue runtime independent of UI so it can be tested and resumed.

### 6. Data, Inventory, Quest, Save Foundation

Unity names:

- `GameDatabase`
- `InventoryRuntime`
- `QuestRuntime`
- `SaveSchema`
- `SaveGameRepository`

Responsibilities:

- Load static JSON or ScriptableObject data.
- Maintain inventory stacks, equipment, quest progress, counters, and rewards.
- Serialize snapshots through a versioned schema.

Godot sources:

- `scripts/architecture/inventory/*`
- `scripts/architecture/save/SaveSchema.gd`
- `InventoryManager.gd`
- `QuestManager.gd`
- `ItemData.gd`
- `QuestData.gd`

Recommended change:

- Port architecture-layer inventory and save schema first. These are already
  close to pure domain code.
- Decide later whether static data remains JSON or moves to ScriptableObjects.

### 7. NPC, Schedule, Relationship Foundation

Unity names:

- `NpcDatabase`
- `NpcScheduleService`
- `NpcSpawnService`
- `BondService`
- `CompanionService`
- `WorldEventService`

Responsibilities:

- Resolve NPC locations/actions by hour, weather, moon phase, special events.
- Spawn/despawn location NPCs.
- Track bonds, gifts, recruitment, companion party and companion skills.

Godot sources:

- `NPCDatabase.gd`
- `NPCScheduleManager.gd`
- `NPCManager.gd`
- `BondSystem.gd`
- `CompanionSystem.gd`
- `EventManager.gd`

Recommended change:

- Keep schedule simulation independent of actual scene spawning.
- Spawn services should consume schedule output and scene spawn points.

### 8. Life-Sim Foundation

Unity names:

- `FarmingService`
- `FishingService`
- `CookingService`
- `HomeInteractionService`

Responsibilities:

- Farming grid/plots/crops/growth/quality/soil memory.
- Fishing spot availability, fish roll, minigame, inventory receipt.
- Cooking recipe unlocks, cookware, cooking buffs.
- Sleep/tea/meal/storage.

Godot sources:

- `scripts/systems/farming/*`
- `scripts/systems/fishing/*`
- `scripts/systems/cooking/*`
- `scripts/systems/home/*`

Recommended change:

- Port rules as pure C# first, then attach scene affordances.
- Farming and cooking are strongly tied to time/date, so they should subscribe
  to `GameClock` rather than polling Unity scene objects.

### 9. Card-Build Combat Foundation

Unity names:

- `CardBuildDatabase`
- `CardBuildRuntimeState`
- `CardEffectExecutor`
- `CardRunProgression`
- `CardCombatBridge`
- `CardCombatHudPresenter`

Responsibilities:

- Card data validation, deck/hand/discard/exhaust, resources, statuses,
  boss rules/domains, action loadout, rewards, run storage.

Godot sources:

- `scripts/systems/cardbuild/*`
- `scripts/ui/cardbuild/*`
- `data/cardbuild`

Recommended change:

- This domain is one of the best candidates for direct pure C# porting.
- Keep card runtime independent of Unity UI and boss scene.

### 10. Audio, VFX, Camera, Environment Foundation

Unity names:

- `AudioService`
- `VfxPoolService`
- `CameraRig`
- `CameraShakeService`
- `EnvironmentVisualController`

Responsibilities:

- Audio map, SFX pool, scene music.
- VFX pooling and action event response.
- Camera follow/lock-on/shake.
- Day-night/weather/environment visuals.

Godot sources:

- `AudioManager.gd`
- `VFX3D.gd`
- `VFXPool.gd`
- `CameraShaker.gd`
- `HumanVillageRenderSetup.gd`
- `BKWindController.gd`

Recommended change:

- Keep effect spawning driven by action/domain events.
- Do not let combat/player code instantiate specific VFX paths directly once
  Unity action definitions exist.

## Suggested Migration Order

1. Foundation documentation and scaffold.
2. Pure C# `GameClock` plus tests for minute/hour/day/season/weather events.
3. Scene bootstrap, scene catalog, game mode service, save snapshot contracts.
4. Inventory/save architecture port.
5. Basic UI shell and notification presenter.
6. Human Village blockout with player motor and camera.
7. Character import/animation validation scene.
8. Player action state machine and first combat action.
9. NPC schedule simulation and simple Human Village NPC spawn.
10. Farming/cooking/fishing rules.
11. Card-build domain port.
12. Boss/combat slice.

## First Foundation Slice

Build `GameClock` before richer scene migration. It supports many later systems:

- Day-night lighting.
- NPC schedules.
- Farming growth.
- Shop refresh.
- Weather.
- Fishing availability.
- Fatigue.
- Save/load.

Acceptance criteria:

- A pure C# clock can advance time deterministically in edit-mode tests.
- It exposes current time, date, season, weekday, time period, weather, and moon
  phase snapshots.
- It emits hour/day/season/weather events without relying on Unity scene state.
