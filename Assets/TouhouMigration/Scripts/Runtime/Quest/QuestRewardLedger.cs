using System.Collections.Generic;
using System;

namespace TouhouMigration.Runtime.Quest
{
    public sealed class QuestRewardLedger
    {
        private readonly Dictionary<string, int> items = new Dictionary<string, int>();

        public int Exp { get; private set; }
        public int Coins { get; private set; }
        public IReadOnlyDictionary<string, int> Items => items;

        public void AddExp(int amount)
        {
            if (amount > 0)
            {
                Exp += amount;
            }
        }

        public void AddCoins(int amount)
        {
            if (amount > 0)
            {
                Coins += amount;
            }
        }

        public void AddItem(string itemId, int amount)
        {
            string normalizedItemId = NormalizeId(itemId);
            if (string.IsNullOrEmpty(normalizedItemId) || amount <= 0)
            {
                return;
            }

            items.TryGetValue(normalizedItemId, out int current);
            items[normalizedItemId] = current + amount;
        }

        public void ApplyRewards(QuestDefinition quest)
        {
            if (quest != null)
            {
                ApplyRewards(quest.Rewards);
            }
        }

        public void ApplyRewards(QuestRewardDefinition rewards)
        {
            if (rewards == null)
            {
                return;
            }

            AddExp(rewards.Exp);
            AddCoins(rewards.Coins);
            foreach (KeyValuePair<string, int> pair in rewards.Items)
            {
                AddItem(pair.Key, pair.Value);
            }
        }

        public int GetItemCount(string itemId)
        {
            return items.TryGetValue(NormalizeId(itemId), out int count) ? count : 0;
        }

        public QuestRewardLedgerSnapshot CreateSnapshot()
        {
            QuestRewardLedgerSnapshot snapshot = new QuestRewardLedgerSnapshot
            {
                Exp = Exp,
                Coins = Coins
            };
            foreach (KeyValuePair<string, int> pair in items)
            {
                snapshot.Items.Add(new QuestRewardItemSnapshot
                {
                    ItemId = pair.Key,
                    Amount = pair.Value
                });
            }

            return snapshot;
        }

        public void LoadSnapshot(QuestRewardLedgerSnapshot snapshot)
        {
            Exp = 0;
            Coins = 0;
            items.Clear();
            if (snapshot == null)
            {
                return;
            }

            Exp = snapshot.Exp;
            Coins = snapshot.Coins;
            foreach (QuestRewardItemSnapshot item in snapshot.Items)
            {
                AddItem(item.ItemId, item.Amount);
            }
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }

    [Serializable]
    public sealed class QuestRewardLedgerSnapshot
    {
        public int exp;
        public int coins;
        public List<QuestRewardItemSnapshot> items = new List<QuestRewardItemSnapshot>();

        public int Exp
        {
            get => exp;
            set => exp = value;
        }

        public int Coins
        {
            get => coins;
            set => coins = value;
        }

        public List<QuestRewardItemSnapshot> Items => items ??= new List<QuestRewardItemSnapshot>();
    }

    [Serializable]
    public sealed class QuestRewardItemSnapshot
    {
        public string item_id = string.Empty;
        public int amount;

        public string ItemId
        {
            get => item_id;
            set => item_id = value ?? string.Empty;
        }

        public int Amount
        {
            get => amount;
            set => amount = value;
        }
    }
}
