namespace SupersonicWisdomSDK
{
    internal interface ISwUiToolkitWindowStateListener
    {
        public void OnWindowOpened(SwUiToolkitWindow window);
        public void OnWindowClosed(SwUiToolkitWindow window);
    }
}