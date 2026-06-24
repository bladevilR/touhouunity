using System.Collections.Generic;

namespace TouhouMigration.Runtime.Dialogue
{
    public sealed class DialogueLine
    {
        public string Speaker { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Expression { get; set; } = "neutral";
        public List<DialogueChoice> Choices { get; set; } = new List<DialogueChoice>();

        public DialogueLine Clone()
        {
            List<DialogueChoice> choices = new List<DialogueChoice>();
            foreach (DialogueChoice choice in Choices)
            {
                choices.Add(choice.Clone());
            }

            return new DialogueLine
            {
                Speaker = Speaker,
                Text = Text,
                Expression = Expression,
                Choices = choices
            };
        }
    }
}
