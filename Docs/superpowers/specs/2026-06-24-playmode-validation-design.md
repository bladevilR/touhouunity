# Cycle A — In-Editor Play-Mode Validation & Capture (Design)

- Date: 2026-06-24
- Status: Approved (design); pending implementation plan
- Cycle: A of A→B→C (validation → asmdef/test-runner debt → feature completion)
- Owner spec for: filling the documented gap "manual in-editor play validation has not yet been performed"

## Problem

All verification to date is batch-build + bespoke smoke tests. The only visual
artifacts are two edit-mode renders produced by `MigrationSceneCapture`, which
opens a scene and renders `Camera.main` for a single frame **without entering
Play mode**. Nothing proves the runtime actually executes: `Awake`/`Start`,
service initialization, bootstrap scene transition, player/enemy runtime, and
HUD population have never been observed running. `Verification/VisualChecks`
holds only `HumanVillage_M8.png` and `MokouCharacter_M9.png`, both static.

## Goal / Definition of Done

For four target scenes, prove the game runs in real Play mode:

1. Each scene enters Play mode and runs for a configurable duration (default 3s).
2. Each produces a live in-play screenshot under `Verification/VisualChecks/`.
3. The run asserts **zero** `Error`/`Exception`/`Assert` log messages during play.
4. A summary report is written; any error makes the batch exit non-zero.

## Scope

In scope:

- New editor batch validator covering: `Bootstrap` (entry flow), `BambooHomeVerticalSlice`, `HumanVillageVerticalSlice`, `TitleScreen`.
- Live screenshot capture during a Play frame.
- Runtime log-error collection and a written validation report.

Out of scope (deferred):

- Synthetic input injection / driving gameplay (the higher-fidelity option; deferred per YAGNI for a first validation pass).
- `MokouCharacterValidation` scene (validation-only asset, not a playable slice).
- Migrating this into Unity Test Runner PlayMode tests — intentionally deferred to Cycle B, which establishes the test-runner foundation. Keeping A independent of B avoids an ordering inversion.

## Approach

Chosen: a self-contained editor batch validator that drives Play mode via an
`EditorApplication.update` pump, persisting a scene cursor in `SessionState` so
it survives the domain reload triggered when entering Play mode. This is the
standard recipe for scripted Play mode in batch and keeps A fully independent of
the Cycle B test infrastructure.

Alternatives considered and rejected:

- **Unity Test Runner PlayMode tests** — cleanest assertions/artifacts, but depends on the test-runner/asmdef foundation that Cycle B builds. Doing it in A (before B) inverts the agreed order.
- **Disable domain reload + editor coroutine** — simpler code, but flips a global project setting (`EnterPlayModeOptions`) and is less standard than the `SessionState` pump.

## Detailed Design

New file: `Assets/TouhouMigration/Scripts/Editor/MigrationPlayModeValidator.cs`

Batch entry point: `TouhouMigration.Editor.MigrationPlayModeValidator.ValidateAndCapture`
(invoked via `Unity -batchmode -projectPath ... -executeMethod ...`, **without** `-nographics` so rendering works).

Components:

1. **Scene queue** — ordered list of the four target scene paths. Current
   progress index persisted in `SessionState` (cleared on editor quit) to
   survive the enter-Play-mode domain reload.
2. **Error sink** — subscribe to `Application.logMessageReceived`; record every
   `LogType.Error`, `LogType.Exception`, and `LogType.Assert` (message + first
   stack line) for the duration of each scene's play session.
3. **Per-scene flow**:
   - `EditorSceneManager.OpenScene(path)`.
   - `EditorApplication.EnterPlaymode()`.
   - Pump frames via `EditorApplication.update` until the configured duration
     elapses (default 3s, surfaced as a constant for easy tuning).
   - Capture the active main camera to a 1280x720 `RenderTexture` during a play
     frame and write `Verification/VisualChecks/<Scene>_PlayMode.png` (reusing
     the proven render-to-PNG path from `MigrationSceneCapture`).
   - `EditorApplication.ExitPlaymode()`, advance the cursor, continue.
   - **Entry-flow special case**: starting from `Bootstrap`, let it perform its
     own scene transition; capture whatever it lands on (validates the
     bootstrap → title transition rather than a static bootstrap frame).
4. **Report + exit code** — after the last scene, write
   `Verification/VisualChecks/PlayModeValidationReport.md`: per-scene rows for
   entered-play (yes/no), screenshot path, error count, and first few error
   messages. If any scene recorded errors or failed to enter Play mode, call
   `EditorApplication.Exit(<non-zero>)` so the batch run's status reflects pass/fail.

## Risks / Known Points

- Batch must run **without `-nographics`** (rendering required). Prior runs in
  this project produced non-trivial PNGs, confirming the machine renders in batch.
- A scene may lack a `Main Camera` at the moment of capture (e.g. `Bootstrap`
  before/after transition). Capture must resolve the active camera after the
  transition settles; fall back gracefully (record "no camera" rather than throw).
- The `SessionState` cursor across domain reload is the one piece of indirection
  in this approach; it is the source of most potential flakiness and deserves
  careful sequencing in the plan.

## Tuning Knobs (approved defaults)

- Per-scene play duration: **3 seconds**.
- Scene list: **Bootstrap, BambooHomeVerticalSlice, HumanVillageVerticalSlice, TitleScreen** (Mokou validation scene excluded).
- Hard standard: **any runtime Error/Exception ⇒ non-zero batch exit**.
