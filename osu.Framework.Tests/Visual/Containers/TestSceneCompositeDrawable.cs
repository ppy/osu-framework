// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public partial class TestSceneCompositeDrawable : FrameworkTestScene
    {
        [Test]
        public void TestSortWithComparerChange()
        {
            SortableComposite composite = null;
            Drawable firstItem = null;
            Drawable lastItem = null;

            AddStep("add composite", () => Child = composite = new SortableComposite());

            AddStep("reverse comparer", () =>
            {
                firstItem = composite.InternalChildren[0];
                lastItem = composite.InternalChildren[^1];

                for (int i = 0; i < composite.InternalChildren.Count; i++)
                    ((SortableBox)composite.InternalChildren[i]).Id = composite.InternalChildren.Count - 1 - i;
                composite.Sort();
            });

            AddAssert("children reversed", () => composite.InternalChildren[0] == lastItem && composite.InternalChildren[^1] == firstItem);
        }

        [Test]
        public void TestChangeChildDepthFailsIfNotCalledOnDirectChild()
        {
            Container parent = null;
            Container nestedChild = null;

            AddStep("create hierarchy", () => Child = parent = new Container
            {
                Child = new Container
                {
                    Child = nestedChild = new Container()
                }
            });

            AddStep("bad change child depth call fails", () => Assert.Throws<InvalidOperationException>(() => parent.ChangeChildDepth(nestedChild, 10)));
        }

        /// <summary>
        /// Ensures that <see cref="Framework.Graphics.Transforms.Transformable.ClearTransforms"/> does not discard auto-sizing indefinitely.
        /// </summary>
        [Test]
        public void TestClearTransformsOnDelayedAutoSize()
        {
            Container container = null;
            bool clearedTransforms = false;

            AddStep("create hierarchy", () =>
            {
                Child = container = new Container
                {
                    Masking = true,
                    AutoSizeAxes = Axes.Both,
                    AutoSizeDuration = 1000,
                    Child = new Box { Size = new Vector2(100) },
                };

                clearedTransforms = false;

                container.OnAutoSize += () => Schedule(() =>
                {
                    if (clearedTransforms)
                        return;

                    container.ClearTransforms();
                    clearedTransforms = true;
                });
            });

            AddAssert("transforms cleared", () => clearedTransforms);
            AddUntilStep("container still autosized", () => container.Size == new Vector2(100));
        }

        [Test]
        public void TestAutoSizeDuration()
        {
            Container parent = null;
            Drawable child = null;

            AddStep("create hierarchy", () =>
            {
                Child = parent = new Container
                {
                    Masking = true,
                    AutoSizeAxes = Axes.Both,
                    AutoSizeDuration = 500,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Yellow,
                        },
                        new Container
                        {
                            Padding = new MarginPadding(50),
                            AutoSizeAxes = Axes.Both,
                            Child = child = new Box
                            {
                                Size = new Vector2(100),
                                Colour = Color4.Red,
                            }
                        }
                    }
                };
            });

            AddSliderStep("AutoSizeDuration", 0f, 1500f, 500f, value =>
            {
                if (parent != null) parent.AutoSizeDuration = value;
            });
            AddSliderStep("Width", 0f, 300f, 100f, value =>
            {
                if (child != null) child.Width = value;
            });
            AddSliderStep("Height", 0f, 300f, 100f, value =>
            {
                if (child != null) child.Height = value;
            });
        }

        [Test]
        public void TestFinishAutoSizeTransforms()
        {
            Container parent = null;
            Drawable child = null;

            AddStep("create hierarchy", () =>
            {
                Child = parent = new Container
                {
                    Masking = true,
                    AutoSizeAxes = Axes.Both,
                    AutoSizeDuration = 1000,
                    Name = "Parent",
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Yellow,
                        },
                        child = new Box
                        {
                            Size = new Vector2(100),
                            Colour = Color4.Red,
                            Alpha = 0.5f,
                        }
                    }
                };
            });
            AddAssert("size matches child", () => Precision.AlmostEquals(parent.ChildSize, child.LayoutSize));
            AddStep("resize child", () => child.Size = new Vector2(200));
            AddAssert("size doesn't match child", () => !Precision.AlmostEquals(parent.ChildSize, child.LayoutSize));
            AddStep("finish autosize transform", () => parent.FinishAutoSizeTransforms());
            AddAssert("size matches child", () => Precision.AlmostEquals(parent.ChildSize, child.LayoutSize));
        }

        private partial class SortableComposite : CompositeDrawable
        {
            public SortableComposite()
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
                AutoSizeAxes = Axes.Both;

                for (int i = 0; i < 128; i++)
                {
                    AddInternal(new SortableBox
                    {
                        Id = i,
                        Colour = new Color4(i / 255f, i / 255f, i / 255f, 1.0f),
                        Position = new Vector2(3 * i),
                        Size = new Vector2(50)
                    });
                }
            }

            public void Sort() => SortInternal();

            protected override int Compare(Drawable x, Drawable y)
                => ((SortableBox)x).Id.CompareTo(((SortableBox)y).Id);
        }

        private partial class SortableBox : Box
        {
            public int Id;
        }
    }
}
