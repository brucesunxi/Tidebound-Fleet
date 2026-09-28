# 修改前后回归对照（2026-09-26）

同一 Unity 2022.3.25f1 工程，用本轮开始前的3个文件精确备份运行旧38项；完整保存并恢复本轮实现。测试存档均为内存对象。

- 修改前：38项，23通过、15失败。
- 修改后：42项，27通过、15失败；新增4项全部通过。
- 旧38项结果逐项完全一致，新增失败0项。
- 下列失败属于本轮开始前工作区已有状态，未跨范围改收藏、首页定稿或船体断言。并不表示全项目回归通过。

|旧测试|两次结果|失败摘要|
|---|---|---|
|CollectionConfirmationBlocksCategoryNavigationAndChargesExactlyOnce|一致|System.NullReferenceException : Object reference not set to an instance of an object|
|CommonControlsAndScrollableModalPreserveSelectionAndAccount|一致|System.NullReferenceException : Object reference not set to an instance of an object|
|HomeReflectionAndVoyagePulseRespectReducedMotionAndReleaseMaterial|一致|System.NullReferenceException : Object reference not set to an instance of an object|
|HomeUsesIconFirstEntriesAndShipIdentityInBothLanguages|一致|System.NullReferenceException : Object reference not set to an instance of an object|
|LanguageAndCategoriesPreserveAttemptWalletInventoryAndEquipment|一致|System.NullReferenceException : Object reference not set to an instance of an object|
|ModalRaycastsDoNotReachHomeStartAndSettingsCannotStartAnAttempt|一致|System.NullReferenceException : Object reference not set to an instance of an object|
|RaisedButtonsKeepHitTargetsAndNeverSpendOnPressOrCancel|一致|System.NullReferenceException : Object reference not set to an instance of an object|
|ReducedMotionAndModalDisableResetButtonVisuals|一致|System.NullReferenceException : Object reference not set to an instance of an object|
|ShowcaseSelectionUpdatesHomeAndSurvivesReloadWithoutChangingFleet|一致|Expected: Locked   But was:  Busy|
|SmallScreenHasScrollableContentAndMinimumButtonTargets|一致|InspectArtwork   Expected: greater than or equal to 48   But was:  28.0f|
|BrowsingNeverStartsAttemptAndHomeResumeKeepsIdentityAndPauseOwnership|一致|System.NullReferenceException : Object reference not set to an instance of an object|
|FirstBlueIsClaimedAtHomeBeforeCreatingLevelThreeWithoutAutoEquipment|一致|System.NullReferenceException : Object reference not set to an instance of an object|
|LevelTenIsAnOrdinaryResultWithUnavailableNextAndNoChapterStatistics|一致|Expected: String containing "not available"   But was:  "下一关暂未开放，奖励已保存。"|
|MissingSavedLevelStaysHomeAndPreservesStoredAttempt|一致|Expected: String containing "not available"   But was:  "本关暂未开放，请稍后再来。"|
|FlatReferenceRetainsEveryShipDirectionMesh|一致|Expected: 80   But was:  8|
