using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class CombatActionRewardSmokeTests
    {
        private const string PlayerControllerTypeName = "TouhouMigration.Runtime.Player.MigrationPlayerController, Assembly-CSharp";
        private const string PlayerHealthRuntimeTypeName = "TouhouMigration.Runtime.Player.MigrationPlayerHealthRuntime, Assembly-CSharp";
        private const string PlayerProgressTypeName = "TouhouMigration.Runtime.Player.MigrationPlayerProgressService, Assembly-CSharp";
        private const string CombatRuntimeTypeName = "TouhouMigration.Runtime.Combat.MigrationCombatRuntime, Assembly-CSharp";
        private const string CombatTargetBehaviourTypeName = "TouhouMigration.Runtime.Combat.MigrationCombatTargetBehaviour, Assembly-CSharp";
        private const string PlayerAttackHitboxTypeName = "TouhouMigration.Runtime.Combat.MigrationPlayerAttackHitbox, Assembly-CSharp";
        private const string PlayerCombatActionControllerTypeName = "TouhouMigration.Runtime.Combat.MigrationPlayerCombatActionController, Assembly-CSharp";
        private const string CombatDefeatRewardHandlerTypeName = "TouhouMigration.Runtime.Combat.MigrationCombatDefeatRewardHandler, Assembly-CSharp";
        private const string QuestDeliveryServiceTypeName = "TouhouMigration.Runtime.Social.QuestDeliveryService, Assembly-CSharp";
        private const string HumanVillageScenePath = "Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity";

        [MenuItem("Touhou Migration/Tests/Run Combat Action Reward Smoke Tests")]
        public static void RunAll()
        {
            TestActionControllerDrivesLightAndHeavyAttackWindows();
            TestDefeatRewardHandlerGrantsProgressAndQuestCounterOnce();
            TestHumanVillageSceneContainsActionAndRewardAdapters();
            Debug.Log("Combat action reward smoke tests passed.");
        }

        private static void TestActionControllerDrivesLightAndHeavyAttackWindows()
        {
            GameObject playerObject = CreatePlayerObject();
            GameObject hitboxObject = new GameObject("CombatActionSmoke_Hitbox");
            GameObject actionObject = new GameObject("CombatActionSmoke_ActionController");
            GameObject targetObject = new GameObject("CombatActionSmoke_Target");
            try
            {
                object player = playerObject.GetComponent(RequiredType(PlayerControllerTypeName));
                object health = Activator.CreateInstance(RequiredType(PlayerHealthRuntimeTypeName));
                object combat = Activator.CreateInstance(RequiredType(CombatRuntimeTypeName), player, health);

                object hitbox = hitboxObject.AddComponent(RequiredType(PlayerAttackHitboxTypeName));
                Invoke(hitbox, "BindCombat", combat);

                object action = actionObject.AddComponent(RequiredType(PlayerCombatActionControllerTypeName));
                Invoke(action, "BindAttackHitbox", hitbox);
                Invoke(action, "ConfigureDamage", 7f, 15f);

                object target = targetObject.AddComponent(RequiredType(CombatTargetBehaviourTypeName));
                Invoke(target, "Initialize", 30f);

                Invoke(action, "TriggerLightAttack");
                AssertEqual(true, GetProperty<bool>(action, "IsAttacking"), "Light attack should enter an attack window.");
                AssertEqual("light", GetProperty<string>(action, "CurrentAttackType"), "Light attack should configure light attack type.");
                AssertEqual(1, GetProperty<int>(action, "AttackWindowCount"), "Light attack should count one opened window.");
                AssertEqual(true, GetProperty<bool>(hitbox, "IsAttackWindowActive"), "Light attack should open the hitbox window.");
                object lightHit = Invoke(hitbox, "TryHit", target);
                AssertApproximately(7f, GetProperty<float>(lightHit, "DamageApplied"), 0.001f, "Light action damage should route through the hitbox.");

                Invoke(action, "CompleteAttackWindow");
                AssertEqual(false, GetProperty<bool>(action, "IsAttacking"), "Completing attack should exit action state.");
                AssertEqual(false, GetProperty<bool>(hitbox, "IsAttackWindowActive"), "Completing attack should close the hitbox window.");

                Invoke(action, "TriggerHeavyAttack");
                AssertEqual("heavy", GetProperty<string>(action, "CurrentAttackType"), "Heavy attack should configure heavy attack type.");
                AssertEqual(2, GetProperty<int>(action, "AttackWindowCount"), "Heavy attack should count a second opened window.");
                object heavyHit = Invoke(hitbox, "TryHit", target);
                AssertApproximately(15f, GetProperty<float>(heavyHit, "DamageApplied"), 0.001f, "Heavy action damage should route through the hitbox.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
                UnityEngine.Object.DestroyImmediate(actionObject);
                UnityEngine.Object.DestroyImmediate(hitboxObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        private static void TestDefeatRewardHandlerGrantsProgressAndQuestCounterOnce()
        {
            Type targetBehaviourType = RequiredType(CombatTargetBehaviourTypeName);
            Type rewardHandlerType = RequiredType(CombatDefeatRewardHandlerTypeName);
            object progress = Activator.CreateInstance(RequiredType(PlayerProgressTypeName));
            object quests = Activator.CreateInstance(RequiredType(QuestDeliveryServiceTypeName));

            GameObject targetObject = new GameObject("CombatActionSmoke_RewardTarget");
            try
            {
                object target = targetObject.AddComponent(targetBehaviourType);
                Invoke(target, "Initialize", 5f);
                object rewardHandler = targetObject.AddComponent(rewardHandlerType);
                Invoke(rewardHandler, "BindTarget", target);
                Invoke(rewardHandler, "BindRewards", progress, quests);
                Invoke(rewardHandler, "ConfigureRewards", 12, 3, "enemy_killed");

                Invoke(target, "ApplyDamage", 6f);
                AssertEqual(1, GetProperty<int>(rewardHandler, "RewardGrantCount"), "Defeat rewards should grant once on first defeat.");
                AssertEqual(12, GetProperty<int>(progress, "Experience"), "Defeat rewards should grant enemy XP to player progress.");
                AssertEqual(3, GetProperty<int>(progress, "Coins"), "Defeat rewards should grant enemy coins to player progress.");
                AssertEqual(1, GetProperty<int>(progress, "TotalKills"), "Defeat rewards should increment player kill count.");
                AssertEqual(1, Invoke<int>(quests, "GetCounter", "enemy_killed"), "Defeat rewards should increment quest kill counter.");

                Invoke(target, "ApplyDamage", 6f);
                AssertEqual(1, GetProperty<int>(rewardHandler, "RewardGrantCount"), "Defeat rewards should not grant twice.");
                AssertEqual(12, GetProperty<int>(progress, "Experience"), "Duplicate defeat should not grant duplicate XP.");
                AssertEqual(3, GetProperty<int>(progress, "Coins"), "Duplicate defeat should not grant duplicate coins.");
                AssertEqual(1, GetProperty<int>(progress, "TotalKills"), "Duplicate defeat should not increment kills twice.");
                AssertEqual(1, Invoke<int>(quests, "GetCounter", "enemy_killed"), "Duplicate defeat should not increment quest counter twice.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        private static void TestHumanVillageSceneContainsActionAndRewardAdapters()
        {
            Type actionControllerType = RequiredType(PlayerCombatActionControllerTypeName);
            Type rewardHandlerType = RequiredType(CombatDefeatRewardHandlerTypeName);

            EditorSceneManager.OpenScene(HumanVillageScenePath);

            AssertEqual(true, CountComponents(actionControllerType) >= 1, "Human Village player should mount a combat action controller.");
            AssertEqual(true, CountComponents(rewardHandlerType) >= 1, "Human Village combat target should mount a defeat reward handler.");
        }

        private static GameObject CreatePlayerObject()
        {
            Type playerControllerType = RequiredType(PlayerControllerTypeName);
            GameObject player = new GameObject("CombatActionSmoke_Player");
            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 1f, 0f);
            player.AddComponent(playerControllerType);
            return player;
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
