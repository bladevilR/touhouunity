using System.Collections.Generic;
using UnityEngine;

namespace TouhouMigration.Runtime.UI.Dialogue
{
    // The E5 portrait presentation hook: maps a dialogue speaker (npc id + expression) to its portrait
    // image lookup. The portrait PNGs themselves are generated art and are left to Codex/image2 — they
    // drop into Resources/Portraits/<npcId>/<expression>.png. This catalog only resolves the lookup key
    // and the expression fallback so the dialogue view has a ready slot to render into; key/normalization
    // logic is UnityEngine-free and unit-tested, and LoadPortrait returns null until the art exists.
    //
    // Expression set is grounded in the migrated dialogue data (_npc_*.json speaker lines): neutral,
    // happy, sad, surprised, angry. Unknown/empty expressions fall back to neutral; a blank speaker is
    // narration (no portrait).
    public sealed class MigrationPortraitCatalog
    {
        public const string DefaultResourceRoot = "Portraits";

        public static readonly IReadOnlyList<string> StandardExpressions = new[]
        {
            "neutral",
            "happy",
            "sad",
            "surprised",
            "angry"
        };

        private static readonly HashSet<string> Standard = new HashSet<string>(StandardExpressions);

        private readonly string resourceRoot;

        public MigrationPortraitCatalog(string resourceRoot = DefaultResourceRoot)
        {
            this.resourceRoot = string.IsNullOrWhiteSpace(resourceRoot) ? DefaultResourceRoot : resourceRoot.Trim();
        }

        public string ResourceRoot => resourceRoot;

        // A blank/whitespace expression marks a narration line (no character is speaking) -> no portrait.
        public bool IsNarration(string speaker)
        {
            return string.IsNullOrWhiteSpace(speaker);
        }

        // Map a raw dialogue expression to one of the standard portrait expressions, normalizing the
        // synonyms the data/motion layer uses; anything unrecognized falls back to neutral.
        public string NormalizeExpression(string expression)
        {
            string e = (expression ?? string.Empty).Trim().ToLowerInvariant();
            e = e switch
            {
                "mad" => "angry",
                "upset" => "sad",
                "surprise" or "shocked" => "surprised",
                _ => e
            };

            return Standard.Contains(e) ? e : "neutral";
        }

        // The Resources.Load key for a speaker's portrait, e.g. "Portraits/reimu/happy". Returns null when
        // there is no speaker id (narration), so the view shows no portrait.
        public string ResolveResourceKey(string npcId, string expression)
        {
            if (string.IsNullOrWhiteSpace(npcId))
            {
                return null;
            }

            string npc = npcId.Trim().ToLowerInvariant();
            return $"{resourceRoot}/{npc}/{NormalizeExpression(expression)}";
        }

        // Load the portrait texture from Resources. Returns null until the generated art exists
        // (Codex/image2 fills Resources/Portraits/<npc>/<expression>.png).
        public Texture2D LoadPortrait(string npcId, string expression)
        {
            string key = ResolveResourceKey(npcId, expression);
            return string.IsNullOrEmpty(key) ? null : Resources.Load<Texture2D>(key);
        }
    }
}
