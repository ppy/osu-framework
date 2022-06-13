// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Audio
{
    public class TestSceneSamples : FrameworkTestScene
    {
        private readonly AudioContainer samples;
        private readonly TrackingLine tracking;

        private const int beats = 8;
        private const int notes = 16;

        public TestSceneSamples()
        {
            Children = new Drawable[]
            {
                new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Size = new Vector2(0.95f),
                    FillMode = FillMode.Fit,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Colour = Color4.Blue,
                            RelativeSizeAxes = Axes.Both,
                        },
                        new Grid(beats - 1, notes),
                        samples = new AudioContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            RelativeChildSize = new Vector2(beats - 1, notes),
                            Children = new Drawable[]
                            {
                                new DraggableSample(0, 0),
                                new DraggableSample(1, 2),
                                new DraggableSample(2, 4),
                                new DraggableSample(3, 5),
                                new DraggableSample(4, 8),
                                new DraggableSample(5, 11),
                                new DraggableSample(6, 14),
                                new DraggableSample(7, 16),
                                tracking = new TrackingLine()
                            }
                        },
                    }
                },
            };

            AddStep("reduce volume", () => samples.VolumeTo(samples.Volume.Value - 0.5f, 1000, Easing.OutQuint));
            AddStep("increase volume", () => samples.VolumeTo(samples.Volume.Value + 0.5f, 1000, Easing.OutQuint));

            AddStep("reduce frequency", () => samples.FrequencyTo(samples.Frequency.Value - 0.1f, 1000, Easing.OutQuint));
            AddStep("increase frequency", () => samples.FrequencyTo(samples.Frequency.Value + 0.1f, 1000, Easing.OutQuint));

            AddStep("left balance", () => samples.BalanceTo(samples.Balance.Value - 1, 1000, Easing.OutQuint));
            AddStep("right balance", () => samples.BalanceTo(samples.Balance.Value + 1, 1000, Easing.OutQuint));
        }

        protected override void Update()
        {
            base.Update();

            if (tracking.X > beats)
            {
                tracking.X = 0;
                samples.OfType<DraggableSample>().ForEach(s => s.Reset());
            }
            else
            {
                tracking.X += (float)Clock.ElapsedFrameTime / 500;
                samples.OfType<DraggableSample>().Where(s => !s.Played && s.X <= tracking.X).ForEach(s => s.Play());
            }
        }

        private class TrackingLine : CompositeDrawable
        {
            public TrackingLine()
            {
                RelativePositionAxes = Axes.Both;
                RelativeSizeAxes = Axes.Y;
                Size = new Vector2(4, notes);
                Colour = Color4.SkyBlue;

                Blending = BlendingParameters.Additive;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.White,
                        RelativeSizeAxes = Axes.Both,
                    },
                };
            }
        }

        public class Grid : CompositeDrawable
        {
            public Grid(int beats, int notes)
            {
                RelativeSizeAxes = Axes.Both;

                for (int i = 0; i <= beats; i++)
                {
                    AddInternal(new Box
                    {
                        RelativePositionAxes = Axes.Both,
                        RelativeSizeAxes = Axes.Y,
                        Width = 1,
                        Colour = Color4.White,
                        X = (float)i / beats
                    });
                }

                for (int i = 0; i <= notes; i++)
                {
                    AddInternal(new Box
                    {
                        RelativePositionAxes = Axes.Both,
                        RelativeSizeAxes = Axes.X,
                        Height = 1,
                        Colour = Color4.White,
                        Y = (float)i / notes
                    });
                }
            }
        }

        private class DraggableSample : CompositeDrawable
        {
            public DraggableSample(int beat, int pitch)
            {
                RelativePositionAxes = Axes.Both;

                Position = new Vector2(beat, pitch);
                Size = new Vector2(16);

                Origin = Anchor.Centre;

                InternalChildren = new Drawable[]
                {
                    circle = new Circle
                    {
                        Colour = Color4.Yellow,
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                };
            }

            public bool Played { get; private set; }

            [BackgroundDependencyLoader]
            private void load(ISampleStore samples)
            {
                AddInternal(sample = new DrawableSample(samples.Get("long.mp3")));
            }

            private DrawableSample sample;

            private readonly Circle circle;

            protected override bool OnDragStart(DragStartEvent e) => true;

            protected override void OnDrag(DragEvent e)
            {
                Y = (int)(e.MousePosition.Y / (Parent.DrawHeight / notes));
            }

            public void Reset()
            {
                Played = false;
            }

            public void Play()
            {
                Played = true;
                circle.ScaleTo(1.8f).ScaleTo(1, 600, Easing.OutQuint);

                var channel = sample.GetChannel();
                channel.Frequency.Value = 1 + Y / notes;
                channel.Play();
            }
        }
    }
}
