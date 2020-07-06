// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        private DrawableSample sample;

        private Box leftChannel;
        private Box rightChannel;

        private SampleChannelBass bassSample;

        private Container amplitudeBoxes;

        [BackgroundDependencyLoader]
        private void load(ISampleStore samples)
        {
            bassSample = (SampleChannelBass)samples.Get("long.mp3");

            var length = bassSample.CurrentAmplitudes.FrequencyAmplitudes.Length;

            Children = new Drawable[]
            {
                sample = new DrawableSample(bassSample),
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

            sample.Looping = true;
            AddStep("start sample", () => sample.Play());
            AddStep("stop sample", () => sample.Stop());
        }

        protected override void Update()
        {
            base.Update();

            var amplitudes = bassSample.CurrentAmplitudes;

            rightChannel.Width = amplitudes.RightChannel * 0.5f;
            leftChannel.Width = amplitudes.LeftChannel * 0.5f;

            var freqAmplitudes = amplitudes.FrequencyAmplitudes.Span;

            for (int i = 0; i < freqAmplitudes.Length; i++)
                amplitudeBoxes[i].Height = freqAmplitudes[i];
        }
    }
}
