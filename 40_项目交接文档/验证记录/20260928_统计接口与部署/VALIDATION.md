# Git同步与Unity—Vercel—Neon统计桥接

日期：2026-09-28。

## 目标与已完成范围

用户原要求推送最新完成代码至 `brucesunxi/Tidebound-Fleet` 并部署Vercel，之后明确取消WebGL试玩，改为Unity原生游戏通过Vercel接收／查询Neon中的关卡、道具和留存统计。Unity直接构建Android AAB，Vercel不参与游戏打包。

1. 完整游戏快照已提交并推送至 `codex/phase5r-reuse-roadmap`，提交 `19466bc103521d9b90e24d3ae66768016c36e8f2`，包含最新正式资源与百关、交互修复。没有覆盖main或改写历史。原目录中尚未接入的设计预览／草稿继续保留本地。
2. 新后端位于 `10_开发工作区/TideboundFleet_Unity/Tools/AnalyticsBackend`，使用固定字段、安装凭据认证、管理员认证、事务写入和去重；统计接口不操作游戏权益。
3. 已在现有Neon连接对应数据库中新建独立`tidebound_analytics` schema，包含installations、events、rate_limits三表和索引；没有修改既有业务表。驱动、生产API和Unity三轮真实联调均仅使用随机测试安装，完成验证后已删除。
4. Unity增加独立统计文件、256条有界队列、32条批次、超时和退避；与玩家存档／钱包分离。未知年龄／未满18岁／未同意均不采集；明确成年确认与统计选择分两步，退出不会影响玩法。撤回立即清空本地队列并等待联网删除。
5. 设置中的隐私阅读页增加“统计选择”入口，更新中英文内置说明。没有接广告或支付SDK，没有导出或发布AAB，也没有进行Android真机流量验证。

## 验证证据

|检查|结果与范围|
|---|---|
|百关Python验证|100关、8745船、9130步、385次部分移动、191个死局修复证明、100份恢复检查通过；在首次Git推送前运行|
|后端单元测试|10/10通过：拒绝／未知年龄、白名单、认证、去重确认、异常脱敏、删除、时间范围及测试流|
|真实Neon联调|注册、认证、2条写入成功；重试新增0条；按工具汇总可见；最终删除测试安装与关联事件|
|Unity EditMode|4/4：默认关闭、撤回清空、重试ID不变／确认不丢新事件、容量／会话／过期边界|
|Unity PlayMode|6/6：新增2项同意界面与撤回验证，既有4项设置／暂停回归；测试禁用网络，隔离验证工程不使用正式玩家存档|
|Unity公网PlayMode|1/1：隔离工程实际HTTPS注册、单船关卡通关上报、批次确认、撤回删除；随后Neon回查安装和事件均为0|
|生产API与Neon|16/16公网检查通过；认证、事件写入、去重、查询、D1留存、测试流隔离、删除及过期清理|
|Unity编译|2022.3.25f1通过；保留既有CollectionShipPreview.camera隐藏成员警告|
|差异与保密检查|本轮新增文件和修改检查后提交；不提交数据库连接、密钥、node_modules、Unity缓存、构建产物|

EditMode及PlayMode XML在本目录。Assembly路径已标准化为`<isolated-project>`，计数和断言保持原始结果。新增的`unity-live-playmode.xml`来自仅在隔离工程中的一次性联网用例，普通套件不依赖生产网络；回查结果与该测试源码哈希见`unity-live-neon-verification.json`。未重新运行全量Unity套件；旧百关／交互测试仍以各自原记录为准。

首次大批游戏快照存在既有Unity生成meta／prefab等尾随空白，未批量改写用户资源以消除此历史格式。新统计变更单独执行diff检查。

## 生产部署与权限验证

用户已明确授权创建受限账号和配置生产密钥，之前的自动审批确认事项已解除。

- Vercel项目`tidebound-fleet`已上线，固定服务地址为 https://tidebound-fleet.vercel.app；健康检查 `/api/health` 返回200，未认证统计查询返回401。
- 仅新建`tidebound_analytics_runtime`运行角色。实查没有superuser／createdb／createrole／bypassrls权限，能访问3张统计表，其他业务表可访问数为0；结果见`runtime-permissions.json`。
- `DATABASE_URL`、`ANALYTICS_ADMIN_TOKEN`、`ANALYTICS_RATE_SALT`、`CRON_SECRET`仅配置于Vercel Production，类型为encrypted；本地保存在已忽略的私密目录。预览环境未配置生产密钥；临时传输文件已清理。
- 部署根目录为`10_开发工作区/TideboundFleet_Unity/Tools/AnalyticsBackend`，Node 22，构建运行`npm test`，输出`public`只含robots规则，API由Functions处理。
- 首次部署因缺少`public`目录失败，修复后推送提交`60469e433068535d59080683023a9a2c39d0a30a`，Vercel原生Git集成自动发布为READY，source明确为git。证据见`deployment-proof.json`。
- 生产分支为`codex/phase5r-reuse-roadmap`，临时忽略构建设置已解除。以后推送此分支可自动更新API；main未被覆盖，Unity打包不在此流程中，不需要Unity CI授权。
- 公网16项检查通过：健康、查询认证、拒绝采集、注册、事件写入、重试去重、字段拒绝、管理员汇总、成熟D1留存、测试流隔离、客户端删除及Neon回查、已删除凭据拒绝、清理认证、过期测试数据清理。见`production-checks.json`。全部使用随机测试安装，验证后已清除。
- 随后Unity 2022.3.25f1在隔离工程中完成实际HTTPS注册、关卡通关事件上报和撤回删除，1/1通过；Neon回查安装与事件残留数均为0。
- Vercel计划清理任务已启用，定义为`20 3 * * *`（UTC每日03:20），访问`/api/cleanup`；首次计划执行结果尚未观察。

统计是客户端自报数据，不作验单、防作弊或发奖依据。留存分母仅包括同意采集的随机安装ID，重新安装不做跨设备关联。每天从89天开始清理明细，为90天上限留出调度余量；已手动验证生产清理接口，计划调度首轮是否按时完成及平台日志期限／备份轮换仍需持续运维核对。

## 已取消的WebGL工作

修订前已在隔离副本完成WebGL构建与本地浏览器试玩，随后按用户要求停止。没有创建WebGL生产部署；正式WebGL工具和GitHub Unity构建工作流已移入忽略的临时目录，不进入本次Git提交。构建产物保留在忽略的交付目录。本地安装的Unity WebGL支持模块保留，不影响Android工程配置。
