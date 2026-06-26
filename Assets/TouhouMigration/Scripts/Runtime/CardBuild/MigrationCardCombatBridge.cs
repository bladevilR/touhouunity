using System.Collections.Generic;

namespace TouhouMigration.Runtime.CardBuild
{
    // A bullet modifier extracted from a card's modify_bullet effect block.
    public sealed class MigrationBulletModifier
    {
        public string Family = string.Empty;
        public string Modifier = string.Empty;
    }

    // A combat attack's CardBuild-relevant fields (Godot attack_data subset).
    public sealed class MigrationCardAttack
    {
        public double Damage = 0.0;
        public double DamageMultiplier = 1.0;
        public int ProjectileCountBonus;
        public string StatusOnHit;
        public string BulletPattern;
        public bool Homing;
        public bool DelayedRelease;
        public double SpeedMultiplier = 1.0;
        public HashSet<string> Tags = new HashSet<string>();
    }

    // Bridges card effects to combat (Godot CardBuildCombatBridge): pull modify_bullet modifiers out of a
    // card's effect blocks and fold them into a base attack (scorch trail's +15% burn, split's +2
    // projectiles, pause-resume's delayed 0.75x, sealed homing). UnityEngine-free + unit-testable.
    public sealed class MigrationCardCombatBridge
    {
        public IReadOnlyList<MigrationBulletModifier> ExtractBulletModifiers(IEnumerable<MigrationCardEffectBlock> effectBlocks)
        {
            List<MigrationBulletModifier> modifiers = new List<MigrationBulletModifier>();
            if (effectBlocks == null)
            {
                return modifiers;
            }

            foreach (MigrationCardEffectBlock block in effectBlocks)
            {
                if (block != null && block.Type == "modify_bullet")
                {
                    modifiers.Add(new MigrationBulletModifier
                    {
                        Family = block.Family ?? string.Empty,
                        Modifier = block.Modifier ?? string.Empty,
                    });
                }
            }

            return modifiers;
        }

        public MigrationCardAttack ApplyBulletModifiers(MigrationCardAttack baseAttack, IEnumerable<MigrationBulletModifier> modifiers)
        {
            MigrationCardAttack attack = baseAttack ?? new MigrationCardAttack();
            double damageMultiplier = 1.0;

            if (modifiers != null)
            {
                foreach (MigrationBulletModifier modifier in modifiers)
                {
                    if (modifier == null || (string.IsNullOrEmpty(modifier.Family) && string.IsNullOrEmpty(modifier.Modifier)))
                    {
                        continue;
                    }

                    AddTag(attack, modifier.Family);
                    AddTag(attack, modifier.Modifier);

                    switch (modifier.Modifier)
                    {
                        case "scorch_trail":
                            damageMultiplier *= 1.15;
                            AddTag(attack, "burn");
                            AddTag(attack, "scorch");
                            attack.StatusOnHit = "burn";
                            break;
                        case "split":
                            attack.ProjectileCountBonus += 2;
                            AddTag(attack, "split");
                            AddTag(attack, "multihit");
                            attack.BulletPattern = "split";
                            break;
                        case "pause_resume":
                            AddTag(attack, "delayed");
                            AddTag(attack, "time");
                            attack.DelayedRelease = true;
                            attack.SpeedMultiplier *= 0.75;
                            break;
                        case "sealed_homing":
                            AddTag(attack, "seal");
                            AddTag(attack, "homing");
                            attack.Homing = true;
                            attack.StatusOnHit = "seal";
                            break;
                    }
                }
            }

            attack.Damage *= damageMultiplier;
            attack.DamageMultiplier = damageMultiplier;
            return attack;
        }

        private static void AddTag(MigrationCardAttack attack, string tag)
        {
            if (!string.IsNullOrEmpty(tag))
            {
                attack.Tags.Add(tag);
            }
        }
    }
}
