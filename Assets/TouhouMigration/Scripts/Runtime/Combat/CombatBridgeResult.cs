using TouhouMigration.Runtime.Player;

namespace TouhouMigration.Runtime.Combat
{
    public sealed class CombatBridgeResult
    {
        public float RawDamage { get; set; }
        public float DamageApplied { get; set; }
        public string AttackType { get; set; } = string.Empty;
        public bool TargetDefeated { get; set; }
        public float TargetCurrentHp { get; set; }
        public float TargetMaxHp { get; set; }
        public float PlayerHealApplied { get; set; }
        public PlayerHealthResult PlayerHealthResult { get; set; }
    }
}
