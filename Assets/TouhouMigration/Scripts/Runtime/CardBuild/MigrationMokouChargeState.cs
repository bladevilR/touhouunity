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
        private const int MaxEmber = 10;
        private const int MaxAsh = 6;
        private const double DefaultReviveHp = 35.0;

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
        public int Ember { get; private set; }
        public int Ash { get; private set; }
        public int RevivesUsed { get; private set; }
        public int MaxRevives { get; private set; } = 1;
        private double ashDamageRemainder;

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

        // Register a dodge (Godot register_dodge): while charging, retain charge * charge_dodge_retain (the
        // rest is lost); a perfect dodge also grants +10 energy and +1 ember. Returns the retained charge.
        public double RegisterDodge(bool perfect)
        {
            double retainedCharge = Charge;
            if (Phase == MokouPhase.Charging || Phase == MokouPhase.Charged || Phase == MokouPhase.Overheat)
            {
                retainedCharge = Charge * Clamp(ChargeDodgeRetain, 0.0, 1.0);
                Charge = retainedCharge;
                Phase = Charge > 0.0 ? MokouPhase.Charging : MokouPhase.Idle;
            }

            if (perfect)
            {
                Energy = Math.Min(MaxEnergy, Energy + 10.0);
                Ember = Math.Min(MaxEmber, Ember + 1);
            }

            return retainedCharge;
        }

        // Ignite the enemy's flame stacks (Godot ignite): consume up to 10 flame for 2.5 damage each.
        // Returns the damage dealt.
        public double Ignite()
        {
            int consumed = Math.Min(10, GetStatus("flame"));
            if (consumed <= 0)
            {
                return 0.0;
            }

            ApplyStatus("enemy", "flame", -consumed);
            return consumed * 2.5;
        }

        // Take damage (Godot take_damage): lost HP accrues ash (1 per 20 cumulative), and a lethal hit with
        // a full immortal gauge revives to the revive HP (spending the gauge). Returns whether it revived.
        public bool TakeDamage(double amount)
        {
            double damage = Math.Max(0.0, amount);
            if (damage <= 0.0)
            {
                return false;
            }

            double hpBefore = Hp;
            Hp -= damage;
            double hpLost = Math.Min(hpBefore, damage);
            if (hpLost > 0.0)
            {
                ashDamageRemainder += hpLost;
                int ashGain = (int)Math.Floor(ashDamageRemainder / 20.0);
                if (ashGain > 0)
                {
                    Ash = Math.Min(MaxAsh, Ash + ashGain);
                    ashDamageRemainder -= ashGain * 20.0;
                }
            }

            bool revived = false;
            if (Hp <= 0.0 && ImmortalGauge >= MaxImmortalGauge && RevivesUsed < MaxRevives)
            {
                revived = true;
                RevivesUsed++;
                ImmortalGauge = 0.0;
                Hp = DefaultReviveHp;
            }
            else
            {
                Hp = Math.Max(0.0, Hp);
            }

            return revived;
        }

        // Resolve an on-hit trigger (Godot resolve_hit_trigger): add energy_gain * coefficient to energy
        // (capped). Returns the energy gained.
        public double ResolveHitTrigger(double coefficient, double energyGain)
        {
            double gain = energyGain * coefficient;
            if (gain > 0.0)
            {
                Energy = Math.Min(MaxEnergy, Energy + gain);
            }

            return gain;
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
