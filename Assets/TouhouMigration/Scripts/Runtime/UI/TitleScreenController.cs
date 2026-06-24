using TouhouMigration.Runtime.Bootstrap;
using TouhouMigration.Runtime.Data;
using TouhouMigration.Runtime.UI.CardBuild;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TouhouMigration.Runtime.UI
{
    public sealed class TitleScreenController : MonoBehaviour
    {
        [SerializeField] private Texture2D background;
        [SerializeField] private Font titleFont;
        [SerializeField] private MigrationSceneId newGameScene = MigrationSceneId.BambooHomeVerticalSlice;
        [SerializeField] private MigrationSettingsController settingsController;
        [SerializeField] private MokouDeckEditorController deckEditorController;

        private readonly string[] menuItems =
        {
            "新游戏",
            "继续游戏",
            "竹林小屋",
            "卡组编辑",
            "设置",
            "退出游戏"
        };

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle buttonStyle;
        private GUIStyle smallTextStyle;
        private string statusText = "v0.1.0 Alpha";

        private void OnGUI()
        {
            EnsureStyles();
            DrawBackground();
            DrawTitle();
            DrawMenu();
            DrawFooter();
        }

        private void DrawBackground()
        {
            Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
            if (background != null)
            {
                GUI.DrawTexture(screen, background, ScaleMode.ScaleAndCrop);
            }
            else
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(0.18f, 0.12f, 0.1f, 1f);
                GUI.DrawTexture(screen, Texture2D.whiteTexture);
                GUI.color = oldColor;
            }

            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.28f);
            GUI.DrawTexture(screen, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void DrawTitle()
        {
            float width = Mathf.Min(720f, Screen.width * 0.82f);
            Rect titleRect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.16f, width, 92f);
            GUI.Label(titleRect, "东方种地之魂", titleStyle);

            Rect subtitleRect = new Rect(titleRect.x, titleRect.yMax + 4f, titleRect.width, 36f);
            GUI.Label(subtitleRect, "幻想乡的田园冒险", subtitleStyle);
        }

        private void DrawMenu()
        {
            float buttonWidth = Mathf.Min(360f, Screen.width * 0.74f);
            float buttonHeight = 52f;
            float gap = 12f;
            float totalHeight = menuItems.Length * buttonHeight + (menuItems.Length - 1) * gap;
            float startY = Mathf.Clamp(Screen.height * 0.42f, 220f, Screen.height - totalHeight - 90f);
            float x = (Screen.width - buttonWidth) * 0.5f;

            for (int i = 0; i < menuItems.Length; i++)
            {
                Rect rect = new Rect(x, startY + i * (buttonHeight + gap), buttonWidth, buttonHeight);
                if (GUI.Button(rect, menuItems[i], buttonStyle))
                {
                    HandleMenuAction(i);
                }
            }
        }

        private void DrawFooter()
        {
            GUI.Label(new Rect(24f, Screen.height - 42f, 280f, 24f), statusText, smallTextStyle);
            Rect right = new Rect(Screen.width - 360f, Screen.height - 42f, 336f, 24f);
            GUI.Label(right, "Unity Migration", smallTextStyle);
        }

        private void HandleMenuAction(int index)
        {
            switch (index)
            {
                case 0:
                case 1:
                case 2:
                    Load(newGameScene);
                    break;
                case 3:
                    if (deckEditorController != null)
                    {
                        deckEditorController.Open();
                        statusText = "卡组编辑";
                    }
                    else
                    {
                        statusText = "卡组控制器未挂载";
                    }

                    break;
                case 4:
                    if (settingsController != null)
                    {
                        settingsController.Open();
                        statusText = "设置";
                    }
                    else
                    {
                        statusText = "设置控制器未挂载";
                    }

                    break;
                case 5:
                    Application.Quit();
                    break;
            }
        }

        private void Load(MigrationSceneId sceneId)
        {
            if (TouhouMigrationBootstrap.Instance != null &&
                TouhouMigrationBootstrap.Instance.SceneTransitions != null)
            {
                TouhouMigrationBootstrap.Instance.SceneTransitions.Load(sceneId);
                return;
            }

            SceneManager.LoadScene(MigrationSceneCatalog.ToSceneName(sceneId));
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                font = titleFont,
                fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * 0.09f, 48f, 86f)),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.94f, 0.84f, 1f) }
            };

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * 0.028f, 18f, 28f)),
                normal = { textColor = new Color(1f, 0.92f, 0.78f, 0.92f) }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                font = titleFont,
                fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * 0.028f, 18f, 26f)),
                fixedHeight = 52f
            };

            smallTextStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14,
                normal = { textColor = new Color(1f, 1f, 1f, 0.72f) }
            };
        }
    }
}
