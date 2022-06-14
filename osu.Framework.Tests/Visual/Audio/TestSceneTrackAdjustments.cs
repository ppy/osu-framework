// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Audio
{
    public class TestSceneTrackAdjustments : FrameworkTestScene
    {
        [BackgroundDependencyLoader]
        private void load(ITrackStore tracks)
        {
            Child = new DraggableAudioContainer
            {
                FillMode = FillMode.Fit,
                Child = new DraggableAudioContainer
                {
                    Child = new DraggableAudioContainer
                    {
                        Child = new TrackPlayer(tracks.Get("sample-track.mp3"))
                    }
                }
            };
        }

        private class TrackPlayer : CompositeDrawable
        {
            public TrackPlayer(Track track)
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                DrawableTrack drawableTrack;

                Size = new Vector2(50);

                Masking = true;
                CornerRadius = 10;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.Yellow,
                        RelativeSizeAxes = Axes.Both,
                    },
                    new SpriteIcon
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Icon = FontAwesome.Solid.VolumeUp,
                        Colour = Color4.Black,
                        Size = new Vector2(40)
                    },
                    drawableTrack = new DrawableTrack(track)
                };

                drawableTrack.Looping = true;
                drawableTrack.Start();
            }
        }

        private class DraggableAudioContainer : Container
        {
            private readonly Container content;

            private readonly AudioContainer audio;

            private readonly SpriteText textLocal;
            private readonly SpriteText textAggregate;

            private readonly Box volFill;
            private readonly Container warpContent;
            private readonly SpriteIcon spinner;

            protected override Container<Drawable> Content => content;

            public DraggableAudioContainer()
            {
                Size = new Vector2(0.8f);
                RelativeSizeAxes = Axes.Both;

                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                InternalChildren = new Drawable[]
                {
                    warpContent = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                Colour = Color4.DarkGray,
                                Alpha = 0.5f,
                                RelativeSizeAxes = Axes.Both,
                            },
                            volFill = new Box
                            {
                                Anchor = Anchor.BottomLeft,
                                Origin = Anchor.BottomLeft,
                                Colour = Color4.DarkViolet,
                                Alpha = 0.2f,
                                RelativeSizeAxes = Axes.Both,
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding(10),
                                Children = new Drawable[]
                                {
                                    textLocal = new SpriteText
                                    {
                                        Anchor = Anchor.TopLeft,
                                        Origin = Anchor.TopLeft,
                                    },
                                    textAggregate = new SpriteText
                                    {
                                        Anchor = Anchor.BottomRight,
                                        Origin = Anchor.BottomRight,
                                    },
                                }
                            },
                            spinner = new SpriteIcon
                            {
                                Anchor = Anchor.BottomLeft,
                                Origin = Anchor.Centre,
                                Icon = FontAwesome.Solid.CircleNotch,
                                Blending = BlendingParameters.Additive,
                                Colour = Color4.White,
                                Alpha = 0.2f,
                                Scale = new Vector2(20),
                                Position = new Vector2(20, -20)
                            }
                        }
                    },

                    audio = new AudioContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = content = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                    }
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                audio.Volume.BindValueChanged(updateLocal);
                audio.Balance.BindValueChanged(updateLocal);
                audio.Tempo.BindValueChanged(updateLocal);
                audio.Frequency.BindValueChanged(updateLocal, true);

                audio.AggregateVolume.BindValueChanged(updateAggregate);
                audio.AggregateBalance.BindValueChanged(updateAggregate);
                audio.AggregateTempo.BindValueChanged(updateAggregate);
                audio.AggregateFrequency.BindValueChanged(updateAggregate, true);
            }

            private void updateAggregate(ValueChangedEvent<double> obj)
            {
                textAggregate.Text = $"aggr: vol {audio.AggregateVolume.Value:F1} freq {audio.AggregateFrequency.Value:F1} tempo {audio.AggregateTempo.Value:F1} bal {audio.AggregateBalance.Value:F1}";
                volFill.Height = (float)audio.AggregateVolume.Value;

                warpContent.Rotation = (float)audio.AggregateBalance.Value * 4;
            }

            private void updateLocal(ValueChangedEvent<double> obj) =>
                textLocal.Text = $"local: vol {audio.Volume.Value:F1} freq {audio.Frequency.Value:F1} tempo {audio.Tempo.Value:F1} bal {audio.Balance.Value:F1}";

            protected override void OnDrag(DragEvent e)
            {
                Position += e.Delta;

                audio.Balance.Value = X / 100f;
                if (e.ControlPressed)
                    audio.Tempo.Value = 1 - Y / 100f;
                else
                    audio.Frequency.Value = 1 - Y / 100f;
            }

            protected override bool OnScroll(ScrollEvent e)
            {
                audio.Volume.Value += e.ScrollDelta.Y / 100f;
                return true;
            }

            protected override void Update()
            {
                base.Update();
                spinner.Rotation += (float)(audio.AggregateFrequency.Value * Clock.ElapsedFrameTime);
            }

            protected override bool OnDragStart(DragStartEvent e) => true;
        }
    }
}
