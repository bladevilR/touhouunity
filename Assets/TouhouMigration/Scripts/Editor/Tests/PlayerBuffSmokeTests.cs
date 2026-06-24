using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class PlayerBuffSmokeTests
    {
        private const string CookingDatabaseTypeName = "TouhouMigration.Runtime.Cooking.CookingDatabase, Assembly-CSharp";
        private const string CookingBuffServiceTypeName = "TouhouMigration.Runtime.Cooking.CookingBuffService, Assembly-CSharp";
        private const string PlayerControllerTypeName = "TouhouMigration.Runtime.Player.MigrationPlayerController, Assembly-CSharp";
        private const string CookingProfilesPath = "Assets/TouhouMigration/Data/Cooking/cooking_profiles.json";

        [MenuItem("Touhou Migration/Tests/Run Player Buff Smoke Tests")]
        public static void RunAll()
        {
            TestPlayerMovementAndDashQueriesUseCookingBuffs();
            TestPlayerDamageQueriesUseCookingBuffsAndThresholds();
            Debug.Log("Player buff smoke tests passed.");
        }

        private static void TestPlayerMovementAndDashQueriesUseCookingBuffs()
        {
            object cooking = LoadCookingDatabase();
            object buffs = Activator.CreateInstance(RequiredType(CookingBuffServiceTypeName), cooking);
            AssertEqual(true, Invoke<bool>(buffs, "ConsumeDish", "bamboo_cold_noodles", 2), "Speed meal should apply.");
            AssertEqual(8, Invoke<int>(buffs, "GetStatValue", "spd"), "Quality 2 speed meal should produce 8 speed.");

            GameObject playerObject = CreatePlayerObject();
            try
            {
                object controller = playerObject.GetComponent(RequiredType(PlayerControllerTypeName));
                Invoke(controller, "BindCookingBuffs", buffs);

                AssertApproximately(5.4f, Invoke<float>(controller, "GetModifiedWalkSpeed"), 0.001f, "Walk speed should apply cooking speed multiplier.");
                AssertApproximately(8.1f, Invoke<float>(controller, "GetModifiedRunSpeed"), 0.001f, "Run speed should apply cooking speed multiplier.");
                AssertApproximately(0.55f, Invoke<float>(controller, "GetModifiedDashCooldown"), 0.001f, "Dash cooldown should apply cooking cooldown offset.");
                AssertApproximately(1.0f, Invoke<float>(controller, "GetModifiedDashDistanceMultiplier"), 0.001f, "Meal without dash distance effect should not change dash distance.");
                Invoke(buffs, "Tick", 780.1f);
                AssertApproximately(4.5f, Invoke<float>(controller, "GetModifiedWalkSpeed"), 0.001f, "Expired speed meal should return walk speed to base.");
                AssertApproximately(0.75f, Invoke<float>(controller, "GetModifiedDashCooldown"), 0.001f, "Expired speed meal should return dash cooldown to base.");

                object dashDistanceBuffs = Activator.CreateInstance(RequiredType(CookingBuffServiceTypeName), cooking);
                AssertEqual(true, Invoke<bool>(dashDistanceBuffs, "ConsumeDish", "grilled_bamboo_shoot", 0), "Dash distance snack should apply.");
                Invoke(controller, "BindCookingBuffs", dashDistanceBuffs);
                AssertApproximately(1.15f, Invoke<float>(controller, "GetModifiedDashDistanceMultiplier"), 0.001f, "Dash distance special effect should apply.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        private static void TestPlayerDamageQueriesUseCookingBuffsAndThresholds()
        {
            object cooking = LoadCookingDatabase();
            object buffs = Activator.CreateInstance(RequiredType(CookingBuffServiceTypeName), cooking);
            AssertEqual(true, Invoke<bool>(buffs, "ConsumeDish", "spicy_beast_skewer", 2), "Attack meal should apply.");

            GameObject playerObject = CreatePlayerObject();
            try
            {
                object controller = playerObject.GetComponent(RequiredType(PlayerControllerTypeName));
                Invoke(controller, "BindCookingBuffs", buffs);

                AssertApproximately(13.2f, Invoke<float>(controller, "GetModifiedAttackDamage", 10f, "light"), 0.001f, "Light damage should apply atk multiplier.");
                AssertApproximately(14.256f, Invoke<float>(controller, "GetModifiedAttackDamage", 10f, "heavy"), 0.001f, "Heavy damage should apply atk multiplier and atk>=6 threshold.");
                AssertApproximately(1.32f, Invoke<float>(controller, "GetDamageMultiplier"), 0.001f, "Controller should expose cached damage multiplier.");
                AssertApproximately(1.0f, Invoke<float>(controller, "GetModifiedSpiritChargeMultiplier"), 0.001f, "No spi buff should keep spirit charge neutral.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        private static GameObject CreatePlayerObject()
        {
            Type playerControllerType = RequiredType(PlayerControllerTypeName);
            GameObject player = new GameObject("PlayerBuffSmoke_Player");
            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 1f, 0f);
            player.AddComponent(playerControllerType);
            return player;
        }

        private static object LoadCookingDatabase()
        {
            object database = Activator.CreateInstance(RequiredType(CookingDatabaseTypeName));
            AssertEqual(true, Invoke<bool>(database, "LoadFromPath", CookingProfilesPath), "Cooking profiles should load.");
            return database;
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
            MethodInfo method = null;
            foreach (MethodInfo candidate in target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (candidate.Name == methodName && candidate.GetParameters().Length == args.Length)
                {
                    method = candidate;
                    break;
                }
            }

            if (method == null)
            {
                throw new Exception($"Missing method {target.GetType().FullName}.{methodName}");
            }

            return method.Invoke(target, args);
        }

        private static T Invoke<T>(object target, string methodName, params object[] args)
        {
            return (T)Invoke(target, methodName, args);
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }

        private static void AssertApproximately(float expected, float actual, float tolerance, string message)
        {
            if (Mathf.Abs(expected - actual) > tolerance)
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
