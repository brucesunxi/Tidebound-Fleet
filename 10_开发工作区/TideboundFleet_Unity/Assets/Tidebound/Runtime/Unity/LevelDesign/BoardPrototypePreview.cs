using System;
using System.Collections.Generic;
using System.Linq;
using Tidebound.Board;
using Tidebound.Boss;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.Ship;
using UnityEngine;

namespace Tidebound.Unity.LevelDesign
{
    /// <summary>
    /// Instrumented graybox for candidate grids. It reads canonical JSON and mirrors BoardModel;
    /// no Scene, prefab, Transform, Renderer, or Collider becomes a layout source.
    /// </summary>
    public sealed class BoardPrototypePreview : MonoBehaviour
    {
        private readonly Dictionary<string, PrototypeShipView> views =
            new Dictionary<string, PrototypeShipView>(StringComparer.Ordinal);
        private GameSession session;
        private BoardModel board;

        public BoardModel Board => board;
        public int ShipViewCount => views.Count;
        public IReadOnlyCollection<PrototypeShipView> ShipViews => views.Values;

        public void Build(TextAsset canonicalLevelJson)
        {
            if (canonicalLevelJson == null) throw new ArgumentNullException(nameof(canonicalLevelJson));
            Build(LevelJsonReader.Read(canonicalLevelJson.text));
        }

        public void Build(LevelData level)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            Clear();
            session = LevelSessionFactory.Create(level,
                new[] { new ShipDefinition(FoundationLimits.BaseShipTypeId, FoundationLimits.BaseShipDamage) },
                new[] { new BossDefinition("TF_KRAKEN_01") });
            board = session.InitialBoard;
            foreach (var ship in board.Ships) CreateView(ship);
        }

        public bool TryApplyMove(string shipId, out ForwardPathResult path)
        {
            if (session == null) throw new InvalidOperationException("Build a level before applying moves.");
            path = board.QueryForwardPath(shipId);
            if (path.IsBlocked && path.TravelDistance == 0) return false;
            board = board.ApplyPathResult(path);
            if (path.CanExit)
            {
                if (views.TryGetValue(shipId, out var view)) DestroyPreviewObject(view.gameObject);
                views.Remove(shipId);
            }
            else
            {
                PositionView(views[shipId], board.GetShip(shipId));
            }
            return true;
        }

        public void Clear()
        {
            foreach (var view in views.Values.ToArray())
                if (view != null) DestroyPreviewObject(view.gameObject);
            views.Clear();
            session?.Dispose();
            session = null;
            board = null;
        }

        private void OnDestroy() => Clear();

        private void CreateView(BoardShipSnapshot ship)
        {
            var root = new GameObject();
            root.transform.SetParent(transform, false);
            var view = root.AddComponent<PrototypeShipView>();
            view.Configure(ship);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = IsHorizontal(ship.Direction)
                ? new Vector3(ship.Length * 0.90f, 0.25f, 0.68f)
                : new Vector3(0.68f, 0.25f, ship.Length * 0.90f);
            ApplyColor(body.GetComponent<Renderer>(), DirectionColor(ship.Direction));

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "DirectionHead";
            head.transform.SetParent(root.transform, false);
            head.transform.localScale = Vector3.one * 0.38f;
            ApplyColor(head.GetComponent<Renderer>(), Color.white);

            views.Add(ship.Id, view);
            PositionView(view, ship);
            var headCell = ship.OccupiedCells[ship.OccupiedCells.Count - 1];
            head.transform.position = CellCenter(headCell) + Vector3.up * 0.28f;
        }

        private static void PositionView(PrototypeShipView view, BoardShipSnapshot ship)
        {
            var first = ship.OccupiedCells[0];
            var last = ship.OccupiedCells[ship.OccupiedCells.Count - 1];
            view.transform.localPosition = new Vector3((first.X + last.X) * 0.5f, 0f, (first.Y + last.Y) * 0.5f);
            var marker = view.transform.Find("DirectionHead");
            if (marker != null) marker.position = CellCenter(last) + Vector3.up * 0.28f;
        }

        private static Vector3 CellCenter(GridPosition cell) => new Vector3(cell.X, 0f, cell.Y);
        private static bool IsHorizontal(ShipDirection direction) =>
            direction == ShipDirection.Left || direction == ShipDirection.Right;

        private static Color DirectionColor(ShipDirection direction)
        {
            switch (direction)
            {
                case ShipDirection.Up: return new Color(0.20f, 0.68f, 0.95f);
                case ShipDirection.Down: return new Color(0.95f, 0.45f, 0.25f);
                case ShipDirection.Left: return new Color(0.55f, 0.35f, 0.90f);
                default: return new Color(0.25f, 0.78f, 0.45f);
            }
        }

        private static void ApplyColor(Renderer renderer, Color color)
        {
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }

        private static void DestroyPreviewObject(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
