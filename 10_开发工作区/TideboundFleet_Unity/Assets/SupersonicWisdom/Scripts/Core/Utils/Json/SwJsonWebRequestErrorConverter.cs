using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace SupersonicWisdomSDK
{
    [Preserve]
    public class SwJsonWebRequestErrorConverter : JsonConverter
    {
        #region --- Public Methods ---
        
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SwWebRequestError);
        }
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var enumString = reader.Value?.ToString();
            
            if (string.IsNullOrEmpty(enumString))
            {
                return SwWebRequestError.None;
            }
            
            return Enum.TryParse(enumString, true, out SwWebRequestError parsedEnum) ? parsedEnum : SwWebRequestError.Unknown;
        }
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null) return;
            
            try
            {
                var enumValue = (SwWebRequestError)value;
                writer.WriteValue(enumValue.ToString());
            }
            catch (Exception e)
            {
                SwInfra.Logger.LogWarning(EWisdomLogType.Json, $"Can't write enum SwJsonWebError, {e.Message}");
            }
        }
        
        #endregion
    }
}