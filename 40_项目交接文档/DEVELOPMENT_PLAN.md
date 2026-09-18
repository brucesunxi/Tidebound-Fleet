# Tidebound Fleet 开发计划

版本：1.3；日期：2026-09-18。每个阶段只有在入口条件满足后启动，不在前一阶段夹带下一阶段功能。

| 阶段 | 状态 | 目标 | 主要交付与退出条件 |
|---|---|---|---|
| Phase 0 源码审计 | 已完成 | 识别三个购买项目的完整性、可复用性和风险 | 源码资产评估报告、文件扫描基线 |
| Phase 0.5 游戏设计冻结 | 已完成 | 将概念变成可开发的产品规则 | GAME_DESIGN.md，核心循环、占格、攻击和 MVP 范围明确 |
| Phase 0.8 项目治理体系 | 已完成 | 建立目录、Git、文档、私密和临时文件规范 | PROJECT_STRUCTURE、AGENTS、gitignore、Changelog、Git 状态报告 |
| Phase 1 Unity 架构搭建 | 已完成 | 隔离 Tidebound 业务，建立数据与配置底座 | asmdef、namespace、JSON、SO、复制、校验、事件接口；65 项 EditMode 测试通过 |
| Phase 2 棋盘系统 | 已完成 | 建立纯 Grid 棋盘与可验证的移动判定 | 不可变占格事务、前向扫描、阻挡落点、完整出界判定；79 项 EditMode 测试通过 |
| Phase 3 船移动 | 已完成 | 把棋盘结果映射为点击、状态与表现 | 串行点击、状态提交、事件、默认动画和暂停恢复；91 EditMode＋3 PlayMode 通过 |
| Phase 4 航道 | 已完成 | 完成四边出场到中央入口的转场 | 四边选路、并行航道、FIFO 入战；104 EditMode＋5 PlayMode 通过 |
| Phase 5 海怪战斗 | 未开始 | 完成同型舰队聚合及每船一次攻击 | FleetAggregation、AttackToken、Boss 扣血、结算一次性 |
| Phase 6 成长商业化 | 未开始 | 在完整离线核心上接成长、广告和发布服务 | 金币、升级、广告容错、渠道 SDK、存档和发布验证 |

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

## Phase 5 入口条件

- 舰队与战斗只消费 `ShipEnterFleetEvent`，不得直接读取航道 Transform 判断入场。
- 每个 ExitSequence 最多生成一个 AttackToken；同型只聚合展示，不合成新船型。
- Boss 只在攻击命中时扣血；入舰和发射均不是扣血点。
- 胜利必须同时等待棋盘、航道、待发攻击和飞行攻击清空，且 Boss HP 为 0。
- Phase 5 不接金币成长、广告、存档或渠道 SDK。
