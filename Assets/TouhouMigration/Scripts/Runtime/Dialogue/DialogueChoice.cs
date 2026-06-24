using System.Collections.Generic;

namespace TouhouMigration.Runtime.Dialogue
{
    public sealed class DialogueChoice
    {
        public string Text { get; set; } = string.Empty;
        public Dictionary<string, object> Effects { get; set; } = new Dictionary<string, object>();
        public bool HasNextIndex { get; set; }
        public int NextIndex { get; set; }

        public DialogueChoice Clone()
        {
            return new DialogueChoice
            {
                Text = Text,
                Effects = new Dictionary<string, object>(Effects),
                HasNextIndex = HasNextIndex,
                NextIndex = NextIndex
            };
        }
    }
}
