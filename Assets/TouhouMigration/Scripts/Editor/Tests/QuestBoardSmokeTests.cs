using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.Quest;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers MigrationQuestBoard: the daily-quest refresh + accept logic (Godot QuestBoard
    // _refresh_daily_quests / get_available_quests / accept_quest).
    public static class QuestBoardSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Quest Board Smoke Tests")]
        public static void RunAll()
        {
            TestRefreshOffersDailyQuests();
            TestAcceptAvailableQuest();
            TestCannotAcceptAlreadyAccepted();
            TestCannotAcceptUnavailable();
            Debug.Log("Quest board smoke tests passed.");
        }

        private static bool Contains(IReadOnlyList<MigrationBoardQuest> quests, string id)
        {
            foreach (MigrationBoardQuest quest in quests)
            {
                if (quest.Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        private static void TestRefreshOffersDailyQuests()
        {
            MigrationQuestBoard board = new MigrationQuestBoard();
            board.RefreshDailyQuests(_ => 0);

            AssertEqual(3, board.AvailableQuests.Count, "A refresh offers the three daily quests.");
            AssertEqual(true, Contains(board.AvailableQuests, "daily_gather_bamboo"), "The bamboo daily is offered.");
            AssertEqual(true, board.IsAvailable("daily_fishing"), "The fishing daily is available.");
            AssertEqual(false, board.IsAvailable("main_keine_intro"), "A story quest is not a daily-board offer.");
        }

        private static void TestAcceptAvailableQuest()
        {
            MigrationQuestBoard board = new MigrationQuestBoard();
            board.RefreshDailyQuests(_ => 0);

            bool accepted = board.AcceptQuest("daily_gather_bamboo", _ => false);
            AssertEqual(true, accepted, "An available, un-accepted quest can be accepted.");
        }

        private static void TestCannotAcceptAlreadyAccepted()
        {
            MigrationQuestBoard board = new MigrationQuestBoard();
            board.RefreshDailyQuests(_ => 0);

            bool accepted = board.AcceptQuest("daily_gather_bamboo", id => id == "daily_gather_bamboo");
            AssertEqual(false, accepted, "A quest already held by the quest manager cannot be re-accepted.");
        }

        private static void TestCannotAcceptUnavailable()
        {
            MigrationQuestBoard board = new MigrationQuestBoard();
            board.RefreshDailyQuests(_ => 0);

            AssertEqual(false, board.AcceptQuest("not_on_board", _ => false), "A quest not on the board cannot be accepted.");
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
