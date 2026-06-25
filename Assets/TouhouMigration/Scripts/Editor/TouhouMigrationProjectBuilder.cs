using System.Collections.Generic;
using System.IO;
using System.Threading;
using TouhouMigration.Runtime.Bootstrap;
using TouhouMigration.Runtime.Combat;
using TouhouMigration.Runtime.Cooking;
using TouhouMigration.Runtime.Data;
using TouhouMigration.Runtime.Economy;
using TouhouMigration.Runtime.Farming;
using TouhouMigration.Runtime.Fishing;
using TouhouMigration.Runtime.Foundation;
using TouhouMigration.Runtime.Home;
using TouhouMigration.Runtime.Player;
using TouhouMigration.Runtime.Services;
using TouhouMigration.Runtime.Settings;
using TouhouMigration.Runtime.Social;
using TouhouMigration.Runtime.UI;
using TouhouMigration.Runtime.UI.CardBuild;
using TouhouMigration.Runtime.UI.Dialogue;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TouhouMigration.Editor
{
    public static class TouhouMigrationProjectBuilder
    {
        private const string Root = "Assets/TouhouMigration";
        private const string ScenesRoot = Root + "/Scenes";
        private const string BootstrapScenePath = ScenesRoot + "/Bootstrap.unity";
        private const string TitleScreenScenePath = ScenesRoot + "/TitleScreen.unity";
        private const string BambooHomeScenePath = ScenesRoot + "/BambooHomeVerticalSlice.unity";
        private const string HumanVillageScenePath = ScenesRoot + "/HumanVillageVerticalSlice.unity";
        private const string MokouCharacterValidationScenePath = ScenesRoot + "/MokouCharacterValidation.unity";
        private const string PureNatureMeadowsScenePath = ScenesRoot + "/PureNatureMeadows.unity";
        private const string EnemyPrefabsRoot = Root + "/Prefabs/Enemies";
        private const string CombatFeedbackPrefabsRoot = Root + "/Prefabs/CombatFeedback";
        private const string EncounterPrefabsRoot = Root + "/Prefabs/Encounters";
        private const string CombatDataRoot = Root + "/Data/Combat";
        private const string PerfectFreezeDataRoot = CombatDataRoot + "/PerfectFreeze";
        private const string EnemyProjectileFeedbackPrefabPath = CombatFeedbackPrefabsRoot + "/MigrationEnemyProjectileFeedback.prefab";
        private const string IceOrbProjectileFeedbackPrefabPath = CombatFeedbackPrefabsRoot + "/MigrationIceOrbProjectileFeedback.prefab";
        private const string IceShardProjectileFeedbackPrefabPath = CombatFeedbackPrefabsRoot + "/MigrationIceShardProjectileFeedback.prefab";
        private const string IceLanceProjectileFeedbackPrefabPath = CombatFeedbackPrefabsRoot + "/MigrationIceLanceProjectileFeedback.prefab";
        private const string PerfectFreezeProjectileFeedbackPrefabPath = CombatFeedbackPrefabsRoot + "/MigrationPerfectFreezeProjectileFeedback.prefab";
        private const string PerfectFreezeEncounterPrefabPath = EncounterPrefabsRoot + "/MigrationPerfectFreezeEncounter.prefab";
        private const string PerfectFreezePhasePlanPath = PerfectFreezeDataRoot + "/MigrationPerfectFreezePhasePlan.asset";
        private const string MeleeDangerFeedbackPrefabPath = CombatFeedbackPrefabsRoot + "/MigrationMeleeDangerFeedback.prefab";
        private const string EnemyDeathFeedbackPrefabPath = CombatFeedbackPrefabsRoot + "/MigrationEnemyDeathFeedback.prefab";
        private const string MainMenuBackgroundPath = Root + "/Art/UI/Backgrounds/main_menu_bg.png";
        private const string TitleFontPath = Root + "/Art/Fonts/MaShanZheng-Regular.ttf";
        private const string BambooHousePath = Root + "/Art/BambooHome/House/20260113162802_ac31bd73.glb";
        private const string BambooRocksPath = Root + "/Art/BambooHome/Props/rocks.glb";
        private const string BambooShootsPath = Root + "/Art/BambooHome/Props/bamboo_shoots.glb";
        private const string BambooLanternPath = Root + "/Art/BambooHome/Props/lantern.glb";
        private const string BambooFlowersPath = Root + "/Art/BambooHome/Props/wildflowers.glb";
        private const string HumanVillageTerrainPath = Root + "/Art/HumanVillage/Terrain/Suntail/Village_Terrain_terrain.obj";
        private const string HumanVillageMeadowsTerrainPath = Root + "/Art/HumanVillage/Terrain/PureNatureMeadows/TerrainMeadows_terrain.obj";
        private const string HumanVillageEnvironmentModelsRoot = Root + "/Art/HumanVillage/Suntail/Models/Environment";
        private const string HumanVillageNatureModelsRoot = Root + "/Art/HumanVillage/Suntail/Models/Nature";
        private const string HumanVillageBackgroundModelsRoot = Root + "/Art/HumanVillage/Suntail/Models/BackgroundTerrains";
        private const string HumanVillageBuildingPrefabsRoot = Root + "/Art/HumanVillage/Suntail/Prefabs/Buildings";
        private const string HumanVillageMaterialsRoot = Root + "/Art/HumanVillage/Materials";
        private const string MeadowsArtRoot = Root + "/Art/Locations/PureNatureMeadows";
        private const string MeadowsTerrainPath = MeadowsArtRoot + "/Terrain/TerrainMeadows.obj";
        private const string MeadowsTreesRoot = MeadowsArtRoot + "/Trees";
        private const string MeadowsPlantsRoot = MeadowsArtRoot + "/Plants";
        private const string MeadowsRocksRoot = MeadowsArtRoot + "/Rocks";
        private const string MeadowsMushroomRoot = MeadowsArtRoot + "/Mushroom";
        private const string MeadowsMountainsRoot = MeadowsArtRoot + "/Mountains";
        private const string MeadowsMaterialsRoot = MeadowsArtRoot + "/Materials";
        private const string LocationsArtRoot = Root + "/Art/Locations";
        private const string ClassicScenePath = ScenesRoot + "/PureNatureClassic.unity";
        private const string JungleScenePath = ScenesRoot + "/PureNatureJungle.unity";
        private const string IslandsScenePath = ScenesRoot + "/PureNatureIslands.unity";
        private const string MountainsScenePath = ScenesRoot + "/PureNatureMountains.unity";
        private const string FantasyForestScenePath = ScenesRoot + "/PureNatureFantasyForest.unity";
        private const string AngryMeshMeadowScenePath = ScenesRoot + "/AngryMeshMeadow.unity";
        private const string MagicForestScenePath = ScenesRoot + "/MagicForest.unity";
        private const string MistyLakeScenePath = ScenesRoot + "/MistyLake.unity";
        private const string TownWorldScenePath = ScenesRoot + "/TownWorld.unity";
        private const string FantasyVillageScenePath = ScenesRoot + "/FantasyVillage.unity";
        private const string SuntailVillagePlayableScenePath = ScenesRoot + "/SuntailVillagePlayable.unity";
        private const string SuntailVillageImportedScenePath = ScenesRoot + "/SuntailVillageImported.unity";
        private const string HakureiShrineScenePath = ScenesRoot + "/HakureiShrine.unity";
        private const string ScarletMansionFrontScenePath = ScenesRoot + "/ScarletMansionFront.unity";
        private const string DungeonEntranceScenePath = ScenesRoot + "/DungeonEntrance.unity";
        private const string FarmScenePath = ScenesRoot + "/Farm.unity";
        private const string MokouHouse3DScenePath = ScenesRoot + "/MokouHouse3D.unity";
        private const string BambooHouseScenePath = ScenesRoot + "/BambooHouse.unity";
        private const string CombatArenaScenePath = ScenesRoot + "/CombatArena.unity";
        private const string CombatArenaHD2DScenePath = ScenesRoot + "/CombatArenaHD2D.unity";
        private const string CirnoBossArenaScenePath = ScenesRoot + "/CirnoBossArena.unity";
        private const string MainMenuScenePath = ScenesRoot + "/MainMenu.unity";
        private const string LoadingScreenScenePath = ScenesRoot + "/LoadingScreen.unity";
        private const string WorldScenePath = ScenesRoot + "/World.unity";
        private const string LoadingScreenBackgroundPath = Root + "/Art/UI/Backgrounds/loading_screen_bg.png";
        private const string MokouVisualPath = Root + "/Art/Characters/Mokou/Models/mokou.fbx";
        private const string MokouLocomotionControllerPath = Root + "/Animations/Characters/MokouLocomotion.controller";
        private const string MokouReferenceRigPath = Root + "/Art/Characters/ReferenceRigs/ReimuMokouCc/reimu_mokou_cc.glb";
        private const string MokouValidationAnimationsRoot = Root + "/Animations/Characters/MokouValidation";
        private const string EnemyArtRoot = Root + "/Art/Enemies";
        private const string EnemyAnimationsRoot = Root + "/Animations/Enemies";

        private static readonly CharacterAnimationImportSpec[] MokouValidationAnimations =
        {
            new CharacterAnimationImportSpec("Standing Idle.fbx", true),
            new CharacterAnimationImportSpec("Standard Run.fbx", true),
            new CharacterAnimationImportSpec("Fast Run.fbx", true),
            new CharacterAnimationImportSpec("Jump.fbx", false),
            new CharacterAnimationImportSpec("Stand To Roll.fbx", false),
            new CharacterAnimationImportSpec("Mma Kick.fbx", false),
            new CharacterAnimationImportSpec("Uppercut Jab.fbx", false)
        };

        [MenuItem("Touhou Migration/Build Initial Project")]
        public static void BuildInitialProject()
        {
            EnsureFolders();
            AssetDatabase.Refresh();
            ConfigureCharacterImports();
            ConfigureEnemyAnimationImportsAndControllers();
            CreateCombatFeedbackPrefabs();
            CreateEncounterPrefabs();
            CreateEnemyCatalogPrefabs();
            CreateBootstrapScene();
            CreateTitleScreenScene();
            CreateBambooHomeVerticalSlice();
            CreateHumanVillageVerticalSlice();
            CreatePureNatureMeadowsScene();
            CreatePureNatureVariantScenes();
            CreateBespokeNatureScenes();
            CreateVillageScenes();
            CreateLandmarkScenes();
            CreateBambooVariantScenes();
            CreateArenaScenes();
            CreateMainEntryScenes();
            CreateMokouCharacterValidationScene();
            RegisterBuildScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Touhou Unity migration initial project built.");
        }

        [MenuItem("Touhou Migration/Build Location Scenes")]
        public static void BuildLocationScenes()
        {
            EnsureFolders();
            AssetDatabase.Refresh();
            CreatePureNatureMeadowsScene();
            CreatePureNatureVariantScenes();
            CreateBespokeNatureScenes();
            CreateVillageScenes();
            CreateLandmarkScenes();
            CreateBambooVariantScenes();
            CreateArenaScenes();
            CreateMainEntryScenes();
            RegisterBuildScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Touhou Unity migration location scenes built.");
        }

        [MenuItem("Touhou Migration/Build Enemy Catalog Prefabs")]
        public static void BuildEnemyCatalogPrefabs()
        {
            EnsureFolders();
            AssetDatabase.Refresh();
            ConfigureEnemyAnimationImportsAndControllers();
            CreateCombatFeedbackPrefabs();
            CreateEncounterPrefabs();
            CreateEnemyCatalogPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Touhou Unity migration enemy catalog prefabs built.");
        }

        [MenuItem("Touhou Migration/Build Enemy Animation Controllers")]
        public static void BuildEnemyAnimationControllers()
        {
            EnsureFolders();
            AssetDatabase.Refresh();
            ConfigureEnemyAnimationImportsAndControllers();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Touhou Unity migration enemy animation controllers built.");
        }

        [MenuItem("Touhou Migration/Build Combat Feedback Prefabs")]
        public static void BuildCombatFeedbackPrefabs()
        {
            EnsureFolders();
            AssetDatabase.Refresh();
            CreateCombatFeedbackPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Touhou Unity migration combat feedback prefabs built.");
        }

        [MenuItem("Touhou Migration/Build Encounter Prefabs")]
        public static void BuildEncounterPrefabs()
        {
            EnsureFolders();
            AssetDatabase.Refresh();
            CreateCombatFeedbackPrefabs();
            CreateEncounterPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Touhou Unity migration encounter prefabs built.");
        }

        [MenuItem("Touhou Migration/Install glTFast Package")]
        public static void InstallGltfFastPackage()
        {
            AddRequest request = Client.Add("com.unity.cloud.gltfast");
            while (!request.IsCompleted)
            {
                Thread.Sleep(100);
            }

            if (request.Status == StatusCode.Failure)
            {
                throw new IOException($"Failed to install glTFast package: {request.Error.message}");
            }

            Debug.Log($"Installed package: {request.Result.packageId}");
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                Root,
                ScenesRoot,
                Root + "/Art",
                Root + "/Art/UI",
                Root + "/Art/UI/Backgrounds",
                Root + "/Art/UI/Dialogue",
                Root + "/Art/UI/Dialogue/Marisa",
                Root + "/Art/UI/Dialogue/Marisa/Portraits",
                Root + "/Art/Fonts",
                Root + "/Art/BambooHome",
                Root + "/Art/BambooHome/House",
                Root + "/Art/BambooHome/Props",
                Root + "/Art/BambooHome/Textures",
                Root + "/Art/HumanVillage",
                Root + "/Art/HumanVillage/Materials",
                Root + "/Art/HumanVillage/Terrain",
                Root + "/Art/HumanVillage/Terrain/Suntail",
                Root + "/Art/HumanVillage/Terrain/PureNatureMeadows",
                Root + "/Art/Locations",
                Root + "/Art/Locations/PureNatureMeadows",
                Root + "/Art/Locations/PureNatureMeadows/Terrain",
                Root + "/Art/Locations/PureNatureMeadows/Trees",
                Root + "/Art/Locations/PureNatureMeadows/Plants",
                Root + "/Art/Locations/PureNatureMeadows/Rocks",
                Root + "/Art/Locations/PureNatureMeadows/Mushroom",
                Root + "/Art/Locations/PureNatureMeadows/Mountains",
                Root + "/Art/Locations/PureNatureMeadows/Materials",
                Root + "/Art/HumanVillage/Suntail",
                Root + "/Art/HumanVillage/Suntail/Models",
                Root + "/Art/HumanVillage/Suntail/Models/Environment",
                Root + "/Art/HumanVillage/Suntail/Models/Nature",
                Root + "/Art/HumanVillage/Suntail/Models/BackgroundTerrains",
                Root + "/Art/HumanVillage/Suntail/Prefabs",
                Root + "/Art/HumanVillage/Suntail/Prefabs/Buildings",
                Root + "/Art/Characters",
                Root + "/Art/Characters/Mokou",
                Root + "/Art/Characters/Mokou/Models",
                Root + "/Art/Characters/Mokou/Textures",
                Root + "/Art/Characters/ReferenceRigs",
                Root + "/Art/Characters/ReferenceRigs/ReimuMokouCc",
                Root + "/Art/Enemies",
                Root + "/Animations",
                Root + "/Animations/Characters",
                Root + "/Animations/Characters/MokouValidation",
                Root + "/Animations/Enemies",
                Root + "/Data",
                Root + "/Data/CardBuild",
                Root + "/Data/Combat",
                Root + "/Data/Combat/PerfectFreeze",
                Root + "/Data/Cooking",
                Root + "/Data/Dialogue",
                Root + "/Data/Items",
                Root + "/Data/Quests",
                Root + "/Data/Social",
                Root + "/Prefabs",
                Root + "/Prefabs/Enemies",
                Root + "/Prefabs/CombatFeedback",
                Root + "/Prefabs/Encounters",
                Root + "/Scripts",
                Root + "/Scripts/Runtime",
                Root + "/Scripts/Runtime/CardBuild",
                Root + "/Scripts/Runtime/Cooking",
                Root + "/Scripts/Runtime/Dialogue",
                Root + "/Scripts/Runtime/Inventory",
                Root + "/Scripts/Runtime/Quest",
                Root + "/Scripts/Runtime/Save",
                Root + "/Scripts/Runtime/Serialization",
                Root + "/Scripts/Runtime/Settings",
                Root + "/Scripts/Runtime/Social",
                Root + "/Scripts/Runtime/UI",
                Root + "/Scripts/Runtime/UI/CardBuild",
                Root + "/Scripts/Runtime/UI/Dialogue",
                Root + "/Scripts/Editor"
            };

            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
        }

        private static void CreateCombatFeedbackPrefabs()
        {
            SavePrefab(CreateEnemyProjectileFeedbackPrefab(), EnemyProjectileFeedbackPrefabPath);
            SavePrefab(CreateIceOrbProjectileFeedbackPrefab(), IceOrbProjectileFeedbackPrefabPath);
            SavePrefab(CreateIceShardProjectileFeedbackPrefab(), IceShardProjectileFeedbackPrefabPath);
            SavePrefab(CreateIceLanceProjectileFeedbackPrefab(), IceLanceProjectileFeedbackPrefabPath);
            SavePrefab(CreatePerfectFreezeProjectileFeedbackPrefab(), PerfectFreezeProjectileFeedbackPrefabPath);
            SavePrefab(CreateMeleeDangerFeedbackPrefab(), MeleeDangerFeedbackPrefabPath);
            SavePrefab(CreateEnemyDeathFeedbackPrefab(), EnemyDeathFeedbackPrefabPath);
        }

        private static void CreateEncounterPrefabs()
        {
            MigrationPerfectFreezePhasePlan phasePlan = CreatePerfectFreezePhasePlanAsset();
            SavePrefab(CreatePerfectFreezeEncounterPrefab(phasePlan), PerfectFreezeEncounterPrefabPath);
        }

        private static MigrationPerfectFreezePhasePlan CreatePerfectFreezePhasePlanAsset()
        {
            MigrationPerfectFreezePhasePlan phasePlan =
                AssetDatabase.LoadAssetAtPath<MigrationPerfectFreezePhasePlan>(PerfectFreezePhasePlanPath);
            if (phasePlan == null)
            {
                phasePlan = ScriptableObject.CreateInstance<MigrationPerfectFreezePhasePlan>();
                AssetDatabase.CreateAsset(phasePlan, PerfectFreezePhasePlanPath);
            }

            phasePlan.ConfigurePhase(300f, 70f, 2.2f, 18, 22f, 1.05f);
            phasePlan.ConfigureOutcomes(70f, 100f, 3.5f, 4.5f);
            phasePlan.ConfigureCastPlan(11, 82f, 2, 12, 3, 6, 68f, 12f, 8f, 4.2f);
            EditorUtility.SetDirty(phasePlan);
            return phasePlan;
        }

        private static GameObject CreateEnemyProjectileFeedbackPrefab()
        {
            GameObject projectile = new GameObject("MigrationEnemyProjectileFeedback");
            MigrationCombatFeedbackTemplate template = projectile.AddComponent<MigrationCombatFeedbackTemplate>();
            template.ConfigureTemplate(
                "enemy_projectile",
                true,
                "EnemyProjectile",
                4f,
                0.18f,
                new Color(1f, 0.2f, 0.12f, 1f),
                true,
                true,
                true,
                1.15f,
                0.7f,
                "enemy_projectile",
                false,
                0f,
                string.Empty);

            MigrationEnemyProjectile runtime = projectile.AddComponent<MigrationEnemyProjectile>();
            runtime.ApplyFeedbackTemplate(template);
            MigrationProjectileGrazePresenter grazePresenter = projectile.AddComponent<MigrationProjectileGrazePresenter>();
            grazePresenter.BindProjectile(runtime);
            grazePresenter.ConfigurePresentation(
                0.45f,
                new Color(1f, 0.72f, 0.25f, 1f),
                new Color(0.4f, 0.95f, 1f, 1f));
            MigrationProjectileShatterPresenter shatterPresenter = projectile.AddComponent<MigrationProjectileShatterPresenter>();
            shatterPresenter.BindProjectile(runtime);
            shatterPresenter.ConfigurePresentation(
                0.5f,
                new Color(0.55f, 0.95f, 1f, 1f),
                new Color(1f, 0.74f, 0.28f, 1f));
            MigrationProjectileSpecialSettlement settlement = projectile.AddComponent<MigrationProjectileSpecialSettlement>();
            settlement.BindProjectile(runtime);
            settlement.ConfigureRewards(2f, 5f, 8f, 12f, 12, 1.2f);
            return projectile;
        }

        private static GameObject CreateIceOrbProjectileFeedbackPrefab()
        {
            GameObject projectile = new GameObject("MigrationIceOrbProjectileFeedback");
            MigrationCombatFeedbackTemplate template = projectile.AddComponent<MigrationCombatFeedbackTemplate>();
            template.ConfigureTemplate(
                "ice_orb_projectile",
                true,
                "EnemyProjectile",
                6f,
                0.4f,
                new Color(0.35f, 0.78f, 1f, 1f),
                true,
                true,
                grazeEnabled: true,
                grazeRadius: 1.15f,
                perfectGrazeRadius: 0.7f,
                projectileFamily: "ice_orb",
                armDelaySeconds: 0.32f);

            MigrationEnemyProjectile runtime = projectile.AddComponent<MigrationEnemyProjectile>();
            runtime.ApplyFeedbackTemplate(template);
            runtime.Configure(6.5f, 8f, Vector3.forward, true, 0.4f);
            MigrationProjectileGrazePresenter grazePresenter = projectile.AddComponent<MigrationProjectileGrazePresenter>();
            grazePresenter.BindProjectile(runtime);
            grazePresenter.ConfigurePresentation(
                0.45f,
                new Color(1f, 0.72f, 0.25f, 1f),
                new Color(0.4f, 0.95f, 1f, 1f));
            MigrationProjectileShatterPresenter shatterPresenter = projectile.AddComponent<MigrationProjectileShatterPresenter>();
            shatterPresenter.BindProjectile(runtime);
            shatterPresenter.ConfigurePresentation(
                0.5f,
                new Color(0.55f, 0.95f, 1f, 1f),
                new Color(1f, 0.74f, 0.28f, 1f));
            MigrationProjectileSpecialSettlement settlement = projectile.AddComponent<MigrationProjectileSpecialSettlement>();
            settlement.BindProjectile(runtime);
            settlement.ConfigureRewards(2f, 5f, 8f, 12f, 12, 1.2f);
            return projectile;
        }

        private static GameObject CreateIceShardProjectileFeedbackPrefab()
        {
            GameObject projectile = new GameObject("MigrationIceShardProjectileFeedback");
            MigrationCombatFeedbackTemplate template = projectile.AddComponent<MigrationCombatFeedbackTemplate>();
            template.ConfigureTemplate(
                "ice_shard_projectile",
                true,
                "EnemyProjectile",
                6f,
                0.52f,
                new Color(0.78f, 0.96f, 1f, 1f),
                true,
                true,
                grazeEnabled: true,
                grazeRadius: 1.15f,
                perfectGrazeRadius: 0.7f,
                projectileFamily: "ice_shard",
                armDelaySeconds: 0.42f);

            MigrationEnemyProjectile runtime = projectile.AddComponent<MigrationEnemyProjectile>();
            runtime.ApplyFeedbackTemplate(template);
            runtime.Configure(10.5f, 12f, Vector3.forward, true, 0.52f);
            MigrationProjectileGrazePresenter grazePresenter = projectile.AddComponent<MigrationProjectileGrazePresenter>();
            grazePresenter.BindProjectile(runtime);
            grazePresenter.ConfigurePresentation(
                0.45f,
                new Color(1f, 0.72f, 0.25f, 1f),
                new Color(0.4f, 0.95f, 1f, 1f));
            MigrationProjectileShatterPresenter shatterPresenter = projectile.AddComponent<MigrationProjectileShatterPresenter>();
            shatterPresenter.BindProjectile(runtime);
            shatterPresenter.ConfigurePresentation(
                0.5f,
                new Color(0.55f, 0.95f, 1f, 1f),
                new Color(1f, 0.74f, 0.28f, 1f));
            MigrationProjectileSpecialSettlement settlement = projectile.AddComponent<MigrationProjectileSpecialSettlement>();
            settlement.BindProjectile(runtime);
            settlement.ConfigureRewards(2f, 5f, 8f, 12f, 12, 1.2f);
            return projectile;
        }

        private static GameObject CreateIceLanceProjectileFeedbackPrefab()
        {
            GameObject projectile = new GameObject("MigrationIceLanceProjectileFeedback");
            MigrationCombatFeedbackTemplate template = projectile.AddComponent<MigrationCombatFeedbackTemplate>();
            template.ConfigureTemplate(
                "ice_lance_projectile",
                true,
                "EnemyProjectile",
                5.5f,
                0.24f,
                new Color(0.78f, 0.96f, 1f, 1f),
                true,
                true,
                grazeEnabled: true,
                grazeRadius: 1.15f,
                perfectGrazeRadius: 0.7f,
                projectileFamily: "ice_lance",
                armDelaySeconds: 0.62f,
                reflectable: true,
                reflectStunReward: true,
                reflectStunSeconds: 2f);

            MigrationEnemyProjectile runtime = projectile.AddComponent<MigrationEnemyProjectile>();
            runtime.ApplyFeedbackTemplate(template);
            runtime.Configure(22.5f, 16f, Vector3.forward, true, 0.24f);
            MigrationProjectileGrazePresenter grazePresenter = projectile.AddComponent<MigrationProjectileGrazePresenter>();
            grazePresenter.BindProjectile(runtime);
            grazePresenter.ConfigurePresentation(
                0.45f,
                new Color(1f, 0.72f, 0.25f, 1f),
                new Color(0.4f, 0.95f, 1f, 1f));
            MigrationProjectileShatterPresenter shatterPresenter = projectile.AddComponent<MigrationProjectileShatterPresenter>();
            shatterPresenter.BindProjectile(runtime);
            shatterPresenter.ConfigurePresentation(
                0.5f,
                new Color(0.55f, 0.95f, 1f, 1f),
                new Color(1f, 0.74f, 0.28f, 1f));
            MigrationProjectileSpecialSettlement settlement = projectile.AddComponent<MigrationProjectileSpecialSettlement>();
            settlement.BindProjectile(runtime);
            settlement.ConfigureRewards(2f, 5f, 8f, 12f, 12, 1.2f);
            return projectile;
        }

        private static GameObject CreatePerfectFreezeProjectileFeedbackPrefab()
        {
            GameObject projectile = new GameObject("MigrationPerfectFreezeProjectileFeedback");
            MigrationCombatFeedbackTemplate template = projectile.AddComponent<MigrationCombatFeedbackTemplate>();
            template.ConfigureTemplate(
                "perfect_freeze_projectile",
                true,
                "EnemyProjectile",
                6f,
                0.22f,
                new Color(0.55f, 0.9f, 1f, 1f),
                true,
                true,
                true,
                1.15f,
                0.7f,
                "frozen_crystal",
                false,
                20f,
                "fire,heavy,shatter",
                true,
                1.6f,
                2.4f,
                4.2f,
                8f,
                7f,
                8f,
                10f,
                20f,
                0.5f);

            MigrationEnemyProjectile runtime = projectile.AddComponent<MigrationEnemyProjectile>();
            runtime.ApplyFeedbackTemplate(template);
            MigrationProjectileGrazePresenter grazePresenter = projectile.AddComponent<MigrationProjectileGrazePresenter>();
            grazePresenter.BindProjectile(runtime);
            grazePresenter.ConfigurePresentation(
                0.45f,
                new Color(1f, 0.72f, 0.25f, 1f),
                new Color(0.4f, 0.95f, 1f, 1f));
            MigrationProjectileShatterPresenter shatterPresenter = projectile.AddComponent<MigrationProjectileShatterPresenter>();
            shatterPresenter.BindProjectile(runtime);
            shatterPresenter.ConfigurePresentation(
                0.5f,
                new Color(0.55f, 0.95f, 1f, 1f),
                new Color(1f, 0.74f, 0.28f, 1f));
            MigrationProjectileSpecialSettlement settlement = projectile.AddComponent<MigrationProjectileSpecialSettlement>();
            settlement.BindProjectile(runtime);
            settlement.ConfigureRewards(2f, 5f, 8f, 12f, 12, 1.2f);
            return projectile;
        }

        private static GameObject CreateMeleeDangerFeedbackPrefab()
        {
            GameObject danger = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            danger.name = "MigrationMeleeDangerFeedback";
            danger.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            SphereCollider collider = danger.GetComponent<SphereCollider>();
            collider.isTrigger = true;

            Rigidbody rigidbody = danger.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            MigrationEnemyDamageSource damageSource = danger.AddComponent<MigrationEnemyDamageSource>();
            damageSource.ConfigureWindowing(true, false);

            MigrationCombatFeedbackTemplate template = danger.AddComponent<MigrationCombatFeedbackTemplate>();
            template.ConfigureTemplate(
                "melee_danger",
                true,
                "Default",
                0.12f,
                0.35f,
                new Color(1f, 0.35f, 0.08f, 0.65f),
                false,
                false);
            return danger;
        }

        private static GameObject CreateEnemyDeathFeedbackPrefab()
        {
            GameObject death = new GameObject("MigrationEnemyDeathFeedback");
            ParticleSystem particles = death.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.duration = 0.45f;
            main.startLifetime = 0.35f;
            main.startSpeed = 1.1f;
            main.startSize = 0.45f;
            main.startColor = new Color(1f, 0.35f, 0.12f, 1f);

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)16)
            });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.45f;

            MigrationCombatFeedbackTemplate template = death.AddComponent<MigrationCombatFeedbackTemplate>();
            template.ConfigureTemplate(
                "death_feedback",
                true,
                "Default",
                0.45f,
                0.45f,
                new Color(1f, 0.35f, 0.12f, 1f),
                false,
                false);
            return death;
        }

        private static GameObject CreatePerfectFreezeEncounterPrefab(MigrationPerfectFreezePhasePlan phasePlan)
        {
            GameObject encounter = new GameObject("MigrationPerfectFreezeEncounter");

            MigrationProjectileSpecialSettlement scopedSettlement =
                encounter.AddComponent<MigrationProjectileSpecialSettlement>();
            scopedSettlement.ConfigureRewards(2f, 5f, 8f, 12f, 12, 1.2f);
            scopedSettlement.ConfigureSharedSettlementFallback(false);

            MigrationPerfectFreezeEncounterDirector director =
                encounter.AddComponent<MigrationPerfectFreezeEncounterDirector>();
            MigrationPrefabPoolService projectilePool = encounter.AddComponent<MigrationPrefabPoolService>();
            director.BindProjectilePool(projectilePool);
            GameObject projectileFeedbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PerfectFreezeProjectileFeedbackPrefabPath);
            MigrationEnemyProjectile projectilePrefab = projectileFeedbackPrefab != null
                ? projectileFeedbackPrefab.GetComponent<MigrationEnemyProjectile>()
                : null;
            director.BindProjectilePrefab(projectilePrefab);
            GameObject iceOrbFeedbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IceOrbProjectileFeedbackPrefabPath);
            MigrationEnemyProjectile iceOrbProjectilePrefab = iceOrbFeedbackPrefab != null
                ? iceOrbFeedbackPrefab.GetComponent<MigrationEnemyProjectile>()
                : null;
            director.BindIceOrbProjectilePrefab(iceOrbProjectilePrefab);
            GameObject iceShardFeedbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IceShardProjectileFeedbackPrefabPath);
            MigrationEnemyProjectile iceShardProjectilePrefab = iceShardFeedbackPrefab != null
                ? iceShardFeedbackPrefab.GetComponent<MigrationEnemyProjectile>()
                : null;
            director.BindIceShardProjectilePrefab(iceShardProjectilePrefab);
            GameObject iceLanceFeedbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IceLanceProjectileFeedbackPrefabPath);
            MigrationEnemyProjectile iceLanceProjectilePrefab = iceLanceFeedbackPrefab != null
                ? iceLanceFeedbackPrefab.GetComponent<MigrationEnemyProjectile>()
                : null;
            director.BindIceLanceProjectilePrefab(iceLanceProjectilePrefab);
            director.BindScopedSettlement(scopedSettlement);
            director.ConfigurePattern(80, 12, 4f, 1.2f);
            director.BindPhasePlan(phasePlan);

            // Co-locate the phase-outcome presenter with the director so it auto-binds to PhaseFinished at runtime.
            encounter.AddComponent<MigrationPerfectFreezeOutcomePresenter>();

            GameObject safeLane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            safeLane.name = "PerfectFreezeSafeLaneCue";
            safeLane.transform.SetParent(encounter.transform, false);
            safeLane.transform.localPosition = new Vector3(0f, 0.04f, 4f);
            float safeLaneWidth = Mathf.Tan(director.SafeLaneHalfAngleDegrees * Mathf.Deg2Rad) * 4.3f * 1.55f;
            safeLane.transform.localScale = new Vector3(safeLaneWidth, 0.025f, 8f);

            Collider safeLaneCollider = safeLane.GetComponent<Collider>();
            if (safeLaneCollider != null)
            {
                Object.DestroyImmediate(safeLaneCollider);
            }

            Renderer safeLaneRenderer = safeLane.GetComponent<Renderer>();
            if (safeLaneRenderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = new Color(1f, 0.54f, 0.18f, 0.3f);
                safeLaneRenderer.sharedMaterial = material;
            }

            MigrationPerfectFreezeSafeLaneCue safeLaneCue =
                safeLane.AddComponent<MigrationPerfectFreezeSafeLaneCue>();
            safeLaneCue.BindRenderer(safeLaneRenderer);
            safeLaneCue.ConfigureCue(22f, 1.05f, new Color(1f, 0.54f, 0.18f, 0.3f));
            safeLaneCue.HideCue();
            director.BindSafeLaneCue(safeLaneCue);

            GameObject snowball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            snowball.name = "PerfectFreezeSnowballHazard";
            snowball.transform.SetParent(encounter.transform, false);
            snowball.transform.localScale = Vector3.one * 0.88f;

            SphereCollider snowballCollider = snowball.GetComponent<SphereCollider>();
            if (snowballCollider != null)
            {
                snowballCollider.isTrigger = true;
            }

            Rigidbody snowballRigidbody = snowball.AddComponent<Rigidbody>();
            snowballRigidbody.isKinematic = true;
            snowballRigidbody.useGravity = false;

            Renderer snowballRenderer = snowball.GetComponent<Renderer>();
            if (snowballRenderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.86f, 0.94f, 0.98f, 1f);
                material.SetColor("_EmissionColor", new Color(0.22f, 0.58f, 0.78f, 1f) * 0.22f);
                snowballRenderer.sharedMaterial = material;
                snowballRenderer.enabled = false;
            }

            MigrationPerfectFreezeSnowballHazard snowballHazard =
                snowball.AddComponent<MigrationPerfectFreezeSnowballHazard>();
            snowballHazard.BindEncounterDirector(director);
            director.BindSnowballHazard(snowballHazard);

            GameObject boss = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            boss.name = "MigrationPerfectFreezeBossTarget";
            boss.transform.SetParent(encounter.transform, false);
            boss.transform.localPosition = new Vector3(0f, 1f, 0f);
            boss.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

            Renderer renderer = boss.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.35f, 0.82f, 1f, 1f);
                renderer.sharedMaterial = material;
            }

            MigrationCombatTargetBehaviour target = boss.AddComponent<MigrationCombatTargetBehaviour>();
            target.Initialize(300f);

            MigrationCombatDefeatHandler defeatHandler = boss.AddComponent<MigrationCombatDefeatHandler>();
            defeatHandler.ConfigureDefeatDelay(0.45f);
            defeatHandler.ConfigureDeathFeedback(0.45f, new Color(0.55f, 0.9f, 1f, 1f));
            GameObject deathFeedbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyDeathFeedbackPrefabPath);
            if (deathFeedbackPrefab != null)
            {
                defeatHandler.ConfigureDeathFeedbackPrefab(deathFeedbackPrefab);
            }

            MigrationCombatHurtFeedback hurtFeedback = boss.AddComponent<MigrationCombatHurtFeedback>();
            hurtFeedback.BindTarget(target);
            hurtFeedback.ConfigureFeedback(0.18f, new Color(0.45f, 0.95f, 1f, 1f), 0.06f);

            MigrationSimpleEnemyController bossController = boss.AddComponent<MigrationSimpleEnemyController>();
            bossController.BindTarget(target);
            bossController.ConfigureMovement(0f, 0f, 0f);
            bossController.ConfigureAttackCooldown(999f);
            bossController.ConfigureActionTimings(0f, 0f, 0f);

            MigrationPerfectFreezeStaggerAdapter staggerAdapter =
                boss.AddComponent<MigrationPerfectFreezeStaggerAdapter>();
            staggerAdapter.BindSettlement(scopedSettlement);
            staggerAdapter.BindEnemyController(bossController);

            director.BindBossController(bossController);
            director.BindBossTarget(target);
            director.BindStaggerAdapter(staggerAdapter);
            return encounter;
        }

        private static GameObject SavePrefab(GameObject prefabRoot, string prefabPath)
        {
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            Object.DestroyImmediate(prefabRoot);
            if (savedPrefab == null)
            {
                throw new IOException($"Failed to save prefab at {prefabPath}.");
            }

            return savedPrefab;
        }

        private static void CreateEnemyCatalogPrefabs()
        {
            ClearGeneratedEnemyPrefabs();

            MigrationEnemyCatalog catalog = new MigrationEnemyCatalog();
            catalog.LoadGodotDefaults();
            foreach (MigrationEnemyVariantProfile profile in catalog.GetAllProfiles())
            {
                GameObject prefabRoot = CreateEnemyPrefabRoot(profile);
                string prefabPath = $"{EnemyPrefabsRoot}/MigrationEnemy_{ToPascal(profile.VariantId)}.prefab";
                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                Object.DestroyImmediate(prefabRoot);
                if (savedPrefab == null)
                {
                    throw new IOException($"Failed to save enemy prefab at {prefabPath}.");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ClearGeneratedEnemyPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { EnemyPrefabsRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(path);
                if (fileName.StartsWith("MigrationEnemy_"))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
        }

        private static GameObject CreateEnemyPrefabRoot(MigrationEnemyVariantProfile profile)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = $"MigrationEnemy_{ToPascal(profile.VariantId)}";
            root.transform.localScale = ScaleForEnemyProfile(profile);

            MigrationCombatTargetBehaviour target = root.AddComponent<MigrationCombatTargetBehaviour>();
            target.Initialize(profile.MaxHp);

            MigrationCombatDefeatHandler defeatHandler = root.AddComponent<MigrationCombatDefeatHandler>();
            defeatHandler.ConfigureDefeatDelay(0.45f);
            defeatHandler.ConfigureDeathFeedback(0.45f, new Color(1f, 0.35f, 0.12f, 1f));
            GameObject deathFeedbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyDeathFeedbackPrefabPath);
            if (deathFeedbackPrefab != null)
            {
                defeatHandler.ConfigureDeathFeedbackPrefab(deathFeedbackPrefab);
            }

            MigrationCombatHurtFeedback hurtFeedback = root.AddComponent<MigrationCombatHurtFeedback>();
            hurtFeedback.BindTarget(target);
            hurtFeedback.ConfigureFeedback(0.16f, new Color(1f, 0.15f, 0.1f, 1f), 0.08f);

            MigrationCombatDefeatRewardHandler rewardHandler = root.AddComponent<MigrationCombatDefeatRewardHandler>();
            rewardHandler.BindTarget(target);
            rewardHandler.ConfigureRewards(profile.XpValue, profile.XpValue, "enemy_killed");

            MigrationCombatLootDropHandler lootDropHandler = root.AddComponent<MigrationCombatLootDropHandler>();
            lootDropHandler.BindTarget(target);

            AttachCombatReadabilityPresenters(root, target, rewardHandler, lootDropHandler);

            MigrationSimpleEnemyController controller = root.AddComponent<MigrationSimpleEnemyController>();
            controller.BindTarget(target);
            controller.BindLootDropHandler(lootDropHandler);

            if (profile.CanMelee)
            {
                MigrationEnemyDamageSource damageSource = CreateEnemyDamageSourceMarker(root.transform);
                controller.BindDamageSource(damageSource);
            }

            controller.ApplyVariant(profile);
            if (profile.CanShoot)
            {
                GameObject projectileFeedbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyProjectileFeedbackPrefabPath);
                MigrationEnemyProjectile projectilePrefab = projectileFeedbackPrefab != null
                    ? projectileFeedbackPrefab.GetComponent<MigrationEnemyProjectile>()
                    : null;
                controller.ConfigureProjectilePrefab(projectilePrefab);
            }

            controller.ConfigureActionTimings(profile.AttackWindupSeconds, 0.12f, 0.28f);
            AttachEnemyVisual(root, profile);
            AttachEnemyAnimation(root, profile);
            return root;
        }

        private static void AttachCombatReadabilityPresenters(
            GameObject root,
            MigrationCombatTargetBehaviour target,
            MigrationCombatDefeatRewardHandler rewardHandler,
            MigrationCombatLootDropHandler lootDropHandler)
        {
            MigrationDamageNumberPresenter damageNumbers = root.AddComponent<MigrationDamageNumberPresenter>();
            damageNumbers.BindTarget(target);
            damageNumbers.ConfigurePresentation(0.45f, 1.35f, new Color(1f, 0.92f, 0.35f, 1f));

            MigrationCombatRewardPresentation rewardPresentation = root.AddComponent<MigrationCombatRewardPresentation>();
            rewardPresentation.BindRewardHandler(rewardHandler);
            rewardPresentation.BindLootDropHandler(lootDropHandler);
            rewardPresentation.ConfigurePresentation(
                0.8f,
                new Color(1f, 0.86f, 0.32f, 1f),
                new Color(0.42f, 1f, 0.58f, 1f));
        }

        private static void AttachEnemyVisual(GameObject root, MigrationEnemyVariantProfile profile)
        {
            EnemyVisualSpec visualSpec = ResolveEnemyVisualSpec(profile);
            MigrationEnemyVisualSource visualSource = root.AddComponent<MigrationEnemyVisualSource>();
            visualSource.Configure(
                profile.VariantId,
                profile.GodotScenePath,
                visualSpec.ModelAssetPath,
                visualSpec.TextureAssetPaths,
                !visualSpec.HasImportedModel);

            if (!visualSpec.HasImportedModel)
            {
                return;
            }

            Renderer rootRenderer = root.GetComponent<Renderer>();
            if (rootRenderer != null)
            {
                rootRenderer.enabled = false;
            }

            GameObject visualRoot = new GameObject("Visual");
            visualRoot.transform.SetParent(root.transform, false);

            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(visualSpec.ModelAssetPath);
            GameObject model = PrefabUtility.InstantiatePrefab(modelPrefab) as GameObject;
            if (model == null)
            {
                Debug.LogWarning($"Unable to instantiate enemy visual at {visualSpec.ModelAssetPath}.");
                return;
            }

            model.name = "VisualModel";
            model.transform.SetParent(visualRoot.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            RemoveColliders(model);
            Material material = EnsureEnemyVisualMaterial(profile, visualSpec.TextureAssetPaths);
            if (material != null)
            {
                ApplyMaterialToRenderers(model, material);
            }

            NormalizeVisualBounds(model, root.transform.position + new Vector3(0f, profile.FloatHeight, 0f), 1.6f);
        }

        private static void AttachEnemyAnimation(GameObject root, MigrationEnemyVariantProfile profile)
        {
            EnemyAnimationSpec animationSpec = ResolveEnemyAnimationSpec(profile.VariantId);
            AnimatorController controller = string.IsNullOrEmpty(animationSpec.ControllerPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<AnimatorController>(animationSpec.ControllerPath);
            bool usesFallback = controller == null || !animationSpec.HasAnyClip;

            MigrationEnemyAnimationSource animationSource = root.AddComponent<MigrationEnemyAnimationSource>();
            animationSource.Configure(
                profile.VariantId,
                usesFallback ? string.Empty : animationSpec.ControllerPath,
                animationSpec.IdleClipPath,
                animationSpec.MoveClipPath,
                animationSpec.AttackClipPath,
                animationSpec.ProjectileClipPath,
                animationSpec.TakeDamageClipPath,
                animationSpec.DieClipPath,
                usesFallback);

            if (usesFallback)
            {
                return;
            }

            Transform visualModel = root.transform.Find("Visual/VisualModel");
            if (visualModel == null)
            {
                return;
            }

            Animator animator = visualModel.GetComponent<Animator>();
            if (animator == null)
            {
                animator = visualModel.gameObject.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            MigrationEnemyAnimationBridge bridge = root.AddComponent<MigrationEnemyAnimationBridge>();
            bridge.BindController(root.GetComponent<MigrationSimpleEnemyController>());
            bridge.BindTarget(root.GetComponent<MigrationCombatTargetBehaviour>());
            bridge.BindAnimator(animator);
        }

        private static EnemyVisualSpec ResolveEnemyVisualSpec(MigrationEnemyVariantProfile profile)
        {
            string pascalId = ToPascal(profile.VariantId);
            string modelPath = $"{EnemyArtRoot}/{pascalId}/Models/{pascalId}.fbx";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(modelPath) == null)
            {
                return EnemyVisualSpec.Fallback();
            }

            return new EnemyVisualSpec(modelPath, FindEnemyTexturePaths(profile.VariantId, pascalId));
        }

        private static string[] FindEnemyTexturePaths(string variantId, string pascalId)
        {
            string textureRoot = $"{EnemyArtRoot}/{pascalId}/Textures";
            string[] sceneTextureFileNames = SceneTextureFileNamesForEnemy(variantId);
            if (sceneTextureFileNames.Length == 0)
            {
                return FindAllEnemyTexturePaths(textureRoot);
            }

            System.Collections.Generic.List<string> texturePaths = new System.Collections.Generic.List<string>();
            foreach (string fileName in sceneTextureFileNames)
            {
                string texturePath = $"{textureRoot}/{fileName}";
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath) != null)
                {
                    texturePaths.Add(texturePath);
                }
            }

            return texturePaths.Count == 0 ? FindAllEnemyTexturePaths(textureRoot) : texturePaths.ToArray();
        }

        private static string[] FindAllEnemyTexturePaths(string textureRoot)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { textureRoot });
            string[] paths = new string[guids.Length];
            for (int index = 0; index < guids.Length; index++)
            {
                paths[index] = AssetDatabase.GUIDToAssetPath(guids[index]);
            }

            System.Array.Sort(paths, System.StringComparer.Ordinal);
            return paths;
        }

        private static string[] SceneTextureFileNamesForEnemy(string variantId)
        {
            return variantId switch
            {
                "bat" => new[] { "Vampire Bat.png", "Vampire Bat Emission.png" },
                "bee" => new[] { "Bee.png" },
                "bird" => new[] { "Bird.png" },
                "bumble" => new[] { "Bumble.png" },
                "ghost" => new[] { "Ghost.png" },
                "phantom" => new[] { "Phantom.png", "Phantom Emission.png" },
                "spook" => new[] { "Spook.png", "Spook Emission.png" },
                "sting" => new[] { "Sting.png", "Sting Emission.png" },
                "fungi" => new[] { "Fungi.png" },
                "mushroom" => new[] { "Mushroom.png" },
                "seed" => new[] { "Seed.png" },
                "shade" => new[] { "Shade.png", "Shade Emission.png" },
                "shadow" => new[] { "Shadow.png", "Shadow Emission.png" },
                "sprout" => new[] { "Sprout.png" },
                "toadstool" => new[] { "Toadstool.png" },
                "chick" => new[] { "Chick.png" },
                "egg" => new[] { "Egg.png", "Egg Emission.png" },
                "fledgling" => new[] { "Fledgling.png" },
                "spider" => new[] { "Spider.png", "Spider Emission.png" },
                _ => System.Array.Empty<string>()
            };
        }

        private static Material EnsureEnemyVisualMaterial(MigrationEnemyVariantProfile profile, string[] texturePaths)
        {
            string pascalId = ToPascal(profile.VariantId);
            string materialFolder = $"{EnemyArtRoot}/{pascalId}/Materials";
            Directory.CreateDirectory(materialFolder);
            string materialPath = $"{materialFolder}/{pascalId}_Visual.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            Texture2D texture = FindPrimaryEnemyTexture(texturePaths);
            if (texture != null)
            {
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", texture);
                }

                if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", texture);
                }
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D FindPrimaryEnemyTexture(string[] texturePaths)
        {
            foreach (string texturePath in texturePaths)
            {
                string fileName = Path.GetFileNameWithoutExtension(texturePath);
                if (fileName.IndexOf("Emission", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (texture != null)
                {
                    return texture;
                }
            }

            return texturePaths.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(texturePaths[0]);
        }

        private static MigrationEnemyDamageSource CreateEnemyDamageSourceMarker(Transform parent)
        {
            GameObject damageSource = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            damageSource.name = "EnemyDamageSourceMarker";
            damageSource.transform.SetParent(parent);
            damageSource.transform.localPosition = new Vector3(1.2f, 0f, 0f);
            damageSource.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            SphereCollider collider = damageSource.GetComponent<SphereCollider>();
            collider.isTrigger = true;

            Rigidbody rigidbody = damageSource.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            MigrationEnemyDamageSource enemyDamageSource = damageSource.AddComponent<MigrationEnemyDamageSource>();
            enemyDamageSource.ConfigureWindowing(true, false);
            return enemyDamageSource;
        }

        private static Vector3 ScaleForEnemyProfile(MigrationEnemyVariantProfile profile)
        {
            float styleScale = profile.MoveStyle switch
            {
                "fly" => 0.75f,
                "crawl" => 0.65f,
                "jump" => 0.85f,
                _ => 1f
            };

            float resolvedScale = Mathf.Max(0.1f, profile.ModelScale) * styleScale;
            return new Vector3(resolvedScale, resolvedScale, resolvedScale);
        }

        private static string ToPascal(string value)
        {
            string[] parts = string.IsNullOrWhiteSpace(value)
                ? new string[0]
                : value.Trim().ToLowerInvariant().Split('_');
            string result = string.Empty;
            foreach (string part in parts)
            {
                if (part.Length > 0)
                {
                    result += char.ToUpperInvariant(part[0]) + part.Substring(1);
                }
            }

            return result;
        }

        private static EnemyAnimationSpec ResolveEnemyAnimationSpec(string variantId)
        {
            string pascalId = ToPascal(variantId);
            string clipRoot = $"{EnemyAnimationsRoot}/{pascalId}/Clips";
            string controllerPath = $"{EnemyAnimationsRoot}/{pascalId}/{pascalId}_Enemy.controller";

            return variantId switch
            {
                "bat" => new EnemyAnimationSpec(controllerPath, clipRoot, "Bat@Idle.fbx", "Bat@Fly Forward In Place.fbx", "Bat@Bite Attack.fbx", "Bat@Projectile Attack.fbx", "Bat@Take Damage.fbx", "Bat@Die.fbx"),
                "bee" => new EnemyAnimationSpec(controllerPath, clipRoot, "Bee@Idle.fbx", "Bee@Fly Forward In Place.fbx", "Bee@Sting Attack.fbx", "Bee@Projectile Attack.fbx", "Bee@Take Damage.fbx", "Bee@Die.fbx"),
                "bird" => new EnemyAnimationSpec(controllerPath, clipRoot, "Bird@Fly Idle.fbx", "Bird@Fly Forward In Place.fbx", "Bird@Bite Attack.fbx", "Bird@Projectile Attack.fbx", "Bird@Take Damage.fbx", "Bird@Die.fbx"),
                "bumble" => new EnemyAnimationSpec(controllerPath, clipRoot, "Bumble@Idle.fbx", "Bumble@Fly Forward In Place.fbx", "Bumble@Sting Attack.fbx", "Bumble@Cast Spell.fbx", "Bumble@Take Damage.fbx", "Bumble@Die.fbx"),
                "ghost" => new EnemyAnimationSpec(controllerPath, clipRoot, "Ghost@Idle.fbx", "Ghost@Fly Forward In Place.fbx", "Ghost@Attack.fbx", string.Empty, "Ghost@Take Damage.fbx", "Ghost@Die.fbx"),
                "phantom" => new EnemyAnimationSpec(controllerPath, clipRoot, "Phantom@Idle.fbx", "Phantom@Fly Forward In Place.fbx", "Phantom@Right Slash Attack.fbx", "Phantom@Projectile Attack.fbx", "Phantom@Take Damage.fbx", "Phantom@Die.fbx"),
                "spook" => new EnemyAnimationSpec(controllerPath, clipRoot, "Spook@Idle.fbx", "Spook@Fly Forward In Place.fbx", "Spook@Slash Right Attack.fbx", "Spook@Projectile Attack.fbx", "Spook@Take Damage.fbx", "Spook@Die.fbx"),
                "sting" => new EnemyAnimationSpec(controllerPath, clipRoot, "Sting@Idle.fbx", "Sting@Fly Forward In Place.fbx", "Sting@Sting Attack.fbx", "Sting@Cast Spell.fbx", "Sting@Take Damage.fbx", "Sting@Die.fbx"),
                "fungi" => new EnemyAnimationSpec(controllerPath, clipRoot, "Fungi@Idle.fbx", "Fungi@Walk Forward In Place.fbx", "Fungi@Stab Attack.fbx", "Fungi@Projectile Attack.fbx", "Fungi@Take Damage.fbx", "Fungi@Die.fbx"),
                "mushroom" => new EnemyAnimationSpec(controllerPath, clipRoot, "Mushroom@Idle.fbx", "Mushroom@Jump Forward In Place.fbx", "Mushroom@Punch Attack.fbx", "Mushroom@Projectile Attack.fbx", "Mushroom@Take Damage.fbx", "Mushroom@Die.fbx"),
                "seed" => new EnemyAnimationSpec(controllerPath, clipRoot, "Seed@Idle.fbx", "Seed@Walk Forward In Place.fbx", "Seed@Head Attack.fbx", "Seed@Cast Spell.fbx", "Seed@Take Damage.fbx", "Seed@Die.fbx"),
                "shade" => new EnemyAnimationSpec(controllerPath, clipRoot, "Shade@Idle.fbx", "Shade@Walk Forward In Place.fbx", "Shade@Claw Right Attack.fbx", "Shade@Cast Spell.fbx", "Shade@Take Damage.fbx", "Shade@Die.fbx"),
                "shadow" => new EnemyAnimationSpec(controllerPath, clipRoot, "Shadow@Idle.fbx", "Shadow@Walk Forward In Place.fbx", "Shadow@Right Slash Attack.fbx", "Shadow@Projectile Attack.fbx", "Shadow@Take Damage.fbx", "Shadow@Die.fbx"),
                "sprout" => new EnemyAnimationSpec(controllerPath, clipRoot, "Sprout@Idle.fbx", "Sprout@Walk Forward In Place.fbx", "Sprout@Head Attack.fbx", "Sprout@Cast Spell.fbx", "Sprout@Take Damage.fbx", "Sprout@Die.fbx"),
                "toadstool" => new EnemyAnimationSpec(controllerPath, clipRoot, "Toadstool@Idle.fbx", "Toadstool@Walk Forward In Place.fbx", "Toadstool@Right Slash Attack.fbx", "Toadstool@Projectile Attack.fbx", "Toadstool@Take Damage.fbx", "Toadstool@Die.fbx"),
                "chick" => new EnemyAnimationSpec(controllerPath, clipRoot, "Chick@Idle.fbx", "Chick@Jump Forward In Place.fbx", "Chick@Bite Attack.fbx", "Chick@Cast Spell.fbx", "Chick@Take Damage.fbx", "Chick@Die.fbx"),
                "egg" => new EnemyAnimationSpec(controllerPath, clipRoot, "Egg@Idle.fbx", "Egg@Shake.fbx", "Egg@Shake.fbx", "Egg@Spawn.fbx", string.Empty, string.Empty),
                "fledgling" => new EnemyAnimationSpec(controllerPath, clipRoot, "Fledgling@Idle.fbx", "Fledgling@Walk Forward In Place.fbx", "Fledgling@Bite Attack.fbx", "Fledgling@Cast Spell.fbx", "Fledgling@Take Damage.fbx", "Fledgling@Die.fbx"),
                "spider" => new EnemyAnimationSpec(controllerPath, clipRoot, "Spider@Idle.fbx", "Spider@Crawl Forward Slow In Place.fbx", "Spider@Bite Attack.fbx", "Spider@Projectile Attack.fbx", "Spider@Take Damage.fbx", "Spider@Die.fbx"),
                _ => EnemyAnimationSpec.Fallback()
            };
        }

        private static void CreateBootstrapScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = MigrationSceneCatalog.Bootstrap;

            GameObject bootstrap = new GameObject("TouhouMigrationBootstrap");
            TouhouMigrationBootstrap bootstrapComponent = bootstrap.AddComponent<TouhouMigrationBootstrap>();

            SceneTransitionService transitionService = bootstrap.AddComponent<SceneTransitionService>();

            SerializedObject bootstrapSerialized = new SerializedObject(bootstrapComponent);
            bootstrapSerialized.FindProperty("sceneTransitionService").objectReferenceValue = transitionService;
            bootstrapSerialized.FindProperty("initialScene").enumValueIndex = (int)MigrationSceneId.TitleScreen;
            bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        private static void CreateTitleScreenScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = MigrationSceneCatalog.TitleScreen;

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;

            GameObject titleObject = new GameObject("TitleScreen");
            TitleScreenController titleController = titleObject.AddComponent<TitleScreenController>();
            MigrationSettingsController settingsController = titleObject.AddComponent<MigrationSettingsController>();
            MokouDeckEditorController deckEditorController = titleObject.AddComponent<MokouDeckEditorController>();

            SerializedObject titleSerialized = new SerializedObject(titleController);
            titleSerialized.FindProperty("background").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Texture2D>(MainMenuBackgroundPath);
            titleSerialized.FindProperty("titleFont").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Font>(TitleFontPath);
            titleSerialized.FindProperty("newGameScene").enumValueIndex = (int)MigrationSceneId.BambooHomeVerticalSlice;
            titleSerialized.FindProperty("settingsController").objectReferenceValue = settingsController;
            titleSerialized.FindProperty("deckEditorController").objectReferenceValue = deckEditorController;
            titleSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, TitleScreenScenePath);
        }

        private static void CreateBambooHomeVerticalSlice()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = MigrationSceneCatalog.BambooHomeVerticalSlice;

            GameObject root = new GameObject("BambooHomeVerticalSlice");
            CreateWorldSimulation(root.transform);
            CreateBlockoutGround(root.transform, "BambooHomeGround", new Vector3(6f, 1f, 6f));
            CreateMigrationPlayer(root.transform, new Vector3(0f, 1f, 0f));
            CreateFollowCamera(root.transform, new Vector3(0f, 6f, -8f), Quaternion.Euler(38f, 0f, 0f));
            CreateGlobalUi(root.transform);

            GameObject house = InstantiateAssetPrefab(
                BambooHousePath,
                "House3D",
                root.transform,
                new Vector3(4.8f, 0f, -3.7f),
                Quaternion.Euler(0f, 88.8f, 0f),
                new Vector3(12f, 12f, 12f));

            if (house == null)
            {
                GameObject homeMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                homeMarker.name = "BambooHomeBlockoutMarker";
                homeMarker.transform.SetParent(root.transform);
                homeMarker.transform.position = new Vector3(0f, 1.2f, 4f);
                homeMarker.transform.localScale = new Vector3(2.4f, 2.4f, 1.4f);
            }

            GameObject props = new GameObject("Props");
            props.transform.SetParent(root.transform);
            CreateBambooHomeProps(props.transform);
            CreateBambooHomeBed(root.transform);
            CreateCookingStation(root.transform, new Vector3(3.2f, 0.75f, -0.8f));
            CreatePortal(
                root.transform,
                "TownPortal",
                new Vector3(-10f, 1f, 15f),
                MigrationSceneId.HumanVillageVerticalSlice,
                new Color(0.2f, 0.55f, 1f, 0.45f));

            EditorSceneManager.SaveScene(scene, BambooHomeScenePath);
        }

        private static void CreateBambooHomeProps(Transform parent)
        {
            InstantiateAssetPrefab(BambooRocksPath, "Rock1", parent, new Vector3(-14f, 0f, 5f), Quaternion.identity, Vector3.one * 0.5f);
            InstantiateAssetPrefab(BambooRocksPath, "Rock2", parent, new Vector3(13f, 0f, -8f), Quaternion.identity, Vector3.one * 0.35f);
            InstantiateAssetPrefab(BambooRocksPath, "Rock3", parent, new Vector3(-18f, 0f, -10f), Quaternion.identity, Vector3.one * 0.45f);
            InstantiateAssetPrefab(BambooRocksPath, "Rock4", parent, new Vector3(16f, 0f, 8f), Quaternion.identity, Vector3.one * 0.3f);

            InstantiateAssetPrefab(BambooShootsPath, "Bamboo1", parent, new Vector3(-8f, 0f, 10f), Quaternion.identity, Vector3.one * 0.6f);
            InstantiateAssetPrefab(BambooShootsPath, "Bamboo2", parent, new Vector3(-16f, 0f, -4f), Quaternion.identity, Vector3.one * 0.5f);
            InstantiateAssetPrefab(BambooShootsPath, "Bamboo3", parent, new Vector3(17f, 0f, -12f), Quaternion.identity, Vector3.one * 0.65f);
            InstantiateAssetPrefab(BambooShootsPath, "Bamboo4", parent, new Vector3(-20f, 0f, 8f), Quaternion.identity, Vector3.one * 0.45f);
            InstantiateAssetPrefab(BambooShootsPath, "Bamboo5", parent, new Vector3(10f, 0f, -18f), Quaternion.identity, Vector3.one * 0.55f);

            InstantiateAssetPrefab(BambooLanternPath, "Lantern1", parent, new Vector3(-12f, 0f, 12f), Quaternion.identity, Vector3.one * 0.6f);
            InstantiateAssetPrefab(BambooLanternPath, "Lantern2", parent, new Vector3(17f, 0f, 12f), Quaternion.identity, Vector3.one * 0.6f);

            InstantiateAssetPrefab(BambooFlowersPath, "Flower1", parent, new Vector3(-6f, 0f, 4f), Quaternion.identity, Vector3.one * 0.7f);
            InstantiateAssetPrefab(BambooFlowersPath, "Flower2", parent, new Vector3(11f, 0f, 5f), Quaternion.identity, Vector3.one * 0.55f);
            InstantiateAssetPrefab(BambooFlowersPath, "Flower3", parent, new Vector3(-15f, 0f, 2f), Quaternion.identity, Vector3.one * 0.65f);
            InstantiateAssetPrefab(BambooFlowersPath, "Flower4", parent, new Vector3(6f, 0f, -14f), Quaternion.identity, Vector3.one * 0.5f);
            InstantiateAssetPrefab(BambooFlowersPath, "Flower5", parent, new Vector3(-19f, 0f, 13f), Quaternion.identity, Vector3.one * 0.6f);
        }

        private static void CreateHumanVillageVerticalSlice()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = MigrationSceneCatalog.HumanVillageVerticalSlice;

            GameObject root = new GameObject("HumanVillageVerticalSlice");
            CreateWorldSimulation(root.transform);

            if (!CreateHumanVillageTerrain(root.transform))
            {
                CreateBlockoutGround(root.transform, "BlockoutGround", new Vector3(8f, 1f, 8f));
            }

            CreateHumanVillageSetDressing(root.transform);
            CreateHumanVillageNpcMarkers(root.transform);
            CreateMigrationPlayer(root.transform, new Vector3(0f, 3f, 0f));
            CreateCombatTrainingTargets(root.transform);
            CreateFollowCamera(root.transform, new Vector3(0f, 48f, -90f), Quaternion.Euler(55f, 0f, 0f));
            CreateGlobalUi(root.transform);
            CreatePortal(
                root.transform,
                "BambooHomeReturnPortal",
                new Vector3(-18f, 2f, -18f),
                MigrationSceneId.BambooHomeVerticalSlice,
                new Color(0.95f, 0.66f, 0.25f, 0.45f));

            EditorSceneManager.SaveScene(scene, HumanVillageScenePath);
        }

        private static void CreateMokouCharacterValidationScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MokouCharacterValidation";

            GameObject root = new GameObject("MokouCharacterValidation");
            CreateWorldSimulation(root.transform);
            CreateBlockoutGround(root.transform, "CharacterValidationGround", new Vector3(4f, 1f, 4f));

            GameObject player = CreateMigrationPlayer(root.transform, new Vector3(0f, 1f, 0f));
            player.name = "MigrationPlayer_MokouVisual";

            GameObject referenceRig = InstantiateAssetPrefab(
                MokouReferenceRigPath,
                "ReimuMokouCcReferenceRig",
                root.transform,
                new Vector3(2.8f, 0f, 0f),
                Quaternion.Euler(0f, 180f, 0f),
                Vector3.one);

            if (referenceRig != null)
            {
                RemoveColliders(referenceRig);
                NormalizeVisualBounds(referenceRig, new Vector3(2.8f, 0f, 0f), 1.8f);
            }

            CreateAnimationImportMarkers(root.transform);
            CreateLookAtCamera(root.transform, new Vector3(0f, 2.1f, -5.5f), new Vector3(0f, 1.15f, 0f));

            EditorSceneManager.SaveScene(scene, MokouCharacterValidationScenePath);
        }

        private static void CreatePureNatureMeadowsScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = MigrationSceneCatalog.PureNatureMeadows;

            GameObject root = new GameObject("PureNatureMeadows");
            CreateWorldSimulation(root.transform);

            if (!CreatePureNatureMeadowsTerrain(root.transform))
            {
                CreateBlockoutGround(root.transform, "MeadowsBlockoutGround", new Vector3(12f, 1f, 12f));
            }

            CreatePureNatureMeadowsSetDressing(root.transform);

            float playerGroundY = SampleGroundY(0f, 0f, 0f);
            CreateMigrationPlayer(root.transform, new Vector3(0f, playerGroundY + 2f, 0f));
            // Low establishing angle centered on the grove: shows skybox + horizon mountains behind
            // the trees so the meadow reads as a place rather than a top-down field of fog.
            CreateFollowCamera(root.transform, new Vector3(0f, 32f, -62f), Quaternion.Euler(26f, 0f, 0f));
            CreateGlobalUi(root.transform);

            float portalGroundY = SampleGroundY(-16f, -10f, 0f);
            CreatePortal(
                root.transform,
                "BambooHomeReturnPortal",
                new Vector3(-16f, portalGroundY + 2f, -10f),
                MigrationSceneId.BambooHomeVerticalSlice,
                new Color(0.45f, 0.85f, 0.5f, 0.45f));

            EditorSceneManager.SaveScene(scene, PureNatureMeadowsScenePath);
        }

        private static bool CreatePureNatureMeadowsTerrain(Transform parent)
        {
            Material terrainMaterial = EnsureSimpleMaterial(
                MeadowsMaterialsRoot + "/MeadowsTerrain.mat",
                new Color(0.31f, 0.47f, 0.23f, 1f));

            GameObject terrain = InstantiateAssetPrefab(
                MeadowsTerrainPath,
                "PureNatureMeadowsTerrain",
                parent,
                Vector3.zero,
                Quaternion.identity,
                Vector3.one);

            if (terrain == null)
            {
                return false;
            }

            ApplyMaterialToRenderers(terrain, terrainMaterial);
            AddMeshColliders(terrain);
            CenterGroundOnPlane(terrain);
            // Sync so the freshly added terrain MeshCollider is queryable by the prop-grounding raycasts.
            Physics.SyncTransforms();

            // Drop the terrain so the spawn column (world origin) rests at y=0, regardless of where
            // this terrain's hills fall. This keeps the proven follow-camera framing valid for every
            // promoted environment variant.
            float originSurface = SampleGroundY(0f, 0f, float.NaN);
            if (!float.IsNaN(originSurface))
            {
                terrain.transform.position += new Vector3(0f, -originSurface, 0f);
                Physics.SyncTransforms();
            }

            return true;
        }

        private static void CreatePureNatureMeadowsSetDressing(Transform parent)
        {
            GameObject setDressing = new GameObject("MeadowsSetDressing");
            setDressing.transform.SetParent(parent);

            Material treeMaterial = EnsureSimpleMaterial(MeadowsMaterialsRoot + "/MeadowsTree.mat", new Color(0.18f, 0.40f, 0.20f, 1f));
            Material grassMaterial = EnsureSimpleMaterial(MeadowsMaterialsRoot + "/MeadowsGrass.mat", new Color(0.30f, 0.55f, 0.26f, 1f));
            Material flowerMaterial = EnsureSimpleMaterial(MeadowsMaterialsRoot + "/MeadowsFlower.mat", new Color(0.62f, 0.46f, 0.72f, 1f));
            Material rockMaterial = EnsureSimpleMaterial(MeadowsMaterialsRoot + "/MeadowsRock.mat", new Color(0.45f, 0.45f, 0.43f, 1f));
            Material mushroomMaterial = EnsureSimpleMaterial(MeadowsMaterialsRoot + "/MeadowsMushroom.mat", new Color(0.72f, 0.27f, 0.22f, 1f));
            Material mountainMaterial = EnsureSimpleMaterial(MeadowsMaterialsRoot + "/MeadowsMountain.mat", new Color(0.38f, 0.40f, 0.43f, 1f));

            Transform p = setDressing.transform;

            // Foreground grove clustered tightly around the spawn (within ~100u of the follow
            // camera at z=-90) so the exponential distance fog reads it crisply, mirroring the
            // proven Human Village framing. Trees are canopy-dominant: one flat foliage material.
            InstantiateMeadowProp(MeadowsTreesRoot, "Oak1", "Oak1", p, -30f, -10f, 20f, 16f, treeMaterial, true);
            InstantiateMeadowProp(MeadowsTreesRoot, "Oak2", "Oak2", p, 35f, -25f, -35f, 17f, treeMaterial, true);
            InstantiateMeadowProp(MeadowsTreesRoot, "Oak3", "Oak3", p, -48f, 12f, 60f, 16f, treeMaterial, true);
            InstantiateMeadowProp(MeadowsTreesRoot, "Birch1", "Birch1", p, 20f, 5f, 0f, 18f, treeMaterial, true);
            InstantiateMeadowProp(MeadowsTreesRoot, "Birch2", "Birch2", p, -14f, 25f, 120f, 17f, treeMaterial, true);
            InstantiateMeadowProp(MeadowsTreesRoot, "Birch3", "Birch3", p, 45f, 18f, -20f, 18f, treeMaterial, true);
            InstantiateMeadowProp(MeadowsTreesRoot, "Willow1", "Willow1", p, -55f, -20f, 200f, 15f, treeMaterial, true);
            InstantiateMeadowProp(MeadowsTreesRoot, "Elm1", "Elm1", p, 30f, 30f, -60f, 17f, treeMaterial, true);
            InstantiateMeadowProp(MeadowsTreesRoot, "Cypres1", "Cypres1", p, 52f, 0f, 0f, 19f, treeMaterial, true);
            InstantiateMeadowProp(MeadowsTreesRoot, "Bush1", "Bush1", p, -8f, -18f, 0f, 3.5f, treeMaterial, false);
            InstantiateMeadowProp(MeadowsTreesRoot, "Bush2", "Bush2", p, 10f, -12f, 45f, 4f, treeMaterial, false);

            // Rocks.
            InstantiateMeadowProp(MeadowsRocksRoot, "Stone1", "Stone1", p, -22f, -28f, 10f, 3f, rockMaterial, true);
            InstantiateMeadowProp(MeadowsRocksRoot, "Stone2", "Stone2", p, 18f, -30f, -15f, 2.8f, rockMaterial, true);
            InstantiateMeadowProp(MeadowsRocksRoot, "Stone3", "Stone3", p, -40f, 20f, 30f, 3.5f, rockMaterial, true);
            InstantiateMeadowProp(MeadowsRocksRoot, "Cliff1", "Cliff1", p, 60f, 28f, 30f, 10f, rockMaterial, true);

            // Mushrooms.
            InstantiateMeadowProp(MeadowsMushroomRoot, "Mushroom1", "Mushroom1", p, -6f, -8f, 0f, 2.2f, mushroomMaterial, false);
            InstantiateMeadowProp(MeadowsMushroomRoot, "Mushroom2", "Mushroom2", p, 6f, -2f, 90f, 2.4f, mushroomMaterial, false);

            // Plants, grass, flowers near the spawn.
            InstantiateMeadowProp(MeadowsPlantsRoot, "Grass1", "Grass1", p, -16f, -6f, 0f, 2f, grassMaterial, false);
            InstantiateMeadowProp(MeadowsPlantsRoot, "Grass2", "Grass2", p, 14f, 8f, 0f, 2f, grassMaterial, false);
            InstantiateMeadowProp(MeadowsPlantsRoot, "FlowerMeadow1", "FlowerMeadow1", p, -10f, 2f, 0f, 1.8f, flowerMaterial, false);
            InstantiateMeadowProp(MeadowsPlantsRoot, "FlowerMeadow2", "FlowerMeadow2", p, 22f, -4f, 0f, 1.8f, flowerMaterial, false);
            InstantiateMeadowProp(MeadowsPlantsRoot, "Lupin1", "Lupin1", p, 4f, 14f, 0f, 2.4f, flowerMaterial, false);
            InstantiateMeadowProp(MeadowsPlantsRoot, "Daisy", "Daisy", p, -20f, 8f, 0f, 1.6f, flowerMaterial, false);
            InstantiateMeadowProp(MeadowsPlantsRoot, "Lavender", "Lavender", p, 26f, 6f, 0f, 2f, flowerMaterial, false);

            // Background hills on the near horizon (deliberately faint behind the fog).
            InstantiateMeadowProp(MeadowsMountainsRoot, "Mountain1", "Mountain1", p, -90f, 150f, 20f, 90f, mountainMaterial, false);
            InstantiateMeadowProp(MeadowsMountainsRoot, "Mountain2", "Mountain2", p, 95f, 165f, -30f, 90f, mountainMaterial, false);
        }

        private static GameObject InstantiateMeadowProp(
            string folderRoot,
            string fileName,
            string instanceName,
            Transform parent,
            float x,
            float z,
            float yaw,
            float targetHeight,
            Material material,
            bool addCollider)
        {
            GameObject instance = InstantiateAssetPrefab(
                $"{folderRoot}/{fileName}.fbx",
                instanceName,
                parent,
                Vector3.zero,
                Quaternion.Euler(0f, yaw, 0f),
                Vector3.one);

            if (instance == null)
            {
                return null;
            }

            ApplyMaterialToRenderers(instance, material);
            float groundY = SampleGroundY(x, z, 0f);
            NormalizeVisualBounds(instance, new Vector3(x, groundY, z), targetHeight);
            if (addCollider)
            {
                AddMeshColliders(instance);
            }

            return instance;
        }

        // Raycasts straight down onto the already-built terrain collider to find ground height.
        // Falls back gracefully when edit-mode physics can't resolve a hit.
        private static float SampleGroundY(float x, float z, float fallback)
        {
            if (Physics.Raycast(new Vector3(x, 2000f, z), Vector3.down, out RaycastHit hit, 8000f))
            {
                return hit.point.y;
            }

            return fallback;
        }

        // Re-centers a ground mesh so its XZ bounds sit on the world origin and its base rests on y=0.
        private static void CenterGroundOnPlane(GameObject go)
        {
            if (!TryCalculateRendererBounds(go, out Bounds bounds))
            {
                return;
            }

            Vector3 shift = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            go.transform.position += shift;
        }

        // ----- Generic PureNature variant scenes (reuse the proven meadow recipe) -----------------

        private readonly struct LocationPropSlot
        {
            public LocationPropSlot(string category, string materialKey, float x, float z, float yaw, float height, bool collider)
            {
                Category = category;
                MaterialKey = materialKey;
                X = x;
                Z = z;
                Yaw = yaw;
                Height = height;
                Collider = collider;
            }

            public string Category { get; }      // sub-folder under the art root to discover meshes from
            public string MaterialKey { get; }   // which flat material to apply
            public float X { get; }
            public float Z { get; }
            public float Yaw { get; }
            public float Height { get; }
            public bool Collider { get; }
        }

        // Shared grove layout (mirrors the validated meadow placement). Slots whose category has no
        // promoted meshes are simply skipped, so each pack ships whatever species it actually has.
        private static readonly LocationPropSlot[] LocationGroveLayout =
        {
            new LocationPropSlot("Trees", "tree", -30f, -10f, 20f, 16f, true),
            new LocationPropSlot("Trees", "tree", 35f, -25f, -35f, 17f, true),
            new LocationPropSlot("Trees", "tree", -48f, 12f, 60f, 16f, true),
            new LocationPropSlot("Trees", "tree", 20f, 5f, 0f, 18f, true),
            new LocationPropSlot("Trees", "tree", -14f, 25f, 120f, 17f, true),
            new LocationPropSlot("Trees", "tree", 45f, 18f, -20f, 18f, true),
            new LocationPropSlot("Trees", "tree", -55f, -20f, 200f, 15f, true),
            new LocationPropSlot("Trees", "tree", 30f, 30f, -60f, 17f, true),
            new LocationPropSlot("Trees", "tree", 52f, 0f, 0f, 19f, true),
            new LocationPropSlot("Trees", "tree", -8f, -18f, 0f, 5f, false),
            new LocationPropSlot("Trees", "tree", 10f, -12f, 45f, 5.5f, false),
            new LocationPropSlot("Rocks", "rock", -22f, -28f, 10f, 3f, true),
            new LocationPropSlot("Rocks", "rock", 18f, -30f, -15f, 2.8f, true),
            new LocationPropSlot("Rocks", "rock", -40f, 20f, 30f, 3.5f, true),
            new LocationPropSlot("Rocks", "rock", 60f, 28f, 30f, 10f, true),
            new LocationPropSlot("Mushroom", "mushroom", -6f, -8f, 0f, 2.2f, false),
            new LocationPropSlot("Mushroom", "mushroom", 6f, -2f, 90f, 2.4f, false),
            new LocationPropSlot("Plants", "grass", -16f, -6f, 0f, 2f, false),
            new LocationPropSlot("Plants", "grass", 14f, 8f, 0f, 2f, false),
            new LocationPropSlot("Plants", "flower", -10f, 2f, 0f, 1.8f, false),
            new LocationPropSlot("Plants", "flower", 22f, -4f, 0f, 1.8f, false),
            new LocationPropSlot("Plants", "flower", 4f, 14f, 0f, 2.4f, false),
            new LocationPropSlot("Plants", "flower", -20f, 8f, 0f, 1.6f, false),
            new LocationPropSlot("Plants", "flower", 26f, 6f, 0f, 2f, false),
            new LocationPropSlot("Mountains", "mountain", -90f, 150f, 20f, 90f, false),
            new LocationPropSlot("Mountains", "mountain", 95f, 165f, -30f, 90f, false),
        };

        private static void CreatePureNatureVariantScenes()
        {
            CreateNatureLocationScene(MigrationSceneCatalog.PureNatureClassic, ClassicScenePath, LocationsArtRoot + "/PureNatureClassic", LocationsArtRoot + "/PureNatureClassic/Terrain/terrain.obj",
                MigrationSceneId.BambooHomeVerticalSlice, new Color(0.55f, 0.85f, 0.45f, 0.45f), new Color(0.34f, 0.46f, 0.24f, 1f), new Color(0.20f, 0.42f, 0.22f, 1f));
            CreateNatureLocationScene(MigrationSceneCatalog.PureNatureJungle, JungleScenePath, LocationsArtRoot + "/PureNatureJungle", LocationsArtRoot + "/PureNatureJungle/Terrain/terrain.obj",
                MigrationSceneId.BambooHomeVerticalSlice, new Color(0.30f, 0.75f, 0.40f, 0.45f), new Color(0.18f, 0.38f, 0.16f, 1f), new Color(0.12f, 0.34f, 0.14f, 1f));
            CreateNatureLocationScene(MigrationSceneCatalog.PureNatureIslands, IslandsScenePath, LocationsArtRoot + "/PureNatureIslands", LocationsArtRoot + "/PureNatureIslands/Terrain/terrain.obj",
                MigrationSceneId.BambooHomeVerticalSlice, new Color(0.35f, 0.80f, 0.85f, 0.45f), new Color(0.44f, 0.50f, 0.30f, 1f), new Color(0.22f, 0.46f, 0.24f, 1f));
            CreateNatureLocationScene(MigrationSceneCatalog.PureNatureMountains, MountainsScenePath, LocationsArtRoot + "/PureNatureMountains", LocationsArtRoot + "/PureNatureMountains/Terrain/terrain.obj",
                MigrationSceneId.BambooHomeVerticalSlice, new Color(0.55f, 0.62f, 0.75f, 0.45f), new Color(0.34f, 0.40f, 0.30f, 1f), new Color(0.22f, 0.40f, 0.24f, 1f));
            CreateNatureLocationScene(MigrationSceneCatalog.PureNatureFantasyForest, FantasyForestScenePath, LocationsArtRoot + "/PureNatureFantasyForest", LocationsArtRoot + "/PureNatureFantasyForest/Terrain/terrain.obj",
                MigrationSceneId.BambooHomeVerticalSlice, new Color(0.78f, 0.45f, 0.85f, 0.45f), new Color(0.26f, 0.42f, 0.28f, 1f), new Color(0.18f, 0.40f, 0.26f, 1f));

            // AngryMesh meadow ships no terrain export → flat ground (empty terrain path).
            CreateNatureLocationScene(MigrationSceneCatalog.AngryMeshMeadow, AngryMeshMeadowScenePath, LocationsArtRoot + "/AngryMeshMeadow", string.Empty,
                MigrationSceneId.BambooHomeVerticalSlice, new Color(0.85f, 0.78f, 0.40f, 0.45f), new Color(0.33f, 0.49f, 0.24f, 1f), new Color(0.22f, 0.44f, 0.22f, 1f));
        }

        // Village/town locations: flat ground + Suntail houses, shop/well/cart/bridge props, and
        // trees, reusing the shared promoted Suntail meshes with per-location materials.
        private static void CreateVillageScenes()
        {
            CreateVillageLocationScene(MigrationSceneCatalog.TownWorld, TownWorldScenePath, LocationsArtRoot + "/TownWorld",
                MigrationSceneId.BambooHomeVerticalSlice, new Color(0.95f, 0.66f, 0.25f, 0.45f), new Color(0.64f, 0.48f, 0.34f, 1f), new Color(0.32f, 0.44f, 0.24f, 1f), new Color(0.20f, 0.42f, 0.23f, 1f), 0);
            CreateVillageLocationScene(MigrationSceneCatalog.FantasyVillage, FantasyVillageScenePath, LocationsArtRoot + "/FantasyVillage",
                MigrationSceneId.BambooHomeVerticalSlice, new Color(0.80f, 0.45f, 0.85f, 0.45f), new Color(0.70f, 0.55f, 0.42f, 1f), new Color(0.30f, 0.48f, 0.28f, 1f), new Color(0.22f, 0.46f, 0.26f, 1f), 4);
            CreateVillageLocationScene(MigrationSceneCatalog.SuntailVillagePlayable, SuntailVillagePlayableScenePath, LocationsArtRoot + "/SuntailVillagePlayable",
                MigrationSceneId.BambooHomeVerticalSlice, new Color(0.95f, 0.82f, 0.40f, 0.45f), new Color(0.66f, 0.50f, 0.36f, 1f), new Color(0.33f, 0.45f, 0.25f, 1f), new Color(0.21f, 0.43f, 0.24f, 1f), 8);
            CreateVillageLocationScene(MigrationSceneCatalog.SuntailVillageImported, SuntailVillageImportedScenePath, LocationsArtRoot + "/SuntailVillageImported",
                MigrationSceneId.BambooHomeVerticalSlice, new Color(0.90f, 0.78f, 0.45f, 0.45f), new Color(0.62f, 0.47f, 0.33f, 1f), new Color(0.31f, 0.46f, 0.26f, 1f), new Color(0.20f, 0.42f, 0.23f, 1f), 12);
        }

        private static void CreateVillageLocationScene(
            string sceneName,
            string scenePath,
            string artRoot,
            MigrationSceneId returnTo,
            Color portalColor,
            Color buildingColor,
            Color groundColor,
            Color natureColor,
            int npcStartIndex)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = sceneName;

            GameObject root = new GameObject(sceneName);
            CreateWorldSimulation(root.transform);
            EnsureAssetFolder(artRoot + "/Materials");
            CreateFlatGround(root.transform, sceneName + "Ground", artRoot + "/Materials/Ground.mat", groundColor);

            Material building = EnsureSimpleMaterial(artRoot + "/Materials/Building.mat", buildingColor);
            Material prop = EnsureSimpleMaterial(artRoot + "/Materials/Prop.mat", new Color(0.50f, 0.36f, 0.24f, 1f));
            Material stone = EnsureSimpleMaterial(artRoot + "/Materials/Stone.mat", new Color(0.45f, 0.45f, 0.42f, 1f));
            Material nature = EnsureSimpleMaterial(artRoot + "/Materials/Nature.mat", natureColor);

            GameObject dress = new GameObject("VillageSetDressing");
            dress.transform.SetParent(root.transform);
            Transform p = dress.transform;

            string b = HumanVillageBuildingPrefabsRoot;
            string e = HumanVillageEnvironmentModelsRoot;
            string n = HumanVillageNatureModelsRoot;

            InstantiateLocationProp($"{b}/House_1.prefab", "House1", p, -30f, 22f, 30f, 10f, building, true);
            InstantiateLocationProp($"{b}/House_3.prefab", "House2", p, 30f, 26f, -25f, 10f, building, true);
            InstantiateLocationProp($"{b}/House_5.prefab", "House3", p, -38f, -16f, 15f, 10f, building, true);
            InstantiateLocationProp($"{b}/House_2.prefab", "House4", p, 36f, -12f, -60f, 10f, building, true);
            InstantiateLocationProp($"{b}/House_4.prefab", "House5", p, 8f, 38f, 180f, 10f, building, true);
            InstantiateLocationProp($"{b}/House_6.prefab", "House6", p, -12f, -4f, 90f, 9f, building, true);

            GameObject shop1 = InstantiateLocationProp($"{e}/Shop_1.fbx", "Shop1", p, -14f, 12f, 30f, 6f, prop, true);
            if (shop1 != null)
            {
                shop1.AddComponent<MigrationShopInteractor>().Configure("town_general");
            }
            GameObject shop2 = InstantiateLocationProp($"{e}/Shop_2.fbx", "Shop2", p, 14f, 10f, -25f, 6f, prop, true);
            if (shop2 != null)
            {
                shop2.AddComponent<MigrationShopInteractor>().Configure("town_blacksmith");
            }
            InstantiateLocationProp($"{e}/Well_1.fbx", "Well", p, 3f, 3f, 0f, 3.5f, stone, true);
            InstantiateLocationProp($"{e}/Cart_1.fbx", "Cart", p, -6f, -12f, 60f, 3f, prop, true);
            InstantiateLocationProp($"{e}/Lantern_1.fbx", "Lantern1", p, 6f, 15f, 0f, 4.5f, prop, false);
            InstantiateLocationProp($"{e}/Lantern_2.fbx", "Lantern2", p, -18f, 8f, 0f, 4.5f, prop, false);
            InstantiateLocationProp($"{e}/Barrel.fbx", "Barrel", p, 17f, -2f, 0f, 2.5f, prop, false);
            InstantiateLocationProp($"{e}/Bridge_centre.fbx", "Bridge", p, 0f, -30f, 90f, 3f, prop, true);

            InstantiateLocationProp($"{n}/Broadleaf_1.fbx", "Tree1", p, -46f, 30f, 0f, 16f, nature, true);
            InstantiateLocationProp($"{n}/Broadleaf_2.fbx", "Tree2", p, 46f, 30f, 25f, 16f, nature, true);
            InstantiateLocationProp($"{n}/Broadleaf_3.fbx", "Tree3", p, 42f, -34f, -45f, 16f, nature, true);
            InstantiateLocationProp($"{n}/Bush_1.fbx", "Bush1", p, -22f, 16f, 0f, 4f, nature, false);
            InstantiateLocationProp($"{n}/Bush_2.fbx", "Bush2", p, 22f, 18f, 0f, 4f, nature, false);
            InstantiateLocationProp($"{n}/Stone_1.fbx", "Stone1", p, -34f, -8f, 0f, 3f, stone, true);
            InstantiateLocationProp($"{n}/Stone_2.fbx", "Stone2", p, 34f, -22f, 0f, 3f, stone, true);

            // Populate the village with a distinct subset of the canonical cast as interactable markers.
            GameObject npcRoot = new GameObject("NPCMarkers");
            npcRoot.transform.SetParent(root.transform);
            const int villageNpcCount = 5;
            for (int k = 0; k < villageNpcCount; k++)
            {
                int idx = (npcStartIndex + k) % HumanVillageNpcIds.Length;
                float angle = (k / (float)villageNpcCount) * Mathf.PI * 2f;
                float radius = 16f + (k % 2) * 8f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 2.4f, Mathf.Sin(angle) * radius + 8f);
                Color color = Color.HSVToRGB((idx * 0.137f) % 1f, 0.62f, 0.92f);
                CreateNpcMarker(npcRoot.transform, HumanVillageNpcIds[idx], HumanVillageNpcNames[idx], HumanVillageNpcGifts[idx], pos, color);
            }

            float playerGroundY = SampleGroundY(-4f, -6f, 0f);
            CreateMigrationPlayer(root.transform, new Vector3(-4f, playerGroundY + 2f, -6f));
            CreateFollowCamera(root.transform, new Vector3(0f, 34f, -66f), Quaternion.Euler(28f, 0f, 0f));
            CreateGlobalUi(root.transform);

            float portalGroundY = SampleGroundY(-20f, -14f, 0f);
            CreatePortal(root.transform, "BambooHomeReturnPortal", new Vector3(-20f, portalGroundY + 2f, -14f), returnTo, portalColor);

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        // Landmark structure locations (shrine/mansion/dungeon/farm): flat ground + a signature
        // primitive-built landmark + Suntail nature dressing. Blockout-grade; E7 swaps in real models.
        private static void CreateLandmarkScenes()
        {
            CreateShrineScene();
            CreateMansionScene();
            CreateDungeonEntranceScene();
            CreateFarmScene();
        }

        private static GameObject CreatePrimitiveBlock(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Vector3 euler, Material mat, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localEulerAngles = euler;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && mat != null)
            {
                renderer.sharedMaterial = mat;
            }
            if (!collider)
            {
                Collider c = go.GetComponent<Collider>();
                if (c != null)
                {
                    Object.DestroyImmediate(c);
                }
            }
            return go;
        }

        private static void FinishLocationScene(GameObject root, string scenePath, MigrationSceneId returnTo, Color portalColor, Scene scene)
        {
            float playerGroundY = SampleGroundY(0f, -8f, 0f);
            CreateMigrationPlayer(root.transform, new Vector3(0f, playerGroundY + 2f, -8f));
            CreateFollowCamera(root.transform, new Vector3(0f, 34f, -66f), Quaternion.Euler(27f, 0f, 0f));
            CreateGlobalUi(root.transform);
            float portalGroundY = SampleGroundY(-20f, -16f, 0f);
            CreatePortal(root.transform, "BambooHomeReturnPortal", new Vector3(-20f, portalGroundY + 2f, -16f), returnTo, portalColor);
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void CreateShrineScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = MigrationSceneCatalog.HakureiShrine;
            GameObject root = new GameObject(MigrationSceneCatalog.HakureiShrine);
            CreateWorldSimulation(root.transform);
            string artRoot = LocationsArtRoot + "/HakureiShrine";
            EnsureAssetFolder(artRoot + "/Materials");
            CreateFlatGround(root.transform, "ShrineGround", artRoot + "/Materials/Ground.mat", new Color(0.40f, 0.44f, 0.30f, 1f));

            Material red = EnsureSimpleMaterial(artRoot + "/Materials/Red.mat", new Color(0.80f, 0.18f, 0.16f, 1f));
            Material white = EnsureSimpleMaterial(artRoot + "/Materials/White.mat", new Color(0.92f, 0.90f, 0.86f, 1f));
            Material stone = EnsureSimpleMaterial(artRoot + "/Materials/Stone.mat", new Color(0.50f, 0.50f, 0.48f, 1f));
            Material nature = EnsureSimpleMaterial(artRoot + "/Materials/Nature.mat", new Color(0.20f, 0.42f, 0.22f, 1f));

            GameObject lm = new GameObject("ShrineLandmark");
            lm.transform.SetParent(root.transform);
            Transform t = lm.transform;
            // Torii gate at the approach.
            CreatePrimitiveBlock(t, "ToriiPillarL", PrimitiveType.Cylinder, new Vector3(-4f, 3f, 18f), new Vector3(0.5f, 3f, 0.5f), Vector3.zero, red, true);
            CreatePrimitiveBlock(t, "ToriiPillarR", PrimitiveType.Cylinder, new Vector3(4f, 3f, 18f), new Vector3(0.5f, 3f, 0.5f), Vector3.zero, red, true);
            CreatePrimitiveBlock(t, "ToriiTop", PrimitiveType.Cube, new Vector3(0f, 6.2f, 18f), new Vector3(11f, 0.7f, 0.9f), Vector3.zero, red, false);
            CreatePrimitiveBlock(t, "ToriiTie", PrimitiveType.Cube, new Vector3(0f, 5.2f, 18f), new Vector3(9f, 0.5f, 0.6f), Vector3.zero, red, false);
            // Shrine hall.
            CreatePrimitiveBlock(t, "ShrineHall", PrimitiveType.Cube, new Vector3(0f, 2.5f, -8f), new Vector3(12f, 5f, 9f), Vector3.zero, white, true);
            CreatePrimitiveBlock(t, "ShrineRoof", PrimitiveType.Cube, new Vector3(0f, 5.4f, -8f), new Vector3(14f, 0.7f, 11f), Vector3.zero, red, false);
            CreatePrimitiveBlock(t, "LanternL", PrimitiveType.Cube, new Vector3(-7f, 1f, 8f), new Vector3(0.9f, 2f, 0.9f), Vector3.zero, stone, true);
            CreatePrimitiveBlock(t, "LanternR", PrimitiveType.Cube, new Vector3(7f, 1f, 8f), new Vector3(0.9f, 2f, 0.9f), Vector3.zero, stone, true);

            InstantiateLocationProp($"{HumanVillageNatureModelsRoot}/Broadleaf_1.fbx", "Tree1", t, -18f, 6f, 0f, 14f, nature, true);
            InstantiateLocationProp($"{HumanVillageNatureModelsRoot}/Broadleaf_2.fbx", "Tree2", t, 18f, 10f, 25f, 14f, nature, true);
            InstantiateLocationProp($"{HumanVillageNatureModelsRoot}/Stone_1.fbx", "Stone1", t, -12f, -16f, 0f, 3f, stone, true);

            CreateLocationNpcCast(root.transform, new[]
            {
                ("reimu", "博丽灵梦", "green_tea"),
                ("sanae", "东风谷早苗", "sunflower"),
                ("suika", "伊吹萃香", "sake"),
            }, 8f);

            FinishLocationScene(root, HakureiShrineScenePath, MigrationSceneId.BambooHomeVerticalSlice, new Color(0.90f, 0.30f, 0.28f, 0.45f), scene);
        }

        private static void CreateMansionScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = MigrationSceneCatalog.ScarletMansionFront;
            GameObject root = new GameObject(MigrationSceneCatalog.ScarletMansionFront);
            CreateWorldSimulation(root.transform);
            string artRoot = LocationsArtRoot + "/ScarletMansionFront";
            EnsureAssetFolder(artRoot + "/Materials");
            CreateFlatGround(root.transform, "MansionGround", artRoot + "/Materials/Ground.mat", new Color(0.26f, 0.30f, 0.24f, 1f));

            Material wall = EnsureSimpleMaterial(artRoot + "/Materials/Wall.mat", new Color(0.55f, 0.14f, 0.16f, 1f));
            Material roof = EnsureSimpleMaterial(artRoot + "/Materials/Roof.mat", new Color(0.30f, 0.10f, 0.12f, 1f));
            Material stone = EnsureSimpleMaterial(artRoot + "/Materials/Stone.mat", new Color(0.42f, 0.40f, 0.42f, 1f));
            Material nature = EnsureSimpleMaterial(artRoot + "/Materials/Nature.mat", new Color(0.16f, 0.32f, 0.20f, 1f));

            GameObject lm = new GameObject("MansionLandmark");
            lm.transform.SetParent(root.transform);
            Transform t = lm.transform;
            CreatePrimitiveBlock(t, "MansionMain", PrimitiveType.Cube, new Vector3(0f, 6f, -10f), new Vector3(26f, 12f, 14f), Vector3.zero, wall, true);
            CreatePrimitiveBlock(t, "MansionRoof", PrimitiveType.Cube, new Vector3(0f, 12.4f, -10f), new Vector3(28f, 1f, 16f), Vector3.zero, roof, false);
            CreatePrimitiveBlock(t, "TowerL", PrimitiveType.Cube, new Vector3(-12f, 10f, -10f), new Vector3(5f, 20f, 5f), Vector3.zero, wall, true);
            CreatePrimitiveBlock(t, "TowerR", PrimitiveType.Cube, new Vector3(12f, 10f, -10f), new Vector3(5f, 20f, 5f), Vector3.zero, wall, true);
            CreatePrimitiveBlock(t, "TowerRoofL", PrimitiveType.Cylinder, new Vector3(-12f, 21f, -10f), new Vector3(3.5f, 3f, 3.5f), Vector3.zero, roof, false);
            CreatePrimitiveBlock(t, "TowerRoofR", PrimitiveType.Cylinder, new Vector3(12f, 21f, -10f), new Vector3(3.5f, 3f, 3.5f), Vector3.zero, roof, false);
            CreatePrimitiveBlock(t, "GatePillarL", PrimitiveType.Cube, new Vector3(-9f, 2.5f, 12f), new Vector3(1.2f, 5f, 1.2f), Vector3.zero, stone, true);
            CreatePrimitiveBlock(t, "GatePillarR", PrimitiveType.Cube, new Vector3(9f, 2.5f, 12f), new Vector3(1.2f, 5f, 1.2f), Vector3.zero, stone, true);

            InstantiateLocationProp($"{HumanVillageNatureModelsRoot}/Broadleaf_1.fbx", "Tree1", t, -22f, 8f, 0f, 13f, nature, true);
            InstantiateLocationProp($"{HumanVillageNatureModelsRoot}/Broadleaf_3.fbx", "Tree2", t, 22f, 8f, -30f, 13f, nature, true);

            CreateLocationNpcCast(root.transform, new[]
            {
                ("remilia", "蕾米莉亚·斯卡蕾特", "black_tea"),
                ("sakuya", "十六夜咲夜", "sake"),
                ("patchouli", "帕秋莉·诺蕾姬", "black_tea"),
                ("meiling", "红美铃", "dango"),
            }, 9f);

            FinishLocationScene(root, ScarletMansionFrontScenePath, MigrationSceneId.BambooHomeVerticalSlice, new Color(0.75f, 0.20f, 0.30f, 0.45f), scene);
        }

        private static void CreateDungeonEntranceScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = MigrationSceneCatalog.DungeonEntrance;
            GameObject root = new GameObject(MigrationSceneCatalog.DungeonEntrance);
            CreateWorldSimulation(root.transform);
            string artRoot = LocationsArtRoot + "/DungeonEntrance";
            EnsureAssetFolder(artRoot + "/Materials");
            CreateFlatGround(root.transform, "DungeonGround", artRoot + "/Materials/Ground.mat", new Color(0.28f, 0.30f, 0.26f, 1f));

            Material rock = EnsureSimpleMaterial(artRoot + "/Materials/Rock.mat", new Color(0.34f, 0.34f, 0.36f, 1f));
            Material dark = EnsureSimpleMaterial(artRoot + "/Materials/Dark.mat", new Color(0.05f, 0.05f, 0.06f, 1f));
            Material nature = EnsureSimpleMaterial(artRoot + "/Materials/Nature.mat", new Color(0.18f, 0.34f, 0.20f, 1f));

            GameObject lm = new GameObject("DungeonLandmark");
            lm.transform.SetParent(root.transform);
            Transform t = lm.transform;
            // Rocky cliff face with a dark cave mouth.
            CreatePrimitiveBlock(t, "Cliff", PrimitiveType.Cube, new Vector3(0f, 9f, -12f), new Vector3(34f, 18f, 10f), Vector3.zero, rock, true);
            CreatePrimitiveBlock(t, "CaveMouth", PrimitiveType.Cube, new Vector3(0f, 4f, -6f), new Vector3(8f, 8f, 4f), Vector3.zero, dark, false);
            CreatePrimitiveBlock(t, "BoulderL", PrimitiveType.Sphere, new Vector3(-9f, 2f, -2f), new Vector3(5f, 4f, 5f), Vector3.zero, rock, true);
            CreatePrimitiveBlock(t, "BoulderR", PrimitiveType.Sphere, new Vector3(9f, 2.5f, -1f), new Vector3(6f, 5f, 6f), Vector3.zero, rock, true);
            CreatePrimitiveBlock(t, "TorchL", PrimitiveType.Cylinder, new Vector3(-5f, 2f, -3.5f), new Vector3(0.3f, 2f, 0.3f), Vector3.zero, dark, false);
            CreatePrimitiveBlock(t, "TorchR", PrimitiveType.Cylinder, new Vector3(5f, 2f, -3.5f), new Vector3(0.3f, 2f, 0.3f), Vector3.zero, dark, false);

            InstantiateLocationProp($"{HumanVillageNatureModelsRoot}/Stone_1.fbx", "Stone1", t, -16f, 6f, 0f, 4f, rock, true);
            InstantiateLocationProp($"{HumanVillageNatureModelsRoot}/Stone_2.fbx", "Stone2", t, 16f, 8f, 30f, 4f, rock, true);
            InstantiateLocationProp($"{HumanVillageNatureModelsRoot}/Broadleaf_2.fbx", "Tree1", t, -20f, 14f, 0f, 12f, nature, true);

            // Chireiden (underground) cast at the dungeon mouth.
            CreateLocationNpcCast(root.transform, new[]
            {
                ("satori", "古明地觉", "youkan"),
                ("koishi", "古明地恋", "dango"),
                ("utsuho", "灵乌路空", "youkan"),
                ("rin", "火焰猫燐", "rice_ball"),
            }, 8f);

            FinishLocationScene(root, DungeonEntranceScenePath, MigrationSceneId.BambooHomeVerticalSlice, new Color(0.55f, 0.45f, 0.30f, 0.45f), scene);
        }

        private static void CreateFarmScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = MigrationSceneCatalog.Farm;
            GameObject root = new GameObject(MigrationSceneCatalog.Farm);
            CreateWorldSimulation(root.transform);
            string artRoot = LocationsArtRoot + "/Farm";
            EnsureAssetFolder(artRoot + "/Materials");
            CreateFlatGround(root.transform, "FarmGround", artRoot + "/Materials/Ground.mat", new Color(0.36f, 0.46f, 0.24f, 1f));

            Material soil = EnsureSimpleMaterial(artRoot + "/Materials/Soil.mat", new Color(0.34f, 0.24f, 0.16f, 1f));
            Material crop = EnsureSimpleMaterial(artRoot + "/Materials/Crop.mat", new Color(0.30f, 0.60f, 0.26f, 1f));
            Material wood = EnsureSimpleMaterial(artRoot + "/Materials/Wood.mat", new Color(0.52f, 0.38f, 0.24f, 1f));
            Material nature = EnsureSimpleMaterial(artRoot + "/Materials/Nature.mat", new Color(0.20f, 0.42f, 0.22f, 1f));

            GameObject lm = new GameObject("FarmLandmark");
            lm.transform.SetParent(root.transform);
            Transform t = lm.transform;
            // Tilled plot rows with crop rows on top; the first 9 crops are workable plots wired to the
            // FarmingManager (plant/water/harvest via MigrationFarmPlotInteractor; growth via the day-loop).
            int plotCounter = 0;
            for (int row = 0; row < 4; row++)
            {
                float z = -4f + row * 6f;
                CreatePrimitiveBlock(t, $"Plot_{row}", PrimitiveType.Cube, new Vector3(0f, 0.15f, z), new Vector3(20f, 0.3f, 3.2f), Vector3.zero, soil, false);
                for (int col = -2; col <= 2; col++)
                {
                    GameObject cropCube = CreatePrimitiveBlock(t, $"Crop_{row}_{col}", PrimitiveType.Cube, new Vector3(col * 4f, 0.7f, z), new Vector3(0.8f, 1.1f, 0.8f), Vector3.zero, crop, false);
                    if (plotCounter < 9)
                    {
                        cropCube.AddComponent<MigrationFarmPlotInteractor>().Configure(plotCounter, "crop_turnip");
                        plotCounter++;
                    }
                }
            }
            // Barn (Suntail house) + fences.
            InstantiateLocationProp($"{HumanVillageBuildingPrefabsRoot}/House_7.prefab", "Barn", t, -16f, 18f, 40f, 11f, wood, true);
            InstantiateLocationProp($"{HumanVillageEnvironmentModelsRoot}/Fence_1.fbx", "FenceA", t, -12f, -10f, 90f, 2.5f, wood, false);
            InstantiateLocationProp($"{HumanVillageEnvironmentModelsRoot}/Fence_2.fbx", "FenceB", t, 12f, -10f, 90f, 2.5f, wood, false);
            InstantiateLocationProp($"{HumanVillageNatureModelsRoot}/Broadleaf_1.fbx", "Tree1", t, 20f, 16f, 0f, 13f, nature, true);

            FinishLocationScene(root, FarmScenePath, MigrationSceneId.BambooHomeVerticalSlice, new Color(0.55f, 0.80f, 0.35f, 0.45f), scene);
        }

        // Main/entry flow scenes: post-title main menu, loading screen, and the overworld hub.
        private static void CreateMainEntryScenes()
        {
            CreateMainMenuScene();
            CreateLoadingScreenScene();
            CreateWorldScene();
        }

        private static void CreateMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = MigrationSceneCatalog.MainMenu;

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;

            GameObject menuObject = new GameObject("MainMenu");
            TitleScreenController titleController = menuObject.AddComponent<TitleScreenController>();
            MigrationSettingsController settingsController = menuObject.AddComponent<MigrationSettingsController>();
            MokouDeckEditorController deckEditorController = menuObject.AddComponent<MokouDeckEditorController>();

            SerializedObject s = new SerializedObject(titleController);
            s.FindProperty("background").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(MainMenuBackgroundPath);
            s.FindProperty("titleFont").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(TitleFontPath);
            s.FindProperty("newGameScene").enumValueIndex = (int)MigrationSceneId.World;
            s.FindProperty("settingsController").objectReferenceValue = settingsController;
            s.FindProperty("deckEditorController").objectReferenceValue = deckEditorController;
            s.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        private static void CreateLoadingScreenScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = MigrationSceneCatalog.LoadingScreen;

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.04f, 0.06f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;

            EnsureAssetFolder(LocationsArtRoot + "/LoadingScreen/Materials");
            string matPath = LocationsArtRoot + "/LoadingScreen/Materials/LoadingBg.mat";
            Material loadingMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (loadingMat == null)
            {
                Shader unlit = Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
                loadingMat = new Material(unlit);
                AssetDatabase.CreateAsset(loadingMat, matPath);
            }
            loadingMat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(LoadingScreenBackgroundPath);
            EditorUtility.SetDirty(loadingMat);

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "LoadingBackground";
            quad.transform.position = Vector3.zero;
            quad.transform.localScale = new Vector3(17.8f, 10f, 1f);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.GetComponent<Renderer>().sharedMaterial = loadingMat;

            EditorSceneManager.SaveScene(scene, LoadingScreenScenePath);
        }

        private static void CreateWorldScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = MigrationSceneCatalog.World;
            GameObject root = new GameObject("World");
            CreateWorldSimulation(root.transform);
            string artRoot = LocationsArtRoot + "/World";
            EnsureAssetFolder(artRoot + "/Materials");
            CreateFlatGround(root.transform, "WorldGround", artRoot + "/Materials/Ground.mat", new Color(0.32f, 0.46f, 0.26f, 1f));

            Material nature = EnsureSimpleMaterial(artRoot + "/Materials/Nature.mat", new Color(0.20f, 0.42f, 0.22f, 1f));
            GameObject dress = new GameObject("WorldDressing");
            dress.transform.SetParent(root.transform);
            Transform d = dress.transform;
            InstantiateLocationProp($"{HumanVillageNatureModelsRoot}/Broadleaf_1.fbx", "Tree1", d, -34f, 18f, 0f, 15f, nature, true);
            InstantiateLocationProp($"{HumanVillageNatureModelsRoot}/Broadleaf_2.fbx", "Tree2", d, 34f, 20f, 25f, 15f, nature, true);
            InstantiateLocationProp($"{HumanVillageNatureModelsRoot}/Broadleaf_3.fbx", "Tree3", d, 0f, 36f, -30f, 15f, nature, true);
            InstantiateLocationProp($"{HumanVillageNatureModelsRoot}/Bush_1.fbx", "Bush1", d, -16f, 10f, 0f, 4f, nature, false);
            InstantiateLocationProp($"{HumanVillageNatureModelsRoot}/Bush_2.fbx", "Bush2", d, 16f, 10f, 0f, 4f, nature, false);

            // Hub portals to the key destinations, arranged in front of the spawn.
            GameObject portals = new GameObject("HubPortals");
            portals.transform.SetParent(root.transform);
            Transform pt = portals.transform;
            CreateHubPortal(pt, "ToBambooHome", -24f, 8f, MigrationSceneId.BambooHomeVerticalSlice, new Color(0.95f, 0.66f, 0.25f, 0.5f));
            CreateHubPortal(pt, "ToHumanVillage", -8f, 14f, MigrationSceneId.HumanVillageVerticalSlice, new Color(0.30f, 0.70f, 0.95f, 0.5f));
            CreateHubPortal(pt, "ToMagicForest", 8f, 14f, MigrationSceneId.MagicForest, new Color(0.55f, 0.45f, 0.85f, 0.5f));
            CreateHubPortal(pt, "ToMistyLake", 24f, 8f, MigrationSceneId.MistyLake, new Color(0.45f, 0.72f, 0.85f, 0.5f));
            CreateHubPortal(pt, "ToHakureiShrine", -16f, 22f, MigrationSceneId.HakureiShrine, new Color(0.90f, 0.30f, 0.28f, 0.5f));
            CreateHubPortal(pt, "ToScarletMansion", 16f, 22f, MigrationSceneId.ScarletMansionFront, new Color(0.75f, 0.20f, 0.30f, 0.5f));
            CreateHubPortal(pt, "ToFarm", 0f, 28f, MigrationSceneId.Farm, new Color(0.55f, 0.80f, 0.35f, 0.5f));

            float playerGroundY = SampleGroundY(0f, -6f, 0f);
            CreateMigrationPlayer(root.transform, new Vector3(0f, playerGroundY + 2f, -6f));
            CreateFollowCamera(root.transform, new Vector3(0f, 30f, -58f), Quaternion.Euler(28f, 0f, 0f));
            CreateGlobalUi(root.transform);

            EditorSceneManager.SaveScene(scene, WorldScenePath);
        }

        private static void CreateHubPortal(Transform parent, string name, float x, float z, MigrationSceneId target, Color color)
        {
            float groundY = SampleGroundY(x, z, 0f);
            CreatePortal(parent, name, new Vector3(x, groundY + 2f, z), target, color);
        }

        // Bamboo-home overworld variants (Mokou's house) reusing the Bamboo Home glb + prop set.
        private static void CreateBambooVariantScenes()
        {
            CreateBambooHomeLikeScene(MigrationSceneCatalog.MokouHouse3D, MokouHouse3DScenePath, new[]
            {
                ("mokou", "藤原妹红", "youkan"),
            });
            CreateBambooHomeLikeScene(MigrationSceneCatalog.BambooHouse, BambooHouseScenePath, new[]
            {
                ("kaguya", "蓬莱山辉夜", "moon_cake"),
                ("eirin", "八意永琳", "herbal_tea"),
                ("reisen", "铃仙·优昙华院·因幡", "moon_cake"),
                ("tewi", "因幡帝", "rice_ball"),
            });
        }

        private static void CreateBambooHomeLikeScene(string sceneName, string scenePath, (string id, string name, string gift)[] cast)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = sceneName;
            GameObject root = new GameObject(sceneName);
            CreateWorldSimulation(root.transform);
            CreateBlockoutGround(root.transform, sceneName + "Ground", new Vector3(6f, 1f, 6f));
            CreateMigrationPlayer(root.transform, new Vector3(0f, 1f, 0f));
            CreateFollowCamera(root.transform, new Vector3(0f, 6f, -8f), Quaternion.Euler(38f, 0f, 0f));
            CreateGlobalUi(root.transform);

            InstantiateAssetPrefab(BambooHousePath, "House3D", root.transform, new Vector3(4.8f, 0f, -3.7f), Quaternion.Euler(0f, 88.8f, 0f), new Vector3(12f, 12f, 12f));
            GameObject props = new GameObject("Props");
            props.transform.SetParent(root.transform);
            CreateBambooHomeProps(props.transform);
            CreateBambooHomeBed(root.transform);
            CreateLocationNpcCast(root.transform, cast, 6f);
            CreatePortal(root.transform, "TownPortal", new Vector3(-10f, 1f, 15f), MigrationSceneId.HumanVillageVerticalSlice, new Color(0.2f, 0.55f, 1f, 0.45f));

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        // A bed prop with a MigrationBedInteractor: pressing the interact key in range sleeps to the
        // next morning (advance day + restore fatigue) via the global UI owner.
        private static void CreateBambooHomeBed(Transform parent)
        {
            GameObject bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bed.name = "Bed";
            bed.transform.SetParent(parent);
            bed.transform.position = new Vector3(-3.2f, 0.4f, -2.4f);
            bed.transform.localScale = new Vector3(1.4f, 0.6f, 2.6f);

            Renderer renderer = bed.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.62f, 0.5f, 0.72f, 1f) };
            }

            bed.AddComponent<MigrationBedInteractor>();
        }

        // Combat/boss arenas: a walled flat arena + training dummies or a Cirno boss marker. The full
        // Perfect Freeze encounter wiring is E6 (combat breadth, deprioritized) — kept as a marker here.
        private static void CreateArenaScenes()
        {
            CreateArenaScene(MigrationSceneCatalog.CombatArena, CombatArenaScenePath, new Color(0.34f, 0.36f, 0.34f, 1f), false, new Color(0.80f, 0.40f, 0.30f, 1f));
            CreateArenaScene(MigrationSceneCatalog.CombatArenaHD2D, CombatArenaHD2DScenePath, new Color(0.30f, 0.34f, 0.40f, 1f), false, new Color(0.70f, 0.50f, 0.85f, 1f));
            CreateArenaScene(MigrationSceneCatalog.CirnoBossArena, CirnoBossArenaScenePath, new Color(0.62f, 0.74f, 0.82f, 1f), true, new Color(0.30f, 0.55f, 0.90f, 1f));
        }

        private static void CreateArenaScene(string sceneName, string scenePath, Color groundColor, bool boss, Color accentColor)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = sceneName;
            GameObject root = new GameObject(sceneName);
            CreateWorldSimulation(root.transform);
            string artRoot = LocationsArtRoot + "/" + sceneName;
            EnsureAssetFolder(artRoot + "/Materials");
            CreateFlatGround(root.transform, sceneName + "Ground", artRoot + "/Materials/Ground.mat", groundColor);

            Material wall = EnsureSimpleMaterial(artRoot + "/Materials/Wall.mat", new Color(0.40f, 0.40f, 0.45f, 1f));
            Material accent = EnsureSimpleMaterial(artRoot + "/Materials/Accent.mat", accentColor);

            GameObject lm = new GameObject("Arena");
            lm.transform.SetParent(root.transform);
            Transform t = lm.transform;
            CreatePrimitiveBlock(t, "WallN", PrimitiveType.Cube, new Vector3(0f, 2f, 23f), new Vector3(48f, 4f, 1.5f), Vector3.zero, wall, true);
            CreatePrimitiveBlock(t, "WallS", PrimitiveType.Cube, new Vector3(0f, 2f, -23f), new Vector3(48f, 4f, 1.5f), Vector3.zero, wall, true);
            CreatePrimitiveBlock(t, "WallE", PrimitiveType.Cube, new Vector3(23f, 2f, 0f), new Vector3(1.5f, 4f, 48f), Vector3.zero, wall, true);
            CreatePrimitiveBlock(t, "WallW", PrimitiveType.Cube, new Vector3(-23f, 2f, 0f), new Vector3(1.5f, 4f, 48f), Vector3.zero, wall, true);

            if (boss)
            {
                CreatePrimitiveBlock(t, "CirnoBossMarker", PrimitiveType.Capsule, new Vector3(0f, 3f, 10f), new Vector3(2.6f, 3f, 2.6f), Vector3.zero, accent, true);
                CreatePrimitiveBlock(t, "IcePillarL", PrimitiveType.Cube, new Vector3(-10f, 3f, 6f), new Vector3(1.4f, 6f, 1.4f), new Vector3(0f, 45f, 0f), accent, true);
                CreatePrimitiveBlock(t, "IcePillarR", PrimitiveType.Cube, new Vector3(10f, 3f, 6f), new Vector3(1.4f, 6f, 1.4f), new Vector3(0f, 45f, 0f), accent, true);
            }
            else
            {
                CreatePrimitiveBlock(t, "Dummy1", PrimitiveType.Capsule, new Vector3(-6f, 1.6f, 6f), new Vector3(1.2f, 1.6f, 1.2f), Vector3.zero, accent, true);
                CreatePrimitiveBlock(t, "Dummy2", PrimitiveType.Capsule, new Vector3(6f, 1.6f, 8f), new Vector3(1.2f, 1.6f, 1.2f), Vector3.zero, accent, true);
                CreatePrimitiveBlock(t, "Dummy3", PrimitiveType.Capsule, new Vector3(0f, 1.6f, 12f), new Vector3(1.2f, 1.6f, 1.2f), Vector3.zero, accent, true);
            }

            FinishLocationScene(root, scenePath, MigrationSceneId.BambooHomeVerticalSlice, accentColor, scene);
        }

        // Bespoke Godot nature locations that reuse the generic nature builder on flat ground with
        // distinctive palettes (Magic Forest = dark/teal woods; Misty Lake = blue-grey "misty water").
        private static void CreateBespokeNatureScenes()
        {
            CreateNatureLocationScene(MigrationSceneCatalog.MagicForest, MagicForestScenePath, LocationsArtRoot + "/MagicForest", string.Empty,
                MigrationSceneId.BambooHomeVerticalSlice, new Color(0.55f, 0.45f, 0.85f, 0.45f), new Color(0.20f, 0.34f, 0.22f, 1f), new Color(0.14f, 0.32f, 0.22f, 1f),
                CreateMagicForestCast);
            CreateNatureLocationScene(MigrationSceneCatalog.MistyLake, MistyLakeScenePath, LocationsArtRoot + "/MistyLake", string.Empty,
                MigrationSceneId.BambooHomeVerticalSlice, new Color(0.45f, 0.72f, 0.85f, 0.45f), new Color(0.34f, 0.46f, 0.48f, 1f), new Color(0.26f, 0.42f, 0.40f, 1f),
                CreateMistyLakeFishingSpot);
        }

        // Forest of Magic residents: Marisa, Alice, Kogasa.
        private static void CreateMagicForestCast(Transform root)
        {
            CreateLocationNpcCast(root, new[]
            {
                ("marisa", "雾雨魔理沙", "magic_crystal"),
                ("alice", "爱丽丝·玛格特罗依德", "cherry_blossom"),
                ("kogasa", "多多良小伞", "manjuu"),
            }, 9f);
        }

        // A small dock + fishing-spot prop at the misty lake: a MigrationFishingSpotInteractor lets the
        // player cast (interact key) to roll a weighted catch into the inventory via the owner's service.
        private static void CreateMistyLakeFishingSpot(Transform root)
        {
            string artRoot = LocationsArtRoot + "/MistyLake";
            EnsureAssetFolder(artRoot + "/Materials");
            Material wood = EnsureSimpleMaterial(artRoot + "/Materials/Dock.mat", new Color(0.46f, 0.34f, 0.22f, 1f));
            Material water = EnsureSimpleMaterial(artRoot + "/Materials/FishingWater.mat", new Color(0.30f, 0.52f, 0.62f, 1f));

            GameObject lm = new GameObject("FishingSpot");
            lm.transform.SetParent(root);
            Transform t = lm.transform;

            float dockGroundY = SampleGroundY(8f, 6f, 0f);
            CreatePrimitiveBlock(t, "Dock", PrimitiveType.Cube, new Vector3(8f, dockGroundY + 0.3f, 6f), new Vector3(3f, 0.4f, 6f), Vector3.zero, wood, false);
            GameObject ripple = CreatePrimitiveBlock(t, "FishingWater", PrimitiveType.Cylinder, new Vector3(8f, dockGroundY + 0.05f, 11f), new Vector3(5f, 0.1f, 5f), Vector3.zero, water, false);
            ripple.AddComponent<MigrationFishingSpotInteractor>();

            // Misty Lake regulars: Cirno + Mystia.
            CreateLocationNpcCast(root, new[]
            {
                ("cirno", "琪露诺", "herbal_tea"),
                ("mystia", "米斯蒂娅·萝蕾拉", "rice_ball"),
            }, 7f);
        }

        // Generic nature-location builder reused by every promoted environment pack (PureNature
        // variants, AngryMesh, and the bespoke nature locations). Pass an empty terrainObjPath to
        // get a large flat ground instead of a promoted terrain mesh.
        private static void CreateNatureLocationScene(
            string sceneName,
            string scenePath,
            string artRoot,
            string terrainObjPath,
            MigrationSceneId returnTo,
            Color portalColor,
            Color terrainColor,
            Color treeColor,
            System.Action<Transform> decorate = null)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = sceneName;

            GameObject root = new GameObject(sceneName);
            CreateWorldSimulation(root.transform);

            EnsureAssetFolder(artRoot + "/Materials");

            bool hasTerrain = !string.IsNullOrEmpty(terrainObjPath)
                && CreateLocationTerrain(root.transform, terrainObjPath, artRoot + "/Materials/Terrain.mat", terrainColor, sceneName + "Terrain");
            if (!hasTerrain)
            {
                CreateFlatGround(root.transform, sceneName + "Ground", artRoot + "/Materials/Terrain.mat", terrainColor);
            }

            CreateLocationSetDressing(root.transform, artRoot, treeColor);

            float playerGroundY = SampleGroundY(0f, 0f, 0f);
            CreateMigrationPlayer(root.transform, new Vector3(0f, playerGroundY + 2f, 0f));
            CreateFollowCamera(root.transform, new Vector3(0f, 32f, -62f), Quaternion.Euler(26f, 0f, 0f));
            CreateGlobalUi(root.transform);

            float portalGroundY = SampleGroundY(-16f, -10f, 0f);
            CreatePortal(root.transform, "BambooHomeReturnPortal", new Vector3(-16f, portalGroundY + 2f, -10f), returnTo, portalColor);

            decorate?.Invoke(root.transform);

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static bool CreateLocationTerrain(Transform parent, string terrainObjPath, string materialPath, Color color, string name)
        {
            Material terrainMaterial = EnsureSimpleMaterial(materialPath, color);
            GameObject terrain = InstantiateAssetPrefab(terrainObjPath, name, parent, Vector3.zero, Quaternion.identity, Vector3.one);
            if (terrain == null)
            {
                return false;
            }

            ApplyMaterialToRenderers(terrain, terrainMaterial);
            AddMeshColliders(terrain);
            CenterGroundOnPlane(terrain);
            Physics.SyncTransforms();

            float originSurface = SampleGroundY(0f, 0f, float.NaN);
            if (!float.IsNaN(originSurface))
            {
                terrain.transform.position += new Vector3(0f, -originSurface, 0f);
                Physics.SyncTransforms();
            }

            return true;
        }

        // Large flat ground for packs that ship no terrain mesh (e.g. AngryMesh). The Plane primitive
        // brings its own MeshCollider, so the prop-grounding raycasts resolve against it at y=0.
        private static void CreateFlatGround(Transform parent, string name, string materialPath, Color color)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = name;
            ground.transform.SetParent(parent);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(60f, 1f, 60f);

            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = EnsureSimpleMaterial(materialPath, color);
            }

            Physics.SyncTransforms();
        }

        private static void CreateLocationSetDressing(Transform parent, string artRoot, Color treeColor)
        {
            GameObject setDressing = new GameObject("SetDressing");
            setDressing.transform.SetParent(parent);
            Transform p = setDressing.transform;

            var materials = new Dictionary<string, Material>
            {
                ["tree"] = EnsureSimpleMaterial(artRoot + "/Materials/Tree.mat", treeColor),
                ["grass"] = EnsureSimpleMaterial(artRoot + "/Materials/Grass.mat", new Color(0.30f, 0.55f, 0.26f, 1f)),
                ["flower"] = EnsureSimpleMaterial(artRoot + "/Materials/Flower.mat", new Color(0.62f, 0.46f, 0.72f, 1f)),
                ["rock"] = EnsureSimpleMaterial(artRoot + "/Materials/Rock.mat", new Color(0.45f, 0.45f, 0.43f, 1f)),
                ["mushroom"] = EnsureSimpleMaterial(artRoot + "/Materials/Mushroom.mat", new Color(0.72f, 0.27f, 0.22f, 1f)),
                ["mountain"] = EnsureSimpleMaterial(artRoot + "/Materials/Mountain.mat", new Color(0.38f, 0.40f, 0.43f, 1f)),
            };

            // Auto-discover the promoted meshes per category and round-robin them across the slots.
            var meshesByCategory = new Dictionary<string, List<string>>();
            var cursor = new Dictionary<string, int>();
            var counter = new Dictionary<string, int>();

            foreach (LocationPropSlot slot in LocationGroveLayout)
            {
                if (!meshesByCategory.TryGetValue(slot.Category, out List<string> meshes))
                {
                    meshes = DiscoverCategoryMeshes(artRoot + "/" + slot.Category);
                    meshesByCategory[slot.Category] = meshes;
                    cursor[slot.Category] = 0;
                }

                if (meshes.Count == 0)
                {
                    continue;
                }

                int index = cursor[slot.Category];
                cursor[slot.Category] = index + 1;
                string assetPath = meshes[index % meshes.Count];

                counter.TryGetValue(slot.Category, out int n);
                counter[slot.Category] = n + 1;
                string instanceName = $"{slot.Category}_{n + 1}";

                Material material = materials.TryGetValue(slot.MaterialKey, out Material m) ? m : materials["tree"];
                InstantiateLocationProp(assetPath, instanceName, p, slot.X, slot.Z, slot.Yaw, slot.Height, material, slot.Collider);
            }
        }

        private static List<string> DiscoverCategoryMeshes(string folder)
        {
            var paths = new List<string>();
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return paths;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    paths.Add(path);
                }
            }

            paths.Sort(System.StringComparer.Ordinal);
            return paths;
        }

        private static GameObject InstantiateLocationProp(
            string assetPath,
            string instanceName,
            Transform parent,
            float x,
            float z,
            float yaw,
            float targetHeight,
            Material material,
            bool addCollider)
        {
            GameObject instance = InstantiateAssetPrefab(assetPath, instanceName, parent, Vector3.zero, Quaternion.Euler(0f, yaw, 0f), Vector3.one);
            if (instance == null)
            {
                return null;
            }

            ApplyMaterialToRenderers(instance, material);
            float groundY = SampleGroundY(x, z, 0f);
            NormalizeVisualBounds(instance, new Vector3(x, groundY, z), targetHeight);
            if (addCollider)
            {
                AddMeshColliders(instance);
            }

            return instance;
        }

        // Creates a nested Asset folder chain (AssetDatabase-aware) if it does not exist yet.
        private static void EnsureAssetFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(assetFolderPath).Replace("\\", "/");
            string leaf = Path.GetFileName(assetFolderPath);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureAssetFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static bool CreateHumanVillageTerrain(Transform parent)
        {
            Material terrainMaterial = EnsureSimpleMaterial(
                HumanVillageMaterialsRoot + "/HumanVillageTerrain.mat",
                new Color(0.32f, 0.43f, 0.22f, 1f));

            GameObject terrain = InstantiateAssetPrefab(
                HumanVillageTerrainPath,
                "SuntailVillageTerrain",
                parent,
                new Vector3(-150f, -5f, 150f),
                Quaternion.identity,
                Vector3.one);

            if (terrain == null)
            {
                return false;
            }

            ApplyMaterialToRenderers(terrain, terrainMaterial);
            AddMeshColliders(terrain);

            GameObject meadowsBackdrop = InstantiateAssetPrefab(
                HumanVillageMeadowsTerrainPath,
                "PureNatureMeadowsBackdrop",
                parent,
                new Vector3(-210f, -8f, 210f),
                Quaternion.identity,
                Vector3.one * 0.35f);

            if (meadowsBackdrop != null)
            {
                Material backdropMaterial = EnsureSimpleMaterial(
                    HumanVillageMaterialsRoot + "/HumanVillageBackdropTerrain.mat",
                    new Color(0.24f, 0.36f, 0.18f, 1f));
                ApplyMaterialToRenderers(meadowsBackdrop, backdropMaterial);
            }

            return true;
        }

        private static void CreateHumanVillageSetDressing(Transform parent)
        {
            GameObject setDressing = new GameObject("SuntailVillageSetDressing");
            setDressing.transform.SetParent(parent);

            Material buildingMaterial = EnsureSimpleMaterial(
                HumanVillageMaterialsRoot + "/HumanVillageBuilding.mat",
                new Color(0.64f, 0.48f, 0.34f, 1f));
            Material propMaterial = EnsureSimpleMaterial(
                HumanVillageMaterialsRoot + "/HumanVillageProp.mat",
                new Color(0.50f, 0.36f, 0.24f, 1f));
            Material foliageMaterial = EnsureSimpleMaterial(
                HumanVillageMaterialsRoot + "/HumanVillageFoliage.mat",
                new Color(0.18f, 0.42f, 0.23f, 1f));
            Material flowerMaterial = EnsureSimpleMaterial(
                HumanVillageMaterialsRoot + "/HumanVillageFlowers.mat",
                new Color(0.76f, 0.48f, 0.66f, 1f));
            Material stoneMaterial = EnsureSimpleMaterial(
                HumanVillageMaterialsRoot + "/HumanVillageStone.mat",
                new Color(0.42f, 0.42f, 0.40f, 1f));

            InstantiateHumanVillageBuilding("House_1", "VillageHouse1", setDressing.transform, new Vector3(-35f, 0.3f, 4f), Quaternion.Euler(0f, 35f, 0f), Vector3.one * 1.2f, buildingMaterial);
            InstantiateHumanVillageBuilding("House_3", "VillageHouse2", setDressing.transform, new Vector3(34f, 0.3f, 9f), Quaternion.Euler(0f, -30f, 0f), Vector3.one * 1.2f, buildingMaterial);
            InstantiateHumanVillageBuilding("House_5", "VillageHouse3", setDressing.transform, new Vector3(6f, 0.3f, -38f), Quaternion.Euler(0f, 170f, 0f), Vector3.one * 1.2f, buildingMaterial);

            InstantiateHumanVillageEnvironment("Shop_1", "MarketShop1", setDressing.transform, new Vector3(-18f, 0.4f, 14f), Quaternion.Euler(0f, 35f, 0f), Vector3.one * 2.6f, buildingMaterial);
            InstantiateHumanVillageEnvironment("Shop_2", "MarketShop2", setDressing.transform, new Vector3(14f, 0.4f, 18f), Quaternion.Euler(0f, -28f, 0f), Vector3.one * 2.5f, buildingMaterial);
            InstantiateHumanVillageEnvironment("Shop_3", "MarketShop3", setDressing.transform, new Vector3(22f, 0.4f, -10f), Quaternion.Euler(0f, -120f, 0f), Vector3.one * 2.5f, buildingMaterial);
            InstantiateHumanVillageEnvironment("Well_1", "VillageWell", setDressing.transform, new Vector3(0f, 0.6f, 8f), Quaternion.identity, Vector3.one * 2.2f, stoneMaterial);
            InstantiateHumanVillageEnvironment("Cart_1", "MarketCart1", setDressing.transform, new Vector3(-8f, 0.4f, -6f), Quaternion.Euler(0f, 70f, 0f), Vector3.one * 1.8f, propMaterial);
            InstantiateHumanVillageEnvironment("Cart_2", "MarketCart2", setDressing.transform, new Vector3(10f, 0.4f, -14f), Quaternion.Euler(0f, -35f, 0f), Vector3.one * 1.8f, propMaterial);
            InstantiateHumanVillageEnvironment("Bridge_centre", "RiverBridgeCenter", setDressing.transform, new Vector3(0f, 0.4f, -28f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 2.8f, propMaterial);
            InstantiateHumanVillageEnvironment("Bridge_left", "RiverBridgeLeft", setDressing.transform, new Vector3(-12f, 0.4f, -28f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 2.8f, propMaterial);
            InstantiateHumanVillageEnvironment("Bridge_right", "RiverBridgeRight", setDressing.transform, new Vector3(12f, 0.4f, -28f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 2.8f, propMaterial);
            InstantiateHumanVillageEnvironment("Boat", "RiverBoat", setDressing.transform, new Vector3(26f, 0.4f, -30f), Quaternion.Euler(0f, -20f, 0f), Vector3.one * 1.9f, propMaterial);

            InstantiateHumanVillageEnvironment("Fence_1", "FenceNorthA", setDressing.transform, new Vector3(-28f, 0.4f, 3f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 2f, propMaterial);
            InstantiateHumanVillageEnvironment("Fence_2", "FenceNorthB", setDressing.transform, new Vector3(-28f, 0.4f, -3f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 2f, propMaterial);
            InstantiateHumanVillageEnvironment("Fence_3", "FenceSouthA", setDressing.transform, new Vector3(31f, 0.4f, 8f), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 2f, propMaterial);
            InstantiateHumanVillageEnvironment("Fence_4", "FenceSouthB", setDressing.transform, new Vector3(31f, 0.4f, 2f), Quaternion.Euler(0f, -90f, 0f), Vector3.one * 2f, propMaterial);

            InstantiateHumanVillageEnvironment("Lantern_1", "PathLantern1", setDressing.transform, new Vector3(-6f, 0.4f, 14f), Quaternion.identity, Vector3.one * 2f, propMaterial);
            InstantiateHumanVillageEnvironment("Lantern_2", "PathLantern2", setDressing.transform, new Vector3(7f, 0.4f, 13f), Quaternion.identity, Vector3.one * 2f, propMaterial);
            InstantiateHumanVillageEnvironment("Barrel", "MarketBarrel", setDressing.transform, new Vector3(18f, 0.4f, 8f), Quaternion.identity, Vector3.one * 1.8f, propMaterial);
            InstantiateHumanVillageEnvironment("Bag_1", "MarketBag", setDressing.transform, new Vector3(17f, 0.4f, 5f), Quaternion.identity, Vector3.one * 1.8f, propMaterial);
            InstantiateHumanVillageEnvironment("Box_1", "MarketBox", setDressing.transform, new Vector3(-14f, 0.4f, 6f), Quaternion.identity, Vector3.one * 1.8f, propMaterial);
            InstantiateHumanVillageEnvironment("Rack_1", "MarketRack", setDressing.transform, new Vector3(-21f, 0.4f, 7f), Quaternion.Euler(0f, 20f, 0f), Vector3.one * 1.8f, propMaterial);
            InstantiateHumanVillageEnvironment("Case_and_food_1", "FoodCase", setDressing.transform, new Vector3(5f, 0.4f, 20f), Quaternion.Euler(0f, -15f, 0f), Vector3.one * 1.8f, propMaterial);

            InstantiateHumanVillageNature("Broadleaf_1", "BroadleafTree1", setDressing.transform, new Vector3(-45f, 0.3f, 25f), Quaternion.identity, Vector3.one * 2.6f, foliageMaterial);
            InstantiateHumanVillageNature("Broadleaf_2", "BroadleafTree2", setDressing.transform, new Vector3(42f, 0.3f, 30f), Quaternion.Euler(0f, 25f, 0f), Vector3.one * 2.6f, foliageMaterial);
            InstantiateHumanVillageNature("Broadleaf_3", "BroadleafTree3", setDressing.transform, new Vector3(36f, 0.3f, -42f), Quaternion.Euler(0f, -45f, 0f), Vector3.one * 2.6f, foliageMaterial);
            InstantiateHumanVillageNature("Bush_1", "VillageBush1", setDressing.transform, new Vector3(-26f, 0.3f, 18f), Quaternion.identity, Vector3.one * 2.2f, foliageMaterial);
            InstantiateHumanVillageNature("Bush_2", "VillageBush2", setDressing.transform, new Vector3(25f, 0.3f, 16f), Quaternion.identity, Vector3.one * 2.2f, foliageMaterial);
            InstantiateHumanVillageNature("Flowers", "VillageFlowers1", setDressing.transform, new Vector3(-4f, 0.3f, 22f), Quaternion.identity, Vector3.one * 2f, flowerMaterial);
            InstantiateHumanVillageNature("Flowers", "VillageFlowers2", setDressing.transform, new Vector3(12f, 0.3f, 4f), Quaternion.Euler(0f, 40f, 0f), Vector3.one * 1.8f, flowerMaterial);
            InstantiateHumanVillageNature("Grass", "VillageGrass1", setDressing.transform, new Vector3(-18f, 0.3f, -14f), Quaternion.identity, Vector3.one * 2.2f, foliageMaterial);
            InstantiateHumanVillageNature("Grass", "VillageGrass2", setDressing.transform, new Vector3(20f, 0.3f, -21f), Quaternion.identity, Vector3.one * 2.2f, foliageMaterial);
            InstantiateHumanVillageNature("Stone_1", "VillageStone1", setDressing.transform, new Vector3(-31f, 0.3f, -18f), Quaternion.identity, Vector3.one * 2f, stoneMaterial);
            InstantiateHumanVillageNature("Stone_2", "VillageStone2", setDressing.transform, new Vector3(34f, 0.3f, -19f), Quaternion.identity, Vector3.one * 2f, stoneMaterial);

            InstantiateHumanVillageBackground("Mountain_1", "BackgroundMountain1", setDressing.transform, new Vector3(-95f, -3f, 80f), Quaternion.Euler(0f, 20f, 0f), Vector3.one * 5f, stoneMaterial);
            InstantiateHumanVillageBackground("Mountain_2", "BackgroundMountain2", setDressing.transform, new Vector3(110f, -3f, 72f), Quaternion.Euler(0f, -30f, 0f), Vector3.one * 5f, stoneMaterial);
            InstantiateHumanVillageBackground("Mountain_3", "BackgroundMountain3", setDressing.transform, new Vector3(96f, -3f, -100f), Quaternion.Euler(0f, 180f, 0f), Vector3.one * 5f, stoneMaterial);
        }

        // Canonical Human Village NPC cast — ids match the dialogue/gift/bond systems (_npc_*.json),
        // with Chinese display names. (The E3.1 roster JSON uses display-name ids that don't map to
        // these canonical ids, so markers use the canonical cast for system integration.)
        private static readonly string[] HumanVillageNpcIds =
        {
            "marisa", "reimu", "keine", "sakuya", "kaguya", "koishi", "akyuu", "kosuzu",
            "nitori", "alice", "patchouli", "remilia", "sanae", "cirno", "aya",
        };

        private static readonly string[] HumanVillageNpcNames =
        {
            "雾雨魔理沙", "博丽灵梦", "上白泽慧音", "十六夜咲夜", "蓬莱山辉夜", "古明地恋", "稗田阿求", "本居小铃",
            "河城荷取", "爱丽丝·玛格特罗依德", "帕秋莉·诺蕾姬", "蕾米莉亚·斯卡蕾特", "东风谷早苗", "琪露诺", "射命丸文",
        };

        // Per-NPC preferred gift (parallel to HumanVillageNpcIds). marisa/reimu/keine use their
        // canonical loved gifts; the rest are sensible defaults until GiftDatabase preferences wire in (E5).
        private static readonly string[] HumanVillageNpcGifts =
        {
            "magic_crystal", "green_tea", "history_book", "sake", "moon_cake", "dango", "youkan", "manjuu",
            "mushroom_stew", "cherry_blossom", "black_tea", "rice_ball", "sunflower", "herbal_tea", "lily",
        };

        private static void CreateHumanVillageNpcMarkers(Transform parent)
        {
            GameObject npcRoot = new GameObject("NPCMarkers");
            npcRoot.transform.SetParent(parent);

            // Spawn the canonical Human Village cast as interactable markers in a ring around the
            // market square. Real character models (VRM/glb) are an E5/E7 follow-up.
            int n = HumanVillageNpcIds.Length;
            for (int i = 0; i < n; i++)
            {
                float angle = (i / (float)n) * Mathf.PI * 2f;
                float radius = 12f + (i % 3) * 7f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 2.4f, Mathf.Sin(angle) * radius);
                Color color = Color.HSVToRGB((i * 0.137f) % 1f, 0.62f, 0.92f);
                CreateNpcMarker(npcRoot.transform, HumanVillageNpcIds[i], HumanVillageNpcNames[i], HumanVillageNpcGifts[i], pos, color);
            }
        }

        private static void CreateNpcMarker(
            Transform parent,
            string npcId,
            string displayName,
            string preferredGiftId,
            Vector3 position,
            Color color)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            marker.name = $"NPC_{npcId}";
            marker.transform.SetParent(parent);
            marker.transform.position = position;
            marker.transform.localScale = new Vector3(0.85f, 1.25f, 0.85f);

            Renderer renderer = marker.GetComponent<Renderer>();
            Material material = new Material(Shader.Find("Standard"))
            {
                color = color
            };
            renderer.sharedMaterial = material;

            MigrationNpcInteractor interactor = marker.AddComponent<MigrationNpcInteractor>();
            interactor.Configure(npcId, displayName, preferredGiftId);
        }

        // Spawn a location's canonical resident cast as interactable NPC markers in a ring near the player
        // spawn (E5 reachability — more of the 35 NPCs reachable beyond Human Village + the villages). Real
        // character models (VRM/glb) + hour-driven schedule placement are E5/E7 follow-ups.
        private static void CreateLocationNpcCast(Transform parent, (string id, string name, string gift)[] cast, float baseRadius)
        {
            if (cast == null || cast.Length == 0)
            {
                return;
            }

            GameObject npcRoot = new GameObject("NPCMarkers");
            npcRoot.transform.SetParent(parent);
            int n = cast.Length;
            for (int i = 0; i < n; i++)
            {
                float angle = (i / (float)n) * Mathf.PI * 2f;
                float radius = baseRadius + (i % 2) * 4f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 2.4f, Mathf.Sin(angle) * radius + baseRadius * 0.4f);
                Color color = Color.HSVToRGB((i * 0.21f) % 1f, 0.6f, 0.92f);
                CreateNpcMarker(npcRoot.transform, cast[i].id, cast[i].name, cast[i].gift, pos, color);
            }
        }

        private static void CreateCookingStation(Transform parent, Vector3 position)
        {
            GameObject station = GameObject.CreatePrimitive(PrimitiveType.Cube);
            station.name = "BambooCookingStation";
            station.transform.SetParent(parent);
            station.transform.position = position;
            station.transform.localScale = new Vector3(1.5f, 0.7f, 1.1f);

            Renderer renderer = station.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"))
                {
                    color = new Color(0.40f, 0.24f, 0.14f, 1f)
                };
                renderer.sharedMaterial = material;
            }

            BoxCollider collider = station.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.isTrigger = true;
                collider.size = new Vector3(2.5f, 2f, 2.5f);
            }

            MigrationCookingStationInteractor interactor = station.AddComponent<MigrationCookingStationInteractor>();
            interactor.Configure("bamboo_home", "做菜 [E]");
        }

        private static void CreateBlockoutGround(Transform parent, string name, Vector3 scale)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = name;
            ground.transform.SetParent(parent);
            ground.transform.localScale = scale;
        }

        private static GameObject CreateMigrationPlayer(Transform parent, Vector3 position)
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "MigrationPlayer";
            player.tag = "Player";
            player.transform.SetParent(parent);
            player.transform.position = position;
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 1f, 0f);
            player.AddComponent<MigrationPlayerController>();
            MigrationPlayerAttackHitbox attackHitbox = CreatePlayerAttackHitbox(player.transform);
            MigrationPlayerCombatActionController actionController = player.AddComponent<MigrationPlayerCombatActionController>();
            actionController.BindAttackHitbox(attackHitbox);
            actionController.ConfigureDamage(10f, 20f);
            AttachMokouVisual(player);
            return player;
        }

        private static MigrationPlayerAttackHitbox CreatePlayerAttackHitbox(Transform player)
        {
            GameObject hitbox = new GameObject("PlayerAttackHitbox");
            hitbox.transform.SetParent(player);
            hitbox.transform.localPosition = new Vector3(0f, 1.05f, 1.15f);
            hitbox.transform.localRotation = Quaternion.identity;
            hitbox.transform.localScale = Vector3.one;

            BoxCollider collider = hitbox.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(1.3f, 1.2f, 1.6f);

            Rigidbody rigidbody = hitbox.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            MigrationPlayerAttackHitbox attackHitbox = hitbox.AddComponent<MigrationPlayerAttackHitbox>();
            attackHitbox.Configure(10f, "light");
            return attackHitbox;
        }

        private static void CreateCombatTrainingTargets(Transform parent)
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            target.name = "MigrationEnemy_FairyScout";
            target.transform.SetParent(parent);
            target.transform.position = new Vector3(7f, 3f, 8f);
            target.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            MigrationCombatTargetBehaviour targetBehaviour = target.AddComponent<MigrationCombatTargetBehaviour>();
            targetBehaviour.Initialize(30f);
            MigrationCombatDefeatHandler defeatHandler = target.AddComponent<MigrationCombatDefeatHandler>();
            defeatHandler.ConfigureDefeatDelay(0.45f);
            defeatHandler.ConfigureDeathFeedback(0.45f, new Color(1f, 0.35f, 0.12f, 1f));
            GameObject deathFeedbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyDeathFeedbackPrefabPath);
            if (deathFeedbackPrefab != null)
            {
                defeatHandler.ConfigureDeathFeedbackPrefab(deathFeedbackPrefab);
            }

            MigrationCombatHurtFeedback hurtFeedback = target.AddComponent<MigrationCombatHurtFeedback>();
            hurtFeedback.BindTarget(targetBehaviour);
            hurtFeedback.ConfigureFeedback(0.16f, new Color(1f, 0.15f, 0.1f, 1f), 0.08f);

            MigrationCombatDefeatRewardHandler rewardHandler = target.AddComponent<MigrationCombatDefeatRewardHandler>();
            rewardHandler.BindTarget(targetBehaviour);
            rewardHandler.ConfigureRewards(10, 10, "enemy_killed");
            MigrationCombatLootDropHandler lootDropHandler = target.AddComponent<MigrationCombatLootDropHandler>();
            lootDropHandler.BindTarget(targetBehaviour);

            AttachCombatReadabilityPresenters(target, targetBehaviour, rewardHandler, lootDropHandler);

            MigrationSimpleEnemyController enemyController = target.AddComponent<MigrationSimpleEnemyController>();
            enemyController.BindTarget(targetBehaviour);
            enemyController.BindLootDropHandler(lootDropHandler);

            Renderer targetRenderer = target.GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.9f, 0.25f, 0.18f, 1f);
                targetRenderer.sharedMaterial = material;
            }

            GameObject damageSource = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            damageSource.name = "EnemyDamageSourceMarker";
            damageSource.transform.SetParent(target.transform);
            damageSource.transform.localPosition = new Vector3(1.8f, 0f, 0f);
            damageSource.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

            SphereCollider collider = damageSource.GetComponent<SphereCollider>();
            collider.isTrigger = true;

            Rigidbody rigidbody = damageSource.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            MigrationEnemyDamageSource enemyDamageSource = damageSource.AddComponent<MigrationEnemyDamageSource>();
            enemyDamageSource.ConfigureWindowing(true, false);
            enemyController.BindDamageSource(enemyDamageSource);

            MigrationEnemyVariantProfile fairyScoutProfile = new MigrationEnemyVariantProfile();
            fairyScoutProfile.Configure(
                "fairy_scout",
                "fairy",
                string.Empty,
                30f,
                8f,
                1.6f,
                2f,
                10f,
                1.2f,
                0.2f,
                true);
            enemyController.ApplyVariant(fairyScoutProfile);

            MigrationPerfectFreezeStaggerAdapter staggerAdapter = target.AddComponent<MigrationPerfectFreezeStaggerAdapter>();
            staggerAdapter.BindEnemyController(enemyController);

            Renderer damageRenderer = damageSource.GetComponent<Renderer>();
            if (damageRenderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.35f, 0.12f, 0.85f, 0.85f);
                damageRenderer.sharedMaterial = material;
            }

            CreateCatalogBackedRangedEnemy(parent);
        }

        private static void CreateCatalogBackedRangedEnemy(Transform parent)
        {
            MigrationEnemyCatalog catalog = new MigrationEnemyCatalog();
            catalog.LoadGodotDefaults();
            MigrationEnemyVariantProfile batProfile = catalog.GetProfile("bat");
            if (batProfile == null)
            {
                Debug.LogWarning("Unable to build MigrationEnemy_BatScout because the bat profile is missing.");
                return;
            }

            GameObject bat = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bat.name = "MigrationEnemy_BatScout";
            bat.transform.SetParent(parent);
            bat.transform.position = new Vector3(11f, 4f, 10f);
            bat.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);

            MigrationCombatTargetBehaviour targetBehaviour = bat.AddComponent<MigrationCombatTargetBehaviour>();
            MigrationCombatDefeatHandler defeatHandler = bat.AddComponent<MigrationCombatDefeatHandler>();
            defeatHandler.ConfigureDefeatDelay(0.45f);
            defeatHandler.ConfigureDeathFeedback(0.45f, new Color(1f, 0.35f, 0.12f, 1f));
            GameObject deathFeedbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyDeathFeedbackPrefabPath);
            if (deathFeedbackPrefab != null)
            {
                defeatHandler.ConfigureDeathFeedbackPrefab(deathFeedbackPrefab);
            }

            MigrationCombatHurtFeedback hurtFeedback = bat.AddComponent<MigrationCombatHurtFeedback>();
            hurtFeedback.BindTarget(targetBehaviour);
            hurtFeedback.ConfigureFeedback(0.16f, new Color(1f, 0.15f, 0.1f, 1f), 0.08f);

            MigrationCombatDefeatRewardHandler rewardHandler = bat.AddComponent<MigrationCombatDefeatRewardHandler>();
            rewardHandler.BindTarget(targetBehaviour);
            rewardHandler.ConfigureRewards(batProfile.XpValue, batProfile.XpValue, "enemy_killed");

            MigrationCombatLootDropHandler lootDropHandler = bat.AddComponent<MigrationCombatLootDropHandler>();
            lootDropHandler.BindTarget(targetBehaviour);

            AttachCombatReadabilityPresenters(bat, targetBehaviour, rewardHandler, lootDropHandler);

            MigrationSimpleEnemyController enemyController = bat.AddComponent<MigrationSimpleEnemyController>();
            enemyController.BindTarget(targetBehaviour);
            enemyController.BindLootDropHandler(lootDropHandler);
            enemyController.ApplyVariant(batProfile);
            GameObject projectileFeedbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyProjectileFeedbackPrefabPath);
            MigrationEnemyProjectile projectilePrefab = projectileFeedbackPrefab != null
                ? projectileFeedbackPrefab.GetComponent<MigrationEnemyProjectile>()
                : null;
            enemyController.ConfigureProjectilePrefab(projectilePrefab);

            MigrationPerfectFreezeStaggerAdapter staggerAdapter = bat.AddComponent<MigrationPerfectFreezeStaggerAdapter>();
            staggerAdapter.BindEnemyController(enemyController);

            Renderer renderer = bat.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.28f, 0.12f, 0.45f, 1f);
                renderer.sharedMaterial = material;
            }
        }

        private static void CreateFollowCamera(Transform parent, Vector3 position, Quaternion rotation)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent);
            cameraObject.transform.position = position;
            cameraObject.transform.rotation = rotation;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
        }

        private static void CreateLookAtCamera(Transform parent, Vector3 position, Vector3 target)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent);
            cameraObject.transform.position = position;
            cameraObject.transform.LookAt(target);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 38f;
        }

        private static void CreatePortal(
            Transform parent,
            string name,
            Vector3 position,
            MigrationSceneId targetScene,
            Color color)
        {
            GameObject portal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            portal.name = name;
            portal.transform.SetParent(parent);
            portal.transform.position = position;
            portal.transform.localScale = new Vector3(2f, 2f, 0.2f);

            BoxCollider collider = portal.GetComponent<BoxCollider>();
            collider.isTrigger = true;

            Renderer renderer = portal.GetComponent<Renderer>();
            Material material = new Material(Shader.Find("Standard"));
            material.color = color;
            renderer.sharedMaterial = material;

            ScenePortal scenePortal = portal.AddComponent<ScenePortal>();
            scenePortal.Configure(targetScene);
        }

        private static void CreateGlobalUi(Transform parent)
        {
            GameObject ui = new GameObject("MigrationGlobalUI");
            ui.transform.SetParent(parent);

            MigrationGlobalUiController globalUi = ui.AddComponent<MigrationGlobalUiController>();
            MigrationHudController hud = ui.AddComponent<MigrationHudController>();
            MigrationUnifiedMenuController menu = ui.AddComponent<MigrationUnifiedMenuController>();
            MigrationSettingsController settings = ui.AddComponent<MigrationSettingsController>();
            RuneDialogueController runeDialogue = ui.AddComponent<RuneDialogueController>();
            MigrationGiftSelectionController giftSelection = ui.AddComponent<MigrationGiftSelectionController>();
            MigrationShopController shop = ui.AddComponent<MigrationShopController>();
            MigrationProjectileSpecialSettlement projectileSettlement = ui.AddComponent<MigrationProjectileSpecialSettlement>();
            projectileSettlement.ConfigureRewards(2f, 5f, 8f, 12f, 12, 1.2f);

            SerializedObject serialized = new SerializedObject(globalUi);
            serialized.FindProperty("hudController").objectReferenceValue = hud;
            serialized.FindProperty("unifiedMenuController").objectReferenceValue = menu;
            serialized.FindProperty("settingsController").objectReferenceValue = settings;
            serialized.FindProperty("runeDialogueController").objectReferenceValue = runeDialogue;
            serialized.FindProperty("giftSelectionController").objectReferenceValue = giftSelection;
            serialized.FindProperty("shopController").objectReferenceValue = shop;
            serialized.FindProperty("projectileSettlement").objectReferenceValue = projectileSettlement;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            foreach (MigrationPlayerCombatActionController action in Object.FindObjectsByType<MigrationPlayerCombatActionController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                action.BindProjectileSettlement(projectileSettlement);
            }

            foreach (MigrationPerfectFreezeStaggerAdapter adapter in Object.FindObjectsByType<MigrationPerfectFreezeStaggerAdapter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                adapter.BindSettlement(projectileSettlement);
            }
        }

        private static void CreateWorldSimulation(Transform parent)
        {
            GameObject worldSimulation = new GameObject("WorldSimulation");
            worldSimulation.transform.SetParent(parent);

            GameObject sunObject = new GameObject("Sun");
            sunObject.transform.SetParent(worldSimulation.transform);
            sunObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.0f;
            sun.shadows = LightShadows.Soft;

            GameObject moonObject = new GameObject("Moon");
            moonObject.transform.SetParent(worldSimulation.transform);
            moonObject.transform.rotation = Quaternion.Euler(230f, 150f, 0f);
            Light moon = moonObject.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.7f, 0.8f, 1f, 1f);
            moon.intensity = 0f;
            moon.enabled = false;

            DayNightLightingController lightingController = worldSimulation.AddComponent<DayNightLightingController>();
            lightingController.Bind(sun, moon);

            WorldSimulationBehaviour simulationBehaviour = worldSimulation.AddComponent<WorldSimulationBehaviour>();
            simulationBehaviour.SetLightingController(lightingController);
            simulationBehaviour.Initialize();
        }

        private static GameObject InstantiateAssetPrefab(
            string assetPath,
            string instanceName,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning($"Unable to load asset prefab at {assetPath}.");
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                Debug.LogWarning($"Unable to instantiate asset prefab at {assetPath}.");
                return null;
            }

            instance.name = instanceName;
            instance.transform.SetParent(parent);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.localScale = scale;
            return instance;
        }

        private static void AttachMokouVisual(GameObject player)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MokouVisualPath);
            if (prefab == null)
            {
                return;
            }

            GameObject visual = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (visual == null)
            {
                Debug.LogWarning($"Unable to instantiate Mokou visual at {MokouVisualPath}.");
                return;
            }

            visual.name = "MokouVisual";
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            visual.transform.localScale = Vector3.one;
            RemoveColliders(visual);
            NormalizeVisualBounds(visual, player.transform.position + Vector3.down, 1.8f);

            Renderer capsuleRenderer = player.GetComponent<Renderer>();
            if (capsuleRenderer != null)
            {
                capsuleRenderer.enabled = false;
            }

            // The FBX imports with white non-URP Standard materials (embedded textures don't carry over),
            // so give the character a clean URP placeholder material until Codex/image2 supplies the real
            // skin/outfit textures (TODO(codex/image2): Mokou textures). Keeps her readable, not white.
            Shader charShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material placeholder = new Material(charShader) { color = new Color(0.82f, 0.42f, 0.40f, 1f) };
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = placeholder;
                }

                renderer.sharedMaterials = mats;
            }

            // Drive the rigged Humanoid visual with the locomotion blend tree so it animates (idle/run)
            // instead of standing in a static bind pose. The FBX prefab's Animator already carries the
            // avatar from import; we just assign the controller and bind the locomotion bridge.
            Animator animator = visual.GetComponent<Animator>();
            if (animator == null)
            {
                animator = visual.AddComponent<Animator>();
            }

            RuntimeAnimatorController locomotion = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MokouLocomotionControllerPath);
            if (locomotion != null)
            {
                animator.runtimeAnimatorController = locomotion;
            }

            animator.applyRootMotion = false;

            MigrationLocomotionAnimatorBridge bridge = player.GetComponent<MigrationLocomotionAnimatorBridge>();
            if (bridge == null)
            {
                bridge = player.AddComponent<MigrationLocomotionAnimatorBridge>();
            }

            bridge.BindAnimator(animator);
        }

        private static void CreateAnimationImportMarkers(Transform parent)
        {
            GameObject markers = new GameObject("ImportedAnimationClips");
            markers.transform.SetParent(parent);

            foreach (CharacterAnimationImportSpec spec in MokouValidationAnimations)
            {
                string markerName = Path.GetFileNameWithoutExtension(spec.FileName);
                GameObject marker = new GameObject($"{markerName}_{(spec.LoopTime ? "Loop" : "OneShot")}");
                marker.transform.SetParent(markers.transform);
            }
        }

        private static void ConfigureCharacterImports()
        {
            foreach (CharacterAnimationImportSpec spec in MokouValidationAnimations)
            {
                ConfigureAnimationImport($"{MokouValidationAnimationsRoot}/{spec.FileName}", spec.LoopTime);
            }
        }

        private static void ConfigureEnemyAnimationImportsAndControllers()
        {
            MigrationEnemyCatalog catalog = new MigrationEnemyCatalog();
            catalog.LoadGodotDefaults();

            foreach (MigrationEnemyVariantProfile profile in catalog.GetAllProfiles())
            {
                EnemyAnimationSpec spec = ResolveEnemyAnimationSpec(profile.VariantId);
                if (!spec.HasAnyClip)
                {
                    continue;
                }

                string controllerDirectory = Path.GetDirectoryName(spec.ControllerPath);
                if (!string.IsNullOrEmpty(controllerDirectory))
                {
                    Directory.CreateDirectory(controllerDirectory);
                }

                ConfigureEnemyAnimationClip(spec.IdleClipPath, true);
                ConfigureEnemyAnimationClip(spec.MoveClipPath, true);
                ConfigureEnemyAnimationClip(spec.AttackClipPath, false);
                ConfigureEnemyAnimationClip(spec.ProjectileClipPath, false);
                ConfigureEnemyAnimationClip(spec.TakeDamageClipPath, false);
                ConfigureEnemyAnimationClip(spec.DieClipPath, false);
                CreateEnemyAnimatorController(spec);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ConfigureEnemyAnimationClip(string assetPath, bool loopTime)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Unable to configure enemy animation import at {assetPath}.");
                return;
            }

            bool shouldReimport = false;
            if (!importer.importAnimation)
            {
                importer.importAnimation = true;
                shouldReimport = true;
            }

            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                shouldReimport = true;
            }

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips.Length == 0)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                clips = importer.defaultClipAnimations;
            }

            for (int index = 0; index < clips.Length; index++)
            {
                if (clips[index].loopTime != loopTime || clips[index].loopPose != loopTime)
                {
                    clips[index].loopTime = loopTime;
                    clips[index].loopPose = loopTime;
                    shouldReimport = true;
                }
            }

            importer.clipAnimations = clips;

            if (shouldReimport)
            {
                importer.SaveAndReimport();
            }
        }

        private static void CreateEnemyAnimatorController(EnemyAnimationSpec spec)
        {
            if (string.IsNullOrEmpty(spec.ControllerPath))
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(spec.ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(spec.ControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(spec.ControllerPath);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("MotionState", AnimatorControllerParameterType.Int);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Projectile", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("TakeDamage", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idleState = AddEnemyAnimationState(stateMachine, "Idle", spec.IdleClipPath, new Vector3(0f, 0f, 0f));
            AnimatorState moveState = AddEnemyAnimationState(stateMachine, "Move", spec.MoveClipPath, new Vector3(220f, 0f, 0f));
            AnimatorState attackState = AddEnemyAnimationState(stateMachine, "Attack", spec.AttackClipPath, new Vector3(440f, 0f, 0f));
            AnimatorState projectileState = AddEnemyAnimationState(stateMachine, "Projectile", spec.ProjectileClipPath, new Vector3(660f, 0f, 0f));
            AnimatorState takeDamageState = AddEnemyAnimationState(stateMachine, "TakeDamage", spec.TakeDamageClipPath, new Vector3(880f, 0f, 0f));
            AnimatorState dieState = AddEnemyAnimationState(stateMachine, "Die", spec.DieClipPath, new Vector3(1100f, 0f, 0f));

            if (idleState != null)
            {
                stateMachine.defaultState = idleState;
            }

            AddEnemyLocomotionTransitions(idleState, moveState);
            AddEnemyTriggeredStateTransition(stateMachine, attackState, "Attack", idleState);
            AddEnemyTriggeredStateTransition(stateMachine, projectileState, "Projectile", idleState);
            AddEnemyTriggeredStateTransition(stateMachine, takeDamageState, "TakeDamage", idleState);
            AddEnemyTriggeredStateTransition(stateMachine, dieState, "Die", null);

            EditorUtility.SetDirty(controller);
        }

        private static void AddEnemyLocomotionTransitions(AnimatorState idleState, AnimatorState moveState)
        {
            if (idleState == null || moveState == null)
            {
                return;
            }

            AnimatorStateTransition idleToMove = idleState.AddTransition(moveState);
            idleToMove.hasExitTime = false;
            idleToMove.duration = 0.1f;
            idleToMove.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");

            AnimatorStateTransition moveToIdle = moveState.AddTransition(idleState);
            moveToIdle.hasExitTime = false;
            moveToIdle.duration = 0.1f;
            moveToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
        }

        private static void AddEnemyTriggeredStateTransition(
            AnimatorStateMachine stateMachine,
            AnimatorState triggeredState,
            string triggerName,
            AnimatorState returnState)
        {
            if (stateMachine == null || triggeredState == null)
            {
                return;
            }

            AnimatorStateTransition triggerTransition = stateMachine.AddAnyStateTransition(triggeredState);
            triggerTransition.hasExitTime = false;
            triggerTransition.duration = 0.05f;
            triggerTransition.canTransitionToSelf = false;
            triggerTransition.AddCondition(AnimatorConditionMode.If, 0f, triggerName);

            if (returnState == null)
            {
                return;
            }

            AnimatorStateTransition returnTransition = triggeredState.AddTransition(returnState);
            returnTransition.hasExitTime = true;
            returnTransition.exitTime = 0.95f;
            returnTransition.duration = 0.05f;
        }

        private static AnimatorState AddEnemyAnimationState(AnimatorStateMachine stateMachine, string stateName, string clipPath, Vector3 position)
        {
            AnimationClip clip = LoadFirstAnimationClip(clipPath);
            if (clip == null)
            {
                return null;
            }

            AnimatorState state = stateMachine.AddState(stateName, position);
            state.motion = clip;
            return state;
        }

        private static AnimationClip LoadFirstAnimationClip(string clipPath)
        {
            if (string.IsNullOrEmpty(clipPath))
            {
                return null;
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(clipPath);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", System.StringComparison.OrdinalIgnoreCase))
                {
                    return clip;
                }
            }

            return null;
        }

        private static void ConfigureAnimationImport(string assetPath, bool loopTime)
        {
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Unable to configure animation import at {assetPath}.");
                return;
            }

            bool shouldReimport = false;

            if (!importer.importAnimation)
            {
                importer.importAnimation = true;
                shouldReimport = true;
            }

            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                shouldReimport = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                shouldReimport = true;
            }

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips.Length == 0)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                clips = importer.defaultClipAnimations;
            }

            for (int index = 0; index < clips.Length; index++)
            {
                if (clips[index].loopTime != loopTime || clips[index].loopPose != loopTime)
                {
                    clips[index].loopTime = loopTime;
                    clips[index].loopPose = loopTime;
                    shouldReimport = true;
                }
            }

            importer.clipAnimations = clips;

            if (shouldReimport)
            {
                importer.SaveAndReimport();
            }
        }

        private static GameObject InstantiateHumanVillageEnvironment(
            string modelName,
            string instanceName,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Material material)
        {
            return InstantiateHumanVillageModel(
                HumanVillageEnvironmentModelsRoot,
                modelName,
                instanceName,
                parent,
                position,
                rotation,
                scale,
                material);
        }

        private static GameObject InstantiateHumanVillageBuilding(
            string prefabName,
            string instanceName,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Material material)
        {
            GameObject instance = InstantiateAssetPrefab(
                $"{HumanVillageBuildingPrefabsRoot}/{prefabName}.prefab",
                instanceName,
                parent,
                position,
                rotation,
                scale);

            if (instance == null)
            {
                return null;
            }

            ApplyMaterialToRenderers(instance, material);
            AddMeshColliders(instance);
            return instance;
        }

        private static GameObject InstantiateHumanVillageNature(
            string modelName,
            string instanceName,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Material material)
        {
            return InstantiateHumanVillageModel(
                HumanVillageNatureModelsRoot,
                modelName,
                instanceName,
                parent,
                position,
                rotation,
                scale,
                material);
        }

        private static GameObject InstantiateHumanVillageBackground(
            string modelName,
            string instanceName,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Material material)
        {
            return InstantiateHumanVillageModel(
                HumanVillageBackgroundModelsRoot,
                modelName,
                instanceName,
                parent,
                position,
                rotation,
                scale,
                material);
        }

        private static GameObject InstantiateHumanVillageModel(
            string modelRoot,
            string modelName,
            string instanceName,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Material material)
        {
            GameObject instance = InstantiateAssetPrefab(
                $"{modelRoot}/{modelName}.fbx",
                instanceName,
                parent,
                position,
                rotation,
                scale);

            if (instance == null)
            {
                return null;
            }

            ApplyMaterialToRenderers(instance, material);
            AddMeshColliders(instance);
            return instance;
        }

        private static Material EnsureSimpleMaterial(string materialPath, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyMaterialToRenderers(GameObject root, Material material)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = material;
                    continue;
                }

                for (int index = 0; index < materials.Length; index++)
                {
                    materials[index] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static void AddMeshColliders(GameObject root)
        {
            foreach (MeshFilter meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter.sharedMesh == null || meshFilter.GetComponent<Collider>() != null)
                {
                    continue;
                }

                MeshCollider collider = meshFilter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = meshFilter.sharedMesh;
            }
        }

        private static void RemoveColliders(GameObject root)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static void NormalizeVisualBounds(GameObject root, Vector3 targetGroundPosition, float targetHeight)
        {
            if (!TryCalculateRendererBounds(root, out Bounds bounds) || bounds.size.y <= 0.001f)
            {
                return;
            }

            float scaleFactor = targetHeight / bounds.size.y;
            root.transform.localScale *= scaleFactor;

            if (!TryCalculateRendererBounds(root, out bounds))
            {
                return;
            }

            Vector3 targetCenter = new Vector3(targetGroundPosition.x, bounds.center.y, targetGroundPosition.z);
            Vector3 lateralDelta = targetCenter - bounds.center;
            Vector3 verticalDelta = new Vector3(0f, targetGroundPosition.y - bounds.min.y, 0f);
            root.transform.position += lateralDelta + verticalDelta;
        }

        private static bool TryCalculateRendererBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bounds = new Bounds(root.transform.position, Vector3.zero);
            bool hasBounds = false;

            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            return hasBounds;
        }

        private static void RegisterBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScenePath, true),
                new EditorBuildSettingsScene(TitleScreenScenePath, true),
                new EditorBuildSettingsScene(BambooHomeScenePath, true),
                new EditorBuildSettingsScene(HumanVillageScenePath, true),
                new EditorBuildSettingsScene(PureNatureMeadowsScenePath, true),
                new EditorBuildSettingsScene(ClassicScenePath, true),
                new EditorBuildSettingsScene(JungleScenePath, true),
                new EditorBuildSettingsScene(IslandsScenePath, true),
                new EditorBuildSettingsScene(MountainsScenePath, true),
                new EditorBuildSettingsScene(FantasyForestScenePath, true),
                new EditorBuildSettingsScene(AngryMeshMeadowScenePath, true),
                new EditorBuildSettingsScene(MagicForestScenePath, true),
                new EditorBuildSettingsScene(MistyLakeScenePath, true),
                new EditorBuildSettingsScene(TownWorldScenePath, true),
                new EditorBuildSettingsScene(FantasyVillageScenePath, true),
                new EditorBuildSettingsScene(SuntailVillagePlayableScenePath, true),
                new EditorBuildSettingsScene(SuntailVillageImportedScenePath, true),
                new EditorBuildSettingsScene(HakureiShrineScenePath, true),
                new EditorBuildSettingsScene(ScarletMansionFrontScenePath, true),
                new EditorBuildSettingsScene(DungeonEntranceScenePath, true),
                new EditorBuildSettingsScene(FarmScenePath, true),
                new EditorBuildSettingsScene(MokouHouse3DScenePath, true),
                new EditorBuildSettingsScene(BambooHouseScenePath, true),
                new EditorBuildSettingsScene(CombatArenaScenePath, true),
                new EditorBuildSettingsScene(CombatArenaHD2DScenePath, true),
                new EditorBuildSettingsScene(CirnoBossArenaScenePath, true),
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(LoadingScreenScenePath, true),
                new EditorBuildSettingsScene(WorldScenePath, true)
            };
        }

        private readonly struct CharacterAnimationImportSpec
        {
            public CharacterAnimationImportSpec(string fileName, bool loopTime)
            {
                FileName = fileName;
                LoopTime = loopTime;
            }

            public string FileName { get; }
            public bool LoopTime { get; }
        }

        private readonly struct EnemyVisualSpec
        {
            public EnemyVisualSpec(string modelAssetPath, string[] textureAssetPaths)
            {
                ModelAssetPath = modelAssetPath;
                TextureAssetPaths = textureAssetPaths ?? System.Array.Empty<string>();
            }

            public string ModelAssetPath { get; }
            public string[] TextureAssetPaths { get; }
            public bool HasImportedModel => !string.IsNullOrEmpty(ModelAssetPath);

            public static EnemyVisualSpec Fallback()
            {
                return new EnemyVisualSpec(string.Empty, System.Array.Empty<string>());
            }
        }

        private readonly struct EnemyAnimationSpec
        {
            public EnemyAnimationSpec(
                string controllerPath,
                string clipRoot,
                string idleFileName,
                string moveFileName,
                string attackFileName,
                string projectileFileName,
                string takeDamageFileName,
                string dieFileName)
            {
                ControllerPath = controllerPath;
                IdleClipPath = ResolveClipPath(clipRoot, idleFileName);
                MoveClipPath = ResolveClipPath(clipRoot, moveFileName);
                AttackClipPath = ResolveClipPath(clipRoot, attackFileName);
                ProjectileClipPath = ResolveClipPath(clipRoot, projectileFileName);
                TakeDamageClipPath = ResolveClipPath(clipRoot, takeDamageFileName);
                DieClipPath = ResolveClipPath(clipRoot, dieFileName);
            }

            public string ControllerPath { get; }
            public string IdleClipPath { get; }
            public string MoveClipPath { get; }
            public string AttackClipPath { get; }
            public string ProjectileClipPath { get; }
            public string TakeDamageClipPath { get; }
            public string DieClipPath { get; }
            public bool HasAnyClip =>
                !string.IsNullOrEmpty(IdleClipPath)
                || !string.IsNullOrEmpty(MoveClipPath)
                || !string.IsNullOrEmpty(AttackClipPath)
                || !string.IsNullOrEmpty(ProjectileClipPath)
                || !string.IsNullOrEmpty(TakeDamageClipPath)
                || !string.IsNullOrEmpty(DieClipPath);

            public static EnemyAnimationSpec Fallback()
            {
                return new EnemyAnimationSpec(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            private static string ResolveClipPath(string clipRoot, string fileName)
            {
                if (string.IsNullOrEmpty(clipRoot) || string.IsNullOrEmpty(fileName))
                {
                    return string.Empty;
                }

                return $"{clipRoot}/{fileName}";
            }
        }
    }
}
