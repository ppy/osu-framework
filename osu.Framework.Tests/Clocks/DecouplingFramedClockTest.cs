// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Clocks
{
    [TestFixture]
    public class DecouplingFramedClockTest
    {
        private IAdjustableClock source = null!;
        private DecouplingFramedClock decouplingClock = null!;

        [SetUp]
        public void SetUp()
        {
            source = new TestClockWithRange();

            decouplingClock = new DecouplingFramedClock();
            decouplingClock.ChangeSource(source);
        }

        #region Basic assumptions (which hold for both decoupled and not)

        [TestCase(true)]
        [TestCase(false)]
        public void TestStartFromDecoupling(bool allowDecoupling)
        {
            decouplingClock.AllowDecoupling = allowDecoupling;

            Assert.That(source.IsRunning, Is.False);
            Assert.That(decouplingClock.IsRunning, Is.False);

            decouplingClock.Start();
            decouplingClock.ProcessFrame();

            Assert.That(source.IsRunning, Is.True);
            Assert.That(decouplingClock.IsRunning, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestStartFromSource(bool allowDecoupling)
        {
            decouplingClock.AllowDecoupling = allowDecoupling;

            Assert.That(source.IsRunning, Is.False);
            Assert.That(decouplingClock.IsRunning, Is.False);

            source.Start();
            decouplingClock.ProcessFrame();

            Assert.That(source.IsRunning, Is.True);
            Assert.That(decouplingClock.IsRunning, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestSeekFromDecoupling(bool allowDecoupling)
        {
            decouplingClock.AllowDecoupling = allowDecoupling;

            Assert.That(source.CurrentTime, Is.EqualTo(0));
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(0));

            decouplingClock.Seek(1000);

            Assert.That(source.CurrentTime, Is.EqualTo(1000));
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(0));

            decouplingClock.ProcessFrame();

            Assert.That(source.CurrentTime, Is.EqualTo(1000));
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(1000));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestSeekFromSource(bool allowDecoupling)
        {
            decouplingClock.AllowDecoupling = allowDecoupling;

            // Seeking the source when in decoupled mode isn't really supported.
            // But it will work if the source is running.
            source.Start();

            Assert.That(source.CurrentTime, Is.EqualTo(0));
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(0));

            source.Seek(1000);
            decouplingClock.ProcessFrame();

            Assert.That(source.CurrentTime, Is.EqualTo(1000));
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(1000));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestChangeSourceUpdatesToNewSourceTime(bool allowDecoupling)
        {
            decouplingClock.AllowDecoupling = allowDecoupling;

            const double first_source_time = 256000;
            const double second_source_time = 128000;

            source.Seek(first_source_time);
            source.Start();

            decouplingClock.ProcessFrame();

            var secondSource = new TestClock { CurrentTime = second_source_time };

            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(first_source_time));

            decouplingClock.ChangeSource(secondSource);
            decouplingClock.ProcessFrame();

            Assert.That(secondSource.CurrentTime, Is.EqualTo(second_source_time));
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(second_source_time));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestChangeSourceUpdatesToCorrectSourceState(bool allowDecoupling)
        {
            decouplingClock.AllowDecoupling = allowDecoupling;

            source.Start();
            decouplingClock.ProcessFrame();
            Assert.That(decouplingClock.IsRunning, Is.True);

            var secondSource = new TestClock();

            decouplingClock.ChangeSource(secondSource);
            decouplingClock.ProcessFrame();
            Assert.That(decouplingClock.IsRunning, Is.False);

            decouplingClock.ChangeSource(source);
            decouplingClock.ProcessFrame();
            Assert.That(decouplingClock.IsRunning, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestReset(bool allowDecoupling)
        {
            decouplingClock.AllowDecoupling = allowDecoupling;

            source.Seek(2000);
            source.Start();

            decouplingClock.ProcessFrame();

            Assert.That(decouplingClock.IsRunning, Is.True);
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(2000));

            decouplingClock.Reset();
            decouplingClock.ProcessFrame();

            Assert.That(decouplingClock.IsRunning, Is.False);
            Assert.That(source.IsRunning, Is.False);
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(0));
            Assert.That(source.CurrentTime, Is.EqualTo(0));
        }

        #endregion

        #region Operation in non-decoupling mode

        [Test]
        public void TestSourceStoppedWhileNotDecoupling()
        {
            decouplingClock.AllowDecoupling = false;
            decouplingClock.Start();
            decouplingClock.ProcessFrame();

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

            Assert.That(decouplingClock.Seek(-1000), Is.False);

            Assert.That(source.CurrentTime, Is.EqualTo(0));
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(0));
        }

        [Test]
        public void TestSeekPositiveWhileNotDecoupling()
        {
            decouplingClock.AllowDecoupling = false;
            Assert.That(decouplingClock.Seek(1000), Is.True);
            decouplingClock.ProcessFrame();

            Assert.That(source.CurrentTime, Is.EqualTo(1000));
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(1000));
        }

        #endregion

        #region Operation in decoupling mode

        [Test]
        public void TestSourceStoppedWhileDecoupling()
        {
            decouplingClock.AllowDecoupling = true;
            decouplingClock.Start();
            decouplingClock.ProcessFrame();

            Assert.That(source.IsRunning, Is.True);
            Assert.That(decouplingClock.IsRunning, Is.True);

            source.Stop();

            Assert.That(source.IsRunning, Is.False);
            // We're decoupling, so should still be running.
            Assert.That(decouplingClock.IsRunning, Is.True);
        }

        [Test]
        public void TestSeekNegativeWhileDecoupling()
        {
            decouplingClock.AllowDecoupling = true;
            Assert.That(decouplingClock.Seek(-1000), Is.True);

            decouplingClock.ProcessFrame();

            Assert.That(source.CurrentTime, Is.EqualTo(0));

            // We're decoupling, so should be able to go beyond zero.
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(-1000));
        }

        [Test]
        public void TestSeekPositiveWhileDecoupling()
        {
            decouplingClock.AllowDecoupling = true;
            Assert.That(decouplingClock.Seek(1000), Is.True);
            decouplingClock.ProcessFrame();

            Assert.That(source.CurrentTime, Is.EqualTo(1000));
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(1000));
        }

        [Test]
        public void TestSeekBeyondLengthWhileDecoupling()
        {
            source = new TestStopwatchClockWithRangeLimit
            {
                MaxTime = 500
            };

            decouplingClock.ChangeSource(source);
            decouplingClock.AllowDecoupling = true;

            Assert.That(decouplingClock.Seek(1000), Is.True);
            decouplingClock.ProcessFrame();

            Assert.That(source.CurrentTime, Is.EqualTo(500));
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(1000));
        }

        [Test]
        public void TestSeekFromNegativeToBeyondLengthWhileDecoupling()
        {
            source = new TestStopwatchClockWithRangeLimit
            {
                MaxTime = 500
            };

            decouplingClock.ChangeSource(source);
            decouplingClock.AllowDecoupling = true;

            decouplingClock.Start();

            Assert.That(decouplingClock.Seek(-1000), Is.True);
            decouplingClock.ProcessFrame();

            Assert.That(source.CurrentTime, Is.EqualTo(0).Within(30));
            Assert.That(source.IsRunning, Is.False);
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(-1000));
            Assert.That(decouplingClock.IsRunning, Is.True);

            Assert.That(decouplingClock.Seek(1000), Is.True);
            decouplingClock.ProcessFrame();

            Assert.That(source.CurrentTime, Is.EqualTo(500));
            Assert.That(source.IsRunning, Is.False);
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(1000).Within(30));
            Assert.That(decouplingClock.IsRunning, Is.True);
        }

        /// <summary>
        /// In decoupled operation, seeking the source while it's not playing is undefined
        /// behaviour.
        /// </summary>
        [Test]
        public void TestSeekFromSourceWhileDecoupling()
        {
            decouplingClock.AllowDecoupling = true;

            Assert.That(source.CurrentTime, Is.EqualTo(0));
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(0));

            source.Seek(1000);

            Assert.That(source.CurrentTime, Is.EqualTo(1000));
            // One might expect this to match the source, but with the current implementation, it doesn't.
            Assert.That(decouplingClock.CurrentTime, Is.Not.EqualTo(1000));

            // One should seek the decoupling clock directly.
            decouplingClock.Seek(1000);
            decouplingClock.ProcessFrame();

            Assert.That(source.CurrentTime, Is.EqualTo(1000));
            Assert.That(decouplingClock.CurrentTime, Is.EqualTo(1000));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestStartFromNegativeTimeIncrementsCorrectly(bool seekBeforeStart)
        {
            // Intentionally wait some time to allow the reference clock to
            // build up some elapsed difference.
            //
            // We want to make sure that this isn't all applied at once causing a large jump.
            Thread.Sleep(500);

            decouplingClock.AllowDecoupling = true;

            if (seekBeforeStart)
            {
                decouplingClock.Seek(-300);
                decouplingClock.Start();
            }
            else
            {
                decouplingClock.Start();
                decouplingClock.Seek(-300);
            }

            decouplingClock.ProcessFrame();

            Assert.That(source.IsRunning, Is.False);
            Assert.That(source.CurrentTime, Is.EqualTo(0));

            double time = decouplingClock.CurrentTime;

            Assert.That(decouplingClock.IsRunning, Is.True);
            Assert.That(decouplingClock.CurrentTime, Is.LessThan(0));

            Thread.Sleep(100);

            decouplingClock.ProcessFrame();
            Assert.That(decouplingClock.CurrentTime, Is.LessThan(0));
            Assert.That(decouplingClock.CurrentTime, Is.GreaterThan(time));
        }

        [Test]
        public void TestBackwardPlaybackOverZeroBoundary()
        {
            source = new TestStopwatchClockWithRangeLimit();
            decouplingClock.ChangeSource(source);
            decouplingClock.AllowDecoupling = true;

            decouplingClock.Seek(300);
            decouplingClock.Rate = -1;
            decouplingClock.Start();

            decouplingClock.ProcessFrame();

            while (source.IsRunning)
            {
                decouplingClock.ProcessFrame();
                Assert.That(decouplingClock.CurrentTime, Is.EqualTo(source.CurrentTime).Within(30));
            }

            Assert.That(source.IsRunning, Is.False);

            double time = decouplingClock.CurrentTime;

            while (decouplingClock.CurrentTime > -300)
            {
                Assert.That(source.IsRunning, Is.False);
                Assert.That(decouplingClock.CurrentTime, Is.LessThanOrEqualTo(time));
                time = decouplingClock.CurrentTime;

                decouplingClock.ProcessFrame();
            }
        }

        [Test]
        public void TestForwardPlaybackOverZeroBoundary()
        {
            source = new TestStopwatchClockWithRangeLimit();
            decouplingClock.ChangeSource(source);
            decouplingClock.AllowDecoupling = true;

            decouplingClock.Seek(-300);
            decouplingClock.Start();

            decouplingClock.ProcessFrame();

            double time = decouplingClock.CurrentTime;

            while (decouplingClock.CurrentTime < 0)
            {
                Assert.That(source.IsRunning, Is.False);
                Assert.That(decouplingClock.CurrentTime, Is.GreaterThanOrEqualTo(time));
                time = decouplingClock.CurrentTime;

                decouplingClock.ProcessFrame();
            }

            Assert.That(source.CurrentTime, Is.EqualTo(decouplingClock.CurrentTime).Within(30));
            Assert.That(source.IsRunning, Is.True);

            // Subsequently test stop/start works correctly.
            decouplingClock.Stop();
            decouplingClock.ProcessFrame();
            Assert.That(decouplingClock.IsRunning, Is.False);
            Assert.That(source.IsRunning, Is.False);

            decouplingClock.Start();
            decouplingClock.ProcessFrame();
            Assert.That(decouplingClock.IsRunning, Is.True);
            Assert.That(source.IsRunning, Is.True);
        }

        [Test]
        public void TestForwardPlaybackOverLengthBoundary()
        {
            source = new TestStopwatchClockWithRangeLimit
            {
                MaxTime = 10000
            };

            decouplingClock.ChangeSource(source);
            decouplingClock.AllowDecoupling = true;

            decouplingClock.Seek(9800);
            decouplingClock.Start();

            decouplingClock.ProcessFrame();

            double time = decouplingClock.CurrentTime;
            const double tolerance = 30;

            // The decoupling clock generally lags behind the source clock,
            // so we don't want the threshold here to go up to the full tolerance,
            // to avoid situations like so:
            //
            // x: decouplingClock
            // o: sourceClock
            //
            // ------x-----------o------>
            //    9980ms      10000ms
            //
            // The source clock has reached its playback limit and cannot seek further, so it will stop.
            // The decoupling clock hasn't caught up to the source clock yet, but it is close enough to pass the tolerance check.
            //
            // Subtracting the tolerance ensures that both the decoupling and source clocks stay in the same 30ms band, but neither stops yet.
            // We will assert that the source should eventually stop further down anyway.
            while (decouplingClock.CurrentTime < 10000 - tolerance)
            {
                Assert.That(source.IsRunning, Is.True);
                Assert.That(source.CurrentTime, Is.EqualTo(decouplingClock.CurrentTime).Within(30));
                Assert.That(decouplingClock.CurrentTime, Is.GreaterThanOrEqualTo(time));
                time = decouplingClock.CurrentTime;

                decouplingClock.ProcessFrame();
            }

            while (source.IsRunning)
                decouplingClock.ProcessFrame();

            Assert.That(source.IsRunning, Is.False);
            Assert.That(decouplingClock.CurrentTime, Is.LessThan(10100));

            while (decouplingClock.CurrentTime < 10200)
            {
                Assert.That(decouplingClock.IsRunning, Is.True);
                Assert.That(decouplingClock.CurrentTime, Is.GreaterThanOrEqualTo(time));
                time = decouplingClock.CurrentTime;

                decouplingClock.ProcessFrame();
            }

            Assert.That(source.IsRunning, Is.False);
        }

        [Test]
        public void TestPlayDifferentSourceAfterSeekFailure()
        {
            decouplingClock.AllowDecoupling = true;

            var firstSource = (TestClockWithRange)source;
            firstSource.MaxTime = 100;

            decouplingClock.Seek(1000);

            Assert.That(firstSource.IsRunning, Is.False);

            var secondSource = new TestClockWithRange();

            decouplingClock.ChangeSource(secondSource);
            decouplingClock.Start();

            Assert.That(secondSource.IsRunning, Is.True);
        }

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
