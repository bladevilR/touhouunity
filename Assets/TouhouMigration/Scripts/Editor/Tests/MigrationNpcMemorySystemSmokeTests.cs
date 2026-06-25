using System;
using TouhouMigration.Runtime.Social;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationNpcMemorySystem: the NPC memory core (Godot NPCMemorySystem) — forming memories that
    // shift relationship aspects (trust/affection/respect/familiarity) and recompute the player impression,
    // plus capacity-limited memory storage and queries. Decay, save, and dialogue modifiers are deferred.
    public static class MigrationNpcMemorySystemSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Migration NPC Memory Smoke Tests")]
        public static void RunAll()
        {
            TestNewNpcStartsUnknownWithDefaultAspects();
            TestGiftRaisesAffection();
            TestBetrayalLowersTrustDoubleAndAffection();
            TestRepeatedBetrayalBecomesHostile();
            TestQuestHelpRaisesTrustAndRespect();
            TestHasMemoryAndCount();
            TestMemoryCapacityEvictsWeakest();
            TestMemoryForgottenWhenWeightDropsBelowMinimum();
            TestGratitudeRateSlowsPositiveMemoryDecay();
            TestForgivenessRateSlowsNegativeMemoryDecay();
            Debug.Log("Migration NPC memory smoke tests passed.");
        }

        private static void TestNewNpcStartsUnknownWithDefaultAspects()
        {
            MigrationNpcMemorySystem memory = new MigrationNpcMemorySystem();
            AssertEqual(NpcImpression.Unknown, memory.GetImpression("reimu"), "A never-met NPC has an Unknown impression.");
            AssertEqual(50, memory.GetRelationshipAspect("reimu", "trust"), "Trust starts at 50.");
            AssertEqual(0, memory.GetRelationshipAspect("reimu", "familiarity"), "Familiarity starts at 0.");
        }

        private static void TestGiftRaisesAffection()
        {
            MigrationNpcMemorySystem memory = new MigrationNpcMemorySystem();
            memory.AddMemory("reimu", NpcMemoryType.GiftReceived);
            // gift weight 30 -> change +3 to affection (50 -> 53)
            AssertEqual(53, memory.GetRelationshipAspect("reimu", "affection"), "A received gift raises affection by weight/10.");
            AssertEqual(1, memory.GetMemoryCount("reimu"), "Adding a memory increases the memory count.");
        }

        private static void TestBetrayalLowersTrustDoubleAndAffection()
        {
            MigrationNpcMemorySystem memory = new MigrationNpcMemorySystem();
            memory.AddMemory("reimu", NpcMemoryType.Betrayal);
            // betrayal weight 150 -> change -15; trust gets 2x (-30 -> 20), affection -15 (-> 35)
            AssertEqual(20, memory.GetRelationshipAspect("reimu", "trust"), "Betrayal lowers trust by double the change.");
            AssertEqual(35, memory.GetRelationshipAspect("reimu", "affection"), "Betrayal lowers affection by the change.");
        }

        private static void TestRepeatedBetrayalBecomesHostile()
        {
            MigrationNpcMemorySystem memory = new MigrationNpcMemorySystem();
            memory.AddMemory("reimu", NpcMemoryType.Betrayal);
            memory.AddMemory("reimu", NpcMemoryType.Betrayal); // trust -> 0
            AssertEqual(0, memory.GetRelationshipAspect("reimu", "trust"), "Two betrayals drive trust to zero.");
            AssertEqual(NpcImpression.Hostile, memory.GetImpression("reimu"), "Very low trust makes the impression Hostile.");
            AssertEqual("敌对", memory.GetImpressionName("reimu"), "Hostile impression name.");
        }

        private static void TestQuestHelpRaisesTrustAndRespect()
        {
            MigrationNpcMemorySystem memory = new MigrationNpcMemorySystem();
            memory.AddMemory("keine", NpcMemoryType.QuestHelp);
            // quest help weight 50 -> change +5: trust +5 (-> 55), respect +2.5 (-> 52.5 -> int 52)
            AssertEqual(55, memory.GetRelationshipAspect("keine", "trust"), "Quest help raises trust by the change.");
            AssertEqual(52, memory.GetRelationshipAspect("keine", "respect"), "Quest help raises respect by half the change.");
        }

        private static void TestHasMemoryAndCount()
        {
            MigrationNpcMemorySystem memory = new MigrationNpcMemorySystem();
            memory.AddMemory("marisa", NpcMemoryType.GiftReceived);
            memory.AddMemory("marisa", NpcMemoryType.CombatTogether);
            AssertEqual(2, memory.GetMemoryCount("marisa"), "Two memories are counted.");
            AssertEqual(true, memory.HasMemoryOf("marisa", NpcMemoryType.GiftReceived), "A formed memory type is recalled.");
            AssertEqual(false, memory.HasMemoryOf("marisa", NpcMemoryType.Betrayal), "An unformed memory type is not recalled.");
        }

        private static void TestMemoryCapacityEvictsWeakest()
        {
            MigrationNpcMemorySystem memory = new MigrationNpcMemorySystem();
            // koishi's memory capacity is 10; adding 11 keeps only 10 (the weakest is evicted).
            for (int i = 0; i < 11; i++)
            {
                memory.AddMemory("koishi", NpcMemoryType.RepeatedVisit);
            }

            AssertEqual(10, memory.GetMemoryCount("koishi"), "Memory storage is capped at the NPC's capacity.");
        }

        private static void TestMemoryForgottenWhenWeightDropsBelowMinimum()
        {
            MigrationNpcMemorySystem memory = new MigrationNpcMemorySystem();
            // "youmu" uses the default personality (gratitude 1.0); a RepeatedVisit (weight 15, positive)
            // decays 2.0/day, so 15 -> 5 after 5 days (kept) -> 3 after 6 (forgotten, below the min of 5).
            memory.AddMemory("youmu", NpcMemoryType.RepeatedVisit);
            for (int i = 0; i < 5; i++)
            {
                memory.DecayAllMemories();
            }

            AssertEqual(1, memory.GetMemoryCount("youmu"), "A memory still at the minimum weight is not yet forgotten.");
            memory.DecayAllMemories();
            AssertEqual(0, memory.GetMemoryCount("youmu"), "A memory below the minimum weight is forgotten.");
        }

        private static void TestGratitudeRateSlowsPositiveMemoryDecay()
        {
            MigrationNpcMemorySystem memory = new MigrationNpcMemorySystem();
            memory.AddMemory("koishi", NpcMemoryType.RepeatedVisit);  // gratitude 0.5 -> positive decay 4.0/day
            memory.AddMemory("kaguya", NpcMemoryType.RepeatedVisit);  // gratitude 1.5 -> positive decay ~1.33/day
            for (int i = 0; i < 3; i++)
            {
                memory.DecayAllMemories();
            }

            AssertEqual(0, memory.GetMemoryCount("koishi"), "An ungrateful NPC forgets a good memory quickly (15-12=3).");
            AssertEqual(1, memory.GetMemoryCount("kaguya"), "A grateful NPC keeps a good memory longer (15-4=11).");
        }

        private static void TestForgivenessRateSlowsNegativeMemoryDecay()
        {
            MigrationNpcMemorySystem memory = new MigrationNpcMemorySystem();
            NpcMemoryContext disliked = new NpcMemoryContext { Liked = false };
            memory.AddMemory("marisa", NpcMemoryType.GiftReceived, disliked);  // forgiveness 0.9 -> neg decay 1.8/day
            memory.AddMemory("sakuya", NpcMemoryType.GiftReceived, disliked);  // forgiveness 0.3 -> neg decay 0.6/day
            for (int i = 0; i < 14; i++)
            {
                memory.DecayAllMemories();
            }

            AssertEqual(0, memory.GetMemoryCount("marisa"), "A forgiving NPC lets go of a grudge (30-25.2=4.8).");
            AssertEqual(1, memory.GetMemoryCount("sakuya"), "An unforgiving NPC holds the grudge (30-8.4=21.6).");
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
