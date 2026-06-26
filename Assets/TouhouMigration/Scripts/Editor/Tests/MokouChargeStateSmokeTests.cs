using System;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationMokouChargeState: the Mokou charge state machine (Godot CardBuildMokouActionChain
    // charge/release): charge build-up + phases, overheat self-damage, the charge-tier damage multiplier,
    // and terminal release (energy gate, damage scaling, flame, perfect bonus).
    public static class MokouChargeStateSmokeTests
    {
        private const double Tol = 1e-6;

        [MenuItem("Touhou Migration/Tests/Run Mokou Charge State Smoke Tests")]
        public static void RunAll()
        {
            TestBeginAndFullCharge();
            TestOverheatSelfDamage();
            TestChargeDamageMultiplierTiers();
            TestReleaseGatesAndScaling();
            TestReleaseBlockedReasons();
            TestProcessModifierAndTriggers();
            Debug.Log("Mokou charge state smoke tests passed.");
        }

        private static void TestBeginAndFullCharge()
        {
            MigrationMokouChargeState chain = new MigrationMokouChargeState();
            AssertEqual(MokouPhase.Idle, chain.Phase, "Starts idle.");
            AssertEqual(100.0, chain.Energy, "Starts at full energy.");

            chain.BeginCharge();
            AssertEqual(MokouPhase.Charging, chain.Phase, "Begin enters the charging phase.");

            // Half of FULL_CHARGE_SECONDS (1.6) -> ~50 charge, still charging.
            chain.AdvanceCharge(0.8);
            AssertTrue(Math.Abs(50.0 - chain.Charge) < Tol, "0.8s of 1.6s full-charge reaches ~50 charge.");
            AssertEqual(MokouPhase.Charging, chain.Phase, "Below 100 stays charging.");

            // Another 0.8s -> exactly 100 -> Charged (no overshoot, no overheat).
            chain.AdvanceCharge(0.8);
            AssertTrue(Math.Abs(100.0 - chain.Charge) < Tol, "A full 1.6s reaches 100 charge.");
            AssertEqual(MokouPhase.Charged, chain.Phase, "Exactly 100 is Charged, not Overheat.");
        }

        private static void TestOverheatSelfDamage()
        {
            MigrationMokouChargeState chain = new MigrationMokouChargeState();
            chain.BeginCharge();
            // 2.0s overshoots 100 (full at 1.6s); the 0.4s past full overheats and self-damages.
            chain.AdvanceCharge(2.0);
            AssertEqual(MokouPhase.Overheat, chain.Phase, "Charging past 100 enters Overheat.");
            AssertTrue(chain.Hp < 100.0, "Overheat applies self-damage.");
            AssertTrue(chain.Hp >= 1.0, "Self-damage never drops HP below 1.");
        }

        private static void TestChargeDamageMultiplierTiers()
        {
            // <35 -> 0.45, >=35 -> 0.65, >=70 -> 0.85, >=100 -> 1.0.
            AssertTrue(Math.Abs(0.45 - MultiplierAtCharge(0.3)) < Tol, "Below 35 charge -> x0.45.");
            AssertTrue(Math.Abs(0.65 - MultiplierAtCharge(0.6)) < Tol, "35..70 charge -> x0.65.");
            AssertTrue(Math.Abs(0.85 - MultiplierAtCharge(1.2)) < Tol, "70..100 charge -> x0.85.");
            AssertTrue(Math.Abs(1.0 - MultiplierAtCharge(1.6)) < Tol, "Full charge -> x1.0.");
        }

        private static double MultiplierAtCharge(double chargeSeconds)
        {
            MigrationMokouChargeState chain = new MigrationMokouChargeState();
            chain.BeginCharge();
            chain.AdvanceCharge(chargeSeconds);
            return chain.ChargeDamageMultiplier();
        }

        private static void TestReleaseGatesAndScaling()
        {
            MigrationMokouChargeState chain = new MigrationMokouChargeState();
            chain.BindTerminal(baseDamage: 100, energyCost: 25, flame: 4, triggerCoefficient: 1.0);
            chain.BeginCharge();
            chain.AdvanceCharge(1.6); // full charge -> x1.0 charge multiplier, terminal multiplier 1.0

            MokouReleaseResult result = chain.ReleaseCharge();
            AssertTrue(result.Success, "A charged terminal release with enough energy succeeds.");
            AssertTrue(Math.Abs(100.0 - result.Damage) < Tol, "Full-charge base damage is 100 * 1.0 * 1.0.");
            AssertEqual(25.0, result.EnergySpent, "Release spends the terminal's energy cost.");
            AssertEqual(75.0, chain.Energy, "Energy is deducted by the cost.");
            AssertEqual(4, result.Flame, "Release reports the terminal flame.");
            AssertEqual(4, chain.GetStatus("flame"), "Release applies flame to the enemy status.");
            AssertEqual(MokouPhase.Recovery, chain.Phase, "Release moves to Recovery.");
            AssertEqual(0.0, chain.Charge, "Release consumes the charge.");
        }

        private static void TestReleaseBlockedReasons()
        {
            // No bound terminal.
            MigrationMokouChargeState noTerminal = new MigrationMokouChargeState();
            noTerminal.BeginCharge();
            noTerminal.AdvanceCharge(1.6);
            AssertEqual("missing_terminal", noTerminal.ReleaseCharge().Reason, "Release without a terminal fails.");

            // Not charging (idle).
            MigrationMokouChargeState idle = new MigrationMokouChargeState();
            idle.BindTerminal(100, 25, 0, 1.0);
            AssertTrue(idle.ReleaseCharge().Reason.StartsWith("invalid_phase", StringComparison.Ordinal),
                "Release from idle fails on phase.");

            // Not enough energy.
            MigrationMokouChargeState broke = new MigrationMokouChargeState();
            broke.BindTerminal(100, 90, 0, 1.0);
            broke.SetEnergy(10);
            broke.BeginCharge();
            broke.AdvanceCharge(1.6);
            AssertEqual("energy", broke.ReleaseCharge().Reason, "Release without enough energy fails.");
        }

        private static void TestProcessModifierAndTriggers()
        {
            MigrationMokouChargeState chain = new MigrationMokouChargeState();
            AssertEqual(1.0, chain.ChargeSpeedMultiplier, "Charge speed starts at x1.");
            AssertEqual(0.0, chain.ChargeDodgeRetain, "Charge-dodge retain starts at 0.");

            // Godot apply_process_modifier: dodge-retain takes the max, speed multiplies, damage-bonus adds.
            chain.ApplyProcessModifier(chargeDodgeRetain: 0.5, chargeSpeedMultiplier: 2.0, terminalDamageBonus: 0.1);
            AssertEqual(0.5, chain.ChargeDodgeRetain, "Process modifier sets the dodge retain.");
            AssertEqual(2.0, chain.ChargeSpeedMultiplier, "Process modifier multiplies the charge speed.");
            AssertTrue(Math.Abs(0.1 - chain.TerminalDamageBonus) < Tol, "Process modifier adds the terminal damage bonus.");

            chain.ApplyProcessModifier(chargeDodgeRetain: 0.3, chargeSpeedMultiplier: 0.5, terminalDamageBonus: 0.2);
            AssertEqual(0.5, chain.ChargeDodgeRetain, "Dodge retain keeps the higher value (max).");
            AssertEqual(1.0, chain.ChargeSpeedMultiplier, "Charge speed multiplies (2.0 * 0.5).");
            AssertTrue(Math.Abs(0.3 - chain.TerminalDamageBonus) < Tol, "Terminal damage bonus accumulates (0.1 + 0.2).");

            // A faster charge speed reaches full charge in less time.
            chain.BeginCharge();
            chain.AdvanceCharge(1.6); // at x1.0 speed (2.0*0.5) -> exactly 100
            AssertTrue(Math.Abs(100.0 - chain.Charge) < Tol, "Charge speed multiplier feeds the charge rate.");

            chain.InstallTrigger("after_perfect_dodge");
            chain.InstallTrigger("on_overheat");
            AssertEqual(2, chain.TriggerCount, "Installed triggers are tracked.");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
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
