using System.Collections.Generic;

namespace TouhouMigration.Runtime.CardBuild
{
    // The CardBuild run facade (Godot CardBuildMvpRunController): composes the independently-tested units
    // — deck piles, the resource/status substrate, the boss HP pool, the vulnerability window, boss
    // clauses, boss domains, and the Mokou charge chain — behind one object and resolves the composite
    // queries the per-card resolution and effect executor build on. Unlike Godot's single god-state, the
    // units stay separate and unit-testable; this facade is the integration seam over them.
    public sealed class MigrationCardBuildRunController
    {
        public MigrationCardBuildRunController(IEnumerable<string> deckCards, int bossMaxHp = 540, string bossClauseId = "cirno_domain")
        {
            Deck = new MigrationCardDeck(deckCards);
            State = new MigrationCardRunState();
            Boss = new MigrationCardBossHp(bossMaxHp);
            Clauses = new MigrationCardBossClauses();
            Domains = new MigrationCardBossDomains();
            Mokou = new MigrationMokouChargeState();
            BossClauseId = bossClauseId ?? string.Empty;
        }

        public MigrationCardDeck Deck { get; }
        public MigrationCardRunState State { get; }
        public MigrationCardBossHp Boss { get; }
        public MigrationCardBossClauses Clauses { get; }
        public MigrationCardBossDomains Domains { get; }
        public MigrationMokouChargeState Mokou { get; }

        private readonly MigrationCardVulnerabilityWindow vulnerability = new MigrationCardVulnerabilityWindow();

        public string BossClauseId { get; }

        // Godot init: terrain pressure starts at 2, no rewritten rules yet.
        public int TerrainPressure { get; set; } = 2;
        public int RewrittenRuleCount { get; set; }

        public double VulnerabilitySeconds => vulnerability.Seconds;

        public void OpenVulnerability(double seconds) => vulnerability.Open(seconds);

        public void ReduceVulnerability(double seconds) => vulnerability.Reduce(seconds);

        public void TickVulnerability(double deltaSeconds) => vulnerability.Tick(deltaSeconds);

        // Godot is_vulnerability_open: the timer window is open OR the boss clause has been sealed.
        public bool IsVulnerabilityOpen => vulnerability.IsOpen || Clauses.IsSealed(BossClauseId);

        // Resolve a player attack through the live vulnerability/terrain/rewritten-rule state and apply it
        // to the boss (Godot apply_player_attack_damage). Returns the HP removed.
        public int ApplyPlayerAttack(double amount)
        {
            return Boss.ApplyPlayerAttack(amount, IsVulnerabilityOpen, TerrainPressure, RewrittenRuleCount);
        }

        public int BossHp => Boss.CurrentHp;

        public bool IsBossDefeated => Boss.IsDefeated;

        // Run-level effect collections (Godot CardBuildRuntimeState summons / installed_cards /
        // field_objects / partner_events / bullet_modifiers): simple append-lists the effect executor
        // grows. Reads expose counts/contents; the systems that consume them are later slices.
        private readonly List<MigrationCardEffectBlock> summons = new List<MigrationCardEffectBlock>();
        private readonly List<MigrationCardEffectBlock> installedCards = new List<MigrationCardEffectBlock>();
        private readonly List<MigrationCardEffectBlock> fieldObjects = new List<MigrationCardEffectBlock>();
        private readonly List<MigrationCardEffectBlock> partnerEvents = new List<MigrationCardEffectBlock>();
        private readonly List<MigrationCardEffectBlock> bulletModifiers = new List<MigrationCardEffectBlock>();

        public IReadOnlyList<MigrationCardEffectBlock> Summons => summons;
        public IReadOnlyList<MigrationCardEffectBlock> InstalledCards => installedCards;
        public IReadOnlyList<MigrationCardEffectBlock> FieldObjects => fieldObjects;
        public IReadOnlyList<MigrationCardEffectBlock> PartnerEvents => partnerEvents;
        public IReadOnlyList<MigrationCardEffectBlock> BulletModifiers => bulletModifiers;

        public void AddSummon(MigrationCardEffectBlock block) => Append(summons, block);
        public void AddInstalledCard(MigrationCardEffectBlock block) => Append(installedCards, block);
        public void AddFieldObject(MigrationCardEffectBlock block) => Append(fieldObjects, block);
        public void AddPartnerEvent(MigrationCardEffectBlock block) => Append(partnerEvents, block);
        public void AddBulletModifier(MigrationCardEffectBlock block) => Append(bulletModifiers, block);

        private static void Append(List<MigrationCardEffectBlock> list, MigrationCardEffectBlock block)
        {
            if (block != null)
            {
                list.Add(block);
            }
        }
    }
}
