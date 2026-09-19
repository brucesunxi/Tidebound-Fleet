# 三源码借鉴与快速实现清单

日期：2026-09-19。状态：本地源码只读复核；行为与缺陷可作为实施依据，未宣称三个原工程已运行通过。当前规则与阶段以 [重设计方案](PHASE5R_REDESIGN.md) 和 [后续计划 v3](PHASE5_PLUS_PLAN.md) 为准。

进度更新：S16中的Solver与反向生成已在I1原创实现并验收，S01已有移动逻辑继续复用；见[I1验收](验证记录/Phase5R_I1_反向生成与求解/I1_VALIDATION.md)。I3已继续复用S05现有航道／FIFO，并原创补齐S16的舰队AttackToken、命中和胜利；未导入原车辆脚本。I4已借鉴S07目标选择／取消和S08真实洗牌／翻转行为，接入自己的事务、Solver与航道攻击链；原代码里onFlip／onShuffle命名与真实行为相反，未照搬。其他后续借鉴项仍按阶段推进。

百关扩展补充：S02笔刷、S03按数据表加载、S13内容差异研究继续作为最短复用路线；新增[外部工具调查](EXTERNAL_LEVEL_TOOLS_RESEARCH.md)和[内容生产体系](LEVEL_CONTENT_PIPELINE.md)。已有89个模板只提供结构研究入口，不能因已购买源码就默认全部关卡及素材可直接移植；本轮未导入任何原始布局。

## 1. 结论与使用方式

最快路线是保留已经通过测试的 Tidebound Grid、移动事务、输入和航道；从 Cocos 学交互与内容组织，从 Unity 学表现组件和界面流程。三个源码都没有可直接接入的反向生成器、完整 Solver 或潮汐舰队战斗／经济系统，这些缺口必须在自己的模块内实现。

“借助源码”不等于逐行跨引擎翻译，也不等于复制原关卡。每项工作从具体参考函数开始，提取输入、状态、反馈、异常分支，写成 Tidebound 用例后只补缺失部分。优先级：A 已有模块直接复用；B 源码行为借鉴并做薄适配；C 资源／插件候选，使用前核实授权和依赖；D 产品原创能力，必须新写。

原始项目统一位于 `00_远端接收区/已购源码与参考项目/`，保持只读：

| 代号 | 项目目录 | 主要价值 |
|---|---|---|
| P | `cocos源码-救救小猪/` | 四方向扫描、受阻前进、方向笔刷、占格编辑 |
| R | `cocos逆向_猪了个猪_2.4.15/` | 高密度手工模板、道具选择、连击、图鉴与池化流程 |
| U | `unity--Bus_Mania_100_BugFix/` | Unity 工程与表现组件、路径转场、界面开关和音效接线 |

下表路径相对于各项目目录；行号是本次只读快照定位。它们是研究入口，不是要导入的文件清单。

## 2. 可直接指导后续工作的参考表

| ID／阶段 | 源码证据 | 能学什么 | Tidebound 落地方式与验收 |
|---|---|---|---|
| S01／5R，A+B | P `assets/Script/UIManager/GameMain/GameMain.ts:131`，`checkMove`；R `assets/scripts/GameMgr.js:425`，`getMovePath` | 四方向前向扫描、最近阻挡、分段移动 | 保留 `BoardModel`、`ShipMovementSystem`；前进至阻挡前的新位置，紧邻零移动；覆盖四方向、重复点击、暂停和出界 |
| S02／5R，B | P `assets/Script/UIManager/CreateLevelMain/CreateLevelMain.ts:143`，`_createPig` | 方向笔刷和完整占格预览 | 复用现有 Level Studio，补长度 2／3 足迹与非法落点反馈；不再做另一套编辑器 |
| S03／5R，B | P 同文件 `:118`；R `assets/scripts/StaticData.js`、`GameMgr.js:575` | 人工制作关卡、教程后直接高密度、按表加载 | 保留 JSON 唯一布局源；新建原创 7／80 船固定测试集。借鉴节奏，不能把 89 个模板当新生成算法或原创关卡 |
| S04／5R，B | R `assets/scripts/GameMgr.js:726`，`optimizePos` | 坐标归一和统一摆放 | 使用自己的 Grid→世界／屏幕映射；上战区、中棋盘及航道、下道具区，三类安全区统一公式；不用每关手调 Transform |
| S05／7，A+B | U `Assets/TJ/Scripts/Vehicle.cs:390,422` | 出场后的航点、转弯、完成回调 | 保留已有 `TransitSystem` 和 FIFO，只借鉴动画节奏；同船只入舰一次，取消与暂停不重复完成 |
| S06／7、10，B+C | U `Assets/TJ/Scripts/SoundController.cs:20,29`；R `assets/scripts/UIMgr.js:338,349` | 音效开关、连击层级与表现递进 | 薄音效适配器；本产品连击按 GDD 的 5 秒窗口，仅影响表现，不改变每船 10 伤害；不复制参考的 7 秒参数 |
| S07／8，B | R `assets/scripts/PropsPanel.js:177`、`GameMgr.js:256` | 移除道具进入选目标状态、确认后执行 | 选择／取消／消耗事务分开；成功才扣次数；一艘被移除的船按 GDD 进入统一离场及攻击路径，不能漏奖励或重复攻击 |
| S08／8，B | R `assets/scripts/GameMgr.js:285`；`Animal.js:274` | 洗牌、180°翻转和目标反馈 | 依据实际行为迁移：`onFlip` 是洗牌流程，`onShuffle` 是翻转。翻转后验证足迹和动态可解性；洗牌保留剩余船身份与属性，失败回滚且不扣次数 |
| S09／9、10，B | R `assets/scripts/UIMgr.js:300`、`IllustratedPanel.js`、`AnimalDecPanel.js` | 图鉴未拥有／拥有／使用状态、解锁进度 | 借鉴交互状态，重写皮肤定义、5 装备槽与本产品金币规则；先占位图跑通保存／读档／装备，再做正式资产 |
| S10／9，反例 | R `assets/scripts/StorageMgr.js:17,39,47`；U `Assets/TJ/Scripts/CoinsManager.cs:27` | 存取入口及其缺陷 | 使用有版本、强类型、可恢复的离线 SaveData；结算账本独立于动画。不能复制字符串 Boolean、全局 clear 或直接加金币模式 |
| S11／10，B+C | U `Assets/TJ/Scripts/UIManager.cs:32,71` | 按钮锁、页面转换、金币飞行动画 | 只借鉴视图和反馈；剥离旧 SDK／LevelManager。账本先幂等提交，动画播放不决定是否入账 |
| S12／性能，B | R `assets/scripts/PoolMgr.js:14,26` | 预热、取出、回收生命周期 | 先用现有视图；设备分析证明分配／实例化是瓶颈再加 Unity 对象池，回收时清理事件和旧会话标识 |
| S13／6B，B | R 89 份模板；U 101 个玩法 Scene | 内容差异与制作成本 | 以数据清单批量校验；不复制 100 个场景式生产。每批 5 关，证明、人工体验与差异性一起审核 |
| S14／10，C | U 的 TMP、UGUI、DOTween／Pro、ParticleImage、QuickOutline 等依赖与资源 | 已有 UI／动画／粒子组件候选 | 优先现有 TMP／UGUI 和已实现动画；需要插件时先核对许可及移动端依赖，不为了“复用”增加框架 |
| S15／11，B | R `LocalPlatform`；U 旧广告及统计入口 | 奖励请求／成功／失败交互分支 | 本地模拟广告不是真实 SDK；后续通过统一广告接口接入本产品配置，离线仍可玩 |
| S16／5R、7、9，D | 三源码中未找到对应完整系统 | 反向依赖生成、真实状态 Solver、舰队 AttackToken、幂等结算与概率经济 | 原创实现；不得用随机摆放、原停车寻路或参考商城冒充完成 |

## 3. 源码中应避免带入的问题

- P 的 `checkMove` 在发现阻挡时先清旧格；紧邻阻挡的零移动分支有占格恢复风险。保留我们的不可变占格事务，不直接搬该函数。
- P 的 `_deletePig` 使用当前笔刷方向推断第二格，切换笔刷后可能删错占格。删除必须读取被删对象完整足迹。
- R 的 `onTouchEnd` 在动画前清占格。我们的串行状态机与完成提交时序已明确，不混用两套提交时机。
- R 的 `getLevel` 在固定模板用尽后随机选已有模板；`MainPanel` 的随机矩形只是首页装饰。两者都不是 Puzzle 生成器。
- R 的 `StorageMgr.getBoolean` 将字符串转 Boolean，`"false"` 也会变真；`clear` 清全部 localStorage。只学习入口，不复制实现。
- U 的 `Vehicle` 碰撞后退回起点，与本次确认的“前进到受阻点停住”直接冲突。不能接入其车辆移动逻辑。
- U 的 `UIManager.NextLevel` 将加币挂在等待／动画流程之后，不能用作可重试的结算事务；旧 SDK 初始化不能进入新灰盒启动路径。

## 4. 三个明确需要自己补的缺口

1. **生成与求解：**扩展现有 `BoardDependencyGraph`、`BoardSolvabilityAnalyzer` 和 `SolutionProof`，新增 `LevelSolver` 统一入口与反向生成器。复用真实查询／事务，不重新写棋盘。
2. **船到攻击的闭环：**从已存在的 `ShipEnterFleetEvent` 接一次性 AttackToken、Boss HP 和胜利屏障。源码只提供表现参考，没有这个业务契约。
3. **本产品经济与可靠存档：**每局结算唯一键、概率锁定、抽取保底与存档版本迁移。参考图鉴／按钮可学，经济逻辑不可照搬。

## 5. 每项功能的短实施循环

1. 读表中指定函数及调用者，记录正常、取消、重复点击和失败行为。
2. 先查 Tidebound 已有 API；能复用就接线，仅在既有模块新增缺口。
3. 写最小行为用例，用灰盒完成一个端到端场景；正式代码只进 `Assets/Tidebound`。
4. 跑相关 EditMode／PlayMode，必要时 Android 本地构建；记录借鉴项 ID、改造点和未验证边界。
5. 提交一个可回退功能；达到阶段门槛才进入下一阶段。不整包搬脚本、资源或 SDK。

三个目录未发现一份覆盖全部代码、美术、字体与插件的统一授权记录；本轮仅研究行为，未新增导入外部素材。需要直接采用具体资产时逐项核对，不影响独立实现机制和继续使用已验证的工程底座。
