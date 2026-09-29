// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Layout;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public partial class TestSceneSpring : TestScene
    {
        private readonly BindableFloat naturalFrequency = new BindableFloat(2)
        {
            MinValue = 0.1f,
            MaxValue = 8f,
            Precision = 0.01f,
        };

        private readonly BindableFloat damping = new BindableFloat(1)
        {
            MinValue = 0f,
            MaxValue = 6f,
            Precision = 0.01f,
        };

        private readonly BindableFloat response = new BindableFloat(0)
        {
            MinValue = -5f,
            MaxValue = 5f,
            Precision = 0.01f,
        };

        private SpringTimeline timeline = null!;
        private FollowingCircle followingCircle = null!;
        private DraggableCircle targetCircle = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Children =
                [
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        ColumnDimensions = [new Dimension(GridSizeMode.Absolute, 300), new Dimension()],
                        RowDimensions = [new Dimension(GridSizeMode.AutoSize)],
                        Padding = new MarginPadding { Vertical = 150 },
                        Content = new Drawable[][]
                        {
                            [
                                new FillFlowContainer
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Direction = FillDirection.Vertical,
                                    Children =
                                    [
                                        new LabelledSliderBar("Frequency")
                                        {
                                            Size = new Vector2(300, 30),
                                            Current = naturalFrequency,
                                        },
                                        new LabelledSliderBar("Damping")
                                        {
                                            Size = new Vector2(300, 30),
                                            Current = damping,
                                        },
                                        new LabelledSliderBar("Response")
                                        {
                                            Size = new Vector2(300, 30),
                                            Current = response,
                                        },
                                    ]
                                },
                                timeline = new SpringTimeline
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = 150,
                                },
                            ]
                        }
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 300,
                        Children =
                        [
                            targetCircle = new DraggableCircle
                            {
                                Anchor = Anchor.Centre,
                            },
                            followingCircle = new FollowingCircle(targetCircle)
                            {
                                Anchor = Anchor.Centre,
                                Depth = 1,
                            }
                        ]
                    }
                ]
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            naturalFrequency.BindValueChanged(_ => Scheduler.AddOnce(updateSpring));
            damping.BindValueChanged(_ => Scheduler.AddOnce(updateSpring));
            response.BindValueChanged(_ => Scheduler.AddOnce(updateSpring));
            updateSpring();
        }

        private void updateSpring()
        {
            var springParameters = new SpringParameters
            {
                NaturalFrequency = naturalFrequency.Value,
                Damping = damping.Value,
                Response = response.Value,
            };

            followingCircle.SpringParameters = springParameters;

            timeline.SetSpringParameters(springParameters);
        }

        private partial class LabelledSliderBar : CompositeDrawable
        {
            private readonly BasicSliderBar<float> sliderBar;
            private readonly SpriteText label;
            private readonly string labelText;

            public Bindable<float> Current
            {
                get => sliderBar.Current;
                set => sliderBar.Current = value;
            }

            public LabelledSliderBar(string labelText)
            {
                this.labelText = labelText;

                InternalChildren =
                [
                    sliderBar = new BasicSliderBar<float>
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    label = new SpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Margin = new MarginPadding { Left = 5 },
                        Font = new FontUsage(size: 15f),
                        Colour = Color4.Black
                    },
                ];
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                Current.BindValueChanged(e =>
                {
                    label.Text = $"{labelText}: {e.NewValue:F2}";
                }, true);
            }
        }

        private partial class SpringTimeline : CompositeDrawable
        {
            private const double graph_duration = 3_000;

            private readonly SmoothPath graph;

            private readonly FloatSpring spring = new FloatSpring();

            private readonly LayoutValue drawSizeBacking = new LayoutValue(Invalidation.DrawSize);

            public SpringTimeline()
            {
                AddLayout(drawSizeBacking);

                InternalChildren =
                [
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0.1f
                    },
                    graph = new SmoothPath
                    {
                        PathRadius = 1
                    }
                ];

                for (int i = 0; i <= graph_duration; i += 1000)
                {
                    AddInternal(new Box
                    {
                        RelativePositionAxes = Axes.X,
                        X = i / (float)graph_duration,
                        RelativeSizeAxes = Axes.Y,
                        Width = 1,
                        Origin = Anchor.TopCentre,
                        Alpha = 0.2f,
                    });
                }
            }

            public void SetSpringParameters(SpringParameters parameters)
            {
                spring.Parameters = parameters;
                updateGraph();
            }

            protected override void Update()
            {
                base.Update();

                if (!drawSizeBacking.IsValid)
                {
                    updateGraph();
                    drawSizeBacking.Validate();
                }
            }

            private void updateGraph()
            {
                spring.Current = 0;
                spring.Velocity = 0;
                spring.PreviousTarget = 0;

                int numSteps = (int)DrawWidth;
                double timestep = graph_duration / numSteps;

                var vertices = new Vector2[numSteps];

                for (int i = 0; i < numSteps; i++)
                {
                    vertices[i] = new Vector2(i, (1 - spring.Current) * DrawHeight);

                    spring.Update(timestep, 1);
                }

                graph.Vertices = vertices;

                graph.OriginPosition = graph.PositionInBoundingBox(new Vector2());
            }
        }

        private partial class DraggableCircle : Circle
        {
            public DraggableCircle()
            {
                Size = new Vector2(20);
                Colour = FrameworkColour.Green;
                Origin = Anchor.Centre;
            }

            protected override bool OnHover(HoverEvent e)
            {
                Scale = new Vector2(1.2f);

                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                Scale = new Vector2(1);

                base.OnHoverLost(e);
            }

            protected override bool OnDragStart(DragStartEvent e) => true;

            protected override void OnDrag(DragEvent e)
            {
                Position += e.Delta;
            }
        }

        private partial class FollowingCircle : Circle
        {
            private readonly Drawable target;

            public FollowingCircle(Drawable target)
            {
                this.target = target;
                Size = new Vector2(30);
                Colour = FrameworkColour.Yellow;
                Origin = Anchor.Centre;
            }

            private readonly Vector2Spring position = new Vector2Spring();

            public SpringParameters SpringParameters
            {
                set => position.Parameters = value;
            }

            protected override void Update()
            {
                base.Update();

                Position = position.Update(Time.Elapsed, target.Position);
            }
        }
    }
}
