# Tidebound Fleet 开发计划

版本：2.2；日期：2026-09-18。每个阶段只有在入口条件满足后启动，不在前一阶段夹带下一阶段功能。Phase 5 以后以高密度方向疏通体验为主线，详细计划见 [PHASE5_PLUS_PLAN.md](PHASE5_PLUS_PLAN.md)。

| 阶段 | 状态 | 目标 | 主要交付与退出条件 |
|---|---|---|---|
| Phase 0 源码审计 | 已完成 | 识别三个购买项目的完整性、可复用性和风险 | 源码资产评估报告、文件扫描基线 |
| Phase 0.5 游戏设计冻结 | 已完成 | 将概念变成可开发的产品规则 | GAME_DESIGN.md，核心循环、占格、攻击和 MVP 范围明确 |
| Phase 0.8 项目治理体系 | 已完成 | 建立目录、Git、文档、私密和临时文件规范 | PROJECT_STRUCTURE、AGENTS、gitignore、Changelog、Git 状态报告 |
| Phase 1 Unity 架构搭建 | 已完成 | 隔离 Tidebound 业务，建立数据与配置底座 | asmdef、namespace、JSON、SO、复制、校验、事件接口；65 项 EditMode 测试通过 |
| Phase 2 棋盘系统 | 已完成 | 建立纯 Grid 棋盘与可验证的移动判定 | 不可变占格事务、前向扫描、阻挡落点、完整出界判定；79 项 EditMode 测试通过 |
| Phase 3 船移动 | 已完成 | 把棋盘结果映射为点击、状态与表现 | 串行点击、状态提交、事件、默认动画和暂停恢复；91 EditMode＋3 PlayMode 通过 |
| Phase 4 航道 | 已完成 | 完成四边出场到中央入口的转场 | 四边选路、并行航道、FIFO 入战；104 EditMode＋5 PlayMode 通过 |
| Phase 5 单船迁移与 80 船灰盒 | 自动化验收完成 | 建立正式高密度数据、输入与编辑基础 | 单基础船、schema v2、12×18／80 船可清盘夹具、Grid 点击、SafeArea 计算、Level Studio V0；110 EditMode＋5 PlayMode 通过 |
| Phase 6 关卡验证与生产工具 | 未开始 | 建立可扩展到数百关的内容能力 | JSON v2、解法回放、难度指标、候选生成流程、30 关灰盒数据 |
| Phase 7 舰队与海怪战斗 | 未开始 | 完成 80+ 船的聚合和高吞吐攻击 | 最多 5 个皮肤席位、AttackToken、Boss 扣血、队列追赶与完整胜利 |
| Phase 8 道具、死局与重开 | 未开始 | 完成高密度关的救援和失败闭环 | 三道具、死局检测、部分结算、每日 10 次奖励重开、幂等终态 |
| Phase 9 皮肤收藏与经济 | 未开始 | 完成局内外收藏循环 | 16 皮肤、5 槽、概率金币、抽取、保底、收藏券和存档 |
| Phase 10 UI、教学与 30 关整合 | 未开始 | 形成可连续体验的产品版本 | 第 1 关唯一教学、第 2 关起 80 艘、完整 UI、30 关内容和正式表现基线 |
| Phase 11 IAA、分析与调优 | 未开始 | 接入可选商业化和数据验证 | 激励广告、事件分析、配置版本、经济模拟与广告容错 |
| Phase 12 软启动与发布 | 未开始 | 完成多平台发布准备 | Android／iOS 构建、商店合规、设备覆盖、软启动和扩关决策 |

正式棋盘、屏幕分区和 78–82 艘容量上限见 [BOARD_LAYOUT_CAPACITY_PLAN.md](BOARD_LAYOUT_CAPACITY_PLAN.md)。

## Phase 2 完成记录

- Git 治理基线已提交并推送，Phase 2 在 `feat/board-grid` 分支实施。
- 保持 JSON/SO 和船尾 Grid 坐标契约，没有引入 Transform、Collider 或模型尺寸。
- `BoardModel` 复制并索引完整占格；`ForwardPathResult` 明确遇阻与完整离界。
- `ApplyPathResult` 返回新棋盘并原子替换或释放占格，旧快照不变；过期结果被拒绝。
- TestLevel_001 的指定 7 船顺序已通过数据层清盘测试；没有加入场景、输入、动画、航道或战斗。
- 完整结论见 [Phase 2 验收记录](验证记录/Phase2_棋盘系统/PHASE2_VALIDATION.md)。

## Phase 2 实际拆分

1. `BoardModel` 与 `BoardShipSnapshot`：不可变占格所有者和船逻辑快照。
2. `QueryForwardPath` 与 `ForwardPathResult`：最近阻挡、空格序列、移动距离、落点及完整出界。
3. `ApplyPathResult`：返回新棋盘的原子事务；0 位移不丢占格，旧结果不能提交到新状态。
4. `BoardQueryTests`：四方向离界、零位移、受阻落点、快照隔离、事务和 TestLevel_001 清盘序列。
5. 不创建动画、场景表现、输入状态机或玩法事件。

## Phase 3 入口条件

- 评审 `ForwardPathResult` 与不可变事务契约，保持棋盘为移动规则唯一来源。
- 决定棋盘事务与 `ShipRuntimeData`、ShipState、事件和动画完成点的一致提交顺序。
- 继续使用逻辑格控制占位，MonoBehaviour 只负责输入和表现映射。
- Phase 3 不提前接航道、Boss 战斗或商业 SDK。

## Phase 3 完成记录

- `ShipMovementSystem` 串行接受 Idle 船点击，拒绝忙碌、暂停、未知、离场或非 Idle 船。
- 棋盘占格和 RuntimeData 只在表现完成点同步提交；InitialBoard 保留开局快照。
- 实现 Moving、BlockedFeedback、Exiting、InLane 状态转换以及 MoveStart、MoveComplete、ExitBoard 事件。
- 完整离界时分配单局递增 ExitSequence，为 Phase 4 FIFO 提供稳定输入。
- `ShipMovementController` 处理点击绑定、动画回调、暂停竞态与恢复；默认 Transform 视图不依赖 DOTween。
- TestLevel_001 已通过完整状态机按指定顺序清盘。
- 完整结论见 [Phase 3 验收记录](验证记录/Phase3_船移动/PHASE3_VALIDATION.md)。

## Phase 4 入口条件

- 航道只消费已经完成离界的 `ShipExitBoardEvent`，不得提前释放或再次移动棋盘船。
- 以 ExitSequence 作为中央入口 FIFO 的唯一主排序键。
- 暂停必须冻结所有航道进度和入口等待，不改变序号。
- Phase 4 不提前创建攻击或扣减 Boss HP。

## Phase 4 完成记录

- `TransitSystem` 只订阅已完成离界的 `ShipExitBoardEvent`，并校验 Session、船状态、类型、实例和序号唯一性。
- 四边路线已经固定；下边按较短侧绕行，等距固定走右侧。
- 每艘船独立推进 1.20 秒航道进度；中央入口严格按 ExitSequence FIFO，默认 0.15 秒间隔和 0.15 秒融入。
- 暂停不推进航道时钟、路径、入口等待和融入进度；恢复后继续，不重复入舰。
- Unity 表现层提供场景航点、平滑路径采样、缩略船影视图与进度协调，不参与逻辑顺序。
- TestLevel_001 七艘船按指定序列全部进入 InFleet，产生七次且仅七次 ShipEnterFleetEvent。
- 完整结论见 [Phase 4 验收记录](验证记录/Phase4_航道系统/PHASE4_VALIDATION.md)。

## Phase 5 完成记录

- 保留 Phase 1–4 已验证的 Grid、事务、移动状态和航道 FIFO，不以密度调整为理由重写。
- 先迁移单基础船、实例长度和稳定 skinId，再扩大棋盘尺寸；禁止在四船型旧结构上继续做战斗。
- 校验和数据 schema 已迁移到 12×18 上限、实例长度 2／3、唯一 `TF_BASE_SHIP` 与固定伤害 10。
- 正式关总船数不超过 82、长船不超过 8、占用格不超过 172，并至少保留 44 个空格。
- `TestLevel_001` 保持 4×6／7 艘教学；`TestLevel_002` 固定 12×18／80 艘／160 占格／56 空格／Boss HP 800。
- 第二关开局至少 12 艘可直接驶出，记录的 80 步序列可无阻挡清空；这验证该夹具，不构成通用求解器。
- SafeArea 布局计算覆盖 360×640、390×844、430×932；集中 Grid 点击以逻辑占格锁定目标，空格不吸附。
- Level Studio V0 可创建、打开、放置、删除、校验并保存 schema v2 JSON；未包含解法录制、Undo／Redo、批量生产或难度评分。
- Unity 2022.3.25f1 自动化结果：110/110 EditMode、5/5 PlayMode。完整证据见 [Phase 5 验收记录](验证记录/Phase5_高密度棋盘基础/PHASE5_VALIDATION.md)。
- 真人点击命中率、方向可读性和最低设备帧率仍是 Phase 6 前置的真机灰盒门槛。

## Phase 5 产品规则变更记录

- 2026-09-18 确认取消快艇、炮艇、战舰、旗舰四种逻辑船型，采用一种基础船逻辑。
- 长度只保留 2／3：1×2 标准船参与皮肤换装，1×3 长船固定外观；取消长度 4。两者都不改变 Grid、移动、攻击次数或伤害。
- 所有船固定伤害 10，Boss HP＝初始船数×10。
- 玩家最多装备 5 个皮肤；关卡用平衡洗牌袋分配，战区按 skinId 最多聚合 5 个席位。
- 红色皮肤允许单独装备；每船金币在 1 至品质上限间生成，概率按该 skinId 开局占比锁定，多皮肤搭配更容易获得高值。
- 战斗金币通常在胜利时入账；系统确认死局且玩家放弃道具时可部分结算。当前关每日前 10 次重开具有死局结算额度，已通关关卡不可返回。
- 产品类型明确为方向性消除／Tap Away／拔针疏通。第 1 关是唯一低数量玩法教学，第 2 关起直接进入 80 艘左右的正式棋盘。
- MVP 调整为 30 关；第 2–30 关主要通过依赖深度、分支、长船锁点和死局路线增加难度，不靠逐关增加对象数量。
- 产品基线见 [GAME_DESIGN.md](GAME_DESIGN.md) 与 [SHIP_SKIN_COLLECTION_ECONOMY.md](SHIP_SKIN_COLLECTION_ECONOMY.md)。
- Phase 5 数据迁移和基础工具已经落地；下一步先完成真机灰盒门槛，再进入 Phase 6 的解法录制、难度指标和批量关卡生产。
