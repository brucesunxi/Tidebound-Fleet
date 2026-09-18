# Git 状态报告

检查日期：2026-09-18；检查阶段：Phase 0.8 项目治理。

## 结论

本地仓库已正确初始化在项目根目录，当前分支为 `main`，远端 `origin` 已关联到 `https://github.com/brucesunxi/Tidebound-Fleet.git`。只读远端检查成功且返回 0 个 refs，说明远端当前为空仓库。

尚未创建本地 commit，没有暂存文件，也没有执行 fetch、pull 或 push。治理完成后的全部正式文件目前显示为 untracked，这是首次基线提交前的正常状态。

## Git 状态

| 检查项 | 结果 |
|---|---|
| 工作树 | 有效 Git worktree |
| 当前分支 | `main` |
| 本地提交 | 0，`No commits yet on main` |
| origin fetch | `https://github.com/brucesunxi/Tidebound-Fleet.git` |
| origin push | `https://github.com/brucesunxi/Tidebound-Fleet.git` |
| 远端 refs | 0；当前为空仓库 |
| 可跟踪文件候选 | 6,723 |
| 10 MiB 及以上候选文件 | 0 |
| 100 MiB 以上候选文件 | 0 |
| Git LFS | 本机未检测到；当前没有大文件阻断，正式 PSD/FBX/音频批量入库前应另行规划 |

## 忽略规则验证

以下实测路径均被 `.gitignore` 命中：

- `00_远端接收区/` 下的购买源码
- `20_交付构建/` 下的 APK 示例路径
- `90_本地私密配置_不提交/.env` 及旧 Unity 第三方配置备份
- `99_垃圾存储区/` 下的临时日志示例路径
- Unity 的 Library、UserSettings 等生成目录
- 根目录 `.DS_Store`

以下正式内容实测可被 Git 跟踪：

- 根目录 `.env.example`
- `40_项目交接文档/PROJECT_STRUCTURE.md`
- `10_开发工作区/TideboundFleet_Unity/Assets/Tidebound/` 下的正式业务文件

## 敏感信息检查

对所有 6,723 个可跟踪候选文件执行了受限模式扫描：数据库 URI 0、Neon 密码令牌模式 0、私钥头 0。用户提供的 Neon 连接信息只存在于被忽略的本地 `.env`，没有进入候选文件。

购买工程工作副本中发现的旧 Supersonic Wisdom、GameAnalytics、Facebook 配置及 Android 签名路径已处理：

1. 原配置复制到 `90_本地私密配置_不提交/Unity_旧购买工程配置备份/`。
2. 正式 Unity 工作副本的对应字段清空，并关闭旧自定义 keystore 与旧 Unity Cloud 绑定。
3. 不修改购买工程原件，不复述或提交原值。
4. 清理后重新运行 Unity EditMode：65/65 通过，0 C# error，0 C# warning。

第三方 SDK 自身源码可能包含厂商公开分发所需的常量、字段名称和接口实现；本次未擅自改写供应商代码。发布前仍需决定哪些旧 SDK 被移除，以及为 Tidebound Fleet 重新申请哪些独立配置。

## 首次提交建议

当前仓库已具备创建基线提交的条件，但本阶段没有获得提交或推送指令，因此保持未提交状态。建议人工复核 GitHub 仓库可见性与资源许可后执行：

```text
chore(governance): 建立项目治理与Unity基础工程
```

首次提交前应再次查看完整待提交清单，并重点复核购买工程中允许进入私有/公开仓库的资源许可。若仓库是公开仓库，不应默认上传购买源码衍生的全部商业资源；需要先确定授权与仓库可见性。
