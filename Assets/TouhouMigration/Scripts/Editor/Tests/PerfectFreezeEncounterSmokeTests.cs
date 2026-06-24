using System;
using TouhouMigration.Editor;
using TouhouMigration.Runtime.Combat;
using TouhouMigration.Runtime.Player;
using TouhouMigration.Runtime.UI;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    public static class PerfectFreezeEncounterSmokeTests
    {
        private const string EncounterPrefabPath = "Assets/TouhouMigration/Prefabs/Encounters/MigrationPerfectFreezeEncounter.prefab";
        private const string PhasePlanAssetPath = "Assets/TouhouMigration/Data/Combat/PerfectFreeze/MigrationPerfectFreezePhasePlan.asset";

        [MenuItem("Touhou Migration/Tests/Run Perfect Freeze Encounter Smoke Tests")]
        public static void RunAll()
        {
            TestScopedSettlementDoesNotForwardFrozenCrystalStreakToGlobalSettlement();
            TestEncounterDirectorSpawnsScopedPerfectFreezeBurst();
            TestPerfectFreezePhasePlanAssetAppliesRuntimeCastValues();
            TestEncounterDirectorUsesPrefabKeyedProjectilePool();
            TestPerfectFreezePhaseStartsWithSafeLaneAndTimedCasts();
            TestIceLanceSnipeCastsWhenPlayerIsFarAndReflectsIntoScopedBoss();
            TestActiveSnowballPressureSuppressesFarIceLanceSnipe();
            TestNearDistanceCastStartsSnowballHazard();
            TestCloseDistanceDoesNotStartSnowballHazard();
            TestBossMovementIntentUsesSnowballPushAndCloseEvade();
            TestSnowballHazardRollsGrowsExpiresAndShatters();
            TestSnowballHazardDamagesPlayerAndBouncesAtArenaBoundary();
            TestPerfectFreezePhaseEmitsClearCaptureAndTimeoutResults();
            TestPerfectFreezeOutcomePresenterConsumesPhaseFinished();
            TestGeneratedPerfectFreezeEncounterPrefabWiresScopedBoss();
            Debug.Log("Perfect Freeze encounter smoke tests passed.");
        }

        private static void TestScopedSettlementDoesNotForwardFrozenCrystalStreakToGlobalSettlement()
        {
            GameObject globalUiObject = CreateGlobalUiWithSettlement(out MigrationProjectileSpecialSettlement globalSettlement);
            GameObject scopedObject = new GameObject("PerfectFreezeEncounterSmoke_ScopedSettlement");
            try
            {
                MigrationProjectileSpecialSettlement scopedSettlement = scopedObject.AddComponent<MigrationProjectileSpecialSettlement>();
                scopedSettlement.ConfigureSharedSettlementFallback(false);

                int scopedEvents = 0;
                scopedSettlement.PerfectFreezeStaggerReady += _ => scopedEvents++;
                for (int index = 0; index < 12; index++)
                {
                    scopedSettlement.SettleShatter(CreateFrozenCrystalShatter());
                }

                AssertEqual(false, scopedSettlement.UsesSharedSettlementFallback, "Encounter-scoped settlement should opt out of global forwarding.");
                AssertEqual(12, scopedSettlement.FrozenCrystalBreakCount, "Scoped settlement should count frozen crystals even without a local gauge.");
                AssertEqual(1, scopedSettlement.PerfectFreezeStaggerEventCount, "Scoped settlement should emit its own Perfect Freeze stagger.");
                AssertEqual(1, scopedEvents, "Scoped event subscriber should receive the local stagger event.");
                AssertEqual(0, globalSettlement.FrozenCrystalBreakCount, "Global settlement should not receive scoped encounter frozen crystals.");
                AssertEqual(0, globalSettlement.PerfectFreezeStaggerEventCount, "Global settlement should not emit a stagger for the scoped encounter.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(scopedObject);
                UnityEngine.Object.DestroyImmediate(globalUiObject);
            }
        }

        private static void TestEncounterDirectorSpawnsScopedPerfectFreezeBurst()
        {
            GameObject globalUiObject = CreateGlobalUiWithSettlement(out MigrationProjectileSpecialSettlement globalSettlement);
            GameObject encounterObject = new GameObject("PerfectFreezeEncounterSmoke_Encounter");
            GameObject bossObject = CreateBossTarget("PerfectFreezeEncounterSmoke_Boss");
            GameObject spectatorObject = CreateBossTarget("PerfectFreezeEncounterSmoke_Spectator");
            GameObject projectilePrefab = CreatePerfectFreezeProjectilePrefab();
            try
            {
                MigrationSimpleEnemyController boss = bossObject.GetComponent<MigrationSimpleEnemyController>();
                MigrationSimpleEnemyController spectator = spectatorObject.GetComponent<MigrationSimpleEnemyController>();

                MigrationProjectileSpecialSettlement scopedSettlement = encounterObject.AddComponent<MigrationProjectileSpecialSettlement>();
                scopedSettlement.ConfigureSharedSettlementFallback(false);

                MigrationPerfectFreezeStaggerAdapter bossAdapter = bossObject.AddComponent<MigrationPerfectFreezeStaggerAdapter>();
                bossAdapter.BindSettlement(scopedSettlement);
                bossAdapter.BindEnemyController(boss);

                MigrationPerfectFreezeStaggerAdapter spectatorAdapter = spectatorObject.AddComponent<MigrationPerfectFreezeStaggerAdapter>();
                spectatorAdapter.BindSettlement(globalSettlement);
                spectatorAdapter.BindEnemyController(spectator);

                MigrationPerfectFreezeEncounterDirector director = encounterObject.AddComponent<MigrationPerfectFreezeEncounterDirector>();
                director.BindProjectilePrefab(projectilePrefab.GetComponent<MigrationEnemyProjectile>());
                director.BindScopedSettlement(scopedSettlement);
                director.BindBossController(boss);
                director.BindStaggerAdapter(bossAdapter);
                director.ConfigurePattern(80, 12, 4f, 1.2f);

                int spawned = director.SpawnPerfectFreezeBurst(Vector3.zero, new Vector3(0f, 0f, 8f));
                AssertEqual(12, spawned, "Encounter burst should spawn the configured frozen-crystal streak count.");
                AssertEqual(12, director.ActiveProjectileCount, "Director should track spawned active projectiles.");
                AssertEqual(true, director.LastSpawnedProjectile.PerfectFreezeCycleEnabled, "Spawned projectiles should use the Perfect Freeze lifecycle prefab.");

                foreach (MigrationEnemyProjectile projectile in director.GetActiveProjectilesSnapshot())
                {
                    projectile.Tick(0.5f, new Vector3(99f, 0f, 99f));
                    projectile.Tick(1.6f, new Vector3(99f, 0f, 99f));
                    AssertEqual("frozen", projectile.CurrentPerfectFreezeState, "Encounter projectile should become a shatterable frozen crystal.");
                    AssertEqual(true, projectile.TryApplyShatterDamage(20f, "heavy", projectile.transform.position), "Heavy attacks should shatter frozen encounter crystals.");
                }

                AssertEqual(1, scopedSettlement.PerfectFreezeStaggerEventCount, "Encounter settlement should receive the full frozen-crystal streak.");
                AssertEqual(true, boss.IsStunned, "Encounter boss should consume the local Perfect Freeze stagger.");
                AssertApproximately(1.2f, boss.StunRemainingSeconds, 0.001f, "Boss stun should use the Godot-like Perfect Freeze duration.");
                AssertEqual(0, globalSettlement.PerfectFreezeStaggerEventCount, "Global settlement should remain untouched by scoped encounter projectiles.");
                AssertEqual(false, spectator.IsStunned, "Unrelated adapters bound to the global settlement should not be stunned by the encounter.");
            }
            finally
            {
                foreach (MigrationEnemyProjectile projectile in UnityEngine.Object.FindObjectsByType<MigrationEnemyProjectile>(FindObjectsInactive.Include))
                {
                    if (projectile != null && projectile.gameObject.name.StartsWith("MigrationPerfectFreezeEncounterProjectile", StringComparison.Ordinal))
                    {
                        UnityEngine.Object.DestroyImmediate(projectile.gameObject);
                    }
                }

                UnityEngine.Object.DestroyImmediate(projectilePrefab);
                UnityEngine.Object.DestroyImmediate(spectatorObject);
                UnityEngine.Object.DestroyImmediate(bossObject);
                UnityEngine.Object.DestroyImmediate(encounterObject);
                UnityEngine.Object.DestroyImmediate(globalUiObject);
            }
        }

        private static void TestGeneratedPerfectFreezeEncounterPrefabWiresScopedBoss()
        {
            TouhouMigrationProjectBuilder.BuildInitialProject();

            GameObject encounterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EncounterPrefabPath);
            AssertEqual(true, encounterPrefab != null, "Builder should generate a dedicated Perfect Freeze encounter prefab.");

            MigrationPerfectFreezeEncounterDirector director = encounterPrefab.GetComponent<MigrationPerfectFreezeEncounterDirector>();
            MigrationPerfectFreezePhasePlan phasePlan = AssetDatabase.LoadAssetAtPath<MigrationPerfectFreezePhasePlan>(PhasePlanAssetPath);
            AssertEqual(true, director != null, "Generated encounter prefab should carry the encounter director.");
            AssertEqual(true, phasePlan != null, "Builder should generate a Perfect Freeze phase plan asset.");
            AssertEqual(true, director.HasPhasePlan, "Generated encounter prefab should reference the phase plan asset.");
            AssertEqual(phasePlan, director.PhasePlan, "Generated encounter director should use the generated phase plan asset.");
            AssertEqual(true, director.HasProjectilePool, "Generated encounter prefab should own a prefab-keyed projectile pool.");
            AssertEqual(true, director.HasProjectilePrefab, "Encounter director should reference the Perfect Freeze projectile prefab.");
            AssertEqual(true, director.HasIceOrbProjectilePrefab, "Encounter director should reference the ice-orb projectile prefab.");
            AssertEqual(true, director.HasIceShardProjectilePrefab, "Encounter director should reference the ice-shard projectile prefab.");
            AssertEqual(true, director.HasIceLanceProjectilePrefab, "Encounter director should reference the reflectable ice-lance projectile prefab.");
            AssertEqual(true, director.HasSnowballHazard, "Encounter director should reference the rolling snowball hazard runtime.");
            AssertEqual(true, director.HasScopedSettlement, "Encounter director should own a scoped projectile settlement.");
            AssertEqual(true, director.HasBossController, "Encounter director should target the local boss controller.");
            AssertEqual(true, director.HasStaggerAdapter, "Encounter director should wire the local Perfect Freeze stagger adapter.");
            AssertEqual(true, director.HasBossTarget, "Encounter director should target the local boss HP component.");
            AssertEqual(true, director.HasSafeLaneCue, "Encounter director should wire a local safe-lane cue.");
            MigrationPerfectFreezeOutcomePresenter outcomePresenter = director.GetComponent<MigrationPerfectFreezeOutcomePresenter>();
            AssertEqual(true, outcomePresenter != null, "Generated encounter prefab should carry a Perfect Freeze outcome presenter co-located with the director so it auto-binds at runtime.");
            AssertEqual(80, director.ActiveProjectileCap, "Encounter should preserve the Godot Perfect Freeze active bullet cap.");
            AssertEqual(12, director.BurstProjectileCount, "Encounter burst should preserve the 12 frozen-crystal stagger threshold.");
            AssertEqual(18, director.MaxProjectilesPerCast, "Encounter timed casts should preserve Godot's per-cast projectile budget.");
            AssertApproximately(300f, phasePlan.PhaseMaxHp, 0.001f, "Phase plan should preserve the Perfect Freeze phase HP.");
            AssertApproximately(70f, phasePlan.PhaseDurationSeconds, 0.001f, "Phase plan should preserve the Perfect Freeze phase duration.");
            AssertApproximately(2.2f, phasePlan.PatternIntervalSeconds, 0.001f, "Phase plan should preserve the Perfect Freeze cast cadence.");
            AssertEqual(18, phasePlan.MaxProjectilesPerCast, "Phase plan should preserve the per-cast projectile budget.");
            AssertApproximately(22f, phasePlan.SafeLaneHalfAngleDegrees, 0.001f, "Phase plan should preserve the safe-lane half angle.");
            AssertApproximately(1.05f, phasePlan.SafeLaneCueDurationSeconds, 0.001f, "Phase plan should preserve the safe-lane cue duration.");
            AssertApproximately(70f, phasePlan.ClearBonus, 0.001f, "Phase plan should preserve the clear bonus.");
            AssertApproximately(100f, phasePlan.CaptureBonus, 0.001f, "Phase plan should preserve the capture bonus.");
            AssertApproximately(3.5f, phasePlan.ClearStunSeconds, 0.001f, "Phase plan should preserve the clear stun.");
            AssertApproximately(4.5f, phasePlan.CaptureStunSeconds, 0.001f, "Phase plan should preserve the capture stun.");
            AssertEqual(11, phasePlan.OrbSpreadProjectileCount, "Phase plan should preserve the ice-orb spread count.");
            AssertApproximately(82f, phasePlan.OrbSpreadDegrees, 0.001f, "Phase plan should preserve the ice-orb spread width.");
            AssertEqual(2, phasePlan.FieldRingCount, "Phase plan should preserve the field ring count.");
            AssertEqual(12, phasePlan.FieldBulletsPerRing, "Phase plan should preserve the field bullets per ring.");
            AssertEqual(3, phasePlan.ShardFanRowCount, "Phase plan should preserve the ice-shard fan row count.");
            AssertEqual(6, phasePlan.ShardFanColumnCount, "Phase plan should preserve the ice-shard fan column count.");
            AssertApproximately(68f, phasePlan.ShardFanSpreadDegrees, 0.001f, "Phase plan should preserve the ice-shard fan spread.");
            AssertApproximately(4.2f, phasePlan.CloseRangeDistance, 0.001f, "Phase plan should preserve the close-range evade distance gate.");
            AssertApproximately(12f, phasePlan.IceLanceMinDistance, 0.001f, "Phase plan should preserve the ice-lance snipe distance gate.");
            AssertApproximately(8f, phasePlan.SnowballPreferredDistance, 0.001f, "Phase plan should preserve the snowball preferred distance gate.");
            AssertEqual(11, director.PerfectFreezeOrbSpreadProjectileCount, "Encounter casts should always lead with the Godot ice-orb spread.");
            AssertApproximately(82f, director.PerfectFreezeOrbSpreadDegrees, 0.001f, "Encounter ice-orb spread should preserve the Godot fan width.");
            AssertEqual(2, director.PerfectFreezeFieldRingCount, "Encounter even casts should use the Perfect Freeze field ring count.");
            AssertEqual(12, director.PerfectFreezeFieldBulletsPerRing, "Encounter even casts should preserve the Godot field bullet input before clamping.");
            AssertEqual(3, director.IceShardFanRowCount, "Encounter odd casts should preserve the Godot ice-shard fan row count.");
            AssertEqual(6, director.IceShardFanColumnCount, "Encounter odd casts should preserve the Godot ice-shard fan column count.");
            AssertApproximately(68f, director.IceShardFanSpreadDegrees, 0.001f, "Encounter odd casts should preserve the Godot ice-shard fan width.");
            AssertApproximately(300f, director.PhaseMaxHp, 0.001f, "Encounter should preserve the Perfect Freeze phase HP.");
            AssertApproximately(70f, director.PhaseDurationSeconds, 0.001f, "Encounter should preserve the Perfect Freeze phase duration.");
            AssertApproximately(70f, director.PhaseClearBonus, 0.001f, "Encounter should preserve the Perfect Freeze clear bonus.");
            AssertApproximately(100f, director.PhaseCaptureBonus, 0.001f, "Encounter should preserve the Perfect Freeze capture bonus.");
            AssertApproximately(3.5f, director.PhaseClearStunSeconds, 0.001f, "Encounter should preserve the Perfect Freeze clear stun.");
            AssertApproximately(4.5f, director.PhaseCaptureStunSeconds, 0.001f, "Encounter should preserve the Perfect Freeze capture stun.");
            AssertApproximately(2.2f, director.PatternIntervalSeconds, 0.001f, "Encounter should preserve the Perfect Freeze cast cadence.");
            AssertApproximately(22f, director.SafeLaneHalfAngleDegrees, 0.001f, "Encounter should preserve the safe-lane half angle.");
            AssertApproximately(1.05f, director.SafeLaneCueDurationSeconds, 0.001f, "Encounter should preserve the safe-lane cue fade duration.");
            AssertEqual(true, director.ProjectilePrefab.PerfectFreezeCycleEnabled, "Encounter should use the generated Perfect Freeze projectile lifecycle prefab.");
            AssertEqual("ice_orb", director.IceOrbProjectilePrefab.ProjectileFamily, "Encounter should use a distinct ice-orb projectile family.");
            AssertEqual(false, director.IceOrbProjectilePrefab.PerfectFreezeCycleEnabled, "Ice-orb prefab should not use the Perfect Freeze lifecycle.");
            AssertApproximately(0.32f, director.IceOrbProjectilePrefab.ArmDelaySeconds, 0.001f, "Ice-orb prefab should serialize its Godot telegraph delay.");
            AssertEqual("ice_shard", director.IceShardProjectilePrefab.ProjectileFamily, "Encounter should use a distinct ice-shard projectile family.");
            AssertEqual(false, director.IceShardProjectilePrefab.PerfectFreezeCycleEnabled, "Ice-shard prefab should not use the Perfect Freeze lifecycle.");
            AssertApproximately(0.42f, director.IceShardProjectilePrefab.ArmDelaySeconds, 0.001f, "Ice-shard prefab should serialize its Godot telegraph delay.");
            AssertEqual("ice_lance", director.IceLanceProjectilePrefab.ProjectileFamily, "Encounter should use a distinct ice-lance projectile family.");
            AssertEqual(true, director.IceLanceProjectilePrefab.Reflectable, "Ice-lance prefab should preserve reflectable counterplay.");
            AssertApproximately(2f, director.IceLanceProjectilePrefab.ReflectStunSeconds, 0.001f, "Ice-lance prefab should preserve the Godot stun reward duration.");
            AssertApproximately(4.2f, director.CloseRangeDistance, 0.001f, "Encounter should preserve the close-range evade distance gate.");
            AssertApproximately(12f, director.IceLanceMinDistance, 0.001f, "Encounter should preserve the ice-lance snipe distance gate.");
            AssertApproximately(8f, director.SnowballPreferredDistance, 0.001f, "Encounter should preserve the snowball preferred distance gate.");
            AssertApproximately(1.35f, director.SnowballPushOffset, 0.001f, "Encounter should preserve the snowball push-position offset.");
            AssertEqual(director, director.SnowballHazard.EncounterDirector, "Generated snowball hazard should drive the encounter director pressure seam.");
            AssertEqual(false, director.SnowballHazard.IsActive, "Generated snowball hazard should not start active in the prefab.");
            AssertApproximately(4.2f, director.SnowballHazard.Speed, 0.001f, "Snowball hazard should preserve the Godot roll speed.");
            AssertApproximately(16f, director.SnowballHazard.Damage, 0.001f, "Snowball hazard should preserve the Godot damage.");
            AssertApproximately(5.8f, director.SnowballHazard.DurationSeconds, 0.001f, "Snowball hazard should preserve the Godot pressure duration.");
            AssertApproximately(42f, director.SnowballHazard.InitialShatterHp, 0.001f, "Snowball hazard should preserve the Godot shatter HP.");
            AssertEqual(false, director.ScopedSettlement.UsesSharedSettlementFallback, "Encounter settlement should be scoped instead of forwarding to the global settlement.");
            AssertApproximately(22f, director.SafeLaneCue.HalfAngleDegrees, 0.001f, "Generated safe-lane cue should serialize the half angle.");
            AssertApproximately(1.05f, director.SafeLaneCue.DurationSeconds, 0.001f, "Generated safe-lane cue should serialize the display duration.");
            AssertColorApproximately(new Color(1f, 0.54f, 0.18f, 0.3f), director.SafeLaneCue.CueColor, 0.001f, "Generated safe-lane cue should preserve the Godot albedo color.");
            float expectedSafeLaneWidth = Mathf.Tan(22f * Mathf.Deg2Rad) * 4.3f * 1.55f;
            AssertApproximately(expectedSafeLaneWidth, director.SafeLaneCue.transform.localScale.x, 0.001f, "Generated safe-lane cue should preserve the Godot strip width approximation.");
        }

        private static void TestPerfectFreezePhasePlanAssetAppliesRuntimeCastValues()
        {
            GameObject encounterObject = CreateConfiguredPhaseEncounter(
                "PerfectFreezeEncounterSmoke_PhasePlan",
                out MigrationPerfectFreezeEncounterDirector director,
                out _,
                out _,
                out GameObject bossObject,
                out GameObject safeLaneObject,
                out GameObject perfectFreezeProjectilePrefab,
                out GameObject iceOrbProjectilePrefab,
                out GameObject iceShardProjectilePrefab);
            MigrationPerfectFreezePhasePlan phasePlan = ScriptableObject.CreateInstance<MigrationPerfectFreezePhasePlan>();
            try
            {
                phasePlan.ConfigurePhase(300f, 70f, 2.2f, 18, 22f, 1.05f);
                phasePlan.ConfigureOutcomes(70f, 100f, 3.5f, 4.5f);
                phasePlan.ConfigureCastPlan(11, 82f, 2, 12, 3, 6, 68f);

                director.ConfigurePhase(25f, 5f, 0.5f, 4, 5f, 0.1f);
                director.ConfigurePhaseOutcomes(1f, 2f, 0.25f, 0.5f);
                director.ConfigurePerfectFreezeCastPlan(1, 12f, 1, 4, 1, 2, 16f);
                director.BindPhasePlan(phasePlan);

                AssertEqual(true, director.HasPhasePlan, "Binding a phase plan should mark the director as data-driven.");
                AssertEqual(phasePlan, director.PhasePlan, "Director should retain the bound phase plan.");
                AssertApproximately(300f, director.PhaseMaxHp, 0.001f, "Phase plan should apply phase HP to the director.");
                AssertApproximately(70f, director.PhaseDurationSeconds, 0.001f, "Phase plan should apply phase duration to the director.");
                AssertApproximately(2.2f, director.PatternIntervalSeconds, 0.001f, "Phase plan should apply cast cadence to the director.");
                AssertEqual(18, director.MaxProjectilesPerCast, "Phase plan should apply cast budget to the director.");
                AssertApproximately(22f, director.SafeLaneHalfAngleDegrees, 0.001f, "Phase plan should apply safe-lane angle to the director.");
                AssertApproximately(1.05f, director.SafeLaneCueDurationSeconds, 0.001f, "Phase plan should apply safe-lane cue duration to the director.");
                AssertApproximately(70f, director.PhaseClearBonus, 0.001f, "Phase plan should apply clear bonus to the director.");
                AssertApproximately(100f, director.PhaseCaptureBonus, 0.001f, "Phase plan should apply capture bonus to the director.");
                AssertApproximately(3.5f, director.PhaseClearStunSeconds, 0.001f, "Phase plan should apply clear stun to the director.");
                AssertApproximately(4.5f, director.PhaseCaptureStunSeconds, 0.001f, "Phase plan should apply capture stun to the director.");
                AssertEqual(11, director.PerfectFreezeOrbSpreadProjectileCount, "Phase plan should apply the ice-orb spread count.");
                AssertApproximately(82f, director.PerfectFreezeOrbSpreadDegrees, 0.001f, "Phase plan should apply the ice-orb spread width.");
                AssertEqual(2, director.PerfectFreezeFieldRingCount, "Phase plan should apply the field ring count.");
                AssertEqual(12, director.PerfectFreezeFieldBulletsPerRing, "Phase plan should apply the field bullets per ring.");
                AssertEqual(3, director.IceShardFanRowCount, "Phase plan should apply the ice-shard fan row count.");
                AssertEqual(6, director.IceShardFanColumnCount, "Phase plan should apply the ice-shard fan column count.");
                AssertApproximately(68f, director.IceShardFanSpreadDegrees, 0.001f, "Phase plan should apply the ice-shard fan spread.");

                director.BeginPerfectFreezePhase(Vector3.zero, new Vector3(0f, 0f, 10f));
                AssertEqual("perfect_freeze_field", director.LastCastPatternKind, "A plan-driven opening cast should still use the field pattern.");
                AssertEqual(11, director.LastCastOrbProjectileCount, "A plan-driven opening cast should still start with the ice-orb spread.");
                AssertEqual(7, director.LastCastPerfectFreezeProjectileCount, "A plan-driven opening cast should keep the existing budgeted field count.");

                director.TickPhase(2.2f, Vector3.zero, new Vector3(0f, 0f, 10f));
                AssertEqual("ice_shard_fan", director.LastCastPatternKind, "A plan-driven second cast should still use the ice-shard fan.");
                AssertEqual(11, director.LastCastOrbProjectileCount, "A plan-driven second cast should still start with the ice-orb spread.");
                AssertEqual(7, director.LastCastIceShardProjectileCount, "A plan-driven second cast should keep the existing budgeted shard count.");
            }
            finally
            {
                DestroyPerfectFreezeProjectiles();
                UnityEngine.Object.DestroyImmediate(phasePlan);
                DestroyPhaseEncounter(
                    encounterObject,
                    bossObject,
                    safeLaneObject,
                    perfectFreezeProjectilePrefab,
                    iceOrbProjectilePrefab,
                    iceShardProjectilePrefab);
            }
        }

        private static void TestEncounterDirectorUsesPrefabKeyedProjectilePool()
        {
            GameObject encounterObject = CreateConfiguredPhaseEncounter(
                "PerfectFreezeEncounterSmoke_PooledPhase",
                out MigrationPerfectFreezeEncounterDirector director,
                out _,
                out _,
                out GameObject bossObject,
                out GameObject safeLaneObject,
                out GameObject perfectFreezeProjectilePrefab,
                out GameObject iceOrbProjectilePrefab,
                out GameObject iceShardProjectilePrefab);
            GameObject poolObject = new GameObject("PerfectFreezeEncounterSmoke_ProjectilePool");
            try
            {
                MigrationPrefabPoolService pool = poolObject.AddComponent<MigrationPrefabPoolService>();
                director.BindProjectilePool(pool);

                AssertEqual(true, director.HasProjectilePool, "Director should expose when it owns a projectile pool.");
                AssertEqual(pool, director.ProjectilePool, "Director should retain the bound projectile pool.");

                director.BeginPerfectFreezePhase(Vector3.zero, new Vector3(0f, 0f, 10f));
                AssertEqual(18, director.ActiveProjectileCount, "Opening pooled cast should still spawn the same budgeted projectile count.");
                AssertEqual(18, pool.TotalCreatedCount, "Opening pooled cast should create one instance for each active projectile.");
                AssertEqual(0, pool.TotalReusedCount, "Opening pooled cast should not report reuse before anything is released.");
                AssertEqual(2, pool.PrefabKeyCount, "Opening field cast should use ice-orb and Perfect Freeze prefab keys.");

                ExpireActiveProjectiles(director);
                AssertEqual(0, director.ActiveProjectileCount, "Expired pooled projectiles should be pruned from the director active list.");
                AssertEqual(18, pool.TotalReleasedCount, "Pruning expired projectiles should return them to the pool.");
                AssertEqual(18, pool.InactiveInstanceCount, "Released encounter projectiles should wait inactive in the pool.");

                director.TickPhase(2.2f, Vector3.zero, new Vector3(0f, 0f, 10f));
                AssertEqual("ice_shard_fan", director.LastCastPatternKind, "Second pooled cast should keep the existing odd cast pattern.");
                AssertEqual(18, director.ActiveProjectileCount, "Second pooled cast should still fill the cast budget.");
                AssertEqual(25, pool.TotalCreatedCount, "Second pooled cast should create only the new ice-shard prefab-key instances.");
                AssertEqual(11, pool.TotalReusedCount, "Second pooled cast should reuse the released ice-orb projectiles.");
                AssertEqual(3, pool.PrefabKeyCount, "Pool should track ice-orb, Perfect Freeze field, and ice-shard keys separately.");

                ExpireActiveProjectiles(director);
                AssertEqual(0, director.ActiveProjectileCount, "Second pooled cast projectiles should also return through pruning.");

                director.TickPhase(2.2f, Vector3.zero, new Vector3(0f, 0f, 10f));
                AssertEqual("perfect_freeze_field", director.LastCastPatternKind, "Third pooled cast should keep the existing even cast pattern.");
                AssertEqual(18, director.ActiveProjectileCount, "Third pooled cast should still fill the cast budget.");
                AssertEqual(25, pool.TotalCreatedCount, "Third pooled cast should reuse existing ice-orb and field instances without growing the pool.");
                AssertEqual(29, pool.TotalReusedCount, "Third pooled cast should reuse all eighteen projectile instances from existing prefab keys.");
            }
            finally
            {
                DestroyPerfectFreezeProjectiles();
                UnityEngine.Object.DestroyImmediate(poolObject);
                DestroyPhaseEncounter(
                    encounterObject,
                    bossObject,
                    safeLaneObject,
                    perfectFreezeProjectilePrefab,
                    iceOrbProjectilePrefab,
                    iceShardProjectilePrefab);
            }
        }

        private static void TestIceLanceSnipeCastsWhenPlayerIsFarAndReflectsIntoScopedBoss()
        {
            GameObject globalUiObject = CreateGlobalUiWithSettlement(out MigrationProjectileSpecialSettlement globalSettlement);
            GameObject encounterObject = new GameObject("PerfectFreezeEncounterSmoke_IceLanceSnipe");
            GameObject bossObject = CreateBossTarget("PerfectFreezeEncounterSmoke_IceLanceBoss");
            GameObject spectatorObject = CreateBossTarget("PerfectFreezeEncounterSmoke_IceLanceSpectator");
            GameObject safeLaneObject = new GameObject("PerfectFreezeEncounterSmoke_IceLanceSafeLane");
            GameObject projectilePrefab = CreatePerfectFreezeProjectilePrefab();
            GameObject iceOrbProjectilePrefab = CreateSimpleProjectilePrefab(
                "PerfectFreezeEncounterSmoke_IceLanceIceOrbProjectilePrefab",
                "ice_orb",
                0.4f,
                new Color(0.35f, 0.78f, 1f, 1f),
                0.32f);
            GameObject iceShardProjectilePrefab = CreateSimpleProjectilePrefab(
                "PerfectFreezeEncounterSmoke_IceLanceIceShardProjectilePrefab",
                "ice_shard",
                0.52f,
                new Color(0.78f, 0.96f, 1f, 1f),
                0.42f);
            GameObject iceLanceProjectilePrefab = CreateIceLanceProjectilePrefab(
                "PerfectFreezeEncounterSmoke_IceLanceProjectilePrefab");
            try
            {
                MigrationSimpleEnemyController boss = bossObject.GetComponent<MigrationSimpleEnemyController>();
                MigrationSimpleEnemyController spectator = spectatorObject.GetComponent<MigrationSimpleEnemyController>();
                MigrationCombatTargetBehaviour bossTarget = bossObject.GetComponent<MigrationCombatTargetBehaviour>();
                MigrationProjectileSpecialSettlement scopedSettlement =
                    encounterObject.AddComponent<MigrationProjectileSpecialSettlement>();
                scopedSettlement.ConfigureSharedSettlementFallback(false);

                MigrationPerfectFreezeStaggerAdapter bossAdapter =
                    bossObject.AddComponent<MigrationPerfectFreezeStaggerAdapter>();
                bossAdapter.BindSettlement(scopedSettlement);
                bossAdapter.BindEnemyController(boss);

                MigrationPerfectFreezeStaggerAdapter spectatorAdapter =
                    spectatorObject.AddComponent<MigrationPerfectFreezeStaggerAdapter>();
                spectatorAdapter.BindSettlement(globalSettlement);
                spectatorAdapter.BindEnemyController(spectator);

                MigrationPerfectFreezeSafeLaneCue safeLaneCue =
                    safeLaneObject.AddComponent<MigrationPerfectFreezeSafeLaneCue>();
                MigrationPerfectFreezeEncounterDirector director =
                    encounterObject.AddComponent<MigrationPerfectFreezeEncounterDirector>();
                director.BindProjectilePrefab(projectilePrefab.GetComponent<MigrationEnemyProjectile>());
                director.BindIceOrbProjectilePrefab(iceOrbProjectilePrefab.GetComponent<MigrationEnemyProjectile>());
                director.BindIceShardProjectilePrefab(iceShardProjectilePrefab.GetComponent<MigrationEnemyProjectile>());
                director.BindIceLanceProjectilePrefab(iceLanceProjectilePrefab.GetComponent<MigrationEnemyProjectile>());
                director.BindScopedSettlement(scopedSettlement);
                director.BindBossController(boss);
                director.BindBossTarget(bossTarget);
                director.BindSafeLaneCue(safeLaneCue);
                director.ConfigurePattern(80, 12, 4f, 1.2f);
                director.ConfigurePhase(300f, 70f, 2.2f, 18, 22f, 1.05f);
                director.ConfigurePhaseOutcomes(70f, 100f, 3.5f, 4.5f);
                director.ConfigurePerfectFreezeCastPlan(11, 82f, 2, 12, 3, 6, 68f, 12f);

                Vector3 farPlayerPosition = new Vector3(0f, 0f, 16f);
                director.BeginPerfectFreezePhase(Vector3.zero, farPlayerPosition);

                AssertEqual("ice_lance_snipe", director.LastCastPatternKind, "Far-distance casts should use the Godot ice-lance snipe intent.");
                AssertEqual(0, director.LastCastOrbProjectileCount, "Ice-lance snipe should not spend budget on the ice-orb spread.");
                AssertEqual(0, director.LastCastPerfectFreezeProjectileCount, "Ice-lance snipe should not spawn Perfect Freeze field crystals.");
                AssertEqual(0, director.LastCastIceShardProjectileCount, "Ice-lance snipe should not spawn the ice-shard fan.");
                AssertEqual(1, director.LastCastIceLanceProjectileCount, "Ice-lance snipe should spawn exactly one lance.");
                AssertEqual(1, director.LastCastProjectileCount, "Ice-lance snipe should be a focused one-projectile threat.");
                AssertEqual(1, director.ActiveProjectileCount, "Director should track the single ice-lance projectile.");
                AssertEqual(0, safeLaneCue.CueEventCount, "Ice-lance snipe should not reuse the Perfect Freeze safe-lane cue.");
                AssertEqual(1, CountProjectilesByFamily(director.GetActiveProjectilesSnapshot(), "ice_lance"), "Active projectiles should contain one ice-lance family instance.");

                MigrationEnemyProjectile lance =
                    FindProjectileByFamily(director.GetActiveProjectilesSnapshot(), "ice_lance");
                AssertEqual(true, lance != null, "Ice-lance cast should expose a projectile instance.");
                AssertApproximately(22.5f, lance.Speed, 0.001f, "Ice-lance should use the Godot snipe speed.");
                AssertApproximately(16f, lance.Damage, 0.001f, "Ice-lance should use the Godot snipe damage.");
                AssertApproximately(0.62f, lance.ArmDelaySeconds, 0.001f, "Ice-lance should preserve the Godot telegraph delay.");
                AssertApproximately(2f, lance.ReflectStunSeconds, 0.001f, "Ice-lance should preserve the reflect stun reward.");
                AssertEqual(true, lance.Reflectable, "Ice-lance should be reflectable.");
                AssertVectorApproximately(new Vector3(0f, 1.2f, 0.55f), lance.transform.position, 0.001f, "Ice-lance should spawn just in front of the boss origin.");

                lance.Tick(0.62f, farPlayerPosition);
                bool reflected = lance.TryReflect("heavy", lance.transform.position, Vector3.back, scopedSettlement);

                AssertEqual(true, reflected, "Armed ice-lance should accept a player reflect.");
                AssertEqual(1, scopedSettlement.ReflectStunEventCount, "Scoped settlement should receive the ice-lance reflect reward.");
                AssertEqual(1, bossAdapter.ReflectStunEventCount, "Local boss adapter should consume the reflect stun event.");
                AssertEqual(true, boss.IsStunned, "Local boss should be stunned by the reflected ice-lance reward.");
                AssertApproximately(2f, boss.StunRemainingSeconds, 0.001f, "Boss stun should use the Godot ice-lance reward duration.");
                AssertEqual(false, spectator.IsStunned, "Global-settlement spectators should not be stunned by the scoped ice-lance reflect.");
            }
            finally
            {
                DestroyPerfectFreezeProjectiles();
                UnityEngine.Object.DestroyImmediate(iceLanceProjectilePrefab);
                UnityEngine.Object.DestroyImmediate(iceShardProjectilePrefab);
                UnityEngine.Object.DestroyImmediate(iceOrbProjectilePrefab);
                UnityEngine.Object.DestroyImmediate(projectilePrefab);
                UnityEngine.Object.DestroyImmediate(safeLaneObject);
                UnityEngine.Object.DestroyImmediate(spectatorObject);
                UnityEngine.Object.DestroyImmediate(bossObject);
                UnityEngine.Object.DestroyImmediate(encounterObject);
                UnityEngine.Object.DestroyImmediate(globalUiObject);
            }
        }

        private static void TestActiveSnowballPressureSuppressesFarIceLanceSnipe()
        {
            GameObject encounterObject = new GameObject("PerfectFreezeEncounterSmoke_SnowballPressure");
            GameObject bossObject = CreateBossTarget("PerfectFreezeEncounterSmoke_SnowballPressureBoss");
            GameObject safeLaneObject = new GameObject("PerfectFreezeEncounterSmoke_SnowballPressureSafeLane");
            GameObject iceLanceProjectilePrefab = CreateIceLanceProjectilePrefab(
                "PerfectFreezeEncounterSmoke_SnowballPressureIceLanceProjectilePrefab");
            try
            {
                MigrationCombatTargetBehaviour bossTarget = bossObject.GetComponent<MigrationCombatTargetBehaviour>();
                MigrationSimpleEnemyController boss = bossObject.GetComponent<MigrationSimpleEnemyController>();
                MigrationProjectileSpecialSettlement scopedSettlement =
                    encounterObject.AddComponent<MigrationProjectileSpecialSettlement>();
                scopedSettlement.ConfigureSharedSettlementFallback(false);

                MigrationPerfectFreezeSafeLaneCue safeLaneCue =
                    safeLaneObject.AddComponent<MigrationPerfectFreezeSafeLaneCue>();
                MigrationPerfectFreezeEncounterDirector director =
                    encounterObject.AddComponent<MigrationPerfectFreezeEncounterDirector>();
                director.BindIceLanceProjectilePrefab(iceLanceProjectilePrefab.GetComponent<MigrationEnemyProjectile>());
                director.BindScopedSettlement(scopedSettlement);
                director.BindBossController(boss);
                director.BindBossTarget(bossTarget);
                director.BindSafeLaneCue(safeLaneCue);
                director.ConfigurePattern(80, 12, 4f, 1.2f);
                director.ConfigurePhase(300f, 70f, 2.2f, 18, 22f, 1.05f);
                director.ConfigurePhaseOutcomes(70f, 100f, 3.5f, 4.5f);
                director.ConfigurePerfectFreezeCastPlan(11, 82f, 2, 12, 3, 6, 68f, 12f);

                Vector3 farPlayerPosition = new Vector3(0f, 0f, 16f);
                director.SetSnowballPressureActive(true);
                AssertEqual(true, director.SnowballPressureActive, "Director should expose active snowball pressure as runtime intent state.");

                director.BeginPerfectFreezePhase(Vector3.zero, farPlayerPosition);

                AssertEqual("snowball_pressure", director.LastCastPatternKind, "Active snowball pressure should suppress the far ice-lance snipe intent.");
                AssertEqual(0, director.LastCastIceLanceProjectileCount, "Snowball pressure should not stack a new ice-lance threat.");
                AssertEqual(0, director.LastCastOrbProjectileCount, "Snowball pressure should not fall back to the ice-orb spread.");
                AssertEqual(0, director.LastCastPerfectFreezeProjectileCount, "Snowball pressure should not fall back to Perfect Freeze field crystals.");
                AssertEqual(0, director.LastCastIceShardProjectileCount, "Snowball pressure should not fall back to the ice-shard fan.");
                AssertEqual(0, director.LastCastProjectileCount, "Active snowball pressure is represented as ongoing arena pressure, not an extra projectile cast.");
                AssertEqual(0, director.ActiveProjectileCount, "Director should not spawn extra projectiles while snowball pressure owns the cast.");
                AssertEqual(0, CountProjectilesByFamily(director.GetActiveProjectilesSnapshot(), "ice_lance"), "No ice-lance projectile should exist while snowball pressure is active.");
                AssertEqual(0, safeLaneCue.CueEventCount, "Snowball pressure should not reuse the Perfect Freeze safe-lane cue.");

                director.SetSnowballPressureActive(false);
                AssertEqual(false, director.SnowballPressureActive, "Director should allow snowball pressure to be cleared by the future snowball runtime.");
                director.TickPhase(2.2f, Vector3.zero, farPlayerPosition);

                AssertEqual("ice_lance_snipe", director.LastCastPatternKind, "After snowball pressure clears, far-distance casts should be allowed to snipe again.");
                AssertEqual(1, director.LastCastIceLanceProjectileCount, "Cleared snowball pressure should restore the authored ice-lance cast.");
                AssertEqual(1, CountProjectilesByFamily(director.GetActiveProjectilesSnapshot(), "ice_lance"), "The restored far cast should spawn one ice-lance projectile.");
            }
            finally
            {
                DestroyPerfectFreezeProjectiles();
                UnityEngine.Object.DestroyImmediate(iceLanceProjectilePrefab);
                UnityEngine.Object.DestroyImmediate(safeLaneObject);
                UnityEngine.Object.DestroyImmediate(bossObject);
                UnityEngine.Object.DestroyImmediate(encounterObject);
            }
        }

        private static void TestNearDistanceCastStartsSnowballHazard()
        {
            GameObject encounterObject = new GameObject("PerfectFreezeEncounterSmoke_NearSnowballIntent");
            GameObject bossObject = CreateBossTarget("PerfectFreezeEncounterSmoke_NearSnowballIntentBoss");
            GameObject safeLaneObject = new GameObject("PerfectFreezeEncounterSmoke_NearSnowballIntentSafeLane");
            GameObject snowballObject = new GameObject("PerfectFreezeEncounterSmoke_NearSnowballIntentHazard");
            GameObject projectilePrefab = CreatePerfectFreezeProjectilePrefab();
            GameObject iceOrbProjectilePrefab = CreateSimpleProjectilePrefab(
                "PerfectFreezeEncounterSmoke_NearSnowballIntentIceOrbPrefab",
                "ice_orb",
                0.4f,
                new Color(0.35f, 0.78f, 1f, 1f),
                0.32f);
            GameObject iceShardProjectilePrefab = CreateSimpleProjectilePrefab(
                "PerfectFreezeEncounterSmoke_NearSnowballIntentIceShardPrefab",
                "ice_shard",
                0.52f,
                new Color(0.74f, 0.94f, 1f, 1f),
                0.42f);
            GameObject iceLanceProjectilePrefab = CreateIceLanceProjectilePrefab(
                "PerfectFreezeEncounterSmoke_NearSnowballIntentIceLancePrefab");
            try
            {
                MigrationCombatTargetBehaviour bossTarget = bossObject.GetComponent<MigrationCombatTargetBehaviour>();
                MigrationSimpleEnemyController boss = bossObject.GetComponent<MigrationSimpleEnemyController>();
                MigrationProjectileSpecialSettlement scopedSettlement =
                    encounterObject.AddComponent<MigrationProjectileSpecialSettlement>();
                scopedSettlement.ConfigureSharedSettlementFallback(false);

                MigrationPerfectFreezeSafeLaneCue safeLaneCue =
                    safeLaneObject.AddComponent<MigrationPerfectFreezeSafeLaneCue>();
                MigrationPerfectFreezeSnowballHazard snowball =
                    snowballObject.AddComponent<MigrationPerfectFreezeSnowballHazard>();
                MigrationPerfectFreezeEncounterDirector director =
                    encounterObject.AddComponent<MigrationPerfectFreezeEncounterDirector>();
                director.BindProjectilePrefab(projectilePrefab.GetComponent<MigrationEnemyProjectile>());
                director.BindIceOrbProjectilePrefab(iceOrbProjectilePrefab.GetComponent<MigrationEnemyProjectile>());
                director.BindIceShardProjectilePrefab(iceShardProjectilePrefab.GetComponent<MigrationEnemyProjectile>());
                director.BindIceLanceProjectilePrefab(iceLanceProjectilePrefab.GetComponent<MigrationEnemyProjectile>());
                director.BindScopedSettlement(scopedSettlement);
                director.BindBossController(boss);
                director.BindBossTarget(bossTarget);
                director.BindSafeLaneCue(safeLaneCue);
                director.BindSnowballHazard(snowball);
                director.ConfigurePattern(80, 12, 4f, 1.2f);
                director.ConfigurePhase(300f, 70f, 2.2f, 18, 22f, 1.05f);
                director.ConfigurePhaseOutcomes(70f, 100f, 3.5f, 4.5f);
                director.ConfigurePerfectFreezeCastPlan(11, 82f, 2, 12, 3, 6, 68f, 12f, 8f);

                Vector3 nearPlayerPosition = new Vector3(0f, 0f, 8f);
                director.BeginPerfectFreezePhase(Vector3.zero, nearPlayerPosition);

                AssertEqual("snowball_roll", director.LastCastPatternKind, "Near-distance casts should start the authored rolling snowball pressure.");
                AssertEqual(true, snowball.IsActive, "Near-distance snowball casts should activate the bound hazard.");
                AssertEqual(true, director.SnowballPressureActive, "Started snowball casts should immediately own the pressure seam.");
                AssertEqual(1, director.LastAuthoredProjectileCount, "Snowball roll should count as one authored arena hazard.");
                AssertEqual(0, director.LastCastProjectileCount, "Snowball roll should not consume ordinary projectile instances.");
                AssertEqual(0, director.LastCastOrbProjectileCount, "Snowball roll should not stack an ice-orb spread.");
                AssertEqual(0, director.LastCastPerfectFreezeProjectileCount, "Snowball roll should not stack Perfect Freeze field crystals.");
                AssertEqual(0, director.LastCastIceShardProjectileCount, "Snowball roll should not stack an ice-shard fan.");
                AssertEqual(0, director.LastCastIceLanceProjectileCount, "Snowball roll should not stack an ice-lance snipe.");
                AssertEqual(0, director.ActiveProjectileCount, "Snowball roll is a singleton hazard, not an active projectile list entry.");
                AssertEqual(0, safeLaneCue.CueEventCount, "Snowball roll should not reuse the Perfect Freeze safe-lane cue.");
                AssertApproximately(0.88f, snowball.Radius, 0.001f, "Opening snowball cast should use the Godot growth seed for radius.");
                AssertVectorApproximately(new Vector3(0f, 0.88f, 2.4f), snowball.transform.position, 0.001f, "Snowball cast should spawn ahead of the boss toward the player.");

                director.TickPhase(2.2f, Vector3.zero, nearPlayerPosition);

                AssertEqual("snowball_pressure", director.LastCastPatternKind, "An already-active snowball should keep owning pressure instead of spawning a duplicate.");
                AssertEqual(true, snowball.IsActive, "The original snowball should remain active after a pressure tick.");
                AssertEqual(0, director.ActiveProjectileCount, "Ongoing snowball pressure should still not populate the projectile list.");
            }
            finally
            {
                DestroyPerfectFreezeProjectiles();
                UnityEngine.Object.DestroyImmediate(iceLanceProjectilePrefab);
                UnityEngine.Object.DestroyImmediate(iceShardProjectilePrefab);
                UnityEngine.Object.DestroyImmediate(iceOrbProjectilePrefab);
                UnityEngine.Object.DestroyImmediate(projectilePrefab);
                UnityEngine.Object.DestroyImmediate(snowballObject);
                UnityEngine.Object.DestroyImmediate(safeLaneObject);
                UnityEngine.Object.DestroyImmediate(bossObject);
                UnityEngine.Object.DestroyImmediate(encounterObject);
            }
        }

        private static void TestCloseDistanceDoesNotStartSnowballHazard()
        {
            GameObject encounterObject = new GameObject("PerfectFreezeEncounterSmoke_CloseSnowballGuard");
            GameObject snowballObject = new GameObject("PerfectFreezeEncounterSmoke_CloseSnowballGuardHazard");
            try
            {
                MigrationPerfectFreezeSnowballHazard snowball =
                    snowballObject.AddComponent<MigrationPerfectFreezeSnowballHazard>();
                MigrationPerfectFreezeEncounterDirector director =
                    encounterObject.AddComponent<MigrationPerfectFreezeEncounterDirector>();
                director.BindSnowballHazard(snowball);
                director.ConfigurePattern(80, 12, 4f, 1.2f);
                director.ConfigurePhase(300f, 70f, 2.2f, 18, 22f, 1.05f);
                director.ConfigurePerfectFreezeCastPlan(11, 82f, 2, 12, 3, 6, 68f, 12f, 8f, 4.2f);

                director.BeginPerfectFreezePhase(Vector3.zero, new Vector3(0f, 0f, 3f));

                AssertEqual(false, snowball.IsActive, "Close-range casts should not start snowball pressure; Godot uses the evade-close intent there.");
                AssertEqual(false, director.SnowballPressureActive, "Close-range no-snowball casts should leave the pressure seam inactive.");
                AssertEqual("perfect_freeze_field", director.LastCastPatternKind, "Until a Unity evade-close seam exists, close range should fall through without starting snowball.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(snowballObject);
                UnityEngine.Object.DestroyImmediate(encounterObject);
            }
        }

        private static void TestBossMovementIntentUsesSnowballPushAndCloseEvade()
        {
            GameObject pushEncounter = new GameObject("PerfectFreezeEncounterSmoke_SnowballPushMovement");
            GameObject pushBossObject = CreateBossTarget("PerfectFreezeEncounterSmoke_SnowballPushBoss");
            GameObject pushSnowballObject = new GameObject("PerfectFreezeEncounterSmoke_SnowballPushHazard");
            GameObject closeEncounter = new GameObject("PerfectFreezeEncounterSmoke_CloseEvadeMovement");
            GameObject closeBossObject = CreateBossTarget("PerfectFreezeEncounterSmoke_CloseEvadeBoss");
            GameObject closeSnowballObject = new GameObject("PerfectFreezeEncounterSmoke_CloseEvadeHazard");
            try
            {
                MigrationSimpleEnemyController pushBoss = pushBossObject.GetComponent<MigrationSimpleEnemyController>();
                MigrationPerfectFreezeSnowballHazard pushSnowball =
                    pushSnowballObject.AddComponent<MigrationPerfectFreezeSnowballHazard>();
                MigrationPerfectFreezeEncounterDirector pushDirector =
                    pushEncounter.AddComponent<MigrationPerfectFreezeEncounterDirector>();
                pushDirector.BindBossController(pushBoss);
                pushDirector.BindSnowballHazard(pushSnowball);
                pushDirector.ConfigurePhase(300f, 70f, 2.2f, 18, 22f, 1.05f);
                pushDirector.ConfigurePerfectFreezeCastPlan(11, 82f, 2, 12, 3, 6, 68f, 12f, 8f, 4.2f);
                pushDirector.ConfigureBossMovement(1.35f, 2.6f, 1.75f, 2.2f);

                Vector3 playerPosition = new Vector3(0f, 0f, 8f);
                pushDirector.BeginPerfectFreezePhase(Vector3.zero, playerPosition);
                pushDirector.TickPhase(1f, Vector3.zero, playerPosition);

                AssertEqual("snowball_push_position", pushDirector.LastBossMovementIntentKind, "Active snowball pressure should make the boss choose a push-behind-snowball movement intent.");
                Vector3 snowballToPlayer = playerPosition - pushSnowball.transform.position;
                snowballToPlayer.y = 0f;
                Vector3 snowballToBossTarget = pushDirector.LastDesiredBossPosition - pushSnowball.transform.position;
                snowballToBossTarget.y = 0f;
                AssertApproximately(pushSnowball.Radius + pushDirector.SnowballPushOffset, snowballToBossTarget.magnitude, 0.001f, "Snowball push target should stay one radius plus push offset behind the rolling snowball.");
                AssertEqual(true, Vector3.Dot(snowballToPlayer.normalized, snowballToBossTarget.normalized) < -0.999f, "Snowball push target should sit on the opposite side of the snowball from the player lane.");
                AssertVectorApproximately(pushDirector.LastDesiredBossPosition, pushBossObject.transform.position, 0.001f, "Boss should move onto the snowball push target through the phase movement seam.");

                MigrationSimpleEnemyController closeBoss = closeBossObject.GetComponent<MigrationSimpleEnemyController>();
                MigrationPerfectFreezeSnowballHazard closeSnowball =
                    closeSnowballObject.AddComponent<MigrationPerfectFreezeSnowballHazard>();
                MigrationPerfectFreezeEncounterDirector closeDirector =
                    closeEncounter.AddComponent<MigrationPerfectFreezeEncounterDirector>();
                closeDirector.BindBossController(closeBoss);
                closeDirector.BindSnowballHazard(closeSnowball);
                closeDirector.ConfigurePhase(300f, 70f, 2.2f, 18, 22f, 1.05f);
                closeDirector.ConfigurePerfectFreezeCastPlan(11, 82f, 2, 12, 3, 6, 68f, 12f, 8f, 4.2f);
                closeDirector.ConfigureBossMovement(1.35f, 2.6f, 1.75f, 2.2f);

                Vector3 closePlayerPosition = new Vector3(0f, 0f, 3f);
                closeDirector.BeginPerfectFreezePhase(Vector3.zero, closePlayerPosition);
                closeDirector.TickPhase(1f, Vector3.zero, closePlayerPosition);

                AssertEqual("evade_close", closeDirector.LastBossMovementIntentKind, "Close range should use the evade-close movement seam instead of starting snowball.");
                Vector3 closeForward = closePlayerPosition - Vector3.zero;
                closeForward.y = 0f;
                closeForward.Normalize();
                Vector3 closeMovement = closeDirector.LastDesiredBossPosition - Vector3.zero;
                closeMovement.y = 0f;
                float closeBackProjection = Vector3.Dot(closeMovement, -closeForward);
                Vector3 closeSideComponent = closeMovement + closeForward * closeDirector.CloseEvadeBackDistance;
                AssertApproximately(closeDirector.CloseEvadeBackDistance, closeBackProjection, 0.001f, "Close evade target should back away from the player lane.");
                AssertApproximately(closeDirector.CloseEvadeSideDistance, closeSideComponent.magnitude, 0.001f, "Close evade target should add a sidestep without locking tests to left or right.");
                AssertEqual(true, Vector3.Distance(closePlayerPosition, closeDirector.LastDesiredBossPosition) > closePlayerPosition.magnitude, "Close evade target should increase the boss distance from the player.");
                AssertVectorApproximately(closeDirector.LastDesiredBossPosition, closeBossObject.transform.position, 0.001f, "Boss should move onto the close evade target through the phase movement seam.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(closeSnowballObject);
                UnityEngine.Object.DestroyImmediate(closeBossObject);
                UnityEngine.Object.DestroyImmediate(closeEncounter);
                UnityEngine.Object.DestroyImmediate(pushSnowballObject);
                UnityEngine.Object.DestroyImmediate(pushBossObject);
                UnityEngine.Object.DestroyImmediate(pushEncounter);
            }
        }

        private static void TestSnowballHazardRollsGrowsExpiresAndShatters()
        {
            GameObject encounterObject = new GameObject("PerfectFreezeEncounterSmoke_SnowballHazardEncounter");
            GameObject snowballObject = new GameObject("PerfectFreezeEncounterSmoke_SnowballHazard");
            GameObject expiringObject = new GameObject("PerfectFreezeEncounterSmoke_ExpiringSnowballHazard");
            try
            {
                MigrationPerfectFreezeEncounterDirector director =
                    encounterObject.AddComponent<MigrationPerfectFreezeEncounterDirector>();
                MigrationPerfectFreezeSnowballHazard snowball =
                    snowballObject.AddComponent<MigrationPerfectFreezeSnowballHazard>();
                snowball.BindEncounterDirector(director);
                director.BindSnowballHazard(snowball);

                snowball.BeginRolling(Vector3.zero, new Vector3(0f, 0f, 16f), 1f);

                AssertEqual(true, snowball.IsActive, "Beginning a roll should activate the snowball hazard.");
                AssertEqual(false, snowball.IsExpired, "Fresh snowball hazards should not start expired.");
                AssertEqual(false, snowball.IsShattered, "Fresh snowball hazards should not start shattered.");
                AssertEqual(true, director.SnowballPressureActive, "An active snowball hazard should own the encounter pressure seam.");
                AssertApproximately(4.2f, snowball.Speed, 0.001f, "Snowball should use the Godot roll speed.");
                AssertApproximately(16f, snowball.Damage, 0.001f, "Snowball should use the Godot damage.");
                AssertApproximately(42f, snowball.ShatterHp, 0.001f, "Snowball should start with Godot shatter HP.");
                AssertApproximately(0.88f, snowball.Radius, 0.001f, "Growth seed 1 should produce the Godot initial radius.");
                AssertApproximately(2.43f, snowball.MaxRadius, 0.001f, "Snowball max radius should add the Godot growth band.");
                AssertVectorApproximately(new Vector3(0f, 0.88f, 2.4f), snowball.transform.position, 0.001f, "Snowball should spawn ahead of the boss with center height matching radius.");
                AssertVectorApproximately(Vector3.one * 0.88f, snowball.transform.localScale, 0.001f, "Snowball visual scale should follow current radius.");

                snowball.TickSnowball(1f);

                AssertApproximately(1f, snowball.ElapsedSeconds, 0.001f, "Snowball should track elapsed rolling time.");
                AssertApproximately(1.06f, snowball.Radius, 0.001f, "Snowball should grow while rolling.");
                AssertVectorApproximately(new Vector3(0f, 1.06f, 6.6f), snowball.transform.position, 0.001f, "Snowball should roll forward at Godot speed.");
                AssertVectorApproximately(Vector3.one * 1.06f, snowball.transform.localScale, 0.001f, "Snowball scale should update after growth.");

                AssertEqual(false, snowball.TryApplyCounterDamage(14f, "light"), "Non-lethal non-weak counter damage should not shatter the snowball.");
                AssertApproximately(28f, snowball.ShatterHp, 0.001f, "Non-weak counter damage should apply without the weakness multiplier.");
                AssertEqual(false, snowball.TryApplyCounterDamage(14f, "heavy"), "First weak counter hit should damage but not yet shatter.");
                AssertApproximately(7f, snowball.ShatterHp, 0.001f, "Weak counter families should apply the Godot 1.5x multiplier.");
                AssertEqual(true, snowball.TryApplyCounterDamage(5f, "fire"), "A lethal weak counter hit should shatter the snowball.");
                AssertEqual(true, snowball.IsShattered, "Snowball should record a shatter state.");
                AssertEqual(false, snowball.IsActive, "Shattered snowballs should stop rolling.");
                AssertEqual(false, director.SnowballPressureActive, "Shattering the snowball should clear encounter pressure.");
                AssertEqual(1, snowball.ShatterEventCount, "Snowball should count one shatter event.");
                AssertEqual("fire", snowball.LastCounterSourceFamily, "Snowball should record the family that shattered it.");

                MigrationPerfectFreezeSnowballHazard expiringSnowball =
                    expiringObject.AddComponent<MigrationPerfectFreezeSnowballHazard>();
                expiringSnowball.BindEncounterDirector(director);
                director.BindSnowballHazard(expiringSnowball);
                expiringSnowball.BeginRolling(Vector3.zero, new Vector3(0f, 0f, 16f), 1f);
                expiringSnowball.TickSnowball(5.8f);

                AssertEqual(true, expiringSnowball.IsExpired, "Snowball should expire after the Godot pressure duration.");
                AssertEqual(false, expiringSnowball.IsActive, "Expired snowballs should stop rolling.");
                AssertEqual(false, director.SnowballPressureActive, "Expired snowballs should clear encounter pressure.");
                AssertEqual(1, expiringSnowball.ExpireEventCount, "Snowball should count one expiry event.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(expiringObject);
                UnityEngine.Object.DestroyImmediate(snowballObject);
                UnityEngine.Object.DestroyImmediate(encounterObject);
            }
        }

        private static void TestSnowballHazardDamagesPlayerAndBouncesAtArenaBoundary()
        {
            GameObject encounterObject = new GameObject("PerfectFreezeEncounterSmoke_SnowballDamageBounceEncounter");
            GameObject snowballObject = new GameObject("PerfectFreezeEncounterSmoke_SnowballDamageBounceHazard");
            try
            {
                MigrationPerfectFreezeEncounterDirector director =
                    encounterObject.AddComponent<MigrationPerfectFreezeEncounterDirector>();
                MigrationPerfectFreezeSnowballHazard snowball =
                    snowballObject.AddComponent<MigrationPerfectFreezeSnowballHazard>();
                MigrationPlayerHealthRuntime playerHealth = new MigrationPlayerHealthRuntime();
                playerHealth.SetHealth(100f, 100f);
                MigrationCombatRuntime combat = new MigrationCombatRuntime(null, playerHealth);

                snowball.BindEncounterDirector(director);
                snowball.BindCombatRuntime(combat);
                snowball.ConfigureArena(Vector3.zero, 6f);
                director.BindSnowballHazard(snowball);
                director.ConfigurePhase(300f, 70f, 2.2f, 18, 22f, 1.05f);
                director.ConfigurePerfectFreezeCastPlan(11, 82f, 2, 12, 3, 6, 68f, 12f, 8f, 4.2f);
                director.BeginPerfectFreezePhase(Vector3.zero, new Vector3(0f, 0f, 10f));

                snowball.BeginRolling(Vector3.zero, new Vector3(0f, 0f, 8f), 1f);
                PlayerHealthResult hitResult = snowball.TryDamagePlayer();

                AssertApproximately(16f, hitResult.DamageApplied, 0.001f, "Snowball should route player damage through the shared combat runtime.");
                AssertApproximately(84f, playerHealth.CurrentHp, 0.001f, "Snowball player damage should reduce the real player health runtime.");
                AssertEqual(1, snowball.PlayerDamageEventCount, "Snowball should count successful player hit events.");
                AssertEqual(1, director.PhaseHitCount, "Snowball player hits should register against Perfect Freeze capture eligibility.");
                AssertEqual(false, director.CaptureEligible, "Snowball player hits should break Perfect Freeze capture eligibility.");
                AssertApproximately(0.75f, snowball.PlayerDamageCooldownSeconds, 0.001f, "Snowball should preserve the Godot Cirno arena player hit i-frame duration.");
                AssertEqual(true, snowball.PlayerDamageCooldownRemainingSeconds > 0.7f, "A successful snowball hit should open a short player-hit cooldown.");

                PlayerHealthResult blockedHit = snowball.TryDamagePlayer();

                AssertApproximately(0f, blockedHit.DamageApplied, 0.001f, "Snowball should not repeatedly damage the player while the hit cooldown is active.");
                AssertApproximately(84f, playerHealth.CurrentHp, 0.001f, "Blocked snowball hits should leave player HP unchanged.");
                AssertEqual(1, snowball.PlayerDamageEventCount, "Blocked snowball hits should not count as successful damage events.");
                AssertEqual(1, director.PhaseHitCount, "Blocked snowball hits should not register extra phase hits.");

                snowball.TickSnowball(0.75f);
                PlayerHealthResult cooledHit = snowball.TryDamagePlayer();

                AssertApproximately(16f, cooledHit.DamageApplied, 0.001f, "Snowball should damage again after the player-hit cooldown expires.");
                AssertApproximately(68f, playerHealth.CurrentHp, 0.001f, "Second eligible snowball hit should reduce player HP once.");
                AssertEqual(2, snowball.PlayerDamageEventCount, "Second eligible snowball hit should count as another damage event.");
                AssertEqual(2, director.PhaseHitCount, "Second eligible snowball hit should register one more phase hit.");

                AssertVectorApproximately(new Vector3(0f, 0f, 1f), snowball.Direction, 0.001f, "Snowball should start rolling toward the player lane.");
                snowball.TickSnowball(1f);

                AssertEqual(1, snowball.BounceEventCount, "Snowball should bounce once after crossing the configured arena radius.");
                AssertEqual(true, snowball.Direction.z < -0.999f, "Snowball direction should reflect away from the arena edge after bounce.");
                AssertVectorApproximately(new Vector3(0f, 0f, 1f), snowball.LastBounceNormal, 0.001f, "Snowball should expose the arena normal that caused the bounce.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(snowballObject);
                UnityEngine.Object.DestroyImmediate(encounterObject);
            }
        }

        private static void TestPerfectFreezePhaseStartsWithSafeLaneAndTimedCasts()
        {
            GameObject encounterObject = new GameObject("PerfectFreezeEncounterSmoke_Phase");
            GameObject bossObject = CreateBossTarget("PerfectFreezeEncounterSmoke_PhaseBoss");
            GameObject safeLaneObject = new GameObject("PerfectFreezeEncounterSmoke_SafeLaneCue");
            GameObject projectilePrefab = CreatePerfectFreezeProjectilePrefab();
            GameObject iceOrbProjectilePrefab = CreateSimpleProjectilePrefab(
                "PerfectFreezeEncounterSmoke_IceOrbProjectilePrefab",
                "ice_orb",
                0.4f,
                new Color(0.35f, 0.78f, 1f, 1f),
                0.32f);
            GameObject iceShardProjectilePrefab = CreateSimpleProjectilePrefab(
                "PerfectFreezeEncounterSmoke_IceShardProjectilePrefab",
                "ice_shard",
                0.52f,
                new Color(0.78f, 0.96f, 1f, 1f),
                0.42f);
            try
            {
                MigrationCombatTargetBehaviour bossTarget = bossObject.GetComponent<MigrationCombatTargetBehaviour>();
                MigrationSimpleEnemyController boss = bossObject.GetComponent<MigrationSimpleEnemyController>();
                MigrationProjectileSpecialSettlement scopedSettlement = encounterObject.AddComponent<MigrationProjectileSpecialSettlement>();
                scopedSettlement.ConfigureSharedSettlementFallback(false);

                MigrationPerfectFreezeSafeLaneCue safeLaneCue = safeLaneObject.AddComponent<MigrationPerfectFreezeSafeLaneCue>();
                MigrationPerfectFreezeEncounterDirector director = encounterObject.AddComponent<MigrationPerfectFreezeEncounterDirector>();
                director.BindProjectilePrefab(projectilePrefab.GetComponent<MigrationEnemyProjectile>());
                director.BindIceOrbProjectilePrefab(iceOrbProjectilePrefab.GetComponent<MigrationEnemyProjectile>());
                director.BindIceShardProjectilePrefab(iceShardProjectilePrefab.GetComponent<MigrationEnemyProjectile>());
                director.BindScopedSettlement(scopedSettlement);
                director.BindBossController(boss);
                director.BindBossTarget(bossTarget);
                director.BindSafeLaneCue(safeLaneCue);
                director.ConfigurePattern(80, 12, 4f, 1.2f);
                director.ConfigurePhase(300f, 70f, 2.2f, 18, 22f, 1.05f);
                director.ConfigurePerfectFreezeCastPlan(11, 82f, 2, 12, 3, 6, 68f);

                director.BeginPerfectFreezePhase(Vector3.zero, new Vector3(0f, 0f, 10f));

                AssertEqual(true, director.PhaseActive, "Beginning the phase should mark the encounter phase active.");
                AssertApproximately(300f, bossTarget.MaxHp, 0.001f, "Beginning the phase should configure boss HP from the phase table.");
                AssertApproximately(300f, bossTarget.CurrentHp, 0.001f, "Beginning the phase should reset boss HP for the phase.");
                AssertEqual(1, director.PatternCastEventCount, "Phase should immediately start one Perfect Freeze cast.");
                AssertEqual(0, director.LastCastIndex, "The opening Perfect Freeze cast should use the zero-based Godot parity index.");
                AssertEqual("perfect_freeze_field", director.LastCastPatternKind, "Zero-based even casts should add a Perfect Freeze field after the orb spread.");
                AssertEqual(11, director.LastCastOrbProjectileCount, "Every Perfect Freeze cast should lead with the ice-orb spread.");
                AssertEqual(7, director.LastCastPerfectFreezeProjectileCount, "Even casts should spend the remaining budget on field crystals.");
                AssertEqual(0, director.LastCastIceShardProjectileCount, "Even casts should not spawn the odd ice-shard fan.");
                AssertEqual(18, director.LastCastProjectileCount, "Timed cast should use the per-cast projectile budget.");
                AssertEqual(18, director.ActiveProjectileCount, "Director should track timed-cast projectiles.");
                AssertEqual(11, CountProjectilesByFamily(director.GetActiveProjectilesSnapshot(), "ice_orb"), "Opening cast should spawn distinct ice-orb projectiles.");
                AssertEqual(7, CountProjectilesByFamily(director.GetActiveProjectilesSnapshot(), "frozen_crystal"), "Opening cast should spawn distinct Perfect Freeze field crystals.");
                MigrationEnemyProjectile firstOrb = FindProjectileByFamily(director.GetActiveProjectilesSnapshot(), "ice_orb");
                AssertEqual(true, firstOrb != null, "Opening cast should expose an ice-orb projectile instance.");
                AssertApproximately(6.5f, firstOrb.Speed, 0.001f, "Ice-orb projectile should use the Godot speed.");
                AssertApproximately(8f, firstOrb.Damage, 0.001f, "Ice-orb projectile should use the Godot damage.");
                AssertApproximately(0.32f, firstOrb.ArmDelaySeconds, 0.001f, "Ice-orb projectile should use the Godot telegraph delay.");
                AssertEqual(true, safeLaneCue.IsActive, "A timed cast should show the safe-lane cue before the player has to read bullets.");
                AssertEqual(1, safeLaneCue.CueEventCount, "Safe-lane cue should count the cast cue once.");
                AssertApproximately(22f, safeLaneCue.HalfAngleDegrees, 0.001f, "Safe lane should preserve Godot's half-angle value.");
                AssertApproximately(1.05f, safeLaneCue.DurationSeconds, 0.001f, "Safe lane should preserve the Godot-like cue fade duration.");
                AssertVectorApproximately(new Vector3(0f, 0f, 1f), safeLaneCue.LastLaneDirection, 0.001f, "Safe lane should point from the cast center toward the player lane.");

                director.TickPhase(1.04f, Vector3.zero, new Vector3(0f, 0f, 10f));
                AssertEqual(true, safeLaneCue.IsActive, "Safe-lane cue should remain visible before its fade duration elapses.");
                AssertEqual(1, director.PatternCastEventCount, "Pattern cadence should not cast again before 2.2 seconds.");

                director.TickPhase(0.02f, Vector3.zero, new Vector3(0f, 0f, 10f));
                AssertEqual(false, safeLaneCue.IsActive, "Safe-lane cue should hide after its display duration.");
                AssertEqual(1, director.PatternCastEventCount, "Cue expiry should not imply a second cast.");

                director.TickPhase(1.14f, Vector3.zero, new Vector3(0f, 0f, 10f));
                AssertEqual(2, director.PatternCastEventCount, "Pattern cadence should trigger the next cast at 2.2 seconds.");
                AssertEqual(1, director.LastCastIndex, "The second Perfect Freeze cast should use the odd zero-based parity index.");
                AssertEqual("ice_shard_fan", director.LastCastPatternKind, "Zero-based odd casts should add an ice-shard fan after the orb spread.");
                AssertEqual(11, director.LastCastOrbProjectileCount, "Odd casts should still lead with the ice-orb spread.");
                AssertEqual(0, director.LastCastPerfectFreezeProjectileCount, "Odd casts should not spend budget on field crystals.");
                AssertEqual(7, director.LastCastIceShardProjectileCount, "Odd casts should spend the remaining budget on ice shards.");
                AssertEqual(18, director.LastCastProjectileCount, "Odd timed casts should also respect the per-cast projectile budget.");
                AssertEqual(36, director.ActiveProjectileCount, "Second timed cast should add another per-cast projectile set.");
                AssertEqual(22, CountProjectilesByFamily(director.GetActiveProjectilesSnapshot(), "ice_orb"), "Two casts should include two ice-orb spreads.");
                AssertEqual(7, CountProjectilesByFamily(director.GetActiveProjectilesSnapshot(), "ice_shard"), "Odd cast should spawn distinct ice-shard projectiles.");
                MigrationEnemyProjectile firstShard = FindProjectileByFamily(director.GetActiveProjectilesSnapshot(), "ice_shard");
                AssertEqual(true, firstShard != null, "Odd cast should expose an ice-shard projectile instance.");
                AssertApproximately(12f, firstShard.Damage, 0.001f, "Ice-shard projectile should use the Godot damage.");
                AssertApproximately(0.42f, firstShard.ArmDelaySeconds, 0.001f, "Ice-shard projectile should use the Godot telegraph delay.");
                AssertEqual(1, safeLaneCue.CueEventCount, "Odd ice-shard casts should not reuse the Perfect Freeze safe-lane cue.");
            }
            finally
            {
                foreach (MigrationEnemyProjectile projectile in UnityEngine.Object.FindObjectsByType<MigrationEnemyProjectile>(FindObjectsInactive.Include))
                {
                    if (projectile != null && projectile.gameObject.name.StartsWith("MigrationPerfectFreeze", StringComparison.Ordinal))
                    {
                        UnityEngine.Object.DestroyImmediate(projectile.gameObject);
                    }
                }

                UnityEngine.Object.DestroyImmediate(iceShardProjectilePrefab);
                UnityEngine.Object.DestroyImmediate(iceOrbProjectilePrefab);
                UnityEngine.Object.DestroyImmediate(projectilePrefab);
                UnityEngine.Object.DestroyImmediate(safeLaneObject);
                UnityEngine.Object.DestroyImmediate(bossObject);
                UnityEngine.Object.DestroyImmediate(encounterObject);
            }
        }

        private static void TestPerfectFreezePhaseEmitsClearCaptureAndTimeoutResults()
        {
            GameObject captureEncounter = CreateConfiguredPhaseEncounter(
                "PerfectFreezeEncounterSmoke_CapturePhase",
                out MigrationPerfectFreezeEncounterDirector captureDirector,
                out MigrationCombatTargetBehaviour captureTarget,
                out _,
                out GameObject captureBoss,
                out GameObject captureSafeLane,
                out GameObject capturePerfectFreezePrefab,
                out GameObject captureOrbPrefab,
                out GameObject captureShardPrefab);
            GameObject hitEncounter = CreateConfiguredPhaseEncounter(
                "PerfectFreezeEncounterSmoke_HitPhase",
                out MigrationPerfectFreezeEncounterDirector hitDirector,
                out MigrationCombatTargetBehaviour hitTarget,
                out _,
                out GameObject hitBoss,
                out GameObject hitSafeLane,
                out GameObject hitPerfectFreezePrefab,
                out GameObject hitOrbPrefab,
                out GameObject hitShardPrefab);
            GameObject timeoutEncounter = CreateConfiguredPhaseEncounter(
                "PerfectFreezeEncounterSmoke_TimeoutPhase",
                out MigrationPerfectFreezeEncounterDirector timeoutDirector,
                out _,
                out _,
                out GameObject timeoutBoss,
                out GameObject timeoutSafeLane,
                out GameObject timeoutPerfectFreezePrefab,
                out GameObject timeoutOrbPrefab,
                out GameObject timeoutShardPrefab);
            try
            {
                int captureEvents = 0;
                MigrationPerfectFreezePhaseResult captureResult = null;
                captureDirector.PhaseFinished += result =>
                {
                    captureEvents++;
                    captureResult = result;
                };
                captureDirector.BeginPerfectFreezePhase(Vector3.zero, new Vector3(0f, 0f, 10f));
                captureTarget.ApplyDamage(300f);

                AssertEqual(false, captureDirector.PhaseActive, "A clear should finish the active Perfect Freeze phase.");
                AssertEqual(1, captureEvents, "Capture clear should emit one phase-finished event.");
                AssertEqual(1, captureDirector.PhaseFinishedEventCount, "Director should count phase-finished events.");
                AssertEqual(true, captureResult != null, "Capture clear should provide a phase result.");
                AssertEqual(captureResult, captureDirector.LastPhaseResult, "Director should retain the latest phase result.");
                AssertEqual("clear", captureResult.Reason, "Boss defeat should produce a clear result.");
                AssertEqual(true, captureResult.Captured, "Clearing without player hits should count as capture.");
                AssertApproximately(70f, captureResult.ClearBonus, 0.001f, "Clear result should preserve the Perfect Freeze clear bonus.");
                AssertApproximately(100f, captureResult.CaptureBonus, 0.001f, "Capture result should preserve the Perfect Freeze capture bonus.");
                AssertApproximately(4.5f, captureResult.StunSeconds, 0.001f, "Capture result should use capture stun seconds.");
                AssertEqual(0, captureResult.PhaseHitCount, "Capture result should record no phase hits.");
                AssertEqual(1, captureResult.NextPhaseIndex, "Single-phase result should point to the next phase index.");
                int captureCastCount = captureDirector.PatternCastEventCount;
                captureDirector.TickPhase(2.2f, Vector3.zero, new Vector3(0f, 0f, 10f));
                AssertEqual(captureCastCount, captureDirector.PatternCastEventCount, "Finished phases should not keep casting.");

                int hitEvents = 0;
                hitDirector.PhaseFinished += _ => hitEvents++;
                hitDirector.BeginPerfectFreezePhase(Vector3.zero, new Vector3(0f, 0f, 10f));
                hitDirector.RegisterPlayerHit();
                hitTarget.ApplyDamage(300f);

                AssertEqual(1, hitEvents, "Hit clear should emit one phase-finished event.");
                AssertEqual("clear", hitDirector.LastPhaseResult.Reason, "Boss defeat after a player hit should still clear the phase.");
                AssertEqual(false, hitDirector.LastPhaseResult.Captured, "A player hit should break capture eligibility.");
                AssertApproximately(70f, hitDirector.LastPhaseResult.ClearBonus, 0.001f, "Non-capture clear should still grant clear bonus.");
                AssertApproximately(0f, hitDirector.LastPhaseResult.CaptureBonus, 0.001f, "Non-capture clear should not grant capture bonus.");
                AssertApproximately(3.5f, hitDirector.LastPhaseResult.StunSeconds, 0.001f, "Non-capture clear should use clear stun seconds.");
                AssertEqual(1, hitDirector.LastPhaseResult.PhaseHitCount, "Result should record phase hit count.");
                AssertEqual(1, hitDirector.TotalPlayerHitCount, "Director should retain total player hit count.");

                int timeoutEvents = 0;
                timeoutDirector.PhaseFinished += _ => timeoutEvents++;
                timeoutDirector.BeginPerfectFreezePhase(Vector3.zero, new Vector3(0f, 0f, 10f));
                timeoutDirector.TickPhase(70f, Vector3.zero, new Vector3(0f, 0f, 10f));

                AssertEqual(false, timeoutDirector.PhaseActive, "Timeout should finish the active Perfect Freeze phase.");
                AssertEqual(1, timeoutEvents, "Timeout should emit one phase-finished event.");
                AssertEqual("timeout", timeoutDirector.LastPhaseResult.Reason, "Duration expiry should produce a timeout result.");
                AssertEqual(false, timeoutDirector.LastPhaseResult.Captured, "Timeout should not count as capture.");
                AssertApproximately(0f, timeoutDirector.LastPhaseResult.ClearBonus, 0.001f, "Timeout should not grant clear bonus.");
                AssertApproximately(0f, timeoutDirector.LastPhaseResult.CaptureBonus, 0.001f, "Timeout should not grant capture bonus.");
                AssertApproximately(0f, timeoutDirector.LastPhaseResult.StunSeconds, 0.001f, "Timeout should not grant stun seconds.");
                AssertApproximately(70f, timeoutDirector.LastPhaseResult.PhaseElapsedSeconds, 0.001f, "Timeout result should record elapsed phase time.");
            }
            finally
            {
                DestroyPerfectFreezeProjectiles();
                DestroyPhaseEncounter(
                    captureEncounter,
                    captureBoss,
                    captureSafeLane,
                    capturePerfectFreezePrefab,
                    captureOrbPrefab,
                    captureShardPrefab);
                DestroyPhaseEncounter(
                    hitEncounter,
                    hitBoss,
                    hitSafeLane,
                    hitPerfectFreezePrefab,
                    hitOrbPrefab,
                    hitShardPrefab);
                DestroyPhaseEncounter(
                    timeoutEncounter,
                    timeoutBoss,
                    timeoutSafeLane,
                    timeoutPerfectFreezePrefab,
                    timeoutOrbPrefab,
                    timeoutShardPrefab);
            }
        }

        private static void TestPerfectFreezeOutcomePresenterConsumesPhaseFinished()
        {
            GameObject captureEncounter = CreateConfiguredPhaseEncounter(
                "PerfectFreezeEncounterSmoke_CaptureOutcomePresentation",
                out MigrationPerfectFreezeEncounterDirector captureDirector,
                out MigrationCombatTargetBehaviour captureTarget,
                out _,
                out GameObject captureBoss,
                out GameObject captureSafeLane,
                out GameObject capturePerfectFreezePrefab,
                out GameObject captureOrbPrefab,
                out GameObject captureShardPrefab);
            GameObject hitEncounter = CreateConfiguredPhaseEncounter(
                "PerfectFreezeEncounterSmoke_HitOutcomePresentation",
                out MigrationPerfectFreezeEncounterDirector hitDirector,
                out MigrationCombatTargetBehaviour hitTarget,
                out _,
                out GameObject hitBoss,
                out GameObject hitSafeLane,
                out GameObject hitPerfectFreezePrefab,
                out GameObject hitOrbPrefab,
                out GameObject hitShardPrefab);
            GameObject timeoutEncounter = CreateConfiguredPhaseEncounter(
                "PerfectFreezeEncounterSmoke_TimeoutOutcomePresentation",
                out MigrationPerfectFreezeEncounterDirector timeoutDirector,
                out _,
                out _,
                out GameObject timeoutBoss,
                out GameObject timeoutSafeLane,
                out GameObject timeoutPerfectFreezePrefab,
                out GameObject timeoutOrbPrefab,
                out GameObject timeoutShardPrefab);
            try
            {
                MigrationPerfectFreezeOutcomePresenter capturePresenter =
                    captureEncounter.AddComponent<MigrationPerfectFreezeOutcomePresenter>();
                capturePresenter.BindDirector(captureDirector);
                capturePresenter.ConfigurePresentation(
                    1.1f,
                    new Color(0.64f, 0.95f, 1f, 1f),
                    new Color(1f, 0.86f, 0.34f, 1f),
                    new Color(0.74f, 0.78f, 0.86f, 1f));

                captureDirector.BeginPerfectFreezePhase(Vector3.zero, new Vector3(0f, 0f, 10f));
                captureTarget.ApplyDamage(300f);

                AssertEqual(1, capturePresenter.OutcomeNotificationCount, "Capture result should show one outcome notification.");
                AssertEqual(1, capturePresenter.BonusNotificationCount, "Capture result should show one bonus notification.");
                AssertEqual(true, capturePresenter.HasActiveOutcomeNotification, "Capture result should leave outcome text active.");
                AssertEqual(true, capturePresenter.HasActiveBonusNotification, "Capture result should leave bonus text active.");
                AssertEqual("Perfect Freeze Capture", capturePresenter.LastOutcomeText, "Capture text should name the capture outcome.");
                AssertEqual("+170 bonus  Stun 4.5s", capturePresenter.LastBonusText, "Capture text should summarize clear+capture bonus and stun.");
                AssertEqual(captureDirector.LastPhaseResult, capturePresenter.LastPresentedResult, "Presenter should retain the presented phase result.");
                capturePresenter.Tick(1.1f);
                AssertEqual(false, capturePresenter.HasActiveOutcomeNotification, "Outcome text should hide after its configured display time.");
                AssertEqual(false, capturePresenter.HasActiveBonusNotification, "Bonus text should hide after its configured display time.");

                MigrationPerfectFreezeOutcomePresenter hitPresenter =
                    hitEncounter.AddComponent<MigrationPerfectFreezeOutcomePresenter>();
                hitPresenter.BindDirector(hitDirector);
                hitDirector.BeginPerfectFreezePhase(Vector3.zero, new Vector3(0f, 0f, 10f));
                hitDirector.RegisterPlayerHit();
                hitTarget.ApplyDamage(300f);

                AssertEqual("Perfect Freeze Clear", hitPresenter.LastOutcomeText, "Hit clear should still show a clear result.");
                AssertEqual("+70 bonus  Stun 3.5s", hitPresenter.LastBonusText, "Hit clear should summarize clear bonus without capture bonus.");
                AssertEqual(false, hitPresenter.LastPresentedResult.Captured, "Presenter should keep the non-capture result data.");

                MigrationPerfectFreezeOutcomePresenter timeoutPresenter =
                    timeoutEncounter.AddComponent<MigrationPerfectFreezeOutcomePresenter>();
                timeoutPresenter.BindDirector(timeoutDirector);
                timeoutDirector.BeginPerfectFreezePhase(Vector3.zero, new Vector3(0f, 0f, 10f));
                timeoutDirector.TickPhase(70f, Vector3.zero, new Vector3(0f, 0f, 10f));

                AssertEqual("Perfect Freeze Timeout", timeoutPresenter.LastOutcomeText, "Timeout should show a timeout result.");
                AssertEqual("No bonus", timeoutPresenter.LastBonusText, "Timeout should not claim a clear or capture bonus.");
            }
            finally
            {
                DestroyPerfectFreezeProjectiles();
                DestroyPhaseEncounter(
                    captureEncounter,
                    captureBoss,
                    captureSafeLane,
                    capturePerfectFreezePrefab,
                    captureOrbPrefab,
                    captureShardPrefab);
                DestroyPhaseEncounter(
                    hitEncounter,
                    hitBoss,
                    hitSafeLane,
                    hitPerfectFreezePrefab,
                    hitOrbPrefab,
                    hitShardPrefab);
                DestroyPhaseEncounter(
                    timeoutEncounter,
                    timeoutBoss,
                    timeoutSafeLane,
                    timeoutPerfectFreezePrefab,
                    timeoutOrbPrefab,
                    timeoutShardPrefab);
            }
        }

        private static GameObject CreateGlobalUiWithSettlement(out MigrationProjectileSpecialSettlement settlement)
        {
            GameObject globalUiObject = new GameObject("PerfectFreezeEncounterSmoke_GlobalUI");
            settlement = globalUiObject.AddComponent<MigrationProjectileSpecialSettlement>();
            MigrationGlobalUiController globalUi = globalUiObject.AddComponent<MigrationGlobalUiController>();
            SerializedObject serialized = new SerializedObject(globalUi);
            serialized.FindProperty("projectileSettlement").objectReferenceValue = settlement;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return globalUiObject;
        }

        private static GameObject CreateBossTarget(string name)
        {
            GameObject bossObject = new GameObject(name);
            MigrationCombatTargetBehaviour target = bossObject.AddComponent<MigrationCombatTargetBehaviour>();
            target.Initialize(160f);
            MigrationSimpleEnemyController boss = bossObject.AddComponent<MigrationSimpleEnemyController>();
            boss.BindTarget(target);
            boss.ConfigureMovement(0f, 0f, 0f);
            boss.ConfigureAttackCooldown(999f);
            return bossObject;
        }

        private static GameObject CreatePerfectFreezeProjectilePrefab()
        {
            GameObject projectileObject = new GameObject("PerfectFreezeEncounterSmoke_PerfectFreezeProjectilePrefab");
            MigrationCombatFeedbackTemplate template = projectileObject.AddComponent<MigrationCombatFeedbackTemplate>();
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

            MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
            projectile.ApplyFeedbackTemplate(template);
            MigrationProjectileSpecialSettlement projectileSettlement = projectileObject.AddComponent<MigrationProjectileSpecialSettlement>();
            projectileSettlement.BindProjectile(projectile);
            projectileSettlement.ConfigureRewards(2f, 5f, 8f, 12f, 12, 1.2f);
            return projectileObject;
        }

        private static GameObject CreateConfiguredPhaseEncounter(
            string name,
            out MigrationPerfectFreezeEncounterDirector director,
            out MigrationCombatTargetBehaviour bossTarget,
            out MigrationPerfectFreezeSafeLaneCue safeLaneCue,
            out GameObject bossObject,
            out GameObject safeLaneObject,
            out GameObject perfectFreezeProjectilePrefab,
            out GameObject iceOrbProjectilePrefab,
            out GameObject iceShardProjectilePrefab)
        {
            GameObject encounterObject = new GameObject(name);
            bossObject = CreateBossTarget(name + "_Boss");
            safeLaneObject = new GameObject(name + "_SafeLaneCue");
            perfectFreezeProjectilePrefab = CreatePerfectFreezeProjectilePrefab();
            iceOrbProjectilePrefab = CreateSimpleProjectilePrefab(
                name + "_IceOrbProjectilePrefab",
                "ice_orb",
                0.4f,
                new Color(0.35f, 0.78f, 1f, 1f),
                0.32f);
            iceShardProjectilePrefab = CreateSimpleProjectilePrefab(
                name + "_IceShardProjectilePrefab",
                "ice_shard",
                0.52f,
                new Color(0.78f, 0.96f, 1f, 1f),
                0.42f);

            bossTarget = bossObject.GetComponent<MigrationCombatTargetBehaviour>();
            MigrationSimpleEnemyController boss = bossObject.GetComponent<MigrationSimpleEnemyController>();
            MigrationProjectileSpecialSettlement scopedSettlement = encounterObject.AddComponent<MigrationProjectileSpecialSettlement>();
            scopedSettlement.ConfigureSharedSettlementFallback(false);

            safeLaneCue = safeLaneObject.AddComponent<MigrationPerfectFreezeSafeLaneCue>();
            director = encounterObject.AddComponent<MigrationPerfectFreezeEncounterDirector>();
            director.BindProjectilePrefab(perfectFreezeProjectilePrefab.GetComponent<MigrationEnemyProjectile>());
            director.BindIceOrbProjectilePrefab(iceOrbProjectilePrefab.GetComponent<MigrationEnemyProjectile>());
            director.BindIceShardProjectilePrefab(iceShardProjectilePrefab.GetComponent<MigrationEnemyProjectile>());
            director.BindScopedSettlement(scopedSettlement);
            director.BindBossController(boss);
            director.BindBossTarget(bossTarget);
            director.BindSafeLaneCue(safeLaneCue);
            director.ConfigurePattern(80, 12, 4f, 1.2f);
            director.ConfigurePhase(300f, 70f, 2.2f, 18, 22f, 1.05f);
            director.ConfigurePhaseOutcomes(70f, 100f, 3.5f, 4.5f);
            director.ConfigurePerfectFreezeCastPlan(11, 82f, 2, 12, 3, 6, 68f);
            return encounterObject;
        }

        private static void DestroyPhaseEncounter(
            GameObject encounterObject,
            GameObject bossObject,
            GameObject safeLaneObject,
            GameObject perfectFreezeProjectilePrefab,
            GameObject iceOrbProjectilePrefab,
            GameObject iceShardProjectilePrefab)
        {
            UnityEngine.Object.DestroyImmediate(iceShardProjectilePrefab);
            UnityEngine.Object.DestroyImmediate(iceOrbProjectilePrefab);
            UnityEngine.Object.DestroyImmediate(perfectFreezeProjectilePrefab);
            UnityEngine.Object.DestroyImmediate(safeLaneObject);
            UnityEngine.Object.DestroyImmediate(bossObject);
            UnityEngine.Object.DestroyImmediate(encounterObject);
        }

        private static void DestroyPerfectFreezeProjectiles()
        {
            foreach (MigrationEnemyProjectile projectile in UnityEngine.Object.FindObjectsByType<MigrationEnemyProjectile>(FindObjectsInactive.Include))
            {
                if (projectile != null && projectile.gameObject.name.StartsWith("MigrationPerfectFreeze", StringComparison.Ordinal))
                {
                    UnityEngine.Object.DestroyImmediate(projectile.gameObject);
                }
            }
        }

        private static void ExpireActiveProjectiles(MigrationPerfectFreezeEncounterDirector director)
        {
            foreach (MigrationEnemyProjectile projectile in director.GetActiveProjectilesSnapshot())
            {
                projectile.Tick(10f, new Vector3(99f, 0f, 99f));
            }
        }

        private static GameObject CreateSimpleProjectilePrefab(
            string name,
            string family,
            float visualRadius,
            Color color,
            float armDelaySeconds)
        {
            GameObject projectileObject = new GameObject(name);
            MigrationCombatFeedbackTemplate template = projectileObject.AddComponent<MigrationCombatFeedbackTemplate>();
            template.ConfigureTemplate(
                family,
                true,
                "EnemyProjectile",
                6f,
                visualRadius,
                color,
                true,
                true,
                true,
                1.15f,
                0.7f,
                family,
                false,
                0f,
                string.Empty,
                false,
                armDelaySeconds: armDelaySeconds);

            MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
            projectile.ApplyFeedbackTemplate(template);
            MigrationProjectileGrazePresenter grazePresenter = projectileObject.AddComponent<MigrationProjectileGrazePresenter>();
            grazePresenter.BindProjectile(projectile);
            MigrationProjectileShatterPresenter shatterPresenter = projectileObject.AddComponent<MigrationProjectileShatterPresenter>();
            shatterPresenter.BindProjectile(projectile);
            return projectileObject;
        }

        private static GameObject CreateIceLanceProjectilePrefab(string name)
        {
            GameObject projectileObject = new GameObject(name);
            MigrationCombatFeedbackTemplate template = projectileObject.AddComponent<MigrationCombatFeedbackTemplate>();
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

            MigrationEnemyProjectile projectile = projectileObject.AddComponent<MigrationEnemyProjectile>();
            projectile.ApplyFeedbackTemplate(template);
            projectile.Configure(22.5f, 16f, Vector3.forward, true, 0.24f);
            MigrationProjectileGrazePresenter grazePresenter = projectileObject.AddComponent<MigrationProjectileGrazePresenter>();
            grazePresenter.BindProjectile(projectile);
            MigrationProjectileShatterPresenter shatterPresenter = projectileObject.AddComponent<MigrationProjectileShatterPresenter>();
            shatterPresenter.BindProjectile(projectile);
            MigrationProjectileSpecialSettlement settlement =
                projectileObject.AddComponent<MigrationProjectileSpecialSettlement>();
            settlement.BindProjectile(projectile);
            settlement.ConfigureRewards(2f, 5f, 8f, 12f, 12, 1.2f);
            return projectileObject;
        }

        private static MigrationProjectileShatterResult CreateFrozenCrystalShatter()
        {
            return new MigrationProjectileShatterResult(
                null,
                "frozen_crystal",
                "heavy",
                20f,
                1.5f,
                30f,
                0f,
                Vector3.zero,
                true,
                null);
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

        private static void AssertVectorApproximately(Vector3 expected, Vector3 actual, float tolerance, string message)
        {
            if (Vector3.Distance(expected, actual) > tolerance)
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }

        private static void AssertColorApproximately(Color expected, Color actual, float tolerance, string message)
        {
            if (Mathf.Abs(expected.r - actual.r) > tolerance ||
                Mathf.Abs(expected.g - actual.g) > tolerance ||
                Mathf.Abs(expected.b - actual.b) > tolerance ||
                Mathf.Abs(expected.a - actual.a) > tolerance)
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }

        private static int CountProjectilesByFamily(MigrationEnemyProjectile[] projectiles, string family)
        {
            int count = 0;
            foreach (MigrationEnemyProjectile projectile in projectiles)
            {
                if (projectile != null && projectile.ProjectileFamily == family)
                {
                    count++;
                }
            }

            return count;
        }

        private static MigrationEnemyProjectile FindProjectileByFamily(MigrationEnemyProjectile[] projectiles, string family)
        {
            foreach (MigrationEnemyProjectile projectile in projectiles)
            {
                if (projectile != null && projectile.ProjectileFamily == family)
                {
                    return projectile;
                }
            }

            return null;
        }
    }
}
