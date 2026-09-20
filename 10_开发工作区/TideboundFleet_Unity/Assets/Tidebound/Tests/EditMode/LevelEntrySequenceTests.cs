using System;
using System.Linq;
using NUnit.Framework;
using Tidebound.LevelDesign;

namespace Tidebound.Tests
{
    public sealed class LevelEntrySequenceTests
    {
        [TestCase(7)][TestCase(80)]
        public void FinalGroupIsFullyVisibleBeforeReadyRegardlessOfDensity(int count)
        {
            var e=new LevelEntrySequence();e.Begin(false,false);
            Assert.That(e.Phase,Is.EqualTo(LevelEntryPhase.Field));Assert.That(e.Alpha(0,count),Is.Zero);
            e.Advance(.17,false);Assert.That(e.Alpha(0,count),Is.Zero);
            e.Advance(.21,false);
            Assert.That(e.Phase,Is.EqualTo(LevelEntryPhase.Ships));
            Assert.That(e.Alpha(0,count),Is.EqualTo(1).Within(.0001));Assert.That(e.Alpha(count-1,count),Is.Zero);
            e.Advance(.279,false);Assert.That(e.IsReady,Is.False);Assert.That(e.Alpha(count-1,count),Is.LessThan(1));
            e.Advance(.001,false);Assert.That(e.IsReady,Is.True);
            Assert.That(Enumerable.Range(0,count).All(i=>e.Alpha(i,count)==1),Is.True);
        }
        [TestCase(true,false)][TestCase(false,true)][TestCase(true,true)]
        public void RestoreAndReducedMotionUseShortUniformFade(bool resume,bool reduced)
        {
            var e=new LevelEntrySequence();e.Begin(resume,reduced);e.Advance(.06,false);
            Assert.That(e.Phase,Is.EqualTo(LevelEntryPhase.Ships));Assert.That(e.Alpha(0,80),Is.EqualTo(.5f).Within(.0001));
            Assert.That(e.Alpha(79,80),Is.EqualTo(e.Alpha(0,80)));
            e.Advance(.06,false);Assert.That(e.IsReady,Is.True);
        }
        [Test]
        public void SaveFailureAndSuspensionDoNotAdvanceClock()
        {
            var e=new LevelEntrySequence();e.WaitForSave();e.Advance(500,false);
            Assert.That(e.IsReady,Is.False);Assert.That(e.Elapsed,Is.Zero);
            e.Begin(false,false);e.Advance(.3,false);e.Advance(500,true);
            Assert.That(e.Elapsed,Is.EqualTo(.3));Assert.That(e.IsReady,Is.False);
            e.Advance(500,false);Assert.That(e.IsReady,Is.True);Assert.That(e.Alpha(0,1),Is.EqualTo(1));
        }
        [TestCase(-1)][TestCase(double.NaN)][TestCase(double.PositiveInfinity)]
        public void InvalidElapsedCannotBypassReady(double seconds)
        {var e=new LevelEntrySequence();e.Begin(false,false);Assert.Throws<ArgumentOutOfRangeException>(()=>e.Advance(seconds,false));Assert.That(e.IsReady,Is.False);}
    }
}
