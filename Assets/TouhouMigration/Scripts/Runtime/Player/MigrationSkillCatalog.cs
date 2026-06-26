using System.Collections.Generic;

namespace TouhouMigration.Runtime.Player
{
    public enum MigrationSkillType { Active, Passive, Ultimate, Bond }

    public enum MigrationSkillDamageType { Physical, Fire, Ice, Lightning, Poison, Holy }

    public enum MigrationSkillTargetType { Self, Single, Aoe, Line, Cone, Global }

    // A character skill's data (Godot SkillDataRecord).
    public sealed class MigrationSkillDefinition
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public MigrationSkillType SkillType = MigrationSkillType.Active;
        public MigrationSkillDamageType DamageType = MigrationSkillDamageType.Physical;
        public MigrationSkillTargetType TargetType = MigrationSkillTargetType.Self;
        public double BaseDamage;
        public double Cooldown;
        public double Range;
        public double Duration;
        public List<string> Effects = new List<string>();
    }

    // The character skill table + queries (Godot SkillDatabase): skills keyed by id, queryable by type and
    // by character (id prefix "<character>_"). Pure data. Reimu/Marisa/Sakuya are placeholder skills in the
    // source data, faithfully carried over.
    public sealed class MigrationSkillCatalog
    {
        private static MigrationSkillDefinition Skill(string id, string name, string description,
            MigrationSkillType type, MigrationSkillDamageType damage, MigrationSkillTargetType target,
            double baseDamage, double cooldown, double range, double duration, params string[] effects)
        {
            MigrationSkillDefinition skill = new MigrationSkillDefinition
            {
                Id = id,
                Name = name,
                Description = description,
                SkillType = type,
                DamageType = damage,
                TargetType = target,
                BaseDamage = baseDamage,
                Cooldown = cooldown,
                Range = range,
                Duration = duration,
            };
            if (effects != null)
            {
                skill.Effects.AddRange(effects);
            }

            return skill;
        }

        private static readonly MigrationSkillDefinition[] Table =
        {
            Skill("mokou_phoenix_dash", "凤凰之翼", "化身火焰凤凰向前冲刺，对路径上的敌人造成火焰伤害",
                MigrationSkillType.Active, MigrationSkillDamageType.Fire, MigrationSkillTargetType.Line,
                50.0, 8.0, 300.0, 0.0, "burn", "knockback"),
            Skill("mokou_immortal_flame", "不死之炎", "点燃不灭之火，持续恢复生命值并免疫控制效果",
                MigrationSkillType.Active, MigrationSkillDamageType.Fire, MigrationSkillTargetType.Self,
                0.0, 20.0, 0.0, 5.0),
            Skill("reimu_skill_1", "灵梦技能", "博丽灵梦的示例技能",
                MigrationSkillType.Active, MigrationSkillDamageType.Physical, MigrationSkillTargetType.Self, 0, 0, 0, 0),
            Skill("marisa_skill_1", "魔理沙技能", "雾雨魔理沙的示例技能",
                MigrationSkillType.Active, MigrationSkillDamageType.Physical, MigrationSkillTargetType.Self, 0, 0, 0, 0),
            Skill("sakuya_skill_1", "咲夜技能", "十六夜咲夜的示例技能",
                MigrationSkillType.Active, MigrationSkillDamageType.Physical, MigrationSkillTargetType.Self, 0, 0, 0, 0),
        };

        private readonly Dictionary<string, MigrationSkillDefinition> skills;

        public MigrationSkillCatalog()
        {
            skills = new Dictionary<string, MigrationSkillDefinition>();
            foreach (MigrationSkillDefinition skill in Table)
            {
                skills[skill.Id] = skill;
            }
        }

        public int Count => skills.Count;

        public MigrationSkillDefinition GetSkill(string skillId)
        {
            return skillId != null && skills.TryGetValue(skillId, out MigrationSkillDefinition skill) ? skill : null;
        }

        public IReadOnlyList<MigrationSkillDefinition> GetSkillsByType(MigrationSkillType type)
        {
            List<MigrationSkillDefinition> result = new List<MigrationSkillDefinition>();
            foreach (MigrationSkillDefinition skill in Table)
            {
                if (skill.SkillType == type)
                {
                    result.Add(skill);
                }
            }

            return result;
        }

        public IReadOnlyList<MigrationSkillDefinition> GetCharacterSkills(string characterId)
        {
            List<MigrationSkillDefinition> result = new List<MigrationSkillDefinition>();
            if (string.IsNullOrEmpty(characterId))
            {
                return result;
            }

            string prefix = characterId + "_";
            foreach (MigrationSkillDefinition skill in Table)
            {
                if (skill.Id.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    result.Add(skill);
                }
            }

            return result;
        }
    }
}
