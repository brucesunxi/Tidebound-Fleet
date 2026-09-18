using UnityEngine;

namespace SupersonicWisdomSDK
{
    internal class SwSettingsFactory<T> where T : SwSettings, ISwSettings
    {
        #region --- Members ---
        
        private readonly string _resourcePath;

        #endregion


        #region --- Construction ---

        public SwSettingsFactory(string resourcePath = SwConstants.SETTINGS_RESOURCE_PATH)
        {
            _resourcePath = resourcePath;
        }

        #endregion


        #region --- Public Methods ---

        public T GetInstance(T settings = null)
        {
            var resourceSettings = settings ?? Resources.Load(_resourcePath, typeof(T)) as T;
            
            if (resourceSettings == null && Application.isPlaying)
            {
                Debug.LogError("Missing SupersonicWisdom's Settings asset!\nTo create the settings asset, go to: Window > SupersonicWisdom > Edit Settings.\nQuitting the app!");
                Application.Quit();
            }
            else if (resourceSettings != null)
            {
                resourceSettings.Init();
                SwInfra.Logger.Setup(resourceSettings.IsDebugEnabled);
            }
            
            return resourceSettings;
        }

        #endregion
    }
}