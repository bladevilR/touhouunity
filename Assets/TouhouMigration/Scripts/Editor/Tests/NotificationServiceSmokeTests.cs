using System;
using TouhouMigration.Runtime.UI;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationNotificationService: the notify -> bound-handler dispatch + bind/unbind lifecycle
    // (Godot NotificationService notify / bind_signal_bus / unbind_signal_bus / is_signal_bus_bound).
    public static class NotificationServiceSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Notification Service Smoke Tests")]
        public static void RunAll()
        {
            TestNotifyDispatchesToBoundHandler();
            TestUnbindStopsDispatch();
            TestNotifyAlwaysRecorded();
            Debug.Log("Notification service smoke tests passed.");
        }

        private static void TestNotifyDispatchesToBoundHandler()
        {
            MigrationNotificationService service = new MigrationNotificationService();
            AssertEqual(false, service.IsBound, "A fresh service is unbound.");

            string seen = null;
            MigrationNotificationColor seenColor = default;
            AssertEqual(true, service.Bind((msg, color) => { seen = msg; seenColor = color; }), "Binding a handler succeeds.");
            AssertEqual(true, service.IsBound, "It is now bound.");

            service.Notify("quest complete", new MigrationNotificationColor(0f, 1f, 0f));
            AssertEqual("quest complete", seen, "The bound handler receives the message.");
            AssertEqual(1f, seenColor.G, "The handler receives the colour.");
        }

        private static void TestUnbindStopsDispatch()
        {
            MigrationNotificationService service = new MigrationNotificationService();
            int calls = 0;
            service.Bind((msg, color) => calls++);
            service.Notify("a");
            service.Unbind();
            AssertEqual(false, service.IsBound, "Unbinding clears the binding.");
            service.Notify("b"); // no handler now
            AssertEqual(1, calls, "After unbind the handler no longer receives notifications.");
        }

        private static void TestNotifyAlwaysRecorded()
        {
            MigrationNotificationService service = new MigrationNotificationService();
            // Notify works (records) even with no handler bound (Godot emits the signal regardless).
            service.Notify("item obtained");
            AssertEqual(1, service.NotificationCount, "A notification is recorded even when unbound.");
            AssertEqual("item obtained", service.LastMessage, "The last message is tracked.");

            service.Notify("level up", MigrationNotificationColor.White);
            AssertEqual(2, service.NotificationCount, "Each notify increments the count.");
            AssertEqual("level up", service.LastMessage, "The latest message is tracked.");
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new Exception($"{message} Expected: {expected}. Actual: {actual}.");
            }
        }
    }
}
