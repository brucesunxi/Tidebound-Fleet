using System;
using System.Collections.Generic;
using Tidebound.Board;
using Tidebound.Boss;
using Tidebound.Events;
using Tidebound.Ship;

namespace Tidebound.Core
{
    public sealed class GameSession : IDisposable
    {
        private readonly Dictionary<string, ShipRuntimeData> shipsById;
        private bool disposed;

        public string SessionId { get; }
        public string LevelId { get; }
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<ShipRuntimeData> Ships { get; }
        public BossRuntimeData Boss { get; }
        public BoardModel Board { get; internal set; }
        public BoardModel InitialBoard { get; }
        public GameState State { get; internal set; } = GameState.Prepare;
        public string EndReason { get; private set; }
        public IEventBus Events { get; } = new SessionEventBus();

        internal GameSession(string levelId, int width, int height, ShipRuntimeData[] ships, BossRuntimeData boss, string sessionId=null)
        {
            SessionId = sessionId ?? Guid.NewGuid().ToString("N"); LevelId = levelId; Width = width; Height = height;
            Ships = Array.AsReadOnly(ships); Boss = boss;
            Board = new BoardModel(width, height, ships);
            InitialBoard = Board;
            shipsById = new Dictionary<string, ShipRuntimeData>(StringComparer.Ordinal);
            foreach (var ship in ships) shipsById.Add(ship.Id, ship);
        }

        public bool TryGetShip(string shipId, out ShipRuntimeData ship)
        {
            if (shipId == null)
            {
                ship = null;
                return false;
            }
            return shipsById.TryGetValue(shipId, out ship);
        }

        public ShipRuntimeData GetShip(string shipId)
        {
            if (string.IsNullOrEmpty(shipId)) throw new ArgumentException("A ship id is required.", nameof(shipId));
            if (!shipsById.TryGetValue(shipId, out var ship))
                throw new KeyNotFoundException($"Ship '{shipId}' does not exist in this session.");
            return ship;
        }

        internal bool TryEnd(GameState outcome,string reason)
        {
            if(outcome!=GameState.Victory && outcome!=GameState.Failed) throw new ArgumentException("Expected a terminal outcome.");
            if(disposed || State==GameState.Victory || State==GameState.Failed) return false;
            State=outcome;EndReason=reason;
            Events.Publish(new AttemptEndedEvent(SessionId,LevelId,outcome,reason));
            if(outcome==GameState.Victory && !disposed) Events.Publish(new GameWinEvent(SessionId,LevelId));
            return true;
        }

        public void Dispose() { if(disposed)return;disposed=true;Events.Dispose(); }
    }
}
