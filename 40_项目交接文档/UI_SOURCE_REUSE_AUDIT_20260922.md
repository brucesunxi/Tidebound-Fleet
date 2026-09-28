# 三套购买源码 UI 复用审计

日期：2026-09-22。范围只包含 UI 原型、页面组织、按钮／弹窗反馈和相关运行时结构。三套外部项目保持只读，未向正式工程复制其脚本、Prefab、贴图、字体、SDK 或关卡。

2026-09-23补充：[逐项素材盘点与设计学习](UI_ASSET_REUSE_STUDY_20260923.md)已检查实际PNG、切片参数和Prefab引用，并提供52件候选图册。三套均存在可复用底图/图标；下文的“结构借鉴”是当时的实施范围，不能解读为没有可用美术。具体图像的技术与视觉适用性以补充报告为准，正式接入仍记录选中项来源。

## 结论

当前可以直接用于产品决策的是“结构和行为原型”，不能直接把现成美术当正式资产。原因有三点：统一授权记录仍不完整；原 UI 与潮汐舰队的珍珠白／金边／海蓝阴影语言不一致；两个 Cocos 项目无法把 Prefab 原样导入 Unity。正式实现继续使用 Tidebound 原创 `HarborDesignTokens`、组件和 Prefab。

| 来源 | 可借鉴原型 | 处理方式 | 结论 |
|---|---|---|---|
| Unity Bus Mania | 单一 UIManager 根 Prefab、胜负层、道具购买面板、金币／广告双入口、缩放弹窗、短提示 | 重写为 Tidebound 页面控制器和通用弹窗；保留已有无依赖动效 | B：结构借鉴，不复制 Prefab／DOTween 代码 |
| Cocos 正向源码 | `UIParent` 生命周期、统一加载与页面字典、首开回调、弹性开关动画、Tip 独立层 | 映射到 Unity 页面基类／Popup Host；剥离平台登录、广告和全局单例 | B：跨引擎行为借鉴 |
| Cocos 逆向源码 | UI／Popup／Dialog／System／Notify／Guide 分层、配置式 UIMap、图鉴列表、道具说明、排行模式 | 只采用层级和状态模型；禁止复制反编译脚本 | B：架构研究，不复用代码 |

## 1. Unity 停车场源码

路径：`00_远端接收区/已购源码与参考项目/unity--Bus_Mania_100_BugFix/`

证据入口：

- `Assets/TJ/Prefabs/UIManager.prefab`：集中包含 `InGamePanel`、`WinPanel`、`GameOverPanel`、`PowerUps`、金币与提示节点；
- `Assets/TJ/Scripts/UIManager.cs:99`：短提示使用缩放进入、停留、退出；
- `Assets/TJ/Scripts/PowerUps.cs:75-176`：同一面板按道具类型替换标题、说明与图标，打开／关闭统一缩放；
- `Assets/TJ/Scripts/PowerUps.cs:186-273`：金币不足提示、金币／广告两种入口与监听清理。

可采用：

1. 一个道具弹窗骨架按数据替换内容，避免三种道具复制三套页面；
2. 关闭时移除临时监听，防止重复购买；
3. 提示层独立于业务面板；
4. 购买入口清楚区分金币和广告。

不采用：

- `UIManager.prefab` 是大体量集中对象，页面和业务强耦合，不适合作为 Tidebound 的组件基线；
- DOTween 只为简单缩放新增依赖没有收益，本项目已有 unscaled time 动效；
- 原按钮贴图、字体、金币图标和广告资源未验证逐项许可，也不符合当前风格；
- 结算、广告和金币逻辑不能由动画回调决定。

## 2. Cocos Creator 正向源码

路径：`00_远端接收区/已购源码与参考项目/cocos源码-救救小猪/`

证据入口：

- `assets/Script/Manager/UIManager.ts`：批量加载 `HomeMain`、`GameMain`、`GameOverMain`、`GameSetMain`、`SignMain`、`TipMain` 等 Prefab，并按名称放入字典；
- `assets/Script/Base/UIParent.ts`：`InitUI`、`ShowUI`、`HideUI`、`FirstOpen`、数据刷新与统一弹性开关；
- `assets/resources/UIPanel/`：页面 Prefab 目录；
- `assets/Script/UIManager/HomeMain/HomeMain.ts` 与 `GameSetMain.ts`：主页／设置页路由和状态绑定。

可采用：

1. 页面 ID 与 Prefab 配置分离；
2. 初始化、首次打开、每次刷新、显示和隐藏有明确生命周期；
3. Toast／Tip 使用独立父层，不受普通弹窗遮挡；
4. 弹窗动效参数集中管理。

不采用：

- 全量预加载所有页面会增加首包内存和启动时间；Tidebound 按核心常驻、次级按需加载；
- UI 管理器同时启动平台登录、广告和音频，职责过大；
- Cocos Prefab、脚本和 SpriteFrame 不能作为 Unity 原生 Prefab 直接复用；
- 原 `0 → 1.2 → 0.9 → 1.0` 开场幅度过大，正式基线改为 `0.92 → 1.03 → 1.0`。

## 3. Cocos Creator 逆向源码

路径：`00_远端接收区/已购源码与参考项目/cocos逆向_猪了个猪_2.4.15/`

证据入口：

- `assets/scripts/LayerManager.js`：Game、UI、PopUp、Dialog、System、Notify、Guide 七层；
- `assets/scripts/UIMgr.js`、`UIMap.js`、`GameUIConfig.js`：配置驱动的打开、异步打开、查找、移除和清层；
- `assets/game2/uiPrefab/illustratedPanel.prefab`、`animalDecPanel.prefab`：图鉴列表与详情；
- `assets/game2/uiPrefab/propsPanel.prefab`：道具说明与获取入口；
- `assets/game2/uiPrefab/rankPanel.prefab`、`rankItem.prefab`：榜单模式与自身排名固定区。

可采用：

1. 页面、弹窗、系统确认、Toast 和引导拥有固定层；
2. 图鉴使用“列表概览 → 单项详情 → 拥有／使用状态”信息结构；
3. 道具弹窗使用统一图标、说明、获得／使用行动槽；
4. 排行榜保留榜单与自身位置的双区域结构。

不采用：

- 反编译 JavaScript 只作行为证据，不能复制进正式代码；
- 页面启用时自动插屏、Banner 等平台行为不进入 UI 基类；
- 广告、分享和本地存储实现不满足当前可靠存档与离线要求；
- 旧图鉴对象、动物素材和排行视觉不属于本产品资产。

## 4. 对当前 Design System 的实际影响

| 决策 | 来源 | Tidebound 落地 |
|---|---|---|
| 固定 UI 分层 | 正向 UIManager + 逆向 LayerManager | `Background/Page/Popup/Dialog/System/Toast/Guide` 规范 |
| 页面生命周期 | 正向 `UIParent` | 初始化、首次打开、刷新、关闭、焦点恢复合同 |
| 数据驱动道具弹窗 | Unity `PowerUps` + 逆向 `PropsPanel` | 后续 `UI_Popup_Base` + 道具数据模型 |
| 图鉴信息架构 | 逆向 `IllustratedPanel` | 收藏列表、详情、获取与装备状态 |
| 反馈动画 | Unity／正向源码 | 采用较克制的原创参数，支持减弱动效 |
| 监听清理 | Unity `PowerUps` | 临时 listener 在关闭时解除，防重复提交 |

本轮已直接实现的是 Tidebound 自己的 `UI_MainMenu`、`UI_Button_Common`、`UI_Button_Main`、`UI_HUD_Resource` 与 `UI_Ship_Display`。外部项目没有任何文件进入这些 Prefab。

## 5. 后续直接复用门槛

若要把某张按钮、九宫格、字体或音效直接导入正式素材库，必须先记录：购买订单／许可范围、作者或平台、是否允许商用修改与再分发、第三方依赖、源文件路径、目标文件路径和 SHA-256。任何一项缺失时，只保留结构研究结论。
