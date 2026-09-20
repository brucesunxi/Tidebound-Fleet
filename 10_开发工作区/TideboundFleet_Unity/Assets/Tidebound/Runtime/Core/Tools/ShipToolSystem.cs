using System;
using System.Collections.Generic;
using System.Linq;
using Tidebound.Board;
using Tidebound.Core;
using Tidebound.LevelDesign;
using Tidebound.Ship;

namespace Tidebound.Tools
{
    public enum ShipTool { None, Rescue, Shuffle, Reverse }
    public enum ToolUseStatus { Selected, Cancelled, Applied, Disabled, Busy, Paused, Terminal, Empty, NoUses, InvalidTarget, Unproven, StorageUnavailable, UseLimitReached }
    public sealed class ShipToolSystem : IDisposable
    {
        private readonly GameSession session;
        private readonly ShipMovementSystem movement;
        private readonly ToolInventory inventory;
        private readonly LevelSolverOptions solverOptions;
        private readonly int shuffleAttempts;
        private readonly Random random;
        private bool applying;
        private bool disposed;
        public const int MaxUsesPerAttempt=5;
        public int UsesLeft => Math.Max(0,MaxUsesPerAttempt-session.ToolUses);
        public bool Enabled { get; }
        public ShipTool Selection { get; private set; }
        public IReadOnlyList<string> LastAffectedIds { get; private set; } = Array.Empty<string>();
        public int Remaining(ShipTool tool) => inventory.Count(tool);
        public ShipToolSystem(GameSession session,ShipMovementSystem movement,ToolInventory inventory,bool enabled=true,LevelSolverOptions options=null,int shuffleAttempts=32,int? seed=null)
        {
            this.session=session;this.movement=movement;this.inventory=inventory ?? throw new ArgumentNullException(nameof(inventory));
            Enabled=enabled;this.shuffleAttempts=shuffleAttempts;random=seed.HasValue ? new Random(seed.Value) : new Random();
            solverOptions=options ?? new LevelSolverOptions(4000,400000,50);
        }
        public void CancelSelection() => Selection=ShipTool.None;
        private ToolUseStatus? Guard(ShipTool tool)
        {
            if(disposed)throw new ObjectDisposedException(nameof(ShipToolSystem));
            if(!Enabled)return ToolUseStatus.Disabled;
            if(session.State==GameState.Paused)return ToolUseStatus.Paused;
            if(session.State!=GameState.Playing)return ToolUseStatus.Terminal;
            if(applying || movement.IsBusy)return ToolUseStatus.Busy;
            if(session.Board.ShipCount==0)return ToolUseStatus.Empty;
            if(UsesLeft==0)return ToolUseStatus.UseLimitReached;
            if(!inventory.IsAvailable)return ToolUseStatus.StorageUnavailable;
            if(Remaining(tool)<=0)return ToolUseStatus.NoUses;
            return null;
        }
        public ToolUseStatus Select(ShipTool tool)
        {
            if(tool!=ShipTool.Reverse)throw new ArgumentException("Only reverse needs a target.");
            if(Selection==tool) { CancelSelection();return ToolUseStatus.Cancelled; }
            var error=Guard(tool);if(error.HasValue)return error.Value;
            Selection=tool;return ToolUseStatus.Selected;
        }
        public ToolUseStatus UseSelected(string shipId)
        {
            if(Selection!=ShipTool.Reverse)return ToolUseStatus.InvalidTarget;
            var error=Guard(ShipTool.Reverse);if(error.HasValue)return error.Value;
            if(!session.Board.TryGetShip(shipId,out var ship) || session.GetShip(shipId).State!=ShipState.Idle)return ToolUseStatus.InvalidTarget;
            var candidate=session.Board.WithPlacements(session.Board.Ships.Select(s=>s.Id==shipId ?
                s.WithPlacement(ship.OccupiedCells[ship.Length-1],RemainingFleetShuffler.Opposite(ship.Direction)) : s));
            if(!inventory.TrySpend(ShipTool.Reverse,new ToolMutation(candidate)))return ToolUseStatus.StorageUnavailable;
            session.ToolUses++;Commit(candidate);LastAffectedIds=new[]{shipId};CancelSelection();return ToolUseStatus.Applied;
        }
        public ToolUseStatus Rescue()
        {
            var error=Guard(ShipTool.Rescue);if(error.HasValue)return error.Value;
            var outer=PeripheralShips(session.Board).ToArray();RemainingFleetShuffler.Randomize(outer,random);
            var targets=outer.Take(2).ToArray();
            if(targets.Length==0)return ToolUseStatus.InvalidTarget;
            var next=session.Board;foreach(var id in targets)next=next.WithoutShip(id);
            if(!inventory.TrySpend(ShipTool.Rescue,new ToolMutation(next,movement.PlanRescue(targets))))return ToolUseStatus.StorageUnavailable;
            session.ToolUses++;CancelSelection();LastAffectedIds=targets;applying=true;
            try { movement.RescueDirectlyToLane(targets); }
            finally { applying=false; }
            return ToolUseStatus.Applied;
        }
        public ToolUseStatus Shuffle()
        {
            var error=Guard(ShipTool.Shuffle);if(error.HasValue)return error.Value;
            CancelSelection();var before=session.Board;
            var candidate=RemainingFleetShuffler.Propose(before,random.Next(),solverOptions,shuffleAttempts);
            if(candidate==null)return ToolUseStatus.Unproven;
            if(!inventory.TrySpend(ShipTool.Shuffle,new ToolMutation(candidate)))return ToolUseStatus.StorageUnavailable;
            session.ToolUses++;LastAffectedIds=before.Ships.Where(s=>!candidate.GetShip(s.Id).Position.Equals(s.Position) || candidate.GetShip(s.Id).Direction!=s.Direction).Select(s=>s.Id).ToArray();
            Commit(candidate);return ToolUseStatus.Applied;
        }
        // Exposed outline of an irregular fleet: first/last occupied cells in each row and column.
        public static IReadOnlyList<string> PeripheralShips(BoardModel board)
        {
            var ids=new HashSet<string>(StringComparer.Ordinal);
            for(var y=0;y<board.Height;y++)
            {
                var row=Enumerable.Range(0,board.Width).Select(x=>board.GetShipId(new GridPosition(x,y))).Where(x=>x!=null).ToArray();
                if(row.Length>0) { ids.Add(row[0]);ids.Add(row[row.Length-1]); }
            }
            for(var x=0;x<board.Width;x++)
            {
                var column=Enumerable.Range(0,board.Height).Select(y=>board.GetShipId(new GridPosition(x,y))).Where(y=>y!=null).ToArray();
                if(column.Length>0) { ids.Add(column[0]);ids.Add(column[column.Length-1]); }
            }
            return ids.OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        }
        private void Commit(BoardModel next)
        {
            session.Board=next;
            foreach(var placement in next.Ships)
            { var runtime=session.GetShip(placement.Id);runtime.Position=placement.Position;runtime.Direction=placement.Direction; }
        }
        public void Dispose() { disposed=true;Selection=ShipTool.None; }
    }
}
