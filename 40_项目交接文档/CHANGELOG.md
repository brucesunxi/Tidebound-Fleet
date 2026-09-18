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

### Changed

- 将 Unity 正式工作副本归位到 `10_开发工作区/TideboundFleet_Unity/`。
- 将购买源码归档到不提交 Git 的 `00_远端接收区/`。
- 将审计、设计、架构和验证资料集中到 `40_项目交接文档/`。

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
