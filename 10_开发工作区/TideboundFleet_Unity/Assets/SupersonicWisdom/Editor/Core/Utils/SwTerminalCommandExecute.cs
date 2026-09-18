using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SupersonicWisdomSDK.Editor
{
    public class SwTerminalCommandExecute
    {
        #region --- Constants ---

        private const string WINDOWS_COMMAND_LINE = "cmd.exe";
        private const string OSX_SHELL = "/bin/bash";
        private const string OSX_SHELL_FLAGS = "-l -c";
        private const string HOMEBREW_PATH = "/opt/homebrew/bin:";
        private const string UTF8 = "en_US.UTF-8";

        #endregion


        #region --- Public Methods ---

        public string ExecuteTerminalCommand(string command, string args, string regexPattern)
        {
            var (shell, shellArgs) = GetShellAndArguments(command, args);
            string warningMessage;

            var startInfo = new ProcessStartInfo
            {
                FileName = shell,
                Arguments = shellArgs,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            // Set environment variables for macOS/Linux
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                startInfo.EnvironmentVariables["PATH"] = HOMEBREW_PATH + Environment.GetEnvironmentVariable("PATH");
                startInfo.EnvironmentVariables["LANG"] = UTF8;
            }

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                warningMessage = "Failed to start process.";
                SwInfra.Logger.LogWarning(EWisdomLogType.Build, warningMessage);
                SwEditorTracker.TrackEditorEvent(nameof(ExecuteTerminalCommand),ESwEditorWisdomLogType.PreBuild, ESwEventSeverity.Warning, warningMessage);
                return string.Empty;
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            var match = Regex.Match(string.IsNullOrEmpty(output) ? error : output, regexPattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            warningMessage = $"ExecuteTerminalCommand Failed with error: {error}";
            SwInfra.Logger.LogWarning(EWisdomLogType.Build, warningMessage);
            SwEditorTracker.TrackEditorEvent(nameof(ExecuteTerminalCommand),ESwEditorWisdomLogType.PreBuild, ESwEventSeverity.Error, warningMessage);
            return string.Empty;
        }

        #endregion


        #region --- Private Methods ---

        private (string shell, string shellArgs) GetShellAndArguments(string command, string args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return (WINDOWS_COMMAND_LINE, $"{command} {args}");
            }

            var shellArgs = $"{OSX_SHELL_FLAGS} \"{command} {args}\"";
            return (OSX_SHELL, shellArgs);
        }

        #endregion
    }
}