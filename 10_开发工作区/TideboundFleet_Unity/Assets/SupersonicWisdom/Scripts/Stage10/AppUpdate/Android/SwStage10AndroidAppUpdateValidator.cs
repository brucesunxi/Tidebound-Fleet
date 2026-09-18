#if SW_STAGE_STAGE10_OR_ABOVE
using System.Collections;
using UnityEngine;

namespace SupersonicWisdomSDK
{
    internal class SwStage10AndroidAppUpdateValidator : SwBaseAppUpdateValidator
    {
        #region --- Members ---
        
        private readonly ISwStage10NativeApi _nativeApi;
        private bool _isUpdateCheckComplete;
        
        #endregion
        
        
        #region --- Construction ---
        
        public SwStage10AndroidAppUpdateValidator(ISwStage10NativeApi nativeApi)
        {
            _nativeApi = nativeApi;
        }
        
        #endregion
        
        
        #region --- Public Methods ---
        
        public override IEnumerator CheckForUpdate()
        {
            _nativeApi.CheckForUpdate();

            yield return new WaitUntil(() => _isUpdateCheckComplete);
        }
        
        public void OnUpdateCheckResult(string versionCode)
        {
            SwInfra.Logger.Log(EWisdomLogType.AppUpdate ,"Callback from Native - OnUpdateCheckResult: versionCode = " + versionCode);
            IsUpdateAvailable = !versionCode.SwIsNullOrEmpty();
            _isUpdateCheckComplete = true;
        }
        
        #endregion
    }
}
#endif