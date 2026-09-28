# Tidebound 行为统计接口

Unity负责原生游戏和Android AAB；本目录是独立Node服务，不构建Unity或WebGL。Vercel只托管HTTPS接口，Neon保存统计，GitHub保存代码并驱动接口部署。后端可迁移到其他支持Node的托管环境，客户端只依赖HTTPS协议。

## 运行与部署

生产API已部署到 https://tidebound-fleet.vercel.app ，健康检查为`/api/health`。Git生产分支已设为`codex/phase5r-reuse-roadmap`，推送自动发布已验证。管理员可在本目录运行`npm run summary -- --from=2026-09-01 --to=2026-09-29`查询聚合结果；省略日期默认近30天，凭据从本地私密配置读取，不出现在命令行。

- Node 22；`npm ci --ignore-scripts`，`npm test`。
- Vercel项目根目录设为 `10_开发工作区/TideboundFleet_Unity/Tools/AnalyticsBackend`；Framework选Other；安装命令与测试命令由 `vercel.json` 提供。
- 接入原仓库 `brucesunxi/Tidebound-Fleet`。本轮代码在 `codex/phase5r-reuse-roadmap`；只有将它设为Production Branch，推送它才发布到正式域名。以后切回main需先正常合并，再更新Production Branch。
- 使用原生Vercel Git集成，不需要Unity许可证或GitHub内的Unity账户。新提交自动跑后端测试并部署；数据库迁移不自动运行。
- Vercel的Production环境必须配置下表变量；预览环境不复用生产数据库。缺少配置时数据接口返回503，禁止未认证查询。

|变量|用途|
|---|---|
|`DATABASE_URL`|只允许访问`tidebound_analytics` schema的服务端数据库连接|
|`ANALYTICS_ADMIN_TOKEN`|管理员统计查询，32字节随机值以上；不进入Unity|
|`ANALYTICS_RATE_SALT`|注册限流的每日网络标识散列，不保存原始IP|
|`CRON_SECRET`|定时清理认证，Vercel自动作为Bearer头发送|

本地使用仓库根目录已忽略的 `90_本地私密配置_不提交/.env`。`tools/database.js check`只读检查，`tools/database.js migrate`在独立schema创建表。`tools/setup-runtime.js`创建受限登录角色与本地密钥，属于明确的生产权限变更，须获得操作者授权后执行；不会重置已有未知角色密码。部署时把本地`TIDEBOUND_ANALYTICS_DATABASE_URL`映射为Vercel的`DATABASE_URL`，不把迁移账号用于运行接口。

TLS证书验证必须开启。本机若Node不信任已配置的系统CA，可使用本机可信CA文件的`NODE_EXTRA_CA_CERTS`设置；不要提交机器路径到部署配置。

## 接口

|方法与路径|授权与行为|
|---|---|
|`GET /api/health`|公开的服务存活标志，不包含数据库或账户信息|
|`POST /api/register`|明确同意且年龄符合条件后注册随机安装ID及随机设备凭据；按网络散列限流|
|`POST /api/events`|安装凭据Bearer认证；每批1–32条，最多48KiB，整批严格验证，事务写入并去重|
|`GET /api/summary`|管理员Bearer认证；支持`from=YYYY-MM-DD&to=YYYY-MM-DD&test=false`，左闭右开、UTC、最多90天|
|`DELETE /api/installation`|安装凭据认证；删除本安装的统计记录，不改游戏存档或钱包|
|`GET /api/cleanup`|CRON_SECRET认证；每天03:20 UTC清理过期明细、非活跃安装及限流记录|

注册JSON包含`installationId`、64位十六进制`secret`、`consent:true`、`ageEligible:true`、`isTest`。安装认证头格式是 `Bearer <installationId>.<secret>`。客户端随机凭据只授权该安装的事件与删除，不是数据库密码或管理员密钥。

上报JSON包含`sessionId`、`appVersion`、`platform`（Android/iOS/Editor）和`events`。事件只接受固定字段`eventId`、`name`、`occurredAt`、`level`、`attemptId`、`tool`、`durationMs`；不存在任意属性包。支持：

- `session_start`：进程启动／从后台超过30分钟后返回；只有选择加入后才发送。
- `level_start`、`level_resume`、`level_complete`、`level_restart`、`level_deadlock`：已可靠保存的正式游戏；完成事件只在胜利已结算后记录；诊断Unknown不记死局。
- `tool_use`：只记录实际成功消耗的Rescue／Shuffle／Reverse。

事件ID用于网络重试去重；同安装、同attempt的开始／完成／重开另有唯一约束。窗口外超过7天的客户端队列过期；不接收任意历史补录或未来5分钟以外的时间。管理员密钥、原始购买令牌、姓名／邮箱不属于事件字段。

## Unity接入与口径

模块位于`Assets/Tidebound/Runtime/Unity/Analytics`。`Resources/TideboundAnalytics.json`只含公共HTTPS地址及开关。入口在“设置→隐私政策→统计选择”。默认不注册、不创建安装标识、不发事件；只有明确确认18岁以上并另行同意才启用。年龄未知和未满18岁保守关闭，不收集出生日期。广告同意与本模块无关。

队列最多256条，按32条分批，10秒超时、失败退避至5分钟；统计文件独立于玩家存档，失败不进入核心事务。撤回立即清空队列并停止采集，联网后请求删除；删除完成前不重新启用。测试／Editor／Development Build独立标记，普通查询默认排除。测试安装转正式发行应先完成关闭与删除，再重新选择加入。

统计按随机安装而非实名用户计算；重新安装或重新选择加入后的新ID不做跨设备关联。D1／D7／D30以首次收到事件的UTC日期为cohort，只有回访日期完整结束后才计入分母。关卡开始数与完成数可能跨查询日期边界，因此接口不把两者直接除成“通关率”。时长为客户端累积的局内活动时长，暂停与后台不计，崩溃可能丢失未保存片段。

这是客户端自报分析，不是防作弊、验单或奖励依据。未加入统计的玩家不在留存分母中。统计不是云存档；后端不会改变金币、道具或关卡结果。

每日清理从89天开始，给90天明细上限留出调度余量；仍需监控任务成功。生产平台访问日志期限、数据库备份轮换、移动端流量、遗留SDK和AAB签名／真机验收需独立完成，不由代码测试代替。

## 验证

`npm test`覆盖字段白名单、未成年／未知与拒绝、认证、批次边界、敏感异常清理、删除、时间窗口与测试流隔离。`tools/integration.js`仅创建随机测试安装，验证真实Neon写入、重试去重和查询后删除测试数据。

[本次实施与验证记录](../../../../40_项目交接文档/验证记录/20260928_统计接口与部署/VALIDATION.md)。技术依据：[Unity AAB构建](https://docs.unity3d.com/2022.3/Documentation/Manual/android-BuildProcess.html)、[Vercel Git部署](https://vercel.com/docs/git)、[Neon官方驱动](https://github.com/neondatabase/serverless)。
