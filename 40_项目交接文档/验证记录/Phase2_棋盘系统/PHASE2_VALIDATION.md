# Phase 2 棋盘系统验收记录

日期：2026-09-18。结论：**纯 Grid 棋盘、路径判定和不可变占格事务通过；尚未实现玩家输入、移动表现或玩法状态机。**

## 1. 实现范围

- `BoardModel` 复制每艘船的逻辑摆放并建立完整占格索引。
- `BoardShipSnapshot` 保存 ID、类型、船尾、方向、长度和只读占格，不引用表现对象。
- `QueryForwardPath` 从船头前一格扫描到最近障碍或边界。
- `ForwardPathResult` 区分 Blocked／Exit，提供空格序列、移动距离、目标船尾、阻挡船和阻挡格。
- Exit 的距离以船尾完全越过棋盘边界为准。
- `ApplyPathResult` 返回新的不可变棋盘：受阻时原子替换整船占格，驶出时释放整船占格。
- 查询和事务不修改 `ShipRuntimeData`、ShipState、事件总线、JSON 或 ScriptableObject。

未实现点击、MonoBehaviour 控制器、Tween、碰撞反馈、暂停恢复、航道、战斗、UI、音效和通用关卡求解器。

## 2. 关键规则验证

| 验收项 | 结果 |
|---|---|
| 左下原点、船尾锚点、四方向、完整占格 | 通过 |
| 坐标查询、船只查询、边界查询 | 通过 |
| 紧邻阻挡距离为 0，原占格不丢失 | 通过 |
| 隔空阻挡停在最后合法位置 | 通过 |
| 上下左右无阻挡时船尾完全离界 | 通过 |
| 受阻事务原子更新，新旧快照互不污染 | 通过 |
| 离场事务只释放目标船完整占格 | 通过 |
| 过期路径结果不能提交到变化后的棋盘 | 通过 |
| RuntimeData 被直接编辑不会污染棋盘快照 | 通过 |
| TestLevel_001 初始阻挡关系 | 通过 |
| TestLevel_001 指定七船顺序最终清空 | 通过 |

TestLevel_001 验证顺序：`S005 → S002 → S001 → S004 → S006 → S003 → S007`。每一步查询均为 Exit，最终新棋盘 `ShipCount = 0`、`OccupiedCellCount = 0`；原 GameSession 保持 7 艘 Idle 船。

## 3. 测试结果

环境：Unity 2022.3.25f1，Unity Test Framework 1.1.33，EditMode，`Tidebound.Tests.EditMode`。

最终结果：**79 / 79 Passed，0 Failed，0 Skipped，Unity 进程退出码 0。**

执行时间：2026-09-18 07:41:39 UTC（北京时间 15:41:39）。

| 测试组 | 数量 | 通过 |
|---|---:|---:|
| AssemblyBoundaryTests | 2 | 2 |
| BoardQueryTests | 14 | 14 |
| BoardValidationTests | 29 | 29 |
| EventBusTests | 6 | 6 |
| LevelJsonReaderTests | 20 | 20 |
| LevelLoadingTests | 8 | 8 |
| 合计 | 79 | 79 |

测试证据：[NUnit XML](Validation/EditMode-results.xml)。测试日志写入 `/private/tmp`，未作为项目临时文件提交。

## 4. 后续边界

Phase 3 负责把纯逻辑查询与事务接入：点击输入、单船串行锁、`ShipRuntimeData.Position`、ShipState、事件、动画完成点、暂停和恢复。提交顺序必须保证棋盘占格与运行时位置一致；表现层不能自行修改格子。

Phase 3 仍不包含航道、舰队、Boss 扣血或商业化系统。
