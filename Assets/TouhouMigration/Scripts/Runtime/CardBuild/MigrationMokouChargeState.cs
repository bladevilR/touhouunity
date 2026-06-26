using System;

namespace TouhouMigration.Runtime.CardBuild
{
    public enum MokouPhase
    {
        Idle,
        Startup,
        Charging,
        Charged,
        Overheat,
        Release,
        Recovery,
        Aftermath
    }

    // The outcome of a terminal release (Godot release_charge return dict).
    public readonly struct MokouReleaseResult
    {
        public MokouReleaseResult(bool success, string reason, double damage, double energySpent, int flame, double triggerCoefficient, bool perfect)
        {
            Success = success;
            Reason = reason;
            Damage = damage;
            EnergySpent = energySpent;
            Flame = flame;
            TriggerCoefficient = triggerCoefficient;
            Perfect = perfect;
        }

        public bool Success { get; }
        public string Reason { get; }
        public double Damage { get; }
        public double EnergySpent { get; }
        public int Flame { get; }
        public double TriggerCoefficient { get; }
        public bool Perfect { get; }

        public static MokouReleaseResult Fail(string reason) => new MokouReleaseResult(false, reason, 0, 0, 0, 0, false);
    }

    // The Mokou charge state machine (Godot CardBuildMokouActionChain charge/release): a terminal binds a
    // base damage + energy cost + flame; charging builds toward 100 (overshoot to 150 incurs overheat
    // self-damage), and a release spends energy, scales damage by the charge tier + overheat bonus + a
    // perfect-release bonus, and applies flame. UnityEngine-free + unit-testable. (The full action chain's
    // dodge/ignite/process-modifier/trigger surface is a follow-up; this slice covers the charge/release
    // core that drives boss damage.)
    public sealed class MigrationMokouChargeState
    {
        private const double MaxCharge = 150.0;
        private const double MaxEnergy = 100.0;
        private const double MaxHp = 100.0;
        private const double MaxImmortalGauge = 100.0;
        private const double FullChargeSeconds = 1.6;
        private const double PerfectReleaseBonus = 0.20;
        private const double OverheatSelfDamagePerSecond = 4.0;
        private const double OverheatDamagePer10Charge = 0.06;

        private readonly System.Collections.Generic.Dictionary<string, int> statuses =
            new System.Collections.Generic.Dictionary<string, int>();

        private readonly System.Collections.Generic.List<string> triggers = new System.Collections.Generic.List<string>();

        private bool hasTerminal;
        private double terminalBaseDamage;
        private double terminalEnergyCost;
        private int terminalFlame;
        private double terminalTriggerCoefficient = 1.0;

        // Process-modifier accumulators (Godot apply_process_modifier): feed AdvanceCharge (speed) and
        // TerminalDamageMultiplier (bonus); charge-dodge retain is consumed by the deferred dodge mechanic.
        public double ChargeDodgeRetain { get; private set; }
        public double ChargeSpeedMultiplier { get; private set; } = 1.0;
        public double TerminalDamageBonus { get; private set; }
        public int TriggerCount => triggers.Count;

        public MokouPhase Phase { get; private set; } = MokouPhase.Idle;
        public double Charge { get; private set; }
        public double Hp { get; private set; } = MaxHp;
        public double Energy { get; private set; } = MaxEnergy;
        public double ImmortalGauge { get; private set; }

        public void BindTerminal(double baseDamage, double energyCost, int flame, double triggerCoefficient)
        {
            hasTerminal = true;
            terminalBaseDamage = baseDamage;
            terminalEnergyCost = energyCost;
            terminalFlame = flame;
            terminalTriggerCoefficient = triggerCoefficient;
        }

        public void BeginCharge()
        {
            if (Phase == MokouPhase.Release || Phase == MokouPhase.Recovery)
            {
                return;
            }

            Phase = MokouPhase.Charging;
            Charge = Math.Max(0.0, Charge);
        }

        public void AdvanceCharge(double deltaSeconds)
        {
            if (Phase != MokouPhase.Charging && Phase != MokouPhase.Charged && Phase != MokouPhase.Overheat)
            {
                return;
            }

            double dt = Math.Max(0.0, deltaSeconds);
            if (dt <= 0.0)
            {
                return;
            }

            double chargeBefore = Charge;
            double chargeRate = (100.0 / FullChargeSeconds) * ChargeSpeedMultiplier;
            Charge = Clamp(Charge + chargeRate * dt, 0.0, MaxCharge);

            if (Charge >= 100.0)
            {
                double overheatStart = Math.Max(100.0, chargeBefore);
                double overheatGain = Math.Max(0.0, Charge - overheatStart);
                if (overheatGain > 0.0)
                {
                    double overheatSeconds = overheatGain / chargeRate;
                    ApplySelfDamage(OverheatSelfDamagePerSecond * overheatSeconds);
                    Phase = MokouPhase.Overheat;
                }
                else
                {
                    Phase = MokouPhase.Charged;
                }
            }
            else
            {
                Phase = MokouPhase.Charging;
            }
        }

        public MokouReleaseResult ReleaseCharge(bool perfectRelease = false)
        {
            if (!hasTerminal)
            {
                return MokouReleaseResult.Fail("missing_terminal");
            }

            if (Phase != MokouPhase.Charging && Phase != MokouPhase.Charged && Phase != MokouPhase.Overheat)
            {
                return MokouReleaseResult.Fail("invalid_phase:" + Phase);
            }

            if (Energy < terminalEnergyCost)
            {
                return MokouReleaseResult.Fail("energy");
            }

            Energy = Math.Max(0.0, Energy - terminalEnergyCost);

            double damage = terminalBaseDamage * ChargeDamageMultiplier() * TerminalDamageMultiplier();
            if (perfectRelease)
            {
                damage *= 1.0 + PerfectReleaseBonus;
            }

            if (terminalFlame > 0)
            {
                ApplyStatus("enemy", "flame", terminalFlame);
            }

            Phase = MokouPhase.Recovery;
            MokouReleaseResult result = new MokouReleaseResult(
                true, string.Empty, damage, terminalEnergyCost, terminalFlame, terminalTriggerCoefficient, perfectRelease);
            Charge = 0.0;
            return result;
        }

        public void SetEnergy(double value)
        {
            Energy = Clamp(value, 0.0, MaxEnergy);
        }

        public void AddImmortalGauge(double amount)
        {
            ImmortalGauge = Clamp(ImmortalGauge + amount, 0.0, MaxImmortalGauge);
        }

        // Apply a process-card modifier (Godot apply_process_modifier): dodge-retain takes the higher
        // value, charge-speed multiplies (floored at 0.05), terminal-damage bonus accumulates. Each field
        // is optional and only applied when supplied.
        public void ApplyProcessModifier(double? chargeDodgeRetain = null, double? chargeSpeedMultiplier = null, double? terminalDamageBonus = null)
        {
            if (chargeDodgeRetain.HasValue)
            {
                ChargeDodgeRetain = Math.Max(ChargeDodgeRetain, chargeDodgeRetain.Value);
            }

            if (chargeSpeedMultiplier.HasValue)
            {
                ChargeSpeedMultiplier *= Math.Max(0.05, chargeSpeedMultiplier.Value);
            }

            if (terminalDamageBonus.HasValue)
            {
                TerminalDamageBonus += terminalDamageBonus.Value;
            }
        }

        // Install a trigger card (Godot install_trigger appends the trigger).
        public void InstallTrigger(string triggerId)
        {
            if (!string.IsNullOrEmpty(triggerId))
            {
                triggers.Add(triggerId);
            }
        }

        // Charge-tier damage multiplier (Godot _charge_damage_multiplier).
        public double ChargeDamageMultiplier()
        {
            if (Charge >= 100.0)
            {
                return 1.0;
            }

            if (Charge >= 70.0)
            {
                return 0.85;
            }

            return Charge >= 35.0 ? 0.65 : 0.45;
        }

        // Overheat + bound bonus multiplier (Godot _terminal_damage_multiplier).
        public double TerminalDamageMultiplier()
        {
            double overheatBonus = Math.Max(0.0, Charge - 100.0) / 10.0 * OverheatDamagePer10Charge;
            return 1.0 + overheatBonus + TerminalDamageBonus;
        }

        public int GetStatus(string statusId)
        {
            return statusId != null && statuses.TryGetValue(statusId, out int value) ? value : 0;
        }

        private void ApplyStatus(string targetId, string statusId, int amount)
        {
            // The chain tracks only the enemy-facing statuses it applies (flame); target kept for fidelity.
            if (string.IsNullOrEmpty(statusId) || amount == 0)
            {
                return;
            }

            statuses[statusId] = GetStatus(statusId) + amount;
        }

        private void ApplySelfDamage(double amount)
        {
            if (amount <= 0.0)
            {
                return;
            }

            Hp = Math.Max(1.0, Hp - amount);
        }

        private static double Clamp(double value, double min, double max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
