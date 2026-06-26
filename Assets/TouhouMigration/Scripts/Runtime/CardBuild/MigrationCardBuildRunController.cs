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
        private readonly MigrationCardVulnerabilityWindow terrainSuppression = new MigrationCardVulnerabilityWindow();

        public const int MaxTerrainPressure = 6;

        public string BossClauseId { get; }

        // Godot init: terrain pressure starts at 2, no rewritten rules yet.
        public int TerrainPressure { get; set; } = 2;
        public int RewrittenRuleCount { get; set; }

        // Terrain-suppression timer (Godot _terrain_suppression_seconds / is_terrain_suppressed): a window,
        // extended by certain cards, that — like vulnerability — also reads suppressed when the boss clause
        // is sealed. Bump TerrainPressure with the MAX clamp.
        public double TerrainSuppressionSeconds => terrainSuppression.Seconds;
        public void SuppressTerrain(double seconds) => terrainSuppression.Open(seconds);
        public void TickTerrainSuppression(double deltaSeconds) => terrainSuppression.Tick(deltaSeconds);
        public bool IsTerrainSuppressed => terrainSuppression.IsOpen || Clauses.IsSealed(BossClauseId);

        public void AddTerrainPressure(int delta)
        {
            int next = TerrainPressure + delta;
            TerrainPressure = next < 0 ? 0 : next > MaxTerrainPressure ? MaxTerrainPressure : next;
        }

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

        // Cirno's seconds-based per-card replay cooldown (Godot _card_cooldowns), distinct from the deck's
        // turn-based cooldown pile (which stays for the retain/exhaust/cooldown deck cycle). play_card sets
        // it; tick / reduce count it down; the play gate reads it.
        private readonly Dictionary<string, double> cardCooldowns = new Dictionary<string, double>();

        public void SetCardCooldown(string cardId, double seconds)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                return;
            }

            if (seconds <= 0.0)
            {
                cardCooldowns.Remove(cardId);
            }
            else
            {
                cardCooldowns[cardId] = seconds;
            }
        }

        public double GetCardCooldown(string cardId)
        {
            return cardId != null && cardCooldowns.TryGetValue(cardId, out double seconds) ? seconds : 0.0;
        }

        // Godot get_card_cooldown > 0.05 threshold.
        public bool IsCardOnCooldown(string cardId) => GetCardCooldown(cardId) > 0.05;

        // Per-frame countdown (Godot tick): reduce every card cooldown by dt, dropping expired entries.
        public void TickCardCooldowns(double deltaSeconds) => ReduceCardCooldowns(deltaSeconds);

        // Reduce every card cooldown by `seconds` (Godot _reduce_card_cooldowns); a non-positive value is
        // ignored. Expired cooldowns are removed.
        public void ReduceCardCooldowns(double seconds)
        {
            if (seconds <= 0.0)
            {
                return;
            }

            List<string> expired = null;
            List<string> keys = new List<string>(cardCooldowns.Keys);
            foreach (string cardId in keys)
            {
                double next = cardCooldowns[cardId] - seconds;
                if (next <= 0.0)
                {
                    (expired ??= new List<string>()).Add(cardId);
                }
                else
                {
                    cardCooldowns[cardId] = next;
                }
            }

            if (expired != null)
            {
                foreach (string cardId in expired)
                {
                    cardCooldowns.Remove(cardId);
                }
            }
        }

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
            IReadOnlyDictionary<string, int> cost = null, double cooldownSeconds = 0.0)
        {
            if (IsBossDefeated)
            {
                return CardPlayResult.Blocked(cardId, "boss_defeated");
            }

            if (!HandContains(cardId))
            {
                return CardPlayResult.Blocked(cardId, "not_in_hand");
            }

            if (IsCardOnCooldown(cardId))
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
            ResolveCardEffect(cardId);
            Deck.DiscardFromHand(cardId);
            if (cooldownSeconds > 0.0)
            {
                SetCardCooldown(cardId, cooldownSeconds);
            }

            return CardPlayResult.Ok(cardId);
        }

        // Per-card bespoke resolution (Godot _apply_cirno_card_resolution). Ported by archetype; an
        // unported / inert card id is a no-op. Fire archetype is done; blood/mechanism/mokou follow.
        public void ResolveCardEffect(string cardId)
        {
            switch (cardId)
            {
                case "fire_starter_ember_shot":
                    if (IsVulnerabilityOpen)
                    {
                        Boss.Damage(18 + RewrittenRuleCount * 6);
                    }
                    break;
                case "fire_resource_ash_collector":
                {
                    int emberGain = System.Math.Max(1, State.GetStatus("enemy", "burn"));
                    State.AddResource("ember", emberGain);
                    ReduceCardCooldowns(0.35 * emberGain);
                    break;
                }
                case "fire_payoff_detonation_palm":
                {
                    int burn = State.GetStatus("enemy", "burn");
                    if (burn <= 0)
                    {
                        break;
                    }

                    State.ApplyStatus("enemy", "burn", -burn);
                    int damage = 22 + burn * 18 + State.GetResource("ember") * 4;
                    if (IsVulnerabilityOpen)
                    {
                        damage = (int)System.Math.Round(damage * 1.35, System.MidpointRounding.AwayFromZero);
                    }

                    Boss.Damage(damage);
                    break;
                }
                case "fire_defense_phoenix_guard":
                    AddInstalledCard(new MigrationCardEffectBlock { Type = "install", Id = "phoenix_guard" });
                    AddTerrainPressure(-1);
                    break;
                case "fire_movement_firestep":
                    SuppressTerrain(2.5);
                    ReduceCardCooldowns(0.25);
                    break;
                case "fire_draw_smoke_reading":
                    ReduceCardCooldowns(1.0);
                    break;
                case "fire_partner_shared_kindling":
                    State.ApplyStatus("enemy", "burn", 1);
                    State.AddResource("ember", 1);
                    break;
                case "fire_risk_burn_the_hand":
                {
                    int emberGain = System.Math.Max(2, (int)System.Math.Ceiling(Deck.HandCount / 4.0));
                    State.AddResource("ember", emberGain);
                    ReduceCardCooldowns(0.5);
                    break;
                }
                case "fire_bridge_hot_iron_contract":
                    AddInstalledCard(new MigrationCardEffectBlock { Type = "install", Id = "hot_iron_contract" });
                    break;
                case "fire_boss_ash_seal":
                    ResolveAshSeal();
                    break;
                case "fire_terminal_hourai_phoenix":
                    ResolveHouraiPhoenix();
                    break;
                case "blood_starter_scarlet_shot":
                    State.ApplyStatus("enemy", "fate_lock", 1);
                    Boss.Damage(14 + State.GetResource("fate") * 3);
                    break;
                case "blood_payoff_fate_spear":
                    ResolveFateSpear();
                    break;
                case "blood_defense_noble_guard":
                    AddInstalledCard(new MigrationCardEffectBlock { Type = "install", Id = "noble_guard" });
                    break;
                case "blood_movement_night_dash":
                    State.ApplyStatus("enemy", "fate_lock", 1);
                    SuppressTerrain(1.8);
                    break;
                case "blood_draw_moonlit_invitation":
                    State.AddResource("fate", 1);
                    ReduceCardCooldowns(0.8);
                    break;
                case "blood_partner_vampire_contract":
                    AddPartnerEvent(new MigrationCardEffectBlock { Type = "trigger_partner", Id = "vampire_contract" });
                    break;
                case "blood_boss_break_destiny":
                    AddTerrainPressure(-1);
                    OpenVulnerability(2.5);
                    break;
                case "blood_terminal_spear_the_gungnir":
                    ResolveGungnir();
                    break;
                case "blood_risk_forbidden_appetite":
                    AddInstalledCard(new MigrationCardEffectBlock { Type = "install", Id = "forbidden_appetite" });
                    State.AddResource("fate", 1);
                    break;
                case "blood_bridge_bloody_barrier":
                    AddInstalledCard(new MigrationCardEffectBlock { Type = "install", Id = "bloody_barrier" });
                    break;
                case "mechanism_starter_first_seal":
                case "mechanism_movement_ritual_position":
                    State.AddResource("seal", 1);
                    break;
                case "mechanism_resource_evidence_token":
                    State.AddResource("seal", Clauses.IsExposed(BossClauseId) ? 2 : 1);
                    break;
                case "mechanism_payoff_binding_verdict":
                    ResolveBindingVerdict();
                    break;
                case "mechanism_defense_procedural_immunity":
                    AddInstalledCard(new MigrationCardEffectBlock { Type = "install", Id = "procedural_immunity" });
                    break;
                case "mechanism_draw_historical_precedent":
                    ReduceCardCooldowns(1.2);
                    break;
                case "mechanism_partner_witness_statement":
                    AddPartnerEvent(new MigrationCardEffectBlock { Type = "trigger_partner", Id = "witness_statement" });
                    break;
                case "mechanism_boss_clause_lock":
                    ResolveClauseLock();
                    break;
                case "mechanism_terminal_fantasy_verdict":
                    ResolveFantasyVerdict();
                    break;
                case "mechanism_risk_legal_trap":
                    AddTerrainPressure(1);
                    State.AddResource("seal", 2);
                    break;
                case "mechanism_bridge_market_regulation":
                    AddInstalledCard(new MigrationCardEffectBlock { Type = "install", Id = "market_regulation" });
                    break;
                case "mokou_starter_fire_bird":
                    if (IsVulnerabilityOpen)
                    {
                        Boss.Damage(20 + RewrittenRuleCount * 6 + State.GetResource("ember") * 2);
                    }
                    break;
                case "mokou_payoff_fujiyama_burst":
                    ResolveMokouFujiyamaBurst();
                    break;
                case "mokou_defense_xu_fu_dimension":
                    AddTerrainPressure(-2);
                    SuppressTerrain(3.5);
                    ReduceCardCooldowns(0.35);
                    break;
                case "mokou_movement_bamboo_escape":
                    SuppressTerrain(2.8);
                    ReduceCardCooldowns(0.35);
                    break;
                case "mokou_draw_old_history_cinders":
                    ReduceCardCooldowns(0.75);
                    break;
                case "mokou_attack_flame_fist":
                    ResolveMokouFlameFist();
                    break;
                case "mokou_terminal_hourai_doll":
                    ResolveMokouHouraiDoll();
                    break;
                case "mokou_risk_honest_mans_death":
                    AddInstalledCard(new MigrationCardEffectBlock { Type = "install", Id = "honest_mans_death" });
                    AddTerrainPressure(1);
                    OpenVulnerability(2.0);
                    ReduceCardCooldowns(0.8);
                    break;
                case "mokou_bridge_imperishable_shooting":
                    ReduceCardCooldowns(0.6);
                    break;
                case "mokou_boss_melt_the_lake":
                    ResolveMokouMeltTheLake();
                    break;
            }
        }

        private void ResolveMokouMeltTheLake()
        {
            if (State.GetResource("ember") < 1 || State.GetStatus("enemy", "burn") < 1)
            {
                return;
            }

            State.SpendResource("ember", 1);
            State.ApplyStatus("enemy", "burn", -1);
            RewrittenRuleCount++;
            Domains.Contest(BossClauseId, 1);
            AddTerrainPressure(-3);
            SuppressTerrain(7.5);
            OpenVulnerability(5.0);
            Clauses.Disable(BossClauseId, 1);

            if (Domains.IsSealed(BossClauseId) && !Clauses.IsSealed(BossClauseId))
            {
                Clauses.SealWithAnswer(BossClauseId, "field_replace", 2);
            }
        }

        private void ResolveMokouFujiyamaBurst()
        {
            int burn = State.GetStatus("enemy", "burn");
            if (burn <= 0)
            {
                return;
            }

            State.ApplyStatus("enemy", "burn", -burn);
            int damage = 28 + burn * 20 + State.GetResource("ember") * 5;
            if (IsVulnerabilityOpen)
            {
                damage = (int)System.Math.Round(damage * 1.3, System.MidpointRounding.AwayFromZero);
            }

            Boss.Damage(damage);
            AddTerrainPressure(-1);
        }

        private void ResolveMokouFlameFist()
        {
            int damage = 24 + State.GetResource("ember") * 3;
            if (State.GetStatus("enemy", "burn") > 0)
            {
                damage += 18;
                OpenVulnerability(1.6);
            }

            Boss.Damage(damage);
            State.ApplyStatus("enemy", "burn", 1);
        }

        private void ResolveMokouHouraiDoll()
        {
            int ember = State.GetResource("ember");
            int burn = State.GetStatus("enemy", "burn");
            if (ember < 2)
            {
                return;
            }

            int damage = 92 + ember * 30 + burn * 14 + RewrittenRuleCount * 30;
            if (IsVulnerabilityOpen)
            {
                damage = (int)System.Math.Round(damage * 1.22, System.MidpointRounding.AwayFromZero);
            }

            State.SpendResource("ember", ember);
            if (burn > 0)
            {
                State.ApplyStatus("enemy", "burn", -burn);
            }

            Boss.Damage(damage);
            ReduceVulnerability(1.5);
        }

        private void ResolveBindingVerdict()
        {
            if (State.GetResource("seal") + State.GetStatus("enemy", "seal") < 2)
            {
                return;
            }

            State.SpendResource("seal", System.Math.Min(State.GetResource("seal"), 1));
            if (State.GetStatus("enemy", "seal") > 0)
            {
                State.ApplyStatus("enemy", "seal", -1);
            }

            RewrittenRuleCount++;
            AddTerrainPressure(-1);
            SuppressTerrain(4.0);
            OpenVulnerability(3.0);
        }

        private void ResolveClauseLock()
        {
            if (State.GetResource("seal") + State.GetStatus("enemy", "seal") > 0)
            {
                State.SpendResource("seal", System.Math.Min(State.GetResource("seal"), 1));
                RewrittenRuleCount++;
            }

            AddTerrainPressure(-1);
            SuppressTerrain(4.5);
            OpenVulnerability(2.5);
        }

        private void ResolveFantasyVerdict()
        {
            int sealTotal = State.GetResource("seal") + State.GetStatus("enemy", "seal") + RewrittenRuleCount;
            if (sealTotal < 3)
            {
                return;
            }

            int damage = 96 + sealTotal * 24;
            State.SpendResource("seal", State.GetResource("seal"));
            int enemySeal = State.GetStatus("enemy", "seal");
            if (enemySeal > 0)
            {
                State.ApplyStatus("enemy", "seal", -enemySeal);
            }

            Boss.Damage(damage);
            TerrainPressure = 0;
            SuppressTerrain(8.0);
            OpenVulnerability(5.0);
        }

        private void ResolveFateSpear()
        {
            int fateLock = State.GetStatus("enemy", "fate_lock");
            int fate = State.GetResource("fate");
            int damage = 18;
            if (fateLock > 0)
            {
                damage += 34 + fate * 8;
                State.ApplyStatus("enemy", "fate_lock", -1);
            }

            if (fate > 0)
            {
                State.SpendResource("fate", System.Math.Min(fate, 1));
            }

            Boss.Damage(damage);
        }

        private void ResolveGungnir()
        {
            int fate = State.GetResource("fate");
            int fateLock = State.GetStatus("enemy", "fate_lock");
            if (fate <= 0 && fateLock <= 0)
            {
                return;
            }

            int damage = 70 + fate * 22 + fateLock * 18;
            State.SpendResource("fate", fate);
            if (fateLock > 0)
            {
                State.ApplyStatus("enemy", "fate_lock", -fateLock);
            }

            Boss.Damage(damage);
        }

        private void ResolveAshSeal()
        {
            if (State.GetResource("ember") < 1 || State.GetStatus("enemy", "burn") < 1)
            {
                return;
            }

            State.SpendResource("ember", 1);
            State.ApplyStatus("enemy", "burn", -1);
            RewrittenRuleCount++;
            AddTerrainPressure(-2);
            SuppressTerrain(7.0);
            OpenVulnerability(5.0);
            Clauses.Disable(BossClauseId, 1);

            if (RewrittenRuleCount >= 3 && !Clauses.IsSealed(BossClauseId))
            {
                Clauses.SealWithAnswer(BossClauseId, "mechanism_rewrite_environment", 2);
            }
        }

        private void ResolveHouraiPhoenix()
        {
            int ember = State.GetResource("ember");
            int burn = State.GetStatus("enemy", "burn");
            if (!IsVulnerabilityOpen || ember < 2)
            {
                return;
            }

            int damage = 80 + ember * 34 + burn * 16 + RewrittenRuleCount * 28;
            State.SpendResource("ember", ember);
            if (burn > 0)
            {
                State.ApplyStatus("enemy", "burn", -burn);
            }

            Boss.Damage(damage);
            ReduceVulnerability(2.0);
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
