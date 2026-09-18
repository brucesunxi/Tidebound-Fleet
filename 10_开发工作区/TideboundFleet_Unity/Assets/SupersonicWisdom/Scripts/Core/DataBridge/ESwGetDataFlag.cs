namespace SupersonicWisdomSDK
{
    public enum ESwGetDataFlag
    {
        #if SW_STAGE_STAGE10_OR_ABOVE
        InstallDate,
        SessionId,
        MegaSessionId,
        BundleId,
        IosStoreId,
        SupersonicId,
        AppVersion,
        Os,
        OsVersion,
        TotalRevenue,
        MainLevel,
        TotalNetoPlayTime,
        ActiveDay,
        #endif
        
        #if SW_STAGE_STAGE20_OR_ABOVE
        Idfa,
        Idfv,
        Gaid,
        AppsetId,
        WisdomAbId,
        WisdomAbVariantId,
        AdsLtv,
        #endif
        
        #if SW_STAGE_STAGE30_OR_ABOVE
        FirebaseAbId,
        FirebaseAbVariantId,
        #endif
        
        #if SW_STAGE_STAGE40_OR_ABOVE
        IapLtv,
        #endif
    }
}