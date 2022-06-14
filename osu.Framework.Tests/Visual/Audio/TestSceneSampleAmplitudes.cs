// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Tests.Visual.Audio
{
    public class TestSceneSampleAmplitudes : FrameworkTestScene
    {
        private Box leftChannel;
        private Box rightChannel;

        private DrawableSample sample;
        private SampleChannel channel;

        private Container amplitudeBoxes;

        [BackgroundDependencyLoader]
        private void load(ISampleStore samples)
        {
            Children = new Drawable[]
            {
                sample = new DrawableSample(samples.Get("long.mp3")),
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
                                    Enumerable.Range(0, ChannelAmplitudes.AMPLITUDES_SIZE)
                                              .Select(i => new Box
                                              {
                                                  RelativeSizeAxes = Axes.Both,
                                                  RelativePositionAxes = Axes.X,
                                                  Anchor = Anchor.BottomLeft,
                                                  Origin = Anchor.BottomLeft,
                                                  Width = 1f / ChannelAmplitudes.AMPLITUDES_SIZE,
                                                  X = (float)i / ChannelAmplitudes.AMPLITUDES_SIZE
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

            AddStep("start sample", () =>
            {
                channel = sample.Play();
                channel.Looping = true;
            });

            AddStep("stop sample", () => channel.Stop());
        }

        protected override void Update()
        {
            base.Update();

            if (channel == null)
                return;

            var amplitudes = channel.CurrentAmplitudes;

            rightChannel.Width = amplitudes.RightChannel * 0.5f;
            leftChannel.Width = amplitudes.LeftChannel * 0.5f;

            var freqAmplitudes = amplitudes.FrequencyAmplitudes.Span;

            for (int i = 0; i < freqAmplitudes.Length; i++)
                amplitudeBoxes[i].Height = freqAmplitudes[i];
        }
    }
}
