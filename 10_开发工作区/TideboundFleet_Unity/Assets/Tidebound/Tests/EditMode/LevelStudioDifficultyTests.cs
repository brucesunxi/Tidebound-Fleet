using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using Tidebound.Config;
using Tidebound.LevelDesign;
using UnityEditor;
using UnityEngine;

namespace Tidebound.Tests
{
    public sealed class LevelStudioDifficultyTests
    {
        private EditorWindow window;
        private Type type;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private object Read(string name) => type.GetField(name, Private).GetValue(window);
        private object Call(string name, params object[] args) => type.GetMethod(name, Private).Invoke(window, args);

        [SetUp]
        public void CreateDraftWindow()
        {
            type = Type.GetType("Tidebound.EditorTools.TideboundLevelStudio, Tidebound.Editor", true);
            window = (EditorWindow)ScriptableObject.CreateInstance(type);
            type.GetMethod("CreateGUI").Invoke(window, null);
            var path = Path.Combine(Application.dataPath, "Tidebound/Config/LevelPrototypes/DifficultyV2/DV2_P4.json");
            type.GetField("level", Private).SetValue(window, LevelJsonReader.Read(File.ReadAllText(path)));
            Call("ResetDocumentHistory"); Call("SyncFields"); Call("RefreshGrid");
        }

        [TearDown]
        public void DisposeWindow()
        {
            if (window == null) return;
            typeof(EditorWindow).GetProperty("hasUnsavedChanges").GetSetMethod(true).Invoke(window, new object[] { false });
            UnityEngine.Object.DestroyImmediate(window);
        }

        [Test]
        public void UndoRedoRestoresDraftAndMarksUnchangedDraftClean()
        {
            var old = LevelJsonWriter.Write((LevelData)Read("level"));
            Call("BeforeEdit"); ((LevelData)Read("level")).LevelId = "edited"; Call("RefreshStatus");
            Assert.That(window.hasUnsavedChanges, Is.True);
            Call("RestoreDraft", Read("undoDrafts"), Read("redoDrafts"));
            Assert.That(LevelJsonWriter.Write((LevelData)Read("level")), Is.EqualTo(old));
            Assert.That(window.hasUnsavedChanges, Is.False);
            Call("RestoreDraft", Read("redoDrafts"), Read("undoDrafts"));
            Assert.That(((LevelData)Read("level")).LevelId, Is.EqualTo("edited"));
            Assert.That(window.hasUnsavedChanges, Is.True);
        }

        [Test]
        public void PlaytestBackForwardAndBranchKeepPartialMoveHistoryCorrect()
        {
            Call("StartPlaytest"); var initial = LevelStateIdentity.Fingerprint(((SolutionProofRecorder)Read("playRecorder")).CurrentBoard);
            Call("ApplyPlaytestMove", "A", true);
            var moved = LevelStateIdentity.Fingerprint(((SolutionProofRecorder)Read("playRecorder")).CurrentBoard);
            Assert.That(moved, Is.Not.EqualTo(initial));
            Assert.That(((SolutionProofRecorder)Read("playRecorder")).CurrentBoard.ShipCount, Is.EqualTo(4));
            Call("BackPlaytest"); Assert.That(LevelStateIdentity.Fingerprint(((SolutionProofRecorder)Read("playRecorder")).CurrentBoard), Is.EqualTo(initial));
            Call("ForwardPlaytest"); Assert.That(LevelStateIdentity.Fingerprint(((SolutionProofRecorder)Read("playRecorder")).CurrentBoard), Is.EqualTo(moved));
            Call("BackPlaytest"); Call("ApplyPlaytestMove", "D", true);
            CollectionAssert.AreEqual(new[] { "D" }, (List<string>)Read("playTimeline"));
            Assert.That(window.hasUnsavedChanges, Is.False, "Playing must not edit the initial document.");
        }

        [Test]
        public void SavedDynamicProofQueuesRepeatedShipsAndIsInvalidatedAfterMoving()
        {
            type.GetField("sourceAsset", Private).SetValue(window,
                AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Tidebound/Config/LevelPrototypes/DifficultyV2/DV2_P4.json"));
            Call("LoadSidecarProof"); var report = (LevelDifficultyReport)Read("difficultyReport");
            Assert.That(report.Kind, Is.EqualTo(PuzzleSolutionKind.RequiresPartial));
            Assert.That(report.IsMinimumExact, Is.True);
            Assert.That(((List<string>)Read("playTimeline")).Count, Is.EqualTo(5));
            Call("ForwardPlaytest");
            Assert.That(((SolutionProofRecorder)Read("playRecorder")).CurrentBoard.ShipCount, Is.EqualTo(4));
            Assert.That(Read("difficultyReport"), Is.Null, "A report for the old state must not survive a move.");
        }

        [Test]
        public void AnEditInvalidatesAnInFlightAnalysis()
        {
            Call("BeginDifficultyAnalysis", false); var task = (Task)Read("analysisTask");
            Call("BeforeEdit"); ((LevelData)Read("level")).LevelId = "new";
            Assert.That(task.Wait(4000), Is.True); ((Delegate)Read("analysisPoll")).DynamicInvoke();
            Assert.That(Read("difficultyReport"), Is.Null);
        }

        [Test]
        public void DraftEditsInvalidateRiskAnalysisAndCannotPaintAnOldResult()
        {
            Call("BeginRiskAnalysis"); var task = (Task)Read("riskTask");
            Call("BeforeEdit"); ((LevelData)Read("level")).LevelId = "changed";
            Assert.That(task.Wait(5000), Is.True); ((Delegate)Read("riskPoll")).DynamicInvoke();
            Assert.That(Read("riskReport"), Is.Null);
        }

        [Test]
        public void PublishedLayoutsAndProofsAreProtectedIncludingNormalizedPaths()
        {
            var method = type.GetMethod("IsPublishedPath", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method.Invoke(null, new object[] { "Assets/Tidebound/Config/Levels/Campaign/TF_Campaign_024.json" }), Is.True);
            Assert.That(method.Invoke(null, new object[] { "Assets/Tidebound/Config/Levels/Campaign/../Campaign/TF_Campaign_024.solution.json" }), Is.True);
            Assert.That(method.Invoke(null, new object[] { "Assets/Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates/P5R_Ten_001.json" }), Is.True);
            Assert.That(method.Invoke(null, new object[] { "Assets/Tidebound/Config/LevelPrototypes/MyDraft.json" }), Is.False);
        }

        [Test]
        public void BattleBudgetUsesInitialFleetAndRefreshesAfterDraftCountChanges()
        {
            type.GetField("level", Private).SetValue(window, VariableFleetCombatTests.Layout(90));
            Call("ResetDocumentHistory"); Call("SyncFields"); Call("RefreshGrid");
            var label = (UnityEngine.UIElements.Label)Read("battleBudgetSummary");
            Assert.That(label.text, Does.Contain("初始 90 艘").And.Contain("海怪总血量 900"));
            Call("StartPlaytest"); Call("ApplyPlaytestMove", "S089", true);
            Assert.That(label.text, Does.Contain("海怪总血量 900").And.Contain("试玩剩余 89 艘"));
            Call("StopPlaytest"); Call("BeforeEdit");
            type.GetField("level", Private).SetValue(window, VariableFleetCombatTests.Layout(92, 8));
            Call("RefreshGrid");
            Assert.That(label.text, Does.Contain("初始 92 艘").And.Contain("海怪总血量 920"));
            ((LevelData)Read("level")).Ships[0].Length = 99; Call("RefreshStatus");
            Assert.That(label.text, Does.Contain("未通过数据校验").And.Not.Contain("920"));
        }
    }
}
