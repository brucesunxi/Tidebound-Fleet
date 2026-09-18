namespace SupersonicWisdomSDK
{
    public enum ESwActionType
    {
        None,
#if SW_STAGE_STAGE10_OR_ABOVE
        ShowAppUpdate,        
#endif
#if SW_STAGE_STAGE20_OR_ABOVE
        ShowAtt,
#endif
#if SW_STAGE_STAGE30_OR_ABOVE
        ShowInterstitial,
        ShowRateUs,
        NagCmp,
        ShowPrivacy,
        ShowNotificationPermission,
#endif
    }
}