namespace TouhouMigration.Runtime.Combat
{
    // The bond characters selectable as a combat ally (Godot GameConstants.CharacterId subset used by
    // CombatBondSystem.BOND_CONFIGS).
    public enum MigrationBondCharacter
    {
        None,
        Reimu,
        Marisa,
        Mokou,
        Sakuya,
        Yuma,
        Koishi
    }

    // The combat-ally bond (Godot CombatBondSystem): pick a non-player character as a bond for an active
    // skill on a cooldown plus a permanent passive modifier. UnityEngine-free; the active skill's VFX
    // (full-screen clear, master spark, …) and the passive's bullet hooks are scene work — this models the
    // bond selection, cooldown gate, and the passive-modifier values.
    public sealed class MigrationCombatBondSystem
    {
        private sealed class BondConfig
        {
            public string Name;
            public string SkillName;
            public double Cooldown;
        }

        private static readonly System.Collections.Generic.Dictionary<MigrationBondCharacter, BondConfig> Configs =
            new System.Collections.Generic.Dictionary<MigrationBondCharacter, BondConfig>
            {
                [MigrationBondCharacter.Reimu] = new BondConfig { Name = "灵梦", SkillName = "梦想封印", Cooldown = 30.0 },
                [MigrationBondCharacter.Marisa] = new BondConfig { Name = "魔理沙", SkillName = "极限火花", Cooldown = 25.0 },
                [MigrationBondCharacter.Mokou] = new BondConfig { Name = "妹红", SkillName = "凯风快晴", Cooldown = 20.0 },
                [MigrationBondCharacter.Sakuya] = new BondConfig { Name = "咲夜", SkillName = "杀人玩偶", Cooldown = 35.0 },
                [MigrationBondCharacter.Yuma] = new BondConfig { Name = "尤魔", SkillName = "暴食盛宴", Cooldown = 25.0 },
                [MigrationBondCharacter.Koishi] = new BondConfig { Name = "恋恋", SkillName = "无意识", Cooldown = 25.0 },
            };

        public MigrationBondCharacter CurrentBond { get; private set; } = MigrationBondCharacter.None;
        public double SkillCooldown { get; private set; }
        public double MaxCooldown { get; private set; }

        public bool HasBond => CurrentBond != MigrationBondCharacter.None;
        public bool IsSkillReady => HasBond && SkillCooldown <= 0.0;
        public string SkillName => Configs.TryGetValue(CurrentBond, out BondConfig c) ? c.SkillName : string.Empty;
        public string BondName => Configs.TryGetValue(CurrentBond, out BondConfig c) ? c.Name : string.Empty;

        // Select a bond character (Godot set_bond): adopts its cooldown. An unconfigured character (None) is
        // ignored.
        public void SetBond(MigrationBondCharacter character)
        {
            if (!Configs.TryGetValue(character, out BondConfig config))
            {
                return;
            }

            CurrentBond = character;
            MaxCooldown = config.Cooldown;
        }

        // Count the skill cooldown down (Godot _process), clamped at zero.
        public void TickCooldown(double deltaSeconds)
        {
            SkillCooldown = System.Math.Max(0.0, SkillCooldown - System.Math.Max(0.0, deltaSeconds));
        }

        // Activate the bond skill (Godot activate_bond_skill): blocked while on cooldown or without a bond;
        // on success it starts the full cooldown. (The skill's effect is scene work.) Returns whether it
        // activated.
        public bool TryActivateSkill()
        {
            if (!HasBond || SkillCooldown > 0.0)
            {
                return false;
            }

            SkillCooldown = MaxCooldown;
            return true;
        }

        // The permanent passive modifier for a given type (Godot get_passive_modifier), or 0 when the
        // current bond has no such modifier.
        public double GetPassiveModifier(string modifierType)
        {
            switch (CurrentBond)
            {
                case MigrationBondCharacter.Reimu:
                    if (modifierType == "bounce_bonus") return 1.0;
                    if (modifierType == "homing_after_bounce") return 0.1;
                    break;
                case MigrationBondCharacter.Marisa:
                    if (modifierType == "size_bonus") return 1.3;
                    if (modifierType == "knockback_bonus") return 1.5;
                    break;
                case MigrationBondCharacter.Mokou:
                    if (modifierType == "bullet_revive") return 1.0;
                    break;
                case MigrationBondCharacter.Sakuya:
                    if (modifierType == "delay_time") return 0.3;
                    break;
                case MigrationBondCharacter.Yuma:
                    if (modifierType == "gravity_pull") return 50.0;
                    break;
                case MigrationBondCharacter.Koishi:
                    if (modifierType == "phase_chance") return 0.2;
                    break;
            }

            return 0.0;
        }
    }
}
