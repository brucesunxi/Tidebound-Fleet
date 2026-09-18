using System.Threading;
using UnityEngine;

namespace SupersonicWisdomSDK
{
    internal static class SwInfra
    {
        #region --- Members ---

        private static ISwLogger _logger;
        private static MonoBehaviour _mono;
        private static bool _isInitialized;
        private static ISwKeyValueStore _keyValueStore;
        private static ISwKeyValueStore _jsonKeyValueStore;
        private static SwCoroutineService _coroutineService;
        private static SwFilesCacheManager _fileCacheManager;
        private static ISwTacSystem _tacSystem;

        #endregion


        #region --- Properties ---

        public static ISwKeyValueStore KeyValueStore
        {
            get { return _keyValueStore ??= new SwPlayerPrefsStore(); }
        }
        
        public static ISwKeyValueStore JsonKeyValueStore
        {
            get { return _jsonKeyValueStore ??= new SwJsonKeyValueStore(); }
        }

        public static ISwLogger Logger
        {
            get { return _logger ??= new SwLoggerService(); }
        }

        public static SwFilesCacheManager FileCacheManager
        {
            get { return _fileCacheManager ??= new SwFilesCacheManager(); }
        }

        public static ISwMainThreadRunner MainThreadRunner { get; private set; }

        public static SwCoroutineService CoroutineService
        {
            get { return _coroutineService; }
        }

        public static Thread MainThread { get; } = Thread.CurrentThread;
        
        public static ISwTacSystem TacSystem
        {
            get { return _tacSystem ??= new SwTacSystem();}
        }

        #endregion


        #region --- Public Methods ---

        public static void Initialize(ISwMainThreadRunner mainThreadRunner, MonoBehaviour mono, ISwTacSystem tacSystem)
        {
            MainThreadRunner = mainThreadRunner;
            _coroutineService = new SwCoroutineService(mono);
            _tacSystem = tacSystem;
        }

        #endregion
    }
}