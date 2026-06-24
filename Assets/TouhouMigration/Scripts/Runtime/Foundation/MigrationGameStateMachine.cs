using System;
using System.Collections.Generic;

namespace TouhouMigration.Runtime.Foundation
{
    // Pure game-mode state machine. ChangeMode replaces the current mode; Push/Pop layer a transient
    // mode (e.g. Dialogue or Combat over Overworld) and restore the previous one. ModeChanged fires
    // only on an actual transition. The MonoBehaviour/scene-flow owner drives this in E2.
    public sealed class MigrationGameStateMachine
    {
        private readonly Stack<MigrationGameStateMode> stack = new Stack<MigrationGameStateMode>();

        public MigrationGameStateMode CurrentMode { get; private set; }
        public MigrationGameStateMode PreviousMode { get; private set; }
        public int Depth => stack.Count;

        public event Action<MigrationGameStateMode, MigrationGameStateMode> ModeChanged;

        public MigrationGameStateMachine(MigrationGameStateMode initial = MigrationGameStateMode.Menu)
        {
            CurrentMode = initial;
            PreviousMode = initial;
        }

        public bool ChangeMode(MigrationGameStateMode mode)
        {
            if (mode == CurrentMode)
            {
                return false;
            }
            SetMode(mode);
            return true;
        }

        public void Push(MigrationGameStateMode mode)
        {
            stack.Push(CurrentMode);
            if (mode != CurrentMode)
            {
                SetMode(mode);
            }
        }

        public bool Pop()
        {
            if (stack.Count == 0)
            {
                return false;
            }
            MigrationGameStateMode restored = stack.Pop();
            if (restored != CurrentMode)
            {
                SetMode(restored);
            }
            return true;
        }

        private void SetMode(MigrationGameStateMode mode)
        {
            PreviousMode = CurrentMode;
            CurrentMode = mode;
            ModeChanged?.Invoke(PreviousMode, CurrentMode);
        }
    }
}
