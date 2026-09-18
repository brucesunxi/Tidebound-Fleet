#if SW_STAGE_STAGE10_OR_ABOVE

namespace SupersonicWisdomSDK
{
    public interface ISwStage10ConnectionStatusListener
    {
        public void OnConnectionStatusChanged(SwConnectionStatusDto statusDto, bool didStatusChanged);
    }
}

#endif