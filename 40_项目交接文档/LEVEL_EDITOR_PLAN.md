# Tidebound Fleet 关卡编辑器调研与规划

版本：1.1

日期：2026-09-18

状态：工具设计，尚未开发

## 1. 结论

现有资产中没有可直接用于 Tidebound Unity 工程的完整关卡编辑器。推荐在 Phase 6 开发 Tidebound 专用 Unity EditorWindow，直接读写关卡 JSON，并调用游戏实际使用的 BoardValidator 和解法回放器。

不推荐购买或接入通用关卡编辑器作为核心依赖。Tidebound 的多格船尾坐标、四方向、受阻前进、完整离界、死局和解法证明都属于专用规则；通用 Tilemap 工具最终仍需重写关键部分。

Unity 2022.3 官方支持使用 UI Toolkit 创建可停靠的自定义 EditorWindow，也支持拖放式编辑界面，适合作为本工具的技术基础。官方依据见本文第 7 节。

## 2. 已有项目检查

### 2.1 Unity Bus Mania

检查结果：

- `Assets/TJ/Scenes/` 中存在约 100 个 `Level N.unity` 关卡场景，另有地图／过场场景。
- LevelManager 通过 Scene buildIndex 加载、重载和推进关卡。
- 游戏业务脚本中没有发现关卡专用 EditorWindow、CustomEditor、自动保存或关卡生成工具。
- 高关卡场景包含约 30 个 PrefabInstance，说明主要工作流是复制场景后在 Scene 中手工摆放对象。

判断：

- 可以借鉴“独立关卡排序、进度推进和循环取关”的产品思路。
- 不能复用为 Tidebound 编辑器。80 艘船 × 数百关如果继续使用独立 Scene，会造成场景数量、合并冲突、校验、批量调整和版本迁移成本失控。

### 2.2 Cocos 正向《救救小猪》

发现运行时工具 `CreateLevelMain`：

- 输入网格宽高并生成格子。
- 选择上／下／左／右方向。
- 点击空格放置占两格的猪。
- 点击对象删除。
- 用二维方向矩阵表示关卡：0 为空，1／2／3／4 表示四方向。
- 点击输出后把矩阵打印到控制台，再由人员手工复制到 LevelConfig。

其现有关卡数据已经采用高密度矩阵：

| 样例 | 网格 | 方向锚点数量 |
|---|---:|---:|
| Level 1 | 5×5 | 5 |
| Level 2 | 18×22 | 112 |
| Level 3 | 5×3 | 5 |
| Level 4 | 18×18 | 92 |

这说明竞品工作流本身就是“第一关少量教学，随后立即进入完整密度”，与 Tidebound 最新产品决定一致。

可借鉴：

- 网格点击放置。
- 方向笔刷。
- 放置时即时检查相邻占格。
- 二维概览快速编辑大量对象。

不可直接复用：

- Cocos／TypeScript 代码不能直接用于 Unity C# 编辑程序集。
- 只支持固定长度 2。
- 没有 JSON 文件读写、Undo／Redo、批量关卡索引、版本迁移、难度统计或求解验证。
- 删除逻辑依赖当前选择方向，存在误删占格风险。
- 具体关卡布局属于参考项目内容，不复制到原创关卡。

### 2.3 Cocos 逆向项目

未发现可读、可维护的关卡编辑器。可以观察现有关卡矩阵和 UI 表现，不应把逆向脚本作为生产工具基础。

### 2.4 当前 Tidebound 工程

当前已有：

- JSON 是关卡布局唯一数据源。
- LevelConfigSO 只引用 JSON。
- LevelConfigInspector 提供 `Validate data only` 按钮。
- BoardValidator 可检查尺寸、越界、重叠和配置引用。

当前缺少：

- 可视化网格编辑。
- JSON 创建、另存、复制和批量编号。
- Undo／Redo。
- 实时占格、出口和阻挡预览。
- 已知解法录制、回放与可解性验证。
- 关卡难度统计和 80 船性能预览。

## 3. 推荐方案：Tidebound Level Studio

工具入口建议为：

```text
Tools > Tidebound > Level Studio
```

工具只存在于 `Tidebound.Editor` 程序集，不进入玩家安装包。编辑结果写入 `Assets/Tidebound/Config/Levels/` 下的版本化 JSON，运行时与编辑器读取同一份数据。

### 3.1 编辑区

- 中央显示 4×6 至 12×18 网格。
- 左键放置，右键删除；拖动用于连续选择，不允许产生半个船体。
- 方向笔刷：上、下、左、右；支持方向键或 W／A／S／D 快捷键。
- 长度笔刷：2、3；取消长度 4。长度 2 显示可换皮标准船占格，长度 3 显示固定长船占格。
- 船尾格使用实心标记，船头使用尖角与方向箭头；占用的其他格使用半透明连体色。
- 点击已有船可以旋转、改长度、移动或复制。
- 支持框选、批量删除、水平镜像、垂直镜像和 180° 旋转。

### 3.2 数据与保存

- 新建关卡时输入 levelId、宽、高和设计备注。
- 保存前自动生成稳定 shipId，不允许重复。
- Open／Save／Save As／Duplicate 都直接操作 JSON。
- 自动维护关卡清单，但不把布局复制进 ScriptableObject。
- 记录 schemaVersion；旧版本必须通过显式迁移器升级。
- 保存使用临时文件加原子替换，避免 Unity 或系统中断造成半份 JSON。

### 3.3 实时验证

编辑过程中持续显示：

- 船数、各长度数量、四方向数量和占用率。
- 空格数、空白率、3×3 宏区空格分布，以及 82 艘／172 占用格硬上限。
- 越界、重叠、重复 ID 和不合法长度。
- 当前可直接出场数、可前进数和零位移受阻数。
- 每艘船的首个阻挡对象和依赖连线开关。
- Boss HP 预览＝初始船数×10。

编辑器必须直接调用运行时 BoardValidator／BoardModel，不能复制一套容易分叉的编辑器规则。

### 3.4 解法工作流

V1 不承诺通用自动求解器，采用更可靠的“录制＋回放证明”：

1. 策划完成初始布局。
2. 在编辑器预览中按真实规则点击清盘。
3. 工具记录 shipId 操作序列和每步棋盘摘要。
4. 保存时重新从初始 JSON 回放整条序列。
5. 只有成功清空、无非法事务且 Boss 伤害一致时，才写入有效解法证明。

后续可以增加搜索求解器和反向生成器，但不能让“自动生成成功”替代真人可读性测试。

### 3.5 排列组合效率工具

因为第 2 关起船数长期保持约 80，关卡差异主要来自排列组合，编辑器应支持：

- 从现有关卡复制后重新编号。
- 对选区或全局做镜像、旋转和方向重排。
- 在保持合法占格的前提下交换同长度船的位置。
- 锁定关键船后随机化其他船。
- 根据目标方向比例生成候选布局。
- 对两个关卡计算布局相似度，避免只做镜像就当作新关。
- 批量输出关卡指标，按依赖深度和分支数排序。

随机化只产生候选，不直接进入正式关卡。每个候选都必须经过合法性、解法回放和真人体验检查。

## 4. 开发分级

### Editor V0：Phase 5 灰盒工具

- 12×18 网格预览。
- 放置／删除长度 2 船。
- 四方向切换。
- 导入／导出单个 JSON。
- 调用 BoardValidator。
- 船数、方向和占用率统计。

用途：尽快制作第 1 关 7 艘和第 2 关 80 艘灰盒。

### Editor V1：Phase 6 生产工具

- 长度 2／3；编辑器拒绝长度 4。
- Undo／Redo、框选、复制、镜像和旋转。
- 多关卡浏览、Duplicate、稳定 ID 和版本迁移。
- 真实规则试玩、操作录制和解法回放。
- 阻挡关系、初始候选、依赖深度和死局提示。
- 一键运行当前关测试。

用途：生产并验证 30 个 MVP 关卡。

### Editor V2：内容扩展工具

- 反向生成候选。
- 批量变体与相似度检测。
- 难度评分校准。
- 玩家数据回灌热力图。
- 数百关批量验证和报表。

用途：软启动后扩展到 100–300 关。

## 5. 验收标准

| ID | 场景 | 通过条件 |
|---|---|---|
| E01 | 创建第 1 关 | 4×6、7 艘可以可视化放置、保存、重开并保持一致 |
| E02 | 创建第 2 关 | 12×18、80 艘编辑过程流畅，无重叠或半船；空格和占用格统计准确 |
| E03 | 保存 JSON | Runtime 加载结果与编辑器预览逐格一致 |
| E04 | 撤销重做 | 放置、删除、旋转、移动和批量操作均可恢复 |
| E05 | 非法数据 | 越界、重叠、重复 ID 和不支持长度禁止发布 |
| E06 | 解法证明 | 已录制步骤从初始数据完整回放并清盘 |
| E07 | 批量验证 | 30 关可一次验证并输出失败关卡及具体原因 |
| E08 | 数据唯一性 | Scene、Prefab 和 SO 中不保存第二份船只布局 |

## 6. 最终建议

采用“Cocos 编辑器交互思路＋Unity UI Toolkit EditorWindow＋Tidebound 运行时规则复用”的方案。先做满足第 1、2 关生产的 V0，再在 Phase 6 扩展到 V1。这样比导入通用插件更可控，也能保证后续移动规则变化时，编辑器、验证器和游戏不会产生三套不一致逻辑。

棋盘几何、空白率和发布硬上限见 [BOARD_LAYOUT_CAPACITY_PLAN.md](BOARD_LAYOUT_CAPACITY_PLAN.md)。

## 7. 调研依据

本地源码证据：

- Cocos 摆关工具：`00_远端接收区/已购源码与参考项目/cocos源码-救救小猪/assets/Script/UIManager/CreateLevelMain/CreateLevelMain.ts`
- Tidebound 当前 Inspector：`10_开发工作区/TideboundFleet_Unity/Assets/Tidebound/Editor/LevelValidator/LevelConfigInspector.cs`
- Tidebound 运行时校验器：`10_开发工作区/TideboundFleet_Unity/Assets/Tidebound/Runtime/Core/Board/BoardValidator.cs`

Unity 2022.3 官方资料：

- [Editor UI 的 UI Toolkit 支持](https://docs.unity3d.com/cn/2022.3/Manual/UIE-support-for-editor-ui.html)
- [创建自定义 EditorWindow](https://docs.unity3d.com/kr/2022.3/Manual/editor-EditorWindows.html)
- [UI Toolkit 拖放界面示例](https://docs.unity3d.com/cn/2022.3/Manual/UIE-create-drag-and-drop-ui.html)
