// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneCompositeDrawable : FrameworkTestScene
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

        private class SortableComposite : CompositeDrawable
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

        private class SortableBox : Box
        {
            public int Id;
        }
    }
}
