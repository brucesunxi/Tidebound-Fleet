# 设置与暂停正式接入

完成日期：2026-09-26（目录沿用 9 月 25 日任务起始日期）。

用户确认 `settings_pause_v2.html` 后授权正式接入，并明确跳关在广告接入后开放、本次禁用提示；协议由本项目参照 Crystal Rescue 的组织方式编写。

## 本次实现

- 首页设置：复用正式金框、木牌、珊瑚关闭钮。框体缩短，木牌跨上缘；只有中文／EN、音乐、音效和两项协议。旧 Automatic、提示与减少动态控件不再出现在设置中。
- 局内菜单及暂停入口：共用已批准的暂停布局。框内只保留音乐／音效；框外蓝色退出回港口、金色直接重新开始、禁用橙色跳关、绿色继续。关闭按钮等同继续，Escape 可返回。
- 继续与退出保持原 attempt；重开调用原有事务，不增加确认。暂停引起的临时状态不写成永久暂停。存储失败时继续按钮保留暂停并显示局部重试文字，不新增保存失败页面。
- 跳关没有监听器，`interactable=false`；固定提示“广告接入后开放跳关”。没有增加跳关、广告、付费或奖励规则。
- 音乐与音效为独立本地偏好，分别控制两个 AudioSource；不进入钱包或关卡存档。当前使用原创程序合成的轻量循环音乐与 UI 提示音作为可工作的音频通道，没有引入第三方音频素材。未做最终配乐与真机混音验收。
- 隐私／用户协议提供中英文本地 TextAsset，支持滚动、返回与当前语言切换，无需网络或虚构 URL。参考 Crystal Rescue 的数据／用途／存储／第三方／用户选择结构，正文按本游戏当前离线功能重新编写。

## 修改范围

正式目录 `10_开发工作区/TideboundFleet_Unity/Assets/Tidebound/`：

- `Runtime/Unity/LevelDesign/PortraitPuzzleGraybox.Home.cs`、`PortraitPuzzleGraybox.cs`：入口、导航和临时暂停所有权。
- `Runtime/Unity/UI/Common/HarborSettingsPanel.cs`、`HarborSettingsIcon.cs`、`HarborAudio.cs`：新布局、图标、音频偏好与通道。
- `Resources/TideboundUI/Locale.txt` 和 `Legal/`：本地化与四份协议正文。
- `Tests/PlayMode/SettingsPausePlayModeTests.cs`：新增 4 项实质交互测试。
- `Editor/UI/SettingsPauseReview.cs`：正式组件的内存存档截图检查器。

没有改正式场景、Prefab、经济表、Grid、航道路径、战斗规则、关卡内容或存档 schema；没有清除实际玩家进度。被取消的两张保存失败弹窗及重开确认方案没有接入。其他审计项继续待审。

## 运行证据

- Unity **2022.3.25f1** 实际编译通过。
- 初次定向回归 42 项：27 通过、15 失败。用本轮修改前精确备份复测旧 38 项：23 通过、同样 15 失败，逐项结果完全一致。详见 [前后对照](BASELINE_COMPARISON.md)、[基线 XML](Baseline.xml)、[初次 XML](PlayMode.xml)。不能将其表述为全项目回归通过。
- 最终版本有效定向回归 **27/27 通过**：[Final_PlayMode.xml](Final_PlayMode.xml)。覆盖新设置／协议、音频偏好、暂停冻结、禁用跳关、继续／退出保持身份、直接重开、写入失败重试；并覆盖原有关卡入场、动画、船只与航道、补给等相关通过项。
- 正式组件在 **390×844、360×640，中英文** 下运行。共 16 张 Game View 截图、100 项窗口边界／导航检查：[visual_checks.txt](visual_checks.txt)。设置、暂停及两份协议均为实际 Unity 输出。
- [实际运行对照板](实际运行.html)。沿用正式首页港口背景；关内底图仍是当前 Unity 灰盒。未把浏览器中的整屏美术原型作为正式关卡背景冒充实现。
- 仅使用 `MemoryPlayerSaveStore`；偏好测试结束恢复原设置；截图期间关闭偏好写入。未进行 Android／iOS 真机、触控或性能验收，未发布构建。

## 协议来源与发布限制

只读参考 Crystal Rescue Next 的 `integration/backend/account-api/src/server.js` 内 `privacyPolicyHtml()`；没有复用其运营公司名、广告商、支付／云存档承诺或联系资料。

根据 [Google Play User Data](https://support.google.com/googleplay/android-developer/answer/10144311?hl=en-GB) 与 [Apple 审核指南](https://developer.apple.com/app-store/review/guidelines/) 中关于隐私说明的要求，当前提供可访问的本地说明并区分实际功能。该正文是离线开发版本草案：**正式发布前仍需确认运营主体、有效联系渠道、公开政策与协议网址，并结合最终接入的 SDK 核对数据清单。** 本轮不构成上架或法律合规验收。

## 使用入口

正常打开正式竖屏场景运行：港口左上角设置进入首页设置；关内菜单／暂停进入新暂停弹窗。协议位于设置底部。重新开始立即执行；退出游戏回港口；继续及右上角关闭恢复当前挑战。
