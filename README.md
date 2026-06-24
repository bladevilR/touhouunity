# Touhou Unity Migration

Independent Unity migration workspace for the Godot project at:

`/Users/Shared/Touhougodot`

This project is intentionally outside the Godot repository so Unity migration
can proceed in isolated slices.

## Current Scope

- Unity version: `6000.5.0f1`
- Migration root: `Assets/TouhouMigration`
- Current playable slice: title/bootstrap shell, Bamboo Home vertical slice,
  and Human Village stub.
- Unity-origin source packs are stored under `ExternalUnityAssets`.
- Runtime-ready migrated assets live under `Assets/TouhouMigration`.

## Directory Layout

- `Assets/TouhouMigration/Scenes`: migrated Unity scenes.
- `Assets/TouhouMigration/Scripts/Runtime`: runtime C# components and services.
- `Assets/TouhouMigration/Scripts/Editor`: migration/build helper tools.
- `Assets/TouhouMigration/Art`: copied or converted runtime art assets.
- `Assets/TouhouMigration/Prefabs`: migrated prefabs.
- `Assets/TouhouMigration/Data`: ScriptableObjects and converted data tables.
- `ExternalUnityAssets/unity_imports`: relocated original Unity packages and
  terrain exports; use this as the source warehouse, not as direct build
  content.
- `Docs`: migration notes and source inventory.

## First Editor Action

Open this folder as a Unity project, or run:

```bash
"/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit \
  -projectPath /Users/Shared/TouhouUnityMigration \
  -executeMethod TouhouMigration.Editor.TouhouMigrationProjectBuilder.BuildInitialProject
```

The command recreates the generated Bootstrap, TitleScreen,
BambooHomeVerticalSlice, and HumanVillageVerticalSlice scenes and registers
them in Build Settings.
