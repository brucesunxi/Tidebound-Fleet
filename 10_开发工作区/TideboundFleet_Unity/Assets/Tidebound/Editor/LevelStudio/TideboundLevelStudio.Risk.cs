using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Tidebound.Board;
using Tidebound.LevelDesign;
using Tidebound.Tools;
using UnityEditor;
using UnityEngine.UIElements;

namespace Tidebound.EditorTools
{
    public sealed partial class TideboundLevelStudio
    {
        private Label riskSummary;
        private VisualElement riskRows;
        private Task riskTask;
        private EditorApplication.CallbackFunction riskPoll;
        private LevelRiskReport riskReport;
        private ToolRecoveryReport recoveryReport;

        private void BuildRiskTools()
        {
            var fold = new Foldout { text = "错误分支与道具救回 / 仅编辑器证据", value = true };
            var buttons = Row();
            buttons.Add(new Button(BeginRiskAnalysis) { text = "检查每一步：有解 / 死局 / 未知" });
            buttons.Add(new Button(() => BeginRecoveryAnalysis(null)) { text = "检查当前局面的道具效果" });
            buttons.Add(new Button(ExportRiskReport) { text = "导出本状态证据" });
            fold.Add(buttons);
            riskSummary = new Label("先检查当前布局。错误分支比例不是玩家失败率；未知不能按死局计分。") { style = { whiteSpace = WhiteSpace.Normal } };
            fold.Add(riskSummary);
            var scroll = new ScrollView { style = { maxHeight = 190 } }; riskRows = new VisualElement(); scroll.Add(riskRows); fold.Add(scroll);
            rootVisualElement.Add(fold);
        }

        private void ClearRiskReport()
        {
            riskReport = null; recoveryReport = null; riskRows?.Clear();
            if (riskSummary != null) riskSummary.text = "布局或试玩状态已变化，请重新检查错误分支。未知不能按死局计分。";
        }

        private void BeginRiskAnalysis()
        {
            if (riskTask != null && !riskTask.IsCompleted) { riskSummary.text = "正在进行有预算限制的分析…"; return; }
            try
            {
                var board = playRecorder?.CurrentBoard ?? CreateInitialBoard();
                var local = LocalLayoutAnalyzer.Analyze(board);
                var generation = analysisGeneration;
                var continuation = difficultyReport?.HasVerifiedSolution == true ? difficultyReport.Solution.ToArray() :
                    playTimeline.Skip(playRecorder?.ShipIds.Count ?? 0).ToArray();
                try { LevelSolutionProof.Create("RiskContinuation", board, continuation); }
                catch (Exception e) when (e is ArgumentException || e is KeyNotFoundException) { continuation = null; }
                var task = Task.Run(() => LevelRiskAnalysis.Analyze(board, verifiedContinuation: continuation)); riskTask = task;
                riskSummary.text = "正在逐一尝试有效操作并求解后续状态…";
                PollRisk(task, generation, () =>
                {
                    riskReport = task.Result; recoveryReport = null; riskRows.Clear();
                    riskSummary.text = $"本状态：仍有解 {riskReport.SafeMoves}；已证明死局 {riskReport.DeadlockMoves}；未知 {riskReport.UnknownMoves}；零位移 {riskReport.ZeroDistanceMoves}。\n" +
                        $"同向连续（含1格间隔）横 {local.LongestGappedRow?.ShipCount ?? 0} / 纵 {local.LongestGappedColumn?.ShipCount ?? 0}；最大空矩形 {local.LargestEmptyArea} 格。\n" +
                        "这里只检查当前一步的所有分支；没有危险首步不代表后续没有陷阱。没有未知才可完整比较本状态分支比例。";
                    foreach (var move in riskReport.Moves)
                    {
                        var row = Row();
                        var state = move.Status == BranchSolvability.Solvable ? "仍有解" : move.Status == BranchSolvability.Deadlocked ? "死局已证明" : "未知";
                        row.Add(new Label($"{move.ShipId} / {move.Outcome}: {state}") { style = { minWidth = 270 } });
                        row.Add(new Button(() => PreviewRiskMove(board, move)) { text = "执行并查看" });
                        if (move.Status == BranchSolvability.Deadlocked)
                            row.Add(new Button(() => BeginRecoveryAnalysis(board.ApplyPathResult(board.QueryForwardPath(move.ShipId)))) { text = "检验道具救回" });
                        riskRows.Add(row);
                    }
                });
            }
            catch (Exception e) { riskSummary.text = e.Message; }
        }

        private void PreviewRiskMove(BoardModel board, MoveRiskEvidence move)
        {
            var current = playRecorder?.CurrentBoard ?? CreateInitialBoard();
            if (LevelStateIdentity.Fingerprint(current) != LevelStateIdentity.Fingerprint(board)) { ClearRiskReport(); return; }
            if (playRecorder == null) playRecorder = new SolutionProofRecorder(board);
            ApplyPlaytestMove(move.ShipId, true);
        }

        private void BeginRecoveryAnalysis(BoardModel selected)
        {
            if (riskTask != null && !riskTask.IsCompleted) { riskSummary.text = "正在进行有预算限制的分析…"; return; }
            try
            {
                var board = selected ?? playRecorder?.CurrentBoard ?? CreateInitialBoard();
                var generation = analysisGeneration;
                var task = Task.Run(() => LevelRiskAnalysis.AnalyzeRecovery(board)); riskTask = task;
                riskSummary.text = "正在模拟反向、随机救援的候选结果及洗牌；不扣库存，不改当前棋盘…";
                PollRisk(task, generation, () =>
                {
                    recoveryReport = task.Result; riskRows.Clear();
                    var rescue = recoveryReport.Cases.Where(c => c.Tool == ShipTool.Rescue).ToArray();
                    riskSummary.text = $"救回对象状态 {recoveryReport.StateFingerprint.Substring(0, 12)}；存在已证明的一次道具救回：{recoveryReport.HasVerifiedOneToolRecovery}。\n" +
                        $"救援枚举 {rescue.Length}/{recoveryReport.PossibleRescuePairs} 对，全部随机结果均验证可解：{recoveryReport.AllRescueOutcomesVerified}。\n" +
                        "下列仅列出已证明可解的候选；未验证候选不保证有效。发现一组有效救援不等于随机救援保证成功。";
                    foreach (var c in recoveryReport.Cases.Where(c => c.Status == BranchSolvability.Solvable))
                        riskRows.Add(new Label($"{c.Tool}: {string.Join(",", c.Targets)}{(c.Seed.HasValue ? " / seed " + c.Seed : "")}；后续 {c.Solution.Count} 步有解"));
                });
            }
            catch (Exception e) { riskSummary.text = e.Message; }
        }

        private void PollRisk(Task task, int generation, Action completed)
        {
            if (riskPoll != null) EditorApplication.update -= riskPoll;
            riskPoll = () =>
            {
                if (!task.IsCompleted) return;
                EditorApplication.update -= riskPoll; riskPoll = null;
                if (this == null || generation != analysisGeneration) return;
                if (task.IsFaulted) { riskSummary.text = task.Exception.GetBaseException().Message; return; }
                completed(); Repaint();
            };
            EditorApplication.update += riskPoll;
        }

        private void ExportRiskReport()
        {
            if (riskReport == null && recoveryReport == null) { riskSummary.text = "先完成一次分析。"; return; }
            var path = EditorUtility.SaveFilePanel("Export risk evidence", "", level.LevelId + ".risk.json", "json");
            if (string.IsNullOrEmpty(path)) return;
            if (IsPublishedPath(path)) { riskSummary.text = "评审包／正式关卡目录受保护，请导出到验证记录目录。"; return; }
            File.WriteAllText(path, JsonConvert.SerializeObject(new { version = LevelRiskAnalysis.Version, rules = LevelRules.Version,
                levelId = level.LevelId, scope = "Current state / bounded one-step branch analysis; not human failure probability", risk = riskReport, recovery = recoveryReport }, Formatting.Indented));
            riskSummary.text += "\n证据已导出。";
        }
    }
}
