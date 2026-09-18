using Newtonsoft.Json;

namespace SupersonicWisdomSDK
{
    internal class SwCoreTacConditions
    {
        [JsonProperty("configStatus")] public EConfigStatus[] ConfigStatus { get; set; }
        [JsonProperty("isDuringLevel")] public bool? IsDuringLevel { get; set; }
        [JsonProperty("consecutiveCompletedLevels")] public int? ConsecutiveCompletedLevels { get; set; }
        [JsonProperty("consecutiveFailedLevels")] public int? ConsecutiveFailedLevels { get; set; }
        [JsonProperty("currentState")] public SwSystemState.EGameState[] CurrentState { get; set; }
        [JsonProperty("previousState")] public SwSystemState.EGameState[] PreviousState { get; set; }
        [JsonProperty("minLevel")] public int? MinLevel { get; set; }
        [JsonProperty("minMinute")] public int? MinMinute { get; set; }
        [JsonProperty("minAge")] public int? MinAge { get; set; }
        [JsonProperty("maxLevel")] public int? MaxLevel { get; set; }
        [JsonProperty("maxMinute")] public int? MaxMinute { get; set; }
        [JsonProperty("exactLevels")] public int[] ExactLevels { get; set; }
        [JsonProperty("singleUseAboveSeconds")] public int[] SingleUseAboveSeconds { get; set; }
        [JsonProperty("allowAfter")] public ESwActionType[] AllowAfter { get; set; }
        [JsonProperty("minActiveDays")] public int? MinActiveDays { get; set; }
        [JsonProperty("minRevenue")] public float? MinRevenue { get; set; }
        [JsonProperty("freeZone")] public bool? AdFreeZone { get; set; }
        [JsonProperty("minLevelAttempts")] public int? MinLevelAttempts { get; set; }
        [JsonProperty("minLevelRevives")] public int? MinLevelRevives { get; set; }
        [JsonProperty("minCompletedLevels")] public int? MinCompletedLevels { get; set; }
        [JsonProperty("minCompletedBonusLevels")] public int? MinCompletedBonusLevels { get; set; }
        [JsonProperty("minCompletedTutorialLevels")] public int? MinCompletedTutorialLevels { get; set; }
        [JsonProperty("isNoAds")] public bool? IsNoAds { get; set; }
        
        public override string ToString()
        {
            return SwUtils.JsonHandler.SerializeObject(this);
        }
    }
}