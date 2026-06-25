using System;
using TouhouMigration.Runtime.Social;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationNpcRelationshipNetwork: the NPC-to-NPC relationship graph (Godot NPCRelationshipNetwork)
    // — order-independent pair keys, type/name lookups, friend/enemy classification, and clamped value modify.
    // Factions, gossip, and group events are deferred.
    public static class MigrationNpcRelationshipNetworkSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Migration NPC Relationship Smoke Tests")]
        public static void RunAll()
        {
            TestUnregisteredPairDefaultsToStranger();
            TestSymmetricKeyIgnoresOrder();
            TestFriendsClassification();
            TestEnemiesClassification();
            TestRelationshipNameMapping();
            TestModifyClampsAndStartsAtFifty();
            TestModifyUnregisteredPairStartsAtFifty();
            Debug.Log("Migration NPC relationship smoke tests passed.");
        }

        private static void TestUnregisteredPairDefaultsToStranger()
        {
            MigrationNpcRelationshipNetwork net = new MigrationNpcRelationshipNetwork();
            AssertEqual(NpcRelationType.Stranger, net.GetRelationshipType("a", "b"), "Unregistered pairs are strangers.");
            AssertEqual("陌生", net.GetRelationshipName("a", "b"), "Stranger relationship name.");
            AssertEqual(false, net.AreFriends("a", "b"), "Strangers are not friends.");
            AssertEqual(false, net.AreEnemies("a", "b"), "Strangers are not enemies.");
            AssertEqual(50, net.GetRelationshipValue("a", "b"), "An unregistered pair's value defaults to 50.");
        }

        private static void TestSymmetricKeyIgnoresOrder()
        {
            MigrationNpcRelationshipNetwork net = new MigrationNpcRelationshipNetwork();
            net.RegisterRelationship("reimu", "marisa", NpcRelationType.Friend, 70);
            AssertEqual(NpcRelationType.Friend, net.GetRelationshipType("marisa", "reimu"), "Relationship key is order-independent.");
            AssertEqual(true, net.AreFriends("marisa", "reimu"), "Friendship is symmetric.");
            AssertEqual(70, net.GetRelationshipValue("marisa", "reimu"), "The registered base value is order-independent.");
        }

        private static void TestFriendsClassification()
        {
            MigrationNpcRelationshipNetwork net = new MigrationNpcRelationshipNetwork();
            net.RegisterRelationship("a", "b", NpcRelationType.Friend, 60);
            net.RegisterRelationship("c", "d", NpcRelationType.CloseFriend, 90);
            net.RegisterRelationship("e", "f", NpcRelationType.Acquaintance, 55);
            AssertEqual(true, net.AreFriends("a", "b"), "Friend counts as friends.");
            AssertEqual(true, net.AreFriends("c", "d"), "CloseFriend counts as friends.");
            AssertEqual(false, net.AreFriends("e", "f"), "Acquaintance does not count as friends.");
        }

        private static void TestEnemiesClassification()
        {
            MigrationNpcRelationshipNetwork net = new MigrationNpcRelationshipNetwork();
            net.RegisterRelationship("a", "b", NpcRelationType.Enemy, -80);
            net.RegisterRelationship("c", "d", NpcRelationType.Rival, -30);
            net.RegisterRelationship("e", "f", NpcRelationType.Friend, 60);
            AssertEqual(true, net.AreEnemies("a", "b"), "Enemy counts as enemies.");
            AssertEqual(true, net.AreEnemies("c", "d"), "Rival counts as enemies.");
            AssertEqual(false, net.AreEnemies("e", "f"), "Friend does not count as enemies.");
        }

        private static void TestRelationshipNameMapping()
        {
            MigrationNpcRelationshipNetwork net = new MigrationNpcRelationshipNetwork();
            net.RegisterRelationship("a", "b", NpcRelationType.Romantic, 95);
            net.RegisterRelationship("c", "d", NpcRelationType.MasterStudent, 80);
            AssertEqual("恋人", net.GetRelationshipName("a", "b"), "Romantic relationship name.");
            AssertEqual("师徒", net.GetRelationshipName("c", "d"), "Master-student relationship name.");
        }

        private static void TestModifyClampsAndStartsAtFifty()
        {
            MigrationNpcRelationshipNetwork net = new MigrationNpcRelationshipNetwork();
            net.RegisterRelationship("a", "b", NpcRelationType.Acquaintance, 70);
            AssertEqual(70, net.GetRelationshipValue("a", "b"), "Before any modify, the value is the registered base.");
            net.ModifyRelationship("a", "b", 10);
            AssertEqual(60, net.GetRelationshipValue("a", "b"), "Modify seeds from 50 (Godot quirk), so +10 -> 60.");
            net.ModifyRelationship("a", "b", 100);
            AssertEqual(100, net.GetRelationshipValue("a", "b"), "The value clamps at 100.");
            net.ModifyRelationship("a", "b", -300);
            AssertEqual(-100, net.GetRelationshipValue("a", "b"), "The value clamps at -100.");
        }

        private static void TestModifyUnregisteredPairStartsAtFifty()
        {
            MigrationNpcRelationshipNetwork net = new MigrationNpcRelationshipNetwork();
            net.ModifyRelationship("x", "y", 20);
            AssertEqual(70, net.GetRelationshipValue("x", "y"), "Modifying an unregistered pair seeds from 50 (-> 70).");
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
