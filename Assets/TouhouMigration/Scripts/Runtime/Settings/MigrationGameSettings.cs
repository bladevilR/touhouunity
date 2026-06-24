using UnityEngine;

namespace TouhouMigration.Runtime.Settings
{
    public sealed class MigrationGameSettings
    {
        private const string Prefix = "TouhouMigration.Settings.";
        private const string ShowDpsKey = Prefix + "ShowDps";
        private const string ShowRoomMapKey = Prefix + "ShowRoomMap";
        private const string ShowDamageNumbersKey = Prefix + "ShowDamageNumbers";
        private const string UiSoundEnabledKey = Prefix + "UiSoundEnabled";
        private const string MasterVolumeKey = Prefix + "MasterVolume";
        private const string GraphicsQualityKey = Prefix + "GraphicsQuality";
        private const string VisualPresetKey = Prefix + "VisualPreset";
        private const string PreferredScenePrefsKey = Prefix + "PreferredSceneKey";

        public bool ShowDps { get; set; }
        public bool ShowRoomMap { get; set; } = true;
        public bool ShowDamageNumbers { get; set; } = true;
        public bool UiSoundEnabled { get; set; } = true;
        public float MasterVolume { get; private set; } = 1f;
        public int GraphicsQuality { get; set; } = 2;
        public int VisualPreset { get; set; }
        public string PreferredSceneKey { get; set; } = "bamboo_home";

        public static MigrationGameSettings Load()
        {
            MigrationGameSettings settings = new MigrationGameSettings
            {
                ShowDps = PlayerPrefs.GetInt(ShowDpsKey, 0) == 1,
                ShowRoomMap = PlayerPrefs.GetInt(ShowRoomMapKey, 1) == 1,
                ShowDamageNumbers = PlayerPrefs.GetInt(ShowDamageNumbersKey, 1) == 1,
                UiSoundEnabled = PlayerPrefs.GetInt(UiSoundEnabledKey, 1) == 1,
                GraphicsQuality = Mathf.Clamp(PlayerPrefs.GetInt(GraphicsQualityKey, 2), 0, 3),
                VisualPreset = Mathf.Clamp(PlayerPrefs.GetInt(VisualPresetKey, 0), 0, 3),
                PreferredSceneKey = PlayerPrefs.GetString(PreferredScenePrefsKey, "bamboo_home")
            };

            settings.SetMasterVolume(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            return settings;
        }

        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            AudioListener.volume = MasterVolume;
        }

        public void Save()
        {
            PlayerPrefs.SetInt(ShowDpsKey, ShowDps ? 1 : 0);
            PlayerPrefs.SetInt(ShowRoomMapKey, ShowRoomMap ? 1 : 0);
            PlayerPrefs.SetInt(ShowDamageNumbersKey, ShowDamageNumbers ? 1 : 0);
            PlayerPrefs.SetInt(UiSoundEnabledKey, UiSoundEnabled ? 1 : 0);
            PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
            PlayerPrefs.SetInt(GraphicsQualityKey, Mathf.Clamp(GraphicsQuality, 0, 3));
            PlayerPrefs.SetInt(VisualPresetKey, Mathf.Clamp(VisualPreset, 0, 3));
            PlayerPrefs.SetString(PreferredScenePrefsKey, string.IsNullOrWhiteSpace(PreferredSceneKey) ? "bamboo_home" : PreferredSceneKey);
            PlayerPrefs.Save();
        }

        public static void ResetPlayerPrefsForTests()
        {
            PlayerPrefs.DeleteKey(ShowDpsKey);
            PlayerPrefs.DeleteKey(ShowRoomMapKey);
            PlayerPrefs.DeleteKey(ShowDamageNumbersKey);
            PlayerPrefs.DeleteKey(UiSoundEnabledKey);
            PlayerPrefs.DeleteKey(MasterVolumeKey);
            PlayerPrefs.DeleteKey(GraphicsQualityKey);
            PlayerPrefs.DeleteKey(VisualPresetKey);
            PlayerPrefs.DeleteKey(PreferredScenePrefsKey);
            PlayerPrefs.Save();
        }
    }
}
