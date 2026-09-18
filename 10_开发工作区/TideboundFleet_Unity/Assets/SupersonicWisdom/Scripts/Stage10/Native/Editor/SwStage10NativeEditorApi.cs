#if SW_STAGE_STAGE10_OR_ABOVE && UNITY_EDITOR
namespace SupersonicWisdomSDK.Editor
{
    internal class SwStage10NativeEditorApi : SwNativeEditorApi, ISwStage10NativeApi
    {
        #region --- Construction ---
        
        public SwStage10NativeEditorApi(SwNativeBridge nativeBridge) : base(nativeBridge)
        {
        }
        
        #endregion
        
        
        #region --- Public Methods ---
        
        public void StartAppUpdate(SwAppUpdateParam updateParam)
        {
            SwInfra.Logger.Log(EWisdomLogType.Native);
        }
        
        public void CheckForUpdate()
        {
            SwInfra.Logger.Log(EWisdomLogType.Native);
        }

        public void AddUpdateCheckCallback(OnUpdateCheckResult onUpdateCheckResult)
        {
            SwInfra.Logger.Log(EWisdomLogType.Native);
        }

        public void RemoveUpdateCheckCallback(OnUpdateCheckResult onUpdateCheckResult)
        {
            SwInfra.Logger.Log(EWisdomLogType.Native);
        }

        public void AddUpdateStartedCallback(OnUpdateStarted onUpdateStarted)
        {
            SwInfra.Logger.Log(EWisdomLogType.Native);
        }

        public void RemoveUpdateStartedCallback(OnUpdateStarted onUpdateStarted)
        {
            SwInfra.Logger.Log(EWisdomLogType.Native);
        }
        
        #endregion
    }
}
#endif