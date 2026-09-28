# V2-C2 主页重构与 UI Design System 基线验证

日期：2026-09-22  
Unity：2022.3.25f1  
验证工程：`/tmp/TideboundLaneEntryVerification`，由正式 `Assets/Tidebound`、正式 Packages 与正式 URP 配置建立的隔离副本。

## 交付范围

- 主页改为 `UI_MainMenu` 模块树：Background、TopHUD、PlayerShipView、LeftMenu、RightMenu、MainActionButton、Notice；
- 船展示位从旧版最大 304 dp 提升到最大 350 dp，保留水线、反光、轻浮动和页面失焦／减弱动效冻结；
- 左侧固定收藏、抽奖、补给；右侧固定每日奖励、活动、排行；
- 顶部设置、船长等级、金币统一为珍珠白／金边／海蓝厚度胶囊；
- 通用入口统一图标、名称、状态、厚底、按压与图标轻浮；状态文字收回卡片内部；
- 主按钮统一为金色面板、深金厚底、内描边、顶部高光、船锚和低幅辉光，文案为“开始航行 / Start Voyage”；
- 背景源图不改写，运行时降低饱和度并轻度模糊，顶部和侧边增加聚焦遮罩；
- 新增五个可加载 Prefab 与确定性构建入口；
- 新增完整 Design System 文档与三套购买源码 UI 只读审计。

本轮没有修改 Grid、移动、航道、战斗、经济、存档、广告或支付规则。

## Prefab 合同

实际资源：

- `UI_MainMenu.prefab`
- `UI_Button_Common.prefab`
- `UI_Button_Main.prefab`
- `UI_HUD_Resource.prefab`
- `UI_Ship_Display.prefab`

构建入口：`Tidebound/UI/Rebuild Design System Prefabs`。新增 EditMode 测试校验五个资源可加载、没有 Missing Component、主页模块树和按钮子节点合同完整、主按钮使用真实船锚组件。

## 自动验证

| 测试 | 结果 | 说明 |
|---|---:|---|
| EditMode | 435/435 通过 | 含 11 项新颜色／Prefab 合同用例 |
| PlayMode | 72/72 通过 | 完整回归，含主页导航、弹窗输入隔离、按压／取消、减弱动效、小屏、船展示与 URP 体积表现 |
| Prefab 构建 | 通过 | Unity 批处理构建退出码 0，无编译错误 |
| 运行截图 | 4/4 输出 | 中／英 × 360×640／390×844 |

测试原始结果：`EditMode-results.xml`、`PlayMode-results.xml`。

## 运行截图

- `Showcase_Home_Chinese_390x844.png`
- `Showcase_Home_Chinese_360x640.png`
- `Showcase_Home_English_390x844.png`
- `Showcase_Home_English_360x640.png`

人工检查：两档竖屏均无按钮越界；顶部三组 HUD 无重叠；左右各三入口保持同宽、同圆角和同阴影；状态文字位于卡片内部；船为第一视觉；主按钮不遮挡底部码头主体；中英文主行动、入口和状态已切换。

## 三源码借鉴边界

Unity Bus Mania 只借鉴了单道具弹窗骨架、临时监听清理和短提示反馈；Cocos 正向源码只借鉴页面生命周期与 Tip 独立层；Cocos 逆向源码只借鉴 UI／Popup／Dialog／System／Notify／Guide 分层和图鉴／道具／排行信息结构。没有复制三者的 Prefab、脚本、反编译代码、美术、字体、SDK 或广告流程，详见 `UI_SOURCE_REUSE_AUDIT_20260922.md`。

## 尚未冻结

- 四张运行图是桌面 Game View 证据，不代表 Android／iOS 真机验收；
- 当前船和六个功能图标沿用已批准的 C2 项目资源，后续正式资产仍要遵守同一 Token、光源和图标占比；
- `UI_Popup_Base`、`UI_Tab_Common`、`UI_Item_Card`、`UI_Toast` 与 `UI_Reward_Reveal` 已进入规范，需在对应页面实施时建立实际 Prefab；
- D1 及核心玩法后续工作仍按既定阶段边界推进。
