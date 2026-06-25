using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Economy;
using TouhouMigration.Runtime.Inventory;
using TouhouMigration.Runtime.Player;
using UnityEngine;

namespace TouhouMigration.Runtime.UI
{
    // A scoped shop modal (Godot ShopManager UI): a shopkeeper interactor opens it for a specific shop
    // id; it draws that shop's catalog with buy/sell buttons against the live MigrationShop (open-hours +
    // per-shop price/buy_rate gated), spending/earning coins through MigrationPlayerProgressService.
    // Mirrors MigrationGiftSelectionController's scoped-modal pattern (Bind/OpenForShop/Close/IsOpen,
    // an OnGUI panel) so it composes into the owner's BlocksGameplayInput + Escape-close flow. The IMGUI
    // panel is the migration shell; a uGUI/UITK skin is the later presentation pass.
    public sealed class MigrationShopController : MonoBehaviour
    {
        private MigrationShopDatabase shopDatabase;
        private MigrationShopService shopService;
        private InventoryService inventoryService;
        private ItemDatabase itemDatabase;
        private MigrationPlayerProgressService progress;
        private Func<int> hourProvider;
        private Action<string> playSfx;

        private bool isOpen;
        private string shopId = string.Empty;
        private MigrationShopDefinition definition;
        private MigrationShop shop;
        private string lastMessage = string.Empty;
        private Vector2 scrollPosition;

        private GUIStyle overlayStyle;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle closeButtonStyle;

        public bool IsOpen => isOpen;
        public string ShopId => shopId;
        public int ItemCount => definition != null ? definition.Items.Count : 0;
        public bool IsShopOpenNow => definition != null && definition.IsOpen(CurrentHour());

        public void Bind(
            MigrationShopDatabase database,
            MigrationShopService service,
            InventoryService inventory,
            ItemDatabase items,
            MigrationPlayerProgressService playerProgress,
            Func<int> hour,
            Action<string> sfx = null)
        {
            shopDatabase = database;
            shopService = service;
            inventoryService = inventory;
            itemDatabase = items;
            progress = playerProgress;
            hourProvider = hour;
            playSfx = sfx;
        }

        // Resolve a shop by id and open the modal scoped to it. Returns false if the shop is unknown or
        // the controller is not bound.
        public bool OpenForShop(string targetShopId)
        {
            string id = NormalizeId(targetShopId);
            definition = shopDatabase != null ? shopDatabase.GetShop(id) : null;
            if (definition == null || shopService == null)
            {
                definition = null;
                shop = null;
                return false;
            }

            shopId = id;
            shop = new MigrationShop(definition, shopService);
            scrollPosition = Vector2.zero;
            lastMessage = string.Empty;
            isOpen = true;
            return true;
        }

        public void Close()
        {
            isOpen = false;
        }

        public ShopTransactionResult Buy(string itemId, int quantity = 1)
        {
            if (shop == null)
            {
                return ShopTransactionResult.Fail(itemId, quantity, "shop_not_open");
            }

            ShopTransactionResult result = shop.Buy(itemId, quantity, CurrentHour());
            lastMessage = DescribeResult(result, true);
            if (result.Success)
            {
                playSfx?.Invoke("coin");
            }

            return result;
        }

        public ShopTransactionResult Sell(string itemId, int quantity = 1)
        {
            if (shop == null)
            {
                return ShopTransactionResult.Fail(itemId, quantity, "shop_not_open");
            }

            ShopTransactionResult result = shop.Sell(itemId, quantity, CurrentHour());
            lastMessage = DescribeResult(result, false);
            if (result.Success)
            {
                playSfx?.Invoke("coin");
            }

            return result;
        }

        private int CurrentHour()
        {
            return hourProvider != null ? hourProvider() : 12;
        }

        private void OnGUI()
        {
            if (!isOpen)
            {
                return;
            }

            EnsureStyles();
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none, overlayStyle);

            float width = Mathf.Min(660f, Screen.width - 36f);
            float height = Mathf.Min(560f, Screen.height - 36f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(panel, GUIContent.none, panelStyle);

            GUILayout.BeginArea(new Rect(panel.x + 24f, panel.y + 22f, panel.width - 48f, panel.height - 44f));
            GUILayout.BeginHorizontal();
            string status = IsShopOpenNow ? "营业中" : "已打烊";
            GUILayout.Label($"商店：{shopId} ({status})", titleStyle, GUILayout.Height(42f));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("关闭", closeButtonStyle, GUILayout.Width(88f), GUILayout.Height(36f)))
            {
                Close();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label($"金币：{(progress != null ? progress.Coins : 0)}", labelStyle);
            if (!string.IsNullOrWhiteSpace(lastMessage))
            {
                GUILayout.Label(lastMessage, labelStyle);
            }

            if (definition == null || definition.Items.Count == 0)
            {
                GUILayout.Label("这家店暂时没有商品。", labelStyle);
                GUILayout.EndArea();
                return;
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (MigrationShopItem item in definition.Items)
            {
                int owned = inventoryService != null ? inventoryService.GetItemCount(item.ItemId) : 0;
                int sellUnit = (int)Math.Floor(item.Price * Math.Max(0f, definition.BuyRate));
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{ResolveDisplayName(item.ItemId)} (持有 {owned})", labelStyle, GUILayout.Width(300f), GUILayout.Height(34f));
                if (GUILayout.Button($"买 {item.Price}", buttonStyle, GUILayout.Width(110f), GUILayout.Height(34f)))
                {
                    Buy(item.ItemId);
                }
                if (GUILayout.Button($"卖 {sellUnit}", buttonStyle, GUILayout.Width(110f), GUILayout.Height(34f)))
                {
                    Sell(item.ItemId);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private string ResolveDisplayName(string itemId)
        {
            ItemDefinition item = itemDatabase != null ? itemDatabase.GetItem(itemId) : null;
            return item != null && !string.IsNullOrWhiteSpace(item.Name) ? item.Name : itemId;
        }

        private string DescribeResult(ShopTransactionResult result, bool buying)
        {
            if (result.Success)
            {
                return buying
                    ? $"购买 {ResolveDisplayName(result.ItemId)} x{result.Quantity}（{result.CoinDelta} 金币）"
                    : $"出售 {ResolveDisplayName(result.ItemId)} x{result.Quantity}（+{result.CoinDelta} 金币）";
            }

            return FailureLabel(result.FailureReason);
        }

        private static string FailureLabel(string reason)
        {
            return reason switch
            {
                "shop_closed" => "商店已打烊。",
                "not_for_sale" => "这里不卖这件商品。",
                "insufficient_funds" => "金币不足。",
                "insufficient_items" => "背包里没有这件商品。",
                "inventory_full" => "背包已满。",
                _ => "交易失败。"
            };
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            overlayStyle = BoxStyle(new Color(0.03f, 0.025f, 0.02f, 0.62f));
            panelStyle = BoxStyle(new Color(0.08f, 0.09f, 0.12f, 0.96f));
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.86f, 0.92f, 1f, 1f) }
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true,
                normal = { textColor = new Color(0.82f, 0.88f, 0.98f, 1f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                padding = new RectOffset(10, 10, 6, 6)
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
