// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Timing;

namespace osu.Framework.Tests.NonVisual
{
    [TestFixture]
    public class TestDecoupleableClock
    {
        private TestClock source;
        private DecoupleableInterpolatingFramedClock decoupleable;

        [SetUp]
        public void SetUp()
        {
            source = new TestClock();

            decoupleable = new DecoupleableInterpolatingFramedClock();
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

#endregion

#region Offset start

        /// <summary>
        /// Tests that the coupled clock seeks to the correct position when the source clock starts.
        /// </summary>
        [Test]
        public void TestCoupledStartBySourceWithSourceOffset()
        {
            const double expected_time = 1000;

            source.Seek(expected_time);

            source.Start();
            decoupleable.ProcessFrame();

            Assert.AreEqual(expected_time, decoupleable.CurrentTime, "Coupled time should match source time.");
        }

        /// <summary>
        /// Tests that the coupled clock seeks the source to its time when it starts.
        /// </summary>
        [Test]
        public void TestCoupledStartWithSouceOffset()
        {
            const double expected_time = 0;

            source.Seek(1000);
            decoupleable.Start();

            Assert.AreEqual(expected_time, decoupleable.CurrentTime, "Coupled time should match source time.");
        }

#endregion

#region Seeking

        /// <summary>
        /// Tests that the source clock is seeked when the coupled clock is seeked.
        /// </summary>
        [Test]
        public void TestSourceSeekedByCoupledSeek()
        {
            const double expected_time = 1000;

            decoupleable.Seek(expected_time);

            Assert.AreEqual(expected_time, source.CurrentTime, "Source time should match coupled time.");
        }

        /// <summary>
        /// Tests that the coupled clock is seeked when the source clock is seeked.
        /// </summary>
        [Test]
        public void TestCoupledSeekedBySourceSeek()
        {
            const double expected_time = 1000;

            decoupleable.Start();

            source.Seek(expected_time);
            decoupleable.ProcessFrame();

            Assert.AreEqual(expected_time, decoupleable.CurrentTime, "Coupled time should match source time.");
        }

#endregion

        private class TestClock : IAdjustableClock
        {
            public double CurrentTime { get; set; }
            public double Rate { get; set; }

            private bool isRunning;
            public bool IsRunning => isRunning;

            public void Reset() => throw new System.NotImplementedException();
            public void Start() => isRunning = true;
            public void Stop() => isRunning = false;

            public bool Seek(double position)
            {
                CurrentTime = position;
                return true;
            }

            public void ResetSpeedAdjustments() => throw new System.NotImplementedException();
        }
    }
}
