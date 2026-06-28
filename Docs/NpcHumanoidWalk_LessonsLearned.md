# 人之里 NPC 走路动画：经验教训（2026-06-28）

把 token-rig 角色（灵梦/美玲/魔理沙）做成"会正常走路"的完整踩坑记录与最终方案。供后续/Codex 接手。

## 最终方案（已在人之里验收通过）

**真 Humanoid 重定向**，工具：`MigrationHumanoidRetargetNpcs.ApplyToScene`。

流程：
1. **Blender 把 token-rig GLB → FBX**（`scripts: /tmp/glb2fbx*.py`，`blender --background --python`）。
   - **必须删掉 GLB 里那颗杂散 `Icosphere`（42 verts）占位球**，否则 Blender 会把它一起导出，变成一个大灰圆顶罩住角色（glTFast 会隐藏它，Blender 不会）。只保留 `Armature` + 真 skinned mesh（`tripo_node_*`，~47k verts）。
   - 产物在 `Assets/RealModels/HumanoidNpc/{reimu,meiling,marisa_blackwhite}.fbx`。
2. **FBX 进 Unity 设 `animationType = Human` + `CreateFromThisModel`**。token-rig 用标准骨名（Hips/Spine/LeftUpperArm…），导入器**自动映射一次过**（valid + isHuman），并做 **Enforce T-Pose + 轴标定**——这一步是关键。
3. **任何 humanoid clip 直接重定向**（当前用 Mixamo `Female Walk`；HumanF / Quaternius `Walk_Loop` 等都能换）。归一化通了，动作库随便喂。
4. FBX 在内置管线显白，**贴图从源 GLB 的 `Texture2D` 子资源重新绑**（`MigrationUrpMaterialUtility`）。
5. 朝向 `yaw = 0`（FBX 在 identity 朝 +Z）；`MigrationNpcWalker` 负责巡逻位移。

## 核心教训（别再踩）

1. **不要手搓 `AvatarBuilder.BuildHumanAvatar`。** 手搓 avatar 会得到"指挥交通的螃蟹"——胳膊平举不动、腿在走。用 `HumanPoseHandler` 读肌肉值证明：**重定向在肌肉层面是完美的**（target 与 source 每条肌肉 range 全等），错只错在**手搓 avatar 没做 Enforce T-Pose + 轴标定**，导致静止参考姿势的胳膊是平举的。→ 一定走 **Unity 导入器的 humanoid 解算**（=Mixamo 等能正常的原因）。glTFast 的 GLB 不能直接用 ModelImporter humanoid，所以必须先转 FBX。

2. **诊断要拿肌肉数据，不要靠肉眼猜。** `HumanPoseHandler.GetHumanPose().muscles[]` 对比 target vs source，一眼看出是"肌肉没驱动"还是"标定错"。这是整件事的转折点。

3. **headless 验证必须真 Play 模式抓帧。** `-nographics` 不渲染蒙皮；带图形但**手动 `cam.Render()` 也不会重新蒙皮**。只有真 Play 模式（player loop）才会。验证入口 `NpcCloseupPlayRunner`。

4. **走过但放弃的两条路**（记录以免重走）：
   - **procedural 步态**（`MigrationProceduralWalker` 摆真骨）：能走、不螃蟹，但合成的略僵，且每角色都一样。可作 fallback。
   - **直接绑定 boned 模型 + 97 动作库**（同骨架零 retarget）：机制验证能绑（Animator 挂在 `Armature` 节点上，clip 路径 `Root/Hip/…` 直接对上），但库里 97 个动作全是无意义名（`NlaTrack.NNN`）、且只有灵梦有库 → 性价比低，没采用。

## 参考

- UniHumanoid `AvatarDescription.cs`（UniVRM 在用的正确做法）
- Tripo 官方 `RuntimeHumanoidAvatarBuilder.cs`（不公开，在 Tripo for Unity 插件里）
- Unity 论坛："Avatar stuck in bike pose after BuildHumanAvatar"（muscle limit 被夹成 0 / `useDefaultValues`）

## 待办（follow-up）

- spawn 重建路径（`MigrationVillageNpcs.SpawnVillagers/SpawnFixedBakedStoryCharacters`）目前仍是 procedural；重建村子会回退。需切到 humanoid FBX 这套，才能重建不回退。
- 可给三个角色配不同走路 clip（避免一致）+ 加 idle/手势（归一化已通，任何 humanoid clip 都能用）。
