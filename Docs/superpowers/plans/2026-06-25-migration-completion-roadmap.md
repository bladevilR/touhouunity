# Touhou Unity Migration — Completion Roadmap

- Date: 2026-06-25
- Owner: Claude (sole owner; Codex stopped 2026-06-25)
- Purpose: north-star program plan for the long "complete all migration" goal task. Epic-level; each epic runs its own brainstorm → spec → plan → TDD-implement → play-validate → record-milestone cycle.
- Authoritative state docs: `Docs/CURRENT_HANDOFF.md`, `Docs/PROJECT_PROGRESS.md`, `Docs/MigrationInventory.md`, `Docs/GodotProjectOverview.md`. Godot source of truth for "is this in the game?": `project.godot` → autoloads → `core/autoloads/SceneManager.gd` → scene `ext_resource` refs → `docs/SYSTEM_CATALOG.md`.

## 1. Definition of "Migration Complete"

The formal Touhou Phantom game is playable end-to-end in Unity, Unity-native (not a Godot-shape copy):

- Boot → Title → life-sim loop (home/overworld, time/calendar/weather/day-night) → dialogue/social → combat/bosses → save/load, across the **formal** registered locations.
- All formal content parity: 35 NPCs, 200 items, 40 recipes, 15 quests, 30 gifts, 20 enemies, cardbuild deck/run loop, formal bosses.
- **Every registered location ships — including all PureNature/AngryMesh overworld variants. No location is optional** (user decision, 2026-06-25).
- Every formal scene passes in-editor **play-mode** validation (runs, renders, zero runtime errors), not just batch smoke tests.
- Dev/test/debug-only Godot scenes are explicitly out of scope (`scenes/test|tests|debug|tools`, `scripts/test|tests|debug|dev|tools`).

## 2. Current State (grounded)

- Compiles **green**. M57 complete; **M58 half-done** (`MigrationPerfectFreezeOutcomePresenter` component + test are green, but not wired into the generated encounter prefab, docs not updated, regressions not re-run — see `CURRENT_HANDOFF.md` §"Not Done Yet").
- 113 runtime C# files / ~18.7k LOC, 35 bespoke smoke-test files (no Unity Test Runner, no master runner).
- **Lopsided**: M40–M57 (18 milestones) all went into one boss (Cirno Perfect Freeze). Most life-sim/RPG systems are "first slice / foundation only" with explicit deferred work; save-orchestration is repeatedly "not wired"; only Bamboo Home + Human Village + Title/Bootstrap exist as scenes (vs ~20 registered Godot locations).
- **Not a git repo.** Single Assembly-CSharp (no asmdef). Generated prefabs/scenes are owned by `TouhouMigrationProjectBuilder` (do not hand-edit as source of truth).

## 3. Phase 0 — Stabilize the baseline (do first, in order)

0.1 **Finish M58 — DONE (2026-06-25).** `MigrationPerfectFreezeOutcomePresenter` wired into the generated encounter prefab (co-located with the director, auto-binds to `PhaseFinished`); TDD red→green + 4 adjacent regressions green; docs updated. Milestone closed.

0.2 **git — DONE (2026-06-25).** Repo is initialized and pushed: `origin = git@github.com:bladevilR/touhouunity.git`, branch `main` in sync (commit `1f8c04d`, 2551 files, `Library/Temp/Logs/UserSettings` excluded). GitHub management is live. Commit per milestone going forward; never force-push shared history.

0.3 **Foundation debts** (unblock everything downstream):
- Cycle A play-mode validator — DONE: all 4 current scenes run in Play with **zero game-runtime errors** (validator now filters editor-tooling noise via `MigrationPlayModeReport.IsRuntimeFailure`). Follow-ups: (a) validator can't capture Screen-Space-Overlay UI, so UI scenes capture black — improve with ScreenCapture/end-of-frame; (b) HumanVillage camera framing needs scene polish (E2/E3).
- Assembly definitions (Runtime / Editor / Tests asmdefs) + migrate bespoke smoke tests onto **Unity Test Runner** + a **master "run all tests"** entry (today 35 tests run one-by-one). This is the old "Cycle B" and it pays for itself across every later epic (independent compiles, CI-able, one-command regression).
- Wire the repeatedly-deferred **save orchestration** so cooking/buffs/quest/bond/inventory snapshots are actually persisted by one runtime owner.
- **Generated-asset / build determinism** (found at M58): `BuildInitialProject` re-serializes ~44 unrelated generated assets (19 enemy AnimatorControllers, 20 enemy prefabs, 5 scenes) on every run, churning git even when no code changed. Either make the build deterministic or gitignore builder-generated assets (prefabs/controllers/scenes/generated materials/phase-plan) and regenerate on setup — while keeping imported source (FBX/GLB/PNG/TTF) tracked. Until fixed, each milestone commit includes only the assets it genuinely changed.

## 4. Epics (ordered; each is its own spec → plan → implement cycle)

**E1 — Player & combat execution core.** Split Godot `Player3D.gd` (movement/dash/jump/swim/attack/VFX/buffs/animation/interaction) into Unity-native components; real input + animation-event attack windows (replace timer-driven windows); player i-frame system (currently only a local snowball cooldown); full health/death/rebirth. *Done when:* Mokou is fully controllable with real combat in a scene, play-validated.

**E2 — Game-state & life-sim loop.** `GameStateManager` modes (MENU/HOME/OVERWORLD/COMBAT/DIALOGUE/CUTSCENE/SLEEPING); `SceneManager` parity (spawn points, fades, combat enter/exit, sync-load exceptions); drive the existing time/calendar/weather/day-night foundations into real scene behavior + sleep-to-advance-day. *Done when:* a full day cycle and scene-to-scene flow work end-to-end.

**E3 — Locations.** Rebuild the formal registered scenes as Unity-native, asset-promoted from `ExternalUnityAssets/unity_imports` and Godot sources: Suntail/Human Village (extend), Bamboo Home (extend), Hakurei Shrine, Magic Forest, Misty Lake, Scarlet Mansion front, Farm, Dungeon Entrance, Town World, plus **all** PureNature/AngryMesh overworld variants. *Main-path locations come first for ordering only — every registered location is mandatory and ships (no variant is optional).* *Done when:* each location loads, renders, and is play-validated.

**E4 — Life-sim systems to closed loops.** Take the foundations to playable closure + production UI: inventory/equipment, shop economy (`ShopManager`/`ShopData`), cooking full loop+timer UI, farming (`CropDatabase`/farm), fishing (`FishingManager`/`FishDatabase`), quests full loop, NPC schedules/`NPCManager`, bond/`BondEventSystem`/companion/memory/relationship, gifts production UI. *Done when:* each system is usable in-game, not just service-level.

**E5 — Dialogue completeness.** Route remaining `fx`/actions (Inventory/Shop, entry/line-level) not just Bond/Quest; portrait catalog; all 35 NPCs reachable; Dialogic-equivalent flows. *Done when:* every formal NPC conversation runs with effects + portraits.

**E6 — Combat breadth & meta.** General combat arena (`CombatArenaHD2D`); all 20 enemies as real AI (NavMesh / behavior-tree parity, not placeholder); CardBuild full schema-v2 (slot/activation modes, Mokou overrides, boss clauses, cooldowns, run/deck loop + deck editor production UI); formal bosses beyond Cirno. *Done when:* a combat session can be entered, fought with deck rules, and settled.

**E7 — Presentation & polish.** Settings → URP pipeline/quality/post mapping; audio (`AudioManager` BGM/SFX/fades); VFX (`VFX3D`/`VFXPool`); camera; full production UI screens (HUD/menu/notifications/`GlobalUIManager` parity). *Done when:* the game looks/sounds like the formal product, not a shell.

**E8 — Save/content parity & final QA.** Complete `SaveSystem` schema parity (calendar/fatigue/NPC/companion/home/fishing/buffs); content audit vs Godot databases; full play-validation sweep across all formal scenes. *Done when:* save/load round-trips a full session and every formal scene is green.

## 5. Sequencing & dependencies

Phase 0 → E1 → E2 → (E3 ∥ E4 ∥ E5 can interleave once E2 scene-flow exists) → E6 → E7 → E8. E1/E2 are prerequisites (player + loop) for meaningful play-validation of everything else. E7/E8 are finishing passes. Within each epic keep the project's rule: **small playable slices over broad incomplete conversion**.

## 6. Working method (for the goal task)

- **Autonomy (user decision, 2026-06-25): drive the whole roadmap autonomously to completion. No mid-way check-ins or status questions — the user reviews the final result only.** The audit trail the user inspects is git history + `PROJECT_PROGRESS.md`/`CURRENT_HANDOFF.md`; keep both current so progress is reconstructable without live reporting. Only stop for a true hard blocker that cannot be resolved by judgment, the source, or sensible defaults.
- Per epic/milestone: brainstorm → spec (`Docs/superpowers/specs/`) → plan (`Docs/superpowers/plans/`) → **TDD** (red → green via the existing bespoke smoke pattern, or Unity Test Runner after Phase 0.3) → in-editor play-validate → update `PROJECT_PROGRESS.md` + `MigrationInventory.md` + `CURRENT_HANDOFF.md` → commit (after git).
- Treat Godot as content/intent reference; **keep `/Users/Shared/Touhougodot` read-only** unless explicitly exporting source data. Reuse `ExternalUnityAssets/unity_imports` kits.
- Prefer Unity-native (ScriptableObject/prefab/Mecanim-humanoid/NavMesh/Input/URP) over Godot shapes; split god-objects like `Player3D`.

## 7. Hazards (from CURRENT_HANDOFF + this session)

- **Verify compile-green before every Unity batch**; one Unity batch at a time (no parallel editor/batch runs).
- Generated prefabs/scenes are regenerated by `TouhouMigrationProjectBuilder` — author them in the builder, not by hand-editing generated assets.
- git is live (`main` ↔ `github.com:bladevilR/touhouunity`); commit per milestone, never force-push shared history.
- Play-mode batch can hang; the Cycle A validator already includes a watchdog.

## 8. Locked decisions (user, 2026-06-25)

1. **Sequence**: Phase 0 → E1 → E2 → (E3/E4/E5 interleave) → E6 → E7 → E8, as in §5. (Owner's call, accepted.)
2. **Location scope**: **all** registered locations are mandatory, including every PureNature/AngryMesh variant. Nothing optional.
3. **Autonomy**: full autonomy, no mid-way reporting; user reviews the final result only (see §6).
4. **git**: authorized — `git init` at the green baseline in Phase 0.2.
5. **Execution start**: hold until the user opens the goal task. Do not execute the roadmap before then.
