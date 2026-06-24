using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class PlayerHealthBuffSmokeTests
    {
        private const string CookingDatabaseTypeName = "TouhouMigration.Runtime.Cooking.CookingDatabase, Assembly-CSharp";
        private const string CookingBuffServiceTypeName = "TouhouMigration.Runtime.Cooking.CookingBuffService, Assembly-CSharp";
        private const string PlayerHealthRuntimeTypeName = "TouhouMigration.Runtime.Player.MigrationPlayerHealthRuntime, Assembly-CSharp";
        private const string CookingProfilesPath = "Assets/TouhouMigration/Data/Cooking/cooking_profiles.json";

        [MenuItem("Touhou Migration/Tests/Run Player Health Buff Smoke Tests")]
        public static void RunAll()
        {
            TestDamageReductionUsesCurrentHpRatioAndCookingBuffs();
            TestRegenUsesCookingBuffs();
            TestKillHealUsesCookingBuffs();
            TestRebirthOncePreventsFirstLethalDamageOnly();
            TestHitstunAndSuperArmorQueriesUseCookingBuffs();
            Debug.Log("Player health buff smoke tests passed.");
        }

        private static void TestDamageReductionUsesCurrentHpRatioAndCookingBuffs()
        {
            object cooking = LoadCookingDatabase();
            object buffs = Activator.CreateInstance(RequiredType(CookingBuffServiceTypeName), cooking);
            AssertEqual(true, Invoke<bool>(buffs, "ConsumeDish", "ginseng_chicken_soup", 2), "Quality 2 ginseng soup should apply.");

            object health = Activator.CreateInstance(RequiredType(PlayerHealthRuntimeTypeName));
            Invoke(health, "BindCookingBuffs", buffs);
            Invoke(health, "SetHealth", 200f, 200f);

            object fullHpResult = Invoke(health, "ApplyDamage", 100f);
            AssertApproximately(61f, GetProperty<float>(fullHpResult, "DamageApplied"), 0.001f, "Full HP high_hp_guard should add 15% extra reduction.");
            AssertApproximately(139f, GetProperty<float>(health, "CurrentHp"), 0.001f, "Damage should subtract the reduced amount.");

            Invoke(health, "SetHealth", 100f, 200f);
            object midHpResult = Invoke(health, "ApplyDamage", 100f);
            AssertApproximately(76f, GetProperty<float>(midHpResult, "DamageApplied"), 0.001f, "Below 70% HP should lose high_hp_guard bonus.");
            AssertApproximately(24f, GetProperty<float>(health, "CurrentHp"), 0.001f, "Mid HP damage should use only def-based reduction.");
        }

        private static void TestRegenUsesCookingBuffs()
        {
            object cooking = LoadCookingDatabase();
            object buffs = Activator.CreateInstance(RequiredType(CookingBuffServiceTypeName), cooking);
            AssertEqual(true, Invoke<bool>(buffs, "ConsumeDish", "grilled_fish", 0), "Grilled fish should apply regen_1.");

            object health = Activator.CreateInstance(RequiredType(PlayerHealthRuntimeTypeName));
            Invoke(health, "BindCookingBuffs", buffs);
            Invoke(health, "SetHealth", 50f, 200f);

            Invoke(health, "Tick", 3.5f);
            AssertApproximately(53.5f, GetProperty<float>(health, "CurrentHp"), 0.001f, "regen_1 should heal one HP per second.");
        }

        private static void TestKillHealUsesCookingBuffs()
        {
            object cooking = LoadCookingDatabase();
            object buffs = Activator.CreateInstance(RequiredType(CookingBuffServiceTypeName), cooking);
            AssertEqual(true, Invoke<bool>(buffs, "ConsumeDish", "reishi_stew", 0), "Reishi stew should apply kill_heal_8_percent.");

            object health = Activator.CreateInstance(RequiredType(PlayerHealthRuntimeTypeName));
            Invoke(health, "BindCookingBuffs", buffs);
            Invoke(health, "SetHealth", 50f, 200f);

            object killHealResult = Invoke(health, "NotifyEnemyKilled");
            AssertApproximately(16f, GetProperty<float>(killHealResult, "HealApplied"), 0.001f, "kill_heal_8_percent should heal 8% max HP.");
            AssertApproximately(66f, GetProperty<float>(health, "CurrentHp"), 0.001f, "Kill heal should add to current HP.");
        }

        private static void TestRebirthOncePreventsFirstLethalDamageOnly()
        {
            object cooking = LoadCookingDatabase();
            object buffs = Activator.CreateInstance(RequiredType(CookingBuffServiceTypeName), cooking);
            AssertEqual(true, Invoke<bool>(buffs, "ConsumeDish", "kaguya_elixir_feast", 0), "Kaguya feast should apply rebirth_once.");

            object health = Activator.CreateInstance(RequiredType(PlayerHealthRuntimeTypeName));
            Invoke(health, "BindCookingBuffs", buffs);
            Invoke(health, "SetHealth", 20f, 100f);

            object firstLethal = Invoke(health, "ApplyDamage", 100f);
            AssertEqual(true, GetProperty<bool>(firstLethal, "RebirthTriggered"), "First lethal damage should trigger rebirth.");
            AssertEqual(false, GetProperty<bool>(health, "IsDead"), "Rebirth should prevent death.");
            AssertApproximately(50f, GetProperty<float>(health, "CurrentHp"), 0.001f, "Rebirth should restore 50% max HP.");

            object secondLethal = Invoke(health, "ApplyDamage", 200f);
            AssertEqual(false, GetProperty<bool>(secondLethal, "RebirthTriggered"), "Rebirth should only trigger once.");
            AssertEqual(true, GetProperty<bool>(health, "IsDead"), "Second lethal damage should kill the player.");
            AssertApproximately(0f, GetProperty<float>(health, "CurrentHp"), 0.001f, "Dead player HP should clamp to zero.");
        }

        private static void TestHitstunAndSuperArmorQueriesUseCookingBuffs()
        {
            object cooking = LoadCookingDatabase();
            object thresholdBuffs = Activator.CreateInstance(RequiredType(CookingBuffServiceTypeName), cooking);
            AssertEqual(true, Invoke<bool>(thresholdBuffs, "ConsumeDish", "ginseng_chicken_soup", 2), "Quality 2 ginseng soup should unlock def>=6.");

            object health = Activator.CreateInstance(RequiredType(PlayerHealthRuntimeTypeName));
            Invoke(health, "BindCookingBuffs", thresholdBuffs);
            AssertApproximately(0.05f, Invoke<float>(health, "GetHitstunSeconds"), 0.001f, "def>=6 should halve base hitstun.");
            AssertEqual(false, Invoke<bool>(health, "ShouldSuppressHitFeedbackWhileAttacking"), "def>=6 alone should not suppress attack hit feedback.");

            object resistBuffs = Activator.CreateInstance(RequiredType(CookingBuffServiceTypeName), cooking);
            AssertEqual(true, Invoke<bool>(resistBuffs, "ConsumeDish", "pumpkin_congee", 0), "Pumpkin congee should apply hitstun_resist_20.");
            Invoke(health, "BindCookingBuffs", resistBuffs);
            AssertApproximately(0.08f, Invoke<float>(health, "GetHitstunSeconds"), 0.001f, "hitstun_resist_20 should reduce base hitstun by 20%.");

            object armorBuffs = Activator.CreateInstance(RequiredType(CookingBuffServiceTypeName), cooking);
            AssertEqual(true, Invoke<bool>(armorBuffs, "ConsumeDish", "ginseng_chicken_soup", 3), "Legendary ginseng soup should unlock def>=10.");
            Invoke(health, "BindCookingBuffs", armorBuffs);
            AssertEqual(true, Invoke<bool>(health, "ShouldSuppressHitFeedbackWhileAttacking"), "def>=10 should suppress attack hit feedback.");
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

        private static void AssertApproximately(float expected, float actual, float tolerance, string message)
        {
            if (Mathf.Abs(expected - actual) > tolerance)
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
