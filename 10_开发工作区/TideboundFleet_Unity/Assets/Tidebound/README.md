# Tidebound Unity 业务工程

本目录包含独立的 Tidebound 数据、配置加载、棋盘、船移动、航道、事件契约和自动测试。所有逻辑使用 `Tidebound.*` namespace 与 asmdef 隔离，不依赖旧停车玩法代码。

入口配置：`Config/Levels/TestLevel_001.asset`，唯一布局源为旁边的 JSON；4×6、7 艘快艇、Boss 初始 HP 70。

详细目录、模块职责、程序集依赖、数据流、状态语义和后续开发顺序见仓库根目录 `40_项目交接文档/ARCHITECTURE.md`。当前测试结论见 `40_项目交接文档/验证记录/Phase4_航道系统/PHASE4_VALIDATION.md`。

Fleet、Combat、Boss 表现、UI 和 DebugTools 仍为后续阶段。当前没有正式游戏场景、美术、攻击、Boss 扣血或结算；未迁移旧停车逻辑。
