# Current Handoff

Last updated: 2026-06-25 05:04 CST

## Objective

Build the full Touhou Phantom game in `/Users/Shared/TouhouUnityMigration`. Godot at
`/Users/Shared/Touhougodot` is the gameplay/content reference, not the implementation
shape to copy. Prefer Unity-native prefab, ScriptableObject, physics, animation, camera,
UI, and service boundaries where they serve the same player-facing effect better.

Ownership: Claude is sole owner (Codex stopped 2026-06-25). Standing goal: **complete all
migration**, driven autonomously against the north-star roadmap
`Docs/superpowers/plans/2026-06-25-migration-completion-roadmap.md` (Phase 0 → E1…E8).

## Current State

- Compiles green; full smoke suite **42/42 via `MigrationSmokeTestRunner.RunAll`** (one command).
  Under git, pushed to `git@github.com:bladevilR/touhouunity.git` (`main`).
- **Phase 0 (foundation) COMPLETE**: M58 closed; git + GitHub live; first real play-mode validation
  (scenes runtime-clean); master one-command test runner; save-orchestration logic. Deferred as
  optional: full asmdef/NUnit migration; generated-asset gitignore hygiene.
- **Next: E1 — player/combat execution core.** `MigrationPlayerController` already has
  CharacterController move + jump + cooking-buff-modified speed/dash/jump *queries*, but lacks dash
  *execution*, swim, animation-event attack windows, a general player **i-frame** system (only a local
  snowball cooldown today), Animator integration, and split concerns. E1 sub-milestones: (1) player
  i-frames ✓ → (2) dash execution → (3) Animator/movement integration → (4) animation-event attack
  windows → (5) swim → (6) split Player concerns. **E1.1 i-frames DONE** (opt-in invulnerability window
  in `MigrationPlayerHealthRuntime`: a landed hit blocks further damage until it ticks down; default 0 =
  off, live player sets 0.75; 37/37 regression). Wiring 0.75 into the live player + reconciling the
  snowball's local cooldown is deferred to player integration. **E1.2 dash state machine DONE** (pure
`MigrationDashState`: cooldown + active window). **E1.3 dash wiring DONE** — `MigrationPlayerController`
dashes on LeftControl in move/facing direction (buff-modified cooldown/distance); also fixed jump to use
the cooking jump-boost (`GetModifiedJumpHeight`). Verified by compile gate; dash motion to be confirmed by
play-validation. **E1.4 DONE** — `MigrationGlobalUiController` sets the player health runtime i-frame to
0.75s (ticked each frame); 38/38 regression. Snowball's local 0.75s cooldown left as redundant-but-harmless.
E1 **swim model done** (`MigrationSwimState`: slowed horizontal + buoyant clamped vertical, TDD;
water-volume detection wired during integration). E1 **locomotion params done** (`MigrationLocomotion.Resolve`
→ normalized speed + move/run/grounded/dash flags for an Animator driver, TDD). E1's remaining items — the
**Mecanim humanoid AnimatorController setup** (player model + clips → controller). The live
`MigrationPlayerController` now exposes `CurrentLocomotion` each frame as the driver seam, so what remains
is the AnimatorController asset + a component that pushes those params onto it. Plus animation-event attack
windows and splitting Player concerns — heavier asset/integration work for a fresh pass. **E2.1 game-state mode machine DONE**
(`MigrationGameStateMachine` + `MigrationGameStateMode` Menu/Home/Overworld/Combat/Dialogue/Cutscene/Sleeping,
ChangeMode/Push/Pop, TDD) — game-loop foundation. **Next:** E2 scene-flow driving the mode machine, or E1
Animator integration. Pure-logic milestones (i-frames, dash, save orch, game-state) are TDD-tested; live
integration (controller dash, owner i-frame) is play-validated.

## M58 — Done

- `MigrationPerfectFreezeOutcomePresenter` (runtime component) subscribes to
  `MigrationPerfectFreezeEncounterDirector.PhaseFinished` and shows capture/clear/timeout
  plus bonus/stun summary text via lightweight `TextMesh` children. Grants no rewards and
  owns no settlement.
- `TouhouMigrationProjectBuilder.CreatePerfectFreezeEncounterPrefab(...)` now adds the
  presenter on the encounter root (co-located with the director), so it auto-binds to
  `PhaseFinished` at runtime via its `OnEnable` `GetComponent` resolve.
- `PerfectFreezeEncounterSmokeTests.TestGeneratedPerfectFreezeEncounterPrefabWiresScopedBoss`
  now asserts the generated prefab carries the presenter.

## Verification

- TDD red→green (`PerfectFreezeEncounterSmokeTests.RunAll`):
  - RED before builder wiring: "...outcome presenter co-located..." failed (Expected True, Actual False).
  - GREEN: exit 0, "Perfect Freeze encounter smoke tests passed."
- Adjacent regressions all green (exit 0): `EnemyProjectileSpecialRulesSmokeTests`,
  `EnemyProjectilePerfectFreezeCycleSmokeTests`, `ProjectileSettlementStaggerSmokeTests`,
  `CombatBridgeSmokeTests`.

## Phase 0.3 — Foundation Debts

- DONE — In-editor play-mode validation: the Cycle A validator runs end-to-end; all 4
  current scenes (Bootstrap, BambooHome, HumanVillage, TitleScreen) enter Play and run with
  **zero game-runtime errors** after filtering Unity QuickSearch editor-tooling noise
  (`MigrationPlayModeReport.IsRuntimeFailure`). First real play validation; scenes are
  runtime-clean.
  - Finding A (validator capture gap): `camera.Render()` does not capture Screen-Space-Overlay
    UI, so UI-only scenes (TitleScreen, Bootstrap) capture black. Not a game error; improve
    capture (ScreenCapture / end-of-frame) when UI-scene visual proof matters.
  - Finding B (scene): HumanVillage camera framing is poor (content clusters at top of frame);
    scene/camera-setup polish belongs to E2/E3.
- DONE — Master one-command smoke-test runner: `MigrationSmokeTestRunner.RunAll` discovers and
  runs every `*SmokeTests` suite in one Unity launch. First full-suite baseline: **35/35 suites
  pass, 0 compile errors**. (Full asmdef restructure + NUnit Test Runner migration deferred as
  optional polish — the bespoke smoke pattern works and the aggregator gives one-command regression.)
- DONE (logic) — Save orchestration: `MigrationSaveOrchestrator.Capture/Apply` bridges the 5
  gameplay services (Inventory/Cooking/CookingBuff/SocialBond/QuestDelivery) and `MigrationSaveData`;
  bond state round-trips through capture→apply (TDD). Pending: hook it into a runtime global save
  owner + player scalar fields (fold into E2/E7 save UI).
- NEXT — Generated-asset/build-determinism hygiene (BuildInitialProject churns ~44 assets;
  gitignore builder-generated assets).
Then proceed to E1 (player/combat execution core) per the roadmap.

## Hazards

- Verify compile-green before every Unity batch; run only one Unity batch/editor command at a time (no parallel).
- Generated prefabs/scenes are regenerated by `TouhouMigrationProjectBuilder` — author them in the builder, not by hand-editing generated assets.
- git is live; commit per milestone, never force-push shared history.
- Keep the Godot source tree (`/Users/Shared/Touhougodot`) read-only unless explicitly exporting/reading source data.
