# Tidebound Unity 业务工程

本目录包含独立的 Tidebound 数据、配置加载、棋盘、船移动、航道、事件契约和自动测试。所有逻辑使用 `Tidebound.*` namespace 与 asmdef 隔离，不依赖旧停车玩法代码。

教学入口：`Config/Levels/TestLevel_001.asset`，唯一布局源为旁边的 JSON；4×6、7 艘基础船、Boss 初始 HP 70。

高数量技术回归夹具：`Config/Levels/TestLevel_002.asset`；12×18、80艘基础船、160个占用格、56个空格、Boss初始HP 800。它只验证schema、加载、输入和回放，不是正式第2关、产品容量结论或运行时求解器。

编辑入口：Unity 菜单 `Tools/Tidebound/Level Studio`。当前支持 schema v2 JSON 的创建、打开、网格放置／删除、候选尺寸、结构分析、真实规则试玩、解法录制／验证和原子保存。

上一轮Phase 5R历史原型位于 `Config/LevelPrototypes/Phase5R`：12个80～110艘固定JSON、对应布局指纹证明和指标清单。`Runtime/Core/LevelDesign` 保存纯逻辑依赖图、生产门槛、搜索和证明；`Runtime/Unity/LevelDesign` 只负责从同一JSON生成灰盒预览。

详细目录、模块职责、程序集依赖、数据流、状态语义和后续开发顺序见仓库根目录 `40_项目交接文档/ARCHITECTURE.md`。2026-09-19复跑现有自动化为119/119 EditMode、6/6 PlayMode；证据见 `40_项目交接文档/验证记录/Phase5R_基础复核_20260919/BASELINE_REVIEW.md`。

Fleet、Combat、Boss表现、UI和DebugTools仍为后续阶段。当前没有正式游戏场景、美术、攻击、Boss扣血、皮肤经济或结算；未迁移旧停车逻辑。旧12个样本均为PrototypeOnly；新LevelSolver、反向生成与7／80艘的10关尚未实现。BoardPrototypePreview直接操作棋盘快照，只是诊断预览；下一步必须接实际移动状态机与航道，再做人机验收。计划与源码借鉴见 `40_项目交接文档/PHASE5_PLUS_PLAN.md` 和 `SOURCE_REUSE_MATRIX.md`。
