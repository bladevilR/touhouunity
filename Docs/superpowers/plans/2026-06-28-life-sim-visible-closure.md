# Life-Sim Visible Closure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the migrated life-sim systems player-visible and repeatable in formal Unity scenes, with screenshot-based PlayMode validation.

**Architecture:** Keep core rules UnityEngine-free and tested with existing editor smoke tests. Add small scene-facing MonoBehaviours and IMGUI panels bound through `MigrationGlobalUiController`, matching existing shop, cooking, bed, farm, and fishing patterns. Scene placement is done through builder/editor repair methods, not manual scene source-of-truth edits.

**Tech Stack:** Unity C#, editor smoke-test menu methods, IMGUI runtime panels, existing `MigrationSmokeTestRunner`, `MigrationPlayModeValidator`, and `Verification/VisualChecks` screenshots.

---

### Task 1: Quest Board Visible Loop

**Files:**
- Modify: `Assets/TouhouMigration/Scripts/Runtime/Quest/MigrationQuestBoard.cs`
- Create: `Assets/TouhouMigration/Scripts/Runtime/Quest/MigrationQuestBoardInteractor.cs`
- Create: `Assets/TouhouMigration/Scripts/Runtime/UI/MigrationQuestBoardController.cs`
- Modify: `Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- Modify: `Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- Modify: `Assets/TouhouMigration/Scripts/Editor/Tests/QuestBoardSmokeTests.cs`
- Modify: `Assets/TouhouMigration/Scripts/Editor/Tests/ScenePlayabilitySmokeTests.cs`

- [ ] **Step 1: Write failing tests**

Add tests proving `MigrationQuestBoard` can start a daily quest through `QuestDeliveryService` and that formal village scenes contain a quest-board interactor.

- [ ] **Step 2: Run red verification**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.0.57f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath . -executeMethod TouhouMigration.Editor.Tests.QuestBoardSmokeTests.RunAll -logFile Logs/CodexLifeSimQuestBoard_red.log
```

Expected: FAIL because board acceptance is not wired into quest delivery or scene interactors are absent.

- [ ] **Step 3: Implement controller and interactor**

Implement a board panel that shows daily quests, accepts one quest through `QuestDeliveryService`, opens the journal after acceptance, plays `ui_open`/feedback SFX where available, and blocks gameplay input while open.

- [ ] **Step 4: Place quest boards**

Update the builder to place visible board props and `MigrationQuestBoardInteractor` in TownWorld, FantasyVillage, and HumanVillageVerticalSlice.

- [ ] **Step 5: Run green verification**

Run focused quest-board tests and the full smoke runner.

### Task 2: Home Storage Visible Loop

**Files:**
- Modify: `Assets/TouhouMigration/Scripts/Runtime/Home/MigrationHomeStorage.cs`
- Create: `Assets/TouhouMigration/Scripts/Runtime/Home/MigrationHomeStorageInteractor.cs`
- Create: `Assets/TouhouMigration/Scripts/Runtime/UI/MigrationHomeStorageController.cs`
- Modify: `Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- Modify: `Assets/TouhouMigration/Scripts/Editor/TouhouMigrationProjectBuilder.cs`
- Modify: `Assets/TouhouMigration/Scripts/Editor/Tests/MigrationHomeStorageSmokeTests.cs`
- Modify: `Assets/TouhouMigration/Scripts/Editor/Tests/ScenePlayabilitySmokeTests.cs`

- [ ] **Step 1: Write failing tests**

Add tests for inventory-to-storage transfer, storage-to-inventory transfer, capacity failure, full-inventory failure, and required home-scene interactor placement.

- [ ] **Step 2: Run red verification**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.0.57f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath . -executeMethod TouhouMigration.Editor.Tests.MigrationHomeStorageSmokeTests.RunAll -logFile Logs/CodexLifeSimHomeStorage_red.log
```

Expected: FAIL because transfer helpers and scene-facing storage UI are absent.

- [ ] **Step 3: Implement controller and interactor**

Add a chest interactor and storage panel with inventory and storage sections, one-unit store/retrieve buttons, visible failure messages, Escape close, and input blocking.

- [ ] **Step 4: Place storage chests**

Update the builder to place storage chests in BambooHomeVerticalSlice, BambooHouse, and MokouHouse3D.

- [ ] **Step 5: Run green verification**

Run focused storage tests and full smoke runner.

### Task 3: Fishing Minigame Scene Integration

**Files:**
- Modify: `Assets/TouhouMigration/Scripts/Runtime/Fishing/MigrationFishingSpotInteractor.cs`
- Create: `Assets/TouhouMigration/Scripts/Runtime/UI/MigrationFishingMinigameController.cs`
- Modify: `Assets/TouhouMigration/Scripts/Runtime/UI/MigrationGlobalUiController.cs`
- Modify: `Assets/TouhouMigration/Scripts/Editor/Tests/FishingSessionSmokeTests.cs`
- Modify: `Assets/TouhouMigration/Scripts/Editor/Tests/FishingSceneSmokeTests.cs`

- [ ] **Step 1: Write failing tests**

Add tests proving formal fishing spots start a `MigrationFishingSession` instead of directly granting fish, and that deterministic reel success grants a catch while failure does not.

- [ ] **Step 2: Run red verification**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.0.57f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath . -executeMethod TouhouMigration.Editor.Tests.FishingSessionSmokeTests.RunAll -logFile Logs/CodexLifeSimFishing_red.log
```

Expected: FAIL because the spot still direct-rolls catches.

- [ ] **Step 3: Implement minigame controller**

Add an IMGUI reel panel showing catch progress, box position, fish position, instructions, success/failure state, and close behavior after result.

- [ ] **Step 4: Route spot interaction**

Change formal spot interaction to open the minigame through global UI. Keep direct `Fish()` as a test seam only.

- [ ] **Step 5: Run green verification**

Run focused fishing tests and full smoke runner.

### Task 4: Farming Depth Rules

**Files:**
- Modify: `Assets/TouhouMigration/Scripts/Runtime/Farming/MigrationCropDefinition.cs`
- Modify: `Assets/TouhouMigration/Scripts/Runtime/Farming/MigrationCropDatabase.cs`
- Modify: `Assets/TouhouMigration/Scripts/Runtime/Farming/MigrationFarmPlot.cs`
- Modify: `Assets/TouhouMigration/Scripts/Runtime/Farming/MigrationFarmingManager.cs`
- Modify: `Assets/TouhouMigration/Scripts/Runtime/Farming/MigrationFarmPlotInteractor.cs`
- Modify: `Assets/TouhouMigration/Scripts/Editor/Tests/FarmPlotSmokeTests.cs`
- Modify: `Assets/TouhouMigration/Scripts/Editor/Tests/FarmingManagerSmokeTests.cs`

- [ ] **Step 1: Write failing tests**

Add tests for water-level growth speed, Masterwork/Legendary quality with optional full-moon context, and data-driven regrow behavior.

- [ ] **Step 2: Run red verification**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.0.57f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath . -executeMethod TouhouMigration.Editor.Tests.FarmPlotSmokeTests.RunAll -logFile Logs/CodexLifeSimFarming_red.log
```

Expected: FAIL because those rules are currently deferred.

- [ ] **Step 3: Implement rule changes**

Implement optional `RegrowDays`, water-speed growth rules, and explicit quality context without breaking existing one-time crops.

- [ ] **Step 4: Run green verification**

Run focused farming tests and full smoke runner.

### Task 5: Visual PlayMode Review

**Files:**
- Modify: `Assets/TouhouMigration/Scripts/Editor/MigrationPlayModeValidator.cs` if targeted scene filtering or feature-state captures are needed.
- Create or update: `Verification/VisualChecks/LifeSimVisibleClosureReport.md`

- [ ] **Step 1: Build affected scenes**

Run the builder/repair methods needed for TownWorld, FantasyVillage, HumanVillageVerticalSlice, BambooHomeVerticalSlice, BambooHouse, MokouHouse3D, MistyLake, and Farm.

- [ ] **Step 2: Capture PlayMode evidence**

Run PlayMode validation and targeted screenshots for affected scenes.

- [ ] **Step 3: Inspect screenshots**

Verify quest board, storage chest, fishing prompt/minigame, and farm plot states are visible, readable, and not occluded. Record pass/fail notes in `Verification/VisualChecks/LifeSimVisibleClosureReport.md`.

- [ ] **Step 4: Iterate**

Fix object placement, prompt text, UI size, or camera framing until screenshot evidence passes.

### Task 6: Final Regression and Documentation

**Files:**
- Modify: `Docs/CURRENT_HANDOFF.md`
- Modify: `Docs/PROJECT_PROGRESS.md`
- Modify: `Docs/MigrationInventory.md`

- [ ] **Step 1: Run full smoke runner**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.0.57f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath . -executeMethod TouhouMigration.Editor.Tests.MigrationSmokeTestRunner.RunAll -logFile Logs/CodexLifeSimVisibleClosure_full_smoke.log
```

Expected: all discovered suites pass.

- [ ] **Step 2: Run PlayMode validation**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.0.57f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath . -executeMethod TouhouMigration.Editor.MigrationPlayModeValidator.ValidateAndCapture -logFile Logs/CodexLifeSimVisibleClosure_playmode.log
```

Expected: formal scenes enter PlayMode with zero runtime errors, and affected-scene screenshots support the visual closure report.

- [ ] **Step 3: Update docs**

Record shipped behavior, validation logs, screenshots, and remaining non-first-batch gaps.

- [ ] **Step 4: Commit only owned files**

Stage the plan, code, tests, affected generated scenes, visual report, and docs by explicit path.
