using System.Collections.Generic;
using TouhouMigration.Runtime.Cooking;
using TouhouMigration.Runtime.Settings;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Quest;
using TouhouMigration.Runtime.Social;
using UnityEngine;

namespace TouhouMigration.Runtime.UI
{
    public sealed class MigrationUnifiedMenuController : MonoBehaviour
    {
        private static readonly string[] TabIds =
        {
            "overview",
            "character",
            "inventory",
            "cooking",
            "journal",
            "deck",
            "social",
            "codex",
            "settings"
        };

        private static readonly string[] TabLabels =
        {
            "总览",
            "角色",
            "背包",
            "料理",
            "任务",
            "卡组",
            "关系",
            "图鉴",
            "设置"
        };

        private MigrationGameSettings settings;
        private MigrationSettingsController settingsController;
        private InventoryService inventoryService;
        private ItemDatabase itemDatabase;
        private QuestDatabase questDatabase;
        private QuestDeliveryService questDeliveryService;
        private SocialBondService socialBondService;
        private CookingDatabase cookingDatabase;
        private CookingService cookingService;
        private CookingBuffService cookingBuffService;
        private ItemUseService itemUseService;
        private bool isOpen;
        private int currentTab;
        private string lastCookingMessage = string.Empty;
        private string lastItemUseMessage = string.Empty;
        private GUIStyle overlayStyle;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle selectedButtonStyle;

        public bool IsOpen => isOpen;
        public string CurrentTabId => TabIds[Mathf.Clamp(currentTab, 0, TabIds.Length - 1)];
        public int QuestJournalEntryCount => GetQuestJournalEntries("all").Count;
        public int CookingRecipeCount => cookingDatabase != null ? cookingDatabase.RecipeCount : 0;

        public void Bind(MigrationGameSettings boundSettings, MigrationSettingsController boundSettingsController)
        {
            settings = boundSettings;
            settingsController = boundSettingsController;
        }

        public void Bind(
            MigrationGameSettings boundSettings,
            MigrationSettingsController boundSettingsController,
            InventoryService inventory,
            ItemDatabase items)
        {
            settings = boundSettings;
            settingsController = boundSettingsController;
            inventoryService = inventory;
            itemDatabase = items;
        }

        public void Bind(
            MigrationGameSettings boundSettings,
            MigrationSettingsController boundSettingsController,
            InventoryService inventory,
            ItemDatabase items,
            QuestDatabase quests,
            QuestDeliveryService questDelivery,
            SocialBondService socialBonds)
        {
            settings = boundSettings;
            settingsController = boundSettingsController;
            inventoryService = inventory;
            itemDatabase = items;
            questDatabase = quests;
            questDeliveryService = questDelivery;
            socialBondService = socialBonds;
        }

        public void Bind(
            MigrationGameSettings boundSettings,
            MigrationSettingsController boundSettingsController,
            InventoryService inventory,
            ItemDatabase items,
            QuestDatabase quests,
            QuestDeliveryService questDelivery,
            SocialBondService socialBonds,
            CookingDatabase cooking,
            CookingService cookingRuntime)
        {
            settings = boundSettings;
            settingsController = boundSettingsController;
            inventoryService = inventory;
            itemDatabase = items;
            questDatabase = quests;
            questDeliveryService = questDelivery;
            socialBondService = socialBonds;
            cookingDatabase = cooking;
            cookingService = cookingRuntime;
        }

        public void Bind(
            MigrationGameSettings boundSettings,
            MigrationSettingsController boundSettingsController,
            InventoryService inventory,
            ItemDatabase items,
            QuestDatabase quests,
            QuestDeliveryService questDelivery,
            SocialBondService socialBonds,
            CookingDatabase cooking,
            CookingService cookingRuntime,
            CookingBuffService cookingBuffs,
            ItemUseService itemUse)
        {
            Bind(
                boundSettings,
                boundSettingsController,
                inventory,
                items,
                quests,
                questDelivery,
                socialBonds,
                cooking,
                cookingRuntime);
            cookingBuffService = cookingBuffs;
            itemUseService = itemUse;
        }

        public List<QuestJournalEntry> GetQuestJournalEntries(string status = "active")
        {
            return questDeliveryService != null
                ? questDeliveryService.GetJournalEntries(status)
                : new List<QuestJournalEntry>();
        }

        public void Open(string tabId = "overview")
        {
            currentTab = FindTabIndex(tabId);
            isOpen = true;
        }

        public void Close()
        {
            isOpen = false;
        }

        public void Toggle(string tabId = "overview")
        {
            if (isOpen && CurrentTabId == tabId)
            {
                Close();
                return;
            }

            Open(tabId);
        }

        private void Awake()
        {
            settings ??= MigrationGameSettings.Load();
            settingsController ??= GetComponent<MigrationSettingsController>();
        }

        private void OnGUI()
        {
            if (!isOpen)
            {
                return;
            }

            EnsureStyles();
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none, overlayStyle);

            float width = Mathf.Min(980f, Screen.width - 44f);
            float height = Mathf.Min(650f, Screen.height - 44f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(panel, GUIContent.none, panelStyle);

            Rect tabs = new Rect(panel.x + 24f, panel.y + 28f, 184f, panel.height - 56f);
            Rect content = new Rect(tabs.xMax + 24f, panel.y + 28f, panel.width - tabs.width - 72f, panel.height - 56f);

            DrawTabs(tabs);
            DrawContent(content);
        }

        private void DrawTabs(Rect area)
        {
            GUILayout.BeginArea(area);
            GUILayout.Label(TabLabels[currentTab], titleStyle, GUILayout.Height(52f));
            for (int index = 0; index < TabLabels.Length; index++)
            {
                if (GUILayout.Button(TabLabels[index], index == currentTab ? selectedButtonStyle : buttonStyle, GUILayout.Height(42f)))
                {
                    currentTab = index;
                    if (TabIds[index] == "settings" && settingsController != null)
                    {
                        settingsController.Open();
                    }
                }
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("返回游戏", buttonStyle, GUILayout.Height(42f)))
            {
                Close();
            }

            GUILayout.EndArea();
        }

        private void DrawContent(Rect area)
        {
            GUILayout.BeginArea(area);
            GUILayout.Label(TabLabels[currentTab], titleStyle, GUILayout.Height(52f));

            switch (CurrentTabId)
            {
                case "overview":
                    GUILayout.Label("第1年 春 1日", labelStyle);
                    GUILayout.Label("当前迁移切片：竹林小屋 / 人之里 / Mokou 角色验证", labelStyle);
                    GUILayout.Label(settings != null && settings.ShowDps ? "DPS显示已开启" : "DPS显示已关闭", labelStyle);
                    break;
                case "character":
                    GUILayout.Label("藤原妹红", labelStyle);
                    GUILayout.Label("角色属性、装备、状态效果将在动作系统切片接入。", labelStyle);
                    break;
                case "inventory":
                    DrawInventoryContent();
                    break;
                case "cooking":
                    DrawCookingContent();
                    break;
                case "journal":
                    DrawJournalContent();
                    break;
                case "deck":
                    GUILayout.Label("卡组编辑器是下一批正式功能候选。", labelStyle);
                    break;
                case "social":
                    DrawSocialContent();
                    break;
                case "codex":
                    GUILayout.Label("图鉴会复用已迁移的物品、卡牌、地点数据。", labelStyle);
                    break;
                case "settings":
                    GUILayout.Label("设置面板已在上层打开，可调整画质、视觉、音量与场景入口。", labelStyle);
                    if (settingsController != null && GUILayout.Button("打开设置", buttonStyle, GUILayout.Width(180f), GUILayout.Height(42f)))
                    {
                        settingsController.Open();
                    }

                    break;
            }

            GUILayout.EndArea();
        }

        private void DrawCookingContent()
        {
            if (cookingDatabase == null || cookingService == null)
            {
                GUILayout.Label("料理数据初始化中。", labelStyle);
                return;
            }

            GUILayout.Label($"{cookingDatabase.GetCookwareName(cookingService.CookwareLevel)} Lv.{cookingService.CookwareLevel}", labelStyle);
            GUILayout.Label($"烹饪等级 {cookingService.CookingLevel} / 经验 {cookingService.CookingExperience}", labelStyle);
            if (cookingBuffService != null)
            {
                GUILayout.Label(
                    $"料理构筑 力{cookingBuffService.GetStatValue("atk")} 韧{cookingBuffService.GetStatValue("def")} 速{cookingBuffService.GetStatValue("spd")} 灵{cookingBuffService.GetStatValue("spi")}",
                    labelStyle);
                GUILayout.Label(
                    $"战斗倍率 伤害x{cookingBuffService.GetDamageMultiplier():0.00} 减伤{cookingBuffService.GetDamageReduction():0%} 速度x{cookingBuffService.GetSpeedMultiplier():0.00} 灵力x{cookingBuffService.GetSpiritChargeMultiplier():0.00}",
                    labelStyle);
                if (cookingBuffService.HasActiveDrink())
                {
                    CookingBuffDrinkSnapshot drink = cookingBuffService.GetActiveDrink();
                    GUILayout.Label($"饮品：{drink.DishId} {drink.Remaining:0}s", labelStyle);
                }
            }

            if (!string.IsNullOrWhiteSpace(lastCookingMessage))
            {
                GUILayout.Label(lastCookingMessage, labelStyle);
            }

            List<string> recipeIds = new List<string>(cookingService.GetUnlockedRecipes());
            recipeIds.Sort(System.StringComparer.Ordinal);
            foreach (string recipeId in recipeIds)
            {
                CookingRecipe recipe = cookingDatabase.GetRecipe(recipeId);
                if (recipe == null)
                {
                    continue;
                }

                bool canCook = cookingService.CanCook(recipeId);
                string tier = cookingDatabase.GetRecipeTier(recipeId);
                string result = string.IsNullOrWhiteSpace(recipe.ResultId) ? "" : $" -> {recipe.ResultId} x{recipe.ResultQuantity}";
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{TierLabel(tier)} {recipe.Name}{result}", labelStyle, GUILayout.Width(320f), GUILayout.Height(34f));
                GUILayout.Label(canCook ? "可烹饪" : "材料/炊具不足", labelStyle, GUILayout.Width(128f), GUILayout.Height(34f));
                if (GUILayout.Button("烹饪", buttonStyle, GUILayout.Width(96f), GUILayout.Height(34f)))
                {
                    CookingResult cookingResult = cookingService.Cook(recipeId);
                    lastCookingMessage = cookingResult.Success
                        ? $"完成：{recipe.Name} x{cookingResult.ResultQuantity} [{cookingDatabase.GetQualityName(cookingResult.Quality)}]"
                        : $"失败：{FailureLabel(cookingResult.FailureReason)}";
                }

                GUILayout.EndHorizontal();
            }
        }

        private void DrawInventoryContent()
        {
            if (inventoryService == null || itemDatabase == null)
            {
                GUILayout.Label("背包数据初始化中。", labelStyle);
                return;
            }

            GUILayout.Label($"背包容量：{inventoryService.UsedSlots} / {InventoryService.DefaultMaxSlots}", labelStyle);
            if (!string.IsNullOrWhiteSpace(lastItemUseMessage))
            {
                GUILayout.Label(lastItemUseMessage, labelStyle);
            }

            foreach (InventorySlotData slot in inventoryService.GetOccupiedSlots())
            {
                ItemDefinition item = itemDatabase.GetItem(slot.ItemId);
                string displayName = item != null && !string.IsNullOrWhiteSpace(item.Name) ? item.Name : slot.ItemId;
                string itemType = item != null ? item.ItemType : "";
                string qualityText = slot.Quality > 0 && cookingDatabase != null
                    ? $" [{cookingDatabase.GetQualityName(slot.Quality)}]"
                    : string.Empty;
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{displayName}{qualityText} x{slot.Amount}  {itemType}", labelStyle, GUILayout.Width(360f), GUILayout.Height(28f));
                bool canUse = itemUseService != null &&
                    item != null &&
                    (item.ItemType == "dish" || item.ItemType == "drink" || item.ItemType == "consumable");
                if (canUse && GUILayout.Button("使用", buttonStyle, GUILayout.Width(72f), GUILayout.Height(28f)))
                {
                    ItemUseResult result = itemUseService.UseItem(slot.ItemId, slot.Quality);
                    lastItemUseMessage = result.Success
                        ? $"已使用：{displayName}"
                        : $"无法使用：{FailureLabel(result.FailureReason)}";
                }

                GUILayout.EndHorizontal();
            }
        }

        private void DrawJournalContent()
        {
            if (questDeliveryService == null || questDatabase == null)
            {
                GUILayout.Label("任务日志初始化中。", labelStyle);
                return;
            }

            List<QuestJournalEntry> activeEntries = questDeliveryService.GetJournalEntries("active");
            List<QuestJournalEntry> completedEntries = questDeliveryService.GetJournalEntries("completed");

            GUILayout.Label("进行中", labelStyle, GUILayout.Height(28f));
            if (activeEntries.Count == 0)
            {
                GUILayout.Label("暂无任务", labelStyle);
            }
            else
            {
                foreach (QuestJournalEntry entry in activeEntries)
                {
                    DrawQuestEntry(entry);
                }
            }

            GUILayout.Space(14f);
            GUILayout.Label("已完成", labelStyle, GUILayout.Height(28f));
            if (completedEntries.Count == 0)
            {
                GUILayout.Label("暂无已完成任务", labelStyle);
            }
            else
            {
                foreach (QuestJournalEntry entry in completedEntries)
                {
                    DrawQuestEntry(entry);
                }
            }
        }

        private void DrawQuestEntry(QuestJournalEntry entry)
        {
            GUILayout.Label($"{QuestTypeLabel(entry.Type)} {entry.Title}", labelStyle, GUILayout.Height(28f));
            if (!string.IsNullOrWhiteSpace(entry.Description))
            {
                GUILayout.Label(entry.Description, labelStyle);
            }

            if (!string.IsNullOrWhiteSpace(entry.ProgressText))
            {
                GUILayout.Label(entry.ProgressText, labelStyle);
            }

            if (!string.IsNullOrWhiteSpace(entry.RewardText))
            {
                GUILayout.Label("任务奖励：" + entry.RewardText, labelStyle);
            }
        }

        private void DrawSocialContent()
        {
            if (socialBondService == null)
            {
                GUILayout.Label("关系数据初始化中。", labelStyle);
                return;
            }

            GUILayout.Label("已接入关系服务，NPC 详情页将在角色交互切片展开。", labelStyle);
            GUILayout.Label($"Marisa Lv.{socialBondService.GetBondLevel("marisa")}  {socialBondService.GetBondPoints("marisa")} pts", labelStyle);
            GUILayout.Label($"Reimu Lv.{socialBondService.GetBondLevel("reimu")}  {socialBondService.GetBondPoints("reimu")} pts", labelStyle);
        }

        private static string QuestTypeLabel(string type)
        {
            return type switch
            {
                "main" => "[主线]",
                "side" => "[支线]",
                "daily" => "[每日]",
                _ => "[任务]"
            };
        }

        private static string TierLabel(string tier)
        {
            return tier switch
            {
                "drink" => "[饮]",
                "meal" => "[餐]",
                "feast" => "[宴]",
                _ => "[点]"
            };
        }

        private static string FailureLabel(string reason)
        {
            return reason switch
            {
                "recipe_locked" => "食谱未解锁",
                "cookware_level_low" => "炊具等级不足",
                "missing_ingredients" => "材料不足",
                "result_inventory_full" => "背包已满",
                _ => "无法烹饪"
            };
        }

        private static int FindTabIndex(string tabId)
        {
            for (int index = 0; index < TabIds.Length; index++)
            {
                if (TabIds[index] == tabId)
                {
                    return index;
                }
            }

            return 0;
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            overlayStyle = BoxStyle(new Color(0.03f, 0.025f, 0.02f, 0.65f));
            panelStyle = BoxStyle(new Color(0.10f, 0.07f, 0.052f, 0.95f));
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.91f, 0.74f, 1f) }
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                wordWrap = true,
                normal = { textColor = new Color(0.96f, 0.86f, 0.68f, 1f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            selectedButtonStyle = new GUIStyle(buttonStyle)
            {
                normal = { textColor = new Color(1f, 0.78f, 0.36f, 1f) }
            };
        }

        private static GUIStyle BoxStyle(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();

            return new GUIStyle(GUI.skin.box)
            {
                normal = { background = texture }
            };
        }
    }
}
