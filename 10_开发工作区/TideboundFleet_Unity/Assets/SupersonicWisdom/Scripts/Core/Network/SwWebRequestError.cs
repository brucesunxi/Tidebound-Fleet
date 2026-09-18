using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace SupersonicWisdomSDK
{
    [JsonConverter(typeof(SwJsonWebRequestErrorConverter))]
    public enum SwWebRequestError
    {
        [EnumMember(Value = "None")] None,
        [EnumMember(Value = "Http")] Http,
        [EnumMember(Value = "Network")] Network,
        [EnumMember(Value = "Timeout")] Timeout,
        [EnumMember(Value = "InvalidUrl")] InvalidUrl,
        [EnumMember(Value = "Unknown")] Unknown,
    }
}