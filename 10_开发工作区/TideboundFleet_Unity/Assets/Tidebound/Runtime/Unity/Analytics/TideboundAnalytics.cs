using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Tidebound.Core;
using Tidebound.Tools;
using Tidebound.Unity.LevelDesign;
using UnityEngine;
using UnityEngine.Networking;

namespace Tidebound.Unity.Analytics
{
    /// <summary>Optional telemetry. Never participates in game transactions or save validation.</summary>
    public sealed class TideboundAnalytics : MonoBehaviour
    {
        [Serializable] private sealed class Config { public string endpoint; public bool enabled; }
        [Serializable] private sealed class Registration
        { public string installationId, secret; public bool consent = true, ageEligible = true, isTest; }
        private PortraitPuzzleGraybox game;
        private AnalyticsBuffer buffer;
        private string path, endpoint, sessionId, observedAttempt;
        private bool configured, backgrounded, observedComplete, observedDeadlock, failedStorage;
        private float nextSend, retrySeconds = 10, backgroundAt;
        private Coroutine sending;
        private UnityWebRequest request;
        public bool IsEnabled => configured && !failedStorage && buffer != null && buffer.CanCollect;
        public bool IsAvailable => configured && !failedStorage;
        public bool IsDeletionPending => buffer?.State.pendingDeletion == true;
        public string ReferenceId => buffer?.State.installationId ?? "";

        public void Initialize(PortraitPuzzleGraybox owner)
        {
            game = owner;
            try
            {
                var asset = Resources.Load<TextAsset>("TideboundAnalytics");
                var config = asset == null ? null : JsonUtility.FromJson<Config>(asset.text);
                configured = config != null && config.enabled && Uri.TryCreate(config.endpoint, UriKind.Absolute, out var uri) &&
                    uri.Scheme == "https" && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query);
                endpoint = configured ? config.endpoint.TrimEnd('/') : null;
                var root = Application.persistentDataPath;
#if UNITY_EDITOR
                root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/Tidebound"));
#endif
                path = Path.Combine(root, "analytics-v1.json");
                buffer = new AnalyticsBuffer(File.Exists(path) ? JsonUtility.FromJson<AnalyticsState>(File.ReadAllText(path)) : new AnalyticsState());
                NewSession(); nextSend = Time.realtimeSinceStartup + 10;
            }
            catch { failedStorage = true; }
        }
        public bool SetConsent(bool adultConfirmed, bool optedIn)
        {
            if (buffer == null) return false;
            CancelRequest();
            if (!adultConfirmed || !optedIn)
            {
                buffer.Withdraw(); observedAttempt = null; Persist(); nextSend = 0; return true;
            }
            if (!IsAvailable || buffer.State.pendingDeletion) return false;
            var state = buffer.State;
            if (string.IsNullOrEmpty(state.installationId))
            {
                state.installationId = Guid.NewGuid().ToString("D");
                var bytes = new byte[32]; using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
                state.secret = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
                state.isTest = Application.isEditor || Debug.isDebugBuild;
            }
            state.ageEligible = true; state.consent = true; observedAttempt = null;
            NewSession(); Persist(); nextSend = 0; return IsEnabled;
        }
        private void NewSession()
        { sessionId = Guid.NewGuid().ToString("D"); Track("session_start"); }
        public void TrackTool(ShipTool tool)
        { if (tool != ShipTool.None) Track("tool_use", tool.ToString()); }
        public void TrackRestart() { Track("level_restart"); }
        private void Track(string name, string tool = "")
        {
            if (!IsEnabled) return;
            var isLevel = name != "session_start";
            if (isLevel && game?.Session == null) return;
            var value = new AnalyticsEvent { eventId = Guid.NewGuid().ToString("D"), name = name,
                occurredAt = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
                attemptId = isLevel ? game.Session.SessionId : "", level = isLevel ? game.LevelIndex + 1 : 0, tool = tool,
                durationMs = isLevel ? Mathf.Clamp(Mathf.RoundToInt(buffer.State.activeSeconds * 1000), 0, 604800000) : 0 };
            buffer.Add(new PendingAnalyticsEvent { sessionId = sessionId, appVersion = Application.version,
                platform = Application.isEditor ? "Editor" : Application.platform == RuntimePlatform.IPhonePlayer ? "iOS" : "Android", item = value });
            Persist();
        }
        private void LateUpdate()
        {
            if (!configured || failedStorage || buffer == null) return;
            if (IsEnabled && game?.Session != null && game.SaveService?.IsAvailable == true && !game.IsHomeOpen && game.IsEntryReady && !game.IsEntrySaveBlocked)
            {
                var attempt = game.Session.SessionId;
                if (observedAttempt != attempt)
                {
                    observedAttempt = attempt; observedComplete = false; observedDeadlock = false;
                    if (buffer.State.activeAttempt != attempt) { buffer.State.activeAttempt = attempt; buffer.State.activeSeconds = 0; }
                    if (!game.IsCleared) Track(game.IsResumingEntry ? "level_resume" : "level_start");
                }
                if (!backgrounded && !game.IsPaused && !game.IsMenuOpen && !game.IsAcquisitionOpen && !game.IsCollectionOpen && game.Session.State == GameState.Playing)
                    buffer.State.activeSeconds += Time.unscaledDeltaTime;
                if (!observedComplete && game.IsCleared && game.SaveService.CurrentAttemptSettled)
                { observedComplete = true; Track("level_complete"); }
                var deadlock = game.Progress?.NeedsRescue == true;
                if (deadlock && !observedDeadlock) Track("level_deadlock");
                observedDeadlock = deadlock;
            }
            if (!backgrounded && sending == null && Time.realtimeSinceStartup >= nextSend && (IsEnabled || IsDeletionPending))
                sending = StartCoroutine(Send());
        }
        private IEnumerator Send()
        {
            // Yield once so the Coroutine handle is assigned even when there is no batch.
            yield return null;
            var state = buffer.State;
            if (state.pendingDeletion)
            {
                yield return SendRequest("installation", "DELETE", null, true);
                if (Succeeded() || request?.responseCode == 401)
                {
                    buffer = new AnalyticsBuffer(new AnalyticsState()); Persist(); retrySeconds = 10;
                }
            }
            else if (IsEnabled)
            {
                if (!state.registered)
                {
                    yield return SendRequest("register", "POST", JsonUtility.ToJson(new Registration {
                        installationId = state.installationId, secret = state.secret, isTest = state.isTest }), false);
                    if (Succeeded()) { state.registered = true; Persist(); }
                }
                if (state.registered && IsEnabled)
                {
                    var batch = buffer.Peek(DateTime.UtcNow);
                    if (batch != null)
                    {
                        yield return SendRequest("events", "POST", JsonUtility.ToJson(batch), true);
                        if (Succeeded()) { buffer.Acknowledge(batch); retrySeconds = 10; Persist(); }
                        else if (request?.responseCode == 401) { state.registered = false; Persist(); }
                    }
                }
            }
            if (request != null && !Succeeded()) retrySeconds = Mathf.Min(300, retrySeconds * 2);
            request?.Dispose(); request = null;
            nextSend = Time.realtimeSinceStartup + retrySeconds; sending = null;
        }
        private IEnumerator SendRequest(string route, string method, string body, bool authenticate)
        {
            request?.Dispose(); request = new UnityWebRequest(endpoint + "/api/" + route, method) { downloadHandler = new DownloadHandlerBuffer(), timeout = 10 };
            if (body != null) { request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)); request.SetRequestHeader("Content-Type", "application/json"); }
            if (authenticate) request.SetRequestHeader("Authorization", "Bearer " + buffer.State.installationId + "." + buffer.State.secret);
            yield return request.SendWebRequest();
        }
        private bool Succeeded() => request != null && request.result == UnityWebRequest.Result.Success && request.responseCode == 200;
        private void Persist()
        {
            if (path == null || buffer == null || failedStorage) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var temp = path + ".tmp"; File.WriteAllText(temp, JsonUtility.ToJson(buffer.State));
                if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
            }
            catch { failedStorage = true; CancelRequest(); }
        }
        private void CancelRequest()
        { if (sending != null) StopCoroutine(sending); sending = null; request?.Abort(); request?.Dispose(); request = null; }
        private void OnApplicationPause(bool paused)
        {
            backgrounded = paused;
            if (paused) { backgroundAt = Time.realtimeSinceStartup; Persist(); }
            else if (Time.realtimeSinceStartup - backgroundAt >= 1800) NewSession();
        }
        private void OnApplicationQuit() { Persist(); CancelRequest(); }
        private void OnDestroy() { CancelRequest(); }
    }
}
