using System.ComponentModel;
using Newtonsoft.Json;

namespace SupersonicWisdomSDK
{
    internal class SwTacConfig
    {
        [JsonProperty("maximumUiActions")] [DefaultValue(2)] public int MaximumUiActions;
        [JsonProperty("actionsWaterfall")] public SwActionData[] Actions;
    }
}