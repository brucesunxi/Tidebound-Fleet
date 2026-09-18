#if SW_STAGE_STAGE10_OR_ABOVE

using System;

namespace SupersonicWisdomSDK
{
    [Serializable]
    public class SwConnectionStatusDto
    {
        #region --- Members ---

        public bool isAvailable = true;
        public bool isFlightMode = false;

        #endregion
    }
}

#endif