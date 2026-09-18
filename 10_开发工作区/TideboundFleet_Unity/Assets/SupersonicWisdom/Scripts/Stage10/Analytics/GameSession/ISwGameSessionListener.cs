#if SW_STAGE_STAGE10_OR_ABOVE
using System;

namespace SupersonicWisdomSDK
{
    internal interface ISwGameSessionListener
    {
        void OnSessionStarted(string sessionId, string flag, int bruttoDuration, int nettoDuration);
        void OnSessionEnded(string sessionId, string flag, int bruttoDuration, int nettoDuration);
    }
}

#endif