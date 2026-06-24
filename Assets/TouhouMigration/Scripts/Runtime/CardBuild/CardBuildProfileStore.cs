using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TouhouMigration.Runtime.CardBuild
{
    public sealed class CardBuildProfileStore
    {
        public const string DefaultCharacterId = "fujiwara_no_mokou";
        public const int MinDeckSize = 8;
        public const int MaxDeckSize = 20;
        public const int MaxDuplicates = 2;

        private static readonly string[] RequiredActionSlots =
        {
            "attack_light",
            "attack_heavy",
            "dash",
            "skill_1"
        };

        private static readonly string[] MokouStarterDeck =
        {
            "mokou_starter_fire_bird",
            "mokou_resource_hourai_embers",
            "mokou_payoff_fujiyama_burst",
            "mokou_defense_xu_fu_dimension",
            "mokou_movement_bamboo_escape",
            "mokou_draw_old_history_cinders",
            "mokou_attack_flame_fist",
            "mokou_bullet_phoenix_tail",
            "mokou_boss_melt_the_lake",
            "mokou_terminal_hourai_doll",
            "mokou_risk_honest_mans_death",
            "mokou_bridge_imperishable_shooting"
        };

        private static readonly Dictionary<string, string> MokouActionLoadout = new Dictionary<string, string>
        {
            { "attack_light", "mokou_starter_fire_bird" },
            { "attack_heavy", "mokou_payoff_fujiyama_burst" },
            { "dash", "mokou_movement_bamboo_escape" },
            { "skill_1", "mokou_terminal_hourai_doll" },
            { "passive_1", "mokou_bullet_phoenix_tail" },
            { "rule_1", "mokou_boss_melt_the_lake" }
        };

        private readonly CardBuildDatabase database;
        private readonly string profilePath;

        public CardBuildProfileStore(CardBuildDatabase database, string profilePath)
        {
            this.database = database;
            this.profilePath = string.IsNullOrWhiteSpace(profilePath)
                ? Path.Combine(Application.persistentDataPath, "cardbuild_profiles.json")
                : profilePath;
        }

        public CardBuildProfile CreateDefaultProfile()
        {
            return CreateDefaultProfile(DefaultCharacterId);
        }

        public CardBuildProfile CreateDefaultProfile(string characterId)
        {
            if (characterId == DefaultCharacterId)
            {
                return CardBuildProfile.Create(characterId, MokouStarterDeck, MokouActionLoadout);
            }

            IReadOnlyList<string> available = database.GetAvailableCardIds(characterId);
            Dictionary<string, string> loadout = new Dictionary<string, string>();
            for (int index = 0; index < RequiredActionSlots.Length && index < available.Count; index++)
            {
                loadout[RequiredActionSlots[index]] = available[index];
            }

            return CardBuildProfile.Create(characterId, available.Take(MaxDeckSize), loadout);
        }

        public CardBuildProfileValidationResult ValidateProfile(CardBuildProfile profile)
        {
            List<string> errors = new List<string>();
            if (profile == null)
            {
                return new CardBuildProfileValidationResult(false, new[] { "profile is null" });
            }

            if (string.IsNullOrWhiteSpace(profile.CharacterId))
            {
                errors.Add("profile missing character_id");
            }
            else if (!database.HasCharacter(profile.CharacterId))
            {
                errors.Add($"unknown character_id: {profile.CharacterId}");
            }

            IReadOnlyList<string> deck = profile.ActiveDeck;
            if (deck.Count < MinDeckSize)
            {
                errors.Add($"active_deck needs at least {MinDeckSize} cards");
            }

            if (deck.Count > MaxDeckSize)
            {
                errors.Add($"active_deck supports at most {MaxDeckSize} cards");
            }

            HashSet<string> available = new HashSet<string>(database.GetAvailableCardIds(profile.CharacterId));
            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (string cardId in deck)
            {
                if (!database.HasCard(cardId))
                {
                    errors.Add($"unknown card in active_deck: {cardId}");
                    continue;
                }

                if (!available.Contains(cardId))
                {
                    errors.Add($"card outside character pool: {cardId}");
                }

                counts[cardId] = counts.TryGetValue(cardId, out int count) ? count + 1 : 1;
                if (counts[cardId] > MaxDuplicates)
                {
                    errors.Add($"too many copies of card: {cardId}");
                }
            }

            IReadOnlyDictionary<string, string> loadout = profile.ActionLoadout;
            foreach (string slot in RequiredActionSlots)
            {
                if (!loadout.TryGetValue(slot, out string cardId) || string.IsNullOrWhiteSpace(cardId))
                {
                    errors.Add($"missing action card: {slot}");
                    continue;
                }

                if (!deck.Contains(cardId))
                {
                    errors.Add($"action card not in active_deck: {slot}={cardId}");
                }
            }

            return new CardBuildProfileValidationResult(errors.Count == 0, errors);
        }

        public bool SaveProfile(CardBuildProfile profile)
        {
            CardBuildProfileValidationResult validation = ValidateProfile(profile);
            if (!validation.IsValid)
            {
                return false;
            }

            CardBuildProfileFile file = ReadProfileFile();
            file.profiles.RemoveAll(existing => existing.character_id == profile.character_id);
            file.profiles.Add(profile);

            Directory.CreateDirectory(Path.GetDirectoryName(profilePath));
            File.WriteAllText(profilePath, JsonUtility.ToJson(file, true));
            return true;
        }

        public CardBuildProfile LoadProfile(string characterId)
        {
            CardBuildProfileFile file = ReadProfileFile();
            CardBuildProfile stored = file.profiles.FirstOrDefault(profile => profile.character_id == characterId);
            if (stored != null && ValidateProfile(stored).IsValid)
            {
                return stored;
            }

            return CreateDefaultProfile(characterId);
        }

        private CardBuildProfileFile ReadProfileFile()
        {
            if (!File.Exists(profilePath))
            {
                return new CardBuildProfileFile();
            }

            CardBuildProfileFile parsed = JsonUtility.FromJson<CardBuildProfileFile>(File.ReadAllText(profilePath));
            return parsed ?? new CardBuildProfileFile();
        }
    }
}
