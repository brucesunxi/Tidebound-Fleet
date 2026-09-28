using NUnit.Framework;
using Tidebound.Combat;
using Tidebound.Events;
using Tidebound.Board;
using Tidebound.Ship;

namespace Tidebound.Tests
{
 public sealed class ExitComboTests
 {
  private static ShipExitBoardEvent Exit(int n,string session="run")=>new ShipExitBoardEvent(new ShipEventContext(session,"Ship"+n,"TF_BASE_SHIP",null),new GridPosition(0,0),ShipDirection.Up,n);
  [Test] public void OnlyUniqueMatchingExitEventsCountAndPartialMovementCannotIncrement()
  {var c=new ExitComboState("run");Assert.That(c.Record(Exit(1,"old"),0),Is.False);Assert.That(c.Count,Is.Zero);c.Record(Exit(1),0);Assert.That(c.Record(Exit(1),1),Is.False);Assert.That(c.Count,Is.EqualTo(1));Assert.That(c.Remaining,Is.EqualTo(4));}
  [Test] public void ExactFiveSecondsExpiresWhileFourPointNineContinues()
  {var c=new ExitComboState("run");c.Record(Exit(1),0);c.Record(Exit(2),4.9);Assert.That(c.Count,Is.EqualTo(2));c.Record(Exit(3),9.9);Assert.That(c.Count,Is.EqualTo(1));}
  [Test] public void TierThresholdsAndSnapshotSurviveExpiryOnTheWayToBattle()
  {var c=new ExitComboState("run");for(var i=1;i<=12;i++){c.Record(Exit(i),i*.1);Assert.That(c.Tier,Is.EqualTo(i<3?0:i<6?1:i<10?2:3));}c.Advance(7);Assert.That(c.Count,Is.Zero);Assert.That(c.TierFor("Ship3"),Is.EqualTo(1));Assert.That(c.TierFor("Ship6"),Is.EqualTo(2));Assert.That(c.TierFor("Ship12"),Is.EqualTo(3));}
  [Test] public void FrozenClockAndEndNeverGiveExtraCountsOrCarryIntoNextRun()
  {var c=new ExitComboState("run");c.Record(Exit(1),1);c.Advance(3);for(var i=0;i<100;i++)c.Advance(3);Assert.That(c.Remaining,Is.EqualTo(3));c.End();Assert.That(c.Count,Is.Zero);Assert.That(c.TierFor("Ship1"),Is.Zero);}
 }
}
