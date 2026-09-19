using System;
using Tidebound.Events;
using Tidebound.Fleet;

namespace Tidebound.Combat
{
    public enum AttackStage { Queued, InFlight, Hit }
    public sealed class AttackToken
    {
        public string AttackId { get; }
        public ShipEventContext Ship { get; }
        public long ExitSequence { get; }
        public int Damage { get; }
        public FleetGroup Group { get; }
        public double LaunchAt { get; }
        public double HitAt { get; }
        public AttackStage Stage { get; internal set; }
        internal AttackToken(ShipEventContext ship,long sequence,int damage,FleetGroup group,double launchAt,double flightDuration)
        {
            Ship=ship;ExitSequence=sequence;Damage=damage;Group=group;
            AttackId=ship.SessionId+":"+ship.ShipId;LaunchAt=launchAt;HitAt=launchAt+flightDuration;
        }
        public float FlightProgress(double now) => (float)Math.Max(0,Math.Min(1,(now-LaunchAt)/(HitAt-LaunchAt)));
    }
    public sealed class CombatTiming
    {
        public double LaunchInterval { get; }
        public double FlightDuration { get; }
        public CombatTiming(double launchInterval=.20,double flightDuration=.25)
        {
            if(double.IsNaN(launchInterval)||double.IsInfinity(launchInterval)||launchInterval<=0 ||
               double.IsNaN(flightDuration)||double.IsInfinity(flightDuration)||flightDuration<=0)
                throw new ArgumentOutOfRangeException(nameof(launchInterval));
            LaunchInterval=launchInterval;FlightDuration=flightDuration;
        }
    }
}
