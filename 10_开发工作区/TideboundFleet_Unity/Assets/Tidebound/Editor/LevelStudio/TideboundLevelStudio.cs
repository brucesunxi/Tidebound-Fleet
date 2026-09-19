using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tidebound.Board;
using Tidebound.Boss;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.LevelDesign;
using Tidebound.Ship;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tidebound.EditorTools
{
    public sealed class TideboundLevelStudio : EditorWindow
    {
        private readonly ShipDefinition[] shipCatalog =
            { new ShipDefinition(FoundationLimits.BaseShipTypeId, FoundationLimits.BaseShipDamage) };
        private readonly BossDefinition[] bossCatalog = { new BossDefinition("TF_KRAKEN_01") };
        private LevelData level;
        private TextAsset sourceAsset;
        private ShipDirection brushDirection = ShipDirection.Right;
        private int brushLength = 2;
        private SolutionProofRecorder playRecorder;
        private ObjectField sourceField;
        private TextField levelIdField;
        private IntegerField widthField;
        private IntegerField heightField;
        private TextField bossIdField;
        private EnumField directionField;
        private IntegerField lengthField;
        private Label statistics;
        private Label validation;
        private VisualElement grid;

        [MenuItem("Tools/Tidebound/Level Studio")]
        public static void Open() => GetWindow<TideboundLevelStudio>("Tidebound Level Studio");

        public void CreateGUI()
        {
            rootVisualElement.style.paddingLeft = 8;
            rootVisualElement.style.paddingRight = 8;
            rootVisualElement.style.paddingTop = 8;
            rootVisualElement.style.paddingBottom = 8;
            BuildToolbar();
            BuildSettings();
            statistics = new Label { style = { whiteSpace = WhiteSpace.Normal } };
            validation = new Label { style = { whiteSpace = WhiteSpace.Normal } };
            rootVisualElement.Add(statistics);
            rootVisualElement.Add(validation);
            var scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal) { style = { flexGrow = 1 } };
            grid = new VisualElement();
            scroll.Add(grid);
            rootVisualElement.Add(scroll);
            NewLevel();
        }

        private void BuildToolbar()
        {
            var bar = Row();
            sourceField = new ObjectField("JSON") { objectType = typeof(TextAsset), allowSceneObjects = false };
            sourceField.style.minWidth = 250;
            sourceField.RegisterValueChangedCallback(evt => sourceAsset = evt.newValue as TextAsset);
            bar.Add(sourceField);
            bar.Add(new Button(NewLevel) { text = "New" });
            bar.Add(new Button(LoadSelected) { text = "Open" });
            bar.Add(new Button(Save) { text = "Save" });
            bar.Add(new Button(SaveAs) { text = "Save As" });
            bar.Add(new Button(ValidateCurrent) { text = "Validate + Analyze" });
            rootVisualElement.Add(bar);

            var playBar = Row();
            playBar.Add(new Button(StartPlaytest) { text = "Start Playtest" });
            playBar.Add(new Button(ResetPlaytest) { text = "Reset Playtest" });
            playBar.Add(new Button(() => VerifyRecordedProof()) { text = "Verify Recorded Proof" });
            playBar.Add(new Button(SaveRecordedProof) { text = "Save Proof" });
            rootVisualElement.Add(playBar);
        }

        private void BuildSettings()
        {
            var settings = Row();
            levelIdField = new TextField("Level ID") { style = { width = 190 } };
            widthField = new IntegerField("Width") { style = { width = 100 } };
            heightField = new IntegerField("Height") { style = { width = 100 } };
            bossIdField = new TextField("Boss ID") { style = { width = 190 } };
            directionField = new EnumField("Direction", ShipDirection.Right) { style = { width = 170 } };
            lengthField = new IntegerField("Length") { value = 2, style = { width = 100 } };
            settings.Add(levelIdField);
            settings.Add(widthField);
            settings.Add(heightField);
            settings.Add(bossIdField);
            settings.Add(directionField);
            settings.Add(lengthField);
            rootVisualElement.Add(settings);

            levelIdField.RegisterValueChangedCallback(evt => { if (level != null) level.LevelId = evt.newValue; StopPlaytest(); RefreshStatus(); });
            bossIdField.RegisterValueChangedCallback(evt => { if (level != null) level.BossId = evt.newValue; StopPlaytest(); RefreshStatus(); });
            widthField.RegisterValueChangedCallback(evt => Resize(evt.newValue, level?.Height ?? 6));
            heightField.RegisterValueChangedCallback(evt => Resize(level?.Width ?? 4, evt.newValue));
            directionField.RegisterValueChangedCallback(evt => brushDirection = (ShipDirection)evt.newValue);
            lengthField.RegisterValueChangedCallback(evt =>
            {
                brushLength = Mathf.Clamp(evt.newValue, FoundationLimits.MinShipLength, FoundationLimits.MaxShipLength);
                if (lengthField.value != brushLength) lengthField.SetValueWithoutNotify(brushLength);
            });
        }

        private static VisualElement Row()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.marginBottom = 6;
            return row;
        }

        private void NewLevel()
        {
            sourceAsset = null;
            playRecorder = null;
            if (sourceField != null) sourceField.SetValueWithoutNotify(null);
            level = new LevelData
            {
                SchemaVersion = FoundationLimits.LevelSchemaVersion,
                LevelId = "Level_New",
                Width = 4,
                Height = 6,
                BossId = "TF_KRAKEN_01",
                Ships = Array.Empty<ShipPlacementData>()
            };
            SyncFields();
            RefreshGrid();
        }

        private void LoadSelected()
        {
            if (sourceAsset == null) { validation.text = "Select a level JSON TextAsset first."; return; }
            try
            {
                level = LevelJsonReader.Read(sourceAsset.text);
                playRecorder = null;
                SyncFields();
                RefreshGrid();
                validation.text = $"Loaded {AssetDatabase.GetAssetPath(sourceAsset)}";
            }
            catch (Exception e) { validation.text = e.Message; }
        }

        private void SyncFields()
        {
            levelIdField?.SetValueWithoutNotify(level.LevelId);
            widthField?.SetValueWithoutNotify(level.Width);
            heightField?.SetValueWithoutNotify(level.Height);
            bossIdField?.SetValueWithoutNotify(level.BossId);
        }

        private void Resize(int width, int height)
        {
            if (level == null) return;
            playRecorder = null;
            level.Width = Mathf.Clamp(width, 1, FoundationLimits.MaxTechnicalBoardWidth);
            level.Height = Mathf.Clamp(height, 1, FoundationLimits.MaxTechnicalBoardHeight);
            widthField.SetValueWithoutNotify(level.Width);
            heightField.SetValueWithoutNotify(level.Height);
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            if (grid == null || level == null) return;
            grid.Clear();
            var occupancy = Occupancy();
            for (var y = level.Height - 1; y >= 0; y--)
            {
                var row = Row();
                row.style.marginBottom = 0;
                for (var x = 0; x < level.Width; x++)
                {
                    var cell = new GridPosition(x, y);
                    var button = new Button { text = CellLabel(cell, occupancy), userData = cell };
                    button.style.width = 28;
                    button.style.height = 28;
                    button.style.marginLeft = 1;
                    button.style.marginRight = 1;
                    button.style.marginTop = 1;
                    button.style.marginBottom = 1;
                    button.RegisterCallback<MouseDownEvent>(OnCellMouseDown);
                    if (occupancy.ContainsKey(cell))
                        button.style.backgroundColor = playRecorder == null
                            ? new Color(0.20f, 0.62f, 0.82f)
                            : new Color(0.20f, 0.72f, 0.42f);
                    row.Add(button);
                }
                grid.Add(row);
            }
            RefreshStatus();
        }

        private void OnCellMouseDown(MouseDownEvent evt)
        {
            if (!(evt.currentTarget is VisualElement target) || !(target.userData is GridPosition cell)) return;
            var occupancy = Occupancy();
            if (playRecorder != null)
            {
                if (evt.button == 0 && occupancy.TryGetValue(cell, out var playable))
                {
                    if (playRecorder.TryApply(playable.Id, out var path))
                        validation.text = path.CanExit ? $"{playable.Id} exited." :
                            $"{playable.Id} moved {path.TravelDistance} cells and stopped before {path.BlockerShipId}.";
                    else validation.text = $"{playable.Id} is blocked with no legal travel.";
                    RefreshGrid();
                }
                evt.StopPropagation();
                return;
            }

            if (evt.button == 1)
            {
                if (occupancy.TryGetValue(cell, out var existing))
                    level.Ships = level.Ships.Where(x => !string.Equals(x.Id, existing.Id, StringComparison.Ordinal)).ToArray();
                RefreshGrid();
                evt.StopPropagation();
                return;
            }
            if (evt.button != 0 || occupancy.ContainsKey(cell)) return;
            var footprint = GridFootprint.Cells(cell, brushDirection, brushLength).ToArray();
            if (footprint.Any(x => x.X < 0 || x.X >= level.Width || x.Y < 0 || x.Y >= level.Height || occupancy.ContainsKey(x)))
            {
                validation.text = "The selected footprint is outside the board or overlaps another ship.";
                return;
            }
            var items = level.Ships.ToList();
            items.Add(new ShipPlacementData
            {
                Id = NextShipId(items), TypeId = FoundationLimits.BaseShipTypeId, Length = brushLength,
                Position = cell, Direction = brushDirection
            });
            level.Ships = items.ToArray();
            RefreshGrid();
        }

        private Dictionary<GridPosition, CellShip> Occupancy()
        {
            var result = new Dictionary<GridPosition, CellShip>();
            if (playRecorder != null)
            {
                foreach (var ship in playRecorder.CurrentBoard.Ships)
                {
                    var item = new CellShip(ship.Id, ship.Position, ship.Direction, ship.Length);
                    foreach (var cell in ship.OccupiedCells) result[cell] = item;
                }
                return result;
            }
            if (level?.Ships == null) return result;
            foreach (var ship in level.Ships)
            {
                if (ship == null || ship.Length < FoundationLimits.MinShipLength || ship.Length > FoundationLimits.MaxShipLength) continue;
                var item = new CellShip(ship.Id, ship.Position, ship.Direction, ship.Length);
                foreach (var cell in GridFootprint.Cells(ship.Position, ship.Direction, ship.Length))
                    if (!result.ContainsKey(cell)) result.Add(cell, item);
            }
            return result;
        }

        private static string CellLabel(GridPosition cell, IReadOnlyDictionary<GridPosition, CellShip> occupancy)
        {
            if (!occupancy.TryGetValue(cell, out var ship)) return string.Empty;
            if (!ship.Position.Equals(cell)) return "•";
            switch (ship.Direction)
            {
                case ShipDirection.Up: return "↑" + ship.Length;
                case ShipDirection.Down: return "↓" + ship.Length;
                case ShipDirection.Left: return "←" + ship.Length;
                default: return "→" + ship.Length;
            }
        }

        private static string NextShipId(IReadOnlyCollection<ShipPlacementData> items)
        {
            var used = new HashSet<string>(items.Where(x => x != null).Select(x => x.Id), StringComparer.Ordinal);
            for (var i = 1; i <= FoundationLimits.MaxTechnicalShipCount; i++)
            {
                var id = "S" + i.ToString("D3");
                if (!used.Contains(id)) return id;
            }
            return "S" + (items.Count + 1).ToString("D3");
        }

        private void RefreshStatus()
        {
            if (statistics == null || level == null) return;
            if (playRecorder != null)
            {
                var current = playRecorder.CurrentBoard;
                var report = LevelStructureAnalyzer.Analyze(current);
                statistics.text = $"PLAYTEST  Remaining {current.ShipCount}  Steps {playRecorder.ShipIds.Count}  " +
                                  $"Exits {report.Dependencies.InitialExitCount}  Moves {report.Dependencies.InitialMoveCount}  Cycles {report.Dependencies.Cycles.Count}";
                return;
            }
            var ships = level.Ships ?? Array.Empty<ShipPlacementData>();
            var occupied = ships.Where(x => x != null).Sum(x => Math.Max(0, x.Length));
            var cells = Math.Max(0, level.Width * level.Height);
            var empty = cells - occupied;
            var longCount = ships.Count(x => x != null && x.Length == 3);
            var rate = cells == 0 ? 0f : empty * 100f / cells;
            statistics.text = $"Ships {ships.Length}/{FoundationLimits.MaxTechnicalShipCount}  Long {longCount}  " +
                              $"Occupied {occupied}/{cells}  Empty {empty} ({rate:0.0}%)";
        }

        private ValidationResult ValidateData() => BoardValidator.Validate(level, shipCatalog, bossCatalog);

        private BoardModel CreateInitialBoard()
        {
            using (var session = LevelSessionFactory.Create(level, shipCatalog, bossCatalog)) return session.InitialBoard;
        }

        private void ValidateCurrent()
        {
            var data = ValidateData();
            if (!data.IsValid)
            {
                validation.text = string.Join(Environment.NewLine, data.Issues.Select(x => x.ToString()));
                return;
            }
            var board = CreateInitialBoard();
            var report = LevelStructureAnalyzer.Analyze(board);
            var summary = $"Data valid. Hash {BoardStateFingerprint.Compute(board)}  Ships {report.ShipCount}  " +
                          $"Occupancy {report.OccupancyRatio:P1}  Entropy {report.DirectionEntropy:0.000}  " +
                          $"Clustering {report.DirectionClustering:0.000}  Exits {report.Dependencies.InitialExitCount}  " +
                          $"Moves {report.Dependencies.InitialMoveCount}  Depth {report.Dependencies.DependencyDepth}  " +
                          $"Cycles {report.Dependencies.Cycles.Count} (hard {report.Dependencies.HardLockedCycleCount}).";
            if (LevelProductionProfiles.TryGetCandidate(board.Width, board.Height, out var profile))
            {
                var production = LevelProductionValidator.Validate(board, profile);
                summary += production.IsValid ? $" Production profile {profile.Id} passed." :
                    Environment.NewLine + string.Join(Environment.NewLine, production.Issues.Select(x => x.ToString()));
            }
            else summary += " No full-scale production profile is assigned to this size.";
            validation.text = summary;
        }

        private void StartPlaytest()
        {
            var data = ValidateData();
            if (!data.IsValid)
            {
                validation.text = string.Join(Environment.NewLine, data.Issues.Select(x => x.ToString()));
                return;
            }
            playRecorder = new SolutionProofRecorder(CreateInitialBoard());
            validation.text = "Playtest started. Click an occupied cell to execute the real board transaction.";
            RefreshGrid();
        }

        private void ResetPlaytest()
        {
            if (playRecorder == null) { StartPlaytest(); return; }
            playRecorder.Reset();
            validation.text = "Playtest reset to canonical JSON.";
            RefreshGrid();
        }

        private void StopPlaytest()
        {
            if (playRecorder == null) return;
            playRecorder = null;
            RefreshGrid();
        }

        private SolutionReplayResult VerifyRecordedProof()
        {
            if (playRecorder == null) { validation.text = "Start a playtest and record moves first."; return null; }
            var replay = SolutionProofReplay.Replay(CreateInitialBoard(), playRecorder.CreateProof(level.LevelId));
            validation.text = replay.IsComplete
                ? $"Proof passed: {replay.AppliedStepCount} real transactions cleared the board."
                : replay.IsValid ? $"Proof is valid but incomplete: {replay.FinalBoard.ShipCount} ships remain." : replay.Error;
            return replay;
        }

        private void SaveRecordedProof()
        {
            var replay = VerifyRecordedProof();
            if (replay == null || !replay.IsComplete) return;
            var proof = playRecorder.CreateProof(level.LevelId);
            var defaultName = string.IsNullOrWhiteSpace(level.LevelId) ? "Level.solution" : level.LevelId + ".solution";
            var path = EditorUtility.SaveFilePanelInProject("Save verified solution proof", defaultName, "json",
                "Save beside the canonical level JSON.", "Assets/Tidebound/Config/Levels");
            if (string.IsNullOrEmpty(path)) return;
            var file = new ProofFile { levelId = proof.LevelId, layoutFingerprint = proof.LayoutFingerprint, shipIds = proof.ShipIds.ToArray() };
            AtomicWrite(Path.GetFullPath(path), JsonUtility.ToJson(file, true) + Environment.NewLine);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            validation.text = "Verified proof saved to " + path;
        }

        private void Save()
        {
            var path = sourceAsset == null ? null : AssetDatabase.GetAssetPath(sourceAsset);
            if (string.IsNullOrEmpty(path)) { SaveAs(); return; }
            SaveTo(path);
        }

        private void SaveAs()
        {
            var defaultName = string.IsNullOrWhiteSpace(level?.LevelId) ? "Level_New" : level.LevelId;
            var path = EditorUtility.SaveFilePanelInProject("Save Tidebound level", defaultName, "json",
                "Save the canonical level JSON.", "Assets/Tidebound/Config/Levels");
            if (!string.IsNullOrEmpty(path)) SaveTo(path);
        }

        private void SaveTo(string path)
        {
            var result = ValidateData();
            if (!result.IsValid)
            {
                validation.text = string.Join(Environment.NewLine, result.Issues.Select(x => x.ToString()));
                return;
            }
            AtomicWrite(Path.GetFullPath(path), LevelJsonWriter.Write(level));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            sourceAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            sourceField.SetValueWithoutNotify(sourceAsset);
            playRecorder = null;
            validation.text = "Saved " + path;
            RefreshGrid();
        }

        private static void AtomicWrite(string path, string contents)
        {
            var temporary = path + ".writing-" + Guid.NewGuid().ToString("N");
            try
            {
                File.WriteAllText(temporary, contents);
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        private sealed class CellShip
        {
            public string Id { get; }
            public GridPosition Position { get; }
            public ShipDirection Direction { get; }
            public int Length { get; }
            public CellShip(string id, GridPosition position, ShipDirection direction, int length)
            { Id = id; Position = position; Direction = direction; Length = length; }
        }

        [Serializable]
        private sealed class ProofFile
        {
            public string levelId;
            public string layoutFingerprint;
            public string[] shipIds;
        }
    }
}
