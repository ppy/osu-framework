// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneWaveform : FrameworkTestScene
    {
        private BasicButton button;
        private Track track;
        private Waveform waveform;
        private Container<Drawable> waveformContainer;
        private readonly Bindable<float> zoom = new BindableFloat(1) { MinValue = 0.1f, MaxValue = 20 };

        [BackgroundDependencyLoader]
        private void load(Game game, AudioManager audio)
        {
            var store = audio.GetTrackStore(game.Resources);

            const string track_name = "Tracks/sample-track.mp3";

            track = store.Get(track_name);
            waveform = new Waveform(store.GetStream(track_name));

            const float track_width = 1366; // required because RelativeSizeAxes.X doesn't seem to work with horizontal scroll

            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Children = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Y,
                        RelativeSizeAxes = Axes.X,
                        Spacing = new Vector2(10),
                        Children = new Drawable[]
                        {
                            button = new BasicButton
                            {
                                Text = "Start",
                                Size = new Vector2(100, 50),
                                BackgroundColour = Color4.DarkSlateGray,
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Action = startStop
                            },
                            new SpriteText
                            {
                                Text = "Zoom Level:",
                                Origin = Anchor.CentreLeft,
                                Anchor = Anchor.CentreLeft,
                            },
                            new BasicSliderBar<float>
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Size = new Vector2(200, 40),
                                Current = zoom
                            },
                        },
                    },
                    new BasicScrollContainer(Direction.Horizontal)
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = waveformContainer = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Y,
                            Width = track_width,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 10)
                        }
                    }
                }
            };

            zoom.ValueChanged += e => waveformContainer.Width = track_width * e.NewValue;
        }

        [TestCase(1f)]
        [TestCase(1f / 2)]
        [TestCase(1f / 4)]
        [TestCase(1f / 8)]
        [TestCase(1f / 16)]
        [TestCase(0)]
        public void TestResolution(float resolution)
        {
            TestWaveform graph = null;

            AddStep("create waveform", () => waveformContainer.Child = graph = new TestWaveform(track, resolution) { Waveform = waveform });
            AddUntilStep("wait for load", () => graph.Regenerated);
        }

        [Test]
        public void TestNullWaveform()
        {
            TestWaveform graph = null;

            AddStep("create waveform", () => waveformContainer.Child = graph = new TestWaveform(track, 1) { Waveform = new Waveform(null) });
            AddUntilStep("wait for load", () => graph.Regenerated);
        }

        [Test]
        public void TestWaveformAlpha()
        {
            TestWaveform graph = null;

            AddStep("create waveform", () => waveformContainer.Child = graph = new TestWaveform(track, 1)
            {
                Waveform = waveform,
                Alpha = 0.5f,
            });

            AddUntilStep("wait for load", () => graph.Regenerated);

            AddUntilStep("wait for sampling", () => graph.Waveform.GetPoints().Count > 0);
        }

        private void startStop()
        {
            if (track.IsRunning)
            {
                track.Stop();
                button.Text = "Start";
            }
            else
            {
                track.Start();
                button.Text = "Stop";
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            track?.Stop();
        }

        private class TestWaveform : CompositeDrawable
        {
            private readonly Track track;
            private readonly TestWaveformGraph graph;
            private readonly Drawable marker;

            public TestWaveform(Track track, float resolution)
            {
                this.track = track;

                RelativeSizeAxes = Axes.X;
                Height = 100;

                InternalChildren = new[]
                {
                    graph = new TestWaveformGraph
                    {
                        RelativeSizeAxes = Axes.Both,
                        Resolution = resolution,
                        BaseColour = new Color4(232, 78, 6, 255),
                        LowColour = new Color4(255, 232, 100, 255),
                        MidColour = new Color4(255, 153, 19, 255),
                        HighColour = new Color4(255, 46, 7, 255),
                    },
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Black,
                                Alpha = 0.75f
                            },
                            new SpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Padding = new MarginPadding(4),
                                Text = $"Resolution: {resolution:0.00}"
                            }
                        }
                    },
                    marker = new Box
                    {
                        RelativeSizeAxes = Axes.Y,
                        RelativePositionAxes = Axes.X,
                        Width = 2,
                        Colour = Color4.Blue
                    },
                };
            }

            public bool Regenerated => graph.Regenerated;

            public Waveform Waveform
            {
                get => graph.Waveform;
                set => graph.Waveform = value;
            }

            protected override void Update()
            {
                base.Update();

                if (track.IsLoaded)
                    marker.X = (float)(track.CurrentTime / track.Length);
            }

            private bool mouseDown;

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                mouseDown = true;
                seekTo(ToLocalSpace(e.ScreenSpaceMousePosition).X);
                return true;
            }

            protected override void OnMouseUp(MouseUpEvent e)
            {
                mouseDown = false;
            }

            protected override bool OnMouseMove(MouseMoveEvent e)
            {
                if (mouseDown)
                {
                    seekTo(ToLocalSpace(e.ScreenSpaceMousePosition).X);
                    return true;
                }

                return false;
            }

            private void seekTo(float x)
            {
                track.Seek(x / DrawWidth * track.Length);
            }
        }

        private class TestWaveformGraph : WaveformGraph
        {
            public bool Regenerated { get; private set; }

            protected override void OnWaveformRegenerated(Waveform waveform)
            {
                base.OnWaveformRegenerated(waveform);
                Regenerated = true;
            }
        }
    }
}
