using System;

namespace TouhouMigration.Runtime.UI
{
    // A notification's colour (Godot Color), kept UnityEngine-free for testability.
    public readonly struct MigrationNotificationColor
    {
        public MigrationNotificationColor(float r, float g, float b, float a = 1f)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public float R { get; }
        public float G { get; }
        public float B { get; }
        public float A { get; }

        public static MigrationNotificationColor White => new MigrationNotificationColor(1f, 1f, 1f, 1f);
    }

    // Dispatches notifications to a bound handler (Godot NotificationService notify / bind_signal_bus /
    // unbind / is_signal_bus_bound): notify always records the message and, when a handler is bound, fires
    // it with the message + colour. UnityEngine-free + unit-testable; a UI layer binds the handler.
    public sealed class MigrationNotificationService
    {
        private Action<string, MigrationNotificationColor> handler;

        public bool IsBound => handler != null;
        public int NotificationCount { get; private set; }
        public string LastMessage { get; private set; } = string.Empty;

        public bool Bind(Action<string, MigrationNotificationColor> handler)
        {
            if (handler == null)
            {
                return false;
            }

            this.handler = handler;
            return true;
        }

        public void Unbind()
        {
            handler = null;
        }

        public void Notify(string message)
        {
            Notify(message, MigrationNotificationColor.White);
        }

        public void Notify(string message, MigrationNotificationColor color)
        {
            LastMessage = message ?? string.Empty;
            NotificationCount++;
            handler?.Invoke(LastMessage, color);
        }
    }
}
