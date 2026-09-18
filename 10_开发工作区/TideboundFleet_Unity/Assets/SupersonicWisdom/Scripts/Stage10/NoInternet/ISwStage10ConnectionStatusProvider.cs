#if SW_STAGE_STAGE10_OR_ABOVE

using System.Collections.Generic;

namespace SupersonicWisdomSDK
{
    public interface ISwStage10ConnectionStatusProvider
    {
        SwConnectionStatusDto ConnectionStatus { get; }
        public void AddListeners(List<ISwStage10ConnectionStatusListener> listeners);
        public void RemoveListeners(List<ISwStage10ConnectionStatusListener> listeners);
    }
}

#endif