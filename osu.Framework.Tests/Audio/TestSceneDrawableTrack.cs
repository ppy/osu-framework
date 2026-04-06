// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using JetBrains.Annotations;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;
using osu.Framework.Utils;

namespace osu.Framework.Tests.Audio
{
    [HeadlessTest]
    public partial class TestSceneDrawableTrack : FrameworkTestScene
    {
        [Resolved]
        private ITrackStore trackStore { get; set; }

        private DrawableTrack track;

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            Child = track = new DrawableTrack(trackStore.Get("sample-track"));
        });

        [Test]
        public void TestVolumeResetWhenReset()
        {
            AddStep("set volume to 0", () => track.Volume.Value = 0);
            AddAssert("track volume is 0", () => Precision.AlmostEquals(0, track.AggregateVolume.Value));

            AddStep("reset", () => track.Reset());
            AddAssert("track volume is 1", () => Precision.AlmostEquals(1, track.AggregateVolume.Value));
        }

        [Test]
        public void TestAdjustmentsRemovedWhenReset()
        {
            // ReSharper disable once NotAccessedVariable
            // Bindable - need to store the reference.
            BindableDouble volumeAdjustment = null;

            AddStep("add adjustment", () => track.AddAdjustment(AdjustableProperty.Volume, volumeAdjustment = new BindableDouble { Value = 0.5 }));
            AddAssert("track volume is 0.5", () => Precision.AlmostEquals(0.5, track.AggregateVolume.Value));

            AddStep("reset", () => track.Reset());
            AddAssert("track volume is 1", () => Precision.AlmostEquals(1, track.Volume.Value));
        }

        [Test]
        public void TestTrackingParentAdjustmentChangedWhenMovedParents()
        {
            AddStep("set volume 1", () => track.Volume.Value = 1);

            AddStep("move track a container with 0 volume", () =>
            {
                Remove(track, false);
                Child = new AudioContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Volume = { Value = 0 },
                    Child = track
                };
            });

            AddAssert("track has 0 volume", () => track.AggregateVolume.Value == 0);
        }

        // this test attempts to exercise that `DrawableTrack` is safe to initialise in BDL via a specific sequence of steps.
        // while it may seem farfetched, there are reasonable game-side usages that may hit this same pattern
        // (example: https://github.com/ppy/osu/blob/5b6d2155835d2682d2599b4dc5c7b3a2e61b73a8/osu.Game/Screens/OnlinePlay/Matchmaking/RankedPlay/Components/BackgroundMusicManager.cs).
        //
        // if this test is going to fail, it will fail in single thread mode.
        [Test]
        public void TestAsyncLoadComponentWithTrack()
        {
            var component = new ComponentWithTrack();

            AddStep("create component with track", () =>
            {
                // async load our `ComponentWithTrack`.
                // this is important because we want to provoke `TrackBass`'s ctor to be actually enqueued for execution on audio thread later
                // and not accidentally inlined due to being on update or audio thread.
                LoadComponentAsync(component, t => Child = t);

                // with the async load started, attempt to start the track as soon as it's initialised in the component.
                // notably we're on the update thread here.
                // the expectation is that all track initialisation logic runs *before* the track start is attempted.
                while (!component.StartTrack())
                {
                }
            });
            AddUntilStep("wait for running", () => component.IsRunning);
        }

        private partial class ComponentWithTrack : CompositeDrawable
        {
            [CanBeNull]
            private DrawableTrack track;

            public bool IsRunning => track?.IsRunning == true;

            [BackgroundDependencyLoader]
            private void load(ITrackStore trackStore)
            {
                AddInternal(track = new DrawableTrack(trackStore.Get("sample-track")));
            }

            public bool StartTrack()
            {
                if (track == null)
                    return false;

                track.Start();
                return true;
            }
        }
    }
}
