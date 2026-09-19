# Tidebound Fleet 开发计划

版本：3.0；日期：2026-09-19。每个阶段只有在入口条件满足后启动，不在前一阶段夹带下一阶段功能。Phase 5R 以后以真实高密度方向疏通体验为主线，详细计划见 [PHASE5_PLUS_PLAN.md](PHASE5_PLUS_PLAN.md)。

| 阶段 | 状态 | 目标 | 主要交付与退出条件 |
|---|---|---|---|
| Phase 0 源码审计 | 已完成 | 识别三个购买项目的完整性、可复用性和风险 | 源码资产评估报告、文件扫描基线 |
| Phase 0.5 游戏设计冻结 | 已完成 | 将概念变成可开发的产品规则 | GAME_DESIGN.md，核心循环、占格、攻击和 MVP 范围明确 |
| Phase 0.8 项目治理体系 | 已完成 | 建立目录、Git、文档、私密和临时文件规范 | PROJECT_STRUCTURE、AGENTS、gitignore、Changelog、Git 状态报告 |
| Phase 1 Unity 架构搭建 | 已完成 | 隔离 Tidebound 业务，建立数据与配置底座 | asmdef、namespace、JSON、SO、复制、校验、事件接口；65 项 EditMode 测试通过 |
| Phase 2 棋盘系统 | 已完成 | 建立纯 Grid 棋盘与可验证的移动判定 | 不可变占格事务、前向扫描、阻挡落点、完整出界判定；79 项 EditMode 测试通过 |
| Phase 3 船移动 | 已完成 | 把棋盘结果映射为点击、状态与表现 | 串行点击、状态提交、事件、默认动画和暂停恢复；91 EditMode＋3 PlayMode 通过 |
| Phase 4 航道 | 已完成 | 完成四边出场到中央入口的转场 | 四边选路、并行航道、FIFO 入战；104 EditMode＋5 PlayMode 通过 |
| Phase 5 技术基础 | 工程验收完成／产品验收撤回 | 建立高密度数据、输入与编辑基础 | 单基础船、schema v2、Grid点击、SafeArea计算、Level Studio V0；12×18夹具仅作回归 |
| Phase 5R 规则与关卡重构 | 重设计完成／Unity实施待执行 | 保留受阻前进，重构动态依赖生成与竖屏边界 | 完整阻挡Solver、反向生成、7／80艘的10关回放、统一分区与真人真机验收；见PHASE5R_REDESIGN.md |
| Phase 6A 最小验证工具 | 待随5R实施 | 支撑10关验收 | 复用Level Studio，接生成／求解／回放／批量验证与证明失效 |
| Phase 6B 量产工具与内容 | 后移至核心闭环之后 | 扩展到30关 | 按瓶颈补编辑能力、相似度与批量报告；每批5关 |
| Phase 7 舰队与海怪战斗 | 未开始 | 完成完整体量船群的聚合和高吞吐攻击 | 最多5个皮肤席位、AttackToken、Boss扣血、队列追赶与完整胜利 |
| Phase 8 道具、死局与重开 | 未开始 | 完成高密度关的救援和失败闭环 | 三道具、死局检测、部分结算、每日 10 次奖励重开、幂等终态 |
| Phase 9 皮肤收藏与经济 | 未开始 | 完成局内外收藏循环 | 16 皮肤、5 槽、概率金币、抽取、保底、收藏券和存档 |
| Phase 10 UI、教学与 30 关整合 | 未开始 | 形成可连续体验的产品版本 | 第1关唯一教学、第2关起完整体量、完整UI、30关内容和正式表现基线 |
| Phase 11 IAA、分析与调优 | 未开始 | 接入可选商业化和数据验证 | 激励广告、事件分析、配置版本、经济模拟与广告容错 |
| Phase 12 软启动与发布 | 未开始 | 完成多平台发布准备 | Android／iOS 构建、商店合规、设备覆盖、软启动和扩关决策 |

**2026-09-19更新：** 规则详见[Phase 5R重设计](PHASE5R_REDESIGN.md)，剩余顺序以[后续计划v3](PHASE5_PLUS_PLAN.md)为准：5R＋6A十关及竖屏验收→7战斗→8道具→9存档与经济→6B＋10量产整合→11→12。源码函数定位、适配与反例见[SOURCE_REUSE_MATRIX.md](SOURCE_REUSE_MATRIX.md)。本次复跑基础119/119＋6/6通过，不等于新生成器和十关完成。

正式棋盘和船数尚未冻结。候选规格、屏幕分区和冻结门槛见 [BOARD_LAYOUT_CAPACITY_PLAN.md](BOARD_LAYOUT_CAPACITY_PLAN.md)，源码证据见 [LEVEL_SOURCE_AUDIT.md](LEVEL_SOURCE_AUDIT.md)。

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

## Phase 5 技术完成记录与产品校正

- 保留 Phase 1–4 已验证的 Grid、事务、移动状态和航道 FIFO，不以密度调整为理由重写。
- 先迁移单基础船、实例长度和稳定 skinId，再扩大棋盘尺寸；禁止在四船型旧结构上继续做战斗。
- 校验和数据 schema 已迁移到实例长度2／3、唯一 `TF_BASE_SHIP` 与固定伤害10。
- `TestLevel_001` 保持4×6／7艘教学；`TestLevel_002` 的12×18／80艘只作为技术回归夹具，不再代表第2关或正式上限。
- 原12×18、82艘、172占格与44空格限制已从运行时合法性移除；运行时只保留24×24、160艘技术护栏，候选产品门槛由 `LevelProductionProfile` 管理。
- SafeArea 布局计算覆盖 360×640、390×844、430×932；集中 Grid 点击以逻辑占格锁定目标，空格不吸附。
- Level Studio 已在V0编辑能力上增加结构分析、真实规则试玩、解法录制回放、布局指纹和候选JSON／证明原子保存；Undo／Redo、框选、批量生产、相似度和正式难度评分仍属于Phase 6。
- Unity 2022.3.25f1 自动化结果：110/110 EditMode、5/5 PlayMode。完整证据见 [Phase 5 验收记录](验证记录/Phase5_高密度棋盘基础/PHASE5_VALIDATION.md)。
- 源码复核证明正式竞品模板约92～124个对象，且使用18×18～24×24逻辑网格和全盘方向交错；原条带灰盒已经撤销且未提交。
- 真人点击、方向可读性、依赖结构和最低设备帧率改为Phase 5R退出门槛。

## Phase 5 产品规则变更记录

- 2026-09-18 确认取消快艇、炮艇、战舰、旗舰四种逻辑船型，采用一种基础船逻辑。
- 长度只保留 2／3：1×2 标准船参与皮肤换装，1×3 长船固定外观；取消长度 4。两者都不改变 Grid、移动、攻击次数或伤害。
- 所有船固定伤害 10，Boss HP＝初始船数×10。
- 玩家最多装备 5 个皮肤；关卡用平衡洗牌袋分配，战区按 skinId 最多聚合 5 个席位。
- 红色皮肤允许单独装备；每船金币在 1 至品质上限间生成，概率按该 skinId 开局占比锁定，多皮肤搭配更容易获得高值。
- 战斗金币通常在胜利时入账；系统确认死局且玩家放弃道具时可部分结算。当前关每日前 10 次重开具有死局结算额度，已通关关卡不可返回。
- 产品类型明确为方向性消除／Tap Away／拔针疏通。第1关是唯一低数量玩法教学，第2关直接进入完整体量；首轮候选范围80～110艘。
- MVP 调整为 30 关；第 2–30 关主要通过依赖深度、分支、长船锁点和死局路线增加难度，不靠逐关增加对象数量。
- 产品基线见 [GAME_DESIGN.md](GAME_DESIGN.md) 与 [SHIP_SKIN_COLLECTION_ECONOMY.md](SHIP_SKIN_COLLECTION_ECONOMY.md)。
- 上一轮Phase 5R原型已落地；该产品验收结论现已撤回。当前先重构生成与求解、完成新十关和真实灰盒，再执行人体触控与最低Android验证。

## 上一轮 Phase 5R 工程记录（历史原型，不能替代重构验收）

- 建立18×18、18×22、20×20、22×22四组候选配置和12个80～110艘固定原型，每个原型都有与布局指纹绑定的完整清盘证明。
- 实现首阻挡依赖图、初始出口／可移动统计、方向熵、同向聚集、最长依赖链、硬锁环和小图有界精确搜索。
- 第一轮带边缘方向区和中心空洞的样本已否决；当前样本采用全盘均匀占格与真实剥离证明，四方向在全盘交错。
- `Open／Mid／Deep` 仍是比较标签，不能作为正式难度等级；12个样本不进入正式关卡清单。
- Unity 2022.3.25f1 自动化结果：119/119 EditMode、6/6 PlayMode。
- 完整指标和待办门槛见 [Phase 5R 验证记录](验证记录/Phase5R_关卡体系校准/PHASE5R_VALIDATION.md)。
