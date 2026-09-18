#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

// This script embeds frameworks that are not automatically embedded by Unity/EDM4U.
// https://stackoverflow.com/questions/26024100/dyld-library-not-loaded-rpath-libswiftcore-dylib
public static class SwPbxEmbedFrameworks
{
    private const string XC_FRAMEWORK_EXTENSION = ".xcframework";

    // List of frameworks to embed
    private static readonly List<string> FrameworkNames = new List<string>
    {
        "AppLovinSDK",
        "FBAEMKit",
        "MolocoSDK",
        "FBSDKCoreKit",
        "FBSDKCoreKit_Basics",
        "FBSDKGamingServicesKit",
        "FBSDKLoginKit",
        "FBSDKShareKit",
    };

    [PostProcessBuild(101)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
        {
            return;
        }

        var projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        var project = new PBXProject();
        project.ReadFromString(File.ReadAllText(projectPath));
        
        var targetGuid = project.GetUnityMainTargetGuid();
        EmbedFrameworks(project, targetGuid, pathToBuiltProject);
        File.WriteAllText(projectPath, project.WriteToString());
    }

    private static void EmbedFrameworks(PBXProject project, string targetGuid, string pathToBuiltProject)
    {
        var podsPath = Path.Combine(pathToBuiltProject, "Pods");
        var frameworkPaths = FindXCFrameworks(podsPath);

        foreach (var frameworkName in FrameworkNames)
        {
            var matchedFrameworks = frameworkPaths
                .Where(path => path.EndsWith($"{frameworkName}{XC_FRAMEWORK_EXTENSION}"))
                .ToList();

            if (matchedFrameworks.Count == 0)
            {
                Debug.LogWarning($"Framework {frameworkName}{XC_FRAMEWORK_EXTENSION} not found in Pods directory.");
                continue;
            }

            if (matchedFrameworks.Count > 1)
            {
                Debug.LogWarning($"Multiple matches found for {frameworkName}{XC_FRAMEWORK_EXTENSION}. Using the first match.");
            }

            var frameworkPath = matchedFrameworks.First();
            var fileGuid = project.AddFile(frameworkPath, frameworkPath, PBXSourceTree.Absolute);
            project.AddFileToEmbedFrameworks(targetGuid, fileGuid);

            project.AddBuildProperty(targetGuid, "LD_RUNPATH_SEARCH_PATHS", "$(inherited) @executable_path/Frameworks");
        }
    }

    private static List<string> FindXCFrameworks(string searchingPath)
    {
        var xcframeworks = new List<string>();
        var directories = Directory.GetDirectories(searchingPath, "*", SearchOption.AllDirectories);

        foreach (var directory in directories)
        {
            if (directory.EndsWith(XC_FRAMEWORK_EXTENSION))
            {
                xcframeworks.Add(directory);
            }
        }

        return xcframeworks;
    }
}
#endif
