# NPC Dialogue Portraits — TODO(codex/image2)

This folder is the **portrait image slot** for the dialogue system. The code/data hooks are
wired (`MigrationPortraitCatalog` + `RuneDialogueController.PortraitTexture`); the **images
themselves are generated art and are Codex/image2's job** (per the project division of labor).

## Convention (what the catalog loads)

`MigrationPortraitCatalog.LoadPortrait(npcId, expression)` calls:

```
Resources.Load<Texture2D>("Portraits/<npcId>/<expression>")
```

So drop each portrait at:

```
Assets/TouhouMigration/Resources/Portraits/<npcId>/<expression>.png
```

`Resources.Load` resolves by path **without** the extension, so `.png` (import as a
`Texture2D`/Sprite-readable texture) works directly.

## Expressions (grounded in the migrated dialogue data `_npc_*.json`)

`neutral`, `happy`, `sad`, `surprised`, `angry`

- An empty/blank speaker line is **narration** → no portrait is shown.
- Any expression outside the set above falls back to `neutral` (see
  `MigrationPortraitCatalog.NormalizeExpression`; `mad→angry`, `upset→sad`,
  `surprise/shocked→surprised`).
- So a complete set per NPC is **5 PNGs**. `neutral` is the most important (it is the
  fallback for everything).

## NPC ids (35) — one folder each

```
akyuu alice aya beizi cirno eirin kaguya keine kogasa koishi kosuzu mamizou marisa
meiling miyoi mystia nitori patchouli reimu reisen remilia rin sakuya sanae satori
suika sumireko tenshi tewi utsuho youmu yukari yuuka yuuma yuyuko
```

Until a portrait exists the dialogue view simply renders no portrait (the layout is
unchanged), so partial delivery is safe — add NPCs/expressions incrementally.
