// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Clocks
{
    [TestFixture]
    public class DecouplingFramedClockTest
    {
        private TestClockWithRange source = null!;
        private DecouplingFramedClock decouplingClock = null!;

        [SetUp]
        public void SetUp()
        {
            source = new TestClockWithRange();

            decouplingClock = new DecouplingFramedClock();
            decouplingClock.ChangeSource(source);
        }

        #region Basic assumptions

        [Test]
        public void TestStartFromDecoupling()
        {
            Assert.That(source.IsRunning, Is.False);
            Assert.That(decouplingClock.IsRunning, Is.False);

            decouplingClock.Start();

            Assert.That(source.IsRunning, Is.True);
            Assert.That(decouplingClock.IsRunning, Is.True);
        }

        [Test]
        public void TestStartFromSource()
        {
            Assert.That(source.IsRunning, Is.False);
            Assert.That(decouplingClock.IsRunning, Is.False);

            source.Start();

            Assert.That(source.IsRunning, Is.True);
            Assert.That(decouplingClock.IsRunning, Is.True);
        }

        [Test]
        public void TestSeekFromDecoupling()
        {
            Assert.That(source.CurrentTime, Is.EqualTo(0));
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(0));

            decouplingClock.Seek(1000);

            Assert.That(source.CurrentTime, Is.EqualTo(1000));
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(0));

            decouplingClock.ProcessFrame();

            Assert.That(source.CurrentTime, Is.EqualTo(1000));
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(1000));
        }

        [Test]
        public void TestSeekFromSource()
        {
            Assert.That(source.CurrentTime, Is.EqualTo(0));
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(0));

            source.Seek(1000);

            Assert.That(source.CurrentTime, Is.EqualTo(1000));
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(0));

            decouplingClock.ProcessFrame();

            Assert.That(source.CurrentTime, Is.EqualTo(1000));
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(1000));
        }

        [Test]
        public void ChangeSourceUpdatesToNewSourceTime()
        {
            const double first_source_time = 256000;
            const double second_source_time = 128000;

            source.Seek(first_source_time);
            source.Start();

            var secondSource = new TestClock { CurrentTime = second_source_time };

            decouplingClock.ProcessFrame();
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(first_source_time));

            decouplingClock.ChangeSource(secondSource);

            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(first_source_time));
            decouplingClock.ProcessFrame();
            Assert.That(secondSource.CurrentTime, Is.EqualTo(second_source_time));
        }

        #endregion

        #region Operation in non-decoupling mode

        [Test]
        public void TestSourceStoppedWhileNotDecoupling()
        {
            decouplingClock.AllowDecoupling = false;
            decouplingClock.Start();

            Assert.That(source.IsRunning, Is.True);
            Assert.That(decouplingClock.IsRunning, Is.True);

            source.Stop();
            decouplingClock.ProcessFrame();

            Assert.That(source.IsRunning, Is.False);
            Assert.That(decouplingClock.IsRunning, Is.False);
        }

        [Test]
        public void TestSeekNegativeWhileNotDecoupling()
        {
            decouplingClock.AllowDecoupling = false;
            decouplingClock.Seek(-1000);

            decouplingClock.ProcessFrame();

            Assert.AreEqual(0, source.CurrentTime);
            Assert.AreEqual(0, decouplingClock.CurrentTime);
        }

        #endregion

        #region Operation in decoupling mode

        // TODO: test playback is always forward over the 0ms boundary.

        // TODO: test backwards playback. (over the boundary?)

        #endregion

        private class TestClockWithRange : TestClock
        {
            public double MinTime => 0;
            public double MaxTime { get; set; } = double.PositiveInfinity;

            public override bool Seek(double position)
            {
                if (Math.Clamp(position, MinTime, MaxTime) != position)
                    return false;

                return base.Seek(position);
            }
        }
    }
}
