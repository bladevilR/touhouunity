# Codex / image2 交接 —— 美术 + 音频 + 模型

**写给：** Codex（image2）—— 负责生成图像/音频/模型。
**写者：** Claude（负责代码 + 数据钩子；不碰生成式资源）。
**日期：** 2026-06-26 · `origin/main` `7c13fb7` · 回归 136/136 全绿。

## 分工原则（项目既定）
> 任何需要生成图像/纹理/UI 美术/VFX/音频/模型的功能，代码与数据钩子由 Claude 接好并留空槽；**资源本体是 Codex 的活**。

每个槽都是 **null-safe**：资源没到位时，场景照常跑、只是不显示该资源（布局不变）。所以**可以增量交付**——做一个补一个，不会破坏构建。

---

## ✅ 已就绪、可直接开工的槽（已有详细约定文档）

### 1. NPC 对话立绘 —— `Resources/Portraits/README.md`
- 代码钩子：`MigrationPortraitCatalog` + `RuneDialogueController.PortraitTexture`（已接好）。
- 放置：`Assets/TouhouMigration/Resources/Portraits/<npcId>/<expression>.png`
- 表情（5 个）：`neutral` `happy` `sad` `surprised` `angry`（`neutral` 最重要，是兜底）。
- **35 个 NPC**（akyuu alice aya beizi cirno eirin kaguya keine kogasa koishi kosuzu mamizou marisa meiling miyoi mystia nitori patchouli reimu reisen remilia rin sakuya sanae satori suika sumireko tenshi tewi utsuho youmu yukari yuuka yuuma yuyuko）。
- 完整 = 35 × 5 = **175 张 PNG**。细节看那个 README。

### 2. 音频 BGM + SFX —— `Resources/Audio/README.md`
- 代码钩子：`MigrationAudioCatalog`（名→键）+ `MigrationAudioManager`（播放），已接好。
- 放置：`Assets/TouhouMigration/Resources/Audio/BGM/<key>.ogg` 和 `.../SFX/<category>/<key>.ogg`。
- BGM 键：`town_theme` / `a_new_town` / `battle_theme_a`（场景→曲目路由在 `MigrationAudioCatalog.SceneBgm`）。
- SFX 分类：`combat/` `footsteps/` `ui/` `rpg/` `jingles/`（含 quest_complete / level_up / item_obtained / bond_up）。完整键表看那个 README + `MigrationAudioCatalog`。

---

## 🟡 需要 Codex 但**还没有专门约定文档**的槽（本次新增说明）

### 3. 角色 3D 模型（最大缺口）
- **现状**：只有**妹红 Fujiwara no Mokou** 做成了 Humanoid（`Art/Characters/Mokou` + `Animations/Characters/MokouValidation/*.fbx`）。其余角色无模型。
- **谁需要模型**：
  - **可玩角色**（`Data/CardBuild/characters.json` 36 个；`MigrationCharacterCatalog` 的 6 个 VS 角色：reimu / mokou✓ / marisa / sakuya / yuma / koishi）。
  - **村庄 NPC**（`Data/Npc/human_village_roster.json`，**26 条**都带 `model_path`，形如 `res://assets/characters/model/NEW/_tripo_native/<id>.glb`）。
- **格式/放置**：Unity 用 `.glb` 或 `.fbx`，导入为 **Humanoid** rig（这样能复用已有的 Kevin/Mokou 动画）。建议落到 `Assets/TouhouMigration/Art/Characters/<id>/<id>.glb`。
- **Claude 侧待办（模型到位后我接）**：把 roster 的 Godot `res://...glb` 路径重映射到 Unity 资源 + `MigrationNpcRoster` 的 spawn 接线（目前 roster 数据已加载，`MigrationNpcRosterReconciler` 已把 18 个对话 NPC 映射到 id、8 个纯模型背景 NPC 已分类）。**注意：玩家角色的 locomotion/绑定接线属于并发会话 `MigrationLocomotionAnimatorBridge`，那条线落地后再统一。**

### 4. 环境 / 场景美术（HD2D、地形、背景）
- `Art/Locations/<scene>/`（HakureiShrine、CirnoBossArena、MistyLake、CombatArenaHD2D…）+ `Art/HumanVillage/Terrain`。
- 当前是第三方/占位素材；HD2D 背景板、地形纹理、天空盒等可由 Codex 产出。
- **此项无独立数据钩子**——它直接是场景资源；属于"Unity 交互编辑器场景工作"，建议等场景/绑定那条线一起做，优先级低于立绘/音频/模型。

### 5. VFX（法术 / 战斗特效）
- Godot 侧有 `DustEffect`、`spawn_fire_trail`（`CharacterSkills`）、弹幕等粒子/特效。
- Unity 端无现成钩子（是场景/粒子系统工作）。**优先级最低**；等核心可玩性接好后再做。

---

## ❌ 不需要 Codex 的（澄清，省得误做）
- **卡面**：cardbuild 是 **IMGUI 文字牌**，`cards.json` 无 art 字段——**不需要卡图**。
- **物品/作物图标**：`items.json` 当前**无 icon 字段**，UI 用文字。若以后要图标，需 **Claude 先接一个 icon 钩子**，那之前 Codex 无需产出。
- **敌人美术**：`Art/Enemies/`（Bat/Bee/Bird…）已用第三方素材，已覆盖。

---

## 优先级建议
1. **立绘**（35×5，已就绪，影响所有对话）→ 先做 `neutral`，6 个核心角色（reimu/mokou/marisa/sakuya/cirno/koishi）优先。
2. **音频**（已就绪，BGM 3 首 + 核心 SFX 让场景有声）。
3. **角色模型**（妹红已做，可玩 6 角色其次，村庄 NPC 再次）。
4. 环境 / VFX（等场景线）。

## 给 Codex 的安全须知
- **不要改** `Assets/TouhouMigration/Scripts/`（代码钩子是 Claude 的，已接好且测试覆盖）。
- **不要改** 这 4 个并发会话在途文件：`TouhouMigrationProjectBuilder.cs`、`MigrationGlobalUiController.cs`、`ScenePortal.cs`、`MigrationLocomotionAnimatorBridge.cs`。
- 只往 `Resources/` 和 `Art/` 投放资源 + 必要的 `.meta`。增量提交，每批跑一次构建确认无导入报错即可。
- 完整代码/系统现状见 `Docs/CURRENT_HANDOFF.md`（含 92 刀逐条 + 191 类覆盖审计）。
