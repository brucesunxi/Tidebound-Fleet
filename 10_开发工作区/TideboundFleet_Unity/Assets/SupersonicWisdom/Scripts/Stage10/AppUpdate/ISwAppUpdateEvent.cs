#if SW_STAGE_STAGE10_OR_ABOVE
namespace SupersonicWisdomSDK
{
    /// <summary>
    /// Represents an event related to application updates.
    /// </summary>
    internal interface ISwAppUpdateEvent
    {
        const string UPDATE_PRESSED_EVENT_VALUE = "Update";
        const string UPDATE_CLOSE_EVENT_VALUE = "Close";
        string eventName { get; }
        string eventValue { get; set; }
    }
}
#endif