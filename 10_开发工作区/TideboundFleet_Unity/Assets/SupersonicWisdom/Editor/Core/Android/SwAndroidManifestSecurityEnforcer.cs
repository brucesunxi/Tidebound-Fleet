#if UNITY_ANDROID

using System.Xml;
using SupersonicWisdomSDK.Editor;
using UnityEditor.Android;

public class SwAndroidManifestSecurityEnforcer : IPostGenerateGradleAndroidProject
{
    #region --- Constants ---
    
    private const string ALLOW_BACKUP = "android:allowBackup";
    
    #endregion
    
    
    #region --- Properties ---
    
    public int callbackOrder
    {
        get { return int.MaxValue; }
    }
    
    #endregion
    
    
    #region --- Public Methods ---
    
    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var manifestPath = $"{path}/src/main/AndroidManifest.xml";
        SetManifestForProduction(manifestPath);
    }
    
    #endregion
    
    
    #region --- Private Methods ---
    
    private static void SetManifestForProduction(string manifestPath)
    {
#if !DEVELOPMENT_BUILD
        var xmlDoc = new SwAndroidXmlDocument(manifestPath);
        var applicationNode = xmlDoc.DocumentElement?.SelectSingleNode("/manifest/application");
        
        if (applicationNode is XmlElement applicationElement)
        {
            var toolsReplaceValue = applicationElement.GetAttribute("replace", SwAndroidXmlDocument.TOOLS_XML_NAMESPACE);
            
            if (!toolsReplaceValue.Contains(ALLOW_BACKUP))
            {
                toolsReplaceValue = toolsReplaceValue.Length > 0 ? toolsReplaceValue + ",android:allowBackup" : "android:allowBackup";
                applicationElement.SetAttribute("replace", SwAndroidXmlDocument.TOOLS_XML_NAMESPACE, toolsReplaceValue);
            }
            
            applicationElement.SetAttribute("debuggable", SwAndroidXmlDocument.ANDROID_XML_NAMESPACE, "false");
            applicationElement.SetAttribute("allowBackup", SwAndroidXmlDocument.ANDROID_XML_NAMESPACE, "false");
            
            xmlDoc.Save();
        }
#endif
    }
    
    #endregion
}

#endif