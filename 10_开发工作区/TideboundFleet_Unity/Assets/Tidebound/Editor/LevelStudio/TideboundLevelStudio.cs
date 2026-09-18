using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tidebound.Board;
using Tidebound.Boss;
using Tidebound.Config;
using Tidebound.Core;
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

        private LevelData level;
        private TextAsset sourceAsset;
        private ShipDirection brushDirection = ShipDirection.Right;
        private int brushLength = 2;

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
            statistics = new Label();
            validation = new Label();
            validation.style.whiteSpace = WhiteSpace.Normal;
            rootVisualElement.Add(statistics);
            rootVisualElement.Add(validation);
            var scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            scroll.style.flexGrow = 1;
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
            bar.Add(new Button(ValidateCurrent) { text = "Validate" });
            rootVisualElement.Add(bar);
        }

        private void BuildSettings()
        {
            var settings = Row();
            levelIdField = new TextField("Level ID");
            widthField = new IntegerField("Width");
            heightField = new IntegerField("Height");
            bossIdField = new TextField("Boss ID");
            directionField = new EnumField("Direction", ShipDirection.Right);
            lengthField = new IntegerField("Length") { value = 2 };
            levelIdField.style.width = 190;
            widthField.style.width = 100;
            heightField.style.width = 100;
            bossIdField.style.width = 190;
            directionField.style.width = 170;
            lengthField.style.width = 100;
            settings.Add(levelIdField);
            settings.Add(widthField);
            settings.Add(heightField);
            settings.Add(bossIdField);
            settings.Add(directionField);
            settings.Add(lengthField);
            rootVisualElement.Add(settings);

            levelIdField.RegisterValueChangedCallback(evt => { if (level != null) level.LevelId = evt.newValue; RefreshStatus(); });
            bossIdField.RegisterValueChangedCallback(evt => { if (level != null) level.BossId = evt.newValue; RefreshStatus(); });
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
            if (sourceAsset == null)
            {
                validation.text = "Select a level JSON TextAsset first.";
                return;
            }
            try
            {
                level = LevelJsonReader.Read(sourceAsset.text);
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
            level.Width = Mathf.Clamp(width, 1, FoundationLimits.MaxBoardWidth);
            level.Height = Mathf.Clamp(height, 1, FoundationLimits.MaxBoardHeight);
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
                    var button = new Button { text = CellLabel(cell, occupancy) };
                    button.style.width = 30;
                    button.style.height = 30;
                    button.style.marginLeft = 1;
                    button.style.marginRight = 1;
                    button.style.marginTop = 1;
                    button.style.marginBottom = 1;
                    button.userData = cell;
                    button.RegisterCallback<MouseDownEvent>(OnCellMouseDown);
                    if (occupancy.ContainsKey(cell)) button.style.backgroundColor = new Color(0.20f, 0.62f, 0.82f);
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
                Id = NextShipId(items),
                TypeId = FoundationLimits.BaseShipTypeId,
                Length = brushLength,
                Position = cell,
                Direction = brushDirection
            });
            level.Ships = items.ToArray();
            RefreshGrid();
        }

        private Dictionary<GridPosition, ShipPlacementData> Occupancy()
        {
            var result = new Dictionary<GridPosition, ShipPlacementData>();
            if (level?.Ships == null) return result;
            foreach (var ship in level.Ships)
            {
                if (ship == null || ship.Length < FoundationLimits.MinShipLength || ship.Length > FoundationLimits.MaxShipLength) continue;
                foreach (var cell in GridFootprint.Cells(ship.Position, ship.Direction, ship.Length))
                    if (!result.ContainsKey(cell)) result.Add(cell, ship);
            }
            return result;
        }

        private static string CellLabel(GridPosition cell, IReadOnlyDictionary<GridPosition, ShipPlacementData> occupancy)
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
            for (var i = 1; i <= FoundationLimits.MaxShipCount; i++)
            {
                var id = "S" + i.ToString("D3");
                if (!used.Contains(id)) return id;
            }
            return "S" + (items.Count + 1).ToString("D3");
        }

        private void RefreshStatus()
        {
            if (statistics == null || level == null) return;
            var ships = level.Ships ?? Array.Empty<ShipPlacementData>();
            var occupied = ships.Where(x => x != null).Sum(x => Math.Max(0, x.Length));
            var cells = Math.Max(0, level.Width * level.Height);
            var empty = cells - occupied;
            var longCount = ships.Count(x => x != null && x.Length == 3);
            var rate = cells == 0 ? 0f : empty * 100f / cells;
            statistics.text = $"Ships {ships.Length}/{FoundationLimits.MaxShipCount}  Long {longCount}/{FoundationLimits.MaxLongShipCount}  " +
                              $"Occupied {occupied}/{FoundationLimits.MaxOccupiedCellCount}  Empty {empty} ({rate:0.0}%)";
        }

        private ValidationResult ValidateData()
        {
            return BoardValidator.Validate(level, shipCatalog, new[] { new BossDefinition("TF_KRAKEN_01") });
        }

        private void ValidateCurrent()
        {
            var result = ValidateData();
            validation.text = result.IsValid ? "Validation passed. Solvability is not checked in V0." :
                string.Join(Environment.NewLine, result.Issues.Select(x => x.ToString()));
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
            File.WriteAllText(Path.GetFullPath(path), LevelJsonWriter.Write(level));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            sourceAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            sourceField.SetValueWithoutNotify(sourceAsset);
            validation.text = "Saved " + path;
        }
    }
}
