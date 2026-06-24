using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Cooking
{
    public sealed class CookingBuffService
    {
        private const int SlotCount = 3;
        private const int SoftCap = 12;
        private const int HardCap = 16;
        private const float DrinkBaseDuration = 300f;
        private const float DrinkAttackUp = 0.10f;
        private const float DrinkSpeedUp = 0.08f;
        private const float DrinkRiskTakenUp = 0.10f;
        private const float DrinkCdReduction = 0.15f;
        private const float SpecialFlameFistBonus = 0.25f;
        private const float SpecialComboDamageBonus = 0.30f;
        private const float SpecialBerserkBurnBonus = 0.80f;
        private const float SpecialBreakArmorPenetration = 0.20f;
        private const float SpecialKillHealSmallPercent = 3f;
        private const float SpecialKillHealLargePercent = 8f;
        private const float SpecialHighHpGuardBonus = 0.15f;
        private const float SpecialDrinkRegenPerSecond = 3f;
        private const float LowHpRegenPercentPerSecond = 2f;
        private const float PhoenixRegenPercent = 2f;
        private const float FullHpRegenPerSecond = 2f;

        private static readonly string[] StatKeys = { "atk", "def", "spd", "spi" };
        private static readonly int[] Thresholds = { 6, 10, 15 };

        private readonly CookingDatabase cookingDatabase;
        private readonly CookingBuffSlotSnapshot[] buffSlots = new CookingBuffSlotSnapshot[SlotCount];
        private readonly Dictionary<string, int> totalStats = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> unlockedThresholds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> activeSpecialEffects = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> drinkEffectsState = new HashSet<string>(StringComparer.Ordinal);
        private CookingBuffDrinkSnapshot activeDrink = new CookingBuffDrinkSnapshot();
        private float playerHpRatio = 1f;

        public CookingBuffService(CookingDatabase cookingDatabase)
        {
            this.cookingDatabase = cookingDatabase;
            InitializeSlots();
            RecalculateTotalStats();
        }

        public event Action<IReadOnlyDictionary<string, int>> StatsChanged;
        public event Action<string, int> ThresholdUnlocked;
        public event Action<string, int> BuffApplied;

        public bool ConsumeDish(string dishId)
        {
            return ConsumeDish(dishId, 0);
        }

        public bool ConsumeDish(string dishId, int quality)
        {
            string normalizedDishId = NormalizeId(dishId);
            if (string.IsNullOrEmpty(normalizedDishId) ||
                cookingDatabase == null ||
                !cookingDatabase.HasDishCombatProfile(normalizedDishId))
            {
                return false;
            }

            if (cookingDatabase.IsDishDrink(normalizedDishId))
            {
                return ConsumeDrink(normalizedDishId);
            }

            CookingDishProfile profile = cookingDatabase.GetDishProfile(normalizedDishId);
            int targetSlot = ChooseSlotForProfile(profile);
            if (targetSlot < 0 || targetSlot >= SlotCount)
            {
                return false;
            }

            float qualityMultiplier = cookingDatabase.GetQualityMultiplier(quality);
            CookingBuffSlotSnapshot slot = new CookingBuffSlotSnapshot
            {
                slot_index = targetSlot,
                dish_id = normalizedDishId,
                main_stat = NormalizeId(profile.MainStat),
                quality = Math.Max(0, quality),
                duration = profile.BuffDuration > 0f ? profile.BuffDuration : 600f,
                remaining = profile.BuffDuration > 0f ? profile.BuffDuration : 600f,
                special_effects = CopyStrings(profile.SpecialEffects)
            };

            foreach (string statKey in StatKeys)
            {
                int baseValue = profile.Stats.TryGetValue(statKey, out int value) ? value : 0;
                slot.SetStat(statKey, (int)Math.Floor(baseValue * qualityMultiplier));
            }

            buffSlots[targetSlot] = slot;
            RecalculateTotalStats();
            BuffApplied?.Invoke(normalizedDishId, targetSlot);
            return true;
        }

        public bool ConsumeDrink(string dishId)
        {
            string normalizedDishId = NormalizeId(dishId);
            if (string.IsNullOrEmpty(normalizedDishId) ||
                cookingDatabase == null ||
                !cookingDatabase.HasDishCombatProfile(normalizedDishId) ||
                !cookingDatabase.IsDishDrink(normalizedDishId))
            {
                return false;
            }

            CookingDishProfile profile = cookingDatabase.GetDishProfile(normalizedDishId);
            float duration = profile.BuffDuration > 0f ? profile.BuffDuration : DrinkBaseDuration;
            activeDrink = new CookingBuffDrinkSnapshot
            {
                dish_id = normalizedDishId,
                duration = duration,
                remaining = duration,
                drink_effects = MergeEffects(profile.DrinkEffects, profile.SpecialEffects)
            };

            foreach (string statKey in StatKeys)
            {
                activeDrink.SetStat(statKey, profile.Stats.TryGetValue(statKey, out int value) ? value : 0);
            }

            RecalculateTotalStats();
            return true;
        }

        public void ClearAllBuffs()
        {
            InitializeSlots();
            activeDrink = new CookingBuffDrinkSnapshot();
            unlockedThresholds.Clear();
            activeSpecialEffects.Clear();
            drinkEffectsState.Clear();
            RecalculateTotalStats();
        }

        public void Tick(float delta)
        {
            if (delta <= 0f)
            {
                return;
            }

            bool changed = TickBuffs(delta);
            changed |= TickDrink(delta);
            if (changed)
            {
                RecalculateTotalStats();
            }
        }

        public IReadOnlyDictionary<string, int> GetTotalStats()
        {
            return new Dictionary<string, int>(totalStats, StringComparer.Ordinal);
        }

        public IReadOnlyList<CookingBuffSlotSnapshot> GetBuffSlots()
        {
            List<CookingBuffSlotSnapshot> result = new List<CookingBuffSlotSnapshot>();
            foreach (CookingBuffSlotSnapshot slot in buffSlots)
            {
                result.Add(CopySlot(slot));
            }

            return result;
        }

        public CookingBuffDrinkSnapshot GetActiveDrink()
        {
            return CopyDrink(activeDrink);
        }

        public int GetStatValue(string statKey)
        {
            return totalStats.TryGetValue(NormalizeId(statKey), out int value) ? value : 0;
        }

        public float GetDamageMultiplier()
        {
            int atk = GetStatValue("atk");
            float multiplier;
            if (atk <= 0)
            {
                multiplier = HasDrinkEffect("attack_up_10") ? 1f + DrinkAttackUp : 1f;
                return multiplier;
            }

            multiplier = atk <= 12
                ? 1f + atk * 0.04f
                : 1.48f + (atk - 12) * 0.02f;

            if (IsComboActiveAtkSpi())
            {
                multiplier *= 1.10f;
            }

            if (HasSpecialEffect("flame_fist_bonus"))
            {
                multiplier *= 1f + SpecialFlameFistBonus;
            }

            if (HasSpecialEffect("combo_damage_up"))
            {
                multiplier *= 1f + SpecialComboDamageBonus;
            }

            if (HasDrinkEffect("attack_up_10"))
            {
                multiplier *= 1f + DrinkAttackUp;
            }

            if (HasSpecialEffect("berserk_burn") && playerHpRatio > 0f && playerHpRatio < 0.30f)
            {
                multiplier *= 1f + SpecialBerserkBurnBonus;
            }

            return multiplier;
        }

        public float GetDamageReduction()
        {
            int def = GetStatValue("def");
            float reduction;
            if (def <= 0)
            {
                reduction = HasSpecialEffect("high_hp_guard") && playerHpRatio >= 0.70f
                    ? SpecialHighHpGuardBonus
                    : 0f;
                return Clamp(reduction, 0f, 0.9f);
            }

            reduction = def <= 12
                ? def * 0.03f
                : 0.36f + (def - 12) * 0.015f;

            if (HasSpecialEffect("high_hp_guard") && playerHpRatio >= 0.70f)
            {
                reduction += SpecialHighHpGuardBonus;
            }

            if (HasDrinkEffect("risk_taken_up_10"))
            {
                reduction -= DrinkRiskTakenUp;
            }

            return Clamp(reduction, 0f, 0.9f);
        }

        public float GetSpeedMultiplier()
        {
            int spd = GetStatValue("spd");
            if (spd <= 0)
            {
                return HasDrinkEffect("speed_up_8") ? 1f + DrinkSpeedUp : 1f;
            }

            float multiplier = spd <= 12
                ? 1f + spd * 0.025f
                : 1.30f + (spd - 12) * 0.0125f;

            if (HasDrinkEffect("speed_up_8"))
            {
                multiplier *= 1f + DrinkSpeedUp;
            }

            return multiplier;
        }

        public float GetDashCooldownOffset()
        {
            int spd = GetStatValue("spd");
            return spd <= 0 ? 0f : (spd / 3) * 0.1f;
        }

        public float GetSpiritChargeMultiplier()
        {
            int spi = GetStatValue("spi");
            if (spi <= 0)
            {
                return HasSpecialEffect("spirit_charge_bonus") ? 1.10f : 1f;
            }

            float multiplier = spi <= 12
                ? 1f + spi * 0.06f
                : 1.72f + (spi - 12) * 0.04f;

            if (HasSpecialEffect("spirit_charge_bonus"))
            {
                multiplier *= 1.10f;
            }

            if (HasSpecialEffect("skill_cd_10"))
            {
                multiplier *= 1.10f;
            }

            if (HasSpecialEffect("skill_cd_15"))
            {
                multiplier *= 1.15f;
            }

            if (HasDrinkEffect("drink_focus"))
            {
                multiplier *= 1f + DrinkCdReduction;
            }

            return multiplier;
        }

        public bool IsThresholdActive(string statKey, int threshold)
        {
            return unlockedThresholds.Contains(BuildThresholdToken(statKey, threshold));
        }

        public bool IsComboActiveAtkSpd()
        {
            return GetStatValue("atk") >= 6 && GetStatValue("spd") >= 6;
        }

        public bool IsComboActiveAtkSpi()
        {
            return GetStatValue("atk") >= 6 && GetStatValue("spi") >= 6;
        }

        public bool IsComboActiveDefSpi()
        {
            return GetStatValue("def") >= 6 && GetStatValue("spi") >= 6;
        }

        public bool IsComboActiveSpdSpi()
        {
            return GetStatValue("spd") >= 6 && GetStatValue("spi") >= 6;
        }

        public bool IsComboActiveAtkDef()
        {
            return GetStatValue("atk") >= 10 && GetStatValue("def") >= 10;
        }

        public bool HasSpecialEffect(string effectId)
        {
            return activeSpecialEffects.Contains(NormalizeId(effectId));
        }

        public bool HasDrinkEffect(string effectId)
        {
            string normalizedEffect = NormalizeId(effectId);
            return drinkEffectsState.Contains(normalizedEffect) || activeSpecialEffects.Contains(normalizedEffect);
        }

        public bool HasActiveDrink()
        {
            return activeDrink != null && !activeDrink.IsEmpty;
        }

        public float GetHeavyArmorPenetration()
        {
            return HasSpecialEffect("heavy_armor_break") ? SpecialBreakArmorPenetration : 0f;
        }

        public float GetEnemyKillHealPercent()
        {
            float percent = 0f;
            if (HasSpecialEffect("kill_heal_3_percent"))
            {
                percent += SpecialKillHealSmallPercent;
            }

            if (HasSpecialEffect("kill_heal_8_percent"))
            {
                percent += SpecialKillHealLargePercent;
            }

            return percent;
        }

        public float GetRegenerationPerSecond(float maxHp, float currentHp)
        {
            if (maxHp <= 0f || currentHp <= 0f)
            {
                return 0f;
            }

            float regen = 0f;
            if (HasSpecialEffect("regen_1"))
            {
                regen += 1f;
            }

            if (IsThresholdActive("def", 15))
            {
                regen += FullHpRegenPerSecond;
            }

            if (IsComboActiveDefSpi() && currentHp <= maxHp * 0.2f)
            {
                regen += maxHp * (LowHpRegenPercentPerSecond / 100f);
            }

            if (HasSpecialEffect("phoenix_regen") && currentHp <= maxHp * 0.2f)
            {
                regen += maxHp * (PhoenixRegenPercent / 100f);
            }

            if (HasSpecialEffect("drink_regen_3"))
            {
                regen += SpecialDrinkRegenPerSecond;
            }

            return regen;
        }

        public void SetPlayerHpRatio(float hpRatio)
        {
            playerHpRatio = Clamp(hpRatio, 0f, 1f);
        }

        public CookingBuffRuntimeSnapshot CreateSnapshot()
        {
            CookingBuffRuntimeSnapshot snapshot = new CookingBuffRuntimeSnapshot
            {
                active_drink = CopyDrink(activeDrink)
            };

            foreach (CookingBuffSlotSnapshot slot in buffSlots)
            {
                snapshot.buff_slots.Add(CopySlot(slot));
            }

            foreach (string statKey in StatKeys)
            {
                snapshot.SetTotalStat(statKey, GetStatValue(statKey));
            }

            foreach (string token in unlockedThresholds)
            {
                snapshot.AddUnlockedThreshold(token);
            }

            return snapshot;
        }

        public void LoadSnapshot(CookingBuffRuntimeSnapshot snapshot)
        {
            InitializeSlots();
            activeDrink = new CookingBuffDrinkSnapshot();
            unlockedThresholds.Clear();

            if (snapshot != null)
            {
                int limit = Math.Min(SlotCount, snapshot.buff_slots?.Count ?? 0);
                for (int index = 0; index < limit; index++)
                {
                    CookingBuffSlotSnapshot slot = snapshot.buff_slots[index];
                    buffSlots[index] = slot == null || slot.IsEmpty ? CreateEmptySlot(index) : CopySlot(slot, index);
                }

                activeDrink = CopyDrink(snapshot.active_drink);
                if (snapshot.unlocked_thresholds != null)
                {
                    foreach (string token in snapshot.unlocked_thresholds)
                    {
                        string normalizedToken = NormalizeId(token);
                        if (!string.IsNullOrEmpty(normalizedToken))
                        {
                            unlockedThresholds.Add(normalizedToken);
                        }
                    }
                }
            }

            RecalculateTotalStats();
        }

        private void InitializeSlots()
        {
            for (int index = 0; index < SlotCount; index++)
            {
                buffSlots[index] = CreateEmptySlot(index);
            }
        }

        private static CookingBuffSlotSnapshot CreateEmptySlot(int index)
        {
            return new CookingBuffSlotSnapshot { slot_index = index };
        }

        private int ChooseSlotForProfile(CookingDishProfile profile)
        {
            string mainStat = NormalizeId(profile?.MainStat);
            for (int index = 0; index < SlotCount; index++)
            {
                CookingBuffSlotSnapshot slot = buffSlots[index];
                if (!slot.IsEmpty && slot.MainStat == mainStat)
                {
                    return index;
                }
            }

            for (int index = 0; index < SlotCount; index++)
            {
                if (buffSlots[index].IsEmpty)
                {
                    return index;
                }
            }

            int earliestIndex = 0;
            float minRemaining = float.MaxValue;
            for (int index = 0; index < SlotCount; index++)
            {
                if (buffSlots[index].remaining < minRemaining)
                {
                    minRemaining = buffSlots[index].remaining;
                    earliestIndex = index;
                }
            }

            return earliestIndex;
        }

        private bool TickBuffs(float delta)
        {
            bool changed = false;
            for (int index = 0; index < SlotCount; index++)
            {
                CookingBuffSlotSnapshot slot = buffSlots[index];
                if (slot.IsEmpty)
                {
                    continue;
                }

                float before = slot.remaining;
                slot.remaining = Math.Max(0f, before - delta);
                if (slot.remaining <= 0.001f && before > 0.001f)
                {
                    buffSlots[index] = CreateEmptySlot(index);
                    changed = true;
                }
            }

            return changed;
        }

        private bool TickDrink(float delta)
        {
            if (activeDrink == null || activeDrink.IsEmpty)
            {
                return false;
            }

            float before = activeDrink.remaining;
            activeDrink.remaining = Math.Max(0f, before - delta);
            if (activeDrink.remaining <= 0.001f && before > 0.001f)
            {
                activeDrink = new CookingBuffDrinkSnapshot();
                return true;
            }

            return false;
        }

        private void RecalculateTotalStats()
        {
            totalStats.Clear();
            foreach (string statKey in StatKeys)
            {
                totalStats[statKey] = 0;
            }

            foreach (CookingBuffSlotSnapshot slot in buffSlots)
            {
                foreach (string statKey in StatKeys)
                {
                    totalStats[statKey] += slot.GetStat(statKey);
                }
            }

            if (activeDrink != null)
            {
                foreach (string statKey in StatKeys)
                {
                    totalStats[statKey] += activeDrink.GetStat(statKey);
                }
            }

            foreach (string statKey in StatKeys)
            {
                totalStats[statKey] = ApplyCaps(totalStats[statKey]);
            }

            RebuildEffectStates();
            CheckThresholdUnlocks();
            StatsChanged?.Invoke(GetTotalStats());
        }

        private void RebuildEffectStates()
        {
            activeSpecialEffects.Clear();
            drinkEffectsState.Clear();

            foreach (CookingBuffSlotSnapshot slot in buffSlots)
            {
                if (slot.special_effects == null)
                {
                    continue;
                }

                foreach (string effectId in slot.special_effects)
                {
                    string normalizedEffect = NormalizeId(effectId);
                    if (!string.IsNullOrEmpty(normalizedEffect))
                    {
                        activeSpecialEffects.Add(normalizedEffect);
                    }
                }
            }

            if (activeDrink?.drink_effects == null)
            {
                return;
            }

            foreach (string effectId in activeDrink.drink_effects)
            {
                string normalizedEffect = NormalizeId(effectId);
                if (!string.IsNullOrEmpty(normalizedEffect))
                {
                    drinkEffectsState.Add(normalizedEffect);
                }
            }
        }

        private void CheckThresholdUnlocks()
        {
            foreach (string statKey in StatKeys)
            {
                foreach (int threshold in Thresholds)
                {
                    string token = BuildThresholdToken(statKey, threshold);
                    if (GetStatValue(statKey) >= threshold && unlockedThresholds.Add(token))
                    {
                        ThresholdUnlocked?.Invoke(statKey, threshold);
                    }
                }
            }
        }

        private static int ApplyCaps(int value)
        {
            if (value <= SoftCap)
            {
                return Math.Min(value, HardCap);
            }

            int overSoft = value - SoftCap;
            int reducedGain = (int)Math.Floor(overSoft / 2f);
            return Math.Min(SoftCap + reducedGain, HardCap);
        }

        private static CookingBuffSlotSnapshot CopySlot(CookingBuffSlotSnapshot slot)
        {
            return CopySlot(slot, slot?.slot_index ?? 0);
        }

        private static CookingBuffSlotSnapshot CopySlot(CookingBuffSlotSnapshot slot, int index)
        {
            if (slot == null || slot.IsEmpty)
            {
                return CreateEmptySlot(index);
            }

            return new CookingBuffSlotSnapshot
            {
                slot_index = index,
                dish_id = NormalizeId(slot.dish_id),
                main_stat = NormalizeId(slot.main_stat),
                quality = Math.Max(0, slot.quality),
                atk = slot.atk,
                def = slot.def,
                spd = slot.spd,
                spi = slot.spi,
                duration = Math.Max(0f, slot.duration),
                remaining = Math.Max(0f, slot.remaining),
                special_effects = CopyStrings(slot.special_effects)
            };
        }

        private static CookingBuffDrinkSnapshot CopyDrink(CookingBuffDrinkSnapshot drink)
        {
            if (drink == null || drink.IsEmpty)
            {
                return new CookingBuffDrinkSnapshot();
            }

            return new CookingBuffDrinkSnapshot
            {
                dish_id = NormalizeId(drink.dish_id),
                atk = drink.atk,
                def = drink.def,
                spd = drink.spd,
                spi = drink.spi,
                duration = Math.Max(0f, drink.duration),
                remaining = Math.Max(0f, drink.remaining),
                drink_effects = CopyStrings(drink.drink_effects)
            };
        }

        private static List<string> MergeEffects(IEnumerable<string> first, IEnumerable<string> second)
        {
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            AppendEffects(first, result, seen);
            AppendEffects(second, result, seen);
            return result;
        }

        private static void AppendEffects(IEnumerable<string> source, List<string> target, HashSet<string> seen)
        {
            if (source == null)
            {
                return;
            }

            foreach (string effectId in source)
            {
                string normalizedEffect = NormalizeId(effectId);
                if (!string.IsNullOrEmpty(normalizedEffect) && seen.Add(normalizedEffect))
                {
                    target.Add(normalizedEffect);
                }
            }
        }

        private static List<string> CopyStrings(IEnumerable<string> source)
        {
            List<string> result = new List<string>();
            if (source == null)
            {
                return result;
            }

            foreach (string value in source)
            {
                string normalizedValue = NormalizeId(value);
                if (!string.IsNullOrEmpty(normalizedValue))
                {
                    result.Add(normalizedValue);
                }
            }

            return result;
        }

        private static string BuildThresholdToken(string statKey, int threshold)
        {
            return $"{NormalizeId(statKey)}_{threshold}";
        }

        private static float Clamp(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}
