using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace SupersonicWisdomSDK
{
    [Serializable]
    public class SwJsonKeyValueStore : ISwKeyValueStore
    {
        #region --- Constants ---
        
        private const string FILE_PATH_NAME = "SwPrefs.json";

        #endregion
        
        
        #region --- Members ---

        private static JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto, Culture = CultureInfo.InvariantCulture,
        };

        private static string _dataFilePath = Path.Combine(Application.persistentDataPath, FILE_PATH_NAME);

        private Dictionary<string, object> _keyValueStore = new Dictionary<string, object>();

        #endregion


        #region --- Construction ---

        public SwJsonKeyValueStore()
        {
            Load();
        }

        #endregion


        #region --- Public Methods ---

        public void Save()
        {
            try
            {
                
                var json = JsonConvert.SerializeObject(_keyValueStore, _settings);
                
                var encrypted = SwEncryptor.EncryptAesBase64(json, SwConstants.PREFS_ENCRYPTION_KEY, SwConstants.PREFS_ENCRYPTION_IV);
                
                File.WriteAllText(_dataFilePath, encrypted);
            }
            catch (Exception ex)
            {
                SwInfra.Logger.LogError(EWisdomLogType.Cache, $"Error saving data: {ex.Message}");
            }
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_dataFilePath))
                {
                    var encrypted = File.ReadAllText(_dataFilePath);
                    var json = SwEncryptor.DecryptAesBase64(encrypted, SwConstants.PREFS_ENCRYPTION_KEY, SwConstants.PREFS_ENCRYPTION_IV);
                    
                    _keyValueStore = JsonConvert.DeserializeObject<Dictionary<string, object>>(json, _settings);
                }
            }
            catch (Exception ex)
            {
                SwInfra.Logger.LogError(EWisdomLogType.Cache, $"Error loading data: {ex.Message}");
            }
        }

        public void DeleteAll()
        {
            _keyValueStore.Clear();
            Save();
        }

        public ISwKeyValueStore DeleteKey(string key, bool isInternal = true)
        {
            if (!_keyValueStore.ContainsKey(key)) return this;

            _keyValueStore.Remove(key);
            Save();

            return this;
        }

        public bool GetBoolean(string key, bool defaultValue = false, bool isInternal = true)
        {           
            bool result;

            try
            {
                result = _keyValueStore.TryGetValue(key, out var value) ? Convert.ToBoolean(value) : defaultValue;
                SwInfra.Logger.Log(EWisdomLogType.Cache, $"{key} = {result.SwToString()}");
            }
            catch (Exception e)
            {
                LogGetWarning(key, e);
                result = defaultValue;
            }

            return result;
        }

        public float GetFloat(string key, float defaultValue = 0f, bool isInternal = true)
        {
            float result;

            try
            {
                result = _keyValueStore.TryGetValue(key, out var value) ? Convert.ToSingle(value) : defaultValue;
                SwInfra.Logger.Log(EWisdomLogType.Cache, $"{key} = {result.SwToString()}");
            }
            catch (Exception e)
            {
                LogGetWarning(key, e);
                result = defaultValue;
            }

            return result;
        }

        public int GetInt(string key, int defaultValue = 0, bool isInternal = true)
        {
            int result;

            try
            {
                result = _keyValueStore.TryGetValue(key, out var value) ? Convert.ToInt32(value) : defaultValue;
                SwInfra.Logger.Log(EWisdomLogType.Cache, $"{key} = {result.SwToString()}");
            }
            catch (Exception e)
            {
                LogGetWarning(key, e);
                result = defaultValue;
            }

            return result;
        }

        public string GetString(string key, string defaultValue = "", bool isInternal = true)
        {
            string result;

            try
            {
                result = _keyValueStore.TryGetValue(key, out var value) ? Convert.ToString(value) : defaultValue;
                SwInfra.Logger.Log(EWisdomLogType.Cache, $"{key} = {result}");
            }
            catch (Exception e)
            {
                LogGetWarning(key, e);
                result = defaultValue;
            }

            return result;
        }

        public T GetGenericSerializedData<T>(string key, T defaultValue, bool isInternal = true)
        {
            T result;
            
            try
            {
                result = _keyValueStore.TryGetValue(key, out var value) ? JsonConvert.DeserializeObject<T>(Convert.ToString(value), _settings) : defaultValue;
                SwInfra.Logger.Log(EWisdomLogType.Cache, $"{key}");
            }
            catch (Exception e)
            {
                LogGetWarning(key, e);
                result = defaultValue;
            }

            return result;
        }

        public bool HasKey(string key, bool isInternal = true)
        {
            return _keyValueStore.ContainsKey(key);
        }

        public ISwKeyValueStore SetBoolean(string key, bool value, bool isInternal = true, bool save = false)
        {
            _keyValueStore[key] = value;

            if (save)
            {
                Save();
            }

            return this;
        }

        public ISwKeyValueStore SetFloat(string key, float value, bool isInternal = true, bool save = false)
        {
            _keyValueStore[key] = value;

            if (save)
            {
                Save();
            }

            return this;
        }

        public ISwKeyValueStore SetInt(string key, int value, bool isInternal = true, bool save = false)
        {
            _keyValueStore[key] = value;

            if (save)
            {
                Save();
            }

            return this;
        }

        public ISwKeyValueStore SetString(string key, string value, bool isInternal = true, bool save = false)
        {
            _keyValueStore[key] = value;

            if (save)
            {
                Save();
            }

            return this;
        }

        public ISwKeyValueStore SetGenericSerializedData(string key, object value, bool isInternal = true, bool save = false)
        {
            try
            {
                var jsonValue = JsonConvert.SerializeObject(value, _settings);
                _keyValueStore[key] = jsonValue;
            }
            catch (Exception e)
            {
                SwInfra.Logger.LogWarning(EWisdomLogType.Cache, $"Could not set {key} - no value saved\n{e}");
            }

            if (save)
            {
                Save();
            }

            return this;
        }

        private static void LogGetWarning(string key, Exception e)
        {
            SwInfra.Logger.LogWarning(EWisdomLogType.Cache, $"Could not load {key} - using default\n{e}");
        }

        #endregion
    }
}