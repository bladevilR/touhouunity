using System;

namespace TouhouMigration.Runtime.Foundation
{
    public enum MigrationLogLevel { Debug = 0, Info = 1, Warning = 2, Error = 3 }

    // Structured, level-gated logging (Godot GameLogger): messages below CurrentLevel are dropped; the rest
    // format as "[LEVEL][tag] message" and go to the injected sink (a UI/Debug layer binds it).
    // UnityEngine-free + unit-testable; the sink routes to UnityEngine.Debug in the app.
    public sealed class MigrationGameLogger
    {
        private readonly Action<MigrationLogLevel, string> sink;

        public MigrationGameLogger(Action<MigrationLogLevel, string> sink = null)
        {
            this.sink = sink;
        }

        public MigrationLogLevel CurrentLevel { get; set; } = MigrationLogLevel.Debug;
        public int EmittedCount { get; private set; }
        public string LastLine { get; private set; } = string.Empty;

        public void Debug(string tag, string message) => Log(MigrationLogLevel.Debug, tag, message);
        public void Info(string tag, string message) => Log(MigrationLogLevel.Info, tag, message);
        public void Warning(string tag, string message) => Log(MigrationLogLevel.Warning, tag, message);
        public void Error(string tag, string message) => Log(MigrationLogLevel.Error, tag, message);

        private void Log(MigrationLogLevel level, string tag, string message)
        {
            if (level < CurrentLevel)
            {
                return;
            }

            string line = $"[{level.ToString().ToUpperInvariant()}][{tag}] {message}";
            LastLine = line;
            EmittedCount++;
            sink?.Invoke(level, line);
        }
    }
}
