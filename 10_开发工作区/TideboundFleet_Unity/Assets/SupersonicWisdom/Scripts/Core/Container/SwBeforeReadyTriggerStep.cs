using System.Collections;

namespace SupersonicWisdomSDK
{
    internal class SwBeforeReadyTriggerStep : ISwAsyncRunnable
    {
        #region --- Constants ---

        public const string BEFORE_READY = "before_ready";

        #endregion


        #region --- Public Methods ---

        public IEnumerator Run()
        {
            yield return SwInfra.TacSystem.InternalFireTriggersRoutine(BEFORE_READY);
        }

        #endregion
    }
}