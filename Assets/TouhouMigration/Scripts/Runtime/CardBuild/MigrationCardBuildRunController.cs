using System.Collections.Generic;

namespace TouhouMigration.Runtime.CardBuild
{
    // The outcome of a card play (Godot play_card_by_id success/blocking-reason).
    public readonly struct CardPlayResult
    {
        public CardPlayResult(bool success, string cardId, string reason)
        {
            Success = success;
            CardId = cardId;
            Reason = reason;
        }

        public bool Success { get; }
        public string CardId { get; }
        public string Reason { get; }

        public static CardPlayResult Ok(string cardId) => new CardPlayResult(true, cardId, string.Empty);
        public static CardPlayResult Blocked(string cardId, string reason) => new CardPlayResult(false, cardId, reason);
    }

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

        private readonly MigrationCardEffectExecutor effectExecutor = new MigrationCardEffectExecutor();

        // Per-card runtime requirement gate (Godot _runtime_requirement_disabled_reason): the bespoke
        // resource/status/vulnerability/rewritten-rule thresholds certain payoff/terminal cards need.
        // Returns the (Chinese) reason a card is currently unplayable, or "" when its requirement is met.
        public string RuntimeRequirementReason(string cardId)
        {
            int ember = State.GetResource("ember");
            int seal = State.GetResource("seal");
            int fate = State.GetResource("fate");
            int enemyBurn = State.GetStatus("enemy", "burn");
            int enemySeal = State.GetStatus("enemy", "seal");
            int enemyFateLock = State.GetStatus("enemy", "fate_lock");

            switch (cardId)
            {
                case "fire_payoff_detonation_palm":
                case "mokou_payoff_fujiyama_burst":
                    return enemyBurn <= 0 ? "需要灼烧层数来引爆" : string.Empty;
                case "fire_boss_ash_seal":
                case "mokou_boss_melt_the_lake":
                    return ember < 1 || enemyBurn < 1 ? "需要火种和灼烧来融化湖面" : string.Empty;
                case "fire_terminal_hourai_phoenix":
                    return !IsVulnerabilityOpen || ember < 2 ? "需要破绽窗口和2点火种" : string.Empty;
                case "mokou_terminal_hourai_doll":
                    return ember < 2 ? "需要2点火种" : string.Empty;
                case "blood_terminal_spear_the_gungnir":
                    return fate <= 0 && enemyFateLock <= 0 ? "需要命运或命运锁定" : string.Empty;
                case "mechanism_payoff_binding_verdict":
                    return seal + enemySeal < 2 ? "需要2层封印" : string.Empty;
                case "mechanism_terminal_fantasy_verdict":
                    return seal + enemySeal + RewrittenRuleCount < 3 ? "需要3层封印或规则破解" : string.Empty;
                default:
                    return string.Empty;
            }
        }

        // Play a card (Godot play_card_by_id, generic parts): block when the card isn't in hand, is on
        // cooldown, or its cost can't be paid; otherwise run its effect blocks, move it to the discard
        // pile, and put it on cooldown. The card's cost is checked (not spent — cards spend via their own
        // spend_resource blocks). The bespoke per-card resolution + runtime-requirement gates are a later
        // slice; pass the card's parsed effect blocks + (optional) cost + cooldown.
        public CardPlayResult PlayCard(string cardId, IReadOnlyList<MigrationCardEffectBlock> effectBlocks,
            IReadOnlyDictionary<string, int> cost = null, int cooldownTurns = 0)
        {
            if (IsBossDefeated)
            {
                return CardPlayResult.Blocked(cardId, "boss_defeated");
            }

            if (!HandContains(cardId))
            {
                return CardPlayResult.Blocked(cardId, "not_in_hand");
            }

            if (Deck.IsOnCooldown(cardId))
            {
                return CardPlayResult.Blocked(cardId, "cooldown");
            }

            string missing = State.MissingCostResource(cost);
            if (!string.IsNullOrEmpty(missing))
            {
                return CardPlayResult.Blocked(cardId, "resource:" + missing);
            }

            string requirement = RuntimeRequirementReason(cardId);
            if (!string.IsNullOrEmpty(requirement))
            {
                return CardPlayResult.Blocked(cardId, requirement);
            }

            effectExecutor.Execute(effectBlocks, this);
            Deck.DiscardFromHand(cardId);
            if (cooldownTurns > 0)
            {
                Deck.PutOnCooldown(cardId, cooldownTurns);
            }

            return CardPlayResult.Ok(cardId);
        }

        private bool HandContains(string cardId)
        {
            foreach (string card in Deck.Hand)
            {
                if (card == cardId)
                {
                    return true;
                }
            }

            return false;
        }

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
