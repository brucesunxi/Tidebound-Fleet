using JetBrains.Annotations;
using Newtonsoft.Json;

namespace SupersonicWisdomSDK
{
    internal class SwJsonHandler
    {
        #region --- Public Methods ---
        
        public string SerializeObject([CanBeNull] object obj)
        {
            try
            {
                if (obj != null)
                {
                    return JsonConvert.SerializeObject(obj);
                }
                
                LogWarning<object>(ErrorType.Serializing);
                return default;
            }
            catch (JsonException ex)
            {
                LogException<object>(ErrorType.Serializing, ex);
                return null;
            }
        }
        
        public string SerializeObject([CanBeNull] object obj, JsonSerializerSettings settings)
        {
            try
            {
                if (obj != null)
                {
                    return JsonConvert.SerializeObject(obj, settings);
                }
                
                LogWarning<object>(ErrorType.Serializing);
                return default;
            }
            catch (JsonException ex)
            {
                LogException<object>(ErrorType.Serializing, ex);
                return null;
            }
        }
        
        public string SerializeObject([CanBeNull] object obj, Formatting formatting)
        {
            try
            {
                if (obj != null)
                {
                    return JsonConvert.SerializeObject(obj, formatting);
                }
                
                LogWarning<object>(ErrorType.Serializing);
                return default;
            }
            catch (JsonException ex)
            {
                LogException<object>(ErrorType.Serializing, ex);
                return null;
            }
        }
        
        public T DeserializeObject<T>(string json)
        {
            try
            {
                if (!json.SwIsNullOrEmpty())
                {
                    return JsonConvert.DeserializeObject<T>(json);
                }
                
                LogWarning<T>(ErrorType.Deserializing);
                return default;
            }
            catch (JsonException ex)
            {
                LogException<T>(ErrorType.Deserializing, ex);
                return default;
            }
        }
        
        public T DeserializeObject<T>(string json, JsonSerializerSettings settings)
        {
            try
            {
                if (!json.SwIsNullOrEmpty())
                {
                    return JsonConvert.DeserializeObject<T>(json, settings);
                }
                
                LogWarning<T>(ErrorType.Deserializing);
                return default;
            }
            catch (JsonException ex)
            {
                LogException<T>(ErrorType.Deserializing, ex);
                return default;
            }
        }
        
        public void PopulateObject(string json, [NotNull] object target)
        {
            try
            {
                if (!json.SwIsNullOrEmpty())
                {
                    JsonConvert.PopulateObject(json, target);
                }
                else
                {
                    LogWarning<object>(ErrorType.PopulateObject);
                }
            }
            catch (JsonException ex)
            {
                LogException<object>(ErrorType.PopulateObject, ex);
            }
        }
        
        #endregion
        
        
        #region --- Private Methods ---
        
        private static void LogWarning<T>(ErrorType type)
        {
            SwInfra.Logger.LogWarning(EWisdomLogType.Json,$"JSON {type} error: is null or empty.\n objectType: {typeof(T)}\n");
        }
        
        private static void LogException<T>(ErrorType type, JsonException ex)
        {
            SwInfra.Logger.LogException(ex, EWisdomLogType.Json,$"JSON {type} error: .\n objectType: {typeof(T)}\n message: {ex.Message}\n");
        }
        
        #endregion
        
        
        #region --- Enums ---
        
        private enum ErrorType { Serializing, Deserializing, PopulateObject }
        
        #endregion
    }
}