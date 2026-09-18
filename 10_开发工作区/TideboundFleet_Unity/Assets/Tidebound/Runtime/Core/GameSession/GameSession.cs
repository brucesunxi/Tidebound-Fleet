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
        public string SessionId { get; }
        public string LevelId { get; }
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<ShipRuntimeData> Ships { get; }
        public BossRuntimeData Boss { get; }
        public BoardModel InitialBoard { get; }
        public GameState State { get; set; } = GameState.Prepare;
        public IEventBus Events { get; } = new SessionEventBus();

        internal GameSession(string levelId, int width, int height, ShipRuntimeData[] ships, BossRuntimeData boss)
        {
            SessionId = Guid.NewGuid().ToString("N"); LevelId = levelId; Width = width; Height = height;
            Ships = Array.AsReadOnly(ships); Boss = boss;
            InitialBoard = new BoardModel(width, height, ships);
        }
        public void Dispose() => Events.Dispose();
    }
}
