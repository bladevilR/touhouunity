using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class CombatBridgeSmokeTests
    {
        private const string CookingDatabaseTypeName = "TouhouMigration.Runtime.Cooking.CookingDatabase, Assembly-CSharp";
        private const string CookingBuffServiceTypeName = "TouhouMigration.Runtime.Cooking.CookingBuffService, Assembly-CSharp";
        private const string PlayerControllerTypeName = "TouhouMigration.Runtime.Player.MigrationPlayerController, Assembly-CSharp";
        private const string PlayerHealthRuntimeTypeName = "TouhouMigration.Runtime.Player.MigrationPlayerHealthRuntime, Assembly-CSharp";
        private const string CombatRuntimeTypeName = "TouhouMigration.Runtime.Combat.MigrationCombatRuntime, Assembly-CSharp";
        private const string CombatTargetTypeName = "TouhouMigration.Runtime.Combat.MigrationCombatTargetRuntime, Assembly-CSharp";
        private const string CombatTargetBehaviourTypeName = "TouhouMigration.Runtime.Combat.MigrationCombatTargetBehaviour, Assembly-CSharp";
        private const string PlayerAttackHitboxTypeName = "TouhouMigration.Runtime.Combat.MigrationPlayerAttackHitbox, Assembly-CSharp";
        private const string EnemyDamageSourceTypeName = "TouhouMigration.Runtime.Combat.MigrationEnemyDamageSource, Assembly-CSharp";
        private const string CombatDefeatHandlerTypeName = "TouhouMigration.Runtime.Combat.MigrationCombatDefeatHandler, Assembly-CSharp";
        private const string CookingProfilesPath = "Assets/TouhouMigration/Data/Cooking/cooking_profiles.json";
        private const string HumanVillageScenePath = "Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity";

        [MenuItem("Touhou Migration/Tests/Run Combat Bridge Smoke Tests")]
        public static void RunAll()
        {
            TestPlayerAttackRoutesModifiedDamageAndKillHeal();
            TestIncomingDamageRoutesThroughPlayerHealthRuntime();
            TestDefeatedTargetDoesNotGrantKillHealTwice();
            TestTargetBehaviourEmitsDefeatOnce();
            TestPlayerAttackHitboxRoutesCombatAndDeduplicatesWindow();
            TestPlayerAttackHitboxMissingCombatNoOps();
            TestEnemyDamageSourceRoutesPlayerDamage();
            TestEnemyDamageSourceMissingCombatNoOps();
            TestCombatDefeatHandlerDisablesTargetCollidersOnce();
            TestHumanVillageSceneContainsLiveCombatAdapters();
            Debug.Log("Combat bridge smoke tests passed.");
        }

        private static void TestPlayerAttackRoutesModifiedDamageAndKillHeal()
        {
            object cooking = LoadCookingDatabase();
            object buffs = Activator.CreateInstance(RequiredType(CookingBuffServiceTypeName), cooking);
            AssertEqual(true, Invoke<bool>(buffs, "ConsumeDish", "spicy_beast_skewer", 2), "Attack meal should apply.");
            AssertEqual(true, Invoke<bool>(buffs, "ConsumeDish", "reishi_stew", 0), "Kill-heal meal should apply.");

            GameObject playerObject = CreatePlayerObject();
            try
            {
                object player = playerObject.GetComponent(RequiredType(PlayerControllerTypeName));
                Invoke(player, "BindCookingBuffs", buffs);
                object health = Activator.CreateInstance(RequiredType(PlayerHealthRuntimeTypeName));
                Invoke(health, "BindCookingBuffs", buffs);
                Invoke(health, "SetHealth", 50f, 200f);

                object combat = Activator.CreateInstance(RequiredType(CombatRuntimeTypeName), player, health);
                object target = Activator.CreateInstance(RequiredType(CombatTargetTypeName), 20f);

                object firstHit = Invoke(combat, "ApplyPlayerAttack", target, 10f, "heavy");
                AssertApproximately(14.256f, GetProperty<float>(firstHit, "DamageApplied"), 0.001f, "Heavy attack should use player cooking attack modifiers.");
                AssertEqual(false, GetProperty<bool>(firstHit, "TargetDefeated"), "First hit should not defeat a 20 HP target.");
                AssertApproximately(5.744f, GetProperty<float>(target, "CurrentHp"), 0.001f, "Target HP should subtract modified damage.");

                object secondHit = Invoke(combat, "ApplyPlayerAttack", target, 10f, "light");
                AssertApproximately(13.2f, GetProperty<float>(secondHit, "DamageApplied"), 0.001f, "Light attack should use player cooking damage multiplier.");
                AssertEqual(true, GetProperty<bool>(secondHit, "TargetDefeated"), "Second hit should defeat the target.");
                AssertApproximately(16f, GetProperty<float>(secondHit, "PlayerHealApplied"), 0.001f, "Target defeat should route kill-heal through player health runtime.");
                AssertApproximately(66f, GetProperty<float>(health, "CurrentHp"), 0.001f, "Player HP should receive kill-heal.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        private static void TestIncomingDamageRoutesThroughPlayerHealthRuntime()
        {
            object cooking = LoadCookingDatabase();
            object buffs = Activator.CreateInstance(RequiredType(CookingBuffServiceTypeName), cooking);
            AssertEqual(true, Invoke<bool>(buffs, "ConsumeDish", "ginseng_chicken_soup", 2), "Defense meal should apply.");

            GameObject playerObject = CreatePlayerObject();
            try
            {
                object player = playerObject.GetComponent(RequiredType(PlayerControllerTypeName));
                Invoke(player, "BindCookingBuffs", buffs);
                object health = Activator.CreateInstance(RequiredType(PlayerHealthRuntimeTypeName));
                Invoke(health, "BindCookingBuffs", buffs);
                Invoke(health, "SetHealth", 200f, 200f);

                object combat = Activator.CreateInstance(RequiredType(CombatRuntimeTypeName), player, health);
                object result = Invoke(combat, "ApplyDamageToPlayer", 100f);

                AssertApproximately(61f, GetProperty<float>(result, "DamageApplied"), 0.001f, "Incoming damage should route through cooking damage reduction.");
                AssertApproximately(139f, GetProperty<float>(health, "CurrentHp"), 0.001f, "Player health should lose reduced damage.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        private static void TestDefeatedTargetDoesNotGrantKillHealTwice()
        {
            object cooking = LoadCookingDatabase();
            object buffs = Activator.CreateInstance(RequiredType(CookingBuffServiceTypeName), cooking);
            AssertEqual(true, Invoke<bool>(buffs, "ConsumeDish", "reishi_stew", 0), "Kill-heal meal should apply.");

            GameObject playerObject = CreatePlayerObject();
            try
            {
                object player = playerObject.GetComponent(RequiredType(PlayerControllerTypeName));
                Invoke(player, "BindCookingBuffs", buffs);
                object health = Activator.CreateInstance(RequiredType(PlayerHealthRuntimeTypeName));
                Invoke(health, "BindCookingBuffs", buffs);
                Invoke(health, "SetHealth", 50f, 200f);

                object combat = Activator.CreateInstance(RequiredType(CombatRuntimeTypeName), player, health);
                object target = Activator.CreateInstance(RequiredType(CombatTargetTypeName), 5f);

                object firstHit = Invoke(combat, "ApplyPlayerAttack", target, 10f, "light");
                object secondHit = Invoke(combat, "ApplyPlayerAttack", target, 10f, "light");

                AssertEqual(true, GetProperty<bool>(firstHit, "TargetDefeated"), "First hit should defeat target.");
                AssertApproximately(16f, GetProperty<float>(firstHit, "PlayerHealApplied"), 0.001f, "First defeat should heal once.");
                AssertEqual(false, GetProperty<bool>(secondHit, "TargetDefeated"), "Already defeated target should not emit a second defeat.");
                AssertApproximately(0f, GetProperty<float>(secondHit, "PlayerHealApplied"), 0.001f, "Second hit should not grant kill-heal again.");
                AssertApproximately(66f, GetProperty<float>(health, "CurrentHp"), 0.001f, "Player HP should only include one kill-heal.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        private static void TestTargetBehaviourEmitsDefeatOnce()
        {
            Type behaviourType = RequiredType(CombatTargetBehaviourTypeName);
            GameObject targetObject = new GameObject("CombatBridgeSmoke_TargetBehaviour");
            try
            {
                object target = targetObject.AddComponent(behaviourType);
                Invoke(target, "Initialize", 5f);

                object partialHit = Invoke(target, "ApplyDamage", 3f);
                AssertEqual(false, GetProperty<bool>(partialHit, "TargetDefeated"), "Partial behaviour damage should not defeat target.");
                AssertApproximately(2f, GetProperty<float>(target, "CurrentHp"), 0.001f, "Behaviour target HP should subtract damage.");
                AssertEqual(0, GetProperty<int>(target, "DefeatEventCount"), "Behaviour should not emit defeat before death.");

                object defeatingHit = Invoke(target, "ApplyDamage", 3f);
                AssertEqual(true, GetProperty<bool>(defeatingHit, "TargetDefeated"), "Behaviour damage should surface first defeat.");
                AssertApproximately(0f, GetProperty<float>(target, "CurrentHp"), 0.001f, "Behaviour target HP should clamp at zero.");
                AssertEqual(1, GetProperty<int>(target, "DefeatEventCount"), "Behaviour should emit exactly one defeat event.");

                object repeatHit = Invoke(target, "ApplyDamage", 3f);
                AssertEqual(false, GetProperty<bool>(repeatHit, "TargetDefeated"), "Already defeated behaviour target should not defeat again.");
                AssertEqual(1, GetProperty<int>(target, "DefeatEventCount"), "Behaviour defeat event should remain one-shot.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        private static void TestPlayerAttackHitboxRoutesCombatAndDeduplicatesWindow()
        {
            object cooking = LoadCookingDatabase();
            object buffs = Activator.CreateInstance(RequiredType(CookingBuffServiceTypeName), cooking);
            AssertEqual(true, Invoke<bool>(buffs, "ConsumeDish", "spicy_beast_skewer", 2), "Attack meal should apply.");
            AssertEqual(true, Invoke<bool>(buffs, "ConsumeDish", "reishi_stew", 0), "Kill-heal meal should apply.");

            GameObject playerObject = CreatePlayerObject();
            GameObject hitboxObject = new GameObject("CombatBridgeSmoke_PlayerAttackHitbox");
            GameObject targetObject = new GameObject("CombatBridgeSmoke_LiveTarget");
            try
            {
                object player = playerObject.GetComponent(RequiredType(PlayerControllerTypeName));
                Invoke(player, "BindCookingBuffs", buffs);
                object health = Activator.CreateInstance(RequiredType(PlayerHealthRuntimeTypeName));
                Invoke(health, "BindCookingBuffs", buffs);
                Invoke(health, "SetHealth", 50f, 200f);
                object combat = Activator.CreateInstance(RequiredType(CombatRuntimeTypeName), player, health);

                object hitbox = hitboxObject.AddComponent(RequiredType(PlayerAttackHitboxTypeName));
                Invoke(hitbox, "BindCombat", combat);
                Invoke(hitbox, "Configure", 10f, "heavy");
                Invoke(hitbox, "BeginAttackWindow");

                object target = targetObject.AddComponent(RequiredType(CombatTargetBehaviourTypeName));
                Invoke(target, "Initialize", 20f);

                object firstHit = Invoke(hitbox, "TryHit", target);
                AssertApproximately(14.256f, GetProperty<float>(firstHit, "DamageApplied"), 0.001f, "Live hitbox should route modified heavy damage.");
                AssertApproximately(5.744f, GetProperty<float>(target, "CurrentHp"), 0.001f, "Live target HP should subtract modified heavy damage.");
                AssertEqual(1, GetProperty<int>(hitbox, "HitEventCount"), "Live hitbox should count the first landed hit.");
                AssertEqual(0, GetProperty<int>(target, "DefeatEventCount"), "Live target should not emit defeat before death.");

                object duplicateHit = Invoke(hitbox, "TryHit", target);
                AssertApproximately(0f, GetProperty<float>(duplicateHit, "DamageApplied"), 0.001f, "Live hitbox should not damage the same target twice in one attack window.");
                AssertApproximately(5.744f, GetProperty<float>(target, "CurrentHp"), 0.001f, "Duplicate live hit should leave target HP unchanged.");
                AssertEqual(1, GetProperty<int>(hitbox, "HitEventCount"), "Duplicate hit should not count as a landed hit.");

                Invoke(hitbox, "EndAttackWindow");
                Invoke(hitbox, "Configure", 10f, "light");
                Invoke(hitbox, "BeginAttackWindow");

                object secondWindowHit = Invoke(hitbox, "TryHit", target);
                AssertEqual(true, GetProperty<bool>(secondWindowHit, "TargetDefeated"), "A new attack window should be able to defeat the target.");
                AssertApproximately(13.2f, GetProperty<float>(secondWindowHit, "DamageApplied"), 0.001f, "Live hitbox should route modified light damage.");
                AssertApproximately(16f, GetProperty<float>(secondWindowHit, "PlayerHealApplied"), 0.001f, "Live target defeat should route kill-heal.");
                AssertEqual(1, GetProperty<int>(target, "DefeatEventCount"), "Live target should emit defeat exactly once.");
                AssertEqual(2, GetProperty<int>(hitbox, "HitEventCount"), "Second attack window hit should count.");
                AssertApproximately(66f, GetProperty<float>(health, "CurrentHp"), 0.001f, "Player HP should include live kill-heal.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
                UnityEngine.Object.DestroyImmediate(hitboxObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        private static void TestPlayerAttackHitboxMissingCombatNoOps()
        {
            GameObject hitboxObject = new GameObject("CombatBridgeSmoke_NoCombatHitbox");
            GameObject targetObject = new GameObject("CombatBridgeSmoke_NoCombatTarget");
            try
            {
                object hitbox = hitboxObject.AddComponent(RequiredType(PlayerAttackHitboxTypeName));
                Invoke(hitbox, "Configure", 99f, "light");
                Invoke(hitbox, "BeginAttackWindow");

                object target = targetObject.AddComponent(RequiredType(CombatTargetBehaviourTypeName));
                Invoke(target, "Initialize", 5f);

                object result = Invoke(hitbox, "TryHit", target);
                AssertApproximately(0f, GetProperty<float>(result, "DamageApplied"), 0.001f, "Hitbox without combat runtime should not apply damage.");
                AssertEqual(false, GetProperty<bool>(result, "TargetDefeated"), "Hitbox without combat runtime should not defeat target.");
                AssertApproximately(5f, GetProperty<float>(target, "CurrentHp"), 0.001f, "Hitbox without combat runtime should leave target HP unchanged.");
                AssertEqual(0, GetProperty<int>(hitbox, "HitEventCount"), "Hitbox without combat runtime should not count a landed hit.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
                UnityEngine.Object.DestroyImmediate(hitboxObject);
            }
        }

        private static void TestEnemyDamageSourceRoutesPlayerDamage()
        {
            object cooking = LoadCookingDatabase();
            object buffs = Activator.CreateInstance(RequiredType(CookingBuffServiceTypeName), cooking);
            AssertEqual(true, Invoke<bool>(buffs, "ConsumeDish", "ginseng_chicken_soup", 2), "Defense meal should apply.");

            GameObject playerObject = CreatePlayerObject();
            GameObject damageSourceObject = new GameObject("CombatBridgeSmoke_EnemyDamageSource");
            try
            {
                object player = playerObject.GetComponent(RequiredType(PlayerControllerTypeName));
                Invoke(player, "BindCookingBuffs", buffs);
                object health = Activator.CreateInstance(RequiredType(PlayerHealthRuntimeTypeName));
                Invoke(health, "BindCookingBuffs", buffs);
                Invoke(health, "SetHealth", 200f, 200f);
                object combat = Activator.CreateInstance(RequiredType(CombatRuntimeTypeName), player, health);

                object damageSource = damageSourceObject.AddComponent(RequiredType(EnemyDamageSourceTypeName));
                Invoke(damageSource, "BindCombat", combat);
                Invoke(damageSource, "Configure", 100f);

                object result = Invoke(damageSource, "TryDamagePlayer");
                AssertApproximately(61f, GetProperty<float>(result, "DamageApplied"), 0.001f, "Enemy damage source should route through player damage reduction.");
                AssertApproximately(139f, GetProperty<float>(health, "CurrentHp"), 0.001f, "Enemy damage source should reduce player HP.");
                AssertEqual(1, GetProperty<int>(damageSource, "DamageEventCount"), "Enemy damage source should count successful damage.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(damageSourceObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        private static void TestEnemyDamageSourceMissingCombatNoOps()
        {
            GameObject damageSourceObject = new GameObject("CombatBridgeSmoke_NoCombatDamageSource");
            try
            {
                object damageSource = damageSourceObject.AddComponent(RequiredType(EnemyDamageSourceTypeName));
                Invoke(damageSource, "Configure", 50f);

                object result = Invoke(damageSource, "TryDamagePlayer");
                AssertApproximately(0f, GetProperty<float>(result, "DamageApplied"), 0.001f, "Enemy damage source without combat runtime should not apply damage.");
                AssertEqual(0, GetProperty<int>(damageSource, "DamageEventCount"), "Enemy damage source without combat runtime should not count damage.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(damageSourceObject);
            }
        }

        private static void TestCombatDefeatHandlerDisablesTargetCollidersOnce()
        {
            Type targetBehaviourType = RequiredType(CombatTargetBehaviourTypeName);
            Type defeatHandlerType = RequiredType(CombatDefeatHandlerTypeName);
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                object target = targetObject.AddComponent(targetBehaviourType);
                Invoke(target, "Initialize", 5f);
                object defeatHandler = targetObject.AddComponent(defeatHandlerType);
                Invoke(defeatHandler, "BindTarget", target);

                Collider collider = targetObject.GetComponent<Collider>();
                AssertEqual(true, collider.enabled, "Target collider should start enabled.");

                Invoke(target, "ApplyDamage", 6f);
                AssertEqual(1, GetProperty<int>(defeatHandler, "HandledDefeatCount"), "Defeat handler should observe the first defeat.");
                AssertEqual(false, collider.enabled, "Defeat handler should disable target colliders.");

                Invoke(target, "ApplyDamage", 6f);
                AssertEqual(1, GetProperty<int>(defeatHandler, "HandledDefeatCount"), "Defeat handler should remain one-shot.");
                AssertEqual(1, GetProperty<int>(target, "DefeatEventCount"), "Target defeat event should remain one-shot.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        private static void TestHumanVillageSceneContainsLiveCombatAdapters()
        {
            Type playerAttackHitboxType = RequiredType(PlayerAttackHitboxTypeName);
            Type combatTargetBehaviourType = RequiredType(CombatTargetBehaviourTypeName);
            Type enemyDamageSourceType = RequiredType(EnemyDamageSourceTypeName);
            Type defeatHandlerType = RequiredType(CombatDefeatHandlerTypeName);

            EditorSceneManager.OpenScene(HumanVillageScenePath);

            AssertEqual(true, CountComponents(playerAttackHitboxType) >= 1, "Human Village should mount a player attack hitbox adapter.");
            AssertEqual(true, CountComponents(combatTargetBehaviourType) >= 1, "Human Village should contain at least one live combat target adapter.");
            AssertEqual(true, CountComponents(enemyDamageSourceType) >= 1, "Human Village should contain at least one enemy damage-source adapter.");
            AssertEqual(true, CountComponents(defeatHandlerType) >= 1, "Human Village combat target should mount a defeat handler.");
        }

        private static GameObject CreatePlayerObject()
        {
            Type playerControllerType = RequiredType(PlayerControllerTypeName);
            GameObject player = new GameObject("CombatBridgeSmoke_Player");
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

        private static T GetProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                throw new Exception($"Missing property {target.GetType().FullName}.{propertyName}");
            }

            return (T)property.GetValue(target);
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
