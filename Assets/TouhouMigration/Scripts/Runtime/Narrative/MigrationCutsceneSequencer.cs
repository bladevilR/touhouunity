using System.Collections.Generic;

namespace TouhouMigration.Runtime.Narrative
{
    // One authored cutscene step (Godot CutsceneSequencer step dict).
    public sealed class MigrationCutsceneStep
    {
        public string Type = string.Empty;       // "text" | "wait" | "fade_out" | "fade_in"
        public string Speaker = string.Empty;
        public string Text = string.Empty;
        public double Seconds = 1.5;
    }

    // A dialogue line shown during a cutscene.
    public readonly struct CutsceneLine
    {
        public CutsceneLine(string speaker, string text)
        {
            Speaker = speaker;
            Text = text;
        }

        public string Speaker { get; }
        public string Text { get; }
    }

    // Timed playback of authored cutscene steps (Godot CutsceneSequencer play / _run_step): a text step
    // shows its line then waits its duration; wait/fade steps just wait; the sequence finishes after the
    // last step. UnityEngine-free + unit-testable via Tick (the SceneManager fade calls + UI rendering are
    // scene wiring — fades are modelled as plain waits).
    public sealed class MigrationCutsceneSequencer
    {
        private readonly List<CutsceneLine> shownLines = new List<CutsceneLine>();
        private IReadOnlyList<MigrationCutsceneStep> steps = System.Array.Empty<MigrationCutsceneStep>();
        private double remaining;

        public bool IsPlaying { get; private set; }
        public bool IsFinished { get; private set; }
        public int CurrentStepIndex { get; private set; } = -1;
        public IReadOnlyList<CutsceneLine> ShownLines => shownLines;

        public void Play(IReadOnlyList<MigrationCutsceneStep> steps)
        {
            this.steps = steps ?? System.Array.Empty<MigrationCutsceneStep>();
            shownLines.Clear();
            CurrentStepIndex = -1;
            IsFinished = false;
            IsPlaying = true;
            EnterStep(0);
        }

        public void Tick(double deltaSeconds)
        {
            if (!IsPlaying)
            {
                return;
            }

            remaining -= System.Math.Max(0.0, deltaSeconds);
            while (IsPlaying && remaining <= 0.0)
            {
                double carry = remaining; // negative excess carries into the next step
                EnterStep(CurrentStepIndex + 1);
                if (IsPlaying)
                {
                    remaining += carry;
                }
            }
        }

        private void EnterStep(int index)
        {
            if (index >= steps.Count)
            {
                IsPlaying = false;
                IsFinished = true;
                return;
            }

            CurrentStepIndex = index;
            MigrationCutsceneStep step = steps[index];
            if (step.Type == "text")
            {
                shownLines.Add(new CutsceneLine(step.Speaker ?? string.Empty, step.Text ?? string.Empty));
            }

            remaining = System.Math.Max(0.0, step.Seconds);
        }
    }
}
