using System.Collections.Generic;

namespace TouhouMigration.Runtime.Narrative
{
    // A prologue beat (Godot PrologueBeat): a cutscene or gameplay step in the prologue sequence.
    public sealed class MigrationPrologueBeat
    {
        public string Id = string.Empty;
        public string Kind = "gameplay";   // "cutscene" | "gameplay"
        public string SceneKey = string.Empty;
        public string SpawnPoint = "PlayerStart";
        public string Objective = string.Empty;
    }

    // The prologue beat-progression state machine (Godot PrologueDirector start / advance): gameplay beats
    // wait for an external Advance, cutscene beats auto-advance through, and running past the last beat
    // completes the prologue. UnityEngine-free; the scene change / cutscene playback / guidance UI / quest
    // hand-off are scene wiring.
    public sealed class MigrationPrologueDirector
    {
        private IReadOnlyList<MigrationPrologueBeat> beats = System.Array.Empty<MigrationPrologueBeat>();

        public int CurrentIndex { get; private set; } = -1;
        public bool IsActive { get; private set; }
        public bool IsComplete { get; private set; }

        public MigrationPrologueBeat CurrentBeat =>
            CurrentIndex >= 0 && CurrentIndex < beats.Count ? beats[CurrentIndex] : null;

        public void Start(IReadOnlyList<MigrationPrologueBeat> beats)
        {
            this.beats = beats ?? System.Array.Empty<MigrationPrologueBeat>();
            CurrentIndex = -1;
            IsComplete = false;
            if (this.beats.Count == 0)
            {
                IsActive = false;
                return;
            }

            IsActive = true;
            EnterBeat(0);
        }

        public void Advance()
        {
            if (!IsActive)
            {
                return;
            }

            EnterBeat(CurrentIndex + 1);
        }

        // Enter a beat: past the end completes; a cutscene beat auto-advances through; a gameplay beat waits.
        private void EnterBeat(int index)
        {
            if (index >= beats.Count)
            {
                Complete();
                return;
            }

            CurrentIndex = index;
            if (beats[index].Kind == "cutscene")
            {
                EnterBeat(index + 1);
            }
        }

        private void Complete()
        {
            IsActive = false;
            IsComplete = true;
        }
    }
}
