#if SW_STAGE_STAGE10_OR_ABOVE
using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace SupersonicWisdomSDK
{
    internal class SwStage10IosAppUpdateValidator : SwBaseAppUpdateValidator
    {
        #region --- Constants ---
        
        private const string APP_STORE_LOOKUP_URL = "https://itunes.apple.com/lookup?bundleId=";
        
        #endregion
        
        
        #region --- Public Methods ---
        
        
        public override IEnumerator CheckForUpdate()
        {
            var bundleId = Application.identifier; 
            var url = $"{APP_STORE_LOOKUP_URL}{bundleId}";

            using var webRequest = UnityWebRequest.Get(url);
    
            yield return webRequest.SendWebRequest();
            
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                SwInfra.Logger.LogError(EWisdomLogType.AppUpdate, "Error checking for updates: " + webRequest.error);
            }
            else
            {
                try
                {
                    var applicationVersion = Application.version;
                    var result = JObject.Parse(webRequest.downloadHandler.text);

                    if (result["results"] is JArray resultsArray && resultsArray.Count > 0)
                    {
                        var appInfo = resultsArray.First as JObject;
                        var storeVersion = appInfo?["version"]?.Value<string>() ?? string.Empty;
                        
                        if (IsUpdateRequired(applicationVersion, storeVersion))
                        {
                            SwInfra.Logger.Log(EWisdomLogType.AppUpdate, "New Update Available: " + storeVersion);
                            IsUpdateAvailable = true;
                        }
                        else
                        {
                            SwInfra.Logger.Log(EWisdomLogType.AppUpdate, "No need to update");
                            IsUpdateAvailable = false;
                        }
                    }
                    else
                    {
                        SwInfra.Logger.LogError(EWisdomLogType.AppUpdate, "Error: No results returned from App Store lookup.");
                    }
                }
                catch (Exception e)
                {
                    SwInfra.Logger.LogError(EWisdomLogType.AppUpdate, "Exception parsing update info: " + e.Message);
                }
            }
        }

        private static bool IsUpdateRequired(string currentVersion, string latestVersion)
        {
            return SwUtils.System.IsBVersionNewer(currentVersion, latestVersion);
        }
        
        #endregion
    }
}
#endif