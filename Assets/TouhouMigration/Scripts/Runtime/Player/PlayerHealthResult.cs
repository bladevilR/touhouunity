namespace TouhouMigration.Runtime.Player
{
    public sealed class PlayerHealthResult
    {
        public float RawDamage { get; set; }
        public float DamageApplied { get; set; }
        public float HealApplied { get; set; }
        public bool WasLethal { get; set; }
        public bool RebirthTriggered { get; set; }
        public bool BlockedByInvulnerability { get; set; }
        public float CurrentHp { get; set; }
        public float MaxHp { get; set; }
    }
}
