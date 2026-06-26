using System.Collections.Generic;

namespace TouhouMigration.Runtime.Weapons
{
    // A weapon's gameplay config (Godot WeaponData.WeaponConfig, pure-data fields): max level, base
    // cooldown, base damage. The firing-specific fields (projectile count/speed/homing/lifetime) are scene
    // work and are not ported here.
    public sealed class MigrationWeaponDefinition
    {
        public MigrationWeaponDefinition(string id, int maxLevel, double cooldownMax, double baseDamage)
        {
            Id = id ?? string.Empty;
            MaxLevel = maxLevel;
            CooldownMax = cooldownMax;
            BaseDamage = baseDamage;
        }

        public string Id { get; }
        public int MaxLevel { get; }
        public double CooldownMax { get; }
        public double BaseDamage { get; }
    }

    // The weapon gameplay-config table (Godot WeaponData.WEAPONS, the 17 active weapons; the two commented
    // configs — phoenix_wings / shanghai_doll — are excluded). Pure data; feeds MigrationWeaponInventory's
    // max-level cap (MaxLevelOf) and the fusion / firing layers.
    public sealed class MigrationWeaponCatalog
    {
        private static readonly MigrationWeaponDefinition[] Table =
        {
            new MigrationWeaponDefinition("homing_amulet", 8, 1.0, 15.0),
            new MigrationWeaponDefinition("star_dust", 8, 0.5, 12.0),
            new MigrationWeaponDefinition("mokou_kick_heavy", 20, 0.8, 80.0),
            new MigrationWeaponDefinition("mokou_kick_light", 20, 0.2, 15.0),
            new MigrationWeaponDefinition("knives", 8, 0.6, 10.0),
            new MigrationWeaponDefinition("spoon", 8, 1.0, 40.0),
            new MigrationWeaponDefinition("mines", 8, 2.0, 60.0),
            new MigrationWeaponDefinition("molotov", 8, 2.5, 20.0),
            new MigrationWeaponDefinition("laser", 8, 4.0, 5.0),
            new MigrationWeaponDefinition("yin_yang_orb", 8, 1.5, 35.0),
            new MigrationWeaponDefinition("tengu_fan", 8, 3.0, 0.0),
            new MigrationWeaponDefinition("haniwa", 8, 10.0, 10.0),
            new MigrationWeaponDefinition("boundary", 8, 8.0, 3.0),
            new MigrationWeaponDefinition("dream_seal", 8, 3.0, 80.0),
            new MigrationWeaponDefinition("master_spark", 8, 10.0, 20.0),
            new MigrationWeaponDefinition("phoenix_rebirth", 8, 60.0, 500.0),
            new MigrationWeaponDefinition("sakuyas_world", 8, 8.0, 30.0),
        };

        private readonly Dictionary<string, MigrationWeaponDefinition> weapons;

        public MigrationWeaponCatalog()
        {
            weapons = new Dictionary<string, MigrationWeaponDefinition>();
            foreach (MigrationWeaponDefinition weapon in Table)
            {
                weapons[weapon.Id] = weapon;
            }
        }

        public int Count => weapons.Count;

        public MigrationWeaponDefinition GetWeapon(string weaponId)
        {
            return weaponId != null && weapons.TryGetValue(weaponId, out MigrationWeaponDefinition weapon) ? weapon : null;
        }

        // The weapon's max level, or 0 for an unknown id (so it can directly feed MigrationWeaponInventory).
        public int MaxLevelOf(string weaponId)
        {
            MigrationWeaponDefinition weapon = GetWeapon(weaponId);
            return weapon != null ? weapon.MaxLevel : 0;
        }
    }
}
