using System.Collections.Generic;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Social;
using UnityEngine;

namespace TouhouMigration.Runtime.UI
{
    public sealed class MigrationGiftSelectionController : MonoBehaviour
    {
        private GiftDatabase giftDatabase;
        private GiftInteractionService giftInteractionService;
        private InventoryService inventoryService;
        private ItemDatabase itemDatabase;
        private bool isOpen;
        private string npcId = string.Empty;
        private string displayName = string.Empty;
        private Vector2 scrollPosition;
        private GUIStyle overlayStyle;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle closeButtonStyle;

        public bool IsOpen => isOpen;
        public string NpcId => npcId;
        public string DisplayName => displayName;
        public int OptionCount => GetGiftOptions().Count;
        public GiftDeliveryResult LastResult { get; private set; } = new GiftDeliveryResult();

        public void Bind(
            GiftDatabase database,
            GiftInteractionService interactionService,
            InventoryService inventory,
            ItemDatabase items)
        {
            giftDatabase = database;
            giftInteractionService = interactionService;
            inventoryService = inventory;
            itemDatabase = items;
        }

        public void OpenForNpc(string targetNpcId, string targetDisplayName)
        {
            npcId = NormalizeId(targetNpcId);
            displayName = string.IsNullOrWhiteSpace(targetDisplayName) ? npcId : targetDisplayName;
            scrollPosition = Vector2.zero;
            LastResult = new GiftDeliveryResult();
            isOpen = true;
        }

        public void Close()
        {
            isOpen = false;
        }

        public List<GiftSelectionOption> GetGiftOptions()
        {
            List<GiftSelectionOption> options = new List<GiftSelectionOption>();
            if (giftDatabase == null || inventoryService == null)
            {
                return options;
            }

            foreach (KeyValuePair<string, int> item in inventoryService.GetAllItems())
            {
                if (item.Value <= 0 || !giftDatabase.HasGift(item.Key))
                {
                    continue;
                }

                GiftDefinition gift = giftDatabase.GetGift(item.Key);
                GiftReactionResult reaction = giftDatabase.GetReaction(npcId, item.Key);
                options.Add(new GiftSelectionOption
                {
                    GiftId = item.Key,
                    DisplayName = ResolveDisplayName(item.Key, gift),
                    Description = gift != null ? gift.Description : string.Empty,
                    Category = gift != null ? gift.Category : string.Empty,
                    Amount = item.Value,
                    ReactionId = reaction.ReactionId,
                    BondChange = reaction.BondChange
                });
            }

            options.Sort(CompareOptions);
            return options;
        }

        public GiftDeliveryResult SelectGift(string giftId)
        {
            if (giftInteractionService == null || string.IsNullOrWhiteSpace(npcId))
            {
                LastResult = new GiftDeliveryResult
                {
                    Success = false,
                    NpcId = npcId,
                    GiftId = NormalizeId(giftId),
                    FailureReason = "gift_selection_not_bound"
                };
                return LastResult;
            }

            LastResult = giftInteractionService.GiveGift(npcId, giftId);
            if (LastResult.Success)
            {
                Close();
            }

            return LastResult;
        }

        private void OnGUI()
        {
            if (!isOpen)
            {
                return;
            }

            EnsureStyles();
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none, overlayStyle);

            float width = Mathf.Min(620f, Screen.width - 36f);
            float height = Mathf.Min(540f, Screen.height - 36f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(panel, GUIContent.none, panelStyle);

            GUILayout.BeginArea(new Rect(panel.x + 24f, panel.y + 22f, panel.width - 48f, panel.height - 44f));
            GUILayout.BeginHorizontal();
            GUILayout.Label($"赠礼：{displayName}", titleStyle, GUILayout.Height(42f));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("关闭", closeButtonStyle, GUILayout.Width(88f), GUILayout.Height(36f)))
            {
                Close();
            }
            GUILayout.EndHorizontal();

            List<GiftSelectionOption> options = GetGiftOptions();
            if (options.Count == 0)
            {
                GUILayout.Label("当前背包没有可赠送的礼物。", labelStyle);
                GUILayout.EndArea();
                return;
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (GiftSelectionOption option in options)
            {
                string label = $"{option.DisplayName} x{option.Amount}  {ReactionLabel(option.ReactionId)}  羁绊 {FormatDelta(option.BondChange)}";
                if (GUILayout.Button(label, buttonStyle, GUILayout.Height(46f)))
                {
                    SelectGift(option.GiftId);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private string ResolveDisplayName(string giftId, GiftDefinition gift)
        {
            if (gift != null && !string.IsNullOrWhiteSpace(gift.Name))
            {
                return gift.Name;
            }

            ItemDefinition item = itemDatabase != null ? itemDatabase.GetItem(giftId) : null;
            if (item != null && !string.IsNullOrWhiteSpace(item.Name))
            {
                return item.Name;
            }

            return giftId;
        }

        private static int CompareOptions(GiftSelectionOption left, GiftSelectionOption right)
        {
            int score = ReactionScore(right.ReactionId).CompareTo(ReactionScore(left.ReactionId));
            if (score != 0)
            {
                return score;
            }

            score = right.BondChange.CompareTo(left.BondChange);
            return score != 0 ? score : string.CompareOrdinal(left.DisplayName, right.DisplayName);
        }

        private static int ReactionScore(string reactionId)
        {
            return reactionId switch
            {
                "LOVE" => 100,
                "SPECIAL" => 90,
                "LIKE" => 75,
                "NEUTRAL" => 50,
                "DISLIKE" => 25,
                "HATE" => 0,
                _ => 50
            };
        }

        private static string ReactionLabel(string reactionId)
        {
            return reactionId switch
            {
                "LOVE" => "最爱",
                "SPECIAL" => "特殊",
                "LIKE" => "喜欢",
                "DISLIKE" => "不喜欢",
                "HATE" => "讨厌",
                _ => "普通"
            };
        }

        private static string FormatDelta(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString();
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            overlayStyle = BoxStyle(new Color(0.03f, 0.025f, 0.02f, 0.62f));
            panelStyle = BoxStyle(new Color(0.11f, 0.07f, 0.052f, 0.96f));
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.91f, 0.74f, 1f) }
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true,
                normal = { textColor = new Color(0.96f, 0.86f, 0.68f, 1f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(14, 14, 6, 6)
            };
            closeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15
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

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}
