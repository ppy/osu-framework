// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using NUnit.Framework;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Audio;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;

namespace osu.Framework.Tests.Audio
{
    [HeadlessTest]
    public partial class DrawableAudioWrapperTest : FrameworkTestScene
    {
        [Test]
        public void TestAdjustmentsPreservedOnOwnedComponentAfterRemoval()
        {
            TrackVirtual track = null!;
            SlowDisposingDrawableAudioWrapper wrapper = null!;

            AddStep("add slow disposing wrapper for owned track",
                () => Child = wrapper = new SlowDisposingDrawableAudioWrapper(track = new TrackVirtual(1000)));
            AddStep("mute wrapper", () => wrapper.AddAdjustment(AdjustableProperty.Volume, new BindableDouble()));
            AddStep("expire wrapper", () => wrapper.Expire());
            AddAssert("component still muted", () => track.AggregateVolume.Value, () => Is.EqualTo(0));
            AddStep("allow disposal to complete", () => wrapper.AllowDisposal.Set());
        }

        [Test]
        public void TestAdjustmentsRevertedOnOwnedComponentAfterRemoval()
        {
            TrackVirtual track = null!;
            SlowDisposingDrawableAudioWrapper wrapper = null!;

            AddStep("add slow disposing wrapper for non-owned track",
                () => Child = wrapper = new SlowDisposingDrawableAudioWrapper(track = new TrackVirtual(1000), false));
            AddStep("mute wrapper", () => wrapper.AddAdjustment(AdjustableProperty.Volume, new BindableDouble()));
            AddStep("expire wrapper", () => wrapper.Expire());
            AddAssert("component unmuted", () => track.AggregateVolume.Value, () => Is.EqualTo(1));
            AddStep("allow disposal to complete", () => wrapper.AllowDisposal.Set());
        }

        private partial class SlowDisposingDrawableAudioWrapper : DrawableAudioWrapper
        {
            public ManualResetEvent AllowDisposal { get; private set; } = new ManualResetEvent(false);

            public SlowDisposingDrawableAudioWrapper(IAdjustableAudioComponent component, bool disposeUnderlyingComponentOnDispose = true)
                : base(component, disposeUnderlyingComponentOnDispose)
            {
            }

            protected override void Dispose(bool isDisposing)
            {
                AllowDisposal.WaitOne(10_000);

                base.Dispose(isDisposing);
            }
        }
    }
}
