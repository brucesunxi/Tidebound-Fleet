using System;
using System.Linq;
using Tidebound.Board;
using Tidebound.Core;
using Tidebound.Events;

namespace Tidebound.Ship
{
    /// <summary>
    /// Session-scoped movement state machine. It serializes clicks and commits logical board changes only
    /// when presentation reports the corresponding completion point.
    /// </summary>
    public sealed class ShipMovementSystem
    {
        private readonly GameSession session;
        private long nextOperationId;
        private long nextExitSequence;
        private bool rescuing;

        public ShipMoveOperation ActiveOperation { get; private set; }
        public bool IsBusy => ActiveOperation != null || rescuing;
        public BoardModel Board => session.Board;
        public GameState GameState => session.State;

        public ShipMovementSystem(GameSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public bool StartPlaying()
        {
            if (session.State != GameState.Prepare) return false;
            session.State = GameState.Playing;
            return true;
        }

        public bool Pause()
        {
            if (session.State != GameState.Playing) return false;
            session.State = GameState.Paused;
            return true;
        }

        public bool Resume()
        {
            if (session.State != GameState.Paused) return false;
            session.State = GameState.Playing;
            return true;
        }

        public ShipMoveRequestResult TryBeginMove(string shipId)
        {
            if (session.State == GameState.Paused)
                return new ShipMoveRequestResult(ShipMoveRequestStatus.SessionPaused);
            if (session.State != GameState.Playing)
                return new ShipMoveRequestResult(ShipMoveRequestStatus.SessionNotPlaying);
            if (IsBusy) return new ShipMoveRequestResult(ShipMoveRequestStatus.Busy);
            if (!session.TryGetShip(shipId, out var ship))
                return new ShipMoveRequestResult(ShipMoveRequestStatus.ShipNotFound);
            if (ship.State != ShipState.Idle)
                return new ShipMoveRequestResult(ShipMoveRequestStatus.ShipNotIdle);
            if (!session.Board.TryGetShip(ship.Id, out _))
                return new ShipMoveRequestResult(ShipMoveRequestStatus.ShipNotOnBoard);

            var path = session.Board.QueryForwardPath(ship.Id);
            var operation = new ShipMoveOperation(checked(++nextOperationId), path);
            ActiveOperation = operation;
            ship.State = path.CanExit ? ShipState.Exiting :
                path.TravelDistance == 0 ? ShipState.BlockedFeedback : ShipState.Moving;

            var context = Context(ship);
            session.Events.Publish(new ShipMoveStartEvent(context, path.OriginTail, path.Direction));

            if (operation.Stage == ShipMoveStage.BlockedFeedback)
                CommitBlockedArrival(ship, operation);

            return new ShipMoveRequestResult(ShipMoveRequestStatus.Accepted, operation);
        }

        internal void RescueDirectlyToLane(string[] ids)
        {
            if(session.State!=GameState.Playing || IsBusy || ids==null || ids.Length<1 || ids.Length>2 || ids.Distinct().Count()!=ids.Length)
                throw new InvalidOperationException("Invalid rescue batch.");
            var paths=ids.Select(id=>
            {
                var ship=session.Board.GetShip(id);
                if(session.GetShip(id).State!=ShipState.Idle)throw new InvalidOperationException("Rescue needs idle ships.");
                var distances=new[]{session.Height-ship.OccupiedCells.Min(c=>c.Y),session.Width-ship.OccupiedCells.Min(c=>c.X),
                    ship.OccupiedCells.Max(c=>c.Y)+1,ship.OccupiedCells.Max(c=>c.X)+1};
                var directions=new[]{ShipDirection.Up,ShipDirection.Right,ShipDirection.Down,ShipDirection.Left};
                var i=Array.IndexOf(distances,distances.Min());var direction=directions[i];var step=GridFootprint.DirectionStep(direction);
                var target=new GridPosition(ship.Position.X+step.X*distances[i],ship.Position.Y+step.Y*distances[i]);
                return ForwardPathResult.Exit(id,ship.Position,direction,target,Array.Empty<GridPosition>(),distances[i]);
            }).ToArray();
            var next=session.Board;
            foreach(var path in paths)next=next.WithoutShip(path.ShipId);
            // Commit both before publishing either event; all observers see the same completed rescue.
            session.Board=next;
            foreach(var path in paths) {var ship=session.GetShip(path.ShipId);ship.Position=path.TargetTail;ship.State=ShipState.InLane;}
            rescuing=true;
            try
            {
                foreach(var path in paths)
                {
                    var ship=session.GetShip(path.ShipId);
                    session.Events.Publish(new ShipExitBoardEvent(Context(ship),path.TargetTail,path.Direction,checked(++nextExitSequence)));
                }
            }
            finally { rescuing=false; }
        }

        public ShipMoveAdvanceStatus CompleteTravel(long operationId)
        {
            var readiness = CheckAdvance(operationId, ShipMoveStage.Traveling);
            if (readiness != ShipMoveAdvanceStatus.Applied) return readiness;

            var operation = ActiveOperation;
            var ship = session.GetShip(operation.ShipId);
            if (operation.WasBlocked)
            {
                CommitBlockedArrival(ship, operation);
                return ShipMoveAdvanceStatus.Applied;
            }

            session.Board = session.Board.ApplyPathResult(operation.PathResult);
            ship.Position = operation.TargetTail;
            ship.State = ShipState.InLane;
            operation.Stage = ShipMoveStage.Completed;
            var context = Context(ship);
            var exitSequence = checked(++nextExitSequence);
            try
            {
                session.Events.Publish(new ShipMoveCompleteEvent(
                    context, operation.OriginTail, operation.TargetTail, false));
                session.Events.Publish(new ShipExitBoardEvent(
                    context, operation.TargetTail, operation.Direction, exitSequence));
            }
            finally
            {
                // Subscriber failures are visible, but a logically completed exit must not leave input locked.
                ActiveOperation = null;
            }
            return ShipMoveAdvanceStatus.Applied;
        }

        public ShipMoveAdvanceStatus CompleteBlockedFeedback(long operationId)
        {
            var readiness = CheckAdvance(operationId, ShipMoveStage.BlockedFeedback);
            if (readiness != ShipMoveAdvanceStatus.Applied) return readiness;

            var operation = ActiveOperation;
            var ship = session.GetShip(operation.ShipId);
            ship.State = ShipState.Idle;
            operation.Stage = ShipMoveStage.Completed;
            ActiveOperation = null;
            return ShipMoveAdvanceStatus.Applied;
        }

        private void CommitBlockedArrival(ShipRuntimeData ship, ShipMoveOperation operation)
        {
            session.Board = session.Board.ApplyPathResult(operation.PathResult);
            ship.Position = operation.TargetTail;
            ship.State = ShipState.BlockedFeedback;
            operation.Stage = ShipMoveStage.BlockedFeedback;
            session.Events.Publish(new ShipMoveCompleteEvent(
                Context(ship), operation.OriginTail, operation.TargetTail, true));
        }

        private ShipMoveAdvanceStatus CheckAdvance(long operationId, ShipMoveStage expectedStage)
        {
            if (session.State == GameState.Paused) return ShipMoveAdvanceStatus.SessionPaused;
            if (session.State != GameState.Playing) return ShipMoveAdvanceStatus.SessionNotPlaying;
            if (ActiveOperation == null) return ShipMoveAdvanceStatus.NoActiveOperation;
            if (ActiveOperation.OperationId != operationId) return ShipMoveAdvanceStatus.OperationMismatch;
            if (ActiveOperation.Stage != expectedStage) return ShipMoveAdvanceStatus.WrongStage;
            return ShipMoveAdvanceStatus.Applied;
        }

        private ShipEventContext Context(ShipRuntimeData ship) =>
            new ShipEventContext(session.SessionId, ship.Id, ship.TypeId, ship.SkinId);
    }
}
