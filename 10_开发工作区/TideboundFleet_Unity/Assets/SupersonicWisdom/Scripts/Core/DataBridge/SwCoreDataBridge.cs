using System;
using System.Collections.Generic;
using System.Globalization;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace SupersonicWisdomSDK
{
    /// <summary>
    /// This class is used to bridge between Wisdom's internally collected data and the user.
    /// </summary>
    internal abstract class SwCoreDataBridge
    {
        #region --- Properties ---
        
        protected Dictionary<string, object> DataDictionary { get; } = new Dictionary<string, object>();
        internal Dictionary<string, object> CustomAndExternalDataDictionary { get; } = new Dictionary<string, object>();
        
        #endregion
        
        
        #region --- Fields ---

        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            Culture = CultureInfo.InvariantCulture,
        };
        
        #endregion

        
        #region --- Public Methods ---
        
        [CanBeNull]
        internal Dictionary<string, object> GetAllDataAsDictionary()
        {
            var getDataFlags = Enum.GetValues(typeof(ESwGetDataFlag)) as ESwGetDataFlag[];

            if (TryConstructGetDataDictionary(getDataFlags, out var resultDictionary))
            {
                return resultDictionary;
            }

            SwInfra.Logger.LogError(EWisdomLogType.DataBridge, $"{nameof(GetAllDataAsDictionary)} | Could not convert Enum values to ESwGetDataFlag array");
            return null;
        }

        [CanBeNull]
        internal Dictionary<string, object> GetDataBasedOnFlagsAsDictionary(params ESwGetDataFlag[] getDataFlags)
        {
            if (TryConstructGetDataDictionary(getDataFlags, out var resultDictionary))
            {
                return resultDictionary;
            }

            SwInfra.Logger.LogError(EWisdomLogType.DataBridge, $"{nameof(GetDataBasedOnFlagsAsDictionary)} | getDataFlags cannot be null or empty");
            return null;
        }

        [CanBeNull]
        internal string GetAllDataAsJsonString()
        {
            var data = GetAllDataAsDictionary();
            return SwUtils.JsonHandler.SerializeObject(data, _jsonSerializerSettings);
        }

        [CanBeNull]
        internal string GetDataBasedOnFlagsAsJsonString(params ESwGetDataFlag[] getDataFlags)
        {
            var data = GetDataBasedOnFlagsAsDictionary(getDataFlags);
            return SwUtils.JsonHandler.SerializeObject(data, _jsonSerializerSettings);
        }

        internal void SetData(string key, string value)
        {
            if (key.SwIsNullOrEmpty() || value.SwIsNullOrEmpty())
            {
                SwInfra.Logger.LogError(EWisdomLogType.DataBridge, $"{nameof(SetData)} | key and value cannot be null or empty");
                return;
            }

            SwInfra.KeyValueStore.SetString(key, value);
            DataDictionary.SwAddOrReplace(key, value);
            CustomAndExternalDataDictionary.SwAddOrReplace(key, value);
        }
        
        #endregion
        
        
        #region --- Private Methods ---

        private bool TryConstructGetDataDictionary(ESwGetDataFlag[] getDataFlags, out Dictionary<string, object> resultDictionary)
        {
            if (getDataFlags != null && !getDataFlags.SwIsNullOrEmpty())
            {
                resultDictionary = ConstructGetDataDictionary(getDataFlags);
                return true;
            }

            resultDictionary = null;
            return false;
        }

        protected Dictionary<string, object> ConstructGetDataDictionary(IEnumerable<ESwGetDataFlag> getDataFlags)
        {
            var data = new Dictionary<string, object>();
            
            foreach (var flag in getDataFlags)
            {
                AddDataToDictionary(data, flag);
            }
            
            SwInfra.Logger.Log(EWisdomLogType.DataBridge, $"{nameof(ConstructGetDataDictionary)} | Constructed data dictionary: {data.SwToString()}");
            return data;
        }

        protected abstract void AddDataToDictionary(Dictionary<string, object> data, ESwGetDataFlag flag);
        
        #endregion
    }
}