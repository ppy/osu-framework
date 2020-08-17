// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;
using osu.Framework.Timing;

namespace osu.Framework.Tests
{
    [HeadlessTest]
    public class TestSceneDrawableAudioWrapper : FrameworkTestScene
    {
        private IAdjustableClock audioClock;
        private AudioContainer<DrawableAudioWrapper> audioContainer;

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            Child = audioContainer = new AudioContainer<DrawableAudioWrapper>
            {
                Clock = new FramedClock(audioClock = new StopwatchClock(true)),
                Frequency = { Value = 2f },
                Children = new DrawableAudioWrapper[]
                {
                    createTestTrack(0.75f),
                    createTestSample(1.50f),
                    new AudioContainer { Frequency = { Value = 0f } },
                }
            };

            Assert.IsTrue(checkAllComponentsFrequencies(audioContainer, 1f));
        });

        [Test]
        public void TestParentAdjustsAutomaticallyBoundToChildren()
        {
            AddStep("change parent volume", () => audioContainer.Volume.Value = 0.5f);
            AddAssert("children volume adjusted", () => audioContainer.All(c => c.AggregateVolume.Value == 0.5f));
            AddStep("change volume back", () => audioContainer.Volume.Value = 1f);
            AddAssert("children volume adjusted back", () => audioContainer.All(c => c.AggregateVolume.Value == 1f));
        }

        [Test]
        public void TestZeroFrequencyOnClockStop()
        {
            AddStep("stop audio clock", () => audioClock.Stop());
            AddAssert("all component frequency set zero", () => checkAllComponentsFrequencies(audioContainer, 0f));

            // pick first audio component and ensure its public aggregate frequency is still non-zero,
            // clock state frequency adjustment is applied internally to their underlying components.
            AddAssert("clock adjustment not publicly exposed", () => audioContainer.First().AggregateFrequency.Value != 0);
        }

        [Test]
        public void TestClockSpeedNotAffectingFrequency()
        {
            AddStep("change audio clock rate", () => audioClock.Rate = 1.25f);
            AddAssert("frequency remains unchanged", () => checkAllComponentsFrequencies(audioContainer, 1f));
        }

        [Test]
        public void TestParentClockAdjustsNotAffectingIndependents()
        {
            IAdjustableClock independentClock = null;
            AudioContainer<DrawableAudioWrapper> independentContainer = null;

            AddStep("add independent container", () =>
            {
                audioContainer.Add(independentContainer = new AudioContainer<DrawableAudioWrapper>
                {
                    Frequency = { Value = 0.25f },
                    Clock = new FramedClock(independentClock = new StopwatchClock(true)),
                    Children = new DrawableAudioWrapper[]
                    {
                        createTestTrack(-2.0f),
                        createTestSample(1f),
                    }
                });
            });

            AddStep("stop parent clock", () => audioClock.Stop());
            AddAssert("ensure parent frequency set zero", () => checkAllComponentsFrequencies(audioContainer, 0f));
            AddAssert("independent container remain unchanged", () => checkAllComponentsFrequencies(independentContainer, 1f));

            AddStep("start parent clock", () => audioClock.Start());
            AddStep("stop independent clock", () => independentClock.Stop());
            AddAssert("ensure parent frequency back one", () => checkAllComponentsFrequencies(audioContainer, 1f));
            AddAssert("independent container frequency set zero", () => checkAllComponentsFrequencies(independentContainer, 0f));
        }

        /// <summary>
        /// Checks if the audio hierarchy of a container match up with the <paramref name="expectedInternalFrequencyAdjustment"/>.
        /// </summary>
        /// <param name="container">The container to check for.</param>
        /// <param name="expectedInternalFrequencyAdjustment">The expected internal frequency adjustment, this is multiplied by the wrapper's <see cref="DrawableAudioWrapper.AggregateFrequency"/> for each to check.</param>
        /// <returns>Whether all frequencies match up with the expected value.</returns>
        private bool checkAllComponentsFrequencies(AudioContainer<DrawableAudioWrapper> container, double expectedInternalFrequencyAdjustment)
        {
            var children = container.ChildrenOfType<DrawableAudioWrapper>()
                                    .Where(c => c.Clock == container.Clock)
                                    .OfType<IComponentAggregateAdjustment>();

            return children.All(daw => daw.ComponentFrequency.Value == daw.AggregateFrequency.Value * expectedInternalFrequencyAdjustment);
        }

        private TestDrawableTrack createTestTrack(double frequency) => new TestDrawableTrack(new TrackVirtual(0)) { Frequency = { Value = frequency } };
        private TestDrawableSample createTestSample(double frequency) => new TestDrawableSample(new SampleChannelVirtual()) { Frequency = { Value = frequency } };

        private interface IComponentAggregateAdjustment : IAggregateAudioAdjustment
        {
            IBindable<double> ComponentFrequency { get; }
        }

        private class TestDrawableTrack : DrawableTrack, IComponentAggregateAdjustment
        {
            private readonly Track track;

            public IBindable<double> ComponentFrequency => track.AggregateFrequency;

            public TestDrawableTrack(Track track, bool disposeTrackOnDisposal = true)
                : base(track, disposeTrackOnDisposal)
            {
                this.track = track;
            }
        }

        private class TestDrawableSample : DrawableSample, IComponentAggregateAdjustment
        {
            private readonly SampleChannel channel;

            public IBindable<double> ComponentFrequency => channel.AggregateFrequency;

            public TestDrawableSample(SampleChannel channel, bool disposeChannelOnDisposal = true)
                : base(channel, disposeChannelOnDisposal)
            {
                this.channel = channel;
            }
        }
    }
}
