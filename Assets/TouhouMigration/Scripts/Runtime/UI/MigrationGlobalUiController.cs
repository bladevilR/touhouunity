using System.Collections.Generic;
using TouhouMigration.Runtime.Bootstrap;
using TouhouMigration.Runtime.Combat;
using TouhouMigration.Runtime.Cooking;
using TouhouMigration.Runtime.Dialogue;
using TouhouMigration.Runtime.Foundation;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Player;
using TouhouMigration.Runtime.Quest;
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
        [SerializeField] private MigrationProjectileSpecialSettlement projectileSettlement;

        private MigrationGameSettings settings;
        private ItemDatabase itemDatabase;
        private InventoryService inventoryService;
        private MigrationPlayerProgressService playerProgressService;
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
        private DialogueEffectRouter dialogueEffectRouter;

        public bool BlocksGameplayInput =>
            (giftSelectionController != null && giftSelectionController.IsOpen) ||
            (unifiedMenuController != null && unifiedMenuController.IsOpen) ||
            (dialogueFacade != null && dialogueFacade.IsActive);

        public CookingBuffService CookingBuffs => cookingBuffService;
        public MigrationPlayerHealthRuntime PlayerHealth => playerHealthRuntime;
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

            hudController ??= GetComponent<MigrationHudController>();
            unifiedMenuController ??= GetComponent<MigrationUnifiedMenuController>();
            settingsController ??= GetComponent<MigrationSettingsController>();
            runeDialogueController ??= GetComponent<RuneDialogueController>();
            giftSelectionController ??= GetComponent<MigrationGiftSelectionController>();
            projectileSettlement ??= GetComponent<MigrationProjectileSpecialSettlement>();

            WorldSimulationBehaviour simulation = FindAnyObjectByType<WorldSimulationBehaviour>();
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

            if (settingsController != null)
            {
                settingsController.BindSettings(settings);
                settingsController.SceneLoadRequested += LoadScene;
            }
        }

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
            dialogueEffectRouter = new DialogueEffectRouter(socialBondService, questDeliveryService);
            if (dialogueFacade != null)
            {
                dialogueFacade.ActionRequested += OnDialogueActionRequested;
            }

            giftInteractionService = new GiftInteractionService(
                giftDatabase,
                inventoryService,
                dialogueDatabase,
                dialogueFacade,
                socialBondService,
                questDeliveryService);
        }

        private void Update()
        {
            cookingBuffService?.Tick(Time.deltaTime);
            playerHealthRuntime?.Tick(Time.deltaTime);
            phoenixGaugeRuntime?.Tick(Time.deltaTime);

            if (giftSelectionController != null && giftSelectionController.IsOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    giftSelectionController.Close();
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
            return true;
        }

        public GiftDeliveryResult SelectGiftForCurrentNpc(string giftId)
        {
            return giftSelectionController != null
                ? giftSelectionController.SelectGift(giftId)
                : new GiftDeliveryResult
                {
                    Success = false,
                    GiftId = giftId,
                    FailureReason = "gift_selection_missing"
                };
        }

        public bool GiveGiftToNpc(string npcId, string giftId)
        {
            if (giftInteractionService == null || dialogueFacade == null || dialogueFacade.IsActive)
            {
                return false;
            }

            GiftDeliveryResult result = giftInteractionService.GiveGift(npcId, giftId);
            if (!result.Success)
            {
                Debug.LogWarning($"Gift delivery failed for {npcId}/{giftId}: {result.FailureReason}");
            }

            return result.Success;
        }

        private void OnDialogueActionRequested(string actionId, Dictionary<string, object> payload)
        {
            dialogueEffectRouter?.ApplyAction(actionId, payload);
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
                ["humanity"] = 100,
                ["time_of_day"] = "afternoon",
                ["active_quests"] = activeQuestIds,
                ["completed_quests"] = completedQuestIds,
                ["started_quests"] = startedQuestIds
            };
        }
    }
}
