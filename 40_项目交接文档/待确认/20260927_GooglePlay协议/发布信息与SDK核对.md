# Google Play 协议定稿：待确认信息

**2026-09-28补充：**用户已确认首发包含Google AdMob激励广告（仅此形式）、Neon数据库＋自建行为分析后台和Google Play真钱内购，计划面向10+；公开支持／隐私邮箱为tideboundfleet@hotmail.com，发行范围为Google Play全部可选国家／地区。这些是首发要求，不是当前SDK已接入的验收结论。公司参考、年龄建议、中英文新版草稿及网页包见[首发隐私与上架准备](../20260928_首发隐私与上架准备/README.md)。下方“首发是否启用”及旧异步问题状态是9月27日历史记录；实际字段、期限、SDK配置及儿童流程仍待落实。

2026-09-27。用户已授权将协议改成发行版本；正文准备不等于已具备发行事实。本轮没有发布网站、接入SDK或改写第三方配置。

## 当前可确认的事实

- 唯一启用的 Build Scene 是 `Assets/Tidebound/Scenes/Phase5R_PortraitGraybox.unity`；购买源码的 Loader 及百关场景处于禁用状态。
- `Assets/Tidebound/Runtime/` 未发现账号注册、开发者网络请求、第三方SDK初始化、广告展示或真钱购买调用。核心进度、库存、收藏和设置使用本地存储。暂停中的继续恢复当前挑战；首页“开始游戏”重建待挑战关卡。
- **上述结论不能外推为 Android 发布包不采集数据。** 购买源码的SDK仍位于全工程导入／原生合并范围：
  - `Assets/Plugins/Android/AndroidManifest.xml` 注册 Facebook activity/provider，并启用自动事件记录、广告标识收集标记。无需在 Tidebound 脚本搜索到 `FB.Init` 才需考虑原生入口。
  - `Assets/FacebookSDK/SDK/Resources/FacebookSettings.asset` 的自动事件及广告标识开关开启。
  - `Assets/Plugins/Android/SupersonicWisdom.androidlib/src/main/AndroidManifest.xml` 包含 Wisdom 初始化 provider、AppsFlyer 广播 receiver，声明网络、广告标识和电话状态相关权限。权限声明本身不证明实际读取；发布合并清单及实际初始化仍须核对。
  - `Assets/LevelPlay/Runtime/IronSourceInitilizer.cs` 在 iOS/Android 存在 RuntimeInitializeOnLoadMethod；是否初始化取决于资源配置。
  - 工程还保留 GameAnalytics。存在资源不等于本次核心场景运行时已调用。
- `ProjectSettings/ProjectSettings.asset` 仍使用购买源码的公司名及产品名，不能用作当前游戏的法律运营主体。本记录不复制任何SDK账号或密钥。

## 必须补充后才能定稿的发行事实

1. Google Play 商店开发者／运营主体名称与公开支持邮箱。
2. 首发是否启用广告、统计、归因、真钱购买；保留哪些SDK及实际开关。若选择纯离线发行，需清理／隔离遗留原生入口并验证最终安装包，不能只改文案。
3. 目标受众及发行地区；如面向儿童，需要确定相应SDK适用性和选择流程，不能擅自写“13+”或虚构年龄限制。
4. 用于公开托管政策的可访问网址。当前本地文字阅读入口可保留，不能将 localhost 链接提交 Google Play。

已经通过当前任务的异步问题向用户询问第1、2项，尚待回复。准备文本见同目录《用户协议_待发布信息确认.md》《隐私政策_待数据实践确认.md》；未把占位内容覆盖到游戏中。正式游戏现有用户协议仅同步修正了开始游戏／退出后重开的说明。

## 定稿依据

Google Play [User Data / Privacy Policy](https://support.google.com/googleplay/android-developer/answer/10144311?hl=en) 要求政策与真实数据使用及SDK一致，提供开发者／隐私联系机制、保留和删除说明，并有公开可访问、非PDF的政策网址。`Data safety` 声明也必须一致。该要求是本次无法把未知信息写成最终事实的原因；不是另行要求用户审批已经授权的UI修改。

后续只需在确认发行信息后完成正文、公开HTML文件与游戏内中英文本的同步；实际托管／发布仍需指定目的地，不冒称已经上架。
