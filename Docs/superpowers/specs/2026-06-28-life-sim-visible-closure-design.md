# Life-Sim Visible Closure Design

## Goal

Bring the migrated life-sim systems from service-level presence to player-visible, repeatable gameplay loops. The first batch focuses on the highest-impact gaps: quest board, home storage, fishing minigame, farming depth, and the visual validation loop that proves each feature works in actual scenes.

## Approved Direction

Use the player-visible closure approach. The Unity project already has many core services and data sets; the practical degradation is that several systems are hard to discover, lack scene entry points, or have simplified runtime behavior. This pass should wire those systems into formal scenes with clear prompts, success and failure feedback, save/load continuity, and PlayMode screenshots.

This batch does not rewrite the full UI stack or rebuild large art sets. It keeps the existing MonoBehaviour and IMGUI patterns where they are already used, then verifies the result in-game. Production UI reskinning remains a later pass.

## Feature Scope

### Quest Board

Add a scene-facing quest board loop:

- A player can approach a quest board in a village-style scene and open daily offers.
- The board shows the currently offered daily quests from `MigrationQuestBoard`.
- Accepting a quest starts it in `QuestDeliveryService`.
- Accepted quests appear in the existing journal tab.
- Daily offers refresh on day start.
- Feedback covers accepted, already accepted, unavailable, and missing service states.

### Home Storage

Add a usable home storage loop:

- A player can approach a storage chest in home scenes and open a storage panel.
- The panel shows inventory contents and stored contents.
- The player can store one item stack unit from inventory and retrieve one item stack unit to inventory.
- Capacity and inventory-full failures are visible.
- Contents persist through the existing save/load path.

### Fishing Minigame

Replace formal-scene one-key fishing with a real catch attempt:

- A player approaches a fishing spot and starts a cast.
- The spot opens a reel/catch-bar minigame backed by `MigrationFishingSession`.
- Holding the configured input lifts the catch box; releasing lets it fall.
- Success rolls a fish through `MigrationFishingService` and grants it to inventory.
- Failure ends the attempt with a visible "got away" result.
- The existing direct catch logic remains available only as a low-level test seam.

### Farming Depth

Improve farming rules without inventing unsupported source content:

- Water level affects crop growth speed for crops that need daily water.
- Strong water/fertilizer conditions can reach Masterwork quality.
- Full-moon or excellent-condition bonuses can reach Legendary quality when the calendar context is supplied.
- Multi-harvest/regrow is added only as data-driven behavior. Existing crops default to one-time harvest unless a crop definition explicitly says otherwise.
- The existing farm plot interactor remains the scene entry point.

## Visual Validation Requirement

Every feature in this batch must be checked in the actual Unity scene, not only by smoke tests.

For each feature:

1. Run focused smoke tests and verify they fail before implementation when introducing new behavior.
2. Implement the smallest code and scene-builder changes that make the tests pass.
3. Rebuild or patch the formal scene through the existing builder pattern.
4. Enter PlayMode for the relevant scene and capture screenshots after the player-visible state is reached.
5. Inspect screenshots for prompt visibility, camera framing, UI overlap, readable text, object placement, and clear success/failure feedback.
6. Iterate until the screenshot and PlayMode report match the feature intent.

Required visual targets:

- Quest board: a village scene with the board visible, interaction prompt visible, board panel open, and journal updated after acceptance.
- Home storage: a home scene with chest visible, storage panel open, inventory and storage sections readable.
- Fishing: Misty Lake with fishing prompt visible, minigame active, success or failure feedback visible.
- Farming: Farm scene with plot state visible before and after watering/fertilizing/harvesting.

## Architecture

Use small scene-facing controllers that bind to `MigrationGlobalUiController`, mirroring existing bed, shop, cooking, fishing, and farm plot interactors. Core rules stay UnityEngine-free where they already are, so behavior can be smoke-tested without PlayMode. Scene builders or repair menu commands are responsible for placing interactors into formal scenes; generated scene assets should not be hand-edited as source of truth.

The global UI controller remains the owner for shared services. New panels can start as IMGUI controllers consistent with the existing shop, gift, cooking, and unified menu shells. They must gate gameplay input through `BlocksGameplayInput` and close on Escape or an explicit close action.

## Data Flow

Quest board:

`MigrationQuestBoard` daily offers -> quest board panel -> `QuestDeliveryService.StartQuest` -> journal tab -> save via existing quest snapshot.

Home storage:

`InventoryService` <-> storage panel <-> `MigrationHomeStorage` -> existing save orchestrator home storage snapshot.

Fishing:

`MigrationFishingSpotInteractor` -> `MigrationFishingSession` -> `MigrationFishingMinigame` -> `MigrationFishingService.Catch` -> `InventoryService`.

Farming:

`MigrationFarmPlotInteractor` -> `MigrationFarmingManager` -> `MigrationFarmPlot` with optional calendar context -> inventory harvest result.

## Error Handling

Player-facing failures should be visible but non-fatal. Missing services, blocked UI, full inventory, full storage, unavailable quest, and failed catch attempts should produce local feedback and never throw runtime exceptions in PlayMode. Service methods continue to return result objects or booleans so tests can verify the failure reason.

## Testing

Use the existing editor smoke-test style. Add focused tests for:

- Quest board accepting into `QuestDeliveryService`.
- Storage transfer from inventory to storage and back, including capacity/full inventory failure.
- Fishing spot/session integration using deterministic inputs and RNG.
- Farming water-speed, high-quality, and data-driven regrow behavior.
- Scene/builder smoke checks proving required interactors exist in formal scenes.

After focused tests, run the full migration smoke runner and PlayMode validation for the affected scenes. Screenshot review is a required part of completion, and failed screenshots are treated as failed validation.
