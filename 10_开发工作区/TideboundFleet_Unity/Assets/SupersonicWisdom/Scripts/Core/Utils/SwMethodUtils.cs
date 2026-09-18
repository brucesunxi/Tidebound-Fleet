using System;

namespace SupersonicWisdomSDK
{
    internal class SwMethodUtils
    {
        #region --- Public Methods ---
        
        /// <summary>
        ///   Safely invokes the action. If an exception occurs, logs it.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="logType"></param>
        internal void InvokeMethodSafely(Action action, EWisdomLogType logType)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                SwInfra.Logger.LogException(e, logType, $"There was an error invoking method {action?.Method.Name}.");
            }
        }
        
        #endregion
    }
}