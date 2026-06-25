# Touhou Unity Migration Progress

Last updated: 2026-06-25 11:05 CST

## Working Discipline

1. Keep the Unity migration project independent at `/Users/Shared/TouhouUnityMigration`.
2. Do not modify `/Users/Shared/Touhougodot` unless a task explicitly requires reading or exporting source data.
3. Before porting gameplay code, maintain a written understanding of the formal Godot app: entry scenes, autoload systems, runtime scene registry, formal runtime scenes, and dev/test-only scenes.
4. Do not bulk-copy all Godot-native source assets. Confirmed Unity-origin source packs must be moved into the standalone Unity migration project, leaving only a migrated README in the Godot source path. This is a move/relocation discipline, not a duplicate-copy discipline: keep source packs in `ExternalUnityAssets`, then promote build/runtime content into `Assets/TouhouMigration` by migration slice.
5. Every milestone must update this file before handoff.
6. Every handoff must include current status, changed files, verification run, known blockers, and next recommended step.
7. Prefer small playable slices over broad incomplete conversion.
8. Treat Unity compile errors, missing scene assets, broken Build Settings, and failed editor batch runs as blockers.
9. Keep generated Unity cache folders (`Library`, `Temp`, `Logs`, `UserSettings`) out of versioned handoff scope.
10. Record assumptions explicitly. Do not leave important migration decisions only in chat history.
11. When using MCP or Unity Editor automation, record whether the tool connected successfully and what it changed.
12. When Unity offers a cleaner architecture than the direct Godot shape, prefer the cleaner Unity architecture and record the decision.
13. The goal is to build the game in Unity, not to mechanically copy Godot implementation shapes. Treat Godot as the source of gameplay intent and content inventory; prefer Unity-native Animator, physics, navigation, prefab, ScriptableObject, Input, and camera patterns when they make the same player-facing effect cleaner or better.

## Current Handoff Snapshot

- Date: 2026-06-25 05:04 CST
- Recommended context mode: fresh/project handoff. Start a new Codex conversation from this document plus `/Users/Shared/Touhougodot/docs/AI_AGENT_GUIDE.md`.
- Objective: build the formal Touhou game experience in an independent Unity project while preserving Godot source-traceability, using Godot as gameplay/content reference rather than a shape to copy, improving architecture where Unity has a cleaner native path, and updating this progress document at every milestone.
- Unity migration project: `/Users/Shared/TouhouUnityMigration`
- Godot source project: `/Users/Shared/Touhougodot`
- Latest completed milestone: E4.12, farming manager registers crops from the database — end-to-end (session 2). Earlier: E4.11 shop runtime (catalog/hours-gated buy/sell). Session-2 milestones (all in the Milestone Log below): E5.2-E5.9 (dialogue fx routing + story flags + live conditions: humanity/time_of_day/is_full_moon/weather/seen_events), E4.1-E4.11 (shop economy end-to-end: service/hours/catalog/runtime, farm growth + harvest loop + 67-crop catalog, fishing weighted catch + level, NPC schedules + manager), E2.4/E2.5 (world-time + menu game-state gating), E8.1/E8.2 (humanity + story-flag save). Prior milestone M58 plus the session-1 epic slices (Phase 0 / E1 / E2 / E5.1 / E4-E8) are tracked in `Docs/CURRENT_HANDOFF.md`.
- Current overall status: foundation and several vertical slices are migrated, but the full formal game is not complete yet.

Done at handoff:

- Standalone Unity project exists at `/Users/Shared/TouhouUnityMigration`.
- Original Unity-origin asset packs were moved into the Unity migration project; the Godot `assets/unity_imports` path is marker-only.
- Core migration foundations exist for scene catalog/bootstrap, time/day-night planning, save/inventory/cooking/quest/social/dialogue/buff/combat bridge smoke coverage, player health/buffs, UI smoke checks, and content smoke checks.
- Enemy catalog slice is current:
  - 20 formal `MonsterDatabase` profiles represented by generated Unity prefabs.
  - 19 formal monster scenes have real imported FBX/texture visual children.
  - 220 monster animation FBX files imported under `Assets/TouhouMigration/Animations/Enemies`.
  - 19 Generic AnimatorControllers generated with basic Unity parameters/transitions and bound to the imported enemy visual models.
  - 19 non-fallback generated enemy prefabs now carry a `MigrationEnemyAnimationBridge` bound to `MigrationSimpleEnemyController` and the visual `Animator`.
  - 20 generated enemy prefabs now serialize readable attack timing: telegraph, active window, recovery window, and delayed defeat cleanup.
  - Melee-capable generated enemies now use active-window danger volumes: collider/renderer disabled outside the active phase, enabled only while the attack can hurt the player.
  - Enemy projectiles now create visible Unity mesh/trail feedback and have a configurable lifetime/expiry seam.
  - Combat targets now emit `Damaged` feedback events; the runtime animation bridge triggers `TakeDamage` on non-lethal and lethal hits before death.
  - Generated enemies now serialize a simple ParticleSystem death-feedback burst that plays during the defeat delay before renderer/collider cleanup.
  - M36 adds reusable CombatFeedback prefabs for enemy projectile, melee danger, and death burst presentation.
  - Ranged enemies now serialize a projectile feedback prefab reference; death handlers serialize a reusable death feedback prefab reference.
  - Projectiles can apply feedback templates, expose pooling-ready ownership, perform segment/sweep checks for fast movement, and spawn impact ParticleSystem feedback on hit.
  - Generated enemies now carry `MigrationCombatHurtFeedback` for material flash and lightweight knockback hooks when `Damaged` fires.
  - M37 adds settings-gated damage-number presentation through `MigrationDamageNumberPresenter`.
  - Reward and loot handlers now emit one-shot presentation events after their existing one-shot grant logic; `MigrationCombatRewardPresentation` consumes those events without owning XP, coins, inventory, or quest progress.
  - Enemy projectiles now support Unity physics environment raycast impacts, stop on blocking geometry before player damage, and reuse the impact ParticleSystem hook for wall hits.
  - Generated enemy prefabs and Human Village training enemies now carry damage-number and reward/loot presentation seams.
  - M38 adds Bullet3D-style projectile graze detection: enemy projectiles can fire one-shot `Grazed` events when the player is outside hit radius but inside graze radius, and can distinguish `normal` from `perfect` graze quality.
  - The reusable enemy projectile feedback prefab now serializes graze defaults and carries `MigrationProjectileGrazePresenter`.
  - M39 adds the first Bullet3D-style projectile special rule beyond graze: shatterable projectile family data, weakness-aware shatter HP, one-shot `Shattered` result events, and lightweight shatter presentation.
  - Player attack hitboxes can now shatter eligible projectiles once per attack window, using the attack type as the projectile's source family.
  - The reusable enemy projectile feedback prefab now serializes default special-rule family data and carries `MigrationProjectileShatterPresenter`; default generated enemy bullets remain not globally shatterable.
  - M40 adds a Unity-native Phoenix gauge runtime matching the Godot gauge intent: 300 max, 100-point segments, 50 start, 45 graze-per-second soft cap, and 20-point hit loss.
  - Projectile graze and shatter events now feed a separate `MigrationProjectileSpecialSettlement` subscriber, so projectiles remain event sources and arena/player reward authority stays outside projectile code.
  - Settlement grants Cirno-style gauge rewards: +2 normal graze, +5 dash graze, +8 perfect dash graze, and +12 for shatter families such as ice wall, ice crystal, frozen crystal, and snowball.
  - Settlement tracks 3 ice-crystal shatters as a pending 1.25x heavy-burst radius multiplier and 12 frozen-crystal shatters as a 1.2s Perfect Freeze stagger event.
  - The runtime HUD now shows the Phoenix gauge, and the reusable enemy projectile feedback prefab now carries the settlement seam with Godot-like reward defaults.
  - M41 consumes the 3-crystal heavy-burst output in the real Unity player heavy attack path.
  - `MigrationPlayerCombatActionController` can bind the projectile settlement and consumes `PendingHeavyBurstRadiusMultiplier` only when opening a heavy attack window; light attacks leave the reward pending.
  - `MigrationPlayerAttackHitbox` now applies a temporary range multiplier to Box/Sphere/Capsule colliders during the active attack window, then restores base collider dimensions when the window closes.
  - Projectile-local settlement components can forward graze/shatter settlement to a shared scene-level settlement, so three separate projectile instances can aggregate into one heavy-burst reward.
  - Bamboo Home and Human Village generated scenes now carry a shared `MigrationProjectileSpecialSettlement` on `MigrationGlobalUI`, and generated player action controllers serialize a reference to it.
  - M42 consumes the 12-frozen-crystal Perfect Freeze output in live Unity gameplay through a target-side `MigrationPerfectFreezeStaggerAdapter`.
  - `MigrationSimpleEnemyController` now owns a timed stun state with `ApplyStun`, `IsStunned`, `StunRemainingSeconds`, and `StunEventCount`; stun cancels current action windows, blocks movement/attack, returns to idle on expiry, and resumes AI on the next tick.
  - Human Village generated enemies now serialize Perfect Freeze stagger adapters bound to the shared projectile settlement, so projectiles stay event sources and enemies/bosses own their own control state.
  - `MigrationEnemyAnimationBridge` maps `stunned` to a stationary `TakeDamage` reaction using the existing generated Animator trigger path instead of treating stun as ordinary idle.
  - M43 adds a Unity-native Perfect Freeze projectile lifecycle: projectiles can spray, freeze into shatterable `frozen_crystal`, and thaw back into fast danger without putting settlement or boss stun logic inside the projectile.
  - `MigrationCombatFeedbackTemplate` now serializes Perfect Freeze cycle data, and generated combat feedback prefabs include a dedicated `MigrationPerfectFreezeProjectileFeedback.prefab` with 6s lifetime plus Godot-like 1.6s spray / 2.4s freeze / 8m/s thaw values.
  - Default reusable enemy projectile feedback keeps Perfect Freeze cycle disabled, preserving ordinary enemy bullet behavior.
  - M44 mounts the Perfect Freeze projectile/stun chain into a dedicated Unity encounter seam through `MigrationPerfectFreezeEncounterDirector`.
  - `MigrationProjectileSpecialSettlement` now supports encounter-scoped settlement by disabling implicit global-settlement fallback, so a boss arena can own its own frozen-crystal streaks without stunning every adapter bound to the scene-level settlement.
  - Generated encounter prefabs now include `MigrationPerfectFreezeEncounter.prefab` with a local boss target, local stagger adapter, scoped settlement, active projectile cap 80, burst count 12, and a reference to `MigrationPerfectFreezeProjectileFeedback.prefab`.
  - M45 expands that seam into the first playable Perfect Freeze phase contract: boss HP 300, duration 70s, pattern interval 2.2s, 18 projectiles per cast, active cap 80, and immediate first cast on phase start.
  - `MigrationPerfectFreezeSafeLaneCue` now provides a Unity-native warm strip cue with Godot-intent values: 22 degree half-angle, approximately 2.69m generated strip width, 1.05s duration, and RGBA `(1, 0.54, 0.18, 0.3)`.
  - `MigrationPerfectFreezeEncounterDirector` owns phase/cast cadence and safe-lane cue activation, while projectile lifecycle, shatter settlement, and boss stun remain in their existing Unity-native seams.
- M46 adds a real projectile arm-delay/readability gate to `MigrationEnemyProjectile`: unarmed projectiles can be visible, but do not move, damage, graze, or advance Perfect Freeze spray/freeze timers until the delay expires.
- `MigrationCombatFeedbackTemplate` now serializes `armDelaySeconds`; the generated `MigrationPerfectFreezeProjectileFeedback.prefab` preserves the Godot Perfect Freeze crystal value `0.5s` and starts with `isArmed: 0`.
- M47 turns the Perfect Freeze phase into a Unity-native cast planner instead of a single migrated burst: every timed cast first spends budget on an 11-shot `ice_orb` spread, zero-based even casts add a Perfect Freeze field with a safe-lane cue, and zero-based odd casts add an `ice_shard` fan without reusing the field cue.
- Generated combat feedback now includes separate `MigrationIceOrbProjectileFeedback.prefab` and `MigrationIceShardProjectileFeedback.prefab` assets with their own projectile families and arm-delay values, while the encounter prefab binds all three projectile families explicitly.
- M48 adds Unity-native Perfect Freeze phase outcome events:
  - `MigrationPerfectFreezePhaseResult` records `Reason`, `Captured`, clear/capture bonuses, stun seconds, phase/total hit counts, capture count, elapsed time, duration, phase index, and next phase index.
  - `MigrationPerfectFreezeEncounterDirector` now subscribes to local boss-target defeat, emits `PhaseFinished`, stores `LastPhaseResult`, exposes `RegisterPlayerHit`, and cleanly ends the phase on clear or timeout without continuing timed casts.
  - Clear without a registered player hit is represented as `Reason="clear"` plus `Captured=true`, preserving the Godot player-facing semantics while giving Unity HUD/audio/reward systems a typed result object instead of a Godot-shaped callback chain.
  - The generated encounter prefab serializes Perfect Freeze outcome values: clear bonus `70`, capture bonus `100`, clear stun `3.5s`, and capture stun `4.5s`.
- M49 extracts the hardcoded Perfect Freeze phase/cast/outcome numbers into a Unity `ScriptableObject` asset:
  - `MigrationPerfectFreezePhasePlan` stores phase HP/duration/cadence/budget, safe-lane cue values, clear/capture bonuses and stuns, and the 11/82, 2x12, 3x6/68 cast-plan inputs.
  - `MigrationPerfectFreezeEncounterDirector` now exposes `PhasePlan`, `HasPhasePlan`, `BindPhasePlan`, and `ApplyPhasePlan`; the director remains the owner of projectile spawning, budget enforcement, scoped settlement wiring, cue activation, and phase-finish events.
  - `TouhouMigrationProjectBuilder` now generates `Assets/TouhouMigration/Data/Combat/PerfectFreeze/MigrationPerfectFreezePhasePlan.asset` and binds it into `MigrationPerfectFreezeEncounter.prefab`.
- M50 adds the first real Unity-native prefab-keyed projectile pool:
  - `MigrationPrefabPoolService` owns prefab-keyed checkout/release, active/inactive counts, and instance-to-prefab tracking instead of relying on tag or family names.
  - `MigrationEnemyProjectile.Configure()` now stops and hides stale impact `ParticleSystem` children before reuse, so pooled projectiles do not carry old hit visuals into the next shot.
  - `MigrationPerfectFreezeEncounterDirector` can bind a projectile pool, checks out all three Perfect Freeze projectile prefabs through that pool, and returns expired or shattered active-list projectiles during prune.
  - `MigrationSimpleEnemyController` can bind the same pool seam for ordinary ranged projectile checkout while preserving variant speed/damage and combat binding.
  - `MigrationPerfectFreezeEncounter.prefab` now serializes a local `MigrationPrefabPoolService` on the encounter root.
- M51 adds the first Bullet3D reflect reward seam:
  - Godot `ice_lance` reflect metadata is represented as Unity prefab/template data, not copied as Godot metadata.
  - `MigrationProjectileReflectResult` carries projectile family, source family, hit position, reflected direction, speed, damage, stun reward flag, stun seconds, and source object.
  - `MigrationEnemyProjectile` now serializes `reflectable`, `reflectStunReward`, and `reflectStunSeconds`; `TryReflect()` consumes eligible armed projectiles, emits `Reflected`, marks them expired for pool reclamation, and does not touch shatter HP/events.
  - `MigrationPlayerAttackHitbox` attempts reflect before shatter, using the attack type as source family and counting reflect separately from shatter.
  - `MigrationProjectileSpecialSettlement` subscribes to reflect events and emits `ReflectStunReady`; `MigrationPerfectFreezeStaggerAdapter` can apply that reflect stun to its bound enemy controller.
  - `TouhouMigrationProjectBuilder` now generates `MigrationIceLanceProjectileFeedback.prefab` with Godot-like ice-lance values: speed `22.5`, damage `16`, hit radius `0.24`, arm delay `0.62s`, reflect stun reward `2s`, and family `ice_lance`.
- M52 turns that reflectable ice-lance data into an authored Unity encounter cast:
  - `MigrationPerfectFreezePhasePlan` now serializes the ice-lance snipe distance gate at `12m` alongside the existing phase/cast values.
  - `MigrationPerfectFreezeEncounterDirector` now binds a distinct `iceLanceProjectilePrefab` and, when the player is at or beyond `12m`, casts the exclusive `ice_lance_snipe` pattern: one ice-lance, no ice-orb spread, no Perfect Freeze field, no ice-shard fan, and no safe-lane cue.
  - The spawned lance uses the Godot-intent contract in Unity terms: family `ice_lance`, speed `22.5`, damage `16`, hit radius `0.24`, `0.62s` arm delay, spawn offset `0.55m` in front of the boss, and a `2s` scoped reflect stun reward.
  - The generated `MigrationPerfectFreezeEncounter.prefab` now binds `MigrationIceLanceProjectileFeedback.prefab` directly, so the authored cast survives full project regeneration.
  - Future snowball work must preserve the Godot intent that active snowball pressure suppresses ice-lance snipe instead of layering both threats at once.
- M53 adds the snowball/lance intent arbitration seam:
  - `MigrationPerfectFreezeEncounterDirector` now exposes `SnowballPressureActive` and `SetSnowballPressureActive(bool)` for the rolling snowball runtime to drive.
  - When snowball pressure is active, the director chooses `snowball_pressure` before the far-distance `ice_lance_snipe` gate, consumes the cast cadence, hides the safe-lane cue, and spawns no extra orb/field/shard/ice-lance projectiles.
  - Clearing snowball pressure restores the M52 far-distance ice-lance snipe on the next eligible cast.
  - The generated `MigrationPerfectFreezeEncounter.prefab` serializes `snowballPressureActive: 0`, so the authored snipe remains enabled by default unless a runtime snowball explicitly owns pressure.
  - This is a Unity-native boss-intent input seam; M54 binds the first runtime snowball hazard to it.
- M54 adds the first Unity-native rolling snowball hazard that drives the M53 pressure seam:
  - `MigrationPerfectFreezeSnowballHazard` is a singleton arena hazard/presenter, not a pooled projectile-budget entry.
  - It uses Godot-intent values in Unity terms: speed `4.2`, damage `16`, spawn-forward offset `2.4m`, seeded initial radius `0.88`, max radius `2.43`, growth `0.18/s`, duration `5.8s`, shatter HP `42`, and weak families `fire,heavy,shatter`.
  - Beginning a roll calls `SetSnowballPressureActive(true)`; expiry, shatter, disable, and destroy all clear pressure so the M52 ice-lance snipe can resume.
  - Counter damage applies weak-family multipliers before shatter; M57 now adds real player-damage routing and arena-radius bounce, leaving polished hit/bounce/shatter presentation for later.
  - The generated `MigrationPerfectFreezeEncounter.prefab` now includes a `PerfectFreezeSnowballHazard` child with sphere renderer/collider/rigidbody, bound to the encounter director and inactive until a roll begins.
- M55 connects the authored boss intent to the M54 snowball hazard:
  - `MigrationPerfectFreezePhasePlan` and `MigrationPerfectFreezeEncounterDirector` now serialize Godot's close/snowball distance gates: close range `4.2m`, snowball preferred distance `8m`, and ice-lance distance `12m`.
  - The director starts `snowball_roll` only in the Godot middle band `(4.2m, 8m]`, calls `MigrationPerfectFreezeSnowballHazard.BeginRolling(center, playerPosition, seed)`, hides the safe-lane cue, and spawns no ordinary projectile instances.
  - Far distance still chooses `ice_lance_snipe`, active snowball still chooses `snowball_pressure`, and close range no longer incorrectly starts a snowball; M56 turns that close band into a movement target instead of a cast fallback.
  - Timed cast tests that are meant to exercise normal Perfect Freeze field/fan behavior now use a `10m` player distance so they do not accidentally sit on the Godot snowball threshold.
- M56 adds a Unity-native boss movement-intent seam for the Perfect Freeze director:
  - Active snowball pressure now drives `LastBossMovementIntentKind="snowball_push_position"` and computes a desired boss target behind the rolling snowball by `snowball.Radius + snowballPushOffset`, preserving the Godot `1.35m` push offset as gameplay intent while keeping the movement authority in the Unity encounter director.
  - Close range without an active snowball now drives `LastBossMovementIntentKind="evade_close"` and computes a desired boss target that backs away by `2.6m` plus a `1.75m` sidestep, so the `<=4.2m` band is now a real movement seam instead of a no-snowball fallthrough.
  - `TickPhase()` consumes that desired movement through a configurable `bossMovementLerp = 2.2`; tests assert relative direction/distance rather than locking future navigation or animation work to one fixed left/right coordinate.
  - This keeps `MigrationSimpleEnemyController` generic and leaves Cirno-specific positioning inside `MigrationPerfectFreezeEncounterDirector`, where it can later be replaced by NavMesh, root motion, or richer flight pose presentation without moving ordinary enemy AI.
- M57 turns the rolling snowball into a real Unity arena hazard:
  - `MigrationPerfectFreezeSnowballHazard` now binds or resolves `MigrationCombatRuntime` and routes player damage through `ApplyDamageToPlayer(16)`, so the real player health runtime, cooking damage reduction, and rebirth rules stay authoritative.
  - A successful snowball hit calls `MigrationPerfectFreezeEncounterDirector.RegisterPlayerHit()`, breaking Perfect Freeze capture eligibility through the same typed phase-result seam as other player hits.
  - Snowball hits now apply a `0.75s` player-hit cooldown matching the Godot Cirno arena's player-side i-frame intent; this is intentionally local to the snowball hazard until the full Unity player i-frame system is migrated.
  - The snowball now has an arena center/radius seam with Godot's `34m` default, reflects direction off the horizontal arena normal, exposes `BounceEventCount` / `LastBounceNormal`, and still ignores wall/terrain physics for bounce just like the Godot source.
  - The generated encounter prefab now serializes `arenaRadius: 34` and `playerDamageCooldownSeconds: 0.75` on `PerfectFreezeSnowballHazard`.
- M58 adds the first production consumer of the Perfect Freeze phase outcome: `MigrationPerfectFreezeOutcomePresenter` subscribes to `MigrationPerfectFreezeEncounterDirector.PhaseFinished` and shows capture/clear/timeout plus bonus/stun summary text via lightweight `TextMesh` children, granting no rewards and owning no settlement.
  - `TouhouMigrationProjectBuilder.CreatePerfectFreezeEncounterPrefab` now adds the presenter on the encounter root, co-located with the director, so it auto-binds to `PhaseFinished` at runtime through its `OnEnable` `GetComponent` resolve.
  - Verified TDD red→green (`PerfectFreezeEncounterSmokeTests.RunAll`) plus green regressions: `EnemyProjectileSpecialRulesSmokeTests`, `EnemyProjectilePerfectFreezeCycleSmokeTests`, `ProjectileSettlementStaggerSmokeTests`, `CombatBridgeSmokeTests`.
- `vampire` remains explicit fallback because the formal Godot source scene is missing.

Evidence:

- M57 TDD red runs failed as expected before implementation:
  - first red run failed on missing `BindCombatRuntime`, `ConfigureArena`, `TryDamagePlayer`, `PlayerDamageEventCount`, `Direction`, `BounceEventCount`, and `LastBounceNormal`.
  - second red run failed on missing `PlayerDamageCooldownSeconds` and `PlayerDamageCooldownRemainingSeconds`, after the subagent confirmed Godot's `0.75s` player-side Cirno i-frame.
  - green tests prove snowball damage routes through `MigrationCombatRuntime`, reduces real `MigrationPlayerHealthRuntime` HP by `16`, increments `PlayerDamageEventCount`, and registers one Perfect Freeze phase hit.
  - green tests prove immediate repeated hits are blocked by the `0.75s` cooldown and do not apply damage or add extra phase hits; after cooldown expiry, another hit applies once.
  - green tests prove arena boundary crossing reflects direction away from the boundary and records the bounce normal.
- M57 read-only subagent review confirmed Godot truth: `SnowballHazardArea` calls player `take_damage(16, snowball)`, the real Cirno arena uses `Player3D` with a `0.75s` hit invincibility timer, arena bounce is manual radius-based `velocity.bounce(normal)` around boss home position, and there is no wall/StaticBody snowball bounce path.
- M57 GitHub/code-search pass did not identify a useful reusable open-source rolling-ball hazard damage/boss arena bounce implementation for this narrow slice; the project keeps the local Unity hazard/director/combat-runtime boundary and copies no external code.
- Focused M57 Perfect Freeze encounter smoke tests passed after damage/bounce and cooldown implementation, before and after full project regeneration.
- M57 adjacent regressions passed: `EnemyProjectileSpecialRulesSmokeTests`, `EnemyProjectilePerfectFreezeCycleSmokeTests`, `ProjectileSettlementStaggerSmokeTests`, and `CombatBridgeSmokeTests`.
- `TouhouMigrationProjectBuilder.BuildInitialProject` completed successfully after M57 regenerated the encounter prefab with snowball arena/damage cooldown values.
- M56 TDD red run failed as expected before implementation:
  - red run failed on missing `SnowballPushOffset`, `ConfigureBossMovement`, `LastBossMovementIntentKind`, and `LastDesiredBossPosition`.
  - green tests prove active snowball pressure produces `snowball_push_position`, places the desired boss target behind the snowball by `radius + 1.35`, and moves the bound boss transform through `TickPhase()`.
  - green tests prove close range without an active snowball produces `evade_close`, backs the desired target away from the player lane by `2.6`, adds a `1.75` sidestep, and moves the bound boss transform through the same seam.
  - final test assertions validate relative direction and distance rather than a fixed left/right coordinate, preserving room for Unity-native NavMesh/root-motion/flight-pose upgrades.
- M56 GitHub/code-search pass did not identify a useful reusable open-source boss close-range evade / snowball push-position implementation for this narrow slice; the project keeps the local Unity director movement seam and copies no external code.
- M56 read-only subagent review confirmed the Godot movement priority and consumption order: active snowball positioning wins before close evade, close evade uses `4.2m` plus `2.6/1.75` offsets, and Godot consumes desired movement with a `delta * 2.2` lerp plus separate hover/pose presentation.
- Focused M56 Perfect Freeze encounter smoke test passed before and after full project regeneration.
- M56 adjacent regressions passed: `EnemyProjectileSpecialRulesSmokeTests`, `EnemyProjectilePerfectFreezeCycleSmokeTests`, `ProjectileSettlementStaggerSmokeTests`, and `EnemyAnimationBridgeSmokeTests`.
- `TouhouMigrationProjectBuilder.BuildInitialProject` completed successfully after M56 regenerated the encounter prefab with boss movement parameters.
- M55 TDD red run failed as expected before implementation:
  - red run failed on missing `SnowballPreferredDistance` in the phase plan/director and the missing nine-argument cast-plan configuration.
  - green tests prove a player at `8m` starts `LastCastPatternKind="snowball_roll"`, activates the bound snowball hazard, sets pressure active, spawns zero orb/field/shard/lance projectiles, and leaves the safe-lane cue hidden.
  - green tests prove an already-active snowball remains `snowball_pressure` on the next cadence tick instead of duplicating the hazard.
  - green tests prove close range (`3m`) does not start snowball pressure, preserving Godot's `<= 4.2m` evade-close boundary for a future seam.
  - generated prefab and phase-plan serialization prove `closeRangeDistance: 4.2` and `snowballPreferredDistance: 8`.
- M55 GitHub/code-search pass did not identify a useful reusable open-source boss snowball / rolling hazard / close-range priority implementation for this slice; the project keeps the local Unity director/phase-plan/hazard boundary and copies no external code.
- M55 subagent review confirmed the Godot intent priority: active snowball pressure, close-range evade at `<=4.2m`, far ice-lance at `>=12m`, and push-snowball only for `(4.2m, 8m]` without an already-active duplicate.
- Focused M55 Perfect Freeze encounter smoke test passed before and after full project regeneration.
- M55 adjacent regressions passed: `EnemyProjectileSpecialRulesSmokeTests`, `EnemyProjectilePerfectFreezeCycleSmokeTests`, and `ProjectileSettlementStaggerSmokeTests`.
- `TouhouMigrationProjectBuilder.BuildInitialProject` completed successfully after M55 regenerated the phase plan and encounter prefab with close/snowball distance gates.
- M54 TDD red run failed as expected before implementation:
  - red run failed on missing `MigrationPerfectFreezeSnowballHazard`, `HasSnowballHazard`, `SnowballHazard`, and `BindSnowballHazard`.
  - green tests prove begin/roll/grow/expire/shatter behavior, pressure activation/cleanup, weak-family counter multipliers, radius/position/scale progression, and generated prefab wiring/defaults.
  - generated prefab serialization proves `snowballHazard` is bound to `PerfectFreezeSnowballHazard` with speed `4.2`, damage `16`, duration `5.8`, initial shatter HP `42`, and default inactive presentation.
- M54 GitHub/code-search pass did not identify a useful reusable open-source rolling snowball/hazard implementation for this slice; the project keeps the local Unity hazard/prefab/director boundary and copies no external code.
- M54 subagent review confirmed the Godot snowball is a singleton arena pressure hazard, not an ordinary bullet pool member; pressure must clear on shatter, expiry, disable, or destroy.
- Focused M54 Perfect Freeze encounter smoke test passed before and after full project regeneration.
- M54 adjacent regressions passed: `EnemyProjectileSpecialRulesSmokeTests`, `EnemyProjectilePerfectFreezeCycleSmokeTests`, and `ProjectileSettlementStaggerSmokeTests`.
- `TouhouMigrationProjectBuilder.BuildInitialProject` completed successfully after M54 regenerated the encounter prefab with the bound snowball hazard.
- M53 TDD red run failed as expected before implementation:
  - red run failed on missing `SetSnowballPressureActive(bool)` and `SnowballPressureActive`.
  - green tests prove active snowball pressure at `16m` chooses `LastCastPatternKind="snowball_pressure"`, spawns zero ice-lance/orb/field/shard projectiles, does not show the safe-lane cue, and prevents stacking a far-distance ice-lance on top of ongoing snowball pressure.
  - green tests prove clearing snowball pressure restores the far-distance `ice_lance_snipe` cast on the next eligible cadence tick.
  - generated prefab serialization proves `snowballPressureActive: 0` by default.
- M53 GitHub/code-search pass did not identify a better reusable open-source boss-intent priority implementation for this narrow slice; the project keeps its existing director/prefab/ScriptableObject architecture and copies no external code.
- M53 subagent review confirmed the Godot ordering: active snowball is evaluated before `distance >= ice_lance_min_distance`, so a far player still stays in snowball pressure instead of getting sniped.
- Focused M53 Perfect Freeze encounter smoke test passed before and after full project regeneration.
- M53 adjacent regressions passed: `EnemyProjectileSpecialRulesSmokeTests`, `EnemyProjectilePerfectFreezeCycleSmokeTests`, and `ProjectileSettlementStaggerSmokeTests`.
- `TouhouMigrationProjectBuilder.BuildInitialProject` completed successfully after M53 regenerated the encounter prefab with default inactive snowball pressure.
- M52 TDD red run failed as expected before implementation:
  - red run failed on missing ice-lance encounter APIs such as `HasIceLanceProjectilePrefab`, `IceLanceProjectilePrefab`, `BindIceLanceProjectilePrefab`, `IceLanceMinDistance`, `LastCastIceLanceProjectileCount`, and the eight-argument cast-plan configuration.
  - green tests prove a far player position (`16m`) selects `LastCastPatternKind="ice_lance_snipe"` and spawns exactly one ice-lance while leaving orb/field/shard counts at zero and not showing the safe-lane cue.
  - green tests prove the lance carries speed `22.5`, damage `16`, arm delay `0.62s`, reflectability, reflect stun `2s`, and the forward spawn offset, then reflects through the encounter-scoped settlement to stun only the local boss.
  - builder tests prove the generated phase plan preserves the `12m` snipe gate and the generated encounter prefab references the distinct ice-lance projectile prefab.
- M52 GitHub/code-search pass did not identify a better reusable open-source snipe-pattern implementation for this slice; the Unity-native director/prefab/ScriptableObject shape remains the clearest fit.
- M52 subagent review confirmed the Godot player-facing contract and the important future snowball rule: active snowball pressure must suppress ice-lance snipe rather than layering both.
- Focused M52 Perfect Freeze encounter smoke test passed before and after full project regeneration.
- M52 adjacent regressions passed: `EnemyProjectileSpecialRulesSmokeTests`, `EnemyProjectilePerfectFreezeCycleSmokeTests`, and `ProjectileSettlementStaggerSmokeTests`.
- `TouhouMigrationProjectBuilder.BuildInitialProject` completed successfully after M52 regenerated the phase plan and encounter prefab with the ice-lance binding.
- M51 TDD red run failed as expected before implementation:
  - red run failed on missing `ConfigureTemplate` reflect arguments, `Reflectable`, `ConfigureReflectRules`, `MigrationProjectileReflectResult`, `Reflected`, `TryReflect`, reflect event counters, and hitbox reflect counters.
  - green tests prove reflectable ice-lance projectiles consume into an expired reflected state, emit one result with family/source/stun/damage data, do not emit shatter events, and cannot reflect twice.
  - green tests prove player attack hitboxes reflect eligible projectiles before shatter and keep reflect/shatter counts separate.
  - green tests prove settlement emits one reflect stun seam and the target-side stun adapter can apply the resulting `2s` stun to an enemy controller.
  - builder tests prove the generated `MigrationIceLanceProjectileFeedback.prefab` is distinct from `ice_shard` and carries `reflectable: 1`, `reflectStunReward: 1`, and `reflectStunSeconds: 2`.
- Focused M51 reflect smoke tests passed: `EnemyProjectileSpecialRulesSmokeTests`.
- M51 adjacent regressions passed: `ProjectileSettlementStaggerSmokeTests`, `EnemyProjectilePerfectFreezeCycleSmokeTests`, and `PerfectFreezeEncounterSmokeTests`.
- `TouhouMigrationProjectBuilder.BuildInitialProject` completed successfully after M51 generated the ice-lance feedback prefab.
- M50 TDD red run failed as expected before implementation:
  - red run failed on missing `MigrationPrefabPoolService`, `BindProjectilePool`, `HasProjectilePool`, and `ProjectilePool`.
  - green tests prove same-prefab reuse, different-prefab isolation, release-to-inactive behavior, reuse spawn-position application, projectile gameplay reset after `Configure`, and stale impact particle shutdown.
  - Perfect Freeze tests prove the director creates 18 opening projectiles, returns expired active-list projectiles to the pool, reuses the 11 ice-orb instances on the second cast, creates only 7 new ice-shard instances, and reuses existing ice-orb plus field instances on the third cast.
  - ranged enemy tests prove ordinary enemy projectile checkout can use the same prefab-keyed pool seam while preserving bat projectile speed `8` and damage `12`.
- Focused M50 pool smoke tests passed: `EnemyCombatFeedbackTemplateSmokeTests`, `EnemyActionTimingSmokeTests`, and `PerfectFreezeEncounterSmokeTests`.
- M50 adjacent regressions passed: `EnemyProjectilePerfectFreezeCycleSmokeTests`, `EnemyProjectileGrazeSmokeTests`, `ProjectileSpecialSettlementSmokeTests`, `ProjectileSettlementStaggerSmokeTests`, and `ProjectileSettlementConsumptionSmokeTests`.
- `TouhouMigrationProjectBuilder.BuildInitialProject` completed successfully after M50 generated the encounter prefab with its local pool component.
- M49 TDD red run failed as expected before implementation:
  - red run failed on missing `MigrationPerfectFreezePhasePlan`, `HasPhasePlan`, `PhasePlan`, and `BindPhasePlan`.
  - green tests prove a runtime-bound phase plan overwrites stale director phase/cast/outcome values, then still produces the existing budgeted `perfect_freeze_field` opening cast and `ice_shard_fan` second cast.
  - builder tests prove the generated encounter prefab references the generated plan asset and that the plan asset serializes the M45-M48 values.
- Focused M49 Perfect Freeze encounter smoke test passed after implementation and again after full project regeneration.
- M49 adjacent regressions passed: `EnemyProjectilePerfectFreezeCycleSmokeTests`, `EnemyProjectileGrazeSmokeTests`, `ProjectileSpecialSettlementSmokeTests`, and `ProjectileSettlementStaggerSmokeTests`.
- `TouhouMigrationProjectBuilder.BuildInitialProject` completed successfully after M49 generated the phase plan asset and rewired the encounter prefab.
- M48 TDD red run failed as expected before implementation:
  - red run failed on missing phase outcome APIs: `MigrationPerfectFreezePhaseResult`, `PhaseFinished`, `LastPhaseResult`, `PhaseFinishedEventCount`, `RegisterPlayerHit`, `TotalPlayerHitCount`, `ConfigurePhaseOutcomes`, and serialized outcome properties.
  - green tests prove no-hit boss defeat emits `Reason="clear"`, `Captured=true`, bonuses `70+100`, stun `4.5s`, and stops future casts.
  - green tests prove a registered player hit breaks capture while preserving clear bonus `70`, stun `3.5s`, and hit counts.
  - green tests prove `70s` timeout emits `Reason="timeout"` with no bonuses/stun and stops the active phase.
- Focused M48 Perfect Freeze encounter smoke test passed after implementation and again after full project regeneration.
- M48 adjacent regressions passed: `EnemyProjectilePerfectFreezeCycleSmokeTests`, `EnemyProjectileGrazeSmokeTests`, `ProjectileSpecialSettlementSmokeTests`, and `ProjectileSettlementStaggerSmokeTests`.
- `TouhouMigrationProjectBuilder.BuildInitialProject` completed successfully after M48 serialized the phase outcome values on the encounter prefab.
- M47 TDD red run failed as expected before implementation:
  - red run failed on missing `MigrationPerfectFreezeEncounterDirector` APIs for ice-orb/ice-shard prefab binding, cast-plan configuration, zero-based cast index, pattern kind, and per-family spawned counts.
  - first green attempt caught a stale M44/M46 burst assertion: Perfect Freeze projectiles now require the `0.5s` arm-delay window before the `1.6s` spray timer can freeze.
- Focused M47 Perfect Freeze encounter smoke test passed after implementation and again after full project regeneration.
- M47 adjacent regressions passed: `EnemyProjectilePerfectFreezeCycleSmokeTests`, `EnemyProjectileGrazeSmokeTests`, `ProjectileSpecialSettlementSmokeTests`, and `ProjectileSettlementStaggerSmokeTests`.
- `TouhouMigrationProjectBuilder.BuildInitialProject` completed successfully after M47 generated the ice-orb/ice-shard feedback prefabs and rewired the encounter prefab.
- M46 TDD red run failed as expected before implementation:
  - red run failed on missing `MigrationEnemyProjectile.ConfigureArmDelay`, `IsArmed`, `ArmDelaySeconds`, `ArmDelayRemainingSeconds`, and `MigrationCombatFeedbackTemplate.ArmDelaySeconds`.
  - first green attempt caught a test setup radius issue; second green attempt caught prefab runtime state not being serialized, so the generated prefab now records `isArmed: 0` and `armDelayRemainingSeconds: 0.5`.
- Focused M46 Perfect Freeze projectile cycle smoke test passed after implementation and again after full project regeneration.
- M46 adjacent regressions passed: `PerfectFreezeEncounterSmokeTests`, `EnemyProjectileGrazeSmokeTests`, `ProjectileSpecialSettlementSmokeTests`, and `ProjectileSettlementStaggerSmokeTests`.
- `TouhouMigrationProjectBuilder.BuildInitialProject` completed successfully after the M46 wiring.
- M45 TDD red runs failed as expected before implementation:
  - first red run failed on missing `MigrationPerfectFreezeSafeLaneCue` plus director phase/cue APIs such as `ConfigurePhase`, `BindBossTarget`, `BindSafeLaneCue`, `BeginPerfectFreezePhase`, `TickPhase`, and cast/cue counters.
  - follow-up red run caught the generated safe-lane cue color mismatch, preserving the Godot albedo color instead of a brighter migration placeholder.
- Focused M45 Perfect Freeze encounter smoke test passed after implementation, after color correction, and after full project regeneration.
- M45 adjacent regressions passed: `EnemyProjectilePerfectFreezeCycleSmokeTests`, `ProjectileSettlementStaggerSmokeTests`, `ProjectileSpecialSettlementSmokeTests`, and `ProjectileSettlementConsumptionSmokeTests`.
- M44 TDD red run failed as expected before implementation:
  - red run failed on missing `MigrationProjectileSpecialSettlement.ConfigureSharedSettlementFallback`, `UsesSharedSettlementFallback`, missing `MigrationPerfectFreezeEncounterDirector`, and missing generated `MigrationPerfectFreezeEncounter.prefab`.
- Focused M44 Perfect Freeze encounter smoke test passed after implementation and again after full project regeneration.
- M43 TDD red runs failed as expected before implementation:
  - first red run failed on missing `ConfigurePerfectFreezeCycle`, `PerfectFreezeCycleEnabled`, `CurrentPerfectFreezeState`, `IsFrozen`, template cycle fields, and the generated Perfect Freeze projectile prefab.
  - first implementation red run caught the exact `4s` lifetime / `1.6+2.4s` thaw threshold collision, so Perfect Freeze cycle now extends lifetime to at least spray + freeze + 1 second.
- Focused M43 Perfect Freeze projectile cycle smoke test passed after implementation and after full project regeneration.
- M42 TDD red runs failed as expected before implementation:
  - first red run failed on missing `MigrationPerfectFreezeStaggerAdapter`, `MigrationSimpleEnemyController.IsStunned`, and `StunRemainingSeconds`.
  - animation bridge red run failed because `stunned` mapped to `Idle` instead of a stationary hit reaction.
- Focused M42 projectile settlement stagger smoke test passed after implementation and again after full project regeneration.
- M41 TDD red runs failed as expected before implementation:
  - first red run failed on missing `BindProjectileSettlement`, `CurrentRangeMultiplier`, `LastHeavyBurstRadiusMultiplier`, and `HeavyBurstConsumeCount`.
  - second red run failed on missing shared-settlement forwarding (`BindSharedSettlement`) and saved scene binding (`HasProjectileSettlement`).
- Focused M41 projectile settlement consumption smoke test passed after implementation and again after full project regeneration.
- M40 projectile settlement, M39 projectile shatter, and M38 projectile graze smoke tests still pass after M41.
- Player/combat regressions passed: `CombatActionRewardSmokeTests` and `CombatBridgeSmokeTests`.
- Global UI regression passed after adding a shared scene settlement: `GlobalUiSmokeTests`.
- Reward/loot adjacent regressions passed: `CombatActionRewardSmokeTests` and `CombatLootQuestSmokeTests`.
- Enemy adjacent regressions passed: `EnemyPrefabSmokeTests`, `EnemyCatalogPrefabSmokeTests`, and `CombatBridgeSmokeTests`.
- `TouhouMigrationProjectBuilder.BuildInitialProject` completed successfully after the M45 wiring.
- Local asset count at handoff:
  - monster animation FBX files: `220`.
  - generated enemy AnimatorControllers: `19`.
  - generated enemy prefabs with `MigrationEnemyAnimationBridge`: `19`.
  - generated enemy prefabs with serialized `attackActiveSeconds: 0.12`: `20`.
  - generated enemy prefabs with serialized `defeatDelaySeconds: 0.45`: `20`.
- Representative current serialization check:
  - `Assets/TouhouMigration/Prefabs/Encounters` contains `MigrationPerfectFreezeEncounter.prefab`.
  - `MigrationPerfectFreezeEncounter.prefab` serializes `useSharedSettlementFallback: 0` on its scoped settlement.
  - `MigrationPerfectFreezeEncounter.prefab` serializes `MigrationPerfectFreezeEncounterDirector` with `projectilePrefab` pointing at `MigrationPerfectFreezeProjectileFeedback.prefab`, `iceOrbProjectilePrefab` pointing at `MigrationIceOrbProjectileFeedback.prefab`, `iceShardProjectilePrefab` pointing at `MigrationIceShardProjectileFeedback.prefab`, `iceLanceProjectilePrefab` pointing at `MigrationIceLanceProjectileFeedback.prefab`, `scopedSettlement`, `bossController`, and `staggerAdapter` references populated.
  - `MigrationPerfectFreezeEncounter.prefab` serializes `activeProjectileCap: 80` and `burstProjectileCount: 12`.
  - `MigrationPerfectFreezeEncounter.prefab` serializes M45 phase values: `phaseMaxHp: 300`, `phaseDurationSeconds: 70`, `patternIntervalSeconds: 2.2`, `maxProjectilesPerCast: 18`, `safeLaneHalfAngleDegrees: 22`, `safeLaneCueDurationSeconds: 1.05`, and `safeLaneCueColor: {r: 1, g: 0.54, b: 0.18, a: 0.3}`.
  - `MigrationPerfectFreezeEncounter.prefab` serializes M48 phase outcome values: `phaseClearBonus: 70`, `phaseCaptureBonus: 100`, `phaseClearStunSeconds: 3.5`, and `phaseCaptureStunSeconds: 4.5`.
  - `MigrationPerfectFreezeEncounter.prefab` serializes M49 `phasePlan` to `MigrationPerfectFreezePhasePlan.asset`.
  - `MigrationPerfectFreezeEncounter.prefab` serializes M50 `projectilePool` to a local `MigrationPrefabPoolService` component on the encounter root.
  - `MigrationPerfectFreezePhasePlan.asset` serializes phase values `300/70/2.2/18/22/1.05`, outcome values `70/100/3.5/4.5`, cast-plan values `11/82`, `2x12`, `3x6/68`, plus M52/M55 gates `iceLanceMinDistance: 12`, `snowballPreferredDistance: 8`, and `closeRangeDistance: 4.2`.
  - `MigrationPerfectFreezeEncounter.prefab` serializes M47/M52/M53/M54/M55/M56/M57 cast-plan, intent, boss-movement, and snowball-hazard values: `perfectFreezeOrbSpreadProjectileCount: 11`, `perfectFreezeOrbSpreadDegrees: 82`, `perfectFreezeFieldRingCount: 2`, `perfectFreezeFieldBulletsPerRing: 12`, `iceShardFanRowCount: 3`, `iceShardFanColumnCount: 6`, `iceShardFanSpreadDegrees: 68`, `closeRangeDistance: 4.2`, `iceLanceMinDistance: 12`, `snowballPreferredDistance: 8`, `snowballPushOffset: 1.35`, `closeEvadeBackDistance: 2.6`, `closeEvadeSideDistance: 1.75`, `bossMovementLerp: 2.2`, default `snowballPressureActive: 0`, and a bound `snowballHazard`.
  - `MigrationPerfectFreezeEncounter.prefab` contains `PerfectFreezeSnowballHazard` with `MigrationPerfectFreezeSnowballHazard`, speed `4.2`, damage `16`, durationSeconds `5.8`, initialShatterHp `42`, arenaRadius `34`, playerDamageCooldownSeconds `0.75`, and an inactive renderer until a roll begins.
  - `MigrationPerfectFreezeEncounter.prefab` contains `PerfectFreezeSafeLaneCue` with `MigrationPerfectFreezeSafeLaneCue`, local scale `x: 2.6928349`, `y: 0.025`, `z: 8`, and matching cue color serialization.
  - `Assets/TouhouMigration/Prefabs/CombatFeedback` contains `MigrationEnemyProjectileFeedback.prefab`, `MigrationIceOrbProjectileFeedback.prefab`, `MigrationIceShardProjectileFeedback.prefab`, `MigrationIceLanceProjectileFeedback.prefab`, `MigrationPerfectFreezeProjectileFeedback.prefab`, `MigrationMeleeDangerFeedback.prefab`, and `MigrationEnemyDeathFeedback.prefab`.
  - `MigrationEnemyProjectileFeedback.prefab` contains `templateKind: enemy_projectile`, `poolingReady: 1`, `impactFeedbackEnabled: 1`, `sweepCollisionEnabled: 1`, `grazeEnabled: 1`, `grazeRadius: 1.15`, `perfectGrazeRadius: 0.7`, `projectileFamily: enemy_projectile`, `shatterable: 0`, `shatterHp: 0`, and `shatterWeaknesses`.
  - `MigrationEnemyProjectileFeedback.prefab` serializes `reflectable: 0`, preserving default enemy projectile behavior.
  - `MigrationEnemyProjectileFeedback.prefab` also contains `MigrationProjectileGrazePresenter`, `MigrationProjectileShatterPresenter`, and `MigrationProjectileSpecialSettlement`.
  - `MigrationEnemyProjectileFeedback.prefab` serializes `normalGrazeGauge: 2`, `dashGrazeGauge: 5`, `perfectDashGrazeGauge: 8`, `shatterGauge: 12`, `perfectFreezeStaggerBreaks: 12`, and `perfectFreezeStaggerSeconds: 1.2`.
  - `MigrationEnemyProjectileFeedback.prefab` serializes `perfectFreezeCycleEnabled: 0`, preserving normal projectile behavior.
  - `MigrationPerfectFreezeProjectileFeedback.prefab` serializes `templateKind: perfect_freeze_projectile`, `lifetimeSeconds: 6`, `projectileFamily: frozen_crystal`, `perfectFreezeCycleEnabled: 1`, `perfectFreezeSpraySeconds: 1.6`, `perfectFreezeFreezeSeconds: 2.4`, `perfectFreezeSpraySpeed: 4.2`, `perfectFreezeSprayDamage: 8`, `perfectFreezeFrozenDamage: 7`, `perfectFreezeThawSpeed: 8`, `perfectFreezeThawDamage: 10`, and `perfectFreezeFrozenShatterHp: 20`.
  - `MigrationPerfectFreezeProjectileFeedback.prefab` serializes M46 arm-delay data on both template/runtime components: `armDelaySeconds: 0.5`, `isArmed: 0`, and `armDelayRemainingSeconds: 0.5`.
  - `MigrationIceOrbProjectileFeedback.prefab` serializes `projectileFamily: ice_orb` and `armDelaySeconds: 0.32` on both template/runtime components.
  - `MigrationIceShardProjectileFeedback.prefab` serializes `projectileFamily: ice_shard` and `armDelaySeconds: 0.42` on both template/runtime components.
  - `MigrationIceLanceProjectileFeedback.prefab` serializes `projectileFamily: ice_lance`, `speed: 22.5`, `damage: 16`, `hitRadius: 0.24`, `armDelaySeconds: 0.62`, `reflectable: 1`, `reflectStunReward: 1`, and `reflectStunSeconds: 2` on template/runtime components.
  - `BambooHomeVerticalSlice.unity` and `HumanVillageVerticalSlice.unity` contain a scene-level `MigrationProjectileSpecialSettlement` on `MigrationGlobalUI` with reward defaults `2/5/8/12`.
  - generated player action controllers in Bamboo Home and Human Village serialize `projectileSettlement` references to the shared scene settlement.
  - generated player attack hitboxes serialize `rangeMultiplier: 1` as the neutral baseline.
  - `HumanVillageVerticalSlice.unity` contains two `MigrationPerfectFreezeStaggerAdapter` components on generated scene enemies; both serialize `settlement` to the shared `MigrationGlobalUI` settlement and `enemyController` to their local `MigrationSimpleEnemyController`. M51 extends the same adapter to consume `ReflectStunReady` results as timed enemy stun.
  - `MigrationEnemy_Bat.prefab` contains `deathFeedbackEnabled: 1`, `requiresActiveWindow: 1`, `visibleWhenInactive: 0`, `windowActive: 0`, `attackActiveSeconds: 0.12`, and `defeatDelaySeconds: 0.45`.
  - `MigrationEnemy_Bat.prefab` also contains serialized `projectilePrefab`, `deathFeedbackPrefab`, `MigrationCombatHurtFeedback`, `MigrationDamageNumberPresenter`, and `MigrationCombatRewardPresentation`.
  - `HumanVillageVerticalSlice.unity` contains projectile/death feedback prefab references plus hurt, damage-number, and reward/loot presentation components for generated scene enemies.
- Unity process check at 2026-06-25 05:04 CST: no real `Unity.app/Contents/MacOS/Unity` process remained after batch commands.
- Godot git status at handoff: `/Users/Shared/Touhougodot/assets/unity_imports/README_MIGRATED_TO_UNITY.md` is modified, and three unrelated `.png.import` files under `subprojects/ue project rebuild/artifacts` are untracked. This M57 slice did not edit the Godot source tree.

Important files for the next worker:

- Progress and strategy:
  - `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
  - `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
  - `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- Unity builder:
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- Runtime enemy foundation:
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyCatalog.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyVariantProfile.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationSimpleEnemyController.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatTargetBehaviour.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyDamageSource.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyProjectile.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPrefabPoolService.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyVisualSource.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyAnimationSource.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyAnimationBridge.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatDefeatHandler.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatFeedbackTemplate.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatHurtFeedback.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationDamageNumberPresenter.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatRewardPresentation.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileGrazeResult.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileGrazePresenter.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileShatterResult.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileReflectResult.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileShatterPresenter.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPhoenixGaugeRuntime.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileSpecialSettlement.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeStaggerAdapter.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeEncounterDirector.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeSnowballHazard.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezePhasePlan.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezePhaseResult.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeSafeLaneCue.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPlayerAttackHitbox.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPlayerCombatActionController.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/CombatFeedback`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Encounters`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Data/Combat/PerfectFreeze`
- Enemy tests:
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyPrefabSmokeTests.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyCatalogPrefabSmokeTests.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyVisualPrefabSmokeTests.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyAnimationPrefabSmokeTests.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyAnimationBridgeSmokeTests.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyActionTimingSmokeTests.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyCombatFeedbackSmokeTests.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyCombatFeedbackTemplateSmokeTests.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyCombatReadabilitySmokeTests.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyProjectileGrazeSmokeTests.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyProjectileSpecialRulesSmokeTests.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyProjectilePerfectFreezeCycleSmokeTests.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/PerfectFreezeEncounterSmokeTests.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/ProjectileSpecialSettlementSmokeTests.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/ProjectileSettlementConsumptionSmokeTests.cs`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/ProjectileSettlementStaggerSmokeTests.cs`

Known hazards:

- Do not run multiple Unity batch/editor commands in parallel for `/Users/Shared/TouhouUnityMigration`; Unity project access must stay serial.
- `MigrationEnemy_*.prefab` files are generated by `TouhouMigrationProjectBuilder`; hand edits can be overwritten.
- M57 keeps gameplay-owned telegraph/active/recovery timing, visible danger windows, projectile trails, damage events, reusable feedback prefabs, projectile sweep/environment-impact hooks, projectile graze, projectile shatter, ice-lance reflect reward events, authored ice-lance snipe casts, snowball/lance intent arbitration, a singleton rolling snowball hazard started by boss intent, boss push-behind-snowball movement intent, close-range evade movement intent, snowball player-damage routing, snowball capture-hit registration, snowball arena-radius bounce, Perfect Freeze projectile lifecycle, Perfect Freeze projectile arm-delay, player-hitbox shatter/reflect routing, Phoenix gauge settlement, one-shot heavy-burst range consumption, Perfect Freeze/reflect timed stun, a scoped Perfect Freeze encounter seam with a data-driven phase plan, safe-lane cue, zero-based cast-plan parity, typed phase outcome events, and prefab-keyed projectile pooling, but action windows are still timer-driven from `MigrationSimpleEnemyController`, not authored animation events or StateMachineBehaviour callbacks.
- `MigrationEnemyProjectile` now has reusable template data, pooling-ready flags, player segment/sweep checks, environment raycast impact, one-shot normal/perfect graze events, weakness-aware shatter HP/result events, ice-lance reflect rules/result events, Perfect Freeze spray/frozen/thaw state transitions, arm-delay gating, and impact particles. M50 stops stale impact particles during `Configure()` for reuse; M51 resets reflect event state during `Configure()` so pooled projectiles do not carry stale reflect results.
- `MigrationProjectileSpecialSettlement` now exposes Cirno-style settlement outputs, M41 consumes heavy-burst range rewards through player heavy attacks, M42 consumes the 1.2s Perfect Freeze stagger event through a target-side stun adapter, M44 adds opt-out global forwarding for scoped boss/arena settlements, and M51 forwards reflect stun rewards through `ReflectStunReady`. Dash state still needs to be bound to the real player dash/runtime instead of test-controlled `SetPlayerDashing`.
- `MigrationPerfectFreezeEncounterDirector` now scopes the reusable Perfect Freeze projectile/stun chain to one encounter prefab and owns the first Perfect Freeze phase/cast/safe-lane/outcome contract. M47 adds zero-based odd/even cast parity and distinct ice-orb/ice-shard projectile prefabs; M48 adds clear/capture/timeout result emission; M49 extracts phase/cast/outcome numbers into `MigrationPerfectFreezePhasePlan.asset`; M50 adds prefab-keyed checkout and prune-time release for expired/shattered projectiles; M51 adds the ice-lance reflect prefab and generic reflect settlement; M52 adds the authored distance-gated `ice_lance_snipe` cast; M53 adds the active snowball pressure gate that suppresses far-distance snipe while pressure owns the cast; M54 binds a singleton snowball hazard runtime/presenter to that gate; M55 starts that hazard from the authored `(4.2m, 8m]` boss snowball intent; M56 adds the boss movement-intent seam for active-snowball push positioning and close-range evade; M57 registers snowball player hits against capture eligibility. The director still lacks camera lock-on, production arena mounting, hover/flight pose presentation, NavMesh/root-motion movement polish, and final HUD/audio/reward consumers for the result event.
- `MigrationPerfectFreezeSnowballHazard` is now a playable rolling arena hazard. It rolls, grows, expires, shatters, clears pressure, presents a Unity sphere hazard, bounces from an arena radius, routes player damage through `MigrationCombatRuntime`, applies a `0.75s` hit cooldown, and lets M56 make the boss desired target follow behind it while pressure is active. It still needs polished snow/crack presentation, camera/audio feedback, final player i-frame ownership on the player runtime instead of the hazard-local cooldown, and richer boss animation so the push reads as an authored attack.
- `RegisterPlayerHit()` is the Unity seam for breaking Perfect Freeze capture eligibility. It should be called from the real player-damage/health pipeline after damage lands, not from raw projectile overlap, matching the Godot source semantics.
- All adapters bound to the shared scene settlement still receive that shared scene event by design. Dedicated encounters should continue to use scoped settlements, as M44 does, when only one boss/arena target should be affected.
- `MigrationPlayerAttackHitbox` scales collider dimensions for the empowered heavy window, but this is still a first gameplay seam; it does not yet drive final VFX scale, camera/audio emphasis, or animation-event-authored hit frames.
- `MigrationCombatDefeatHandler` can use a reusable death-feedback prefab, but there is still no animation-finished callback, corpse fade, pooled despawn policy, or physical XP gem/pickup routing.
- `MigrationDamageNumberPresenter`, `MigrationCombatRewardPresentation`, `MigrationProjectileGrazePresenter`, and `MigrationProjectileShatterPresenter` are lightweight TextMesh presenters for readability. They are not final combat HUD/UI art, not pooled, and do not yet billboard toward the camera.
- `MigrationCombatHurtFeedback` now does material flash and lightweight knockback, but hit pause, camera/audio feedback, and enemy-specific material override policy remain future work.
- Root-motion `W Root` monster clips are copied but intentionally not selected for the first controller-driven movement pass.
- `Egg` animation mapping is intentionally partial and does not borrow `Egglet@*` clips.
- GitHub/gh CLI access was checked during M40 through `agent-reach doctor`; if more internet research is needed, run `agent-reach check-update` first and keep source-code references as references only unless their license is compatible.

Next recommended milestone:

- M58: keep turning the Cirno/Perfect Freeze boss slice into production Unity architecture rather than adding more Godot-shaped one-offs.
- First step: either attach production HUD/audio/reward/camera consumers to the M48 `PhaseFinished` event, or move the temporary snowball-local hit cooldown into a broader player i-frame/combat feedback seam.
- Likely deliverables:
  - hover/flight pose, look-at, and animation presentation for the boss movement intent seam.
  - Unity HUD/audio/reward consumers for the M48 `PhaseFinished` result event.
  - production feedback for `ReflectStunReady` and `MigrationPerfectFreezePhaseResult`.
  - polished snowball shatter/expiry/hit VFX, audio, and camera feedback.
  - player-side i-frame ownership for repeated hazard hits, replacing the M57 snowball-local cooldown once the full player damage runtime is ready.
  - camera-facing/fading damage, reward, and graze presenters or a world-space UI replacement.
  - wider pool return/despawn ownership for non-encounter projectiles and VFX feedback.
  - death feedback upgraded from a simple burst to fade/corpse/despawn presentation.
  - focused tests plus existing enemy/reward/graze regressions.
- Stopping condition for M58: the boss slice gains production phase-outcome consumers, player-side i-frame ownership, or polished boss/snowball presentation without breaking `BuildInitialProject` regeneration.

## Milestone Log

### E4.12: Farming Manager Registers Crops From The Database (end-to-end)

- Date: 2026-06-25 11:05 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: Connect the crop catalog (E4.9) to the farming loop (E4.4): a real crop is plantable by id -> grows -> harvests, symmetric to the shop end-to-end (E4.11).

Completed:

- `MigrationFarmingManager.RegisterCropsFrom(MigrationCropDatabase)` registers every crop from a loaded catalog so crops can be planted by their id.

TDD (red -> green):

- Extended `FarmingManagerSmokeTests`: load crops.json into a `MigrationCropDatabase`, `RegisterCropsFrom` into a manager, plant `crop_turnip` by id, water + advance 3 days, harvest -> produce `turnip`; an unknown crop id does not plant.
- RED: focused run failed to compile on the missing `RegisterCropsFrom` (CS1061).
- GREEN: full regression 57/57 suites passed, 0 compile errors (extended in place).

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/Farming/MigrationFarmingManager.cs`
- `Assets/TouhouMigration/Scripts/Editor/Tests/FarmingManagerSmokeTests.cs`

Known follow-ups:

- Water/quality yield scaling + `multi_harvest` from crops.json; a Farm scene + day-loop driving `AdvanceDay` on the E2 clock.

### E4.11: Shop Runtime — Catalog/Hours-Gated Buy/Sell (end-to-end)

- Date: 2026-06-25 11:01 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: Connect the shop economy end-to-end (E4.1 service + E4.3 hours + E4.10 catalog): a runtime shop that gates buy/sell on the shop's open hours + catalog, using the per-shop price / buy_rate.

Completed:

- New `MigrationShop` (`Runtime/Economy/`): `Buy(itemId, qty, currentHour)` -> `shop_closed` outside hours / `not_for_sale` if not in the catalog, else buys at the shop's catalog price; `Sell(itemId, qty, currentHour)` -> `shop_closed` outside hours, else sells at the shop's `buy_rate`. Composes `MigrationShopService` (no duplication).
- `MigrationShopService` gained a `Buy(itemId, qty, unitPrice)` overload (buy at an explicit price); the existing `Buy(itemId, qty)` delegates to it with the item's base price (behavior preserved).

TDD (red -> green):

- New `MigrationShopSmokeTests` (buys at the shop's catalog price when open; fails `shop_closed` after hours / `not_for_sale` for unstocked items with no coins spent; sells when open, fails `shop_closed` after hours).
- RED: focused run failed to compile on the missing `MigrationShop` (CS0246).
- GREEN: full regression 57/57 suites passed, 0 compile errors (56 prior + new shop-runtime suite); `ShopServiceSmokeTests` confirms the `Buy` refactor preserved behavior.

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/Economy/MigrationShop.cs` (new)
- `Assets/TouhouMigration/Scripts/Runtime/Economy/MigrationShopService.cs` (Buy overload)
- `Assets/TouhouMigration/Scripts/Editor/Tests/MigrationShopSmokeTests.cs` (new)

Known follow-ups:

- Build a `MigrationShop` from `MigrationShopDatabase` + an NPC interaction / shop UI; stock decrement; seasonal/festival items; read the E2 clock hour.

### E4.10: Shop Database (real shops.json catalog loader)

- Date: 2026-06-25 10:55 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: Wire real shop content into the economy — promote Godot `data/shops.json` (7 shops) and load it into shop definitions (items/prices, buy_rate, open hours, owner) the shop service can use.

Completed:

- Promoted Godot `data/shops.json` → `Assets/TouhouMigration/Data/Shops/shops.json` (7 shops, verbatim).
- New `MigrationShopDatabase` (`Runtime/Economy/`): `LoadFromPath` parses `shop_owner_npc_ids` + `shops` via `MigrationJson` into shop definitions. `GetShop` / `GetAllShops` / `ShopCount` / `Errors`.
- New `MigrationShopDefinition` (ShopId, OwnerNpcId, BuyRate, OpenHourStart/End, Items) with `GetItemPrice(itemId)`, `SellsItem(itemId)`, `IsOpen(hour)` (via the shared `MigrationHourRange`). New `MigrationShopItem` (ItemId / Price / Stock).

TDD (red -> green):

- New `ShopDatabaseSmokeTests` (loads 7 shops; `nitori_combat` owner = `nitori`; unknown shop -> null; `town_general` buy_rate 0.5, sells `seed_tomato`@50, not `sword_steel`, open 6-20 / closed at 22).
- RED: focused run failed to compile on the missing shop types (CS0246).
- GREEN: full regression 56/56 suites passed, 0 compile errors (55 prior + new shop-database suite).

Changed files:

- `Assets/TouhouMigration/Data/Shops/shops.json` (new, promoted from Godot)
- `Assets/TouhouMigration/Scripts/Runtime/Economy/MigrationShopDatabase.cs` (new)
- `Assets/TouhouMigration/Scripts/Runtime/Economy/MigrationShopDefinition.cs` (new)
- `Assets/TouhouMigration/Scripts/Runtime/Economy/MigrationShopItem.cs` (new)
- `Assets/TouhouMigration/Scripts/Editor/Tests/ShopDatabaseSmokeTests.cs` (new)

Known follow-ups:

- Wire the shop DB into `MigrationShopService` (gate Buy on the shop's catalog/stock + `IsOpen`; Sell with the per-shop `buy_rate`); seasonal/festival items; shop UI.

### E4.9: Crop Database (real crops.json catalog loader)

- Date: 2026-06-25 10:49 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: Wire real crop content into farming — promote Godot `data/crops.json` (67 crops) and load it into `MigrationCropDefinition` entries a `MigrationFarmingManager` can register.

Completed:

- Promoted Godot `data/crops.json` → `Assets/TouhouMigration/Data/Farming/crops.json` (67 crops, verbatim).
- New `MigrationCropDatabase` (`Runtime/Farming/`): `LoadFromPath` parses `{"crops": {...}}` via the shared `MigrationJson`, mapping `growth_days` from the JSON; the harvest item id strips the `crop_` prefix (+ `HARVEST_ITEM_OVERRIDES`, e.g. `crop_pepper`→`chili`); needs-daily-water defaults true; yield 1 for now. `GetCrop` / `GetAllCrops` / `CropCount` / `Errors`. Mirrors the `ItemDatabase` loader (reuses `MigrationJson`, `GetInt`, `ResolvePath`).

TDD (red -> green):

- New `CropDatabaseSmokeTests` (loads 60+ crops; `crop_turnip` growth_days 3 + needs water; unknown crop -> null; harvest id strips the `crop_` prefix + the `crop_pepper`->`chili` override).
- RED: focused run failed to compile on the missing `MigrationCropDatabase` (CS0246).
- GREEN: full regression 55/55 suites passed, 0 compile errors (54 prior + new crop-database suite).

Changed files:

- `Assets/TouhouMigration/Data/Farming/crops.json` (new, promoted from Godot)
- `Assets/TouhouMigration/Scripts/Runtime/Farming/MigrationCropDatabase.cs` (new)
- `Assets/TouhouMigration/Scripts/Editor/Tests/CropDatabaseSmokeTests.cs` (new)

Known follow-ups:

- Map the remaining crops.json fields (per-crop season, `multi_harvest`/`harvest_count`, `base_price`) and derive real min/max yield; register the database into a `MigrationFarmingManager` + a Farm scene; water/quality yield scaling (Godot `_calculate_harvest_yield`).

### E4.8: NPC Manager (registry + LocationOf)

- Date: 2026-06-25 10:40 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: The NPC-registry layer over E4.7 schedules (Godot `NPCScheduleManager`) — register NPCs with a schedule + home and resolve where each NPC is at a given hour.

Completed:

- New `MigrationNpcManager` (`Runtime/Social/`): `RegisterNpc(npcId, schedule, homeLocation)`, `IsRegistered(npcId)`, `LocationOf(npcId, hour)` -> the NPC's schedule location or home fallback (empty string for an unknown NPC).

TDD (red -> green):

- New `NpcManagerSmokeTests` (a registered NPC resolves location by hour incl. the home fallback; an unknown NPC resolves to empty).
- RED: focused run failed to compile on the missing `MigrationNpcManager` (CS0246).
- GREEN: full regression 54/54 suites passed, 0 compile errors (53 prior + new NPC-manager suite).

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/Social/MigrationNpcManager.cs` (new)
- `Assets/TouhouMigration/Scripts/Editor/Tests/NpcManagerSmokeTests.cs` (new)

Known follow-ups:

- NPC schedule data for the 35 NPCs (load `NPCScheduleData` / JSON into the manager); drive `LocationOf` on the E2 clock hour to place NPCs in scenes (E3); activity/sprite per schedule entry.

### E4.7: NPC Schedule (location by hour) + shared MigrationHourRange

- Date: 2026-06-25 10:36 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: NPC daily schedules (Godot `NPCScheduleManager`): resolve an NPC's location from its schedule + the current hour. Also DRY the wrap-around hour-range logic shared with shop hours.

Completed:

- New `MigrationHourRange.Contains(start, end, hour)` (`Runtime/Foundation/`): inclusive-start / exclusive-end with wrap-around (Godot `is_shop_open` / `_is_hour_in_range`). `MigrationShopHours.IsOpen` (E4.3) now delegates to it — single source of truth for the hour-range rule.
- New `MigrationNpcScheduleEntry` (start / end / location) + `MigrationNpcSchedule.LocationAt(hour, homeLocation)` (`Runtime/Social/`): the first matching entry's location, else the home fallback.

TDD (red -> green):

- New `NpcScheduleSmokeTests` (day blocks by hour incl. the exclusive-end handoff; wrap-around night block; empty schedule -> home).
- RED: focused run failed to compile on the missing schedule types (CS0246).
- GREEN: full regression 53/53 suites passed, 0 compile errors (52 prior + new NPC-schedule suite); `ShopHoursSmokeTests` confirms the `MigrationShopHours` delegation preserved behavior.

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/Foundation/MigrationHourRange.cs` (new)
- `Assets/TouhouMigration/Scripts/Runtime/Economy/MigrationShopHours.cs` (delegates to `MigrationHourRange`)
- `Assets/TouhouMigration/Scripts/Runtime/Social/MigrationNpcScheduleEntry.cs` (new)
- `Assets/TouhouMigration/Scripts/Runtime/Social/MigrationNpcSchedule.cs` (new)
- `Assets/TouhouMigration/Scripts/Editor/Tests/NpcScheduleSmokeTests.cs` (new)

Known follow-ups:

- NPC schedule data (`NPCScheduleData` / JSON) for the 35 NPCs; an `NPCManager` driving locations on `hour_changed` (E2 clock); activity/sprite per entry; spawn NPCs into scenes by schedule (E3).

### E4.6: Fishing-Level Catch Boost (completes roll_fish)

- Date: 2026-06-25 10:31 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: Complete Godot `roll_fish` fidelity — rare and legendary fish gain `fishing_level * 2` catch weight (E4.5 omitted the level term).

Completed:

- `MigrationFishingService` level-aware overloads: `RarityWeight(rarity, fishingLevel)` adds `level*2` to Rare/Legendary (common/uncommon unaffected); `TotalWeight(fishingLevel)`; `Catch(nextInt, fishingLevel)`. The no-arg versions delegate to level 0 (non-breaking).

TDD (red -> green):

- Extended `FishingServiceSmokeTests` (boosted rare/legendary weights; total weight grows with level; the same roll lands in the now-wider rare band at a higher level).
- RED: focused run failed to compile on the missing 2-arg overloads (CS1501).
- GREEN: full regression 52/52 suites passed, 0 compile errors (extended in place).

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/Fishing/MigrationFishingService.cs`
- `Assets/TouhouMigration/Scripts/Editor/Tests/FishingServiceSmokeTests.cs`

Known follow-ups (E4.5/E4.6): `FishDatabase` JSON catalog; spot/season/hour context filtering; size roll; fishing spot + owner wiring that tracks the player's fishing level.

### E4.5: Fishing Service — Weighted Catch By Rarity

- Date: 2026-06-25 10:26 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: First fishing life-sim slice (Godot `FishingManager.roll_fish`): weighted random fish catch by rarity, granting the fish item to the inventory. No Unity fishing existed.

Completed:

- New `MigrationFishingService` (`Runtime/Fishing/`): `RegisterFish`, `TotalWeight`, `Catch(nextInt)` — weighted random selection over registered fish by rarity weight, adds the caught fish's item to `InventoryService`, returns a `MigrationFishCatchResult`. Injected RNG (`nextInt(maxExclusive)`).
- `RarityWeight`: Common 50, Uncommon 30, Rare 15, Legendary 5 (Godot `FishDatabase.RARITY_WEIGHTS`, verified).
- New `MigrationFishRarity` enum, `MigrationFishDefinition` (fishId / rarity / itemId), `MigrationFishCatchResult` (Success / FishId / ItemId / Rarity / FailureReason).

TDD (red -> green):

- New `FishingServiceSmokeTests` (rarity weights match Godot; weighted selection by roll lands in the correct rarity band [0,50)/[50,65); catch adds the fish item to inventory; empty service fails `no_fish`).
- RED: focused run failed to compile on the missing `Fishing` namespace (CS0234).
- GREEN: full regression 52/52 suites passed, 0 compile errors (51 prior + new fishing suite).

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/Fishing/MigrationFishingService.cs` (new)
- `Assets/TouhouMigration/Scripts/Runtime/Fishing/MigrationFishRarity.cs` (new)
- `Assets/TouhouMigration/Scripts/Runtime/Fishing/MigrationFishDefinition.cs` (new)
- `Assets/TouhouMigration/Scripts/Runtime/Fishing/MigrationFishCatchResult.cs` (new)
- `Assets/TouhouMigration/Scripts/Editor/Tests/FishingServiceSmokeTests.cs` (new)

Known follow-ups:

- Fish catalog (`FishDatabase` JSON) loader; fishing-level boost for rare+ (Godot `roll_fish` adds `fishing_level * 2`); spot/season/hour context filtering (`get_available_fish_for_context`); fish size roll; a fishing spot/minigame + owner wiring.

### E4.4: Farming Manager — Plant -> Grow -> Harvest -> Inventory

- Date: 2026-06-25 10:20 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: Complete the farming loop (Godot `FarmingManager` intent) so farming is playable end-to-end: plant a crop, water it, advance days to grow, harvest produce into the inventory. Builds on E4.2 plot growth without changing it.

Completed:

- New `MigrationFarmingManager` (`Runtime/Farming/`): owns N plots + a crop catalog. `Plant(plotIndex, cropId)`, `Water(plotIndex)`, `AdvanceDay()` (advances all plots), `Harvest(plotIndex, randomRange)` -> rolls yield in [min,max] (injected RNG), adds produce to `InventoryService`, clears the plot, returns a `MigrationHarvestResult`.
- New `MigrationCropDefinition` (cropId, growthDays, needsWaterDaily, harvestItemId, minYield, maxYield) — Godot `CropData`. New `MigrationHarvestResult` (Success / ItemId / Amount / FailureReason: no_plot / not_ready / unknown_crop).
- `MigrationFarmPlot` (E4.2) is unchanged — the manager composes it.

TDD (red -> green):

- New `FarmingManagerSmokeTests` (plant->grow->harvest adds produce to inventory + clears the plot; harvest-before-ready fails; unknown crop fails; yield honors the [min,max] range via injected RNG).
- RED: focused run failed to compile on the 3 missing types (CS0246).
- GREEN: full regression 51/51 suites passed, 0 compile errors (50 prior + new farming-manager suite).

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/Farming/MigrationFarmingManager.cs` (new)
- `Assets/TouhouMigration/Scripts/Runtime/Farming/MigrationCropDefinition.cs` (new)
- `Assets/TouhouMigration/Scripts/Runtime/Farming/MigrationHarvestResult.cs` (new)
- `Assets/TouhouMigration/Scripts/Editor/Tests/FarmingManagerSmokeTests.cs` (new)

Known follow-ups:

- `crops.json` (`CropData` catalog) loader; water/fertilizer/quality yield scaling (Godot `_calculate_harvest_yield`); tilling; multi-harvest regrow; owner wiring (a Farm scene + `AdvanceDay` driven by the day-loop / sleep — ties to E2).

### E5.9: Live Weather In Dialogue Context

- Date: 2026-06-25 10:14 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: Wire `weather` into the dialogue context so the `weather` / `weather_not` conditions fire (Godot rain-gated dialogue lines). Last dialogue-condition gap.

Completed:

- `BuildDialogueContext` sets `weather = GetWeatherSnapshot().Weather.ToString().ToLowerInvariant()` — matching the Godot lowercase weather condition values (verified against `WeatherSystem.WEATHER_NAMES` + `DialogueDatabaseExpanded` conditions, e.g. `{"weather": "rain"}`).
- **Every dialogue condition is now driven by live world state:** bond_level, humanity, time_of_day, is_full_moon, weather, active/completed/started quests, seen_events.

Verification:

- Owner-only wiring (casing verified against Godot, per the E5.8 lesson) — full regression 50/50 suites, 0 compile errors. `GlobalUiSmokeTests` gates the owner.

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`

Known follow-ups:

- Dialogue context is complete for the implemented conditions. Next E5: portraits; all 35 NPCs reachable; shop dialogue flows.

### E5.8: Correct is_full_moon To Night-Gated IsFullMoonActive (fixes E5.7)

- Date: 2026-06-25 10:11 CST (session 2)
- Status: Complete (correction)
- Owner: Claude
- Goal: Fix E5.7. Deeper inspection found two issues: (a) the dialogue `is_full_moon` should be the **night-gated** value — Godot `WeatherSystem.is_full_moon()` returns `is_full_moon_active` (FULL_MOON phase AND night, hour >= 19 or < 5), not a phase-only check; and (b) `WeatherService` already computes the moon phase (`(day % 32) / 4`) and `IsFullMoonActive`, so the E5.7 `MigrationMoonPhase` duplicated existing logic.

Completed:

- `BuildDialogueContext` now sets `is_full_moon = worldSimulation.GetWeatherSnapshot().IsFullMoonActive` (matches Godot `is_full_moon()`), instead of the phase-only `MigrationMoonPhase.IsFullMoon(day)`.
- Removed the redundant `MigrationMoonPhase` + `MoonPhaseSmokeTests`. `WeatherService.CalculateMoonPhase` + `UpdateFullMoonState` remain the single source of truth for lunar state.

Verification:

- Owner-only change after the removal — full regression 50/50 suites (51 minus the removed moon suite), 0 compile errors. `GlobalUiSmokeTests` gates the owner.

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- removed `Assets/TouhouMigration/Scripts/Runtime/Foundation/MigrationMoonPhase.cs` + `Assets/TouhouMigration/Scripts/Editor/Tests/MoonPhaseSmokeTests.cs`

Note: the E5.7 entry below describes the superseded phase-only approach; this entry corrects it.

### E5.7: Lunar Phase + is_full_moon Dialogue Context

- Date: 2026-06-25 10:06 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: Feed `is_full_moon` into the dialogue context so the existing `is_full_moon` condition fires (Godot full-moon dialogue / bond events such as `kaguya_moon_memory`).

Completed:

- New `MigrationMoonPhase` (`Runtime/Foundation/`): `PhaseIndex(day) = (day % 32) / 4`; `IsFullMoon(day)` = phase index 4. An 8-phase, 32-day cycle (4 days/phase), normalizing negative days. Matches Godot `WeatherSystem._update_moon_phase` (MoonPhase `FULL_MOON` = index 4). Pure.
- Owner `BuildDialogueContext` adds `["is_full_moon"] = MigrationMoonPhase.IsFullMoon(worldSimulation.GetTimeSnapshot().Day)`.

TDD (red -> green):

- New `MoonPhaseSmokeTests` (phase cycle, full-moon window days 16-19, 32-day wrap, negative-day normalization).
- RED: focused run failed to compile on the missing `MigrationMoonPhase` (CS0103).
- GREEN: full regression 51/51 suites passed, 0 compile errors (50 prior + new moon-phase suite). `GlobalUiSmokeTests` gates the owner change.

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/Foundation/MigrationMoonPhase.cs` (new)
- `Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `Assets/TouhouMigration/Scripts/Editor/Tests/MoonPhaseSmokeTests.cs` (new)

Known follow-ups:

- Dialogue context conditions now live: bond_level, humanity, time_of_day, is_full_moon, quests, seen_events. Remaining: `weather` / `weather_not` (needs the `WorldWeatherSnapshot` weather accessor wired into the context).

### E5.6: Live Time-Of-Day In Dialogue Context

- Date: 2026-06-25 09:58 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: Feed the live world-clock hour into the dialogue `time_of_day` condition. `BuildDialogueContext` hardcoded `"afternoon"`, so time-gated dialogue never varied with the in-game clock.

Completed:

- New `MigrationTimeOfDay.FromHour(hour)` (`Runtime/Foundation/`): maps a clock hour to the Godot `TimeManager.get_time_period` band (midnight 0-5, dawn 5-7, morning 7-12, noon 12-14, afternoon 14-17, evening 17-20, night 20-24), normalizing out-of-range hours. Pure.
- Owner `BuildDialogueContext` now sets `time_of_day = MigrationTimeOfDay.FromHour(worldSimulation.GetTimeSnapshot().Hour)` (falls back to `"afternoon"` if no world simulation is present).

TDD (red -> green):

- New `TimeOfDaySmokeTests` (every band + boundary hours + negative/overflow normalization).
- RED: focused run failed to compile on the missing `MigrationTimeOfDay` (CS0103).
- GREEN: full regression 50/50 suites passed, 0 compile errors (49 prior + new time-of-day suite). `GlobalUiSmokeTests` gates the owner change.

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/Foundation/MigrationTimeOfDay.cs` (new)
- `Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `Assets/TouhouMigration/Scripts/Editor/Tests/TimeOfDaySmokeTests.cs` (new)

Known follow-ups:

- Surface weather + `is_full_moon` into the dialogue context (those conditions already exist). HUD/lighting can also consume `MigrationTimeOfDay`.

### E5.5: Story Flags Gate Dialogue (seen_events context)

- Date: 2026-06-25 09:54 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: Make the dialogue `event` / `event_not_seen` conditions (already implemented in `DialogueDatabase.ConditionsPass`) actually fire. `BuildDialogueContext` never populated `seen_events`, so event-gated lines were inert.

Completed:

- `MigrationGlobalUiController.BuildDialogueContext` now adds `["seen_events"] = storyFlagService.CreateSnapshot()`. This closes the story-flag loop: dialogue `event` fx fire (E5.4) → persist across save/load (E8.2) → gate dialogue lines via the `event` / `event_not_seen` conditions.

Verification:

- Owner-only wiring (no new pure-logic; the condition logic already exists in `DialogueDatabase`) — gated by full regression `MigrationSmokeTestRunner.RunAll` = 49/49 suites, 0 compile errors (`DialogueSmokeTests` + `GlobalUiSmokeTests` cover the paths). Consistent with the E2.5 owner-wiring rhythm.

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`

Known follow-ups:

- `time_of_day` is still hardcoded `"afternoon"` in the context — surface it from the live world clock; add weather / `is_full_moon` context for those conditions (the conditions already exist).

### E8.2: Story Flag Save Persistence

- Date: 2026-06-25 09:48 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: Persist fired narrative events (E5.4). Without saving, a reloaded game forgets story beats (`mokou_fate`, `elixir_bad_end`) — the same correctness gap humanity had before E8.1.

Completed:

- `MigrationStoryFlagService.CreateSnapshot()` / `LoadSnapshot(IEnumerable<string>)` round-trip the fired-event set (LoadSnapshot replaces prior flags).
- `MigrationSaveData.story_flags` (`List<string>`) + `StoryFlags` property.
- Owner `SaveGame` captures `data.StoryFlags = storyFlagService.CreateSnapshot()` (alongside HP); `LoadGame` restores via `storyFlagService.LoadSnapshot(data.StoryFlags)`. Kept owner-direct (like the HP scalar) to avoid growing the save-orchestrator constructor arity.

TDD (red -> green):

- Extended `StoryFlagServiceSmokeTests` with a snapshot round-trip (snapshot count; restore into a fresh service, replacing a prior stale flag).
- RED: focused run failed to compile on the missing `CreateSnapshot` / `LoadSnapshot` (CS1061).
- GREEN: full regression 49/49 suites passed, 0 compile errors (suite count unchanged — extended in place). `GlobalUiSmokeTests` gates the owner save/load.

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/Narrative/MigrationStoryFlagService.cs`
- `Assets/TouhouMigration/Scripts/Runtime/Save/MigrationSaveData.cs`
- `Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `Assets/TouhouMigration/Scripts/Editor/Tests/StoryFlagServiceSmokeTests.cs`

Known follow-ups:

- `save_schema` stays 3 (`story_flags` defaults empty, backward-compatible). If the orchestrator construction is later refactored (DI/builder), fold humanity + story flags back into it to remove the owner-direct split.

### E5.4: Narrative Event (Story Flag) Routing

- Date: 2026-06-25 09:43 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: Route the Godot dialogue `event` effect (narrative events like `mokou_fate` / `elixir_bad_end` / `keine_helps`) to a story-flag service. It was the last unrouted dialogue fx after E5.2/E5.3 — so all known Godot dialogue fx are now routed.

Completed:

- New `MigrationStoryFlagService` (`Runtime/Narrative/`): `MarkEvent(id)` records a fired narrative event (idempotent), `HasEvent(id)`, `EventCount`. Free of UnityEngine.
- `DialogueEffectRouter.BindStoryFlags` + a `case "event"` routes the event id to `MarkEvent`. The owner constructs + binds it (mirrors the E5.2 humanity wiring).
- Dialogue fx now fully routed: bond, `bond_<npc>`, humanity, quest/start/complete, counter, unlock_npc, quest_progress, give/take_item, and event.

TDD (red -> green):

- New `StoryFlagServiceSmokeTests` (mark/query incl. idempotency; the `event` effect routes to the bound service; no-op when unbound).
- RED: focused run failed to compile on the missing `Narrative` namespace (CS0234).
- GREEN: full regression 49/49 suites passed, 0 compile errors (48 prior + new story-flag suite). `GlobalUiSmokeTests` gates the owner wiring.

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/Narrative/MigrationStoryFlagService.cs` (new)
- `Assets/TouhouMigration/Scripts/Runtime/Dialogue/DialogueEffectRouter.cs`
- `Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `Assets/TouhouMigration/Scripts/Editor/Tests/StoryFlagServiceSmokeTests.cs` (new)

Known follow-ups:

- Consumers: gate dialogue/quests/content on `HasEvent` (add story-flag dialogue conditions; the dialogue context already carries quest/bond/humanity). Save round-trip for fired events (fold into E8).

### E5.3: Cross-NPC Dialogue Bond Effects

- Date: 2026-06-25 09:39 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: Route Godot `bond_<npcId>` dialogue effects (e.g. `bond_keine` raised while talking to Koishi). The router only handled plain `bond` (current NPC), silently dropping the cross-NPC bond effects used in story dialogues (`DialogueDatabaseExpanded`).

Completed:

- `DialogueEffectRouter.ApplyEffect` now recognizes the `bond_<npcId>` prefix and routes the value to `bondService.AddBondPoints(<targetNpcId>, "dialogue", value)`. Plain `bond` still applies to the current NPC via the existing switch.

TDD (red -> green):

- New `DialogueBondEffectSmokeTests` (plain `bond` → current NPC; `bond_keine` → Keine, not the current NPC).
- RED: a genuine assertion failure (not a compile error) — `bond_keine` returned `false` and left Keine at 0 points.
- GREEN: full regression 48/48 suites passed, 0 compile errors (47 prior + new bond-effect suite).

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/Dialogue/DialogueEffectRouter.cs`
- `Assets/TouhouMigration/Scripts/Editor/Tests/DialogueBondEffectSmokeTests.cs` (new)

Known follow-ups:

- The `event` dialogue effect (narrative flags, e.g. `mokou_fate` / `elixir_bad_end` / `keine_helps`) is still unrouted — needs a story-flag/event service (E5/E6).

### E4.3: Shop Open-Hours Check

- Date: 2026-06-25 09:35 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: Port Godot `ShopData.is_shop_open` so shop availability can gate buying by time of day. Extends E4.1.

Completed:

- New `MigrationShopHours.IsOpen(startHour, endHour, currentHour)` (`Runtime/Economy/`): end-exclusive; `end < start` wraps past midnight (e.g. 22..2 → open 22:00-01:59); 0..24 is always open. Pure static, faithful to the Godot branch logic.

TDD (red -> green):

- New `ShopHoursSmokeTests` (3 cases: normal inclusive-start/exclusive-end, wrap-around-midnight, all-day-open).
- RED: focused run failed to compile on the missing `MigrationShopHours` (CS0103).
- GREEN: full regression 47/47 suites passed, 0 compile errors (46 prior + new shop-hours suite).

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/Economy/MigrationShopHours.cs` (new)
- `Assets/TouhouMigration/Scripts/Editor/Tests/ShopHoursSmokeTests.cs` (new)

Known follow-ups:

- Wire into the shop service/UI (gate `Buy` when the shop is closed) once shop catalog data (`open_hours` per shop) lands; reads the E2 world clock's current hour.

### E4.2: Farm Plot Crop Growth

- Date: 2026-06-25 09:31 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: First farming life-sim slice (Godot `FarmPlot`/`CropData` intent) — a deterministic crop-growth core. No farming existed in Unity.

Completed:

- New `MigrationFarmPlot` (`Runtime/Farming/`): `Plant(cropId, growthDays, needsWaterDaily)`; `Water()`; `AdvanceDay()` grows one day unless the crop needs water and was not watered that day (the watered flag clears daily, per Godot `is_watered_today`); `IsReadyToHarvest` at full growth; `GrowthProgress` 0..1; `Harvest()` clears a ready plot. Free of UnityEngine → unit-testable.

TDD (red -> green):

- New `FarmPlotSmokeTests` (4 cases: plant initializes state; daily watering gates growth; reaches harvest then harvest clears; cannot plant on an occupied plot / harvest an unready or empty plot).
- RED: focused run failed to compile on the missing `Farming` namespace (CS0234).
- GREEN: full regression 46/46 suites passed, 0 compile errors (45 prior + new farm-plot suite).

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/Farming/MigrationFarmPlot.cs` (new)
- `Assets/TouhouMigration/Scripts/Editor/Tests/FarmPlotSmokeTests.cs` (new)

Known follow-ups:

- Harvest yield (`min_yield`/`max_yield` + RNG) and inventory payout; quality tiers, fertilizer, multi-harvest regrow, soil memory (all in Godot `FarmPlot`).
- A `FarmingManager` owning multiple plots + tilling; a `CropDatabase` (crops.json) for crop defs; calendar-day integration so plots advance on day change (ties to E2 day-loop).

### E2.5: Menu Mode Drives World-Time / HUD Gate

- Date: 2026-06-25 09:26 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: Drive the `Menu` game-state mode from the pause/unified menu, so opening it freezes world-time (E2.4) and hides the HUD via `MigrationGameStateRules`. Session 1 only drove `Dialogue` mode.

Completed:

- `MigrationGlobalUiController.SyncMenuGameState()` (polled each frame in `Update`): pushes `MigrationGameStateMode.Menu` when `unifiedMenuController.IsOpen` and pops it when closed, idempotently via a `menuModePushed` guard. Pushing Menu fires `gameState.ModeChanged` → `WorldTimeScale(Menu) = 0` freezes the world clock; `ShowsHud(Menu) = false` marks the HUD hidden.

Verification:

- Owner MonoBehaviour wiring (no pure-logic surface, like the existing Dialogue push/pop) — gated by full regression `MigrationSmokeTestRunner.RunAll` = 45/45 suites, 0 compile errors; `GlobalUiSmokeTests` covers the owner.

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`

Known follow-ups:

- Scoped to the unified menu; gift selection and other overlays don't drive Menu mode yet. Combat/Sleeping modes still need gameplay triggers (combat enter/exit, sleep). HUD show/hide + input gating still read overlay state directly (`BlocksGameplayInput`) rather than consuming `MigrationGameStateRules.ShowsHud`/`AllowsGameplayInput`; reconcile in a later E2 slice.

### E8.1: Humanity Save Persistence

- Date: 2026-06-25 09:20 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: Persist the E5.2 humanity stat. `HumanityService` was added but never saved, so humanity reset to 100 on load — a correctness gap for the dialogue humanity effects.

Completed:

- `MigrationSaveData.humanity` scalar (default 100) + `Humanity` property (clamped 0..100).
- `MigrationSaveOrchestrator` now threads `HumanityService` (6th ctor param): Capture stores `humanity.Humanity` → `data.Humanity`; Apply restores via `humanity.Set(data.Humanity)`. The owner passes `humanityService` into the orchestrator.

TDD (red -> green):

- Extended `SaveOrchestratorSmokeTests` with `TestHumanityRoundTripsThroughSaveData` (adjust 100→70, capture, restore into a fresh service). RED on the missing 6-arg ctor + `MigrationSaveData.Humanity` (CS1729 / CS1061); GREEN at full regression 45/45 suites, 0 compile errors.

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/Save/MigrationSaveData.cs`
- `Assets/TouhouMigration/Scripts/Runtime/Save/MigrationSaveOrchestrator.cs`
- `Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `Assets/TouhouMigration/Scripts/Editor/Tests/SaveOrchestratorSmokeTests.cs`

Known follow-ups:

- `save_schema` left at 3 (the `humanity` field defaults to 100, backward-compatible with old saves); bump on the next deliberate schema revision.
- Player scalars still pending in the owner save path: coins/scene/position (per the orchestrator's caller-responsibility note).

### E4.1: Shop Economy Service

- Date: 2026-06-25 09:15 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: First E4 life-sim economy slice — a Unity-native shop transaction service (Godot `ShopData`/`ShopManager` intent). No shop service existed in Unity yet.

Completed:

- New `MigrationShopService` (`Runtime/Economy/`): `Buy(itemId, qty)` deducts price×qty coins and grants items; `Sell(itemId, qty[, buyRate])` pays floor(price × buy_rate) per unit and removes items. Bridges `InventoryService` (items), `ItemDatabase` (prices via `ItemDefinition.Price`), and `MigrationPlayerProgressService` (coins). `DefaultBuyRate = 0.5` (Godot `ShopData.get_buy_rate` default). Free of UnityEngine → unit-testable.
- New `ShopTransactionResult`: Success / ItemId / Quantity / CoinDelta (negative = spent, positive = earned) / FailureReason (`insufficient_funds` / `insufficient_items` / `inventory_full` / `unknown_item` / `invalid_request`).
- `MigrationPlayerProgressService.TrySpendCoins(int)`: deducts coins iff affordable (the service was add-only before).

TDD (red -> green):

- New `ShopServiceSmokeTests` (4 cases): buy deducts coins + grants item; buy fails on insufficient funds (no item, no spend); sell pays floor(price × buy_rate) + removes item; sell fails without the item. Price is derived dynamically from items.json so the test stays deterministic.
- RED: focused run failed to compile on the missing `Economy` namespace (CS0234).
- GREEN: full regression 45/45 suites passed, 0 compile errors (44 prior + new shop suite).

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/Economy/MigrationShopService.cs` (new)
- `Assets/TouhouMigration/Scripts/Runtime/Economy/ShopTransactionResult.cs` (new)
- `Assets/TouhouMigration/Scripts/Runtime/Player/MigrationPlayerProgressService.cs`
- `Assets/TouhouMigration/Scripts/Editor/Tests/ShopServiceSmokeTests.cs` (new)

Known follow-ups:

- Shop catalogs/hours/seasonal+festival items (Godot `ShopData` JSON), per-shop `buy_rate`, and shop-owner NPC gating are not loaded yet — this slice is the transaction core. Wire shop data + a shop UI in a later E4 slice.
- Not yet wired into the owner (`MigrationGlobalUiController`) or a shop interaction; pure service for now.

### E2.4: World-Time Gating By Game-State Mode

- Date: 2026-06-25 09:08 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: Make the existing game-state machine actually gate the world clock. Session 1 added `MigrationGameStateRules` but nothing applied them, so entering Dialogue did not stop time.

Completed:

- `MigrationGameStateRules.WorldTimeScale(mode)` (pure): 0 for Menu/Dialogue/Cutscene (frozen), `SleepingTimeScale` (×12, Godot `TimeManager.time_scale` intent) for Sleeping, ×1 otherwise. Invariant: scale == 0 iff `FreezesWorldTime(mode)`.
- `WorldSimulationBehaviour.SetExternalTimeScale(float)` + an `externalTimeScale` applied to clock/weather advancement (default 1 = behavior-preserving; 0 freezes, >1 fast-forwards).
- `MigrationGlobalUiController` subscribes `gameState.ModeChanged` and applies `WorldTimeScale(mode)` to the world sim. The existing Dialogue Push/Pop now freezes/resumes the world clock with no extra wiring.

TDD (red -> green):

- Extended `GameStateRulesSmokeTests` with `TestWorldTimeScaleModes` (per-mode scale + the scale-0-iff-frozen invariant across all modes).
- RED: focused run failed to compile on missing `WorldTimeScale` / `SleepingTimeScale` (CS0117).
- GREEN: full regression 44/44 suites passed, 0 compile errors (suite count unchanged — extended in place). `GlobalUiSmokeTests` + `FoundationSmokeTests` gate the owner/sim changes.

E1.5 investigation (recorded, not yet actioned):

- The MokouValidation clips are already Humanoid, but `Art/Characters/Mokou/Models/mokou.glb` imports via the glTF ScriptedImporter (glTFast) and has **no Mecanim Humanoid avatar**, so the humanoid Mixamo clips can't retarget onto Mokou. E1.5's player-animation visual work is blocked until the model gets a Humanoid avatar (convert `mokou.glb`→FBX, or source a humanoid-rigged Mokou FBX). See `CURRENT_HANDOFF.md`.

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/Foundation/MigrationGameStateRules.cs`
- `Assets/TouhouMigration/Scripts/Runtime/Foundation/WorldSimulationBehaviour.cs`
- `Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `Assets/TouhouMigration/Scripts/Editor/Tests/GameStateRulesSmokeTests.cs`

Known follow-ups:

- Combat/Sleeping/Menu modes are not yet pushed from gameplay; only Dialogue drives `ModeChanged` today. The Sleeping fast-forward scale exists but no sleep flow triggers it yet.
- Pre-existing: `WorldSimulationBehaviour` advances the clock by real deltaTime (now ×`externalTimeScale`); `Clock.TimeScale` still only affects weather-hour bookkeeping. Left as-is.

### E5.2: Dialogue Humanity Stat Routing

- Date: 2026-06-25 08:52 CST (session 2)
- Status: Complete (slice)
- Owner: Claude
- Goal: Route the dialogue `humanity` fx to a real stat target. Session 1 wired `bond` but left `humanity` unrouted (no stat target). Godot `DialogueDatabaseExpanded` uses `humanity` +/- deltas; `MokouMonologueSystem` gates Mokou behavior on humanity-level thresholds.

Completed:

- New `HumanityService` (`Runtime/Player/HumanityService.cs`): Mokou's global humanity stat — default 100, clamped 0..100, `Adjust(delta)` bidirectional, `Set(value)`, and `Level` -> `HumanityLevel {Low,Medium,High}` at the Godot `MokouMonologueSystem` thresholds (>=70 High, >=40 Medium, else Low).
- `DialogueEffectRouter`: added `BindHumanity(...)` + a `case "humanity"` routing the data value through `HumanityService.Adjust` (mirrors the optional `BindInventory` pattern; no-op when unbound).
- `MigrationGlobalUiController`: constructs + binds `HumanityService`; `BuildDialogueContext` now reads the live `humanity` value instead of a hardcoded `100`, so `humanity_min`/`humanity_max` dialogue conditions react to accumulated effects.

TDD (red -> green):

- New `DialogueHumanityEffectSmokeTests` (5 cases): service default/clamp, level thresholds, effect routing, live `ActionRequested` path, and unbound no-op.
- RED: focused run failed to compile on missing `HumanityService` / `HumanityLevel` / `BindHumanity` (feature-missing red).
- GREEN: full regression `MigrationSmokeTestRunner.RunAll` = 44/44 suites passed, 0 compile errors (43 prior + new humanity suite). `GlobalUiSmokeTests` green gates the controller change.

Changed files:

- `Assets/TouhouMigration/Scripts/Runtime/Player/HumanityService.cs` (new)
- `Assets/TouhouMigration/Scripts/Runtime/Dialogue/DialogueEffectRouter.cs`
- `Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `Assets/TouhouMigration/Scripts/Editor/Tests/DialogueHumanityEffectSmokeTests.cs` (new)

Known follow-ups:

- Humanity is not yet in the save schema (`MigrationSaveData`) — fold into E8 save parity.
- No HUD surface for humanity yet (E7 presentation); Mokou monologue-level consumers (E5/E6) can read `HumanityService.Level`.

### M57: Snowball Damage And Arena Bounce

- Date: 2026-06-25 05:04 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Kuhn
- Goal: Turn the M54-M56 rolling snowball from pressure/presentation into a real Unity arena hazard: player damage, capture-hit registration, hit cooldown, and Godot-style arena radius bounce.

Completed:

- Audited Godot truth:
  - `CirnoDanmakuPatterns.gd` defines `snowball` speed `4.2`, damage `16`, duration `5.8`, shatter HP `42`, and weaknesses `fire/heavy/shatter`.
  - `CirnoBossController.gd` creates `SnowballHazardArea`, masks only the player layer, and calls `body.take_damage(16, snowball)` on `body_entered`.
  - The real Cirno arena player is `Player3D`, where Cirno MVP hit invulnerability is a `0.75s` player-side timer.
  - Snowball arena bounce is manual radius logic around boss home position: when the next position leaves `snowball_arena_radius = 34`, Godot bounces velocity on the horizontal arena normal. There is no wall/StaticBody snowball bounce path.
- Expanded `MigrationPerfectFreezeSnowballHazard`:
  - binds or resolves `MigrationCombatRuntime`.
  - exposes `TryDamagePlayer()` and routes damage through `MigrationCombatRuntime.ApplyDamageToPlayer(damage)`.
  - calls `encounterDirector.RegisterPlayerHit()` only after a successful damage/rebirth result, so blocked hits do not break capture multiple times.
  - adds a temporary snowball-local `playerDamageCooldownSeconds = 0.75` seam, matching Godot's Cirno i-frame timing until player-side i-frame ownership is migrated.
  - adds `ConfigureArena(center, radius)`, `ArenaRadius`, `Direction`, `BounceEventCount`, and `LastBounceNormal`.
  - reflects the rolling direction off the horizontal arena normal when the next flat position leaves the arena radius.
  - supports `OnTriggerEnter` / `OnCollisionEnter` for tagged `Player` objects while remaining directly testable through `TryDamagePlayer()`.
- Expanded `PerfectFreezeEncounterSmokeTests`:
  - first red test asserted missing damage/bounce APIs and counters.
  - second red test asserted missing `0.75s` player-hit cooldown state.
  - green tests prove a snowball hit applies `16` real player damage, reduces HP from `100` to `84`, increments snowball damage count, and registers a Perfect Freeze phase hit.
  - green tests prove immediate repeated snowball hits are blocked by cooldown and do not add damage or extra phase hits; after `0.75s`, one more hit applies once.
  - green tests prove a configured arena radius bounce reflects direction and exposes the bounce normal.
- Regenerated `MigrationPerfectFreezeEncounter.prefab`; it now serializes `arenaRadius: 34` and `playerDamageCooldownSeconds: 0.75` on `PerfectFreezeSnowballHazard`.
- Internet/open-source reference note:
  - `gh search code "Unity rolling ball hazard damage player" --language C#` returned no useful reusable result.
  - `gh search code "Unity boss arena hazard bounce bounds" --language C#` returned no useful reusable result.
  - The current Unity implementation keeps the local hazard/director/combat-runtime seam and copies no external code.
  - `agent-reach check-update` reports `v1.5.0` is already current.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeSnowballHazard.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/PerfectFreezeEncounterSmokeTests.cs`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Encounters/MigrationPerfectFreezeEncounter.prefab`

Verification:

- M57 damage/bounce red run:
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M57_SnowballDamageBounce_RED_PerfectFreezeEncounter.log`
  - Result: failed as expected on missing `BindCombatRuntime`, `ConfigureArena`, `TryDamagePlayer`, `PlayerDamageEventCount`, `Direction`, `BounceEventCount`, and `LastBounceNormal`.
- M57 cooldown red run:
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M57_SnowballDamageCooldown_RED_PerfectFreezeEncounter.log`
  - Result: failed as expected on missing `PlayerDamageCooldownSeconds` and `PlayerDamageCooldownRemainingSeconds`.
- Focused green runs:
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M57_SnowballDamageBounce_GREEN_PerfectFreezeEncounter.log`: passed before the cooldown follow-up.
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M57_SnowballDamageCooldown_GREEN_PerfectFreezeEncounter.log`: passed after cooldown implementation.
- Adjacent regressions:
  - `EnemyProjectileSpecialRulesSmokeTests.RunAll` -> `Logs/M57_SnowballDamageBounce_REG_SpecialRules.log`: passed.
  - `EnemyProjectilePerfectFreezeCycleSmokeTests.RunAll` -> `Logs/M57_SnowballDamageBounce_REG_PerfectFreezeCycle.log`: passed.
  - `ProjectileSettlementStaggerSmokeTests.RunAll` -> `Logs/M57_SnowballDamageBounce_REG_SettlementStagger.log`: passed.
  - `CombatBridgeSmokeTests.RunAll` -> `Logs/M57_SnowballDamageBounce_REG_CombatBridge.log`: passed.
- Builder:
  - `TouhouMigrationProjectBuilder.BuildInitialProject` -> `Logs/M57_SnowballDamageBounce_BUILD_BuildInitialProject.log`: completed successfully.
- Post-build focused check:
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M57_SnowballDamageBounce_FINAL_PerfectFreezeEncounter.log`: passed.
- Serialization checks:
  - `MigrationPerfectFreezeEncounter.prefab` serializes `arenaRadius: 34` and `playerDamageCooldownSeconds: 0.75`.
  - Existing snowball values remain serialized: `damage: 16`, `durationSeconds: 5.8`, and `initialShatterHp: 42`.
- Process/tool checks:
  - 2026-06-25 05:04 CST: no real `Unity.app/Contents/MacOS/Unity` process remained.
  - `agent-reach check-update`: current `v1.5.0`, already latest.

Known blockers:

- M57 keeps the 0.75s repeated-hit cooldown local to the snowball hazard. Godot owns this on `Player3D`; Unity should eventually move it into a broader player i-frame/hit feedback system.
- Snowball shatter/expiry/hit/bounce still need polished VFX, audio, camera impulse, and HUD readability.
- The reusable encounter prefab still is not a final Misty Lake boss arena scene with production camera lock-on and combat UI.

Next recommended step:

- M58 should wire production consumers for `MigrationPerfectFreezePhaseResult` / `ReflectStunReady`, or move repeated-hit immunity into a proper player-side i-frame runtime while adding hit VFX/audio/camera feedback.

### M56: Boss Snowball Movement Intent

- Date: 2026-06-25 03:52 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Averroes
- Goal: Add the boss movement-intent seam that makes active snowball pressure visibly push from behind and gives the close-range band a real `evade_close` target, while keeping the implementation Unity-native and localized to the Perfect Freeze encounter.

Completed:

- Audited Godot truth:
  - `_get_desired_ai_position()` gives active snowball priority over close evade, placing the boss behind the snowball by `snowball_radius + 1.35`.
  - close range without active snowball uses `origin - forward * 2.6 + side * 1.75`.
  - `_update_boss_motion()` consumes desired movement with `global_position.lerp(desired, clamp(delta * 2.2, 0, 1))`, while hover height, look-at, and flight pose are presentation layers that can become richer Unity work later.
- Expanded `MigrationPerfectFreezeEncounterDirector`:
  - added serialized boss movement parameters: `snowballPushOffset = 1.35`, `closeEvadeBackDistance = 2.6`, `closeEvadeSideDistance = 1.75`, and `bossMovementLerp = 2.2`.
  - added `ConfigureBossMovement(...)`, public parameter accessors, `LastBossMovementIntentKind`, and `LastDesiredBossPosition`.
  - updates boss movement during `TickPhase()` before cast cadence.
  - chooses `snowball_push_position` when the bound snowball hazard is active, using the hazard radius plus push offset instead of a fixed Godot coordinate.
  - chooses `evade_close` when player distance is `<= closeRangeDistance` and no snowball is active.
  - keeps Cirno-specific positioning in the encounter director rather than putting it into the generic `MigrationSimpleEnemyController`.
- Expanded `PerfectFreezeEncounterSmokeTests`:
  - red test first asserted missing movement APIs and state.
  - final green tests assert gameplay relationships: active snowball target is opposite the player lane and exactly `radius + offset` from the snowball; close evade backs away and sidesteps without locking tests to one permanent left/right side.
  - tests also prove the bound boss transform consumes the desired target through `TickPhase()`.
- Regenerated `MigrationPerfectFreezeEncounter.prefab`; it now serializes `snowballPushOffset: 1.35`, `closeEvadeBackDistance: 2.6`, `closeEvadeSideDistance: 1.75`, and `bossMovementLerp: 2.2`.
- Internet/open-source reference note:
  - `gh search code "Unity boss evade close range movement target" --language C#` returned no useful reusable result for this narrow movement seam.
  - The current Unity implementation keeps the local director movement boundary and copies no external code.
  - `agent-reach check-update` reports `v1.5.0` is already current.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeEncounterDirector.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/PerfectFreezeEncounterSmokeTests.cs`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Encounters/MigrationPerfectFreezeEncounter.prefab`

Verification:

- M56 red run:
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M56_BossMovementIntent_RED_PerfectFreezeEncounter.log`
  - Result: failed as expected on missing `SnowballPushOffset`, `ConfigureBossMovement`, `LastBossMovementIntentKind`, and `LastDesiredBossPosition`.
- Focused green run:
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M56_BossMovementIntent_GREEN_PerfectFreezeEncounter.log`: passed.
- Adjacent regressions:
  - `EnemyProjectileSpecialRulesSmokeTests.RunAll` -> `Logs/M56_BossMovementIntent_REG_SpecialRules.log`: passed.
  - `EnemyProjectilePerfectFreezeCycleSmokeTests.RunAll` -> `Logs/M56_BossMovementIntent_REG_PerfectFreezeCycle.log`: passed.
  - `ProjectileSettlementStaggerSmokeTests.RunAll` -> `Logs/M56_BossMovementIntent_REG_SettlementStagger.log`: passed.
  - `EnemyAnimationBridgeSmokeTests.RunAll` -> `Logs/M56_BossMovementIntent_REG_EnemyAnimationBridge.log`: passed.
- Builder:
  - `TouhouMigrationProjectBuilder.BuildInitialProject` -> `Logs/M56_BossMovementIntent_BUILD_BuildInitialProject.log`: completed successfully.
- Post-build focused check:
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M56_BossMovementIntent_FINAL_PerfectFreezeEncounter.log`: passed.
- Serialization checks:
  - `MigrationPerfectFreezeEncounter.prefab` serializes `snowballPushOffset: 1.35`, `closeEvadeBackDistance: 2.6`, `closeEvadeSideDistance: 1.75`, and `bossMovementLerp: 2.2`.
  - `MigrationPerfectFreezePhasePlan.asset` still serializes `closeRangeDistance: 4.2`, `iceLanceMinDistance: 12`, and `snowballPreferredDistance: 8`.
- Process check:
  - 2026-06-25 03:52 CST: no real `Unity.app/Contents/MacOS/Unity` process remained.

Known blockers:

- Boss movement now has a gameplay intent seam, but hover height, look-at, roll/pitch/yaw pose, animation-state consumption, and final camera/audio feedback are still first-pass or missing.
- Completed by M57: snowball arena-radius bounce and real player-health damage routing now exist. Polished snow/crack/hit/bounce presentation and reward/HUD/audio consumers remain future work.
- `PhaseFinished`, `ReflectStunReady`, and snowball shatter/expiry still need production UI/audio/reward/camera presentation.

Next recommended step:

- Completed by M57: arena-radius bounce and real player-damage routing landed. Next work should wire production HUD/audio/reward/camera consumers to `MigrationPerfectFreezePhaseResult` and `ReflectStunReady`, or move repeated-hit immunity into a broader player-side i-frame runtime.

### M55: Snowball Boss Intent Start

- Date: 2026-06-25 01:33 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Galileo
- Goal: Connect the authored boss `push_snowball` / near-distance intent to the M54 snowball hazard without turning it into an ordinary projectile cast.

Completed:

- Audited Godot truth:
  - no active snowball: `distance <= 4.2m` is `evade_close`, `distance >= 12m` is `ice_lance_snipe`, and `(4.2m, 8m]` is `push_snowball`.
  - active snowball keeps pressure ownership and should not spawn a duplicate.
  - push-snowball growth seed is deterministic: `1.0 + cast_index % 3 * 0.22`.
- Expanded `MigrationPerfectFreezePhasePlan`:
  - added serialized `closeRangeDistance = 4.2`.
  - added serialized `snowballPreferredDistance = 8`.
  - `ConfigureCastPlan(...)` now carries both distance gates with defaults for older callers.
- Expanded `MigrationPerfectFreezeEncounterDirector`:
  - exposes `CloseRangeDistance` and `SnowballPreferredDistance`.
  - applies both gates from the phase plan.
  - starts `snowball_roll` by calling the bound `MigrationPerfectFreezeSnowballHazard.BeginRolling(center, playerPosition, growthSeed)`.
  - keeps `snowball_roll` out of `activeProjectiles` and the prefab-keyed projectile pool.
  - preserves active-pressure suppression and far-distance ice-lance priority.
- Expanded `PerfectFreezeEncounterSmokeTests`:
  - near-distance `8m` starts `snowball_roll`, activates the hazard, sets pressure, hides the cue, and spawns no projectile instances.
  - a second cadence tick while the hazard is active stays in `snowball_pressure` and does not duplicate the hazard.
  - close range `3m` does not start the snowball, preserving the then-future evade-close boundary that M56 later turns into a movement seam.
  - normal PF field/fan tests now use `10m` so they do not accidentally sit on the Godot snowball threshold.
- Regenerated `MigrationPerfectFreezePhasePlan.asset` and `MigrationPerfectFreezeEncounter.prefab`; both serialize `closeRangeDistance: 4.2` and `snowballPreferredDistance: 8`.
- Internet/open-source reference note:
  - `agent-reach doctor --json` reported GitHub served by `gh CLI`.
  - `gh search code` checks for Unity boss snowball / rolling hazard / close-range priority examples returned no useful reusable implementation for this narrow slice.
  - The current Unity implementation keeps the local director/phase-plan/hazard seam and copies no external code.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezePhasePlan.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeEncounterDirector.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/PerfectFreezeEncounterSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Data/Combat/PerfectFreeze/MigrationPerfectFreezePhasePlan.asset`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Encounters/MigrationPerfectFreezeEncounter.prefab`

Verification:

- M55 red run:
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M55_SnowballIntent_RED_PerfectFreezeEncounter.log`
  - Result: failed as expected on missing `SnowballPreferredDistance` and the missing nine-argument cast-plan overload.
- Focused green run:
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M55_SnowballIntent_GREEN_PerfectFreezeEncounter.log`: passed.
- Adjacent regressions:
  - `EnemyProjectileSpecialRulesSmokeTests.RunAll` -> `Logs/M55_SnowballIntent_REG_SpecialRules.log`: passed.
  - `EnemyProjectilePerfectFreezeCycleSmokeTests.RunAll` -> `Logs/M55_SnowballIntent_REG_PerfectFreezeCycle.log`: passed.
  - `ProjectileSettlementStaggerSmokeTests.RunAll` -> `Logs/M55_SnowballIntent_REG_SettlementStagger.log`: passed.
- Builder:
  - `TouhouMigrationProjectBuilder.BuildInitialProject` -> `Logs/M55_SnowballIntent_BUILD_BuildInitialProject.log`: completed successfully.
- Post-build focused check:
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M55_SnowballIntent_FINAL_PerfectFreezeEncounter.log`: passed.
- Process check:
  - 2026-06-25 01:33 CST: no real `Unity.app/Contents/MacOS/Unity` process remained.

Known blockers:

- Completed by M56: close range now has an `evade_close` movement target and active snowball now pushes the boss target behind the snowball by `snowball_radius + 1.35`.
- Completed by M57: snowball arena-radius bounce and real player-health damage routing now exist. Polished snow/crack presentation, hover/flight-pose polish, player-side i-frame ownership, and snowball shatter reward consumers remain future work.

Next recommended step:

- Completed by M56/M57: close-range evade, active-snowball boss push-positioning, snowball arena-radius bounce, and snowball player damage now exist. Next work should add presentation and production HUD/audio/reward/camera consumers for `MigrationPerfectFreezePhaseResult` and `ReflectStunReady`.

### M54: Rolling Snowball Hazard Runtime

- Date: 2026-06-25 01:20 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Newton
- Goal: Turn the M53 snowball-pressure seam into a real Unity hazard/presenter while keeping the snowball out of ordinary projectile budget/pool ownership.

Completed:

- Audited Godot truth and implementation shape:
  - snowball pressure is a singleton arena hazard, not another ordinary bullet.
  - core values are speed `4.2`, damage `16`, telegraph `0.72s`, initial radius `0.78 + seed * 0.10`, growth `0.18/s`, duration `5.8s`, shatter HP `42`, and weaknesses `fire/heavy/shatter`.
  - pressure must clear on shatter, expiry, disable, and destroy so far-distance ice-lance does not stay suppressed.
- Added `MigrationPerfectFreezeSnowballHazard`:
  - rolls from an origin toward a target using a flat Unity direction.
  - grows radius over time up to `initialRadius + 1.55`.
  - keeps transform position/scale aligned to the active radius for a simple sphere presenter.
  - exposes counter damage and weakness multipliers without putting counter authority into the encounter director.
  - calls `SetSnowballPressureActive(true/false)` on the bound director.
- Expanded `MigrationPerfectFreezeEncounterDirector`:
  - added serialized `snowballHazard`.
  - added `SnowballHazard`, `HasSnowballHazard`, and `BindSnowballHazard(...)`.
  - resolves/binds a child hazard on `Awake()`.
- Expanded `PerfectFreezeEncounterSmokeTests`:
  - verifies generated prefab snowball wiring/defaults.
  - verifies roll begin, radius growth, position/scale updates, duration expiry, shatter, weak-family multiplier, and pressure cleanup.
- Regenerated `MigrationPerfectFreezeEncounter.prefab`; it now contains a `PerfectFreezeSnowballHazard` sphere child with trigger collider, kinematic rigidbody, renderer, and bound hazard component.
- Internet/open-source reference note:
  - `agent-reach doctor --json` reported GitHub served by `gh CLI`.
  - `gh search code` checks for Unity rolling hazard/snowball examples returned no useful reusable implementation for this narrow slice.
  - The current Unity implementation keeps the local hazard/prefab/director seam and copies no external code.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeSnowballHazard.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeSnowballHazard.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeEncounterDirector.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/PerfectFreezeEncounterSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Encounters/MigrationPerfectFreezeEncounter.prefab`

Verification:

- M54 red run:
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M54_SnowballRuntime_RED_PerfectFreezeEncounter.log`
  - Result: failed as expected on missing `MigrationPerfectFreezeSnowballHazard`, `HasSnowballHazard`, `SnowballHazard`, and `BindSnowballHazard`.
- Focused green run:
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M54_SnowballRuntime_GREEN_PerfectFreezeEncounter.log`: passed.
- Adjacent regressions:
  - `EnemyProjectileSpecialRulesSmokeTests.RunAll` -> `Logs/M54_SnowballRuntime_REG_SpecialRules.log`: passed.
  - `EnemyProjectilePerfectFreezeCycleSmokeTests.RunAll` -> `Logs/M54_SnowballRuntime_REG_PerfectFreezeCycle.log`: passed.
  - `ProjectileSettlementStaggerSmokeTests.RunAll` -> `Logs/M54_SnowballRuntime_REG_SettlementStagger.log`: passed.
- Builder:
  - `TouhouMigrationProjectBuilder.BuildInitialProject` -> `Logs/M54_SnowballRuntime_BUILD_BuildInitialProject.log`: completed successfully.
- Post-build focused check:
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M54_SnowballRuntime_FINAL_PerfectFreezeEncounter.log`: passed.
- Process check:
  - 2026-06-25 01:20 CST: no real `Unity.app/Contents/MacOS/Unity` process remained.

Known blockers:

- M54 is the first real rolling snowball runtime, but the encounter director still does not author/start `push_snowball` casts from boss intent.
- Completed by M57: arena-radius bounce and real player-health damage routing now exist. Polished snow/crack/hit/bounce presentation and any source-specific bonus hooks remain future work.
- The hazard is intentionally not returned through `MigrationPrefabPoolService`; it is singleton encounter state, matching the player-facing pressure behavior.

Next recommended step:

- Completed by M55/M56/M57: the director-authored `push_snowball` cast start / near-distance intent now calls `MigrationPerfectFreezeSnowballHazard.BeginRolling(...)`, the boss movement seam handles active-snowball push positioning plus close-range evade, and the snowball now damages/bounces in the arena. The remaining branch is polished presentation plus production HUD/audio/reward/camera consumers for `MigrationPerfectFreezePhaseResult` and `ReflectStunReady`.

### M53: Snowball Pressure Intent Arbitration

- Date: 2026-06-25 01:11 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Cicero
- Goal: Preserve the Godot player-facing rule that active snowball pressure suppresses far-distance ice-lance snipe, while implementing the smallest Unity-native input seam for a future rolling snowball runtime.

Completed:

- Audited Godot truth:
  - `CirnoBossController.gd` evaluates `snowball_active` before `distance >= ice_lance_min_distance`.
  - with active snowball, far distance stays in `push_snowball` pressure instead of casting `ice_lance_snipe`.
  - snowball specs remain future runtime work: speed `4.2`, damage `16`, telegraph `0.72s`, duration `5.8s`, shatter HP `42`, and weaknesses `fire/heavy/shatter`.
- Added TDD coverage to `PerfectFreezeEncounterSmokeTests`:
  - active snowball pressure at `16m` suppresses the M52 far-distance `ice_lance_snipe`.
  - pressure casts no extra orb, field, shard, or ice-lance projectiles.
  - pressure does not reuse the Perfect Freeze safe-lane cue.
  - clearing pressure restores the far-distance `ice_lance_snipe` on the next cadence tick.
- Expanded `MigrationPerfectFreezeEncounterDirector`:
  - added serialized `snowballPressureActive`.
  - added `SnowballPressureActive` and `SetSnowballPressureActive(bool)`.
  - added `snowball_pressure` as a director-level cast kind.
  - checks active snowball pressure before the M52 ice-lance distance gate.
  - hides the safe-lane cue and consumes cadence without spawning extra projectiles while snowball pressure owns the cast.
- Regenerated `MigrationPerfectFreezeEncounter.prefab`; it serializes `snowballPressureActive: 0`, so M53 does not accidentally disable the authored snipe until a future snowball runtime explicitly activates pressure.
- Internet/open-source reference note:
  - `agent-reach doctor --json` reported GitHub served by `gh CLI`.
  - `gh search code` checks for Unity boss intent/attack priority examples returned no useful reusable implementation for this narrow slice.
  - The current Unity implementation keeps the local director seam and copies no external code.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeEncounterDirector.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/PerfectFreezeEncounterSmokeTests.cs`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Encounters/MigrationPerfectFreezeEncounter.prefab`

Verification:

- M53 red run:
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M53_SnowballIntent_RED_PerfectFreezeEncounter.log`
  - Result: failed as expected on missing `SetSnowballPressureActive(bool)` and `SnowballPressureActive`.
- Focused green run:
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M53_SnowballIntent_GREEN_PerfectFreezeEncounter.log`: passed.
- Adjacent regressions:
  - `EnemyProjectileSpecialRulesSmokeTests.RunAll` -> `Logs/M53_SnowballIntent_REG_SpecialRules.log`: passed.
  - `EnemyProjectilePerfectFreezeCycleSmokeTests.RunAll` -> `Logs/M53_SnowballIntent_REG_PerfectFreezeCycle.log`: passed.
  - `ProjectileSettlementStaggerSmokeTests.RunAll` -> `Logs/M53_SnowballIntent_REG_SettlementStagger.log`: passed.
- Builder:
  - `TouhouMigrationProjectBuilder.BuildInitialProject` -> `Logs/M53_SnowballIntent_BUILD_BuildInitialProject.log`: completed successfully.
- Post-build focused check:
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M53_SnowballIntent_FINAL_PerfectFreezeEncounter.log`: passed.
- Process check:
  - 2026-06-25 01:11 CST: no real `Unity.app/Contents/MacOS/Unity` process remained.

Known blockers:

- M53 is not the complete rolling snowball runtime. Future work still needs a snowball entity/presenter with movement, radius growth, shatter HP, weaknesses, player damage, and pressure cleanup.
- A future snowball runtime must clear `SetSnowballPressureActive(false)` on shatter/expiry; otherwise far-distance snipe stays suppressed.
- The current pressure branch consumes one pattern cadence without spawning a projectile. Revisit parity/cadence once the real snowball cast exists.

Next recommended step:

- Completed by M54: the first rolling snowball runtime/presenter now drives this pressure seam. The remaining branch is director-authored `push_snowball` start timing plus production HUD/audio/reward/camera consumers for `MigrationPerfectFreezePhaseResult` and `ReflectStunReady`.

### M52: Authored Ice Lance Snipe Cast

- Date: 2026-06-25 01:01 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Parfit
- Goal: Turn the M51 reflectable ice-lance prefab into an authored Unity encounter cast that matches the Godot player-facing purpose: a far-distance, single-projectile, counterable Cirno snipe with a scoped boss-stun reward.

Completed:

- Audited Godot truth:
  - `CirnoDanmakuPatterns.gd` builds `ice_lance` with speed `22.5`, damage `16`, radius `0.24`, telegraph `0.62s`, forward spawn offset `0.55`, reflectable/stun reward flags, and stun seconds `2`.
  - `CirnoBossController.gd` chooses `ice_lance_snipe` only when the player is at least `12m` away and snowball pressure is not active.
  - `CirnoBossRedesignCli.gd` protects the rule that snowball and ice-lance are mutually exclusive.
- Added TDD coverage to `PerfectFreezeEncounterSmokeTests`:
  - generated phase plan/prefab wiring now asserts the ice-lance prefab reference and `12m` distance gate.
  - far-distance phase start at `16m` now asserts `LastCastPatternKind="ice_lance_snipe"`.
  - the far cast asserts exactly one ice-lance, zero orb/field/shard projectiles, zero safe-lane cue events, and one active `ice_lance` projectile.
  - the lance contract asserts speed `22.5`, damage `16`, arm delay `0.62s`, reflectability, reflect stun `2s`, and forward spawn offset `0.55`.
  - the reflect path asserts the scoped settlement stuns only the local boss adapter, not a global-settlement spectator.
- Expanded `MigrationPerfectFreezePhasePlan` with `iceLanceMinDistance`, defaulting to `12f`, while keeping existing seven-argument cast-plan callers source-compatible through a default parameter.
- Expanded `MigrationPerfectFreezeEncounterDirector`:
  - adds `iceLanceProjectilePrefab` binding and inspection properties.
  - applies `IceLanceMinDistance` from the phase plan.
  - checks flat player distance before the normal orb/field/shard cast plan.
  - casts the exclusive `ice_lance_snipe` pattern when distance is at or beyond the gate.
  - spawns the lance through the existing pooled `SpawnProjectile` seam so template application, scoped settlement wiring, and pool reclamation stay unified.
- Expanded `TouhouMigrationProjectBuilder`:
  - writes the `12f` ice-lance distance gate into `MigrationPerfectFreezePhasePlan.asset`.
  - binds `MigrationIceLanceProjectileFeedback.prefab` into the generated `MigrationPerfectFreezeEncounter.prefab`.
- Internet/open-source reference note:
  - `agent-reach doctor --json` confirmed GitHub via `gh CLI`.
  - `gh search code` checks for Unity boss snipe/aimed-shot pattern examples returned no useful reusable repo for this narrow slice.
  - The final implementation keeps the local Unity-native director/prefab/ScriptableObject boundary; no external code was copied.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeEncounterDirector.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezePhasePlan.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/PerfectFreezeEncounterSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Data/Combat/PerfectFreeze/MigrationPerfectFreezePhasePlan.asset`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Encounters/MigrationPerfectFreezeEncounter.prefab`

Verification:

- M52 red run:
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M52_IceLance_RED_PerfectFreezeEncounter.log`
  - Result: failed as expected on missing ice-lance prefab binding APIs, `IceLanceMinDistance`, `LastCastIceLanceProjectileCount`, and eight-argument cast-plan configuration.
- Focused green run:
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M52_IceLance_GREEN_PerfectFreezeEncounter.log`: passed.
- Adjacent regressions:
  - `EnemyProjectileSpecialRulesSmokeTests.RunAll` -> `Logs/M52_IceLance_REG_SpecialRules.log`: passed.
  - `EnemyProjectilePerfectFreezeCycleSmokeTests.RunAll` -> `Logs/M52_IceLance_REG_PerfectFreezeCycle.log`: passed.
  - `ProjectileSettlementStaggerSmokeTests.RunAll` -> `Logs/M52_IceLance_REG_SettlementStagger.log`: passed.
- Builder:
  - `TouhouMigrationProjectBuilder.BuildInitialProject` -> `Logs/M52_IceLance_BUILD_BuildInitialProject.log`: completed successfully.
- Post-build focused check:
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M52_IceLance_FINAL_PerfectFreezeEncounter.log`: passed.
- Process check:
  - 2026-06-25 01:01 CST: no real `Unity.app/Contents/MacOS/Unity` process remained.

Known blockers:

- M52 still lacked the snowball pressure state; M53 resolves that intent-arbitration seam, and M54 adds the first rolling snowball runtime.
- The lance reflect success currently consumes the projectile and emits the typed reflect/stun result; visible return-shot VFX, camera kick, and audio remain future presentation work.
- Production HUD/audio/reward consumers for `MigrationPerfectFreezePhaseResult` still remain future work.

Next recommended step:

- Completed by M53/M54/M55/M56/M57: snowball/lance intent arbitration, the first rolling snowball runtime, the director-authored snowball start, boss movement intent for snowball push positioning / close-range evade, and snowball arena damage/bounce now exist. The remaining branch is polished snowball presentation, player-side i-frame ownership, plus production HUD/audio/reward/camera consumers for `MigrationPerfectFreezePhaseResult` and `ReflectStunReady`.

### M51: Ice Lance Reflect Reward Seam

- Date: 2026-06-25 00:14 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Feynman
- Goal: Add the Godot `ice_lance` reflect reward as a Unity-native projectile result and settlement/boss-stun seam, without mechanically copying Godot metadata or mixing the rule into unrelated ice-shard projectiles.

Completed:

- Audited Godot truth:
  - `CirnoDanmakuPatterns.gd` marks `ice_lance` as `reflectable`, `stun_reward`, and `stun_seconds: 2.0`.
  - `Bullet3D.gd` accepts and mirrors `reflectable/stun_reward/stun_seconds` into runtime metadata.
  - the Godot truth probe only requires runtime bullet truth data and nonzero damage, so Unity does not need a Godot-shaped metadata port.
- Added TDD coverage to `EnemyProjectileSpecialRulesSmokeTests`:
  - reflectable ice-lance projectiles consume into a reflected/expired state, emit one typed result, preserve family/source/stun/damage data, and do not emit shatter events.
  - player attack hitboxes attempt reflect before shatter and count reflect separately from shatter.
  - settlement emits one reflect stun seam and the generated ice-lance prefab carries the expected reflect reward data.
  - default enemy projectiles and ice-shard fan projectiles remain not globally reflectable.
- Added `MigrationProjectileReflectResult` as the typed Unity result object for future boss, HUD, audio, camera, and return-shot presentation consumers.
- Expanded `MigrationCombatFeedbackTemplate` with serialized `Reflectable`, `ReflectStunReward`, and `ReflectStunSeconds` data.
- Expanded `MigrationEnemyProjectile`:
  - `ConfigureReflectRules()` stores Unity-native reflect data.
  - `TryReflect()` requires an eligible armed projectile, records source family/direction/stun data, marks the projectile expired for pool reclamation, emits `Reflected`, and avoids shatter HP/events.
  - `Configure()` resets reflect event state for pooled projectile reuse while preserving template reflect rules.
- Expanded `MigrationPlayerAttackHitbox`:
  - reflects eligible projectiles before shatter.
  - derives the reflect direction away from the attacker.
  - exposes `ProjectileReflectEventCount`.
- Expanded `MigrationProjectileSpecialSettlement`:
  - subscribes to projectile `Reflected`.
  - emits `ReflectStunReady(MigrationProjectileReflectResult)` only when the result carries a stun reward.
  - records reflect settlement/stun counters and last stun seconds.
- Expanded `MigrationPerfectFreezeStaggerAdapter` so the existing target-side stun adapter can also consume reflect stun results from the same settlement seam.
- Added `MigrationIceLanceProjectileFeedback.prefab` generation:
  - family `ice_lance`.
  - speed `22.5`, damage `16`, hit radius/visual radius `0.24`.
  - arm delay `0.62s`.
  - reflectable `true`, stun reward `true`, stun seconds `2`.
  - graze and impact feedback still use existing reusable presenter/settlement seams.
- Internet/open-source reference note:
  - `agent-reach doctor --json` reported GitHub served by `gh CLI`.
  - `gh search` reviewed Unity/C# deflect examples, including `Stat1c-Null/MaskOff` `DeflectionHandling.cs`.
  - The open-source reference supported the high-level boundary of attack-window check plus projectile deflection/consumption; no external code was copied.
  - `agent-reach check-update` reported current v1.5.0 is latest.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileReflectResult.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileReflectResult.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyProjectile.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatFeedbackTemplate.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPlayerAttackHitbox.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileSpecialSettlement.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeStaggerAdapter.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyProjectileSpecialRulesSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/ProjectileSettlementStaggerSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- generated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/CombatFeedback/MigrationIceLanceProjectileFeedback.prefab`
- generated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/CombatFeedback/MigrationIceLanceProjectileFeedback.prefab.meta`

Verification:

- M51 red run:
  - `EnemyProjectileSpecialRulesSmokeTests.RunAll` -> `Logs/M51_Reflect_RED_SpecialRules.log`
  - Result: failed as expected on missing reflect template args, projectile reflect properties/methods/events, `MigrationProjectileReflectResult`, and hitbox reflect counter.
- Focused green run:
  - `EnemyProjectileSpecialRulesSmokeTests.RunAll` -> `Logs/M51_Reflect_FINAL_SpecialRules.log`: passed.
- Adjacent regressions:
  - `ProjectileSettlementStaggerSmokeTests.RunAll` -> `Logs/M51_Reflect_FINAL_SettlementStagger.log`: passed.
  - `EnemyProjectilePerfectFreezeCycleSmokeTests.RunAll` -> `Logs/M51_Reflect_FINAL_PerfectFreezeCycle.log`: passed.
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M51_Reflect_FINAL_PerfectFreezeEncounter.log`: passed.
- Builder:
  - `TouhouMigrationProjectBuilder.BuildInitialProject` -> `Logs/M51_Reflect_FINAL_BuildInitialProject.log`: completed successfully.
- Post-build focused check:
  - `EnemyProjectileSpecialRulesSmokeTests.RunAll` -> `Logs/M51_Reflect_FINAL_PostBuild_SpecialRules.log`: passed.
- Process check:
  - 2026-06-25 00:14 CST: no real `Unity.app/Contents/MacOS/Unity` process remained.

Known blockers:

- M51 created the ice-lance prefab and runtime reward seam; the authored `ice_lance` snipe pattern was completed in M52.
- Reflect success currently consumes the enemy projectile and emits a typed result. A visible return-shot projectile/VFX can be spawned by a future presenter or combat consumer without changing the enemy projectile lifecycle.
- Production HUD/audio/camera feedback for reflect stun and phase outcomes remains future work.

Next recommended step:

- Completed by M52: spawn/use `MigrationIceLanceProjectileFeedback.prefab` from an authored Cirno snipe pattern. The remaining branch is production HUD/audio/reward/camera consumers for `MigrationPerfectFreezePhaseResult` and `ReflectStunReady`.

### M50: Prefab-Keyed Projectile Pool Ownership

- Date: 2026-06-25 00:01 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Lagrange
- Goal: Add Unity-native projectile lifecycle ownership through a prefab-keyed pool, using Godot as gameplay-intent reference rather than copying its object creation shape.

Completed:

- Added TDD coverage across three existing smoke surfaces:
  - `EnemyCombatFeedbackTemplateSmokeTests` now proves same-prefab reuse, different-prefab isolation, release-to-inactive behavior, position reset on checkout, projectile state reset through `Configure`, and stale impact-particle shutdown.
  - `PerfectFreezeEncounterSmokeTests` now proves the encounter director binds a pool, spawns the same 18-projectile budget, returns expired active-list projectiles to the pool, reuses the 11 ice-orb instances on the second cast, creates only the 7 new ice-shard instances required by the odd cast, and reuses all 18 needed ice-orb/field instances on the third cast.
  - `EnemyActionTimingSmokeTests` now proves ordinary ranged enemies can check out projectiles from the same pool seam while preserving profile projectile speed and attack damage.
- Added `MigrationPrefabPoolService`:
  - tracks inactive instances by source prefab `GameObject`, not by projectile family or template kind.
  - tracks instance-to-prefab ownership for safe release.
  - exposes `TotalCreatedCount`, `TotalReusedCount`, `TotalGetCount`, `TotalReleasedCount`, `PrefabKeyCount`, `ActiveInstanceCount`, and `InactiveInstanceCount` for smoke tests and future diagnostics.
  - deactivates released instances instead of destroying them when capacity allows.
- Expanded `MigrationEnemyProjectile`:
  - `Configure()` now stops, clears, and hides existing impact particles before a reused projectile becomes active again.
  - gameplay reset authority remains in projectile configuration, while pool ownership stays outside projectile code.
- Expanded `MigrationPerfectFreezeEncounterDirector`:
  - adds `ProjectilePool`, `HasProjectilePool`, and `BindProjectilePool`.
  - checks out Perfect Freeze field, ice-orb, and ice-shard projectile prefabs through the pool when one is bound.
  - returns expired/shattered active-list projectiles to the pool during prune.
  - continues to own scoped settlement wiring, cast budgets, safe-lane cue activation, and phase outcomes.
- Expanded `MigrationSimpleEnemyController`:
  - adds `ProjectilePool`, `HasProjectilePool`, `BindProjectilePool`, and `LastSpawnedProjectile`.
  - ordinary ranged projectiles can now check out from the shared prefab-keyed pool while keeping combat binding and variant speed/damage.
- Updated `TouhouMigrationProjectBuilder`:
  - generated `MigrationPerfectFreezeEncounter.prefab` now carries a local `MigrationPrefabPoolService` component and binds it into the director.
- Integrated Lagrange findings:
  - keep `Configure()` as the reuse reset contract.
  - keep settlement forwarding scoped to the encounter, not global.
  - use prefab keys rather than projectile family names so ice-orb, field, and ice-shard instances never mix.
  - do not auto-release inside projectile hit handling yet, because immediate release would hide impact feedback and change existing readability semantics.
- Internet/open-source reference note:
  - `agent-reach doctor --json` reported GitHub served by `gh CLI`.
  - `gh search` reviewed Unity object-pooling references including `thefuntastic/unity-object-pool` `PoolManager.cs` and `yimengfan/BDFramework.Core` `GameObjectPoolManager.cs`.
  - Both references support the same Unity-native boundary chosen here: prefab-keyed lookup, checkout sets transform and active state, release deactivates and returns to the keyed pool. Reference only; no external code copied.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPrefabPoolService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPrefabPoolService.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyProjectile.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationSimpleEnemyController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeEncounterDirector.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyCombatFeedbackTemplateSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyActionTimingSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/PerfectFreezeEncounterSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Encounters/MigrationPerfectFreezeEncounter.prefab`

Verification:

- M50 red run:
  - `EnemyCombatFeedbackTemplateSmokeTests.RunAll` -> `Logs/M50_PrefabPool_RED_Feedback.log`
  - Result: failed as expected on missing `MigrationPrefabPoolService`, `BindProjectilePool`, `HasProjectilePool`, and `ProjectilePool`.
- Focused green runs:
  - `EnemyCombatFeedbackTemplateSmokeTests.RunAll` -> `Logs/M50_PrefabPool_GREEN2_Feedback.log`: passed.
  - `EnemyActionTimingSmokeTests.RunAll` -> `Logs/M50_PrefabPool_GREEN1_ActionTiming.log`: passed.
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M50_PrefabPool_GREEN1_PerfectFreeze.log`: passed.
- Adjacent regressions:
  - `EnemyProjectilePerfectFreezeCycleSmokeTests.RunAll` -> `Logs/M50_PrefabPool_PerfectFreezeCycle.log`: passed.
  - `EnemyProjectileGrazeSmokeTests.RunAll` -> `Logs/M50_PrefabPool_Graze.log`: passed.
  - `ProjectileSpecialSettlementSmokeTests.RunAll` -> `Logs/M50_PrefabPool_SpecialSettlement.log`: passed.
  - `ProjectileSettlementStaggerSmokeTests.RunAll` -> `Logs/M50_PrefabPool_Stagger.log`: passed.
  - `ProjectileSettlementConsumptionSmokeTests.RunAll` -> `Logs/M50_PrefabPool_Consumption.log`: passed.
- Builder:
  - `TouhouMigrationProjectBuilder.BuildInitialProject` -> `Logs/M50_PrefabPool_BuildInitialProject.log`: passed.
- Final verification after documentation updates:
  - `EnemyCombatFeedbackTemplateSmokeTests.RunAll` -> `Logs/M50_PrefabPool_FINAL_Feedback.log`: passed.
  - `EnemyActionTimingSmokeTests.RunAll` -> `Logs/M50_PrefabPool_FINAL_ActionTiming.log`: passed.
  - `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M50_PrefabPool_FINAL_PerfectFreeze.log`: passed.
  - `TouhouMigrationProjectBuilder.BuildInitialProject` -> `Logs/M50_PrefabPool_FINAL_BuildInitialProject.log`: passed.
  - post-build `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M50_PrefabPool_FINAL_PostBuild_PerfectFreeze.log`: passed.
  - process check at 2026-06-25 00:01 CST: no real `Unity.app/Contents/MacOS/Unity` process remained.
  - `agent-reach check-update`: current v1.5.0 is latest.

Known blockers:

- Perfect Freeze encounter projectiles now return to a prefab-keyed pool when expired or shattered. Player-hit projectiles still keep the existing impact-readability behavior and are not auto-returned inside projectile hit handling.
- Ordinary ranged enemies can check out from a pool, but broad scene-level non-encounter projectile/VFX return ownership is still a future slice.
- Reflect parity, production phase-result consumers, final arena mounting, and production combat HUD/audio/camera feedback remain future work.

Next recommended step:

- M51 should add Bullet3D reflect parity on top of the pooled projectile lifecycle, or mount production HUD/audio/reward/camera consumers for `MigrationPerfectFreezePhaseResult` without breaking `BuildInitialProject` regeneration.

### M49: Perfect Freeze ScriptableObject Phase Plan

- Date: 2026-06-24 23:38 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Rawls
- Goal: Move the now-tested Perfect Freeze phase/cast/outcome constants into a Unity-native data asset while preserving the existing director ownership of spawning, budget enforcement, settlement, cue timing, and phase-finish events.

Completed:

- Expanded `PerfectFreezeEncounterSmokeTests` using TDD:
  - red run failed on missing `MigrationPerfectFreezePhasePlan`, `HasPhasePlan`, `PhasePlan`, and `BindPhasePlan`.
  - green tests prove a runtime-bound phase plan can overwrite stale director values and still produce the existing `11+7` opening field cast and `11+7` second ice-shard cast.
  - builder tests prove `MigrationPerfectFreezeEncounter.prefab` references the generated phase plan asset and that the asset serializes all phase, outcome, and cast-plan values.
- Added `MigrationPerfectFreezePhasePlan`:
  - a `ScriptableObject` with `CreateAssetMenu` support.
  - stores phase HP/duration/cadence/budget, safe-lane cue timing, clear/capture bonuses and stuns, and the ice-orb/field/ice-shard cast-plan inputs.
  - exposes focused configuration methods for builder/test setup without giving the asset authority over projectile spawning or phase result emission.
- Expanded `MigrationPerfectFreezeEncounterDirector`:
  - adds `PhasePlan`, `HasPhasePlan`, `BindPhasePlan`, and `ApplyPhasePlan`.
  - applies the bound plan on bind, awake, and phase begin so prefab-authored data remains authoritative at runtime.
  - preserves the existing direct configuration API for tests and later migration slices.
- Updated `TouhouMigrationProjectBuilder`:
  - ensures `Assets/TouhouMigration/Data/Combat/PerfectFreeze` exists.
  - generates or refreshes `MigrationPerfectFreezePhasePlan.asset`.
  - binds that asset to `MigrationPerfectFreezeEncounter.prefab` while leaving projectile prefab references, scoped settlement, boss target, stagger adapter, and safe-lane cue as MonoBehaviour/prefab responsibilities.
- Open-source reference note:
  - M49 rechecked the previously recorded LoneFighter `BulletPattern`/`PoolService`, KenshiK `PatternSequencer`, and Unite2017 `GameEvent` references. The chosen slice follows the data-driven pattern-plan direction only; pooling and event-channel infrastructure remain separate future seams.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezePhasePlan.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezePhasePlan.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeEncounterDirector.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/PerfectFreezeEncounterSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Data/Combat/PerfectFreeze/MigrationPerfectFreezePhasePlan.asset`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Data/Combat/PerfectFreeze/MigrationPerfectFreezePhasePlan.asset.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Encounters/MigrationPerfectFreezeEncounter.prefab`
- generated scenes/prefabs refreshed by `TouhouMigrationProjectBuilder.BuildInitialProject`

Verification:

- Red: `PerfectFreezeEncounterSmokeTests.RunAll` -> expected compile failure on missing M49 phase plan APIs.
- Green:
  - `PerfectFreezeEncounterSmokeTests.RunAll`
- Adjacent regressions:
  - `EnemyProjectilePerfectFreezeCycleSmokeTests.RunAll`
  - `EnemyProjectileGrazeSmokeTests.RunAll`
  - `ProjectileSpecialSettlementSmokeTests.RunAll`
  - `ProjectileSettlementStaggerSmokeTests.RunAll`
- Regeneration:
  - `TouhouMigrationProjectBuilder.BuildInitialProject`
  - `PerfectFreezeEncounterSmokeTests.RunAll` after regeneration
  - final focused rerun: `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M49_PhasePlan_FINAL.log`

Known follow-up:

- M50 should likely add prefab-keyed projectile/impact pooling, Bullet3D reflect parity, or production consumers for `PhaseFinished`. The phase plan asset is intentionally not a sequencer or pool owner yet.

### M48: Perfect Freeze Phase Outcome Events

- Date: 2026-06-24 23:24 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Volta
- Goal: Finish the first playable Perfect Freeze phase contract by turning Godot clear/capture/timeout semantics into a typed Unity result event that later HUD, reward, stun, camera, and arena systems can consume.

Completed:

- Expanded `PerfectFreezeEncounterSmokeTests` using TDD:
  - red run failed on the missing phase outcome API surface: result object, finish event, last result, event count, player-hit registration, total hit count, and outcome configuration.
  - green tests prove boss defeat with no registered player hits emits one `PhaseFinished` result with `Reason="clear"`, `Captured=true`, clear bonus `70`, capture bonus `100`, stun `4.5s`, hit count `0`, and `NextPhaseIndex=1`.
  - green tests prove boss defeat after `RegisterPlayerHit()` still emits `Reason="clear"` but sets `Captured=false`, grants only clear bonus `70`, uses clear stun `3.5s`, and records phase/total hits.
  - green tests prove `TickPhase(70f)` emits `Reason="timeout"` with no bonus/stun and stops future casts.
- Added `MigrationPerfectFreezePhaseResult`:
  - immutable typed result object for phase reason, capture state, bonuses, stun seconds, phase index, next phase index, phase hit count, total hit count, total capture count, elapsed seconds, and duration seconds.
  - keeps phase outcome data separate from projectile collision, projectile settlement, and boss-control code.
- Expanded `MigrationPerfectFreezeEncounterDirector`:
  - subscribes to the local `MigrationCombatTargetBehaviour.Defeated` event and converts boss defeat into a `clear` phase result.
  - adds `PhaseFinished`, `LastPhaseResult`, `PhaseFinishedEventCount`, `RegisterPlayerHit`, `TotalPlayerHitCount`, `TotalCaptureCount`, and `ConfigurePhaseOutcomes`.
  - finishes phases through one `FinishPhase` path for clear and timeout, hides the safe-lane cue, stops cadence, and prevents additional timed casts after the phase is over.
- Updated `TouhouMigrationProjectBuilder`:
  - serializes Perfect Freeze result values on `MigrationPerfectFreezeEncounter.prefab`: clear bonus `70`, capture bonus `100`, clear stun `3.5s`, and capture stun `4.5s`.
- Integrated Volta findings:
  - Godot keeps the result reason as `clear`; capture is derived from zero player hits during the phase.
  - Player-hit registration happens after actual player health damage, not raw projectile collision.
  - Timeout advances the phase without clear/capture bonus or stun.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezePhaseResult.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezePhaseResult.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeEncounterDirector.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/PerfectFreezeEncounterSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Encounters/MigrationPerfectFreezeEncounter.prefab`
- generated scenes/prefabs refreshed by `TouhouMigrationProjectBuilder.BuildInitialProject`

Verification:

- Red: `PerfectFreezeEncounterSmokeTests.RunAll` -> expected compile failure on missing M48 phase outcome APIs.
- Green:
  - `PerfectFreezeEncounterSmokeTests.RunAll`
- Adjacent regressions:
  - `EnemyProjectilePerfectFreezeCycleSmokeTests.RunAll`
  - `EnemyProjectileGrazeSmokeTests.RunAll`
  - `ProjectileSpecialSettlementSmokeTests.RunAll`
  - `ProjectileSettlementStaggerSmokeTests.RunAll`
- Regeneration:
  - `TouhouMigrationProjectBuilder.BuildInitialProject`
  - `PerfectFreezeEncounterSmokeTests.RunAll` after regeneration
  - final focused rerun: `PerfectFreezeEncounterSmokeTests.RunAll` -> `Logs/M48_PFPhaseOutcome_FINAL.log`

Known follow-up:

- M49 should likely extract the now-tested hardcoded cast plan into ScriptableObject phase/pattern assets, add prefab-keyed pooling for projectile/impact lifecycle, or add Bullet3D reflect parity. The M48 result event still needs production HUD/audio/reward/camera consumers in the final arena slice.

### M47: Perfect Freeze Cast Plan Parity

- Date: 2026-06-24 23:11 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Maxwell and Locke
- Goal: Treat Perfect Freeze as a Unity boss-phase plan rather than a single migrated projectile burst: preserve Godot's player-facing cast order, budget, telegraph, and family semantics while giving Unity separate projectile prefabs and testable director counters.

Completed:

- Expanded `PerfectFreezeEncounterSmokeTests` using TDD:
  - red run failed on missing director APIs for ice-orb/ice-shard prefab binding, cast-plan configuration, zero-based cast index, pattern kind, and per-family counters.
  - green tests prove cast `0` is `orb spread + perfect_freeze_field`: 11 `ice_orb` projectiles plus 7 `frozen_crystal` field projectiles under the total 18-projectile budget, with one safe-lane cue.
  - green tests prove cast `1` is `orb spread + ice_shard_fan`: 11 more `ice_orb` projectiles plus 7 `ice_shard` projectiles under the same budget, with no new Perfect Freeze safe-lane cue.
  - builder test proves generated prefabs wire the three projectile families and serialize the M47 cast-plan values.
- Expanded `MigrationPerfectFreezeEncounterDirector`:
  - adds `iceOrbProjectilePrefab` and `iceShardProjectilePrefab` alongside the existing Perfect Freeze field projectile prefab.
  - adds `ConfigurePerfectFreezeCastPlan`, zero-based `LastCastIndex`, `LastCastPatternKind`, authored counts, and per-family spawned counts.
  - turns timed phase casts into an ordered budget allocator: ice-orb spread consumes the first 11 non-snowball projectile slots, then even casts spend the remaining slots on Perfect Freeze field crystals while odd casts spend them on ice shards.
  - keeps field safe-lane cues only on even field casts, matching Godot's `_last_perfect_freeze_field` cue gate.
- Updated `TouhouMigrationProjectBuilder`:
  - generates `MigrationIceOrbProjectileFeedback.prefab` with family `ice_orb`, speed/damage `6.5/8`, visual radius `0.4`, and `0.32s` arm delay.
  - generates `MigrationIceShardProjectileFeedback.prefab` with family `ice_shard`, speed/damage starting at `10.5/12`, visual radius `0.52`, and `0.42s` arm delay.
  - rewires `MigrationPerfectFreezeEncounter.prefab` to reference the ice-orb, ice-shard, and Perfect Freeze field projectile prefabs and preserve the 11/82, 2x12, 3x6/68 cast plan.
- Integrated Maxwell findings:
  - Godot Perfect Freeze casts are zero-based: even casts append `build_perfect_freeze_field(origin, 2, 12, safe_lane)` after the always-first 11-shot `ice_orb` spread.
  - odd casts append `build_ice_shard_fan(origin, target, 3, 6)` after that same orb spread.
  - `max_bullets_per_cast = 18` is a runtime spawn cap over ordered non-snowball specs, so orbs consume the first 11 slots and the second pattern receives 7 spawned projectiles.
- Integrated Locke's open-source Unity references:
  - short-term: keep the current deterministic `TickPhase` and smoke-testable counters.
  - next-step architecture: extract M47's hardcoded shapes toward ScriptableObject bullet patterns and a phase plan, using open-source Unity bullet-pattern and sequencer repos only as references.
  - next infrastructure seam: prefer prefab-keyed pooling over tag-singleton pooling because these projectiles carry arm delay, graze, shatter, settlement binding, and reset-sensitive event state.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeEncounterDirector.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/PerfectFreezeEncounterSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/CombatFeedback/MigrationIceOrbProjectileFeedback.prefab`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/CombatFeedback/MigrationIceShardProjectileFeedback.prefab`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Encounters/MigrationPerfectFreezeEncounter.prefab`
- generated scenes/prefabs refreshed by `TouhouMigrationProjectBuilder.BuildInitialProject`

Verification:

- Red: `PerfectFreezeEncounterSmokeTests.RunAll` -> expected compile failure on missing M47 director APIs.
- Green: `PerfectFreezeEncounterSmokeTests.RunAll`
- Adjacent regressions:
  - `EnemyProjectilePerfectFreezeCycleSmokeTests.RunAll`
  - `EnemyProjectileGrazeSmokeTests.RunAll`
  - `ProjectileSpecialSettlementSmokeTests.RunAll`
  - `ProjectileSettlementStaggerSmokeTests.RunAll`
- Regeneration:
  - `TouhouMigrationProjectBuilder.BuildInitialProject`
  - `PerfectFreezeEncounterSmokeTests.RunAll` after regeneration

Known follow-up:

- M48 should likely choose one of three Unity-native next moves: phase clear/capture/timeout outcome events, ScriptableObject pattern/phase-plan extraction for the now-tested cast plan, or prefab-keyed projectile pooling. Reflect parity remains the next projectile-special-rule branch.

### M46: Perfect Freeze Projectile Arm Delay

- Date: 2026-06-24 22:56 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Kierkegaard
- Goal: Convert Perfect Freeze `telegraph_seconds` from a purely visible cue into a real Unity projectile danger gate, matching the Godot player-facing intent that fresh bullets are visible before they become active.

Completed:

- Expanded `EnemyProjectilePerfectFreezeCycleSmokeTests` using TDD:
  - red run failed on missing projectile/template arm-delay APIs.
  - green run proves a `0.5s` Perfect Freeze projectile starts unarmed, does not move, does not damage, does not graze, and does not advance spray/freeze timing while unarmed.
  - green run proves the projectile arms after the delay and only then starts movement, hit detection, and Perfect Freeze cycle elapsed time.
  - builder test proves `MigrationPerfectFreezeProjectileFeedback.prefab` serializes the Godot-like `0.5s` arm delay and starts as unarmed.
- Expanded `MigrationEnemyProjectile`:
  - adds `ConfigureArmDelay`, `ArmDelaySeconds`, `ArmDelayRemainingSeconds`, and `IsArmed`.
  - keeps the projectile visible during arm delay but gates movement, player damage, graze, environment hit, and Perfect Freeze cycle timers.
  - serializes initial armed state so generated prefabs can truthfully represent their starting danger state.
- Expanded `MigrationCombatFeedbackTemplate`:
  - adds serialized `armDelaySeconds`.
  - applies arm delay to projectiles through the existing template path.
- Updated `TouhouMigrationProjectBuilder`:
  - generated Perfect Freeze projectile prefab now carries `armDelaySeconds: 0.5`, `isArmed: 0`, and `armDelayRemainingSeconds: 0.5`.
  - default enemy projectile feedback keeps arm delay at `0`, preserving ordinary projectile behavior.
- Integrated Kierkegaard findings:
  - Godot `Bullet3D.arm_delay` disables collision/monitoring and returns before movement, graze, raycast, or Perfect Freeze cycle updates.
  - Perfect Freeze frozen-crystal specs use `telegraph_seconds = 0.5`, passed into `arm_delay`.
  - Godot cast parity and phase outcome data are now documented as M47 candidates rather than hidden chat context.
- Internet/open-source reference note:
  - GitHub code search around Unity telegraph/damage scripts reinforced the same Unity-native split: visual warnings are separate timing/readability surfaces, while the damaging object owns its activation gate. No code was copied.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyProjectile.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatFeedbackTemplate.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyProjectilePerfectFreezeCycleSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/CombatFeedback/MigrationPerfectFreezeProjectileFeedback.prefab`
- generated scenes/prefabs refreshed by `TouhouMigrationProjectBuilder.BuildInitialProject`

Verification:

- `EnemyProjectilePerfectFreezeCycleSmokeTests.RunAll`
- `PerfectFreezeEncounterSmokeTests.RunAll`
- `EnemyProjectileGrazeSmokeTests.RunAll`
- `ProjectileSpecialSettlementSmokeTests.RunAll`
- `ProjectileSettlementStaggerSmokeTests.RunAll`
- `TouhouMigrationProjectBuilder.BuildInitialProject`
- `EnemyProjectilePerfectFreezeCycleSmokeTests.RunAll` after regeneration

Known follow-up:

- M47 resolved zero-based Perfect Freeze cast parity. Phase clear/capture/timeout outcome handling, ScriptableObject pattern-plan extraction, reflect, prefab-keyed pooling, final arena scene, and production HUD remain separate follow-up work.

### M45: Perfect Freeze Safe-Lane Phase Timing

- Date: 2026-06-24 22:44 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Bernoulli
- Goal: Move the Perfect Freeze encounter from a scoped prefab seam toward an actual playable Unity boss-phase contract: safe-lane readability, phase HP/duration, cast cadence, and generated prefab serialization.

Completed:

- Expanded `PerfectFreezeEncounterSmokeTests` using TDD:
  - red run failed on missing safe-lane cue class and phase/cast/cue APIs.
  - green run proves `BeginPerfectFreezePhase` resets the boss target to 300 HP, casts immediately, spawns 18 Perfect Freeze projectiles, activates one safe-lane cue, and then casts again on the 2.2s interval.
  - builder test proves the generated encounter prefab serializes boss target, safe-lane cue, phase max HP 300, duration 70s, interval 2.2s, max projectiles per cast 18, half-angle 22 degrees, cue duration 1.05s, and Godot-like cue color/width.
  - follow-up red run caught a brighter placeholder cue color; the generated prefab and cue default now use RGBA `(1, 0.54, 0.18, 0.3)`.
- Added `MigrationPerfectFreezeSafeLaneCue`:
  - stores the last lane center, target, and direction for tests/debugging.
  - shows a Unity `Renderer` cue for a timed 1.05s window, then hides it without owning projectile damage or boss state.
  - preserves the Godot-intent safe-lane shape as a warm strip cue rather than copying the source implementation shape.
- Expanded `MigrationPerfectFreezeEncounterDirector`:
  - owns phase state, phase timer, cast cooldown, cast counters, and safe-lane cue activation.
  - configures the Perfect Freeze phase as 300 HP, 70s duration, 2.2s cast interval, 18 projectiles per cast, 22 degree safe lane, and 1.05s cue duration.
  - leaves projectile motion/collision/freeze/thaw inside `MigrationEnemyProjectile`, frozen-crystal settlement inside `MigrationProjectileSpecialSettlement`, and boss stun inside `MigrationPerfectFreezeStaggerAdapter`.
- Updated `TouhouMigrationProjectBuilder`:
  - regenerates `MigrationPerfectFreezeEncounter.prefab` with a `PerfectFreezeSafeLaneCue` child.
  - generates the cue width from `tan(22 degrees) * 4.3 * 1.55`, producing `2.6928349` on the prefab.
  - initializes the local boss target at 300 HP and binds the director to both target and cue.
- Integrated Bernoulli findings from the Godot reference:
  - safe lane half angle is 22 degrees.
  - Perfect Freeze phase values are HP 300, duration 70s, pattern interval 2.2s, max 18 bullets per cast, active cap 80.
  - Godot presents the safe lane as a warm rectangular strip, not a wedge; the Unity cue follows that player-facing intent.
- Internet/open-source reference note:
  - GitHub code search found Unity examples where warning/telegraph indicators are independent cue components activated by boss/attack directors, matching the Unity-native split used here. No code was copied.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeEncounterDirector.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeSafeLaneCue.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeSafeLaneCue.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/PerfectFreezeEncounterSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Encounters/MigrationPerfectFreezeEncounter.prefab`
- generated scenes/prefabs refreshed by `TouhouMigrationProjectBuilder.BuildInitialProject`

Verification:

- `PerfectFreezeEncounterSmokeTests.RunAll`
- `EnemyProjectilePerfectFreezeCycleSmokeTests.RunAll`
- `ProjectileSettlementStaggerSmokeTests.RunAll`
- `ProjectileSpecialSettlementSmokeTests.RunAll`
- `ProjectileSettlementConsumptionSmokeTests.RunAll`
- `TouhouMigrationProjectBuilder.BuildInitialProject`
- `PerfectFreezeEncounterSmokeTests.RunAll` after regeneration

Known follow-up:

- M46 resolved projectile arm-delay/damage grace and M47 resolved odd-cast ice-fan alternation. The phase still needs clear/capture outcome handling, ScriptableObject pattern-plan extraction, camera/arena presentation, final HUD feedback, and real pooling/reflect support.

### M44: Perfect Freeze Encounter Seam

- Date: 2026-06-24 22:26 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Wegener
- Goal: Turn the M42/M43 Perfect Freeze projectile and stun seams into a Unity-native encounter prefab/director, while preventing boss-arena crystal streaks from leaking into unrelated scene-level settlement adapters.

Completed:

- Added `PerfectFreezeEncounterSmokeTests` using TDD:
  - red run failed on missing scoped settlement APIs, missing `MigrationPerfectFreezeEncounterDirector`, and missing generated encounter prefab.
  - green run proves a scoped settlement can count 12 frozen crystals and emit one local Perfect Freeze stagger without forwarding to the global settlement.
  - green run proves the director spawns a 12-projectile Perfect Freeze burst from the dedicated projectile prefab, projectiles enter `frozen`, shatter through heavy damage, and stun only the local boss target.
  - builder test proves `MigrationPerfectFreezeEncounter.prefab` serializes the local settlement, boss controller, stagger adapter, projectile prefab reference, active cap 80, and burst count 12.
- Expanded `MigrationProjectileSpecialSettlement`:
  - adds `ConfigureSharedSettlementFallback` and `UsesSharedSettlementFallback`.
  - preserves existing global forwarding as the default for projectile-local components.
  - lets dedicated encounters opt out of implicit global forwarding so their frozen-crystal streaks stay local.
  - counts frozen/ice crystal shatter streaks even when no local Phoenix gauge is bound, allowing boss encounter settlement to exist before final HUD/gauge wiring.
- Added `MigrationPerfectFreezeEncounterDirector`:
  - owns the encounter-local projectile prefab reference, scoped settlement, boss controller, and local stagger adapter.
  - spawns a radial Perfect Freeze burst up to the active projectile cap.
  - binds spawned projectile-local settlement components to the encounter-scoped settlement.
  - keeps projectile motion/collision/state inside `MigrationEnemyProjectile` and keeps boss stun inside the target-side adapter.
- Updated `TouhouMigrationProjectBuilder`:
  - adds `Assets/TouhouMigration/Prefabs/Encounters`.
  - adds `Build Encounter Prefabs`.
  - generates `MigrationPerfectFreezeEncounter.prefab` with a local boss target, local scoped settlement, local `MigrationPerfectFreezeStaggerAdapter`, and a director wired to `MigrationPerfectFreezeProjectileFeedback.prefab`.
- Integrated Wegener findings:
  - Godot's Perfect Freeze player intent is a warm safe lane, slow spray, freeze, thaw, shatterable frozen crystals, and 12 frozen crystal breaks for 1.2s boss stun.
  - Godot values to carry forward for this seam are active cap 80, 12 frozen crystals, and 1.2s stun; phase HP 300, 70s duration, clear/capture rewards, cast cadence, and safe-lane visuals are the next larger boss/arena layer.
- Internet/open-source reference note:
  - GitHub code search found common Unity boss-pattern/projectile-spawner organization where a boss/encounter director owns prefab references, spawn budget, and target bindings while projectiles remain movement/collision event sources. No code was copied.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileSpecialSettlement.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeEncounterDirector.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeEncounterDirector.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/PerfectFreezeEncounterSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/PerfectFreezeEncounterSmokeTests.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- generated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Encounters/MigrationPerfectFreezeEncounter.prefab`
- generated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Encounters/MigrationPerfectFreezeEncounter.prefab.meta`
- generated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Encounters.meta`
- regenerated combat feedback/enemy prefabs and runtime scenes through `BuildInitialProject`

Verification:

- Red check:
  - `TouhouMigration.Editor.Tests.PerfectFreezeEncounterSmokeTests.RunAll` failed before implementation on missing scoped settlement APIs, missing encounter director, and missing generated encounter prefab.
- Green checks:
  - `TouhouMigration.Editor.Tests.PerfectFreezeEncounterSmokeTests.RunAll`
  - `TouhouMigration.Editor.Tests.EnemyProjectilePerfectFreezeCycleSmokeTests.RunAll`
  - `TouhouMigration.Editor.Tests.ProjectileSettlementStaggerSmokeTests.RunAll`
  - `TouhouMigration.Editor.Tests.ProjectileSpecialSettlementSmokeTests.RunAll`
  - `TouhouMigration.Editor.Tests.ProjectileSettlementConsumptionSmokeTests.RunAll`
  - `TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - post-builder `TouhouMigration.Editor.Tests.PerfectFreezeEncounterSmokeTests.RunAll`
- Serialization check:
  - `MigrationPerfectFreezeEncounter.prefab` has `useSharedSettlementFallback: 0`.
  - `MigrationPerfectFreezeEncounterDirector` serializes `projectilePrefab`, `scopedSettlement`, `bossController`, and `staggerAdapter`.
  - `MigrationPerfectFreezeEncounterDirector` serializes `activeProjectileCap: 80` and `burstProjectileCount: 12`.

Known follow-up:

- The encounter prefab is not yet a full Cirno boss arena scene.
- Safe-lane telegraph visuals, phase HP/duration/rewards, pattern cadence, lock-on camera, final arena dressing, real pooling, reflect, and production HUD feedback remain future work.

### M43: Perfect Freeze Projectile Cycle

- Date: 2026-06-24 22:13 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Beauvoir
- Goal: Turn M42's frozen-crystal stagger chain into a projectile-owned Unity lifecycle, so Perfect Freeze bullets naturally become shatterable frozen crystals before thawing back into danger.

Completed:

- Added `EnemyProjectilePerfectFreezeCycleSmokeTests` using TDD:
  - red run failed on missing projectile cycle APIs, template fields, and generated Perfect Freeze projectile prefab.
  - green run proves `spray -> frozen -> thawed` timing, speed, damage, shatterability, family changes, weakness changes, and thaw movement.
  - settlement-chain test proves 12 frozen cycle projectiles can shatter as `frozen_crystal` and produce one Perfect Freeze stagger opportunity.
  - template/builder tests prove cycle data serializes and applies through reusable feedback prefabs.
- Expanded `MigrationEnemyProjectile`:
  - adds `ConfigurePerfectFreezeCycle`, `PerfectFreezeCycleEnabled`, `CurrentPerfectFreezeState`, `IsFrozen`, and cycle timing accessors.
  - advances state at the top of `Tick`, before movement uses speed, matching Godot `Bullet3D._update_perfect_freeze_cycle` placement.
  - spray state uses speed `4.2`, damage `8`, and is not shatterable.
  - frozen state stops movement, uses damage `7`, switches family to `frozen_crystal`, resets shatter HP to `20`, and merges `fire/heavy/shatter` weaknesses.
  - thawed state uses speed `8`, damage `10`, clears frozen/shatterable state, and resumes movement immediately on the thaw frame.
  - enabling the cycle extends lifetime to at least spray + freeze + 1 second, preventing the Unity default 4s lifetime from deleting the projectile exactly as it thaws.
- Expanded `MigrationCombatFeedbackTemplate`:
  - serializes Perfect Freeze cycle flags, timings, speeds, damages, and frozen shatter HP beside existing projectile special-rule data.
  - default values preserve the Godot values from `Bullet3D.gd` and `CirnoDanmakuPatterns.gd`.
- Updated `TouhouMigrationProjectBuilder`:
  - keeps `MigrationEnemyProjectileFeedback.prefab` as a normal projectile with `perfectFreezeCycleEnabled: 0`.
  - adds `MigrationPerfectFreezeProjectileFeedback.prefab` with `templateKind: perfect_freeze_projectile`, 6s lifetime, frozen-crystal family, Perfect Freeze cycle enabled, graze/shatter presenters, and settlement seam.
- Integrated subagent findings:
  - Godot values to preserve are spray `1.6s`, freeze `2.4s`, spray speed `4.2`, spray damage `8`, frozen damage `7`, thaw speed `8`, thaw damage `10`, frozen shatter HP `20`, and 12 frozen crystal breaks for 1.2s stagger.
  - The dedicated Unity prefab needs lifetime longer than the 4s spray+freeze threshold.
  - A future boss encounter should scope settlement/adapters so one shared event does not stun every generated adapter in a scene.
- Internet/open-source reference note:
  - GitHub code search found Unity examples that model frozen projectile behavior as projectile configuration/runtime state (`FrozenProjectile`-style classes). No code was copied; the architectural signal was to keep projectile lifecycle on projectile/template and keep stun/status on the target side.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyProjectile.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatFeedbackTemplate.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyProjectilePerfectFreezeCycleSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyProjectilePerfectFreezeCycleSmokeTests.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/CombatFeedback/MigrationEnemyProjectileFeedback.prefab`
- generated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/CombatFeedback/MigrationPerfectFreezeProjectileFeedback.prefab`
- generated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/CombatFeedback/MigrationPerfectFreezeProjectileFeedback.prefab.meta`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`

Verification:

- Red checks:
  - `TouhouMigration.Editor.Tests.EnemyProjectilePerfectFreezeCycleSmokeTests.RunAll` failed on missing cycle APIs/template fields/prefab before implementation.
  - first implementation red run exposed the 4s lifetime/thaw-frame expiry collision.
- Green checks:
  - `TouhouMigration.Editor.Tests.EnemyProjectilePerfectFreezeCycleSmokeTests.RunAll`
  - `TouhouMigration.Editor.Tests.EnemyProjectileSpecialRulesSmokeTests.RunAll`
  - `TouhouMigration.Editor.Tests.ProjectileSpecialSettlementSmokeTests.RunAll`
  - `TouhouMigration.Editor.Tests.ProjectileSettlementStaggerSmokeTests.RunAll`
  - `TouhouMigration.Editor.Tests.ProjectileSettlementConsumptionSmokeTests.RunAll`
  - `TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
- Serialization check:
  - `MigrationEnemyProjectileFeedback.prefab` keeps `perfectFreezeCycleEnabled: 0`.
  - `MigrationPerfectFreezeProjectileFeedback.prefab` serializes 6s lifetime and the Godot-like Perfect Freeze values `1.6/2.4/4.2/8/7/8/10/20`.

Known follow-up:

- The new Perfect Freeze projectile prefab is not yet mounted into a dedicated boss/arena encounter.
- Reflect, real object pooling/return, boss-scoped settlement/adapters, stronger freeze/thaw VFX/audio/camera feedback, and production HUD feedback remain future work.

### M42: Perfect Freeze Stagger Adapter

- Date: 2026-06-24 21:57 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Mencius and Copernicus
- Goal: Consume the M40 12-frozen-crystal Perfect Freeze stagger output in live Unity gameplay without moving control logic into projectile objects.

Completed:

- Added `ProjectileSettlementStaggerSmokeTests` using TDD:
  - red run failed on missing `MigrationPerfectFreezeStaggerAdapter` plus `MigrationSimpleEnemyController` stun APIs.
  - green run proves 12 `frozen_crystal` shatters emit one settlement event, the adapter consumes it once, the enemy enters `stunned` for 1.2s, stun blocks movement/attacks/damage, expiry returns to `idle`, and AI resumes on the next tick.
  - generated Human Village scene test proves adapters bind the shared projectile settlement and local enemy controllers after `BuildInitialProject`.
- Added `MigrationPerfectFreezeStaggerAdapter`:
  - subscribes to `MigrationProjectileSpecialSettlement.PerfectFreezeStaggerReady`.
  - records `StaggerEventCount` and `LastStaggerSeconds`.
  - resolves the shared scene settlement through `MigrationGlobalUiController` when no explicit binding is serialized.
  - calls the target `MigrationSimpleEnemyController.ApplyStun(seconds)` instead of letting projectiles or settlement own enemy behavior.
- Expanded `MigrationSimpleEnemyController`:
  - adds `ApplyStun`, `IsStunned`, `StunRemainingSeconds`, and `StunEventCount`.
  - cancels active telegraph/active/recovery windows during stun, disables danger-window damage sources, blocks movement/attacks, and resumes from `idle` after the stun timer expires.
  - uses max-duration refresh semantics so repeated Perfect Freeze events refresh/extend rather than shorten the current stun.
- Expanded `MigrationEnemyAnimationBridge`:
  - added a red/green smoke test for `stunned` state presentation.
  - maps `stunned` to the existing stationary `TakeDamage` reaction path so generated enemy AnimatorControllers show a readable hit-reaction instead of ordinary idle.
- Updated `TouhouMigrationProjectBuilder`:
  - generated Human Village `MigrationEnemy_FairyScout` and `MigrationEnemy_BatScout` carry `MigrationPerfectFreezeStaggerAdapter`.
  - `CreateGlobalUi` binds every generated adapter to the shared `MigrationProjectileSpecialSettlement`, matching the player heavy-burst binding pattern.
- Integrated subagent findings:
  - Godot `Bullet3D` only emits shatter-style events; `CirnoMvpArena` is the arena consumer that grants gauge and calls boss stun after 12 frozen crystals.
  - Unity should keep projectile/settlement as event/reward layers and put timed control state on the enemy/boss controller.
- Internet/open-source reference note:
  - GitHub code search found common Unity patterns such as `StunEffect` and `ApplyStun(duration)` in open-source projects (`Darkest-Dungeon-Unity`, `SummonerSiege`, `PinataHell`, and others). No code was copied; the useful architectural signal was to keep stun as a status/control effect on the target entity.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeStaggerAdapter.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPerfectFreezeStaggerAdapter.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationSimpleEnemyController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyAnimationBridge.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/ProjectileSettlementStaggerSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/ProjectileSettlementStaggerSmokeTests.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyAnimationBridgeSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`

Verification:

- Red checks:
  - `TouhouMigration.Editor.Tests.ProjectileSettlementStaggerSmokeTests.RunAll` failed on missing adapter/stun APIs before implementation.
  - `TouhouMigration.Editor.Tests.EnemyAnimationBridgeSmokeTests.RunAll` failed on `stunned` mapping to `Idle` before the bridge update.
- Green checks:
  - `TouhouMigration.Editor.Tests.ProjectileSettlementStaggerSmokeTests.RunAll`
  - `TouhouMigration.Editor.Tests.EnemyAnimationBridgeSmokeTests.RunAll`
  - `TouhouMigration.Editor.Tests.EnemyActionTimingSmokeTests.RunAll`
  - `TouhouMigration.Editor.Tests.EnemyPrefabSmokeTests.RunAll`
  - `TouhouMigration.Editor.Tests.ProjectileSettlementConsumptionSmokeTests.RunAll`
  - `TouhouMigration.Editor.Tests.ProjectileSpecialSettlementSmokeTests.RunAll`
  - `TouhouMigration.Editor.Tests.EnemyProjectileSpecialRulesSmokeTests.RunAll`
  - `TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
- Serialization check:
  - `HumanVillageVerticalSlice.unity` has two `MigrationPerfectFreezeStaggerAdapter` components.
  - both adapters serialize `settlement` to the shared `MigrationGlobalUI` projectile settlement and `enemyController` to their local `MigrationSimpleEnemyController`.

Known follow-up:

- The reusable stun adapter is mounted on Human Village training enemies, not a final Cirno boss prefab yet.
- Perfect Freeze projectile spray/frozen/thaw lifecycle, reflect, real pool return/despawn, camera/audio/VFX emphasis, and real player dash-state binding remain future work.

### M41: Heavy Burst Settlement Consumption

- Date: 2026-06-24 21:41 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Sagan
- Goal: Consume one M40 settlement output in live Unity gameplay by turning the 3-crystal heavy-burst reward into a one-shot heavy attack range multiplier.

Completed:

- Added `ProjectileSettlementConsumptionSmokeTests` using TDD:
  - first red run failed on missing player-action binding, hitbox range multiplier, heavy-burst consumption count, and action last multiplier APIs.
  - second red run failed on missing shared settlement forwarding and generated scene binding.
  - green runs passed after adding one-shot consumption, collider scaling/restoration, shared settlement forwarding, and builder scene wiring.
- Expanded `MigrationProjectileSpecialSettlement`:
  - adds `ConsumePendingHeavyBurstRadiusMultiplier`, `HeavyBurstConsumeCount`, and `LastConsumedHeavyBurstRadiusMultiplier`.
  - supports `BindSharedSettlement` so projectile-local settlement components can forward graze/shatter settlement into a shared scene-level settlement.
  - preserves local settlement behavior when a gauge is explicitly bound for focused tests or standalone use.
- Expanded `MigrationPlayerCombatActionController`:
  - adds `BindProjectileSettlement`, `HasProjectileSettlement`, and `LastHeavyBurstRadiusMultiplier`.
  - consumes the pending heavy-burst multiplier only for heavy attacks.
  - leaves pending heavy-burst rewards intact when light attacks open a window.
- Expanded `MigrationPlayerAttackHitbox`:
  - adds `ConfigureRangeMultiplier` and `CurrentRangeMultiplier`.
  - caches base Box/Sphere/Capsule collider dimensions, applies the multiplier during the active window, and restores neutral dimensions on window completion.
- Updated `MigrationGlobalUiController` and `TouhouMigrationProjectBuilder`:
  - generated runtime scenes now include a shared `MigrationProjectileSpecialSettlement` on `MigrationGlobalUI`.
  - generated player action controllers serialize a reference to the shared settlement in Bamboo Home and Human Village.
- Integrated Sagan findings:
  - heavy-burst consumption was the smallest high-value M41 slice because Unity already had a real player heavy attack chain.
  - boss stagger is valid but should wait for a boss/arena stun adapter.
  - do not put reward consumption back into projectiles; local projectile settlement should forward to shared settlement.
- Internet/open-source reference note:
  - `agent-reach` GitHub/gh CLI searches for Unity heavy attack radius, graze gauge, and boss stagger patterns found no useful licensed implementation for this slice; a few boss-stagger examples appeared, but no code was copied.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileSpecialSettlement.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPlayerCombatActionController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPlayerAttackHitbox.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/ProjectileSettlementConsumptionSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/ProjectileSettlementConsumptionSmokeTests.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`
- regenerated combat feedback/enemy prefabs through `BuildInitialProject`

Verification:

- Focused M41 projectile settlement consumption smoke test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.ProjectileSettlementConsumptionSmokeTests.RunAll`
  - Result: passed, including final post-builder rerun.
- Focused regressions:
  - `ProjectileSpecialSettlementSmokeTests`: passed.
  - `EnemyProjectileSpecialRulesSmokeTests`: passed.
  - `EnemyProjectileGrazeSmokeTests`: passed.
  - `CombatActionRewardSmokeTests`: passed.
  - `CombatBridgeSmokeTests`: passed.
  - `GlobalUiSmokeTests`: passed.
  - `EnemyPrefabSmokeTests`: passed.
  - `EnemyCatalogPrefabSmokeTests`: passed.
- Builder:
  - `TouhouMigrationProjectBuilder.BuildInitialProject`: passed.
- Serialization check:
  - `BambooHomeVerticalSlice.unity` and `HumanVillageVerticalSlice.unity` contain shared `MigrationProjectileSpecialSettlement` components with reward defaults `2/5/8/12`.
  - player action controllers in both scenes serialize `projectileSettlement` references to the shared scene settlement.
  - player attack hitboxes serialize neutral `rangeMultiplier: 1`.

Known blockers:

- At M41 handoff, Perfect Freeze stagger was still exposed but not consumed by a boss/arena stun adapter. M42 resolves this for generated Human Village enemies through `MigrationPerfectFreezeStaggerAdapter`; a dedicated boss/arena mounting remains future work.
- Heavy-burst gameplay now changes hitbox collider range, but VFX scale/audio/camera feedback and authored animation timing are still future work.
- Player dash state is still not wired into graze quality settlement.

Next recommended step:

- Completed by M42: `PerfectFreezeStaggerReady` is consumed through a reusable target-side stun adapter. Remaining alternatives are Perfect Freeze projectile state cycles, reflect, real boss/arena mounting, or real pool owner for projectile/impact feedback.

### M40: Projectile Special Settlement And Phoenix Gauge

- Date: 2026-06-24 21:25 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Banach
- Goal: Convert M38/M39 projectile graze and shatter events into a Unity-native gameplay settlement layer, preserving Godot's Cirno/Phoenix intent while keeping projectile objects as event sources rather than reward owners.

Completed:

- Added `ProjectileSpecialSettlementSmokeTests` using TDD:
  - First red run failed as expected with missing `MigrationPhoenixGaugeRuntime` and `MigrationProjectileSpecialSettlement`.
  - Second red run failed as expected with missing 3-crystal heavy-burst state surfaces.
  - Green runs passed after adding Phoenix gauge rules, graze/shatter settlement, HUD binding, prefab wiring, and ice/frozen crystal counters.
- Added `MigrationPhoenixGaugeRuntime`:
  - preserves Godot-like gauge defaults: 300 max, 100-point segments, 50 start, 45 graze-per-second soft cap, and 20-point hit loss.
  - exposes gauge/segment change events plus attack, graze, hit-loss, spend, reset, and tick APIs.
- Added `MigrationProjectileSpecialSettlement`:
  - subscribes to projectile `Grazed` and `Shattered` events.
  - grants +2 normal graze, +5 dash graze, +8 perfect dash graze, and +12 shatter gauge rewards.
  - maps frozen-crystal shatters to `perfect_freeze_crystal`, tracks every 12 frozen crystals as a 1.2s stagger event, and tracks every 3 ice crystals as a pending 1.25x heavy-burst radius multiplier.
- Updated `MigrationGlobalUiController` and `MigrationHudController`:
  - global runtime owns and ticks the Phoenix gauge.
  - HUD displays `火焰槽 current / max`.
- Updated `TouhouMigrationProjectBuilder`:
  - reusable enemy projectile feedback prefab now carries the settlement seam with defaults `2/5/8/12`, `12` frozen-crystal breaks, and `1.2` stagger seconds.
- Integrated Banach findings:
  - Godot's `Bullet3D` should stay an event source while Phoenix gauge, ice-crystal streaks, and Perfect Freeze stagger live in a settlement subscriber.
  - use Godot values where player-facing balance is already explicit: +2/+5/+8 graze, +12 shatter, 3 ice crystals for heavy burst, 12 frozen crystals for 1.2s stagger.
- Internet/open-source reference note:
  - `agent-reach` GitHub/gh CLI search found `https://github.com/Apptive-Game-Team/GyeMong`, where a Unity `GrazeController` increases a gauge from graze events. The repository has no license, so no code was copied; it was used only as architecture confirmation that graze should feed a separate gauge system.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPhoenixGaugeRuntime.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPhoenixGaugeRuntime.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileSpecialSettlement.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileSpecialSettlement.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationHudController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/ProjectileSpecialSettlementSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/ProjectileSpecialSettlementSmokeTests.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/CombatFeedback/MigrationEnemyProjectileFeedback.prefab`

Verification:

- Focused M40 projectile settlement smoke test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.ProjectileSpecialSettlementSmokeTests.RunAll`
  - Result: passed, including final post-builder rerun.
- Projectile regressions:
  - `EnemyProjectileSpecialRulesSmokeTests`: passed.
  - `EnemyProjectileGrazeSmokeTests`: passed.
- Readability/template regressions:
  - `EnemyCombatReadabilitySmokeTests`: passed.
  - `EnemyCombatFeedbackTemplateSmokeTests`: passed.
- Reward/loot regressions:
  - `CombatActionRewardSmokeTests`: passed.
  - `CombatLootQuestSmokeTests`: passed.
- Enemy/combat regressions:
  - `CombatBridgeSmokeTests`: passed.
  - `EnemyPrefabSmokeTests`: passed.
  - `EnemyCatalogPrefabSmokeTests`: passed.
- Builder:
  - `TouhouMigrationProjectBuilder.BuildInitialProject`: passed.
- Serialization check:
  - `MigrationEnemyProjectileFeedback.prefab` contains `MigrationProjectileSpecialSettlement`, `normalGrazeGauge: 2`, `dashGrazeGauge: 5`, `perfectDashGrazeGauge: 8`, `shatterGauge: 12`, `perfectFreezeStaggerBreaks: 12`, and `perfectFreezeStaggerSeconds: 1.2`.

Known blockers:

- Heavy-burst multiplier and Perfect Freeze stagger are exposed as settlement outputs but not yet consumed by player attack or boss logic.
- `SetPlayerDashing` is a test/control seam; the real player dash state must be wired before dash-graze bonuses become live.
- Reflect, freeze/thaw projectile cycles, camera-facing/fading presenters, and real pooling remain future slices.

Next recommended step:

- M41 follow-up was to consume one settlement output in live gameplay. The heavy-burst branch is now complete in M41; the remaining branches are Perfect Freeze boss/arena stun, reflect/freeze state, or real projectile/impact pooling.

### M39: Enemy Projectile Shatter Special Rules

- Date: 2026-06-24 21:10 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Lovelace
- Goal: Add the first Bullet3D-style projectile special rule beyond graze, using Unity-native projectile events, templates, and player attack hitboxes instead of mechanically copying Godot node metadata.

Completed:

- Added `EnemyProjectileSpecialRulesSmokeTests` using TDD:
  - First red run failed as expected with missing shatter projectile APIs, result DTO, presenter, and template defaults.
  - Second red run failed as expected with missing `MigrationPlayerAttackHitbox.TryHitProjectile` and `ProjectileShatterEventCount`.
  - Green runs passed after adding shatter rules, hitbox routing, presenter, template defaults, and builder wiring.
- Added `MigrationProjectileShatterResult`:
  - reports projectile, projectile family, source family, raw damage, multiplier, applied damage, remaining shatter HP, position, weakness use, and source object.
- Added `MigrationProjectileShatterPresenter`:
  - subscribes to `MigrationEnemyProjectile.Shattered`.
  - displays lightweight TextMesh feedback at the shatter position, with a stronger color for weakness shatters.
- Expanded `MigrationEnemyProjectile`:
  - adds `ConfigureShatterRules`, `TryApplyShatterDamage`, `IsWeakTo`, `Shattered`, `ProjectileFamily`, `Shatterable`, `ShatterHp`, `ShatterWeaknesses`, `IsShattered`, `ShatterEventCount`, `LastShatterSourceFamily`, `LastShatterDamageApplied`, `LastShatterRemainingHp`, and `LastShatterPosition`.
  - applies a 1.5x weakness multiplier for matching source families such as `fire`, `heavy`, or `shatter`.
  - marks shattered projectiles expired, disables their visual danger, spawns the existing impact feedback hook, and emits a one-shot shatter result event.
- Expanded `MigrationPlayerAttackHitbox`:
  - adds `TryHitProjectile` and `ProjectileShatterEventCount`.
  - uses the attack type as the projectile shatter source family.
  - prevents repeat projectile shatter/chip processing within the same attack window.
  - checks projectiles before enemy HP targets in trigger entry handling.
- Expanded `MigrationCombatFeedbackTemplate`:
  - adds serialized projectile special-rule defaults: `ProjectileFamily`, `Shatterable`, `ShatterHp`, and `ShatterWeaknesses`.
- Updated `TouhouMigrationProjectBuilder`:
  - generated enemy projectile feedback prefab serializes `projectileFamily: enemy_projectile` and keeps `shatterable: 0` so normal generated enemy bullets are not globally breakable.
  - projectile feedback prefab carries `MigrationProjectileShatterPresenter`.
- Integrated Lovelace findings:
  - shatter is the best M39 slice because Godot already has complete ice wall/crystal/frozen-crystal player-facing intent, while reflect is mostly metadata and Perfect Freeze is a larger state-machine/arena pass.
  - projectile should emit shatter events and keep reward/gauge/boss settlement out of projectile UI code.
  - keep family strings normalized and avoid making all default enemy bullets shatterable.
- Internet/open-source reference note:
  - `agent-reach` GitHub/gh CLI search found general Unity bullet-hell/pooling references such as `jongallant/Unity-Bullet-Hell`, plus common `UnityEngine.Pool.ObjectPool<T>` projectile examples. No code was copied; these informed the decision to defer real pooling and prioritize the player-visible shatter mechanic first.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileShatterResult.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileShatterResult.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileShatterPresenter.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileShatterPresenter.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyProjectile.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPlayerAttackHitbox.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatFeedbackTemplate.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyProjectileSpecialRulesSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyProjectileSpecialRulesSmokeTests.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/CombatFeedback/MigrationEnemyProjectileFeedback.prefab`

Verification:

- Focused M39 projectile special-rules smoke test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyProjectileSpecialRulesSmokeTests.RunAll`
  - Result: passed, including final post-builder rerun.
- M38 projectile graze regression:
  - `EnemyProjectileGrazeSmokeTests`: passed before and after `BuildInitialProject`.
- Readability/template regressions:
  - `EnemyCombatReadabilitySmokeTests`: passed.
  - `EnemyCombatFeedbackTemplateSmokeTests`: passed.
- Reward/loot regressions:
  - `CombatActionRewardSmokeTests`: passed.
  - `CombatLootQuestSmokeTests`: passed.
- Enemy/combat regressions:
  - `CombatBridgeSmokeTests`: passed.
  - `EnemyPrefabSmokeTests`: passed.
  - `EnemyCatalogPrefabSmokeTests`: passed.
- Builder:
  - `TouhouMigrationProjectBuilder.BuildInitialProject`: passed.
- Serialization check:
  - `MigrationEnemyProjectileFeedback.prefab` contains `projectileFamily: enemy_projectile`, `shatterable: 0`, `shatterHp: 0`, `shatterWeaknesses`, and `MigrationProjectileShatterPresenter`.

Known blockers:

- Shatter currently emits event/presenter data and hitbox routing only. Arena/gauge settlement, Cirno crystal streaks, Perfect Freeze stagger, and CardBuild rewards are future subscriber services.
- Default enemy bullets are intentionally not shatterable. Boss/arena/projectile pattern code must opt into `Shatterable` and weakness data.
- Reflect, Perfect Freeze spray/frozen/thaw cycles, and real object pooling remain future projectile slices.
- Shatter presentation is a lightweight TextMesh placeholder, not final world-space UI, VFX, audio, or camera feedback.

Next recommended step:

- M40 should either wire `Shattered` into a Cirno-style arena/gauge service, implement Perfect Freeze state transitions using the new shatter seam, or add real Unity pooling ownership for projectile/impact feedback.

### M38: Enemy Projectile Graze First Pass

- Date: 2026-06-24 17:54 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Boyle
- Goal: Add the first Bullet3D-style non-damage projectile interaction in Unity: near-miss graze events and feedback, without moving damage, reward, or CardBuild settlement into projectile UI code.

Completed:

- Added `EnemyProjectileGrazeSmokeTests` using TDD:
  - Red run failed as expected with missing `ConfigureGraze`, `MigrationProjectileGrazeResult`, `MigrationProjectileGrazePresenter`, and template graze defaults.
  - Green runs passed after adding projectile graze rules, result DTO, presenter, template defaults, and builder wiring.
- Added `MigrationProjectileGrazeResult`:
  - reports projectile, quality, distance, hit radius, graze radius, perfect graze radius, player position, and closest projectile point.
- Added `MigrationProjectileGrazePresenter`:
  - subscribes to `MigrationEnemyProjectile.Grazed`.
  - displays lightweight TextMesh feedback at the player-position result, distinguishing `Graze` from `Perfect Graze`.
- Expanded `MigrationEnemyProjectile`:
  - adds `ConfigureGraze`, `Grazed`, `GrazeEnabled`, `GrazeRadius`, `PerfectGrazeRadius`, `GrazeEventCount`, `LastGrazeQuality`, and `LastGrazeDistance`.
  - triggers graze only for enemy projectiles when the player is outside `HitRadius` and inside `GrazeRadius`.
  - marks each projectile as already grazed after the first graze, preserving one-shot per projectile behavior for the current single-player slice.
  - uses segment closest-point checks so fast projectiles can be grazed along their movement path.
  - suppresses graze when a player hit occurs.
- Expanded `MigrationCombatFeedbackTemplate`:
  - adds serialized graze defaults: `GrazeEnabled`, `GrazeRadius`, and `PerfectGrazeRadius`.
- Updated `TouhouMigrationProjectBuilder`:
  - generated enemy projectile feedback prefab now serializes graze defaults (`1.15` normal radius and `0.7` perfect radius).
  - projectile feedback prefab carries `MigrationProjectileGrazePresenter`.
- Integrated Boyle findings:
  - preserve Godot's hit-radius exclusion and one-shot projectile/player graze intent.
  - keep projectile as event source and defer gauge/CardBuild reward settlement to later services.
  - attach visual feedback around the player position, matching the Godot Cirno MVP pattern.
- Internet/open-source reference note:
  - `agent-reach` GitHub/gh CLI search found `Axy-sys/nave` (MIT) `GrazingSystem.cs`, whose bullet-hell graze definition matches the Godot intent: larger graze box around a smaller real hitbox, no code copied.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileGrazeResult.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileGrazeResult.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileGrazePresenter.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationProjectileGrazePresenter.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyProjectile.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatFeedbackTemplate.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyProjectileGrazeSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyProjectileGrazeSmokeTests.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/CombatFeedback/MigrationEnemyProjectileFeedback.prefab`

Verification:

- Focused M38 projectile graze smoke test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyProjectileGrazeSmokeTests.RunAll`
  - Result: passed, including final post-builder rerun.
- Readability/template regressions:
  - `EnemyCombatReadabilitySmokeTests`: passed.
  - `EnemyCombatFeedbackTemplateSmokeTests`: passed.
- Reward/loot regressions:
  - `CombatActionRewardSmokeTests`: passed.
  - `CombatLootQuestSmokeTests`: passed.
- Enemy/combat regressions:
  - `CombatBridgeSmokeTests`: passed.
  - `EnemyPrefabSmokeTests`: passed.
  - `EnemyCatalogPrefabSmokeTests`: passed.
- Builder:
  - `TouhouMigrationProjectBuilder.BuildInitialProject`: passed.
- Serialization check:
  - `MigrationEnemyProjectileFeedback.prefab` contains `grazeEnabled: 1`, `grazeRadius: 1.15`, `perfectGrazeRadius: 0.7`, and `MigrationProjectileGrazePresenter`.

Known blockers:

- Graze currently emits event/presenter data only. Gauge, CardBuild ember/cooldown rewards, dash-quality bonuses, and rate limits are future settlement services.
- The one-shot latch is single-player oriented. A future multiplayer/co-op slice would need per-player graze IDs like Godot's `_grazed_player_ids`.
- Shatter, reflect, perfect-freeze, real projectile pooling, and production combat HUD presentation remain future work.

Next recommended step:

- M39 should add either shatter/reflect/perfect-freeze projectile state seams or real projectile/impact pooling ownership, then wire graze settlement into player gauge/CardBuild services.

### M37: Combat Readability Presenters And Environment Impact

- Date: 2026-06-24 17:42 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Hilbert
- Goal: Turn the M36 combat feedback seams into player-facing readability without moving reward, loot, inventory, quest, or damage authority into UI code.

Completed:

- Added `EnemyCombatReadabilitySmokeTests` using TDD:
  - Red run failed as expected with missing `MigrationDamageNumberPresenter`, missing `MigrationCombatRewardPresentation`, and missing projectile environment-impact APIs.
  - Green runs passed after adding damage-number presentation, one-shot reward/loot notifications, environment impact raycasts, and builder wiring.
- Added `MigrationDamageNumberPresenter`:
  - subscribes to `MigrationCombatTargetBehaviour.Damaged`.
  - respects `MigrationGameSettings.ShowDamageNumbers`.
  - creates a lightweight TextMesh marker and records suppressed events when the setting is disabled.
- Added `MigrationCombatRewardPresentation`:
  - subscribes to reward and loot presentation events.
  - displays XP/coin and item notifications without owning player progress, inventory, or quest state.
- Expanded `MigrationCombatDefeatRewardHandler`:
  - emits `RewardsGranted(experience, coins, questCounterId)` after the existing one-shot reward logic runs.
- Expanded `MigrationCombatLootDropHandler`:
  - emits `LootGranted(itemId, amount)` only when an inventory grant succeeds.
  - preserves the existing one-shot defeat guard and quest-kill notification behavior.
- Expanded `MigrationEnemyProjectile`:
  - adds `ConfigureEnvironmentImpact`, `EnvironmentImpactEnabled`, `EnvironmentImpactEventCount`, and `LastEnvironmentImpactPoint`.
  - uses Unity physics raycast sweep from previous to next position to stop on blocking scene geometry before player damage.
  - reuses the existing impact ParticleSystem hook for environment hits.
- Updated `TouhouMigrationProjectBuilder`:
  - projectile feedback prefab now enables environment impact behavior through template application.
  - generated catalog enemies and Human Village training enemies carry damage-number and reward/loot presentation components.
- Integrated Hilbert findings:
  - preserve Godot `Bullet3D` intent as spatial, readable danger rather than a mere damage object.
  - keep core damage/reward/loot services authoritative and use event consumers for Unity presentation.
  - defer graze, shatter, reflect, and perfect-freeze behavior to future extension-point slices.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationDamageNumberPresenter.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationDamageNumberPresenter.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatRewardPresentation.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatRewardPresentation.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatDefeatRewardHandler.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatLootDropHandler.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyProjectile.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyCombatReadabilitySmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyCombatReadabilitySmokeTests.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/CombatFeedback/MigrationEnemyProjectileFeedback.prefab`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Enemies/MigrationEnemy_*.prefab`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`

Verification:

- Focused M37 combat readability smoke test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyCombatReadabilitySmokeTests.RunAll`
  - Result: passed, including final post-builder rerun.
- M36 regression:
  - `EnemyCombatFeedbackTemplateSmokeTests`: passed before and after `BuildInitialProject`.
- Reward/loot regressions:
  - `CombatActionRewardSmokeTests`: passed.
  - `CombatLootQuestSmokeTests`: passed.
- Enemy/combat regressions:
  - `CombatBridgeSmokeTests`: passed.
  - `EnemyPrefabSmokeTests`: passed.
  - `EnemyCatalogPrefabSmokeTests`: passed.
- Builder:
  - `TouhouMigrationProjectBuilder.BuildInitialProject`: passed.

Known blockers:

- Damage/reward notifications are TextMesh placeholders, not final combat HUD/world-space UI.
- Projectile environment collision is a first raycast sweep and does not yet implement ricochet, reflect, freeze, shatter, or graze.
- `PoolingReady` remains metadata; no actual Unity object pool owns projectile or impact lifecycle yet.
- Death feedback still lacks corpse fade, animation completion, and final pickup/XP gem presentation.

Next recommended step:

- M38 should add Bullet3D-style graze and special projectile states on top of the new environment-impact/readability seams, then replace the temporary TextMesh presenters with camera-facing or UI Toolkit/uGUI production presentation.

### M36: Reusable Combat Feedback Templates And Hurt Reaction

- Date: 2026-06-24 17:27 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Einstein
- Goal: Move M35 feedback from runtime-only markers toward reusable Unity gameplay assets, while adding fast-projectile anti-tunneling, impact feedback, and target hurt reaction hooks.

Completed:

- Added `EnemyCombatFeedbackTemplateSmokeTests` using TDD:
  - Red run failed as expected with missing `MigrationCombatFeedbackTemplate`, `MigrationCombatHurtFeedback`, projectile template/sweep/impact APIs, controller projectile-prefab seam, and defeat-handler death-prefab seam.
  - Green runs passed after adding the reusable feedback templates, projectile sweep/impact, hurt flash/knockback, and builder wiring.
- Added `MigrationCombatFeedbackTemplate`:
  - records template kind, pooling-ready flag, layer policy, lifetime, visual radius/color, impact feedback, and sweep collision flags.
  - provides a reusable way for runtime logic to consume feedback prefab metadata without copying Godot singleton call shapes.
- Added `MigrationCombatHurtFeedback`:
  - subscribes to `MigrationCombatTargetBehaviour.Damaged`.
  - flashes renderer material color for a short duration.
  - exposes a lightweight knockback hook and last knockback direction.
- Expanded `MigrationEnemyProjectile`:
  - adds `ApplyFeedbackTemplate`, `UsesFeedbackTemplate`, `PoolingReady`, `SweepCollisionEnabled`, `ImpactEventCount`, and `HasActiveImpactFeedback`.
  - performs segment-distance checks between previous and current positions when sweep collision is enabled, covering fast projectiles that cross the player between ticks.
  - spawns a ParticleSystem impact feedback hook on hit while keeping damage authority in `MigrationCombatRuntime`.
- Expanded `MigrationSimpleEnemyController`:
  - adds a serialized projectile prefab seam and `HasProjectilePrefab`.
  - spawned ranged projectiles now instantiate the configured feedback prefab when present, then apply the embedded template.
- Expanded `MigrationCombatDefeatHandler`:
  - adds `ConfigureDeathFeedbackPrefab` and `HasDeathFeedbackPrefab`.
  - death feedback can now come from a reusable prefab, with the old generated ParticleSystem path preserved as fallback.
- Updated `TouhouMigrationProjectBuilder`:
  - adds `BuildCombatFeedbackPrefabs`.
  - generates three reusable prefabs under `Assets/TouhouMigration/Prefabs/CombatFeedback`: enemy projectile, melee danger, and enemy death feedback.
  - generated enemy prefabs now attach `MigrationCombatHurtFeedback`.
  - ranged generated enemies serialize a projectile feedback prefab reference.
  - generated defeat handlers serialize a death feedback prefab reference.
- Integrated Einstein findings:
  - preserve Godot's intent that feedback is gameplay readability, not decoration.
  - avoid recreating Godot `/root/VFX3D` singleton shape; use Unity prefab/template/event consumers instead.
  - keep Bullet3D full graze/shatter/perfect-freeze behavior as incremental extension points, not a single risky port.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatFeedbackTemplate.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatHurtFeedback.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyProjectile.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationSimpleEnemyController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatDefeatHandler.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyCombatFeedbackTemplateSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyCombatFeedbackTemplateSmokeTests.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/CombatFeedback/MigrationEnemyProjectileFeedback.prefab`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/CombatFeedback/MigrationEnemyProjectileFeedback.prefab.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/CombatFeedback/MigrationMeleeDangerFeedback.prefab`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/CombatFeedback/MigrationMeleeDangerFeedback.prefab.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/CombatFeedback/MigrationEnemyDeathFeedback.prefab`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/CombatFeedback/MigrationEnemyDeathFeedback.prefab.meta`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Enemies/MigrationEnemy_*.prefab`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`

Verification:

- Focused M36 combat feedback template smoke test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyCombatFeedbackTemplateSmokeTests.RunAll`
  - Result: passed, including final post-builder rerun.
- Adjacent regressions:
  - `EnemyCombatFeedbackSmokeTests`: passed.
  - `EnemyActionTimingSmokeTests`: passed.
  - `EnemyPrefabSmokeTests`: passed.
  - `CombatBridgeSmokeTests`: passed.
  - `EnemyAnimationBridgeSmokeTests`: passed.
  - `EnemyCatalogPrefabSmokeTests`: passed.
- Builder:
  - `TouhouMigrationProjectBuilder.BuildInitialProject`: passed.
- Serialization check:
  - `Assets/TouhouMigration/Prefabs/CombatFeedback` contains the three generated feedback prefabs and `.meta` files.
  - `MigrationEnemyProjectileFeedback.prefab` contains `templateKind: enemy_projectile`, `poolingReady: 1`, `impactFeedbackEnabled: 1`, and `sweepCollisionEnabled: 1`.
  - `MigrationEnemy_Bat.prefab` contains serialized `projectilePrefab`, `deathFeedbackPrefab`, and `MigrationCombatHurtFeedback`.
  - `HumanVillageVerticalSlice.unity` contains projectile/death feedback prefab references and `MigrationCombatHurtFeedback` for generated scene enemies.
- Internet/open-source reference note:
  - `agent-reach` GitHub CLI searches for Unity projectile pooling/trails/impact, material hit flash, and raycast projectile patterns returned no useful matches in this environment.
  - Web search was used for general Unity projectile/pooling/raycast pattern confirmation; implementation used Unity-native component seams rather than copying external code.

Known blockers:

- `PoolingReady` is now explicit metadata, but there is no real pool allocator/return-to-pool service yet.
- Projectile sweep currently covers player crossing by segment distance; it does not yet raycast scene geometry or emit environment impact results.
- Damage numbers are still settings data only; no runtime presenter consumes `ShowDamageNumbers`.
- Hurt feedback is first-pass material flash plus transform knockback; no hit pause, camera shake, audio, or per-enemy material override strategy exists yet.
- Death feedback has a reusable prefab seam, but no corpse fade, animation-complete callback, or reward pickup presentation.
- Bullet3D graze, shatter, reflect, freeze, and damage-family rules remain future work.

Next recommended step:

- M37 should connect the combat feedback seams to player-facing reward/readability: damage numbers, XP/loot pickup or notification feedback, environment projectile impact, and the first Bullet3D extension points.

### M35: Enemy Combat Feedback And Death Presentation

- Date: 2026-06-24 17:10 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Curie
- Goal: Make enemy combat readable in Unity: attacks should be visible and windowed, projectiles should be inspectable game objects, damage should emit presentation events, and death should show feedback during delayed cleanup.

Completed:

- Added `EnemyCombatFeedbackSmokeTests` using TDD:
  - Initial red run failed as expected with missing `MigrationCombatTargetBehaviour.Damaged` / `DamageEventCount`.
  - Later red run failed as expected with missing `MigrationCombatDefeatHandler.ConfigureDeathFeedback`.
  - Animation bridge red run failed as expected when non-lethal damage left the bridge in `Attack` instead of `TakeDamage`.
  - Green runs passed after adding visible danger windows, projectile feedback/lifetime, damage events, death feedback, and bridge subscriptions.
- Expanded `MigrationEnemyDamageSource`:
  - adds `ConfigureWindowing(bool requiresActiveWindow, bool visibleWhenInactive)`.
  - exposes `RequiresActiveWindow`, `IsWindowActive`, and `WindowBlockedCount`.
  - disables colliders and renderers outside active melee windows for generated melee-capable enemies.
- Expanded `MigrationSimpleEnemyController`:
  - turns melee damage sources on during active frames and off during recovery/reset/defeat.
  - configures spawned enemy projectiles with visible red mesh/trail feedback and a lifetime.
- Expanded `MigrationEnemyProjectile`:
  - creates a lightweight Unity mesh renderer on the projectile root plus a `TrailRenderer`.
  - adds `ConfigureFeedback`, `LifetimeSeconds`, `IsExpired`, `HasVisualFeedback`, and `ExpiredEventCount`.
  - expires after lifetime without destroying editor-test objects.
- Expanded `MigrationCombatTargetBehaviour` and `MigrationEnemyAnimationBridge`:
  - target damage now emits a `Damaged` event and `DamageEventCount`.
  - the bridge subscribes to damage and defeat, triggering `TakeDamage` for non-lethal and lethal hits and preserving one-shot `Die`.
- Expanded `MigrationCombatDefeatHandler`:
  - adds `ConfigureDeathFeedback`, `DeathFeedbackEnabled`, `HasActiveDeathFeedback`, `DeathFeedbackProgress`, and `DeathFeedbackStartedCount`.
  - disables active damage sources when defeat starts.
  - plays a simple Unity `ParticleSystem` burst during the configured defeat delay, then performs existing renderer/collider cleanup.
- Updated `TouhouMigrationProjectBuilder`:
  - generated catalog enemy prefabs serialize active-window melee damage sources and death feedback.
  - Human Village generated enemies also receive death feedback and delayed cleanup.
- Integrated Curie findings:
  - preserve Godot's gameplay intent, not node shape: readable attacks, spatial danger/projectiles, hit confirmation, death confirmation, and one-shot kill reward routes.
  - keep Bullet3D raycast/graze/shatter/impact extensions as later Unity-native projectile refinements.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatTargetBehaviour.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyDamageSource.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyProjectile.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationSimpleEnemyController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatDefeatHandler.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyAnimationBridge.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyCombatFeedbackSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyCombatFeedbackSmokeTests.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyAnimationBridgeSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Enemies/MigrationEnemy_*.prefab`
- regenerated `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`

Verification:

- Focused M35 combat feedback smoke test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyCombatFeedbackSmokeTests.RunAll`
  - Result: passed, including final post-builder rerun.
- Adjacent enemy regressions:
  - `EnemyActionTimingSmokeTests`: passed.
  - `EnemyPrefabSmokeTests`: passed.
  - `CombatBridgeSmokeTests`: passed.
  - `EnemyAnimationBridgeSmokeTests`: passed, including final post-builder rerun.
  - `EnemyCatalogPrefabSmokeTests`: passed.
- Builders:
  - `TouhouMigrationProjectBuilder.BuildEnemyCatalogPrefabs`: passed.
  - `TouhouMigrationProjectBuilder.BuildInitialProject`: passed.
- Serialization check:
  - `MigrationEnemy_Bat.prefab` contains `deathFeedbackEnabled: 1`, `requiresActiveWindow: 1`, `visibleWhenInactive: 0`, `windowActive: 0`, `attackActiveSeconds: 0.12`, and `defeatDelaySeconds: 0.45`.
  - `HumanVillageVerticalSlice.unity` contains `deathFeedbackEnabled: 1` and active-window damage source flags for generated enemies.
- Internet/open-source reference note:
  - `agent-reach` GitHub search route was consulted; `gh search code` returned no useful results for the narrow Unity projectile/hurtbox queries in this environment, so the slice used common Unity-native component patterns rather than copying external code.
  - `agent-reach check-update` reports current version `v1.5.0` is latest.

Known blockers:

- Projectile feedback is still generated at runtime on the projectile root, not authored as a reusable prefab/pool with layer policy.
- Melee danger volumes are generated primitive markers; they need authored materials, shape tuning, and possibly animation-event timing in a later pass.
- Death feedback is a simple ParticleSystem burst during the delay, not a final death animation, fade, corpse, pooled despawn, XP gem, or loot notification presentation.
- `TakeDamage` is now triggered, but material flash, hit pause, knockback, damage numbers, and camera/audio feedback remain future work.
- Bullet3D special rules such as raycast sweep, graze, shatter, perfect-freeze interactions, and rich impact VFX remain pending.

Next recommended step:

- M36 should promote the runtime feedback seams into reusable projectile/danger/death feedback assets, add hit flash/knockback or damage-number hooks, and begin Bullet3D-specific projectile fidelity without regressing the one-shot reward/loot/quest bridge.

### M34: Enemy Action Timing And Death Delay

- Date: 2026-06-24 16:47 CST
- Status: Complete for this slice
- Owner: Codex
- Goal: Move generated enemies from "animations can be requested" toward readable Unity gameplay by adding deterministic telegraph, active, recovery, and delayed defeat-cleanup windows without copying Godot node structure.

Completed:

- Added `EnemyActionTimingSmokeTests` using TDD:
  - Red run failed as expected with `Missing method TouhouMigration.Runtime.Combat.MigrationSimpleEnemyController.ConfigureActionTimings`.
  - Green runs passed after adding action phase timing and configurable defeat delay.
- Expanded `MigrationSimpleEnemyController`:
  - adds `ConfigureActionTimings(telegraphSeconds, activeSeconds, recoverySeconds)`.
  - exposes `CurrentActionPhase`, `ActionTelegraphEventCount`, `ActionActiveEventCount`, and `ActionRecoveryEventCount`.
  - uses `telegraph` before applying melee damage or spawning ranged projectiles.
  - keeps damage/projectile application single-shot during the active window.
  - enters a visible `recovery` state before returning to idle action phase.
  - preserves old default behavior when active/recovery timing is not configured.
- Expanded `MigrationCombatDefeatHandler`:
  - adds `ConfigureDefeatDelay(delaySeconds)`, `IsDefeatPending`, `DefeatDelayRemaining`, and `DefeatDelaySeconds`.
  - default delay remains `0`, preserving older immediate-disable tests.
  - configured generated enemies keep renderers/colliders alive during a short death delay before cleanup.
- Updated `TouhouMigrationProjectBuilder`:
  - generated enemy prefabs now serialize `attackActiveSeconds: 0.12` and `attackRecoverySeconds: 0.28`.
  - generated enemy prefabs now serialize `defeatDelaySeconds: 0.45`.
  - all 20 generated catalog enemy prefabs get the action timing and death delay settings, including fallback `vampire`.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationSimpleEnemyController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatDefeatHandler.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyActionTimingSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyActionTimingSmokeTests.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Enemies/MigrationEnemy_*.prefab`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/Bootstrap.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/TitleScreen.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/MokouCharacterValidation.unity`

Verification:

- M34 red Enemy Action Timing test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyActionTimingSmokeTests.RunAll`
  - Result: failed as expected with `Missing method TouhouMigration.Runtime.Combat.MigrationSimpleEnemyController.ConfigureActionTimings`.
- Focused prefab builder:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildEnemyCatalogPrefabs`
  - Result: builder completed successfully.
- Green Enemy Action Timing test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyActionTimingSmokeTests.RunAll`
  - Result: passed.
- Enemy adjacent regressions:
  - `EnemyPrefabSmokeTests`, `CombatBridgeSmokeTests`, `EnemyAnimationBridgeSmokeTests`, and `EnemyCatalogPrefabSmokeTests`: passed.
- Full builder:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: builder completed successfully.
- Post-builder M34 timing check:
  - `EnemyActionTimingSmokeTests`: passed again after `BuildInitialProject`.
- Local serialization checks:
  - generated enemy prefabs with `attackActiveSeconds: 0.12`: `20`.
  - generated enemy prefabs with `defeatDelaySeconds: 0.45`: `20`.

Known blockers:

- M34 timing windows are still code-driven timers, not animation-event-authored frames.
- No visible melee danger volume, projectile trail/model, hurt flash, knockback feedback, death VFX, or fade-out is complete yet.
- Ranged projectiles still use transient runtime `GameObject` creation and lack pooling/lifetime visuals.
- Movement remains direct transform movement, not NavMeshAgent/AI Navigation.

Next recommended step:

- M35 should add visible combat feedback around M34: projectile prefab visuals/lifetime, melee danger volume presentation, hurt flash/knockback hooks, and death VFX or fade-out that respects the delayed cleanup window.

### M33: Runtime Enemy Animation Bridge

- Date: 2026-06-24 16:31 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Dirac, Meitner, and Pasteur
- Goal: Turn the M32 monster animation libraries into a Unity-native runtime presentation seam, while keeping gameplay state as the source of truth and treating Godot as gameplay intent rather than implementation shape.

Completed:

- Added `EnemyAnimationBridgeSmokeTests` using TDD:
  - Red run failed as expected with `Runtime enemy animation bridge type should exist. Expected: True. Actual: False.`
  - Green runs passed after adding the runtime bridge, controller events, generated Animator parameters/transitions, and prefab builder wiring.
- Added `MigrationEnemyAnimationBridge`:
  - binds `MigrationSimpleEnemyController`, `MigrationCombatTargetBehaviour`, `MigrationEnemyAnimationSource`, and the child visual `Animator`.
  - maps controller states to Unity presentation states: `Idle`, `Move`, `Attack`, `Projectile`, and `Die`.
  - records testable counters for attack, projectile, take-damage, and death triggers.
  - writes Animator-friendly parameters when they exist: `IsMoving`, `MotionState`, `Attack`, `Projectile`, `TakeDamage`, and `Die`.
- Updated `MigrationSimpleEnemyController`:
  - added state-change, melee-attack, projectile-attack, and windup-start events.
  - kept deterministic damage/projectile timing in gameplay code for this slice.
  - avoided making Animator the AI authority.
- Updated `TouhouMigrationProjectBuilder`:
  - generated enemy AnimatorControllers now include basic parameters and transitions.
  - `Idle` and `Move` transition through `IsMoving`.
  - `Attack`, `Projectile`, `TakeDamage`, and `Die` are trigger-addressable from AnyState where clips exist.
  - every non-fallback generated enemy prefab with a real controller receives `MigrationEnemyAnimationBridge`.
  - `vampire` stays explicit animation fallback and does not pretend to have bridge/controller support.
- Integrated external/open-source research:
  - Unity `Gamekit3D` and Unity Open Project patterns support the decision that gameplay state should request animation, while Animator/StateMachineBehaviour/animation events report timing back.
  - Unity AI Navigation/NavMeshComponents and Cinemachine should remain adapters around gameplay state, not dependencies inside enemy damage logic.
  - future data-driven enemy work should start thin, then graduate to `EnemyArchetypeSO`, `AttackPatternSO`, `ProjectileSO`, and `PerceptionSO` only after the current controller seam proves itself.
- Integrated Godot source-intent findings:
  - formal enemies are meant to support short combat-room waves, ranged keep-away, readable attack danger windows, enemy projectiles, hurt flash/knockback, death rewards, loot, and quest kill progress.
  - Unity should preserve those player-facing rules but not copy Godot `Beehave`, `CharacterBody3D`, `Area3D`, autoload, or collision-layer shapes directly.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyAnimationBridge.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyAnimationBridge.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationSimpleEnemyController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyAnimationBridgeSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyAnimationBridgeSmokeTests.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Animations/Enemies/<PascalId>/<PascalId>_Enemy.controller`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Enemies/MigrationEnemy_*.prefab`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/Bootstrap.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/TitleScreen.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/MokouCharacterValidation.unity`

Verification:

- M33 red Enemy Animation Bridge test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyAnimationBridgeSmokeTests.RunAll`
  - Result: failed as expected with `Runtime enemy animation bridge type should exist. Expected: True. Actual: False.`
- Focused prefab builder:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildEnemyCatalogPrefabs`
  - Result: builder completed successfully.
- Green Enemy Animation Bridge test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyAnimationBridgeSmokeTests.RunAll`
  - Result: passed.
- Enemy adjacent regressions:
  - `EnemyAnimationPrefabSmokeTests`, `EnemyVisualPrefabSmokeTests`, `EnemyCatalogPrefabSmokeTests`, and `EnemyPrefabSmokeTests`: passed.
- Full builder:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: builder completed successfully.
- Post-builder M33 bridge check:
  - `EnemyAnimationBridgeSmokeTests`: passed again after `BuildInitialProject`.
- Final handoff M33 bridge check:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyAnimationBridgeSmokeTests.RunAll`
  - Result: passed.
- Local serialization checks:
  - generated enemy prefabs with `MigrationEnemyAnimationBridge`: `19`.
  - `MigrationEnemy_Bat.prefab` contains `MigrationEnemyAnimationBridge` and non-fallback animation source metadata.
  - `MigrationEnemy_Vampire.prefab` still records `usesFallbackAnimations: 1` and has no animation bridge.
  - `Bat_Enemy.controller` contains `IsMoving`, `MotionState`, `Attack`, `Projectile`, `TakeDamage`, and `Die` parameters plus trigger/locomotion transitions.

Known blockers:

- M33 does not yet use animation events or StateMachineBehaviour to open hitbox windows or spawn projectiles; gameplay timing remains deterministic in `MigrationSimpleEnemyController`.
- Take-damage triggers are parameterized but not yet wired from target damage events.
- `MigrationCombatDefeatHandler` can still hide renderers immediately on defeat, cutting off the visible death animation.
- Movement remains direct transform movement, not NavMeshAgent/AI Navigation.
- Projectiles are still runtime-created logic objects without prefab visuals, pooling, lifetime cleanup, raycast sweep, graze, or shatter/reflect rules.

Next recommended step:

- M34 should create a thin enemy action/timing layer and use it to make one melee and one ranged enemy feel readable in Unity: telegraph, active window, recovery, hurt feedback, death delay, and reward preservation. Keep the Godot rules as intent, but use Unity-native hit volumes, projectiles, Animator callbacks, and later NavMesh adapters.

### M32: Generic Monster Animation Import And Controller Binding

- Date: 2026-06-24 14:23 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Godel; Chandrasekhar was still running at documentation time and was not used as evidence
- Goal: Import formal monster animation FBX clips into Unity, configure them as creature/Generic animation sources, generate first-pass AnimatorControllers, and bind those controllers to generated enemy prefab visuals.

Completed:

- Added `EnemyAnimationPrefabSmokeTests` using TDD:
  - Red run failed as expected with `Builder should expose a focused batchmode entry point for enemy animation import/controller generation. Expected: True. Actual: False.`
  - Green runs passed after adding the builder entry point, Generic importer configuration, controller generation, prefab Animator binding, and animation source metadata.
- Promoted monster animation source assets into Unity:
  - copied 220 `*@*.fbx` monster animation files into `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Animations/Enemies/<PascalId>/Clips`.
  - generated 19 first-pass AnimatorControllers at `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Animations/Enemies/<PascalId>/<PascalId>_Enemy.controller`.
- Added `MigrationEnemyAnimationSource`:
  - records `VariantId`, `AnimatorControllerAssetPath`, role clip paths for `Idle`, `Move`, `Attack`, `Projectile`, `TakeDamage`, `Die`, and `UsesFallbackAnimations`.
  - exposes boolean role helpers so tests and later runtime bridges can distinguish complete/partial animation mappings.
- Updated `TouhouMigrationProjectBuilder`:
  - adds `BuildEnemyAnimationControllers()` as a focused batchmode/editor entry point.
  - runs enemy animation import/controller generation before enemy prefab generation in both `BuildInitialProject()` and `BuildEnemyCatalogPrefabs()`.
  - configures monster animation FBXs as `ModelImporterAnimationType.Generic`, not Humanoid.
  - loops idle/move clips and keeps attack/projectile/take-damage/die one-shot.
  - generates a simple state-only AnimatorController with states `Idle`, `Move`, `Attack`, `Projectile`, `TakeDamage`, and `Die` when role clips exist.
  - binds the generated controller to the imported `Visual/VisualModel` Animator for generated enemy prefabs.
- Integrated Godel findings:
  - prefer `In Place` locomotion clips for controller-driven movement.
  - prefer `Projectile Attack` when present, otherwise `Cast Spell`.
  - `Bird` uses `Bird@Fly Idle.fbx`, not `Bird@Idle.fbx`.
  - `Mushroom` and `Chick` locomotion are jump clips despite the catalog/simple movement foundation.
  - `Egg` only has `Egg@Idle.fbx`, `Egg@Shake.fbx`, and `Egg@Spawn.fbx`; M32 does not silently swap in `Egglet@*` clips because that is a different model/rig policy decision.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyAnimationSource.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyAnimationSource.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyAnimationPrefabSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyAnimationPrefabSmokeTests.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Animations/Enemies/<PascalId>/Clips/*.fbx`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Animations/Enemies/<PascalId>/<PascalId>_Enemy.controller`
- Unity-generated `.meta` files under `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Animations/Enemies`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Enemies/MigrationEnemy_*.prefab`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/MokouCharacterValidation.unity`

Verification:

- M32 red Enemy Animation test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyAnimationPrefabSmokeTests.RunAll`
  - Result: failed as expected with `Builder should expose a focused batchmode entry point for enemy animation import/controller generation. Expected: True. Actual: False.`
- Focused animation builder:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildEnemyAnimationControllers`
  - Result: builder completed successfully.
- Focused prefab builder:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildEnemyCatalogPrefabs`
  - Result: builder completed successfully.
- Full builder:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: builder completed successfully.
- Green Enemy Animation Prefab test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyAnimationPrefabSmokeTests.RunAll`
  - Result: passed.
- Enemy adjacent regression:
  - `EnemyVisualPrefabSmokeTests`, `EnemyCatalogPrefabSmokeTests`, and `EnemyPrefabSmokeTests`: passed.
- Local asset count:
  - promoted monster animation FBX files: `220`.
  - generated enemy AnimatorControllers: `19`.
  - Unity-generated `.meta` files under `Animations/Enemies`: `277`.
- Representative serialization check:
  - `MigrationEnemy_Bat.prefab` contains `MigrationEnemyAnimationSource`, `animatorControllerAssetPath: Assets/TouhouMigration/Animations/Enemies/Bat/Bat_Enemy.controller`, role clip paths for `Bat@Idle`, `Bat@Fly Forward In Place`, `Bat@Bite Attack`, `Bat@Projectile Attack`, `Bat@Take Damage`, and `Bat@Die`, plus a `VisualModel` Animator controller reference.
  - `MigrationEnemy_Spider.prefab` contains `Spider@Crawl Forward Slow In Place` locomotion and `Spider@Projectile Attack`.
  - `MigrationEnemy_Vampire.prefab` contains empty animation paths and `usesFallbackAnimations: 1`.
- Full M32 Unity smoke regression:
  - `EnemyAnimationPrefabSmokeTests`, `EnemyVisualPrefabSmokeTests`, `EnemyCatalogPrefabSmokeTests`, `EnemyPrefabSmokeTests`, `CombatLootQuestSmokeTests`, `CombatActionRewardSmokeTests`, `CombatBridgeSmokeTests`, `PlayerHealthBuffSmokeTests`, `PlayerBuffSmokeTests`, `CookingBuffSmokeTests`, `SaveInventorySmokeTests`, `GlobalUiSmokeTests`, `FoundationSmokeTests`, `ContentSmokeTests`, `QuestJournalSmokeTests`, `CookingQuestSmokeTests`, `BondQuestSmokeTests`, `SocialGiftSmokeTests`, `SocialLoopSmokeTests`, `DialogueSmokeTests`, `CookingServiceSmokeTests`, and `CardBuildSmokeTests`: passed.
  - Result marker: `M32_FULL_SMOKE_REGRESSION_PASSED`.
- Godot source tree check:
  - `/Users/Shared/Touhougodot` still shows only `assets/unity_imports/README_MIGRATED_TO_UNITY.md` modified.

Known blockers:

- The generated AnimatorControllers are state libraries, not yet driven by `MigrationSimpleEnemyController` state changes or animation events.
- Root-motion `W Root` clips are copied but not selected for controller-driven movement.
- No transitions, parameters, blend trees, attack event markers, hurt/death VFX events, or projectile animation event timing are wired yet.
- `Egg` animation mapping is intentionally partial; using `Egglet@*` clips would be a separate model/rig swap decision.
- `vampire` remains animation fallback because the formal source scene is still missing.

Next recommended step:

- Add a runtime animation bridge that maps `MigrationSimpleEnemyController` states to Animator parameters/triggers, then use animation events or timed adapters for attack/projectile windows without breaking the existing deterministic controller tests.

### M31: Real Monster Visual Sources For Generated Enemy Prefabs

- Date: 2026-06-24 14:09 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Halley and Erdos
- Goal: Replace the M30 primitive-only enemy prefab placeholders with real imported visual model children for every formal monster that has a Godot source scene, while keeping the missing `vampire` source explicit as fallback.

Completed:

- Added `EnemyVisualPrefabSmokeTests` using TDD:
  - Red run failed as expected with `Primary model file should be promoted for 'bat'. Expected: True. Actual: False.`
  - A second red run, after Halley's source audit, failed as expected because `Bat` was using the wrong primary texture: expected `Vampire Bat.png`, actual `Bat Emission.png`.
  - Green runs passed after promoting source assets, adding visual-source metadata, mounting imported model children, and correcting scene-referenced texture mapping.
- Promoted real monster visual source assets into Unity:
  - 19 primary FBX files from `/Users/Shared/Touhougodot/assets/monsters/<id>/<PascalId>.fbx`.
  - 31 runtime texture files from the same monster asset folders.
  - 19 generated Unity material assets under `Assets/TouhouMigration/Art/Enemies/<PascalId>/Materials`.
- Added `MigrationEnemyVisualSource`:
  - serializes `VariantId`, `GodotScenePath`, `UnityModelAssetPath`, `PrimaryTextureAssetPath`, `TextureAssetPaths`, and `UsesFallbackVisual`.
  - gives smoke tests and future tools a stable way to distinguish real imported visuals from fallback placeholders.
- Updated `TouhouMigrationProjectBuilder` enemy prefab generation:
  - creates `Visual/VisualModel` child hierarchy for every non-fallback generated enemy prefab.
  - hides the primitive root renderer when a real model is mounted.
  - removes colliders from imported model children so gameplay collision stays owned by the migration enemy root and damage-source adapters.
  - normalizes imported visual bounds into a first-pass enemy-sized presentation.
  - generates a simple material per enemy and assigns the primary scene-referenced texture.
  - keeps `vampire` as explicit fallback because `/Users/Shared/Touhougodot/scenes/monsters/VampireMonster.tscn` is missing.
- Integrated Halley findings:
  - all 19 existing monster scenes use `Skeleton3D` and `AnimationPlayer`; none has scene signal connection blocks.
  - `BatMonster.tscn` uses `Bat.fbx` but references `Vampire Bat.png` and `Vampire Bat Emission.png`, so M31 maps those textures instead of the similarly named `Bat.png`.
  - `mushroom` and `egg` are cataloged as `walk` but expose jump clips in their scenes; keep that for the animation/locomotion pass.
  - `vampire` lacks a formal monster scene; only partial related assets exist.
- Integrated Erdos findings:
  - `CreateEnemyPrefabRoot(profile)` is the correct generated-prefab seam.
  - `BuildEnemyCatalogPrefabs()` remains the focused regeneration entry point.
  - smoke coverage now verifies real visual child renderers, no imported visual colliders, hidden primitive root renderers, and M30 catalog field preservation.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyVisualSource.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyVisualSource.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyVisualPrefabSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyVisualPrefabSmokeTests.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Art/Enemies/<PascalId>/Models/<PascalId>.fbx`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Art/Enemies/<PascalId>/Textures/*.png`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Art/Enemies/<PascalId>/Materials/<PascalId>_Visual.mat`
- Unity-generated `.meta` files under `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Art/Enemies`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Enemies/MigrationEnemy_*.prefab`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/MokouCharacterValidation.unity`

Verification:

- M31 primary model red test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyVisualPrefabSmokeTests.RunAll`
  - Result: failed as expected with `Primary model file should be promoted for 'bat'. Expected: True. Actual: False.`
- M31 Bat texture red test:
  - same command.
  - Result: failed as expected with `Bat primary texture should match the texture referenced by BatMonster.tscn... Expected: .../Vampire Bat.png. Actual: .../Bat Emission.png.`
- Focused prefab builder:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildEnemyCatalogPrefabs`
  - Result: builder completed successfully.
- Full builder:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: builder completed successfully.
- Green Enemy Visual Prefab test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyVisualPrefabSmokeTests.RunAll`
  - Result: passed.
- M30/M29 enemy regressions:
  - `EnemyCatalogPrefabSmokeTests`: passed.
  - `EnemyPrefabSmokeTests`: passed.
- Local asset count:
  - promoted primary FBX files: `19`.
  - promoted runtime texture files, excluding `.meta`: `31`.
  - generated enemy visual material assets: `19`.
- Representative serialization check:
  - `MigrationEnemy_Bat.prefab` contains `Visual`, `MigrationEnemyVisualSource`, `unityModelAssetPath: Assets/TouhouMigration/Art/Enemies/Bat/Models/Bat.fbx`, `primaryTextureAssetPath: Assets/TouhouMigration/Art/Enemies/Bat/Textures/Vampire Bat.png`, and `usesFallbackVisual: 0`.
  - `MigrationEnemy_Vampire.prefab` contains `MigrationEnemyVisualSource`, an empty `unityModelAssetPath`, and `usesFallbackVisual: 1`.
- Full M31 Unity smoke regression:
  - `EnemyVisualPrefabSmokeTests`, `EnemyCatalogPrefabSmokeTests`, `EnemyPrefabSmokeTests`, `CombatLootQuestSmokeTests`, `CombatActionRewardSmokeTests`, `CombatBridgeSmokeTests`, `PlayerHealthBuffSmokeTests`, `PlayerBuffSmokeTests`, `CookingBuffSmokeTests`, `SaveInventorySmokeTests`, `GlobalUiSmokeTests`, `FoundationSmokeTests`, `ContentSmokeTests`, `QuestJournalSmokeTests`, `CookingQuestSmokeTests`, `BondQuestSmokeTests`, `SocialGiftSmokeTests`, `SocialLoopSmokeTests`, `DialogueSmokeTests`, `CookingServiceSmokeTests`, and `CardBuildSmokeTests`: passed.
  - Result marker: `M31_FULL_SMOKE_REGRESSION_PASSED`.
- Godot source tree check:
  - `/Users/Shared/Touhougodot` still shows only `assets/unity_imports/README_MIGRATED_TO_UNITY.md` modified.

Known blockers:

- Enemy animation clips are not yet imported/configured into Unity AnimatorControllers. Do not force the monster FBXs through the Mokou humanoid importer path; monster rigs need a separate Generic/legacy animation policy.
- The visual material pass is intentionally simple and only assigns primary albedo-style textures. Emission maps are preserved in `MigrationEnemyVisualSource.TextureAssetPaths` but not yet wired into material emission.
- `vampire` remains fallback because the formal Godot monster scene is missing. A later slice should decide whether to author a new Unity fallback from partial Vampire Bat assets or add a formal source scene.
- Enemy prefab visuals are generated; do not hand-edit `MigrationEnemy_*.prefab` expecting changes to survive `BuildEnemyCatalogPrefabs()`.
- NavMesh movement, behavior-tree parity, Bullet3D special rules, VFX, damage numbers, and combat-session/rank settlement remain pending.

Next recommended step:

- Import/configure the monster animation clips and create a first Generic enemy AnimatorController policy for idle/move/attack/take-damage/die, then connect animation state names to `MigrationSimpleEnemyController` without breaking the catalog/profile seams.

### M30: Generated Enemy Catalog Prefabs

- Date: 2026-06-24 13:56 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Dalton; Gibbs timed out and was closed without usable findings, so Codex completed the monster scene source check directly
- Goal: Generate one Unity prefab placeholder for every formal Godot `MonsterDatabase` enemy profile and expose an independent batchmode builder for those prefabs.

Completed:

- Added `EnemyCatalogPrefabSmokeTests` using TDD:
  - Red run failed as expected with `Enemy prefab folder should contain one prefab per formal Godot monster. Expected: 20. Actual: 0.`
  - Entry-point red run failed as expected with `Builder should expose a batchmode entry point for enemy catalog prefab generation. Expected: True. Actual: False.`
  - Green run passed after adding the prefab builder entry point, catalog enumeration, generated prefab folder, and generated enemy prefabs.
- Expanded `MigrationEnemyCatalog`:
  - keeps the formal records in deterministic registration order.
  - exposes `GetAllProfiles()` so editor tooling can generate assets without hardcoding ids in the builder.
- Updated `TouhouMigrationProjectBuilder`:
  - adds `Assets/TouhouMigration/Prefabs/Enemies` to the managed folder set.
  - calls `CreateEnemyCatalogPrefabs()` during `BuildInitialProject()`.
  - exposes `BuildEnemyCatalogPrefabs()` for focused batchmode/editor regeneration.
  - clears only generated `MigrationEnemy_*.prefab` files in the generated enemy prefab folder before regeneration.
  - checks the `PrefabUtility.SaveAsPrefabAsset` return value and fails hard if Unity does not save a prefab.
- Generated 20 catalog-backed Unity enemy prefab placeholders:
  - `MigrationEnemy_Bat.prefab`
  - `MigrationEnemy_Bee.prefab`
  - `MigrationEnemy_Bird.prefab`
  - `MigrationEnemy_Bumble.prefab`
  - `MigrationEnemy_Chick.prefab`
  - `MigrationEnemy_Egg.prefab`
  - `MigrationEnemy_Fledgling.prefab`
  - `MigrationEnemy_Fungi.prefab`
  - `MigrationEnemy_Ghost.prefab`
  - `MigrationEnemy_Mushroom.prefab`
  - `MigrationEnemy_Phantom.prefab`
  - `MigrationEnemy_Seed.prefab`
  - `MigrationEnemy_Shade.prefab`
  - `MigrationEnemy_Shadow.prefab`
  - `MigrationEnemy_Spider.prefab`
  - `MigrationEnemy_Spook.prefab`
  - `MigrationEnemy_Sprout.prefab`
  - `MigrationEnemy_Sting.prefab`
  - `MigrationEnemy_Toadstool.prefab`
  - `MigrationEnemy_Vampire.prefab`
- Each generated prefab has a catalog-applied `MigrationSimpleEnemyController`, `MigrationCombatTargetBehaviour`, defeat handler, reward handler, loot handler, and melee damage marker when the source profile can melee.
- Representative prefab serialization now preserves formal profile shape:
  - `MigrationEnemy_Bat.prefab`: `currentVariantId: bat`, `maxHp: 45`, `canShoot: 1`, `projectileSpeed: 8`, `currentAttackDamage: 12`.
  - `MigrationEnemy_Egg.prefab`: `currentVariantId: egg`, `maxHp: 100`.
- Source scene check:
  - 19 formal Godot monster scenes exist under `/Users/Shared/Touhougodot/scenes/monsters`.
  - `vampire` has no `/Users/Shared/Touhougodot/scenes/monsters/VampireMonster.tscn`; keep it as a procedural/fallback prefab until a source asset is found.
  - `BatMonster.tscn` has a real FBX, texture, emission texture, `ModelContainer`, `Skeleton3D`, `MeshInstance3D`, and `AnimationPlayer`, so it remains the best first real-visual enemy import candidate.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/Touhougodot/assets/unity_imports/README_MIGRATED_TO_UNITY.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyCatalog.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyCatalogPrefabSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyCatalogPrefabSmokeTests.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Enemies/MigrationEnemy_*.prefab`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Enemies/MigrationEnemy_*.prefab.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Prefabs/Enemies.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/MokouCharacterValidation.unity`

Verification:

- M30 enemy prefab folder red test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyCatalogPrefabSmokeTests.RunAll`
  - Result: failed as expected with `Enemy prefab folder should contain one prefab per formal Godot monster. Expected: 20. Actual: 0.`
- M30 enemy prefab builder entry-point red test:
  - same command.
  - Result: failed as expected with `Builder should expose a batchmode entry point for enemy catalog prefab generation. Expected: True. Actual: False.`
- Builder:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: builder completed successfully.
- Focused prefab builder:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildEnemyCatalogPrefabs`
  - Result: builder completed successfully.
- Green Enemy Catalog Prefab test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyCatalogPrefabSmokeTests.RunAll`
  - Result: passed.
- Local prefab count:
  - `find Assets/TouhouMigration/Prefabs/Enemies -maxdepth 1 -name 'MigrationEnemy_*.prefab' | wc -l`
  - Result: `20`.
- Representative prefab serialization check:
  - `MigrationEnemy_Bat.prefab` contains `maxHp: 45`, `currentVariantId: bat`, `canShoot: 1`, `currentAttackDamage: 12`.
  - `MigrationEnemy_Egg.prefab` contains `maxHp: 100`, `currentVariantId: egg`.
- Full M30 Unity smoke regression:
  - `EnemyCatalogPrefabSmokeTests`, `EnemyPrefabSmokeTests`, `CombatLootQuestSmokeTests`, `CombatActionRewardSmokeTests`, `CombatBridgeSmokeTests`, `PlayerHealthBuffSmokeTests`, `PlayerBuffSmokeTests`, `CookingBuffSmokeTests`, `SaveInventorySmokeTests`, `GlobalUiSmokeTests`, `FoundationSmokeTests`, `ContentSmokeTests`, `QuestJournalSmokeTests`, `CookingQuestSmokeTests`, `BondQuestSmokeTests`, `SocialGiftSmokeTests`, `SocialLoopSmokeTests`, `DialogueSmokeTests`, `CookingServiceSmokeTests`, and `CardBuildSmokeTests`: passed.
  - Result marker: `M30_FULL_SMOKE_REGRESSION_PASSED`.
- Unity-origin asset relocation check:
  - `/Users/Shared/Touhougodot/assets/unity_imports` contains only `README_MIGRATED_TO_UNITY.md`.

Known blockers:

- The 20 prefabs are generated primitive placeholders, not real imported monster visuals.
- `vampire` has no formal Godot source monster scene and should remain fallback/procedural until a source asset is found.
- `Assets/TouhouMigration/Prefabs/Enemies` is a generated folder: do not mix hand-authored prefabs named `MigrationEnemy_*.prefab` into it.
- Enemy movement still uses the simple controller, not Unity NavMesh or Beehave/behavior-tree parity.
- `Bullet3D` special rules, graze/perfect-freeze physics, damage numbers, death VFX, and combat-session/rank settlement remain pending.

Next recommended step:

- Map/import real visual assets for the 19 existing Godot monster scenes into Unity prefab visuals, define the `vampire` fallback decision, then add NavMesh/behavior-tree movement and richer Bullet3D parity behind the existing catalog/profile seams.

### M29: Formal Enemy Catalog And First Ranged Projectile Enemy

- Date: 2026-06-24 13:42 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Wegener and Galileo
- Goal: Port the formal Godot `MonsterDatabase` enemy catalog into Unity records and add the first ranged/projectile enemy path using the `bat` variant.

Completed:

- Extended `EnemyPrefabSmokeTests` using TDD:
  - Red run failed as expected with `Missing required type: TouhouMigration.Runtime.Combat.MigrationEnemyCatalog, Assembly-CSharp`.
  - Scene red run failed as expected with `Human Village should contain a catalog-backed ranged bat enemy. Expected: True. Actual: False.`
  - Green run passed after adding catalog records, ranged projectile behavior, builder scene wiring, and scene regeneration.
- Added `MigrationEnemyCatalog`:
  - loads the 20 formal Godot `MonsterDatabase.gd` records.
  - preserves id, display name, move style, Godot scene path, HP, damage, speed, XP, melee/ranged flags, model scale, and float height.
  - exposes `LoadGodotDefaults()`, `Count`, and `GetProfile(string id)`.
- Expanded `MigrationEnemyVariantProfile`:
  - now carries formal monster metadata: display name, move style, Godot scene path, XP, `CanMelee`, `CanShoot`, model scale, float height, projectile speed, and ranged keep-away distance.
  - adds `ConfigureGodotMonster(...)` for catalog-backed profiles.
- Expanded `MigrationSimpleEnemyController`:
  - supports `BindCombat(MigrationCombatRuntime)`.
  - adds ranged states `ranged_attack` and `ranged_reposition`.
  - uses Godot ranged range `8.0`, keep-away threshold `5.0`, projectile speed `8.0`, and attack windup `0.5s` from the Godot source.
  - tracks `ProjectileEventCount`.
  - preserves existing melee/no-windup behavior and keeps ranged fire independent from melee damage-source binding.
- Added `MigrationEnemyProjectile`:
  - stores speed, damage, enemy-projectile flag, and hit count.
  - moves along a configured direction and routes player hits through `MigrationCombatRuntime.ApplyDamageToPlayer`.
- Updated `TouhouMigrationProjectBuilder`:
  - keeps the existing `MigrationEnemy_FairyScout` training enemy.
  - adds `MigrationEnemy_BatScout` using `MigrationEnemyCatalog.GetProfile("bat")`.
  - scene serialization now includes `currentVariantId: bat`, `canShoot: 1`, `projectileSpeed: 8`, and `rangedMinDistance: 5`.
- Integrated Wegener findings:
  - `MonsterDatabase.gd` is the formal 3D enemy catalog and is autoloaded in `project.godot`.
  - `bat` is the best first ranged variant because it is the first ranged entry, has a real `BatMonster.tscn`, exercises `float_height`, and has `can_shoot = true`.
  - `VampireMonster.tscn` is missing in Godot and would fall back to procedural visuals; preserve that caveat for the full catalog pass.
- Integrated Galileo findings:
  - catalog records should stay plain code/loaded records for now, not inspector-authored Unity assets.
  - ranged fire must not depend on `MigrationEnemyDamageSource`, because the projectile path owns damage delivery.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyCatalog.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyCatalog.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyProjectile.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyProjectile.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyVariantProfile.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationSimpleEnemyController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyPrefabSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/MokouCharacterValidation.unity`

Verification:

- M29 catalog red Enemy Prefab test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyPrefabSmokeTests.RunAll`
  - Result: failed as expected with `Missing required type: TouhouMigration.Runtime.Combat.MigrationEnemyCatalog, Assembly-CSharp`.
- M29 scene red Enemy Prefab test:
  - same command.
  - Result: failed as expected with `Human Village should contain a catalog-backed ranged bat enemy. Expected: True. Actual: False.`
- Builder:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: builder completed successfully.
- Scene serialization check:
  - `HumanVillageVerticalSlice.unity` contains `MigrationEnemy_BatScout`, `currentVariantId: bat`, `canShoot: 1`, `projectileSpeed: 8`, and `rangedMinDistance: 5`.
- Green Enemy Prefab test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyPrefabSmokeTests.RunAll`
  - Result: passed.
- Full M29 Unity smoke regression:
  - `EnemyPrefabSmokeTests`, `CombatLootQuestSmokeTests`, `CombatActionRewardSmokeTests`, `CombatBridgeSmokeTests`, `PlayerHealthBuffSmokeTests`, `PlayerBuffSmokeTests`, `CookingBuffSmokeTests`, `SaveInventorySmokeTests`, `GlobalUiSmokeTests`, `FoundationSmokeTests`, `ContentSmokeTests`, `QuestJournalSmokeTests`, `CookingQuestSmokeTests`, `BondQuestSmokeTests`, `SocialGiftSmokeTests`, `SocialLoopSmokeTests`, `DialogueSmokeTests`, `CookingServiceSmokeTests`, and `CardBuildSmokeTests`: passed.
  - Result marker: `M29_FULL_SMOKE_REGRESSION_PASSED`.

Known blockers:

- The catalog is code-authored. A later slice should promote it to JSON or Unity assets once the schema stabilizes.
- Only `bat` has a live scene instance and ranged projectile behavior. The other 19 formal records are data-only in Unity for now.
- Projectile movement is testable and service-routed, but not yet physics-layer/raycast/graze/perfect-freeze parity with Godot `Bullet3D.gd`.
- `MigrationEnemy_BatScout` uses a primitive placeholder visual; real `BatMonster.tscn` model/prefab parity is still pending.
- Navigation still uses direct movement, not Unity NavMesh or full Beehave/NavigationAgent3D parity.

Next recommended step:

- Promote all 20 catalog variants into generated Unity enemy prefab placeholders with formal visual/model mapping, then add NavMesh-style movement and richer projectile/Bullet3D parity before starting combat-session/rank settlement.

### M28: Enemy Variant Profile And Attack Windup

- Date: 2026-06-24 13:20 CST
- Status: Complete for this slice
- Owner: Codex; earlier read-only subagent Rawls was unavailable after context compaction, so Codex completed the source audit directly
- Goal: Move the first reusable enemy root from loose manual tuning to an authored variant profile that can configure stats, loot classification, damage, cooldown, and Godot-style attack windup timing.

Completed:

- Extended `EnemyPrefabSmokeTests` using TDD:
  - Red run failed as expected with `Missing method TouhouMigration.Runtime.Combat.MigrationSimpleEnemyController.BindLootDropHandler`.
  - Green runs passed after adding variant profiles, loot binding, windup timing, builder wiring, and scene regeneration.
- Added `MigrationEnemyVariantProfile`:
  - captures variant id, enemy type, elemental group, max HP, chase range, attack range, move speed, attack damage, attack cooldown, attack windup, and forced loot-table mode.
  - normalizes ids and clamps unsafe numeric values at the Unity service edge.
- Expanded `MigrationSimpleEnemyController`:
  - binds `MigrationCombatLootDropHandler`.
  - applies a profile to target HP, movement, damage source, cooldown, windup, and Godot loot-table classification.
  - exposes serialized `currentVariantId` so scene instances retain the active variant label after reload.
  - adds deterministic windup state timing before attacks while preserving immediate attacks for old no-windup configurations.
  - tracks `WindupEventCount` for smoke coverage and cancels windup on defeat.
- Updated `TouhouMigrationProjectBuilder`:
  - configures `MigrationEnemy_FairyScout` through a `fairy_scout` profile instead of scattered movement/loot/damage calls.
  - persists `attackWindupSeconds: 0.2` and `currentVariantId: fairy_scout` into `HumanVillageVerticalSlice.unity`.
- Source audit result:
  - Godot `Enemy3D.gd` applies monster data for HP, damage, speed, XP, `can_shoot`, `can_melee`, and float height.
  - Godot melee attack windup is represented by `MeleeAttackAction` and `EnemyAttack.gd` at `0.5` seconds.
  - Godot death emits `SignalBus.enemy_killed(self, xp_value, global_position)` before death animation/VFX cleanup.
  - M28 covers the reusable profile/timing foundation; ranged shots, nav/pathing, full death VFX, and behavior-tree parity remain separate slices.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyVariantProfile.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyVariantProfile.cs.meta`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationSimpleEnemyController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyPrefabSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/MokouCharacterValidation.unity`

Verification:

- M28 red Enemy Prefab test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyPrefabSmokeTests.RunAll`
  - Result: failed as expected with `Missing method TouhouMigration.Runtime.Combat.MigrationSimpleEnemyController.BindLootDropHandler`.
- Builder:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: builder completed successfully.
- Scene serialization check:
  - `HumanVillageVerticalSlice.unity` contains `attackWindupSeconds: 0.2` and `currentVariantId: fairy_scout`.
- Green Enemy Prefab test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyPrefabSmokeTests.RunAll`
  - Result: passed.
- Full M28 Unity smoke regression:
  - `EnemyPrefabSmokeTests`, `CombatLootQuestSmokeTests`, `CombatActionRewardSmokeTests`, `CombatBridgeSmokeTests`, `PlayerHealthBuffSmokeTests`, `PlayerBuffSmokeTests`, `CookingBuffSmokeTests`, `SaveInventorySmokeTests`, `GlobalUiSmokeTests`, `FoundationSmokeTests`, `ContentSmokeTests`, `QuestJournalSmokeTests`, `CookingQuestSmokeTests`, `BondQuestSmokeTests`, `SocialGiftSmokeTests`, `SocialLoopSmokeTests`, `DialogueSmokeTests`, `CookingServiceSmokeTests`, and `CardBuildSmokeTests`: passed.
  - Result marker: `M28_FULL_SMOKE_REGRESSION_PASSED`.

Known blockers:

- Variant profiles are still code/builder-authored. A later slice should promote formal enemy data into Unity assets or JSON-backed records instead of hardcoding profiles in the builder.
- The controller still uses simple direct movement, not Unity NavMesh or full Godot `NavigationAgent3D` parity.
- Godot ranged `_shoot`, projectile config, Beehave tree behavior, contact-damage nuance, hurt flash, knockback, death animation/VFX, XP gem pickup, and combat-session/rank settlement remain unmigrated.
- Attack windup is timer-driven and testable, but not yet driven by animation events or authored attack timelines.

Next recommended step:

- Port the formal enemy catalog into Unity variant records and add at least one ranged/projectile enemy path, then decide whether first production AI should stay as a lightweight project state machine or adopt a Unity behavior-tree package behind an adapter.

### M27: Reusable Simple Enemy Controller

- Date: 2026-06-24 09:33 CST
- Status: Complete for this slice
- Owner: Codex; attempted read-only subagent Cicero but it timed out and was closed without findings
- Goal: Replace the Human Village combat target/damage-source loose pieces with the first reusable Unity enemy controller that can idle, chase, attack through the existing damage source, and stop when defeated.

Completed:

- Added `EnemyPrefabSmokeTests` using TDD:
  - Red run failed as expected with `Missing required type: TouhouMigration.Runtime.Combat.MigrationSimpleEnemyController`.
  - Scene red run failed as expected because Human Village did not mount the reusable enemy controller.
  - Green run passed after implementing the controller, builder wiring, and scene regeneration.
- Added `MigrationSimpleEnemyController`:
  - binds to `MigrationCombatTargetBehaviour` and `MigrationEnemyDamageSource`.
  - exposes `ConfigureMovement`, `ConfigureAttackCooldown`, `BindTarget`, `BindDamageSource`, and deterministic `Tick(deltaTime, playerPosition)` for smoke tests.
  - supports runtime `Update()` by finding the tagged player for first-slice scene behavior.
  - tracks `idle`, `chase`, `attack`, and `defeated` state.
  - moves toward the player inside chase range and attacks inside attack range.
  - routes attack damage through the existing `MigrationEnemyDamageSource`, preserving the M24/M22 player-health path.
  - stops movement and attacks after the bound combat target is defeated.
- Updated `TouhouMigrationProjectBuilder`:
  - replaced the old loose `CombatTargetDummy` name with `MigrationEnemy_FairyScout`.
  - mounts `MigrationSimpleEnemyController` on the enemy root.
  - keeps existing target HP, defeat handler, reward handler, loot handler, and damage source adapters.
  - moves `EnemyDamageSourceMarker` under the enemy root and binds it to the controller.
  - regenerated Unity scenes through the builder.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationSimpleEnemyController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/EnemyPrefabSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/MokouCharacterValidation.unity`
- Unity-generated `.meta` files for new M27 enemy scripts/tests.

Verification:

- Initial red Enemy Prefab test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyPrefabSmokeTests.RunAll`
  - Result: failed as expected with `Missing required type: TouhouMigration.Runtime.Combat.MigrationSimpleEnemyController`.
- Scene-wiring red Enemy Prefab test:
  - same command.
  - Result: failed as expected with `Human Village should contain at least one reusable enemy controller. Expected: True. Actual: False.`
- Builder:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: builder completed successfully.
- Green Enemy Prefab test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.EnemyPrefabSmokeTests.RunAll`
  - Result: passed.
- Full M27 Unity smoke regression:
  - `EnemyPrefabSmokeTests`, `CombatLootQuestSmokeTests`, `CombatActionRewardSmokeTests`, `CombatBridgeSmokeTests`, `PlayerHealthBuffSmokeTests`, `PlayerBuffSmokeTests`, `CookingBuffSmokeTests`, `SaveInventorySmokeTests`, `GlobalUiSmokeTests`, `FoundationSmokeTests`, `ContentSmokeTests`, `QuestJournalSmokeTests`, `CookingQuestSmokeTests`, `BondQuestSmokeTests`, `SocialGiftSmokeTests`, `SocialLoopSmokeTests`, `DialogueSmokeTests`, `CookingServiceSmokeTests`, and `CardBuildSmokeTests`: passed.
  - Result marker: `M27_FULL_SMOKE_REGRESSION_PASSED`.

Known blockers:

- This is a simple reusable enemy state driver, not full Godot `Enemy3D` parity.
- Formal Enemy3D behavior still needs a dedicated source audit and future slices for navigation, attack telegraphs, animation states, hurt reactions, knockback, death VFX, drops/notifications, variants, ranged/projectile attacks, and boss logic.
- The controller uses `GameObject.FindGameObjectWithTag("Player")` in first-slice runtime mode; a later scene/combat-session owner should inject player targets explicitly.
- Damage still comes from a marker sphere rather than authored hitboxes, animation events, or attack timelines.

Next recommended step:

- Audit formal Godot `Enemy3D` and combat scene variants in detail, then extend the simple enemy slice with animation-event attack timing and reusable enemy variant configuration before starting combat-session/rank settlement.

### M26: Combat Loot Tables And Quest Kill Objectives

- Date: 2026-06-24 09:20 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Maxwell
- Goal: Move target defeat closer to Godot `SignalBus.enemy_killed`: direct inventory loot, active quest kill-objective progress, and first Godot loot-table family coverage.

Completed:

- Added `CombatLootQuestSmokeTests` using TDD:
  - Red run failed as expected with `Missing method TouhouMigration.Runtime.Social.QuestDeliveryService.NotifyEnemyKilled`.
  - Second red run failed as expected with `Missing required type: TouhouMigration.Runtime.Combat.MigrationCombatLootDropHandler`.
  - Scene red run failed as expected because Human Village did not mount a loot adapter.
  - Loot-table red run failed as expected with `Missing method TouhouMigration.Runtime.Combat.MigrationCombatLootDropHandler.ConfigureGodotLootTables`.
  - Green runs passed after implementing kill-objective scanning, loot adapter, builder wiring, scene regeneration, and Godot loot-table forced verification.
- Added `QuestDeliveryService.NotifyEnemyKilled()`:
  - scans all active quests.
  - advances every objective whose `Type == "kill"`, matching Godot's `QuestManager._on_enemy_killed`.
  - keeps generic counters separate from kill objectives, because Godot `quest_progress_counters` is a distinct save/counter surface.
- Added `MigrationCombatLootDropHandler`:
  - subscribes to `MigrationCombatTargetBehaviour.Defeated`.
  - grants loot once per defeated target through `InventoryService.AddItem`.
  - calls `QuestDeliveryService.NotifyEnemyKilled()` once per defeated target.
  - supports explicit service binding for tests and scene-time fallback through `MigrationGlobalUiController.FindInventoryService()` / `FindQuestDeliveryService()`.
  - supports a deterministic `ConfigureGuaranteedDrop` path for simple vertical-slice targets.
  - supports `ConfigureGodotLootTables(enemyType, elementalGroup, forceAllTables)` for Godot table-family verification.
- Implemented first Godot loot-table family coverage in Unity:
  - enemy classification: `fairy`, `beast`, `elite`/`elite_enemy`, and `boss`.
  - meat drops: `fairy_meat`, `beast_meat`, `youkai_beast_meat`.
  - elemental crystals: fire, ice, earth, and wind group routes.
  - seed drops: common seed, elite rare seed, and boss seed routes.
  - fertilizer drops: common dungeon compost plus elite/boss deep fertilizer route.
  - forced-table smoke mode proves table coverage without relying on random probability.
- Expanded `MigrationGlobalUiController` with `Inventory` and `FindInventoryService()` so scene combat adapters can resolve the shared runtime inventory service.
- Updated `TouhouMigrationProjectBuilder`:
  - Human Village target dummy now mounts `MigrationCombatLootDropHandler`.
  - the dummy uses Godot loot table forced mode for the `fairy` classification so the scene demonstrates the inventory/quest kill loop.
  - regenerated Unity scenes through the builder.
- Integrated Maxwell findings:
  - Godot `LootDropManager` is an autoload listening to `SignalBus.enemy_killed`.
  - Godot loot is direct-to-inventory, not a ground pickup.
  - Godot kill objective shape is just `type: "kill"` with `required`; there is no enemy id/type filter in the formal data.
  - Godot combat end/rank settlement belongs to a later combat-session/rank slice, not this per-kill loot/objective slice.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatLootDropHandler.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Social/QuestDeliveryService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/CombatLootQuestSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/MokouCharacterValidation.unity`
- Unity-generated `.meta` files for new M26 combat scripts/tests.

Verification:

- Initial red Combat Loot Quest test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CombatLootQuestSmokeTests.RunAll`
  - Result: failed as expected with `Missing method TouhouMigration.Runtime.Social.QuestDeliveryService.NotifyEnemyKilled`.
- Loot handler red test:
  - same command.
  - Result: failed as expected with `Missing required type: TouhouMigration.Runtime.Combat.MigrationCombatLootDropHandler`.
- Scene-wiring red test:
  - same command.
  - Result: failed as expected with `Human Village combat target should mount a loot drop handler. Expected: True. Actual: False.`
- Loot table red test:
  - same command.
  - Result: failed as expected with `Missing method TouhouMigration.Runtime.Combat.MigrationCombatLootDropHandler.ConfigureGodotLootTables`.
- Builder:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: builder completed successfully.
- Green Combat Loot Quest test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CombatLootQuestSmokeTests.RunAll`
  - Result: passed.
- Full M26 Unity smoke regression:
  - `CombatLootQuestSmokeTests`, `CombatActionRewardSmokeTests`, `CombatBridgeSmokeTests`, `PlayerHealthBuffSmokeTests`, `PlayerBuffSmokeTests`, `CookingBuffSmokeTests`, `SaveInventorySmokeTests`, `GlobalUiSmokeTests`, `FoundationSmokeTests`, `ContentSmokeTests`, `QuestJournalSmokeTests`, `CookingQuestSmokeTests`, `BondQuestSmokeTests`, `SocialGiftSmokeTests`, `SocialLoopSmokeTests`, `DialogueSmokeTests`, `CookingServiceSmokeTests`, and `CardBuildSmokeTests`: passed.
  - Result marker: `M26_FULL_SMOKE_REGRESSION_PASSED`.

Known blockers:

- Loot table probabilities are represented in the handler, but smoke coverage uses forced-table mode. Statistical/probability validation and seeded runtime RNG policy remain future work.
- Unity still lacks real enemy prefabs with AI, nav, animation, hurt reactions, death animations, damage numbers, VFX, and combat HUD feedback.
- Godot XP gem spawn/pickup remains deferred; M25 direct XP reward is still a vertical-slice bridge, not full `ExperienceGem3D` parity.
- Godot combat-session end settlement is not migrated: rank, floor level, kills-based coins, spirit crystal reward, and `grant_rank_bonus_drops()` remain a later combat-session/rank slice.
- Per-defeat `coinReward` in `MigrationCombatDefeatRewardHandler` is still a temporary vertical-slice reward and should be reconciled before adding run-end settlement to avoid double coin grants.
- Notifications/audio for loot and quest progress are not ported yet.

Next recommended step:

- Build a first real enemy prefab/state slice: wrap `MigrationCombatTargetBehaviour`, `MigrationEnemyDamageSource`, `MigrationCombatLootDropHandler`, AI movement, and death/feedback presentation into reusable enemy variants, then start a combat-session/rank service for Godot's end-of-combat settlement.

### M25: Player Attack Action Window And Defeat Rewards

- Date: 2026-06-24 08:57 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Planck
- Goal: Connect the first player light/heavy attack action controller to live hitbox windows, then route target defeat into XP, coins, kill count, and quest counter services.

Completed:

- Added `CombatActionRewardSmokeTests` using TDD:
  - Red run failed as expected with `Missing required type: TouhouMigration.Runtime.Combat.MigrationPlayerCombatActionController`.
  - Second red run failed as expected with `Missing required type: TouhouMigration.Runtime.Combat.MigrationCombatDefeatRewardHandler`.
  - Scene red run failed as expected because Human Village had not yet been regenerated with the new action/reward adapters.
  - Green run passed after adding the action controller, reward handler, progress kill count, global service lookups, builder wiring, and scene regeneration.
- Added `MigrationPlayerCombatActionController`:
  - exposes `TriggerLightAttack`, `TriggerHeavyAttack`, `CompleteAttackWindow`, `BindAttackHitbox`, and `ConfigureDamage`.
  - maps left/right mouse input to light/heavy attack windows when gameplay input is not blocked.
  - configures `MigrationPlayerAttackHitbox` per attack type and keeps a testable action-window count.
- Added `MigrationCombatDefeatRewardHandler`:
  - subscribes to `MigrationCombatTargetBehaviour.Defeated`.
  - grants XP, coins, total kill count, and a quest counter exactly once per target.
  - supports explicit service binding for tests and scene-time fallback through `MigrationGlobalUiController`.
- Expanded `MigrationPlayerProgressService` with `TotalKills`, `RegisterKill`, and save/load mapping to `MigrationSaveData.TotalKills`.
- Expanded `MigrationGlobalUiController` with `PlayerProgress`, `Quests`, `FindPlayerProgressService`, and `FindQuestDeliveryService` so scene adapters can resolve shared runtime services without owning them.
- Updated `TouhouMigrationProjectBuilder`:
  - generated players now mount `MigrationPlayerCombatActionController` and bind the generated attack hitbox.
  - Human Village target dummy now mounts `MigrationCombatDefeatRewardHandler` with first-slice rewards.
  - regenerated Unity scenes through the builder.
- Integrated Planck findings:
  - formal Godot combat light/heavy attacks flow from `PlayerIdle`/`PlayerMove` into `PlayerAttack.enter()` and `Player3D._trigger_attack()`, which opens an `AttackEffectHitbox`.
  - formal Godot enemy death emits `SignalBus.enemy_killed`; current runtime listeners include XP gem spawn, loot drop, quest kill objective updates, and combat kill/coin settlement.
  - M25 should prove direct reward/counter service routing first, while deferring XP gem pickup, loot tables, rank settlement, and full kill-objective scanning.
- Asset discipline update:
  - verified `/Users/Shared/Touhougodot/assets/unity_imports` contains only `README_MIGRATED_TO_UNITY.md`.
  - confirmed the relocated Unity-origin source packs live under `/Users/Shared/TouhouUnityMigration/ExternalUnityAssets/unity_imports`.
  - future confirmed Unity-origin assets should follow the same pattern: physically move to the Unity migration project and leave only a migrated marker document in the old Godot path.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPlayerCombatActionController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatDefeatRewardHandler.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Player/MigrationPlayerProgressService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/CombatActionRewardSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/MokouCharacterValidation.unity`
- `/Users/Shared/Touhougodot/assets/unity_imports/README_MIGRATED_TO_UNITY.md`
- Unity-generated `.meta` files for new M25 combat scripts/tests.

Verification:

- Initial red Combat Action Reward test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CombatActionRewardSmokeTests.RunAll`
  - Result: failed as expected with `Missing required type: TouhouMigration.Runtime.Combat.MigrationPlayerCombatActionController`.
- Reward-handler red Combat Action Reward test:
  - same command.
  - Result: failed as expected with `Missing required type: TouhouMigration.Runtime.Combat.MigrationCombatDefeatRewardHandler`.
- Scene-wiring red Combat Action Reward test:
  - same command.
  - Result: failed as expected with `Human Village player should mount a combat action controller. Expected: True. Actual: False.`
- Builder:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: builder completed successfully.
- Green Combat Action Reward test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CombatActionRewardSmokeTests.RunAll`
  - Result: passed.
- Full M25 Unity smoke regression:
  - `CombatActionRewardSmokeTests`, `CombatBridgeSmokeTests`, `PlayerHealthBuffSmokeTests`, `PlayerBuffSmokeTests`, `CookingBuffSmokeTests`, `SaveInventorySmokeTests`, `GlobalUiSmokeTests`, `FoundationSmokeTests`, `ContentSmokeTests`, `QuestJournalSmokeTests`, `CookingQuestSmokeTests`, `BondQuestSmokeTests`, `SocialGiftSmokeTests`, `SocialLoopSmokeTests`, `DialogueSmokeTests`, `CookingServiceSmokeTests`, and `CardBuildSmokeTests`: passed.
  - Result marker: `M25_FULL_SMOKE_REGRESSION_PASSED`.

Known blockers:

- Attack windows are now input-callable, but they are not yet driven by real animation events, combo state, stamina/cooldown, lock-on, hit pause, knockback, or VFX timelines.
- Defeat rewards currently grant direct XP/coins/counter values; Godot-style XP gems, pickup routing, loot tables, rank settlement, and reward notification UI are still future slices.
- Quest kill handling is currently a generic counter bridge; full Godot `enemy_killed -> active kill objective scan` parity should become a dedicated `NotifyEnemyKilled`/objective adapter.
- Experience is runtime-only in `MigrationPlayerProgressService`; coins and kills save/load, but XP persistence still needs global save orchestration.
- Human Village still has a target dummy and damage marker, not a full AI enemy prefab or formal combat arena.

Next recommended step:

- Build a first enemy/loot reward slice around the existing target dummy: add a simple enemy prefab/state driver, route defeat through loot item grants and quest kill-objective scanning, then add visible reward feedback before moving on to combat VFX and animation event timing.

### M24: Live Combat Adapters And Human Village Target Dummy

- Date: 2026-06-24 07:57 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Bacon
- Goal: Move the M23 combat runtime from pure service calls into scene-mounted Unity adapters that can be driven by triggers, target dummies, and enemy damage markers.

Completed:

- Extended `CombatBridgeSmokeTests` using TDD:
  - Red run failed as expected with `Missing required type: TouhouMigration.Runtime.Combat.MigrationPlayerAttackHitbox`.
  - Scene red run failed as expected because Human Village did not mount live combat adapters.
  - Defeat-handler red run failed as expected with `Missing required type: TouhouMigration.Runtime.Combat.MigrationCombatDefeatHandler`.
  - Binding red run failed as expected with `Missing method TouhouMigration.Runtime.Combat.MigrationCombatDefeatHandler.BindTarget`.
  - Green run passed after implementing live adapters, handler, builder wiring, and scene regeneration.
- Added `MigrationPlayerAttackHitbox`:
  - exposes `Configure(baseDamage, attackType)`, `BeginAttackWindow()`, `EndAttackWindow()`, and `TryHit(MigrationCombatTargetBehaviour)`.
  - routes target hits through `MigrationCombatRuntime.ApplyPlayerAttackToBehaviour`.
  - deduplicates targets within one attack window, matching Godot hitbox-style one-hit-per-window behavior.
  - supports trigger entry for future animation/physics-driven attacks.
- Added `MigrationEnemyDamageSource`:
  - exposes `Configure(damage)` and `TryDamagePlayer()`.
  - routes contact/projectile-style enemy damage through `MigrationCombatRuntime.ApplyDamageToPlayer`.
  - supports trigger and collision entry against objects tagged `Player`.
- Added `MigrationCombatDefeatHandler`:
  - subscribes to `MigrationCombatTargetBehaviour.Defeated`.
  - disables target colliders and renderers once on defeat.
  - exposes `BindTarget` so generated targets and editor smoke tests can bind explicitly.
- Expanded `MigrationCombatRuntime` with `ApplyPlayerAttackToBehaviour` so scene targets can use the same damage, defeat, and kill-heal route as pure runtime targets.
- Updated `TouhouMigrationProjectBuilder`:
  - attaches `PlayerAttackHitbox` under each generated migration player.
  - adds a `CombatTargetDummy` with `MigrationCombatTargetBehaviour` and `MigrationCombatDefeatHandler` to Human Village.
  - adds an `EnemyDamageSourceMarker` with `MigrationEnemyDamageSource` to Human Village.
  - regenerated Unity scenes through the builder.
- Integrated Bacon findings:
  - formal Godot combat entry is `CombatArenaHD2D.tscn` through `SceneManager`.
  - the old `AttackEffectHitbox -> HitboxComponent -> HurtboxComponent` chain exists, but the formal `Enemy3D` path has largely moved to direct collision/damage calls.
  - M24 should prove `Unity trigger hitbox -> target HP -> one-shot defeated -> kill-heal` and `enemy damage source -> player health runtime`, while deferring full AI/VFX/loot/projectile/boss work.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatRuntime.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationPlayerAttackHitbox.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationEnemyDamageSource.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatDefeatHandler.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/CombatBridgeSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/MokouCharacterValidation.unity`
- Unity-generated `.meta` files for the new combat scripts.

Verification:

- Red live adapter Combat Bridge test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CombatBridgeSmokeTests.RunAll`
  - Result: failed as expected with `Missing required type: TouhouMigration.Runtime.Combat.MigrationPlayerAttackHitbox`.
- Red scene wiring test:
  - same command.
  - Result: failed as expected with `Human Village should mount a player attack hitbox adapter. Expected: True. Actual: False.`
- Red defeat handler test:
  - same command.
  - Result: failed as expected with `Missing required type: TouhouMigration.Runtime.Combat.MigrationCombatDefeatHandler`.
- Red explicit handler binding test:
  - same command.
  - Result: failed as expected with `Missing method TouhouMigration.Runtime.Combat.MigrationCombatDefeatHandler.BindTarget`.
- Builder:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: `Touhou Unity migration initial project built.`
- Green Combat Bridge test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CombatBridgeSmokeTests.RunAll`
  - Result: `Combat bridge smoke tests passed.`
- Core M24 regression:
  - `CombatBridgeSmokeTests`: passed.
  - `PlayerHealthBuffSmokeTests`: passed.
  - `PlayerBuffSmokeTests`: passed.
  - `CookingBuffSmokeTests`: passed.
  - `SaveInventorySmokeTests`: passed.
  - `GlobalUiSmokeTests`: passed.
  - `FoundationSmokeTests`: passed.
  - `ContentSmokeTests`: passed.
  - Result marker: `M24_CORE_REGRESSION_PASSED`.

Known blockers:

- Player attack windows are callable and trigger-capable, but no real input/action-state/animation-event system opens and closes them during gameplay yet.
- Human Village has a target dummy and damage marker, not a full enemy prefab with AI, navigation, animations, VFX, loot, XP, quest kill counters, or waves.
- Target defeat disables colliders/renderers, but death animation, damage numbers, hit pause, knockback, i-frames, projectiles, shockwaves, tracking bullets, afterimages, boss HUD, and rewards remain future combat slices.
- Heavy armor penetration remains an outgoing query with no target armor model.
- Health/combat runtime state still needs global save/load orchestration once a real playable combat scene exists.

Next recommended step:

- Build the first player action-window bridge: map light/heavy input or animation events to `MigrationPlayerAttackHitbox.Configure/BeginAttackWindow/EndAttackWindow`, add a simple enemy prefab around the target/damage-source adapters, and start routing defeat into XP/loot/quest kill services.

### M23: Combat Runtime Bridge And Target Behaviour Adapter

- Date: 2026-06-24 07:41 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Halley
- Goal: Add the first Unity combat bridge so outgoing player attacks, incoming player damage, and target defeat/kill-heal behavior use the M21-M22 cooking buff and health seams instead of remaining isolated query helpers.

Completed:

- Added `CombatBridgeSmokeTests` using TDD:
  - Initial red run failed as expected with `Missing required type: TouhouMigration.Runtime.Combat.MigrationCombatRuntime`.
  - Adapter red run failed as expected with `Missing required type: TouhouMigration.Runtime.Combat.MigrationCombatTargetBehaviour`.
  - Green runs passed after implementing the runtime bridge and target MonoBehaviour adapter.
- Added `MigrationCombatRuntime`:
  - routes outgoing player attacks through `MigrationPlayerController.GetModifiedAttackDamage`.
  - routes incoming player damage through `MigrationPlayerHealthRuntime.ApplyDamage`.
  - calls `MigrationPlayerHealthRuntime.NotifyEnemyKilled` when a target is newly defeated.
  - tolerates missing player/health targets for migration-era smoke and scene bring-up.
- Added `MigrationCombatTargetRuntime`:
  - owns target max/current HP.
  - clamps damage and HP.
  - reports newly defeated state only once, preventing duplicate kill-heal grants.
- Added `CombatBridgeResult` DTO for attack/damage outcomes.
- Added `MigrationCombatTargetBehaviour`:
  - thin Unity component wrapper over `MigrationCombatTargetRuntime`.
  - exposes `ApplyDamage(float)`, current HP, defeat state, and a `Defeated` event.
  - emits defeat exactly once for scene objects and tracks `DefeatEventCount` for smoke/instrumentation.
- Updated `MigrationGlobalUiController`:
  - constructs a shared `MigrationCombatRuntime`.
  - binds the active player controller to cooking buffs when found.
  - exposes `Combat` and `FindCombatRuntime()` for migration-era scene adapters.
- Integrated Halley findings:
  - keep combat bridge thin and service-oriented.
  - route target HP/death through a MonoBehaviour adapter, not by embedding combat math in future enemy scripts.
  - defer player hitbox, enemy damage-source, loot/reward, and animation/action-window adapters until a real combat slice has objects to bind.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/CombatBridgeResult.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatRuntime.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatTargetRuntime.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Combat/MigrationCombatTargetBehaviour.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/CombatBridgeSmokeTests.cs`
- Unity-generated `.meta` files for the new combat scripts/tests.

Verification:

- Initial red Combat Bridge test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CombatBridgeSmokeTests.RunAll`
  - Result: failed as expected with `Missing required type: TouhouMigration.Runtime.Combat.MigrationCombatRuntime`.
- Adapter red Combat Bridge test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CombatBridgeSmokeTests.RunAll`
  - Result: failed as expected with `Missing required type: TouhouMigration.Runtime.Combat.MigrationCombatTargetBehaviour`.
- Green Combat Bridge test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CombatBridgeSmokeTests.RunAll`
  - Result: `Combat bridge smoke tests passed.`
- Core M23 regression:
  - `CombatBridgeSmokeTests`: passed.
  - `PlayerHealthBuffSmokeTests`: passed.
  - `PlayerBuffSmokeTests`: passed.
  - `CookingBuffSmokeTests`: passed.
  - `SaveInventorySmokeTests`: passed.
  - `GlobalUiSmokeTests`: passed.
  - `FoundationSmokeTests`: passed.
  - `ContentSmokeTests`: passed.
  - Result marker: `M23_CORE_REGRESSION_PASSED`.

Known blockers:

- No live player hitbox, enemy damage-source, hurtbox/collider, action-window, or animation-event component is wired into scenes yet.
- Target defeat now emits a Unity event, but loot drops, quest kill counters, rewards, XP, VFX, and death animation state are not connected.
- Heavy armor penetration is still exposed as a cooking/action query but has no target armor model to consume it.
- Combat HUD, boss HUD, knockback, hit pause, i-frames, projectile patterns, shockwaves, tracking bullets, afterimages, and special visual effects remain future combat slices.
- Health/combat runtime state still needs global save/load orchestration once a real playable combat scene exists.

Next recommended step:

- Build a minimal live combat vertical slice: add player hitbox/action-window and enemy damage-source adapters around `MigrationCombatRuntime`, mount one target dummy or simple enemy in a scene, then connect defeat to loot/reward/quest signals before visual combat polish.

### M22: Player Health Runtime And Cooking Buff Inbound Effects

- Date: 2026-06-24 07:26 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Sagan
- Goal: Move cooking buffs beyond movement/outgoing query helpers by adding a Unity player health seam that consumes cooking buff damage reduction, regen, kill-heal, rebirth, hitstun, and attack-feedback suppression rules.

Completed:

- Added `PlayerHealthBuffSmokeTests` using TDD:
  - Red run failed as expected with `Missing required type: TouhouMigration.Runtime.Player.MigrationPlayerHealthRuntime`.
  - Green run passed after implementing the health runtime and wiring it into global service ownership.
- Added `MigrationPlayerHealthRuntime`:
  - owns current/max HP, death state, and one-use rebirth state.
  - binds to `CookingBuffService` and keeps HP ratio synchronized for HP-threshold effects.
  - applies incoming damage through `CookingBuffService.GetDamageReduction()`.
  - applies regen through `CookingBuffService.GetRegenerationPerSecond()`.
  - applies kill-heal through `CookingBuffService.GetEnemyKillHealPercent()`.
  - implements `rebirth_once` as a one-time lethal prevention restoring 50% max HP.
  - exposes hitstun duration with Godot `def>=6` and `hitstun_resist_20` rules.
  - exposes attack-hit-feedback suppression for `def>=10` or `atk+def` combo armor.
- Added `PlayerHealthResult` DTO for damage/heal/rebirth outcomes.
- Updated `ItemUseService`:
  - can receive `MigrationPlayerHealthRuntime`.
  - routes item healing through the shared health runtime when present.
  - keeps the older internal test health path as a fallback for older constructors.
- Updated `MigrationGlobalUiController`:
  - constructs `MigrationPlayerHealthRuntime`.
  - binds it to `CookingBuffService`.
  - passes it into `ItemUseService`.
  - ticks health regen each frame from the current migration runtime owner.
  - exposes `PlayerHealth` and `FindPlayerHealthRuntime()` for migration-era lookup.
- Added test coverage for:
  - full-HP `high_hp_guard` damage reduction (`100 -> 61`) and mid-HP reduction (`100 -> 76`).
  - `regen_1` healing one HP per second.
  - `kill_heal_8_percent` healing 8% max HP on kill.
  - `rebirth_once` restoring 50% HP only on the first lethal hit.
  - `def>=6` hitstun halving.
  - `hitstun_resist_20` hitstun reduction.
  - `def>=10` attack-hit-feedback suppression.
- Integrated Sagan findings:
  - inbound health effects belong in a separate `MigrationPlayerHealthRuntime`, not in the movement controller.
  - outgoing/action modifiers should remain in `MigrationPlayerController`.
  - regen and kill-heal tests should be split because same-main-stat dishes replace each other by Godot slot rules.
  - heavy armor penetration belongs to a future outgoing/enemy armor seam.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Player/MigrationPlayerHealthRuntime.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Player/PlayerHealthResult.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Inventory/ItemUseService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/PlayerHealthBuffSmokeTests.cs`
- Unity-generated `.meta` files for the new scripts/tests.

Verification:

- Red Player Health Buff test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.PlayerHealthBuffSmokeTests.RunAll`
  - Result: failed as expected with `Missing required type: TouhouMigration.Runtime.Player.MigrationPlayerHealthRuntime`.
- Green Player Health Buff test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.PlayerHealthBuffSmokeTests.RunAll`
  - Result: `Player health buff smoke tests passed.`
- Core M22 regression:
  - `PlayerHealthBuffSmokeTests`: passed.
  - `PlayerBuffSmokeTests`: passed.
  - `CookingBuffSmokeTests`: passed.
  - `SaveInventorySmokeTests`: passed.
  - `GlobalUiSmokeTests`: passed.
  - `FoundationSmokeTests`: passed.
  - `ContentSmokeTests`: passed.
  - Result marker: `M22_CORE_REGRESSION_PASSED`.
- Final M22 regression after Sagan-requested test split/super-armor coverage:
  - `PlayerHealthBuffSmokeTests`: passed.
  - `PlayerBuffSmokeTests`: passed.
  - `CookingBuffSmokeTests`: passed.
  - `SaveInventorySmokeTests`: passed.
  - `GlobalUiSmokeTests`: passed.
  - `FoundationSmokeTests`: passed.
  - `ContentSmokeTests`: passed.
  - Result marker: `M22_FINAL_CORE_REGRESSION_PASSED`.

Known blockers:

- No real Unity enemy damage source or enemy-kill event currently calls the health runtime; M22 provides the seam and tests the behavior directly.
- `ShouldSuppressHitFeedbackWhileAttacking()` is still a query. A future hit-reaction/action layer must gate it by actual attack state and animation feedback.
- Real dash state, hitboxes, incoming damage sources, enemy kill routing, heavy armor penetration, shockwaves, tracking bullets, afterimages, and visual special effects remain future combat slices.
- Health state is runtime-owned but not yet persisted into the global new-game/load-game orchestration beyond the existing save DTO HP fields.

Next recommended step:

- Build the first Unity combat/action bridge around the existing seams: route incoming damage into `MigrationPlayerHealthRuntime.ApplyDamage`, route enemy kills into `NotifyEnemyKilled`, route outgoing attacks through `MigrationPlayerController.GetModifiedAttackDamage`, and add minimal enemy/target test doubles before visual combat polish.

### M21: Player Cooking Buff Binding And Runtime Expiry Tick

- Date: 2026-06-24 07:13 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Boyle
- Goal: Connect the M20 `CookingBuffService` foundation into the Unity player runtime so active cooking buffs affect player movement/combat query surfaces and expire during live play.

Completed:

- Added `PlayerBuffSmokeTests` using TDD:
  - Red run failed as expected with `Missing method TouhouMigration.Runtime.Player.MigrationPlayerController.BindCookingBuffs`.
  - Green run passed after implementing player buff binding and query methods.
- Expanded `MigrationPlayerController`:
  - self-binds to the scene `MigrationGlobalUiController` cooking buff service in `Start`.
  - exposes `BindCookingBuffs(CookingBuffService)`.
  - uses `GetModifiedWalkSpeed()` / `GetModifiedRunSpeed()` inside live movement.
  - exposes `GetModifiedDashCooldown()`.
  - exposes `GetModifiedDashDistanceMultiplier()` for `dash_distance_15` and `dash_bonus_100` effects.
  - exposes `GetModifiedJumpHeight()` for `jump_boost_20`.
  - exposes `GetModifiedAttackDamage(baseDamage, attackType)` with Godot-compatible atk/spi threshold modifiers for heavy and skill attacks.
  - exposes `GetDamageMultiplier()`, `GetDamageReduction()`, `GetModifiedSpiritChargeMultiplier()`, and `HasCookingSpecialEffect(effectId)`.
- Expanded `MigrationGlobalUiController`:
  - exposes `CookingBuffs`.
  - adds `FindCookingBuffService()` for migration-era scene lookup.
  - ticks `cookingBuffService.Tick(Time.deltaTime)` from the runtime owner so buffs expire during play.
- Added test coverage for:
  - quality 2 `bamboo_cold_noodles` boosting speed and reducing dash cooldown.
  - buff expiry returning walk speed and dash cooldown to base values.
  - `grilled_bamboo_shoot` applying dash distance multiplier.
  - quality 2 `spicy_beast_skewer` applying atk damage multiplier and heavy-attack threshold bonus.
- Integrated Boyle findings:
  - `MigrationGlobalUiController` is the correct current service owner.
  - player combat/dash hooks should stay query-based until full action/combat systems exist.
  - live buff expiry required a single runtime tick owner.
  - health-ratio effects, regen, kill-heal, rebirth, and hitstun should land in a later small player-health/action seam instead of being forced into `MigrationPlayerController`.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Player/MigrationPlayerController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/PlayerBuffSmokeTests.cs`
- Unity-generated `.meta` file for the new test.

Verification:

- Red Player Buff test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.PlayerBuffSmokeTests.RunAll`
  - Result: failed as expected with `Missing method TouhouMigration.Runtime.Player.MigrationPlayerController.BindCookingBuffs`.
- Green Player Buff test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.PlayerBuffSmokeTests.RunAll`
  - Result: `Player buff smoke tests passed.`
- Full M21 regression suite before tick follow-up:
  - `PlayerBuffSmokeTests`: passed.
  - `CookingBuffSmokeTests`: passed.
  - `CookingServiceSmokeTests`: passed.
  - `CookingQuestSmokeTests`: passed.
  - `SaveInventorySmokeTests`: passed.
  - `QuestJournalSmokeTests`: passed.
  - `BondQuestSmokeTests`: passed.
  - `SocialLoopSmokeTests`: passed.
  - `SocialGiftSmokeTests`: passed.
  - `DialogueSmokeTests`: passed.
  - `CardBuildSmokeTests`: passed.
  - `FoundationSmokeTests`: passed.
  - `GlobalUiSmokeTests`: passed.
  - `ContentSmokeTests`: passed.
  - Result marker: `ALL_M21_SMOKE_TESTS_PASSED`.
- Tick follow-up regression:
  - `PlayerBuffSmokeTests`: passed.
  - `CookingBuffSmokeTests`: passed.
  - `GlobalUiSmokeTests`: passed.
  - `FoundationSmokeTests`: passed.
  - `ContentSmokeTests`: passed.
  - Result marker: `M21_TICK_REGRESSION_PASSED`.

Known blockers:

- Unity still lacks a real dash action state, combat hitbox runtime, player health runtime, and death/rebirth pipeline. M21 exposes the query surface those systems should call.
- HP-ratio-sensitive effects need a single health source of truth before they can be reliable.
- Regen, kill-heal, rebirth, hitstun resistance, phantom crit, tracking bullets, shockwaves, and afterimage visual spawning remain future action/health/combat slices.
- Global runtime save/load orchestration still does not automatically snapshot/load `CookingService` and `CookingBuffService`.
- `FindAnyObjectByType` is acceptable for the migration shell, but production bootstrap should bind services explicitly if multiple players/global UIs appear.

Next recommended step:

- Create a small Unity player action/health seam that consumes the existing cooking buff queries: dash action timings, outgoing damage, incoming damage reduction, HP regen, kill-heal, rebirth, and hitstun/armor behavior. Keep it modular; do not port the whole Godot `Player3D.gd` blob directly into one Unity class.

### M20: Item Use, Cooking Buff Runtime, And Buff Save Surface

- Date: 2026-06-24 07:02 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from James and Boole
- Goal: Port the Godot `InventoryManager.use_item` dish/drink route and `CookingBuffSystem` foundation so cooked dishes and drinks can be consumed from quality-aware inventory stacks, produce active combat buff state, expose player-facing menu feedback, and persist buff snapshots in Unity slot saves.

Completed:

- Added `CookingBuffSmokeTests` using TDD:
  - Red run failed as expected with `Missing method TouhouMigration.Runtime.Inventory.ItemDefinition.GetEffectInt`.
  - Green run passed after implementing item effects, cooking buffs, item-use, and buff save/load.
- Expanded `ItemDatabase` / `ItemDefinition`:
  - preserves Godot `effects` dictionaries from consumables.
  - exposes top-level `heal_hp` on dishes/drinks as a usable effect.
  - preserves equipment/stat maps for future item stat work.
  - adds typed helpers `HasEffect`, `GetEffectString`, `GetEffectInt`, and `GetStat`.
- Expanded `CookingDatabase`:
  - exposes full `CookingDishProfile` lookup.
  - exposes dish duration, special effects, drink effects, drink classification, all profiles, and Godot quality multipliers.
- Added Unity cooking buff runtime:
  - `CookingBuffService`
  - `CookingBuffRuntimeSnapshot`
  - `CookingBuffSlotSnapshot`
  - `CookingBuffDrinkSnapshot`
  - `CookingBuffStatValue`
- Ported Godot-compatible `CookingBuffSystem` service behavior:
  - 3 active dish slots.
  - one active drink slot.
  - slot replacement by same main stat, first empty slot, then earliest-expiring slot.
  - quality-scaled dish stats using Godot multipliers and floor rounding.
  - stat totals across dishes and drink with Godot soft cap 12 and hard cap 16.
  - threshold unlock memory at 6/10/15.
  - combo queries for `atk+spd`, `atk+spi`, `def+spi`, `spd+spi`, and `atk+def`.
  - public calculators for damage multiplier, damage reduction, speed multiplier, dash cooldown offset, spirit charge multiplier, heavy armor penetration, kill-heal percent, and regeneration per second.
  - special effect and drink effect queries.
  - buff/drink ticking and expiry.
  - save/load snapshot that preserves dish quality, fixing the Godot-side load omission noted during investigation.
- Added Unity item-use runtime:
  - `ItemUseService`
  - `ItemUseResult`
  - dish/drink use consumes the matching quality stack and applies `CookingBuffService`.
  - consumable use reads `heal_hp`, `buff`, `combat_item`, and `restore_mp` effects.
  - health clamp state is present for migration-era tests and future player-state integration.
- Expanded `InventoryService`:
  - adds `GetOccupiedSlots()` so UI can show quality-specific stacks instead of the aggregated quality-blind count.
- Added cooking buff save surface:
  - `MigrationSaveData.CookingBuffs`.
  - save/load normalization for old saves missing `cooking_buffs`.
  - save smoke coverage proving active dish/drink buff snapshots round-trip.
- Added player-facing Unity entry points:
  - `MigrationGlobalUiController` constructs `CookingBuffService` and `ItemUseService`.
  - `MigrationUnifiedMenuController` receives the services, shows current cooking buff totals and combat multipliers in the `料理` tab, and adds `使用` buttons in the inventory tab for consumables, dishes, and drinks.
- Integrated James findings:
  - Godot `CookingBuffSystem` is an autoload with 3 dish slots, active drink, caps, thresholds, combos, save/load, and player combat hooks.
  - Godot `InventoryManager.use_item()` routes `dish` and `drink` item types through `CookingBuffSystem.consume_dish/consume_drink`.
  - Godot saves dish quality but does not restore it during buff load; Unity preserves it.
- Integrated Boole findings:
  - `CookingService` should remain crafting/progression only.
  - item-use and cooking buffs belong in separate services fed by `InventoryService`, `ItemDatabase`, and `CookingDatabase`.
  - `MigrationGlobalUiController` is the correct current runtime composition point.
  - `JsonUtility` requires serializable list DTOs instead of dictionaries for persisted buff state.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Cooking/CookingDatabase.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Cooking/CookingBuffService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Cooking/CookingBuffRuntimeSnapshot.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Inventory/InventoryService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Inventory/ItemDatabase.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Inventory/ItemDefinition.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Inventory/ItemUseResult.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Inventory/ItemUseService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Save/MigrationSaveData.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Save/MigrationSaveService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationUnifiedMenuController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/CookingBuffSmokeTests.cs`
- Unity-generated `.meta` files for the new scripts.

Verification:

- Red Cooking Buff test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CookingBuffSmokeTests.RunAll`
  - Result: failed as expected with `Missing method TouhouMigration.Runtime.Inventory.ItemDefinition.GetEffectInt`.
- Green Cooking Buff test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CookingBuffSmokeTests.RunAll`
  - Result: `Cooking buff smoke tests passed.`
- Full M20 regression suite:
  - `CookingBuffSmokeTests`: passed.
  - `CookingServiceSmokeTests`: passed.
  - `CookingQuestSmokeTests`: passed.
  - `SaveInventorySmokeTests`: passed.
  - `QuestJournalSmokeTests`: passed.
  - `BondQuestSmokeTests`: passed.
  - `SocialLoopSmokeTests`: passed.
  - `SocialGiftSmokeTests`: passed.
  - `DialogueSmokeTests`: passed.
  - `CardBuildSmokeTests`: passed.
  - `FoundationSmokeTests`: passed.
  - `GlobalUiSmokeTests`: passed.
  - `ContentSmokeTests`: passed.
- Regression runner result:
  - `ALL_SMOKE_TESTS_PASSED`
- Log scan:
  - No C# compile errors, execute-method exceptions, failing smoke tests, missing required types, missing methods, or missing properties in final M20 logs.
  - Unity emitted shutdown/licensing noise only after successful batchmode exits.
- Repository state:
  - `/Users/Shared/Touhougodot` had no tracked worktree changes after this slice.
  - `/Users/Shared/TouhouUnityMigration` is a standalone Unity project and is not currently a git repository.

Known blockers:

- The Unity player controller does not yet consume `CookingBuffService` for live movement/combat changes. M20 exposes the service-level hooks; M21 should connect them into `MigrationPlayerController` and future combat systems.
- Item-use health currently lives inside `ItemUseService`; it is not yet wired to the global player save/progress/health runtime.
- Cooking buff notifications are C# events only; there is no production notification UI or SignalBus-style bridge yet.
- The inventory tab is still an IMGUI migration shell; quality-aware item selection now works, but it is not the final Godot-equivalent inventory UI.
- Cooking completion is still instant from M19; Godot's CookingUI progress timer remains unported.
- Global runtime save/load orchestration still does not automatically call `CookingService.CreateSnapshot`, `CookingBuffService.CreateSnapshot`, or their load methods. The save DTO/service can preserve the data once wired.

Next recommended step:

- Migrate player-facing application of cooking buffs: connect `CookingBuffService` into player movement, dash cooldown/distance, attack damage, defense/hitstun, spirit charge, regen, kill-heal, rebirth/special effects, and add first combat/movement smoke probes.

### M19: Cooking Service, Station Entry, Quality Inventory, And Save Snapshot

- Date: 2026-06-24 02:39 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Dewey and Ampere
- Goal: Port the Godot CookingManager/CookingUI vertical slice far enough that Unity can load real recipes, gate them by unlock/cookware/materials, consume ingredients, produce quality dishes, notify quests, expose a player-facing cooking entry, and preserve cooking state in saves.

Completed:

- Added `CookingServiceSmokeTests` using TDD:
  - Red run failed as expected with `Missing method TouhouMigration.Runtime.Cooking.CookingDatabase.LoadRecipesFromPath`.
  - Green run passed after implementing recipe loading, cooking service behavior, wildcard ingredients, quality rolls, and quest craft notifications.
- Extracted Godot `CookingDatabase.gd::RECIPES` into `Assets/TouhouMigration/Data/Cooking/cooking_recipes.json`:
  - 40 recipes total.
  - Preserves id, Chinese name, category, difficulty, ingredients, result id/quantity, cooking time, exp gain, description, and unlock condition.
- Expanded `CookingDatabase`:
  - keeps M18 dish combat profile classifiers.
  - loads recipes through `LoadRecipesFromPath`.
  - exposes recipe count, recipe lookup, recipe tier, cookware gate, cookware names, quality calculation, quality names, and level exp thresholds.
- Added Unity cooking runtime:
  - `CookingRecipe`
  - `CookingIngredientRequirement`
  - `CookingResult`
  - `CookingService`
  - `CookingRuntimeSnapshot`
  - `MigrationCookingStationInteractor`
- Implemented Godot-compatible cooking service rules:
  - default unlocked recipes: `onigiri`, `grilled_fish`, `miso_soup`, `dango`, `green_tea`, `herb_salad`, `mokou_yakitori`.
  - cookware tier gates: snack/drink Lv1, meal Lv2, feast Lv3.
  - wildcard ingredient counts and consumption for `fish_any` and `meat_any`.
  - deterministic quality test hook with Godot thresholds and cookware bonus.
  - result item grant with quality metadata.
  - cooking exp accumulation and level-based recipe unlock support.
  - automatic `QuestDeliveryService.NotifyCraftCompleted(resultId, resultQty)` on successful cooking.
- Expanded `InventoryService`:
  - `AddItem(itemId, amount, quality)`
  - `RemoveItem(itemId, amount, quality)`
  - `GetItemCount(itemId, quality)`
  - quality-aware stack merging while preserving old quality-agnostic calls.
- Added cooking save surface:
  - `MigrationSaveData.Cooking`.
  - save/load normalization for old saves missing cooking state.
  - cooking snapshot stores cooking level, cooking exp, cookware level, and unlocked recipes.
  - save smoke test now proves a cooked quality dish and cooking snapshot round-trip.
- Added player-facing Unity entry points:
  - `MigrationGlobalUiController` loads cooking profiles and recipes, constructs `CookingService`, seeds basic cooking ingredients for the migration slice, and binds cooking into the unified menu.
  - `MigrationUnifiedMenuController` adds a `料理` tab and can cook unlocked recipes through `CookingService`.
  - `MigrationCookingStationInteractor` opens the cooking tab on `E` or click while the player is in range.
  - `TouhouMigrationProjectBuilder` places `BambooCookingStation` in `BambooHomeVerticalSlice`.
- Integrated Dewey findings:
  - Godot cooking station is a light `Area3D` that opens CookingUI on interact/click.
  - Godot CookingUI owns the cooking timer; CookingManager consumes materials at start and completes after UI progress.
  - Godot has no behavioral test proving wildcard consumption, quality rolls, cookware rejection, or quest craft notification end-to-end.
- Integrated Ampere findings:
  - Best Unity service wiring point is the existing `MigrationGlobalUiController` cooking database initialization.
  - Best migration-era UI point is `MigrationUnifiedMenuController`.
  - Quality-aware public inventory operations were required before cooked dishes could be safe.
  - Save data already preserved slot quality but needed a cooking runtime snapshot.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Data/Cooking/cooking_recipes.json`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Cooking/CookingDatabase.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Cooking/CookingIngredientRequirement.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Cooking/CookingRecipe.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Cooking/CookingResult.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Cooking/CookingRuntimeSnapshot.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Cooking/CookingService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Cooking/MigrationCookingStationInteractor.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Inventory/InventoryService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Save/MigrationSaveData.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Save/MigrationSaveService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationUnifiedMenuController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/CookingServiceSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/GlobalUiSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/SaveInventorySmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- Unity-generated `.meta` files for the new assets/scripts.

Verification:

- Red Cooking Service test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CookingServiceSmokeTests.RunAll`
  - Result: failed as expected with `Missing method TouhouMigration.Runtime.Cooking.CookingDatabase.LoadRecipesFromPath`.
- Red UI/scene test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.GlobalUiSmokeTests.RunAll`
  - Result: failed as expected with missing `MigrationCookingStationInteractor`.
- Red Save/Cooking test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.SaveInventorySmokeTests.RunAll`
  - Result: failed as expected with missing `CookingRuntimeSnapshot`.
- Scene-builder command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: `Touhou Unity migration initial project built.`
- M19 and regression suite:
  - `CookingServiceSmokeTests`: passed.
  - `CookingQuestSmokeTests`: passed.
  - `GlobalUiSmokeTests`: passed.
  - `SaveInventorySmokeTests`: passed.
  - `QuestJournalSmokeTests`: passed.
  - `BondQuestSmokeTests`: passed.
  - `SocialLoopSmokeTests`: passed.
  - `SocialGiftSmokeTests`: passed.
  - `DialogueSmokeTests`: passed.
  - `CardBuildSmokeTests`: passed.
  - `FoundationSmokeTests`: passed.
  - `ContentSmokeTests`: passed.
- Final builder:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: `Touhou Unity migration initial project built.`
- Log scan:
  - No C# compile errors, execute-method exceptions, failing smoke tests, missing required types, missing methods, or missing properties in final M19 logs.
  - Unity licensing access-token refresh emitted a non-blocking batchmode log line.

Known blockers:

- The current cooking UI is an IMGUI migration shell inside the unified menu; it does not yet reproduce Godot's 900x560 CookingUI layout, progress timer, attribute preview, threshold preview, or final art direction.
- `CookingService.Cook` completes instantly; Godot consumes ingredients at start and completes after CookingUI's timer.
- CookingBuffSystem/dish-drink consumption effects are not ported; cooked dishes can be made and saved but eating them does not yet update combat buffs.
- Cookware paid upgrade costs and upgrade UI are not wired; service supports gates and direct cookware level setting only.
- Notification bridge for cooking start/fail/complete is not ported.
- Inventory now supports `quality`, but arbitrary `props` stack identity remains incomplete.
- Global runtime save/load orchestration still does not automatically call `CookingService.CreateSnapshot` or `LoadSnapshot`; the save DTO/service can preserve the data once wired.

Next recommended step:

- Migrate item-use and CookingBuffSystem foundations: dish/drink consumption from inventory, active cooking buff stats/effects, player combat stat hooks, buff save/load, and basic notification events.

### M18: Cooking Classifiers, Symbolic Quest Matching, And Real Reward Sink

- Date: 2026-06-24 02:17 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Heisenberg; Lovelace subagent hit a tool-layer unsupported-content error and produced no findings.
- Goal: Replace manual quest delivery tags with Godot CookingDatabase-derived classifiers, port the craft-objective notification path, and connect completed quest rewards to Unity player progress and inventory services.

Completed:

- Added `CookingQuestSmokeTests` using TDD:
  - Red run failed as expected with `Missing required type: TouhouMigration.Runtime.Cooking.CookingDatabase, Assembly-CSharp`.
  - Green run passed after implementing the M18 cooking/quest reward slice.
- Extracted Godot `CookingDatabase.gd::DISH_COMBAT_PROFILES` into `Assets/TouhouMigration/Data/Cooking/cooking_profiles.json`:
  - 40 dish combat profiles.
  - Preserves tier, main stat, stat map, buff duration, special effects, and drink effects.
- Added Unity cooking runtime types:
  - `CookingDishProfile`
  - `CookingDatabase`
  - public classifiers for combat-profile existence, dish tier, main stat, stat thresholds, and symbolic item ids.
- Implemented Godot-compatible symbolic quest selectors:
  - `meal_any`
  - `drink_any`
  - `feast_any`
  - `atk_5_plus_any`
- Expanded `QuestDeliveryService`:
  - preserves older constructors.
  - adds optional `CookingDatabase` and `QuestRewardSink` dependencies.
  - adds `NotifyCraftCompleted(itemId, amount)` for `craft`, `craft_tier`, and `craft_stat` objectives.
  - routes delivery symbolic matching through `CookingDatabase` before falling back to manual delivery tags.
  - applies real reward side effects through `QuestRewardSink` after ledger completion.
- Added player economy/reward services:
  - `MigrationPlayerProgressService` for XP and coins.
  - `QuestRewardSink` for exp/coin/item reward grants into runtime player progress and `InventoryService`.
- Updated runtime wiring:
  - `MigrationGlobalUiController` now loads the cooking database, owns player progress, creates the reward sink, and constructs `QuestDeliveryService(questDatabase, questRewardLedger, cookingDatabase, questRewardSink)`.
  - `MigrationHudController` now displays live player coins from `MigrationPlayerProgressService`.
  - `TouhouMigrationProjectBuilder` now ensures `Data/Cooking` and `Scripts/Runtime/Cooking`.
- Confirmed Unity-origin source asset policy:
  - `/Users/Shared/Touhougodot/assets/unity_imports` now contains only `README_MIGRATED_TO_UNITY.md`.
  - relocated source packs live under `/Users/Shared/TouhouUnityMigration/ExternalUnityAssets/unity_imports`.
  - strategy docs now record that original Unity assets should stay in the standalone Unity project and only selected runtime assets should be promoted into `Assets/TouhouMigration`.
- Integrated Heisenberg findings:
  - Godot `QuestManager.notify_craft_completed()` handles `craft`, `craft_tier`, and `craft_stat`.
  - Godot delivery matching resolves `meal_any`, `drink_any`, `feast_any`, and `atk_5_plus_any` from CookingDatabase metadata.
  - Formal side quest craft/delivery paths are `side_004`, `side_005`, `side_006`, and `side_007`.
  - Quest completion grants exp, coins, and items through game-state/inventory systems.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Data/Cooking/cooking_profiles.json`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Cooking/CookingDishProfile.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Cooking/CookingDatabase.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Player/MigrationPlayerProgressService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Quest/QuestRewardSink.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Social/QuestDeliveryService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationHudController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/CookingQuestSmokeTests.cs`
- Unity-generated `.meta` files for the new assets/scripts.

Verification:

- Red Cooking/Quest test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CookingQuestSmokeTests.RunAll`
  - Result: failed as expected with `Missing required type: TouhouMigration.Runtime.Cooking.CookingDatabase, Assembly-CSharp`.
- Scene-builder command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: `Touhou Unity migration initial project built.`
- Cooking/Quest test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CookingQuestSmokeTests.RunAll`
  - Result: `Cooking quest smoke tests passed.`
- Regression suite:
  - `QuestJournalSmokeTests`: passed.
  - `BondQuestSmokeTests`: passed.
  - `SocialLoopSmokeTests`: passed.
  - `SocialGiftSmokeTests`: passed.
  - `SaveInventorySmokeTests`: passed.
  - `GlobalUiSmokeTests`: passed.
  - `DialogueSmokeTests`: passed.
  - `CardBuildSmokeTests`: passed.
  - `FoundationSmokeTests`: passed.
  - `ContentSmokeTests`: passed.
- Log scan:
  - No C# compile errors, execute-method exceptions, failing smoke tests, missing required types, missing methods, or missing properties in final M18 logs.
  - Unity licensing access-token refresh emitted a non-blocking batchmode log line.

Known blockers:

- Cooking completion UI/station flow is not ported yet; M18 ports metadata classifiers, craft notifications, and quest/reward consequences.
- Inventory still does not preserve `props` stack identity, and full quality-based stack semantics remain incomplete.
- `MigrationPlayerProgressService` can load/apply slot save data, but runtime save orchestration is not yet wired through the global save flow.
- Player-facing delivery UI does not yet remove/transfer quest delivery items; `NotifyDelivery` assumes delivery has already happened.
- `ResetDailyQuests(day)` is still not wired to the Unity time system.
- Kaguya unlock and bamboo-night visit tracking are not ported.

Next recommended step:

- Migrate the CookingManager/CookingStation/CookingUI vertical slice: recipe unlock/cookware checks, wildcard ingredient consumption, dish result quality, inventory quality/props stack identity, and automatic `QuestDeliveryService.NotifyCraftCompleted` on successful cooking.

### M17: Quest Journal, Rewards, Counters, Unlocks, And Dialogue Effects

- Date: 2026-06-24 02:00 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Kant and Parfit
- Goal: Continue QuestManager/BondSystem parity by replacing Unity journal placeholders with service-backed quest data, granting quest rewards into a runtime ledger, preserving counters/NPC unlock state, and routing dialogue choice effects into the expanded Bond/Quest services.

Completed:

- Added `QuestJournalSmokeTests` using TDD:
  - Red run failed as expected with `Missing required type: TouhouMigration.Runtime.Quest.QuestRewardLedger, Assembly-CSharp`.
  - Green run passed after implementing the M17 quest journal/reward/dialogue-effect slice.
- Added `QuestRewardLedger`:
  - records quest reward exp
  - records quest reward coins
  - records item reward counts
  - can apply `QuestRewardDefinition`
  - supports snapshot/load for save-compatible runtime state
- Added `QuestJournalEntry` DTO:
  - quest id/title/description/type/status
  - objective counts
  - completed objective counts
  - progress text
  - reward text
- Expanded `QuestDeliveryService`:
  - preserves M15/M16 constructors
  - adds `QuestDeliveryService(QuestDatabase, QuestRewardLedger)`
  - grants parsed quest rewards only from automatic objective completion, not from `MarkQuestCompleted` fixture/prerequisite setup
  - adds `IncrementCounter`
  - adds `GetCounter`
  - adds `UnlockNpc`
  - adds `IsNpcUnlocked`
  - adds `IsQuestStarted`
  - adds sorted active/completed quest id queries for dialogue context
  - adds `GetJournalEntries(status)` for active/completed/all journal views
  - adds basic QuestManager-style events for quest started, quest completed, progress updated, counter changed, and NPC unlocked
  - extends quest snapshots with counters, unlocked NPCs, last daily reset day, and reward ledger data
  - adds a minimal `ResetDailyQuests(day)` service entry point; clock/event integration remains future work
- Added `DialogueEffectRouter`:
  - routes `bond` effects through `SocialBondService.AddBondPoints(npc, "dialogue", value)`
  - routes `quest`/`start_quest`
  - routes `counter`
  - routes `unlock_npc`
  - supports explicit `quest_progress` dictionary payloads for future scripted hooks
  - intentionally does not route gift entry-level `fx` to avoid double-counting gift bond changes already handled by `GiftInteractionService`
- Updated `MigrationUnifiedMenuController`:
  - preserves older bind overloads
  - adds a service bind overload for quest database, quest delivery, and social bond services
  - exposes `QuestJournalEntryCount`
  - exposes `GetQuestJournalEntries(status)`
  - replaces the journal placeholder with active/completed quest lists and Godot-compatible empty labels
  - shows reward and progress text from the service model
- Updated `MigrationGlobalUiController`:
  - now owns the M17 `QuestRewardLedger`
  - constructs `QuestDeliveryService(questDatabase, questRewardLedger)`
  - constructs and subscribes `DialogueEffectRouter`
  - binds Quest/Bond services into the unified menu
  - builds dialogue context from current bond level plus active/completed/started quest ids so `quest_active` and `quest_completed` conditions can resolve
- Integrated Kant findings:
  - Formal Godot route is `GlobalUIManager -> UnifiedGameMenu("journal")`, not a current standalone quest scene.
  - Legacy `QuestUI.gd` text contracts include `暂无任务`, `暂无已完成任务`, `[主线]`, `[支线]`, `[每日]`, progress text, and reward text.
  - Godot QuestManager state also includes counters, unlocked NPCs, daily reset day, and delivery-variety state.
  - Kaguya unlock depends on Keine bond, Marisa bond, and bamboo-night visit count; this remains future parity work.
- Integrated Parfit findings:
  - Existing Unity `DialogueRuntimeFacade.ActionRequested` is the right connection point for choice effects.
  - `MigrationUnifiedMenuController` should preserve existing bind overloads and add quest service binding.
  - Journal entries should be sorted deterministically.
  - Reward grants should happen only from automatic completion to avoid polluting prerequisite fixtures.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Quest/QuestRewardLedger.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Quest/QuestJournalEntry.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Social/QuestDeliveryService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Dialogue/DialogueEffectRouter.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationUnifiedMenuController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/QuestJournalSmokeTests.cs`

Verification:

- Red Quest Journal test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.QuestJournalSmokeTests.RunAll`
  - Result: failed as expected with `Missing required type: TouhouMigration.Runtime.Quest.QuestRewardLedger, Assembly-CSharp`.
- Scene-builder command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: `Touhou Unity migration initial project built.`
- Quest Journal test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.QuestJournalSmokeTests.RunAll`
  - Result: `Quest journal smoke tests passed.`
- Bond/Quest regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.BondQuestSmokeTests.RunAll`
  - Result: `Bond quest smoke tests passed.`
- Social loop regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.SocialLoopSmokeTests.RunAll`
  - Result: `Social loop smoke tests passed.`
- Social gift regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.SocialGiftSmokeTests.RunAll`
  - Result: `Social gift smoke tests passed.`
- Save/inventory regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.SaveInventorySmokeTests.RunAll`
  - Result: `Save inventory smoke tests passed.`
- Global UI regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.GlobalUiSmokeTests.RunAll`
  - Result: `Global UI smoke tests passed.`
- Dialogue regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.DialogueSmokeTests.RunAll`
  - Result: `Dialogue smoke tests passed.`
- CardBuild regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CardBuildSmokeTests.RunAll`
  - Result: `CardBuild smoke tests passed.`
- Foundation regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.FoundationSmokeTests.RunAll`
  - Result: `Foundation smoke tests passed.`
- Content regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.ContentSmokeTests.RunAll`
  - Result: `Content smoke tests passed.`
- Log scan:
  - No C# compile errors, execute-method exceptions, failing smoke tests, missing required types, missing methods, or missing properties in final M17 logs.
  - Unity licensing access-token refresh emitted a non-blocking batchmode log line.

Known blockers:

- `QuestRewardLedger` is currently an audit/runtime ledger; rewards are not yet applied to the real player XP, coin, and inventory services.
- Quest UI is service-backed but still an IMGUI migration shell, not production uGUI/UITK presentation.
- `ResetDailyQuests(day)` exists on the service, but it is not yet wired to `GameClock`/day-start events.
- Kaguya unlock and bamboo-night visit tracking are not ported.
- Quest/Bond event dispatch exists as C# service events, but there are no UI notifications, SignalBus-equivalent bridge, or BondEventSystem consumers yet.
- Dialogue route handles choice/action payloads; entry-level gift `fx` remain intentionally un-routed to avoid duplicate gift bond changes.

Next recommended step:

- Migrate CookingDatabase/classifier support so `meal_any`, `drink_any`, `atk_5_plus_any`, and `feast_any` can be resolved from real item/cooking metadata, then connect quest rewards to inventory/coins/XP and wire daily reset to the Unity time system.

### M16: BondSystem And Quest Delivery Foundation

- Date: 2026-06-24 01:41 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Russell and Huygens
- Goal: Replace the thinnest M15 Bond/Quest adapters with formal Unity foundations for Godot BondSystem levels/sources/snapshots and QuestManager delivery objective matching.

Completed:

- Migrated Godot `QuestData.gd` definitions into `Assets/TouhouMigration/Data/Quests/quests.json`:
  - 15 quests total
  - 5 main quests
  - 7 side quests
  - 3 daily quests
  - 3 `deliver` objectives
  - 1 `deliver_variety` objective
- Added Unity quest data models:
  - `QuestDefinition`
  - `QuestObjectiveDefinition`
  - `QuestRewardDefinition`
  - `QuestDatabase`
- `QuestDatabase` now loads the migrated QuestData JSON and preserves:
  - id/type/title/description
  - objectives
  - rewards
  - prerequisites
  - `next_quest`
  - `unique_required`
  - delivery objective NPC filters
- Expanded `SocialBondService` from M15 adapter into a first-class BondSystem foundation:
  - Godot threshold table `[0, 100, 250, 500, 800, 1200, 1700, 2300, 3000, 4000, 5000]`
  - max level `10`
  - source table for `dialogue`, `gift`, `gift_loved`, `quest_help`, `combat_together`, `event_completion`, and `daily_interaction`
  - unknown source fallback `10`
  - source base plus bonus semantics
  - no level-down on negative deltas
  - `SetBondLevel`
  - daily interaction once per NPC
  - daily reset
  - snapshot/load for bonds and daily interaction state
- Expanded `QuestDeliveryService`:
  - optional `QuestDatabase` constructor while preserving the M15 default constructor
  - `StartQuest`
  - prerequisite checks
  - `MarkQuestCompleted`
  - `IsQuestActive`
  - `IsQuestCompleted`
  - `GetQuestProgress`
  - `UpdateQuestProgress`
  - `NotifyDelivery`
  - `deliver` progress clamping
  - `deliver_variety` unique delivered item id tracking
  - duplicate variety delivery ignore
  - NPC-filtered delivery objectives
  - `RegisterDeliveryTag` for symbolic selectors such as `drink_any`
  - completion moves quests from active to completed
  - snapshot/load for active quests, completed quests, delivery-variety state, and last delivery metadata
- Added slot-save integration:
  - `MigrationSaveData.SocialBonds`
  - `MigrationSaveData.Quests`
  - `MigrationSaveService` normalization for older saves with missing social/quest snapshots
- Updated runtime wiring:
  - `MigrationGlobalUiController` loads `QuestDatabase`
  - runtime gift delivery now uses `QuestDeliveryService(questDatabase)`
- Updated builder:
  - ensures `Data/Quests`
  - ensures `Scripts/Runtime/Quest`
- Added `BondQuestSmokeTests` using TDD:
  - Red run failed on missing `TouhouMigration.Runtime.Quest.QuestDatabase`, proving the first M16 test captured missing quest foundation.
  - Red run failed on missing `MigrationSaveData.SocialBonds`, proving save integration was not present before implementation.
  - Green run passed after implementation.
- Integrated Russell findings:
  - Godot BondSystem state shape is `npc_bonds` plus `daily_interacted`.
  - Unknown bond sources default to `10`.
  - Negative point deltas do not lower levels.
  - Central Godot `SaveSystem` currently omits BondSystem, so Unity M16 intentionally improves the persistence foundation.
- Integrated Huygens findings:
  - Godot QuestManager delivery scans active quests, filters `deliver`/`deliver_variety`, filters optional NPC, and then applies symbolic item matching.
  - `deliver_variety` tracks unique delivered item ids per objective index.
  - Current formal delivery objectives are `side_004`, `side_005`, `side_006`, and `side_007`.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Data/Quests/quests.json`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Quest/QuestDefinition.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Quest/QuestObjectiveDefinition.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Quest/QuestRewardDefinition.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Quest/QuestDatabase.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Social/SocialBondService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Social/QuestDeliveryService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Save/MigrationSaveData.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Save/MigrationSaveService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/BondQuestSmokeTests.cs`

Verification:

- Red quest foundation test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.BondQuestSmokeTests.RunAll`
  - Result: failed as expected with `Missing required type: TouhouMigration.Runtime.Quest.QuestDatabase, Assembly-CSharp`.
- Red save integration test:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.BondQuestSmokeTests.RunAll`
  - Result: failed as expected with `Missing property TouhouMigration.Runtime.Save.MigrationSaveData.SocialBonds`.
- Scene-builder command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: `Touhou Unity migration initial project built.`
- Bond/Quest test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.BondQuestSmokeTests.RunAll`
  - Result: `Bond quest smoke tests passed.`
- Social loop regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.SocialLoopSmokeTests.RunAll`
  - Result: `Social loop smoke tests passed.`
- Social gift regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.SocialGiftSmokeTests.RunAll`
  - Result: `Social gift smoke tests passed.`
- Save/inventory regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.SaveInventorySmokeTests.RunAll`
  - Result: `Save inventory smoke tests passed.`
- Global UI regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.GlobalUiSmokeTests.RunAll`
  - Result: `Global UI smoke tests passed.`
- Dialogue regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.DialogueSmokeTests.RunAll`
  - Result: `Dialogue smoke tests passed.`
- CardBuild regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CardBuildSmokeTests.RunAll`
  - Result: `CardBuild smoke tests passed.`
- Foundation regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.FoundationSmokeTests.RunAll`
  - Result: `Foundation smoke tests passed.`
- Content regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.ContentSmokeTests.RunAll`
  - Result: `Content smoke tests passed.`
- Data contract check:
  - Result: Unity `quests.json` has 15 quests, 5 main, 7 side, 3 daily, 3 `deliver`, and 1 `deliver_variety`.
- Log scan:
  - No C# compile errors, C# warnings, exceptions, missing asset loads, failed imports, or failed tests in final M16 logs.
  - Unity licensing access-token refresh emitted a non-blocking batchmode log line.

Known blockers:

- `QuestDeliveryService` has objective matching and completion, but not reward callbacks, reward grants, quest UI, daily reset, NPC unlock, counters, or event dispatch parity yet.
- Symbolic delivery matching currently uses explicit `RegisterDeliveryTag`; it is not yet backed by migrated CookingDatabase tier/stat classifiers.
- Bond events are not dispatched as Unity events yet; the service records last source/delta/levels for tests but does not drive UI notifications or BondEventSystem.
- Koishi and Kaguya special-case gift behavior is still not represented in Unity runtime.
- Dialogue choice `bond` effects are not yet wired to the expanded source+bonus semantics.
- Quest/Bond snapshots are now in slot save data, but there is no load-game screen or runtime save/load orchestration using them yet.

Next recommended step:

- Continue social/quest parity by adding Quest UI/journal data presentation and reward/counter/unlock handling, then wire dialogue choice effects into the expanded Bond/Quest services.

### M15: Player Gift Selection Loop With Bond And Quest Adapters

- Date: 2026-06-24 01:23 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Kepler and Averroes
- Goal: Turn the M14 service-level gift delivery into a player-facing Unity loop: open gift selection from an NPC, list valid gifts from inventory, deliver the selected gift, update social/quest adapters, start reaction dialogue, and block gameplay input while the gift UI is open.

Completed:

- Added `SocialLoopSmokeTests` using TDD:
  - Red run failed on missing `TouhouMigration.Runtime.Social.SocialBondService`, proving the first test captured the M15 adapter gap.
  - A second red run failed on missing `MigrationGlobalUiController.BlocksGameplayInput`, proving the input-blocking contract was missing before implementation.
  - Green run passed after implementation.
- Added `SocialBondService`:
  - records per-NPC bond points in memory
  - applies successful gift bond deltas from `GiftDeliveryResult`
  - records source ids `gift_positive` or `gift_negative`
- Added `QuestDeliveryService`:
  - records successful gift delivery notifications
  - preserves item id, target NPC id, amount, and event count for future QuestManager parity
- Added `GiftSelectionOption` DTO and `MigrationGiftSelectionController`:
  - opens for a target NPC id/display name
  - lists only inventory items that exist in `GiftDatabase`
  - sorts strongest reactions first
  - previews display name, amount, reaction id, and bond delta
  - delegates final selection to `GiftInteractionService`
  - closes after successful delivery
  - renders an IMGUI migration-era gift modal
- Updated `GiftInteractionService`:
  - preserves the existing four-argument constructor used by M14 tests
  - adds an optional six-argument constructor with Bond/Quest adapters
  - notifies Bond/Quest only after inventory removal and successful reaction creation
- Updated `MigrationGlobalUiController`:
  - owns the gift-selection controller binding
  - owns first-slice `SocialBondService` and `QuestDeliveryService`
  - exposes `OpenGiftSelectionForNpc`
  - exposes `SelectGiftForCurrentNpc`
  - exposes `BlocksGameplayInput` and `IsGameplayInputBlocked`
- Updated runtime input pollers:
  - `MigrationNpcInteractor` now opens gift selection on `G` and keeps direct preferred-gift delivery as a debug/service path.
  - `MigrationPlayerController`, `MigrationHudController`, and `MigrationNpcInteractor` now respect global UI input blocking.
- Updated `TouhouMigrationProjectBuilder`:
  - mounts `MigrationGiftSelectionController` on the existing `MigrationGlobalUI` object in Bamboo Home and Human Village.
- Integrated Kepler findings:
  - Godot removes inventory before NPC lookup/reaction and notifies quest delivery after gift delivery.
  - Godot gift UI filters inventory to giftable item classes and emits only an item id.
  - Godot's current live UI previews use older `NPCDatabase.get_gift_reaction`, but Unity intentionally keeps M14's richer `GiftDatabase` convergence.
  - Godot BondSystem adds source-based bond points and emits bond signals; Unity M15 preserves the event seam as an in-memory adapter.
- Integrated Averroes findings:
  - The clean Unity seam is `MigrationGlobalUiController` plus a migration-era IMGUI gift controller.
  - `GiftInteractionService` should remain the backend seam and notify adapters only on success.
  - Gift selection open state should block gameplay/NPC/HUD input polling.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/SocialLoopSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Social/SocialBondService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Social/QuestDeliveryService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/GiftSelectionOption.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGiftSelectionController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Social/GiftInteractionService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Social/MigrationNpcInteractor.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationHudController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Player/MigrationPlayerController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`

Verification:

- Red adapter test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.SocialLoopSmokeTests.RunAll`
  - Result: failed as expected with `Missing required type: TouhouMigration.Runtime.Social.SocialBondService, Assembly-CSharp`.
- Red input-blocking test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.SocialLoopSmokeTests.RunAll`
  - Result: failed as expected with `Missing property TouhouMigration.Runtime.UI.MigrationGlobalUiController.BlocksGameplayInput`.
- Scene-builder command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: `Touhou Unity migration initial project built.`
- Social loop test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.SocialLoopSmokeTests.RunAll`
  - Result: `Social loop smoke tests passed.`
- Social gift regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.SocialGiftSmokeTests.RunAll`
  - Result: `Social gift smoke tests passed.`
- Dialogue regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.DialogueSmokeTests.RunAll`
  - Result: `Dialogue smoke tests passed.`
- Save/inventory regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.SaveInventorySmokeTests.RunAll`
  - Result: `Save inventory smoke tests passed.`
- Global UI regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.GlobalUiSmokeTests.RunAll`
  - Result: `Global UI smoke tests passed.`
- CardBuild regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CardBuildSmokeTests.RunAll`
  - Result: `CardBuild smoke tests passed.`
- Foundation regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.FoundationSmokeTests.RunAll`
  - Result: `Foundation smoke tests passed.`
- Content regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.ContentSmokeTests.RunAll`
  - Result: `Content smoke tests passed.`
- Log scan:
  - No C# compile errors, C# warnings, exceptions, missing asset loads, failed imports, or failed tests in final M15 logs.
  - Unity licensing access-token refresh emitted a non-blocking batchmode log line.

Known blockers:

- `SocialBondService` is an in-memory adapter only; full BondSystem persistence, levels, signals, notifications, and source-table behavior are not migrated yet.
- `QuestDeliveryService` records delivery events only; full `deliver` and `deliver_variety` objective matching, rewards, next-quest transitions, and quest persistence are not migrated yet.
- The gift-selection UI is an IMGUI migration shell. It is usable for the slice but not the final uGUI/UITK production presentation.
- Unity gift previews use the richer `GiftDatabase` contract, intentionally diverging from Godot's current older `NPCDatabase.get_gift_reaction` preview path.
- Koishi/Kaguya special-case NPC delivery overrides are not represented in Unity behavior yet.
- Gift/Bond/Quest state is not saved in slot or meta saves yet.
- Gift IDs still must match inventory item IDs; gift-only ids such as `history_book` cannot be delivered until item availability policy is expanded.

Next recommended step:

- Move into the broader social-system pass: migrate NPC social state/BondSystem levels and persistence, then connect quest objective matching so gift delivery can complete real active quests instead of only recording adapter events.

### M14: Social Gift Data And Human Village NPC Interactors

- Date: 2026-06-24 01:06 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Descartes and Sartre
- Goal: Port the formal gift data/reaction foundation and connect first Human Village NPC markers to the Unity dialogue and inventory services.

Completed:

- Copied Godot `data/gifts.json` into `Assets/TouhouMigration/Data/Social/gifts.json`.
- Added social gift runtime models and results:
  - `GiftDefinition`
  - `GiftReactionResult`
  - `GiftDeliveryResult`
- Added `GiftDatabase`:
  - loads the Godot gift JSON shape
  - indexes 30 gifts, 34 NPC preference records, 34 birthdays, and birthday multiplier `2`
  - preserves gift categories, tags, reactions, dialogue lines, special behavior, and special event ids
  - implements the richer Godot `GiftDatabase.gd` reaction priority: special, loves, likes, hates, dislikes, then tag scoring
  - computes first-slice bond deltas with category bonuses
- Added `GiftInteractionService`:
  - validates gift id and inventory count
  - removes the delivered item from `InventoryService`
  - computes the NPC reaction through `GiftDatabase`
  - starts a one-line reaction dialogue through `DialogueRuntimeFacade`
  - returns success, failure reason, reaction, and bond delta for future Bond/Quest adapters
- Added `MigrationNpcInteractor`:
  - stores NPC id, display name, preferred gift id, and interaction radius
  - starts dialogue through `MigrationGlobalUiController`
  - can give the configured preferred gift through the new gift service
- Updated `MigrationGlobalUiController`:
  - initializes the gift database and gift interaction service
  - exposes `StartDialogueForNpc`
  - exposes `GiveGiftToNpc`
  - seeds first-slice giftable inventory examples for Marisa/Reimu smoke paths
- Updated `TouhouMigrationProjectBuilder`:
  - ensures `Data/Social` and `Scripts/Runtime/Social`
  - places first Human Village NPC marker capsules for Marisa, Reimu, and Keine
  - configures preferred gifts: `magic_crystal`, `green_tea`, and `history_book`
- Added `SocialGiftSmokeTests` using TDD:
  - Red run failed on missing `TouhouMigration.Runtime.Social.GiftDatabase`, proving the test captured the missing M14 slice.
  - Green run passed after implementation.
- Integrated Descartes findings:
  - Godot has two gift contracts: the richer `data/gifts.json` plus `GiftDatabase.gd`, and an older live `NPCDatabase.get_gift_reaction` path.
  - Unity should converge on the richer JSON contract now, then bridge Bond/Quest behavior later.
  - Birthday multiplier exists in data, but Godot birthday checks are effectively inactive in the current gift path.
- Integrated Sartre findings:
  - `MigrationGlobalUiController` is the right first seam because it already owns inventory and dialogue facades.
  - Human Village builder should create stable NPC marker interactors before full NPC model/schedule migration.
  - `history_book` is a valid gift but not yet an item-database item, so future item parity must decide whether to expand `items.json` or gate unavailable gifts.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Data/Social/gifts.json`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Social/GiftDefinition.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Social/GiftReactionResult.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Social/GiftDeliveryResult.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Social/GiftDatabase.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Social/GiftInteractionService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Social/MigrationNpcInteractor.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/SocialGiftSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`

Verification:

- Red test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.SocialGiftSmokeTests.RunAll`
  - Result: failed as expected with `Missing required type: TouhouMigration.Runtime.Social.GiftDatabase, Assembly-CSharp`.
- Scene-builder command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: `Touhou Unity migration initial project built.`
- Social gift test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.SocialGiftSmokeTests.RunAll`
  - Result: `Social gift smoke tests passed.`
- Dialogue regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.DialogueSmokeTests.RunAll`
  - Result: `Dialogue smoke tests passed.`
- Save/inventory regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.SaveInventorySmokeTests.RunAll`
  - Result: `Save inventory smoke tests passed.`
- CardBuild regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CardBuildSmokeTests.RunAll`
  - Result: `CardBuild smoke tests passed.`
- Global UI regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.GlobalUiSmokeTests.RunAll`
  - Result: `Global UI smoke tests passed.`
- Foundation regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.FoundationSmokeTests.RunAll`
  - Result: `Foundation smoke tests passed.`
- Content regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.ContentSmokeTests.RunAll`
  - Result: `Content smoke tests passed.`
- Data contract check:
  - Result: Unity `gifts.json` has 30 gifts, 34 preferences, 34 birthdays, and multiplier `2`.
- Log scan:
  - No C# compile errors, C# warnings, exceptions, missing asset loads, failed imports, or failed tests in final M14 logs.
  - Unity licensing access-token refresh emitted a non-blocking batchmode log line.

Known blockers:

- `GiftInteractionService` returns bond deltas but is not yet wired to a Unity BondSystem adapter.
- Successful gifts do not yet notify a Unity QuestManager adapter.
- The full `GiftSelectionUI` modal is not migrated; current path supports service-level delivery and marker preferred-gift delivery only.
- `history_book`, `spell_card`, and many other valid gifts are not in the 200-item `items.json` database. Unity currently fails cleanly when inventory lacks them; a future item parity pass must decide the canonical availability policy.
- Birthday multiplier is loaded but birthday checks are not applied yet, matching the effectively inactive Godot birthday path.
- NPC markers are simple capsule placeholders, not full character models, schedules, navigation, or social-state presentation.
- Koishi special gift behavior is preserved in data but not yet surfaced through a dedicated Unity NPC behavior.

Next recommended step:

- Deepen the social slice by adding the full gift-selection modal, Bond/Quest adapters, and a small Human Village social smoke scene that verifies open UI, choose gift, consume item, update bond, and route quest-delivery events.

### M13: Dialogue Runtime And Rune Dialogue UI Core

- Date: 2026-06-24 00:52 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Tesla and Laplace
- Goal: Port the formal dialogue data/runtime/UI foundation so Unity scenes can load Godot NPC dialogue JSON, run session-scoped dialogue, commit choices/effects, and display a Rune-style dialogue panel.

Completed:

- Copied all 35 Godot compact NPC dialogue JSON files from `scripts/data/_npc_*.json` into `Assets/TouhouMigration/Data/Dialogue`.
- Copied 5 generated Marisa Rune portrait images into `Assets/TouhouMigration/Art/UI/Dialogue/Marisa/Portraits`.
- Added dialogue data models:
  - `DialogueLine`
  - `DialogueChoice`
  - `DialogueViewModel`
- Added `DialogueDatabase`:
  - loads the compact Godot `_npc_*.json` shape
  - indexes 35 NPC ids
  - preserves NPC display names
  - normalizes compact `l` line arrays into runtime line objects
  - parses compact `c` condition strings
  - parses compact `ch` choices into `{ text, effects }`
  - parses `fx` effect pairs for future gift/line-effect parity
  - supports first-slice condition checks for bond, humanity, weather, moon, trigger, times met, talk count, events, and quests
- Added `DialogueRuntime`:
  - starts lines
  - exposes active/inactive view models
  - advances line index
  - commits choices
  - emits effect action requests
  - cancels and finishes with reasons matching the Godot contract
- Added `DialogueRuntimeFacade`:
  - allocates positive session ids
  - rejects stale `choose_for_session` calls
  - surfaces committed choice index
  - scopes action payloads with session id
- Added `RuneDialogueController`:
  - migration-era IMGUI Rune-style dialogue panel
  - typewriter state
  - speaker/text/choice rendering
  - keyboard advance/choice behavior
  - facade binding
- Updated `MigrationGlobalUiController`:
  - loads the dialogue database
  - owns a `DialogueRuntimeFacade`
  - binds the Rune dialogue controller
  - exposes a temporary `D` key sample dialogue path for local runtime inspection
- Updated `TouhouMigrationProjectBuilder`:
  - ensures `Data/Dialogue`, `Scripts/Runtime/Dialogue`, and `Scripts/Runtime/UI/Dialogue`
  - mounts `RuneDialogueController` in Bamboo Home and Human Village global UI
- Added `DialogueSmokeTests` using TDD:
  - Red run failed on missing `TouhouMigration.Runtime.Dialogue.DialogueDatabase`, proving the test captured the missing M13 slice.
  - Green run passed after implementation.
- Integrated Tesla findings:
  - first-slice contract should be `DialogueRuntime` + `DialogueRuntimeFacade`, not legacy `DialogueManager`
  - Unity should preserve start/advance/choose/cancel, view model, session id, and choice action payload behavior
  - first-slice choice effects should surface `bond`, `event`, `quest`, `shop`, and `item` action ids
- Integrated Laplace findings:
  - formal dialogue content is 35 compact NPC JSON files
  - current compact source volume is 636 entries, 657 line records, and 107 choices
  - only 7 NPCs have Dialogic `.dch`; Unity should not depend on Dialogic internals

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Data/Dialogue/_npc_*.json`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Art/UI/Dialogue/Marisa/Portraits/*.png`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Dialogue/DialogueChoice.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Dialogue/DialogueLine.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Dialogue/DialogueViewModel.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Dialogue/DialogueDatabase.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Dialogue/DialogueRuntime.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Dialogue/DialogueRuntimeFacade.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/Dialogue/RuneDialogueController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/DialogueSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`

Verification:

- Red test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.DialogueSmokeTests.RunAll`
  - Result: failed as expected with `Missing required type: TouhouMigration.Runtime.Dialogue.DialogueDatabase, Assembly-CSharp`.
- Scene-builder command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: `Touhou Unity migration initial project built.`
- Dialogue test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.DialogueSmokeTests.RunAll`
  - Result: `Dialogue smoke tests passed.`
- Save/inventory regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.SaveInventorySmokeTests.RunAll`
  - Result: `Save inventory smoke tests passed.`
- CardBuild regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CardBuildSmokeTests.RunAll`
  - Result: `CardBuild smoke tests passed.`
- Global UI regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.GlobalUiSmokeTests.RunAll`
  - Result: `Global UI smoke tests passed.`
- Foundation regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.FoundationSmokeTests.RunAll`
  - Result: `Foundation smoke tests passed.`
- Content regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.ContentSmokeTests.RunAll`
  - Result: `Content smoke tests passed.`
- Log scan:
  - No C# compile errors, C# warnings, exceptions, missing asset loads, failed imports, or failed tests in final M13 logs.
  - Unity licensing access-token refresh emitted a non-blocking batchmode log line.

Known blockers:

- `DialogueDatabase` currently chooses the first valid entry deterministically for testability; Godot uses random selection among valid entries.
- Entry-level and line-level `fx` are parsed but not yet applied by the runtime. First slice applies choice effects only.
- `DialogueRuntimeFacade` surfaces action ids and payloads, but it does not yet connect to Unity-side Bond, Quest, Shop, or Inventory services.
- The Rune controller is an IMGUI migration shell and does not yet use copied portrait textures in layout. Full portrait catalog parity is still pending.
- Gift selection and `data/gifts.json` are not migrated yet; Laplace identified them as the next dialogue-adjacent content slice.
- `mokou` has a Dialogic character resource but no compact `_npc_mokou.json`; Unity cannot load Mokou-specific NPC dialogue until source content exists or a fallback policy is chosen.

Next recommended step:

- Start NPC/social/gift integration: port `data/gifts.json`, add a gift preference/reaction service, then put simple interactable NPC markers in Human Village that launch the new dialogue facade.

### M12: Save, Inventory, And Item Data Core

- Date: 2026-06-24 00:34 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Feynman and Linnaeus
- Goal: Port the formal save/inventory/item data foundation so settings, CardBuild profiles, inventory UI, shops, dialogue state, and future gameplay systems have a persistence backbone.

Completed:

- Copied Godot `data/items.json` into `Assets/TouhouMigration/Data/Items/items.json`.
- Added a small project-owned JSON parser for dynamic Godot-style dictionaries:
  - `MigrationJson`
- Added item data runtime:
  - `ItemDefinition`
  - `ItemDatabase`
- `ItemDatabase` now loads the Godot item database shape:
  - 13 categories
  - 200 unique item ids
  - Godot category-to-item-type mapping
  - equipment default max stack `1`
  - non-equipment default max stack `99`
  - explicit `spirit_crystal` max stack `999`
- Added inventory runtime:
  - `InventorySlotData`
  - `InventorySnapshot`
  - `InventoryService`
- `InventoryService` now supports:
  - 48-slot inventories
  - add/remove
  - stack splitting by item max stack
  - item counting
  - compacting removed empty slots
  - snapshot create/load using canonical `{ item_id, amount, quality }` slots
- Added slot-save runtime:
  - `MigrationSaveData`
  - `MigrationSavePosition`
  - `MigrationSaveInfo`
  - `MigrationSaveService`
- `MigrationSaveService` now writes/loads `save_slot_<slot>.json` files with:
  - `save_schema = 3`
  - `version = "3.0.0"`
  - player name, level, HP, coins, scene, position, inventory, play time, total kills
- Updated runtime UI binding:
  - `MigrationGlobalUiController` loads the item database and initializes a first inventory with Godot's default test seeds: `seed_apple`, `seed_carrot`, and `seed_tomato` x10.
  - `MigrationHudController` can show fixed tools plus live inventory items in the hotbar.
  - `MigrationUnifiedMenuController` inventory tab now shows real inventory capacity and item rows.
- Added `SaveInventorySmokeTests` using TDD:
  - Red run failed on missing `ItemDatabase`, proving the test captured the missing M12 slice.
  - Green run passed after implementation.
- Integrated Feynman findings:
  - Slot saves live at `user://saves/save_slot_<slot>.json`.
  - Current schema is `save_schema = 3`, `version = "3.0.0"`.
  - Slot saves and meta/settings/CardBuild persistence are separate Godot surfaces.
- Integrated Linnaeus findings:
  - 200 item ids across 13 categories.
  - Effective stack distribution: 11 equipment items stack 1, 188 items stack 99, 1 currency item stacks 999.
  - Canonical inventory slot shape is `{ item_id, amount, quality, props? }`; legacy `{ id, amount }` support remains a future parity task.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Data/Items/items.json`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Serialization/MigrationJson.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Inventory/ItemDefinition.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Inventory/ItemDatabase.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Inventory/InventorySnapshot.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Inventory/InventoryService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Save/MigrationSaveData.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Save/MigrationSaveService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationHudController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationUnifiedMenuController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/SaveInventorySmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`

Verification:

- Red test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.SaveInventorySmokeTests.RunAll`
  - Result: failed as expected with `Missing required type: TouhouMigration.Runtime.Inventory.ItemDatabase, Assembly-CSharp`.
- Scene-builder command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: `Touhou Unity migration initial project built.`
- Save/inventory test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.SaveInventorySmokeTests.RunAll`
  - Result: `Save inventory smoke tests passed.`
- CardBuild regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CardBuildSmokeTests.RunAll`
  - Result: `CardBuild smoke tests passed.`
- Global UI regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.GlobalUiSmokeTests.RunAll`
  - Result: `Global UI smoke tests passed.`
- Foundation regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.FoundationSmokeTests.RunAll`
  - Result: `Foundation smoke tests passed.`
- Content regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.ContentSmokeTests.RunAll`
  - Result: `Content smoke tests passed.`
- Log scan:
  - No C# compile errors, C# warnings, exceptions, missing asset loads, or failed imports in the final M12 logs.
  - Unity licensing access-token refresh emitted a non-blocking batchmode log line.

Known blockers:

- Meta save (`user://touhou_phantom_save.dat`), settings file (`user://game_settings.json`), CardBuild run store, and CardBuild profile migration are still separate future persistence slices.
- Unity M12 save data has a `Vector3`-safe position shape, but Godot slot saves currently serialize only `position {x, y}`. Full compatibility requires an explicit legacy-position migration policy.
- Inventory parity still needs quality/props merge tests, legacy `{ id, amount }` slot migration, swap-slot behavior, slot view DTOs, and item use/equip behavior.
- `MigrationGlobalUiController` seeds a first test inventory directly; full new-game/profile load should come from `MigrationSaveService`.
- `MigrationJson` is intentionally minimal for project data. If future JSON surfaces require comments, trailing commas, or unusual numbers, add coverage before relying on it.

Next recommended step:

- Start dialogue runtime plus Rune dialogue UI, or deepen persistence by adding meta save/settings/card-run stores. The highest player-visible value is likely dialogue/runtime interaction; the highest infrastructure value is meta/settings/card-run persistence.

### M11: CardBuild Data And Mokou Deck Shell

- Date: 2026-06-24 00:22 CST
- Status: Complete for this slice
- Owner: Codex, with read-only subagent findings from Lagrange
- Goal: Migrate the formal CardBuild data shell and make the title-screen “卡组编辑” entry open a real Mokou deck view instead of placeholder text.

Completed:

- Copied all 8 Godot CardBuild JSON files into `Assets/TouhouMigration/Data/CardBuild`:
  - `archetypes.json`
  - `boss_rules.json`
  - `cards.json`
  - `characters.json`
  - `relics.json`
  - `resources.json`
  - `statuses.json`
  - `upgrades.json`
- Added `CardBuildDatabase`:
  - loads the Unity data directory
  - indexes 12 archetypes
  - indexes 36 characters
  - generates 144 archetype cards from 12 archetypes x 12 skeleton slots
  - adds 12 explicit Mokou cards
  - validates core references
  - exposes counts, `HasCard`, `HasCharacter`, `GetCard`, `GetAllCardIds`, and `GetAvailableCardIds`
- Added `CardBuildProfile`, `CardBuildProfileValidationResult`, and `CardBuildProfileStore`.
- Mirrored Godot's first Mokou default profile:
  - character id `fujiwara_no_mokou`
  - 12-card explicit Mokou starter deck
  - 6-slot action loadout
  - profile save/load through JSON
- Added `MokouDeckEditorController`, a first Unity deck shell that displays loaded data, the default deck, and action loadout.
- Updated `TitleScreenController` and `TouhouMigrationProjectBuilder` so the title-screen “卡组编辑” button opens the deck editor overlay.
- Added `CardBuildSmokeTests` using TDD:
  - Red run failed on missing `CardBuildDatabase`, proving the test captured the missing slice.
  - Green run passed after implementation.
- Closed Lagrange's read-only CardBuild explorer after incorporating findings.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Data/CardBuild/*.json`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/CardBuild/CardBuildDatabase.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/CardBuild/CardBuildProfile.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/CardBuild/CardBuildProfileStore.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/CardBuild/MokouDeckEditorController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/TitleScreenController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/CardBuildSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/TitleScreen.unity`

Verification:

- Red test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CardBuildSmokeTests.RunAll`
  - Result: failed as expected with `Missing required type: TouhouMigration.Runtime.CardBuild.CardBuildDatabase, Assembly-CSharp`.
- Scene-builder command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: `Touhou Unity migration initial project built.`
- CardBuild test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.CardBuildSmokeTests.RunAll`
  - Result: `CardBuild smoke tests passed.`
- Global UI regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.GlobalUiSmokeTests.RunAll`
  - Result: `Global UI smoke tests passed.`
- Foundation regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.FoundationSmokeTests.RunAll`
  - Result: `Foundation smoke tests passed.`
- Content regression:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.ContentSmokeTests.RunAll`
  - Result: `Content smoke tests passed.`
- Log scan:
  - No C# compile errors, C# warnings, exceptions, missing asset loads, or failed imports in the final M11 logs.
  - Unity licensing access-token refresh emitted a non-blocking batchmode log line.

Known blockers:

- `CardBuildDatabase` only implements the first slice of Godot schema-v2 behavior. It has not yet mirrored every enum check, slot-mode default, Mokou override, effect block rule, cooldown rule, or boss-answer validation from `CardBuildDatabase.gd`.
- `CardBuildProfileStore` validates deck size, duplicate cap, existence, character pool, and required loadout presence. It does not yet enforce allowed slot + activation-mode compatibility.
- `MokouDeckEditorController` is a functional IMGUI shell, not the final card art/grid/edit/equip UI from Godot.
- Card effects, combat hand cycle, run rewards, progression, and Cirno trial integration remain future slices.

Next recommended step:

- Start save/inventory core or deepen CardBuild validation. The higher project value is likely save/inventory next, because it will persist deck profiles, inventory, settings, and future dialogue/shop state.

### M10: Unified Runtime UI, Settings, And HUD Shell

- Date: 2026-06-24 00:11 CST
- Status: Complete for this slice
- Owner: Codex, incorporating read-only subagent findings from Plato
- Goal: Port the formal runtime shell surface after Title/Bamboo/HumanVillage by adding persistent settings, a Unity scene registry, runtime HUD, unified menu shell, and settings UI hooks.

Completed:

- Added `MigrationGameSettings` with PlayerPrefs persistence for:
  - DPS display
  - room-map display
  - damage-number display
  - UI sound enabled
  - master volume
  - graphics quality
  - visual preset
  - preferred scene key
- Added `MigrationSceneRegistry` / `MigrationSceneOption` so Unity can list formal Godot scene-selection entries while marking unmigrated scenes unavailable.
- Added runtime UI controllers:
  - `MigrationHudController`
  - `MigrationUnifiedMenuController`
  - `MigrationSettingsController`
  - `MigrationGlobalUiController`
- Updated `TitleScreenController` so the title-screen Settings button opens a real settings overlay instead of placeholder status text.
- Updated `TouhouMigrationProjectBuilder` so:
  - `TitleScreen.unity` hosts `MigrationSettingsController` but no runtime HUD.
  - `BambooHomeVerticalSlice.unity` hosts `MigrationGlobalUI`, HUD, unified menu, and settings.
  - `HumanVillageVerticalSlice.unity` hosts `MigrationGlobalUI`, HUD, unified menu, and settings.
- Added `GlobalUiSmokeTests` using TDD:
  - Red run failed on missing `MigrationGameSettings`, proving the test caught the missing feature.
  - Green run passed after implementation.
- Incorporated Plato's ordered next-slice recommendation:
  - UI/settings/HUD shell first.
  - Card deck/data shell next.
  - Save/inventory, dialogue, then Cirno combat/boss after that.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Settings/MigrationGameSettings.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Data/MigrationSceneOption.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Data/MigrationSceneRegistry.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationHudController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationUnifiedMenuController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationSettingsController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/TitleScreenController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/GlobalUiSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/TitleScreen.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Verification/VisualChecks/HumanVillage_M8.png`

Verification:

- Red test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.GlobalUiSmokeTests.RunAll`
  - Result: failed as expected with `Missing required type: TouhouMigration.Runtime.Settings.MigrationGameSettings, Assembly-CSharp`.
- Scene-builder command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: `Touhou Unity migration initial project built.`
- Global UI test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.GlobalUiSmokeTests.RunAll`
  - Result: `Global UI smoke tests passed.`
- Foundation test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.FoundationSmokeTests.RunAll`
  - Result: `Foundation smoke tests passed.`
- Content test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.ContentSmokeTests.RunAll`
  - Result: `Content smoke tests passed.`
- Visual capture command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.MigrationSceneCapture.CaptureHumanVillagePreview`
  - Result: `/Users/Shared/TouhouUnityMigration/Verification/VisualChecks/HumanVillage_M8.png`
  - Pixel check: `1280x720`, `1451` sampled colors, `0` magenta samples.
- Log scan:
  - No C# compile errors, C# warnings, exceptions, missing asset loads, or failed imports in the final M10 logs.
  - Unity licensing access-token refresh emitted a non-blocking batchmode log line.

Known blockers:

- UI is a functional IMGUI migration shell. It is not the final styled uGUI/UITK production interface.
- Settings apply and persist local values, but graphics quality and visual preset are not yet mapped to URP pipeline assets, camera effects, or post-processing profiles.
- Runtime keyboard toggles exist for menu/HUD shell, but manual in-editor play validation has not yet been performed.
- Scene selector lists unmigrated Godot locations as unavailable; it does not attempt broken scene loads.
- Inventory, save, card deck, dialogue, shops, and combat HUD remain separate future slices.

Next recommended step:

- Start the card deck/data shell: migrate `data/cardbuild/*.json`, add a CardBuild runtime data loader/profile store, and make the title-screen “卡组编辑” button open a real deck UI instead of placeholder text.

### M9: Mokou Player Visual And Animation Import Slice

- Date: 2026-06-24 00:00 CST
- Status: Complete for this slice
- Owner: Codex, carrying forward McClintock's character-import checklist
- Goal: Replace the placeholder-only player presentation with a real Mokou visual model, import the first animation validation set, and create a repeatable Unity validation scene.

Completed:

- Copied Mokou visual source content into the Unity migration project:
  - `mokou.glb`
  - five Mokou PBR texture maps
  - `_rigged_mokou_cc` reference GLB, renamed to ASCII `reimu_mokou_cc.glb`
- Copied the first seven animation validation FBX files:
  - `Standing Idle.fbx`
  - `Standard Run.fbx`
  - `Fast Run.fbx`
  - `Jump.fbx`
  - `Stand To Roll.fbx`
  - `Mma Kick.fbx`
  - `Uppercut Jab.fbx`
- Updated `TouhouMigrationProjectBuilder` so every generated `MigrationPlayer` keeps its Unity `CharacterController` but attaches `MokouVisual` as the visible child when the GLB is available.
- Added renderer-bounds normalization so oversized GLB imports are automatically scaled to about 1.8 meters and grounded relative to the player or validation anchor.
- Configured imported validation FBX clips through Unity `ModelImporter`:
  - Humanoid animation type
  - avatar created from each imported model
  - loop enabled for idle/standard run/fast run
  - one-shot import for jump, roll, and attack clips
- Added `MokouCharacterValidation.unity` with:
  - normalized Mokou visual player
  - normalized CC reference rig preview
  - generated animation import markers
  - world simulation lighting
  - a camera framed for batch screenshot verification
- Added `MigrationSceneCapture.CaptureMokouCharacterPreview`.
- Added `ContentSmokeTests` to assert Mokou GLB import, reference GLB import, validation scene generation, and FBX `AnimationClip` availability.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/MigrationSceneCapture.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/ContentSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/MokouCharacterValidation.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Art/Characters/Mokou/*`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Art/Characters/ReferenceRigs/ReimuMokouCc/reimu_mokou_cc.glb`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Animations/Characters/MokouValidation/*.fbx`
- `/Users/Shared/TouhouUnityMigration/Verification/VisualChecks/MokouCharacter_M9.png`

Verification:

- Scene-builder command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: `Touhou Unity migration initial project built.`
- Builder log check:
  - No compile errors, exceptions, missing asset loads, failed imports, or pink-material warnings found in `/tmp/touhou_unity_mokou_builder2.log`.
  - Unity licensing access-token refresh emitted a non-blocking batchmode log line.
- Visual capture command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.MigrationSceneCapture.CaptureMokouCharacterPreview`
  - Result: `/Users/Shared/TouhouUnityMigration/Verification/VisualChecks/MokouCharacter_M9.png`
  - Pixel check: `1280x720`, `4176` sampled colors, `0` magenta samples.
- Foundation test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.FoundationSmokeTests.RunAll`
  - Result: `Foundation smoke tests passed.`
- Content test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.ContentSmokeTests.RunAll`
  - Result: `Content smoke tests passed.`

Known blockers:

- Mokou is currently a visible normalized GLB child on a `CharacterController`; no runtime `AnimatorController`, locomotion blend tree, attack state machine, root-motion policy, or input-to-animation wiring has been implemented yet.
- The validation FBX files import as clips, but retargeting quality and Avatar validity still need in-editor inspection against a chosen runtime humanoid rig.
- The GLB material import is good enough for screenshot validation, but toon shader/UniVRM/MToon decisions are still deferred until the character material pass.
- Human Village play validation remains pending; this milestone only proves the player visual can be attached and rendered in generated scenes.

Next recommended step:

- Build the first Unity player action architecture: AnimatorController, locomotion blend tree, one-shot action triggers, and a runtime validation scene that drives idle/run/jump/roll/attack on the Mokou visual.

### M8: Human Village First Visible Unity Slice

- Date: 2026-06-23 23:45 CST
- Status: Complete
- Owner: Codex, with read-only subagent findings from Bohr and McClintock
- Goal: Replace the Human Village blockout with a visible Unity scene slice built from relocated Unity-origin assets.

Completed:

- Promoted a focused 50 MB / 1185-file Human Village runtime subset from `ExternalUnityAssets/unity_imports` into `Assets/TouhouMigration/Art/HumanVillage`.
- Imported terrain references:
  - Suntail `Village_Terrain_terrain.obj`
  - PureNature Meadows `TerrainMeadows_terrain.obj`
  - related `SplatAlpha` images as references
- Imported selected Suntail FBX models for shops, bridge pieces, well, carts, boat, fences, lanterns, props, trees, bushes, flowers, grass, stones, and background mountains.
- Imported Suntail `Prefabs` and `Models/Building Modules` so the first house prefabs resolve nested prefab dependencies cleanly.
- Cleaned copied Godot/import-cache artifacts such as `*.import`, `*.unwrap_cache`, extracted `.mesh`/`.material` folders, and related `.meta` files from the promoted runtime subset.
- Updated `TouhouMigrationProjectBuilder` so `HumanVillageVerticalSlice.unity` now generates:
  - `SuntailVillageTerrain`
  - `PureNatureMeadowsBackdrop`
  - `SuntailVillageSetDressing`
  - `VillageHouse1`, `VillageHouse2`, `VillageHouse3`
  - visible market/shop/bridge/foliage objects
  - project-owned simple materials
  - `MigrationPlayer`
  - `BambooHomeReturnPortal` targeting `BambooHomeVerticalSlice`
- Added MeshColliders to imported model instances through the builder.
- Added `MigrationSceneCapture` to render a Human Village verification screenshot in batchmode.
- Re-ran Unity builder and foundation tests successfully.
- Integrated subagent recommendations:
  - Bohr: use `ExternalUnityAssets` as the source warehouse, defer old kit controllers and shader stacks, use PureNature/Suntail assets for the next fidelity pass.
  - McClintock: use Mokou visual GLB plus a CC-rig reference GLB for the upcoming character slice, then validate Humanoid retargeting before bulk animation import.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/MigrationSceneCapture.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Art/HumanVillage/*`
- `/Users/Shared/TouhouUnityMigration/Verification/VisualChecks/HumanVillage_M8.png`

Verification:

- Scene-builder command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: `Touhou Unity migration initial project built.`
- Builder log check:
  - No `ERROR`, compile errors, missing asset loads, failed imports, missing nested prefabs, or pink-material warnings found in the final builder log.
- Scene content check:
  - `HumanVillageVerticalSlice.unity` contains `SuntailVillageTerrain`, `PureNatureMeadowsBackdrop`, `SuntailVillageSetDressing`, `VillageHouse1`, `VillageHouse2`, `VillageHouse3`, `MarketShop1`, `RiverBridgeCenter`, `MigrationPlayer`, and `BambooHomeReturnPortal`.
  - `BambooHomeReturnPortal` serializes `targetScene: 2`, matching `BambooHomeVerticalSlice`.
- Visual capture command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.MigrationSceneCapture.CaptureHumanVillagePreview`
  - Result: `/Users/Shared/TouhouUnityMigration/Verification/VisualChecks/HumanVillage_M8.png`
  - Pixel check: `1280x720`, `1509` sampled colors, `0` magenta pixels.
- Foundation test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.FoundationSmokeTests.RunAll`
  - Result: `Foundation smoke tests passed.`

Known blockers:

- Runtime play validation has not been performed yet; terrain height alignment, player traversal, bridge crossing, and portal collision usability still need in-editor validation.
- The first visible slice intentionally uses project-owned simple materials. High-fidelity Suntail/PureNature materials and textures are deferred because old shader stacks and kit scripts may cause Unity 6 pink materials or missing-script warnings.
- Current camera proves scene content is visible, but composition still leaves a large simple terrain area. Human Village needs a second layout/material pass for polished presentation.
- Player still uses a capsule placeholder; Mokou visual/animation import is the next character milestone.
- Unity batchmode commands for the same project must be run serially; one attempted parallel test run failed on Unity's single-project-instance lock, then passed when rerun serially.

Next recommended step:

- Run a Unity visual/play validation pass for Human Village, then start the Mokou/player character slice using the McClintock asset and animation checklist.

### M7: Unity-Origin Source Packs Relocated

- Date: 2026-06-23 23:27 CST
- Status: Complete
- Owner: Codex
- Goal: Move original Unity asset packs out of the Godot source tree and make the Unity migration project the source of truth for those packs.

Completed:

- Moved all direct children of `/Users/Shared/Touhougodot/assets/unity_imports` into `/Users/Shared/TouhouUnityMigration/ExternalUnityAssets/unity_imports`.
- Moved source packs:
  - `AngryMesh_MeadowEnvironment`
  - `PureNature_1_2`
  - `PureNature_FantasyForest`
  - `PureNature_Islands`
  - `PureNature_Jungle`
  - `PureNature_Meadows`
  - `PureNature_Mountains`
  - `Suntail_Village`
  - `TerrainExport`
- Confirmed relocated source warehouse size: about 14 GB.
- Left `/Users/Shared/Touhougodot/assets/unity_imports/README_MIGRATED_TO_UNITY.md` at the historical Godot path.
- Updated the Unity README, replacement strategy, inventory, and working discipline to use `ExternalUnityAssets/unity_imports`.

Changed files:

- `/Users/Shared/Touhougodot/assets/unity_imports/README_MIGRATED_TO_UNITY.md`
- `/Users/Shared/TouhouUnityMigration/README.md`
- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/ExternalUnityAssets/unity_imports/*`

Verification:

- Directory check:
  - `/Users/Shared/TouhouUnityMigration/ExternalUnityAssets/unity_imports` contains the 9 migrated source directories.
  - `/Users/Shared/Touhougodot/assets/unity_imports` contains only the migration README.
- Size check:
  - `du -sh /Users/Shared/TouhouUnityMigration/ExternalUnityAssets/unity_imports`
  - Result: `14G`.

Known blockers:

- Godot scenes/scripts with historical `res://assets/unity_imports/...` references will no longer resolve inside Godot unless those packs are restored or paths are shimmed.
- The source packs are intentionally outside Unity `Assets/`; they are not imported by the editor until a slice promotes selected content into `Assets/TouhouMigration`.

Next recommended step:

- Promote the Human Village terrain/export subset from `ExternalUnityAssets/unity_imports/Suntail_Village` and `PureNature_Meadows` into `Assets/TouhouMigration/Art/HumanVillage`, then rebuild the Human Village vertical slice.

### M6: Bamboo Home To Human Village Portal

- Date: 2026-06-23 23:20 CST
- Status: Complete
- Owner: Codex
- Goal: Restore the first formal overworld route from Bamboo Home toward Human Village.

Completed:

- Added `ScenePortal`, a Unity trigger component that loads a configured `MigrationSceneId`.
- Tagged the migration player as `Player`.
- Added `TownPortal` to `BambooHomeVerticalSlice.unity`.
- Configured `TownPortal` to load `HumanVillageVerticalSlice`.
- Verified `BambooHomeVerticalSlice.unity` serializes `ScenePortal`, `targetScene: 3`, `requiredTag: Player`, and a `Player`-tagged `MigrationPlayer`.
- Re-ran the scene builder and foundation tests.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Services/ScenePortal.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`

Verification:

- Scene-builder command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: `Touhou Unity migration initial project built.`
- Scene content check:
  - `TownPortal` has `ScenePortal`.
  - `targetScene: 3` maps to `HumanVillageVerticalSlice`.
  - `MigrationPlayer` has tag `Player`.
- Foundation test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.FoundationSmokeTests.RunAll`
  - Result: `Foundation smoke tests passed.`

Known blockers:

- Portal currently triggers automatically on collision; interaction prompt / confirm-to-enter behavior is still pending.
- Human Village is still a blockout and has not imported its Suntail/PureNature source assets.

Next recommended step:

- Begin Human Village asset slice using original `Suntail_Village` and `PureNature_Meadows` Unity source folders, then add a return portal back to Bamboo Home.

### M5: Bamboo Home Minimal Visual Asset Import

- Date: 2026-06-23 23:17 CST
- Status: Complete
- Owner: Codex
- Goal: Replace the Bamboo Home blockout marker with the first slice of real source models while keeping asset import scoped.

Completed:

- Copied the minimal Bamboo Home visual asset set into Unity, about 49 MB total:
  - house GLB plus texture/normal/metallic-roughness maps
  - rocks, bamboo shoots, lantern, and wildflowers GLBs plus texture maps
  - `grass2.png` ground texture
- Added a reproducible Package Manager installer for Unity glTFast.
- Installed `com.unity.cloud.gltfast@6.19.0` through local proxy `127.0.0.1:10808`.
- Rebuilt `BambooHomeVerticalSlice.unity`; GLB assets now instantiate as Unity prefab instances.
- Preserved the blockout fallback in the scene builder for future missing-asset cases.
- Verified `BambooHomeVerticalSlice.unity` contains `House3D`, `Rock1`, `Bamboo1`, `Lantern1`, and `Flower1` prefab instances.
- Re-ran foundation smoke tests after installing glTFast and importing assets.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Packages/manifest.json`
- `/Users/Shared/TouhouUnityMigration/Packages/packages-lock.json`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Art/BambooHome/House/*`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Art/BambooHome/Props/*`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Art/BambooHome/Textures/grass2.png`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`

Verification:

- Package install command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.InstallGltfFastPackage`
  - Result: `Installed package: com.unity.cloud.gltfast@6.19.0`
- Scene-builder command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: `Touhou Unity migration initial project built.`
- Scene content check:
  - `BambooHomeVerticalSlice.unity` contains prefab instances for `House3D`, `Rock1`, `Bamboo1`, `Lantern1`, and `Flower1`.
- Foundation test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.FoundationSmokeTests.RunAll`
  - Result: `Foundation smoke tests passed.`

Known blockers:

- Bamboo Home visuals still need material QA, scale/camera validation, collisions, portals, grass/foliage pass, and player/avatar replacement.
- Ground material still uses simple blockout plane; copied `grass2.png` is not wired into a persistent material yet.
- No runtime screenshot/camera validation has been performed for the imported models.

Next recommended step:

- Add Bamboo Home portal interactors and scene-flow to Human Village, then run a first visual validation pass with screenshots once camera tooling is available.

### M4: Title Screen To Bamboo Home Slice

- Date: 2026-06-23 23:10 CST
- Status: Complete
- Owner: Codex
- Goal: Restore the formal app entry shape by adding a Unity title scene and a Bamboo Home vertical slice destination.

Completed:

- Imported only the title-screen slice assets from Godot into Unity:
  - `main_menu_bg.png`
  - `loading_screen_bg.png`
  - `MaShanZheng-Regular.ttf`
- Added `TitleScreenController` as a first-pass Unity-native title shell without adding uGUI package dependency.
- Added `BambooHomeVerticalSlice` scene ID and catalog entry.
- Changed `TouhouMigrationBootstrap` default initial scene to `TitleScreen`, matching Godot's formal main scene entry.
- Updated the scene builder to generate:
  - `Bootstrap.unity`
  - `TitleScreen.unity`
  - `BambooHomeVerticalSlice.unity`
  - `HumanVillageVerticalSlice.unity`
- Wired title menu actions for new game / continue / Bamboo Home into `BambooHomeVerticalSlice`.
- Added `WorldSimulation`, `Sun`, `Moon`, player capsule, camera, ground, and a Bamboo Home blockout marker to the Bamboo Home slice.
- Verified Build Settings contain all four generated scenes.
- Re-ran foundation smoke tests after the scene and UI additions.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Bootstrap/TouhouMigrationBootstrap.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Data/MigrationSceneId.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Data/MigrationSceneCatalog.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/UI/TitleScreenController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Art/UI/Backgrounds/main_menu_bg.png`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Art/UI/Backgrounds/loading_screen_bg.png`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Art/Fonts/MaShanZheng-Regular.ttf`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/TitleScreen.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/ProjectSettings/EditorBuildSettings.asset`

Verification:

- Scene-builder command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: `Touhou Unity migration initial project built.`
- Build Settings check:
  - `Bootstrap.unity`, `TitleScreen.unity`, `BambooHomeVerticalSlice.unity`, and `HumanVillageVerticalSlice.unity` are registered.
- Scene content check:
  - `TitleScreen.unity` contains `TitleScreenController`.
  - `BambooHomeVerticalSlice.unity` contains `BambooHomeBlockoutMarker` and `WorldSimulationBehaviour`.
- Foundation test command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.FoundationSmokeTests.RunAll`
  - Result: `Foundation smoke tests passed.`

Known blockers:

- `TitleScreenController` is an immediate-mode first pass. Replace with the planned UI Toolkit shell after core UI presenters are migrated.
- Bamboo Home is still a blockout; its GLB building/prop/foliage assets have not been imported yet.
- Continue, deck editor, and settings buttons are placeholders except for route-to-Bamboo behavior on continue.

Next recommended step:

- Import the minimal Bamboo Home visual assets and replace the blockout marker with source models, then add a simple portal/scene-flow path from Bamboo Home to Human Village.

### M3: Unity-Native Replacement Strategy And World Simulation Adapter

- Date: 2026-06-23 23:05 CST
- Status: Complete
- Owner: Codex
- Goal: Establish how Unity should replace Godot-specific plugins and wire the time/weather foundation into an actual Unity scene adapter.

Completed:

- Logged GitHub CLI in through local proxy `127.0.0.1:10808`; authenticated account: `bladevilR`.
- Confirmed Godot-origin plugin families that need Unity-native treatment: Dialogic, Beehave, PhantomCamera, Terrain3D, ProtonScatter, VRM/MToon, VFX Library, UniParticles3D, SimpleGrassTextured, Unidot Importer, editor/MCP tooling.
- Confirmed original Unity asset source folders under `/Users/Shared/Touhougodot/assets/unity_imports`, including Suntail, PureNature, and AngryMesh packs. These were later relocated in M7 to `/Users/Shared/TouhouUnityMigration/ExternalUnityAssets/unity_imports`.
- Wrote `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`.
- Added tests for Unity-side day-night lighting and world simulation snapshots.
- Verified RED failure first: missing `TouhouMigration.Runtime.Foundation.DayNightLightingController`.
- Implemented `DayNightLightingController` as the replaceable Unity visual adapter for sun, moon, ambient light, and fog.
- Implemented `WorldSimulationBehaviour` to own `GameClock`, `WeatherService`, and `DayNightPalette` in scene runtime.
- Updated the scene builder so `HumanVillageVerticalSlice.unity` now contains `WorldSimulation`, `Sun`, `Moon`, `WorldSimulationBehaviour`, and `DayNightLightingController`.
- Verified GREEN: foundation smoke tests pass.
- Re-ran the Unity scene builder successfully and confirmed the generated scene contains the world simulation objects.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Docs/FoundationArchitecture.md`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/UnityReplacementAndAssetStrategy.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/FoundationSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Foundation/DayNightLightingController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Foundation/WorldSimulationBehaviour.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`

Verification:

- GitHub auth:
  - `gh auth status`
  - Result: logged in to `github.com` as `bladevilR`; scopes include `repo`, `read:org`, and `gist`.
- RED command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.FoundationSmokeTests.RunAll`
  - Expected failure observed: missing `TouhouMigration.Runtime.Foundation.DayNightLightingController`.
- GREEN command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.FoundationSmokeTests.RunAll`
  - Result: `Foundation smoke tests passed.`
- Scene-builder command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: `Touhou Unity migration initial project built.`
- Scene content check:
  - `HumanVillageVerticalSlice.unity` contains `WorldSimulation`, `Sun`, `Moon`, `WorldSimulationBehaviour`, and `DayNightLightingController`.

Architecture decisions:

- Use original Unity source assets from `ExternalUnityAssets/unity_imports` for imported world kits instead of reconstructing those scenes from Godot `.tscn` wrappers.
- Keep day-night/weather simulation as project-owned pure C# services.
- Treat third-party day-night/weather packages as optional visual adapters, not core simulation dependencies.
- Prefer Cinemachine for PhantomCamera replacement, Yarn Spinner or Ink for dialogue authoring, UniVRM/Unity Toon Shader for VRM/MToon, and Unity ParticleSystem/VFX Graph for Godot VFX plugins.
- Do not add behavior-tree or sky/weather packages until a concrete gameplay slice proves the dependency is worth it.

Known blockers:

- M7 moved the source Unity asset folders into `/Users/Shared/TouhouUnityMigration/ExternalUnityAssets`; import into Unity `Assets/` should still happen per playable scene slice.
- GitHub/other external lookups need proxy environment variables: `HTTP_PROXY=http://127.0.0.1:10808`, `HTTPS_PROXY=http://127.0.0.1:10808`, and `ALL_PROXY=socks5://127.0.0.1:10808`.
- Unity licensing still prints a non-blocking batchmode access-token warning, but compile/test/builder commands complete successfully.

Next recommended step:

- Start the first formal playable slice: import/copy the minimal title-screen assets and Bamboo Home/Human Village Unity-source kit candidates, then build `TitleScreen -> BambooHome` scene flow with a proper camera plan.

### M2: Weather And Day-Night Foundation

- Date: 2026-06-23 22:46 CST
- Status: Complete
- Owner: Codex
- Goal: Port the Godot weather, moon-phase, and day-night brightness foundations into deterministic Unity-side services.

Completed:

- Extended foundation smoke tests to cover weather forcing, storm movement modifiers, full-moon activation timing, and Godot day-night brightness anchors.
- Verified RED failure first: missing `TouhouMigration.Runtime.Foundation.WeatherService`.
- Implemented `WeatherService`, `GameWeather`, `MoonPhase`, and `WorldWeatherSnapshot`.
- Implemented `DayNightPalette` and `DayNightLightingProfile`.
- Matched Godot `WeatherSystem.gd` movement/visibility modifiers and full-moon activation rule.
- Matched Godot `DayNightManager.gd` brightness anchors for night and midnight in tests.
- Fixed a reflection ambiguity by giving the enum-based day-night profile lookup an explicit `GetProfileForPeriod` method name.
- Verified GREEN: foundation smoke tests pass.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/FoundationSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Foundation/GameWeather.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Foundation/MoonPhase.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Foundation/WorldWeatherSnapshot.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Foundation/WeatherService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Foundation/DayNightLightingProfile.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Foundation/DayNightPalette.cs`

Verification:

- RED command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.FoundationSmokeTests.RunAll`
  - Expected failure observed: missing `TouhouMigration.Runtime.Foundation.WeatherService`.
- GREEN command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.FoundationSmokeTests.RunAll`
  - Result: `Foundation smoke tests passed.`

Known blockers:

- Unity MCP connection still needs checking against `/Users/Shared/TouhouUnityMigration`; Unity batch automation works.
- Weather is deterministic foundation logic only; no sky, fog, rain, lighting, or terrain-material adapter has been wired into scenes yet.
- `GameClock`, `WeatherService`, and `DayNightPalette` are pure services; scene MonoBehaviours and save/load bindings are still pending.

Next recommended step:

- Wire a `WorldSimulationBehaviour` into the bootstrap scene that advances `GameClock`, updates `WeatherService`, applies `DayNightPalette` to a directional light and ambient settings, and exposes debug UI/state for validation.

### M1: Formal Project Inventory And GameClock Foundation

- Date: 2026-06-23 22:35 CST
- Status: Complete
- Owner: Codex
- Goal: Understand the formal Godot runtime shape and establish the first Unity foundation service.

Completed:

- Inventoried Godot boot contract, formal autoload families, SceneManager runtime scene registry, runtime/test/debug boundaries, player/action systems, UI/dialogue, data/save, NPC/social, life-sim, card-build, VFX/audio, and character animation constraints.
- Wrote Unity migration architecture notes that favor grouped pure C# services plus Unity adapters instead of one-to-one Godot autoload singletons.
- Added TDD smoke tests for `GameClock`.
- Verified RED failure first: missing `TouhouMigration.Runtime.Foundation.GameClock`.
- Implemented pure C# `GameClock`, `GameSeason`, `GameTimePeriod`, and `WorldTimeSnapshot`.
- Verified GREEN: foundation smoke tests pass.
- Re-ran Unity initial project scene builder after adding foundation code; it still passes.

Changed files:

- `/Users/Shared/TouhouUnityMigration/Docs/GodotProjectOverview.md`
- `/Users/Shared/TouhouUnityMigration/Docs/FoundationArchitecture.md`
- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/Tests/FoundationSmokeTests.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Foundation/GameClock.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Foundation/GameSeason.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Foundation/GameTimePeriod.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Foundation/WorldTimeSnapshot.cs`

Verification:

- RED command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.FoundationSmokeTests.RunAll`
  - Expected failure observed: missing `TouhouMigration.Runtime.Foundation.GameClock`.
- GREEN command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.Tests.FoundationSmokeTests.RunAll`
  - Result: `Foundation smoke tests passed.`
- Reverify command:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
  - Result: `Touhou Unity migration initial project built.`

Architecture decisions:

- Treat Godot autoloads as requirements, not a Unity singleton map.
- First foundation is `GameClock` because time/date/season drives day-night visuals, NPC schedules, farming growth, shops, fatigue, weather, fishing availability, and save/load.
- Split Godot `Player3D.gd` into Unity movement, action state machine, animation bridge, VFX bridge, water, interaction, and buff systems later.
- Use Unity humanoid retargeting for characters instead of recreating Godot runtime animation track-copying.

Known blockers:

- Unity MCP connection still needs checking against `/Users/Shared/TouhouUnityMigration`; Unity batch automation works.
- Weather simulation and day-night visual adapters are documented but not implemented yet.

Next recommended step:

- Extend the foundation slice with deterministic `WeatherService` and `DayNightLightingController` adapter, then wire a `GameClockBehaviour` into the bootstrap scene.

### M0: Independent Unity Project Created

- Date: 2026-06-23 22:25 CST
- Status: Complete
- Owner: Codex
- Goal: Create a clean Unity migration workspace outside the Godot repository.

Completed:

- Created Unity project at `/Users/Shared/TouhouUnityMigration`.
- Unity Editor version used: `6000.5.0f1`.
- Confirmed Unity batch project creation completed successfully.
- Added migration folder structure under `Assets/TouhouMigration`.
- Added initial README and migration inventory notes.
- Added initial C# runtime/editor scaffold files.
- Generated initial Unity scenes:
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/Bootstrap.unity`
  - `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`
- Registered both scenes in Unity Build Settings.

Changed files:

- `/Users/Shared/TouhouUnityMigration/README.md`
- `/Users/Shared/TouhouUnityMigration/.gitignore`
- `/Users/Shared/TouhouUnityMigration/Docs/MigrationInventory.md`
- `/Users/Shared/TouhouUnityMigration/Docs/PROJECT_PROGRESS.md`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Data/MigrationSceneId.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Data/MigrationSceneCatalog.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Services/SceneTransitionService.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Bootstrap/TouhouMigrationBootstrap.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Runtime/Player/MigrationPlayerController.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/Bootstrap.unity`
- `/Users/Shared/TouhouUnityMigration/Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity`
- `/Users/Shared/TouhouUnityMigration/ProjectSettings/EditorBuildSettings.asset`

Verification:

- Unity project creation batch command completed successfully.
- Unity batch scene-builder command completed successfully:
  - `Unity -batchmode -quit -projectPath /Users/Shared/TouhouUnityMigration -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject`
- Build Settings now contains `Bootstrap.unity` and `HumanVillageVerticalSlice.unity`.

Known blockers:

- Unity MCP `get_scene_info` previously timed out before this standalone project was created; reconnect still needs checking against the new project.

Next recommended step:

- Inventory the formal Godot systems and scenes before porting gameplay.
- Write the Unity foundation architecture map for time/day-night, UI, actions, animation/skeletons, save/data, and scene services.

Decisions:

- Removed uGUI-specific types from the initial scene builder after Unity 6 compilation reported `UnityEngine.UI` was unavailable to the editor assembly. The bootstrap shell now avoids choosing uGUI vs UI Toolkit until the UI architecture pass.

## Handoff Template

Use this exact shape for future milestone handoffs:

```text
Milestone:
Status:
Date:
Changed files:
Verification run:
Verification result:
Known blockers:
Source assets imported:
Next recommended step:
```
