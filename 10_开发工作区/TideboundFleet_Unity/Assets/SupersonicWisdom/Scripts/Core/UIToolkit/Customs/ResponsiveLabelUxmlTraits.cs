namespace SupersonicWisdomSDK
{
    using UnityEngine.UIElements;

    public class ResponsiveLabelUxmlTraits : Label.UxmlTraits
    {
        #region --- Members ---
        
        private readonly UxmlIntAttributeDescription m_LandscapeFontSize = new UxmlIntAttributeDescription
            { name = "landscape-font-size", defaultValue = 14 };

        private readonly UxmlIntAttributeDescription m_PortraitFontSize = new UxmlIntAttributeDescription
            { name = "portrait-font-size", defaultValue = 14 };
        
        #endregion


        #region --- Public Methods ---
        
        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);

            if (ve is not ResponsiveLabel responsiveLabel) return;
            
            responsiveLabel.LandscapeFontSize = m_LandscapeFontSize.GetValueFromBag(bag, cc);
            responsiveLabel.PortraitFontSize = m_PortraitFontSize.GetValueFromBag(bag, cc);
        }
        
        #endregion
    }

    #region --- Inner Class ---
    
    public class ResponsiveLabelUxmlFactory : UxmlFactory<ResponsiveLabel, ResponsiveLabelUxmlTraits> { }
    
    #endregion
}