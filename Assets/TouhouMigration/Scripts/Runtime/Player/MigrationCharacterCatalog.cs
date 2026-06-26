namespace TouhouMigration.Runtime.Player
{
    // A playable character's base config (Godot CharacterData.Character + CharacterStats, gameplay fields).
    public sealed class MigrationCharacterDefinition
    {
        public MigrationCharacterDefinition(string id, string name, string defaultWeapon, double maxHp, double speed,
            double might, double area, double cooldown, double pickupRange, double luck, double armor, int revivals)
        {
            Id = id;
            Name = name;
            DefaultWeapon = defaultWeapon;
            MaxHp = maxHp;
            Speed = speed;
            Might = might;
            Area = area;
            Cooldown = cooldown;
            PickupRange = pickupRange;
            Luck = luck;
            Armor = armor;
            Revivals = revivals;
        }

        public string Id { get; }
        public string Name { get; }
        public string DefaultWeapon { get; }
        public double MaxHp { get; }
        public double Speed { get; }
        public double Might { get; }
        public double Area { get; }
        public double Cooldown { get; }
        public double PickupRange { get; }
        public double Luck { get; }
        public double Armor { get; }
        public int Revivals { get; }
    }

    // The playable-character base-stat table (Godot CharacterData.CHARACTERS): six characters keyed by id,
    // with their default weapon + base stats. Pure data; backs character select + the combat stat scaling.
    public sealed class MigrationCharacterCatalog
    {
        private static readonly MigrationCharacterDefinition[] Table =
        {
            //                                id        name        weapon              hp    spd  mgt  area  cd    pick  luck arm rev
            new MigrationCharacterDefinition("reimu",  "博丽灵梦", "homing_amulet",    90,  3.0, 1.0, 1.0, 1.0, 130, 1.0, 0, 0),
            new MigrationCharacterDefinition("mokou",  "藤原妹红", "mokou_kick_light", 120, 3.2, 1.0, 1.0, 1.0, 100, 1.0, 0, 1),
            new MigrationCharacterDefinition("marisa", "雾雨魔理沙", "star_dust",        100, 4.0, 1.0, 1.0, 0.9, 150, 1.0, 0, 0),
            new MigrationCharacterDefinition("sakuya", "十六夜咲夜", "knives",           100, 3.3, 1.0, 1.0, 0.8, 120, 1.0, 0, 0),
            new MigrationCharacterDefinition("yuma",   "饕餮尤魔", "spoon",            150, 2.8, 1.0, 1.0, 1.0, 100, 1.0, 3, 0),
            new MigrationCharacterDefinition("koishi", "古明地恋", "mines",            80,  3.5, 1.0, 1.2, 1.0, 100, 1.5, 0, 0),
        };

        private readonly System.Collections.Generic.Dictionary<string, MigrationCharacterDefinition> characters;

        public MigrationCharacterCatalog()
        {
            characters = new System.Collections.Generic.Dictionary<string, MigrationCharacterDefinition>();
            foreach (MigrationCharacterDefinition character in Table)
            {
                characters[character.Id] = character;
            }
        }

        public int Count => characters.Count;

        public MigrationCharacterDefinition GetCharacter(string characterId)
        {
            return characterId != null && characters.TryGetValue(characterId, out MigrationCharacterDefinition character)
                ? character
                : null;
        }
    }
}
