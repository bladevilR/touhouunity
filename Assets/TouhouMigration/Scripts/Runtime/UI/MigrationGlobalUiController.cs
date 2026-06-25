using System.Collections.Generic;
using TouhouMigration.Runtime.Audio;
using TouhouMigration.Runtime.Bootstrap;
using TouhouMigration.Runtime.Combat;
using TouhouMigration.Runtime.Cooking;
using TouhouMigration.Runtime.Dialogue;
using TouhouMigration.Runtime.Economy;
using TouhouMigration.Runtime.Farming;
using TouhouMigration.Runtime.Fishing;
using TouhouMigration.Runtime.Foundation;
using TouhouMigration.Runtime.Home;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Narrative;
using TouhouMigration.Runtime.Player;
using TouhouMigration.Runtime.Progression;
using TouhouMigration.Runtime.Quest;
using TouhouMigration.Runtime.Save;
using TouhouMigration.Runtime.Settings;
using TouhouMigration.Runtime.Social;
using TouhouMigration.Runtime.UI.Dialogue;
using UnityEngine;

namespace TouhouMigration.Runtime.UI
{
    public sealed class MigrationGlobalUiController : MonoBehaviour
    {
        [SerializeField] private MigrationHudController hudController;
        [SerializeField] private MigrationUnifiedMenuController unifiedMenuController;
        [SerializeField] private MigrationSettingsController settingsController;
        [SerializeField] private RuneDialogueController runeDialogueController;
        [SerializeField] private MigrationGiftSelectionController giftSelectionController;
        [SerializeField] private MigrationShopController shopController;
        [SerializeField] private MigrationProjectileSpecialSettlement projectileSettlement;

        private MigrationGameSettings settings;
        private ItemDatabase itemDatabase;
        private InventoryService inventoryService;
        private MigrationPlayerProgressService playerProgressService;
        private HumanityService humanityService;
        private MigrationStoryFlagService storyFlagService;
        private MigrationFatigueSystem fatigueSystem;
        private MigrationCompanionRoster companionRoster;
        private MigrationHomeStorage homeStorage;
        private MigrationMetaProgression metaProgression;
        private MigrationNpcRelationshipNetwork npcRelationshipNetwork;
        private MigrationNpcMemorySystem npcMemorySystem;
        private MigrationDayCycle dayCycle;
        private MigrationFatigueDriver fatigueDriver;
        private DialogueDatabase dialogueDatabase;
        private DialogueRuntimeFacade dialogueFacade;
        private GiftDatabase giftDatabase;
        private GiftInteractionService giftInteractionService;
        private QuestDatabase questDatabase;
        private QuestRewardLedger questRewardLedger;
        private QuestRewardSink questRewardSink;
        private CookingDatabase cookingDatabase;
        private CookingService cookingService;
        private CookingBuffService cookingBuffService;
        private MigrationPlayerHealthRuntime playerHealthRuntime;
        private MigrationCombatRuntime combatRuntime;
        private MigrationPhoenixGaugeRuntime phoenixGaugeRuntime;
        private ItemUseService itemUseService;
        private SocialBondService socialBondService;
        private QuestDeliveryService questDeliveryService;
        private MigrationSaveService saveService;
        private MigrationSaveOrchestrator saveOrchestrator;
        private MigrationGameStateMachine gameState;
        private WorldSimulationBehaviour worldSimulation;
        private DialogueEffectRouter dialogueEffectRouter;
        private bool menuModePushed;

        private const int FarmPlotCount = 9;
        private MigrationCropDatabase cropDatabase;
        private MigrationFarmingManager farmingManager;
        private MigrationShopDatabase shopDatabase;
        private MigrationShopService shopService;
        private MigrationAudioManager audioManager;
        private MigrationFishDatabase fishDatabase;
        private MigrationFishingService fishingService;
        private MigrationNpcRoster npcRoster;
        private MigrationNpcManager npcManager;

        public MigrationFarmingManager FarmingManager => farmingManager;
        public MigrationShopDatabase ShopCatalog => shopDatabase;
        public MigrationFishingService Fishing => fishingService;
        public MigrationNpcManager NpcManager => npcManager;
        public MigrationAudioManager Audio => audioManager;

        public bool BlocksGameplayInput =>
            (giftSelectionController != null && giftSelectionController.IsOpen) ||
            (shopController != null && shopController.IsOpen) ||
            (unifiedMenuController != null && unifiedMenuController.IsOpen) ||
            (dialogueFacade != null && dialogueFacade.IsActive);

        public CookingBuffService CookingBuffs => cookingBuffService;
        public MigrationPlayerHealthRuntime PlayerHealth => playerHealthRuntime;
        public MigrationGameStateMachine GameState => gameState;

        // Persist gameplay state: the life-sim service snapshots (via the save orchestrator), player HP,
        // story flags, and the active scene + player position scalars. Returns false if not yet initialized.
        public bool SaveGame(int slot)
        {
            if (saveOrchestrator == null || saveService == null)
            {
                return false;
            }

            MigrationSaveData data = saveOrchestrator.Capture(new MigrationSaveData());
            if (playerHealthRuntime != null)
            {
                data.max_hp = Mathf.RoundToInt(playerHealthRuntime.MaxHp);
                data.current_hp = Mathf.RoundToInt(playerHealthRuntime.CurrentHp);
            }

            if (storyFlagService != null)
            {
                data.StoryFlags = storyFlagService.CreateSnapshot();
            }

            CapturePlayerLocation(data);

            return saveService.SaveSlot(slot, data);
        }

        // Record the active scene + player transform position. Keeps the data's existing position when no
        // player is present (e.g. a UI-only scene).
        private void CapturePlayerLocation(MigrationSaveData data)
        {
            string sceneName = gameObject.scene.name;
            Transform player = ResolvePlayerTransform();
            if (player != null)
            {
                Vector3 p = player.position;
                MigrationPlayerLocation.Write(data, sceneName, p.x, p.y, p.z);
            }
            else
            {
                MigrationPlayerLocation.Write(data, sceneName, data.Position.x, data.Position.y, data.Position.z);
            }
        }

        // Move the player back to the saved position when the save's scene matches the active scene.
        // A cross-scene restore (load the saved scene, then place the player) is a scene-flow follow-up.
        private void RestorePlayerLocation(MigrationSaveData data)
        {
            if (!MigrationPlayerLocation.IsSameScene(data, gameObject.scene.name))
            {
                return;
            }

            Transform player = ResolvePlayerTransform();
            if (player != null)
            {
                player.position = new Vector3(data.Position.x, data.Position.y, data.Position.z);
            }
        }

        private Transform ResolvePlayerTransform()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? player.transform : null;
        }

        public bool LoadGame(int slot)
        {
            if (saveOrchestrator == null || saveService == null)
            {
                return false;
            }

            MigrationSaveData data = saveService.LoadSlot(slot);
            if (data == null)
            {
                return false;
            }

            saveOrchestrator.Apply(data);
            playerHealthRuntime?.SetHealth(data.current_hp, data.max_hp);
            storyFlagService?.LoadSnapshot(data.StoryFlags);
            RestorePlayerLocation(data);
            return true;
        }

        // Game loop tracks Dialogue as a transient mode layered over the base (Overworld) mode.
        private void OnDialogueStartedMode(string npcId, int sessionId)
        {
            gameState?.Push(MigrationGameStateMode.Dialogue);
        }

        private void OnDialogueFinishedMode(System.Collections.Generic.Dictionary<string, object> result)
        {
            if (gameState != null && gameState.CurrentMode == MigrationGameStateMode.Dialogue)
            {
                gameState.Pop();
            }
        }
        public MigrationCombatRuntime Combat => combatRuntime;
        public MigrationPhoenixGaugeRuntime PhoenixGauge => phoenixGaugeRuntime;
        public MigrationProjectileSpecialSettlement ProjectileSettlement => projectileSettlement;
        public InventoryService Inventory => inventoryService;
        public MigrationPlayerProgressService PlayerProgress => playerProgressService;
        public QuestDeliveryService Quests => questDeliveryService;

        public static bool IsGameplayInputBlocked()
        {
            MigrationGlobalUiController controller = FindAnyObjectByType<MigrationGlobalUiController>();
            return controller != null && controller.BlocksGameplayInput;
        }

        public static CookingBuffService FindCookingBuffService()
        {
            MigrationGlobalUiController controller = FindAnyObjectByType<MigrationGlobalUiController>();
            return controller != null ? controller.CookingBuffs : null;
        }

        public static MigrationPlayerHealthRuntime FindPlayerHealthRuntime()
        {
            MigrationGlobalUiController controller = FindAnyObjectByType<MigrationGlobalUiController>();
            return controller != null ? controller.PlayerHealth : null;
        }

        public static MigrationCombatRuntime FindCombatRuntime()
        {
            MigrationGlobalUiController controller = FindAnyObjectByType<MigrationGlobalUiController>();
            return controller != null ? controller.Combat : null;
        }

        public static MigrationPhoenixGaugeRuntime FindPhoenixGaugeRuntime()
        {
            MigrationGlobalUiController controller = FindAnyObjectByType<MigrationGlobalUiController>();
            return controller != null ? controller.PhoenixGauge : null;
        }

        public static MigrationProjectileSpecialSettlement FindProjectileSettlement()
        {
            MigrationGlobalUiController controller = FindAnyObjectByType<MigrationGlobalUiController>();
            return controller != null ? controller.ProjectileSettlement : null;
        }

        public static InventoryService FindInventoryService()
        {
            MigrationGlobalUiController controller = FindAnyObjectByType<MigrationGlobalUiController>();
            return controller != null ? controller.Inventory : null;
        }

        public static MigrationPlayerProgressService FindPlayerProgressService()
        {
            MigrationGlobalUiController controller = FindAnyObjectByType<MigrationGlobalUiController>();
            return controller != null ? controller.PlayerProgress : null;
        }

        public static QuestDeliveryService FindQuestDeliveryService()
        {
            MigrationGlobalUiController controller = FindAnyObjectByType<MigrationGlobalUiController>();
            return controller != null ? controller.Quests : null;
        }

        private void Awake()
        {
            settings = MigrationGameSettings.Load();
            InitializeInventory();
            InitializeDialogue();
            InitializeSocial();
            InitializeLifeSimCatalogs();

            hudController ??= GetComponent<MigrationHudController>();
            unifiedMenuController ??= GetComponent<MigrationUnifiedMenuController>();
            settingsController ??= GetComponent<MigrationSettingsController>();
            runeDialogueController ??= GetComponent<RuneDialogueController>();
            giftSelectionController ??= GetComponent<MigrationGiftSelectionController>();
            shopController ??= GetComponent<MigrationShopController>();
            projectileSettlement ??= GetComponent<MigrationProjectileSpecialSettlement>();

            // E7 audio: created at runtime (like the other services) so it needs no per-scene serialization.
            // Its Start auto-plays the active scene's BGM; every play is null-safe until the clips exist.
            audioManager ??= GetComponent<MigrationAudioManager>() ?? gameObject.AddComponent<MigrationAudioManager>();

            WorldSimulationBehaviour simulation = FindAnyObjectByType<WorldSimulationBehaviour>();
            worldSimulation = simulation;
            hudController?.Bind(settings, simulation, inventoryService, itemDatabase, playerProgressService, phoenixGaugeRuntime);
            unifiedMenuController?.Bind(
                settings,
                settingsController,
                inventoryService,
                itemDatabase,
                questDatabase,
                questDeliveryService,
                socialBondService,
                cookingDatabase,
                cookingService,
                cookingBuffService,
                itemUseService);
            runeDialogueController?.Bind(dialogueFacade);
            giftSelectionController?.Bind(giftDatabase, giftInteractionService, inventoryService, itemDatabase);
            shopController?.Bind(
                shopDatabase,
                shopService,
                inventoryService,
                itemDatabase,
                playerProgressService,
                () => worldSimulation != null ? worldSimulation.GetTimeSnapshot().Hour : 12,
                sfx => audioManager?.PlaySfx(sfx));

            if (settingsController != null)
            {
                settingsController.BindSettings(settings);
                settingsController.SceneLoadRequested += LoadScene;
            }

            // Day-loop wiring: now that the world clock + farming manager are resolved, build the day
            // cycle (sleep -> daily resets across farming/quests/bonds/NPC-memory/weather) + the fatigue
            // accrual driver, and upgrade the save orchestrator to also persist the calendar.
            if (worldSimulation != null)
            {
                worldSimulation.Initialize();
                fatigueDriver = new MigrationFatigueDriver(worldSimulation.Clock, fatigueSystem);
                fatigueDriver.CurrentActivity = MigrationFatigueDriver.ActivityForGameMode(gameState.CurrentMode);
                dayCycle = new MigrationDayCycle(
                    worldSimulation.Clock, farmingManager, questDeliveryService, socialBondService,
                    fatigueSystem, npcMemorySystem, worldSimulation.Weather);
                saveOrchestrator = new MigrationSaveOrchestrator(
                    inventoryService, cookingService, cookingBuffService, socialBondService, questDeliveryService, humanityService,
                    fatigue: fatigueSystem, clock: worldSimulation.Clock, companions: companionRoster, homeStorage: homeStorage,
                    meta: metaProgression, relationships: npcRelationshipNetwork, npcMemory: npcMemorySystem);
            }
        }

        private void OnDestroy()
        {
            dayCycle?.Detach();
            fatigueDriver?.Detach();
        }

        // Sleep to the next morning: advances the world clock a day (running the daily resets) and fully
        // restores fatigue. A bed interaction or HUD button calls this.
        public void Sleep()
        {
            dayCycle?.Sleep();
        }

        // The fatigue accrual driver, so a gameplay mode/activity binder can set CurrentActivity.
        public MigrationFatigueDriver FatigueDriver => fatigueDriver;

        private void InitializeInventory()
        {
            itemDatabase = new ItemDatabase();
            if (!itemDatabase.LoadFromPath("Assets/TouhouMigration/Data/Items/items.json"))
            {
                Debug.LogWarning("Migration inventory item database failed to load: " + string.Join("; ", itemDatabase.Errors));
                return;
            }

            inventoryService = new InventoryService(itemDatabase, InventoryService.DefaultMaxSlots);
            playerProgressService = new MigrationPlayerProgressService();
            inventoryService.AddItem("seed_apple", 10);
            inventoryService.AddItem("seed_carrot", 10);
            inventoryService.AddItem("seed_tomato", 10);
            inventoryService.AddItem("magic_crystal", 2);
            inventoryService.AddItem("green_tea", 2);
            inventoryService.AddItem("crucian_carp", 2);
            inventoryService.AddItem("salt", 4);
            inventoryService.AddItem("rice", 2);
            inventoryService.AddItem("tea_leaves", 2);
            inventoryService.AddItem("hot_water", 2);
        }

        // Load the life-sim catalogs (crops/shops/fish/roster) + build their managers so the game has
        // its full life-sim content on startup. Consumers (Farm scene, shop UI, NPC spawning) come later.
        private void InitializeLifeSimCatalogs()
        {
            cropDatabase = new MigrationCropDatabase();
            if (!cropDatabase.LoadFromPath("Assets/TouhouMigration/Data/Farming/crops.json"))
            {
                Debug.LogWarning("Migration crop database failed to load: " + string.Join("; ", cropDatabase.Errors));
            }

            farmingManager = new MigrationFarmingManager(inventoryService, FarmPlotCount);
            farmingManager.RegisterCropsFrom(cropDatabase);

            shopDatabase = new MigrationShopDatabase();
            if (!shopDatabase.LoadFromPath("Assets/TouhouMigration/Data/Shops/shops.json"))
            {
                Debug.LogWarning("Migration shop database failed to load: " + string.Join("; ", shopDatabase.Errors));
            }

            shopService = new MigrationShopService(inventoryService, itemDatabase, playerProgressService);

            fishDatabase = new MigrationFishDatabase();
            if (!fishDatabase.LoadFromPath("Assets/TouhouMigration/Data/Fishing/fish.json"))
            {
                Debug.LogWarning("Migration fish database failed to load: " + string.Join("; ", fishDatabase.Errors));
            }

            fishingService = new MigrationFishingService(inventoryService);
            fishingService.RegisterFrom(fishDatabase);

            npcRoster = new MigrationNpcRoster();
            if (!npcRoster.LoadFromPath("Assets/TouhouMigration/Data/Npc/human_village_roster.json"))
            {
                Debug.LogWarning("Migration npc roster failed to load: " + string.Join("; ", npcRoster.Errors));
            }

            npcManager = new MigrationNpcManager();
            npcManager.RegisterFrom(npcRoster, 8, 18);
        }

        private void InitializeDialogue()
        {
            dialogueDatabase = new DialogueDatabase();
            if (!dialogueDatabase.LoadFromPath("Assets/TouhouMigration/Data/Dialogue"))
            {
                Debug.LogWarning("Migration dialogue database failed to load: " + string.Join("; ", dialogueDatabase.Errors));
            }

            dialogueFacade = new DialogueRuntimeFacade();
        }

        private void InitializeSocial()
        {
            giftDatabase = new GiftDatabase();
            if (!giftDatabase.LoadFromPath("Assets/TouhouMigration/Data/Social/gifts.json"))
            {
                Debug.LogWarning("Migration gift database failed to load: " + string.Join("; ", giftDatabase.Errors));
            }

            questDatabase = new QuestDatabase();
            if (!questDatabase.LoadFromPath("Assets/TouhouMigration/Data/Quests/quests.json"))
            {
                Debug.LogWarning("Migration quest database failed to load: " + string.Join("; ", questDatabase.Errors));
            }

            cookingDatabase = new CookingDatabase();
            if (!cookingDatabase.LoadFromPath("Assets/TouhouMigration/Data/Cooking/cooking_profiles.json"))
            {
                Debug.LogWarning("Migration cooking database failed to load: " + string.Join("; ", cookingDatabase.Errors));
            }

            if (!cookingDatabase.LoadRecipesFromPath("Assets/TouhouMigration/Data/Cooking/cooking_recipes.json"))
            {
                Debug.LogWarning("Migration cooking recipes failed to load: " + string.Join("; ", cookingDatabase.Errors));
            }

            socialBondService = new SocialBondService();
            questRewardLedger = new QuestRewardLedger();
            questRewardSink = new QuestRewardSink(inventoryService, playerProgressService);
            questDeliveryService = new QuestDeliveryService(questDatabase, questRewardLedger, cookingDatabase, questRewardSink);
            cookingService = new CookingService(cookingDatabase, inventoryService, itemDatabase, questDeliveryService);
            cookingBuffService = new CookingBuffService(cookingDatabase);
            playerHealthRuntime = new MigrationPlayerHealthRuntime();
            playerHealthRuntime.BindCookingBuffs(cookingBuffService);
            // General player i-frame after any landed hit (Godot Cirno-arena 0.75s intent); ticked each frame.
            playerHealthRuntime.SetInvulnerabilityDuration(0.75f);
            MigrationPlayerController playerController = FindAnyObjectByType<MigrationPlayerController>();
            if (playerController != null)
            {
                playerController.BindCookingBuffs(cookingBuffService);
            }

            combatRuntime = new MigrationCombatRuntime(playerController, playerHealthRuntime);
            phoenixGaugeRuntime = new MigrationPhoenixGaugeRuntime();
            phoenixGaugeRuntime.Reset(50f);
            projectileSettlement ??= GetComponent<MigrationProjectileSpecialSettlement>();
            if (projectileSettlement != null)
            {
                projectileSettlement.BindGauge(phoenixGaugeRuntime);
            }
            itemUseService = new ItemUseService(inventoryService, itemDatabase, cookingBuffService, playerHealthRuntime);
            humanityService = new HumanityService();
            storyFlagService = new MigrationStoryFlagService();
            fatigueSystem = new MigrationFatigueSystem();
            companionRoster = new MigrationCompanionRoster();
            homeStorage = new MigrationHomeStorage();
            metaProgression = new MigrationMetaProgression();
            npcRelationshipNetwork = new MigrationNpcRelationshipNetwork();
            npcMemorySystem = new MigrationNpcMemorySystem();
            dialogueEffectRouter = new DialogueEffectRouter(socialBondService, questDeliveryService);
            dialogueEffectRouter.BindInventory(inventoryService);
            dialogueEffectRouter.BindHumanity(humanityService);
            dialogueEffectRouter.BindStoryFlags(storyFlagService);
            dialogueEffectRouter.BindMemory(npcMemorySystem);

            saveService = new MigrationSaveService(null);
            // clock/calendar save is wired alongside the day-cycle once worldSimulation is resolved (see Awake).
            saveOrchestrator = new MigrationSaveOrchestrator(
                inventoryService, cookingService, cookingBuffService, socialBondService, questDeliveryService, humanityService,
                fatigue: fatigueSystem, companions: companionRoster, homeStorage: homeStorage, meta: metaProgression,
                relationships: npcRelationshipNetwork, npcMemory: npcMemorySystem);
            gameState = new MigrationGameStateMachine(MigrationGameStateMode.Overworld);
            gameState.ModeChanged += OnGameStateModeChanged;
            ApplyWorldTimeScaleForMode(gameState.CurrentMode);
            if (dialogueFacade != null)
            {
                dialogueFacade.ActionRequested += OnDialogueActionRequested;
                dialogueFacade.DialogueStarted += OnDialogueStartedMode;
                dialogueFacade.DialogueFinished += OnDialogueFinishedMode;
            }

            giftInteractionService = new GiftInteractionService(
                giftDatabase,
                inventoryService,
                dialogueDatabase,
                dialogueFacade,
                socialBondService,
                questDeliveryService,
                npcMemorySystem);
        }

        private void Update()
        {
            cookingBuffService?.Tick(Time.deltaTime);
            playerHealthRuntime?.Tick(Time.deltaTime);
            phoenixGaugeRuntime?.Tick(Time.deltaTime);

            SyncMenuGameState();

            if (giftSelectionController != null && giftSelectionController.IsOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    giftSelectionController.Close();
                }

                return;
            }

            if (shopController != null && shopController.IsOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    shopController.Close();
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                unifiedMenuController?.Toggle();
            }

            if (Input.GetKeyDown(KeyCode.I))
            {
                unifiedMenuController?.Toggle("inventory");
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                unifiedMenuController?.Toggle("overview");
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                StartSampleDialogue();
            }
        }

        private static void LoadScene(Data.MigrationSceneId sceneId)
        {
            if (TouhouMigrationBootstrap.Instance != null &&
                TouhouMigrationBootstrap.Instance.SceneTransitions != null)
            {
                TouhouMigrationBootstrap.Instance.SceneTransitions.Load(sceneId);
            }
        }

        private void StartSampleDialogue()
        {
            if (dialogueDatabase == null || dialogueFacade == null || dialogueFacade.IsActive)
            {
                return;
            }

            var context = new System.Collections.Generic.Dictionary<string, object>
            {
                ["bond_level"] = 5,
                ["time_of_day"] = "afternoon"
            };
            Dictionary<string, object> mergedContext = BuildDialogueContext("marisa");
            foreach (KeyValuePair<string, object> pair in context)
            {
                mergedContext[pair.Key] = pair.Value;
            }

            var lines = dialogueDatabase.GetDialogue("marisa", "question", mergedContext);
            if (lines.Count == 0)
            {
                lines = dialogueDatabase.GetDialogue("marisa", "greeting", mergedContext);
            }

            dialogueFacade.StartLines("marisa", lines);
        }

        public bool StartDialogueForNpc(string npcId)
        {
            if (dialogueDatabase == null || dialogueFacade == null || dialogueFacade.IsActive ||
                (giftSelectionController != null && giftSelectionController.IsOpen))
            {
                return false;
            }

            Dictionary<string, object> context = BuildDialogueContext(npcId);
            var lines = dialogueDatabase.GetDialogue(npcId, "greeting", context);
            if (lines.Count == 0)
            {
                lines = dialogueDatabase.GetDialogue(npcId, "casual", context);
            }

            return lines.Count > 0 && dialogueFacade.StartLines(npcId, lines) > 0;
        }

        public bool OpenGiftSelectionForNpc(string npcId, string displayName)
        {
            if (giftSelectionController == null || giftInteractionService == null ||
                dialogueFacade != null && dialogueFacade.IsActive)
            {
                return false;
            }

            giftSelectionController.OpenForNpc(npcId, displayName);
            audioManager?.PlaySfx("ui_open");
            return true;
        }

        public bool OpenCooking(string stationId)
        {
            if (unifiedMenuController == null || dialogueFacade != null && dialogueFacade.IsActive ||
                giftSelectionController != null && giftSelectionController.IsOpen)
            {
                return false;
            }

            unifiedMenuController.Open("cooking");
            audioManager?.PlaySfx("ui_open");
            return true;
        }

        // Open the scoped shop modal for a shopkeeper interactor's shop id. Gated like OpenCooking so it
        // does not stack on top of an active dialogue/gift/menu modal.
        public bool OpenShop(string shopId)
        {
            if (shopController == null ||
                dialogueFacade != null && dialogueFacade.IsActive ||
                giftSelectionController != null && giftSelectionController.IsOpen ||
                unifiedMenuController != null && unifiedMenuController.IsOpen)
            {
                return false;
            }

            bool opened = shopController.OpenForShop(shopId);
            if (opened)
            {
                audioManager?.PlaySfx("ui_open");
            }

            return opened;
        }

        public MigrationShopController ShopWindow => shopController;

        public GiftDeliveryResult SelectGiftForCurrentNpc(string giftId)
        {
            if (giftSelectionController == null)
            {
                return new GiftDeliveryResult
                {
                    Success = false,
                    GiftId = giftId,
                    FailureReason = "gift_selection_missing"
                };
            }

            GiftDeliveryResult result = giftSelectionController.SelectGift(giftId);
            if (result.Success)
            {
                audioManager?.PlaySfx("bond_up");
            }

            return result;
        }

        public bool GiveGiftToNpc(string npcId, string giftId)
        {
            if (giftInteractionService == null || dialogueFacade == null || dialogueFacade.IsActive)
            {
                return false;
            }

            GiftDeliveryResult result = giftInteractionService.GiveGift(npcId, giftId);
            if (result.Success)
            {
                audioManager?.PlaySfx("bond_up");
            }
            else
            {
                Debug.LogWarning($"Gift delivery failed for {npcId}/{giftId}: {result.FailureReason}");
            }

            return result.Success;
        }

        private void OnDialogueActionRequested(string actionId, Dictionary<string, object> payload)
        {
            dialogueEffectRouter?.ApplyAction(actionId, payload);
        }

        // E2: gate world-time on the active game-state mode. Dialogue/Menu/Cutscene freeze the clock,
        // Sleeping fast-forwards it; the existing Dialogue Push/Pop already drives ModeChanged here.
        private void OnGameStateModeChanged(MigrationGameStateMode previous, MigrationGameStateMode current)
        {
            ApplyWorldTimeScaleForMode(current);
            if (fatigueDriver != null)
            {
                fatigueDriver.CurrentActivity = MigrationFatigueDriver.ActivityForGameMode(current);
            }
        }

        private void ApplyWorldTimeScaleForMode(MigrationGameStateMode mode)
        {
            worldSimulation?.SetExternalTimeScale(MigrationGameStateRules.WorldTimeScale(mode));
        }

        // E2: reflect the pause/unified menu in the game-state stack so opening it freezes world-time
        // and hides the HUD via MigrationGameStateRules. Polled each frame (the menu has no open/close
        // event); the menuModePushed guard keeps the push/pop idempotent across frames.
        private void SyncMenuGameState()
        {
            if (gameState == null || unifiedMenuController == null)
            {
                return;
            }

            bool menuOpen = unifiedMenuController.IsOpen;
            if (menuOpen && !menuModePushed && gameState.CurrentMode != MigrationGameStateMode.Menu)
            {
                gameState.Push(MigrationGameStateMode.Menu);
                menuModePushed = true;
            }
            else if (!menuOpen && menuModePushed)
            {
                if (gameState.CurrentMode == MigrationGameStateMode.Menu)
                {
                    gameState.Pop();
                }

                menuModePushed = false;
            }
        }

        private Dictionary<string, object> BuildDialogueContext(string npcId)
        {
            List<string> activeQuestIds = questDeliveryService != null
                ? questDeliveryService.GetActiveQuestIds()
                : new List<string>();
            List<string> completedQuestIds = questDeliveryService != null
                ? questDeliveryService.GetCompletedQuestIds()
                : new List<string>();
            List<string> startedQuestIds = new List<string>(activeQuestIds);
            startedQuestIds.AddRange(completedQuestIds);

            return new Dictionary<string, object>
            {
                ["bond_level"] = socialBondService != null ? socialBondService.GetBondLevel(npcId) : 0,
                ["humanity"] = humanityService != null ? humanityService.Humanity : 100,
                ["time_of_day"] = worldSimulation != null
                    ? MigrationTimeOfDay.FromHour(worldSimulation.GetTimeSnapshot().Hour)
                    : "afternoon",
                ["is_full_moon"] = worldSimulation != null && worldSimulation.GetWeatherSnapshot().IsFullMoonActive,
                // Godot dialogue weather conditions use lowercase names (e.g. "rain"); match GameWeather.
                ["weather"] = worldSimulation != null ? worldSimulation.GetWeatherSnapshot().Weather.ToString().ToLowerInvariant() : "clear",
                ["active_quests"] = activeQuestIds,
                ["completed_quests"] = completedQuestIds,
                ["started_quests"] = startedQuestIds,
                // E5.5: fired narrative events (E5.4 story flags) drive the dialogue `event` /
                // `event_not_seen` conditions, which read the context's "seen_events" list.
                ["seen_events"] = storyFlagService != null ? storyFlagService.CreateSnapshot() : new List<string>()
            };
        }
    }
}
