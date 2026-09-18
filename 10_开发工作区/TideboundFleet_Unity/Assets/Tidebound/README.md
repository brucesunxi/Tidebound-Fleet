# Tidebound Phase 1 基础架构

本目录仅包含基础数据、配置加载、复制隔离、合法性校验、事件契约和 EditMode 测试。

入口配置：`Config/Levels/TestLevel_001.asset`，唯一布局源为旁边的 JSON；4×6、7 艘快艇、Boss 初始 HP 70。

详细目录、模块职责、程序集依赖、数据流、状态语义和后续开发顺序见仓库根目录 `40_项目交接文档/ARCHITECTURE.md`。测试结论见 `40_项目交接文档/验证记录/Phase1_Unity架构基础建设/PHASE1_VALIDATION.md`。

Fleet、Combat、Lane、Ship/Boss 表现、UI、DebugTools、PlayMode 目前只预留目录。未实现移动、航道、战斗、UI 或动画；未迁移旧停车逻辑。
