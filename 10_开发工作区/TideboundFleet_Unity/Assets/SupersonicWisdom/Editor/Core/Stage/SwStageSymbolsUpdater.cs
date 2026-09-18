#if !SUPERSONIC_WISDOM_TEST
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

namespace SupersonicWisdomSDK.Editor
{
    internal static class SwStageSymbolUpdater
    {

        [InitializeOnLoadMethod]
        public static void UpdateStageSymbols()
        {
            SwStage stage = SwUtils.JsonHandler.DeserializeObject<SwStage>(SwStageUtils.StageMetadataContent);
            SwStageSymbolsUtils.ApplyStage(stage);
        }
    }
}
#endif