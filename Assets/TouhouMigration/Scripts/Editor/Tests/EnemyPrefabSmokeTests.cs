using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class EnemyPrefabSmokeTests
    {
        private const string PlayerControllerTypeName = "TouhouMigration.Runtime.Player.MigrationPlayerController, Assembly-CSharp";
        private const string PlayerHealthRuntimeTypeName = "TouhouMigration.Runtime.Player.MigrationPlayerHealthRuntime, Assembly-CSharp";
        private const string CombatRuntimeTypeName = "TouhouMigration.Runtime.Combat.MigrationCombatRuntime, Assembly-CSharp";
        private const string CombatTargetBehaviourTypeName = "TouhouMigration.Runtime.Combat.MigrationCombatTargetBehaviour, Assembly-CSharp";
        private const string EnemyDamageSourceTypeName = "TouhouMigration.Runtime.Combat.MigrationEnemyDamageSource, Assembly-CSharp";
        private const string CombatLootDropHandlerTypeName = "TouhouMigration.Runtime.Combat.MigrationCombatLootDropHandler, Assembly-CSharp";
        private const string SimpleEnemyControllerTypeName = "TouhouMigration.Runtime.Combat.MigrationSimpleEnemyController, Assembly-CSharp";
        private const string EnemyVariantProfileTypeName = "TouhouMigration.Runtime.Combat.MigrationEnemyVariantProfile, Assembly-CSharp";
        private const string EnemyCatalogTypeName = "TouhouMigration.Runtime.Combat.MigrationEnemyCatalog, Assembly-CSharp";
        private const string EnemyProjectileTypeName = "TouhouMigration.Runtime.Combat.MigrationEnemyProjectile, Assembly-CSharp";
        private const string ItemDatabaseTypeName = "TouhouMigration.Runtime.Inventory.ItemDatabase, Assembly-CSharp";
        private const string InventoryServiceTypeName = "TouhouMigration.Runtime.Inventory.InventoryService, Assembly-CSharp";
        private const string ItemDataPath = "Assets/TouhouMigration/Data/Items/items.json";
        private const string HumanVillageScenePath = "Assets/TouhouMigration/Scenes/HumanVillageVerticalSlice.unity";

        [MenuItem("Touhou Migration/Tests/Run Enemy Prefab Smoke Tests")]
        public static void RunAll()
        {
            TestSimpleEnemyControllerChasesAttacksAndStopsOnDefeat();
            TestVariantProfileConfiguresStatsLootAndAttackWindup();
            TestEnemyCatalogProvidesFormalGodotMonsterVariants();
            TestRangedVariantMaintainsDistanceAndFiresProjectile();
            TestHumanVillageSceneContainsReusableEnemyController();
            TestHumanVillageSceneContainsCatalogRangedEnemy();
        }

        private static void TestSimpleEnemyControllerChasesAttacksAndStopsOnDefeat()
        {
            GameObject playerObject = CreatePlayerObject();
            GameObject enemyObject = new GameObject("EnemyPrefabSmoke_FairyScout");
            GameObject damageObject = new GameObject("EnemyPrefabSmoke_DamageSource");
            try
            {
                object player = playerObject.GetComponent(RequiredType(PlayerControllerTypeName));
                object health = Activator.CreateInstance(RequiredType(PlayerHealthRuntimeTypeName));
                Invoke(health, "SetHealth", 100f, 100f);
                object combat = Activator.CreateInstance(RequiredType(CombatRuntimeTypeName), player, health);

                enemyObject.transform.position = Vector3.zero;
                object target = enemyObject.AddComponent(RequiredType(CombatTargetBehaviourTypeName));
                Invoke(target, "Initialize", 10f);

                damageObject.transform.SetParent(enemyObject.transform);
                object damageSource = damageObject.AddComponent(RequiredType(EnemyDamageSourceTypeName));
                Invoke(damageSource, "BindCombat", combat);
                Invoke(damageSource, "Configure", 8f);

                object enemy = enemyObject.AddComponent(RequiredType(SimpleEnemyControllerTypeName));
                Invoke(enemy, "BindTarget", target);
                Invoke(enemy, "BindDamageSource", damageSource);
                Invoke(enemy, "ConfigureMovement", 5f, 1.5f, 2f);
                Invoke(enemy, "ConfigureAttackCooldown", 0.4f);

                Invoke(enemy, "Tick", 0.5f, new Vector3(8f, 0f, 0f));
                AssertEqual("idle", GetProperty<string>(enemy, "CurrentState"), "Enemy should stay idle outside chase range.");
                AssertApproximately(0f, enemyObject.transform.position.x, 0.001f, "Idle enemy should not move.");

                Invoke(enemy, "Tick", 0.5f, new Vector3(4f, 0f, 0f));
                AssertEqual("chase", GetProperty<string>(enemy, "CurrentState"), "Enemy should chase inside chase range but outside attack range.");
                AssertApproximately(1f, enemyObject.transform.position.x, 0.001f, "Enemy should move toward the player while chasing.");

                Invoke(enemy, "Tick", 0.5f, new Vector3(1.2f, 0f, 0f));
                AssertEqual("attack", GetProperty<string>(enemy, "CurrentState"), "Enemy should attack inside attack range.");
                AssertEqual(1, GetProperty<int>(enemy, "AttackEventCount"), "Enemy should count a successful attack tick.");
                AssertEqual(1, GetProperty<int>(damageSource, "DamageEventCount"), "Enemy attack should route through the damage source.");
                AssertApproximately(92f, GetProperty<float>(health, "CurrentHp"), 0.001f, "Enemy attack should damage the player health runtime.");

                Invoke(target, "ApplyDamage", 12f);
                AssertEqual("defeated", GetProperty<string>(enemy, "CurrentState"), "Enemy should enter defeated state when target is defeated.");
                Invoke(enemy, "Tick", 1f, new Vector3(1f, 0f, 0f));
                AssertEqual(1, GetProperty<int>(enemy, "AttackEventCount"), "Defeated enemy should not keep attacking.");
                AssertEqual(1, GetProperty<int>(damageSource, "DamageEventCount"), "Defeated enemy should not keep damaging the player.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(damageObject);
                UnityEngine.Object.DestroyImmediate(enemyObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        private static void TestVariantProfileConfiguresStatsLootAndAttackWindup()
        {
            object itemDatabase = LoadItemDatabase();
            object inventory = Activator.CreateInstance(RequiredType(InventoryServiceTypeName), itemDatabase, 48);
            GameObject playerObject = CreatePlayerObject();
            GameObject enemyObject = new GameObject("EnemyPrefabSmoke_BeastVariant");
            GameObject damageObject = new GameObject("EnemyPrefabSmoke_BeastVariantDamageSource");
            try
            {
                object player = playerObject.GetComponent(RequiredType(PlayerControllerTypeName));
                object health = Activator.CreateInstance(RequiredType(PlayerHealthRuntimeTypeName));
                Invoke(health, "SetHealth", 100f, 100f);
                object combat = Activator.CreateInstance(RequiredType(CombatRuntimeTypeName), player, health);

                object target = enemyObject.AddComponent(RequiredType(CombatTargetBehaviourTypeName));
                Invoke(target, "Initialize", 10f);

                damageObject.transform.SetParent(enemyObject.transform);
                object damageSource = damageObject.AddComponent(RequiredType(EnemyDamageSourceTypeName));
                Invoke(damageSource, "BindCombat", combat);

                object lootHandler = enemyObject.AddComponent(RequiredType(CombatLootDropHandlerTypeName));
                Invoke(lootHandler, "BindTarget", target);
                Invoke(lootHandler, "BindServices", inventory, null);

                object enemy = enemyObject.AddComponent(RequiredType(SimpleEnemyControllerTypeName));
                Invoke(enemy, "BindTarget", target);
                Invoke(enemy, "BindDamageSource", damageSource);
                Invoke(enemy, "BindLootDropHandler", lootHandler);

                object profile = Activator.CreateInstance(RequiredType(EnemyVariantProfileTypeName));
                Invoke(
                    profile,
                    "Configure",
                    "beast_scout",
                    "beast",
                    "ice_enemy",
                    40f,
                    7f,
                    1.4f,
                    3f,
                    14f,
                    0.5f,
                    0.25f,
                    true);

                Invoke(enemy, "ApplyVariant", profile);

                AssertEqual("beast_scout", GetProperty<string>(enemy, "CurrentVariantId"), "Enemy controller should expose the active variant id.");
                AssertApproximately(40f, GetProperty<float>(target, "MaxHp"), 0.001f, "Variant profile should configure target max HP.");
                AssertApproximately(40f, GetProperty<float>(target, "CurrentHp"), 0.001f, "Variant profile should reset current HP.");

                Invoke(enemy, "Tick", 0.1f, new Vector3(1.2f, 0f, 0f));
                AssertEqual("windup", GetProperty<string>(enemy, "CurrentState"), "Variant attack should enter windup before applying damage.");
                AssertEqual(1, GetProperty<int>(enemy, "WindupEventCount"), "Variant windup should be counted once per attack cycle.");
                AssertEqual(0, GetProperty<int>(enemy, "AttackEventCount"), "Enemy should not damage during windup start.");
                AssertEqual(0, GetProperty<int>(damageSource, "DamageEventCount"), "Damage source should not fire during windup start.");
                AssertApproximately(100f, GetProperty<float>(health, "CurrentHp"), 0.001f, "Player HP should not change before active attack.");

                Invoke(enemy, "Tick", 0.2f, new Vector3(1.2f, 0f, 0f));
                AssertEqual("attack", GetProperty<string>(enemy, "CurrentState"), "Variant attack should become active after windup.");
                AssertEqual(1, GetProperty<int>(enemy, "AttackEventCount"), "Enemy should attack once after windup.");
                AssertEqual(1, GetProperty<int>(damageSource, "DamageEventCount"), "Damage source should fire after windup.");
                AssertApproximately(86f, GetProperty<float>(health, "CurrentHp"), 0.001f, "Variant attack damage should route through player health.");

                Invoke(target, "ApplyDamage", 50f);
                AssertEqual(1, Invoke<int>(inventory, "GetItemCount", "beast_meat"), "Variant enemy type should configure beast loot.");
                AssertEqual(1, Invoke<int>(inventory, "GetItemCount", "element_crystal_ice"), "Variant elemental group should configure ice loot.");
                AssertEqual(1, Invoke<int>(inventory, "GetItemCount", "dungeon_compost"), "Forced loot tables should include fertilizer drops.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(damageObject);
                UnityEngine.Object.DestroyImmediate(enemyObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        private static void TestEnemyCatalogProvidesFormalGodotMonsterVariants()
        {
            object catalog = Activator.CreateInstance(RequiredType(EnemyCatalogTypeName));
            Invoke(catalog, "LoadGodotDefaults");

            AssertEqual(20, GetProperty<int>(catalog, "Count"), "Enemy catalog should mirror the formal Godot MonsterDatabase count.");

            object bat = Invoke<object>(catalog, "GetProfile", "bat");
            AssertEqual("bat", GetProperty<string>(bat, "VariantId"), "Catalog should preserve the Godot monster id as variant id.");
            AssertEqual("蝙蝠", GetProperty<string>(bat, "DisplayName"), "Catalog should preserve Godot display names.");
            AssertEqual("fly", GetProperty<string>(bat, "MoveStyle"), "Catalog should preserve Godot move style.");
            AssertEqual("res://scenes/monsters/BatMonster.tscn", GetProperty<string>(bat, "GodotScenePath"), "Catalog should preserve Godot monster scene paths.");
            AssertEqual(true, GetProperty<bool>(bat, "CanMelee"), "Bat should preserve Godot melee capability.");
            AssertEqual(true, GetProperty<bool>(bat, "CanShoot"), "Bat should preserve Godot ranged capability.");
            AssertEqual(12, GetProperty<int>(bat, "XpValue"), "Bat should preserve Godot XP value.");
            AssertApproximately(45f, GetProperty<float>(bat, "MaxHp"), 0.001f, "Bat should preserve Godot HP.");
            AssertApproximately(12f, GetProperty<float>(bat, "AttackDamage"), 0.001f, "Bat should preserve Godot damage.");
            AssertApproximately(1.8f, GetProperty<float>(bat, "MoveSpeed"), 0.001f, "Bat should preserve Godot speed.");
            AssertApproximately(1.0f, GetProperty<float>(bat, "FloatHeight"), 0.001f, "Bat should preserve Godot float height.");
            AssertApproximately(8.0f, GetProperty<float>(bat, "ProjectileSpeed"), 0.001f, "Ranged catalog entries should use Enemy3D's projectile speed.");

            object egg = Invoke<object>(catalog, "GetProfile", "egg");
            AssertEqual(false, GetProperty<bool>(egg, "CanMelee"), "Egg should preserve Godot non-melee capability.");
            AssertEqual(false, GetProperty<bool>(egg, "CanShoot"), "Egg should preserve Godot non-ranged capability.");
            AssertApproximately(100f, GetProperty<float>(egg, "MaxHp"), 0.001f, "Egg should preserve Godot high HP.");
        }

        private static void TestRangedVariantMaintainsDistanceAndFiresProjectile()
        {
            GameObject playerObject = CreatePlayerObject();
            GameObject enemyObject = new GameObject("EnemyPrefabSmoke_BatRangedVariant");
            Type projectileType = null;
            try
            {
                object catalog = Activator.CreateInstance(RequiredType(EnemyCatalogTypeName));
                Invoke(catalog, "LoadGodotDefaults");
                object batProfile = Invoke<object>(catalog, "GetProfile", "bat");
                projectileType = RequiredType(EnemyProjectileTypeName);

                object player = playerObject.GetComponent(RequiredType(PlayerControllerTypeName));
                object health = Activator.CreateInstance(RequiredType(PlayerHealthRuntimeTypeName));
                Invoke(health, "SetHealth", 100f, 100f);
                object combat = Activator.CreateInstance(RequiredType(CombatRuntimeTypeName), player, health);

                enemyObject.transform.position = Vector3.zero;
                object target = enemyObject.AddComponent(RequiredType(CombatTargetBehaviourTypeName));
                Invoke(target, "Initialize", 10f);

                object enemy = enemyObject.AddComponent(RequiredType(SimpleEnemyControllerTypeName));
                Invoke(enemy, "BindTarget", target);
                Invoke(enemy, "BindCombat", combat);
                Invoke(enemy, "ApplyVariant", batProfile);

                Invoke(enemy, "Tick", 0.1f, new Vector3(7f, 0f, 0f));
                AssertEqual("windup", GetProperty<string>(enemy, "CurrentState"), "Ranged variant should use Godot attack windup before firing.");
                AssertEqual(0, GetProperty<int>(enemy, "ProjectileEventCount"), "Ranged variant should not spawn a projectile during windup.");
                AssertEqual(0, GetProperty<int>(enemy, "AttackEventCount"), "Ranged variant should not use melee attack counters for projectile fire.");
                AssertApproximately(100f, GetProperty<float>(health, "CurrentHp"), 0.001f, "Projectile fire should not damage the player until the projectile reaches them.");

                Invoke(enemy, "Tick", 0.4f, new Vector3(7f, 0f, 0f));
                AssertEqual("ranged_attack", GetProperty<string>(enemy, "CurrentState"), "Ranged variant should fire after Godot attack windup.");
                AssertEqual(1, GetProperty<int>(enemy, "ProjectileEventCount"), "Ranged variant should spawn one projectile when it attacks.");

                object projectile = FindSingleComponent(projectileType);
                AssertApproximately(8f, GetProperty<float>(projectile, "Speed"), 0.001f, "Enemy3D ranged projectile speed should be preserved.");
                AssertApproximately(12f, GetProperty<float>(projectile, "Damage"), 0.001f, "Projectile should inherit the ranged enemy's damage.");
                AssertEqual(true, GetProperty<bool>(projectile, "IsEnemyProjectile"), "Spawned projectiles should be tagged as enemy projectiles.");

                Invoke(projectile, "Tick", 0.875f, new Vector3(7f, 0f, 0f));
                AssertEqual(1, GetProperty<int>(projectile, "HitEventCount"), "Projectile should count the player hit once.");
                AssertApproximately(88f, GetProperty<float>(health, "CurrentHp"), 0.001f, "Enemy projectile should route damage through player health runtime.");

                enemyObject.transform.position = Vector3.zero;
                Invoke(enemy, "Tick", 0.5f, new Vector3(3f, 0f, 0f));
                AssertEqual("ranged_reposition", GetProperty<string>(enemy, "CurrentState"), "Ranged variant should keep distance when the player is too close.");
                AssertEqual(true, enemyObject.transform.position.x < -0.01f, "Ranged variant should move away from a too-close player.");
            }
            finally
            {
                if (projectileType != null)
                {
                    DestroyObjectsWithComponent(projectileType);
                }

                UnityEngine.Object.DestroyImmediate(enemyObject);
                UnityEngine.Object.DestroyImmediate(playerObject);
            }
        }

        private static void TestHumanVillageSceneContainsReusableEnemyController()
        {
            Type enemyControllerType = RequiredType(SimpleEnemyControllerTypeName);
            EditorSceneManager.OpenScene(HumanVillageScenePath);

            AssertEqual(true, CountComponents(enemyControllerType) >= 1, "Human Village should contain at least one reusable enemy controller.");
        }

        private static void TestHumanVillageSceneContainsCatalogRangedEnemy()
        {
            Type enemyControllerType = RequiredType(SimpleEnemyControllerTypeName);
            EditorSceneManager.OpenScene(HumanVillageScenePath);

            GameObject batScout = GameObject.Find("MigrationEnemy_BatScout");
            AssertEqual(true, batScout != null, "Human Village should contain a catalog-backed ranged bat enemy.");
            object enemy = batScout.GetComponent(enemyControllerType);
            AssertEqual(true, enemy != null, "Catalog-backed bat enemy should mount the reusable enemy controller.");
            AssertEqual("bat", GetProperty<string>(enemy, "CurrentVariantId"), "Catalog-backed bat enemy should serialize the formal Godot monster id.");
        }

        private static GameObject CreatePlayerObject()
        {
            Type playerControllerType = RequiredType(PlayerControllerTypeName);
            GameObject player = new GameObject("EnemyPrefabSmoke_Player");
            player.tag = "Player";
            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 1f, 0f);
            player.AddComponent(playerControllerType);
            return player;
        }

        private static object LoadItemDatabase()
        {
            object database = Activator.CreateInstance(RequiredType(ItemDatabaseTypeName));
            AssertEqual(true, Invoke<bool>(database, "LoadFromPath", ItemDataPath), "Item database should load Godot items JSON.");
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

        private static object FindSingleComponent(Type componentType)
        {
            object result = null;
            foreach (GameObject gameObject in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                object component = gameObject.GetComponent(componentType);
                if (component == null)
                {
                    continue;
                }

                if (result != null)
                {
                    throw new Exception($"Expected one component of type {componentType.FullName}, found multiple.");
                }

                result = component;
            }

            if (result == null)
            {
                throw new Exception($"Expected one component of type {componentType.FullName}, found none.");
            }

            return result;
        }

        private static void DestroyObjectsWithComponent(Type componentType)
        {
            foreach (GameObject gameObject in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (gameObject.GetComponent(componentType) != null)
                {
                    UnityEngine.Object.DestroyImmediate(gameObject);
                }
            }
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
