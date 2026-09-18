# Tidebound Unity 业务工程

本目录包含独立的 Tidebound 数据、配置加载、棋盘、船移动、航道、事件契约和自动测试。所有逻辑使用 `Tidebound.*` namespace 与 asmdef 隔离，不依赖旧停车玩法代码。

教学入口：`Config/Levels/TestLevel_001.asset`，唯一布局源为旁边的 JSON；4×6、7 艘基础船、Boss 初始 HP 70。

正式密度夹具：`Config/Levels/TestLevel_002.asset`；12×18、80 艘基础船、160 个占用格、56 个空格、Boss 初始 HP 800。参考解法只用于自动化验收，不是运行时求解器。

编辑入口：Unity 菜单 `Tools/Tidebound/Level Studio`。V0 支持 schema v2 JSON 的创建、打开、网格放置／删除、容量统计、校验和保存。

详细目录、模块职责、程序集依赖、数据流、状态语义和后续开发顺序见仓库根目录 `40_项目交接文档/ARCHITECTURE.md`。当前测试结论见 `40_项目交接文档/验证记录/Phase5_高密度棋盘基础/PHASE5_VALIDATION.md`。

Fleet、Combat、Boss 表现、UI 和 DebugTools 仍为后续阶段。当前没有正式游戏场景、美术、攻击、Boss 扣血、皮肤经济或结算；未迁移旧停车逻辑。真机触控和性能尚未验收。
