using System;
using System.Collections.Generic;
using Tidebound.Events;

namespace Tidebound.Combat
{
    /// <summary>Cosmetic exit chain; has no access to damage, currency or progression.</summary>
    public sealed class ExitComboState
    {
        public const double Window=5;
        private readonly string sessionId;
        private readonly Dictionary<string,int> tiers=new Dictionary<string,int>(StringComparer.Ordinal);
        private double now;
        public int Count {get;private set;}
        public int Tier=>Count<3?0:Count<6?1:Count<10?2:3;
        public double LastExitAt {get;private set;}=double.NegativeInfinity;
        public double Remaining=>Count==0?0:Math.Max(0,Window-(now-LastExitAt));
        public ExitComboState(string sessionId){this.sessionId=sessionId;}
        public void Advance(double time)
        {
            if(double.IsNaN(time)||double.IsInfinity(time)||time<now)throw new ArgumentOutOfRangeException(nameof(time));
            now=time;if(now-LastExitAt>=Window)Count=0;
        }
        public bool Record(ShipExitBoardEvent exit,double time)
        {
            Advance(time);
            if(exit.Ship.SessionId!=sessionId || string.IsNullOrEmpty(exit.Ship.ShipId) || exit.ExitSequence<=0 || tiers.ContainsKey(exit.Ship.ShipId))return false;
            Count++;LastExitAt=now;tiers.Add(exit.Ship.ShipId,Tier);return true;
        }
        public int TierFor(string shipId)=>tiers.TryGetValue(shipId,out var tier)?tier:0;
        public void End(){Count=0;tiers.Clear();LastExitAt=double.NegativeInfinity;}
    }
}
