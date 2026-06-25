# Current Handoff — Resume Point

Last updated: 2026-06-25 (session 2)

## Objective & Ownership

Build the full Touhou Phantom game in `/Users/Shared/TouhouUnityMigration`, migrating from Godot
(`/Users/Shared/Touhougodot`, read-only reference). Claude is sole owner (Codex stopped 2026-06-25).
Standing goal: **complete all migration**, driven autonomously, user reviews final results only.

- North-star roadmap: `Docs/superpowers/plans/2026-06-25-migration-completion-roadmap.md` (Phase 0 → E1…E8; all locations incl. PureNature/AngryMesh variants are in scope).
- GitHub: `git@github.com:bladevilR/touhouunity.git` (`main`). Latest commit: `619c8ba` (session-2 E2.5 menu mode); the E4.2 farm-plot commit lands on top of this update.

## Verified State

- Tree clean, `main` in sync with origin. ~124 runtime C# files; **46 smoke-test suites**.
- Last full regression: **46/46 green** (`MigrationSmokeTestRunner.RunAll`, session 2) — through the E4.2 farm-plot slice; 0 compile errors. (Session-2 added suites: E5.2 dialogue-humanity, E4.1 shop-service, E4.2 farm-plot (44th–46th); E2.4/E8.1 extended `GameStateRulesSmokeTests`/`SaveOrchestratorSmokeTests`; E2.5 is owner-only wiring gated by `GlobalUiSmokeTests`.)
- 4 scenes (Bootstrap/BambooHome/HumanVillage/TitleScreen) **play-validated runtime-clean** via `MigrationPlayModeValidator` (note: it can't capture Screen-Space-Overlay UI, so UI scenes screenshot black — a capture limitation, not a game error).
- Completion: **~13% by roadmap structure.** This is a multi-session, weeks-scale effort.

## Done (session 1)

- **Phase 0 (complete):** M58 closed; git + GitHub live; first in-editor play-mode validation; master one-command test runner (`MigrationSmokeTestRunner`); save orchestration (`MigrationSaveOrchestrator`).
- **E1 player core:** i-frames (`MigrationPlayerHealthRuntime` opt-in window + owner sets 0.75s); dash (`MigrationDashState` + controller wiring, LeftControl; also fixed jump to use cooking boost); swim (`MigrationSwimState`); locomotion (`MigrationLocomotion` + controller `CurrentLocomotion` seam). **Remaining:** Mecanim humanoid AnimatorController asset + a driver pushing `CurrentLocomotion`; animation-event attack windows; split the controller's concerns. **E1.5 blocker found (session 2):** the MokouValidation clips are already Humanoid (`animationType: 3`), but `Art/Characters/Mokou/Models/mokou.glb` imports via the glTF ScriptedImporter (glTFast) → **no Mecanim Humanoid avatar**, so the humanoid clips can't retarget onto Mokou. Unblock first — give the model a Humanoid avatar (convert `mokou.glb`→FBX, or source a humanoid-rigged Mokou FBX) — then author the controller + driver. Driver logic is otherwise ready: `MigrationLocomotion.Resolve` already yields Animator-facing params.
- **E2:** `MigrationGameStateMachine` + `MigrationGameStateMode` + `MigrationGameStateRules`; owner Pushes `Dialogue` mode on dialogue start / Pops on finish (`MigrationGlobalUiController.GameState`). **World-time now gated by mode (E2, session 2):** `MigrationGameStateRules.WorldTimeScale(mode)` (0 freezes Menu/Dialogue/Cutscene, Sleeping fast-forwards ×12, else ×1) + `WorldSimulationBehaviour.SetExternalTimeScale`; the owner applies it on `gameState.ModeChanged`, so the existing Dialogue Push/Pop now actually freezes/resumes the world clock. **Menu mode now driven too (E2.5):** `SyncMenuGameState()` pushes/pops `Menu` from `unifiedMenuController.IsOpen`, so opening the pause menu freezes world-time + marks the HUD hidden via the rules. **Remaining:** drive Combat/Sleeping push/pop from gameplay (combat enter/exit, sleep); have HUD/input actually consume the rules (`BlocksGameplayInput` still reads overlay state directly); scene-flow + full sleep day-loop.
- **E5.1/E5.2:** dialogue give/take-item routing (`DialogueEffectRouter.BindInventory`, wired in owner). **`humanity` fx now routed** (E5.2, session 2) via `HumanityService` (`Runtime/Player/`, default 100, clamped 0..100, Godot `MokouMonologueSystem` level thresholds), bound in owner + read live in `BuildDialogueContext`. **bond + humanity both routed now.** The give_item capability is added but the current data may not use it.
- **E4/E8:** save orchestrator wired into owner `SaveGame(slot)/LoadGame(slot)` (5 service snapshots + HP; **humanity now persisted too — E8.1, session 2** via `MigrationSaveOrchestrator` 6th param + `MigrationSaveData.humanity`, round-trip TDD-verified). **Shop economy service added (E4.1, session 2):** `MigrationShopService` buy/sell against coins+inventory+price (`Runtime/Economy/`, `DefaultBuyRate 0.5`); pure service, not yet owner-wired. **Farm plot crop growth added (E4.2, session 2):** `MigrationFarmPlot` plant/water/`AdvanceDay`/harvest (`Runtime/Farming/`), pure deterministic growth core. **Remaining:** shop catalogs/hours/seasonal items + shop UI; per-shop buy_rate; farm yield/quality + inventory payout, `FarmingManager` + crops.json + day-loop integration; coins/scene/position save scalars + a save-UI trigger.

## How To Work (established rhythm)

- **One Unity batch at a time** (no parallel); verify compile-green before batches.
- Unity binary: `/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity`.
- Full regression (one command): `... -batchmode -quit -executeMethod TouhouMigration.Editor.Tests.MigrationSmokeTestRunner.RunAll`.
- Per milestone: prefer pure-logic TDD (focused smoke test, red→green); for owner/controller (MonoBehaviour) changes, gate via `GlobalUiSmokeTests` + a full regression; then **commit** (end message with `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`) + **push**.
- **Builder churn:** tests that call `TouhouMigrationProjectBuilder.BuildInitialProject` regenerate ~44 generated assets (enemy prefabs/controllers, scenes). Revert before committing so commits stay focused: `git checkout -- Assets/TouhouMigration/Animations/Enemies Assets/TouhouMigration/Prefabs Assets/TouhouMigration/Scenes`. (Roadmap Phase 0.3 lists gitignoring generated assets as a deferred hygiene fix.)
- Generated prefabs/scenes are owned by `TouhouMigrationProjectBuilder` — author them in the builder, never hand-edit generated assets. Keep the Godot source tree read-only.

## Prioritized Next (heavy integration/content — best in fresh context)

1. **E1.5 (asset-blocked — see Done §E1):** player Mecanim AnimatorController + `CurrentLocomotion` driver + animation-event attack windows. **Unblock first:** give `mokou.glb` a Humanoid avatar (convert→FBX or source a rigged Mokou FBX); the clips are already Humanoid. Best in fresh context once the asset decision is resolved.
2. **E2:** Combat/Sleeping mode push/pop drivers (Menu + world-time gating done session 2) + have HUD/input actually consume `MigrationGameStateRules`; scene-flow + full sleep day-loop (time/calendar/weather/day-night already have foundations).
3. **E3:** the ~20 formal locations (incl. all PureNature/AngryMesh variants), asset-promoted from `ExternalUnityAssets/unity_imports`; each play-validated.
4. **E4:** life-sim closure — shop economy (E4.1 buy/sell done) + catalogs/UI, cooking UI/timer, farming (E4.2 plot growth done) + yield/manager/day-loop, fishing, quest/NPC schedule/bond loops to playable.
5. **E5/E6/E7/E8:** full dialogue (portraits, all 35 NPCs, shop) → combat breadth (general arena, 20-enemy AI, cardbuild loop, more bosses) → presentation (URP/audio/VFX/UI) → save parity + content audit + full play-validation.

*(Done in session 2 — E5.2: dialogue `humanity` fx → `HumanityService`. E2.4: world-time gated by game-state mode (Dialogue/Menu/Cutscene freeze the clock, Sleeping fast-forwards). E4.1: `MigrationShopService` buy/sell economy. E8.1: humanity now persists through save/load. E2.5: opening the pause menu freezes world-time + hides HUD. E4.2: farm plot crop growth (plant/water/grow/harvest). E1.5 diagnosed as asset-blocked on the Mokou humanoid avatar.)*
