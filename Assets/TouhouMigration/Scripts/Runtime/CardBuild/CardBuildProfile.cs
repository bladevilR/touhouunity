using System;
using System.Collections.Generic;
using System.Linq;

namespace TouhouMigration.Runtime.CardBuild
{
    [Serializable]
    public sealed class CardBuildProfile
    {
        public int profile_version = 2;
        public string character_id = "fujiwara_no_mokou";
        public List<string> active_deck = new List<string>();
        public List<CardBuildLoadoutEntry> action_loadout = new List<CardBuildLoadoutEntry>();

        public string CharacterId => character_id;
        public IReadOnlyList<string> ActiveDeck => active_deck;
        public IReadOnlyDictionary<string, string> ActionLoadout => action_loadout.ToDictionary(entry => entry.slot, entry => entry.card_id);

        public static CardBuildProfile Create(
            string characterId,
            IEnumerable<string> activeDeck,
            IReadOnlyDictionary<string, string> actionLoadout)
        {
            CardBuildProfile profile = new CardBuildProfile
            {
                character_id = characterId,
                active_deck = activeDeck.ToList(),
                action_loadout = actionLoadout
                    .Select(pair => new CardBuildLoadoutEntry { slot = pair.Key, card_id = pair.Value })
                    .ToList()
            };
            return profile;
        }
    }

    [Serializable]
    public sealed class CardBuildLoadoutEntry
    {
        public string slot;
        public string card_id;
    }

    public sealed class CardBuildProfileValidationResult
    {
        public CardBuildProfileValidationResult(bool isValid, IReadOnlyList<string> errors)
        {
            IsValid = isValid;
            Errors = errors;
        }

        public bool IsValid { get; }
        public IReadOnlyList<string> Errors { get; }
    }

    [Serializable]
    internal sealed class CardBuildProfileFile
    {
        public List<CardBuildProfile> profiles = new List<CardBuildProfile>();
    }
}
