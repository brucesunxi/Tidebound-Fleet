using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityDebug = UnityEngine.Debug;

namespace SupersonicWisdomSDK
{
    internal class SwLoggerService : ISwLogger
    {
        #region --- Constants ---

        private const string DefaultPrefix = "SupersonicWisdom: ";

        #endregion


        #region --- Members ---

        private string _prefix = DefaultPrefix;
        private Func<bool> _isDebugEnabledFunc;

        #endregion


        #region --- Properties ---

        public bool LogViaNetwork { get; set; } = false;

        internal string Prefix
        {
            get { return _prefix; }
            set { _prefix = string.IsNullOrEmpty(value) ? DefaultPrefix : value; }
        }

        #endregion


        #region --- Public Methods ---

        public void Setup(Func<bool> isDebugEnabledFunc)
        {
            _isDebugEnabledFunc = isDebugEnabledFunc;
        }

        #endregion


        #region --- Public Methods ---

        public bool IsEnabled ()
        {
            return _isDebugEnabledFunc?.Invoke() ?? false;
        }

        public void Log(EWisdomLogType wisdomLogType, string msg = "")
        {
            Log(LogType.Log, wisdomLogType,  msg);
        }

        public void LogError(EWisdomLogType wisdomLogType, Exception exception)
        {
            Log(LogType.Error, wisdomLogType, exception.ToString());
        }
        
        public void LogError(EWisdomLogType wisdomLogType, string msg)
        {
            Log(LogType.Error, wisdomLogType, msg);
        }

        public void LogWarning(EWisdomLogType wisdomLogType, string msg)
        {
            Log(LogType.Warning, wisdomLogType, msg);
        }
        
        public void LogException(Exception e, EWisdomLogType logType, string context)
        {
            var errorMessage = new StringBuilder();
            errorMessage.AppendLine($"An exception occurred in context: {context}");
            errorMessage.AppendLine($"Exception type: {e.GetType().FullName}");
            errorMessage.AppendLine($"Message: {e.Message}");
            errorMessage.AppendLine($"Source: {e.Source}");
            errorMessage.AppendLine("Stack Trace:");
            errorMessage.AppendLine(e.StackTrace);

            var innerException = e.InnerException;
            var innerCount = 1;
            
            while (innerException != null)
            {
                errorMessage.AppendLine($"Inner Exception {innerCount}: {innerException.GetType().FullName}");
                errorMessage.AppendLine($"Message: {innerException.Message}");
                errorMessage.AppendLine($"Source: {innerException.Source}");
                errorMessage.AppendLine("Stack Trace:");
                errorMessage.AppendLine(innerException.StackTrace);

                innerException = innerException.InnerException;
                innerCount++;
            }
            
            Log(LogType.Exception, logType, errorMessage.ToString());
        }

        #endregion


        #region --- Private Methods ---

        /// <summary>
        ///     Log the message to a dummy url in oder to see wisdom logs via Proxy app.
        /// </summary>
        /// <param name="level">Log Level</param>
        /// <param name="message">Log Message</param>
        /// <returns></returns>
        private IEnumerator LogThroughNetwork(string level, string message)
        {
            var webRequest = new UnityWebRequest($"http://supersonic-wisdom.log/{level}?message={Uri.EscapeUriString(message)}");

            yield return webRequest.SendWebRequest();
        }

        private string RefineMessage(string msg)
        {
            if (msg.IndexOf(";base64", StringComparison.Ordinal) != -1)
            {
                return Regex.Replace(msg, ";base64,(.*?)\"", ";base64,DATA\"");
            }

            return msg;
        }

        private void Log(LogType logType, EWisdomLogType wisdomLogType, string msg)
        {
            if (!IsEnabled()) return;
            
            var refinedMessage = RefineMessage(msg);
            var message = refinedMessage;
            var logThroughNetworkKey = string.Empty;
            var reflectedObject = new System.Diagnostics.StackTrace().GetFrame(2)?.GetMethod();

            if (reflectedObject != null)
            {
                var className = reflectedObject.ReflectedType?.Name ?? string.Empty;
                var methodName = reflectedObject?.Name ?? string.Empty;
                refinedMessage = !refinedMessage.SwIsNullOrEmpty() ? $" | {refinedMessage}" : string.Empty;
                message = $"{Prefix} #{wisdomLogType} | {className} | {methodName}{refinedMessage}";
            }

            switch (logType)
            {
                case LogType.Log:
                    UnityDebug.Log(message);
                    logThroughNetworkKey = "info";
                    break;
                case LogType.Warning:
                    UnityDebug.LogWarning(message);
                    logThroughNetworkKey = "warning";
                    break;
                case LogType.Error:
                case LogType.Exception:
                    UnityDebug.LogError(message);
                    logThroughNetworkKey = "error";
                    break;
                case LogType.Assert:
                default:
                    break;
            }
            
            if (LogViaNetwork)
            {
                SwInfra.CoroutineService.StartCoroutine(LogThroughNetwork(logThroughNetworkKey, message));
            }
        }

        #endregion
    }

    public enum EWisdomLogType
    {
        ActiveDay,
        Ad,
        Alive,
        Analytics,
        API,
        AppUpdate,
        AppsFlyer,
        Async,
        BackoffPolicy,
        BlockingApi,
        BlockingLoader,
        Build,
        Cache,
        CloudDiagnostics,
        Config,
        Container,
        ConversionValue,
        CoroutineService,
        CrossPromo,
        DataBridge,
        DeepLink,
        Devtools,
        Encryptor,
        EventBus,
        Facebook,
        Firebase,
        Fps,
        GameAnalytics,
        GameBlocker,
        GameSession,
        GameStateSystem,
        IAP,
        InGameConsole,
        IntegrationTool,
        InternetStatus,
        IronSource,
        Json,
        KeyValueStore,
        Native,
        Network,
        NoInternet,
        Notifications,
        Popups,
        Privacy,
        PromotionButton,
        RateUs,
        Revenue,
        SelfUpdate,
        Session,
        StoreKit,
        TAC,
        Test,
        ThirdParties,
        Time,
        UiToolkit,
        UGS,
        User,
        Utils,
    }
}