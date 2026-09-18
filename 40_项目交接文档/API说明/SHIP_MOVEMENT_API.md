# Ship Movement API

Phase 3 的公开入口是 `ShipMovementSystem` 和 Unity 层 `ShipMovementController`。

## 初始化顺序

1. 通过 `LevelConfigLoader.Load(LevelConfigSO)` 创建 `GameSession`。
2. 为该局创建一个 `ShipMovementSystem(session)`。
3. 为当前 `session.Board.Ships` 的每个 ID 准备唯一 `IShipMovementView`。
4. 准备 `IGridWorldMapper`；它只负责船尾 Grid 到世界坐标的映射。
5. 创建 `ShipMovementController(movement, mapper, views, timing)`。
6. 调用 `controller.StartPlaying()`，之后视图点击会进入移动系统。

缺少或重复视图会在 Controller 构造时抛出配置错误，避免开始一局不完整的移动表现。

## 完成点

- `RequestMove` 被接受后，Controller 根据 `ShipMoveOperation` 播放直线移动或零位移反馈。
- 直线表现完成时调用核心 `CompleteTravel(operationId)`。
- 受阻反馈结束时调用 `CompleteBlockedFeedback(operationId)`。
- 业务代码不要自行写 `ShipRuntimeData.Position/State` 或替换 `GameSession.Board`。

## 暂停

暂停菜单或应用生命周期代码调用 `controller.Pause()`，恢复时调用 `controller.Resume()`。Controller 会冻结视图，并处理刚好与暂停同帧到达的完成回调，确保只提交一次。

## 事件

- `ShipMoveStartEvent`：一次点击被接受。
- `ShipMoveCompleteEvent`：受阻落点或完整离界已经提交。
- `ShipExitBoardEvent`：船尾完整离界、占格释放并取得 ExitSequence；Phase 4 从这里接入。

订阅属于 GameSession，使用完成后释放订阅 token 或 Dispose 整个 Session。
