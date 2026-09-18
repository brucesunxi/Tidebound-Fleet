using System;
using System.Globalization;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace SupersonicWisdomSDK
{
    public class SwPlayerPrefsStore : SwUnmanagedPlayerPrefsStore
    {
        #region  --- Constants ---
        
        private const string EXTERNAL_PREFIX = "sw.e.";
        private const string INTERNAL_PREFIX = "sw.i.";

        #endregion
        
        
        #region --- Public Methods ---

        public override void DeleteAll()
        {
            SwInfra.Logger.Log(EWisdomLogType.Cache, "DeleteAll");
            base.DeleteAll();
        }

        public override ISwKeyValueStore DeleteKey(string key, bool isInternal = true)
        {
            if (key.SwIsNullOrEmpty()) return this;
            
            SwInfra.Logger.Log(EWisdomLogType.Cache, $"Key: {key}");

            base.DeleteKey(GetKeyWithPrefix(key, isInternal));

            return this;
        }

        public override bool GetBoolean(string key, bool defaultValue = false, bool isInternal = true)
        {
            if (key.SwIsNullOrEmpty()) return defaultValue;
            
            var value = GetInt(key, defaultValue.SwToInt(), isInternal);
            
            return value != 0;
        }

        public override float GetFloat(string key, float defaultValue = 0f, bool isInternal = true)
        {
            if (key.SwIsNullOrEmpty()) return defaultValue;

            var stringValue = GetString(key, defaultValue.ToString("R", CultureInfo.InvariantCulture), isInternal);

            if (!float.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return defaultValue;
            }
            
            return value;
        }

        public override int GetInt(string key, int defaultValue = 0, bool isInternal = true)
        {
            if (key.SwIsNullOrEmpty()) return defaultValue;

            var stringValue = GetString(key, defaultValue.ToString(CultureInfo.InvariantCulture), isInternal);

            if (!int.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                return defaultValue;
            }
            
            return value;
        }

        public override string GetString(string key, string defaultValue = "", bool isInternal = true)
        {
            if (key.SwIsNullOrEmpty()) return defaultValue;
            
            var keyWithPrefix = GetKeyWithPrefix(key, isInternal);

            if (!GetStringSpecific(keyWithPrefix, defaultValue, out var value))
            {
                if (GetStringSpecific(key, defaultValue, out value))
                {
                    value = ConvertKey(key, value, isInternal);
                }
                else
                {
                    return defaultValue;
                }
            }
            
            var decryptedValue = SwEncryptor.DecryptAesBase64(value, SwConstants.PREFS_ENCRYPTION_KEY, SwConstants.PREFS_ENCRYPTION_IV);
            
            SwInfra.Logger.Log(EWisdomLogType.Cache, $"{key} | {decryptedValue}");
            
            return decryptedValue;
        }

        public override T GetGenericSerializedData<T>(string key, T defaultValue, bool isInternal = true)
        {
            var dateInString = GetString(key, string.Empty, isInternal);
            T returnValue;
            
            if (!string.IsNullOrEmpty(dateInString))
            {
                returnValue = SwUtils.JsonHandler.DeserializeObject<T>(dateInString, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    Culture = CultureInfo.InvariantCulture,
                });
            }
            else
            {
                returnValue = defaultValue; 
            }

            return returnValue;
        }

        public override bool HasKey(string key, bool isInternal = true)
        {
            return base.HasKey(HasPrefix(key, isInternal) ? key : GetKeyWithPrefix(key, isInternal));
        }

        public override ISwKeyValueStore SetBoolean(string key, bool value, bool isInternal = true, bool save = false)
        {
            if (key.SwIsNullOrEmpty()) return this;

            var intValue = value.SwToInt();

            return SetString(key, intValue.ToString(CultureInfo.InvariantCulture), isInternal, save);
        }

        public override ISwKeyValueStore SetFloat(string key, float value, bool isInternal = true, bool save = false)
        {
            if (key.SwIsNullOrEmpty()) return this;
            
            return SetString(key, value.ToString("R", CultureInfo.InvariantCulture), isInternal, save);
        }

        public override ISwKeyValueStore SetInt(string key, int value, bool isInternal = true, bool save = false)
        {
            if (key.SwIsNullOrEmpty()) return this;
            
            return SetString(key, value.ToString(CultureInfo.InvariantCulture), isInternal, save);
        }

        public override ISwKeyValueStore SetString(string key, string value, bool isInternal = true, bool save = false)
        {
            if (key.SwIsNullOrEmpty()) return this;
            
            key = GetKeyWithPrefix(key, isInternal);
            
            SwInfra.Logger.Log(EWisdomLogType.Cache, $"{key} | {value ?? "Null"}");
            value = SwEncryptor.EncryptAesBase64(value, SwConstants.PREFS_ENCRYPTION_KEY, SwConstants.PREFS_ENCRYPTION_IV);
            base.SetString(key, value, isInternal, save);

            return this;
        }

        public override ISwKeyValueStore SetGenericSerializedData(string key, object value, bool isInternal = true, bool save = false)
        {
            var jsonString = SwUtils.JsonHandler.SerializeObject(value, new JsonSerializerSettings
            {
                Culture = CultureInfo.InvariantCulture,
                TypeNameHandling = TypeNameHandling.Auto,
            });
            
            return SetString(key, jsonString, isInternal, save);
        }

        #endregion
        
        
        #region --- Private Methods ---
        
        private bool GetStringSpecific(string key, string defaultValue, [NotNull] out string value)
        {
            if (base.HasKey(key))
            {
                value = base.GetString(key);
                
                return true;
            }

            value = defaultValue;
            
            return false;
        }

        private string ConvertKey(string key, string value, bool isInternal)
        {
            SetString(key, value, isInternal);
            base.DeleteKey(key);

            return base.GetString(GetKeyWithPrefix(key, isInternal));
        }
        
        private static bool HasPrefix(string text, bool isInternal)
        {
            return text.StartsWith(isInternal ? INTERNAL_PREFIX : EXTERNAL_PREFIX);
        }

        private static string GetKeyWithPrefix(string key, bool isInternal)
        {
            var prefix = isInternal ? INTERNAL_PREFIX : EXTERNAL_PREFIX;
            return new StringBuilder(prefix).Append(key).ToString();
        }
        
        #endregion
    }
}