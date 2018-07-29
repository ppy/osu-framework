// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.EventArgs;
using osu.Framework.Input.States;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseWaveform : FrameworkTestCase
    {
        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(Waveform),
            typeof(WaveformGraph),
            typeof(DataStreamFileProcedures)
        };

        private Button button;
        private TrackBass track;
        private SliderBar<float> zoomSlider;
        private readonly Bindable<float> zoom = new BindableFloat(1) { MinValue = 0.1f, MaxValue = 20 };

        [BackgroundDependencyLoader]
        private void load(Game game, AudioManager audio)
        {
            track = new TrackBass(game.Resources.GetStream("Tracks/sample-track.mp3"));
            audio.Track.AddItem(track);

            var waveform = new Waveform(game.Resources.GetStream("Tracks/sample-track.mp3"));

            FillFlowContainer flow;

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
                            button = new Button
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
                            zoomSlider = new BasicSliderBar<float>
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Size = new Vector2(200, 40)
                            },
                        },
                    },
                    new ScrollContainer(Direction.Horizontal)
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = flow = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Y,
                            Width = track_width,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 10)
                        }
                    }
                }
            };

            for (int i = 1; i <= 16; i *= 2)
                flow.Add(new TestWaveform(track, 1f / i) { Waveform = waveform });

            zoomSlider.Current.BindTo(zoom);
            zoomSlider.Current.ValueChanged += v => flow.Width = track_width * v;
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
            private readonly WaveformGraph graph;
            private readonly Drawable marker;

            public TestWaveform(Track track, float resolution)
            {
                this.track = track;

                RelativeSizeAxes = Axes.X;
                Height = 100;

                InternalChildren = new[]
                {
                    graph = new WaveformGraph
                    {
                        RelativeSizeAxes = Axes.Both,
                        Resolution = resolution,
                        Colour = new Color4(232, 78, 6, 255),
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

            public Waveform Waveform
            {
                set => graph.Waveform = value;
            }

            protected override void Update()
            {
                base.Update();

                if (track.IsLoaded)
                    marker.X = (float)(track.CurrentTime / track.Length);
            }

            private bool mouseDown;

            protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
            {
                mouseDown = true;
                seekTo(ToLocalSpace(state.Mouse.NativeState.Position).X);
                return true;
            }

            protected override bool OnMouseUp(InputState state, MouseUpEventArgs args)
            {
                mouseDown = false;
                return true;
            }

            protected override bool OnMouseMove(InputState state)
            {
                if (mouseDown)
                {
                    seekTo(ToLocalSpace(state.Mouse.NativeState.Position).X);
                    return true;
                }

                return false;
            }

            private void seekTo(float x)
            {
                track.Seek(x / DrawWidth * track.Length);
            }
        }
    }
}
