# Phase 5 高密度棋盘基础验收

日期：2026-09-18

Unity：2022.3.25f1，Apple Silicon，BatchMode

结论：**自动化验收通过。** Phase 5 已完成单基础船数据迁移、schema v2、12×18 容量规则、80 船灰盒关、集中 Grid 点击、SafeArea 布局计算与 Level Studio V0。舰队、战斗、皮肤经济、正式 UI 和广告没有进入本阶段。

## 自动化结果

| 套件 | 结果 | 证据 |
|---|---:|---|
| EditMode | 110 / 110 通过 | [EditModeResults.xml](EditModeResults.xml) |
| PlayMode | 5 / 5 通过 | [PlayModeResults.xml](PlayModeResults.xml) |

Unity 两次命令行测试均以退出码 0 完成，日志中没有 C# 编译错误或失败断言。

## 数据与规则验收

- schemaVersion 固定为 2；JSON 每个船实例必须显式提供 length。
- 逻辑船型目录必须且只能包含一个 `TF_BASE_SHIP`，固定伤害 10。
- 长度只允许 2／3；完整占格必须在棋盘内且不得重叠。
- 棋盘上限 12×18；船数≤82；长度 3 的船≤8且≤总船数10%；总占格≤172；正式棋盘空格≥44。
- ShipRuntimeData 和船事件上下文携带稳定 skinId；长度 2 使用 `TF_SKIN_DEFAULT`，长度 3 使用 `TF_LONG_DEFAULT`。
- 两次加载得到独立运行时数据；修改一局不会污染另一局或源配置。
- Boss HP 只在开局按初始船数×10计算一次。

## 灰盒关验收

| 夹具 | 尺寸 | 船数 | 占格 | 空格 | Boss HP |
|---|---:|---:|---:|---:|---:|
| TestLevel_001 | 4×6 | 7 | 14 | 10 | 70 |
| TestLevel_002 | 12×18 | 80 | 160 | 56 | 800 |

TestLevel_002 开局可直接驶出的船不少于 12 艘。`TestLevel_002.solution.json` 保存 80 个唯一 shipId；自动测试逐步验证每艘在操作时均可直接出界，最终船数和占格均为 0。该序列只证明当前夹具可解，不是通用求解器。

## 输入、布局与编辑器

- BoardGridSelection 只依据当前 BoardModel 的占格查询 shipId；空格不吸附邻船。
- PointerDown 锁定目标，PointerUp 仍命中同一艘船才提交。
- GridWorldMapper 已验证世界坐标与射线回格，模型尺寸不参与命中或移动判断。
- BoardLayoutCalculator 已覆盖 360×640、390×844、430×932 安全区计算。
- Tidebound Level Studio V0 可创建、打开、放置、删除、统计、校验和保存 schema v2 JSON；非法关卡禁止保存。

## 保留门槛

自动测试不能替代以下人工／真机验收：

- 80 船正式灰盒场景中的方向箭头可读性。
- 不同手指和屏幕密度下的点击命中率。
- 目标最低 Android／iOS 设备上的帧率、内存和连续出船压力。
- Level Studio 的长时间人工编辑体验、Undo／Redo 和批量生产效率。

这些项目应在进入 Phase 6 大批量生产关卡前完成。Level Studio 的解法录制、难度评分、候选生成、Undo／Redo 和批量操作属于 Phase 6。
