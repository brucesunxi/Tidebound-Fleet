#if UNITY_IOS

using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace SupersonicWisdomSDK.Editor
{
    public static class SwEntitlementsIosInjector
    {
        #region --- Constants ---
        
        private const string ENTITLEMENTS_FILE_NAME = "SwEntitlements.entitlements";
        private const string ENTITLEMENTS_FILE_PATH = "SupersonicWisdom/Editor/Core/Entitlements";
        
        #endregion
        
        
        #region --- Public Methods ---
        
        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;
            
            var projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            var pbxProject = new PBXProject();
            pbxProject.ReadFromString(File.ReadAllText(projPath));
            
            var targetGuid = pbxProject.GetUnityMainTargetGuid();
            var scriptFolderPath = Path.Combine(Application.dataPath, ENTITLEMENTS_FILE_PATH);
            var entitlementsSourcePath = Path.Combine(scriptFolderPath, ENTITLEMENTS_FILE_NAME);
            var entitlementsDestPath = Path.Combine(pathToBuiltProject, ENTITLEMENTS_FILE_NAME);
            
            File.Copy(entitlementsSourcePath, entitlementsDestPath, true);
            
            pbxProject.AddFileToBuild(targetGuid, pbxProject.AddFile(entitlementsDestPath, ENTITLEMENTS_FILE_NAME));
            var didAddCapabilityWasAdded = pbxProject.AddCapability(targetGuid, PBXCapabilityType.ApplePay, ENTITLEMENTS_FILE_NAME);
            
            if (!didAddCapabilityWasAdded)
            {
                SwInfra.Logger.LogWarning(EWisdomLogType.Build, $"Failed to add capability ApplePay to {ENTITLEMENTS_FILE_NAME}");
                return;
            }
            
            File.WriteAllText(projPath, pbxProject.WriteToString());
        }
        
        #endregion
    }
}

#endif