using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Dialogue
{
    public sealed class DialogueRuntime
    {
        private readonly List<DialogueLine> lines = new List<DialogueLine>();
        private readonly List<int> history = new List<int>();
        private int index = -1;

        public event Action<DialogueViewModel> ViewModelChanged;
        public event Action<DialogueChoice> ChoiceCommitted;
        public event Action<string, Dictionary<string, object>> ActionRequested;
        public event Action<Dictionary<string, object>> DialogueFinished;

        public string NpcId { get; private set; } = string.Empty;
        public bool IsActive { get; private set; }
        public string LastFinishReason { get; private set; } = string.Empty;
        public DialogueChoice LastCommittedChoice { get; private set; }

        public void StartLines(string npcId, IEnumerable<DialogueLine> dialogueLines)
        {
            NpcId = npcId ?? string.Empty;
            lines.Clear();
            history.Clear();
            LastFinishReason = string.Empty;
            LastCommittedChoice = null;

            if (dialogueLines != null)
            {
                foreach (DialogueLine line in dialogueLines)
                {
                    lines.Add(line.Clone());
                }
            }

            index = 0;
            IsActive = lines.Count > 0;
            if (IsActive)
            {
                ViewModelChanged?.Invoke(GetViewModel());
            }
            else
            {
                Finish("empty");
            }
        }

        public DialogueViewModel GetViewModel()
        {
            if (!IsActive || index < 0 || index >= lines.Count)
            {
                return new DialogueViewModel
                {
                    Active = false,
                    NpcId = NpcId,
                    Index = index
                };
            }

            DialogueLine line = lines[index];
            return new DialogueViewModel
            {
                Active = true,
                NpcId = NpcId,
                Speaker = line.Speaker,
                Text = line.Text,
                Expression = string.IsNullOrWhiteSpace(line.Expression) ? "neutral" : line.Expression,
                Choices = CloneChoices(line.Choices),
                Index = index
            };
        }

        public bool Advance()
        {
            if (!IsActive)
            {
                return false;
            }

            history.Add(index);
            index++;
            if (index >= lines.Count)
            {
                Finish("end");
                return true;
            }

            ViewModelChanged?.Invoke(GetViewModel());
            return true;
        }

        public bool Choose(int choiceIndex)
        {
            if (!IsActive || index < 0 || index >= lines.Count)
            {
                return false;
            }

            List<DialogueChoice> choices = lines[index].Choices;
            if (choiceIndex < 0 || choiceIndex >= choices.Count)
            {
                return false;
            }

            DialogueChoice choice = choices[choiceIndex].Clone();
            LastCommittedChoice = choice;
            ChoiceCommitted?.Invoke(choice.Clone());
            foreach (KeyValuePair<string, object> effect in choice.Effects)
            {
                ActionRequested?.Invoke(effect.Key, new Dictionary<string, object>
                {
                    ["npc_id"] = NpcId,
                    ["value"] = effect.Value,
                    ["choice"] = choice.Clone()
                });
            }

            history.Add(index);
            index = choice.HasNextIndex ? choice.NextIndex : index + 1;
            if (index < 0 || index >= lines.Count)
            {
                Finish("choice");
                return true;
            }

            ViewModelChanged?.Invoke(GetViewModel());
            return true;
        }

        public void Cancel(string reason = "cancelled")
        {
            if (!IsActive)
            {
                return;
            }

            Finish(reason);
        }

        private void Finish(string reason)
        {
            IsActive = false;
            LastFinishReason = reason;
            Dictionary<string, object> result = new Dictionary<string, object>
            {
                ["npc_id"] = NpcId,
                ["reason"] = reason,
                ["history"] = history.ToArray()
            };
            DialogueFinished?.Invoke(result);
            ViewModelChanged?.Invoke(GetViewModel());
        }

        private static List<DialogueChoice> CloneChoices(IEnumerable<DialogueChoice> choices)
        {
            List<DialogueChoice> cloned = new List<DialogueChoice>();
            if (choices == null)
            {
                return cloned;
            }

            foreach (DialogueChoice choice in choices)
            {
                cloned.Add(choice.Clone());
            }

            return cloned;
        }
    }
}
