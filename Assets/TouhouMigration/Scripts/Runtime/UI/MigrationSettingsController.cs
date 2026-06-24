using System;
using TouhouMigration.Runtime.Data;
using TouhouMigration.Runtime.Settings;
using UnityEngine;

namespace TouhouMigration.Runtime.UI
{
    public sealed class MigrationSettingsController : MonoBehaviour
    {
        public event Action<MigrationSceneId> SceneLoadRequested;

        private static readonly string[] QualityLabels = { "低画质", "中画质", "高画质", "极致画质" };
        private static readonly string[] VisualLabels = { "原味", "清新", "电影", "梦幻" };

        private MigrationGameSettings settings;
        private bool isOpen;
        private GUIStyle overlayStyle;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle disabledLabelStyle;
        private GUIStyle buttonStyle;

        public bool IsOpen => isOpen;

        public void BindSettings(MigrationGameSettings boundSettings)
        {
            settings = boundSettings;
        }

        public void Open()
        {
            settings ??= MigrationGameSettings.Load();
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
            settings ??= MigrationGameSettings.Load();
        }

        private void OnGUI()
        {
            if (!isOpen)
            {
                return;
            }

            EnsureStyles();
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none, overlayStyle);

            float width = Mathf.Min(680f, Screen.width - 48f);
            float height = Mathf.Min(680f, Screen.height - 48f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(panel, GUIContent.none, panelStyle);

            GUILayout.BeginArea(new Rect(panel.x + 28f, panel.y + 24f, panel.width - 56f, panel.height - 48f));
            GUILayout.Label("设置", titleStyle, GUILayout.Height(46f));
            DrawToggles();
            DrawQuality();
            DrawAudio();
            DrawSceneSelector();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("返回", buttonStyle, GUILayout.Height(42f), GUILayout.Width(180f)))
            {
                settings.Save();
                Close();
            }

            GUILayout.EndArea();
        }

        private void DrawToggles()
        {
            GUILayout.Label("显示", labelStyle);
            settings.ShowDps = GUILayout.Toggle(settings.ShowDps, "显示DPS统计", GUILayout.Height(26f));
            settings.ShowRoomMap = GUILayout.Toggle(settings.ShowRoomMap, "显示房间地图", GUILayout.Height(26f));
            settings.ShowDamageNumbers = GUILayout.Toggle(settings.ShowDamageNumbers, "显示伤害数字", GUILayout.Height(26f));
            settings.UiSoundEnabled = GUILayout.Toggle(settings.UiSoundEnabled, "UI按键音效", GUILayout.Height(26f));
        }

        private void DrawQuality()
        {
            GUILayout.Space(12f);
            GUILayout.Label("画质", labelStyle);
            settings.GraphicsQuality = GUILayout.SelectionGrid(Mathf.Clamp(settings.GraphicsQuality, 0, 3), QualityLabels, 4, GUILayout.Height(34f));
            settings.VisualPreset = GUILayout.SelectionGrid(Mathf.Clamp(settings.VisualPreset, 0, 3), VisualLabels, 4, GUILayout.Height(34f));
        }

        private void DrawAudio()
        {
            GUILayout.Space(12f);
            GUILayout.Label($"主音量 {Mathf.RoundToInt(settings.MasterVolume * 100f)}%", labelStyle);
            settings.SetMasterVolume(GUILayout.HorizontalSlider(settings.MasterVolume, 0f, 1f, GUILayout.Height(24f)));
        }

        private void DrawSceneSelector()
        {
            GUILayout.Space(12f);
            GUILayout.Label("场景", labelStyle);

            foreach (MigrationSceneOption option in MigrationSceneRegistry.GetAllOptions())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(option.IsAvailable ? option.Label : $"{option.Label}（未迁移）", option.IsAvailable ? labelStyle : disabledLabelStyle, GUILayout.Width(260f));
                GUI.enabled = option.IsAvailable;
                if (GUILayout.Button("进入", buttonStyle, GUILayout.Width(88f), GUILayout.Height(28f)))
                {
                    settings.PreferredSceneKey = option.Key;
                    settings.Save();
                    SceneLoadRequested?.Invoke(option.SceneId);
                    Close();
                }

                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            overlayStyle = BoxStyle(new Color(0.02f, 0.018f, 0.015f, 0.68f));
            panelStyle = BoxStyle(new Color(0.11f, 0.075f, 0.055f, 0.96f));
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.91f, 0.74f, 1f) }
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = new Color(1f, 0.89f, 0.68f, 1f) }
            };
            disabledLabelStyle = new GUIStyle(labelStyle)
            {
                normal = { textColor = new Color(0.74f, 0.66f, 0.56f, 0.58f) }
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
