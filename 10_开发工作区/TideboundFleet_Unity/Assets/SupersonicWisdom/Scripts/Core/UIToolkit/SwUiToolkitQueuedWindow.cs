using System;

namespace SupersonicWisdomSDK
{
    /// <summary>
    /// Represents a queued window in the Supersonic Wisdom UI Toolkit.
    /// </summary>
    internal class SwUiToolkitQueuedWindow
    {
        #region --- Properties ---

        internal ESwUiToolkitType Type { get; }
        internal SwVisualElementPayload[] Payloads { get; }
        internal int Priority { get; }
        internal Func<bool> Condition { get; }
        internal Action OnOpenAction { get; }

        #endregion
        
        
        #region --- Construction ---
        
        internal SwUiToolkitQueuedWindow(ESwUiToolkitType type, SwVisualElementPayload[] payloads, int priority, Func<bool> condition, Action onOpenAction)
        {
            Type = type;
            Payloads = payloads;
            Priority = priority;
            Condition = condition;
            OnOpenAction = onOpenAction;
        }
        
        #endregion
    }
}