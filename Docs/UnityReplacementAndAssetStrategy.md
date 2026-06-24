# Unity Replacement And Asset Strategy

Last updated: 2026-06-25 04:06 CST

This document records which Godot-era systems should be rebuilt, replaced by
Unity-native packages, or imported from original Unity source assets.

## Main Rule

If a scene or asset pack originally came from Unity, import the original Unity
source asset into `/Users/Shared/TouhouUnityMigration` instead of reconstructing
it from Godot `.tscn` wrappers.

Unity-origin assets should live in the standalone Unity migration project, not
remain as working assets inside `/Users/Shared/Touhougodot`. The old Godot path
should keep only a migrated README/marker document after the asset move. Do not
leave a second working copy in the Godot tree once an asset pack is confirmed to
be Unity-origin.

The original Unity-era source assets have been moved out of the Godot project
and into this standalone Unity migration project:

- `/Users/Shared/TouhouUnityMigration/ExternalUnityAssets/unity_imports/Suntail_Village`
- `/Users/Shared/TouhouUnityMigration/ExternalUnityAssets/unity_imports/PureNature_Meadows`
- `/Users/Shared/TouhouUnityMigration/ExternalUnityAssets/unity_imports/PureNature_FantasyForest`
- `/Users/Shared/TouhouUnityMigration/ExternalUnityAssets/unity_imports/PureNature_Mountains`
- `/Users/Shared/TouhouUnityMigration/ExternalUnityAssets/unity_imports/PureNature_Jungle`
- `/Users/Shared/TouhouUnityMigration/ExternalUnityAssets/unity_imports/PureNature_Islands`
- `/Users/Shared/TouhouUnityMigration/ExternalUnityAssets/unity_imports/PureNature_1_2`
- `/Users/Shared/TouhouUnityMigration/ExternalUnityAssets/unity_imports/AngryMesh_MeadowEnvironment`
- `/Users/Shared/TouhouUnityMigration/ExternalUnityAssets/unity_imports/TerrainExport`

`AngryMesh_MeadowEnvironment` also includes URP/HDRP template `.unitypackage`
files, so its Unity import should be tested directly against the current render
pipeline instead of using the Godot wrapper scene.

As of 2026-06-24 13:56 CST,
`/Users/Shared/Touhougodot/assets/unity_imports` has been verified to contain
only `README_MIGRATED_TO_UNITY.md`; the Unity migration project is the
authoritative source location for those packs.

Keep these packs outside `Assets/` until a migration slice needs specific
models, textures, prefabs, or scenes. Promote only the selected runtime content
into `Assets/TouhouMigration/...` so Unity does not import every source pack on
every editor refresh.

## Replacement Matrix

| Godot system | Unity direction | Decision |
| --- | --- | --- |
| `DayNightCycle3D` / `DayNightManager` / `WeatherSystem` | Keep pure game rules in `GameClock`, `WeatherService`, and `DayNightPalette`; adapt visuals through `DayNightLightingController`. | Do not bind game rules to a third-party sky/weather package. Third-party packages may plug in behind an adapter later. |
| Imported Unity world scenes | Direct Unity asset import from `ExternalUnityAssets/unity_imports`. | Prefer original Unity assets over reverse-converting Godot `.tscn` scenes. |
| `PhantomCamera` | Unity Cinemachine package. Candidate: `Unity-Technologies/com.unity.cinemachine`. | Use for follow/lock-on/cutscene cameras once player slice starts. |
| `Dialogic` | Project-owned dialogue runtime plus Unity presenter first. Candidates for later authoring: Yarn Spinner Unity or Ink Unity. | Do not port Dialogic internals. Unity M13 now preserves current compact JSON conditions/effects as project data rules. |
| `GiftDatabase.gd`, `NPCDatabase.get_gift_reaction`, `GiftSelectionUI`, and NPC `receive_gift` methods | Project-owned social/gift services plus Unity UI/presenter adapters. | Use the richer `data/gifts.json` contract as canonical. Do not preserve the split older NPCDatabase gift table as a separate Unity authority. |
| `CookingDatabase.gd`, `CookingManager.gd`, cooking station/UI, and CookingBuffSystem | Project-owned cooking data/service layer plus migration-era UI first; production UI and buff application in later slices. | Keep dish classifiers, recipes, cooking consequences, and quality stacks in Unity services. Do not bind quest selectors to manual tags once CookingDatabase metadata exists. |
| `Player3D` attacks, `AttackEffectHitbox`, hitbox/hurtbox components, Enemy3D first-slice state, enemy death, kill-heal, per-kill loot, quest kill objectives, enemy variants, attack windup, ranged projectiles, generated enemy prefabs, monster visual sources, monster animation sources, `VFX3D`/`VFXPool`, Phoenix gauge, Cirno arena settlement, and Bullet3D danger intent | Project-owned combat runtime plus thin Unity scene adapters, generated presentation prefabs/templates, and service-style settlement subscribers. | Keep damage math, cooking buffs, HP, one-shot defeat, per-kill loot, quest objective progress, variant stats, attack timing, first projectile rules, generated catalog prefabs, generated real-visual mounting, generated AnimatorControllers, the runtime animation bridge, telegraph/active/recovery/death-delay windows, active-window danger volumes, projectile mesh/trail/lifetime feedback, target damage events, `TakeDamage`/`Die` presentation triggers, reusable feedback templates/prefabs, projectile sweep/impact hooks, hurt flash/knockback, simple death ParticleSystem feedback, settings-gated damage numbers, one-shot XP/coin/loot notifications, projectile environment-impact raycasts, one-shot normal/perfect projectile graze events, weakness-aware projectile shatter events/hitbox routing, Phoenix gauge projectile settlement, one-shot heavy-burst hitbox range consumption, target-side Perfect Freeze stun, projectile-owned Perfect Freeze lifecycle, Perfect Freeze projectile arm-delay, scoped Perfect Freeze encounter prefab, Perfect Freeze safe-lane/phase-cast timing, zero-based Perfect Freeze cast-plan parity with distinct ice-orb/ice-shard prefabs, typed Perfect Freeze clear/capture/timeout result events, a ScriptableObject Perfect Freeze phase-plan asset, prefab-keyed projectile pooling, ice-lance reflect reward events/stun settlement, authored distance-gated ice-lance snipe casts, snowball-pressure intent arbitration, a singleton rolling snowball hazard runtime/presenter started by boss intent, boss snowball push-position targets, close-range evade movement targets, snowball player-health damage routing, snowball capture-hit registration, and snowball arena-radius bounce in services/controllers/editor tooling; attach MonoBehaviour adapters where scene objects need Unity lifecycle/events. Godot provides gameplay intent, not a required implementation shape. Keep animation-event-authored frames, broader VFX/non-encounter return ownership, production combat HUD/world-space UI, XP gem pickup routing, full AI/navigation/final boss arena behavior, hover/flight pose and NavMesh/root-motion polish, polished snowball VFX/audio/camera feedback, player-side i-frame ownership, production phase-outcome consumers, return-shot VFX, graze CardBuild settlement, and combat-session/rank settlement as separate service slices. |
| `CardBuildDatabase.gd`, `CardBuildProfileStore.gd`, `MokouDeckEditor.gd` | Project-owned CardBuild data/profile/UI shell first; combat effect executor later. | Preserve Godot JSON data and default Mokou profile. Avoid coupling the deck editor to combat runtime before save/inventory exists. |
| `SaveSystem.gd`, `GameSaveManager.gd`, `InventoryManager.gd`, `ItemData.gd` | Project-owned C# repositories/services. | Keep slot save, meta save, settings, and card run persistence as separate surfaces until migration proves a safe unification path. |
| `Beehave` | Thin project AI interface first; possible later package: `ashblue/fluid-behavior-tree`. | Do not add package until the M32 real-visual prefab model has runtime animation state driving and has been tested against multiple ranged/melee variants with Unity NavMesh movement. |
| `VRM` and `Godot-MToon-Shader` | UniVRM and Unity Toon Shader. | Strong candidates for character import and toon materials. Validate with Mokou and one `_rigged_mokou_cc` NPC. |
| `VFX Library` and `UniParticles3D` | Unity ParticleSystem first, VFX Graph later if URP/HDRP setup supports it. | Rebuild effects as prefabs; keep VFX calls data-driven from action timelines. |
| `Terrain3D`, `ProtonScatter`, `SimpleGrassTextured` | Unity Terrain, terrain details, original asset-pack prefabs, and optional scatter tooling. | Start with direct world kit import and authored blockouts; add scatter tooling only after scene scale is known. |
| `FlexibleToonShader` | Unity Toon Shader or URP shader graph variants. | Do not port Godot shader code directly. |
| `HUD.tscn`, `UnifiedGameMenu.gd`, `SettingsMenu.gd`, `GameSettings.gd` | Unity runtime UI/settings shell first, then replace IMGUI with uGUI/UITK once data systems stabilize. | Preserve the formal surface and state model now; defer final UI tech/styling until save, inventory, card, and dialogue data are connected. |
| `Unidot Importer` | Not needed in Unity. | Its role was moving Unity assets into Godot; migration reverses that direction. |
| Godot MCP, SceneBuilder, GodotIQ, custom editor setup tools | Not runtime systems. | Ignore for Unity runtime migration. |

## Installed Unity Packages

| Package | Version | Reason |
| --- | --- | --- |
| `com.unity.cloud.gltfast` | `6.19.0` | Required to import `.glb`/glTF source models such as Bamboo Home and props. |

## External Candidates Checked

GitHub CLI was authenticated through the local proxy `127.0.0.1:10808` and used
for repository checks.

| Candidate | Source | Current read |
| --- | --- | --- |
| Cinemachine | https://github.com/Unity-Technologies/com.unity.cinemachine | Official Unity camera package; use as PhantomCamera replacement. |
| Yarn Spinner Unity | https://github.com/YarnSpinnerTool/YarnSpinner-Unity | Active MIT-licensed dialogue integration. Good candidate if dialogue authoring moves to Yarn. |
| Ink Unity Integration | https://github.com/inkle/ink-unity-integration | Active narrative scripting integration. Good candidate if branching prose becomes more important than visual-node authoring. |
| UniVRM | https://github.com/vrm-c/UniVRM | Active MIT-licensed VRM/MToon import path. Strong character-pipeline candidate. |
| Unity Toon Shader | https://github.com/Unity-Technologies/com.unity.toonshader | Official toon-shader candidate for stylized character/material work. |
| fluid-behavior-tree | https://github.com/ashblue/fluid-behavior-tree | MIT-licensed code-driven behavior tree. Consider later for AI, not foundation. |
| Unity Gamekit3D | https://github.com/Unity-Technologies/Gamekit3D | Useful reference for third-person enemy/Animator/NavMesh/melee patterns such as Animator state behaviours and attack windows. No clear license found during M33; reference only, do not copy code/assets. |
| Unity Open Project 1 | https://github.com/UnityTechnologies/open-project-1 | Apache-2.0 Unity project with ScriptableObject-driven state/config patterns. Reference for later enemy archetype/action data, not an M33 dependency. |
| UnityHFSM | https://github.com/Inspiaaa/UnityHFSM | MIT-licensed modern HFSM. Candidate/reference for later enemy gameplay state ownership if `MigrationSimpleEnemyController` grows beyond the first-slice controller. |
| Unity Input System | https://github.com/Unity-Technologies/InputSystem | Official input package. Reference for future gameplay/UI input separation; input should issue commands, not directly open combat hit windows. |
| Diaonic UnityDayNightCycle | https://github.com/Diaonic/UnityDayNightCycle | Small Stardew-inspired script, no license detected. Reference only. |
| aspect-ux WeatherSystem | https://github.com/aspect-ux/WeatherSystem | Apache-2.0 small weather system. Reference only unless future inspection proves it adds value. |
| GyeMong | https://github.com/Apptive-Game-Team/GyeMong | Unity action/bullet reference with a graze controller feeding gauge-like state. No license detected during M40; reference only, no code copied. |
| battle-circle-ai Telegraph | https://github.com/tutsplus/battle-circle-ai/blob/3dcee7b734111d1534e5e7682c1f25ebec5f9a66/src/Assets/Scripts/Telegraph.cs | M45 GitHub code-search reference for keeping attack telegraphs as independent cue components activated by attack/boss logic. Reference only, no code copied. |
| Delv TelegraphAlert | https://github.com/gamkedo-la/Delv/blob/b2c8e8aacac1b6bf06e1fe258f18cb95c26490b1/Assets/Scripts/Enemy/AI/TelegraphAlert.cs | M45 GitHub code-search reference for timed warning indicators. Reference only, no code copied. |
| ChainBreakers Telegraph | https://github.com/wesW1ld/ChainBreakers/blob/d5f8e24d87b87c6eaf49d7a144692883b15013df/Assets/Scripts/Minigame1/Telegraph.cs | M46 GitHub code-search reference for separating player warning windows from damaging logic. Reference only, no code copied. |
| LoneFighter BulletPattern | https://github.com/PetrosTepoyan/LoneFighter/blob/efbbcd6f740ca146999a4ac334045449229c5058/Assets/Scripts/Enemies/Patterns/BulletPattern.cs | M47 GitHub reference for expressing bullet shapes as Unity `ScriptableObject` patterns while an emitter/director owns cadence and target context. Reference only, no code copied. |
| LoneFighter PoolService | https://github.com/PetrosTepoyan/LoneFighter/blob/efbbcd6f740ca146999a4ac334045449229c5058/Assets/Scripts/Systems/PoolService.cs | M47 GitHub reference for prefab-keyed `UnityEngine.Pool.ObjectPool<GameObject>` ownership. Stronger fit than tag-singleton pools for PF projectiles with arm delay, graze, shatter, and settlement binding. Reference only, no code copied. |
| thefuntastic Unity Object Pool PoolManager | https://github.com/thefuntastic/unity-object-pool/blob/2d20e73abe2728633549f19e5d1ecb5ec6def00b/Assets/Scripts/PoolManager.cs | M50 GitHub code reference for simple prefab-keyed GameObject pool lookup and release. Reference only, no code copied. |
| BDFramework GameObjectPoolManager | https://github.com/yimengfan/BDFramework.Core/blob/9739f07c2a9653e391bfbdb605f37de8b5e801a2/Packages/com.popo.bdframework/Runtime/AssetsManager/ObjectPools/GameObjectPoolManager.cs | M50 GitHub code reference for prefab-to-pool and instance-to-pool maps with release deactivation. Reference only, no code copied. |
| MaskOff DeflectionHandling | https://github.com/Stat1c-Null/MaskOff/blob/77ff137c15a70fcd4846baa07589167b654e537c/Assets/Scripts/DeflectionHandling.cs | M51 GitHub code-search reference for checking attack-window state before deflecting a projectile. Current Unity migration uses typed projectile result events and settlement-owned stun authority instead of copying this Rigidbody2D script. Reference only, no code copied. |
| GitHub code search for Unity boss snipe / aimed shot / rolling hazard patterns | `gh search code "Unity projectile snipe pattern boss"`, `gh search code "Unity bullet pattern aimed shot"`, `gh search code "boss state machine attack priority"`, `gh search code "enemy attack priority state machine Unity"`, `gh search code "Unity rolling hazard projectile radius growth"`, `gh search code "Unity snowball rolling hazard damage"`, `gh search code "Unity boss snowball attack pattern"`, `gh search code "Unity boss rolling attack sphere hazard"`, `gh search code "Unity boss close range attack priority projectile"`, `gh search code "Unity boss evade close range movement target"`, `gh search code "Unity rolling ball hazard damage player"`, and `gh search code "Unity boss arena hazard bounce bounds"` | M52-M57 searches returned no useful reusable open-source implementation for the narrow ice-lance snipe, snowball-pressure priority, rolling snowball hazard, snowball boss-intent, boss movement-intent, or snowball damage/bounce slices. Current Unity migration keeps the local director/prefab/ScriptableObject/hazard/movement/combat-runtime boundary and copies no external code. |
| KenshiK Bullet-Hell PatternSequencer | https://github.com/KenshiK/Bullet-Hell/blob/eb26b52fe7bfaf0fd6e1924224bb220335c7a5b4/Assets/Scripts/Weapons/Bullet/BulletPattern/PatternSequencer.cs | M47 GitHub reference for separating pattern sequencing from individual bullet shape generation. Current PF should keep deterministic `TickPhase` tests before adopting a sequencer. Reference only, no code copied. |
| SHMUP-Swarm TelegraphableAttack | https://github.com/Xenation/SHMUP-Swarm/blob/596fdcb7b5fdb0e7ffcabd6094e4c155e70da3ff/Assets/Scripts/TelegraphableAttack.cs | M47 GitHub reference for treating telegraph as an attack state rather than only a visual object. Reference only, no code copied. |
| Unite2017 GameEvent | https://github.com/roboryantron/Unite2017/blob/59186d60af2cf1f5faf69cd45601607531ba260b/Assets/Code/Events/GameEvent.cs | M47 GitHub reference for ScriptableObject event channels. Good for phase outcome/HUD/audio events, not a replacement for scoped PF settlement authority. Reference only, no code copied. |

## Day-Night Architecture Decision

Godot `DayNightCycle3D` mixes several responsibilities:

- time-driven sun and moon light rotation
- sky shader colors and cloud parameters
- Godot `Environment` post effects such as glow, fog, SSAO, and tonemapping
- firefly particle spawning
- scene-specific overrides

Unity migration splits those responsibilities:

- `GameClock`: time/date/season rules.
- `WeatherService`: weather, visibility/movement modifiers, moon phase.
- `DayNightPalette`: period-to-color/brightness anchors from Godot.
- `DayNightLightingController`: Unity `Light`, ambient, and fog adapter.
- Future sky/weather package adapter: optional, replaceable, and kept outside
  simulation rules.

This keeps save data, NPC schedules, farming growth, fishing availability,
dialogue conditions, and lighting visuals on the same clock without making the
game dependent on one skybox/weather asset.

## Runtime UI Architecture Decision

Godot currently has several formal UI surfaces:

- Title screen
- Loading screen
- HUD
- Unified game menu
- Settings menu
- Card deck editor
- Dialogue/Rune UI
- Combat HUD

Unity migration now uses a small project-owned runtime shell:

- `MigrationGameSettings`: persistent settings state via PlayerPrefs.
- `MigrationSceneRegistry`: formal scene-selection keys, with unmigrated scenes
  listed but disabled.
- `MigrationHudController`: time/date/season/hotbar shell.
- `MigrationUnifiedMenuController`: formal tab surface for overview, character,
  inventory, journal, deck, social, codex, and settings.
- `MigrationSettingsController`: display/quality/visual/audio/scene settings.
- `RuneDialogueController`: migration-era Rune-style dialogue panel.
- `MigrationGlobalUiController`: binds HUD, menu, settings, dialogue, and scene
  loading in runtime scenes.

This shell intentionally uses IMGUI for the first migration pass because it is
fast to generate, easy to batch-test, and keeps data contracts visible. The
expected future production pass is uGUI or UI Toolkit once save/inventory/card
and dialogue data contracts stop moving.

Next formal runtime surfaces, in order:

1. Camera-facing/fading replacements for the temporary damage-number and
   reward/loot TextMesh presenters.
2. Expand the Perfect Freeze phase with production phase-outcome consumers,
   polished snowball hit/shatter/expiry/bounce presentation, player-side
   i-frame ownership, boss hover/flight-pose polish, and final boss/arena
   presentation.
3. Return-shot VFX/presentation consumers for M51 `Reflected` results on top of
   the environment-impact, graze, shatter, Perfect Freeze cycle, and settlement seams.
4. Broader pooling/lifetime ownership for non-encounter projectiles and VFX feedback templates.
5. A full fallback/source decision for the missing `vampire` source scene.
6. NavMesh-style movement, richer ranged/melee variant behavior, and Bullet3D
   special-rule parity.
7. Combat-session/rank settlement, XP gem pickup, production reward
   notifications, and combat HUD feedback.
8. Production cooking UI/timer, quest delivery UI, and daily reset wiring.
9. Meta save, settings repository, player-progress/cooking save orchestration, and
   CardBuild run store.
10. CardBuild schema-v2 validation deepening, then Cirno combat/boss HUD vertical
   slice.

## Enemy Variant Architecture Decision

Godot `Enemy3D` combines scene object behavior with MonsterDatabase application,
Beehave blackboard updates, collision damage, attack range flags, animation,
death VFX, and `SignalBus.enemy_killed` emission. Unity migration keeps this
split into smaller seams:

- `MigrationEnemyVariantProfile`: variant id, enemy type, elemental group, HP,
  display name, Godot scene path, move style, chase/attack range, move speed,
  damage, cooldown, windup, ranged flags, XP, and loot-table mode.
- `MigrationEnemyCatalog`: first code-loaded mirror of the 20 formal
  `MonsterDatabase.gd` entries.
- `MigrationSimpleEnemyController`: first Unity scene adapter for idle, chase,
  windup, melee attack, ranged attack, ranged keep-away, and defeated state.
  M50 adds an optional prefab-keyed projectile pool seam for ranged checkout
  while keeping speed/damage/combat binding owned by the controller.
- `MigrationEnemyProjectile`: first enemy-bullet path with profile damage,
  projectile speed, enemy flag, player-health routing, M35 mesh/trail feedback,
  and lifetime expiry. M36 adds reusable template application, pooling-ready
  metadata, sweep/segment anti-tunneling against the player, and impact
  ParticleSystem hooks. M37 adds Unity physics environment raycast impacts that
  stop projectiles on blocking geometry before player damage. M38 adds one-shot
  normal/perfect graze events when the player is outside hit radius but inside
  graze radius. M39 adds projectile family data, shatter HP, weakness matching,
  one-shot `Shattered` result events, and expired/visual-danger shutdown when a
  projectile shatters. M46 adds arm-delay gating so fresh Perfect Freeze bullets
  can be visible before movement, damage, graze, environment impact, or
  Perfect Freeze cycle timing becomes active. M47 keeps that gating while adding
  distinct ice-orb and ice-shard projectile families for the Perfect Freeze
  encounter cast plan. M50 makes `Configure()` stop and hide stale impact
  particles before reuse so pooled projectiles do not carry old hit feedback.
  M51 adds `reflectable`, `reflectStunReward`, and `reflectStunSeconds`
  template/runtime data plus `TryReflect()`, which consumes eligible armed
  enemy projectiles, emits a typed `MigrationProjectileReflectResult`, marks
  the projectile expired for pool reclamation, and leaves shatter HP/events
  untouched.
- `MigrationPhoenixGaugeRuntime`: M40 project-owned gauge runtime for Phoenix
  gauge values, segment thresholds, graze soft cap, hit loss, spending, and
  change events. It is pure C# game state, not a projectile component.
- `MigrationProjectileSpecialSettlement`: M40 MonoBehaviour subscriber for
  projectile `Grazed` and `Shattered` events. It grants Phoenix gauge rewards,
  tracks 3-crystal heavy-burst windows, and emits 12-frozen-crystal Perfect
  Freeze stagger readiness while keeping projectiles as event sources. M41 adds
  shared settlement forwarding for projectile-local instances and one-shot
  consumption of the pending heavy-burst radius multiplier. M42 keeps the
  stagger output here as an event only; enemy/boss control state is consumed by
  a target-side adapter. M44 adds an opt-out switch for implicit global
  forwarding so dedicated boss/arena encounters can keep their crystal streaks
  scoped to one local settlement. M45 keeps that settlement scoped while the
  encounter director owns phase/cast timing and safe-lane cue activation. M48
  keeps clear/capture/timeout phase results outside this settlement so projectile
  rewards and boss-phase rewards remain separate event streams. M51 subscribes
  to projectile `Reflected` events and emits `ReflectStunReady` only for
  reflect results that carry a stun reward.
- `MigrationPerfectFreezeStaggerAdapter`: M42 target-side consumer for the
  settlement's 12-frozen-crystal Perfect Freeze event. It binds the shared
  settlement to a `MigrationSimpleEnemyController`, records the consumed
  duration, and calls `ApplyStun(seconds)` without giving projectiles or the
  settlement direct authority over AI. M51 also lets the same target-side
  adapter consume `ReflectStunReady` results from ice-lance reflect rewards.
- `MigrationCombatFeedbackTemplate`: M36 prefab metadata seam for combat
  presentation assets. It records template kind, pooling readiness, layer policy,
  lifetime, visual radius/color, impact feedback, sweep collision flags, and
  M38 projectile graze defaults. M39 adds projectile special-rule defaults:
  projectile family, shatterable flag, shatter HP, and shatter weaknesses. M43
  adds Perfect Freeze projectile lifecycle data for spray/frozen/thawed timing,
  speed, damage, and frozen shatter HP. M46 adds template-owned arm-delay data.
  M51 adds reflect reward data for ice-lance style projectile templates.
- `MigrationPerfectFreezeProjectileFeedback.prefab`: M43 dedicated projectile
  feedback prefab for Cirno-style Perfect Freeze bullets. It keeps projectile
  lifecycle on the projectile/template layer: spray for 1.6s, freeze for 2.4s
  into a shatterable `frozen_crystal`, then thaw into fast danger. M46 starts it
  with a `0.5s` unarmed readability window before that cycle begins.
- `MigrationIceOrbProjectileFeedback.prefab` and
  `MigrationIceShardProjectileFeedback.prefab`: M47 dedicated projectile feedback
  prefabs for the non-field halves of the Perfect Freeze phase cast plan. The
  ice orb uses family `ice_orb`, Godot-like speed/damage `6.5/8`, visual radius
  `0.4`, and `0.32s` arm delay. The ice shard uses family `ice_shard`, damage
  `12`, visual radius `0.52`, and `0.42s` arm delay while the encounter director
  supplies row-based speed `10.5/11/11.5`.
- `MigrationIceLanceProjectileFeedback.prefab`: M51 dedicated projectile
  feedback prefab for Godot's reflectable Cirno `ice_lance` counterplay. It
  uses family `ice_lance`, speed/damage `22.5/16`, radius `0.24`, arm delay
  `0.62s`, and reflect stun reward `2s`. It is intentionally separate from the
  `ice_shard` fan prefab. M52 binds it into the generated Perfect Freeze
  encounter as an authored snipe projectile rather than leaving it as a passive
  standalone feedback asset.
- `MigrationPerfectFreezeSafeLaneCue`: M45 cue component for the Perfect Freeze
  safe lane. It owns renderer enable/disable timing, last lane direction, and
  Godot-intent cue color/duration, while the encounter director decides when to
  show it.
- `MigrationPerfectFreezePhasePlan`: M49 ScriptableObject data asset for the
  Perfect Freeze phase/cast/outcome values. It stores HP/duration/cadence,
  cast budget, safe-lane timing, clear/capture bonuses and stuns, and the
  ice-orb/field/ice-shard cast-plan inputs. M52 adds the `12m` ice-lance snipe
  distance gate. It is data only; it does not spawn projectiles, own settlement,
  or emit phase-result events.
- `MigrationPrefabPoolService`: M50 prefab-keyed pool owner for Unity projectile
  lifecycle reuse. It keys inactive stacks by source prefab `GameObject`, tracks
  instance-to-prefab ownership, deactivates returned objects, and exposes
  created/reused/get/released/active/inactive counters for diagnostics. It is
  generic Unity lifecycle infrastructure, not a projectile family registry.
- `MigrationPerfectFreezePhaseResult`: M48 immutable result object for a
  Perfect Freeze phase finish. It records reason, capture state, clear/capture
  bonuses, stun seconds, phase/total hit counts, capture count, elapsed time,
  duration, and next phase index so HUD, audio, rewards, camera, and arena flow
  can subscribe later without owning projectile behavior.
- `MigrationPerfectFreezeSnowballHazard`: M54 singleton arena hazard/presenter
  for the rolling snowball. It is not part of the prefab-keyed projectile pool
  or the per-cast projectile budget. It rolls toward a target, grows from seeded
  radius `0.88` toward `2.43`, expires after `5.8s`, shatters from HP `42`,
  applies `fire/heavy/shatter` counter multipliers, and drives the encounter
  director's snowball pressure active/inactive state. M55 starts it from the
  authored boss `snowball_roll` intent instead of leaving it as a manual seam.
  M56 keeps boss positioning in the encounter director by consuming the active
  hazard radius/position rather than making the hazard own boss AI. M57 binds
  or resolves `MigrationCombatRuntime`, applies real player damage with a local
  `0.75s` repeated-hit cooldown, registers a Perfect Freeze phase hit on
  successful damage, and reflects direction against the manual `34m` arena
  radius instead of using wall/terrain bounce physics.
- `MigrationPerfectFreezeEncounterDirector`: M45 boss/arena seam over the
  reusable projectile and settlement pieces. It owns the Perfect Freeze
  projectile prefab reference, scoped settlement, local boss controller,
  stagger adapter, active projectile cap 80, phase max HP 300, phase duration
  70s, cast interval 2.2s, 18 projectiles per cast, and 22 degree safe-lane
  cue timing. M47 adds explicit ice-orb/ice-shard projectile prefab references,
  zero-based cast index counters, authored/spawned family counters, and ordered
  cast-budget allocation: each timed cast first spawns up to 11 ice orbs, even
  casts spend the remaining 7 slots on Perfect Freeze field crystals and show
  the safe-lane cue, and odd casts spend the remaining 7 slots on ice shards
  without showing a new field cue. It spawns projectiles and wires their local
  settlement components into the encounter settlement, but leaves projectile
  lifecycle in `MigrationEnemyProjectile` and boss control in the target-side
  adapter. M48 adds `PhaseFinished`, `LastPhaseResult`,
  `RegisterPlayerHit`, and clear/timeout finishing: boss defeat emits
  `Reason="clear"` while zero phase hits sets `Captured=true`, and duration
  expiry emits `Reason="timeout"` with no bonus or stun. M49 adds
  `PhasePlan`, `BindPhasePlan`, and `ApplyPhasePlan`, moving the phase/cast/
  outcome constants into `MigrationPerfectFreezePhasePlan.asset` while the
  director keeps runtime authority. M50 adds `ProjectilePool`,
  `HasProjectilePool`, and `BindProjectilePool`; when a pool is bound, all
  three projectile prefabs are checked out by prefab key and expired/shattered
  active-list projectiles are returned during prune. M52 adds
  `iceLanceProjectilePrefab`, `IceLanceMinDistance`, and the exclusive
  `ice_lance_snipe` cast: if the player is at or beyond `12m`, the director
  spends the cast on one ice-lance and skips the orb/field/shard plan plus
  safe-lane cue for that cast. M53 adds `SnowballPressureActive` and
  `SetSnowballPressureActive(bool)` so snowball pressure can explicitly
  suppress far-distance snipe while active pressure owns the cast. The pressure
  branch uses `snowball_pressure`, hides the safe-lane cue, and spawns no extra
  projectile threats. M54 adds `SnowballHazard`, `HasSnowballHazard`, and
  `BindSnowballHazard(...)`, resolving a child `MigrationPerfectFreezeSnowballHazard`
  on `Awake()` so the runtime hazard owns pressure cleanup. M55 adds
  `CloseRangeDistance = 4.2` and `SnowballPreferredDistance = 8`: `(4.2m, 8m]`
  starts `snowball_roll`, `>=12m` still uses ice-lance, and `<=4.2m` avoids
  starting snowball. M56 adds `snowballPushOffset = 1.35`,
  `closeEvadeBackDistance = 2.6`, `closeEvadeSideDistance = 1.75`,
  `bossMovementLerp = 2.2`, `LastBossMovementIntentKind`, and
  `LastDesiredBossPosition`, so active snowball pressure targets a boss position
  behind the snowball while close range targets an evade retreat/sidestep. M57
  keeps capture-break authority in the director by letting the snowball call
  `RegisterPlayerHit()` only after successful player-health damage.
- `Assets/TouhouMigration/Prefabs/Encounters/MigrationPerfectFreezeEncounter.prefab`:
  M45 generated encounter prefab with a local boss target, scoped settlement,
  local `MigrationPerfectFreezeStaggerAdapter`, safe-lane cue child, and a
  director wired to `MigrationPerfectFreezeProjectileFeedback.prefab`,
  `MigrationIceOrbProjectileFeedback.prefab`, and
  `MigrationIceShardProjectileFeedback.prefab`. M48 also serializes clear bonus
  `70`, capture bonus `100`, clear stun `3.5s`, and capture stun `4.5s`. M49
  binds `MigrationPerfectFreezePhasePlan.asset` from
  `Assets/TouhouMigration/Data/Combat/PerfectFreeze`. M50 adds a local
  `MigrationPrefabPoolService` on the encounter root and binds it to the
  director. M52 binds `MigrationIceLanceProjectileFeedback.prefab` and the
  generated phase plan serializes `iceLanceMinDistance: 12`. M53 serializes
  `snowballPressureActive: 0` by default. M54 adds a
  `PerfectFreezeSnowballHazard` child with `MigrationPerfectFreezeSnowballHazard`,
  trigger sphere collider, kinematic rigidbody, inactive renderer, and a bound
  director reference. M55 serializes `closeRangeDistance: 4.2` and
  `snowballPreferredDistance: 8` on the phase plan and encounter director. M56
  serializes `snowballPushOffset: 1.35`, `closeEvadeBackDistance: 2.6`,
  `closeEvadeSideDistance: 1.75`, and `bossMovementLerp: 2.2` on the encounter
  director. M57 serializes `arenaRadius: 34` and
  `playerDamageCooldownSeconds: 0.75` on the snowball hazard. It is the first
  reusable Cirno-style phase object, not yet the final Misty Lake arena scene.
- `MigrationPlayerAttackHitbox`: M24 player attack adapter for light/heavy
  windows. M39 lets active attack windows shatter eligible projectiles once per
  window, using the normalized attack type as the projectile source family. M41
  adds temporary range-multiplier application to Box/Sphere/Capsule colliders so
  the next empowered heavy attack can widen its active hitbox and then restore
  the base shape.
- `MigrationPlayerCombatActionController`: M25 player action-window owner for
  light/heavy attacks. M41 binds a shared projectile settlement and consumes the
  3-crystal heavy-burst multiplier only when opening a heavy attack window.
- `Assets/TouhouMigration/Prefabs/Enemies/MigrationEnemy_*.prefab`: M30
  generated placeholder prefab set, one per formal catalog record.
- `MigrationEnemyVisualSource`: M31 source metadata attached to generated
  prefabs, recording Godot scene path, Unity model path, scene-referenced
  texture paths, primary texture, and fallback status.
- `Assets/TouhouMigration/Art/Enemies/<PascalId>`: M31 promoted visual source
  folder for 19 existing monster scenes, with primary model FBX, scene
  textures, and simple generated material.
- `MigrationEnemyAnimationSource`: M32 source metadata attached to generated
  prefabs, recording generated controller path, role clip paths, and fallback
  status.
- `MigrationEnemyAnimationBridge`: M33 runtime presentation adapter attached to
  19 non-fallback generated enemy prefabs. It maps `MigrationSimpleEnemyController`
  state/events to Unity Animator parameters/triggers while keeping gameplay
  state outside the Animator. M35 subscribes to target `Damaged` and `Defeated`
  events so `TakeDamage` and `Die` presentation triggers are emitted even when
  damage does not change controller locomotion state.
- `MigrationSimpleEnemyController` action phases: M34 adds gameplay-owned
  `telegraph`, `active`, and `recovery` phases around melee damage and ranged
  projectile spawn. Generated prefabs serialize short active/recovery windows so
  attacks become readable and single-shot. M35 drives melee damage-source
  visibility/collision from the active window and configures visible projectile
  feedback on spawned ranged shots. M36 adds a serialized projectile prefab seam
  so ranged variants can instantiate reusable feedback templates.
- `MigrationEnemyDamageSource`: M35 adds active-window gating for generated
  melee danger volumes. Generated melee-capable prefabs serialize inactive,
  invisible, non-colliding danger markers outside their active attack phase.
- `MigrationCombatDefeatHandler`: M34 adds configurable delayed cleanup so
  generated enemy renderers/colliders can stay alive during death presentation.
  M35 adds a simple ParticleSystem death burst during that delay and disables
  active damage sources as soon as defeat begins. M36 adds a reusable death
  feedback prefab reference while preserving the generated fallback burst.
- `MigrationCombatHurtFeedback`: M36 damage-event consumer for material flash and
  lightweight knockback hooks. It intentionally reads `Damaged` events without
  taking over damage authority.
- `MigrationDamageNumberPresenter`: M37 damage-event consumer for
  settings-gated, lightweight damage number TextMesh presentation.
- `MigrationCombatRewardPresentation`: M37 reward/loot event consumer for
  XP/coin/item notification TextMesh presentation without owning reward,
  inventory, or quest state.
- `MigrationProjectileGrazePresenter`: M38 projectile-graze event consumer for
  lightweight normal/perfect graze feedback at the player-position result. It
  does not own gauge, CardBuild, or reward settlement.
- `MigrationProjectileShatterPresenter`: M39 projectile-shatter event consumer
  for lightweight shatter feedback at the projectile result position. It does
  not own Phoenix gauge, arena streaks, boss stun, or CardBuild settlement.
- `Assets/TouhouMigration/Animations/Enemies/<PascalId>`: M32 promoted monster
  animation folder, with copied source FBXs and generated first-pass
  `<PascalId>_Enemy.controller` assets. M33 adds `IsMoving`, `MotionState`,
  `Attack`, `Projectile`, `TakeDamage`, and `Die` parameters plus basic
  locomotion/trigger transitions.
- `TouhouMigrationProjectBuilder.BuildEnemyCatalogPrefabs()`: focused editor
  regeneration entry point for the generated prefab set.
- `MigrationCombatTargetBehaviour`, `MigrationEnemyDamageSource`,
  `MigrationCombatLootDropHandler`, and reward handlers: focused adapters for
  HP, damage events, player damage, loot/objectives, and rewards.

M31 intentionally keeps catalog profiles code-loaded and generated prefabs as
the authority for this migration stage. Real monster models are now mounted by
the builder instead of hand-authored in individual prefabs, so regeneration stays
safe. A later slice should promote catalog/visual records into Unity assets or
JSON-loaded data after the animation, behavior, and material schemas are proven.

Monster FBX import policy stays separate from the Mokou humanoid import policy.
M32 configures selected monster animation clips as Generic and prefers in-place
locomotion for controller-driven movement. Root-motion clips are preserved as
copied source assets but are not selected for the first controller pass.

M33 establishes the first Unity-native enemy presentation seam. The Animator is
not the AI authority; gameplay state requests presentation, and future animation
events or StateMachineBehaviour callbacks should report timing back to gameplay
for hit windows, projectile spawn frames, hurt recovery, and death completion.
This follows Unity action-game patterns seen in Gamekit3D/Open Project-style
architectures without adopting those projects as direct dependencies.

M34 keeps that ownership model and adds timer-driven action phases as an
intermediate step. The timers make one melee and one ranged behavior testable
without waiting for authored animation events, but they are not the final combat
feel layer. M35 makes the windows visible through active damage volumes,
projectile mesh/trail feedback, target damage events, animation triggers, and a
simple death ParticleSystem burst. M36 assetizes the first layer of those seams
into reusable prefabs/templates with layer policy metadata, pooling-ready flags,
impact feedback, material flash, knockback, and fast-projectile sweep checks.
M37 adds the first readability presenters and projectile environment impact
using Unity event subscribers and physics queries, keeping Godot as gameplay
intent instead of copying its singleton VFX shape. M38 adds the first
Bullet3D-style non-damage projectile interaction, one-shot graze events and
normal/perfect graze feedback, while leaving gauge/CardBuild settlement for a
later service slice. M39 adds the first projectile special-rule interaction:
weakness-aware shatter events that player attack hitboxes can trigger without
putting arena/gauge settlement inside projectile code. M40 turns those events
into actual gameplay settlement through a separate
Phoenix gauge runtime and projectile-special subscriber. That subscriber now
owns graze/shatter gauge gains, 3-crystal heavy-burst readiness, and
12-frozen-crystal stagger readiness without moving arena rewards into projectile
objects. M41 consumes the 3-crystal heavy-burst output in the player heavy
attack path: projectile-local settlement forwards to a shared scene
settlement, the player action consumes the multiplier only for heavy attacks,
and the hitbox collider shape scales for the active window before restoring.
M42 consumes Perfect Freeze stagger through a reusable target-side stun adapter
on generated Human Village enemies. M43 adds the projectile-owned Perfect
Freeze spray/frozen/thaw cycle and a dedicated reusable projectile feedback
prefab. M44 mounts those pieces into a scoped encounter prefab/director so
Perfect Freeze crystal streaks can stun one local boss target without leaking
into unrelated scene-level adapters. M45 adds the first safe-lane cue and
phase/cast timing contract. M46 adds projectile arm-delay/readability. M47 adds
zero-based Perfect Freeze cast-plan parity with an always-first ice-orb spread,
even field casts, odd ice-shard casts, and distinct generated projectile
prefabs. M48 adds typed clear/capture/timeout phase outcome events and serialized
clear/capture bonus/stun values. M49 extracts those stable phase/cast/outcome
values into a ScriptableObject phase-plan asset. M50 adds first-pass
prefab-keyed projectile pooling for the encounter and ranged enemy checkout
seams. M51 adds the ice-lance reflect reward seam. M52 uses that prefab in an
authored distance-gated `ice_lance_snipe` cast while keeping spawning,
settlement, and pooling in existing Unity seams. M53 adds the active snowball
pressure seam that suppresses snipe before the distance gate, matching the
Godot boss-intent priority. M54 adds the first real rolling snowball
runtime/presenter as singleton arena hazard state rather than another pooled
projectile. M55 connects that hazard to the authored `(4.2m, 8m]` snowball
boss intent while preserving far ice-lance and close-range no-snowball
boundaries. M56 adds the boss movement-intent seam for active snowball
push-positioning and close-range evade. M57 makes the snowball a real arena
hazard by adding player-health damage, capture-hit registration, repeated-hit
cooldown, and manual arena-radius bounce. Next work should continue into
hover/flight-pose polish, polished snowball hit/shatter/expiry/bounce
presentation, player-side i-frame ownership, return-shot presentation,
production phase-outcome consumers, camera-facing/fading presenters, and final
combat HUD feedback.

## Dialogue Architecture Decision

Godot currently uses several dialogue layers:

- `DialogueDatabase.gd`: compact NPC JSON normalization, context selection, and
  conditions.
- `DialogueRuntime.gd`: pure active-line/choice state machine.
- `DialogueRuntimeFacade.gd`: session ids and action routing.
- `DialogicBridge.gd`: Godot/Dialogic/Rune UI adapter.
- `RuneDialogueUI.gd`: current formal Rune-style dialogue presentation.

Unity migration keeps the data and state architecture, but does not port
Dialogic internals:

- `DialogueDatabase`: loads the 35 compact `_npc_*.json` files directly.
- `DialogueRuntime`: owns active state, line advancement, choice commits, cancel,
  and finish reasons.
- `DialogueRuntimeFacade`: owns session ids and stale-choice rejection.
- `RuneDialogueController`: first Unity presenter shell bound to the facade.

This preserves formal runtime behavior while keeping Unity free to adopt Yarn
Spinner, Ink, or another authoring tool later. Those packages should be treated
as authoring/import layers, not as the core game-state contract, unless a later
slice proves a stronger reason to replace the project-owned runtime.

Known M13 limits:

- Valid-entry selection is deterministic in Unity for smoke-test stability;
  Godot chooses randomly among valid entries.
- Choice effects are surfaced as action payloads, but Bond/Quest/Shop/Inventory
  service adapters are not connected yet.
- Gift dialogue data and portrait catalog parity are the next dialogue-adjacent
  migration slices.

## Social And Gift Architecture Decision

Godot currently has two overlapping gift paths:

- `data/gifts.json` plus `GiftDatabase.gd`: richer gift definitions, tags,
  categories, NPC preferences, reaction dialogue, special events, birthdays, and
  category bonuses.
- `NPCDatabase.get_gift_reaction` plus NPC `receive_gift` methods: older live
  delivery behavior used by current NPC scripts.

Unity migration uses the richer JSON contract as the canonical data authority:

- `GiftDatabase`: loads 30 gifts, 34 NPC preference records, 34 birthdays, and
  the birthday multiplier.
- `GiftInteractionService`: validates inventory, removes the delivered item,
  computes the reaction and bond delta, routes the reaction line through the
  dialogue facade, then notifies optional Bond/Quest adapters on success.
- `MigrationGiftSelectionController`: migration-era IMGUI modal that lists
  giftable inventory, previews reaction/bond delta, and delegates delivery to
  `GiftInteractionService`.
- `SocialBondService`: M16 BondSystem foundation with Godot thresholds, source
  table, daily interaction, level queries, and snapshot/load.
- `QuestDatabase`: M16 loader for the full formal QuestData set migrated into
  Unity JSON.
- `QuestDeliveryService`: M18 QuestManager delivery foundation with quest start,
  prerequisites, progress, `craft`/`craft_tier`/`craft_stat`,
  `deliver`/`deliver_variety` matching, completion, CookingDatabase selectors,
  reward sink integration, and snapshot/load.
- `MigrationNpcInteractor`: first Human Village marker-level adapter for dialogue
  and gift-selection opening.
- `MigrationGlobalUiController`: owns the first runtime wiring between inventory,
  dialogue, and gift services.
- `BlocksGameplayInput`: global UI state used by player, HUD, and NPC input
  pollers so modal social UI cannot leak gameplay actions.

This is an intentional architecture convergence instead of a literal port of
both Godot paths. Unity should not maintain two independent gift authorities.
Future full BondEventSystem, production Quest UI, notification, and delivery UI
implementations should consume these services instead of reimplementing gift,
bond, or delivery logic.

Known M15 limits:

- `SocialBondService` now supports levels, source-table bonuses, and
  persistence, but Unity events, notifications, and BondEventSystem are not
  ported.
- `QuestDeliveryService` now supports craft/delivery objective matching,
  CookingDatabase symbolic selectors, deliver-variety uniqueness, completion,
  reward ledger grants, runtime reward sinks, journal entries, counters,
  unlocked NPC state, service events, and persistence. Daily reset has a service
  entry point but is not connected to the time system yet.
- Birthday multiplier is loaded but not applied because Godot's current birthday
  check is effectively inactive.
- `MigrationGiftSelectionController` is an IMGUI migration shell; production UI
  parity and final visual styling are pending.
- Some valid gifts, including `history_book` and `spell_card`, are not present
  in the 200-item `items.json` database. Future item parity must define whether
  gift-only ids become inventory items or remain unavailable until authored.
- Koishi and Kaguya special-case gift overrides are still data/source findings,
  not Unity runtime behavior.

## Bond And Quest Architecture Decision

Godot has independent but gift-adjacent systems:

- `BondSystem.gd`: NPC bond points/levels, source table, daily interaction, and
  local/SignalBus bond-change events.
- `QuestData.gd`: in-code quest database containing main, side, and daily
  quests.
- `QuestManager.gd`: runtime quest state, objective progress, delivery matching,
  rewards, next quest startup, and save/load.

Unity M16-M19 ports the core data/runtime contracts while keeping presentation
side effects replaceable:

- `quests.json`: project data extracted from Godot `QuestData.gd`.
- `QuestDatabase`: pure quest data loader/indexer.
- `SocialBondService`: pure BondSystem service with source+bonus semantics,
  level thresholds, daily interactions, and snapshot/load.
- `QuestDeliveryService`: pure QuestManager delivery runtime with active quests,
  completed quests, progress, delivery-variety state, counters, NPC unlocks,
  service events, journal view models, and snapshot/load.
- `QuestRewardLedger`: reward audit ledger for quest exp/coins/items.
- `QuestRewardSink`: economy side-effect adapter that applies quest rewards to
  runtime XP, coins, and inventory.
- `QuestJournalEntry`: service-backed quest list/detail data for the unified
  menu journal tab.
- `CookingDatabase`: dish classifier and recipe database used by cooking and
  craft/delivery quest selectors.
- `CookingService`: runtime cooking service for unlocks, cookware gates,
  wildcard ingredient consumption, quality results, cooking exp, and craft
  notification.
- `DialogueEffectRouter`: adapter for dialogue action payloads into Bond/Quest
  services.
- `MigrationSaveData`: slot saves now include `social_bonds`, `quests`, and
  `cooking`.

This keeps gift delivery, dialogue effects, quest UI, rewards, and future
BondEventSystem consumers on the same service contracts. Quest rewards now land
in both an audit ledger and the runtime player/inventory services. Cooking-derived
symbolic selectors such as `drink_any`, `meal_any`, `feast_any`, and
`atk_5_plus_any` are resolved through CookingDatabase metadata instead of manual
delivery tags when the database is available.

Known M27 limits:

- Quest rewards mutate runtime XP, coins, and inventory through `QuestRewardSink`,
  but player progress save/load orchestration is not yet wired into the global
  slot-save flow.
- Quest UI/journal presentation is service-backed but still an IMGUI migration
  shell.
- Cooking recipes, recipe unlocks, cookware gates, ingredient consumption, result
  quality, Bamboo Home station entry, migration-era cooking tab, cooking save
  snapshot, item-use, active dish/drink buffs, and cooking buff save snapshots
  now exist. Cooking buffs are bound to the migration player for movement and
  query-level dash/combat modifiers, and a player health runtime now consumes
  inbound cooking buff effects. M23 adds service-level combat routing for
  outgoing player attacks, incoming player damage, and one-shot target
  defeat/kill-heal. M24 adds live player attack hitbox, enemy damage-source,
  target dummy, and defeat-handler adapters plus Human Village scene wiring.
  M25 adds a player light/heavy action controller and one-shot defeat reward
  handler for XP, coins, total kills, and a generic quest counter. M26 adds
  per-kill direct inventory loot, active quest kill-objective scanning, and
  first Godot loot-table family coverage for enemy classifications, meat,
  elemental crystals, seeds, and fertilizer. M27 adds the first reusable enemy
  root/controller with idle, chase, attack, and defeated states bound to the
  existing HP, damage, loot, and reward adapters. Production UI, progress timer,
  paid cookware upgrades, animation-event action timing, full Enemy3D
  AI/navigation parity, probability/statistical loot validation, XP gem pickup,
  combat rank/session settlement, visual combat execution, and notifications
  are not ported yet.
- Inventory supports quality-aware cooked stacks; arbitrary `props` stack identity
  remains incomplete.
- Player-facing quest delivery UI does not yet remove/transfer delivered items;
  service-level `NotifyDelivery` assumes delivery has already happened.
- `ResetDailyQuests(day)` exists on the quest service, but `GameClock`/day-start
  integration is not wired.
- Kaguya unlock and bamboo-night visit tracking are not ported.
- Quest service events are C# events only; no notification UI or SignalBus-style
  bridge exists yet.
- Bond notifications and BondEventSystem are not ported.
- Dialogue choice effects route into Bond/Quest services, but entry-level gift
  `fx` are intentionally not routed to avoid double-counting gift bond changes.

## Cooking Architecture Decision

Godot cooking is split across:

- `CookingDatabase.gd`: static recipes, dish combat profiles, tier/cookware
  helpers, quality helpers, effect descriptions, and ingredient stat metadata.
- `CookingManager.gd`: unlocked recipes, cookware level, cooking exp, ingredient
  consumption, completion, quest notification, and save/load.
- `CookingUI.gd` / `CookingUI.tscn`: recipe list, details, progress timer,
  quality/attribute preview, and completion presentation.
- `CookingStation3D.gd`: a small world interaction object that opens the UI.
- `CookingBuffSystem.gd`: dish/drink consumption and combat buff effects.

Unity M18/M19/M20 keeps the same boundaries but expresses them as C# services:

- `cooking_profiles.json`: dish combat profiles extracted from Godot.
- `cooking_recipes.json`: all 40 Godot recipes extracted from Godot.
- `CookingDatabase`: pure data/index/helper layer for profiles, recipes,
  cookware gates, recipe tiers, quality thresholds, and exp thresholds.
- `CookingService`: runtime cooking state and rules; it consumes ingredients,
  grants quality result items, advances cooking exp/unlocks, and notifies quests.
- `CookingRuntimeSnapshot`: save DTO for cooking level, exp, cookware level, and
  unlocked recipes.
- `ItemUseService`: inventory item-use route for consumables, dishes, and drinks.
  It consumes the matching quality stack for dishes/drinks and delegates buff
  application to `CookingBuffService`.
- `CookingBuffService`: active dish/drink buff state and combat-query surface,
  including three dish slots, one drink slot, quality scaling, caps, thresholds,
  combos, special/drink effects, ticking/expiry, and Godot-compatible multiplier
  calculators.
- `CookingBuffRuntimeSnapshot`: save DTO for active dish slots, active drink,
  total stats, and unlocked thresholds. Unlike the current Godot load path, the
  Unity DTO preserves dish quality after reload.
- `MigrationPlayerController`: consumes cooking speed buffs in live movement and
  exposes query methods for dash cooldown/distance, attack damage, defense, jump,
  spirit charge, and special effects.
- `MigrationPlayerHealthRuntime`: owns runtime HP for the migration seam and
  consumes cooking buff damage reduction, regen, kill-heal, rebirth, hitstun, and
  attack-hit-feedback suppression rules.
- `MigrationGlobalUiController`: migration-era service owner; constructs cooking
  services, exposes the active buff/health services, and ticks buff expiry plus
  health regen during play.
- `MigrationCookingStationInteractor`: migration-era Bamboo Home entry point.
- `MigrationUnifiedMenuController`: temporary IMGUI cooking tab.

This intentionally avoids putting cooking rules inside the UI. The current
menu tab is replaceable; a future uGUI/UITK CookingUI should call the same
`CookingService`, `ItemUseService`, and `CookingBuffService` and preserve the
same tests.

Known M24 cooking/combat limits:

- Cooking completion is instant in `CookingService`; the Godot UI timer/progress
  has not been reproduced yet.
- Paid cookware upgrades are not exposed through a real UI or economy command.
- Cooking notifications are not routed through a production notification system.
- Cooking buff values are service-backed, visible in the migration menu, ticked
  during play, and consumed by `MigrationPlayerController` for live movement and
  query-level dash/attack/defense/jump/spirit modifiers.
- `MigrationPlayerHealthRuntime` now handles inbound cooking buff effects for
  damage reduction, HP regen, kill-heal, rebirth once, hitstun duration, and
  attack-hit-feedback suppression. `MigrationCombatRuntime` now routes outgoing
  player attacks, incoming player damage, and one-shot target defeat/kill-heal
  through those seams. `MigrationPlayerAttackHitbox`,
  `MigrationEnemyDamageSource`, and `MigrationCombatDefeatHandler` now provide
  the first live Unity scene adapters and are mounted into generated scene
  content. Real input/animation timing, AI enemies, dash state, tracking bullets,
  shockwaves, afterimages, loot/reward hooks, and visual special effects are
  still future slices.
- Runtime save/load orchestration still needs to call the cooking and buff
  snapshot APIs, plus synchronize runtime HP, during real new-game/load-game
  flows.
- Crop/fish ingredient stat contributions are not used beyond the existing dish
  profile data.

## Combat Architecture Decision

Godot combat behavior is currently distributed across player state scripts,
hitbox/hurtbox components, enemy health/death behavior, and autoload-level loot
or buff side effects:

- `Player3D.gd`: outgoing attack values, incoming damage, death, and action
  context.
- `PlayerDash.gd` and other player states: action timing and movement windows.
- `AttackEffectHitbox.gd`, `HitboxComponent.gd`, and `HurtboxComponent.gd`:
  collider-level attack contact.
- `HealthComponent.gd` and enemy scripts: target HP/death events.
- `LootDropManager.gd` and `CookingBuffSystem.gd`: kill-side effects such as
  healing on enemy defeat.

Important source finding from M24: the old `AttackEffectHitbox ->
HitboxComponent -> HurtboxComponent` chain still exists, but the formal 3D
combat path has already drifted toward direct contact and `take_damage` calls.
`CombatArenaHD2D.tscn` uses `Player3D` and `Enemy3D`; `Enemy3D` creates attack
range/contact logic directly and formal enemies do not consistently mount
`HurtboxComponent`. Unity therefore uses trigger/collider adapters that call the
project combat services directly instead of porting the old Godot component
chain one-to-one.

Unity M21-M24 keeps those responsibilities split instead of rebuilding one large
`Player3D` port:

- `MigrationPlayerController`: movement plus outgoing cooking-buff query surface
  for dash, attack, defense, jump, and spirit modifiers.
- `MigrationPlayerHealthRuntime`: player HP, incoming damage reduction, regen,
  kill-heal, rebirth, hitstun, and hit-feedback suppression.
- `MigrationCombatTargetRuntime`: target HP and newly-defeated state.
- `MigrationCombatTargetBehaviour`: thin scene adapter that exposes target HP and
  a one-shot `Defeated` event to Unity objects.
- `MigrationCombatRuntime`: service-level bridge that applies modified outgoing
  player damage, routes incoming player damage through health, and grants
  kill-heal once when a target is defeated.
- `MigrationPlayerAttackHitbox`: live Unity hitbox adapter with explicit attack
  windows, per-window target dedupe, trigger entry support, and combat-runtime
  routing.
- `MigrationEnemyDamageSource`: live Unity damage-source adapter for collision,
  trigger, projectile, or enemy-contact damage into the player health runtime.
- `MigrationCombatDefeatHandler`: first death side-effect adapter; disables
  target colliders/renderers once on defeat.
- `MigrationGlobalUiController`: current migration-era owner that constructs the
  shared combat runtime and exposes `FindCombatRuntime()` for scene adapters.
- `TouhouMigrationProjectBuilder`: mounts the player hitbox under generated
  players and adds a Human Village target dummy plus enemy damage-source marker.

Known M24 combat limits:

- Live combat adapters are mounted in generated scenes, but no real player
  input, action-state, or animation-event bridge opens/closes attack windows yet.
- The current Human Village target is a dummy, not a full AI enemy prefab.
- The enemy damage source is a marker/adapter, not a real enemy attack pattern or
  projectile system.
- Defeat events do not yet notify loot drops, XP/coins, quest kill counters, VFX,
  death animations, or boss HUD state.
- Heavy armor penetration exists as a cooking/action query but target armor is
  not modeled yet.
- Combat state is not yet gathered into the real slot-save orchestration.

## CardBuild Architecture Decision

Godot CardBuild has three layers:

- Data/index layer: `data/cardbuild/*.json` plus `CardBuildDatabase.gd`.
- Profile/deck layer: `CardBuildProfileStore.gd`, default Mokou deck, loadout
  validation, and user persistence.
- Combat/run layer: combat bridge, runtime state, run store, progression, rewards,
  CardCombat HUD, and boss rules.

Unity migration now ports the first two layers only:

- `CardBuildDatabase`: loads all 8 JSON files, generates archetype skeleton cards,
  indexes explicit Mokou cards, and exposes first-slice query/count APIs.
- `CardBuildProfileStore`: creates and validates the default Mokou profile, then
  persists it as JSON.
- `MokouDeckEditorController`: title-screen deck shell that proves data/profile
  can be displayed and saved.

This keeps the deck editor useful before combat exists. The next CardBuild pass
should mirror Godot schema-v2 defaults more completely: slot modes, allowed slots,
activation modes, effect-block validation, tactical/installed package splitting,
and run progression persistence.

## Save And Inventory Architecture Decision

Godot has several independent persistence surfaces:

- Slot saves: `user://saves/save_slot_<slot>.json`, managed by `SaveSystem.gd`.
- Meta progression/global save: `user://touhou_phantom_save.dat`, managed by
  `GameSaveManager.gd`.
- Settings: `user://game_settings.json`, managed by `GameSettings.gd`.
- CardBuild profiles/runs: `user://cardbuild_profiles.json` and
  `user://cardbuild_runs.json`.

Unity M12-M19 ports the slot-save, inventory/item, and cooking-save foundation:

- `ItemDatabase`: loads the full 200-item Godot `items.json`, preserving category
  mapping and max-stack rules.
- `InventoryService`: 48-slot inventory with stack splitting, removal, counts, and
  canonical snapshots. M19 adds quality-aware add/remove/count operations for
  cooked dish stacks.
- `CookingRuntimeSnapshot`: cooking level, exp, cookware level, and unlocked
  recipe state.
- `MigrationSaveService`: slot repository for `save_schema = 3` and
  `version = "3.0.0"` save files.

This does not merge Godot's separate save surfaces yet. Merging would be a
behavior change and should wait until settings, CardBuild run state, meta
progression, and load-game UX are all visible in Unity.

Known next parity work:

- Legacy inventory slot migration from `{ id, amount }` to `{ item_id, amount }`.
- arbitrary `props` stack identity.
- Item use/equip behavior for consumables, dish, drink, currency, and equipment.
- Meta save repository.
- Settings repository separate from slot save.
- Runtime save/load orchestration that gathers inventory, player progress,
  quests, bonds, cooking, scene, and position into one slot operation.
- CardBuild run store.
- Unity-safe 3D position migration from Godot's legacy `{x, y}` slot position.
