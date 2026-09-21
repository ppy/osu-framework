// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osuTK;

namespace osu.Framework.Tests.Visual.Drawables
{
    public partial class TestSceneCubicBezierEasing : TestScene
    {
        private SpriteText easingText = null!;
        private EasingEditor easingEditor = null!;

        [Resolved]
        private Clipboard clipboard { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Padding = new MarginPadding { Horizontal = 100 },
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(20),
                Children = new Drawable[]
                {
                    easingEditor = new EasingEditor
                    {
                        Size = new Vector2(200)
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(30),
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Spacing = new Vector2(4),
                                Children = new Drawable[]
                                {
                                    easingText = new SpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Width = 420,
                                    },
                                    new BasicButton
                                    {
                                        AutoSizeAxes = Axes.Y,
                                        Width = 50,
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Text = "Copy",
                                        FlashColour = FrameworkColour.BlueGreen.Lighten(0.5f),
                                        FlashDuration = 1000,
                                        Action = () => clipboard.SetText(easingText.Text.ToString()),
                                    }
                                }
                            },
                            new EasingPreview(easingEditor.EasingFunction.GetBoundCopy())
                        }
                    }
                }
            };

            easingEditor.EasingFunction.BindValueChanged(e =>
            {
                var easing = e.NewValue;

                easingText.Text = FormattableString.Invariant($"new {nameof(CubicBezierEasingFunction)}({easing.X1:0.##}, {easing.Y1:0.##}, {easing.X2:0.##}, {easing.Y2:0.##})");
            });
        }

        private partial class EasingEditor : CompositeDrawable
        {
            private readonly Bindable<Vector2> p1 = new Bindable<Vector2>(new Vector2(0.5f, 0f));
            private readonly Bindable<Vector2> p2 = new Bindable<Vector2>(new Vector2(0.5f, 1f));
            private readonly SmoothPath path;
            private readonly Box line1, line2;

            public readonly Bindable<CubicBezierEasingFunction> EasingFunction = new Bindable<CubicBezierEasingFunction>();

            public EasingEditor()
            {
                InternalChild = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    FillMode = FillMode.Fit,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Alpha = 0.1f,
                        },
                        path = new SmoothPath
                        {
                            Position = new Vector2(1, -1),
                            PathRadius = 1f,
                            Anchor = Anchor.BottomLeft,
                        },
                        line1 = new Box
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 1,
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.CentreLeft,
                            EdgeSmoothness = new Vector2(1),
                            Alpha = 0.1f,
                        },
                        line2 = new Box
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 1,
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.CentreRight,
                            EdgeSmoothness = new Vector2(1),
                            Alpha = 0.1f,
                        },
                        new ControlPointHandle
                        {
                            ControlPoint = { BindTarget = p1 },
                        },
                        new ControlPointHandle
                        {
                            ControlPoint = { BindTarget = p2 },
                        },
                    }
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                p1.BindValueChanged(_ => Scheduler.AddOnce(easingChanged));
                p2.BindValueChanged(_ => Scheduler.AddOnce(easingChanged));
                easingChanged();
            }

            private void easingChanged()
            {
                path.ClearVertices();

                var easing = EasingFunction.Value = new CubicBezierEasingFunction(p1.Value.X, p1.Value.Y, p2.Value.X, p2.Value.Y);

                for (double d = 0; d < 1; d += 0.01)
                {
                    double value = easing.ApplyEasing(d);

                    path.AddVertex(new Vector2((float)d * DrawWidth, 1 - (float)value * DrawHeight));
                }

                path.AddVertex(new Vector2(DrawWidth, 1 - (float)easing.ApplyEasing(1) * DrawHeight));

                path.OriginPosition = path.PositionInBoundingBox(new Vector2());

                line1.Width = p1.Value.Length;
                line1.Rotation = -MathHelper.RadiansToDegrees(MathF.Atan2(p1.Value.Y, p1.Value.X));

                line2.Width = Vector2.Distance(p2.Value, Vector2.One);
                line2.Rotation = -MathHelper.RadiansToDegrees(MathF.Atan2(1 - p2.Value.Y, 1 - p2.Value.X));
            }
        }

        private partial class ControlPointHandle : CompositeDrawable
        {
            public readonly Bindable<Vector2> ControlPoint = new Bindable<Vector2>();

            public ControlPointHandle()
            {
                RelativePositionAxes = Axes.Both;
                Size = new Vector2(20);
                Origin = Anchor.Centre;

                InternalChild = new Circle
                {
                    RelativeSizeAxes = Axes.Both,
                    Size = new Vector2(0.5f),
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                ControlPoint.BindValueChanged(p => Position = new Vector2(p.NewValue.X, 1 - p.NewValue.Y), true);
            }

            protected override bool OnDragStart(DragStartEvent e) => true;

            protected override void OnDrag(DragEvent e)
            {
                base.OnDrag(e);

                var position = Vector2.Divide(Parent!.ToLocalSpace(e.ScreenSpaceMousePosition), Parent.ChildSize);

                ControlPoint.Value = new Vector2(
                    float.Round(float.Clamp(position.X, 0, 1), 2),
                    float.Round(1f - position.Y, 2)
                );
            }

            protected override bool OnHover(HoverEvent e)
            {
                this.ScaleTo(1.35f, 50);
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                this.ScaleTo(1f, 50);
            }
        }

        private partial class EasingPreview : CompositeDrawable
        {
            private readonly Box box;
            private readonly SpriteText durationText;

            private bool flipped;
            private readonly IBindable<CubicBezierEasingFunction> easingFunction;

            private readonly BindableDouble duration = new BindableDouble
            {
                Value = 1000,
                MinValue = 100,
                MaxValue = 5000,
            };

            public EasingPreview(IBindable<CubicBezierEasingFunction> easingFunction)
            {
                this.easingFunction = easingFunction;

                Width = 400;
                AutoSizeAxes = Axes.Y;

                InternalChild = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(10),
                    Children = new Drawable[]
                    {
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 25,
                            RowDimensions = [new Dimension(GridSizeMode.AutoSize)],
                            ColumnDimensions = [new Dimension(GridSizeMode.Absolute, 150), new Dimension(), new Dimension(GridSizeMode.Absolute, 20), new Dimension(GridSizeMode.AutoSize)],

                            Content = new Drawable?[][]
                            {
                                [
                                    durationText = new SpriteText
                                    {
                                        Text = "Duration: 1000"
                                    },
                                    new BasicSliderBar<double>
                                    {
                                        Current = duration,
                                        RelativeSizeAxes = Axes.X,
                                        Height = 20,
                                    },
                                    null,
                                    new BasicButton
                                    {
                                        RelativeSizeAxes = Axes.Y,
                                        Width = 50,
                                        Text = "Play",
                                        FlashColour = FrameworkColour.BlueGreen.Lighten(0.5f),
                                        Action = play,
                                    }
                                ]
                            }
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Padding = new MarginPadding { Right = 50 },
                            Child = box = new Box
                            {
                                RelativePositionAxes = Axes.X,
                                Size = new Vector2(50),
                            },
                        }
                    }
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                duration.BindValueChanged(e => durationText.Text = FormattableString.Invariant($"Duration: {e.NewValue:N0}"), true);
            }

            private void play()
            {
                box.MoveToX(flipped ? 0 : 1, duration.Value, easingFunction.Value);

                flipped = !flipped;
            }
        }
    }
}
