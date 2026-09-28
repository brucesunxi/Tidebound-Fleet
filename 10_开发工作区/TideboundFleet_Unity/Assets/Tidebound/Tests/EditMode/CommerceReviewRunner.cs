using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Tidebound.Tests
{
    // Scoped editor-only verification entry; never included in the game or connected to a player save.
    [InitializeOnLoad]
    public sealed class CommerceReviewRunner : ICallbacks
    {
        private const string Key="Tidebound.Commerce.Tests";
        private static TestRunnerApi api;
        private static double pollAt;
        private static string Repo=>Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).Parent.FullName;
        private static string Work=>Path.Combine(Repo,"99_垃圾存储区/20260924_抽奖补给实施");
        private static string Output=>Path.Combine(Repo,"40_项目交接文档/验证记录/20260924_抽奖补给实施");
        static CommerceReviewRunner(){EditorApplication.delayCall+=Register;EditorApplication.update+=Poll;}
        private static void Register(){api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(new CommerceReviewRunner());}
        private static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying||api==null||EditorApplication.timeSinceStartup<pollAt)return;
            pollAt=EditorApplication.timeSinceStartup+1;var path=Path.Combine(Work,"test_request.txt");if(!File.Exists(path))return;
            var mode=File.ReadAllText(path).Trim();File.Delete(path);if(mode!="edit"&&mode!="play")return;
            Run(mode);
        }
        public static void Run(string mode)
        {
            if(mode!="edit"&&mode!="play")throw new ArgumentException("Unknown test mode",nameof(mode));
            if(api==null)Register();
            var pending=Path.Combine(Work,"test_request.txt");if(File.Exists(pending))File.Delete(pending);
            SessionState.SetString(Key,mode);Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Work,"test_status.txt"),"RUNNING "+mode);
            api.Execute(new ExecutionSettings(new Filter{testMode=mode=="edit"?TestMode.EditMode:TestMode.PlayMode,
                testNames=mode=="edit"?new[]{"Tidebound.Tests.UniqueDrawTests","Tidebound.Tests.CoinShopTests","Tidebound.Tests.AppearanceSaveTests","Tidebound.Tests.AppearanceDrawTests","Tidebound.Tests.PlayerSaveTests"}:
                    new[]{"Tidebound.Tests.CoinShopPlayModeTests"}}));
        }
        public void RunStarted(ITestAdaptor testsToRun){}
        public void TestStarted(ITestAdaptor test){}
        public void TestFinished(ITestResultAdaptor result){}
        public void RunFinished(ITestResultAdaptor result)
        {
            var mode=SessionState.GetString(Key,"");if(mode=="")return;
            Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Output,mode+"-results.xml"),result.ToXml().OuterXml);
            File.WriteAllText(Path.Combine(Work,"test_status.txt"),mode+": "+result.PassCount+" passed, "+result.FailCount+" failed, "+result.SkipCount+" skipped");
            SessionState.EraseString(Key);
        }
    }
}
