// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Tests.Visual.Audio
{
    public partial class TestSceneTrackAmplitudes : FrameworkTestScene
    {
        private DrawableTrack drawableTrack;

        private Box leftChannel;
        private Box rightChannel;

        private Track track;

        private Container amplitudeBoxes;

        [BackgroundDependencyLoader]
        private void load(ITrackStore tracks)
        {
            track = tracks.Get("sample-track.mp3");
            int length = track.CurrentAmplitudes.FrequencyAmplitudes.Length;

            Children = new Drawable[]
            {
                drawableTrack = new DrawableTrack(track),
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    leftChannel = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.CentreRight,
                                    },
                                    rightChannel = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.CentreLeft,
                                    }
                                }
                            },
                        },
                        new Drawable[]
                        {
                            amplitudeBoxes = new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                ChildrenEnumerable =
                                    Enumerable.Range(0, length)
                                              .Select(i => new Box
                                              {
                                                  RelativeSizeAxes = Axes.Both,
                                                  RelativePositionAxes = Axes.X,
                                                  Anchor = Anchor.BottomLeft,
                                                  Origin = Anchor.BottomLeft,
                                                  Width = 1f / length,
                                                  X = (float)i / length
                                              })
                            },
                        }
                    }
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            drawableTrack.Looping = true;
            AddStep("start track", () => drawableTrack.Start());
            AddStep("stop track", () => drawableTrack.Stop());
        }

        protected override void Update()
        {
            base.Update();

            var amplitudes = track.CurrentAmplitudes;

            rightChannel.Width = amplitudes.RightChannel * 0.5f;
            leftChannel.Width = amplitudes.LeftChannel * 0.5f;

            var freqAmplitudes = amplitudes.FrequencyAmplitudes.Span;

            for (int i = 0; i < freqAmplitudes.Length; i++)
                amplitudeBoxes[i].Height = freqAmplitudes[i];
        }
    }
}
