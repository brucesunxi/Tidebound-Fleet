# Phase 3 船移动验收记录

日期：2026-09-18。结论：**点击到逻辑提交、状态、事件和默认 Transform 表现的基础链路通过；尚未实现航道和战斗。**

## 1. 实现范围

- 单局 `ShipMovementSystem`，同一时刻只允许一个活动操作，额外点击不排队。
- Prepare／Playing／Paused 生命周期与暂停恢复。
- Idle → Moving／BlockedFeedback／Exiting → Idle／InLane 状态转换。
- 到达表现完成点后同步提交 Board 与 ShipRuntimeData。
- 零位移碰撞保留完整占格；受阻移动停在新位置，不回到原位置。
- 船尾完全离界后释放占格、进入 InLane，分配 ExitSequence 并发布 ShipExitBoardEvent。
- `ShipMovementController`、`IShipMovementView`、`GridWorldMapper` 和默认 `ShipMovementView`。
- 默认直线动画参考 12 格／秒，限制 0.18–0.60 秒；受阻反馈 0.12 秒且只做横向摆动。

未创建正式场景、船模型、航道、舰队、攻击、Boss 扣血、UI、音效或商业 SDK 接入。

## 2. 规则验收

| 验收项 | 结果 |
|---|---|
| 非 Playing、暂停、忙碌、未知和非 Idle 船拒绝 | 通过 |
| 操作期间额外点击不缓存 | 通过 |
| 受阻直线移动在动画到达前不提交位置 | 通过 |
| 到达后 Board 与 RuntimeData 同步变更 | 通过 |
| BlockedFeedback 完成后回 Idle 并保留新位置 | 通过 |
| 紧邻阻挡为零位移且不丢占格 | 通过 |
| 完整离界后释放占格并进入 InLane | 通过 |
| ExitSequence 单局稳定递增 | 通过 |
| InitialBoard 不随当前棋盘变化 | 通过 |
| 暂停保留活动操作，竞态回调恢复后只提交一次 | 通过 |
| TestLevel_001 七船状态机清盘 | 通过 |
| 默认视图到达目标、暂停冻结、横向反馈归位 | 通过 |

## 3. 测试结果

环境：Unity 2022.3.25f1，Unity Test Framework 1.1.33。

- EditMode：**91 / 91 Passed，0 Failed，0 Skipped**。
- PlayMode：**3 / 3 Passed，0 Failed，0 Skipped**。
- 两次 Unity 进程退出码均为 0，日志未出现 C# error 或 warning。

最终执行时间：EditMode 2026-09-18 08:00:16 UTC；PlayMode 2026-09-18 08:00:45 UTC。

新增测试组：

| 测试组 | 数量 | 通过 |
|---|---:|---:|
| ShipMovementSystemTests | 6 | 6 |
| ShipMovementControllerTests | 6 | 6 |
| ShipMovementViewPlayModeTests | 3 | 3 |

证据：[EditMode NUnit XML](Validation/EditMode-results.xml)、[PlayMode NUnit XML](Validation/PlayMode-results.xml)。完整日志保留在 `/private/tmp`，不作为项目临时文件提交。

## 4. 下一阶段边界

Phase 4 订阅 ShipExitBoardEvent，按 ExitSequence 建立四边路径和中央入口 FIFO。它负责 InLane → InFleet，但不重新计算棋盘移动、不修改已经释放的占格，也不提前创建攻击。
