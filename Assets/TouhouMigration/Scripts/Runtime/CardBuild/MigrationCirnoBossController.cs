using System.Collections.Generic;
using UnityEngine;

namespace TouhouMigration.Runtime.CardBuild
{
    // Scene driver for the Cirno card-fight (Godot CardBuildMvpRunController's MonoBehaviour role): a thin
    // MonoBehaviour over the tested MigrationCirnoBossSession — it builds the session on Start, ticks the
    // run each frame, and offers a minimal IMGUI card-play UI (boss HP + hand buttons). All gameplay logic
    // lives in the (unit-tested) session/run; this is presentation + input glue only.
    //
    // Placement into the CirnoBossArena scene is done by TouhouMigrationProjectBuilder (a concurrent-session
    // file); attach this component to a GameObject there once that lands.
    public sealed class MigrationCirnoBossController : MonoBehaviour
    {
        [SerializeField]
        private string cardsDataPath = "Assets/TouhouMigration/Data/CardBuild/cards.json";

        [SerializeField]
        private int openingHandSize = 5;

        // A Mokou starter deck (the cardbuild cards.json mokou card ids).
        private static readonly string[] StarterDeck =
        {
            "mokou_starter_fire_bird",
            "mokou_resource_hourai_embers",
            "mokou_attack_flame_fist",
            "mokou_payoff_fujiyama_burst",
            "mokou_defense_xu_fu_dimension",
            "mokou_movement_bamboo_escape",
            "mokou_draw_old_history_cinders",
            "mokou_boss_melt_the_lake",
            "mokou_terminal_hourai_doll",
            "mokou_risk_honest_mans_death",
        };

        private MigrationCirnoBossSession session;
        private string lastMessage = string.Empty;
        private readonly System.Random rng = new System.Random();

        private void Start()
        {
            MigrationCardEffectBlockParser parser = new MigrationCardEffectBlockParser();
            if (!parser.LoadFromPath(cardsDataPath))
            {
                lastMessage = "card data failed to load: " + string.Join("; ", parser.Errors);
            }

            session = new MigrationCirnoBossSession(parser, new List<string>(StarterDeck));
            session.StartFight(openingHandSize, max => rng.Next(max));
        }

        private void Update()
        {
            session?.Tick(Time.deltaTime);
        }

        private void OnGUI()
        {
            if (session == null)
            {
                return;
            }

            MigrationCardBuildRunController run = session.Run;
            GUILayout.BeginArea(new Rect(12, 12, 360, Screen.height - 24), GUI.skin.box);
            GUILayout.Label($"Cirno HP: {run.BossHp} / 540");
            GUILayout.Label($"Vulnerable: {(run.IsVulnerabilityOpen ? "OPEN" : "guarded")}   Terrain: {run.TerrainPressure}");
            GUILayout.Space(6);

            if (run.IsBossDefeated)
            {
                GUILayout.Label("CIRNO DEFEATED — mvp_clear");
            }
            else
            {
                GUILayout.Label("Hand:");
                foreach (string cardId in new List<string>(run.Deck.Hand))
                {
                    string suffix = run.IsCardOnCooldown(cardId) ? " (cooldown)" : string.Empty;
                    if (GUILayout.Button(cardId + suffix))
                    {
                        CardPlayResult result = session.PlayCardFromHand(cardId);
                        lastMessage = result.Success ? $"played {cardId}" : $"blocked: {result.Reason}";
                    }
                }
            }

            if (!string.IsNullOrEmpty(lastMessage))
            {
                GUILayout.Space(6);
                GUILayout.Label(lastMessage);
            }

            GUILayout.EndArea();
        }
    }
}
