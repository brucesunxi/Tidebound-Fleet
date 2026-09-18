# Phase 4 航道系统验收记录

日期：2026-09-18

Unity：2022.3.25f1

分支：`feat/lane-transit`

## 实施范围

- 消费 `ShipExitBoardEvent`，不再次移动棋盘船或修改占格。
- 固定上、左、右和下边左右绕行规则。
- 支持多艘船同时处于外围航道。
- 中央入口严格以 `ExitSequence` 为 FIFO 主键。
- 默认航道耗时 1.20 秒、入口间隔 0.15 秒、融入耗时 0.15 秒。
- 暂停冻结逻辑时钟与表现进度，恢复后继续。
- 完成时一次性进入 InFleet 并发布 `ShipEnterFleetEvent`。
- 提供默认场景路径、曲线采样、缩略船影视图和表现协调器。

未实现 FleetAggregation、AttackToken、Boss 扣血、胜利结算、连击、正式 UI、正式场景或美术资源。

## 自动验证

| 测试 | 结果 | 覆盖重点 |
|---|---:|---|
| EditMode | 104 / 104 通过 | 四边选路、下边等距走右、并行进度、乱序到达 FIFO、大步进稳定顺序、入口间隔、暂停、去重、跨局拒绝、七船入舰 |
| PlayMode | 5 / 5 通过 | 原有船移动三项回归；真实 ShipLaneView 位置、缩放、暂停及入舰隐藏两项 |

EditMode 运行时间：2026-09-18 08:16:55Z。

PlayMode 运行时间：2026-09-18 08:17:34Z 至 08:17:35Z。

两次测试均为 0 failed、0 skipped，日志无 C# 编译错误或警告。

测试 XML：

- `Validation/EditMode-results.xml`
- `Validation/PlayMode-results.xml`

## TestLevel_001 集成结论

按 S005 → S002 → S001 → S004 → S006 → S003 → S007 完成棋盘离场后，航道同时持有七艘船，并严格按 ExitSequence 1–7 完成七次入舰。最终当前棋盘为空、航道为空、七艘运行时船均为 InFleet；InitialBoard 未改变。

## 风险与下一阶段边界

- 当前没有正式场景，`LanePathLayout` 的航点仍需在后续可玩场景中由关卡启动代码绑定。
- 默认曲线和缩放已具备可运行行为，最终美术路径、尾迹、转弯朝向和同型席位位置仍需结合正式资产调优。
- Phase 5 必须以 `ShipEnterFleetEvent` 为唯一入战来源，并按 ExitSequence 去重生成攻击凭证。
- 本阶段未修改旧停车玩法、广告 SDK、数据库或发布配置。
