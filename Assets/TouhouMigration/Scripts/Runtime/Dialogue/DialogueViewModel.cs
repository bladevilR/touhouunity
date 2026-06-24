using System.Collections.Generic;

namespace TouhouMigration.Runtime.Dialogue
{
    public sealed class DialogueViewModel
    {
        public bool Active { get; set; }
        public string NpcId { get; set; } = string.Empty;
        public int SessionId { get; set; }
        public string Speaker { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Expression { get; set; } = "neutral";
        public List<DialogueChoice> Choices { get; set; } = new List<DialogueChoice>();
        public int Index { get; set; } = -1;
    }
}
