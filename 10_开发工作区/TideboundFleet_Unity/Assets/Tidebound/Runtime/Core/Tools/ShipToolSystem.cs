using System;
using System.Linq;
using Tidebound.Board;
using Tidebound.Core;
using Tidebound.Events;
using Tidebound.LevelDesign;
using Tidebound.Ship;

namespace Tidebound.Tools
{
    public enum ShipTool { None, Rescue, Shuffle, Reverse }
    public enum ToolUseStatus { Selected, Cancelled, Applied, PendingAnimation, Disabled, Busy, Paused, Terminal, Empty, NoUses, InvalidTarget, Unproven }
    public sealed class ShipToolSystem : IDisposable
    {
        private readonly GameSession session;
        private readonly ShipMovementSystem movement;
        private readonly LevelSolverOptions solverOptions;
        private readonly IDisposable exitSubscription;
        private readonly int[] uses={0,1,1,1};
        private readonly int shuffleAttempts;
        private string pendingRescue;
        private int shuffleSeed=20260920;
        private bool disposed;
        public bool Enabled { get; }
        public ShipTool Selection { get; private set; }
        public int Remaining(ShipTool tool) => tool==ShipTool.None ? 0 : uses[(int)tool];
        public ShipToolSystem(GameSession session,ShipMovementSystem movement,bool enabled=true,LevelSolverOptions options=null,int shuffleAttempts=8)
        {
            this.session=session;this.movement=movement;Enabled=enabled;this.shuffleAttempts=shuffleAttempts;
            solverOptions=options ?? new LevelSolverOptions(4000,400000,50);
            exitSubscription=session.Events.Subscribe<ShipExitBoardEvent>(e=>
            {
                if(!disposed && e.Ship.SessionId==session.SessionId && e.Ship.ShipId==pendingRescue)
                { pendingRescue=null;uses[(int)ShipTool.Rescue]--; }
            });
        }
        public void CancelSelection() => Selection=ShipTool.None;
        private ToolUseStatus? Guard(ShipTool tool)
        {
            if(disposed) throw new ObjectDisposedException(nameof(ShipToolSystem));
            if(!Enabled)return ToolUseStatus.Disabled;
            if(session.State==GameState.Paused)return ToolUseStatus.Paused;
            if(session.State!=GameState.Playing)return ToolUseStatus.Terminal;
            if(movement.IsBusy)return ToolUseStatus.Busy;
            if(session.Board.ShipCount==0)return ToolUseStatus.Empty;
            if(Remaining(tool)<=0)return ToolUseStatus.NoUses;
            return null;
        }
        public ToolUseStatus Select(ShipTool tool)
        {
            if(tool!=ShipTool.Rescue && tool!=ShipTool.Reverse)throw new ArgumentException("Select a targeted tool.");
            if(Selection==tool) { CancelSelection();return ToolUseStatus.Cancelled; }
            var error=Guard(tool);if(error.HasValue)return error.Value;
            Selection=tool;return ToolUseStatus.Selected;
        }
        public ToolUseStatus UseSelected(string shipId)
        {
            var tool=Selection;if(tool==ShipTool.None)return ToolUseStatus.InvalidTarget;
            var error=Guard(tool);if(error.HasValue)return error.Value;
            if(!session.Board.TryGetShip(shipId,out var ship) || session.GetShip(shipId).State!=ShipState.Idle)return ToolUseStatus.InvalidTarget;
            if(tool==ShipTool.Rescue)
            {
                pendingRescue=shipId;
                var request=movement.TryBeginRescue(shipId);
                if(!request.IsAccepted) { pendingRescue=null;return ToolUseStatus.InvalidTarget; }
                CancelSelection();return ToolUseStatus.PendingAnimation;
            }
            var opposite=ship.Direction==ShipDirection.Up ? ShipDirection.Down : ship.Direction==ShipDirection.Down ? ShipDirection.Up :
                ship.Direction==ShipDirection.Left ? ShipDirection.Right : ShipDirection.Left;
            var head=ship.OccupiedCells[ship.Length-1];
            var candidate=session.Board.WithPlacements(session.Board.Ships.Select(s=>s.Id==shipId ? s.WithPlacement(head,opposite) : s));
            if(LevelSolver.Solve(candidate,solverOptions).Status!=LevelSolverStatus.Solved)return ToolUseStatus.Unproven;
            Commit(candidate);uses[(int)ShipTool.Reverse]--;CancelSelection();return ToolUseStatus.Applied;
        }
        public ToolUseStatus Shuffle()
        {
            var error=Guard(ShipTool.Shuffle);if(error.HasValue)return error.Value;
            CancelSelection();
            var candidate=RemainingFleetShuffler.Propose(session.Board,session.InitialBoard,shuffleSeed++,solverOptions,shuffleAttempts);
            if(candidate==null)return ToolUseStatus.Unproven;
            Commit(candidate);uses[(int)ShipTool.Shuffle]--;return ToolUseStatus.Applied;
        }
        private void Commit(BoardModel next)
        {
            // Fully validated immutable board first; identities, skin, length, damage and departed ships remain intact.
            session.Board=next;
            foreach(var placement in next.Ships)
            { var runtime=session.GetShip(placement.Id);runtime.Position=placement.Position;runtime.Direction=placement.Direction; }
        }
        public void Dispose() { if(disposed)return;disposed=true;exitSubscription.Dispose();Selection=ShipTool.None; }
    }
}
