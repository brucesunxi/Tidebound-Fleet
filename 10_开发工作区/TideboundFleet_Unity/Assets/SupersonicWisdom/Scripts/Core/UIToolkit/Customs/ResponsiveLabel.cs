namespace SupersonicWisdomSDK
{
    using UnityEngine.UIElements;

    /// <summary>
    /// A label that adjusts its font size based on the current screen orientation. (should be used in conjuction with OnDisplay/OnGeometryChanged methods in SwUiToolkitWindow.cs)
    /// </summary>
    public class ResponsiveLabel : Label
    {
        #region --- Properties ---
        
        public int LandscapeFontSize { get; set; }
        public int PortraitFontSize { get; set; }
        
        #endregion
        
        
        #region --- Constructor ---
        
        public ResponsiveLabel() : this(string.Empty) { }
        public ResponsiveLabel(string text) : base(text) { }
        
        #endregion
    }
}