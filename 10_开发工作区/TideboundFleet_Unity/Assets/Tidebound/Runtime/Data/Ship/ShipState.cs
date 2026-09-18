namespace Tidebound.Ship
{
    // BlockedFeedback is transient. Its future completion returns to Idle at the new position.
    public enum ShipState { Idle = 0, Moving = 1, BlockedFeedback = 2, Exiting = 3, InLane = 4, InFleet = 5 }
}
