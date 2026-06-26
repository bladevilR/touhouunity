using System;
using System.Collections.Generic;
using TouhouMigration.Runtime.CardBuild;
using UnityEditor;
using UnityEngine;

namespace TouhouMigration.Editor.Tests
{
    // Covers the mechanism-archetype per-card resolution (Godot _apply_cirno_card_resolution mechanism
    // branch): the seal economy, the binding/fantasy verdict payoffs, and the clause-lock rewrite.
    public static class CardResolutionMechanismSmokeTests
    {
        [MenuItem("Touhou Migration/Tests/Run Card Resolution Mechanism Smoke Tests")]
        public static void RunAll()
        {
            TestSealGenerators();
            TestEvidenceTokenScalesWithExposure();
            TestBindingVerdictGate();
            TestFantasyVerdictGateThenBurst();
            TestLegalTrap();
            Debug.Log("Card resolution mechanism smoke tests passed.");
        }

        private static MigrationCardBuildRunController NewRun()
        {
            return new MigrationCardBuildRunController(
                new List<string> { "a", "b" }, bossMaxHp: 540, bossClauseId: "cirno_domain");
        }

        private static void TestSealGenerators()
        {
            MigrationCardBuildRunController run = NewRun();
            run.ResolveCardEffect("mechanism_starter_first_seal");
            run.ResolveCardEffect("mechanism_movement_ritual_position");
            AssertEqual(2, run.State.GetResource("seal"), "First seal + ritual position each grant a seal.");
        }

        private static void TestEvidenceTokenScalesWithExposure()
        {
            MigrationCardBuildRunController unexposed = NewRun();
            unexposed.ResolveCardEffect("mechanism_resource_evidence_token");
            AssertEqual(1, unexposed.State.GetResource("seal"), "Evidence token grants 1 seal when the clause is hidden.");

            MigrationCardBuildRunController exposed = NewRun();
            exposed.Clauses.Install("cirno_domain", new[] { "field_replace" });
            exposed.Clauses.Expose("cirno_domain");
            exposed.ResolveCardEffect("mechanism_resource_evidence_token");
            AssertEqual(2, exposed.State.GetResource("seal"), "Evidence token grants 2 seal when the clause is exposed.");
        }

        private static void TestBindingVerdictGate()
        {
            MigrationCardBuildRunController low = NewRun();
            low.State.AddResource("seal", 1);
            low.ResolveCardEffect("mechanism_payoff_binding_verdict"); // needs 2 -> waits
            AssertEqual(0, low.RewrittenRuleCount, "Binding verdict waits below 2 seal.");
            AssertEqual(1, low.State.GetResource("seal"), "A waiting binding verdict spends nothing.");

            MigrationCardBuildRunController ok = NewRun();
            ok.State.AddResource("seal", 2);
            ok.ResolveCardEffect("mechanism_payoff_binding_verdict");
            AssertEqual(1, ok.RewrittenRuleCount, "Binding verdict rewrites a rule.");
            AssertEqual(1, ok.State.GetResource("seal"), "Binding verdict spends one seal.");
            AssertEqual(true, ok.IsVulnerabilityOpen, "Binding verdict opens vulnerability.");
        }

        private static void TestFantasyVerdictGateThenBurst()
        {
            MigrationCardBuildRunController low = NewRun();
            low.State.AddResource("seal", 2);
            low.ResolveCardEffect("mechanism_terminal_fantasy_verdict"); // total 2 < 3 -> waits
            AssertEqual(540, low.BossHp, "Fantasy verdict waits below a seal-total of 3.");

            // seal res 2 + rewritten 1 = 3: damage = 96 + 3*24 = 168.
            MigrationCardBuildRunController ok = NewRun();
            ok.State.AddResource("seal", 2);
            ok.RewrittenRuleCount = 1;
            ok.ResolveCardEffect("mechanism_terminal_fantasy_verdict");
            AssertEqual(540 - 168, ok.BossHp, "Fantasy verdict burst = 96 + total*24.");
            AssertEqual(0, ok.State.GetResource("seal"), "Fantasy verdict spends all seal.");
            AssertEqual(0, ok.TerrainPressure, "Fantasy verdict zeroes terrain pressure.");
        }

        private static void TestLegalTrap()
        {
            MigrationCardBuildRunController run = NewRun(); // terrain pressure 2
            run.ResolveCardEffect("mechanism_risk_legal_trap");
            AssertEqual(3, run.TerrainPressure, "Legal trap raises terrain pressure by 1.");
            AssertEqual(2, run.State.GetResource("seal"), "Legal trap grants 2 seal.");
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
