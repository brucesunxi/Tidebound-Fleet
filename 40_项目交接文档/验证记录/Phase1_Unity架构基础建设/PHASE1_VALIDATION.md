# Phase 1 Unity 架构基础建设验收记录

日期：2026-09-18。结论：**本次约定的基础建设通过；尚不具备潮汐舰队可玩玩法或移动端发布验收结论。**

## 1. 环境检查

| 项目 | 本次实测 |
|---|---|
| Unity | 2022.3.25f1，源码要求版本一致；使用其实际编辑器执行测试 |
| 安装目录 | `/Applications/Unity/Hub/Editor/2022.3.25f1` |
| 工作工程 | `10_开发工作区/TideboundFleet_Unity`；当次测试日志保留搬迁前历史绝对路径 |
| 执行前编辑器状态 | 仅 Unity Hub 运行，没有 Unity Editor 打开工程；未读取到活动 GUI Console，因此不把当时 Console 数量写为 0 |
| Android Build Support | 已安装 |
| Android SDK | platform-tools 32.0.0；build-tools 32.0.0；platforms android-31、android-32 |
| Android NDK | 23.1.7779620 |
| OpenJDK | Temurin 11.0.14.1 |
| iOS Build Support | 未发现安装；本阶段 EditMode 测试不依赖它 |
| URP | 副本补齐 14.0.11，版本来自当前编辑器自带包目录清单 |
| JSON | 显式声明 Unity Newtonsoft JSON 包 3.2.1 |

Android 工具已安装不等于当前商店上架要求已满足。本次没有 Android/iOS 构建、签名、真机运行或上架兼容性测试。

## 2. 交付与范围

新增 32 个 C# 文件、5 个 asmdef、7 个 SO 资产、1 个原创关卡 JSON 及对应 Unity `.meta`。未创建游戏场景或正式美术。

| 验收项 | 结果 |
|---|---|
| 独立工作副本、原始文件保护 | 通过，原工程摘要与复制前相同 |
| namespace / asmdef 隔离 | 通过，纯数据／逻辑不引用引擎，Tidebound Runtime 不引用旧停车／Editor 程序集 |
| JSON 唯一布局来源 | 通过，SO 仅引用 JSON 和配置；JSON 中重复逻辑字段会被拒绝 |
| 加载与数据复制 | 通过，两局对象及源 JSON/SO 相互隔离 |
| TestLevel_001 | 通过，4×6、7 艘快艇、长度 2、伤害 10、14 格占位、HP 70 |
| Boss HP 固定快照 | 通过，开局按初始 Lv1 伤害求和；船状态或之后配置变化不回算既有单局 |
| 视觉与占格分离 | 通过，极端视觉缩放不改变逻辑长度、占格与 HP，空模型引用也能加载 |
| 合法性验证 | 通过，含缺失引用、重复 ID、方向、全部占格、越界、重叠、非法数值与溢出负例 |
| 事件基础设施 | 通过，七种负载、类型与单局隔离、取消订阅、生命周期、投递次序和异常语义 |
| 移动／航道／战斗／UI／动画 | 未实现，符合本阶段边界 |
| 可解性求解与模拟 | 未实现；布局合法性测试不冒充玩法或完整解法验证 |

## 3. 实际测试结果

使用 Unity Test Framework **1.1.33**，以 `-batchmode -nographics -runTests -testPlatform EditMode` 运行真实开发副本。

最终结果：**65 / 65 Passed，0 Failed，0 Skipped；Unity 进程退出码 0。**

最终测试时间：2026-09-18 06:40:41 UTC（北京时间 14:40:41）。

| 测试组 | 数量 | 通过 |
|---|---:|---:|
| LevelLoadingTests | 8 | 8 |
| BoardValidationTests | 29 | 29 |
| LevelJsonReaderTests | 20 | 20 |
| EventBusTests | 6 | 6 |
| AssemblyBoundaryTests | 2 | 2 |
| 合计 | 65 | 65 |

证据：[NUnit XML](Validation/EditMode-results.xml)、[最终运行日志](Validation/EditMode.log)。复现命令见 [ARCHITECTURE.md](../../ARCHITECTURE.md#9-查看与测试)。

实现过程修正了测试 asmdef 中 TestRunner 重复引用；首轮完整测试的程序集白名单也作了修正，以允许 Unity 给引擎适配程序集自动加入的标准 `UnityEngine.UI`。最终结果来自修正后的实际重跑，未跳过测试。

## 4. 错误与警告的准确口径

- 首次完整 C# 编译：**0 个 C# error，3 个去重后的 C# warning**，全部来自未修改的旧脚本：GiftBox 两个未使用字段、PowerUps 一个未使用字段；Tidebound 无 C# warning。
- 最终测试运行的增量编译日志：未出现新的 C# error / warning。
- 上述为编译日志计数，**不是 GUI Console 全部日志类型的计数**。本次没有为了获取 Console UI 数字额外打开窗口或清除用户日志。
- 无图形导入记录中有 URP fallback shader 提示；本次未验证渲染画面，不能从数据测试通过推断材质显示正常。
- 最终日志有 LicensingClient 验签／访问令牌提示，随后成功解析授权（`Successfully resolved entitlements`），测试完成且退出码为 0；没有修改账号、授权或安全设置。若之后 GUI 启动出现授权阻断，应单独处理。

C# 警告明细保存在 [FirstFullImportDiagnostics.json](Validation/FirstFullImportDiagnostics.json)。

## 5. 文件完整性与改动范围

对原工程 Assets / Packages / ProjectSettings 中排除 `.DS_Store` 的 **6,645 个文件**建立复制前摘要，并在测试后复核。

- 原始购买工程：摘要一致，未修改。
- 副本中的原 Assets：全部与购买工程逐文件一致，未删除旧资源、未修改旧代码。
- 副本中已存在文件的变化仅为 `Packages/manifest.json` 与 `Packages/packages-lock.json`，用于补齐 URP 和显式 JSON 依赖。
- 副本 Assets 中 Tidebound 目录之外没有新增文件。
- `Library`、`UserSettings` 等为 Unity 正常生成的本机缓存，已写入项目 `.gitignore`。只保留最终测试证据及首次完整编译诊断摘要，没有保留搭建时的临时生成脚本或不完整导入日志。

证据：[复制基线](SourceBaseline.json)、[最终完整性复核](Validation/SourceIntegrity.json)。摘要算法为按路径排序，将相对路径、NUL、文件 SHA256 和换行组成文本后再计算 SHA256；复制与复核使用同一顺序。

## 6. 留待后续的已知事项

1. 旧 `Assets/TJ/Scripts/Helper.cs` 无条件引用 UnityEditor；旧 SupersonicWisdom 运行时 asmdef 引用 Editor 程序集。未改动，仍需在移动端构建整改时解决。
2. 旧 Loader、SDK、停车场场景和构建场景列表原样保留；Tidebound 还没有启动场景，不能把旧工程 Play 的结果当作新玩法验收。
3. 旧广告／归因 SDK 不属于本阶段验证对象，Tidebound 基础加载不依赖它们的回调，但它们尚未从整个工程中剥离。
4. 2022.3.25f1 属于此前核实的安全公告影响版本。当前用于恢复源码环境；正式发布前要选择修复版本并重新测试，不直接用本版本产物上架。参考 [Unity 安全公告](https://unity.com/security/sept-2025-01)。
5. Android SDK 平台升级、iOS 模块、Xcode、签名、真机和商店要求另行验收。

下一阶段可从纯 Grid 棋盘移动规则开始；该阶段尚未自动启动。
