#if SW_STAGE_STAGE10_OR_ABOVE
namespace SupersonicWisdomSDK
{
    /// <summary>
    /// This class is used to pass the parameters required for updating the app to our Native Bridge.
    /// </summary>
    public abstract class SwAppUpdateParam { }
    
    public class SwIosAppUpdateParams : SwAppUpdateParam
    {
        public string AppStoreId { get; }
        
        public SwIosAppUpdateParams(string appStoreId)
        {
            AppStoreId = appStoreId;
        }
    }
    
    public class SwAndroidAppUpdateParams : SwAppUpdateParam
    {
        public int UpdateType { get; }
        
        public SwAndroidAppUpdateParams(int updateType)
        {
            UpdateType = updateType;
        }
    }
}
#endif