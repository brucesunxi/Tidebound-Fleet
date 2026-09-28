using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tidebound.Board;
using Tidebound.Config;
using Tidebound.LevelDesign;
using Tidebound.Ship;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tidebound.EditorTools
{
    public sealed partial class TideboundLevelStudio
    {
        [SerializeField] private string persistedDraft;
        [SerializeField] private string savedDraft;
        [SerializeField] private List<string> undoDrafts = new List<string>();
        [SerializeField] private List<string> redoDrafts = new List<string>();
        private readonly List<string> playTimeline = new List<string>();
        private readonly HashSet<GridPosition> selectionCells = new HashSet<GridPosition>();
        private readonly HashSet<GridPosition> pathCells = new HashSet<GridPosition>();
        private readonly HashSet<GridPosition> targetCells = new HashSet<GridPosition>();
        private GridPosition? blockingCell;
        private string selectedShipId;
        private Toggle placementMode;
        private IntegerField selectedX, selectedY, selectedLength;
        private EnumField selectedDirection;
        private Label inspection, difficultySummary, battleBudgetSummary;
        private VisualElement traceView;
        private LevelDifficultyReport difficultyReport;
        private int analysisGeneration;
        private Task<LevelDifficultyReport> analysisTask;
        private EditorApplication.CallbackFunction analysisPoll;

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(persistedDraft))
                try { level = LevelJsonReader.Read(persistedDraft); }
                catch (Exception e) { Debug.LogWarning("Level Studio draft restore: " + e.Message); }
        }

        private void OnDisable()
        {
            if (level != null) persistedDraft = LevelJsonWriter.Write(level);
            InvalidateDifficulty();
            if (analysisPoll != null) EditorApplication.update -= analysisPoll;
            analysisPoll = null;
            if (riskPoll != null) EditorApplication.update -= riskPoll;
            riskPoll = null;
        }

        public override void SaveChanges()
        {
            Save();
            if (!hasUnsavedChanges) base.SaveChanges();
        }

        private void BuildDifficultyTools()
        {
            var bar = Row();
            placementMode = new Toggle("Place on empty cells") { value = false };
            bar.Add(placementMode);
            bar.Add(new Button(() => RestoreDraft(undoDrafts, redoDrafts)) { text = "Undo Edit" });
            bar.Add(new Button(() => RestoreDraft(redoDrafts, undoDrafts)) { text = "Redo Edit" });
            bar.Add(new Button(StopPlaytest) { text = "Exit Playtest" });
            bar.Add(new Button(BackPlaytest) { text = "◀ Back" });
            bar.Add(new Button(ForwardPlaytest) { text = "Forward ▶" });
            rootVisualElement.Add(bar);
            var selected = new Foldout { text = "Selected ship / path (cyan: ship, yellow: path, green: destination, red: blocker)", value = true };
            inspection = new Label { style = { whiteSpace = WhiteSpace.Normal } };
            selected.Add(inspection);
            var fields = Row();
            selectedX = new IntegerField("X") { style = { width = 95 } };
            selectedY = new IntegerField("Y") { style = { width = 95 } };
            selectedLength = new IntegerField("Length") { style = { width = 110 } };
            selectedDirection = new EnumField("Direction", ShipDirection.Up) { style = { width = 180 } };
            fields.Add(selectedX); fields.Add(selectedY); fields.Add(selectedLength); fields.Add(selectedDirection);
            fields.Add(new Button(ApplySelectedEdit) { text = "Apply Selection" });
            selected.Add(fields); rootVisualElement.Add(selected);
            var analysis = new Foldout { text = "Difficulty v2 / verified release trace", value = true };
            var actions = Row();
            actions.Add(new Button(() => BeginDifficultyAnalysis(false)) { text = "Analyze A/B + shortest" });
            actions.Add(new Button(() => BeginDifficultyAnalysis(true)) { text = "Find feasible witness (large B)" });
            actions.Add(new Button(QueueVerifiedSolution) { text = "Queue verified route" });
            actions.Add(new Button(LoadSidecarProof) { text = "Load saved proof" });
            analysis.Add(actions);
            difficultySummary = new Label { style = { whiteSpace = WhiteSpace.Normal } };
            analysis.Add(difficultySummary);
            var traceFold = new Foldout { text = "Release curve / steps", value = false };
            var scroll = new ScrollView { style = { maxHeight = 160 } };
            traceView = new VisualElement(); scroll.Add(traceView); traceFold.Add(scroll); analysis.Add(traceFold);
            rootVisualElement.Add(analysis);
            var battle = new Foldout { text = "船数与海怪血量 / 初始战斗预算", value = true };
            battleBudgetSummary = new Label { style = { whiteSpace = WhiteSpace.Normal } };
            battle.Add(battleBudgetSummary);
            rootVisualElement.Add(battle);
            BuildRiskTools();
        }

        private void RefreshBattleBudget()
        {
            if (battleBudgetSummary == null) return;
            if (level?.Ships == null) { battleBudgetSummary.text = "尚未加载关卡。"; return; }
            var result = ValidateData();
            if (!result.IsValid)
            { battleBudgetSummary.text = "布局未通过数据校验，暂不显示战斗预算；请先修复校验问题。"; return; }
            // Use the same session factory as gameplay; never store a second editable HP value.
            using (var session = Tidebound.Core.LevelSessionFactory.Create(level, shipCatalog, bossCatalog))
            {
                battleBudgetSummary.text = $"初始 {session.Ships.Count} 艘（长船 {session.Ships.Count(s => s.Length == 3)} 艘）" +
                    $" → {session.Ships.Count} 次攻击 → 海怪总血量 {session.Boss.InitialHp}。" +
                    "长短船均为每船一次攻击、每次10点伤害；部分前进不攻击，救援出船仍计入攻击。" +
                    (playRecorder == null ? "" : $" 棋盘试玩剩余 {playRecorder.CurrentBoard.ShipCount} 艘；此处仍显示初始预算，实际剩余HP由局内命中账本决定。");
            }
        }

        private void BeforeEdit()
        {
            undoDrafts.Add(LevelJsonWriter.Write(level));
            if (undoDrafts.Count > 100) undoDrafts.RemoveAt(0);
            redoDrafts.Clear(); playRecorder = null; playTimeline.Clear(); InvalidateDifficulty();
        }

        private void RestoreDraft(List<string> from, List<string> to)
        {
            if (from.Count == 0 || level == null) return;
            var draft = LevelJsonReader.Read(from[from.Count - 1]);
            to.Add(LevelJsonWriter.Write(level)); from.RemoveAt(from.Count - 1);
            level = draft; playRecorder = null; playTimeline.Clear(); InvalidateDifficulty();
            SyncFields(); RefreshGrid();
        }

        private void ResetDocumentHistory()
        {
            undoDrafts.Clear(); redoDrafts.Clear(); playTimeline.Clear(); selectedShipId = null;
            savedDraft = LevelJsonWriter.Write(level); persistedDraft = savedDraft; InvalidateDifficulty();
        }

        private void RefreshDirtyState()
        {
            if (level == null) return;
            persistedDraft = LevelJsonWriter.Write(level);
            hasUnsavedChanges = persistedDraft != savedDraft;
            saveChangesMessage = "The level draft has unsaved edits. Save a separate revision before closing.";
        }

        private bool ConfirmDiscard() => level == null || LevelJsonWriter.Write(level) == savedDraft ||
            EditorUtility.DisplayDialog("Unsaved level draft", "Discard the current unsaved edits?", "Discard", "Cancel");

        private static bool IsPublishedPath(string path)
        {
            var full = Path.GetFullPath(path).Replace('\\', '/');
            if (full.StartsWith(Path.GetFullPath("Assets/Tidebound/Config/Levels/CampaignV3").Replace('\\', '/') + "/", StringComparison.Ordinal)) return true;
            return full.StartsWith(Path.GetFullPath("Assets/Tidebound/Config/Levels/Campaign").Replace('\\', '/') + "/", StringComparison.Ordinal) ||
                   full.StartsWith(Path.GetFullPath("Assets/Tidebound/Config/LevelPrototypes/DifficultyV2").Replace('\\', '/') + "/", StringComparison.Ordinal) ||
                   full.StartsWith(Path.GetFullPath("Assets/Tidebound/Config/LevelPrototypes/Phase5R_TenLevelCandidates").Replace('\\', '/') + "/", StringComparison.Ordinal);
        }

        private void ApplySelectedEdit()
        {
            if (playRecorder != null) { validation.text = "Exit playtest before editing the initial layout."; return; }
            if (level?.Ships == null || !level.Ships.Any(s => s.Id == selectedShipId)) return;
            var proposed = LevelJsonReader.Read(LevelJsonWriter.Write(level));
            var ship = proposed.Ships.First(s => s.Id == selectedShipId);
            ship.Position = new GridPosition(selectedX.value, selectedY.value);
            ship.Length = selectedLength.value; ship.Direction = (ShipDirection)selectedDirection.value;
            var result = BoardValidator.Validate(proposed, shipCatalog, bossCatalog);
            if (!result.IsValid) { validation.text = string.Join("\n", result.Issues.Select(i => i.ToString())); return; }
            BeforeEdit(); level = proposed; RefreshGrid();
        }

        private void PrepareSelectionOverlay()
        {
            selectionCells.Clear(); pathCells.Clear(); targetCells.Clear(); blockingCell = null;
            if (string.IsNullOrEmpty(selectedShipId)) return;
            try
            {
                var board = playRecorder?.CurrentBoard ?? CreateInitialBoard();
                if (!board.TryGetShip(selectedShipId, out var ship)) return;
                selectionCells.UnionWith(ship.OccupiedCells);
                var path = board.QueryForwardPath(ship.Id); pathCells.UnionWith(path.ClearCells); blockingCell = path.BlockerCell;
                if (path.IsBlocked && path.TravelDistance > 0)
                    targetCells.UnionWith(GridFootprint.Cells(path.TargetTail, ship.Direction, ship.Length));
            }
            catch (Exception) { /* Invalid drafts remain editable. Validate reports the exact issue. */ }
        }

        private void PaintDiagnosticCell(Button button, GridPosition cell)
        {
            if (pathCells.Contains(cell)) button.style.backgroundColor = new Color(.75f, .64f, .15f);
            if (selectionCells.Contains(cell)) button.style.backgroundColor = new Color(.05f, .8f, .92f);
            if (targetCells.Contains(cell)) button.style.backgroundColor = new Color(.2f, .8f, .35f);
            if (blockingCell.HasValue && blockingCell.Value.Equals(cell)) button.style.backgroundColor = new Color(.85f, .25f, .2f);
        }

        private void RefreshInspection()
        {
            if (inspection == null) return;
            inspection.text = "Click a ship to inspect. Right click deletes in edit mode. Playtest supports history replay.";
            try
            {
                var board = playRecorder?.CurrentBoard ?? CreateInitialBoard();
                if (string.IsNullOrEmpty(selectedShipId) || !board.TryGetShip(selectedShipId, out var ship)) return;
                var path = board.QueryForwardPath(ship.Id);
                selectedX.SetValueWithoutNotify(ship.Position.X); selectedY.SetValueWithoutNotify(ship.Position.Y);
                selectedLength.SetValueWithoutNotify(ship.Length); selectedDirection.SetValueWithoutNotify(ship.Direction);
                var after = board.ApplyPathResult(path);
                var freed = ship.OccupiedCells.Count(c => !after.IsOccupied(c));
                inspection.text = $"{ship.Id}: {path.Outcome}, distance {path.TravelDistance}, tail {path.OriginTail} → {path.TargetTail}; " +
                    $"blocker {path.BlockerShipId ?? "none"}; freed cells {freed}. " +
                    (playRecorder == null ? "EDIT INITIAL LAYOUT" : $"PLAYTEST {playRecorder.ShipIds.Count}/{playTimeline.Count}");
            }
            catch (Exception e) { inspection.text = e.Message; }
        }

        private void ApplyPlaytestMove(string id, bool branch)
        {
            if (playRecorder == null) return;
            var cursor = playRecorder.ShipIds.Count;
            if (!playRecorder.TryApply(id, out var path)) { validation.text = "Zero-distance block: history unchanged."; RefreshGrid(); return; }
            if (branch) { playTimeline.RemoveRange(cursor, playTimeline.Count - cursor); playTimeline.Add(id); }
            InvalidateDifficulty();
            validation.text = $"{id}: {path.Outcome}, distance {path.TravelDistance}."; RefreshGrid();
        }

        private void BackPlaytest()
        {
            if (playRecorder == null || playRecorder.ShipIds.Count == 0) return;
            var cursor = playRecorder.ShipIds.Count - 1;
            playRecorder = new SolutionProofRecorder(CreateInitialBoard());
            for (var i = 0; i < cursor; i++) playRecorder.TryApply(playTimeline[i], out _);
            InvalidateDifficulty(); RefreshGrid();
        }

        private void ForwardPlaytest()
        {
            if (playRecorder != null && playRecorder.ShipIds.Count < playTimeline.Count)
                ApplyPlaytestMove(playTimeline[playRecorder.ShipIds.Count], false);
        }

        private void InvalidateDifficulty()
        {
            analysisGeneration++; difficultyReport = null;
            ClearRiskReport();
            if (difficultySummary != null) difficultySummary.text = "Analysis required for this state. A search limit means unknown, not a deadlock.";
            traceView?.Clear();
        }

        private void BeginDifficultyAnalysis(bool witness)
        {
            if (analysisTask != null && !analysisTask.IsCompleted) { difficultySummary.text = "Analysis is still running (bounded to 2 seconds)."; return; }
            try
            {
                var board = playRecorder?.CurrentBoard ?? CreateInitialBoard();
                var generation = analysisGeneration;
                var options = new LevelSolverOptions(timeLimitMilliseconds: 2000);
                analysisTask = Task.Run(() => witness ? LevelDifficultyAnalysis.FindWitness(board, options) : LevelDifficultyAnalysis.Analyze(board, options));
                difficultySummary.text = "Analyzing immutable board snapshot…";
                if (analysisPoll != null) EditorApplication.update -= analysisPoll;
                analysisPoll = () =>
                {
                    if (!analysisTask.IsCompleted) return;
                    EditorApplication.update -= analysisPoll; analysisPoll = null;
                    if (generation != analysisGeneration || this == null) return;
                    if (analysisTask.IsFaulted) { difficultySummary.text = analysisTask.Exception.GetBaseException().Message; return; }
                    difficultyReport = analysisTask.Result;
                    var r = difficultyReport;
                    var kind = r.Kind == PuzzleSolutionKind.DirectExit ? "A / pure exit" : r.Kind == PuzzleSolutionKind.RequiresPartial ? "B / partial required" : r.Kind.ToString();
                    difficultySummary.text = $"{kind}; {r.Method}; {r.Solver.Status}; states {r.Solver.VisitedStateCount}. " +
                        $"Necessary partial: {r.MinimumPartialLowerBound?.ToString() ?? "?"}–{r.MinimumPartialUpperBound?.ToString() ?? "?"}" +
                        (r.IsMinimumExact ? " (proven)" : " (bounds only)") + $". Pure-exit residual {r.Peeling.RemainingBoard.ShipCount}.";
                    DrawTrace(r.Trace); Repaint();
                };
                EditorApplication.update += analysisPoll;
            }
            catch (Exception e) { difficultySummary.text = e.Message; }
        }

        private void DrawTrace(IReadOnlyList<ReleaseTraceStep> trace)
        {
            traceView.Clear();
            for (var i = 0; i < trace.Count; i++)
            {
                var step = trace[i]; var row = Row(); row.style.marginBottom = 1;
                row.Add(new Label($"{i + 1,3} {step.ShipId} {step.Outcome}: R={step.RemainingBefore} E={step.ExitsBefore} P={step.PartialBefore} E/R={step.ExitShare:P0} +{step.NewExits}/−{step.LostExits}") { style = { minWidth = 510 } });
                row.Add(new VisualElement { style = { width = step.ExitsBefore * 5, height = 12, backgroundColor = new Color(.2f, .65f, .8f) } });
                traceView.Add(row);
            }
        }

        private void QueueVerifiedSolution()
        {
            if (difficultyReport == null || !difficultyReport.HasVerifiedSolution) { difficultySummary.text = "Find a verified route first."; return; }
            var report = difficultyReport;
            var board = playRecorder?.CurrentBoard ?? CreateInitialBoard();
            if (LevelStateIdentity.Fingerprint(board) != report.StateFingerprint) { InvalidateDifficulty(); return; }
            if (playRecorder == null) playRecorder = new SolutionProofRecorder(board);
            var cursor = playRecorder.ShipIds.Count;
            playTimeline.RemoveRange(cursor, playTimeline.Count - cursor); playTimeline.AddRange(report.Solution);
            validation.text = "Verified route queued. Use Forward / Back; a manual move branches the timeline."; RefreshGrid();
        }

        private void LoadSidecarProof()
        {
            try
            {
                if (sourceAsset == null) throw new InvalidOperationException("Open a level asset first.");
                var path = Path.ChangeExtension(AssetDatabase.GetAssetPath(sourceAsset), "solution.json");
                var proof = LevelProofJson.Read(File.ReadAllText(path));
                var board = CreateInitialBoard(); var replay = proof.Replay(level.LevelId, board);
                if (!replay.IsComplete) throw new InvalidOperationException(replay.Error);
                playRecorder = new SolutionProofRecorder(board); playTimeline.Clear();
                playTimeline.AddRange(proof.Steps.Select(s => s.ShipId)); InvalidateDifficulty();
                difficultyReport = LevelDifficultyAnalysis.FromProof(board, level.LevelId, proof);
                DrawTrace(difficultyReport.Trace);
                difficultySummary.text = $"{difficultyReport.Kind}; saved proof verified: {playTimeline.Count} moves. Necessary partial " +
                    $"{difficultyReport.MinimumPartialLowerBound}–{difficultyReport.MinimumPartialUpperBound}" +
                    (difficultyReport.IsMinimumExact ? " (proven by matching bounds)." : " (bounds only).") + " Use Forward / Back.";
                RefreshGrid();
            }
            catch (Exception e) { validation.text = e.Message; }
        }
    }
}
