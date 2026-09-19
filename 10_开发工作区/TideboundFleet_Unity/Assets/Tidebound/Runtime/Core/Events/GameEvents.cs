using Tidebound.Board;
using Tidebound.Core;
using Tidebound.Ship;

namespace Tidebound.Events
{
    public readonly struct ShipEventContext
    {
        public string SessionId { get; }
        public string ShipId { get; }
        public string TypeId { get; }
        public string SkinId { get; }
        public ShipEventContext(string sessionId, string shipId, string typeId, string skinId)
        { SessionId = sessionId; ShipId = shipId; TypeId = typeId; SkinId = skinId; }
    }

    // These are contracts only. Loading a level does not publish gameplay events.
    public readonly struct ShipMoveStartEvent : ITideboundEvent
    {
        public ShipEventContext Ship { get; }
        public GridPosition From { get; }
        public ShipDirection Direction { get; }
        public ShipMoveStartEvent(ShipEventContext ship, GridPosition from, ShipDirection direction)
        { Ship = ship; From = from; Direction = direction; }
    }

    public readonly struct ShipMoveCompleteEvent : ITideboundEvent
    {
        public ShipEventContext Ship { get; }
        public GridPosition From { get; }
        public GridPosition To { get; }
        public bool WasBlocked { get; }
        public ShipMoveCompleteEvent(ShipEventContext ship, GridPosition from, GridPosition to, bool wasBlocked)
        { Ship = ship; From = from; To = to; WasBlocked = wasBlocked; }
    }

    public readonly struct ShipExitBoardEvent : ITideboundEvent
    {
        public ShipEventContext Ship { get; }
        public GridPosition TailPosition { get; }
        public ShipDirection ExitDirection { get; }
        public long ExitSequence { get; }
        public ShipExitBoardEvent(ShipEventContext ship, GridPosition tailPosition, ShipDirection exitDirection, long exitSequence)
        { Ship = ship; TailPosition = tailPosition; ExitDirection = exitDirection; ExitSequence = exitSequence; }
    }

    public readonly struct ShipEnterFleetEvent : ITideboundEvent
    {
        public ShipEventContext Ship { get; }
        public long ExitSequence { get; }
        public ShipEnterFleetEvent(ShipEventContext ship, long exitSequence) { Ship = ship; ExitSequence = exitSequence; }
    }

    public readonly struct AttackCreatedEvent : ITideboundEvent
    {
        public ShipEventContext Ship { get; }
        public string AttackId { get; }
        public int Damage { get; }
        public AttackCreatedEvent(ShipEventContext ship, string attackId, int damage)
        { Ship = ship; AttackId = attackId; Damage = damage; }
    }

    public readonly struct BossDamagedEvent : ITideboundEvent
    {
        public string SessionId { get; }
        public string BossId { get; }
        public string AttackId { get; }
        public int Damage { get; }
        public int RemainingHp { get; }
        public BossDamagedEvent(string sessionId, string bossId, string attackId, int damage, int remainingHp)
        { SessionId = sessionId; BossId = bossId; AttackId = attackId; Damage = damage; RemainingHp = remainingHp; }
    }

    public readonly struct AttemptEndedEvent : ITideboundEvent
    {
        public string SessionId { get; }
        public string LevelId { get; }
        public GameState Outcome { get; }
        public string Reason { get; }
        public AttemptEndedEvent(string sessionId,string levelId,GameState outcome,string reason)
        { SessionId=sessionId;LevelId=levelId;Outcome=outcome;Reason=reason; }
    }

    public readonly struct GameWinEvent : ITideboundEvent
    {
        public string SessionId { get; }
        public string LevelId { get; }
        public GameWinEvent(string sessionId, string levelId) { SessionId = sessionId; LevelId = levelId; }
    }
}
