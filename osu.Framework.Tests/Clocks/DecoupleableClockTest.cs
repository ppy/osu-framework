// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using NUnit.Framework;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Clocks
{
    [TestFixture]
    public class DecoupleableClockTest
    {
        private TestClock source;
        private TestDecoupleableClock decoupleable;

        [SetUp]
        public void SetUp()
        {
            source = new TestClockPositiveOnly();

            decoupleable = new TestDecoupleableClock();
            decoupleable.ChangeSource(source);
        }

        #region Start/stop by decoupleable

        /// <summary>
        /// Tests that the source clock starts when the coupled clock starts.
        /// </summary>
        [Test]
        public void TestSourceStartedByCoupled()
        {
            decoupleable.Start();

            Assert.IsTrue(source.IsRunning, "Source should be running.");
        }

        /// <summary>
        /// Tests that the source clock stops when the coupled clock stops.
        /// </summary>
        [Test]
        public void TestSourceStoppedByCoupled()
        {
            decoupleable.Start();
            decoupleable.Stop();

            Assert.IsFalse(source.IsRunning, "Source should not be running.");
        }

        /// <summary>
        /// Tests that the source clock starts when the decoupled clock starts.
        /// </summary>
        [Test]
        public void TestSourceStartedByDecoupled()
        {
            decoupleable.IsCoupled = false;
            decoupleable.Start();

            Assert.IsTrue(source.IsRunning, "Source should be running.");
        }

        /// <summary>
        /// Tests that the source clock stops when the decoupled clock stops.
        /// </summary>
        [Test]
        public void TestSourceStoppedByDecoupled()
        {
            decoupleable.Start();

            decoupleable.IsCoupled = false;
            decoupleable.Stop();

            Assert.IsFalse(source.IsRunning, "Source should not be running.");
        }

        #endregion

        #region Start/stop by source

        /// <summary>
        /// Tests that the coupled clock starts when the source clock starts.
        /// </summary>
        [Test]
        public void TestCoupledStartedBySourceClock()
        {
            source.Start();
            decoupleable.ProcessFrame();

            Assert.IsTrue(decoupleable.IsRunning, "Coupled should be running.");
        }

        /// <summary>
        /// Tests that the coupled clock stops when the source clock stops.
        /// </summary>
        [Test]
        public void TestCoupledStoppedBySourceClock()
        {
            decoupleable.Start();

            source.Stop();
            decoupleable.ProcessFrame();

            Assert.IsFalse(decoupleable.IsRunning, "Coupled should not be running.");
        }

        /// <summary>
        /// Tests that the decoupled clock doesn't start when the source clock starts.
        /// </summary>
        [Test]
        public void TestDecoupledNotStartedBySourceClock()
        {
            decoupleable.IsCoupled = false;

            source.Start();
            decoupleable.ProcessFrame();

            Assert.IsFalse(decoupleable.IsRunning, "Decoupled should not be running.");
        }

        /// <summary>
        /// Tests that the decoupled clock doesn't stop when the source clock stops.
        /// </summary>
        [Test]
        public void TestDecoupledNotStoppedBySourceClock()
        {
            decoupleable.Start();
            decoupleable.IsCoupled = false;

            source.Stop();
            decoupleable.ProcessFrame();

            Assert.IsTrue(decoupleable.IsRunning, "Decoupled should be running.");
        }

        #endregion

        #region Offset start

        /// <summary>
        /// Tests that the coupled clock seeks to the correct position when the source clock starts.
        /// </summary>
        [Test]
        public void TestCoupledStartBySourceWithSourceOffset()
        {
            source.Seek(1000);

            source.Start();
            decoupleable.ProcessFrame();

            Assert.AreEqual(source.CurrentTime, decoupleable.CurrentTime, "Coupled time should match source time.");
        }

        /// <summary>
        /// Tests that the coupled clock seeks the source clock to its time when it starts.
        /// </summary>
        [Test]
        public void TestCoupledStartWithSouceOffset()
        {
            source.Seek(1000);
            decoupleable.Start();

            Assert.AreEqual(0, source.CurrentTime);
            Assert.AreEqual(source.CurrentTime, decoupleable.CurrentTime, "Coupled time should match source time.");
        }

        [Test]
        public void TestFromNegativeCoupledMode()
        {
            decoupleable.IsCoupled = true;
            decoupleable.Seek(-1000);

            decoupleable.ProcessFrame();

            Assert.AreEqual(0, source.CurrentTime);
            Assert.AreEqual(0, decoupleable.CurrentTime);
        }

        /// <summary>
        /// Tests that that the decoupled clock does not rewind after the source clock is started as a result of being able to handle a positive time.
        /// </summary>
        [Test]
        public void TestDecoupledTimeDoesNotRewindAfterSourceStarts()
        {
            decoupleable.IsCoupled = false;
            decoupleable.CustomAllowableErrorMilliseconds = 1000;
            decoupleable.Seek(-50);
            decoupleable.ProcessFrame();
            decoupleable.Start();

            // Delay a bit to make sure the clock crosses the 0 boundary
            Thread.Sleep(100);
            decoupleable.ProcessFrame();

            // Make sure that time doesn't rewind
            double last = decoupleable.CurrentTime;
            decoupleable.ProcessFrame();
            Assert.That(decoupleable.CurrentTime, Is.GreaterThanOrEqualTo(last));
        }

        /// <summary>
        /// Tests that the decoupled clock seeks the source clock to its time when it starts.
        /// </summary>
        [Test]
        public void TestDecoupledStartWithSouceOffset()
        {
            decoupleable.IsCoupled = false;

            source.Seek(1000);
            decoupleable.Start();

            Assert.AreEqual(0, source.CurrentTime);
            Assert.AreEqual(source.CurrentTime, decoupleable.CurrentTime, "Deoupled time should match source time.");
        }

        #endregion

        #region Seeking

        /// <summary>
        /// Tests that the source clock is seeked when the coupled clock is seeked.
        /// </summary>
        [Test]
        public void TestSourceSeekedByCoupledSeek()
        {
            decoupleable.Seek(1000);

            Assert.AreEqual(source.CurrentTime, source.CurrentTime, "Source time should match coupled time.");
        }

        /// <summary>
        /// Tests that the coupled clock is seeked when the source clock is seeked.
        /// </summary>
        [Test]
        public void TestCoupledSeekedBySourceSeek()
        {
            decoupleable.Start();

            source.Seek(1000);
            decoupleable.ProcessFrame();

            Assert.AreEqual(source.CurrentTime, decoupleable.CurrentTime, "Coupled time should match source time.");
        }

        /// <summary>
        /// Tests that the source clock is seeked when the decoupled clock is seeked.
        /// </summary>
        [Test]
        public void TestSourceSeekedByDecoupledSeek()
        {
            decoupleable.IsCoupled = false;
            decoupleable.Seek(1000);

            Assert.AreEqual(decoupleable.CurrentTime, source.CurrentTime, "Source time should match coupled time.");
        }

        /// <summary>
        /// Tests that the coupled clock is not seeked while stopped and the source clock is seeked.
        /// </summary>
        [Test]
        public void TestDecoupledNotSeekedBySourceSeekWhenStopped()
        {
            decoupleable.IsCoupled = false;

            source.Seek(1000);
            decoupleable.ProcessFrame();

            Assert.AreEqual(0, decoupleable.CurrentTime);
            Assert.AreNotEqual(source.CurrentTime, decoupleable.CurrentTime, "Coupled time should not match source time.");
        }

        [Test]
        public void TestDecoupledNotSeekedPositivelyByFailedNegativeSeek()
        {
            decoupleable.IsCoupled = false;
            decoupleable.Start();

            decoupleable.Seek(-5000);

            Assert.That(source.IsRunning, Is.False);
            Assert.That(decoupleable.IsRunning, Is.True);
            Assert.That(decoupleable.CurrentTime, Is.LessThan(0));
        }

        #endregion

        /// <summary>
        /// Tests that the state of the decouplable clock is preserved when it is stopped after processing a frame.
        /// </summary>
        [Test]
        public void TestStoppingAfterProcessingFramePreservesState()
        {
            decoupleable.Start();
            source.CurrentTime = 1000;

            decoupleable.ProcessFrame();
            decoupleable.Stop();

            Assert.AreEqual(source.CurrentTime, decoupleable.CurrentTime, decoupleable.AllowableErrorMilliseconds, "Decoupled should match source time.");
        }

        /// <summary>
        /// Tests that the state of the decouplable clock is preserved when it is stopped after having being started by the source clock.
        /// </summary>
        [Test]
        public void TestStoppingAfterStartingBySourcePreservesState()
        {
            source.Start();
            source.CurrentTime = 1000;

            decoupleable.ProcessFrame();
            decoupleable.Stop();

            Assert.AreEqual(source.CurrentTime, decoupleable.CurrentTime, decoupleable.AllowableErrorMilliseconds, "Decoupled should match source time.");
        }

        private class TestDecoupleableClock : DecoupleableInterpolatingFramedClock
        {
            public double? CustomAllowableErrorMilliseconds { get; set; }

            public override double AllowableErrorMilliseconds => CustomAllowableErrorMilliseconds ?? base.AllowableErrorMilliseconds;
        }

        private class TestClockPositiveOnly : TestClock
        {
            public override bool Seek(double position)
            {
                if (position < 0) return false;

                return base.Seek(position);
            }
        }
    }
}
