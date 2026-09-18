# Changelog

本文件记录 Tidebound Fleet 的重要变化。版本发布采用语义化版本；日常改动先写入 `[Unreleased]`。

提交标题格式：`<type>(<scope>): <简明说明>`。

允许类型：

- `feat`：新增用户可见功能
- `fix`：修复错误
- `refactor`：不改变外部行为的结构调整
- `docs`：文档更新
- `asset`：美术、音频或其他资源调整
- `build`：构建、依赖、CI 或发布配置
- `test`：测试新增或调整
- `chore`：治理、清理及不属于以上类型的维护

示例：`feat(board): 完成棋盘数据结构`；`fix(ship): 修复船占格计算`。

## [Unreleased]

### Added

- 建立根目录治理体系、Git 忽略规则、开发宪法和项目文档结构。
- 建立本地私密配置目录及无凭据 `.env.example`。
- 建立不可变 Grid 棋盘快照、完整占格索引和船逻辑快照。
- 增加前向路径查询，返回阻挡船、空白格、落点、移动距离及船尾完整离界结果。
- 增加不可变占格事务和 TestLevel_001 七船参考序列验证。
- 增加 Phase 2 棋盘查询与事务 EditMode 测试。
- 增加单局船移动状态机、串行操作锁和类型化完成结果。
- 增加点击视图、Grid 世界坐标映射、默认 Transform 动画与可配置时间参数。
- 增加移动系统 EditMode 测试和默认视图 PlayMode 测试。
- 增加四边航道路线解析、并行转场时钟和中央入口 ExitSequence FIFO。
- 增加平滑航道路径、缩略船影视图、航道表现协调器及暂停恢复。
- 增加航道 EditMode、PlayMode 测试和 TestLevel_001 七船完整入舰验证。

### Changed

- 将 Unity 正式工作副本归位到 `10_开发工作区/TideboundFleet_Unity/`。
- 将购买源码归档到不提交 Git 的 `00_远端接收区/`。
- 将审计、设计、架构和验证资料集中到 `40_项目交接文档/`。
- `GameSession.Board` 暴露当前已提交棋盘，`InitialBoard` 固定保留开局快照。
- ShipRuntimeData 的位置、方向和状态只允许受信程序集写入，避免表现层绕过规则。
- 船完成中央入口融入后由航道核心将状态从 InLane 提交为 InFleet，并发布一次 ShipEnterFleetEvent。

### Fixed

### Removed

### Security

- 数据库连接信息只保存在被忽略的本地 `.env`，不写入仓库文档和代码。

<!-- 发布时把 Unreleased 内容移入下面格式，并保持最新版本在上方。 -->

<!--
## [0.1.0] - YYYY-MM-DD

### Added
### Changed
### Fixed
### Removed
### Security
-->
