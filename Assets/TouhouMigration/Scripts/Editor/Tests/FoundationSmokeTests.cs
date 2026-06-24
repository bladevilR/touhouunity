using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class FoundationSmokeTests
    {
        private const string GameClockTypeName = "TouhouMigration.Runtime.Foundation.GameClock, Assembly-CSharp";
        private const string WeatherServiceTypeName = "TouhouMigration.Runtime.Foundation.WeatherService, Assembly-CSharp";
        private const string DayNightPaletteTypeName = "TouhouMigration.Runtime.Foundation.DayNightPalette, Assembly-CSharp";
        private const string DayNightLightingControllerTypeName = "TouhouMigration.Runtime.Foundation.DayNightLightingController, Assembly-CSharp";
        private const string WorldSimulationBehaviourTypeName = "TouhouMigration.Runtime.Foundation.WorldSimulationBehaviour, Assembly-CSharp";

        [MenuItem("Touhou Migration/Tests/Run Foundation Smoke Tests")]
        public static void RunAll()
        {
            TestClockAdvancesMinuteHourAndPeriod();
            TestClockRollsIntoNextDay();
            TestClockRollsSeasonAfterTwentyEightDays();
            TestWeatherServiceTracksForcedWeatherAndMoonPhase();
            TestDayNightPaletteMatchesGodotBrightnessAnchors();
            TestDayNightLightingControllerAppliesUnityLights();
            TestWorldSimulationBehaviourExposesSnapshots();
            Debug.Log("Foundation smoke tests passed.");
        }

        private static void TestClockAdvancesMinuteHourAndPeriod()
        {
            object clock = NewClock();

            Invoke(clock, "AdvanceSeconds", 60f);
            object snapshot = Invoke(clock, "GetSnapshot");

            AssertEqual(8, GetProperty<int>(snapshot, "Hour"), "Hour should advance from 7 to 8 after 60 game minutes.");
            AssertEqual(0, GetProperty<int>(snapshot, "Minute"), "Minute should wrap to 0 after one hour.");
            AssertEqual("Morning", GetProperty<object>(snapshot, "Period").ToString(), "8:00 should be morning.");
            AssertEqual("Spring", GetProperty<object>(snapshot, "Season").ToString(), "Default season should be spring.");
        }

        private static void TestClockRollsIntoNextDay()
        {
            object clock = NewClock();

            Invoke(clock, "SetTime", 23, 59);
            Invoke(clock, "AdvanceSeconds", 1f);
            object snapshot = Invoke(clock, "GetSnapshot");

            AssertEqual(0, GetProperty<int>(snapshot, "Hour"), "Hour should wrap to 0 at midnight.");
            AssertEqual(0, GetProperty<int>(snapshot, "Minute"), "Minute should be 0 at midnight.");
            AssertEqual(2, GetProperty<int>(snapshot, "Day"), "Day should advance after 23:59.");
            AssertEqual("Midnight", GetProperty<object>(snapshot, "Period").ToString(), "0:00 should be midnight period.");
        }

        private static void TestClockRollsSeasonAfterTwentyEightDays()
        {
            object clock = NewClock();

            Invoke(clock, "SetDate", 28, "Spring", 1);
            Invoke(clock, "SetTime", 23, 59);
            Invoke(clock, "AdvanceSeconds", 1f);
            object snapshot = Invoke(clock, "GetSnapshot");

            AssertEqual(1, GetProperty<int>(snapshot, "Day"), "Day should wrap to 1 after day 28.");
            AssertEqual("Summer", GetProperty<object>(snapshot, "Season").ToString(), "Season should advance from spring to summer.");
            AssertEqual(1, GetProperty<int>(snapshot, "Year"), "Year should stay at 1 when spring becomes summer.");
        }

        private static object NewClock()
        {
            return NewRequiredType(GameClockTypeName);
        }

        private static void TestWeatherServiceTracksForcedWeatherAndMoonPhase()
        {
            object weather = NewRequiredType(WeatherServiceTypeName);

            Invoke(weather, "UpdateForDate", 16, "Spring");
            Invoke(weather, "UpdateForHour", 18);
            object beforeNight = Invoke(weather, "GetSnapshot");

            AssertEqual("FullMoon", GetProperty<object>(beforeNight, "MoonPhase").ToString(), "Day 16 should map to full moon.");
            AssertEqual(false, GetProperty<bool>(beforeNight, "IsFullMoonActive"), "Full moon should not be active before 19:00.");

            Invoke(weather, "UpdateForHour", 19);
            object fullMoonNight = Invoke(weather, "GetSnapshot");

            AssertEqual(true, GetProperty<bool>(fullMoonNight, "IsFullMoonActive"), "Full moon should be active from 19:00.");

            Invoke(weather, "ForceWeather", "Storm", 2f);
            object forced = Invoke(weather, "GetSnapshot");

            AssertEqual("Storm", GetProperty<object>(forced, "Weather").ToString(), "Forced weather should apply immediately.");
            AssertEqual(0.7f, GetProperty<float>(forced, "MovementModifier"), "Storm movement modifier should match Godot weather rules.");

            Invoke(weather, "AdvanceHours", 2f);
            object expired = Invoke(weather, "GetSnapshot");

            AssertEqual(false, GetProperty<bool>(expired, "IsForced"), "Forced weather should expire after its duration.");
        }

        private static void TestDayNightPaletteMatchesGodotBrightnessAnchors()
        {
            object palette = NewRequiredType(DayNightPaletteTypeName);

            object night = Invoke(palette, "GetProfile", "Night");
            object midnight = Invoke(palette, "GetProfile", "Midnight");

            AssertEqual(0.4f, GetProperty<float>(night, "Brightness"), "Night brightness should match the Godot DayNightManager anchor.");
            AssertEqual(0.25f, GetProperty<float>(midnight, "Brightness"), "Midnight brightness should match the Godot DayNightManager anchor.");
        }

        private static void TestDayNightLightingControllerAppliesUnityLights()
        {
            Type controllerType = RequiredType(DayNightLightingControllerTypeName);
            GameObject controllerObject = new GameObject("DayNightLightingController_Test");
            GameObject sunObject = new GameObject("Sun_Test");
            GameObject moonObject = new GameObject("Moon_Test");

            try
            {
                object controller = controllerObject.AddComponent(controllerType);
                Light sun = sunObject.AddComponent<Light>();
                Light moon = moonObject.AddComponent<Light>();
                sun.type = LightType.Directional;
                moon.type = LightType.Directional;

                object clock = NewClock();
                Invoke(clock, "SetTime", 20, 0);
                object time = Invoke(clock, "GetSnapshot");

                object weather = NewRequiredType(WeatherServiceTypeName);
                Invoke(weather, "UpdateForDate", 16, "Spring");
                Invoke(weather, "UpdateForHour", 20);
                object weatherSnapshot = Invoke(weather, "GetSnapshot");

                object palette = NewRequiredType(DayNightPaletteTypeName);
                Invoke(controller, "Bind", sun, moon);
                Invoke(controller, "Apply", time, weatherSnapshot, palette);

                AssertEqual(0.4f, sun.intensity, "Night sun intensity should come from the day-night palette.");
                AssertEqual(true, moon.enabled, "Moon light should be enabled during night periods.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(controllerObject);
                UnityEngine.Object.DestroyImmediate(sunObject);
                UnityEngine.Object.DestroyImmediate(moonObject);
            }
        }

        private static void TestWorldSimulationBehaviourExposesSnapshots()
        {
            Type simulationType = RequiredType(WorldSimulationBehaviourTypeName);
            GameObject simulationObject = new GameObject("WorldSimulationBehaviour_Test");

            try
            {
                object simulation = simulationObject.AddComponent(simulationType);
                Invoke(simulation, "Initialize");

                object time = Invoke(simulation, "GetTimeSnapshot");
                object weather = Invoke(simulation, "GetWeatherSnapshot");

                AssertEqual(7, GetProperty<int>(time, "Hour"), "World simulation should start at Godot's default 7:00.");
                AssertEqual("Spring", GetProperty<object>(time, "Season").ToString(), "World simulation should start in spring.");
                AssertEqual("Clear", GetProperty<object>(weather, "Weather").ToString(), "World simulation should start with clear weather.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(simulationObject);
            }
        }

        private static object NewRequiredType(string typeName)
        {
            return Activator.CreateInstance(RequiredType(typeName));
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

        private static T GetProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                throw new Exception($"Missing property {target.GetType().FullName}.{propertyName}");
            }

            return (T)property.GetValue(target);
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
