using System;
using System.Collections.Generic;
using System.Linq;
using Tidebound.Core;
using Tidebound.Events;
using Tidebound.Fleet;
using Tidebound.Lane;
using Tidebound.Ship;

namespace Tidebound.Combat
{
    /// <summary>Session-owned attack ledger. Its clock is the transit clock, never an animation callback.</summary>
    public sealed class FleetCombatSystem : IDisposable
    {
        private const double Epsilon=1e-7;
        private readonly GameSession session;
        private readonly TransitSystem transit;
        private readonly CombatTiming timing;
        private readonly IDisposable subscription;
        private readonly List<AttackToken> tokens=new List<AttackToken>();
        private readonly HashSet<string> admitted=new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<long> sequences=new HashSet<long>();
        private bool disposed;
        public FleetRoster Fleet { get; }
        public IReadOnlyList<AttackToken> Attacks { get; }
        public int HitCount { get; private set; }
        public int PendingCount => tokens.Count-HitCount;
        public int InFlightCount => tokens.Count(t=>t.Stage==AttackStage.InFlight);
        public int RejectedEvents { get; private set; }
        public string FaultReason { get; private set; }
        public double Time => transit.ElapsedTime;
        public double LastHitAt { get; private set; } = double.NegativeInfinity;
        public bool IsVictorious => session.State==GameState.Victory;

        public FleetCombatSystem(GameSession session,TransitSystem transit,CombatTiming timing=null,IReadOnlyList<string> equippedOrder=null)
        {
            this.session=session ?? throw new ArgumentNullException(nameof(session));
            this.transit=transit ?? throw new ArgumentNullException(nameof(transit));
            this.timing=timing ?? new CombatTiming();
            if(transit.SessionId!=session.SessionId || session.Ships.Any(s=>s.State==ShipState.InFleet) ||
               session.Boss.InitialHp!=session.Ships.Count*FoundationLimits.BaseShipDamage ||
               session.Ships.Any(s=>s.Damage!=FoundationLimits.BaseShipDamage))
                throw new ArgumentException("Create combat for the matching session before any ship enters its fleet.");
            Fleet=new FleetRoster(session.Ships,equippedOrder);Attacks=tokens.AsReadOnly();
            subscription=session.Events.Subscribe<ShipEnterFleetEvent>(OnEnterFleet);
        }

        private void OnEnterFleet(ShipEnterFleetEvent e)
        {
            // Validate against the committed runtime state; never trust event damage/skin identity.
            if(disposed || e.Ship.SessionId!=session.SessionId || e.ExitSequence<=0 ||
               !session.TryGetShip(e.Ship.ShipId,out var ship) || ship.State!=ShipState.InFleet ||
               e.Ship.TypeId!=ship.TypeId || e.Ship.SkinId!=ship.SkinId ||
               e.ExitSequence>=transit.NextEntranceSequence || admitted.Contains(ship.Id) || sequences.Contains(e.ExitSequence))
            { RejectedEvents++;return; }
            var group=Fleet.GroupFor(ship);
            var launchAt=Math.Max(transit.ElapsedTime,group.NextLaunchAt);
            var token=new AttackToken(e.Ship,e.ExitSequence,ship.Damage,group,launchAt,timing.FlightDuration);
            admitted.Add(ship.Id);sequences.Add(e.ExitSequence);tokens.Add(token);
            group.ArrivedCount++;group.NextLaunchAt=launchAt+timing.LaunchInterval;
            session.Events.Publish(new AttackCreatedEvent(token.Ship,token.AttackId,token.Damage));
        }

        /// <summary>Call after advancing TransitSystem. Pausing transit freezes the entire combat timeline.</summary>
        public void Advance()
        {
            if(disposed) throw new ObjectDisposedException(nameof(FleetCombatSystem));
            if(session.State!=GameState.Playing) return;
            if(session.Boss.Hp!=session.Boss.InitialHp-HitCount*FoundationLimits.BaseShipDamage)
            { Fail("BossHpMismatch");return; }
            while(true)
            {
                AttackToken next=null;var nextTime=double.PositiveInfinity;
                foreach(var token in tokens)
                {
                    if(token.Stage==AttackStage.Hit) continue;
                    var at=token.Stage==AttackStage.Queued ? token.LaunchAt : token.HitAt;
                    if(at>Time+Epsilon) continue;
                    if(at<nextTime || (at==nextTime && (next==null || token.ExitSequence<next.ExitSequence)))
                    { next=token;nextTime=at; }
                }
                if(next==null) break;
                if(next.Stage==AttackStage.Queued) next.Stage=AttackStage.InFlight;
                else
                {
                    // Mark consumed before publishing; re-entrant or repeated Advance cannot hit twice.
                    next.Stage=AttackStage.Hit;HitCount++;next.Group.HitCount++;
                    session.Boss.Hp-=next.Damage;LastHitAt=next.HitAt;
                    session.Events.Publish(new BossDamagedEvent(session.SessionId,session.Boss.BossId,next.AttackId,next.Damage,session.Boss.Hp));
                }
                if(disposed || session.State!=GameState.Playing) return;
            }
            if(session.Board.ShipCount!=0 || !transit.IsEmpty || PendingCount!=0) return;
            if(tokens.Count!=session.Ships.Count || session.Boss.Hp!=0) { Fail("IncompleteAttackLedger");return; }
            session.State=GameState.Victory;
            session.Events.Publish(new GameWinEvent(session.SessionId,session.LevelId));
        }
        private void Fail(string reason) { FaultReason=reason;session.State=GameState.Failed; }
        public void Dispose() { if(disposed)return;disposed=true;subscription.Dispose(); }
    }
}
