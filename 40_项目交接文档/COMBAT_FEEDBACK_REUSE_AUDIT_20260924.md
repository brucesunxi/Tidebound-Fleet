# 战斗动效三源码复用核查与规划

日期：2026-09-24。用户要求先查三套源码，再完成规划；**海怪独立元素、船体立体侧视图由用户提供，本轮不再生成这些素材。**范围为只读核查与规划图，未修改正式Unity代码、插件、配置或存档，未运行原工程／Unity测试。

**当日后续规则覆盖：** 用户提供火焰连击参考，明确第3次才出现整套连击特效；第1／2次仅保留普通计时，第3次起显示火焰徽章与数字。下文“第二艘显示2连击”属于旧提案。当前创作与实现候选见[火焰连击方案](COMBO_FLAME_VISUAL_PLAN_20260924.md)，尚未修改正式表现代码。

## 1. 可复用结论

| 功能 | 找到的依据 | 采用方式 |
|---|---|---|
| 金币飞向HUD | Unity的ParticleImage、CoinAttraction.prefab、UIManager内CoinEffect | 优先复用已有组件的发散／吸附曲线，另接一次攻击的真实奖励数据；不是整包接旧UIManager |
| 连击时间条 | Cocos逆向UIMgr.updateCombo／initCombo／endCombo | 借鉴填满、倒计时、续接、断链和文字弹跳；原创Unity状态，改为5秒与首次出船即启动 |
| 怪物受击 | Unity Vehicle.ShakeVehicle；Cocos Animal.onImpacted／onImpact | 借鉴局部抖动、压缩、回弹；只动海怪视觉子层，不动海面／棋盘 |
| 伤害／金币漂浮字 | Unity自带TMP的TextMeshProFloatingText；Cocos正向TipMain | 有上浮和淡出参考，未找到可直接接AttackId的成品伤害数字组件；改为一次命中触发、固定真实值、并发错位和回收 |
| 船体高光 | Unity QuickOutline；Cocos Animal.onLight；本项目ShipPrototypeAppearance.SetHint | QuickOutline是Mesh描边，不能直接给透明船图描轮廓；Animal.light是道具选中动画，不是连击奖励。复用本项目视图着色入口思路，新增独立连击强度，不能覆盖提示状态 |

**当前正式工程已含ParticleImage、CoinAttraction示例、DOTween.dll、QuickOutline与TMP漂浮示例。**本轮SHA-256对照确认上述5项与购买Unity工程相同。因此不必重新下载或重复导入插件，但“文件已存在”不等于Tidebound战斗层已接入、原预制体可以原样使用或真机已验证。

## 2. 具体源码证据

以下U/P/R路径均为仓库`00_远端接收区/已购源码与参考项目/`下对应工程，只读保留。

### U：unity--Bus_Mania_100_BugFix

- `Assets/TJ/Scripts/UIManager.cs:71`的NextLevel：76行播放coinEffect，等待3秒，78行AddCoins(50)，再等待0.5秒加载场景。**飞币存在，但这套固定金额、延迟入账和切关流程不复用。**
- `Assets/TJ/Prefabs/UIManager.prefab:4850`有独立CoinEffect组件，duration0.3、非循环、RaycastTarget关闭；5861行吸附目标为`coinsImg`。解析其RectTransform发现锚点是左上`(0,1)`，原样不是用户要的右上。
- 同Prefab约6298行粒子完成事件监听为空。旧AddCoins由协程执行，不是金币真正到达事件。
- `Assets/AssetKits/ParticleImage/Demo/Prefabs/CoinAttraction.prefab`提供独立金币吸附示例，适合提取最小视图配置；不用复制整个旧UIManager页面。
- 正式工程已有同路径`ParticleImage.cs:499～529`的attractorEnabled／attractorTarget／attractorLerp；`Particle.cs:242～283`负责坐标换算和目标插值。发射点可对齐海怪coinAnchor，终点绑定右上金币图标的RectTransform。
- 正式`ParticleImage.cs:1114`的onAnyParticleFinished没有AttackId或金额参数；2067～2081行在粒子寿命结束时调用。**粒子结束不等于精确到达，也不能“一颗粒子结束就加1币”。**一张飞币图可代表1～8金币，数值来自奖励数据。
- `ParticleImage.cs:1309`的Play会重置发射时间、burst状态；Pause存在，但不能把Pause后Play当成无损Resume。当前游戏暂停依靠会话状态／Transit时钟，不必然把Unity全局Time.timeScale置零。适配器要单测暂停、恢复、取消，不能以组件选Normal时间就认为解决。
- `Assets/TJ/Scripts/Vehicle.cs:283`的ShakeVehicle是0.2秒DOShakeRotation，323～328行播放碰撞音效、hitEffect并抖动车辆。海怪可借鉴0.2秒量级局部反馈，但不复制物理碰撞、车辆退回或整船移动。
- `Assets/TJ/Scripts/EffectsManager.cs:16`只是Instantiate，没有完整高频回收／会话清理；需要沿用本项目生命周期。
- `Assets/TextMesh Pro/Examples & Extras/Scripts/TextMeshProFloatingText.cs:115`为随机数字倒数、向上运动、淡出并递归循环，依赖Camera.main与世界坐标。可参考上浮／淡出，不能作为每次真实伤害跳字直接挂载。
- `Assets/TJ/QuickOutline/Scripts/Outline.cs:87`起依赖Renderer／MeshFilter、平滑法线和额外材质。用户后续提供透明侧视PNG时，应该使用Alpha轮廓／扫光；若提供真实3D模型，才评估该Mesh方案。

### R：cocos逆向_猪了个猪_2.4.15

- `assets/scripts/UIMgr.js:338～353`：第三次开始显示；每次刷新7秒；0.05秒调度扣时间；10／20／30／40／50次播放不同音效，文字缩放1→1.1→1。
- `GameMgr.js:254～255`经checkWin更新连击；`Animal.js:271`在离场动画完成后调用。与本项目“船尾完整驶出棋盘就开始5秒窗口”时点不同，应接ShipExitBoardEvent而非照搬检查胜利／动画结束入口。
- 前两次参考没有开始倒计时，因此不能把7替成5就结束适配。首次有效离场启动5秒；间隔小于5秒续链，恰好5秒按超时处理；部分移动、撞阻、点击空处不续。
- `Animal.js:210`的onLight播放Spine命名动画；GameMgr.onShuffle发送ANIMAL_LIGHT，用途是道具目标提示。它不是按连击自动加光的现成系统。
- `Animal.js:214～260`只偏移spine子节点，包含防重入和回原位；可作为海怪视觉子层动、逻辑锚点不动的参考。逆向脚本只研究行为，不复制进Unity。

### P：cocos源码-救救小猪

- `assets/Script/UIManager/TipMain/TipMain.ts:56`创建提示，约95～134行安排堆叠、上移与淡出；TipText本身注释掉的tween不是正在运行的成品。
- 可借鉴文字独立层和连续消息避免重叠；该提示是页面Toast，没有怪物命中坐标、真实伤害值或金币飞行。
- 在本轮游戏脚本、提示与奖励入口范围内未找到与Unity飞币同等完整的吸附动画，也未找到现成海怪伤害数字链。此为有边界的只读检索结论，不宣称所有第三方目录均无其他动画。

## 3. 最少改造的落点

1. **连击：**读取ShipExitBoardEvent与Transit.ElapsedTime；按SessionId＋ShipId去重。首次count1启动5秒，第二艘才显示“2连击”；每次有效离场续满。暂停冻结，超时count归零；不重开关卡、不罚伤害。
2. **高光：**3～5次轻青色边缘，6～9次金色扫光，10次以上更亮金白轮廓和短尾迹。优先作用于刚驶出的船和顶部代表船，不让静止80船全部常驻发光。高光强度与品质颜色、自动提示分开叠加；断链0.3秒淡出。只增强表现，不提高伤害／金币倍率。
3. **命中：**BossDamagedEvent按AttackId驱动局部闪白／压缩／回弹、`-10`跳字。读取该AttackToken对应SavedShip.Coins，显示金币图标＋真实`+N`；不能在视图再次随机。
4. **飞币：**优先适配已有ParticleImage，发射点从海怪coinAnchor换算到HUD空间，吸附点为右上金币图标。每笔奖励独立标识，粒子数量仅决定视觉丰富度。多笔重叠时复用小型发射器池，避免反复Play重置同一个发射器。
5. **数字：**金币飞达时推动右上数字的表现值并点亮图标；可靠账本先记录真实命中收入，动画结束不写钱包。跳过、关闭、恢复或重播都能直接对齐权威值。

`Tidebound.Unity.asmdef`当前显式引用Data／Core，且overrideReferences只列Newtonsoft.Json；如直接使用ParticleImage类型需增加对应程序集引用。DOTween存在但不在该白名单；简单缩放可继续使用本项目现有时间轴，避免仅为抖动再扩引用。核查不是接入，未改变asmdef。

## 4. 金币显示与当前结算规则

当前`SavedGameRuntime.PendingCoins`按已命中的唯一船汇总，`PortraitPuzzleGraybox.UpdateLabels`分开显示Coins与Pending。用户要求飞币后右上数字增加，可以先采用：主数字显示“已有金币＋本局已展示金币”，下方小字“本局＋N（待结算）”。到达按奖励值递增，结算时只是把该部分转成已入账，不再次加一遍。商店可消费金额仍读取saveService.Coins。

这是表现层方案，不暗中把每次命中改成立即永久入账；固定100只在胜利给，死局仍走已确认的部分结算规则。若后续要连普通退出也永久保留每次命中金币，属于另一个结算规则变更，本轮不实施。最终胜利奖励100在胜利保存成功后单独展示，不混入每船金币。

## 5. 美术分层和素材交接

海面／远景、海怪、前景水花、代表船、炮弹／命中、跳字／飞币／HUD分层。海怪中心受击锚点hitAnchor、金币起点coinAnchor与水线waterline固定在视觉父节点；受击只变形子节点。提供怪物透明完整图即可先做平移／缩放／闪白；眨眼、触手独立摆动等再需要表情或拆件，当前不要求用户先交全套骨骼动画。

船侧视图建议统一25～30度俯角、三分之四朝向，透明背景、不烘焙水花／高光／炮火；标记水线与炮口。仍按每个实际战斗skinId映射，主页H系列形象不自动混用。元素和船图由用户提供，本轮不继续生成单独素材。

受击候选：闪白约60ms、压缩约100ms、回弹约180ms；伤害字上浮约0.5～0.7秒；金币字短停后飞行0.65～0.9秒。同组发射间隔仍0.20秒、炮弹飞行仍约0.25秒，排队由真实战斗时钟控制。

## 6. 本轮交付与验证范围

- 两张可编辑结构规划图：`Combat_Layers_Plan_v2.svg/png`、`Combat_Feedback_Storyboard_v2.svg/png`，放入原关内小样目录并加入原预览页。
- 新图片生成请求返回额度限制，因此规划图改用原生矢量结构图与文字排版；没有生成新怪物／船元素，也没有用其他生成服务绕过限制。
- 逐项核查组件存在、文件哈希、Prefab金币吸附绑定、脚本事件与依赖。没有把代码检查写成Unity运行通过；暂停、频繁并发命中、对象池、真机和钱到账交互仍待实现验证。
- 外部源码只读；本轮没有导入第三方资产。既有第三方组件如进入发布依赖，沿用项目来源／许可记录要求，不把购买整包等同所有第三方素材授权已齐备。
