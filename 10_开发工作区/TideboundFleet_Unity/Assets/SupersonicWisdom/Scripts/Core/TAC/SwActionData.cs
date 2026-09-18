using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SupersonicWisdomSDK
{
    [Serializable]
    internal class SwActionData
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("priority")] public int Priority { get; set; } // Higher is first - 0 is invalid
        [JsonProperty("action")] public ESwActionType ActionType { get; set; }
        [JsonProperty("searchType")] public ESearchType SearchType { get; set; }
        [JsonConverter(typeof(DictionaryConverter<object>))]
        [JsonProperty("params")] public Dictionary<string, object> Params { get; set; }
        [JsonProperty("triggers")] public string[] Triggers { get; set; }
        [JsonProperty("conditions")] public SwCoreTacConditions Conditions { get; set; }

        public override string ToString()
        {
            return SwUtils.JsonHandler.SerializeObject(this);
        }
    }
}