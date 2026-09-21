# V2-C2 首批主页资源

2026-09-21。根据用户「继续生成」补齐本批资源，沿用[主页终版目标 v2](../20260921_主页终版目标图/Home_FinalTarget_EN_v2.png)。使用内置 imagegen；未使用 CLI/API 后备生成。每张图的完整提示词、尺寸、SHA-256 与透明通道检查见 [SOURCE.json](SOURCE.json)。

## 资源

| 文件 | 用途 |
|---|---|
| [Harbor_Background_v1.png](Harbor_Background_v1.png) | 明亮港湾背景；已移除全部 UI、大展示船及接触浪花 |
| [Icon_Collection_v1.png](Icon_Collection_v1.png) | 收藏：航海图册与小船 |
| [Icon_SkinDraw_v1.png](Icon_SkinDraw_v1.png) | 皮肤抽奖：紫色星标票券 |
| [Icon_Supplies_v1.png](Icon_Supplies_v1.png) | 道具补给：工具与航海图箱 |
| [Icon_DailyGift_v1.png](Icon_DailyGift_v1.png) | 每日奖励：打勾日历，功能仍未开放 |
| [Icon_Invite_v1.png](Icon_Invite_v1.png) | 邀请有礼：礼盒，功能仍未开放 |
| [Icon_Rankings_v1.png](Icon_Rankings_v1.png) | 排行榜：奖杯，功能仍未开放 |

## 检查与接入边界

- 7 张 PNG 可正常解码，归档副本与生成原件一致；6 张图标为真实 RGBA 透明背景，alpha 范围均为 0–255。按 alpha ≥ 128 检查，所有主体均处于画布以内。逐张目视检查主题、无文字、造型完整。
- 少量极低透明度边缘像素保留原始输出；没有用色键抠图。接入后仍需在浅色按钮及实际尺寸检查边缘、识别度和压缩效果。
- 背景为单张静态底图，尚未拆出远中近景；它不包含大船，不能代替实时 3D 展示船及独立水纹。
- 按钮底板、文字、数字、语言标签由 Unity 单独绘制。主页继续无游戏标题、无抽奖券数、无通关进度条、无直接换船按钮；换船在收藏操作。
- 此批是 C2 的资源准备，并非已完成的游戏界面。尚未导入正式 Unity、未实现新的语言服务或 3D 展示船，未运行 Unity 测试；不改存档、经济、解锁和平台配置。每日奖励／邀请／排行图标不代表对应服务已完成。
- C2 下一步继续公共 UI、独立展示船、四分类收藏壳及单语言设置接入，再做实际分辨率、安全区、遮罩输入与动效检查。真机仍延期。
