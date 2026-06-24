using System.Collections.Generic;
using System.IO;
using TouhouMigration.Runtime.CardBuild;
using UnityEngine;

namespace TouhouMigration.Runtime.UI.CardBuild
{
    public sealed class MokouDeckEditorController : MonoBehaviour
    {
        private const string DataDirectory = "Assets/TouhouMigration/Data/CardBuild";

        private readonly CardBuildDatabase database = new CardBuildDatabase();
        private CardBuildProfileStore profileStore;
        private CardBuildProfile profile;
        private bool isOpen;
        private bool loaded;
        private string statusText = "";
        private GUIStyle overlayStyle;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;

        public bool IsOpen => isOpen;

        public void Open()
        {
            EnsureLoaded();
            isOpen = true;
        }

        public void Close()
        {
            isOpen = false;
        }

        public void Toggle()
        {
            if (isOpen)
            {
                Close();
                return;
            }

            Open();
        }

        private void Awake()
        {
            EnsureLoaded();
        }

        private void OnGUI()
        {
            if (!isOpen)
            {
                return;
            }

            EnsureStyles();
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none, overlayStyle);

            float width = Mathf.Min(920f, Screen.width - 48f);
            float height = Mathf.Min(620f, Screen.height - 48f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(panel, GUIContent.none, panelStyle);

            GUILayout.BeginArea(new Rect(panel.x + 28f, panel.y + 24f, panel.width - 56f, panel.height - 48f));
            GUILayout.Label("卡组编辑", titleStyle, GUILayout.Height(44f));

            if (!loaded || profile == null)
            {
                GUILayout.Label(statusText, labelStyle);
            }
            else
            {
                GUILayout.Label($"角色：{profile.CharacterId}", labelStyle);
                GUILayout.Label($"卡牌数据：{database.CardCount} cards / {database.ArchetypeCount} archetypes / {database.BossRuleCount} boss rules", labelStyle);
                DrawDeckList(profile.ActiveDeck);
                DrawLoadout(profile.ActionLoadout);
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("保存默认配置", buttonStyle, GUILayout.Width(160f), GUILayout.Height(40f)) && profileStore != null && profile != null)
            {
                statusText = profileStore.SaveProfile(profile) ? "已保存" : "保存失败";
            }

            if (GUILayout.Button("返回", buttonStyle, GUILayout.Width(120f), GUILayout.Height(40f)))
            {
                Close();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            if (!database.LoadFromDirectory(DataDirectory))
            {
                statusText = "CardBuild 数据加载失败: " + string.Join("; ", database.Errors);
                return;
            }

            string profilePath = Path.Combine(Application.persistentDataPath, "cardbuild_profiles.json");
            profileStore = new CardBuildProfileStore(database, profilePath);
            profile = profileStore.LoadProfile(CardBuildProfileStore.DefaultCharacterId);
            CardBuildProfileValidationResult validation = profileStore.ValidateProfile(profile);
            statusText = validation.IsValid ? "默认卡组已加载" : string.Join("; ", validation.Errors);
            loaded = validation.IsValid;
        }

        private void DrawDeckList(IReadOnlyList<string> deck)
        {
            GUILayout.Space(12f);
            GUILayout.Label($"当前卡组：{deck.Count} / {CardBuildProfileStore.MaxDeckSize}", labelStyle);
            int columns = 3;
            for (int index = 0; index < deck.Count; index += columns)
            {
                GUILayout.BeginHorizontal();
                for (int offset = 0; offset < columns && index + offset < deck.Count; offset++)
                {
                    string cardId = deck[index + offset];
                    CardBuildCardDefinition card = database.GetCard(cardId);
                    string label = card != null && !string.IsNullOrWhiteSpace(card.DisplayNameZh)
                        ? card.DisplayNameZh
                        : cardId;
                    GUILayout.Label(label, labelStyle, GUILayout.Width(240f), GUILayout.Height(26f));
                }

                GUILayout.EndHorizontal();
            }
        }

        private void DrawLoadout(IReadOnlyDictionary<string, string> loadout)
        {
            GUILayout.Space(12f);
            GUILayout.Label("动作配置", labelStyle);
            foreach (KeyValuePair<string, string> entry in loadout)
            {
                CardBuildCardDefinition card = database.GetCard(entry.Value);
                string label = card != null && !string.IsNullOrWhiteSpace(card.DisplayNameZh)
                    ? card.DisplayNameZh
                    : entry.Value;
                GUILayout.Label($"{entry.Key}: {label}", labelStyle, GUILayout.Height(24f));
            }
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            overlayStyle = BoxStyle(new Color(0.02f, 0.018f, 0.015f, 0.70f));
            panelStyle = BoxStyle(new Color(0.105f, 0.068f, 0.052f, 0.96f));
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.91f, 0.74f, 1f) }
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true,
                normal = { textColor = new Color(0.98f, 0.88f, 0.70f, 1f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
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
    }
}
