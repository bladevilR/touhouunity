using TouhouMigration.Runtime.Combat;
using TouhouMigration.Runtime.Foundation;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Player;
using TouhouMigration.Runtime.Settings;
using UnityEngine;

namespace TouhouMigration.Runtime.UI
{
    public sealed class MigrationHudController : MonoBehaviour
    {
        private static readonly string[] HotbarLabels =
        {
            "1 锄头",
            "2 水壶",
            "3 种子",
            "4 收获",
            "5",
            "6",
            "7",
            "8"
        };

        private MigrationGameSettings settings;
        private WorldSimulationBehaviour worldSimulation;
        private InventoryService inventoryService;
        private ItemDatabase itemDatabase;
        private MigrationPlayerProgressService playerProgressService;
        private MigrationPhoenixGaugeRuntime phoenixGaugeRuntime;
        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private GUIStyle hotbarStyle;
        private GUIStyle selectedHotbarStyle;
        private int selectedSlot;

        public void Bind(MigrationGameSettings boundSettings, WorldSimulationBehaviour simulation)
        {
            settings = boundSettings;
            worldSimulation = simulation;
        }

        public void Bind(
            MigrationGameSettings boundSettings,
            WorldSimulationBehaviour simulation,
            InventoryService inventory,
            ItemDatabase items)
        {
            settings = boundSettings;
            worldSimulation = simulation;
            inventoryService = inventory;
            itemDatabase = items;
        }

        public void Bind(
            MigrationGameSettings boundSettings,
            WorldSimulationBehaviour simulation,
            InventoryService inventory,
            ItemDatabase items,
            MigrationPlayerProgressService progress)
        {
            settings = boundSettings;
            worldSimulation = simulation;
            inventoryService = inventory;
            itemDatabase = items;
            playerProgressService = progress;
        }

        public void Bind(
            MigrationGameSettings boundSettings,
            WorldSimulationBehaviour simulation,
            InventoryService inventory,
            ItemDatabase items,
            MigrationPlayerProgressService progress,
            MigrationPhoenixGaugeRuntime phoenixGauge)
        {
            settings = boundSettings;
            worldSimulation = simulation;
            inventoryService = inventory;
            itemDatabase = items;
            playerProgressService = progress;
            phoenixGaugeRuntime = phoenixGauge;
        }

        private void Awake()
        {
            settings ??= MigrationGameSettings.Load();
            worldSimulation ??= FindAnyObjectByType<WorldSimulationBehaviour>();
        }

        private void Update()
        {
            if (MigrationGlobalUiController.IsGameplayInputBlocked())
            {
                return;
            }

            for (int index = 0; index < HotbarLabels.Length; index++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + index))
                {
                    selectedSlot = index;
                }
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawTopBar();
            DrawHotbar();
        }

        private void DrawTopBar()
        {
            WorldTimeSnapshot time = worldSimulation != null
                ? worldSimulation.GetTimeSnapshot()
                : new WorldTimeSnapshot(7, 0, 1, 1, GameSeason.Spring, GameTimePeriod.Morning);

            Rect panelRect = new Rect(18f, 18f, 244f, settings != null && settings.ShowDps ? 150f : 126f);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panelRect.x + 16f, panelRect.y + 12f, 212f, 26f), $"{time.Hour:00}:{time.Minute:00} {PeriodName(time.Period)}", labelStyle);
            GUI.Label(new Rect(panelRect.x + 16f, panelRect.y + 40f, 212f, 24f), $"{SeasonName(time.Season)} {time.Day}日 / 第{time.Year}年", labelStyle);
            int coins = playerProgressService != null ? playerProgressService.Coins : 0;
            GUI.Label(new Rect(panelRect.x + 16f, panelRect.y + 68f, 212f, 24f), $"金 {coins}", labelStyle);
            string gaugeText = phoenixGaugeRuntime != null
                ? $"火焰槽 {Mathf.RoundToInt(phoenixGaugeRuntime.CurrentValue)} / {Mathf.RoundToInt(phoenixGaugeRuntime.MaxValue)}"
                : "火焰槽 --";
            GUI.Label(new Rect(panelRect.x + 16f, panelRect.y + 94f, 212f, 24f), gaugeText, labelStyle);

            if (settings != null && settings.ShowDps)
            {
                GUI.Label(new Rect(panelRect.x + 16f, panelRect.y + 120f, 212f, 24f), $"FPS {Mathf.RoundToInt(1f / Mathf.Max(Time.unscaledDeltaTime, 0.001f))}", labelStyle);
            }
        }

        private void DrawHotbar()
        {
            string[] labels = BuildHotbarLabels();
            float slotSize = 64f;
            float gap = 8f;
            float width = labels.Length * slotSize + (labels.Length - 1) * gap;
            float startX = (Screen.width - width) * 0.5f;
            float y = Screen.height - slotSize - 22f;

            for (int index = 0; index < labels.Length; index++)
            {
                Rect rect = new Rect(startX + index * (slotSize + gap), y, slotSize, slotSize);
                GUI.Box(rect, GUIContent.none, index == selectedSlot ? selectedHotbarStyle : hotbarStyle);
                GUI.Label(new Rect(rect.x + 6f, rect.y + 8f, rect.width - 12f, rect.height - 16f), labels[index], labelStyle);
            }
        }

        private string[] BuildHotbarLabels()
        {
            string[] labels = new string[HotbarLabels.Length];
            for (int index = 0; index < labels.Length; index++)
            {
                labels[index] = HotbarLabels[index];
            }

            if (inventoryService == null || itemDatabase == null)
            {
                return labels;
            }

            int slot = 4;
            foreach (System.Collections.Generic.KeyValuePair<string, int> pair in inventoryService.GetAllItems())
            {
                if (slot >= labels.Length)
                {
                    break;
                }

                ItemDefinition item = itemDatabase.GetItem(pair.Key);
                string displayName = item != null && !string.IsNullOrWhiteSpace(item.Name) ? item.Name : pair.Key;
                labels[slot] = $"{slot + 1} {displayName}\nx{pair.Value}";
                slot++;
            }

            return labels;
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            panelStyle = BoxStyle(new Color(0.10f, 0.075f, 0.055f, 0.80f));
            hotbarStyle = BoxStyle(new Color(0.12f, 0.09f, 0.06f, 0.82f));
            selectedHotbarStyle = BoxStyle(new Color(0.38f, 0.18f, 0.08f, 0.90f));
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                wordWrap = true,
                normal = { textColor = new Color(1f, 0.91f, 0.74f, 1f) }
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

        private static string PeriodName(GameTimePeriod period)
        {
            return period switch
            {
                GameTimePeriod.Dawn => "黎明",
                GameTimePeriod.Morning => "上午",
                GameTimePeriod.Noon => "中午",
                GameTimePeriod.Afternoon => "下午",
                GameTimePeriod.Evening => "黄昏",
                GameTimePeriod.Night => "夜晚",
                GameTimePeriod.Midnight => "深夜",
                _ => ""
            };
        }

        private static string SeasonName(GameSeason season)
        {
            return season switch
            {
                GameSeason.Spring => "春",
                GameSeason.Summer => "夏",
                GameSeason.Autumn => "秋",
                GameSeason.Winter => "冬",
                _ => "春"
            };
        }
    }
}
