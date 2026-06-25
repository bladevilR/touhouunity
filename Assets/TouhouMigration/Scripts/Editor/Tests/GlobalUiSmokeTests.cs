using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class GlobalUiSmokeTests
    {
        private const string GameSettingsTypeName = "TouhouMigration.Runtime.Settings.MigrationGameSettings, Assembly-CSharp";
        private const string SceneRegistryTypeName = "TouhouMigration.Runtime.Data.MigrationSceneRegistry, Assembly-CSharp";
        private const string GlobalUiTypeName = "TouhouMigration.Runtime.UI.MigrationGlobalUiController, Assembly-CSharp";
        private const string HudTypeName = "TouhouMigration.Runtime.UI.MigrationHudController, Assembly-CSharp";
        private const string UnifiedMenuTypeName = "TouhouMigration.Runtime.UI.MigrationUnifiedMenuController, Assembly-CSharp";
        private const string CookingStationTypeName = "TouhouMigration.Runtime.Cooking.MigrationCookingStationInteractor, Assembly-CSharp";
        private const string SettingsControllerTypeName = "TouhouMigration.Runtime.UI.MigrationSettingsController, Assembly-CSharp";

        private const string TitleScenePath = "Assets/TouhouMigration/Scenes/TitleScreen.unity";
        private const string BambooHomeScenePath = "Assets/TouhouMigration/Scenes/BambooHomeVerticalSlice.unity";
        private const string HumanVillageScenePath = "Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity";

        [MenuItem("Touhou Migration/Tests/Run Global UI Smoke Tests")]
        public static void RunAll()
        {
            TestSettingsPersistAndClamp();
            TestSceneRegistryMarksUnavailableScenes();
            TestRuntimeScenesContainGlobalUiAndTitleDoesNotContainHud();
            TestRuntimeCookingEntryPointsExist();
            Debug.Log("Global UI smoke tests passed.");
        }

        private static void TestSettingsPersistAndClamp()
        {
            Type settingsType = RequiredType(GameSettingsTypeName);

            InvokeStatic(settingsType, "ResetPlayerPrefsForTests");
            object settings = Activator.CreateInstance(settingsType);
            SetProperty(settings, "ShowDps", true);
            SetProperty(settings, "ShowRoomMap", false);
            SetProperty(settings, "ShowDamageNumbers", false);
            SetProperty(settings, "UiSoundEnabled", false);
            SetProperty(settings, "GraphicsQuality", 3);
            SetProperty(settings, "VisualPreset", 2);
            SetProperty(settings, "PreferredSceneKey", "town");
            Invoke(settings, "SetMasterVolume", 1.8f);
            Invoke(settings, "Save");

            object loaded = InvokeStatic(settingsType, "Load");
            AssertEqual(true, GetProperty<bool>(loaded, "ShowDps"), "DPS toggle should persist.");
            AssertEqual(false, GetProperty<bool>(loaded, "ShowRoomMap"), "Room-map toggle should persist.");
            AssertEqual(false, GetProperty<bool>(loaded, "ShowDamageNumbers"), "Damage-number toggle should persist.");
            AssertEqual(false, GetProperty<bool>(loaded, "UiSoundEnabled"), "UI sound toggle should persist.");
            AssertEqual(1f, GetProperty<float>(loaded, "MasterVolume"), "Master volume should clamp to 1.0.");
            AssertEqual(3, GetProperty<int>(loaded, "GraphicsQuality"), "Graphics quality should persist.");
            AssertEqual(2, GetProperty<int>(loaded, "VisualPreset"), "Visual preset should persist.");
            AssertEqual("town", GetProperty<string>(loaded, "PreferredSceneKey"), "Preferred scene key should persist.");

            InvokeStatic(settingsType, "ResetPlayerPrefsForTests");
        }

        private static void TestSceneRegistryMarksUnavailableScenes()
        {
            Type registryType = RequiredType(SceneRegistryTypeName);
            object allOptions = InvokeStatic(registryType, "GetAllOptions");

            int count = 0;
            bool foundBamboo = false;
            bool foundTown = false;
            bool foundMagicForestAvailable = false;
            bool foundAnyDisabled = false;

            foreach (object option in (IEnumerable)allOptions)
            {
                count++;
                string key = GetProperty<string>(option, "Key");
                bool isAvailable = GetProperty<bool>(option, "IsAvailable");

                if (key == "bamboo_home" && isAvailable)
                {
                    foundBamboo = true;
                }

                if (key == "town" && isAvailable)
                {
                    foundTown = true;
                }

                if (key == "magic_forest" && isAvailable)
                {
                    foundMagicForestAvailable = true;
                }

                if (!isAvailable)
                {
                    foundAnyDisabled = true;
                }
            }

            AssertEqual(true, count >= 10, "Scene registry should expose the formal scene-selection surface.");
            AssertEqual(true, foundBamboo, "Bamboo Home should be selectable because it has a Unity scene.");
            AssertEqual(true, foundTown, "Human Village/town should be selectable because it has a Unity scene.");
            AssertEqual(true, foundMagicForestAvailable, "Magic Forest is now migrated (E3) and should be selectable.");
            AssertEqual(true, foundAnyDisabled, "Registry should still list at least one not-yet-migrated scene as disabled.");
        }

        private static void TestRuntimeScenesContainGlobalUiAndTitleDoesNotContainHud()
        {
            Type globalUiType = RequiredType(GlobalUiTypeName);
            Type hudType = RequiredType(HudTypeName);
            Type unifiedMenuType = RequiredType(UnifiedMenuTypeName);
            Type settingsControllerType = RequiredType(SettingsControllerTypeName);

            EditorSceneManager.OpenScene(TitleScenePath);
            AssertEqual(1, CountComponents(settingsControllerType), "Title scene should host the settings controller.");
            AssertEqual(0, CountComponents(hudType), "Title scene should not show runtime HUD.");

            EditorSceneManager.OpenScene(BambooHomeScenePath);
            AssertEqual(1, CountComponents(globalUiType), "Bamboo Home should host global UI.");
            AssertEqual(1, CountComponents(hudType), "Bamboo Home should host runtime HUD.");
            AssertEqual(1, CountComponents(unifiedMenuType), "Bamboo Home should host unified menu.");
            AssertEqual(1, CountComponents(settingsControllerType), "Bamboo Home should host settings UI.");

            EditorSceneManager.OpenScene(HumanVillageScenePath);
            AssertEqual(1, CountComponents(globalUiType), "Human Village should host global UI.");
            AssertEqual(1, CountComponents(hudType), "Human Village should host runtime HUD.");
            AssertEqual(1, CountComponents(unifiedMenuType), "Human Village should host unified menu.");
            AssertEqual(1, CountComponents(settingsControllerType), "Human Village should host settings UI.");
        }

        private static void TestRuntimeCookingEntryPointsExist()
        {
            Type cookingStationType = RequiredType(CookingStationTypeName);
            Type unifiedMenuType = RequiredType(UnifiedMenuTypeName);

            EditorSceneManager.OpenScene(BambooHomeScenePath);
            AssertEqual(1, CountComponents(cookingStationType), "Bamboo Home should host one cooking station entry point.");

            object menu = FirstComponent(unifiedMenuType);
            Invoke(menu, "Open", "cooking");
            AssertEqual("cooking", GetProperty<string>(menu, "CurrentTabId"), "Unified menu should expose the cooking tab.");
        }

        private static int CountComponents(Type componentType)
        {
            int count = 0;
            foreach (GameObject gameObject in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (gameObject.GetComponent(componentType) != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static object FirstComponent(Type componentType)
        {
            foreach (GameObject gameObject in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                object component = gameObject.GetComponent(componentType);
                if (component != null)
                {
                    return component;
                }
            }

            throw new Exception($"Missing component {componentType.FullName}.");
        }

        private static Type RequiredType(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type == null)
            {
                throw new Exception($"Missing required type: {typeName}");
            }

            return type;
        }

        private static object Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
            {
                throw new Exception($"Missing method {target.GetType().FullName}.{methodName}");
            }

            return method.Invoke(target, args);
        }

        private static object InvokeStatic(Type type, string methodName, params object[] args)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            if (method == null)
            {
                throw new Exception($"Missing static method {type.FullName}.{methodName}");
            }

            return method.Invoke(null, args);
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                throw new Exception($"Missing property {target.GetType().FullName}.{propertyName}");
            }

            return (T)property.GetValue(target);
        }

        private static void SetProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                throw new Exception($"Missing property {target.GetType().FullName}.{propertyName}");
            }

            property.SetValue(target, value);
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
