# 外部关卡编辑器、生成器与关卡数据调研

日期：2026-09-19。范围：公开网页、作者说明和公开仓库的只读核查。未购买、安装或执行外部工具，未导入外部关卡、美术或代码。本文是采用决策，不是第三方工具验收。

## 1. 结论

网上确实有带编辑器和数百关的模板。当前最快路线仍是扩展现有 Tidebound Level Studio：我们已有真实受阻前进、反向生成、Solver 和逐步证明，缺口主要是内容筛选与运行时接线。另换整套模板会重新处理移动规则、船体形状、出口、数据格式和 Unity 依赖。

建议采用顺序：已有三源码的交互／内容组织经验 → 自有生成器生产原创候选 → 作者公开工具的工作流参考 → 必要时单独评估外部编辑器。暂不增加第四套付费源码。

“已有500关”不等于“500关可直接用于潮汐舰队”。必须分别核对规则、可取得的数据、许可范围和实际清盘证据。下表中的“作者标称”不代表本项目已运行验证。

## 2. 候选与采用决定

| 候选 | 一手来源确认的能力 | 与本项目的差异／待核实项 | 本轮决定 |
|---|---|---|---|
| The Vayuputra — Arrow Escape | 作者商店列出500+关、Unity模板、编辑器和PNG生成关卡；作者更新记录列出拖动画箭头和图片生成 | 箭头路径不等于1×2／1×3刚性船；受阻后是否保留新位置、能否独立导出编辑器及兼容Unity 2022.3均未实测 | 最接近的商业备选；学习“轮廓输入→编辑→验证”流程，暂不购买 |
| gtxPrime/arrow-escape | README描述500关、反向生成与二进制关卡包；仓库存在生成器、Solver、验关脚本及`assets/levels.bin` | Flutter/Dart；可变路径、转向点、配对等规则；其Solver没有我们的受阻位移状态，不能替代当前C#实现 | 可读算法／离线内容流水线参考；不导入500关，不沿用其验关结论 |
| LDtk＋LDtkToUnity | LDtk导出JSON；官方列出Unity导入器，导入器支持实体、字段及保存后重导入 | 通用2D编辑器，不提供我们已验证的船只移动、反向生成或Solver；需要新增数据转换层 | 将来非程序策划确有编辑瓶颈时备选；当前不用替换Level Studio |
| Depfov — Tap Away＋Level Editor | 作者商品页列出72关及编辑器，交付Construct 3源文件 | 3D方块与Construct 3，不是当前Unity二维刚性船 | 不采用，转换成本高 |
| EpicMagicGames — Tap Away Puzzle 2D | Unity Asset Store存在对应商品和授权入口 | 本次公开抓取未取得完整功能说明，不能确认关卡数量、编辑器或自动求解能力 | 信息不足，保留链接，不据此采购 |

一手入口：

- [Arrow Escape作者商品页与更新记录](https://www.codester.com/items/61352/arrow-escape-complete-game-template)、[作者自营itch.io说明](https://thevayuputra.itch.io/arrow-escape-complete-game-template)、[作者对图片编辑器的答复及演示链接](https://www.codester.com/items/comments/61352/arrow-escape-complete-game-template)。不同渠道版本可能不同；本次未下载包，未观看并验收演示操作。
- [gtxPrime公开仓库](https://github.com/gtxPrime/arrow-escape)。本次GitHub树快照：`c0cc3ea14f61382048ff9fb1959e12fcda8ed2e2`。
- [LDtk JSON文档](https://ldtk.io/json/)、[官方Unity导入器入口](https://ldtk.io/api/)、[LDtkToUnity仓库](https://github.com/Cammin/LDtkToUnity)。
- [Depfov商品页](https://codecanyon.net/item/tap-away-html5-game-construct-3/52170212)。
- [Tap Away Puzzle 2D商品页](https://assetstore.unity.com/packages/templates/packs/tap-away-puzzle-2d-348918)。

## 3. 公开500关项目的实现核查

只读核查的是固定提交，不执行其生成器，也不宣称实际500关有问题：

1. [`solver.dart`](https://github.com/gtxPrime/arrow-escape/blob/c0cc3ea14f61382048ff9fb1959e12fcda8ed2e2/lib/data/level_generator/solver.dart) 的`_simulateExit`遇到其他箭头返回null；搜索只提交整条箭头移除，没有“前进到阻挡点后保留新坐标”的分支。迁移到我们的玩法会漏掉合法状态。
2. 同文件状态预算耗尽与无解均返回null，不能借用它的结果直接判断玩家死局。
3. [`bin/verify_levels.dart`](https://github.com/gtxPrime/arrow-escape/blob/c0cc3ea14f61382048ff9fb1959e12fcda8ed2e2/bin/verify_levels.dart) 在`gridSize > 20`分支只检查`solutionOrder`非空；这一检查自身不能证明最终清空。仓库还有其他测试，本次未完整运行，不能由此断言其他验证也无效。

可借鉴的是“离线生成、确定性种子、打包前批量检查”的组织方式。Tidebound继续使用自己的状态转换器、预算状态和从序列化布局开始的逐步证明回放。二进制压缩等到JSON体积／加载测量显示必要时再做。

## 4. 许可与关卡数据采用状态

| 来源 | 本次实际看到的许可信息 | 后续直接采用前需要完成的记录 |
|---|---|---|
| Arrow Escape商业模板 | 作者itch.io允许定制后发布，禁止重新分发模板；Codester另有渠道许可条款 | 精确购买版本、订单、适用许可及关卡数据使用范围；不能把可商用理解成可上传完整模板源文件 |
| gtxPrime | README声明MIT；该提交完整文件树未发现独立LICENSE文件 | 采用具体代码／数据前补齐可保留的版权及许可文本，核对`levels.bin`及资产范围；目前仅研究，不标记“已授权导入” |
| LDtk及Unity导入器 | 编辑器与导入器分别有MIT文本；LDtk官网说明自制内容可用于商业项目 | 分别保留采用组件的通知；示例图片／tileset另看各自说明，不能将工具许可自动扩展为任意输入图片许可 |
| 3个已购源码 | 已有来源，尚无覆盖代码／关卡／美术／插件的统一许可记录 | 按具体采用项补证据；目前优先行为学习及自有实现，详见源码借鉴表 |

许可原文：[LDtk LICENSE](https://github.com/deepnight/ldtk/blob/master/LICENSE)、[LDtkToUnity LICENSE](https://github.com/Cammin/LDtkToUnity/blob/master/LICENSE.md)、[LDtk内容使用说明](https://ldtk.io/download/)、[gtxPrime README](https://github.com/gtxPrime/arrow-escape/blob/c0cc3ea14f61382048ff9fb1959e12fcda8ed2e2/README.md)。此表记录工程采用状态，不代替对未取得素材的许可确认。

## 5. 现成关卡图应该如何用

| 输入 | 推荐用途 | 转为本项目关卡的必要步骤 |
|---|---|---|
| 自制／获准使用的轮廓PNG、SVG | 给出初始可摆船区域 | 固定网格采样→窄部位／容量检查→反向生成→Solver→真实回放→触控审核 |
| 许可明确、规则兼容的坐标JSON | 外部草稿候选 | 坐标及方向转换→长度／出口检查→重新求解→去重→记录来源；原解法不能充当本项目证明 |
| 玩法截图／视频 | 分析疏密、方向、出口与节奏 | 手工提炼结构目标，制作原创布局；截图识别不是可靠的坐标和解法数据 |
| 长折线箭头关卡 | 研究轮廓和依赖组织 | 不机械切成短船；切分会改变阻挡关系、船数与解法，需重新生成完整依赖 |
| 3D方块关卡 | 研究逐层释放节奏 | 不投影成二维地图；投影会产生重叠和不同出口关系 |

图形只限制初始摆放，不能默认变成移动墙。船沿固定方向穿过空白并到棋盘外；退出判定仍按四边航道契约。港湾、鱼形等轮廓必须容纳约80艘和手机触控，不能为了图案压小格子。

## 6. 后续采用实验与停止条件

当前无需新增采购。先用现有Level Studio跑通10关流程；如后续人工制作效率成为实测瓶颈，再比较LDtk或商业编辑器。

商业模板的最小评估包只取3类样本：小教学、密集完整体量、含局部复杂阻挡。检查其是否能导出坐标数据、能否表达2／3长船、是否能保留受阻后的新位置，并送入我们的Solver和回放。若必须替换核心移动／数据层，或大部分关卡需重新制作，就停止整包迁移。此实验及许可核实完成前，外部500关不计入项目关卡产能。

自己的百关路线见[关卡内容生产体系](LEVEL_CONTENT_PIPELINE.md)；三源码具体函数与阶段映射见[源码借鉴表](SOURCE_REUSE_MATRIX.md)。
