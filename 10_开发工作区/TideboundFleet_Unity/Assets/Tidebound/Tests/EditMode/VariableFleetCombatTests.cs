using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tidebound.Board;
using Tidebound.Combat;
using Tidebound.Config;
using Tidebound.Core;
using Tidebound.Events;
using Tidebound.Lane;
using Tidebound.Ship;
using Tidebound.Tools;

namespace Tidebound.Tests
{
    public sealed class VariableFleetCombatTests
    {
        // A legal 14x18 combat fixture, deliberately not a proposed puzzle or difficulty sample.
        internal static LevelData Layout(int count, int longCount = 6)
        {
            var heights = new int[14];
            var placements = new List<ShipPlacementData>();
            for (var i = 0; i < count; i++)
            {
                var x = i % 14; var length = i < longCount ? 3 : 2;
                placements.Add(new ShipPlacementData { Id = "S" + i.ToString("D3"), TypeId = FoundationLimits.BaseShipTypeId,
                    Position = new GridPosition(x, heights[x]), Direction = ShipDirection.Up, Length = length });
                heights[x] += length;
            }
            return new LevelData { SchemaVersion = 2, LevelId = "VariableFleetFixture", Width = 14, Height = 18,
                BossId = "TF_KRAKEN_01", Ships = placements.ToArray() };
        }

        [TestCase(7, 0, false)]
        [TestCase(80, 6, false)]
        [TestCase(84, 6, false)]
        [TestCase(88, 8, false)]
        [TestCase(90, 6, false)]
        [TestCase(90, 8, true)]
        [TestCase(92, 8, false)]
        [TestCase(92, 8, true)]
        public void InitialFleetBudgetSurvivesRescueAndOnlyFinalImpactWins(int count, int longCount, bool rescue)
        {
            var layout = Layout(count, longCount);
            using (var session = CampaignPackValidator.Create(layout))
            using (var lane = new TransitSystem(session, new LaneTransitTiming(.1, .01, .01)))
            using (var combat = new FleetCombatSystem(session, lane))
            {
                Assert.That(session.Boss.InitialHp, Is.EqualTo(count * 10));
                Assert.That(session.Ships.Sum(s => s.Damage), Is.EqualTo(session.Boss.InitialHp));
                Assert.That(session.Ships.Where(s => s.Length == 3).All(s => s.Damage == 10), Is.True);
                var hits = new HashSet<string>(); var wins = 0; var movement = new ShipMovementSystem(session);
                session.Events.Subscribe<GameWinEvent>(_ => wins++);
                session.Events.Subscribe<BossDamagedEvent>(e => {
                    Assert.That(hits.Add(e.AttackId), Is.True, "A ship attacked twice.");
                    Assert.That(e.Damage, Is.EqualTo(10));
                    Assert.That(session.Boss.Hp, Is.EqualTo((count - hits.Count) * 10));
                });
                movement.StartPlaying();
                if (rescue)
                {
                    var stock = new ToolInventory(); stock.Grant("variable-fleet-test", 1, 0, 0);
                    using (var tools = new ShipToolSystem(session, movement, stock, seed: 928))
                    {
                        Assert.That(tools.Rescue(), Is.EqualTo(ToolUseStatus.Applied));
                        Assert.That(session.Board.ShipCount, Is.EqualTo(count - 2));
                        Assert.That(session.Boss.InitialHp, Is.EqualTo(count * 10));
                        Assert.That(session.Boss.Hp, Is.EqualTo(count * 10), "Rescue itself must not reduce HP.");
                        lane.Advance(2); combat.Advance();
                        Assert.That(combat.HitCount, Is.EqualTo(2));
                        Assert.That(combat.Attacks.Select(a => a.Ship.ShipId), Is.EquivalentTo(tools.LastAffectedIds));
                    }
                }
                while (session.Board.ShipCount > 1)
                {
                    var next = session.Board.Ships.First(s => session.Board.QueryForwardPath(s.Id).CanExit);
                    Exit(movement, next.Id); lane.Advance(2); combat.Advance();
                }
                lane.Advance(30); combat.Advance();
                Assert.That(hits.Count, Is.EqualTo(count - 1));
                Assert.That(session.Boss.Hp, Is.EqualTo(10));
                Assert.That(session.State, Is.EqualTo(GameState.Playing));
                Assert.That(wins, Is.Zero);
                Exit(movement, session.Board.Ships.Single().Id);
                Assert.That(session.Board.ShipCount, Is.Zero);
                Assert.That(session.Boss.Hp, Is.EqualTo(10), "The last exit is not the last impact.");
                lane.Advance(.11); combat.Advance();
                Assert.That(session.Boss.Hp, Is.EqualTo(10));
                Assert.That(wins, Is.Zero);
                lane.Advance(1); combat.Advance(); combat.Advance();
                Assert.That(combat.HitCount, Is.EqualTo(count));
                Assert.That(combat.Attacks.Count, Is.EqualTo(count));
                Assert.That(combat.PendingCount, Is.Zero);
                Assert.That(session.Boss.InitialHp, Is.EqualTo(count * 10));
                Assert.That(session.Boss.Hp, Is.Zero);
                Assert.That(combat.IsVictorious, Is.True);
                Assert.That(wins, Is.EqualTo(1));
            }
        }

        private static void Exit(ShipMovementSystem movement, string id)
        {
            var request = movement.TryBeginMove(id);
            Assert.That(request.IsAccepted, Is.True);
            Assert.That(request.Operation.WillExit, Is.True);
            Assert.That(movement.CompleteTravel(request.Operation.OperationId), Is.EqualTo(ShipMoveAdvanceStatus.Applied));
        }
    }
}
