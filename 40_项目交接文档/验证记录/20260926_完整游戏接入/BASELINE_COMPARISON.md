# 旧断言对照

本轮首次相关 PlayMode 共53项：46通过、7失败。没有隐藏失败来宣称全项目通过。

其中以下5项，在上一轮修改前／修改后逐项对照中已经复现，见 `../20260925_设置暂停正式接入/BASELINE_COMPARISON.md`：

- HomeNavigation：BrowsingNeverStartsAttemptAndHomeResumeKeepsIdentityAndPauseOwnership、FirstBlueIsClaimedAtHomeBeforeCreatingLevelThreeWithoutAutoEquipment（旧收藏入口流程断言）。
- HomeNavigation：LevelTenIsAnOrdinaryResultWithUnavailableNextAndNoChapterStatistics、MissingSavedLevelStaysHomeAndPreservesStoredAttempt（旧英文字符串断言，当前界面中文）。
- PortraitGraybox：FlatReferenceRetainsEveryShipDirectionMesh（旧80个程序网格船断言，目前皮肤图接入后为8个长船网格）。

另外两项本轮用任务开始时3个显示层源文件的精确备份重跑，结果与新版相同。为使新增检查工具能编译，基线Initialize仅保留新增的可选参数声明，未使用该参数。测试不传此参数、不使用首页或美术模式。完成后已经恢复全部新版文件。

|测试|修改前|修改后|原因|
|---|---|---|---|
|TwoShipRescueAndSpentInventorySurviveSceneRecreation|期望527，实际467|期望527，实际467|旧断言累加递增首通奖励；当前经济版本每关首通100，差额60|
|CollectionGateRunsBeforeLevelThreeCreationAndCannotBeBypassed|期望307，实际287|期望307，实际287|第2关旧期望120首通；当前版本100，差额20|

`BattleCoinRules.cs`、关卡配置、保存与金币逻辑均未在本次修改。基线结果为 `BaselineReward.xml`。本轮针对正式首页到关卡的新流程截图检查以及最终24项有针对性的运行回归全部通过；百关回放另列，旧测试改写留作独立维护，不冒称这些7项已通过。
