# Current Handoff — Resume Point

Last updated: 2026-06-25 (session 2)

## Objective & Ownership

Build the full Touhou Phantom game in `/Users/Shared/TouhouUnityMigration`, migrating from Godot
(`/Users/Shared/Touhougodot`, read-only reference). Claude is sole owner (Codex stopped 2026-06-25).
Standing goal: **complete all migration**, driven autonomously, user reviews final results only.

- North-star roadmap: `Docs/superpowers/plans/2026-06-25-migration-completion-roadmap.md` (Phase 0 → E1…E8; all locations incl. PureNature/AngryMesh variants are in scope).
- GitHub: `git@github.com:bladevilR/touhouunity.git` (`main`). Session-1 handoff commit: `92d3046`; the session-2 dialogue-humanity-routing commit lands on top of this update.

## Verified State

- Tree clean, `main` in sync with origin. ~121 runtime C# files; **44 smoke-test suites**.
- Last full regression: **44/44 green** (`MigrationSmokeTestRunner.RunAll`, session 2) — confirmed the session-1 43/43 baseline, then added the E5.2 dialogue-humanity suite (44th). 0 compile errors.
- 4 scenes (Bootstrap/BambooHome/HumanVillage/TitleScreen) **play-validated runtime-clean** via `MigrationPlayModeValidator` (note: it can't capture Screen-Space-Overlay UI, so UI scenes screenshot black — a capture limitation, not a game error).
- Completion: **~13% by roadmap structure.** This is a multi-session, weeks-scale effort.

## Done (session 1)

- **Phase 0 (complete):** M58 closed; git + GitHub live; first in-editor play-mode validation; master one-command test runner (`MigrationSmokeTestRunner`); save orchestration (`MigrationSaveOrchestrator`).
- **E1 player core:** i-frames (`MigrationPlayerHealthRuntime` opt-in window + owner sets 0.75s); dash (`MigrationDashState` + controller wiring, LeftControl; also fixed jump to use cooking boost); swim (`MigrationSwimState`); locomotion (`MigrationLocomotion` + controller `CurrentLocomotion` seam). **Remaining:** Mecanim humanoid AnimatorController asset + a driver pushing `CurrentLocomotion`; animation-event attack windows; split the controller's concerns.
- **E2:** `MigrationGameStateMachine` + `MigrationGameStateMode` + `MigrationGameStateRules`; owner Pushes `Dialogue` mode on dialogue start / Pops on finish (`MigrationGlobalUiController.GameState`). **Remaining:** drive Combat/Sleeping/Menu modes; use the rules to gate input/HUD/world-time; scene-flow + full day loop.
- **E5.1/E5.2:** dialogue give/take-item routing (`DialogueEffectRouter.BindInventory`, wired in owner). **`humanity` fx now routed** (E5.2, session 2) via `HumanityService` (`Runtime/Player/`, default 100, clamped 0..100, Godot `MokouMonologueSystem` level thresholds), bound in owner + read live in `BuildDialogueContext`. **bond + humanity both routed now.** The give_item capability is added but the current data may not use it.
- **E4/E8:** save orchestrator wired into owner `SaveGame(slot)/LoadGame(slot)` (5 service snapshots + HP). **Remaining:** coins/scene/position scalars + a save-UI trigger.

## How To Work (established rhythm)

- **One Unity batch at a time** (no parallel); verify compile-green before batches.
- Unity binary: `/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity`.
- Full regression (one command): `... -batchmode -quit -executeMethod TouhouMigration.Editor.Tests.MigrationSmokeTestRunner.RunAll`.
- Per milestone: prefer pure-logic TDD (focused smoke test, red→green); for owner/controller (MonoBehaviour) changes, gate via `GlobalUiSmokeTests` + a full regression; then **commit** (end message with `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`) + **push**.
- **Builder churn:** tests that call `TouhouMigrationProjectBuilder.BuildInitialProject` regenerate ~44 generated assets (enemy prefabs/controllers, scenes). Revert before committing so commits stay focused: `git checkout -- Assets/TouhouMigration/Animations/Enemies Assets/TouhouMigration/Prefabs Assets/TouhouMigration/Scenes`. (Roadmap Phase 0.3 lists gitignoring generated assets as a deferred hygiene fix.)
- Generated prefabs/scenes are owned by `TouhouMigrationProjectBuilder` — author them in the builder, never hand-edit generated assets. Keep the Godot source tree read-only.

## Prioritized Next (heavy integration/content — best in fresh context)

1. **E1.5:** player Mecanim AnimatorController (model + MokouValidation clips → controller) + a driver consuming `CurrentLocomotion`; then animation-event attack windows.
2. **E2:** Combat/Sleeping/Menu mode drivers + use `MigrationGameStateRules` to gate input/HUD/time; scene-flow + full day loop (time/calendar/weather/day-night already have foundations).
3. **E3:** the ~20 formal locations (incl. all PureNature/AngryMesh variants), asset-promoted from `ExternalUnityAssets/unity_imports`; each play-validated.
4. **E4:** life-sim closure — shop/economy, cooking UI/timer, farming, fishing, quest/NPC schedule/bond loops to playable.
5. **E5/E6/E7/E8:** full dialogue (portraits, all 35 NPCs, shop) → combat breadth (general arena, 20-enemy AI, cardbuild loop, more bosses) → presentation (URP/audio/VFX/UI) → save parity + content audit + full play-validation.

*(Done in session 2 — E5.2: dialogue `humanity` fx routed to `HumanityService`. The former #1 "E5 humanity quick win" is complete.)*
