using System;
using System.Linq;
using Tidebound.Board;
using Tidebound.Core;
using Tidebound.LevelDesign;
using Tidebound.Ship;

namespace Tidebound.Tools
{
    public enum BoardProgressStatus { Busy, Clear, Solvable, NoMoves, Unsolvable, Unknown }
    public sealed class BoardProgressMonitor
    {
        private readonly GameSession session;
        private readonly ShipMovementSystem movement;
        private readonly LevelSolverOptions options;
        private BoardModel checkedBoard;
        public BoardProgressStatus Status { get; private set; }=BoardProgressStatus.Unknown;
        public bool NeedsRescue => Status==BoardProgressStatus.NoMoves || Status==BoardProgressStatus.Unsolvable;
        public BoardProgressMonitor(GameSession session,ShipMovementSystem movement,LevelSolverOptions options=null)
        { this.session=session;this.movement=movement;this.options=options ?? new LevelSolverOptions(2000,200000,50); }
        public BoardProgressStatus Refresh()
        {
            if(movement.IsBusy) return BoardProgressStatus.Busy;
            if(ReferenceEquals(checkedBoard,session.Board)) return Status;
            checkedBoard=session.Board;
            if(checkedBoard.ShipCount==0) return Status=BoardProgressStatus.Clear;
            if(!checkedBoard.Ships.Any(s=>checkedBoard.QueryForwardPath(s.Id).TravelDistance>0)) return Status=BoardProgressStatus.NoMoves;
            var result=LevelSolver.Solve(checkedBoard,options);
            return Status=result.Status==LevelSolverStatus.Solved ? BoardProgressStatus.Solvable :
                result.Status==LevelSolverStatus.Deadlocked ? BoardProgressStatus.Unsolvable : BoardProgressStatus.Unknown;
        }
        // Leaving a deadlocked attempt is explicit; merely diagnosing it leaves tools and combat available.
        public bool EndForRestart()
        {
            var status=Refresh();
            return session.TryEnd(GameState.Failed,status==BoardProgressStatus.NoMoves || status==BoardProgressStatus.Unsolvable ? "DeadlockAbandoned" : "Restarted");
        }
    }
}
