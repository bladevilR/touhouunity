using UnityEngine;
using UnityEngine.SceneManagement;

namespace TouhouMigration.Runtime.UI
{
    // A single navigable entry point that ties the five standalone domain demos together: an IMGUI menu that
    // loads each playable scene (E6 Cirno + E4 shop/farm/fishing + combat). The scenes are added to the build
    // settings by the hub's builder so LoadScene resolves them. Pure navigation glue — every domain's logic
    // lives in its own (unit-tested) services + drivers.
    public sealed class MigrationDemoHubDriver : MonoBehaviour
    {
        // Scene name -> label, in play order.
        private static readonly (string scene, string label)[] Demos =
        {
            ("MigrationCirnoBossPlayable", "E6 — Cirno Card-Fight"),
            ("MigrationShopPlayable", "E4 — Shop"),
            ("MigrationFarmPlayable", "E4 — Farm (economy loop)"),
            ("MigrationFishingPlayable", "E4 — Fishing"),
            ("MigrationCombatDemoPlayable", "Combat — Attack/Defeat/Loot"),
        };

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 360, 320), GUI.skin.box);
            GUILayout.Label("Touhou Migration — Playable Demos");
            GUILayout.Space(8);

            foreach ((string scene, string label) in Demos)
            {
                if (GUILayout.Button(label, GUILayout.Height(40)))
                {
                    SceneManager.LoadScene(scene);
                }
            }

            GUILayout.EndArea();
        }
    }
}
