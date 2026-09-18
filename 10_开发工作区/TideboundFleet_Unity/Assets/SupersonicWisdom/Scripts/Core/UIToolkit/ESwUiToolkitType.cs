namespace SupersonicWisdomSDK
{
    internal enum ESwUiToolkitType
    {
        None,
        AppUpdate,
#if SW_STAGE_STAGE20_OR_ABOVE
        NoInternet,
#endif
#if SW_STAGE_STAGE30_OR_ABOVE
        CountryBlocker,
        PrivacyPolicy,
#endif
#if SW_STAGE_STAGE40_OR_ABOVE
        IapError,
#endif
    }

}