﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseLayoutInvalidation : GridTestCase
    {
        private readonly Container<DrawQuadOverlayBox> overlayBoxContainer;

        public TestCaseLayoutInvalidation()
            : base(1, 2)
        {
            Add(overlayBoxContainer = new Container<DrawQuadOverlayBox>());
            addTest<AutoSize1>();
            addTest<AutoSize2>();
            addTest<AutoSize3>();
            addTest<PaddingChange>();
            addTest<AutoSizeWithRotation1>();
            addTest<AutoSizeWithRotation2>(2);
            addTest<AutoSizeWithRotation3>(3);
            addTest<AutoSizeWithShear>();
            addTest<FillFlow1>();
            addTest<FillFlow2>();
            addTest<FillFlow2Simplified>();
        }

        private void addTest<T>(int numUpdates = 1) where T : LayoutInvalidationTest, new()
        {
            var name = typeof(T).Name;
            T instance1 = null, instance2 = null;
            AddStep($"{name} init", () =>
            {
                instance1 = new T();
                instance2 = new T();
                overlayBoxContainer.Clear();
                for (int i = 0; i < 2; i++)
                {
                    var instance = i == 0 ? instance1 : instance2;
                    instance.Container.Name = i == 0 ? "WithoutInvalidation" : "WithInvalidation";
                    Cell(i).Child = instance.Container;
                    for (int j = 0; j < instance.Drawables.Length; j++)
                        overlayBoxContainer.Add(new DrawQuadOverlayBox(instance.Drawables[j]) { Colour = RandomColorPalette.Get(j).Opacity(.5f) });
                }
            });
            AddRepeatStep($"{name} update", () =>
            {
                instance1.DoModification();
                instance2.DoModification();
                foreach (var d in instance2.Drawables)
                    d.Invalidate();
            }, numUpdates);
            AddStep($"{name} check", () =>
            {
                var state1 = instance1.GetRoundedDrawVectors();
                var state2 = instance2.GetRoundedDrawVectors();
                Assert.AreEqual(state2, state1, $"{name}: Same {nameof(DrawPosition)} and {nameof(DrawSize)}s");
            });
        }

        public static class RandomColorPalette
        {
            public const double GOLDEN_RATIO = 1.6180339887498948482;

            public static Color4 Get(int index) =>
                Color4.FromHsv(new Vector4((float)(index * GOLDEN_RATIO % 1), 1, 1, .5f));
        }

        public abstract class LayoutInvalidationTest
        {
            public Container Container;
            public abstract Drawable[] Drawables { get; }

            protected LayoutInvalidationTest()
            {
                Container = new Container
                {
                    Anchor = Anchor.Centre,
                    Scale = new Vector2(100)
                };
            }

            public Vector2[] GetRoundedDrawVectors() => Drawables.SelectMany(d => new[] { d.DrawPosition, d.DrawSize }).Select(v => new Vector2(round(v.X), round(v.Y))).ToArray();
            private static float round(float x) => (float)Math.Round(x, 2);

            public abstract void DoModification();
        }

        public abstract class Size2Case : LayoutInvalidationTest
        {
            protected readonly Container Root, Child;
            public override Drawable[] Drawables => new Drawable[] { Root, Child };

            protected Size2Case()
            {
                Container.Child = Root = new Container
                {
                    Name = "Root",
                    Size = new Vector2(1),
                    Child = Child = new Container { Name = "Child", Size = new Vector2(1) },
                };
            }
        }

        public abstract class Size3Case : LayoutInvalidationTest
        {
            protected readonly Container Root, Child1, Child2;
            public override Drawable[] Drawables => new Drawable[] { Root, Child1, Child2 };

            protected Size3Case()
            {
                Container.Child = Root = new Container
                {
                    Name = "Root",
                    Size = new Vector2(1),
                    Children = new[]
                    {
                        Child1 = new Container { Name = "Child1", Size = new Vector2(1) },
                        Child2 = new Container { Name = "Child2", Size = new Vector2(1) }
                    },
                };
            }
        }

        public class AutoSize1 : Size3Case
        {
            public AutoSize1()
            {
                Root.AutoSizeAxes = Axes.Y;
                Child2.Origin = Anchor.BottomCentre;
            }

            public override void DoModification() => Child1.RelativePositionAxes = Axes.Y;
        }

        public class AutoSize2 : Size3Case
        {
            public AutoSize2()
            {
                Root.AutoSizeAxes = Axes.X;
                Child1.Anchor = Anchor.TopRight;
                Child2.Anchor = Anchor.TopRight;
                Child2.X = 1;
            }

            public override void DoModification() => Child2.RelativePositionAxes = Axes.X;
        }

        public class AutoSize3 : Size3Case
        {
            public AutoSize3()
            {
                Root.AutoSizeAxes = Axes.X;
                Child1.Anchor = Anchor.TopRight;
                Child2.Anchor = Anchor.TopRight;
            }

            public override void DoModification() => Child1.RelativeSizeAxes = Axes.X;
        }

        public class PaddingChange : Size2Case
        {
            public PaddingChange()
            {
                Root.AutoSizeAxes = Axes.Y;
            }

            public override void DoModification() => Root.Padding = new MarginPadding { Top = 1 };
        }

        public class AutoSizeWithRotation1 : Size2Case
        {
            public AutoSizeWithRotation1()
            {
                Root.AutoSizeAxes = Axes.X;
                Child.RelativeSizeAxes = Axes.Y;
                Child.Rotation = -45;
            }

            public override void DoModification() => Root.Height = 2;
        }

        public class AutoSizeWithRotation2 : LayoutInvalidationTest
        {
            protected readonly Container Root, Child1, Child2;
            public override Drawable[] Drawables => new Drawable[] { Root, Child1, Child2 };

            public AutoSizeWithRotation2()
            {
                Container.Child = Root = new Container
                {
                    Name = "Root",
                    Size = new Vector2(1),
                    Children = new Drawable[]
                    {
                        Child1 = new Container
                        {
                            Name = "Child1",
                            Size = new Vector2(1),
                        },
                        Child2 = new Container
                        {
                            Name = "Child2",
                            Size = new Vector2(1)
                        }
                    },
                };
                Root.AutoSizeAxes = Axes.Both;
                Child1.RelativeSizeAxes = Axes.Y;
                Child1.Rotation = -45;
            }

            public override void DoModification() => Child2.Height = 2;
        }

        public class AutoSizeWithRotation3 : LayoutInvalidationTest
        {
            protected readonly Container Root, Child1, Child2, Child3;
            public override Drawable[] Drawables => new Drawable[] { Root, Child1, Child2, Child3 };

            public AutoSizeWithRotation3()
            {
                Container.Child = Root = new Container
                {
                    Name = "Root",
                    Size = new Vector2(1),
                    Children = new Drawable[]
                    {
                        Child1 = new Container
                        {
                            Name = "Child1",
                            Size = new Vector2(1),
                        },
                        Child2 = new Container
                        {
                            Name = "Child2",
                            Size = new Vector2(1)
                        },
                        Child3 = new Container
                        {
                            Name = "Child3",
                            Size = new Vector2(1)
                        }
                    },
                };
                Root.AutoSizeAxes = Axes.Both;
                Child1.RelativeSizeAxes = Axes.Y;
                Child1.Rotation = -45;
                Child3.RelativeSizeAxes = Axes.X;
                Child3.Rotation = 90;
            }

            public override void DoModification() => Child2.Height = 2;
        }
        public class AutoSizeWithShear : Size2Case
        {
            public AutoSizeWithShear()
            {
                Root.AutoSizeAxes = Axes.X;
                Child.RelativeSizeAxes = Axes.Y;
                Child.Anchor = Anchor.BottomRight;
                Child.Shear = new Vector2(1, 0);
            }

            public override void DoModification() => Root.Height = 2;
        }

        public class FillFlow1 : LayoutInvalidationTest
        {
            protected readonly FillFlowContainer Root;
            public override Drawable[] Drawables => new[] { Root, Root.Children[0], Root.Children[1] };

            public FillFlow1()
            {
                Container.Child = Root = new FillFlowContainer
                {
                    Name = "Root",
                    Size = new Vector2(1),
                    Children = new[]
                    {
                        new Container { Name = "Child1", Size = new Vector2(1) },
                        new Container { Name = "Child2", Size = new Vector2(1) }
                    },
                };
            }

            public override void DoModification() => Root.AutoSizeAxes = Axes.X;
        }

        public class FillFlow2 : LayoutInvalidationTest
        {
            protected readonly Container Root, Child2;
            protected readonly FillFlowContainer Child1;
            public override Drawable[] Drawables => new[] { Root, Child1, Child1.Children[0], Child1.Children[1], Child2 };

            public FillFlow2()
            {
                Container.Child = Root = new Container
                {
                    Name = "Root",
                    Size = new Vector2(1),
                    Children = new Drawable[]
                    {
                        Child1 = new FillFlowContainer
                        {
                            Name = "Child1",
                            Size = new Vector2(1),
                            Children = new Drawable[] { new Container { Size = new Vector2(1) }, new Container { Size = new Vector2(1) } }
                        },
                        Child2 = new Container { Name = "Child2", Size = new Vector2(1) }
                    },
                };
                Root.AutoSizeAxes = Axes.Y;
                Root.Margin = new MarginPadding { Right = 1 };
                Child1.RelativeSizeAxes = Axes.Both;
                Child1.FillMode = FillMode.Fill;
            }

            public override void DoModification() => Child2.Margin = new MarginPadding { Top = 1 };
        }

        public class FillFlow2Simplified : LayoutInvalidationTest
        {
            protected readonly Container Root;
            protected readonly FillFlowContainer Child;
            public override Drawable[] Drawables => new[] { Root, Child, Child.Children[0], Child.Children[1] };

            public FillFlow2Simplified()
            {
                Container.Child = Root = new Container
                {
                    Name = "Root",
                    Size = new Vector2(1),
                    Children = new Drawable[]
                    {
                        Child = new FillFlowContainer
                        {
                            Name = "Child",
                            Size = new Vector2(1),
                            Children = new Drawable[] { new Container { Size = new Vector2(1) }, new Container { Size = new Vector2(1) } }
                        },
                    },
                };
                Child.RelativeSizeAxes = Axes.Both;
            }

            public override void DoModification()
            {
                Root.Invalidate(Invalidation.MiscGeometry);
                Root.Size = new Vector2(2);
            }
        }
    }
}
