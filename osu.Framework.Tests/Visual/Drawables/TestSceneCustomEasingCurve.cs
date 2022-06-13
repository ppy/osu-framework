// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneCustomEasingCurve : FrameworkTestScene
    {
        public TestSceneCustomEasingCurve()
        {
            Add(new CurveVisualiser
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(400),
            });
        }

        private class CurveVisualiser : CompositeDrawable
        {
            private readonly BindableList<Vector2> easingVertices = new BindableList<Vector2>();

            private readonly SmoothPath path;
            private readonly Container<ControlPointVisualiser> controlPointContainer;
            private readonly SpriteIcon sideTracker;
            private readonly Box verticalTracker;
            private readonly Box horizontalTracker;

            private readonly CustomEasingFunction easingFunction;

            public CurveVisualiser()
            {
                easingFunction = new CustomEasingFunction { EasingVertices = { BindTarget = easingVertices } };

                Container gridContainer;

                InternalChildren = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        BorderColour = Color4.White,
                        BorderThickness = 2,
                        Child = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Alpha = 0,
                            AlwaysPresent = true
                        }
                    },
                    gridContainer = new Container { RelativeSizeAxes = Axes.Both },
                    path = new SmoothPath
                    {
                        PathRadius = 1
                    },
                    controlPointContainer = new Container<ControlPointVisualiser> { RelativeSizeAxes = Axes.Both },
                    sideTracker = new SpriteIcon
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.BottomCentre,
                        RelativePositionAxes = Axes.Y,
                        Size = new Vector2(10),
                        X = 2,
                        Colour = Color4.SkyBlue,
                        Rotation = 90,
                        Icon = FontAwesome.Solid.MapMarker,
                    },
                    verticalTracker = new Box
                    {
                        Origin = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.X,
                        RelativePositionAxes = Axes.Y,
                        Height = 1,
                        Colour = Color4.SkyBlue
                    },
                    horizontalTracker = new Box
                    {
                        Origin = Anchor.TopCentre,
                        RelativeSizeAxes = Axes.Y,
                        RelativePositionAxes = Axes.X,
                        Width = 1,
                        Colour = Color4.SkyBlue
                    }
                };

                for (int i = 0; i <= 10; i++)
                {
                    gridContainer.Add(new Box
                    {
                        Origin = Anchor.CentreLeft,
                        RelativeSizeAxes = Axes.X,
                        RelativePositionAxes = Axes.Y,
                        Height = 2,
                        Y = 0.1f * i,
                        Colour = Color4.White.Opacity(0.1f)
                    });

                    gridContainer.Add(new Box
                    {
                        Origin = Anchor.TopCentre,
                        RelativeSizeAxes = Axes.Y,
                        RelativePositionAxes = Axes.X,
                        Width = 2,
                        X = 0.1f * i,
                        Colour = Color4.White.Opacity(0.1f)
                    });
                }

                controlPointContainer.Add(new ControlPointVisualiser
                {
                    PointPosition = { Value = new Vector2(100, 100) }
                });
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                sideTracker.MoveToY(1)
                           .Then().MoveToY(0, 2000, easingFunction)
                           .Then().Delay(200)
                           .Loop();

                verticalTracker.MoveToY(1)
                               .Then().MoveToY(0, 2000, easingFunction)
                               .Then().Delay(200)
                               .Loop();

                horizontalTracker.MoveToX(0)
                                 .Then().MoveToX(1, 2000)
                                 .Then().Delay(200)
                                 .Loop();
            }

            protected override void Update()
            {
                base.Update();

                ControlPointVisualiser[] ordered = controlPointContainer.OrderBy(p => p.PointPosition.Value.X).ToArray();

                for (int i = 0; i < ordered.Length; i++)
                {
                    ordered[i].Last = i > 0 ? ordered[i - 1] : null;
                    ordered[i].Next = i < ordered.Length - 1 ? ordered[i + 1] : null;
                }

                var vectorPath = new List<Vector2> { new Vector2(0, DrawHeight) };
                vectorPath.AddRange(ordered.Select(p => p.PointPosition.Value));
                vectorPath.Add(new Vector2(DrawWidth, 0));

                var bezierPath = PathApproximator.ApproximateBezier(vectorPath.ToArray());
                path.Vertices = bezierPath;
                path.Position = -path.PositionInBoundingBox(Vector2.Zero);

                easingVertices.Clear();
                easingVertices.AddRange(bezierPath.Select(p => Vector2.Divide(p, DrawSize)).Select(p => new Vector2(p.X, 1 - p.Y)));
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                controlPointContainer.Add(new ControlPointVisualiser
                {
                    PointPosition = { Value = ToLocalSpace(e.ScreenSpaceMousePosition) }
                });

                return true;
            }
        }

        private class ControlPointVisualiser : CompositeDrawable
        {
            public readonly Bindable<Vector2> PointPosition = new Bindable<Vector2>();

            public ControlPointVisualiser Last;
            public ControlPointVisualiser Next;

            private readonly SmoothPath path;

            public ControlPointVisualiser()
            {
                RelativeSizeAxes = Axes.Both;

                InternalChildren = new Drawable[]
                {
                    path = new SmoothPath
                    {
                        PathRadius = 1,
                        Colour = Color4.Yellow.Opacity(0.5f)
                    },
                    new PointHandle
                    {
                        PointPosition = { BindTarget = PointPosition }
                    }
                };
            }

            protected override void Update()
            {
                base.Update();

                path.ClearVertices();

                path.AddVertex(Last?.PointPosition.Value ?? new Vector2(0, DrawHeight));
                path.AddVertex(PointPosition.Value);

                if (Next == null)
                    path.AddVertex(new Vector2(DrawWidth, 0));

                path.Position = -path.PositionInBoundingBox(Vector2.Zero);
            }
        }

        private class PointHandle : Circle
        {
            public readonly Bindable<Vector2> PointPosition = new Bindable<Vector2>();

            public PointHandle()
            {
                Origin = Anchor.Centre;
                Size = new Vector2(10);

                Colour = Color4.Yellow;
                Alpha = 0.5f;
            }

            protected override void Update()
            {
                base.Update();

                Position = PointPosition.Value;
            }

            private bool isDragging;

            protected override bool OnHover(HoverEvent e)
            {
                updateColour();
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e) => updateColour();

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                isDragging = true;
                return true;
            }

            protected override void OnMouseUp(MouseUpEvent e)
            {
                isDragging = false;
                updateColour();
            }

            protected override bool OnDragStart(DragStartEvent e) => true;

            protected override void OnDrag(DragEvent e) => PointPosition.Value += e.Delta;

            private void updateColour() => Alpha = IsHovered || isDragging ? 1f : 0.5f;
        }

        private class CustomEasingFunction : IEasingFunction
        {
            public readonly BindableList<Vector2> EasingVertices = new BindableList<Vector2>();

            public double ApplyEasing(double time)
            {
                for (int i = 0; i < EasingVertices.Count; i++)
                {
                    if (EasingVertices[i].X < time)
                        continue;

                    Vector2 last = EasingVertices[i - 1];
                    Vector2 next = EasingVertices[i];

                    return Interpolation.ValueAt(time, last.Y, next.Y, last.X, next.X);
                }

                return 0;
            }
        }
    }
}
