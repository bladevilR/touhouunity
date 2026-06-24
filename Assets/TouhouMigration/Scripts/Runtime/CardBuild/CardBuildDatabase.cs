using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TouhouMigration.Runtime.CardBuild
{
    public sealed class CardBuildDatabase
    {
        private readonly Dictionary<string, CardBuildArchetypeDefinition> archetypes = new Dictionary<string, CardBuildArchetypeDefinition>();
        private readonly Dictionary<string, CardBuildCharacterDefinition> characters = new Dictionary<string, CardBuildCharacterDefinition>();
        private readonly Dictionary<string, CardBuildCardDefinition> cards = new Dictionary<string, CardBuildCardDefinition>();
        private readonly Dictionary<string, CardBuildSimpleDefinition> bossRules = new Dictionary<string, CardBuildSimpleDefinition>();
        private readonly Dictionary<string, CardBuildSimpleDefinition> resources = new Dictionary<string, CardBuildSimpleDefinition>();
        private readonly Dictionary<string, CardBuildSimpleDefinition> statuses = new Dictionary<string, CardBuildSimpleDefinition>();
        private readonly Dictionary<string, CardBuildSimpleDefinition> relics = new Dictionary<string, CardBuildSimpleDefinition>();
        private readonly Dictionary<string, CardBuildSimpleDefinition> upgrades = new Dictionary<string, CardBuildSimpleDefinition>();
        private readonly List<string> errors = new List<string>();

        public int ArchetypeCount => archetypes.Count;
        public int CharacterCount => characters.Count;
        public int CardCount => cards.Count;
        public int BossRuleCount => bossRules.Count;
        public int ResourceCount => resources.Count;
        public int StatusCount => statuses.Count;
        public int RelicCount => relics.Count;
        public int UpgradeCount => upgrades.Count;
        public IReadOnlyList<string> Errors => errors;

        public bool LoadFromDirectory(string dataDirectory)
        {
            Clear();
            string resolvedDirectory = ResolveDataDirectory(dataDirectory);
            if (!Directory.Exists(resolvedDirectory))
            {
                errors.Add($"missing cardbuild data directory: {dataDirectory}");
                return false;
            }

            CardBuildArchetypesDocument archetypesDocument = LoadJson<CardBuildArchetypesDocument>(resolvedDirectory, "archetypes.json");
            CardBuildCharactersDocument charactersDocument = LoadJson<CardBuildCharactersDocument>(resolvedDirectory, "characters.json");
            CardBuildCardsDocument cardsDocument = LoadJson<CardBuildCardsDocument>(resolvedDirectory, "cards.json");
            CardBuildBossRulesDocument bossRulesDocument = LoadJson<CardBuildBossRulesDocument>(resolvedDirectory, "boss_rules.json");
            CardBuildResourcesDocument resourcesDocument = LoadJson<CardBuildResourcesDocument>(resolvedDirectory, "resources.json");
            CardBuildStatusesDocument statusesDocument = LoadJson<CardBuildStatusesDocument>(resolvedDirectory, "statuses.json");
            CardBuildRelicsDocument relicsDocument = LoadJson<CardBuildRelicsDocument>(resolvedDirectory, "relics.json");
            CardBuildUpgradesDocument upgradesDocument = LoadJson<CardBuildUpgradesDocument>(resolvedDirectory, "upgrades.json");

            if (errors.Count > 0)
            {
                return false;
            }

            IndexArchetypes(archetypesDocument.archetypes);
            IndexCharacters(charactersDocument.characters);
            IndexGeneratedCards();
            IndexExtraCards(cardsDocument.cards);
            IndexSimpleDefinitions(bossRulesDocument.boss_rules, bossRules, "boss rule");
            IndexSimpleDefinitions(resourcesDocument.resources, resources, "resource");
            IndexSimpleDefinitions(statusesDocument.statuses, statuses, "status");
            IndexSimpleDefinitions(relicsDocument.relics, relics, "relic");
            IndexSimpleDefinitions(upgradesDocument.upgrades, upgrades, "upgrade");
            ValidateReferences();
            return errors.Count == 0;
        }

        public bool HasCard(string cardId)
        {
            return cards.ContainsKey(cardId);
        }

        public bool HasCharacter(string characterId)
        {
            return characters.ContainsKey(characterId);
        }

        public CardBuildCardDefinition GetCard(string cardId)
        {
            return cards.TryGetValue(cardId, out CardBuildCardDefinition card) ? card : null;
        }

        public IReadOnlyList<string> GetAllCardIds()
        {
            return cards.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray();
        }

        public IReadOnlyList<string> GetAvailableCardIds(string characterId)
        {
            if (!characters.TryGetValue(characterId, out CardBuildCharacterDefinition character))
            {
                return Array.Empty<string>();
            }

            HashSet<string> allowedArchetypes = new HashSet<string>(character.Archetypes);
            List<string> ids = new List<string>();
            foreach (CardBuildCardDefinition card in cards.Values)
            {
                if (!string.IsNullOrWhiteSpace(card.OwnerCharacter))
                {
                    if (card.OwnerCharacter == characterId)
                    {
                        ids.Add(card.CardId);
                    }

                    continue;
                }

                if (card.Archetypes.Any(allowedArchetypes.Contains))
                {
                    ids.Add(card.CardId);
                }
            }

            ids.Sort(StringComparer.Ordinal);
            return ids;
        }

        private void Clear()
        {
            archetypes.Clear();
            characters.Clear();
            cards.Clear();
            bossRules.Clear();
            resources.Clear();
            statuses.Clear();
            relics.Clear();
            upgrades.Clear();
            errors.Clear();
        }

        private T LoadJson<T>(string directory, string fileName)
            where T : new()
        {
            string path = Path.Combine(directory, fileName);
            if (!File.Exists(path))
            {
                errors.Add($"missing data file: {fileName}");
                return new T();
            }

            try
            {
                return JsonUtility.FromJson<T>(File.ReadAllText(path)) ?? new T();
            }
            catch (Exception exception)
            {
                errors.Add($"invalid JSON file {fileName}: {exception.Message}");
                return new T();
            }
        }

        private void IndexArchetypes(List<CardBuildArchetypeRecord> records)
        {
            foreach (CardBuildArchetypeRecord record in records ?? new List<CardBuildArchetypeRecord>())
            {
                if (string.IsNullOrWhiteSpace(record.id))
                {
                    errors.Add("archetype missing id");
                    continue;
                }

                if (archetypes.ContainsKey(record.id))
                {
                    errors.Add($"duplicate archetype id: {record.id}");
                    continue;
                }

                archetypes[record.id] = new CardBuildArchetypeDefinition(record);
            }
        }

        private void IndexCharacters(List<CardBuildCharacterRecord> records)
        {
            foreach (CardBuildCharacterRecord record in records ?? new List<CardBuildCharacterRecord>())
            {
                if (string.IsNullOrWhiteSpace(record.id))
                {
                    errors.Add("character missing id");
                    continue;
                }

                if (characters.ContainsKey(record.id))
                {
                    errors.Add($"duplicate character id: {record.id}");
                    continue;
                }

                characters[record.id] = new CardBuildCharacterDefinition(record);
            }
        }

        private void IndexGeneratedCards()
        {
            foreach (CardBuildArchetypeDefinition archetype in archetypes.Values)
            {
                foreach (CardBuildCardRecord slot in archetype.SkeletonSlots)
                {
                    CardBuildCardDefinition card = new CardBuildCardDefinition(slot, new[] { archetype.Id }, archetype.Id);
                    AddCard(card, "generated card");
                }
            }
        }

        private void IndexExtraCards(List<CardBuildCardRecord> records)
        {
            foreach (CardBuildCardRecord record in records ?? new List<CardBuildCardRecord>())
            {
                AddCard(new CardBuildCardDefinition(record, record.archetypes ?? new List<string>(), string.Empty), "extra card");
            }
        }

        private void AddCard(CardBuildCardDefinition card, string label)
        {
            if (string.IsNullOrWhiteSpace(card.CardId))
            {
                errors.Add($"{label} missing card_id");
                return;
            }

            if (cards.ContainsKey(card.CardId))
            {
                errors.Add($"duplicate card id: {card.CardId}");
                return;
            }

            cards[card.CardId] = card;
        }

        private void IndexSimpleDefinitions(
            List<CardBuildSimpleRecord> records,
            Dictionary<string, CardBuildSimpleDefinition> target,
            string label)
        {
            foreach (CardBuildSimpleRecord record in records ?? new List<CardBuildSimpleRecord>())
            {
                if (string.IsNullOrWhiteSpace(record.id))
                {
                    errors.Add($"{label} missing id");
                    continue;
                }

                if (target.ContainsKey(record.id))
                {
                    errors.Add($"duplicate {label} id: {record.id}");
                    continue;
                }

                target[record.id] = new CardBuildSimpleDefinition(record.id, record.display_name_zh, record.display_name_en);
            }
        }

        private void ValidateReferences()
        {
            foreach (CardBuildCharacterDefinition character in characters.Values)
            {
                foreach (string archetypeId in character.Archetypes)
                {
                    if (!archetypes.ContainsKey(archetypeId))
                    {
                        errors.Add($"character {character.Id} references missing archetype {archetypeId}");
                    }
                }
            }

            foreach (CardBuildCardDefinition card in cards.Values)
            {
                foreach (string archetypeId in card.Archetypes)
                {
                    if (!archetypes.ContainsKey(archetypeId))
                    {
                        errors.Add($"card {card.CardId} references missing archetype {archetypeId}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(card.OwnerCharacter) && !characters.ContainsKey(card.OwnerCharacter))
                {
                    errors.Add($"card {card.CardId} references missing owner_character {card.OwnerCharacter}");
                }
            }
        }

        private static string ResolveDataDirectory(string dataDirectory)
        {
            if (Path.IsPathRooted(dataDirectory))
            {
                return dataDirectory;
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), dataDirectory));
        }
    }

    public sealed class CardBuildArchetypeDefinition
    {
        public CardBuildArchetypeDefinition(CardBuildArchetypeRecord record)
        {
            Id = record.id;
            DisplayNameZh = record.display_name_zh;
            DisplayNameEn = record.display_name_en;
            SkeletonSlots = record.skeleton_slots ?? new List<CardBuildCardRecord>();
        }

        public string Id { get; }
        public string DisplayNameZh { get; }
        public string DisplayNameEn { get; }
        public IReadOnlyList<CardBuildCardRecord> SkeletonSlots { get; }
    }

    public sealed class CardBuildCharacterDefinition
    {
        public CardBuildCharacterDefinition(CardBuildCharacterRecord record)
        {
            Id = record.id;
            DisplayNameZh = record.display_name_zh;
            DisplayNameEn = record.display_name_en;
            Archetypes = record.archetypes ?? new List<string>();
        }

        public string Id { get; }
        public string DisplayNameZh { get; }
        public string DisplayNameEn { get; }
        public IReadOnlyList<string> Archetypes { get; }
    }

    public sealed class CardBuildCardDefinition
    {
        public CardBuildCardDefinition(CardBuildCardRecord record, IEnumerable<string> archetypes, string generatedFromArchetype)
        {
            CardId = record.card_id;
            Slot = record.slot;
            DisplayNameZh = record.display_name_zh;
            DisplayNameEn = record.display_name_en;
            CardType = record.card_type;
            OwnerCharacter = record.owner_character ?? string.Empty;
            Archetypes = archetypes.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            GeneratedFromArchetype = generatedFromArchetype;
            AllowedSlots = record.allowed_slots ?? new List<string>();
        }

        public string CardId { get; }
        public string Slot { get; }
        public string DisplayNameZh { get; }
        public string DisplayNameEn { get; }
        public string CardType { get; }
        public string OwnerCharacter { get; }
        public IReadOnlyList<string> Archetypes { get; }
        public string GeneratedFromArchetype { get; }
        public IReadOnlyList<string> AllowedSlots { get; }
    }

    public readonly struct CardBuildSimpleDefinition
    {
        public CardBuildSimpleDefinition(string id, string displayNameZh, string displayNameEn)
        {
            Id = id;
            DisplayNameZh = displayNameZh;
            DisplayNameEn = displayNameEn;
        }

        public string Id { get; }
        public string DisplayNameZh { get; }
        public string DisplayNameEn { get; }
    }

    [Serializable]
    internal sealed class CardBuildArchetypesDocument
    {
        public List<CardBuildArchetypeRecord> archetypes = new List<CardBuildArchetypeRecord>();
    }

    [Serializable]
    internal sealed class CardBuildCharactersDocument
    {
        public List<CardBuildCharacterRecord> characters = new List<CardBuildCharacterRecord>();
    }

    [Serializable]
    internal sealed class CardBuildCardsDocument
    {
        public List<CardBuildCardRecord> cards = new List<CardBuildCardRecord>();
    }

    [Serializable]
    internal sealed class CardBuildBossRulesDocument
    {
        public List<CardBuildSimpleRecord> boss_rules = new List<CardBuildSimpleRecord>();
    }

    [Serializable]
    internal sealed class CardBuildResourcesDocument
    {
        public List<CardBuildSimpleRecord> resources = new List<CardBuildSimpleRecord>();
    }

    [Serializable]
    internal sealed class CardBuildStatusesDocument
    {
        public List<CardBuildSimpleRecord> statuses = new List<CardBuildSimpleRecord>();
    }

    [Serializable]
    internal sealed class CardBuildRelicsDocument
    {
        public List<CardBuildSimpleRecord> relics = new List<CardBuildSimpleRecord>();
    }

    [Serializable]
    internal sealed class CardBuildUpgradesDocument
    {
        public List<CardBuildSimpleRecord> upgrades = new List<CardBuildSimpleRecord>();
    }

    [Serializable]
    public sealed class CardBuildArchetypeRecord
    {
        public string id;
        public string display_name_zh;
        public string display_name_en;
        public List<CardBuildCardRecord> skeleton_slots = new List<CardBuildCardRecord>();
    }

    [Serializable]
    public sealed class CardBuildCharacterRecord
    {
        public string id;
        public string display_name_zh;
        public string display_name_en;
        public List<string> archetypes = new List<string>();
    }

    [Serializable]
    public sealed class CardBuildCardRecord
    {
        public string slot;
        public string card_id;
        public string display_name_zh;
        public string display_name_en;
        public string card_type;
        public string owner_character;
        public List<string> archetypes = new List<string>();
        public List<string> allowed_slots = new List<string>();
    }

    [Serializable]
    public sealed class CardBuildSimpleRecord
    {
        public string id;
        public string display_name_zh;
        public string display_name_en;
    }
}
