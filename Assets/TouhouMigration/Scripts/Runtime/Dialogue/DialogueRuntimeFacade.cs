using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Dialogue
{
    public sealed class DialogueRuntimeFacade
    {
        private readonly DialogueRuntime runtime = new DialogueRuntime();
        private int sessionId;
        private int nextSessionId = 1;

        public event Action<string, int> DialogueStarted;
        public event Action<DialogueViewModel> ViewModelChanged;
        public event Action<string, int, int, DialogueChoice> ChoiceCommitted;
        public event Action<string, Dictionary<string, object>> ActionRequested;
        public event Action<Dictionary<string, object>> DialogueFinished;

        public bool ApplyActions { get; set; } = true;
        public bool IsActive => runtime.IsActive;
        public int LastCommittedChoiceIndex { get; private set; } = -1;
        public string LastActionId { get; private set; } = string.Empty;
        public Dictionary<string, object> LastActionPayload { get; private set; } = new Dictionary<string, object>();

        public DialogueRuntimeFacade()
        {
            runtime.ViewModelChanged += OnRuntimeViewModelChanged;
            runtime.ChoiceCommitted += OnRuntimeChoiceCommitted;
            runtime.ActionRequested += OnRuntimeActionRequested;
            runtime.DialogueFinished += OnRuntimeDialogueFinished;
        }

        public int StartLines(string npcId, IEnumerable<DialogueLine> lines)
        {
            Cancel("replaced");
            sessionId = nextSessionId++;
            LastCommittedChoiceIndex = -1;
            LastActionId = string.Empty;
            LastActionPayload.Clear();

            runtime.StartLines(npcId, lines);
            if (!runtime.IsActive)
            {
                sessionId = 0;
                return 0;
            }

            DialogueStarted?.Invoke(npcId, sessionId);
            return sessionId;
        }

        public bool Advance()
        {
            return runtime.Advance();
        }

        public bool Choose(int choiceIndex)
        {
            if (!runtime.IsActive)
            {
                return false;
            }

            DialogueViewModel view = runtime.GetViewModel();
            if (choiceIndex < 0 || choiceIndex >= view.Choices.Count)
            {
                return false;
            }

            LastCommittedChoiceIndex = choiceIndex;
            return runtime.Choose(choiceIndex);
        }

        public bool ChooseForSession(int requestedSessionId, int choiceIndex)
        {
            if (requestedSessionId <= 0 || requestedSessionId != sessionId)
            {
                return false;
            }

            return Choose(choiceIndex);
        }

        public bool ApplyEffects(string npcId, Dictionary<string, object> effects, DialogueChoice choice = null)
        {
            bool handledAny = false;
            foreach (KeyValuePair<string, object> effect in effects)
            {
                Dictionary<string, object> payload = new Dictionary<string, object>
                {
                    ["npc_id"] = npcId,
                    ["value"] = effect.Value,
                    ["choice"] = choice != null ? choice.Clone() : new DialogueChoice(),
                    ["session_id"] = sessionId
                };
                LastActionId = effect.Key;
                LastActionPayload = payload;
                ActionRequested?.Invoke(effect.Key, payload);
                handledAny = ApplyActions || handledAny;
            }

            return handledAny;
        }

        public void Cancel(string reason = "cancelled")
        {
            if (runtime.IsActive)
            {
                runtime.Cancel(reason);
            }
        }

        public DialogueViewModel GetViewModel()
        {
            DialogueViewModel model = runtime.GetViewModel();
            model.SessionId = sessionId;
            return model;
        }

        private void OnRuntimeViewModelChanged(DialogueViewModel model)
        {
            model.SessionId = sessionId;
            ViewModelChanged?.Invoke(model);
        }

        private void OnRuntimeChoiceCommitted(DialogueChoice choice)
        {
            ChoiceCommitted?.Invoke(runtime.NpcId, sessionId, LastCommittedChoiceIndex, choice.Clone());
        }

        private void OnRuntimeActionRequested(string actionId, Dictionary<string, object> payload)
        {
            Dictionary<string, object> scoped = new Dictionary<string, object>(payload)
            {
                ["session_id"] = sessionId
            };
            DialogueChoice choice = payload.TryGetValue("choice", out object rawChoice) && rawChoice is DialogueChoice typedChoice
                ? typedChoice
                : new DialogueChoice();
            ApplyEffects(Convert.ToString(scoped["npc_id"]) ?? runtime.NpcId, new Dictionary<string, object> { [actionId] = scoped["value"] }, choice);
        }

        private void OnRuntimeDialogueFinished(Dictionary<string, object> result)
        {
            Dictionary<string, object> scoped = new Dictionary<string, object>(result)
            {
                ["session_id"] = sessionId
            };
            DialogueFinished?.Invoke(scoped);
            sessionId = 0;
        }
    }
}
