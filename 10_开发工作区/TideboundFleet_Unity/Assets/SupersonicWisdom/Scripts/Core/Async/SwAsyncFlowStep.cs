using System;

namespace SupersonicWisdomSDK
{
    internal sealed class SwAsyncFlowStep
    {
        #region --- Properties ---

        public int ExecutionIndex { get; }
        public int? MaxExecutionIndex { get; }
        public SwAsyncMethod RunMethod { get; }

        #endregion


        #region --- Construction ---

        internal SwAsyncFlowStep(ISwAsyncRunnable runnable, int executionIndex, int? maxExecutionIndex = null)
        {
            RunMethod = runnable.Run;
            ExecutionIndex = executionIndex;
            MaxExecutionIndex = maxExecutionIndex;
        }
        
        internal SwAsyncFlowStep(SwAsyncMethod customRun, int executionIndex, int? maxExecutionIndex = null)
        {
            RunMethod = customRun;
            ExecutionIndex = executionIndex;
            MaxExecutionIndex = maxExecutionIndex;
        }
        
        #endregion
    }
}