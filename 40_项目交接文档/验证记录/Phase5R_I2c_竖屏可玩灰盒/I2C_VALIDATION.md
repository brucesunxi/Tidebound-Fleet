# Phase 5R I2c：统一竖屏可玩灰盒

日期：2026-09-19。完成T-U01～02运行时原型及T-P05a最小候选校验入口。**仍未完成真人触控、方向识别和最低Android设备G1，不冻结14×18，不进入Boss战斗。**

## 打开与操作

正式工程菜单：`Tools > Tidebound > Open Portrait Puzzle Graybox`，进入Play。独立场景：`Assets/Tidebound/Scenes/Phase5R_PortraitGraybox.unity`。Game视图选择手机竖屏比例；不从旧Loader进入，不修改项目启动／构建场景列表。

- `<`／`>`切换十关；Restart按保存的JSON重开；Pause／Resume同时暂停移动动画和航道计时。
- 点击船的任意占格选中同一条船；按下／松开必须属于同船，移出棋盘取消，第二个触点不能覆盖第一个触点。水面、航道和预留区不选船。
- Hint对当前状态求解并高亮首步；Auto从当前棋盘自动演示；Stop停止后续点击，已开始的移动完成后停稳。求解耗尽预算显示Unknown语义，不冒充死局。
- 顶部是关卡操作与Boss／入舰计数占位，中间是统一14×18棋盘与外围航道，底部三个道具禁用且标明Reserved。当前为英文灰盒标签，后续再做正式UI与本地化。
- 出棋盘后在外围航道缩小并按FIFO入舰，顶部累计次数；此处没有攻击或扣血。

## 复用与边界

直接复用既有 `BoardGridSelection → ShipMovementController / ShipMovementView → ShipMovementSystem → TransitSystem → LaneTransitController`。船的动画完成回调提交Grid事务，航道控制器按状态驱动视图。旧 `BoardPrototypePreview` 继续用于静态诊断，未被冒充为可玩场景。

新增 `PortraitPuzzleGraybox` 做组合、UI与生命周期，`PlanarShipLaneView`适配XY显示。船体与方向箭头用Unity UI几何灰盒绘制，未生成／导入美术。相机、逻辑格中心与点击射线使用同一坐标约定，显示尺寸不回写逻辑数据。

`CandidateLevelCatalog`加载完整十关，拒绝缺关、重复ID、未知版本、状态伪装、失效证明、清单hash不一致及几何重复。每次加载重新解析数据，外部修改返回的数据不污染下一次重开。编辑器菜单 `Validate Ten Candidate Catalog` 提供同一校验入口。完整Level Studio编辑／录制新证明工作流仍可后续补齐，本轮不声称已做完生产编辑器。

当前状态求解上限为4000状态、400000占格单元、150ms；同步求解只用于开发期Hint／Auto。初始proof只负责内容入口验收，受阻移动后重新求解 `session.Board`，不重放过期初始序列。搜索预算未知和真实死局明确区分。玩家任意操作后仍可解并未得到全分支证明。

## 竖屏与生命周期

`PortraitBoardLayout`实现重设计的clamp公式，明确输出Safe／Top／Middle／Grid／Lane／Tools各Rect。Unity以屏幕宽390逻辑单位归一，再扣系统安全区；逻辑单位不等于dp或毫米。纯公式测试仍覆盖360×640、390×844、430×932三个逻辑安全区，不能将其中的单格尺寸当作所有设备物理尺寸。

屏幕或安全区变化仅重排HUD、改变棋盘相机投影；船的世界格坐标和正在播放的目标不变，因此在移动中改变显示区域后仍能停到正确逻辑格。线性航道转角防止曲线路径穿过棋盘。完整离场阶段的船体受中部相机视口裁剪，正式视觉的出海／缩小过渡仍须试玩检查。

重开／换关先取消选择、释放控制器与订阅，禁用并销毁旧视图，再建立新Session。旧协程停止，旧操作回调不能写入新关卡；不通过直接更改Board快照跳过动画。

## 验证结果

- EditMode：229/229（原217＋12）。覆盖三类安全区边界、无效区域、清单排序／隔离和六种拒绝条件、线性航道不切过棋盘。
- PlayMode：10/10（原6＋4）。十关共727艘经过真实 `ShipMovementView` 协程完成回调，最后棋盘和活动视图均为空，出棋盘及入舰事件每船各一次且顺序一致。批量用较短的可注入动画时间保持测试效率；暂停／部分移动测试使用默认时间。
- 三类视口对第2关全部160个占格验证屏幕投影、射线反算回原Grid以及GraphicRaycaster命中棋盘输入面；覆盖多触点归属。像素投影允许1px渲染量化误差，反算Grid必须完全一致。
- 部分移动回归通过真实射线点击：移动途中暂停不提交占格，改变视口后恢复，停到阻挡前的新位置；再次点击紧邻阻挡不回退；移动中重开后旧回调不产生新关事件。

首次EditMode写出229项通过XML后出现Unity／Mono原生异常且进程未正常退出；清理该测试实例后以图形模式复核，最终结果以归档XML为准。首次视口用例把932高模拟区域直接放入640×480批处理窗口，被Unity裁剪；已改成等比缩放到测试窗口，并区分像素量化与严格Grid反算。没有删除失败断言以掩盖映射问题。

原始证据：[EditMode.xml](EditMode.xml)、[PlayMode.xml](PlayMode.xml)。本次自动化不是5名玩家触控测试，也不是Android性能／内存测试。

补充渲染回归：实际截图发现自绘方向箭头缺少CanvasRenderer，修复后新增80艘船方向网格实际生成检查；最终10/10 PlayMode通过。

## 画面审查与后续

`Tools > Tidebound > Capture Portrait Graybox Review`在实际Game视图中截图到系统临时目录；仅用于Unity 2022.3编辑期审查，不进入移动端。调用GameView尺寸接口的反射核对了[Unity 2022.3公开参考源码](https://github.com/Unity-Technologies/UnityCsReference/blob/2022.3/Editor/Mono/GameView/GameView.cs)，没有复制第三方实现。捕获不改变候选数据或正式场景，恢复临时后台运行设置。

实际Game截图（非示意重画、非手机实测）：[360×640](Level2_360x640.png)、[390×844](Level2_390x844.png)、[430×932](Level2_430x932.png)、[教学7船](Level1_390x844.png)。已逐张检查顶部／棋盘／航道／底部分区及船头方向箭头；正式图形样式与最小设备触控仍待G1。

下一步在这份可玩灰盒上开展G1：5名首次玩家各30次指定点击，命中≥95%、方向识别≥90%；第2关至少4/5在3秒内发现可出目标；最低目标Android设备持续操作≥30FPS并记录峰值内存。设备型号／设备可用性和真人测试尚待落实。测试失败先调整显示、触控或关卡结构；通过后再冻结规格、接海怪战斗。
