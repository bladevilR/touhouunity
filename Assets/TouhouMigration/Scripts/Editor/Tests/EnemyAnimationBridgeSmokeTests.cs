using System;
using System.Reflection;
using TouhouMigration.Runtime.Combat;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class EnemyAnimationBridgeSmokeTests
    {
        private const string AnimationBridgeTypeName = "TouhouMigration.Runtime.Combat.MigrationEnemyAnimationBridge, Assembly-CSharp";
        private const string EnemyPrefabsRoot = "Assets/TouhouMigration/Prefabs/Enemies";

        [MenuItem("Touhou Migration/Tests/Run Enemy Animation Bridge Smoke Tests")]
        public static void RunAll()
        {
            TestBridgeTypeExists();
            TestBridgeMapsEnemyControllerStateAndAttackEvents();
            TestBridgeMapsStunnedControllerStateToStationaryHitReaction();
            TestGeneratedAnimatedEnemyPrefabHasBridge();
        }

        private static void TestBridgeTypeExists()
        {
            Type bridgeType = Type.GetType(AnimationBridgeTypeName);
            AssertEqual(true, bridgeType != null, "Runtime enemy animation bridge type should exist.");
        }

        private static void TestBridgeMapsEnemyControllerStateAndAttackEvents()
        {
            Type bridgeType = RequiredType(AnimationBridgeTypeName);
            GameObject enemyObject = new GameObject("EnemyAnimationBridgeSmoke_Enemy");
            GameObject damageObject = new GameObject("EnemyAnimationBridgeSmoke_DamageSource");
            try
            {
                MigrationCombatTargetBehaviour target = enemyObject.AddComponent<MigrationCombatTargetBehaviour>();
                target.Initialize(10f);

                MigrationSimpleEnemyController enemy = enemyObject.AddComponent<MigrationSimpleEnemyController>();
                enemy.BindTarget(target);
                enemy.ConfigureMovement(5f, 1.5f, 2f);
                enemy.ConfigureAttackCooldown(0.4f);

                damageObject.transform.SetParent(enemyObject.transform);
                MigrationEnemyDamageSource damageSource = damageObject.AddComponent<MigrationEnemyDamageSource>();
                enemy.BindDamageSource(damageSource);

                Component bridge = enemyObject.AddComponent(bridgeType);
                Invoke(bridge, "BindController", enemy);
                Invoke(bridge, "SyncNow");
                AssertEqual("Idle", GetProperty<string>(bridge, "LastAnimationState"), "Bridge should map the initial idle controller state to the Idle animator state.");
                AssertEqual(0, GetProperty<int>(bridge, "LastMotionState"), "Idle should use motion state 0.");
                AssertEqual(false, GetProperty<bool>(bridge, "IsMoving"), "Idle should not mark the animator as moving.");

                enemy.Tick(0.5f, new Vector3(4f, 0f, 0f));
                Invoke(bridge, "SyncNow");
                AssertEqual("Move", GetProperty<string>(bridge, "LastAnimationState"), "Bridge should map chase movement to the Move animator state.");
                AssertEqual(1, GetProperty<int>(bridge, "LastMotionState"), "Movement states should use motion state 1.");
                AssertEqual(true, GetProperty<bool>(bridge, "IsMoving"), "Movement states should mark the animator as moving.");

                enemy.Tick(0.5f, new Vector3(1.2f, 0f, 0f));
                AssertEqual("Attack", GetProperty<string>(bridge, "LastAnimationState"), "Bridge should switch to Attack when the controller performs a melee attack.");
                AssertEqual(1, GetProperty<int>(bridge, "AttackTriggerCount"), "Bridge should count melee attack triggers.");

                target.ApplyDamage(2f);
                AssertEqual("TakeDamage", GetProperty<string>(bridge, "LastAnimationState"), "Bridge should map non-lethal damage to the TakeDamage animator state.");
                AssertEqual(1, GetProperty<int>(bridge, "TakeDamageTriggerCount"), "Bridge should count non-lethal damage triggers.");

                target.ApplyDamage(20f);
                AssertEqual("Die", GetProperty<string>(bridge, "LastAnimationState"), "Bridge should map defeated enemies to the Die animator state.");
                AssertEqual(2, GetProperty<int>(bridge, "LastMotionState"), "Defeated state should use motion state 2.");
                AssertEqual(2, GetProperty<int>(bridge, "TakeDamageTriggerCount"), "Lethal damage should still emit damage feedback before death.");
                AssertEqual(1, GetProperty<int>(bridge, "DeathTriggerCount"), "Bridge should count death triggers once.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(damageObject);
                UnityEngine.Object.DestroyImmediate(enemyObject);
            }
        }

        private static void TestBridgeMapsStunnedControllerStateToStationaryHitReaction()
        {
            Type bridgeType = RequiredType(AnimationBridgeTypeName);
            GameObject enemyObject = new GameObject("EnemyAnimationBridgeSmoke_StunnedEnemy");
            try
            {
                MigrationCombatTargetBehaviour target = enemyObject.AddComponent<MigrationCombatTargetBehaviour>();
                target.Initialize(10f);

                MigrationSimpleEnemyController enemy = enemyObject.AddComponent<MigrationSimpleEnemyController>();
                enemy.BindTarget(target);

                Component bridge = enemyObject.AddComponent(bridgeType);
                Invoke(bridge, "BindController", enemy);

                enemy.ApplyStun(1.2f);
                AssertEqual("stunned", enemy.CurrentState, "Enemy controller should expose the stun state.");
                AssertEqual("TakeDamage", GetProperty<string>(bridge, "LastAnimationState"), "Bridge should map stun to the available stationary hit-reaction animation.");
                AssertEqual(0, GetProperty<int>(bridge, "LastMotionState"), "Stun should not use movement motion state.");
                AssertEqual(false, GetProperty<bool>(bridge, "IsMoving"), "Stun should stop movement animation.");
                AssertEqual(1, GetProperty<int>(bridge, "TakeDamageTriggerCount"), "Stun should trigger one hit-reaction animation event.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemyObject);
            }
        }

        private static void TestGeneratedAnimatedEnemyPrefabHasBridge()
        {
            Type bridgeType = RequiredType(AnimationBridgeTypeName);
            GameObject prefab = RequiredPrefab("bat");
            Component bridge = prefab.GetComponent(bridgeType);
            AssertEqual(true, bridge != null, "Generated animated enemy prefabs should carry the runtime animation bridge.");
            AssertEqual(false, GetProperty<bool>(bridge, "UsesFallbackAnimation"), "Bat should not use fallback animation bridge wiring.");
        }

        private static GameObject RequiredPrefab(string monsterId)
        {
            string path = $"{EnemyPrefabsRoot}/MigrationEnemy_{ToPascal(monsterId)}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new Exception($"Missing enemy prefab for '{monsterId}' at {path}.");
            }

            return prefab;
        }

        private static string ToPascal(string value)
        {
            string[] parts = value.Split('_', StringSplitOptions.RemoveEmptyEntries);
            string result = string.Empty;
            foreach (string part in parts)
            {
                result += char.ToUpperInvariant(part[0]) + part.Substring(1);
            }

            return result;
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

        private static void Invoke(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
            {
                throw new Exception($"Missing method {target.GetType().FullName}.{methodName}");
            }

            method.Invoke(target, args);
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
