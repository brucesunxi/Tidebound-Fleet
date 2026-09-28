# 三套购买源码：UI 素材盘点与设计学习

日期：2026-09-23。用户要求检查按钮、弹窗等完整素材，找出可复用部分并学习成熟 UI 设计。

> **本轮新增入口：第8～10节为真实页面观察与纠偏记录。** 前7节是此前静态审计，保留作为历史证据，里面的候选优先级与“未运行”描述不代表本轮最新状态。最新选型以《UI_ASSET_SELECTION_20260923》第“运行观察后的修订”节为准。用户新增偏好：优先学习“猪了个猪”的排版与视觉层次，Bus Mania 不作为美术质量标杆；两张目标图仍决定最终方向。没有批准正式接入方案A。

> **环境就绪后的最新补证：第11节。** Creator 2.4.15与2.4.13现已安装，两套Cocos各取得首页＋一个弹窗/列表页，均为**B类原引擎辅助运行**。三套累计6张主要截图，另有1张图鉴滚动状态。第8～10节中的Cocos“未运行”是首轮历史状态；排行Tab、正常入口A和完整业务仍未验证。没有改变选型审批状态。

**结论：三套都有可拆用的 UI 图片和已组装的页面；Unity 那套的通用按钮、面板最丰富，“猪了个猪”的成组组件和九宫格用法最值得学习，“救救小猪”适合学习简单弹窗组合。可以复用部分底图和图标，但没有证据表明其中任何一套是可直接替换潮汐舰队的完整设计系统。**

先看 [52 件素材可筛选图册](验证记录/UI素材审计_20260923/UI素材图册.html)。图册内含原图、尺寸、具体路径、切片边界、Prefab 引用及九宫格宽度演示；离线可打开。12 件列为优先候选，34 件需换肤或适配，6 件仅作主题/结构参考。这是技术与视觉评级，不是授权结论。

## 1. “完整”要分三个层次

| 完整程度 | Bus Mania / Unity | 救救小猪 / Cocos | 猪了个猪 / Cocos |
|---|---|---|---|
| 独立按钮、底板、关闭键 PNG | 有，品类最丰富 | 有，数量较少 | 有，按功能成组 |
| 组合好的弹窗/页面 | 有集中式 UIManager，含胜负、道具等 | 有设置、通用确认、签到、结算等独立 Prefab | 有道具、设置、图鉴、排行、结算等 Prefab |
| 可伸缩底板和实际切片参数 | 有；部分组件仍用 Simple | 有；通用确认确实用 Sliced | 有；道具页广泛使用 Sliced |
| 开关状态素材 | 音量、振动有开/关图标 | 设置页有结构，通用状态图较弱 | 有开轨、关轨、独立滑块 |
| 每个按钮齐全的正常/按下/禁用/选中/加载状态 | 未发现成套交付证据 | 未发现成套交付证据 | 未发现成套交付证据 |
| 可编辑的产品 UI PSD/Figma/AI | 本次资产目录未发现；只有插件示例 PSD | 本次未发现 | 本次未发现 |
| 能原样作为 Tidebound Unity 组件导入 | 同引擎，但页面/业务耦合，不宜整套导入 | PNG 可导入，Cocos Prefab 需重建 | PNG 可导入，Cocos Prefab 需重建 |

“有完整 PNG”不等于“有完整交互组件”；“有完整页面”也不等于“可跨引擎运行”。本次做文件、元数据、预制体结构和代表图像审查，未启动三个外部项目验证全部页面运行效果。

## 2. 实际扫描范围

统一源根目录：`00_远端接收区/已购源码与参考项目/`。下文 U/P/R 是报告简称。

| 项目 | 图片扫描目录 | 范围内图片文件 | 非零 Border 图片 | 本次 Prefab 引用的范围内图片 | Prefab 文件 |
|---|---|---:|---:|---:|---:|
| U：`unity--Bus_Mania_100_BugFix` | `Assets/TJ/Texture2D`、`Assets/TJ/Sprites`、`Assets/Texture2D` | 577 | 25 | 23 | 1 |
| P：`cocos源码-救救小猪` | `assets/resources/Imager`、`assets/Game/Imager` | 51 | 4 | 25 | 9 |
| R：`cocos逆向_猪了个猪_2.4.15` | `assets/game/texture`、`assets/game2/texture`、`assets/Texture`、`assets/resources` | 220 | 18 | 72 | 13 |
| 合计 | 指定图片目录，不是全项目素材总量 | 848 | 47 | 120 | 23 |

- 848 是文件数，包含游戏动物/车辆、环境、字体图集、编辑器皮肤和重复图片，**不能说有 848 个 UI 控件**。47 张 Border 图片也包含 Unity 默认/通用小底图，不等于47种正式弹窗。
- U 扫描 `Assets/TJ/Prefabs/UIManager.prefab`：60 个 GameObject，36 个带 `m_Sprite` 的组件记录（包括粒子图像组件），11 个带 Transition 的控件；不代表扫描了 U 全部 Prefab。
- P 扫描 `assets/resources/UIPanel/` 下9个页面，R扫描UI目录、notify和图鉴条目共13个。R的 `assets/game/prefab/illustrateAnimalItem.prefab` 为紧凑序列化，本次未展开，因此只有22个Prefab完成结构解析。
- “0处引用”只指本次指定Prefab范围，没有宣称素材全项目无用；运行时换图/动态加载也可能使用它。
- P有7处非空Sprite UUID未在资产meta中解析到（3种UUID）；R有7处（同一种UUID）。可能涉及引擎内建资源，尚未通过Cocos环境确认，不能据此断言素材丢失。U的2处未解析引用是Unity内建GUID。
- U全Assets下找到41个字体文件，含重复/插件字体，并非41套可用中文字体；P/R未找到打包字体文件。保持本项目已选字体，不因包内出现字体就直接替换。

机器可读证据：[完整图片清单](验证记录/UI素材审计_20260923/assets.json)、[Prefab引用及按钮配置](验证记录/UI素材审计_20260923/prefabs.json)、[范围统计](验证记录/UI素材审计_20260923/summary.json)、[52件候选](验证记录/UI素材审计_20260923/candidates.json)。

## 3. 优先复用哪些

以下路径相对于上表对应项目。候选必须放入工作副本适配，外部原目录保持只读。本次未导入正式工程。

| 优先级与用途 | 具体文件或文件组 | 证据与建议 |
|---|---|---|
| A 浅色弹窗内板 | U `Assets/TJ/Texture2D/PanelSettingsInside.png`；`PanelInsideWhite.png` | 1014×539、950×223，有Border；浅奶油色较接近现有风格。前者在根Prefab使用Sliced，后者虽有Border但被Simple引用，接入时需自己校准。 |
| A 极简伸缩底板 | R `assets/game2/texture/props/img_dj_dk.png` | 89×73，左42/下33/右42/上33，中心5×7。道具页以Sliced使用；很适合学习“小图支撑多尺寸”。 |
| A 通用面板基础 | P `assets/resources/Imager/Common/M_FloorBase9.png`；`whitesample.png` | 100×100、108×100，有Border；前者在PopUpMain同时用于面板及确认/取消按钮。 |
| A 设置开关 | R `assets/game2/texture/set/img_sz_guan.png`、`img_sz_kai.png`、`img_sz_dian.png` | 169×67关闭轨道、169×66开启轨道、56×56滑块。可学习独立轨道/滑块组装；在Unity重建Toggle，不能把原Prefab直接拖进来。 |
| A 音量/振动状态图标 | U `Assets/TJ/Texture2D/VolumeOn_icon.png`、`VolumeOff_icon.png`、`VibrationsOn_icon.png`、`VibrationsOff_icon.png` | 四张均158×130；开/关语义明确。调整视觉尺寸和色彩后再选，仍需自己的状态绑定。 |
| B 主次按钮皮肤 | U同目录 `BlueButton.png`、`GreenButton.png`、`PurpleButton.png`、`RedButton.png`、`GrayButton.png` | 同规格395×206，空面与文字分离，已配Border。底部厚度与高光可借鉴；不能将所有色系混进当前页面，也不能把Gray直接当完整禁用逻辑。 |
| B Cocos按钮族 | R `assets/game/texture/common/img_ty_btn_huang.png`、`img_ty_btn_lv.png`；`assets/game2/texture/result/img_gxg_btn_huang.png`、`img_gxg_btn_lan.png` | 通用黄绿与结算黄蓝成组，均有切片。参考主次操作对比，保持Tidebound自身色彩语义。 |
| B 道具槽/卡片 | P `assets/resources/Imager/Common/skillBG.png`、`a1.png`；U `Assets/TJ/Texture2D/ShopPanel.png` | 可做道具槽或卡片边框参考；边框和高光密度需要统一。skillBG有17处引用，说明是重复组合件。 |
| B 关闭键与资源条 | U `Close_Button.png`、`TopUI_Panel_B.png`、`TopUI_Panel_F.png`（均在Texture2D）；R `assets/game/texture/common/img_ty_btn_cha.png` | 关闭键独立，资源条前后板拆开；这是可持续组合的方法，不需每页重画整条。 |
| B 小型黄色按钮 | P `assets/resources/Imager/Common/btn4.png` | 156×72，16处引用，但没有Border；不能直接任意拉长，须新建切片/固定比例。 |
| C 仅作结构/风格参考 | R `assets/game/texture/common/img_ty_dk.png`；P `assets/resources/Imager/Common/bg1.png`；原Logo、动物图、中文烘焙标题 | 前者带装订环与爪印，后者为600×1200整幅背景；主题信息已融入，不能误当空白通用面板。 |

颜色、边框和高光已经烘焙的PNG无法像PSD那样分别改层。简单乘色只能染色，不能可靠把饱和蓝改成干净珍珠白或改掉爪印；因此“换肤”可能意味着以其结构为参考重做图形，不是保证一键变色。

素材购买记录与逐项图像/字体许可尚未在本次证据中建立对应关系，特别是带“逆向”的项目不能仅凭目录名判断权利归属。以上已完成技术与视觉评估；正式采用时再把选中项对应到现有来源/许可记录。此处不作法律结论，也不妨碍当前盘点研究。

## 4. 两个实际弹窗怎么组合

**P：通用确认框** `assets/resources/UIPanel/PopUpMain/PopUpMain.prefab`

12个节点、5个Sprite、2个Button。`allBG/bg1`承载底板，`tip`为“提示”、`score`为内容；确认/取消各有独立`Background/Label`。三处使用同一 `M_FloorBase9.png` 且Sprite类型为Sliced。值得学的是：**一个可拉伸底板 + 独立文字 + 独立操作，形成不同用途弹窗**，不是把“提示/确定/取消”画死在整张图里。

**R：道具说明框** `assets/game2/uiPrefab/propsPanel.prefab`

15个节点、10个Sprite、3个Button。外壳`img_ty_dk`、关闭键、标题板、内板`img_dj_dk`、道具图标、说明、分享/视频按钮分别存在；按钮图标和文案也是子节点。适合转化为我们自己的“标题槽/图标槽/说明槽/行动槽”。原Prefab标题仍写“暂停”，说明静态文案可能由运行时替换；本次未运行，不能把静态序列化当最终屏幕截图。

两者结构都能借鉴；Tidebound业务仍通过自己的控制器提供道具、库存、价格和结果。原广告/分享入口仅是源项目的业务例子，本轮不移植SDK或改变阶段范围。

## 5. 成熟 UI 的设计方法

这里的建议综合实际素材和本项目既有设计系统；并非断言购买源码的每个实现都成熟。

1. **先做组件家族。** 同一按钮共享轮廓、光照方向、边缘厚度和文字内边距，再区分主次和状态。U的五色同形按钮比每页随意找一张图更有一致性，但我们应收敛成现有金色主行动、浅色普通操作、明确危险操作。
2. **让皮肤伸缩，让文字保持独立。** 圆角和装饰留在不变形区域；内容区按文案扩展。九宫格保持四角，边沿只沿一个方向扩展，中间沿两个方向扩展。[Unity 2022.3 九宫格说明](https://docs.unity3d.com/2022.3/Documentation/Manual/9SliceSprites.html)。不要把标题、价格、数量、锁定原因烘焙进皮肤。
3. **设计状态，不只设计颜色。** U被扫描的11个Transition控件全部为ColorTint，没有发现配置状态图替换；P/R多数Button采用缩放或无内置Transition。并不证明它们没有脚本反馈，但也不能说“已提供全套按钮状态”。Tidebound继续覆盖Default、Pressed、Selected、Disabled、Locked、Loading；锁定解释原因，加载防重复提交，禁用同时呈现可辨认线索。
4. **弹窗分三段。** 标题/关闭位置稳定；正文高度可变并可滚动；行动区固定，主行动突出。外壳、标题装饰、内容、按钮分离，才能容纳设置、购买确认和道具说明。背景装饰不能抢正文对比。
5. **图标、视觉尺寸、点击热区分别管理。** 24px的图标不等于24px的点击区域。Android官方建议触摸目标至少48×48dp。[Android触摸目标尺寸](https://support.google.com/accessibility/android/answer/7101858?hl=zh-Hans)。本项目390宽布局中的逻辑单位必须经Canvas缩放与设备密度核对，不能把逻辑48直接宣称为真机48dp；真人真机验收仍未完成。
6. **反馈幅度服从可读性。** P部分Button为0.1秒、1.2倍缩放；适合研究反馈存在性，不宜照搬120%膨胀。沿用当前Face下沉2–4逻辑单位、根点击区不动的规范，避免按钮压到邻居或导致手指跟随目标移动。
7. **视觉和业务分离。** 可更换底图不能改变金币结算、库存或存档；动效结束不能成为奖励到账条件。打开、关闭、回退、焦点恢复、监听清理和重复点击，是完整组件的一部分。
8. **先验收最小组合，再铺全页面。** 同一个按钮、弹窗、开关和道具卡在中英文、小屏、长说明、多位价格及禁用/加载状态下都稳定，才值得扩展。不要因原PNG分辨率大就默认它缩小时更清晰，描边和光照要看实际显示尺寸。

## 6. 对潮汐舰队的具体建议

**第一优先级：复用设计方法与中性底图候选。** 以现有 `UI_Popup`、`UI_Button_Common`、`UI_Tab`、`UI_ItemCard` 为组件入口，优先试比 U浅色内板、R极简九宫格、P通用底板；从中选择一套统一的边缘语言，不同时混用三家的厚度和光照。

**第二优先级：补强状态和设置开关。** 参考R轨道/滑块拆件与U声音/振动图标成对交付，保留现有状态控制器和触控规则。按钮皮肤选择前，先验证文案占位和正常/按下/禁用/选中效果。

**第三优先级：统一结算、收藏、商店的组合方式。** 复用同一面板和卡片家族，标题、道具图、数量、价格、状态独立。排行、广告、动物主题及原始平台代码不因本次素材研究进入开发范围。

这三步是后续接入建议，本轮完成盘点和研究，没有修改生产UI、游戏规则、平台配置或存档，也未新增正式功能。

## 7. 复核与可重现性

- [scan_ui_assets.py](验证记录/UI素材审计_20260923/scan_ui_assets.py)只读指定源图片、meta与Prefab，记录图片尺寸/透明度/Border/SHA-256、Prefab引用、按钮配置；需要Python与Pillow。
- [build_gallery.py](验证记录/UI素材审计_20260923/build_gallery.py)将52件原PNG字节嵌入HTML，不裁图、不改色、不写源目录；只有九宫格演示使用浏览器渲染伸缩。
- [source_hashes.json](验证记录/UI素材审计_20260923/source_hashes.json)记录本次读取的源文件哈希；结束前重新比对，结果见同目录`validation.json`。
- 代表按钮/面板已直接查看原图；浏览器检查图册排版、筛选、路径展开和九宫格宽度交互。没有把网页演示当原游戏实机截图。
- 未运行外部Cocos/Unity项目、未运行本项目Unity测试；本轮为只读研究与文档产物，无生产代码改动，因此没有新的游戏测试通过声明。

与[前一天的结构审计](UI_SOURCE_REUSE_AUDIT_20260922.md)互补：前者主要研究代码/页面架构，本报告补足实际图片、切片、状态与视觉适配证据。底图可用性应以本次具体文件评估为准，不能概括成“三套都没有能用的美术”。

## 8. 原游戏页面观察（2026-09-23追加）

### 8.1 证据等级、入口与范围

**[已观察·首轮记录] 当时取得2张B类原Prefab辅助预览；没有取得A类正常入口运行截图。** Cocos两项当时未运行成功，不能把静态源码拆解标成C类编辑器预览。用户下载完成后的实际补证见第11节。

- [真实页面对照板](验证记录/UI素材审计_20260923/真实页面观察/真实页面对照板.html)：原始截图不改色、不修图；说明在图外。目标图、Unity证据和Cocos阻塞明确分区。
- [U01 地图首页辅助预览](验证记录/UI素材审计_20260923/真实页面观察/U01_Map_B.png) / [运行节点参数](验证记录/UI素材审计_20260923/真实页面观察/U01_Map_B.nodes.txt)。
- [U02 Shuffle弹窗辅助预览](验证记录/UI素材审计_20260923/真实页面观察/U02_ShufflePopup_B.png) / [运行节点参数](验证记录/UI素材审计_20260923/真实页面观察/U02_ShufflePopup_B.nodes.txt)。
- [记录与哈希](验证记录/UI素材审计_20260923/真实页面观察/observation_manifest.json)；[Cocos定向节点记录](验证记录/UI素材审计_20260923/真实页面观察/cocos_targeted_nodes.json)。未重新扫描素材目录或重做52件图册。

| 工程 | 源工程声明版本 / 本机工具 | 现有入口与本轮处理 | 页面结果 |
|---|---|---|---|
| U Bus Mania | `ProjectSettings/ProjectVersion.txt`：Unity 2022.3.25f1；本机已安装完全相同版本 | 原工程Loader/Map场景；正常入口会涉及玩家进度、统计与广告初始化。本轮在原工程独立副本中加载Map Canvas与UIManager原Prefab，进入Play Mode，辅助相机输出390×844 | **B**：地图首页、Shuffle弹窗。正常入口A、真实点击和完整游戏循环未验证 |
| P 救救小猪 | `project.json`：Creator 2.4.13；本轮最初只有Cocos Dashboard 2.2.2，没有Creator | `settings/project.json`起始场景UUID `226e83e6-0a2b-4b6b-8df6-91ec738834b7`；HomeMain由原UIManager页面入口管理。未找到已有Web构建或可运行副本 | **未验证**：首页、通用弹窗。环境缺口阶段停止，未安装或升级 |
| R 猪了个猪 | `project.json`：Creator 2.4.15；同样缺少对应编辑器，用户正在下载 | 起始场景UUID `5a4bf1d4-8831-428c-b9c2-5fa7f430d37a`；`MainPanel`打开`IllustratedPanel`，其onEnable含插屏调用。未找到已有可运行构建 | **未验证**：首页、图鉴。优先补这一套，不能用HTML拼图顶替 |

**[源码确认] 设计分辨率不是截图视口。** P项目设置640×960、fitHeight；R项目设置960×640、fitHeight，但R图鉴Prefab根是1080×1920。尚未运行，不能只根据项目设置断言其实际画面方向。U地图Canvas参考1080×1920、Match=0.5；本轮输出像素390×844，运行Rect单位另见节点记录，不与390 CSS px混用。

### 8.2 U辅助修改与限制

**[已观察]** 工作副本：`99_垃圾存储区/20260923_原游戏UI运行观察/BusMania/`。原源码目录不变。辅助代码仅在副本`Assets/SourceUIObservation/`，新建辅助场景；没有导入Tidebound或改其Packages/渲染设置。

1. 停用原Prefab上的游戏脚本生命周期，保留Unity UI/TMP；不用原Loader，不运行玩家、广告、支付和统计管理器。副本公司/产品名改为观察专用名，隔离PlayerPrefs。
2. 地图手动赋`level=3, startLevel=1, totalLevelCount=63, isNewLevel=false`，调用原`SetContentHeight()`。隐藏GiftPanel。没有领取礼物或开始关卡。
3. 弹窗调用原`InitializeUI()`与`SetPowerUpPanel(ShuffleCar, 原标题, 原说明, 原图标)`，等待原0.3秒打开Tween结束。9999、Level 3、900等为原序列化显示/观察数据，不代表真实账户，也不进入本项目配置。
4. 用辅助相机和RenderTexture记录Play Mode渲染。弹窗背后深色是辅助背景，不是完整原游戏场景。点击监听未经过原Start注册；未验证关闭、购买、广告和穿透操作。
5. **两轮最小修正**：第一次发现相机Canvas下`Instantiate(..., parent, true)`保留世界缩放导致关卡项巨大；先在Overlay下生成，再转换相机渲染，仍有比例偏大。第二次明确将生成项恢复为原NormalLevel/GiftLevel Prefab的本地scale=3。该修正只在辅助入口，**U01不能用来证明正常入口下的最终间距和大小**，也不能由此判定原游戏一定存在同样故障。失败原始截图留在工作区`ObservationOutput/U01_attempt*.png`，不计作主要页面。

从02:25:20 UTC建立副本到约02:38 UTC完成，未超过首轮15分钟与两轮修复上限。未继续排障。只保存安全的节点/图片证据，未归档含本机许可信息的完整Editor日志。

**[已观察] 交付核对：** 两张原始PNG已直接查看，尺寸均390×844；HTML嵌入字节与原PNG哈希一致；21项已有选型源图哈希未变，6项定向核对的原Prefab/脚本与工作副本仍一致。对照板本地file URL的浏览器打开被URL策略阻止，因此未声称完成浏览器排版复核，也未尝试绕过；静态图片/链接检查结果见[validation.json](验证记录/UI素材审计_20260923/真实页面观察/validation.json)。用户随后要求无需等待Creator下载，本轮据此结束，Cocos继续标为运行待验证。

## 9. 三个代表组件的证据链

### 9.1 主按钮：U的Play是反例；P的btn4结构值得对照

**[已观察 / 源码确认] U01 → `Canvas/MainPanel/PlayButton` → `U/Assets/TJ/Sprites/play.png`。** PNG本身4589×2160，绿色高光、黑外轮廓、深绿厚底、阴影以及“Play”文字全部烘焙在一张图中。节点没有独立文字子项；Image=Simple，Border=0，Rect约458.90×216.01，Button=ColorTint。大原图在本轮约183宽的按钮盒中依然是粗黑边与平面绿底；高分辨率不能弥补风格差异。

**[源码确认]** `U/Assets/Map/LevelMapInstantiator.cs`的Start只给Play注册声音/震动及0.2秒后LoadLevel；这段没有按压缩放逻辑。ColorTint保持布局矩形不动，但本轮没点按钮，实际输入反馈仍待验证。由于整图包含文字，扩宽会连带拉伸字形、圆角及高光；不能作为中英文动态“开始航行 / 第10关”的成品模板。**[建议] 只取“稳定根热区”原则，不采用这张主按钮，也不模仿其美术。**

**[源码确认] P链路（没有运行截图）：** `assets/resources/UIPanel/HomeMain/HomeMain.prefab` → `HomeMain/allBG/middleUI/btnParent/startGameBtn/Background` → `assets/resources/Imager/Common/btn4.png`；Label是独立子节点、字号25，Background序列化210×69，Sprite的_type=1（Sliced），但对应meta四边Border全部0。**“组件选了Sliced”不能证明正确九宫格已经建立。** 父Button为0.1秒Scale过渡、zoom=1.2；运行邻接遮挡未验证，不把120%外扩照搬到本项目。

**[已观察] btn4原图156×72已提供：** 黄橙渐变面、四处白色反光、圆角、橙色下沿、透明轮廓。它没有目标稿的完整弧面细节与足够的正式显示采样量。**原A样板新增的内容：** L32/B27/R32/T32试切片、CSS亮边/阴影、滤镜、260×94显示与两行独立文本。HTML增加的厚边不属于源PNG，也没有变成Unity资源。正式化见选型报告；不能放大后改名高清版。

### 9.2 弹窗：U02确认分层，R用于更接近需求的布局研究

**[已观察 / 源码确认] U02 → `U/Assets/TJ/Prefabs/UIManager.prefab` → `UIManager/InGamePanel/PowerUps`：**

| 原节点（相对PowerUps） | 原始素材、运行配置 | 可验证的职责 |
|---|---|---|
| `bg` | 全画布Image，无Sprite，RaycastTarget=true；位于Panel前的兄弟节点 | 遮罩视觉及UI射线阻挡候选；实际穿透未点测 |
| `Panel/BG` | `Texture2D/PanelSettingsInside.png`，Sliced；Border约159/159/159/160；Rect727.98×1135.76 | 可伸缩壳体；这里颜色/轮廓服务原蓝色主题，不是目标金框 |
| `Panel/BG/Banner/Image`与`Image_1` | `BoosterTopBanner_Side.png`，Simple；右侧X scale=-1 | 两端旗尾独立，不随中间文字一起拉长 |
| `Panel/BG/Banner/Title`及子`Title` | `BoosterTopBanner_Middle.png`，Simple；独立TMP，运行标题Shuffle | 标题底与动态文字分开；无切片的中段也不能任意拉宽 |
| `Panel/BG/Banner/Close` | `Close_Button.png`，Simple，独立Button，ColorTint | 关闭控件独立定位；候选图自身没有A的新增奶油金环 |
| `Panel/BG/info`与`info/Info` | `PanelInsideWhite.png`，Image仍为Simple；TMP约38.3并自动缩字 | 说明与底板分开，但长内容只缩字不滚动，不宜照搬 |
| `Panel/BG/icon`及`icon/Image` | `PanelSettingsInside.png`作内板，**此处是Simple**；`Shuffle_icon.png`是独立等比图 | 同一PNG在不同节点的用法不同；有Border不代表实际节点启用了切片 |
| `Panel/BG/useWithCoins`、`useWithAds` | `GreenButton.png` / `PurpleButton.png`；独立TMP、币图/视频图；Image为Simple | 操作区与图标分层；“USE      900”用空格给金币让位，动态价格下不可靠 |

**[源码确认]** `PowerUps.cs:141–177`运行时覆盖title/info/icon，打开/关闭缩放Panel至1/0（0.3秒），遮罩在关闭动画结束后隐藏。这里不存在正文ScrollRect：短道具说明可以用，长收藏列表不能沿用整块固定坐标方案。脚本的动画重入保护被注释，不能把其生命周期照抄为成熟模板。

**[源码确认] R图鉴的布局链（未运行）：** `assets/game2/uiPrefab/illustratedPanel.prefab`根1080×1920；外壳`img_ty_dk`960×1500、Sliced；title中心Y≈642，关闭Y≈670；收集进度Y≈511；滚动区940×1100、中心Y=-90；`scrollView/view`有Mask。title、close、progress均是ScrollView的兄弟，滚动不会带走它们。`bg`带`cc.BlockInputEvents`。`IllustratedPanel.js:46–60`填进度与列表数据，onEnable含插屏入口，本轮未调用。

**[建议]** 学习R的“外壳内固定头区＋独立滚动内容区”，而非U的蓝面板皮肤；本项目底部保留一排四分类，Viewport下边缘必须停在固定Tab区上方。`img_dj_dk`至多解决中性内填充/卡片内衬，**无法提供完整金色外框、起伏顶沿、外部投影、木牌或关闭环**。这些层必须另有真实资源。

### 9.3 卡片与Tab：优先R，源码已确认，运行证据待补

**[待验证] 这一案例尚缺原引擎页面截图，证据链不完整；不是三个案例全部运行通过。** U01的关卡列表仅证明“状态图＋独立文本＋滚动容器”的分离，不拿关卡圆点冒充收藏卡片。

**[源码确认] 卡片链：** `illustratedPanel/scrollView/view/content` → `ListView.itemRender` → `assets/game/prefab/illustrateAnimalItem.prefab`（原节点段可读取，尾部紧凑元数据未作引擎解码）→ 根290×366，底图`assets/game/texture/illustrated/img_tj_dkzhi.png`（Border全0）；`gray`、`animal`同为183×119、Y=35；`aName`独立在Y≈-134.367。序列化例图分别引用`animalGray/乌龟.png`及`animal/乌龟.png`，UUID与原meta相符。

`illustrateAnimalItem.js:148–169`按数据name加载两张图片，用`index >= Global.user.illustrateLock`决定彩色动物是否显示、名称是否“未解锁”、点击是否有效；onDisable移除触摸监听。**源项目只有解锁/未解锁证据，没有“本次使用中”状态条与勾号族**。底图无现成九宫格，不能压缩成四列后假定边缘仍合适。

**[源码确认] 列表布局：** 原图鉴ListView type=3（Grid）、startAxis=2（Vertical）、spaceX=20、spaceY=60、left=10，right默认0。`ListView.js:56–80`进入页面后将Content.anchorX从Prefab的0.5改成0，anchorY=1，按条目尺寸重算列数与高度。按当前参数计算 `floor((940-10)/(290+20))=3列`，30项时10行、内容高`10×366+9×60=4200`。这是**源码推导值**，不冒充实际屏幕读数。它保留图像区和名称区、以垂直留白分隔行的做法可参考；三列、动物资产与解锁阈值不迁移。本项目明确保持四列，按可用宽度反求卡宽，而非从原卡宽反推列数。

**[源码确认] Tab链：** `assets/game2/uiPrefab/rankPanel.prefab` → `rankPanel/worldBtn` / `friendBtn`（各250×107、Y≈-665.018，Sprite=Sliced；独立dec字号50）→ `assets/game2/texture/rank/img_ph_btnlan.png` / `img_ph_btnhuang.png`（已有Border见selection_sources）。`RankPanel.js:168–193 changeMode()`用同一type决定两Tab皮肤/字色、scrollView/selfRankItem与subContext的显示，再请求数据。原代码实际把**蓝色赋给当前模式**，黄色赋给另一模式，不是本项目金色选中规则。异步loadSprite完成时没有再次核对当前type，快速切换的最终一致性待验证，不照抄异步赋图方式。

**[源码确认 / 待验证] 固定底部区域也有反例：** R排行scrollView中心Y=40、高1000，底缘=-460；selfRankItem中心Y≈-521.249、高150，顶缘≈-446.249；按序列化默认中心锚点计算，两者约重叠13.75设计单位。只能说明几何风险，实际遮挡需运行确认；不能盲目照搬坐标。推荐用真实区域边界计算Viewport，不让底栏盖住最后一行。

**[建议] 本项目三态由已有业务驱动：** `!OwnsShowcase(id)`→锁定（锁图、解锁条件、禁用选择）；已拥有且`SelectedShowcaseId==id`→使用中（勾＋绿色状态条＋选中边缘）；其余拥有→已拥有（中性状态条）。换图/切Tab不写存档；只在原SelectShowcase成功后更新选中态。皮肤和状态映射采用已加载的资源，避免异步返回顺序覆盖当前分类。

## 10. 对本项目的六项具体纠偏（均为建议，待批准）

本节只涉及显示组件、布局与资源；没有实施。以下源路径以`10_开发工作区/TideboundFleet_Unity/Assets/Tidebound/Runtime/Unity/`为根。旧Unity截图用于定位差距，不是设计方向。

### ① 正式主按钮与普通入口：调整皮肤和可见比例

- **当前问题：[源码确认]** `LevelDesign/PortraitPuzzleGraybox.Home.cs:166`把CTA设为`w-86`×104（390宽时304×104）；目标图目测约252宽，需重新标定。`CreateHomeEntry:107–127`会覆盖图标uvRect并放进72×61盒，保留图标不代表当前裁切/留白已正确。
- **原游戏中的实际证据：[已观察]** U01按钮把Play烘焙进底图，不利于动态文案；P的btn4把Label拆开，但Scale=1.2只经源码确认。
- **可迁移的方法：[建议]** 固定交互根，Face、文字、图标独立；按钮按下不推动相邻布局。已有`UI_HomeEntry_Pearl_v2`、航海图标保留，按可见Alpha包围范围重新核对视觉大小，避免重复裁切。
- **需要调整：** 新主按钮皮肤；后续修`LayoutHome`与`CreateHomeEntry`的显示赋值；沿用`HarborButtonRelief`根热区不动的实现，重新标定纹理下沉量。旧绳结/浪花装饰不混入新Face。
- **保留业务逻辑：** 开始/继续关卡及各入口路由、库存、存档不变。
- **验收方式：** 390×844、360×640、360×800下两行动态文案不压边；按下/释放时根Rect与邻居坐标不变；中英文与长关卡数字均可读。
- **是否仍缺素材：** 缺正式CTA，不缺普通入口底板与图标。

### ② 完整弹窗外框：填充、轮廓与固定区域分开

- **当前问题：[已观察]** A的金边仅CSS；img_dj_dk只够内衬，当前正式金蓝面板也不符合目标。
- **原游戏中的实际证据：** U02实际分离Banner/Close/icon/info；R图鉴将title/close/进度置于ScrollView之外（源码确认）。
- **可迁移的方法：[建议]** 外框、奶油填充、木牌、关闭键、遮罩、Viewport、固定Tab独立；有起伏的顶沿另拆固定比例件，不把木牌做九宫格拉长。
- **需要调整：** `CollectionPanel.cs:219–226`里的标题区/余额区/Viewport定位与`UI_Popup`表现层；替换完整外框资源，保留HarborHeader标题素材。遮罩先于内容接输入、最后随关闭释放。
- **保留业务逻辑：** 原开关页、返回和购买入口；不复制源游戏广告/全局管理器。
- **验收方式：** 长列表滚到首尾，标题/关闭/Tab坐标不变；点暗背景不触发底层首页；关闭动画中仍阻挡背景输入。这些输入行为本轮尚未验证。
- **是否仍缺素材：** 完整金框、起伏顶沿、正式关闭环；img_dj_dk不抵扣这些缺口。

### ③ 形象卡片：四列与三态都要由实际数据驱动

- **当前问题：[源码确认]** `CollectionPanel.Showcase.cs:46–55`仍使用`cardW=(width-18)/2`及`i%2、i/2`，卡高218；只改Prefab仍会被排回两列。OwnershipText和Preview布局也在这里覆盖。
- **原游戏中的实际证据：** R卡片将底图、彩色/灰图、名称独立，状态由数据更新；但三列且只有解锁两态（源码确认，未运行）。
- **可迁移的方法：[建议]** 明确四列，以`(可用宽度-左右内边距-3×列距)/4`求卡宽；预览保持比例，名称和状态各有独立槽位，不用灰色覆盖全部可读信息。
- **需要调整：** 仅`LayoutShowcases`、`PresentShowcases`的视觉映射及卡片表现层；船图保持。短状态条写“使用中/已拥有/未解锁”，长解锁条件放选中详情或提示，避免四列卡内挤成数行。
- **保留业务逻辑：** `ShowcaseCatalog`、`OwnsShowcase`、`SelectedShowcaseId`和`SelectShowcase`保存成功/失败语义全部保留。
- **验收方式：** 小样模拟三种状态与多行数据，不写业务配置；每行严格四张；选中变更仅一张显示勾；保存失败不假选中；最低视口最后一张卡不被底栏盖住。
- **是否仍缺素材：** 卡边、三态条/勾/锁及选中边缘；船图不缺。

### ④ 分类导航：状态和内容一起切换，纠正运行时染色

- **当前问题：[源码确认]** `UI/Common/HarborUI.cs:57–60 SetSelected`把选中色写成Cream/Aqua混合；`CollectionPanel.cs:223、256、260`同时排四分类与底部收藏/抽取/补给三入口，并重刷选中色。Prefab里改成金色会被覆盖。
- **原游戏中的实际证据：** R RankPanel用同一type切皮肤和内容开关，但异步赋图没有过期校验；不照搬黄/蓝配色和远端排行依赖。
- **可迁移的方法：[建议]** 以当前category一次更新四Tab与内容，用已加载的金/棕状态皮肤；形象弹窗只呈现一排四分类，额外全局入口转由已有页路由承接，不删除业务能力。
- **需要调整：** CollectionPanel显示布局与HarborUI的显式状态皮肤接口；避免全局SetSelected统一染青色影响卡片和Tab两种不同状态。
- **保留业务逻辑：** SelectCategory类别映射及收藏/抽取/补给的原路由；不重写UI框架。
- **验收方式：** 快速连续切四类，每帧选中标记与可见内容一致；重开页及换分辨率不会恢复旧颜色或多排导航。
- **是否仍缺素材：** 金色选中、棕色未选中Tab成品。

### ⑤ 资源栏：目标胶囊优先，图与数字单独排版

- **当前问题：[源码确认]** Home.cs:154用112×54盒；`HarborUI.Button:68–84`还会对普通控件写入旧`UI_HUD_Capsule_v1`及固定SliceUV/SliceSize。后续需按调用点区分，不能以全局普通按钮换图牵连所有控件。
- **原游戏中的实际证据：[已观察]** U02顶部可看到币图、数字、B/F底板分层；原色仍偏黄硬边，仅适合作结构对照。
- **可迁移的方法：[建议]** 奶油胶囊前景、可选薄底影、锚币图、动态余额分槽。目标图目测约86宽只是标定起点；长余额按实际字体扩宽或采用已批准的数字格式，不能随意改经济显示规则。
- **需要调整：** 资源栏独立皮肤引用及Home显示尺寸；不沿用普通按钮的厚底/旧蓝金纹理覆盖。B/F保持参考候选。
- **保留业务逻辑：** 钱包余额读取、金币增减和商店路由。
- **验收方式：** 0/999/99999等仅作小样模拟数字，不与币图相碰；390/360宽同一轻薄轮廓；透明边缘在海蓝和奶油底均不出黑边。
- **是否仍缺素材：** 轻奶油胶囊正式底图，现有B/F不视为满足目标。

### ⑥ 布局赋值归口：防止Prefab正确、进页恢复旧值

- **当前问题：[源码确认]** 本项目Home/LayoutShowcases/CollectionPanel.Layout/HarborUI.SetSelected多处在创建、刷新与resize时覆盖尺寸、图标、色彩；形象Content高度还固定640。仅换贴图无法解决。
- **原游戏中的实际证据：** U辅助预览暴露世界/本地缩放继承差异；P UIParent.InitUI覆盖Widget四边为0、ShowUI改scale；R ListView.init把anchorX改0并重算Content大小。均说明需记录“运行后值”，不能只看Prefab。
- **可迁移的方法：[建议]** 每个小样建立简短“Prefab默认→唯一布局函数→数据状态刷新”归属表；布局负责Rect和内容高度，状态只改文字/皮肤/可交互性，不重新套旧布局。
- **需要调整：** 上述显示赋值位置局部收敛，Content高度按行数、卡高、行距计算，Viewport按头尾区域相减。保留原ScrollRect/RectMask2D，不移植源项目虚拟列表框架。
- **保留业务逻辑：** 关卡、存档、经济、收藏服务与目录均保留。
- **验收方式：** 小样打开→切类→关闭重开→三分辨率切换后记录实际Rect、Sprite、选中态；确认无旧纹理/两列/浅青Tab恢复。未实施前均为待验证。
- **是否仍缺素材：** 本项不额外增加美术清单，依赖前五项获批资源。

## 11. Creator安装完成后的原引擎补证（2026-09-23追加）

### 11.1 完成范围与真实性

**[已观察] 本机实际安装路径：** `/Applications/Cocos/Creator/2.4.15/CocosCreator.app`、`/Applications/Cocos/Creator/2.4.13/CocosCreator.app`，版本与对应工程一致。用户自行安装，本轮没有升级编辑器。用原版本CLI构建Web-mobile，在本机回环地址打开原Cocos引擎；不是HTML素材拼图。

| 编号 | 工程 / 页面 | 类别、视口与入口 | 原始证据 |
|---|---|---|---|
| R01 | 猪了个猪 / 首页 | **B**，390×844；辅助GameMain加载原`game/uiPrefab/mainPanel`，原MainPanel生命周期运行；模拟拼图进度0 | [原始PNG](验证记录/UI素材审计_20260923/真实页面观察/R01_Home_B.png) / [运行节点](验证记录/UI素材审计_20260923/真实页面观察/R01_Home_B.nodes.json) |
| R02 | 猪了个猪 / 图鉴 | **B**，390×844；辅助入口加载原`game2/uiPrefab/illustratedPanel`；模拟解锁6/30，原ListView/卡片脚本负责组装 | [原始PNG](验证记录/UI素材审计_20260923/真实页面观察/R02_Collection_B.png) / [运行节点](验证记录/UI素材审计_20260923/真实页面观察/R02_Collection_B.nodes.json) |
| R02-S | 同一图鉴 / 滚动后锁定状态 | **B补充状态**，390×844；在原ScrollView上实际拖动，无新页面 | [未加工滚动截图](验证记录/UI素材审计_20260923/真实页面观察/R02_Collection_scrolled_B.png) |
| P01 | 救救小猪 / 首页 | **B**，390×844；辅助UIManager只加载原HomeMain、调用原UIParent.InitUI及HomeMain.ShowUI | [原始PNG](验证记录/UI素材审计_20260923/真实页面观察/P01_Home_B.png) / [运行节点](验证记录/UI素材审计_20260923/真实页面观察/P01_Home_B.nodes.json) |
| P02 | 救救小猪 / 通用确认框 | **B**，390×844；首页上加载原PopUpMain并调用原ShowUI；辅助文案“是否返回首页？”，业务回调为空 | [原始PNG](验证记录/UI素材审计_20260923/真实页面观察/P02_Popup_B.png) / [运行节点](验证记录/UI素材审计_20260923/真实页面观察/P02_Popup_B.nodes.json) |

**[已观察]** 四张主要截图均在390×844浏览器视口、390×844引擎Canvas下取得；保留原调试性能文字，未修图、未裁图、未滤色。相应运行节点记录与截图同页、在布局完成后生成；R02的节点记录是初始状态，**不拿它冒充拖动后的节点数据**。原Unity两张保留，累计主要截图为6张，未遍历其他页面。

**[已观察] 有限交互结果：** R02实际拖动列表后，卡片内容变化，标题、进度和外框留在原位；点击原关闭控件经原事件调用辅助GUI返回首页。P02点击取消，经原`onClickCancelFun → HideUI`后弹窗消失，首页仍显示。两项不等于正常路由/业务已完整通过。没有测试广告、支付、分享、排行服务、真实存档或真机；背景输入穿透尚未点测。

### 11.2 辅助修改、计时与保护范围

- **[源码确认] R工作副本：** `99_垃圾存储区/20260923_原游戏UI运行观察/PigReverse/`。只把副本`assets/scripts/GameMain.js`替换为辅助入口；原Scene、MainPanel、IllustratedPanel、ListView、卡片Prefab和图片保留。`Global.user`仅注入解锁6项、关卡3、拼图进度全0；`Global.platform`方法全部为空实现并记录被抑制的调用；日志实际确认`showInterstitialAd`被抑制。辅助GUI仅承接图鉴打开/关闭，不走原登录初始化；页面根置于原Canvas/gameNode中心。原地图动态业务未初始化，首页中央呈现未揭开的拼图，不代表玩家实际进度。
- **[源码确认] P工作副本：** 同目录`PigRescue/`。仅修改副本`assets/Script/Manager/UIManager.ts`：原onLoad/start改名保留但不调用；辅助入口只加载HomeMain/PopUpMain，保留原页面的InitUI/ShowUI/HideUI。跳过HomeMain中平台身份分支，按H5状态隐藏openid节点；声音实例替换为空实现，不初始化音量存档；GameData.sizeType=1（当前竖屏条件下原逻辑也为1）。原启动进度背景隐藏；没有加载关卡、签到或SDK流程。
- **[已观察]** 两套均一次构建成功，应用预览未出现error/warn；首轮补依赖/引擎修复次数为0，辅助入口本身是预先声明的观察装置。R从建立副本到主要截图约6分钟，P约5分钟；未超过每套15分钟上限。精确开始/结束时间见manifest引用的session。
- **[已观察]** 页面用的12项Cocos原Prefab/组件脚本与工作副本哈希相同；原外部目录未写入。Tidebound正式工程没有写操作，前后Git diff numstat一致（用于辅助核对，不代表已有用户改动为空）。原始资产选型21项哈希也未变。CLI日志只在临时目录，没有把含编辑器账户/源构建配置的完整日志归档到报告。

### 11.3 三个代表案例现在得到什么证据

#### A. 主按钮：P01给btn4补上真实显示尺度，R01提供反例

**[已观察 / 源码确认]** P01中链路仍是`HomeMain/allBG/middleUI/btnParent/startGameBtn/Background → btn4.png → 独立Label`。运行Background=210×69设计单位，Canvas有效宽750；换算到390宽画面约**109.2×35.9像素**。原图156×72在这里是缩小显示，本轮观察到的边缘与白色反光可读性，不能推导它在目标约252宽CTA下也足够清晰。原节点Sprite=Sliced，但源Border=0的结论未变；本轮没有尝试改变原按钮宽度。

**[已观察 / 源码确认]** R01的`mainPanel/startBtn → assets/game/texture/mainPanel/ksyx.png`为一张含“开始游戏”文字的Simple Sprite，463×176，无文字子节点。`MainPanel.onLoad → Util.btnAnimation`每秒在scale=1与1.1之间循环；运行节点采样到1.1。这是吸引注意的脉冲，不是按压反馈。对本项目而言，文字必须独立，主按钮不靠持续缩放弥补底板质量；也不复制原按钮纸边和黑描边。

**[建议] 对M1的修订不是换候选，而是提高验收明确度：** btn4保留形状参考，正式金面/薄底沿新制；同时放在约109宽原使用尺度与目标尺度比对，后者才决定是否合格。A的CSS高光/边缘仍非源图自带，不能因原项目成功显示而撤销高清化任务。

#### B. 弹窗：R02验证头区与滚动体分离；P02验证简单槽位的局限

**[已观察]** R02：图鉴外壳完整包住内容；顶部独立标题/关闭与收集进度，卡片只在其下方视口滚动。原壳体960×1500和ScrollView940×1100的参数实际生效；显示尺寸约347×542与339×397像素。原Canvas参考1080×1920、FitWidth，在390×844下可用逻辑高**2337.23**；并非先前settings里的960×640横屏效果。应以场景Canvas与运行参数共同判断。

**[已观察]** R02-S：剪影项滚动到新的位置，但蓝色头区和“6/30”不动，Mask将上下出界卡片裁掉。这里有证据支持本项目“固定头区＋滚动正文”；原图鉴没有本项目一排四分类，底部Tab仍需单独设计。原背景遮罩存在，尚未验证底层点击是否穿透。

**[已观察]** P02：`allBG/bg1`是385×310的M_FloorBase9九宫格底板，标题/正文独立；两个按钮120×60分别在(-100,-90)/(100,-90)，约62×31屏幕像素。当前运行下按钮面与内板对比很弱，只有文字比较明显，不能作为本项目主次按钮质量标杆。浅色内板本身不提供完整金属外壳，依然不能拿它或img_dj_dk顶替M2。

**[建议]** 采用R“稳定头部—正文视口—固定底部”的区域关系；不照搬蓝壳、窄内边距或P的弱对比按钮。目标形象页的金框、木牌和四Tab仍按原制作计划，后续在Unity小样中检查正文边界与固定底部。

#### C. 卡片与状态：R02/R02-S把源码推导补成已观察

**[已观察 / 源码确认]** R02运行链：`illustratedPanel/scrollView/view/content → ListView → illustrateAnimalItem → img_tj_dkzhi + animal/animalGray + aName`。实际是三列，内容宽940、高**4200**，Content位置(-470,550)，与此前源码推导相符。30条数据只实例化**12张卡片节点**循环更新；这是原ListView的可见区复用，不是截图里只有12项收藏。

**[已观察]** 前6项彩图＋名称；之后剪影＋“未解锁”。滚动后出现新的剪影卡而标题不动。背景、插图和名称有明确层次，折角留在固定卡片底图里，没有与名称一起伸缩。卡片名称描边较重、两态规则也较简单；不能据此认为原游戏已提供本项目“使用中/已拥有/未解锁”的完整族。

**[建议]** 本项目保留船图和现有OwnsShowcase/SelectedShowcaseId；借鉴预览区与状态区分离，卡宽由四列计算。锁态增加独立锁标/短状态而不只整体降透明度；是否使用真正灰化或剪影须在小样中确定。当前只4艘展示船，无需为学习原实现而移植其虚拟列表；先确保Content按行数计算、末行可达，业务和框架不扩大。

**[待验证]** R RankPanel没有新增运行页面，金/棕Tab不能因此标成已验证成品。此前对异步赋图、固定底部几何重叠的记录继续保留为源码风险，不升级为已复现缺陷。

### 11.4 对选型的影响与停止点

**[建议] 保持原制作方向。** 保留本项目Pearl底板、航海图标、展示船和木牌；btn4仍只参考；B/F仍只对照；完整金框、卡片三态、金/棕Tab、正式CTA和奶油胶囊仍需真实制作。新的证据增强了R排版方法的依据，没有把它的蓝色主题、三列布局或烘焙文字按钮升格为本项目标准。

**[待用户确认]** 六项纠偏与M1～M5范围未扩大。完成此次真实页面补证后停止；下一步仍需用户确认具体制作方案，之后才做两Unity小样，不执行正式首页/收藏页接入。
