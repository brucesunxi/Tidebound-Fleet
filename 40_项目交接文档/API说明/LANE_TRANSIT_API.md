# Lane Transit API

Phase 4 的公开入口是纯逻辑 `TransitSystem` 和 Unity 表现层 `LaneTransitController`。

## 初始化顺序

1. 通过 `LevelConfigLoader.Load(LevelConfigSO)` 创建 `GameSession`。
2. 在任何船可能离场前创建 `TransitSystem(session)`，使其订阅本局 `ShipExitBoardEvent`。
3. 创建 `ShipMovementSystem(session)` 和移动表现层。
4. 为该局每艘船准备唯一 `ILaneTransitView`；默认实现为 `ShipLaneView`。
5. 在场景中配置 `LanePathLayout` 的左右上下转角、顶部中央入口和舰队接入点。
6. 创建 `LaneTransitController(transit, pathProvider, views)`。
7. 游戏循环只在正常 Playing 状态调用 `controller.Advance(deltaTime)`；系统本身也会拒绝暂停或非 Playing 推进。

必须先创建 `TransitSystem` 再允许船离场。它不回放创建前已经发布的事件；关卡重试应销毁旧 Session 和 Controller，并为新 Session 创建整套对象。

## 逻辑时间与顺序

- 每艘船从离界事件起独立运行 1.20 秒，路线长短不改变名义耗时。
- 上、左、右出口选择同侧路线；下出口选择到左右底角较短的一侧，等距走右侧。
- 后序船可以先抵达并等待，但只有 `NextEntranceSequence` 能进入中央入口。
- 默认入口最小间隔和融入时长均为 0.15 秒。
- 融入完成后状态变为 InFleet，随后发布 `ShipEnterFleetEvent`。
- 同一船、同一序号、跨 Session 或非 InLane 事件均不会创建有效转场。

## 暂停

`TransitSystem` 读取同一 `GameSession.State`。状态为 Paused 时 `Advance` 返回 `SessionPaused`，不改变逻辑时钟、路径进度、等待顺序或融入进度。恢复 Playing 后继续使用剩余时间。

## 表现职责

- `LaneWorldPath` 和 `LanePathLayout` 只提供世界坐标曲线。
- `LaneTransitController` 把 `LaneProgress`／`FleetEntryProgress` 映射到视图。
- 默认 `ShipLaneView` 改变位置、朝向和缩放，入舰后隐藏棋盘船实例。
- 路径距离、Transform、Renderer、Collider 和视觉缩放都不参与 FIFO 或 ShipState 判定。
- Phase 5 根据 `ShipEnterFleetEvent` 创建同型舰队常驻展示和攻击凭证；Phase 4 不创建攻击。
